using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>New six-second neighborhood payoff. A visual rhyme, not a childhood/time-jump claim.</summary>
    public sealed class TrailerStreetPayoffFilm : MapFilm
    {
        public const float Length=6;
        public static readonly (string name,float from,float to)[] Shots={ ("C15-home",0,6) };
        protected override string Region=>"TRAILER";
        protected override Color Accent=>new Color(.95f,.74f,.4f);
        protected override string Score=>null;
        protected override float CloseAt(bool full)=>999;
        protected override bool OwnsLight=>true;
        private sealed class Child
        {
            public Extra E;public TrailerBody Body;public SkinnedMeshRenderer[] Skins;
            public Transform[] Bones;public Vector3[] Positions,Scales;public Quaternion[] Turns;
            public Mesh Baked;public bool[] CarriesScale;public readonly List<Vector3> Vertices=new List<Vector3>();
            public Vector3 ModelPosition,ModelScale;public Quaternion ModelTurn;
            private float Low(int index,bool scaled,out float high)
            {
                var skin=Skins[index];
                Matrix4x4 space=scaled?Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one):skin.transform.localToWorldMatrix;
                float low=float.PositiveInfinity;high=float.NegativeInfinity;
                foreach(var vertex in Vertices) {float y=space.MultiplyPoint3x4(vertex).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                return low;
            }
            public void Calibrate()
            {
                CarriesScale=new bool[Skins.Length];
                // Explicit useScale=false bakes in renderer-local coordinates; use its full localToWorld matrix.
            }
            public void Sample(Vector3 at,float yaw,string clip,float clock,Pose pose)
            {
                E.Show(true);E.Pose(at,yaw);
                E.Model.transform.localPosition=ModelPosition;E.Model.transform.localRotation=ModelTurn;E.Model.transform.localScale=ModelScale;
                for(int i=0;i<Bones.Length;i++) {Bones[i].localPosition=Positions[i];Bones[i].localRotation=Turns[i];Bones[i].localScale=Scales[i];}
                E.Play(clip,clock);Body.Apply(pose,E.Root.transform);
                float low=float.PositiveInfinity;
                for(int i=0;i<Skins.Length;i++) {Skins[i].BakeMesh(Baked,false);Baked.GetVertices(Vertices);low=Mathf.Min(low,Low(i,false,out _));}
                if(!float.IsInfinity(low))E.Root.transform.position+=Vector3.up*(at.y+Mathf.Max(0,pose.Hop)-low);
            }
        }
        private Child _lead,_taya,_bebang,_jun;
        private GameObject _shoe,_can;
        private TrailerPropGrip _grip;
        private float _floor;
        private Vector3 C=>new Vector3(Centre.x,_floor,Centre.z-6);
        private static readonly Mood Warm=new Mood
        {
            Sky=new Color(.55f,.62f,.78f),Equator=new Color(.68f,.65f,.58f),Ground=new Color(.36f,.34f,.30f),
            Fog=new Color(.93f,.80f,.64f),FogStart=45,FogEnd=220,
            Zenith=new Color(.28f,.45f,.67f),Horizon=new Color(.95f,.75f,.51f),
            Sun=new Color(1,.84f,.65f),SunIntensity=1.1f,SunElevation=18,KeepYaw=true,
            Brightness=1.01f,Saturation=1.01f,Lamps=1
        };
        protected override void Prepare()
        {
            _floor=Floor(Centre,Centre.y);
            _lead=Cast("Street Totoy","totoy");_taya=Cast("Street Tikboy","tikboy");
            _bebang=Cast("Street Bebang","bebang");_jun=Cast("Street Jun-Jun","jun_jun");
            var art=RosterBook.Load().SlipperArt(0);if(art?.Model==null)throw new InvalidOperationException("No payoff slipper");
            _shoe=Ghost(art.Model,"Street cinematic slipper");ToonSkin.ApplySlipper(_shoe,ToonSkin.PersonOutlineWidth);
            _grip=new TrailerPropGrip(_shoe);
            var lata=GameServices.Round.Lata;_can=Ghost(lata.gameObject,"Street cinematic can");
            _can.transform.position=C;
            foreach(var slipper in FindObjectsByType<Slipper>())ForceOff(slipper.GetComponentsInChildren<Renderer>(true));
            Hide(lata.GetComponentsInChildren<Renderer>(true));
            Prop(PrimitiveType.Cube,"Chalk finish line",C+new Vector3(0,.008f,7),Quaternion.identity,
                new Vector3(7.2f,.012f,.045f),new Color(.84f,.83f,.72f),false);
        }
        private Child Cast(string name,string id)
        {
            var art=RosterBook.Load().FindPersonArt(id);if(art?.Model==null)throw new InvalidOperationException("Missing child "+id);
            var root=Own(new GameObject(name));root.transform.SetParent(transform,false);
            var model=Instantiate(art.Model,root.transform,false);
            model.transform.localScale=Vector3.one*(CharacterVisual.PersonScale*.64f);
            model.transform.localRotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw,0);
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))Destroy(collider);
            ToonSkin.Apply(model,ToonSkin.PersonOutlineWidth,art.Palette!=null&&art.Palette.Length==16?art.Palette:null);
            var extra=new Extra {Root=root,Model=model,Clips=art.Clips,Scale=.64f};extra.Play("idle",0);
            var bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones).Where(b=>b!=null).Distinct().ToArray();
            var child=new Child {E=extra,Body=new TrailerBody(model),Bones=bones,Positions=bones.Select(b=>b.localPosition).ToArray(),
                Turns=bones.Select(b=>b.localRotation).ToArray(),Scales=bones.Select(b=>b.localScale).ToArray(),
                ModelPosition=model.transform.localPosition,ModelTurn=model.transform.localRotation,ModelScale=model.transform.localScale,
                Skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(false).Where(s=>s.enabled&&s.sharedMesh!=null).ToArray(),
                Baked=Own(new Mesh{name="Street grounding "+id})};child.Calibrate();return child;
        }
        protected override void Stage(float t)
        {
            for(int i=0;i<Seats;i++)ShowSeat(i,false);
            Tint(Warm,1);Beat="C15-home";
            float run=Smooth(t/2.1f);
            Vector3 lead=Vector3.Lerp(C+new Vector3(1.2f,0,4.7f),C+new Vector3(1.9f,0,8.3f),run);
            Vector3 taya=Vector3.Lerp(C+new Vector3(.5f,0,3.3f),C+new Vector3(1.15f,0,6.85f),Smooth(t/2.3f));
            var carry=new Pose{Lean=5,Nod=-2,Arm=new Vector3(.3f,-.65f,.45f),Fore=new Vector3(.15f,.3f,.9f)};
            if(t>2.2f) {carry.Lean=2;carry.Turn=-22*Smooth((t-2.2f)/1.1f);carry.Tilt=-3;}
            float leadYaw=t<2.2f?7:Mathf.LerpAngle(7,-35,Smooth((t-2.2f)/1.3f));
            lead.y=Floor(lead,_floor);taya.y=Floor(taya,_floor);
            _lead.Sample(lead,leadYaw,t<2.2f?"sprint":"idle",t<2.2f?4.4f+t*1.5f:t-2.2f,carry);
            var chase=new Pose{Lean=7,Nod=-2};
            if(t>1.8f) {chase.Lean=14*(1-Smooth((t-2.1f)/1));chase.Nod=3;chase.Turn=10;}
            _taya.Sample(taya,5,t<2.3f?"sprint":"idle",t<2.3f?4.2f+t*1.4f:t-2.3f,chase);
            if(t>1.75f&&t<2.25f)_taya.Body.ReachRight(lead+Vector3.up*.45f);
            var happy=Pose.Rest;
            happy=Pose.Mix(happy,new Pose{Lean=-5,Nod=-10,Arm=new Vector3(.3f,.9f,.15f),Fore=new Vector3(.15f,1,.05f),Off=Pose.OffDown,OffFore=Pose.OffDownFore},Smooth((t-1.9f)/.65f));
            Vector3 friend=C+new Vector3(3.7f,0,10.3f);friend.y=Floor(friend,_floor);
            _bebang.Sample(friend,220,"idle",t+.7f,happy);
            var laugh=Pose.Rest;float reaction=Smooth((t-2.35f)/.65f);laugh.Lean=-7*reaction;laugh.Tilt=5*reaction;
            laugh.Arm=new Vector3(.2f,-.6f,.5f);laugh.Fore=new Vector3(-.6f,.3f,.7f);
            friend=C+new Vector3(4.3f,0,8.7f);friend.y=Floor(friend,_floor);
            _jun.Sample(friend,240,"idle",t+1.8f,laugh);
            Vector3 hand=_lead.Body.Hand(out var fore);
            _grip.Pose(hand,fore,_lead.E.Root.transform.forward,out var shoePosition,out var shoeRotation);
            _shoe.transform.SetPositionAndRotation(shoePosition,shoeRotation);
            float finish=Smooth((t-2.2f)/2.8f);
            Vector3 offset=Vector3.Lerp(new Vector3(-4.2f,1.1f,-.5f),new Vector3(-2.7f,1.1f,2.1f),finish);
            Shoot(lead+offset,lead+Vector3.up*.70f,48);
            var lens=Camera.GetComponent<TrailerLens>();if(lens!=null){lens.Focus=lead+Vector3.up*.7f;lens.BlurPixels=5;}
        }
    }
}
