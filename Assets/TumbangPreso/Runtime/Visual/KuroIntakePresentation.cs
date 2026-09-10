using System.Linq;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Visible inward flow into the actual familiar, inside its real pull radius.</summary>
    public sealed class KuroIntakePresentation : MonoBehaviour, IVfxTimeline
    {
        private GhostPetCompanion _ghost;
        private float _radius,_duration,_age;
        private readonly LineRenderer[] _wisps=new LineRenderer[8];
        private Material _reachMaterial;
        private readonly Vector3[] _starts=new Vector3[8];
        public float LifeSeconds => _duration;

        public static void Build(Transform parent,float radius,float duration,int ownerSlot,bool fromPet)
        {
            var effect=parent.gameObject.AddComponent<KuroIntakePresentation>();
            effect._radius=radius;effect._duration=duration;
            if (fromPet)
                effect._ghost=GameServices.Round?.PlayerAt(ownerSlot)?.GetComponent<CharacterVisual>()?.Companion;
            if (effect._ghost==null)
            {
                var source=Resources.Load<RosterBook>("RosterBook")?.People.FirstOrDefault(p=>p.Id=="nemu")?.PetModel;
                if (source!=null)
                {
                    var pet=Object.Instantiate(source,parent.position,Quaternion.identity);
                    ToonSkin.Apply(pet,ToonSkin.PersonOutlineWidth*.4f,null);
                    foreach(var face in pet.GetComponentsInChildren<Renderer>())
                        if(face.name.Contains("eye")||face.name.Contains("mouth"))ToonSkin.Apply(face,0,null);
                    effect._ghost=pet.AddComponent<GhostPetCompanion>();
                    effect._ghost.Bind(parent,Vector3.zero,CharacterVisual.PersonScale);
                    effect._ghost.Devour(duration);
                }
            }
            var reach=VfxShapes.Lay(parent,"SeanceReach",VfxShapes.Collar(64,0,.977f),radius,0);
            VfxMaterial.Ghost(reach.GetComponent<Renderer>(),new Color(.39f,.34f,.53f,.64f),.04f);
            VfxShapes.DrapeToGround(reach,.004f);
            effect._reachMaterial=reach.GetComponent<Renderer>().sharedMaterial;
            for(int i=0;i<effect._wisps.Length;i++)
            {
                float angle=i*Mathf.PI*.25f;
                effect._starts[i]=VfxShapes.GroundPoint(parent.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius)+Vector3.up*.07f;
                var go=new GameObject("InwardWisp_"+i);go.transform.SetParent(parent,false);
                var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;
                line.positionCount=9;line.widthMultiplier=.018f;
                line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.5f,1),new Keyframe(1,0));
                VfxMaterial.Ghost(line,new Color(.67f,.60f,.79f,.44f),.06f);
                effect._wisps[i]=line;
            }
            effect.StepTo(0);
        }
        private void Update()=>StepTo(_age+Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age=seconds;
            Vector3 mouth=_ghost!=null?_ghost.MouthPosition:transform.position+Vector3.up*1.5f;
            float fade=Mathf.Clamp01((_duration-seconds)/.12f);
            if(_reachMaterial!=null){var c=_reachMaterial.color;c.a=.64f*fade;_reachMaterial.color=c;}
            for(int i=0;i<_wisps.Length;i++)
            {
                float angle=i*Mathf.PI*.25f;
                Vector3 start=_starts[i];
                float head=Mathf.Repeat(seconds*1.1f+i*.127f,1.22f);
                for(int p=0;p<9;p++)
                {
                    float t=Mathf.Clamp01(head-(8-p)*.026f);
                    Vector3 point=Vector3.Lerp(start,mouth,t);
                    float arc=Mathf.Sin(t*Mathf.PI)*.22f;
                    point+=new Vector3(-Mathf.Sin(angle)*arc,arc*.30f,Mathf.Cos(angle)*arc);
                    _wisps[i].SetPosition(p,point);
                }
                var color=_wisps[i].sharedMaterial.color;color.a=.44f*fade;
                _wisps[i].sharedMaterial.color=color;
            }
        }
    }
}
