using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class IlalimStructureFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/IlalimStructure/";
        private const string OriginalMaterialTag="TumpIlalimStructureSource",MeshMarker="TUMP_ILALIM_STRUCTURE_MESH_SOURCE:";
        public static void ClearPrevious(string map)
        {
            if(map!="IlalimNgTulay")return;
            var root=GameObject.Find("IlalimNgTulay/Dressing/Tulay");if(root==null)return;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    string source=materials[i]!=null?materials[i].GetTag(OriginalMaterialTag,false):"";
                    if(string.IsNullOrEmpty(source))continue;
                    materials[i]=AssetDatabase.LoadAssetAtPath<Material>(source)??throw new InvalidOperationException("Missing original structure material "+source);changed=true;
                }
                if(changed){renderer.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                string path=AssetDatabase.GetAssetPath(filter.sharedMesh);if(!path.StartsWith(Folder,StringComparison.Ordinal))continue;
                var importer=AssetImporter.GetAtPath(path);string data=importer.userData;
                if(!data.StartsWith(MeshMarker,StringComparison.Ordinal))throw new InvalidOperationException("Missing track source metadata.");
                filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(data.Substring(MeshMarker.Length));
                if(filter.sharedMesh==null)throw new InvalidOperationException("Missing retained track mesh.");
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-structure");File.WriteAllText("Logs/ilalim-structure/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("IlalimNgTulay");var map=GameObject.Find("IlalimNgTulay");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var concrete=Palette("Assets/TumbangPreso/Art/models/kits/roads/Textures/tumbang-warm-a.png","StructuralConcrete",false);
            var rail=Palette("Assets/TumbangPreso/Art/models/kits/train/Textures/tumbang-lrt.png","TrackMaterials",true);
            var cache=new Dictionary<string,Material>();Mesh trackMesh=null;int pillars=0,bays=0,tracks=0;
            foreach(var renderer in map.transform.Find("Dressing/Tulay").GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                string model=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(MapSurfaceAuthor.SourceMeshForAuthoring(filter.sharedMesh)));
                bool isTrack=model=="track-detailed",isPillar=model=="bridge-pillar-wide",isBay=model=="road-bridge";
                if(!isTrack&&!isPillar&&!isBay)continue;
                var before=renderer.bounds;var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];string source=AssetDatabase.GetAssetPath(original);
                    if(string.IsNullOrEmpty(source))throw new InvalidOperationException("Structure material must have a persistent source.");
                    string key=(isTrack?"Track_":"Concrete_")+AssetDatabase.AssetPathToGUID(source);
                    if(!cache.TryGetValue(key,out var saved))
                    {
                        var draft=new Material(original){name="Ilalim "+key,mainTexture=isTrack?rail:concrete};
                        draft.SetOverrideTag(OriginalMaterialTag,source);
                        if(!isTrack){draft.SetFloat("_SurfaceKind",2);draft.SetFloat("_SurfaceVertexRoles",0);}
                        string path=Folder+key+".mat";saved=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}
                        else{EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);}
                        EditorUtility.SetDirty(saved);cache.Add(key,saved);
                    }
                    materials[i]=saved;
                }
                renderer.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                if(isTrack)
                {
                    if(trackMesh==null)trackMesh=TrackWithConcreteSleepers(filter.sharedMesh);
                    filter.sharedMesh=trackMesh;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);tracks++;
                }
                else if(isPillar)pillars++;else bays++;
                if(renderer.bounds!=before)throw new InvalidOperationException("Material finish changed structural bounds.");
            }
            if(pillars!=24||bays!=28||tracks!=56)throw new InvalidOperationException($"Reassess changed structure inventory: {pillars}pillars/{bays}bays/{tracks}tracks.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Structural finish changed collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"{pillars}pillars/{bays}guideway bays/{tracks}track sections receive local material finishes. Original atlases, train bodies, geometry and collision retained; sleeper roles only changed to concrete.");
        }
        private static Mesh TrackWithConcreteSleepers(Mesh source)
        {
            string original=AssetDatabase.GetAssetPath(source),path=Folder+"TrackConcreteSleepers.asset";
            var draft=Object.Instantiate(source);var colors=draft.colors;var uv=draft.uv;int changed=0;
            if(colors.Length!=draft.vertexCount)throw new InvalidOperationException("Track is missing established vertex material roles.");
            for(int i=0;i<uv.Length;i++)if(Mathf.Abs(uv[i].x-.21875f)<.018f){colors[i].r=2f/32;changed++;}
            if(changed!=72)throw new InvalidOperationException("Expected72source sleeper vertices, found"+changed);
            draft.colors=colors;var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}
            else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}
            var importer=AssetImporter.GetAtPath(path);importer.userData=MeshMarker+original;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
        private static Texture2D Palette(string source,string name,bool track)
        {
            // These kit textures are numeric material lookup strips, not painted art.
            // Preserve their value gradients and untouched columns in a local copy.
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(source));
            var pixels=texture.GetPixels32();int width=texture.width;
            for(int i=0;i<pixels.Length;i++)
            {
                int column=(i%width)*16/width;
                bool selected=track?(column==3||column==15):(column==0||column==4||column==5||column==6||column==7);
                if(!selected)continue;
                var p=pixels[i];float value=.2126f*p.r+.7152f*p.g+.0722f*p.b;
                var tone=!track?new Vector3(1.06f,1.035f,.985f):column==3?new Vector3(.90f,.89f,.85f):new Vector3(.60f,.66f,.68f);
                pixels[i]=new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(value*tone.x),0,255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(value*tone.y),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt(value*tone.z),0,255),p.a);
            }
            texture.SetPixels32(pixels);texture.Apply();string path=Folder+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
