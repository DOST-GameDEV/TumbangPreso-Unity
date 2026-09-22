using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class RafiPortraitAuthor
    {
        public static void Build()
        {
            const string path="Assets/TumbangPreso/Resources/UI/portraits/rafi.png";
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var art=RosterBook.Load().FindPersonArt("rafi");
            art.Tagline="Draws you into the wrong current. Leaves with his slipper.";EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            var model=Object.Instantiate(art.Model);model.transform.localScale=Vector3.one*2.38f;
            model.transform.rotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw+180,0);
            ToonSkin.Apply(model,ToonSkin.PersonOutlineWidth,art.Palette);
            art.Clips.FirstOrDefault(c=>c!=null&&c.name=="idle")?.SampleAnimation(model,.15f);
            var light=new GameObject("Portrait key").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=.85f;light.color=new Color(1,.97f,.90f);light.transform.rotation=Quaternion.Euler(38,-40,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.46f,.42f);RenderSettings.fog=false;
            var camera=new GameObject("Rafi portrait camera").AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=1.03f;camera.transform.position=new Vector3(0,.86f,-6);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.nearClipPlane=.1f;camera.farClipPlane=20;
            var target=new RenderTexture(600,800,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};target.Create();camera.targetTexture=target;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(600,800,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,600,800),0,0);pixels.Apply();
            File.WriteAllBytes(path,pixels.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene,true);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
    }
}
