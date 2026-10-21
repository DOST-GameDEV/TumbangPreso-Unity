using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    [Serializable] public sealed class LocalReplaySegment
    {
        public string File, Sha256;
        public string SceneFile,SceneSha256,EffectsFile,EffectsSha256,SceneryFile,ScenerySha256;
        public float Offset, Start, End;
        public int Round;
    }
    [Serializable] public sealed class LocalReplayManifest
    {
        public int Version = 1;
        public long MatchId;
        public string CreatedUtc, Map, Mode, Rules, Build;
        public bool Custom, Completed;
        public string Warning;
        public List<LocalReplaySegment> Segments = new List<LocalReplaySegment>();
        public float Duration => Segments.Count == 0 ? 0 : Segments[Segments.Count - 1].Offset +
            Segments[Segments.Count - 1].End - Segments[Segments.Count - 1].Start;
    }
    public sealed class LocalReplayEntry
    {
        public string Directory;
        public LocalReplayManifest Manifest;
    }

    // Local replay data is separate from career/reward eligibility. Custom rooms
    // use exactly the same recorder. Each immutable segment is committed before
    // its manifest entry, so an interrupted write never advertises missing data.
    public static class LocalReplayStore
    {
        public static string DefaultFolder => Path.Combine(ProfilePaths.Root, "Replays");
        private static string LocationFile => Path.Combine(ProfilePaths.Root, "replay-folder.txt");
        public static string Folder
        {
            get
            {
                try { if (File.Exists(LocationFile)) return Path.GetFullPath(File.ReadAllText(LocationFile).Trim()); }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { }
                return DefaultFolder;
            }
        }
        public static bool SetFolder(string folder, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathRooted(folder))
                    throw new ArgumentException("Choose an absolute folder path.");
                folder = Path.GetFullPath(folder.Trim());
                Directory.CreateDirectory(folder);
                string probe = Path.Combine(folder, ".tump-write-" + Guid.NewGuid().ToString("N"));
                using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.WriteByte(0);
                File.Delete(probe);
                Directory.CreateDirectory(ProfilePaths.Root);
                AtomicWrite(LocationFile, Encoding.UTF8.GetBytes(folder));
                return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException || e is NotSupportedException)
            { error = "Cannot save replays here: " + e.Message; return false; }
        }
        public static List<LocalReplayEntry> List(string folder)
        {
            var entries = new List<LocalReplayEntry>();
            if (!Directory.Exists(folder)) return entries;
            foreach (string directory in Directory.EnumerateDirectories(folder, "TUMP-*"))
            {
                try
                {
                    string path = Path.Combine(directory, "manifest.json");
                    if (!File.Exists(path) || new FileInfo(path).Length > 4 * 1024 * 1024) continue;
                    // Browsing and the background recorder can overlap. Allow
                    // atomic rename while this reader holds the previous file.
                    using var stream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                    using var reader = new StreamReader(stream,Encoding.UTF8);
                    var manifest = JsonUtility.FromJson<LocalReplayManifest>(reader.ReadToEnd());
                    if (!ValidManifest(manifest)) continue;
                    entries.Add(new LocalReplayEntry { Directory = directory, Manifest = manifest });
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { }
            }
            entries.Sort((a, b) => string.CompareOrdinal(b.Manifest.CreatedUtc, a.Manifest.CreatedUtc));
            return entries;
        }
        private static bool ValidManifest(LocalReplayManifest manifest)
        {
            if(manifest==null||manifest.Version!=1||manifest.MatchId<=0||
                !Enum.TryParse(manifest.Mode,out Core.GameMode mode)||!Enum.IsDefined(typeof(Core.GameMode),mode)||
                manifest.Segments==null||manifest.Segments.Count==0||manifest.Segments.Count>10000)return false;
            float offset=0;int round=0;
            foreach(var segment in manifest.Segments)
            {
                if(segment==null||float.IsNaN(segment.Start)||float.IsInfinity(segment.Start)||
                    float.IsNaN(segment.End)||float.IsInfinity(segment.End)||
                    float.IsNaN(segment.Offset)||float.IsInfinity(segment.Offset)||
                    segment.End<=segment.Start||segment.End-segment.Start>8.1f||Math.Abs(segment.Offset-offset)>.001f||
                    segment.Round<1||segment.Round>64||segment.Round<round||string.IsNullOrEmpty(segment.File))return false;
                offset+=segment.End-segment.Start;round=segment.Round;
            }
            return true;
        }
        public static RecordedMatchClip Read(LocalReplayEntry entry, int index)
        {
            var segment = entry.Manifest.Segments[index];
            // Never follow a path from a replay manifest outside its owned folder.
            if (segment.File != Path.GetFileName(segment.File) || !segment.File.EndsWith(".tps", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid replay segment path.");
            string path = Path.Combine(entry.Directory, segment.File);
            if (new FileInfo(path).Length > RecordedMatchClip.ByteLimit) throw new InvalidDataException("Replay segment is too large.");
            byte[] bytes = File.ReadAllBytes(path);
            if (Hash(bytes) != segment.Sha256 || !RecordedMatchClip.TryDecodeLocal(bytes, out var clip, out string error))
                throw new InvalidDataException("Replay segment is damaged or incompatible.");
            if (clip.MatchId != entry.Manifest.MatchId || clip.Map != entry.Manifest.Map ||
                clip.Round != segment.Round || clip.Start != segment.Start || clip.End != segment.End)
                throw new InvalidDataException("Replay segment identity changed.");
            return clip;
        }
        public static LocalReplaySceneSegment ReadScene(LocalReplayEntry entry,int index)
        {
            var segment=entry.Manifest.Segments[index];if(string.IsNullOrEmpty(segment.SceneFile))return null;
            if(segment.SceneFile!=Path.GetFileName(segment.SceneFile)||!segment.SceneFile.EndsWith(".scene.json",StringComparison.Ordinal))
                throw new InvalidDataException("Invalid replay scene path.");
            string path=Path.Combine(entry.Directory,segment.SceneFile);
            if(new FileInfo(path).Length>4*1024*1024)throw new InvalidDataException("Replay scene data exceeds its budget.");
            byte[] bytes=File.ReadAllBytes(path);if(Hash(bytes)!=segment.SceneSha256)throw new InvalidDataException("Replay scene data is damaged.");
            var result=JsonUtility.FromJson<LocalReplaySceneSegment>(Encoding.UTF8.GetString(bytes));
            if(result==null||result.Version!=1||result.Frames==null||result.Frames.Count<2||result.Frames.Count>MatchPoseHistory.Samples)
                throw new InvalidDataException("Replay scene data is incompatible.");
            float previous=float.NegativeInfinity;
            foreach(var frame in result.Frames)
            {
                if(frame==null||float.IsNaN(frame.Time)||float.IsInfinity(frame.Time)||frame.Time<=previous||
                    frame.Poses==null||frame.Poses.Count>256||frame.Surfaces==null||frame.Surfaces.Count>512)
                    throw new InvalidDataException("Invalid replay scene frame.");
                if(frame.HasShaderTime&&!Finite(frame.ShaderTime))throw new InvalidDataException("Invalid recorded shader clock.");
                if(frame.HasCrowd&&(!Finite(frame.CrowdClock)||!Finite(frame.CrowdCheer)||!Finite(frame.CrowdGroan)||!Finite(frame.CrowdWave)))
                    throw new InvalidDataException("Invalid recorded crowd state.");
                if(frame.Water!=null)
                {
                    if(frame.Water.Count>8)throw new InvalidDataException("Too many recorded water surfaces.");
                    foreach(var water in frame.Water)
                        if(water==null||string.IsNullOrEmpty(water.Path)||water.Path.Length>1024||string.IsNullOrEmpty(water.Name)||
                            water.Shader!="TumbangPreso/RoofPoolWater"||!Finite(water.WakeStrength)||
                            water.Swimmers==null||water.Swimmers.Length!=4||Array.Exists(water.Swimmers,v=>!Finite(v)))
                            throw new InvalidDataException("Invalid recorded water state.");
                }
                previous=frame.Time;
                foreach(var pose in frame.Poses)
                    if(pose==null||string.IsNullOrEmpty(pose.Path)||pose.Path.Length>1024||string.IsNullOrEmpty(pose.Name)||
                        !Finite(pose.Position)||!Finite(pose.Scale)||!Finite(pose.Rotation))throw new InvalidDataException("Invalid recorded traffic pose.");
                foreach(var surface in frame.Surfaces)
                    if(surface==null||string.IsNullOrEmpty(surface.Path)||surface.Path.Length>1024||string.IsNullOrEmpty(surface.Name)||
                        surface.Slot<0||surface.Slot>256||!Finite(surface.Colour)||!Finite(surface.Emission))throw new InvalidDataException("Invalid recorded traffic surface.");
            }
            if(result.Frames[0].Time>segment.Start+.001f||result.Frames[result.Frames.Count-1].Time<segment.End-.001f)
                throw new InvalidDataException("Incomplete recorded scene window.");
            return result;
        }
        public static LocalReplayFxSegment ReadEffects(LocalReplayEntry entry,int index)
        {
            var segment=entry.Manifest.Segments[index];if(string.IsNullOrEmpty(segment.EffectsFile))return null;
            if(segment.EffectsFile!=Path.GetFileName(segment.EffectsFile)||!segment.EffectsFile.EndsWith(".fx.gz",StringComparison.Ordinal))throw new InvalidDataException("Invalid recorded effects path.");
            string path=Path.Combine(entry.Directory,segment.EffectsFile);if(new FileInfo(path).Length>LocalReplayEffectsCodec.MaxEncodedBytes)throw new InvalidDataException("Recorded effects file is too large.");
            byte[] bytes=File.ReadAllBytes(path);if(Hash(bytes)!=segment.EffectsSha256)throw new InvalidDataException("Recorded effects are damaged.");
            return LocalReplayEffectsCodec.Decode(bytes,segment.Start,segment.End);
        }
        public static LocalReplayScenerySegment ReadScenery(LocalReplayEntry entry,int index)
        {
            var segment=entry.Manifest.Segments[index];if(string.IsNullOrEmpty(segment.SceneryFile))return null;
            if(segment.SceneryFile!=Path.GetFileName(segment.SceneryFile)||!segment.SceneryFile.EndsWith(".scenery.gz",StringComparison.Ordinal))throw new InvalidDataException("Invalid recorded scenery path.");
            string path=Path.Combine(entry.Directory,segment.SceneryFile);if(new FileInfo(path).Length>LocalReplaySceneryCodec.MaxEncodedBytes)throw new InvalidDataException("Recorded scenery is too large.");
            byte[] bytes=File.ReadAllBytes(path);if(Hash(bytes)!=segment.ScenerySha256)throw new InvalidDataException("Recorded scenery is damaged.");
            return LocalReplaySceneryCodec.Decode(bytes,segment.Start,segment.End);
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
        private static bool Finite(Vector4 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z)&&Finite(value.w);
        private static bool Finite(Quaternion value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z)&&Finite(value.w);
        private static bool Finite(Color value)=>Finite(value.r)&&Finite(value.g)&&Finite(value.b)&&Finite(value.a);
        internal static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal static void AtomicWrite(string path, byte[] bytes)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            for(int attempt=0;;attempt++)
            {
                try
                {
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                    break;
                }
                catch(IOException error)
                {
                    // Windows indexers/readers can briefly hold a rename target.
                    // Retry the same committed bytes on the disk worker; never
                    // truncate the previous manifest or claim a failed save.
                    if(attempt>=4)throw new IOException("Cannot commit "+Path.GetFileName(path)+": "+error.Message,error);
                    Thread.Sleep(20);
                }
            }
        }

        public sealed class Writer
        {
            public readonly string Directory;
            private readonly LocalReplayManifest _manifest;
            private Task _tail = Task.CompletedTask;
            private int _pending;
            private bool _finished;
            private string _error;
            public string Error => Volatile.Read(ref _error);
            public bool CanAppend => !_finished && Error == null && Volatile.Read(ref _pending) < 2;
            public Task Completion => _tail;
            public Writer(string folder, LocalReplayManifest manifest)
            {
                _manifest = manifest;
                Directory = Path.Combine(folder, "TUMP-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
                // Only managed values are used by the worker. No transforms,
                // Resources, scene objects or simulation calls cross this boundary.
            }
            public bool Append(RecordedMatchClip clip,LocalReplaySceneSegment scene=null,LocalReplayFxSegment effects=null,LocalReplayScenerySegment scenery=null)
            {
                if (!CanAppend) return false;
                Interlocked.Increment(ref _pending);
                _tail = _tail.ContinueWith(_ =>
                {
                    try
                    {
                        if (Error != null) return;
                        byte[] bytes = clip.EncodeLocal();
                        System.IO.Directory.CreateDirectory(Directory);
                        string file = _manifest.Segments.Count.ToString("D6") + ".tps";
                        AtomicWrite(Path.Combine(Directory, file), bytes);
                        string sceneFile=null,sceneHash=null;
                        if(scene!=null&&scene.Frames.Count>=2)
                        {
                            sceneFile=_manifest.Segments.Count.ToString("D6")+".scene.json";
                            byte[] sceneBytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(scene));
                            if(sceneBytes.Length>4*1024*1024)throw new InvalidDataException("Replay scene capture exceeds its budget.");
                            AtomicWrite(Path.Combine(Directory,sceneFile),sceneBytes);sceneHash=Hash(sceneBytes);
                        }
                        string effectsFile=null,effectsHash=null;
                        if(effects!=null&&effects.Frames.Count>0)
                        {
                            effectsFile=_manifest.Segments.Count.ToString("D6")+".fx.gz";byte[] effectBytes=LocalReplayEffectsCodec.Encode(effects);
                            AtomicWrite(Path.Combine(Directory,effectsFile),effectBytes);effectsHash=Hash(effectBytes);
                        }
                        string sceneryFile=null,sceneryHash=null;
                        if(scenery!=null&&scenery.Frames.Count>0)
                        {
                            sceneryFile=_manifest.Segments.Count.ToString("D6")+".scenery.gz";byte[] sceneryBytes=LocalReplaySceneryCodec.Encode(scenery);
                            AtomicWrite(Path.Combine(Directory,sceneryFile),sceneryBytes);sceneryHash=Hash(sceneryBytes);
                        }
                        _manifest.Segments.Add(new LocalReplaySegment { File = file, Sha256 = Hash(bytes),SceneryFile=sceneryFile,ScenerySha256=sceneryHash,
                            SceneFile=sceneFile,SceneSha256=sceneHash,EffectsFile=effectsFile,EffectsSha256=effectsHash,
                            Offset = _manifest.Duration, Start = clip.Start, End = clip.End, Round = clip.Round });
                        WriteManifest();
                    }
                    catch (Exception e) { Volatile.Write(ref _error, e.Message); }
                    finally { Interlocked.Decrement(ref _pending); }
                }, TaskScheduler.Default);
                return true;
            }
            public void Finish(bool completed, string warning = null)
            {
                if (_finished) return;
                _finished = true;
                _tail = _tail.ContinueWith(_ =>
                {
                    try
                    {
                        _manifest.Completed = completed && Error == null && string.IsNullOrEmpty(warning);
                        _manifest.Warning = Error ?? warning;
                        System.IO.Directory.CreateDirectory(Directory);
                        WriteManifest();
                    }
                    catch (Exception e) { Volatile.Write(ref _error, e.Message); }
                }, TaskScheduler.Default);
            }
            private void WriteManifest() => AtomicWrite(Path.Combine(Directory, "manifest.json"), Encoding.UTF8.GetBytes(JsonUtility.ToJson(_manifest, true)));
        }
    }
}
