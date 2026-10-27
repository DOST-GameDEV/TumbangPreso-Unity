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
        public const float OpeningLength = 20f, Length = 44f, Release = 5.92f, Contact = 6.48f;
        public static readonly (string name, float from, float to)[] Shots =
        {
            ("C01-small-object",0,4), ("C02-commitment",4,8), ("C03-world",8,12),
            ("C04-defender",12,16), ("C05-problem",16,20),
            ("C06-return",20,25), ("C07-decision",25,30), ("C08-turn",30,34),
            ("C09-escape",34,38), ("C10-home-placeholder",38,44)
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
        private Vector3 FallenCan => C+new Vector3(-.55f,.18f,-.8f);

        protected override void Prepare()
        {
            _ground = ArenaStage.Instance != null ? ArenaStage.Instance.transform.position.y+ArenaStage.Instance.CanHeight : Floor(Centre,Centre.y);
            _rago=Cast("Trailer Rago","sean"); _dante=Cast("Trailer Dante","dante");
            _amihan=Cast("Trailer Amihan","amihan"); _nemu=Cast("Trailer Nemu","nemu");
            if(_rago==null || _dante==null) throw new InvalidOperationException("Trailer requires the actual Rago and Dante models.");
            var lata=GameServices.Round != null ? GameServices.Round.Lata : null;
            if(lata==null) throw new InvalidOperationException("No can to dress the cinematic set.");
            _can=Ghost(lata.gameObject,"Cinematic can");
            Hide(lata.GetComponentsInChildren<Renderer>(true));
            _canRest=_can.transform.rotation; _canRestScale=_can.transform.localScale;
            _canPivot=_can.transform.position-C;
            var art=RosterBook.Load().SlipperArt(0);
            if(art==null || art.Model==null) throw new InvalidOperationException("No authored slipper model.");
            _shoe=Ghost(art.Model,"Cinematic tsinelas");
            _shoeScale=_shoe.transform.localScale;
            ToonSkin.ApplySlipper(_shoe,ToonSkin.PersonOutlineWidth);
            // A trailer-only dressed level court, above the live stage; no runtime asset or collider is edited.
            Prop(PrimitiveType.Cylinder,"Cinematic competition deck",C+Vector3.down*.14f,Quaternion.identity,
                new Vector3(14.4f,.14f,14.4f),new Color(.38f,.46f,.50f),false);
            Prop(PrimitiveType.Cube,"Physical court exit paint",C+new Vector3(0,.007f,5.35f),Quaternion.identity,
                new Vector3(9.1f,.012f,.055f),new Color(.84f,.84f,.74f),false);
            // Two independent competitors, never teammates. Their attention shifts separately.
            _rago.Sample(RagoMark,180,"idle",0,Pose.Contact);
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
        }

        private void OtherPlayers(float t)
        {
            var watch=Pose.Rest; watch.Turn=9f; watch.Lean=2f;
            _amihan?.Sample(C+new Vector3(-3.8f,0,2.5f),150,"idle",t+.41f,watch);
            watch.Turn=-6f; watch.Lean=-1f;
            _nemu?.Sample(C+new Vector3(3.7f,0,1.5f),220,"idle",t+1.7f,watch);
        }

        private void Opening(float t)
        {
            var pose=ThrowPose(t);
            var at=RagoMark+new Vector3(.08f*Smooth((t-5.5f)/.5f),0,-.16f*Smooth((t-5.5f)/.5f));
            if(t>8) at=Vector3.Lerp(at,RagoMark+new Vector3(.25f,0,-1.7f),Smooth((t-9)/3));
            if(t>12) { pose=Pose.Rest; pose.Turn=-12*Smooth((t-14)/2); pose.Nod=6*Smooth((t-15)/2); pose.Lean=3; }
            _rago.Sample(at,180,t>9 && t<12?"walk":"idle",t>9?(t-9)*1.2f:t*.6f,pose);
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
                float land=Smooth(u/.9f);
                Place(_shoe,Vector3.Lerp(C+_canPivot+Vector3.up*.26f,ShoeMark,land)+Vector3.up*(.3f*Mathf.Sin(land*Mathf.PI)),
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
            return new Track().Key(0,Pose.Rest).Key(2.4f,Pose.Rest).Key(4.7f,load,Ease.Smooth)
                .Key(5.5f,load).Key(Release,commit,Ease.In).Key(6.22f,Pose.Follow,Ease.Out)
                .Key(6.8f,Pose.Follow).Key(8.1f,settle).Key(20,settle).At(t);
        }

        private void CanAndDefender(float t)
        {
            var reach=Pose.Rest;
            if(t<11.9f)
            {
                reach.Lean=4; reach.Nod=-2;
                _dante.Sample(DanteMark,0,"idle",t*.48f,reach);
            }
            else if(t<15.1f)
            {
                float k=Mathf.Clamp01((t-11.9f)/3.2f);
                var bend=Pose.Rest; bend.Lean=39; bend.Nod=-21; bend.Sink=.045f;
                bend.Arm=new Vector3(.16f,-.9f,.45f); bend.Fore=new Vector3(.13f,-.8f,.58f);
                reach=Pose.Mix(Pose.Rest,bend,Mathf.Sin(k*Mathf.PI));
                _dante.Sample(Vector3.Lerp(DanteMark,C+new Vector3(-.5f,0,-.7f),Mathf.Sin(k*Mathf.PI)),0,
                    "pick-up",k*.9f,reach);
            }
            else
            {
                reach.Lean=5; reach.Nod=-5; reach.Turn=8;
                reach.Off=new Vector3(-.3f,-.8f,.2f); reach.OffFore=new Vector3(-.1f,-.95f,.18f);
                _dante.Sample(Vector3.Lerp(DanteMark,C+new Vector3(.65f,0,-.15f),Smooth((t-15.1f)/1.8f)),0,
                    t<17?"walk":"idle",t-15.1f,reach);
            }
            if(t<Contact) Place(_can,C+_canPivot,_canRest);
            else if(t<11.9f)
            {
                float u=t-Contact;
                float k=Smooth(u/.85f);
                var p=Vector3.Lerp(C+_canPivot,FallenCan,k)+Vector3.up*(.48f*Mathf.Abs(Mathf.Sin(Mathf.Min(u/.85f,1)*Mathf.PI)));
                Place(_can,p,Quaternion.Euler(84*k,170*k,32*k)*_canRest);
            }
            else if(t<13.35f) Place(_can,FallenCan,Quaternion.Euler(84,170,32)*_canRest);
            else if(t<14.5f)
            {
                float k=Smooth((t-13.35f)/1.15f);
                Vector3 hand=_dante.Body.Hand(out _);
                Place(_can,Vector3.Lerp(FallenCan,hand,k),Quaternion.Slerp(Quaternion.Euler(84,170,32)*_canRest,_canRest,k));
            }
            else if(t<15.1f)
            {
                float k=Smooth((t-14.5f)/.6f);
                Vector3 hand=_dante.Body.Hand(out _);
                Place(_can,Vector3.Lerp(hand,C+_canPivot,k),_canRest);
            }
            else Place(_can,C+_canPivot,_canRest);
        }

        private void OpeningCamera(float t)
        {
            if(t<4)
            {
                Beat=Shots[0].name;
                Shoot(C+new Vector3(.34f+.12f*Smooth(t/4),.46f,-1.65f-.14f*Smooth(t/4)),
                    C+new Vector3(0,.43f,1.15f),35);
            }
            else if(t<8)
            {
                Beat=Shots[1].name;
                // Side-on three-quarter: the wind-up, shoe path and can share the frame.
                float k=Smooth((t-4)/4);
                Shoot(C+Vector3.Lerp(new Vector3(-7.8f,1.45f,3),new Vector3(-7.1f,1.25f,2.6f),k),
                    C+new Vector3(0,.88f,1.9f),43);
            }
            else if(t<12)
            {
                Beat=Shots[2].name;
                float k=Smooth((t-8)/4);
                Shoot(C+Vector3.Lerp(new Vector3(-5.8f,2,5.3f),new Vector3(-12.5f,10.5f,15.8f),k),
                    C+Vector3.Lerp(new Vector3(0,1,2),new Vector3(0,1.2f,.4f),k),48);
            }
            else if(t<16)
            {
                Beat=Shots[3].name;
                float k=Smooth((t-12)/4);
                Shoot(C+Vector3.Lerp(new Vector3(3.4f,1.2f,2.5f),new Vector3(2.9f,1.1f,2.15f),k),
                    C+new Vector3(-.15f,.72f,-.25f),41);
            }
            else
            {
                Beat=Shots[4].name;
                float k=Smooth((t-16)/4);
                Shoot(C+Vector3.Lerp(new Vector3(-6.4f,1.5f,4.2f),new Vector3(-5.9f,1.4f,3.8f),k),
                    C+new Vector3(.25f,.9f,1.6f),49);
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
            if(t<5) { Beat=Shots[5].name; Shoot(C+new Vector3(-5.9f,1.4f,3.8f),C+new Vector3(.25f,.9f,1.6f),49); }
            else if(t<10) { Beat=Shots[6].name; Shoot(rago+new Vector3(-2.5f,1.8f,-2.8f),rago+Vector3.up*1.35f,38); }
            else if(t<14) { Beat=Shots[7].name; Shoot(C+new Vector3(-5,1.25f,1),C+new Vector3(.5f,.8f,.2f),49); }
            else if(t<18) { Beat=Shots[8].name; Shoot(C+new Vector3(-5.7f,1.3f,5),Vector3.Lerp(rago,dante,.35f)+Vector3.up,52); }
            else { Beat=Shots[9].name; Shoot(C+new Vector3(-6,3.2f,9),rago+Vector3.up,48); }
        }

        private static void Place(GameObject prop,Vector3 at,Quaternion turn)
        { if(prop!=null) { prop.SetActive(true); prop.transform.SetPositionAndRotation(at,turn); } }
    }
}
