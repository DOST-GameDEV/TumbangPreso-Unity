using System;
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
            ("C09-return",20,25), ("C10-decision",25,30), ("C11-turn",30,34),
            ("C12-escape",34,38), ("C13-home-placeholder",38,44)
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
            public void Sample(Vector3 at,float yaw,string clip,float clock,Pose pose)
            {
                E.Show(true); E.Pose(at,yaw);
                E.Model.transform.localPosition=ModelPosition;
                E.Model.transform.localRotation=ModelTurn;
                E.Model.transform.localScale=ModelScale;
                for(int i=0;i<Bones.Length;i++)
                { Bones[i].localPosition=Positions[i]; Bones[i].localRotation=Turns[i]; Bones[i].localScale=Scales[i]; }
                E.Play(clip,clock);
                Body.Apply(pose,E.Root.transform);
            }
        }

        private Actor _rago,_dante,_amihan,_nemu;
        private GameObject _can,_shoe;
        private float _ground;
        private Quaternion _canRest;
        private Vector3 _canRestScale,_canPivot,_shoeScale,_release;
        private Vector3 C => new Vector3(Centre.x,_ground,Centre.z);
        private Vector3 RagoMark => C+new Vector3(0,0,4.7f);
        private Vector3 DanteMark => C+new Vector3(-1.05f,0,-.7f);
        private Vector3 ShoeMark => C+new Vector3(1.05f,.055f,-1.4f);
        private Vector3 FallenCan => C+new Vector3(-.2f,.18f,-.05f);

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
            _release=_rago.Body.Hand(out var pointing)+pointing*.02f;
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
            model.transform.localScale=Vector3.one*CharacterVisual.PersonScale;
            model.transform.localRotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw,0);
            foreach(var component in model.GetComponentsInChildren<MonoBehaviour>(true)) component.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach(var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
            ToonSkin.Apply(model,ToonSkin.PersonOutlineWidth,art.Palette!=null && art.Palette.Length==16 ? art.Palette : null);
            var extra=new Extra {Root=root,Model=model,Clips=art.Clips,Scale=1};
            extra.Play("idle",0);
            var bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones).Where(b=>b!=null).Distinct().ToArray();
            return new Actor { E=extra,Body=new TrailerBody(model),Bones=bones,
                Positions=bones.Select(b=>b.localPosition).ToArray(),Turns=bones.Select(b=>b.localRotation).ToArray(),
                Scales=bones.Select(b=>b.localScale).ToArray(),ModelPosition=model.transform.localPosition,
                ModelTurn=model.transform.localRotation,ModelScale=model.transform.localScale };
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
                var hand=_rago.Body.Hand(out var axis);
                Place(_shoe,hand,Quaternion.LookRotation(axis,Vector3.up)*Quaternion.Euler(0,90,15));
            }
            else if(t<Contact)
            {
                float u=(t-Release)/(Contact-Release);
                var end=C+_canPivot+Vector3.up*.26f;
                Place(_shoe,Vector3.Lerp(_release,end,u)+Vector3.up*(.28f*Mathf.Sin(u*Mathf.PI)),
                    Quaternion.Euler(20+u*650,45,u*140));
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
                    Quaternion.Slerp(Quaternion.Euler(100,25,40),Quaternion.Euler(0,35,0),land));
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

        private void Ending(float t)
        {
            // The final neighborhood scene is a separate street director, not this arena hold.
            // Frames 38-44 are excluded from the production edit until that scene is authored.
            var defender=Pose.Ready;
            Vector3 dante=C+new Vector3(.65f,0,-.15f),rago=RagoMark+new Vector3(.25f,0,-1.7f);
            Pose pose=Pose.Rest;
            string clip="idle";
            if(t>5 && t<10) { pose.Lean=8*Smooth((t-5)/5); pose.Turn=6; pose.Nod=5; }
            if(t>=10)
            {
                float k=Smooth((t-10)/3);
                rago=Vector3.Lerp(rago,ShoeMark+new Vector3(.12f,-.055f,.15f),k);
                rago.x+=.65f*Mathf.Sin(k*Mathf.PI);
                clip="sprint";
                dante+=new Vector3(-1.2f*Smooth((t-10)/.7f),0,1.1f*Smooth((t-10)/.7f));
            }
            if(t>=13 && t<14) { clip="pick-up"; pose.Lean=34; pose.Nod=-18; pose.Arm=new Vector3(.1f,-.9f,.4f); pose.Fore=new Vector3(.1f,-.9f,.3f); }
            if(t>=14)
            {
                float k=Smooth((t-14)/4);
                rago=Vector3.Lerp(ShoeMark+new Vector3(.12f,-.055f,.15f),C+new Vector3(1.9f,0,5.9f),k);
                clip="sprint"; pose=Pose.Rest;
                dante=Vector3.Lerp(C+new Vector3(-.55f,0,.95f),C+new Vector3(.9f,0,4.7f),Smooth((t-14)/4.25f));
            }
            _rago.Sample(rago,t>=14?7:180,clip,t*.9f,pose);
            _dante.Sample(dante,t>=14?7:0,t>=10?"sprint":"idle",t*.9f,defender);
            Place(_can,C+_canPivot,_canRest);
            if(t<13.65f) Place(_shoe,ShoeMark,Quaternion.Euler(0,35,0));
            else { var hand=_rago.Body.Hand(out var axis); Place(_shoe,hand,Quaternion.LookRotation(axis)*Quaternion.Euler(0,90,0)); }
            if(t<5) { Beat="C09-return"; Shoot(C+new Vector3(-5.9f,1.4f,3.8f),C+new Vector3(.25f,.9f,1.6f),49); }
            else if(t<10) { Beat="C10-decision"; Shoot(rago+new Vector3(-2.5f,1.8f,-2.8f),rago+Vector3.up*1.35f,38); }
            else if(t<14) { Beat="C11-turn"; Shoot(C+new Vector3(-5,1.25f,1),C+new Vector3(.5f,.8f,.2f),49); }
            else if(t<18) { Beat="C12-escape"; Shoot(C+new Vector3(-5.7f,1.3f,5),Vector3.Lerp(rago,dante,.35f)+Vector3.up,52); }
            else { Beat="C13-placeholder"; Shoot(C+new Vector3(-6,3.2f,9),rago+Vector3.up,48); }
        }

        private static void Place(GameObject prop,Vector3 at,Quaternion turn)
        { if(prop!=null) { prop.SetActive(true); prop.transform.SetPositionAndRotation(at,turn); } }
    }
}
