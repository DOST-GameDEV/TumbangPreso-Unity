using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // V2 owner map: travel | peripheral strokes | paper/ink | local lens;
    // heard foot contact | broad bearing pip | paper/ink | local screen edge;
    // accepted ultimate | quieter world saturation | neutral | existing grade.
    // These optional comparisons default OFF; no gameplay fact comes from them.
    public sealed partial class ColourGrade
    {
        private struct HeardStep {public Vector3 Position;public float At,Gain;}
        private readonly HeardStep[] _heardSteps=new HeardStep[8];
        private readonly Vector4[] _cuePips=new Vector4[8];
        private int _nextStep;
        private float _cueSpeed,_cueWorld;
        private bool _cueHasPips;
        private Material _cueMaskMaterial;
        private CommandBuffer _cueMaskCommands;
        private readonly List<Renderer> _cueProtected=new List<Renderer>();
        private long _cuePhase=-1;
        private DepthTextureMode _cueOriginalDepth;
        private bool _cueOwnsDepth;
        public float ExperimentSpeed => _cueSpeed;
        public float ExperimentWorld => _cueWorld;
        public int ExperimentPipCount {get;private set;}
        private bool PersonalCueCamera => _camera!=null && _camera==Camera.main &&
            CourtBoundaryPresentation.Viewer(_camera)!=null && !PresentationClock.BlocksInput && !UI.Panel.AnyOpen;
        private bool WorldCueCamera => _camera!=null && GameServices.Round!=null &&
            (_camera==Camera.main || _camera.name=="UltimateSceneCamera");
        private void OnEnable(){AudioDirector.WorldCuePlayed+=HeardWorldStep;}
        private void OnDisable()
        {
            AudioDirector.WorldCuePlayed-=HeardWorldStep;
            System.Array.Clear(_heardSteps,0,_heardSteps.Length);ReleaseCueDepth();
        }
        private void OnPreCull()
        {
            PrepareExperimentValues();
            if(_cueWorld>0 && !_cueOwnsDepth)
            {_cueOriginalDepth=_camera.depthTextureMode;_camera.depthTextureMode|=DepthTextureMode.Depth;_cueOwnsDepth=true;}
            else if(_cueWorld<=0)ReleaseCueDepth();
        }
        private void ReleaseCueDepth()
        {
            if(!_cueOwnsDepth || _camera==null)return;
            // Remove only our bit, preserving other effects' normal/motion requests.
            if((_cueOriginalDepth&DepthTextureMode.Depth)==0)_camera.depthTextureMode&=~DepthTextureMode.Depth;
            _cueOwnsDepth=false;
        }
        private void HeardWorldStep(string id,Vector3 position,float pitch,float gain)
        {
            var profile=WorldCueProfile.Current;
            if(profile.SoundPips<=0 || !PersonalCueCamera || GameServices.Audio?.IsInReplayMix==true)return;
            if(id!="step_rubber" && id!="sfx_step_deck" && id!="sfx_swim_stroke")return;
            var viewer=CourtBoundaryPresentation.Viewer(_camera);var delta=position-viewer.transform.position;delta.y=0;
            if(delta.sqrMagnitude<.8f*.8f)return;
            // Same2..32m linear envelope as AudioDirector's actual world voices.
            // Pre-slider mix gain intentionally still works for muted/deaf play.
            float distance=Vector3.Distance(_camera.transform.position,position);
            float heard=gain*Mathf.Clamp01((32-distance)/30);
            if(heard<.015f)return;
            _heardSteps[_nextStep++%_heardSteps.Length]=new HeardStep{Position=position,At=Time.unscaledTime,Gain=Mathf.Clamp01(heard*4)};
        }
        private void PrepareExperimentValues()
        {
            _cueSpeed=_cueWorld=0;_cueHasPips=false;ExperimentPipCount=0;System.Array.Clear(_cuePips,0,_cuePips.Length);
            var profile=WorldCueProfile.Current;var settings=Settings.SettingsStore.Current;
            if(PersonalCueCamera)
            {
                var actor=CourtBoundaryPresentation.Viewer(_camera);Vector3 velocity=actor.PresentationTravelVelocity;velocity.y=0;
                float walk=Balance.Speed*Stamina.RoleSpeedScale(actor.IsDefender)*Roster.PersonSpeedScale(actor.CharacterIndex,actor.Mode);
                bool fast=actor.Stamina.IsSprinting || velocity.magnitude>walk*1.4f;
                if(fast && !settings.ReducedUiMotion && !settings.ReducedEffects)
                    _cueSpeed=profile.SpeedLines*Mathf.InverseLerp(walk,walk*2,velocity.magnitude);
                if(profile.SoundPips>0 && GameServices.Audio?.IsInReplayMix!=true)
                {
                    for(int i=0;i<_heardSteps.Length;i++)
                    {
                        var step=_heardSteps[i];float age=Time.unscaledTime-step.At;
                        if(step.Gain<=0 || age<0 || age>.45f)continue;
                        Vector3 direction=_camera.transform.InverseTransformDirection(step.Position-_camera.transform.position);
                        float bearing=Mathf.Atan2(direction.x,direction.z);
                        int sector=((Mathf.RoundToInt(bearing/(Mathf.PI/4))%8)+8)%8;float angle=sector*Mathf.PI/4;
                        float strength=step.Gain*(1-age/.45f)*profile.SoundPips;
                        if(strength<=_cuePips[sector].z)continue;
                        _cuePips[sector]=new Vector4(.5f+Mathf.Sin(angle)*.46f,.5f+Mathf.Cos(angle)*.45f,strength,0);
                    }
                    foreach(var pip in _cuePips)if(pip.z>0){_cueHasPips=true;ExperimentPipCount++;}
                }
            }
            var phase=SharedUltimatePhase.Instance;
            if(WorldCueCamera && phase!=null && phase.Active && !settings.ReducedEffects && !settings.ReducedUiMotion)
            {
                float age=(float)(SharedUltimatePhase.Now-phase.Began);
                float envelope=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.18f))*Mathf.Clamp01(((float)phase.Duration-age)/.35f);
                _cueWorld=Mathf.Clamp01(profile.UltimateDesaturation)*envelope;
            }
        }
        private bool HasExperiments => _cueSpeed>0 || _cueWorld>0 || _cueHasPips;
        private RenderTexture PrepareExperimentMaterial(RenderTexture source)
        {
            _material.SetFloat("_CueSpeed",_cueSpeed);_material.SetFloat("_CueTime",Time.unscaledTime);
            _material.SetFloat("_CueEdges",_cueSpeed>0 || _cueHasPips?1:0);
            _material.SetVector("_CuePixels",new Vector4(source.width,source.height,1f/source.width,1f/source.height));
            _material.SetVectorArray("_CuePips",_cuePips);
            _material.SetFloat("_CueWorld",0);_material.SetTexture("_CueMask",Texture2D.blackTexture);
            if(_cueWorld<=0)return null;
            if(_cueMaskMaterial==null)
            {
                var shader=Shader.Find("TumbangPreso/ProtectedColourMask");
                if(shader==null || !shader.isSupported)return null;
                _cueMaskMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                _cueMaskCommands=new CommandBuffer{name="Optional world-only ultimate grade"};
            }
            long phase=SharedUltimatePhase.Instance?.PhaseId??-1;
            if(_cuePhase!=phase || _cueProtected.Count==0)
            {
                _cuePhase=phase;_cueProtected.Clear();
                foreach(var surface in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                    foreach(var material in surface.sharedMaterials)
                        if(material!=null && material.shader!=null && material.shader.name=="TumbangPreso/Toon")
                        {_cueProtected.Add(surface);break;}
            }
            var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)?RenderTextureFormat.R8:RenderTextureFormat.ARGB32;
            var mask=RenderTexture.GetTemporary(source.width,source.height,0,format,RenderTextureReadWrite.Linear);
            mask.filterMode=FilterMode.Point;
            // Explicit unlit matrices leave the camera/lighting globals alone.
            _cueMaskMaterial.SetMatrix("_CueVP",GL.GetGPUProjectionMatrix(_camera.projectionMatrix,true)*_camera.worldToCameraMatrix);
            _cueMaskMaterial.SetMatrix("_CueView",_camera.worldToCameraMatrix);
            _cueMaskCommands.Clear();_cueMaskCommands.SetRenderTarget(mask);
            _cueMaskCommands.ClearRenderTarget(false,true,Color.black);
            foreach(var surface in _cueProtected)
            {
                if(surface==null || !surface.enabled || !surface.gameObject.activeInHierarchy || surface.forceRenderingOff ||
                    surface.shadowCastingMode==ShadowCastingMode.ShadowsOnly || (_camera.cullingMask&(1<<surface.gameObject.layer))==0)continue;
                var materials=surface.sharedMaterials;
                for(int index=0;index<materials.Length;index++)
                    if(materials[index]!=null && materials[index].shader.name=="TumbangPreso/Toon")_cueMaskCommands.DrawRenderer(surface,_cueMaskMaterial,index,0);
            }
            Graphics.ExecuteCommandBuffer(_cueMaskCommands);
            _material.SetTexture("_CueMask",mask);_material.SetFloat("_CueWorld",_cueWorld);return mask;
        }
        private void DisposeExperiments()
        {
            ReleaseCueDepth();_cueMaskCommands?.Release();_cueMaskCommands=null;
            if(_cueMaskMaterial!=null)DestroyImmediate(_cueMaskMaterial);
        }
    }
}
