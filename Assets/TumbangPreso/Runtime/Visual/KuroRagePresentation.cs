using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Presentation only. The existing ability/companion owns duration, placement,
    // pull authority and cancellation; this samples the authored giant rig.
    internal sealed class KuroRagePresentation : IDisposable
    {
        private readonly Transform _calm, _form, _maw;
        private readonly Vector3 _restScale, _restPosition;
        private readonly List<Material> _owned = new List<Material>();
        private readonly List<Renderer> _eyes = new List<Renderer>();
        private readonly List<Transform> _aura = new List<Transform>();
        private PlayableGraph _graph;
        private AnimationClipPlayable _motion;
        private AnimationClip _clip;
        public bool Visible { get; private set; }
        public Vector3 MawPosition => _maw != null ? _maw.position : _form.position;

        public KuroRagePresentation(GameObject actor, Transform calm, Transform form)
        {
            _calm=calm;_form=form;_restScale=form.localScale;_restPosition=form.localPosition;
            foreach(var node in form.GetComponentsInChildren<Transform>(true))
                if(node.name=="RageMaw")_maw=node;
            ToonSkin.Apply(form.gameObject,0,null);
            foreach(var renderer in form.GetComponentsInChildren<Renderer>(true))
            {
                bool face=renderer.name.Contains("Eye") || renderer.name.Contains("Fang");
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)continue;
                    var owned=new Material(source);_owned.Add(owned);materials[i]=owned;
                    if(owned.HasProperty("_RimStrength"))
                    {
                        var color=owned.HasProperty("_Color")?owned.color:Color.black;
                        owned.SetFloat("_RimStrength",!face && color.maxColorComponent > .05f ? .32f : 0);
                        owned.SetFloat("_RimPower",3);
                        owned.SetColor("_RimColor",new Color(.48f,.23f,.72f));
                    }
                }
                renderer.sharedMaterials=materials;
                if(face){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                if(renderer.name.StartsWith("RageEye") && !renderer.name.Contains("Pupil"))_eyes.Add(renderer);
            }
            foreach(var eye in _eyes)
                for(int i=0;i<3;i++)
                {
                    var puff=GameObject.CreatePrimitive(PrimitiveType.Sphere);puff.name="KuroEyeWisp";
                    puff.transform.SetParent(form,false);puff.layer=form.gameObject.layer;
                    var collider=puff.GetComponent<Collider>();if(collider!=null)collider.enabled=false;
                    var renderer=puff.GetComponent<Renderer>();
                    VfxMaterial.Ghost(renderer,new Color(.41f,.14f,.68f,.12f),.18f);
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    _aura.Add(puff.transform);
                }
            _clip=Resources.Load<AnimationClip>("KuroRageInhale");
            if(_clip!=null)
            {
                var animator=actor.GetComponent<Animator>()??actor.AddComponent<Animator>();
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                _graph=PlayableGraph.Create("Kuro authored rage");_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _motion=AnimationClipPlayable.Create(_graph,_clip);_motion.SetApplyFootIK(false);_motion.SetSpeed(0);
                var output=AnimationPlayableOutput.Create(_graph,"Rage rig",animator);output.SetSourcePlayable(_motion);
                _graph.Play();
            }
            else Debug.LogError("[Kuro] Missing authored KuroRageInhale resource; rebuild its animation assets.");
            Sample(0,0,0);
        }

        public void Sample(float amount,float seconds,float smallFootDrop)
        {
            Visible=amount>=.24f;
            _form.gameObject.SetActive(Visible);_calm.gameObject.SetActive(!Visible);
            if(!Visible)return;
            float unfold=Mathf.InverseLerp(.24f,1,amount);
            // Match the small silhouette's height/feet at the swap, then unfold
            // into the full giant instead of snapping to its final scale.
            _form.localScale=_restScale*Mathf.Lerp(.55f,1,unfold);
            _form.localPosition=_restPosition-Vector3.up*(smallFootDrop*(1-unfold));
            if(_graph.IsValid())
            {
                _motion.SetTime(Mathf.Repeat(seconds,Mathf.Max(.001f,_clip.length)));
                _graph.Evaluate(0);
            }
            for(int i=0;i<_aura.Count;i++)
            {
                int eye=i/3;float phase=seconds*1.7f+i*2.1f;
                var center=_form.InverseTransformPoint(_eyes[eye].bounds.center);
                _aura[i].localPosition=center+new Vector3(Mathf.Cos(phase)*.16f,Mathf.Sin(phase)*.105f,-.055f);
                _aura[i].localScale=new Vector3(.19f,.14f,.09f)*(1+.15f*Mathf.Sin(phase+1));
            }
        }

        public void Dispose()
        {
            if(_graph.IsValid())_graph.Destroy();
            foreach(var material in _owned)
                if(material!=null)
                {
                    if(Application.isPlaying)UnityEngine.Object.Destroy(material);
                    else UnityEngine.Object.DestroyImmediate(material);
                }
        }
    }
}
