using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Small grounded dust, from the existing accepted/relayed audio contact.
    // Sound sliders do not gate the event. Saved cues give replay the same seed.
    public sealed class CourtContactDust : MonoBehaviour
    {
        public const float Life=.46f;
        private ParticleSystem _particles;
        private Renderer _renderer;
        private bool _recorded;
        private float _age;
        private static Material _material;
        public static bool Supports(string cue)=>cue=="can_knockdown" || cue=="land" || cue=="slide_scrape";
        public static CourtContactDust Play(string cue,Vector3 at,Transform recordedParent=null)
        {
            if(!Supports(cue) || WorldCueProfile.Current.InkEffects<=0 ||
                !WorldGround.TryBelow(at,.35f,1.35f,out float ground))return null;
            var go=new GameObject("Court dust "+cue);
            if(recordedParent!=null)go.transform.SetParent(recordedParent,false);
            go.transform.position=new Vector3(at.x,ground+.045f,at.z);VfxRenderTag.Attach(go);
            var effect=go.AddComponent<CourtContactDust>();effect._recorded=recordedParent!=null;
            var ps=go.AddComponent<ParticleSystem>();effect._particles=ps;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed=false;ps.randomSeed=cue=="land"?947u:cue=="slide_scrape"?331u:731u;
            bool reduced=Settings.SettingsStore.Current.ReducedEffects,slide=cue=="slide_scrape";
            var main=ps.main;main.duration=Life;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.26f,Life);main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,.9f);
            main.startSize=new ParticleSystem.MinMaxCurve(slide?.09f:.12f,slide?.17f:.23f);
            main.startColor=new Color(.93f,.87f,.73f,(reduced?.36f:.65f)*WorldCueProfile.Current.InkEffects);
            main.gravityModifier=0;main.maxParticles=8;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(reduced?3:slide?4:6))});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=cue=="can_knockdown"?.32f:.18f;shape.rotation=new Vector3(90,0,0);
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.y=new ParticleSystem.MinMaxCurve(.18f);
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.12f));
            var alpha=ps.colorOverLifetime;alpha.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.7f,.5f),new GradientAlphaKey(0,1)});alpha.color=gradient;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();effect._renderer=renderer;
            if(_material==null)_material=new Material(Shader.Find("TumbangPreso/InkDust")){name="Court chalk dust",hideFlags=HideFlags.DontSave};
            renderer.sharedMaterial=_material;renderer.maxParticleSize=.025f;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(effect._recorded)effect.Sample(0);else ps.Play();return effect;
        }
        public void Sample(float age){_particles.Simulate(Mathf.Clamp(age,0,Life),true,true,true);}
        public void ShowForCapture(bool visible){if(_renderer!=null)_renderer.forceRenderingOff=!visible;}
        private void Update()
        {
            if(_recorded)return;_age+=Time.deltaTime;
            if(_age>=Life || GameServices.Round==null || !GameServices.Round.RoundActive)Destroy(gameObject);
        }
    }
}
