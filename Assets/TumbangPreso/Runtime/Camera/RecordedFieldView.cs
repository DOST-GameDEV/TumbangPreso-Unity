using System;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public struct RecordedField {public int Id;public WorldEffectSnapshot.Field State;}
    public sealed class RecordedFieldFrame {public float Time;public RecordedField[] Fields;public RecordedEnvironment Lighting;public RecordedTrail[] Trails=Array.Empty<RecordedTrail>();}
    // The seven persistent field families reuse their authored visual builders.
    // No gameplay factory, collider, hazard registry or expiry callback is invoked.
    public sealed class RecordedFieldView : IDisposable
    {
        public GameObject Root {get;private set;}
        private Action<float> _step;
        private Renderer[] _renderers;
        private Light[] _lights;
        private Transform _mouth;
        private DanteCarapaceVisual _ward;
        private GameObject _attached;
        private WorldEffectSnapshot.Field _built;
        public RecordedFieldView(Transform parent,WorldEffectSnapshot.Field field,GameObject recordedOwnerModel=null,GameObject recordedPropModel=null)
        {
            var random=UnityEngine.Random.state;
            try
            {
            _built=field;
            Root=new GameObject("RecordedField-"+field.Type);Root.transform.SetParent(parent,false);Root.transform.position=field.Position;
            if(RafiWaterField.IsWater(field.Type)){var fx=RafiWaterVisual.Build(Root.transform,field);fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Sheet){var fx=FrostSurfacePresentation.Build(Root.transform,field.Radius,field.Duration);fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Fire||field.Type==WorldEffectSnapshot.Kind.Crater)
            {SeanHeatGround.Build(Root.transform,field.Radius,field.Duration,field.Forward,field.Type==WorldEffectSnapshot.Kind.Crater);var fx=Root.GetComponentInChildren<SeanHeatGround>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Shock)
            {ZackSkateWake.Build(Root.transform,field.Radius,field.Duration,field.Forward);var fx=Root.GetComponentInChildren<ZackSkateWake>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Barricade)
            {Root.transform.rotation=Quaternion.LookRotation(field.Forward);CheskaIceVisuals.BuildWall(Root.transform,field.FirstScale,field.SecondScale,field.Split,renderOnly:true);}
            else if(field.Type==WorldEffectSnapshot.Kind.Hex)
            {var visual=HeroHazards.SpawnHexSigil(field.Position,field.Radius,field.Duration,field.Owner,field.FirstScale,silent:true,renderOnly:true);visual.transform.SetParent(Root.transform,true);var fx=visual.GetComponent<HeroHazards.WardInscribe>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Gale)
            {var fx=AmihanGaleFront.Build(Root.transform,field.Position,field.Forward,field.Duration,Core.AmihanRules.WhirlwindSpeed,Core.AmihanRules.WhirlwindStart,Core.AmihanRules.WhirlwindWidth);fx.enabled=false;_step=fx.StepTo;}
            // Paete (HERO-9): the render-only bodies, posed from age alone like every other field here.
            else if(field.Type==WorldEffectSnapshot.Kind.Plant)
            {var body=PaetePlantBody.Build(Root.transform);float until=field.FirstScale,born=field.Duration-field.Remaining;
             _step=age=>{float a=age;float loosen=Mathf.Clamp01((a-Core.PaeteRules.PlantRootedSeconds)/(Core.PaeteRules.PlantLifeSeconds-Core.PaeteRules.PlantRootedSeconds));
                 body.Pose(a,loosen,a>=Core.PaeteRules.PlantRootedSeconds,Mathf.Clamp01(1f-(born+until-a)/Core.PaeteRules.PlantReloadSeconds),99f);};}
            else if(field.Type==WorldEffectSnapshot.Kind.Thorns)
            {var body=PaeteThornBody.Build(Root.transform,new System.Collections.Generic.List<Slipper>());var at=field.Position;_step=age=>body.Pose(age,at);}
            else if(field.Type==WorldEffectSnapshot.Kind.Sentry)
            {var body=PaeteSentryBody.Build(Root.transform);var at=field.Position;_step=age=>body.Pose(age,at);}
            else if(field.Type==RecordedSpecialFields.Storm)
            {var fx=AmihanStormFan.Build(Root.transform,field.Position,field.Forward,Core.AmihanRules.StormSurgeGatherSeconds);fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Fissure)
            {var fx=DanteFissurePillar.Create(field.Position,field.Forward,(int)field.FirstScale,field.Duration,renderOnly:true);fx.transform.SetParent(Root.transform,true);fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==RecordedSpecialFields.Coven)
            {
                var visual=HeroHazards.SpawnGrandCovenEclipse(field.Position,field.Radius,field.Duration-field.FirstScale,field.FirstScale,renderOnly:true);
                visual.transform.SetParent(Root.transform,true);
                var timelines=new System.Collections.Generic.List<IVfxTimeline>();
                foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
                    if(behaviour is IVfxTimeline timeline){behaviour.enabled=false;timelines.Add(timeline);}
                _step=age=>{foreach(var timeline in timelines)timeline.StepTo(age);};
            }
            else if(field.Type==RecordedSpecialFields.Kuro)
            {
                _mouth=new GameObject("RecordedMawPoint").transform;_mouth.SetParent(Root.transform,false);_mouth.position=field.Forward;
                KuroIntakePresentation.Build(Root.transform,field.Radius,field.Duration,field.Owner,true,_mouth);
                var fx=Root.GetComponent<KuroIntakePresentation>();fx.enabled=false;_step=fx.StepTo;
            }
            else if(field.Type==RecordedSpecialFields.Seismic)
            {
                var fx=DanteSeismicVisual.Recorded(field.Position,field.Forward,field.Radius,field.Split,field.SecondScale>0,field.FirstScale>0,field.Duration);
                fx.transform.SetParent(Root.transform,true);_step=fx.StepTo;
            }
            else if(field.Type==RecordedSpecialFields.Nova)
            {
                var visual=FrostSurfacePresentation.Nova(field.Position,field.Radius,renderOnly:true);visual.transform.SetParent(Root.transform,true);
                var fx=visual.GetComponent<FrostSurfacePresentation>();fx.enabled=false;_step=fx.StepTo;
            }
            else if(field.Type==RecordedSpecialFields.Ward)
            {
                if(recordedOwnerModel==null)throw new ArgumentException("Recorded armor requires its recorded body");
                Root.transform.SetPositionAndRotation(field.Position,Quaternion.LookRotation(field.Forward));
                _ward=DanteCarapaceVisual.Recorded(recordedOwnerModel,Root.transform,field.FirstScale>0,field.Duration,field.SecondScale);
                if(_ward==null)throw new ArgumentException("Recorded armor torso was not found");_step=_ward.StepTo;
            }
            else if(field.Type==RecordedSpecialFields.Ignition||field.Type==RecordedSpecialFields.Charge)
            {
                if(recordedPropModel==null)throw new ArgumentException("Recorded weapon effect requires its recorded prop");
                var meshes=RecordedSpecialFields.PropMeshes(recordedPropModel);int index=(int)field.SecondScale;
                if(index<0||index>=meshes.Length)throw new ArgumentException("Recorded weapon surface is missing");
                if(field.Type==RecordedSpecialFields.Ignition)
                {var fx=SeanIgnitionVisual.Recorded(meshes[index]);_attached=fx.gameObject;_step=fx.StepTo;}
                else {var fx=ZackMagnetCharge.Recorded(meshes[index]);_attached=fx.gameObject;_step=fx.StepTo;}
            }
            else throw new ArgumentException("Unsupported recorded field");
            _renderers=_ward!=null?new System.Collections.Generic.List<Renderer>(_ward.VisiblePieces).ToArray():_attached!=null?_attached.GetComponentsInChildren<Renderer>(true):Root.GetComponentsInChildren<Renderer>(true);_lights=Root.GetComponentsInChildren<Light>(true);Visible(false);
            // The shared pure-visual builders disable primitive colliders immediately
            // and Unity removes them at this frame's end. Never admit an active one.
            foreach(var collider in Root.GetComponentsInChildren<Collider>(true))
                if(collider.enabled)throw new InvalidOperationException("Recorded "+field.Type+" has an active collider: "+collider.name);
            }
            catch{Dispose();throw;}
            finally{UnityEngine.Random.state=random;}
        }
        public bool Matches(WorldEffectSnapshot.Field field)=>field.Type==_built.Type&&field.Split==_built.Split&&field.EventId==_built.EventId;
        public void Sample(WorldEffectSnapshot.Field field,float elapsed)
        {if(_ward!=null)Root.transform.SetPositionAndRotation(field.Position,Quaternion.LookRotation(field.Forward));if(_mouth!=null)_mouth.position=field.Forward;_step?.Invoke(field.Type==RecordedSpecialFields.Ignition||field.Type==RecordedSpecialFields.Charge?field.FirstScale+elapsed:Mathf.Clamp(field.Duration-field.Remaining+elapsed,0,field.Duration));}
        public void Visible(bool on){foreach(var renderer in _renderers)if(renderer!=null)renderer.forceRenderingOff=!on;foreach(var light in _lights)if(light!=null)light.enabled=on;}
        public void Dispose(){if(_attached!=null)UnityEngine.Object.Destroy(_attached);_attached=null;if(_ward!=null)UnityEngine.Object.Destroy(_ward.gameObject);_ward=null;if(Root!=null)UnityEngine.Object.Destroy(Root);Root=null;}
    }
}
