using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // The per-map world look. See WorldLookProfile for the bright-look reasoning.
    // Cast and hero props wear a soft wrapped ramp; imported environment materials stay on
    // their own shaders and are brightened by the rig (sun, sky ambient, air) and the grade.
    // ⚠️ The world is never given a banded toon ramp: that was reverted on 2026-07-29 for
    // banding on large flat surfaces, and nothing here reintroduces it.
    public sealed class WorldLookPresentation : MonoBehaviour
    {
        public static WorldLookPresentation Current {get;private set;}
        public WorldLookProfile.MapLook Look {get;private set;}
        public float Floor {get;private set;}
        /// <summary>The live look weight, 0 for the scene's own lighting.</summary>
        public float Weight => Mathf.Max(0,_weight);
        private Texture2D _ramp;
        private bool _preview;
        private Color _sky,_equator,_ground;private AmbientMode _ambientMode;
        private UnityEngine.SceneManagement.Scene _settingsScene;
        private bool _fog;private FogMode _fogMode;private float _fogStart,_fogEnd,_weight=-1;private Color _fogColour;
        // The key light and sky as the scene authored them, restored exactly on the way out.
        private Light _sun;private Color _sunColour;private float _sunIntensity,_sunShadow;private Quaternion _sunRotation;
        private Material _skyAuthored,_skyLook;
        // The court's own dark ground, found once every Start has run. See MapLook.GroundLift.
        private readonly List<(Renderer renderer,int index)> _court=new List<(Renderer,int)>();
        private MaterialPropertyBlock _groundBlock;private bool _groundPending=true;
        private struct ShaderFrame
        {
            public Camera Camera;public Texture Ramp;public Vector4 Shape,Key,GlassSky,GlassHorizon,Soft;
            public float Weight,Architecture;
        }
        private readonly List<ShaderFrame> _frames=new List<ShaderFrame>();
        private const string RampId="_WorldToonRamp",WeightId="_WorldLookWeight",ShapeId="_WorldLookShape",KeyId="_WorldKeyDirection";
        private const string ArchitectureId="_WorldArchitecture",GlassSkyId="_WorldGlassSky",GlassHorizonId="_WorldGlassHorizon";
        private const string SoftId="_WorldSoftLight";
        public static WorldLookPresentation Install(Transform parent,float floor)
            =>Create(parent,floor,null,false);
        public static WorldLookPresentation InstallPreview(Transform parent,float floor,Light sun)
            =>Create(parent,floor,sun,true);
        private static WorldLookPresentation Create(Transform parent,float floor,Light sun,bool preview)
        {
            var look=WorldLookProfile.Current.Find(parent.gameObject.scene.name);if(look==null)return null;
            var go=new GameObject("WorldLookPresentation");go.SetActive(false);go.transform.SetParent(parent,false);
            var owner=go.AddComponent<WorldLookPresentation>();owner.Look=look;owner.Floor=floor;owner._preview=preview;
            owner.Build(sun);go.SetActive(true);return owner;
        }
        private void Build(Light previewSun)
        {
            _sky=RenderSettings.ambientSkyColor;_equator=RenderSettings.ambientEquatorColor;_ground=RenderSettings.ambientGroundColor;
            _ambientMode=RenderSettings.ambientMode;_fog=RenderSettings.fog;_fogMode=RenderSettings.fogMode;
            _fogStart=RenderSettings.fogStartDistance;_fogEnd=RenderSettings.fogEndDistance;_fogColour=RenderSettings.fogColor;
            _settingsScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            _sun=_preview?previewSun:SkyEvent.RecordedSun;
            if(_sun!=null){_sunColour=_sun.color;_sunIntensity=_sun.intensity;_sunShadow=_sun.shadowStrength;_sunRotation=_sun.transform.rotation;}
            _skyAuthored=RenderSettings.skybox;
            // ⚠️ THE SKYBOX IS INSTANCED, NEVER WRITTEN THROUGH, for SkyEvent's reason: the map's
            // .mat is a shared asset and a runtime colour written into it survives into the
            // editor and into every later load of the map.
            if(_skyAuthored!=null && _skyAuthored.HasProperty("_Zenith"))
            {_skyLook=new Material(_skyAuthored){name=_skyAuthored.name+" (bright look)",hideFlags=HideFlags.HideAndDontSave};}
            _ramp=new Texture2D(64,1,TextureFormat.RGBA32,false,true){name=Look.Map+" soft light ramp",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var profile=WorldLookProfile.Current;
            Color shade=Look.ShadowTint*profile.ShadowLevel;shade.a=1;
            // ⚠️ THE WARM BAND IS WHERE LIGHT TURNS TO SHADE, NOT A SECOND LIGHT. It peaks at
            // 42 per cent of the ramp and is gone by the lit side, which is what reads as the
            // soft-toy warmth on PEAK's scouts. Kept small: a strong one reads as sunburn.
            var warm=new Color(1,.62f,.42f);
            for(int i=0;i<64;i++)
            {
                float t=i/63f,s=t*t*(3-2*t);
                Color c=Color.Lerp(shade,Color.white,s);
                float band=Mathf.Exp(-Mathf.Pow((t-.42f)/.2f,2))*profile.Terminator;
                c=Color.Lerp(c,new Color(c.r*warm.r*1.25f,c.g*warm.g*1.25f,c.b*warm.b*1.25f),band);c.a=1;
                _ramp.SetPixel(i,0,c);
            }
            _ramp.Apply(false,true);Current=this;ApplyScene();
        }
        // The explicit preview sun (or the match sun captured at installation).
        // Screen-space edges must not pick a menu portrait light instead.
        public Light KeyLight=>_sun;
        public static bool HandlesCamera(Camera camera)
            =>Current!=null && camera!=null && (camera.GetComponent<WorldLookCamera>()!=null ||
                (!Current._preview && (camera==Camera.main || camera.GetComponent<CameraSystem.CameraRig>()!=null ||
                camera.name=="RecordedWorldCamera" || camera.name=="UltimateSceneCamera")));
        // Menu refresh restores its cached authored environment first. Reapply the
        // same live rig without rebuilding ramps, sky instances or ground discovery.
        public void ReapplyPreview(){if(_preview && Current==this)ApplyScene();}
        private void OnEnable(){Camera.onPreCull+=BeginCamera;Camera.onPostRender+=EndCamera;}
        private void OnDisable()
        {
            Camera.onPreCull-=BeginCamera;Camera.onPostRender-=EndCamera;
            for(int i=_frames.Count-1;i>=0;i--)RestoreShader(_frames[i]);_frames.Clear();
            if(Current!=this)return;
            RestoreScene();Current=null;Shader.SetGlobalFloat(WeightId,0);Shader.SetGlobalFloat(ArchitectureId,0);
        }
        private void Update()
        {
            if(_groundPending){_groundPending=false;FindGround();ApplyGround();}
            if(_weight!=Mathf.Clamp01(WorldCueProfile.Current.WorldLighting))ApplyScene();
        }
        // ⚠️⚠️ FOUND BY SHAPE, NOT BY NAME, AND ONLY FLAT RENDERERS ARE LIFTED. The court floor
        // arrives through generated meshes and kit prefabs, and no material name is shared by
        // all five maps. A slab at least 6 m square whose top sits at the floor height and which
        // spans the court centre is the ground by definition; every other FLAT renderer drawing
        // the same material is the rest of that road. ⚠️ The flatness test matters: Kenney roads
        // can sample the same atlas as the houses, and a lift keyed on the material alone would
        // brighten every facade on the street with it.
        //
        // ⚠️ IT RUNS ON THE FIRST UPDATE, NOT IN BUILD, because `EnvColourPass.Start` swaps the
        // road onto a tinted instance and Start order between the two is not defined. By the
        // first Update every Start has run, and the colour is read off the live material on
        // every apply for the same reason.
        private void FindGround()
        {
            _court.Clear();if(Look==null || Look.GroundLift<=1.001f)return;
            var scene=gameObject.scene;var slabs=new HashSet<Material>();
            var renderers=FindObjectsByType<MeshRenderer>();
            foreach(var renderer in renderers)
            {
                if(renderer.gameObject.scene!=scene || renderer.GetComponentInParent<VfxRenderTag>()!=null ||
                    renderer.name.IndexOf("chalk",System.StringComparison.OrdinalIgnoreCase)>=0)continue;
                var b=renderer.bounds;
                if(b.size.x<6 || b.size.z<6 || b.size.y>.6f || b.max.y<Floor-.35f || b.max.y>Floor+.1f)continue;
                if(b.min.x>0 || b.max.x<0 || b.min.z>0 || b.max.z<0)continue;
                foreach(var material in renderer.sharedMaterials)if(material!=null && material.HasProperty("_Color"))slabs.Add(material);
            }
            if(slabs.Count==0){Debug.Log($"[WorldLook] {Look.Map}: no court ground found to lift.");return;}
            foreach(var renderer in renderers)
            {
                if(renderer.gameObject.scene!=scene || renderer.bounds.size.y>.6f || renderer.HasPropertyBlock())continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i]!=null && slabs.Contains(materials[i]))_court.Add((renderer,i));
            }
            // It reports the count: "the pass ran" and "the pass lifted anything" are different claims.
            Debug.Log($"[WorldLook] {Look.Map}: lifted {_court.Count} court ground slots over {slabs.Count} material(s) by {Look.GroundLift:0.##}.");
        }
        private void ApplyGround()
        {
            if(_court.Count==0)return;_groundBlock??=new MaterialPropertyBlock();
            float lift=Mathf.Lerp(1,Look.GroundLift,_weight);
            foreach(var (renderer,index) in _court)
            {
                if(renderer==null)continue;_groundBlock.Clear();
                var materials=renderer.sharedMaterials;
                if(_weight>0 && index<materials.Length && materials[index]!=null)
                {
                    Color c=materials[index].GetColor("_Color");
                    _groundBlock.SetColor("_Color",new Color(c.r*lift,c.g*lift,c.b*lift,c.a));
                }
                // Null removes ownership as well as values. An empty block can
                // still make HasPropertyBlock true when a cached map is revisited.
                renderer.SetPropertyBlock(_groundBlock.isEmpty?null:_groundBlock,index);
            }
        }
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
            RenderSettings.fogColor=Color.Lerp(_fogColour,Look.Fog,_weight);
            if(_sun!=null)
            {
                _sun.color=Color.Lerp(_sunColour,Look.Sun,_weight);
                _sun.intensity=Mathf.Lerp(_sunIntensity,Look.SunIntensity,_weight);
                _sun.shadowStrength=Mathf.Lerp(_sunShadow,Look.ShadowStrength,_weight);
                // ⚠️ ONLY THE ELEVATION MOVES, NEVER THE AZIMUTH. Each map's shadows fall the way
                // its streets were composed; lifting the sun shortens the court-wide black
                // shadows that made Eskinita and Ilalim read as night without turning them.
                Vector3 forward=_sunRotation*Vector3.forward;
                if(Look.SunElevation>0 && forward.y<0)
                {
                    float authored=Mathf.Asin(-forward.y)*Mathf.Rad2Deg;
                    float elevation=Mathf.Lerp(authored,Look.SunElevation,_weight)*Mathf.Deg2Rad;
                    Vector3 flat=new Vector3(forward.x,0,forward.z).normalized;
                    Vector3 lifted=flat*Mathf.Cos(elevation)+Vector3.down*Mathf.Sin(elevation);
                    _sun.transform.rotation=Quaternion.LookRotation(lifted,Vector3.up);
                }
            }
            if(_skyLook!=null)
            {
                Blend("_Zenith",Look.Zenith);Blend("_Horizon",Look.Horizon);
                Blend("_CloudLight",Look.CloudLight);Blend("_CloudShade",Look.CloudShade);Blend("_SunColor",Look.Sun);
                if(_sun!=null)_skyLook.SetVector("_SunDirection",-_sun.transform.forward);
                RenderSettings.skybox=_weight>0?_skyLook:_skyAuthored;
            }
            ApplyGround();
        }
        private void Blend(string id,Color look){if(_skyAuthored.HasProperty(id))_skyLook.SetColor(id,Color.Lerp(_skyAuthored.GetColor(id),look,_weight));}
        private void RestoreScene()
        {
            // RenderSettings belongs to the active scene. A preview retiring
            // after START must not put its menu snapshot over the new match.
            // Object-owned sun and ground state still receive their handback.
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene()==_settingsScene)
            {
                RenderSettings.ambientMode=_ambientMode;RenderSettings.ambientSkyColor=_sky;
                RenderSettings.ambientEquatorColor=_equator;RenderSettings.ambientGroundColor=_ground;
                RenderSettings.fog=_fog;RenderSettings.fogMode=_fogMode;RenderSettings.fogStartDistance=_fogStart;RenderSettings.fogEndDistance=_fogEnd;
                RenderSettings.fogColor=_fogColour;
            }
            if(_sun!=null){_sun.color=_sunColour;_sun.intensity=_sunIntensity;_sun.shadowStrength=_sunShadow;_sun.transform.rotation=_sunRotation;}
            if(_skyLook!=null && RenderSettings.skybox==_skyLook)RenderSettings.skybox=_skyAuthored;
            foreach(var (renderer,index) in _court)if(renderer!=null)renderer.SetPropertyBlock(null,index);
        }
        private void BeginCamera(Camera camera)
        {
            if(Current!=this)return;
            _frames.Add(new ShaderFrame{Camera=camera,Ramp=Shader.GetGlobalTexture(RampId),Weight=Shader.GetGlobalFloat(WeightId),
                Shape=Shader.GetGlobalVector(ShapeId),Key=Shader.GetGlobalVector(KeyId),Architecture=Shader.GetGlobalFloat(ArchitectureId),
                GlassSky=Shader.GetGlobalVector(GlassSkyId),GlassHorizon=Shader.GetGlobalVector(GlassHorizonId),Soft=Shader.GetGlobalVector(SoftId)});
            var profile=WorldLookProfile.Current;
            Shader.SetGlobalFloat(WeightId,HandlesCamera(camera)?_weight:0);
            Shader.SetGlobalTexture(RampId,_ramp);
            Shader.SetGlobalVector(ShapeId,new Vector4(profile.BandEdge,profile.UpperRim,profile.FeetShade,profile.MetalHighlight));
            Shader.SetGlobalVector(SoftId,new Vector4(profile.Softness,profile.Wrap,profile.CastInkSelf,profile.CastInkWidth));
            var sun=KeyLight;Vector3 direction=sun!=null?-sun.transform.forward:Vector3.up;
            Shader.SetGlobalVector(KeyId,direction);
            // Use this map's authored sky palette, not a universal blue pane.
            // The same camera scope prevents leakage into character/menu previews.
            var sky=RenderSettings.skybox;
            Color top=sky!=null && sky.HasProperty("_Zenith")?sky.GetColor("_Zenith"):new Color(.47f,.60f,.69f);
            Color horizon=sky!=null && sky.HasProperty("_Horizon")?sky.GetColor("_Horizon"):new Color(.80f,.79f,.71f);
            Color tint=sky!=null && sky.HasProperty("_Tint")?sky.GetColor("_Tint"):Color.white;
            Shader.SetGlobalVector(GlassSkyId,(top*tint).linear);Shader.SetGlobalVector(GlassHorizonId,(horizon*tint).linear);
            Shader.SetGlobalFloat(ArchitectureId,HandlesCamera(camera)?WorldCueProfile.Current.EnvironmentAppeal:0);
        }
        private void EndCamera(Camera camera)
        {
            for(int i=_frames.Count-1;i>=0;i--)if(_frames[i].Camera==camera)
            {var frame=_frames[i];_frames.RemoveAt(i);RestoreShader(frame);return;}
        }
        private static void RestoreShader(ShaderFrame frame)
        {
            Shader.SetGlobalTexture(RampId,frame.Ramp);Shader.SetGlobalFloat(WeightId,frame.Weight);
            Shader.SetGlobalVector(ShapeId,frame.Shape);Shader.SetGlobalVector(KeyId,frame.Key);Shader.SetGlobalVector(SoftId,frame.Soft);
            Shader.SetGlobalFloat(ArchitectureId,frame.Architecture);Shader.SetGlobalVector(GlassSkyId,frame.GlassSky);Shader.SetGlobalVector(GlassHorizonId,frame.GlassHorizon);
        }
        public static Color CourtChalk
            =>Current==null?WorldCueProfile.Current.Chalk:Color.Lerp(WorldCueProfile.Current.Chalk,Current.Look.Chalk,WorldCueProfile.Current.CourtSurface);
        public static Color CourtEdge
            =>Current==null?WorldCueProfile.Current.Ink:Color.Lerp(WorldCueProfile.Current.Ink,Current.Look.ChalkEdge,WorldCueProfile.Current.CourtSurface);
        private void OnDestroy(){if(_ramp!=null)Destroy(_ramp);if(_skyLook!=null)Destroy(_skyLook);}
    }
}
