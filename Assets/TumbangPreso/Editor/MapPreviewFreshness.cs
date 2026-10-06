using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>Source/media provenance for recorded courts. Never records footage as a build side effect.</summary>
    public static class MapPreviewFreshness
    {
        public const string MediaFolder="Assets/TumbangPreso/Resources/UI/map-previews";
        [Serializable]public sealed class Receipt
        {
            public string map,sourceFingerprint,videoSha256,posterSha256;
            public int width=1920,height=1080,fps=30,frames=780;
        }
        static readonly string[] PreviewSources={
            "Assets/TumbangPreso/Runtime/UI/SceneFlow.cs",
            "Assets/TumbangPreso/Runtime/Visual/WorldLookPresentation.cs",
            "Assets/TumbangPreso/Runtime/Visual/WorldOutline.cs",
            "Assets/TumbangPreso/Runtime/Visual/ColourGrade.cs",
            "Assets/TumbangPreso/Runtime/Visual/MapCameraRange.cs"
        };
        public static string SourceFingerprint(string map)
        {
            string scene="Assets/TumbangPreso/Scenes/Maps/"+map+".unity";
            var files=new SortedSet<string>(AssetDatabase.GetDependencies(scene,true),StringComparer.Ordinal);
            foreach(string source in PreviewSources)if(File.Exists(source))files.Add(source);
            foreach(string source in Directory.GetFiles("Assets/TumbangPreso/Runtime/Visual","WorldLook*.cs"))files.Add(source.Replace('\\','/'));
            foreach(string source in Directory.GetFiles("Assets/TumbangPreso/Runtime/Visual","ColourGrade*.cs"))files.Add(source.Replace('\\','/'));
            // Attached map scripts are already scene dependencies. Include their
            // partial implementations without invalidating every court for an
            // unrelated map script change.
            foreach(string source in files.Where(p=>p.StartsWith("Assets/TumbangPreso/Runtime/Map/",StringComparison.Ordinal)&&p.EndsWith(".cs",StringComparison.Ordinal)).ToArray())
                foreach(string partial in Directory.GetFiles(Path.GetDirectoryName(source),Path.GetFileNameWithoutExtension(source)+".*.cs"))files.Add(partial.Replace('\\','/'));
            // Runtime-created map props and Shader.Find are not serialized scene
            // references. Include those visual inputs without hashing menu media.
            foreach(string guid in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/TumbangPreso"}))
                foreach(string dependency in AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid),true))files.Add(dependency);
            foreach(string resource in new[]{"Map/JumpPad/jump_pad","Map/JumpPad/jump_pad_paint"})
                AddResource(files,resource);
            if(map==SceneFlow.Arena)AddResource(files,"UI/brand/tump_logo");
            foreach(string setting in new[]{"ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset"})if(File.Exists(setting))files.Add(setting);
            var builder=new StringBuilder("1920x1080;30fps;780frames;HDR;MSAA4;camera-sway\n");
            foreach(string path in files.ToArray())if(File.Exists(path+".meta"))files.Add(path+".meta");
            foreach(string path in files)
            {
                if(!File.Exists(path))continue;
                builder.Append(path).Append(':').Append(SourceSha(path)).Append('\n');
            }
            // Capture the camera contract, not the recorded-player routing branch.
            string preview=File.ReadAllText("Assets/TumbangPreso/Runtime/UI/MapPreviewSurface.cs");
            foreach(string name in new[]{"private void AimAt(","private void ApplyCamera(","private void AdoptRange(","private void EnsureCamera(","private void EnsureWorldOutline(","private void ApplyMapEnvironment("})
                builder.Append(MethodBody(preview,name));
            foreach(string line in preview.Split('\n'))if(line.Contains(" const "))builder.Append(line.Trim()).Append('\n');
            using(var sha=SHA256.Create())return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
        }
        static void AddResource(SortedSet<string> files,string resource)
        {
            var asset=Resources.Load<UnityEngine.Object>(resource);if(asset==null)return;
            foreach(string path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(asset),true))files.Add(path);
        }
        static string MethodBody(string source,string name)
        {
            int start=source.IndexOf(name,StringComparison.Ordinal);if(start<0)throw new InvalidOperationException("Preview capture method missing: "+name);
            int open=source.IndexOf('{',start),depth=0;
            for(int i=open;i<source.Length;i++){if(source[i]=='{')depth++;else if(source[i]=='}'&&--depth==0)return source.Substring(start,i-start+1).Replace("\r\n","\n");}
            throw new InvalidOperationException("Preview capture method incomplete: "+name);
        }
        public static string Sha(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return Hex(sha.ComputeHash(stream));}
        // Git's Windows checkout changes text line endings without changing the map.
        // Binary textures/meshes remain byte-exact; serialized assets are recognized
        // by their YAML header rather than treating every .asset file as text.
        public static string SourceSha(string path)
        {
            byte[] bytes=File.ReadAllBytes(path);string extension=Path.GetExtension(path);
            bool text=extension==".cs"||extension==".meta"||extension==".shader"||extension==".cginc"||extension==".hlsl"||extension==".compute";
            if(!text&&bytes.Length>=5)text=Encoding.UTF8.GetString(bytes,0,5)=="%YAML";
            if(text)bytes=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\r\n","\n"));
            using(var sha=SHA256.Create())return Hex(sha.ComputeHash(bytes));
        }
        static string Hex(byte[] bytes)=>string.Concat(bytes.Select(b=>b.ToString("x2")));
        public static void WriteReceipt(string map,string capturedFingerprint)
        {
            if(capturedFingerprint!=SourceFingerprint(map))throw new InvalidOperationException("Map changed during recording: "+map);
            string video=MediaFolder+"/"+map+"-loop.mp4",poster=MediaFolder+"/"+map+"-poster.png";
            if(!File.Exists(video)||!File.Exists(poster))throw new FileNotFoundException("Preview media missing: "+map);
            var receipt=new Receipt{map=map,sourceFingerprint=capturedFingerprint,videoSha256=Sha(video),posterSha256=Sha(poster)};
            File.WriteAllText(MediaFolder+"/"+map+"-source.json",JsonUtility.ToJson(receipt,true)+"\n");
        }
        public static string Check(string map)
        {
            string path=MediaFolder+"/"+map+"-source.json";
            if(!File.Exists(path))return map+": source receipt missing; record its current preview";
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(path));
            if(receipt==null||receipt.map!=map||receipt.width!=1920||receipt.height!=1080||receipt.fps!=30||receipt.frames!=780)return map+": capture settings mismatch";
            if(receipt.sourceFingerprint!=SourceFingerprint(map))return map+": map or preview look changed; regenerate footage and poster";
            string video=MediaFolder+"/"+map+"-loop.mp4",poster=MediaFolder+"/"+map+"-poster.png";
            if(!File.Exists(video)||!File.Exists(poster))return map+": media missing";
            if(receipt.videoSha256!=Sha(video)||receipt.posterSha256!=Sha(poster))return map+": media differs from its capture receipt";
            return null;
        }
        [MenuItem("Tumbang Preso/Maps/Validate Recorded Previews")]
        public static void Validate()
        {
            var errors=SceneFlow.Maps.Select(Check).Where(e=>e!=null).ToArray();
            if(errors.Length>0)throw new BuildFailedException(string.Join("\n",errors));
            Debug.Log("[MapPreviewFreshness] All registered map source/media receipts match.");
        }
    }
}
