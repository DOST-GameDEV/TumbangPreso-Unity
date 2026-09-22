using System;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Native toon/outline and actual uniform game scale. Existing character assets
    // are instantiated for comparison only; no original model or recipe is edited.
    public static partial class RafiNativeModelReview
    {
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-out");
            if(at<0||at+1>=args.Length)throw new ArgumentException("A fresh -out turnaround filename is required.");
            string directory=Path.GetDirectoryName(Path.GetFullPath(args[at+1]));Directory.CreateDirectory(directory);
            AssetDatabase.ImportAsset("Assets/TumbangPreso/Art/characters/persons/team-rafi.glb",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            RosterBookBuilder.BuildFromMenu();
            Lineup(directory,"hero-lineup-front.png",180);
            Lineup(directory,"hero-lineup-quarter.png",220);
            if(args.Contains("-tp-part-studies"))RenderPartStudies(directory);
            RafiPortraitAuthor.Build();
            HeroTurnaroundProbe.RunOne();
        }
        private static void Lineup(string directory,string filename,float yaw)
        {
            string output=Path.Combine(directory,filename);if(File.Exists(output))throw new IOException("Refuse to overwrite an earlier review: "+output);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var light=new GameObject("Canonical model key").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=.85f;light.color=new Color(1,.97f,.90f);light.transform.rotation=Quaternion.Euler(38,-40,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.62f,.58f,.52f)*.78f;RenderSettings.fog=false;
            var book=RosterBook.Load();string[] ids={"sean","cheska","dante","zack","nemu","phaister","rafi"};
            float highest=0;
            for(int i=0;i<ids.Length;i++)
            {
                var entry=book.FindPersonArt(ids[i]);if(entry?.Model==null)throw new InvalidOperationException("Missing hero art "+ids[i]);
                var model=Object.Instantiate(entry.Model);model.name="Reference-"+ids[i];
                model.transform.localScale=Vector3.one*2.38f;
                model.transform.rotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw+yaw,0);
                ToonSkin.Apply(model,ToonSkin.PersonOutlineWidth,entry.Palette);
                entry.Clips.FirstOrDefault(c=>c!=null&&c.name=="idle")?.SampleAnimation(model,0);
                var renderers=model.GetComponentsInChildren<Renderer>();float floor=renderers.Min(r=>r.bounds.min.y);
                highest=Mathf.Max(highest,renderers.Max(r=>r.bounds.max.y)-floor);
                model.transform.position=new Vector3(i*2, -floor,0);
                var label=new GameObject("Label-"+ids[i]).AddComponent<TextMesh>();label.transform.position=new Vector3(i*2,-.20f,-.30f);
                label.transform.localScale=Vector3.one*.010f;label.text=ids[i].ToUpperInvariant();label.fontSize=38;label.anchor=TextAnchor.MiddleCenter;
                label.color=ids[i]=="rafi"?new Color(.52f,.84f,.84f):new Color(.88f,.90f,.95f);
            }
            var camera=new GameObject("Hero cast comparison camera").AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=Mathf.Max(1.65f,(highest+.55f)*.5f);camera.transform.position=new Vector3(6,(highest-.25f)*.5f,-6);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.153f,.161f,.208f);
            camera.nearClipPlane=.1f;camera.farClipPlane=30;
            const int width=4200,height=990;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            File.WriteAllBytes(output,pixels.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene,true);
        }
    }
}
