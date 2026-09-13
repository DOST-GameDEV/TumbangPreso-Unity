using System.IO;
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
        public static void Apply(string map)
        {
            bool alley=map=="Eskinita",bridge=map=="IlalimNgTulay";
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
                light.transform.rotation=Quaternion.Euler(alley?34:bridge?38:46,alley?-38:bridge?-52:-28,0);
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
            sky.SetColor("_Zenith",alley?new Color(.43f,.53f,.60f):bridge?new Color(.44f,.55f,.63f):new Color(.47f,.60f,.69f));
            var horizon=alley?new Color(.80f,.73f,.62f):bridge?new Color(.76f,.75f,.69f):new Color(.80f,.79f,.71f);
            sky.SetColor("_Horizon",horizon);sky.SetColor("_Ground",new Color(.40f,.37f,.31f));
            sky.SetColor("_SunColor",sunColor);sky.SetVector("_SunDirection",sunDirection);
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
