using System;
using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.CameraSystem
{
    public struct RecordedTrail
    {
        public int Id,Kind;
        public float Width;
        public Color Head,Tail;
        public Vector3[] Points;
        public static RecordedTrail[] Capture()
        {
            var result=new List<RecordedTrail>(12);
            foreach(var shoe in UnityEngine.Object.FindObjectsByType<Slipper>())
            {
                if(shoe.State!=SlipperState.InFlight||shoe.SeatOfOrigin<0||shoe.SeatOfOrigin>=4)continue;
                foreach(var trail in shoe.GetComponentsInChildren<TrailRenderer>())
                {
                    if(!trail.enabled||trail.positionCount<2)continue;
                    int kind=trail.name=="SlipperMotionStroke"?0:trail.name=="FireSlipperVfx"?1:trail.name=="PositiveTrace"?2:trail.name=="ReturnTrace"?3:-1;
                    if(kind<0||trail.positionCount>512)continue;
                    var raw=new Vector3[trail.positionCount];int count=trail.GetPositions(raw);if(count<2)continue;
                    bool reverse=(raw[count-1]-shoe.transform.position).sqrMagnitude<(raw[0]-shoe.transform.position).sqrMagnitude;
                    int kept=Math.Min(32,count);var points=new Vector3[kept];
                    for(int i=0;i<kept;i++)
                    {
                        float at=i*(count-1f)/(kept-1);if(reverse)at=count-1-at;
                        int left=Mathf.FloorToInt(at);points[i]=Vector3.Lerp(raw[left],raw[Mathf.Min(count-1,left+1)],at-left);
                    }
                    result.Add(new RecordedTrail{Id=shoe.SeatOfOrigin*4+kind,Kind=kind,Width=trail.widthMultiplier*trail.widthCurve.Evaluate(0),Head=trail.startColor,Tail=trail.endColor,Points=points});
                }
            }
            return result.ToArray();
        }
        public Vector3 Point(float t)
        {float at=Mathf.Clamp01(t)*(Points.Length-1);int left=Mathf.FloorToInt(at);return Vector3.Lerp(Points[left],Points[Mathf.Min(left+1,Points.Length-1)],at-left);}
    }

    public sealed class RecordedFlightStroke:IDisposable
    {
        private readonly GameObject _root;
        private readonly LineRenderer _line;
        private readonly Light _light;
        public RecordedFlightStroke(Transform parent,RecordedTrail stroke)
        {
            _root=new GameObject("RecordedFlightStroke");_root.transform.SetParent(parent,false);
            _line=_root.AddComponent<LineRenderer>();_line.useWorldSpace=true;_line.positionCount=16;
            _line.numCapVertices=2;_line.shadowCastingMode=ShadowCastingMode.Off;_line.receiveShadows=false;
            _line.widthCurve=stroke.Kind==0?new AnimationCurve(new Keyframe(0,1),new Keyframe(.35f,.65f),new Keyframe(1,0)):
                AnimationCurve.Linear(0,1,1,0);
            var material=new Material(Shader.Find("Sprites/Default")){name="Recorded flight colour"};_line.sharedMaterial=material;VfxRenderTag.Own(_root,material);
            if(stroke.Kind==1||stroke.Kind==2)
            {
                _light=_root.AddComponent<Light>();_light.type=LightType.Point;_light.shadows=LightShadows.None;
                _light.color=stroke.Kind==1?AbilityVfx.FireHotColour:new Color(1,.9f,.2f);
                _light.range=stroke.Kind==1?2.1f:1.6f;_light.intensity=stroke.Kind==1?.65f:.25f;
            }
            Visible(false);
        }
        public void Sample(RecordedTrail left,RecordedTrail? right,float blend)
        {
            _line.widthMultiplier=left.Width;_line.startColor=left.Head;_line.endColor=left.Tail;
            for(int i=0;i<16;i++){float t=i/15f;var p=left.Point(t);if(right.HasValue)p=Vector3.Lerp(p,right.Value.Point(t),blend);_line.SetPosition(i,p);}
            if(_light!=null)_light.transform.position=_line.GetPosition(0);
        }
        public void Visible(bool on){_line.forceRenderingOff=!on;if(_light!=null)_light.enabled=on;}
        public void Dispose(){if(_root!=null)UnityEngine.Object.Destroy(_root);}
    }
}
