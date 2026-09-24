using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Individually authored home-shop signs on retained Eskinita booths.</summary>
    public static class EskinitaAuthoredSignAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EskinitaShopSigns/";
        public const string RootName="Authored home-shop sign";
        public static void RunWest()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");var report=new StringBuilder();
            Fit("W","West-authored-v1.png",report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/eskinita-authored-west");File.WriteAllText("Logs/eskinita-authored-west/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void RunEast()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");var report=new StringBuilder();
            Fit("E","East-authored-v1.png",report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/eskinita-authored-east");File.WriteAllText("Logs/eskinita-authored-east/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {Fit("W","West-authored-v1.png",report);Fit("E","East-authored-v1.png",report);}
        private static void Fit(string side,string file,StringBuilder report)
        {
            var map=GameObject.Find("Eskinita");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var shop=map.transform.Find("Dressing/Kalat/SariSari_"+side);if(shop==null)throw new InvalidOperationException("Missing shop "+side);
            var old=shop.Find("RefinedShopLettering");if(old==null)throw new InvalidOperationException("Missing retained font baseline "+side);
            var prior=shop.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            string path=Folder+file;AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;
            importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);float aspect=width/(float)height;
            float localHeight=1.66f*Mathf.Abs(shop.lossyScale.x/shop.lossyScale.y)/aspect;
            if(localHeight>.65f||localHeight<.30f)throw new InvalidOperationException("Home sign needs another mounting design.");
            var root=new GameObject(RootName);root.transform.SetParent(shop,false);root.isStatic=true;
            var timber=new Material(Shader.Find("Standard")){color=new Color(.38f,.27f,.16f)};timber.SetFloat("_Glossiness",.06f);
            var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.name="Fitted wood backing";backing.transform.SetParent(root.transform,false);
            backing.transform.localPosition=new Vector3(0,2.65f,.53f);backing.transform.localScale=new Vector3(1.70f,localHeight+.03f,.06f);
            Object.DestroyImmediate(backing.GetComponent<Collider>());backing.isStatic=true;var backingRenderer=backing.GetComponent<MeshRenderer>();
            backingRenderer.sharedMaterial=Save(timber,Folder+side+"-AuthoredBacking.mat");backingRenderer.shadowCastingMode=ShadowCastingMode.Off;
            var material=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};material.SetFloat("_Glossiness",.06f);
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Generated shop lettering";face.transform.SetParent(root.transform,false);
            face.transform.localPosition=new Vector3(0,2.65f,.566f);face.transform.localRotation=Quaternion.Euler(0,180,0);face.transform.localScale=new Vector3(1.66f,localHeight,1);
            Object.DestroyImmediate(face.GetComponent<Collider>());face.isStatic=true;var faceRenderer=face.GetComponent<MeshRenderer>();faceRenderer.sharedMaterial=Save(material,Folder+side+"-AuthoredFace.mat");faceRenderer.shadowCastingMode=ShadowCastingMode.Off;
            foreach(float x in new[]{-.56f,.56f})
            {
                var cleat=GameObject.CreatePrimitive(PrimitiveType.Cube);cleat.name="Front fascia sign cleat";cleat.transform.SetParent(root.transform,false);
                cleat.transform.localPosition=new Vector3(x,2.515f,.47f);cleat.transform.localScale=new Vector3(.055f,.55f,.07f);
                Object.DestroyImmediate(cleat.GetComponent<Collider>());cleat.isStatic=true;var cleatRenderer=cleat.GetComponent<MeshRenderer>();
                cleatRenderer.sharedMaterial=backingRenderer.sharedMaterial;cleatRenderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            old.GetComponent<MeshRenderer>().enabled=false;
            AirborneByDesign.Attach(root,"Two cleats fix the plate onto the retained front awning fascia; original rear board, stall/awning/frame geometry is preserved.");
            if(Mathf.Abs(Mathf.Abs(face.transform.lossyScale.x/face.transform.lossyScale.y)-aspect)>.002f)throw new InvalidOperationException("Home shop sign stretched.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Shop sign changed collision.");
            report.AppendLine(side+": source"+width+"x"+height+", face1.66x"+localHeight.ToString("F3")+" at front-fascia supporty2.65, uniform source aspect, modest wood backing. Booth/stock/house/collision preserved; other shop untouched.");
        }
        private static Material Save(Material draft,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
    }
}
