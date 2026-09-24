using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Per-shop approved generated artwork, fitted at its actual aspect.</summary>
    public static class IlalimShopSignAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/IlalimShopSigns";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-shop-signs");File.WriteAllText("Logs/ilalim-shop-signs/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            // Add other named signs only after their own reference and art review.
            Fit(map.transform,"Print",Folder+"/Print-v3.png",report);
            Fit(map.transform,"Bakery",Folder+"/Bakery-v2.png",report);
            Fit(map.transform,"Laundry",Folder+"/Laundry-v1.png",report);
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Sign art changed gameplay collision.");
        }
        private static void Fit(Transform map,string id,string texturePath,StringBuilder report)
        {
            var room=map.Find("Dressing/PlaceRework/Frontage_"+id);if(room==null)throw new InvalidOperationException("Missing shop frontage "+id);
            var face=room.Find("Sign face "+id);var board=room.Find("Shop sign backing "+id)??room.Find("Banner backing "+id);
            if(face==null||board==null)throw new InvalidOperationException("Missing physical sign mounting "+id);
            AssetDatabase.ImportAsset(texturePath);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            // NPOT rounding would turn 2172x724 into a different aspect before sampling.
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);float aspect=texture.width/(float)texture.height;
            var size=face.localScale;float height=size.x/aspect;
            if(height>1.15f||height<.35f)throw new InvalidOperationException("Artwork needs an individually redesigned mounting: "+id);
            size.y=height;face.localScale=size;var support=board.localScale;support.y=height+.025f;board.localScale=support;
            foreach(Transform fixing in room)if(fixing.name=="Banner fixing "+id)
            {var fixingSize=fixing.localScale;fixingSize.y=height+.1f;fixing.localScale=fixingSize;}
            var renderer=face.GetComponent<MeshRenderer>();var original=renderer.sharedMaterial;
            string prior=original.GetTag("TumpShopSignSource",false);if(!string.IsNullOrEmpty(prior))original=AssetDatabase.LoadAssetAtPath<Material>(prior);
            var draft=new Material(Shader.Find("Standard")){name=id+" authored painted fascia",color=Color.white,mainTexture=texture};draft.SetFloat("_Glossiness",.04f);
            draft.SetOverrideTag("TumpShopSignSource",AssetDatabase.GetAssetPath(original));draft.SetOverrideTag("TumpShopSignAspect",aspect.ToString(System.Globalization.CultureInfo.InvariantCulture));
            string path=Folder+"/"+id+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);EditorUtility.SetDirty(saved);}
            renderer.sharedMaterial=saved;
            float actual=Mathf.Abs(face.lossyScale.x/face.lossyScale.y);
            if(Mathf.Abs(actual-aspect)>.002f)throw new InvalidOperationException("Parent scale distorts sign artwork: "+id);
            report.AppendLine(id+": "+texture.width+"x"+texture.height+", artwork/world aspect "+aspect.ToString("F4")+"/"+actual.ToString("F4")+"; physical backing fitted, old artwork retained, no collision change.");
        }
    }
}
