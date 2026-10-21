using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What the giant is doing right now, for <see cref="KuroRagePresentation"/> to act out on his rig. All zero is him
    /// idling: breathing, weaving, his tail running, his claws working. Degrees are in his own frame (x nods, y turns,
    /// z tilts). Look only: nothing here is read by the ability.
    /// </summary>
    public struct KuroLife
    {
        /// <summary>Where his head looks, on top of where his body faces.</summary>
        public float HeadYaw, HeadPitch, HeadRoll;
        /// <summary>His jaw, wider than it rests (below zero closes it a little).</summary>
        public float Jaw;
        /// <summary>0 to 1: gathered back before a pounce (arms in, head back, tail bunched), and thrown forward out of it.</summary>
        public float Coil, Throw;
        /// <summary>His chest taller (above zero) or shorter: a stretch, or the jiggle of a settle.</summary>
        public float Swell;
    }

    // Presentation only. The existing ability/companion owns duration, placement,
    // pull authority and cancellation; this samples the authored giant rig.
    internal sealed class KuroRagePresentation : IDisposable
    {
        private readonly Transform _calm, _form, _maw;
        private readonly Vector3 _restScale, _restPosition;
        private readonly List<Material> _owned = new List<Material>();
        private readonly List<Renderer> _eyes = new List<Renderer>();
        private readonly List<Transform> _aura = new List<Transform>();
        // ⚠️⚠️ HE IS ALIVE ON HIS OWN RIG (owner, 2026-10-10: "does nemus ghost have a rig/bones? it feels too stiff for me").
        // He has: eighteen joints (chest; a shoulder, an elbow, a wrist and three fingers an arm; head and jaw; three down
        // his tail), and one skin weighted smoothly across them. What he did not have was anything moving them: the
        // authored clip turns each joint about one degree, so he hung in the air like a statue with his arms out.
        // `Live` is the rest of him, laid over the clip every time he is sampled, from the clock he is sampled with:
        // breath, a weave and a hunch, a wave down the tail, arms that hang bent and reach in turn with the hand trailing
        // the forearm and the forearm the shoulder, claws that open and shut, a head that carries on past a turn and comes
        // back, a jaw that works. ⚠️ LOOK ONLY: joints under the form, nothing the ability reads; his `RageMaw` rides his
        // head as it always did and only the intake's picture follows it.
        private Transform _chest,_head,_jaw;
        private readonly Transform[] _tail=new Transform[3],_arm=new Transform[2],_forearm=new Transform[2],_hand=new Transform[2];
        private readonly Transform[] _finger=new Transform[6];
        private readonly List<Transform> _joints=new List<Transform>();
        private readonly List<Vector3> _jointPosition=new List<Vector3>(),_jointScale=new List<Vector3>();
        private readonly List<Quaternion> _jointRotation=new List<Quaternion>();
        private float _lifeYaw,_lifeCarry,_lifeSeconds=-1;
        private PlayableGraph _graph;
        private AnimationClipPlayable _motion;
        private AnimationClip _clip;
        public bool Visible { get; private set; }
        public Vector3 MawPosition => _maw != null ? _maw.position : _form.position;

        public KuroRagePresentation(GameObject actor, Transform calm, Transform form)
        {
            _calm=calm;_form=form;_restScale=form.localScale;_restPosition=form.localPosition;
            foreach(var node in form.GetComponentsInChildren<Transform>(true))
            {
                if(node.name=="RageMaw")_maw=node;
                switch(node.name)
                {
                    case "RageChest":_chest=node;break; case "RageHead":_head=node;break; case "RageJaw":_jaw=node;break;
                    case "RageWispA":_tail[0]=node;break; case "RageWispB":_tail[1]=node;break; case "RageWispC":_tail[2]=node;break;
                    case "RageArmLeft":_arm[0]=node;break; case "RageArmRight":_arm[1]=node;break;
                    case "RageForearmLeft":_forearm[0]=node;break; case "RageForearmRight":_forearm[1]=node;break;
                    case "RageHandLeft":_hand[0]=node;break; case "RageHandRight":_hand[1]=node;break;
                    default:
                        for(int side=0;side<2;side++)for(int k=0;k<3;k++)
                            if(node.name=="RageFinger"+(side==0?"Left":"Right")+k+"Joint")_finger[side*3+k]=node;
                        break;
                }
            }
            // Where every joint rests, put back before each sample: the clip then writes its own pose over it, and if the
            // clip is ever missing the life below still starts from rest instead of from the frame before.
            if(_chest!=null)
                foreach(var joint in _chest.GetComponentsInChildren<Transform>(true))
                { _joints.Add(joint);_jointPosition.Add(joint.localPosition);_jointRotation.Add(joint.localRotation);_jointScale.Add(joint.localScale); }
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
                var animator=actor.GetComponent<Animator>();
                if(animator==null)animator=actor.AddComponent<Animator>();
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                _graph=PlayableGraph.Create("Kuro authored rage");_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _motion=AnimationClipPlayable.Create(_graph,_clip);_motion.SetApplyFootIK(false);_motion.SetSpeed(0);
                var output=AnimationPlayableOutput.Create(_graph,"Rage rig",animator);output.SetSourcePlayable(_motion);
                _graph.Play();
            }
            else Debug.LogError("[Kuro] Missing authored KuroRageInhale resource; rebuild its animation assets.");
            Sample(0,0,0);
        }

        /// <summary>In play: idling, with his head carrying on past each turn his body makes and looking down at who he hunts.</summary>
        public void Sample(float amount,float seconds,float smallFootDrop)
        {
            if(_lifeSeconds<0||seconds<_lifeSeconds){_lifeYaw=_form.eulerAngles.y;_lifeCarry=0;}
            float dt=_lifeSeconds<0?0:Mathf.Clamp(seconds-_lifeSeconds,0,.1f);_lifeSeconds=seconds;
            float turned=Mathf.DeltaAngle(_lifeYaw,_form.eulerAngles.y);_lifeYaw=_form.eulerAngles.y;
            if(dt>0)_lifeCarry=Mathf.Lerp(_lifeCarry,Mathf.Clamp(turned/dt*.1f,-28,28),1-Mathf.Exp(-9f*dt));
            Sample(amount,seconds,smallFootDrop,new KuroLife{HeadYaw=_lifeCarry,HeadPitch=7,HeadRoll=-_lifeCarry*.3f});
        }

        public void Sample(float amount,float seconds,float smallFootDrop,KuroLife life)
        {
            Visible=amount>=.24f;
            _form.gameObject.SetActive(Visible);_calm.gameObject.SetActive(!Visible);
            if(!Visible)return;
            float unfold=Mathf.InverseLerp(.24f,1,amount);
            // Match the small silhouette's height/feet at the swap, then unfold
            // into the full giant instead of snapping to its final scale.
            _form.localScale=_restScale*Mathf.Lerp(.55f,1,unfold);
            _form.localPosition=_restPosition-Vector3.up*(smallFootDrop*(1-unfold));
            for(int i=0;i<_joints.Count;i++)
                if(_joints[i]!=null){_joints[i].localPosition=_jointPosition[i];_joints[i].localRotation=_jointRotation[i];_joints[i].localScale=_jointScale[i];}
            if(_graph.IsValid())
            {
                _motion.SetTime(Mathf.Repeat(seconds,Mathf.Max(.001f,_clip.length)));
                _graph.Evaluate(0);
            }
            Live(seconds,life);
            for(int i=0;i<_aura.Count;i++)
            {
                int eye=i/3;float phase=seconds*1.7f+i*2.1f;
                var center=_form.InverseTransformPoint(_eyes[eye].bounds.center);
                _aura[i].localPosition=center+new Vector3(Mathf.Cos(phase)*.16f,Mathf.Sin(phase)*.105f,-.055f);
                _aura[i].localScale=new Vector3(.19f,.14f,.09f)*(1+.15f*Mathf.Sin(phase+1));
            }
        }

        /// <summary>
        /// Turn a joint by an amount given in HIS frame (x nods forward, y turns, z tilts), whatever way the joint itself
        /// was bound. A parent is always turned before its children, so each turn rides on the ones above it.
        /// </summary>
        private void Turn(Transform joint,Quaternion by)
        {
            if(joint==null)return;
            var frame=_form.rotation;
            joint.rotation=frame*by*Quaternion.Inverse(frame)*joint.rotation;
        }

        private void Live(float t,KuroLife life)
        {
            if(_chest==null||_head==null)return;
            float breath=Mathf.Sin(t*2.1f),slow=Mathf.Sin(t*.9f),slower=Mathf.Sin(t*.55f+1.3f);
            // ---- spine: he hangs in the air and weaves, hunched over what is under him.
            float hunch=11f+3f*slow+14f*life.Throw-10f*life.Coil;
            Turn(_chest,Quaternion.Euler(hunch,5f*slower,4f*Mathf.Sin(t*.7f)));
            _chest.localPosition+=new Vector3(.10f*Mathf.Sin(t*.7f),.05f*breath-.14f*life.Coil,.06f*slow+.2f*life.Throw-.16f*life.Coil);
            // ---- breath: the chest swells and falls, and stretches or sits down when he is told to.
            _chest.localScale=Vector3.Scale(_chest.localScale,new Vector3(1+.03f*breath+.05f*life.Coil,1-.022f*breath+life.Swell-.08f*life.Coil+.07f*life.Throw,1+.04f*breath));
            // ---- head: against the breath, to what he looks at, never square on for long.
            Turn(_head,Quaternion.Euler(life.HeadPitch-hunch*.5f-2.5f*breath-13f*life.Coil+9f*life.Throw,
                life.HeadYaw+4f*Mathf.Sin(t*.8f+.6f),life.HeadRoll+5f*Mathf.Sin(t*.63f+2f)));
            // ---- jaw: eases with the breath, chatters now and then, and goes wide when he is thrown at something.
            float chatter=Mathf.Max(0,Mathf.Sin(t*1.3f))*Mathf.Sin(t*17f)*2.5f;
            Turn(_jaw,Quaternion.Euler(life.Jaw+3f*breath+chatter+16f*life.Throw-5f*life.Coil,0,0));
            // ---- tail: a wave running down it, bigger and later at each joint, so the tip lags and curls. It hangs: the
            // hunch above is taken back out of its first joint.
            for(int i=0;i<3;i++)
            {
                float phase=t*2.3f-i*1.15f,size=14f+8f*i;
                Turn(_tail[i],Quaternion.Euler((i==0?-hunch:0)+size*.5f*Mathf.Sin(phase*.8f+1f)+24f*life.Coil*(i+1)/3f-20f*life.Throw,0,size*Mathf.Sin(phase)));
            }
            // ⚠️ OUT TO HIS SIDES, NOT IN FRONT OF HIM. His arms are short for his head: swung forward as far as a person's hang,
            // both paws sat over his mouth and he read as a ball with claws on its face (film p11). They hang a little down and
            // a little forward, and only a reach or a coil brings one in.
            // ---- arms: hanging bent, not held out. Each reaches in its turn; the forearm follows the shoulder a beat late
            // and the hand follows the forearm, and the claws open as it goes out and shut as it comes back.
            Vector3 middle=_form.InverseTransformPoint(_chest.position);
            for(int side=0;side<2;side++)
            {
                if(_arm[side]==null)continue;
                float out_=Mathf.Sign(_form.InverseTransformPoint(_arm[side].position).x-middle.x);
                float beat=side==0?0:Mathf.PI;
                float reach=Reach(t,beat),after=Reach(t-.2f,beat),last=Reach(t-.4f,beat);
                Turn(_arm[side],Quaternion.Euler(6f*breath,-out_*(-7f+22f*reach+24f*life.Throw+26f*life.Coil),-out_*(7f+5f*breath-10f*reach-8f*life.Throw+12f*life.Coil)));
                Turn(_forearm[side],Quaternion.Euler(0,-out_*(9f-8f*after+40f*life.Coil-10f*life.Throw+6f*Mathf.Sin(t*1.7f+beat)),-out_*(8f-6f*after)));
                Turn(_hand[side],Quaternion.Euler(8f*Mathf.Sin(t*1.9f+beat),-out_*(5f-8f*last+12f*life.Coil),out_*9f*Mathf.Sin(t*1.1f+beat)));
                if(_hand[side]==null)continue;
                Vector3 palm=_form.InverseTransformPoint(_hand[side].position);
                for(int k=0;k<3;k++)
                {
                    var finger=_finger[side*3+k];if(finger==null)continue;
                    Vector3 along=_form.InverseTransformPoint(finger.position)-palm;
                    Vector3 axis=Vector3.Cross(along,Vector3.forward);
                    if(axis.sqrMagnitude<1e-10f)continue;
                    float curl=14f+20f*(.5f+.5f*Mathf.Sin(t*2.7f+k*1.1f+beat))-18f*last+38f*life.Coil-24f*life.Throw;
                    Turn(finger,Quaternion.AngleAxis(curl,axis.normalized));
                }
            }
        }

        /// <summary>One arm's reach on its own slow clock: out and back with a pause between, never a plain sine.</summary>
        private static float Reach(float t,float beat)
        { float u=Mathf.Max(0,Mathf.Sin(t*.85f+beat));return u*u*(3-2*u); }

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
