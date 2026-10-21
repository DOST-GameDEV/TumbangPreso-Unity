using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowChargeUiTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        static readonly MethodInfo Step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);

        private static (CharacterMotor who, Carrier carrier, Slipper shoe, Lata can) MakeRestoreCase()
        {
            GameServices.Ensure(); GameServices.Round.Clear();
            var go=new GameObject("Restore charge actor",typeof(CharacterController));
            var who=go.AddComponent<CharacterMotor>();who.enabled=false;
            who.PlayerSlot=1;who.Mode=GameMode.Classic;who.RoundActive=true;
            who.transform.position=new Vector3(0,.1f,-8);
            var carrier=go.AddComponent<Carrier>();carrier.enabled=false;
            GameServices.Round.Register(who);
            var can=new GameObject("Restore charge can").AddComponent<Lata>();can.enabled=false;
            GameServices.Round.Lata=can;
            GameServices.Match.ApplySnapshot(new int[4],1,true);
            GameServices.Round.ApplySnapshot(90,true,0,true);
            var shoe=new GameObject("Owned reset test slipper").AddComponent<Slipper>();shoe.enabled=false;
            shoe.OwnerSlot=shoe.SeatOfOrigin=1;Assert.IsTrue(shoe.HostForceEquip(who));
            who.Intent.Parked=false;who.Intent.AimPoint=Vector3.zero;
            return (who,carrier,shoe,can);
        }
        private static void ChargeStep(Carrier carrier,float dt)=>Step.Invoke(carrier,new object[]{dt});
        private static void DecayStep(Carrier carrier,float dt)=>typeof(Carrier)
            .GetMethod("StepRestoreChargeDecay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(carrier,new object[]{dt});

        [TestCase(false)] [TestCase(true)]
        public void ThrowsRemainLegalWithCanDownOrProtected(bool protectedCan)
        {
            var x=MakeRestoreCase();x.can.HostKnockDown(-1);
            if(protectedCan)x.can.HostRestore();
            Assert.IsTrue(GameServices.Round.CanThrow(x.who));
            x.who.Intent.Set(Verb.SpecialAbility,true);ChargeStep(x.carrier,0);ChargeStep(x.carrier,.5f);
            Assert.IsTrue(x.carrier.IsCharging);
            x.who.Intent.Set(Verb.SpecialAbility,false);ChargeStep(x.carrier,.01f);
            Assert.IsNull(x.carrier.Held);Assert.AreEqual(SlipperState.InFlight,x.shoe.State);
        }

        [TestCase(false)] [TestCase(true)]
        public void RestoreSmoothlyLowersChargeAndCannotReleaseMidDecay(bool snapshot)
        {
            var x=MakeRestoreCase();
            x.who.Intent.Set(Verb.SpecialAbility,true);ChargeStep(x.carrier,0);ChargeStep(x.carrier,Balance.ChargeFullTime);
            x.can.HostKnockDown(-1);ChargeStep(x.carrier,.02f);
            Assert.IsTrue(x.carrier.IsCharging);Assert.AreEqual(1,x.carrier.ChargeRatio);
            if(snapshot)x.can.ApplySnapshotState(Vector3.zero,Quaternion.identity,true,x.can.SkinIndex);
            else x.can.HostRestore();
            Assert.IsTrue(x.carrier.IsThrowChargeDecaying);Assert.AreEqual(1,x.carrier.ChargeRatio);
            Assert.IsFalse(GameServices.Round.CanThrow(x.who));
            DecayStep(x.carrier,.25f);
            Assert.AreEqual(.5f,x.carrier.ChargeRatio,.0001f);Assert.AreEqual(.5f,x.carrier.ObservedChargePower,.0001f);
            x.who.Intent.Set(Verb.SpecialAbility,false);ChargeStep(x.carrier,.01f);
            x.carrier.HostThrowAt(x.who.transform.position,Vector3.zero,1);
            Assert.AreSame(x.shoe,x.carrier.Held,"Neither input nor direct host release may bypass decay.");
            // Repeated upright packets are observation, not another reset.
            x.can.ApplySnapshotState(Vector3.zero,Quaternion.identity,true,x.can.SkinIndex);
            DecayStep(x.carrier,.25f);
            Assert.IsFalse(x.carrier.IsThrowChargeDecaying);Assert.IsFalse(x.carrier.IsCharging);
            Assert.Zero(x.carrier.ChargeRatio);Assert.AreEqual(-1,x.carrier.ObservedChargePower);
            Assert.AreSame(x.shoe,x.carrier.Held,"A release during lowering is discarded, never buffered.");
            x.who.Intent.Set(Verb.SpecialAbility,true);ChargeStep(x.carrier,0);Assert.Zero(x.carrier.ChargeRatio);
            ChargeStep(x.carrier,Balance.ChargeFullTime);Assert.AreEqual(1,x.carrier.ChargeRatio);
            x.who.Intent.Set(Verb.SpecialAbility,false);ChargeStep(x.carrier,.01f);
            Assert.IsNull(x.carrier.Held);Assert.IsTrue(x.can.IsProtected,"Throwing no longer waits for the can barrier.");
        }

        [Test]
        public void ObservedChargeDecaysAndLateKeepaliveCannotRaiseIt()
        {
            var x=MakeRestoreCase();x.carrier.ApplyObservedCharge(true,Balance.ChargeFullTime,.5f);
            x.can.HostKnockDown(-1);x.can.HostRestore();DecayStep(x.carrier,.25f);
            Assert.AreEqual(.5f,x.carrier.ObservedChargePower,.0001f);
            x.carrier.ApplyObservedCharge(true,Balance.ChargeFullTime,.5f);
            Assert.AreEqual(.5f,x.carrier.ObservedChargePower,.0001f);
            DecayStep(x.carrier,0);Assert.AreEqual(.5f,x.carrier.ObservedChargePower,.0001f,"Paused time cannot consume the decay.");
            DecayStep(x.carrier,.25f);Assert.AreEqual(-1,x.carrier.ObservedChargePower);
        }

        [Test]
        public void LosingTheShoeCancelsRestoreDecay()
        {
            var x=MakeRestoreCase();x.who.Intent.Set(Verb.SpecialAbility,true);
            ChargeStep(x.carrier,0);ChargeStep(x.carrier,.8f);x.can.HostKnockDown(-1);x.can.HostRestore();
            Assert.IsTrue(x.carrier.IsThrowChargeDecaying);x.shoe.HostDisarm();DecayStep(x.carrier,.01f);
            Assert.IsFalse(x.carrier.IsThrowChargeDecaying);Assert.IsFalse(x.carrier.IsCharging);
            Assert.AreEqual(-1,x.carrier.ObservedChargePower);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator RestoreDecayHasNativeBodyAndOwnerHandEvidence()
        {
            var x=MakeRestoreCase();x.who.IsBot=true;x.who.Mode=GameMode.HeroStrike;
            x.who.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"zack");
            var art=Resources.Load<RosterEntryAsset>("Roster/person_zack");
            x.who.gameObject.AddComponent<Visual.CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var shoeArt=Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");
            var shoeModel=Object.Instantiate(shoeArt.Model,x.shoe.transform);
            Visual.ToonSkin.ApplySlipper(shoeModel,Visual.ToonSkin.PropOutlineWidth);
            x.shoe.HostForceEquip(x.who);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;
            floor.transform.localScale=new Vector3(30,1,30);
            var capsule=x.who.GetComponent<CharacterController>();
            capsule.height=1.6f;capsule.center=Vector3.up*.8f;capsule.radius=.35f;
            x.who.enabled=true;
            var light=new GameObject("Charge review light").AddComponent<Light>();
            light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-25,0);
            var owner=new GameObject("Charge owner camera",typeof(Camera));owner.tag="MainCamera";
            var rig=owner.AddComponent<CameraSystem.CameraRig>();rig.Follow(x.who,true);rig.SetAimSource(CameraSystem.AimSource.Movement);
            var witness=new GameObject("Charge body witness").AddComponent<Camera>();witness.enabled=false;
            witness.fieldOfView=45;witness.nearClipPlane=.05f;
            yield return null;yield return null;
            witness.transform.position=x.who.transform.position+new Vector3(2.4f,1.3f,3);
            witness.transform.LookAt(x.who.transform.position+Vector3.up);witness.Render();owner.GetComponent<Camera>().Render();
            yield return null;
            x.can.HostKnockDown(-1);x.carrier.enabled=true;
            bool restored=false,sawMiddle=false;float restoredAt=-1,endedAt=-1;
            yield return ImprovementEvidenceProbe.Record(witness,"restore-charge",3.4f,x.who,t=>
            {
                x.who.Intent.Set(Verb.SpecialAbility,t>=.2f&&t<2.2f);
                if(t>=2.05f&&!restored)
                {
                    Assert.Greater(x.carrier.ChargeRatio,.95f);
                    x.can.HostRestore();restored=true;restoredAt=Time.time;
                }
                if(restored)
                {
                    sawMiddle|=x.carrier.ChargeRatio>.1f&&x.carrier.ChargeRatio<.9f;
                    if(endedAt<0&&!x.carrier.IsThrowChargeDecaying)endedAt=Time.time;
                    Assert.AreSame(x.shoe,x.carrier.Held,"Release during the return must never launch.");
                }
            },new Vector3(2.4f,1.3f,3));
            Assert.IsTrue(restored&&sawMiddle);Assert.GreaterOrEqual(endedAt-restoredAt,.49f);
            Assert.Less(endedAt-restoredAt,.65f,"The real charge return must finish around0.5s, not a later cooldown.");
            Assert.IsFalse(x.carrier.IsCharging);Assert.Zero(x.carrier.ChargeRatio);
        }

        [UnityTest]
        public IEnumerator CentreDotStaysFilledAndOnlyPulsesForItsOwnersThrow()
        {
            var root = new GameObject("DotTestCanvas", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = new GameObject("Reticle", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var reticle = go.AddComponent<HudReticle>();
            reticle.rectTransform.sizeDelta = new Vector2(96, 96);
            var settings = Settings.SettingsStore.Current;
            bool motion = settings.ReducedUiMotion, effects = settings.ReducedEffects;
            try
            {
                settings.ReducedUiMotion = settings.ReducedEffects = false;
                reticle.SetOwner(1);
                foreach (var charge in new[] { 0f, .5f, 1f })
                {
                    reticle.Set(charge, -.7f, .5f, false, true);
                    AssertDot(reticle);
                }
                reticle.Set(.8f, .7f, 0, true, false); AssertDot(reticle);
                reticle.Set(0, 0, 0, false, false); AssertDot(reticle);
                Assert.Zero(reticle.ReleasePulseRemaining, "Cancellation is not a confirmed release.");
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 2, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Another player must not pulse this aim.");
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Greater(reticle.ReleasePulseRemaining, 0);
                yield return new WaitForSeconds(.26f);
                Assert.Zero(reticle.ReleasePulseRemaining);
                settings.ReducedUiMotion = true;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining);
                reticle.Set(1, 0, 0, false, false);
                Assert.AreEqual(3.2f, reticle.AimRadius, .001f);
                reticle.enabled = false;
                settings.ReducedUiMotion = false;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Disabled reticle must unsubscribe.");
                reticle.enabled = true; reticle.SetOwner(1);
                root.GetComponent<Canvas>().enabled = false;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Hidden HUD must not retain a pulse.");
            }
            finally
            {
                settings.ReducedUiMotion = motion; settings.ReducedEffects = effects;
                Object.Destroy(root);
            }
        }

        private static void AssertDot(HudReticle reticle)
        {
            using var vh = new VertexHelper();
            typeof(HudReticle).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null)
                .Invoke(reticle, new object[] { vh });
            var mesh = new Mesh();
            try
            {
                vh.FillMesh(mesh);
                Assert.Greater(mesh.vertexCount, 0);
                var vertices = mesh.vertices; var triangles = mesh.triangles;
                var centre = reticle.rectTransform.rect.center;
                bool fillsCentre=false;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                    float x=Cross(b-a,centre-a),y=Cross(c-b,centre-b),z=Cross(a-c,centre-c);
                    if((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0))fillsCentre=true;
                }
                Assert.IsTrue(fillsCentre,"The aim centre must contain the filled dot");
                if(reticle.Charge==0&&reticle.Cooldown==0)
                    foreach(var v in vertices)Assert.LessOrEqual(Vector2.Distance(v,centre),5.2f,"Idle aim must stay a compact dot");
            }
            finally { Object.Destroy(mesh); }
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        [UnityTest, Timeout(60000)]
        public IEnumerator LiveChargeShowsPowerFullAndRealRefusalsThenClearsOnRelease()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var ready = Object.FindFirstObjectByType<ReadyGate>(); ready.enabled = true; ready.StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            Assert.IsFalse(ready.AwaitingReady || ready.CountingDown);
            var who = GameServices.Round.PlayerAt(1); var carrier = who.GetComponent<Carrier>();
            var can = GameServices.Round.Lata;
            float clearBy = Time.realtimeSinceStartup + 6;
            while (can.IsProtected && Time.realtimeSinceStartup < clearBy) yield return null;
            Assert.IsFalse(can.IsProtected);
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            who.Teleport(new Vector3(-3, .12f, -8)); who.transform.rotation = Quaternion.identity;
            who.Intent.Clear(); who.Intent.Parked = false; who.Intent.AimPoint = new Vector3(-3, .12f, 0);
            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>(); rig.Follow(who);
            rig.SetAimSource(CameraSystem.AimSource.Movement);
            yield return null;
            var direction = who.Intent.AimPoint - rig.transform.position;
            typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig, Mathf.Atan2(-direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
            carrier.enabled = false;
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Aim-dot-idle-960x540", canvas, 960, 540, false, true);
            Assert.IsTrue(GameServices.Round.CanThrow(who));
            who.Intent.Set(Verb.SpecialAbility, true);
            Step.Invoke(carrier, new object[] { 0f }); Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f });
            Assert.IsTrue(carrier.IsCharging);
            yield return null;
            var reticle = Object.FindFirstObjectByType<HudReticle>(); Assert.IsNotNull(reticle);
            Assert.AreEqual("50%", reticle.ChargeCaption); Assert.AreEqual(.5f, reticle.Charge, .001f);
            Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f }); yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption); Assert.IsFalse(reticle.Refused);
            yield return TumpUiCapture.Capture("Throw-charge-full-960x540", canvas, 960, 540, false, true);
            yield return TumpUiCapture.Capture("Throw-charge-full-1600x680", canvas, 1600, 680, false, true);
            can.HostKnockDown(2);
            yield return null;
            Step.Invoke(carrier, new object[] { .02f });yield return null;
            Assert.IsFalse(can.IsUpright); Assert.IsTrue(GameServices.Round.CanThrow(who));
            Assert.IsTrue(carrier.IsCharging,"Knockdown must preserve the charge");
            can.HostRestore();yield return null;
            Assert.IsTrue(carrier.IsThrowChargeDecaying);
            DecayStep(carrier,.25f);yield return null;Assert.AreEqual(.5f,carrier.ChargeRatio,.01f);
            DecayStep(carrier,.25f);yield return null;
            Assert.IsTrue(GameServices.Round.CanThrow(who));Assert.IsFalse(carrier.IsCharging);
            Step.Invoke(carrier,new object[]{0f});Assert.IsTrue(carrier.IsCharging);Assert.AreEqual(0,carrier.ChargeRatio);
            Step.Invoke(carrier,new object[]{Balance.ChargeFullTime});yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption);
            reticle.enabled = false; Assert.AreEqual("", reticle.ChargeCaption, "Hidden reticle must hide its caption too.");
            reticle.enabled = true; yield return null;
            who.Intent.Set(Verb.SpecialAbility, false); Step.Invoke(carrier, new object[] { .01f }); yield return null;
            Assert.IsFalse(carrier.IsCharging); Assert.AreEqual("", reticle.ChargeCaption);
            Assert.Greater(reticle.ReleasePulseRemaining, 0, "The actual accepted throw must pulse the circle.");
            AssertDot(reticle);
            yield return TumpUiCapture.Capture("Aim-circle-release-1600x680", canvas, 1600, 680, false, true);
            yield return new WaitForSeconds(.3f);
            Assert.Zero(reticle.ReleasePulseRemaining);
        }
    }
}
