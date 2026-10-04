using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
namespace TumbangPreso.PlayTests
{
    public class DanteBoulderMotionTests
    {
        private int _quality; private Color _ambient;
        private readonly List<GameObject> _objects=new List<GameObject>();
        private CharacterMotor _actor; private Carrier _carrier; private Slipper _shoe;
        private INetProvider _provider; private int _seat; private CustomRules _rules; private bool _pinned;
        private DanteHeroKit Kit => (DanteHeroKit)_actor.AbilitySystem.Kit;
        private AbilityContext Context => new AbilityContext(_actor,_carrier,_actor.GetComponent<CombatVerbs>());
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        [UnitySetUp] public IEnumerator Before()
        {
            _quality=QualitySettings.GetQualityLevel();_ambient=RenderSettings.ambientLight;
            int low=System.Array.IndexOf(QualitySettings.names,"Low");if(low>=0)QualitySettings.SetQualityLevel(low,true);
            _provider=NetAuthority.Provider;_seat=GameLaunch.SoloSeat;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var can=Keep(new GameObject("Boulder test can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var root=Keep(new GameObject("Boulder actor",typeof(CharacterController)));
            var cc=root.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
            _actor=root.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _carrier=root.AddComponent<Carrier>();root.AddComponent<CombatVerbs>();root.AddComponent<HeroAbilitySystem>().BindHero("dante");
            root.transform.position=new Vector3(0,.13f,-8);GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();
            var artPerson=Resources.Load<RosterEntryAsset>("Roster/person_dante");Assert.IsNotNull(artPerson);
            _actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"dante");
            root.AddComponent<TumbangPreso.Visual.CharacterVisual>().ApplyModel(artPerson.Model,artPerson.Tint,artPerson.Clips,artPerson.Palette,artPerson.PetModel);
            var light=Keep(new GameObject("Dante key light")).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            RenderSettings.ambientLight=new Color(.55f,.57f,.62f);
            var shoeObject=Keep(new GameObject("Boulder own shoe"));
            var art=Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");Assert.IsNotNull(art);Assert.IsNotNull(art.Model);
            var model=Object.Instantiate(art.Model,shoeObject.transform);model.name="Visual";
            foreach(var collider in model.GetComponentsInChildren<Collider>())Object.Destroy(collider);
            TumbangPreso.Visual.ToonSkin.ApplySlipper(model,TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
            _shoe=shoeObject.AddComponent<Slipper>();_shoe.OwnerSlot=1;_shoe.SeatOfOrigin=1;
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return new WaitForSeconds(.2f);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _objects)if(go!=null)Object.Destroy(go);_objects.Clear();yield return PlayModeWorld.Reset();
            QualitySettings.SetQualityLevel(_quality,true);RenderSettings.ambientLight=_ambient;
            NetAuthority.Provider=_provider;GameLaunch.SoloSeat=_seat;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator BoulderTravelsAtAuthoredLaunchSpeed()
        {
            Vector3 origin = new Vector3(0, 1.2f, -4);
            Vector3 target = origin + Vector3.forward * 8;
            var boulder = DanteBoulder.Spawn(origin, target, 1);
            _objects.Add(boulder.gameObject);
            float expectedFlatSpeed = Vector3.ProjectOnPlane(Slipper.SolveArc(origin, target, GeoRules.BoulderSpeed), Vector3.up).magnitude * GeoRules.BoulderSpeed;
            yield return new WaitForFixedUpdate();
            Vector3 before = boulder.transform.position;
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            float travel = Vector3.ProjectOnPlane(boulder.transform.position - before, Vector3.up).magnitude;
            float expected = expectedFlatSpeed * Time.fixedDeltaTime * 5;
            Debug.Log($"[BoulderFlight] travel={travel:F4}, expected={expected:F4}, authoredSpeed={GeoRules.BoulderSpeed}");
            Assert.That(travel, Is.EqualTo(expected).Within(expected * .25f), "Boulder unit direction was used as velocity instead of the authored launch speed.");
        }
        [UnityTest] public IEnumerator BoulderHasItsOwnAuthoredWeightPreparation()
        {
            var rigObject=Keep(new GameObject("Dante owner camera"));rigObject.tag="MainCamera";
            var rig=rigObject.AddComponent<TumbangPreso.CameraSystem.CameraRig>();rig.Follow(_actor);
            rig.SetAimSource(TumbangPreso.CameraSystem.AimSource.Movement);
            var camera=Keep(new GameObject("Dante observer camera")).AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=48;camera.nearClipPlane=.05f;camera.farClipPlane=40;
            yield return new WaitForSeconds(.2f);
            bool requested=false,accepted=false;
            yield return ImprovementEvidenceProbe.Record(camera,"boulder-motion",4f,_actor,t=>
            {
                _actor.Intent.AimPoint=_actor.transform.position+Vector3.forward*5;_actor.Intent.FaceAimPoint=true;
                bool press=!requested&&t>=.25f;_actor.Intent.Set(Verb.Skill2,press);
                if(press){requested=true;_actor.Intent.BufferPress(Verb.Skill2);}
                accepted|=_actor.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Skill2)==HeroKit.CastOutcome.Cast;
            },new Vector3(2.2f,1.6f,3.3f),1.0f);
            _actor.Intent.Set(Verb.Skill2,false);
            Assert.IsTrue(accepted,"The actual Skill2 input was not accepted.");
            Assert.AreEqual(SlipperAffinity.Concussed,_shoe.Affinity);Assert.AreSame(_shoe,_carrier.Held);
            Assert.AreEqual("hero-dante-boulder",Kit.AttackingSkill.CastAction,"Boulder still uses a ground stomp for a held-shoe load.");
            Assert.AreEqual("boulder-load",Kit.AttackingSkill.ViewmodelAction,"Boulder still uses the ground-directed owner gesture.");
            var art=Resources.Load<RosterEntryAsset>("Roster/person_dante");
            Assert.IsTrue(art.Clips.Any(c=>c!=null&&c.name==Kit.AttackingSkill.CastAction&&c.length>.5f),"The shipped roster lacks its authored Boulder clip.");
        }
    }
}
