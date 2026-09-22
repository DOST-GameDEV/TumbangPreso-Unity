using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // V2: quieter structure and shade support the actual rule/identity objects.
    // Two bands remain on cast/hero props; imported environment materials stay lit.
    public sealed class WorldLookPresentation : MonoBehaviour
    {
        public static WorldLookPresentation Current {get;private set;}
        public WorldLookProfile.MapLook Look {get;private set;}
        public float Floor {get;private set;}
        private Texture2D _ramp;
        private Color _sky,_equator,_ground;private AmbientMode _ambientMode;
        private bool _fog;private FogMode _fogMode;private float _fogStart,_fogEnd,_weight=-1;
        private struct ShaderFrame
        {
            public Camera Camera;public Texture Ramp;public Vector4 Shape,Key;
            public float Weight;
        }
        private readonly List<ShaderFrame> _frames=new List<ShaderFrame>();
        private const string RampId="_WorldToonRamp",WeightId="_WorldLookWeight",ShapeId="_WorldLookShape",KeyId="_WorldKeyDirection";
        public static WorldLookPresentation Install(Transform parent,float floor)
        {
            var look=WorldLookProfile.Current.Find(parent.gameObject.scene.name);if(look==null)return null;
            var go=new GameObject("WorldLookPresentation");go.SetActive(false);go.transform.SetParent(parent,false);
            var owner=go.AddComponent<WorldLookPresentation>();owner.Look=look;owner.Floor=floor;owner.Build();go.SetActive(true);return owner;
        }
        private void Build()
        {
            _sky=RenderSettings.ambientSkyColor;_equator=RenderSettings.ambientEquatorColor;_ground=RenderSettings.ambientGroundColor;
            _ambientMode=RenderSettings.ambientMode;_fog=RenderSettings.fog;_fogMode=RenderSettings.fogMode;
            _fogStart=RenderSettings.fogStartDistance;_fogEnd=RenderSettings.fogEndDistance;
            _ramp=new Texture2D(32,1,TextureFormat.RGBA32,false,true){name=Look.Map+" two-band light ramp",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            float shadow=WorldLookProfile.Current.ShadowLevel;
            for(int i=0;i<32;i++)_ramp.SetPixel(i,0,Color.Lerp(Look.ShadowTint*shadow,Color.white,i/31f));
            _ramp.Apply(false,true);Current=this;ApplyScene();
        }
        public static bool HandlesCamera(Camera camera)
            =>Current!=null && camera!=null && (camera==Camera.main || camera.GetComponent<WorldLookCamera>()!=null ||
                camera.GetComponent<CameraSystem.CameraRig>()!=null || camera.name=="RecordedWorldCamera" || camera.name=="UltimateSceneCamera");
        private void OnEnable(){Camera.onPreCull+=BeginCamera;Camera.onPostRender+=EndCamera;}
        private void OnDisable()
        {
            Camera.onPreCull-=BeginCamera;Camera.onPostRender-=EndCamera;
            for(int i=_frames.Count-1;i>=0;i--)RestoreShader(_frames[i]);_frames.Clear();
            if(Current!=this)return;
            RestoreScene();Current=null;Shader.SetGlobalFloat(WeightId,0);
        }
        private void Update(){if(_weight!=Mathf.Clamp01(WorldCueProfile.Current.WorldLighting))ApplyScene();}
        private void ApplyScene()
        {
            if(Look==null)return;_weight=Mathf.Clamp01(WorldCueProfile.Current.WorldLighting);
            RenderSettings.ambientMode=_weight>0?AmbientMode.Trilight:_ambientMode;
            RenderSettings.ambientSkyColor=Color.Lerp(_sky,Look.Sky,_weight);
            RenderSettings.ambientEquatorColor=Color.Lerp(_equator,Look.Equator,_weight);
            RenderSettings.ambientGroundColor=Color.Lerp(_ground,Look.Ground,_weight);
            RenderSettings.fog=_weight>0 || _fog;RenderSettings.fogMode=_weight>0?FogMode.Linear:_fogMode;
            RenderSettings.fogStartDistance=Mathf.Lerp(_fogStart,Look.FogStart,_weight);
            RenderSettings.fogEndDistance=Mathf.Lerp(_fogEnd,Look.FogEnd,_weight);
        }
        private void RestoreScene()
        {
            RenderSettings.ambientMode=_ambientMode;RenderSettings.ambientSkyColor=_sky;
            RenderSettings.ambientEquatorColor=_equator;RenderSettings.ambientGroundColor=_ground;
            RenderSettings.fog=_fog;RenderSettings.fogMode=_fogMode;RenderSettings.fogStartDistance=_fogStart;RenderSettings.fogEndDistance=_fogEnd;
        }
        private void BeginCamera(Camera camera)
        {
            if(Current!=this)return;
            _frames.Add(new ShaderFrame{Camera=camera,Ramp=Shader.GetGlobalTexture(RampId),Weight=Shader.GetGlobalFloat(WeightId),
                Shape=Shader.GetGlobalVector(ShapeId),Key=Shader.GetGlobalVector(KeyId)});
            var profile=WorldLookProfile.Current;
            Shader.SetGlobalFloat(WeightId,HandlesCamera(camera)?_weight:0);
            Shader.SetGlobalTexture(RampId,_ramp);
            Shader.SetGlobalVector(ShapeId,new Vector4(profile.BandEdge,profile.UpperRim,profile.FeetShade,profile.MetalHighlight));
            var sun=SkyEvent.RecordedSun;Vector3 direction=sun!=null?-sun.transform.forward:Vector3.up;
            Shader.SetGlobalVector(KeyId,direction);
        }
        private void EndCamera(Camera camera)
        {
            for(int i=_frames.Count-1;i>=0;i--)if(_frames[i].Camera==camera)
            {var frame=_frames[i];_frames.RemoveAt(i);RestoreShader(frame);return;}
        }
        private static void RestoreShader(ShaderFrame frame)
        {
            Shader.SetGlobalTexture(RampId,frame.Ramp);Shader.SetGlobalFloat(WeightId,frame.Weight);
            Shader.SetGlobalVector(ShapeId,frame.Shape);Shader.SetGlobalVector(KeyId,frame.Key);
        }
        public static Color CourtChalk
            =>Current==null?WorldCueProfile.Current.Chalk:Color.Lerp(WorldCueProfile.Current.Chalk,Current.Look.Chalk,WorldCueProfile.Current.CourtSurface);
        public static Color CourtEdge
            =>Current==null?WorldCueProfile.Current.Ink:Color.Lerp(WorldCueProfile.Current.Ink,Current.Look.ChalkEdge,WorldCueProfile.Current.CourtSurface);
        private void OnDestroy(){if(_ramp!=null)Destroy(_ramp);}
    }
}
