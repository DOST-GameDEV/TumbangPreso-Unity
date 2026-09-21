using System;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public struct RecordedField {public int Id;public WorldEffectSnapshot.Field State;}
    public sealed class RecordedFieldFrame {public float Time;public RecordedField[] Fields;}
    // The seven persistent field families reuse their authored visual builders.
    // No gameplay factory, collider, hazard registry or expiry callback is invoked.
    public sealed class RecordedFieldView : IDisposable
    {
        public GameObject Root {get;private set;}
        private Action<float> _step;
        private Renderer[] _renderers;
        private WorldEffectSnapshot.Field _built;
        public RecordedFieldView(Transform parent,WorldEffectSnapshot.Field field)
        {
            _built=field;
            Root=new GameObject("RecordedField-"+field.Type);Root.transform.SetParent(parent,false);Root.transform.position=field.Position;
            if(field.Type==WorldEffectSnapshot.Kind.Sheet){var fx=FrostSurfacePresentation.Build(Root.transform,field.Radius,field.Duration);fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Fire||field.Type==WorldEffectSnapshot.Kind.Crater)
            {SeanHeatGround.Build(Root.transform,field.Radius,field.Duration,field.Forward,field.Type==WorldEffectSnapshot.Kind.Crater);var fx=Root.GetComponentInChildren<SeanHeatGround>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Shock)
            {ZackSkateWake.Build(Root.transform,field.Radius,field.Duration,field.Forward);var fx=Root.GetComponentInChildren<ZackSkateWake>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Barricade)
            {Root.transform.rotation=Quaternion.LookRotation(field.Forward);CheskaIceVisuals.BuildWall(Root.transform,field.FirstScale,field.SecondScale,field.Split,renderOnly:true);}
            else if(field.Type==WorldEffectSnapshot.Kind.Hex)
            {var visual=HeroHazards.SpawnHexSigil(field.Position,field.Radius,field.Duration,field.Owner,field.FirstScale,silent:true,renderOnly:true);visual.transform.SetParent(Root.transform,true);var fx=visual.GetComponent<HeroHazards.WardInscribe>();fx.enabled=false;_step=fx.StepTo;}
            else if(field.Type==WorldEffectSnapshot.Kind.Fissure)
            {var fx=DanteFissurePillar.Create(field.Position,field.Forward,(int)field.FirstScale,field.Duration,renderOnly:true);fx.transform.SetParent(Root.transform,true);fx.enabled=false;_step=fx.StepTo;}
            else throw new ArgumentException("Unsupported recorded field");
            _renderers=Root.GetComponentsInChildren<Renderer>(true);Visible(false);
            if(Root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Recorded field has a collider");
        }
        public bool Matches(WorldEffectSnapshot.Field field)=>field.Type==_built.Type&&field.Split==_built.Split;
        public void Sample(WorldEffectSnapshot.Field field,float elapsed)
        {_step?.Invoke(Mathf.Clamp(field.Duration-field.Remaining+elapsed,0,field.Duration));}
        public void Visible(bool on){foreach(var renderer in _renderers)if(renderer!=null)renderer.forceRenderingOff=!on;}
        public void Dispose(){if(Root!=null)UnityEngine.Object.Destroy(Root);Root=null;}
    }
}
