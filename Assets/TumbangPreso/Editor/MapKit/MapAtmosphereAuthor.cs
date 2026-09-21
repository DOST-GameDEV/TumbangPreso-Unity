using System.IO;
using System;
using System.Linq;
using System.Security.Cryptography;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Each place has its own daylight, bounce and atmospheric depth.</summary>
    public static class MapAtmosphereAuthor
    {
        [Serializable] private sealed class CloudSampling
        {public string sourceSha256;public int version;public float sunU,lumaScale,sunCutoff,cloudLow,cloudHigh;}
        // Update only the already referenced sky materials; no scene reconstruction.
        public static void RefreshCloudMaterials()
        {
            foreach(string map in UI.SceneFlow.Maps)
            {
                var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/MapAtmosphere/"+map+"Sky.mat");
                if(sky==null)throw new System.InvalidOperationException("Missing authored sky for "+map);
                Clouds(sky,map);
            }
            AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }

        private static void Clouds(Material sky,string map)
        {
            bool alley=map=="Eskinita",bridge=map=="IlalimNgTulay",roof=map=="SaBubong";
            sky.SetColor("_Tint",new Color(.5f,.5f,.5f));sky.SetFloat("_Exposure",1);
            sky.SetColor("_CloudLight",roof?new Color(.95f,.80f,.66f):alley?new Color(.94f,.87f,.73f):new Color(.91f,.92f,.89f));
            sky.SetColor("_CloudShade",roof?new Color(.39f,.40f,.53f):bridge?new Color(.43f,.53f,.61f):new Color(.45f,.54f,.63f));
            string source=roof?"wasteland_clouds_puresky":bridge?"kloppenheim_03_puresky":
                alley?"kloofendal_38d_partly_cloudy_puresky":"kloofendal_48d_partly_cloudy_puresky";
            var texture=CloudTexture(source,out var sampling);
            sky.SetFloat("_CloudSpeed",(roof?.022f:bridge?.045f:alley?.035f:.025f)/360f);
            sky.SetTexture("_CloudMap",texture);sky.SetFloat("_CloudLumaScale",sampling.lumaScale);
            sky.SetFloat("_CloudLumaLow",sampling.cloudLow);sky.SetFloat("_CloudLumaHigh",sampling.cloudHigh);
            sky.SetFloat("_CloudSunCutoff",sampling.sunCutoff);sky.SetFloat("_CloudOpacity",roof?.90f:.94f);
            var direction=sky.GetVector("_SunDirection");
            sky.SetFloat("_CloudYaw",sampling.sunU-(Mathf.Atan2(direction.x,direction.z)/(2*Mathf.PI)+.5f));
            EditorUtility.SetDirty(sky);
        }

        private static Texture2D CloudTexture(string id,out CloudSampling sampling)
        {
            string source="ArtSource/environment/skies/polyhaven/"+id+"_2k.hdr";
            if(!File.Exists(source))throw new InvalidOperationException("Missing credited cloud source "+source);
            string folder="Assets/TumbangPreso/Art/MapAtmosphere/CloudSources";
            Directory.CreateDirectory(folder);string path=folder+"/"+id+"_2k.hdr";
            byte[] bytes=File.ReadAllBytes(source);string hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            if(!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(bytes))File.WriteAllBytes(path,bytes);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            sampling=string.IsNullOrEmpty(importer.userData)?null:JsonUtility.FromJson<CloudSampling>(importer.userData);
            if(sampling==null||sampling.sourceSha256!=hash||sampling.version!=2)
            {
                importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=false;
                importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.isReadable=true;
                importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                var raw=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=raw.GetPixels();
                var light=new System.Collections.Generic.List<float>();
                var cloudLight=new System.Collections.Generic.List<float>();float peak=0;int sunX=0;
                for(int y=raw.height/2;y<raw.height;y+=2)for(int x=0;x<raw.width;x+=2)
                {
                    var c=pixels[y*raw.width+x];float value=c.r*.2126f+c.g*.7152f+c.b*.0722f;
                    light.Add(value);
                    float maximum=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                    if(y>raw.height*.55f&&(c.b-c.r)/Mathf.Max(.00001f,maximum)<.18f)cloudLight.Add(value);
                    if(value>peak){peak=value;sunX=x;}
                }
                light.Sort();cloudLight.Sort();
                if(cloudLight.Count<100)throw new InvalidOperationException("Cloud panorama has insufficient neutral cloud samples: "+id);
                sampling=new CloudSampling{sourceSha256=hash,version=2,cloudLow=cloudLight[(int)(cloudLight.Count*.05f)],
                    cloudHigh=cloudLight[(int)(cloudLight.Count*.90f)],sunU=(sunX+.5f)/raw.width,
                    lumaScale=1/Mathf.Max(.00001f,light[(int)(light.Count*.8f)]),
                    sunCutoff=Mathf.Max(.001f,light[(int)(light.Count*.95f)]*6)};
                importer.userData=JsonUtility.ToJson(sampling);importer.isReadable=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                Debug.Log("[Cloud source] "+id+" "+JsonUtility.ToJson(sampling));
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void Apply(string map)
        {
            bool alley=map=="Eskinita",bridge=map=="IlalimNgTulay",roof=map=="SaBubong";
            var sunColor=alley?new Color(1,.82f,.62f):bridge?new Color(1,.90f,.75f):new Color(1,.88f,.70f);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=bridge?new Color(.59f,.65f,.68f):new Color(.65f,.66f,.64f);
            RenderSettings.ambientEquatorColor=bridge?new Color(.46f,.49f,.49f):
                alley?new Color(.56f,.47f,.37f):new Color(.55f,.50f,.41f);
            RenderSettings.ambientGroundColor=bridge?new Color(.30f,.29f,.26f):new Color(.34f,.28f,.22f);
            Vector3 sunDirection=Vector3.up;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(light.type!=LightType.Directional)continue;
                light.transform.rotation=Quaternion.Euler(roof?28:alley?34:bridge?38:46,alley?-38:bridge?-52:-28,0);
                light.color=sunColor;light.intensity=alley?1.12f:bridge?1.05f:1.10f;
                light.shadows=LightShadows.Soft;light.shadowStrength=bridge?.79f:.84f;
                light.shadowBias=.025f;light.shadowNormalBias=.16f;
                sunDirection=-light.transform.forward;
            }
            Object.FindFirstObjectByType<MapGrade>()?.Set(1,alley?1.04f:1.025f,alley?1.10f:bridge?1.045f:1.06f,1,1.9f);
            const string folder="Assets/TumbangPreso/Art/MapAtmosphere";
            if(!Directory.Exists(folder)){Directory.CreateDirectory(folder);AssetDatabase.Refresh();}
            string path=folder+"/"+map+"Sky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("TumbangPreso/NeighbourhoodSky");
            if(shader==null)throw new System.InvalidOperationException("Missing neighbourhood sky shader");
            if(sky==null){sky=new Material(shader);AssetDatabase.CreateAsset(sky,path);}
            sky.shader=shader;
            sky.SetColor("_Zenith",roof?new Color(.45f,.52f,.64f):alley?new Color(.43f,.53f,.60f):bridge?new Color(.44f,.55f,.63f):new Color(.47f,.60f,.69f));
            var horizon=roof?new Color(.82f,.74f,.65f):alley?new Color(.80f,.73f,.62f):bridge?new Color(.76f,.75f,.69f):new Color(.80f,.79f,.71f);
            sky.SetColor("_Horizon",horizon);sky.SetColor("_Ground",new Color(.40f,.37f,.31f));
            sky.SetColor("_SunColor",sunColor);sky.SetVector("_SunDirection",sunDirection);
            Clouds(sky,map);
            RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=horizon;RenderSettings.fogStartDistance=bridge?68:85;
            RenderSettings.fogEndDistance=bridge?165:190;
            // The asphalt has its own texture. A mild warm binder tint counters
            // the blue-grey aggregate without repainting signs, cast or effects.
            string roadPath="Assets/TumbangPreso/Art/models/materials/"+
                (bridge?"AsphaltRoad_IlalimNgTulay.mat":"AsphaltRoad.mat");
            if(alley||bridge)
            {
                var road=AssetDatabase.LoadAssetAtPath<Material>(roadPath);
                if(road!=null){road.color=new Color(1,.94f,.86f);EditorUtility.SetDirty(road);}
            }
        }
    }
}
