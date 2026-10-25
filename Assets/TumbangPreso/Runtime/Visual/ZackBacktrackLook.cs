using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Render-only reverse travel. The accepted teleport still owns gameplay; this copy and eye offset own the pull.
    [DefaultExecutionOrder(2500)]
    public sealed class ZackBacktrackLook : MonoBehaviour
    {
        public const float PullSeconds = .42f;
        private const float RingHold = .4f;
        private static readonly Dictionary<int,ZackBacktrackLook> Live = new Dictionary<int,ZackBacktrackLook>();
        private CharacterMotor _motor;
        private Vector3[] _path;
        private Vector3 _to, _modelOffset;
        private Quaternion _facing;
        private float _born, _poseTime;
        private int _serial, _round;
        private ZackFx.Echo _body;
        private readonly ZackFx.Echo[] _frames = new ZackFx.Echo[3];
        private Renderer[] _hidden;
        private bool[] _wasHidden;
        private LineRenderer _streak, _ink, _ring, _ringInk;
        private bool _restored, _landed;
        private float Age => Mathf.Max(0,Time.time-_born);
        public bool Pulling => _motor!=null && Age<PullSeconds;
        public Vector3 VisualFeet => FeetAt(Mathf.Clamp01(Age/PullSeconds));

        public static ZackBacktrackLook Play(CharacterMotor motor,Vector3 from,Vector3 to,Vector3[] path)
        {
            if(motor==null)return null;
            if(Live.TryGetValue(motor.PlayerSlot,out var old)&&old!=null)old.Finish();
            var v=new GameObject("ZackBacktrackPull").AddComponent<ZackBacktrackLook>();
            v._motor=motor;v._to=to;v._born=Time.time;v._serial=motor.PresentationTeleportSerial;
            v._round=GameServices.Match?.RoundNumber??0;v._facing=motor.transform.rotation;
            var points=new List<Vector3>{from};
            if(path!=null)for(int i=path.Length-1;i>=0;i--)
                if((path[i]-points[points.Count-1]).sqrMagnitude>.0025f)points.Add(path[i]);
            if((to-points[points.Count-1]).sqrMagnitude>.0001f)points.Add(to);
            if(points.Count<2)points.Add(to);v._path=points.ToArray();
            v._body=ZackFx.Echo.Of(motor.PlayerSlot,v.transform);
            if(v._body!=null)
            {
                v._poseTime=v._body.Track.Newest;v._body.PoseAt(v._poseTime);v._body.Coat(.18f);
                v._modelOffset=v._body.Copy.Root.transform.position-motor.transform.position;
                for(int i=0;i<3;i++)v._frames[i]=ZackFx.Echo.Of(motor.PlayerSlot,v.transform);
                v.CopyShoe();
                var surfaces=new List<Renderer>(motor.GetComponentsInChildren<Renderer>(true));
                var held=motor.GetComponent<Carrier>()?.Held;if(held!=null)surfaces.AddRange(held.GetComponentsInChildren<Renderer>(true));
                v._hidden=surfaces.ToArray();v._wasHidden=new bool[surfaces.Count];
                for(int i=0;i<surfaces.Count;i++){v._wasHidden[i]=surfaces[i].forceRenderingOff;surfaces[i].forceRenderingOff=true;}
            }
            v._ink=ZackFx.Line(v.transform,"PullRibbonInk",24,.13f,ZackFx.Ink,0);
            v._streak=ZackFx.Line(v.transform,"PullRibbon",24,.055f,ZackFx.Core,1.5f);
            v._ringInk=ZackFx.Line(v.transform,"LandingRingInk",24,.09f,ZackFx.Ink,0);
            v._ring=ZackFx.Line(v.transform,"LandingRing",24,.045f,ZackFx.Body,1.3f);
            v._ring.loop=v._ringInk.loop=true;Live[motor.PlayerSlot]=v;v.Step();return v;
        }
        public void SealTeleport(){if(_motor!=null)_serial=_motor.PresentationTeleportSerial;}
        public static Vector3 EyeOffset(CharacterMotor motor)
            => motor!=null&&Live.TryGetValue(motor.PlayerSlot,out var v)&&v!=null&&v._motor==motor&&v.Pulling
                ?v.VisualFeet-motor.transform.position:Vector3.zero;
        private static float PullAt(float u)=>u<.7f?.9f*Mathf.Pow(u/.7f,1.65f):.9f+.1f*Mathf.SmoothStep(0,1,(u-.7f)/.3f);
        private Vector3 Sample(float u)
        {
            float f=Mathf.Clamp01(u)*(_path.Length-1);int i=Mathf.Min(Mathf.FloorToInt(f),_path.Length-2);
            return Vector3.Lerp(_path[i],_path[i+1],f-i);
        }
        private Vector3 FeetAt(float u)
        {
            var p=Sample(PullAt(u));return _motor==null?p:Vector3.Lerp(p,_motor.transform.position,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,u)));
        }
        private void CopyShoe()
        {
            var held=_motor.GetComponent<Carrier>()?.Held;var liveHand=_motor.GetComponent<CharacterVisual>()?.HandAnchor;
            Transform grip=null;foreach(var bone in _body.Copy.Bones)if(bone.name=="HandAnchor"){grip=bone;break;}
            if(held==null||liveHand==null||grip==null)return;
            var root=new GameObject("RewindingHeldSlipper");root.transform.SetParent(grip,false);
            foreach(var surface in held.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!surface.enabled||!surface.gameObject.activeInHierarchy||surface.GetComponent<VfxRenderTag>()!=null
                    ||!surface.TryGetComponent<MeshFilter>(out var mesh)||mesh.sharedMesh==null)continue;
                var part=new GameObject("HeldShoeSurface");part.transform.SetParent(root.transform,false);
                held.FitCarriedSurface(surface.transform,liveHand,part.transform);part.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                var r=part.AddComponent<MeshRenderer>();r.sharedMaterials=surface.sharedMaterials;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        private void LateUpdate()
        {
            if(_motor==null||_motor.AbilitySystem?.HeroId!="zack"||_motor.IsTagged||_motor.IsStunned||_motor.IsTripped
                ||GameServices.Round?.RoundActive!=true||(GameServices.Match?.RoundNumber??0)!=_round||_motor.PresentationTeleportSerial!=_serial)
            {Finish();return;}Step();if(Age>PullSeconds+RingHold+.1f)Finish();
        }
        private void Step()
        {
            float u=Mathf.Clamp01(Age/PullSeconds);bool eyes=ZackFx.InOwnFirstPerson(_motor);
            if(_body!=null)
            {
                _body.PoseAt(_poseTime-ZackRules.BacktrackSeconds*PullAt(u));
                _body.Copy.Root.transform.SetPositionAndRotation(FeetAt(u)+_modelOffset,_facing);_body.Visible(u<1&&!eyes);
                foreach(var r in _body.Copy.Root.GetComponentsInChildren<MeshRenderer>())if(r.name=="HeldShoeSurface")r.forceRenderingOff=u>=1||eyes;
                if(u<1){foreach(var r in _hidden)if(r!=null)r.forceRenderingOff=true;}else Restore();
            }
            for(int i=0;i<3;i++)
            {
                var frame=_frames[i];if(frame==null)continue;float behind=Mathf.Clamp01(u-.075f*(i+1));
                frame.PoseAt(_poseTime-ZackRules.BacktrackSeconds*PullAt(behind));
                frame.Copy.Root.transform.SetPositionAndRotation(FeetAt(behind)+_modelOffset,_facing);
                frame.Visible(!eyes&&u>.05f&&u<.98f&&!Settings.SettingsStore.Current.ReducedEffects);
            }
            float head=PullAt(u),tail=PullAt(Mathf.Max(0,u-.24f));
            for(int i=0;i<24;i++)
            {
                var p=Sample(Mathf.Lerp(tail,head,i/23f))+Vector3.up*(.72f+.07f*Mathf.Sin(i*2.7f+Age*70));
                _streak.SetPosition(i,p);_ink.SetPosition(i,p-Vector3.up*.01f);
                float a=i/24f*Mathf.PI*2;var ring=_to+new Vector3(Mathf.Cos(a)*.65f,.055f,Mathf.Sin(a)*.65f);
                _ring.SetPosition(i,ring);_ringInk.SetPosition(i,ring-Vector3.up*.01f);
            }
            _streak.enabled=_ink.enabled=u<1;float alpha=u<1?0:Mathf.Clamp01(1-(Age-PullSeconds)/RingHold);
            ZackFx.Tint(_ring,new Color(ZackFx.Body.r,ZackFx.Body.g,ZackFx.Body.b,alpha));
            ZackFx.Tint(_ringInk,new Color(ZackFx.Ink.r,ZackFx.Ink.g,ZackFx.Ink.b,alpha));
            if(!_landed&&u>=1){_landed=true;ZackFx.Sparks(_to+Vector3.up*.12f,.45f,4);GameServices.Audio?.PlayAt("sfx_zack_backtrack_land",_to);}
        }
        private void Restore()
        {
            if(_restored||_hidden==null)return;_restored=true;
            for(int i=0;i<_hidden.Length;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
        }
        private void Finish(){Restore();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);Destroy(gameObject);}
        private void OnDestroy(){Restore();_body?.Destroy();foreach(var f in _frames)f?.Destroy();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);}
    }
}
