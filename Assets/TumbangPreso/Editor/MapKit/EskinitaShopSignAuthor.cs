using System.IO;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class EskinitaShopSignAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EskinitaShopSigns/";
        public static void ClearPrevious(string map)
        {
            if(map!="Eskinita")return;
            foreach(string side in new[]{"W","E"})
            {
                var face=GameObject.Find("Eskinita/Dressing/Kalat/SariSari_"+side+"/RefinedShopLettering");
                if(face!=null)Object.DestroyImmediate(face);
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/eskinita-signs");
            File.WriteAllText("Logs/eskinita-signs/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            foreach(string side in new[]{"W","E"})
            {
                var shop=GameObject.Find("Eskinita/Dressing/Kalat/SariSari_"+side);
                if(shop==null)throw new System.InvalidOperationException("Missing existing shop "+side);
                var previous=shop.transform.Find("RefinedShopLettering");
                if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                string name=side=="W"?"WestSign":"EastSign",path=Folder+name+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                string matPath=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,matPath);}
                material.color=Color.white;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                material.SetFloat("_Glossiness",.1f);EditorUtility.SetDirty(material);
                var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="RefinedShopLettering";
                face.transform.SetParent(shop.transform,false);
                // Existing sign from author_neighborhood_models.store(): center(0,2.48,.26),
                // depth.08, then its export mirrors z about-.7. Face sits7mm ahead of it.
                face.transform.localPosition=new Vector3(0,2.48f,-.913f);
                face.transform.localRotation=Quaternion.Euler(0,180,0);
                face.transform.localScale=new Vector3(1.66f,.20f,1);
                Object.DestroyImmediate(face.GetComponent<Collider>());
                var renderer=face.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                face.isStatic=true;AirborneByDesign.Attach(face,"Lettering fitted to the existing solid shop signboard.");
                report.AppendLine(side+": fitted1.66x.20m sign face; existing board, shop, seats and collision retained.");
            }
        }
    }
}
