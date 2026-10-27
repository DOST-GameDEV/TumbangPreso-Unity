using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>New trailer-only dramatic scene. No map/ultimate film or native gameplay footage is reused.</summary>
    public sealed class TrailerOpeningFilm : MapFilm
    {
        public const float OpeningLength = 20f, Length = 44f, Release = 3.95f, Contact = 4.45f;
        public static readonly (string name, float from, float to)[] Shots =
        {
            ("C01-small-object",0,2.2f), ("C02-commitment",2.2f,4.2f), ("C03-contact",4.2f,5.8f), ("C04-world",5.8f,9.6f),
            ("C05-restore",9.6f,12.8f), ("C06-defender",12.8f,15.8f), ("C07-geography",15.8f,18.1f), ("C08-decision",18.1f,20),
            ("C09-return",20,22.5f), ("C10-decision",22.5f,25), ("C11-turn",25,27.7f),
            ("C12-grip",27.7f,29), ("C13-escape",29,34), ("C14-release",34,38), ("C15-home-placeholder",38,44)
        };
        protected override string Region => "TRAILER";
        protected override Color Accent => new Color(.95f,.68f,.25f);
        protected override string Score => null;
        protected override float CloseAt(bool full) => 999f;
        protected override bool OwnsLight => true;

        private sealed class Actor
        {
            public Extra E;
            public TrailerBody Body;
            public Transform[] Bones;
            public Vector3[] Positions, Scales;
            public Quaternion[] Turns;
            public Vector3 ModelPosition, ModelScale;
            public Quaternion ModelTurn;
            public SkinnedMeshRenderer[] Skins;
            public bool[] CarriesScale;
            public Mesh Baked;
            public readonly List<Vector3> Vertices=new List<Vector3>();
            private float Low(SkinnedMeshRenderer skin,bool scaled,out float high)
            {
                Matrix4x4 space=scaled?Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one):skin.transform.localToWorldMatrix;
                float low=float.PositiveInfinity;high=float.NegativeInfinity;
                foreach(var vertex in Vertices) {float y=space.MultiplyPoint3x4(vertex).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                return low;
            }
            public void CalibrateGround()
            {
                CarriesScale=new bool[Skins.Length];
                for(int i=0;i<Skins.Length;i++)
                {
                    Skins[i].BakeMesh(Baked);Baked.GetVertices(Vertices);
                    float a=Low(Skins[i],true,out float aHigh),b=Low(Skins[i],false,out float bHigh);
                    float height=Skins[i].bounds.size.y;
                    CarriesScale[i]=Mathf.Abs(aHigh-a-height)<=Mathf.Abs(bHigh-b-height);
                }
            }
            public void Sample(Vector3 at,float yaw,string clip,float clock,Pose pose)
            {
                E.Show(true); E.Pose(at,yaw);
                E.Model.transform.localPosition=ModelPosition;
                E.Model.transform.localRotation=ModelTurn;
                E.Model.transform.localScale=ModelScale;
                for(int i=0;i<Bones.Length;i++)
                { Bones[i].localPosition=Positions[i]; Bones[i].localRotation=Turns[i]; Bones[i].localScale=Scales[i]; }
                E.Play(clip,clock);
                if(clip=="walk"||clip=="sprint")
                {
                    if(pose.Arm==Pose.Down) {pose.Arm=Vector3.zero;pose.Fore=Vector3.zero;}
                    if(pose.Off==Pose.OffDown) {pose.Off=Vector3.zero;pose.OffFore=Vector3.zero;}
                }
                Body.Apply(pose,E.Root.transform);
                float low=float.PositiveInfinity;
                for(int i=0;i<Skins.Length;i++)
                {
                    Skins[i].BakeMesh(Baked);Baked.GetVertices(Vertices);
                    low=Mathf.Min(low,Low(Skins[i],CarriesScale[i],out _));
                }
                if(!float.IsInfinity(low)&&!float.IsNaN(low))
                    E.Root.transform.position+=Vector3.up*(at.y+Mathf.Max(0,pose.Hop)-low);
            }
        }

        private Actor _rago,_dante,_amihan,_nemu;
        private GameObject _can,_shoe;
        private float _ground;
        private Quaternion _canRest;
        private Quaternion _releaseTurn;
        private Vector3 _canRestScale,_canPivot,_shoeScale,_release;
        private float _fallenHeight=.18f,_shoeHeight=.055f;
        private Vector3 C => new Vector3(Centre.x,_ground,Centre.z);
        private Vector3 RagoMark => C+new Vector3(0,0,4.7f);
        private Vector3 DanteMark => C+new Vector3(-1.05f,0,-.7f);
        private Vector3 ShoeMark => C+new Vector3(1.05f,_shoeHeight,-1.4f);
        private Vector3 FallenCan => C+new Vector3(-.2f,_fallenHeight,-.05f);

        protected override void Prepare()
        {
            _ground = (ArenaStage.Instance != null ? ArenaStage.Instance.transform.position.y+ArenaStage.Instance.CanHeight : Floor(Centre,Centre.y))+.045f;
            _rago=Cast("Trailer Rago","sean"); _dante=Cast("Trailer Dante","dante");
            _amihan=Cast("Trailer Amihan","amihan"); _nemu=Cast("Trailer Nemu","nemu");
            if(_rago==null || _dante==null) throw new InvalidOperationException("Trailer requires the actual Rago and Dante models.");
            var lata=GameServices.Round != null ? GameServices.Round.Lata : null;
            if(lata==null) throw new InvalidOperationException("No can to dress the cinematic set.");
            _can=Ghost(lata.gameObject,"Cinematic can");
            Hide(lata.GetComponentsInChildren<Renderer>(true));
            _canRest=_can.transform.rotation; _canRestScale=_can.transform.localScale;
            _canPivot=_can.transform.position-C;
            _canPivot.y=0;
            var art=RosterBook.Load().SlipperArt(0);
            if(art==null || art.Model==null) throw new InvalidOperationException("No authored slipper model.");
            _shoe=Ghost(art.Model,"Cinematic tsinelas");
            _shoeScale=_shoe.transform.localScale;
            ToonSkin.ApplySlipper(_shoe,ToonSkin.PersonOutlineWidth);
            Place(_can,C,_canRest);
            _canPivot.y=C.y-Lowest(_can)+.003f;
            Place(_can,C+new Vector3(0,.18f,0),Quaternion.Euler(84,170,32)*_canRest);
            _fallenHeight=.18f+C.y-Lowest(_can)+.003f;
            Place(_shoe,C,Quaternion.Euler(0,35,0));
            _shoeHeight=C.y-Lowest(_shoe)+.003f;
            // A trailer-only dressed level court, above the live stage; no runtime asset or collider is edited.
            Prop(PrimitiveType.Cylinder,"Cinematic competition deck",C+Vector3.down*.14f,Quaternion.identity,
                new Vector3(14.4f,.14f,14.4f),new Color(.26f,.29f,.32f),false);
            Prop(PrimitiveType.Cube,"Physical court exit paint",C+new Vector3(0,.007f,5.35f),Quaternion.identity,
                new Vector3(9.1f,.012f,.055f),new Color(.84f,.84f,.74f),false);
            foreach(float side in new[]{-1f,1f})
                Prop(PrimitiveType.Cube,"Physical side court paint",C+new Vector3(side*4.55f,.007f,0),Quaternion.identity,
                    new Vector3(.045f,.012f,10.7f),new Color(.59f,.65f,.65f),false);
            // Two independent competitors, never teammates. Their attention shifts separately.
            _rago.Sample(RagoMark+new Vector3(.08f,0,-.16f),180,"idle",Release*.6f,ThrowPose(Release));
            _release=HeldShoe(out _releaseTurn);
            Debug.Log($"[TrailerOpening] ground={_ground:F3} centre={Centre} canPivot={_canPivot} release={_release} shoeScale={_shoeScale}");
            foreach(var a in new[]{_rago,_dante})
                Debug.Log("[TrailerOpening] "+a.E.Root.name+" clips="+string.Join(",",a.E.Clips.Where(x=>x!=null).Select(x=>x.name)));
        }

        private Actor Cast(string name,string id)
        {
            // MapFilm.Person skips a face already present on a live seat. A separately directed trailer
            // needs exact casting, so instantiate the roster art without that ambient-extra substitution.
            var art=RosterBook.Load()?.FindPersonArt(id);
            if(art?.Model==null) return null;
            var root=Own(new GameObject(name)); root.transform.SetParent(transform,false);
            var model=Instantiate(art.Model,root.transform,false);
            model.transform.localScale=Vector3.one*(CharacterVisual.PersonScale*CharacterVisual.BodyScaleFor(art.Model.name));
            model.transform.localRotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw,0);
            foreach(var component in model.GetComponentsInChildren<MonoBehaviour>(true)) component.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach(var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
            ToonSkin.Apply(model,ToonSkin.PersonOutlineWidth,art.Palette!=null && art.Palette.Length==16 ? art.Palette : null);
            var extra=new Extra {Root=root,Model=model,Clips=art.Clips,Scale=1};
            extra.Play("idle",0);
            var bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones).Where(b=>b!=null).Distinct().ToArray();
            var actor=new Actor { E=extra,Body=new TrailerBody(model),Bones=bones,
                Positions=bones.Select(b=>b.localPosition).ToArray(),Turns=bones.Select(b=>b.localRotation).ToArray(),
                Scales=bones.Select(b=>b.localScale).ToArray(),ModelPosition=model.transform.localPosition,
                ModelTurn=model.transform.localRotation,ModelScale=model.transform.localScale,
                Skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(false).Where(s=>s.enabled&&s.sharedMesh!=null).ToArray(),
                Baked=Own(new Mesh {name="Trailer grounding "+id}) };
            actor.CalibrateGround();
            return actor;
        }

        protected override void Stage(float age)
        {
            for(int i=0;i<Seats;i++) ShowSeat(i,false);
            var mood=Authored; mood.Brightness=1.03f; mood.Saturation=1.02f; Tint(mood,1);
            OtherPlayers(age);
            if(age<20) Opening(age); else Ending(age-20);
            var lens=Camera.GetComponent<TrailerLens>();
            if(lens!=null)
            {
                lens.BlurPixels=0;
                if(age<2.2f || age>=4.2f && age<5.8f) {lens.Focus=C+Vector3.up*.28f;lens.BlurPixels=10;}
                else if(age<4.2f || age>=18.1f && age<20) {lens.Focus=_rago.E.Root.transform.position+Vector3.up*1.2f;lens.BlurPixels=9;}
                else if(age>=9.6f && age<15.8f) {lens.Focus=_dante.E.Root.transform.position+Vector3.up*.8f;lens.BlurPixels=7;}
                else if(age>=20 && age<38)
                {
                    lens.Focus=_rago.E.Root.transform.position+Vector3.up; lens.BlurPixels=7;
                    if(age>=27.7f&&age<29) {lens.Focus=_shoe.transform.position+Vector3.up*.08f;lens.BlurPixels=9;}
                }
            }
        }

        private void OtherPlayers(float t)
        {
            var watch=Pose.Rest; watch.Turn=9f; watch.Lean=2f;
            _amihan?.Sample(C+new Vector3(-5.2f,0,-3.3f),25,"idle",t+.41f,watch);
            watch.Turn=-6f; watch.Lean=-1f;
            _nemu?.Sample(C+new Vector3(5.1f,0,-2.7f),330,"idle",t+1.7f,watch);
        }

        private void Opening(float t)
        {
            var pose=ThrowPose(t);
            var at=RagoMark+new Vector3(.08f*Smooth((t-3.5f)/.45f),0,-.16f*Smooth((t-3.5f)/.45f));
            if(t>5.8f) at=Vector3.Lerp(at,RagoMark+new Vector3(.25f,0,-1.7f),Smooth((t-6.4f)/2.2f));
            if(t>9.5f) { pose=Pose.Rest; pose.Turn=-12*Smooth((t-12.6f)/1.5f); pose.Nod=6*Smooth((t-13.5f)/2); pose.Lean=3; }
            _rago.Sample(at,180,t>6.4f && t<8.6f?"walk":"idle",t>6.4f?(t-6.4f)*1.2f:t*.6f,pose);
            if(t<Release)
            {
                Place(_shoe,HeldShoe(out var turn),turn);
            }
            else if(t<Contact)
            {
                float u=(t-Release)/(Contact-Release);
                var end=C+_canPivot+Vector3.up*.26f;
                Place(_shoe,Vector3.Lerp(_release,end,u)+Vector3.up*(.28f*Mathf.Sin(u*Mathf.PI)),
                    Quaternion.AngleAxis(u*650,Vector3.right)*_releaseTurn);
            }
            else
            {
                float u=t-Contact;
                float land=Mathf.Clamp01(u/.4f);
                Vector3 origin=C+_canPivot+Vector3.up*.26f;
                Vector3 velocity=(ShoeMark-origin)/.4f+Vector3.up*(4.9f*.4f);
                float flight=Mathf.Min(u,.4f);
                Vector3 position=origin+velocity*flight+Vector3.down*(4.9f*flight*flight);
                if(u>.4f) position.y+=.018f*Mathf.Exp(-(u-.4f)*14)*Mathf.Abs(Mathf.Sin((u-.4f)*28));
                Place(_shoe,position,
                    Quaternion.Slerp(Quaternion.AngleAxis(650,Vector3.right)*_releaseTurn,Quaternion.Euler(0,35,0),land));
            }
            CanAndDefender(t);
            OpeningCamera(t);
        }

        private Pose ThrowPose(float t)
        {
            var load=Pose.Charge; load.Nod=-2; load.Twist=36; load.Sink=.015f;
            var commit=Pose.Contact; commit.Lean=12; commit.Twist=-20;
            var settle=Pose.Rest; settle.Turn=-6;
            return new Track().Key(0,Pose.Rest).Key(.7f,Pose.Rest).Key(3f,load,Ease.Smooth)
                .Key(3.5f,load).Key(Release,commit,Ease.In).Key(4.2f,Pose.Follow,Ease.Out)
                .Key(4.7f,Pose.Follow).Key(5.6f,settle).Key(20,settle).At(t);
        }

        private void CanAndDefender(float t)
        {
            var reach=Pose.Rest;
            if(t<9.6f)
            {
                reach.Lean=4; reach.Nod=-2;
                _dante.Sample(DanteMark,0,"idle",t*.48f,reach);
            }
            else if(t<12.8f)
            {
                float k=Mathf.Clamp01((t-9.6f)/3.2f);
                var bend=Pose.Rest; bend.Lean=7; bend.Nod=-3; bend.Sink=0;
                bend.Arm=new Vector3(.16f,-.9f,.45f); bend.Fore=new Vector3(.13f,-.8f,.58f);
                reach=Pose.Mix(Pose.Rest,bend,Mathf.Sin(k*Mathf.PI));
                _dante.Sample(Vector3.Lerp(DanteMark,C+new Vector3(-.55f,0,-.75f),Mathf.Sin(k*Mathf.PI)),0,
                    "pick-up",k*.9f,reach);
            }
            else
            {
                reach.Lean=5; reach.Nod=-5; reach.Turn=8;
                reach.Off=new Vector3(-.3f,-.8f,.2f); reach.OffFore=new Vector3(-.1f,-.95f,.18f);
                _dante.Sample(Vector3.Lerp(DanteMark,C+new Vector3(.65f,0,-.15f),Smooth((t-12.8f)/1.8f)),0,
                    t<14.6f?"walk":"idle",t-12.8f,reach);
            }
            if(t<Contact) Place(_can,C+_canPivot,_canRest);
            else if(t<9.6f)
            {
                float u=t-Contact;
                float k=Mathf.Clamp01(u/.42f);
                float flight=Mathf.Min(u,.42f);
                var velocity=(FallenCan-(C+_canPivot))/.42f+Vector3.up*(4.9f*.42f);
                var p=C+_canPivot+velocity*flight+Vector3.down*(4.9f*flight*flight);
                if(u>.42f) p.y+=.035f*Mathf.Exp(-(u-.42f)*12)*Mathf.Abs(Mathf.Sin((u-.42f)*26));
                Place(_can,p,Quaternion.Euler(84*k,170*k,32*k)*_canRest);
            }
            else if(t<10.95f) Place(_can,FallenCan,Quaternion.Euler(84,170,32)*_canRest);
            else if(t<12.2f)
            {
                float k=Smooth((t-10.95f)/1.25f);
                Vector3 target=Vector3.Lerp(FallenCan+Vector3.up*.14f,C+new Vector3(-.2f,.45f,-.05f),k);
                _dante.Body.ReachRight(target);
                Vector3 hand=_dante.Body.Hand(out _);
                Place(_can,Vector3.Lerp(FallenCan,hand-Vector3.up*.14f,Mathf.Clamp01(k*8)),Quaternion.Slerp(Quaternion.Euler(84,170,32)*_canRest,_canRest,k));
            }
            else if(t<12.8f)
            {
                float k=Smooth((t-12.2f)/.6f);
                _dante.Body.ReachRight(Vector3.Lerp(C+new Vector3(-.2f,.45f,-.05f),C+_canPivot+Vector3.up*.2f,k));
                Vector3 hand=_dante.Body.Hand(out _);
                Place(_can,Vector3.Lerp(hand-Vector3.up*.14f,C+_canPivot,k),_canRest);
            }
            else Place(_can,C+_canPivot,_canRest);
        }

        private void OpeningCamera(float t)
        {
            if(t<2.2f)
            {
                Beat=Shots[0].name;
                Shoot(C+new Vector3(.34f+.12f*Smooth(t/2.2f),.46f,-1.65f-.14f*Smooth(t/2.2f)),
                    C+new Vector3(0,.43f,1.15f),35);
            }
            else if(t<4.2f)
            {
                Beat=Shots[1].name;
                float k=Smooth((t-2.2f)/2);
                Shoot(RagoMark+Vector3.Lerp(new Vector3(-3.25f,1.5f,-2.5f),new Vector3(-2.95f,1.42f,-2.35f),k),
                    RagoMark+new Vector3(0,1.05f,-.1f),40);
            }
            else if(t<5.8f)
            {
                Beat="C03-contact";
                float k=Smooth((t-4.2f)/1.6f);
                Shoot(C+new Vector3(.8f,.42f,-1.85f-.16f*k),C+new Vector3(-.12f,.30f,-.12f),39);
            }
            else if(t<9.6f)
            {
                Beat="C04-world";
                float k=Smooth((t-5.8f)/3.8f);
                Shoot(C+Vector3.Lerp(new Vector3(-5.8f,2,5.3f),new Vector3(-12.5f,10.5f,15.8f),k),
                    C+Vector3.Lerp(new Vector3(0,1,2),new Vector3(0,1.2f,.4f),k),48);
            }
            else if(t<12.8f)
            {
                Beat="C05-restore";
                float k=Smooth((t-9.6f)/3.2f);
                Shoot(C+Vector3.Lerp(new Vector3(3.4f,1.2f,2.5f),new Vector3(2.9f,1.1f,2.15f),k),
                    C+new Vector3(-.15f,.72f,-.25f),41);
            }
            else if(t<15.8f)
            {
                Beat="C06-defender";
                Shoot(C+new Vector3(2.7f,.6f,1.9f),C+new Vector3(.3f,.72f,-.3f),43);
            }
            else if(t<18.1f)
            {
                Beat="C07-geography";
                float k=Smooth((t-15.8f)/2.3f);
                Shoot(C+Vector3.Lerp(new Vector3(-6.4f,1.5f,4.2f),new Vector3(-5.9f,1.4f,3.8f),k),
                    C+new Vector3(.25f,.9f,1.6f),49);
            }
            else
            {
                Beat="C08-decision";
                Vector3 at=RagoMark+new Vector3(.25f,0,-1.7f);
                Shoot(at+new Vector3(-2.05f,1.5f,-2.2f),at+new Vector3(0,1.15f,0),34);
            }
        }

        private void Ending(float time)
        {
            float t=time<5?time*2:time<9?time+5:time<14?14+(time-9)*.8f:18;
            Vector3 start=RagoMark+new Vector3(.25f,0,-1.7f);
            Vector3 fake=start+new Vector3(-.45f,0,-.4f);
            Vector3 pickup=ShoeMark+new Vector3(0,-_shoeHeight,.7f);
            Vector3 rago=start,dante=C+new Vector3(.65f,0,-.15f);
            Pose pose=Pose.Rest,guard=Pose.Ready;
            string clip="idle",guardClip="idle";
            float clock=t*.7f,guardClock=t*.7f;
            if(t>2) {pose.Turn=-12*Smooth((t-2)/2);pose.Nod=5;}
            if(t>=5 && t<10)
            {
                rago=Vector3.Lerp(start,fake,Smooth((t-7.6f)/1.6f));
                pose.Lean=8*Smooth((t-5)/3);pose.Twist=-6;pose.Nod=4;
            }
            if(t>=10 && t<13)
            {
                float k=Smooth((t-10)/3);
                rago=Vector3.Lerp(fake,pickup,k)+Vector3.right*(.65f*Mathf.Sin(k*Mathf.PI));
                clip="sprint";clock=(t-10)*1.05f;pose=Pose.Rest;
            }
            if(t>=13 && t<14)
            {
                rago=pickup;clip="pick-up";clock=(t-13)*.95f;
                pose=new Pose {Lean=5,Nod=-3};
            }
            if(t>=14)
            {
                float k=Smooth((t-14)/4);
                rago=Vector3.Lerp(pickup,C+new Vector3(1.9f,0,5.9f),k);
                clip=t<18?"sprint":"idle";clock=t<18?(t-14)*1.1f:t-18;
                pose=new Pose {Lean=6,Nod=-3,Arm=new Vector3(.35f,-.6f,.55f),Fore=new Vector3(.18f,.35f,.9f)};
            }
            if(t>=10 && t<14)
            {
                dante=Vector3.Lerp(C+new Vector3(.65f,0,-.15f),C+new Vector3(-.55f,0,.95f),Smooth((t-10)/.7f));
                dante+=Vector3.back*(.45f*Smooth((t-11.2f)/2.2f));
                guard=t<10.8f?Pose.Contact:Pose.Rest;guard.Lean=10;
                guardClip=t<10.8f?"attack-melee-right":"idle";guardClock=t-10;
            }
            if(t>=14)
            {
                var mid=C+new Vector3(.1f,0,-1.8f);
                if(t<15.2f) dante=Vector3.Lerp(C+new Vector3(-.55f,0,.5f),mid,Smooth((t-14)/1.2f));
                else
                {
                    float lag=Mathf.Lerp(2.8f,1.35f,Smooth((t-15.2f)/2.8f));
                    Vector3 following=rago+new Vector3(-.55f,0,-lag);
                    dante=Vector3.Lerp(mid,following,Smooth((t-15.2f)/.5f));
                }
                guard=new Pose {Lean=8,Nod=-3};
                guardClip=t<17.6f?"sprint":"attack-melee-right";
                guardClock=t<17.6f?(t-14)*1.1f:(t-17.6f)*1.4f;
            }
            if(time>=14)
            {
                float reaction=time-14;
                var carry=new Pose {Lean=3,Nod=3,Arm=new Vector3(.35f,-.6f,.55f),Fore=new Vector3(.18f,.35f,.9f)};
                var lifted=carry;lifted.Arm=new Vector3(.25f,.18f,.93f);lifted.Fore=new Vector3(.12f,.78f,.6f);lifted.Turn=9;
                pose=Pose.Mix(carry,lifted,Smooth(reaction/.75f));
                guard=Pose.Mix(Pose.Contact,Pose.Rest,Smooth(reaction/.45f));guard.Lean=-2;guard.Nod=3*Mathf.Sin(reaction*2)*Mathf.Exp(-reaction);
                clip=guardClip="idle";clock=guardClock=reaction;
            }
            float yaw=t<13.6f?180:Mathf.LerpAngle(180,7,Smooth((t-13.6f)/.4f));
            float guardYaw=t<10?0:Mathf.Atan2(rago.x-dante.x,rago.z-dante.z)*Mathf.Rad2Deg;
            _rago.Sample(rago,yaw,clip,clock,pose);
            _dante.Sample(dante,guardYaw,guardClip,guardClock,guard);
            if(t>=13 && t<13.65f) _rago.Body.ReachRight(ShoeMark+Vector3.up*.18f);
            if(time<14 && t>=17.72f) _dante.Body.ReachRight(rago+Vector3.up*.75f);
            Place(_can,C+_canPivot,_canRest);
            if(t<13.65f) Place(_shoe,ShoeMark,Quaternion.Euler(0,35,0));
            else
            {
                Vector3 hand=HeldShoe(out var rotation);
                float lift=Smooth((t-13.65f)/.35f);
                Place(_shoe,Vector3.Lerp(ShoeMark,hand,lift),Quaternion.Slerp(Quaternion.Euler(0,35,0),rotation,lift));
            }
            if(time>=14) {Beat="C14-release";Shoot(C+new Vector3(-3.8f,1.65f,8.6f),Vector3.Lerp(rago,dante,.2f)+Vector3.up,42);}
            else if(t<5) {Beat="C09-return";Shoot(C+new Vector3(-5.9f,1.4f,3.8f),C+new Vector3(.25f,.9f,1.6f),49);}
            else if(t<10) {Beat="C10-decision";Shoot(C+new Vector3(-4.2f,1.4f,2.2f),C+new Vector3(.25f,1,1.5f),45);}
            else if(t<12.7f) {Beat="C11-turn";Shoot(C+new Vector3(-4.8f,1.25f,1.1f),C+new Vector3(.5f,.8f,.2f),49);}
            else if(t<14) {Beat="C12-grip";Shoot(ShoeMark+new Vector3(-1.6f,.65f,-1.8f),ShoeMark+new Vector3(0,.32f,.15f),44);}
            else if(t<18) {Beat="C13-escape";Shoot(C+new Vector3(-5.7f,1.3f,5),Vector3.Lerp(rago,dante,.35f)+Vector3.up*.9f,52);}
            else {Beat="C13-placeholder";Shoot(C+new Vector3(-6,3.2f,9),rago+Vector3.up,48);}
        }

        private static void Place(GameObject prop,Vector3 at,Quaternion turn)
        { if(prop!=null) { prop.SetActive(true); prop.transform.SetPositionAndRotation(at,turn); } }
        private static float Lowest(GameObject prop)
        {
            float low=float.PositiveInfinity;
            foreach(var renderer in prop.GetComponentsInChildren<Renderer>(false)) if(renderer.enabled) low=Mathf.Min(low,renderer.bounds.min.y);
            return low;
        }
        private Vector3 HeldShoe(out Quaternion turn)
        {
            Vector3 hand=_rago.Body.Hand(out var fore);
            Vector3 toe=(Vector3.up*.92f+fore*.35f).normalized;
            turn=Quaternion.LookRotation(toe,-_rago.E.Root.transform.forward);
            // Heel remains by the glove, with the toe beyond the fist silhouette.
            return hand+fore*.13f+_rago.E.Root.transform.right*.07f+Vector3.up*.12f;
        }
    }
}
