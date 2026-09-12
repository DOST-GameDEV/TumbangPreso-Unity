using System;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Settings;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Construction/material preflight. Actual map FPP and ordinary play still
    // decide scene acceptance; this bench must not masquerade as either.
    public static class NeighborhoodBuildingReview
    {
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_BUILDING_REVIEW")??"Logs/retained-house-unity-review-v1";
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity",OpenSceneMode.Single);
            var grade=Object.FindFirstObjectByType<MapGrade>();
            if(grade==null)throw new InvalidOperationException("Missing source map grade.");
            var grading=new[]{grade.Brightness,grade.Contrast,grade.Saturation,grade.Exposure,grade.White};
            var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;
            var ground=RenderSettings.ambientGroundColor;var skybox=RenderSettings.skybox;
            var probe=RenderSettings.ambientProbe;
            var originalSun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            var sunColor=originalSun.color;float sunIntensity=originalSun.intensity;var sunRotation=originalSun.transform.rotation;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            RenderSettings.ambientIntensity=1;RenderSettings.skybox=skybox;RenderSettings.fog=false;
            RenderSettings.ambientProbe=probe;
            var sun=new GameObject("Source map sun").AddComponent<Light>();
            sun.type=LightType.Directional;sun.color=sunColor;sun.intensity=sunIntensity;
            sun.transform.rotation=sunRotation;sun.shadows=LightShadows.Soft;
            GraphicsProfiles.Apply(GraphicsProfiles.Default);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="Review ground";floor.transform.position=new Vector3(0,-.06f,0);
            floor.transform.localScale=new Vector3(50,.12f,50);
            var floorMat=new Material(Shader.Find("Standard")){color=new Color(.47f,.46f,.41f)};
            floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            var camera=new GameObject("Building construction witness").AddComponent<Camera>();
            camera.enabled=false;camera.allowHDR=true;camera.allowMSAA=false;
            camera.nearClipPlane=.05f;camera.farClipPlane=200;
            camera.gameObject.AddComponent<WorldOutline>();
            camera.gameObject.AddComponent<ColourGrade>().Set(grading[0],grading[1],grading[2],grading[3],grading[4]);

            var book=AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
            var person=book.People.First(p=>p!=null&&p.Id=="tikboy");
            var reference=Object.Instantiate(person.Model);
            reference.name="Approved Tikboy scale reference";
            reference.transform.localScale=Vector3.one*CharacterVisual.PersonScale;
            reference.transform.rotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw,0);
            ToonSkin.Apply(reference,ToonSkin.PersonOutlineWidth,person.Palette);
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(person.Model)).OfType<AnimationClip>())
                if(clip.name=="idle"){clip.SampleAnimation(reference,0);break;}
            var humanBounds=BoundsOf(reference);
            reference.transform.position+=new Vector3(0,0,3.4f)-new Vector3(humanBounds.center.x,humanBounds.min.y,humanBounds.center.z);
            var report=new StringBuilder("model,width,height,depth,renderers,vertices,reference_height\n");
            foreach(string kind in new[]{"a","c"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/city/building-type-"+kind+".glb");
                var detailAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/retained-house-details/retained-house-"+kind+"-details.glb");
                if(prefab==null||detailAsset==null)throw new InvalidOperationException("Missing retained body or new detail set: "+kind);
                var old=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var building=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                old.name="Retained baseline "+kind;building.name="Retained body with fitted details "+kind;
                foreach(var house in new[]{old,building})
                {
                    house.transform.localScale=Vector3.one*5;
                    var original=BoundsOf(house);
                    house.transform.position=new Vector3(house==old?-5:5,0,0)-new Vector3(original.center.x,original.min.y,original.center.z);
                }
                var details=(GameObject)PrefabUtility.InstantiatePrefab(detailAsset);
                details.transform.SetParent(building.transform,false);details.transform.localScale=Vector3.one*.2f;
                var bounds=BoundsOf(building);
                int vertices=building.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount);
                report.AppendLine(FormattableString.Invariant($"{kind},{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3},{building.GetComponentsInChildren<Renderer>().Length},{vertices},{BoundsOf(reference).size.y:F3}"));
                Shot(camera,output,kind+"-paired-front",new Vector3(0,5,17),new Vector3(0,2.4f,0),62);
                Shot(camera,output,kind+"-paired-back",new Vector3(0,5,-17),new Vector3(0,2.4f,0),62);
                Shot(camera,output,kind+"-paired-eye",new Vector3(0,1.25f,13),new Vector3(0,1.45f,0),75);
                Shot(camera,output,kind+"-new-oblique",new Vector3(13,5,10),new Vector3(5,2.4f,0),58);
                Object.DestroyImmediate(old);Object.DestroyImmediate(building);
            }
            File.WriteAllText(Path.Combine(output,"bounds.csv"),report.ToString());
            Debug.Log("[Building review] 2 retained bodies with fitted details,8 paired/construction views. No map scene saved; scene/FPP acceptance remains open.");
            EditorApplication.Exit(0);
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static void Shot(Camera camera,string output,string name,Vector3 at,Vector3 target,float fov)
        {
            camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));camera.fieldOfView=fov;
            var hdr=new RenderTexture(1600,1000,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var display=new RenderTexture(1600,1000,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=hdr;camera.Render();Graphics.Blit(hdr,display);
                RenderTexture.active=display;pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null;camera.targetTexture=null;
                hdr.Release();display.Release();Object.DestroyImmediate(hdr);Object.DestroyImmediate(display);Object.DestroyImmediate(pixels);
            }
        }
    }
}
