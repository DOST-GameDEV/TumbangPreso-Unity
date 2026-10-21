using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Editor.MapKit
{
    /// <summary>
    /// A FLIGHT RECORDER FOR THE OWNER'S PLAY (owner, 2026-10-19: "i want you to record my play, log every event. then ill
    /// stop the play the moment i see it", of a brief flash of shading outlines showing through the platforms).
    ///
    /// Armed by `Temp/rec.request` reading `on` (`off` disarms; it stays armed across Plays in this editor session). While
    /// armed and in Play it keeps:
    ///   * the LAST `Slots` frames of the game camera's finished picture, copied on the GPU each frame (a command buffer
    ///     at the end of the camera blits its target into a small texture, then into a ring of textures), so the
    ///     flash is in the ring when Play is stopped;
    ///   * a LOG of every frame: time, the camera's place, the cameras that rendered, the main camera's depth texture
    ///     flags, the outline pass's state, every sound cue played (abilities, steps, hits: `AudioDirector.WorldCuePlayed`),
    ///     every match flair, every voice line, and once a second the effect roots that appeared or went away.
    /// When Play stops it writes the ring as numbered PNGs and the log to `Logs/payo/unity/flashrec/TIME/` and the folder to
    /// `Temp/rec.done`. Editor only; nothing in the game reads it.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoFlashRecorder
    {
        private const string Request = "Temp/rec.request", Done = "Temp/rec.done", ArmedKey = "PayoFlashRecorder.armed";
        private const int Slots = 300, Width = 800, Height = 450;

        private static RenderTexture _grab;
        private static RenderTexture[] _ring;
        private static readonly int[] RingFrame = new int[Slots];
        private static readonly float[] RingTime = new float[Slots];
        private static int _next, _filled;
        private static CommandBuffer _buffer;
        private static Camera _attached;
        private static readonly StringBuilder Log = new StringBuilder();
        private static HashSet<string> _roots = new HashSet<string>();
        private static double _nextRoots;
        private static string _lastState = "";
        private static int _lastExcluded = -1;
        private static bool _hooked;
        private static CameraSystem.ViewmodelArms _arms;

        private static bool Armed => SessionState.GetBool(ArmedKey, false);

        static PayoFlashRecorder()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Changed;
        }

        private static void Tick()
        {
            if (File.Exists(Request))
            {
                string word = File.ReadAllText(Request).Trim();
                File.Delete(Request);
                SessionState.SetBool(ArmedKey, word == "on");
                File.WriteAllText(Done, "OK recorder " + (word == "on" ? "ARMED: it records every Play until 'off'" : "off") + "\n");
            }
            if (!Armed || !EditorApplication.isPlaying) return;
            if (_ring == null) Begin();
            Frame();
        }

        private static void Begin()
        {
            _grab = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32) { name = "rec grab" };
            _ring = new RenderTexture[Slots];
            for (int i = 0; i < Slots; i++) _ring[i] = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32) { name = "rec " + i };
            _next = _filled = 0;
            Log.Clear();
            _roots.Clear();
            _lastState = "";
            _lastExcluded = -1;
            Log.AppendLine("PAYO FLASH RECORDER, " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", last " + Slots + " frames kept");
            if (!_hooked)
            {
                AudioDirector.WorldCuePlayed += Cue;
                Visual.MatchFlair.Presented += Flair;
                Audio.HeroVoice.Captioned += Voice;
                _hooked = true;
            }
        }

        private static void Attach()
        {
            var cam = Camera.main;
            if (cam == _attached) return;
            if (_attached != null && _buffer != null) _attached.RemoveCommandBuffer(CameraEvent.AfterEverything, _buffer);
            _attached = cam;
            if (cam == null) return;
            if (_buffer == null)
            {
                _buffer = new CommandBuffer { name = "Payo flash recorder" };
                _buffer.Blit(BuiltinRenderTextureType.CameraTarget, _grab);
            }
            cam.AddCommandBuffer(CameraEvent.AfterEverything, _buffer);
            Log.AppendLine($"f{Time.frameCount} main camera is now '{cam.name}'");
        }

        private static void Frame()
        {
            Attach();
            // The grab holds the last finished picture; keep it in the ring under this frame's number.
            Graphics.CopyTexture(_grab, _ring[_next]);
            RingFrame[_next] = Time.frameCount; RingTime[_next] = Time.time;
            _next = (_next + 1) % Slots; _filled = Mathf.Min(_filled + 1, Slots);

            var cam = Camera.main;
            var line = new StringBuilder();
            line.Append($"f{Time.frameCount} t{Time.time:0.000} dt{Time.unscaledDeltaTime * 1000f:0.0}ms");
            if (cam != null)
            {
                var p = cam.transform.position; var e = cam.transform.eulerAngles;
                line.Append($" cam ({p.x:0.00},{p.y:0.00},{p.z:0.00}) rot ({e.x:0},{e.y:0}) fov {cam.fieldOfView:0}");
                if (_arms == null && Time.frameCount % 30 == 0) _arms = Object.FindFirstObjectByType<CameraSystem.ViewmodelArms>();
                if (_arms != null) line.Append($" armShade {_arms.WorldShade:0.00}");
            }
            // The state that could change what the shading reads: logged whenever it differs from the last frame.
            var state = new StringBuilder();
            if (cam != null)
            {
                state.Append($"depthMode {cam.depthTextureMode}; far {cam.farClipPlane:0}; ");
                var outline = cam.GetComponent<Visual.WorldOutline>();
                state.Append(outline == null ? "WorldOutline none; " : $"WorldOutline {(outline.enabled ? "on" : "OFF")}; ");
                if (outline != null)
                {
                    // The outline pass's own lists (private: read by reflection, editor only): what it masks this frame.
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var excluded = typeof(Visual.WorldOutline).GetField("_excluded", flags)?.GetValue(outline) as System.Collections.IList;
                    var guard = typeof(Visual.WorldOutline).GetMethod("NearGuard", flags);
                    state.Append($"excluded {(excluded != null ? excluded.Count : -1)}; near-fade masks {outline.NearFadeMaskDraws}; near guard {(guard != null ? guard.Invoke(outline, null) : "?")}; ");
                    if (excluded != null && excluded.Count != _lastExcluded)
                    {
                        var names = new List<string>();
                        foreach (var o in excluded) if (o is Renderer r && r != null) names.Add(r.transform.root.name + "/" + r.name);
                        line.Append("\n    EXCLUDED now: " + string.Join(", ", names.GroupBy(n => n).Select(g => g.Count() + "x " + g.Key)));
                        _lastExcluded = excluded.Count;
                    }
                }
                var buffers = cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque).Length + cam.GetCommandBuffers(CameraEvent.AfterImageEffects).Length
                    + cam.GetCommandBuffers(CameraEvent.AfterForwardOpaque).Length + cam.GetCommandBuffers(CameraEvent.BeforeImageEffects).Length;
                state.Append($"cmd buffers {buffers}; ");
            }
            state.Append("cameras " + string.Join(",", Camera.allCameras.Select(c => c.name + (c.enabled ? "" : "(off)") + ":" + c.depth.ToString("0"))));
            state.Append($"; haze {Shader.GetGlobalVector("_MapHaze").x:0.00}");
            state.Append($"; charAO {(Visual.WorldOutline.CharacterAoTest >= 0 ? Visual.WorldOutline.CharacterAoTest : Settings.GameSettings.CharacterShadingShare(Settings.SettingsStore.Current.CharacterShading)):0.00}; armShadeOff {CameraSystem.ViewmodelArms.DebugNoWorldShade}");
            string s = state.ToString();
            if (s != _lastState) { line.Append("\n    STATE " + s); _lastState = s; }
            Log.AppendLine(line.ToString());

            if (EditorApplication.timeSinceStartup >= _nextRoots)
            {
                _nextRoots = EditorApplication.timeSinceStartup + 1.0;
                var now = new HashSet<string>();
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                    var root = r.transform.root;
                    if (root.gameObject.scene.name == "Payo" && root.name != "Players") { if (root.name.StartsWith("Payo")) continue; }
                    now.Add(root.name);
                }
                foreach (var n in now.Except(_roots)) Log.AppendLine($"    + {n}");
                foreach (var n in _roots.Except(now)) Log.AppendLine($"    - {n}");
                _roots = now;
            }
        }

        private static void Cue(string id, Vector3 at, float pitch, float gain) =>
            Log.AppendLine($"    CUE {id} at ({at.x:0.0},{at.y:0.0},{at.z:0.0}) gain {gain:0.00}");
        private static void Flair(Visual.MatchFlair.Kind kind, int a, int b, Vector3 at, float strength) =>
            Log.AppendLine($"    FLAIR {kind} {a} {b} at ({at.x:0.0},{at.y:0.0},{at.z:0.0})");
        private static void Voice(string who, string line, float seconds) =>
            Log.AppendLine($"    VOICE {who}: {line}");

        private static void Changed(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode || _ring == null) return;
            try
            {
                if (_attached != null && _buffer != null) _attached.RemoveCommandBuffer(CameraEvent.AfterEverything, _buffer);
                string folder = "Logs/payo/unity/flashrec/" + System.DateTime.Now.ToString("HHmmss");
                Directory.CreateDirectory(folder);
                var read = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                int start = (_next - _filled + Slots) % Slots;
                for (int k = 0; k < _filled; k++)
                {
                    int i = (start + k) % Slots;
                    var prev = RenderTexture.active; RenderTexture.active = _ring[i];
                    read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); read.Apply(false);
                    RenderTexture.active = prev;
                    File.WriteAllBytes($"{folder}/{k:000}_f{RingFrame[i]}_t{RingTime[i]:0.000}.png", read.EncodeToPNG());
                }
                Object.DestroyImmediate(read);
                File.WriteAllText(folder + "/log.txt", Log.ToString());
                File.WriteAllText(Done, $"OK recorded {_filled} frames and the log into {folder}\n");
            }
            catch (System.Exception e) { File.WriteAllText(Done, "FAIL recorder: " + e + "\n"); }
            finally
            {
                if (_ring != null) foreach (var t in _ring) if (t != null) t.Release();
                if (_grab != null) _grab.Release();
                _ring = null; _grab = null; _attached = null;
                if (_buffer != null) { _buffer.Release(); _buffer = null; }
            }
        }
    }
}
