using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Settings;
using TumbangPreso.Visual;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class RafiBahaProbe
    {
        private readonly List<GameObject> _built=new List<GameObject>();
        private CharacterMotor _actor;private Carrier _carrier;private Slipper _shoe;
        private int _seat,_capture,_mip,_quality,_graphics;private bool _bots,_spectator;private INetProvider _provider;private CustomRules _rules;private bool _pinned;
        private RafiHeroKit Kit=>(RafiHeroKit)_actor.AbilitySystem.Kit;
        private AbilityContext Context=>new AbilityContext(_actor,_carrier,_actor.GetComponent<CombatVerbs>());
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_quality=QualitySettings.GetQualityLevel();_graphics=GraphicsProfiles.Current;_seat=GameLaunch.SoloSeat;_capture=Time.captureFramerate;_mip=QualitySettings.globalTextureMipmapLimit;QualitySettings.globalTextureMipmapLimit=2;_provider=NetAuthority.Provider;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();GameLaunch.SoloSeat=1;NetAuthority.Provider=new SoloProvider();Time.captureFramerate=50;
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _built)if(go!=null)Object.Destroy(go);_built.Clear();
            yield return PlayModeWorld.Reset();GameLaunch.SoloSeat=_seat;NetAuthority.Provider=_provider;Time.captureFramerate=_capture;QualitySettings.SetQualityLevel(_quality,true);GraphicsProfiles.Apply(_graphics);QualitySettings.globalTextureMipmapLimit=_mip;GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private GameObject Track(GameObject go){_built.Add(go);return go;}
        private IEnumerator Open()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.localScale=new Vector3(30,1,30);floor.transform.position=Vector3.down*.5f;
            var can=Track(new GameObject("Baha can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var go=Track(new GameObject("Backwash Rafi",typeof(CharacterController)));
            ConfigureCapsule(go.GetComponent<CharacterController>());
            _actor=go.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");_carrier=go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();go.AddComponent<HeroAbilitySystem>().BindHero("rafi");
            var cc=go.GetComponent<CharacterController>();go.transform.position=new Vector3(0,-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f,-8);
            GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();Physics.SyncTransforms();
            _shoe=Track(new GameObject("Backwash own slipper")).AddComponent<Slipper>();_shoe.OwnerSlot=1;_shoe.SeatOfOrigin=1;
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return new WaitForSeconds(.3f);
        }
        private static void ConfigureCapsule(CharacterController cc)
        {cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);cc.slopeLimit=45;cc.stepOffset=.3f;}
        private CharacterMotor Rival(Vector3 point)
        {
            var go=Track(new GameObject("Baha grounded rival",typeof(CharacterController)));
            ConfigureCapsule(go.GetComponent<CharacterController>());
            var actor=go.AddComponent<CharacterMotor>();actor.PlayerSlot=2;actor.Mode=GameMode.HeroStrike;actor.IsBot=false;
            go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();
            var cc=go.GetComponent<CharacterController>();point.y=-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f;
            go.transform.position=point;GameServices.Round.Register(actor);Physics.SyncTransforms();return actor;
        }
        private Slipper Loose(Vector3 point)
        {
            var shoe=Track(new GameObject("Baha loose slipper")).AddComponent<Slipper>();shoe.OwnerSlot=3;shoe.SeatOfOrigin=3;
            point.y=shoe.RestHeight;shoe.transform.position=point;return shoe;
        }
        private RafiWaterField Cast()
        {
            _actor.Intent.FaceAimPoint=true;_actor.Intent.AimPoint=new Vector3(0,.1f,6);
            var field=RafiWaterField.CastBaha(Context);Assert.IsNotNull(field);Track(field.gameObject);
            Assert.AreEqual(WorldEffectSnapshot.Kind.Baha,field.Capture().Type);
            Assert.Greater(Vector3.Dot(field.Capture().Forward,Vector3.forward),.99f);
            return field;
        }
        [UnityTest,Timeout(60000)] public IEnumerator WarnsBeforeOneGroundedNudgeAndCarriesOnlyLooseShoes()
        {
            yield return Open();var target=Rival(new Vector3(0,0,-4));var loose=Loose(new Vector3(1,0,-3));
            for(int i=0;i<6;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(target.IsGrounded);float start=target.transform.position.z,shoeStart=loose.transform.position.z;
            var field=Cast();yield return new WaitForSeconds(.7f);
            Assert.AreEqual(start,target.transform.position.z,.005f,"Warning must not push.");
            Assert.AreEqual(shoeStart,loose.transform.position.z,.005f);
            yield return new WaitForSeconds(2f);
            Assert.Greater(target.transform.position.z,start+.05f,"Grounded rival did not receive the real impact.");
            Assert.That(loose.transform.position.z-shoeStart,Is.InRange(2.5f,3.01f));
            Assert.AreEqual(SlipperState.Held,_shoe.State,"Held slipper must not be carried.");
            Assert.IsTrue(GameServices.Round.Lata.IsUpright);
            yield return new WaitForSeconds(field.Remaining+.1f);Assert.IsTrue(field==null||!field.gameObject.activeInHierarchy);
        }
        [UnityTest,Timeout(60000)] public IEnumerator ActualJumpAndNarrowCoverAvoidTheFront()
        {
            yield return Open();var target=Rival(new Vector3(0,0,-7));
            for(int i=0;i<6;i++)yield return new WaitForFixedUpdate();Assert.IsTrue(target.IsGrounded);
            var field=Cast();yield return new WaitForSeconds(.78f);
            target.Intent.Set(Verb.Jump,true);yield return new WaitForSeconds(.06f);target.Intent.Set(Verb.Jump,false);
            Assert.IsFalse(target.IsGrounded,"The counter must actually jump.");
            yield return new WaitForSeconds(.65f);Assert.AreEqual(-7,target.transform.position.z,.02f,"An airborne rival was pushed.");
            Object.Destroy(field.gameObject);yield return null;
            target.Teleport(new Vector3(.3f,target.transform.position.y,-4));
            var cover=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));cover.transform.position=new Vector3(.3f,.6f,-6);
            cover.transform.localScale=new Vector3(.1f,2,.2f);Physics.SyncTransforms();
            yield return new WaitForSeconds(.8f);Assert.IsTrue(target.IsGrounded);float start=target.transform.position.z;
            var blocked=Loose(new Vector3(.3f,0,-3));float shoeStart=blocked.transform.position.z;
            var next=Cast();yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(start,target.transform.position.z,.02f,"Cover between sampled lanes must still block a rival.");
            Assert.AreEqual(shoeStart,blocked.transform.position.z,.02f,"Cover must block loose-shoe carry.");
            GameServices.Round.EndRound();yield return null;yield return null;
            Assert.IsTrue(next==null||!next.gameObject.activeInHierarchy,"Round exit must retire the front.");
        }
        [UnityTest,Timeout(180000)] public IEnumerator ActualUltimateShowsWarningFrontAndRetirementOnTheCourt()
        {
            int low=System.Array.IndexOf(QualitySettings.names,"Low");if(low>=0)QualitySettings.SetQualityLevel(low,true);
            GraphicsProfiles.Apply(0);QualitySettings.globalTextureMipmapLimit=2;
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();Assert.IsTrue(GameServices.Round.RoundActive);
            foreach(var actor in GameServices.Round.Players)
            {actor.Intent.Clear();actor.Intent.Parked=true;actor.Teleport(new Vector3(7,.12f,7+actor.PlayerSlot));}
            var caster=GameServices.Round.PlayerAt(1);caster.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");
            caster.AbilitySystem.BindHero("rafi");var art=RosterBook.Load().FindPersonArt("rafi");
            caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            caster.Teleport(new Vector3(-3,.12f,-8));caster.transform.rotation=Quaternion.identity;caster.Intent.Parked=false;
            caster.AbilitySystem.Kit.AddUltimateCharge(100);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);rig.SetAimSource(AimSource.Movement);
            var witness=Track(new GameObject("Baha live action witness")).AddComponent<Camera>();
            witness.enabled=false;witness.fieldOfView=52;witness.nearClipPlane=.05f;witness.farClipPlane=400;witness.allowHDR=true;witness.cullingMask&=~(1<<5);
            witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            bool requested=false,accepted=false,warning=false,live=false;WorldEffectSnapshot.Field recorded=default;
            yield return ImprovementEvidenceProbe.Record(witness,"baha-live",7.5f,caster,time=>
            {
                caster.Intent.AimPoint=new Vector3(-3,.12f,3);caster.Intent.FaceAimPoint=true;
                bool press=!requested&&time>=.25f;
                caster.Intent.Set(Verb.Ultimate,press);
                if(press){requested=true;caster.Intent.BufferPress(Verb.Ultimate);}
                if(caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate)==HeroKit.CastOutcome.Cast)accepted=true;
                var field=RafiWaterField.Active.FirstOrDefault(f=>f!=null&&f.Capture().Type==WorldEffectSnapshot.Kind.Baha);
                if(field!=null)
                {
                    Assert.AreEqual(1,field.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetFloat("_UseVertexColour"));
                    recorded=field.Capture();float age=recorded.Duration-recorded.Remaining;
                    warning|=age<RafiRules.BahaWarning;live|=age>RafiRules.BahaWarning+.3f;
                }
            },witnessOffset:new Vector3(6,4,-4),witnessLookHeight:1f);
            caster.Intent.Set(Verb.Ultimate,false);
            var legacyMaterial=new Material(Shader.Find("TumbangPreso/RafiWater"));
            try{Assert.AreEqual(0,legacyMaterial.GetFloat("_UseVertexColour"),"Existing water materials must remain unchanged by default.");}
            finally{Object.Destroy(legacyMaterial);}
            Assert.IsTrue(accepted);Assert.IsTrue(warning);Assert.IsTrue(live);Assert.AreEqual(0,caster.AbilitySystem.Kit.UltimateCharge,.001f);
            Assert.IsFalse(RafiWaterField.Active.Any(f=>f!=null&&f.Capture().Type==WorldEffectSnapshot.Kind.Baha));
            var stage=Track(new GameObject("Baha render-only witness"));
            using(var view=new RecordedFieldView(stage.transform,recorded))
            {
                view.Sample(recorded,1.2f);view.Visible(true);
                Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>(true));
                Assert.IsEmpty(view.Root.GetComponentsInChildren<RafiWaterField>(true));
                Assert.IsFalse(RafiWaterField.Active.Any(f=>f!=null&&f.Capture().Type==WorldEffectSnapshot.Kind.Baha));
            }
        }
    }
}
