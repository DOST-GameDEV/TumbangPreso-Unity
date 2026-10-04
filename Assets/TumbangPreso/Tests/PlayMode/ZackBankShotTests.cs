using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    [DefaultExecutionOrder(10000)]
    public sealed class BankLoadGripObserver : MonoBehaviour
    {
        public CharacterVisual Visual;
        public Slipper Shoe;
        public Carrier Carrier;
        public CharacterMotor Motor;
        public bool Measuring, BrokenGrip;
        public float MaximumSlack;
        private void LateUpdate()
        {
            if (!Measuring) return;
            var anchor = Visual != null ? Visual.HandAnchor : null;
            if (anchor == null || Shoe == null || Carrier.Held != Shoe || Shoe.Holder != Motor)
            { BrokenGrip = true; return; }
            MaximumSlack = Mathf.Max(MaximumSlack, Vector3.Distance(Shoe.transform.position + Shoe.DrawnCentreOffset,
                anchor.position + anchor.up * Shoe.CarrySupportExtent(anchor.up)));
        }
    }

    public sealed class ZackBankShotTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        AbilityContext Actor(out ZackHeroKit kit)
        {
            var go = Track(new GameObject("Bank owner"));
            var body = go.AddComponent<CharacterMotor>(); body.PlayerSlot = 1; body.enabled = false;
            // The ability system caches its Carrier in Awake, as at the real spawn site.
            var carrier = go.AddComponent<Carrier>(); carrier.enabled = false;
            var system = go.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("zack");
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            kit = (ZackHeroKit)system.Kit;
            return new AbilityContext(body, carrier, null);
        }
        Slipper Shoe(AbilityContext ctx)
        {
            var shoe = Track(new GameObject("Bank slipper")).AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = ctx.Motor.PlayerSlot; shoe.SeatOfOrigin = ctx.Motor.PlayerSlot;
            shoe.HostForceEquip(ctx.Motor); return shoe;
        }
        [Test] public void BankShotUsesCooldownAndEightSecondLoad()
        {
            var kit = new ZackHeroKit();
            Assert.AreEqual("BANK SHOT", kit.AttackingSkill.Name);
            Assert.AreEqual(35, kit.AttackingSkill.Cooldown);
            Assert.AreEqual(8, kit.AttackingSkill.Duration);
            Assert.IsFalse(kit.AttackingSkill.UsesCharges);
        }
        [Test] public void BankShotRequiresHeldSlipperInsteadOfRecallingLooseOne()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            Assert.IsTrue(kit.AttackingSkill.CanActivate(ctx), "A held slipper must be loadable.");
            shoe.HostThrow(ctx.Motor, Vector3.up, Vector3.forward);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx));
            shoe.ApplySnapshotState(SlipperState.Loose, null, Vector3.zero, Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, -1);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx), "A loose shoe must still be retrieved normally.");
            Assert.IsNull(ctx.Carrier.Held);
        }
        [Test] public void ActualCarrierThrowConsumesLoadWithoutSpeedBoostOrZap()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            Assert.IsTrue(kit.IsBankShotLoadedFor(shoe));
            Vector3 origin = new Vector3(0, 1, -8), target = new Vector3(0, 0, 0);
            var expected = shoe.LaunchVelocityTo(origin, target, 1);
            using (NetCue.SuppressRelay()) ctx.Carrier.HostThrowAt(origin, target, 1);
            Assert.AreEqual(SlipperState.InFlight, shoe.State);
            Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
            Assert.Less(Vector3.Distance(expected, shoe.Velocity), .001f);
            Assert.IsFalse(kit.IsOverchargeThrowActive); Assert.IsNull(ctx.Carrier.Held);
            kit.AttackingSkill.Tick(ctx, .02f); Assert.IsFalse(kit.AttackingSkill.IsActive);
            Assert.Greater(kit.AttackingSkill.CooldownRemaining, 34);
        }
        [Test] public void LoadExpiresDropsAndRoundResetDoNotTransferToAnotherShoe()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.AttackingSkill.Tick(ctx, 8); Assert.IsFalse(kit.IsBankShotLoadedFor(shoe));
            kit.AttackingSkill.ApplyNetworkSnapshot(0, 0, true);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            shoe.ApplySnapshotState(SlipperState.Loose, null, Vector3.zero, Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, -1);
            kit.AttackingSkill.Tick(ctx, .02f); Assert.IsFalse(kit.IsOverchargeThrowActive);
            var replacement = Shoe(ctx); Assert.IsFalse(kit.IsBankShotLoadedFor(replacement));
            kit.AttackingSkill.ApplyNetworkSnapshot(0, 0, true);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.ResetForRound(ctx); Assert.IsFalse(kit.IsBankShotLoadedFor(replacement));
        }
        [UnityTest, Timeout(120000)] public IEnumerator AcceptedLoadAnimatesTheHeldShoeWithoutARecall()
            => ReviewLoad(false, false);
        [UnityTest, Timeout(120000)] public IEnumerator OwnerLoadUsesTheNewVisibleHandGesture()
            => ReviewLoad(true, false);
        [UnityTest, Timeout(120000)] public IEnumerator WalkingLoadKeepsItsLegsAndShoeAttached()
            => ReviewLoad(false, true);

        private IEnumerator ReviewLoad(bool ownerView, bool walking)
        {
            var provider = NetAuthority.Provider; NetAuthority.Provider = null;
            var priorRate = Time.captureFramerate; var priorSeat = GameLaunch.SoloSeat;
            var priorBots = GameLaunch.AllBots; var priorSpectator = GameLaunch.Spectator;
            var priorReduced = Settings.SettingsStore.Current.ReducedUiMotion;
            var ambient = RenderSettings.ambientLight;
            try
            {
                Time.captureFramerate = 60;
                var ctx = Actor(out var kit); var motor = ctx.Motor;
                motor.Mode = GameMode.HeroStrike; motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack");
                kit.SetRole(false, ctx);
                var capsule = motor.GetComponent<CharacterController>();
                capsule.height = 1.6f; capsule.radius = .35f; capsule.center = Vector3.up * .8f;
                capsule.slopeLimit = 45; capsule.stepOffset = .3f;
                var visual = motor.gameObject.AddComponent<CharacterVisual>();
                var visualRoot = new GameObject("Visual").transform; visualRoot.SetParent(motor.transform, false);
                visual.SetModelRoot(visualRoot);
                var art = Resources.Load<RosterEntryAsset>("Roster/person_zack");
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                var shoe = Shoe(ctx);
                var shoeArt = Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");
                var shoeModel = Object.Instantiate(shoeArt.Model, shoe.transform);
                foreach (var collider in shoeModel.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                ToonSkin.ApplySlipper(shoeModel, ToonSkin.PropOutlineWidth);
                ctx.Carrier.enabled = true;
                var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
                floor.transform.position = Vector3.down * .5f; floor.transform.localScale = new Vector3(20, 1, 20);
                motor.Teleport(Vector3.up * .1f); motor.enabled = true;
                for (int i = 0; i < 25 && !motor.IsGrounded; i++) yield return new WaitForFixedUpdate();
                Assert.IsTrue(motor.IsGrounded);
                GameLaunch.SoloSeat = motor.PlayerSlot; GameLaunch.AllBots = false; GameLaunch.Spectator = false;
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                Camera ownerCamera = null;
                if (ownerView)
                {
                    var owner = Track(new GameObject("Bank owner camera")); owner.tag = "MainCamera";
                    var rig = owner.AddComponent<CameraSystem.CameraRig>(); rig.Follow(motor, true);
                    rig.SetAimSource(CameraSystem.AimSource.Movement); ownerCamera = owner.GetComponent<Camera>();
                }
                var camera = Track(new GameObject("Bank body observer")).AddComponent<Camera>();
                camera.enabled = false; camera.fieldOfView = 42;
                camera.transform.position = motor.transform.position + new Vector3(2.3f, 1.45f, 3.3f);
                camera.transform.LookAt(motor.transform.position + Vector3.up * .85f);
                var sun = Track(new GameObject("Bank key light")).AddComponent<Light>();
                sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
                RenderSettings.ambientLight = new Color(.55f, .57f, .62f);
                yield return null; yield return null; yield return null;
                var body = motor.GetComponent<CharacterAnimator>();
                var arm = System.Array.Find(visual.Model.GetComponentsInChildren<Transform>(true), t => t.name == "arm-left");
                var leg = System.Array.Find(visual.Model.GetComponentsInChildren<Transform>(true), t => t.name == "leg-left");
                var neutralArm = arm.localRotation; var neutralLeg = leg.localRotation;
                var ownerArms = ownerView ? Object.FindFirstObjectByType<CameraSystem.ViewmodelArms>() : null;
                if (ownerView) { Assert.IsNotNull(ownerArms); Assert.AreSame(motor, ownerArms.BoundCharacter); }
                var ownerHand = ownerArms != null ? ownerArms.LeftHandForProps() : null;
                var ownerNeutral = ownerHand != null ? ownerHand.localRotation : Quaternion.identity;
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var action = typeof(CameraSystem.ViewmodelArms).GetField("_actionName", hidden);
                var gait = typeof(CharacterAnimator).GetField("_gaitWeight", hidden);
                var grip = motor.gameObject.AddComponent<BankLoadGripObserver>();
                grip.Visual = visual; grip.Shoe = shoe; grip.Carrier = ctx.Carrier; grip.Motor = motor;
                var start = motor.transform.position;
                motor.Intent.Move = walking ? Vector2.right : Vector2.zero;
                Assert.AreSame(ctx.Carrier, typeof(HeroAbilitySystem).GetField("_carrier", hidden).GetValue(motor.AbilitySystem));
                Assert.IsTrue(motor.CanAct(), "The staged body must be able to act before the load request.");
                Assert.AreEqual(HeroKit.CastOutcome.Cast, motor.AbilitySystem.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    motor.transform.position, motor.transform.forward, motor.transform.position + Vector3.forward * 5, 0, true));
                Assert.IsTrue(kit.IsBankShotLoadedFor(shoe), "Cosmetic preparation must not delay the real load.");
                bool sawBody = false, sawOwner = false; float maxArm = 0, maxOwner = 0, maxLeg = 0, maxGait = 0;
                grip.Measuring = true;
                string capture = System.Environment.GetEnvironmentVariable("TUMP_BANK_CAPTURE");
                var times = new List<string> { "frame,simulation_seconds" }; float began = Time.time;
                for (int frame = 0; frame < 48; frame++)
                {
                    sawBody |= body.CurrentClipName == "hero-zack-bankshot" && body.IsPlayingAction;
                    maxArm = Mathf.Max(maxArm, Quaternion.Angle(neutralArm, arm.localRotation));
                    if (ownerView)
                    {
                        sawOwner |= (string)action.GetValue(ownerArms) == "bank-load";
                        maxOwner = Mathf.Max(maxOwner, Quaternion.Angle(ownerNeutral, ownerHand.localRotation));
                    }
                    if (body.CurrentClipName == "hero-zack-bankshot" && body.IsPlayingAction)
                    {
                        maxGait = Mathf.Max(maxGait, (float)gait.GetValue(body));
                        maxLeg = Mathf.Max(maxLeg, Quaternion.Angle(neutralLeg, leg.localRotation));
                    }
                    kit.AttackingSkill.Tick(ctx, 1f / 60);
                    if (!string.IsNullOrEmpty(capture) && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    {
                        string kind = ownerView ? "owner" : walking ? "walking" : "body";
                        yield return GameplayShots.Render(ownerView ? ownerCamera : camera, kind + "-" + frame.ToString("D3"), false, capture, motor, 640, 360);
                        times.Add(frame + "," + (Time.time - began).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else yield return null;
                }
                if (!string.IsNullOrEmpty(capture))
                    System.IO.File.WriteAllLines(System.IO.Path.Combine(capture, (ownerView ? "owner" : walking ? "walking" : "body") + "-times.csv"), times);
                motor.Intent.Move = Vector2.zero;
                Assert.IsTrue(sawBody); Assert.Greater(maxArm, 25);
                grip.Measuring = false;
                Assert.IsFalse(grip.BrokenGrip, "The accepted load changed its held-shoe relationship.");
                Assert.Less(grip.MaximumSlack, .05f, "The loaded slipper detached from its moving grip.");
                if (ownerView) { Assert.IsTrue(sawOwner); Assert.Greater(maxOwner, 10); }
                if (walking)
                {
                    Assert.Greater(Vector3.Distance(start, motor.transform.position), .5f);
                    Assert.Greater(maxGait, .8f); Assert.Greater(maxLeg, 10);
                }
                Assert.IsTrue(kit.IsBankShotLoadedFor(shoe));
                Assert.That(kit.AttackingSkill.DurationRemaining, Is.EqualTo(7.2f).Within(.02f));
                using (NetCue.SuppressRelay()) ctx.Carrier.HostThrowAt(motor.transform.position + Vector3.up, Vector3.forward * 5, 1);
                Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
                Assert.IsNull(ctx.Carrier.Held); Assert.IsFalse(kit.IsOverchargeThrowActive);
            }
            finally
            {
                Time.captureFramerate = priorRate; GameLaunch.SoloSeat = priorSeat;
                GameLaunch.AllBots = priorBots; GameLaunch.Spectator = priorSpectator;
                Settings.SettingsStore.Current.ReducedUiMotion = priorReduced;
                RenderSettings.ambientLight = ambient; NetAuthority.Provider = provider;
            }
        }

        static readonly MethodInfo Bounds = typeof(Slipper).GetMethod("BounceOffBounds", BindingFlags.Instance | BindingFlags.NonPublic);
        void Bank(Slipper shoe, bool corner = false)
        {
            shoe.transform.position = new Vector3(AIController.PlayableMaxX + 1, 1, corner ? AIController.PlayableMaxZ + 1 : 0);
            using (NetCue.SuppressRelay()) Bounds.Invoke(shoe, null);
        }
        [TestCase(false)] [TestCase(true)]
        public void CeilingDoesNotSpendTheRemainingPoweredWallCredit(bool overclock)
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            var affinity = overclock ? SlipperAffinity.OverclockBank : SlipperAffinity.BankShot;
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 8, 2), affinity);
            shoe.transform.position = new Vector3(0, AIController.PlayableCeilingY + 1, 0);
            using (NetCue.SuppressRelay()) Bounds.Invoke(shoe, null);
            Assert.AreEqual(affinity, shoe.Affinity, "A ceiling does not consume a powered side-wall bank.");
            Assert.Less(shoe.Velocity.y, 0, "The ceiling must still return the shoe to court.");
            Bank(shoe);
            if (overclock) Bank(shoe);
            Assert.AreEqual(1, shoe.ThrowerSlot, "All promised powered wall banks must retain the thrower's can credit after a ceiling contact.");
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity);
            Bank(shoe);
            Assert.AreEqual(-1, shoe.ThrowerSlot, "The next unpowered wall must still retire scoring credit.");
        }
        [Test] public void CeilingBetweenOverclockBanksDoesNotReplayBankFlairOrSpendCredit()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 8, 2), SlipperAffinity.OverclockBank, .9f);
            int flairs = 0;
            void Observe(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            { if (kind == MatchFlair.Kind.BankShot) flairs++; }
            MatchFlair.Presented += Observe;
            try
            {
                Bank(shoe); Assert.AreEqual(1, flairs);
                shoe.transform.position = new Vector3(0, AIController.PlayableCeilingY + 1, 0);
                using (NetCue.SuppressRelay()) Bounds.Invoke(shoe, null);
                Assert.AreEqual(1, shoe.BankCount); Assert.AreEqual(1, flairs);
                Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
                Bank(shoe); Assert.AreEqual(1, shoe.ThrowerSlot);
                Assert.AreEqual(2, shoe.BankCount); Assert.AreEqual(2, flairs, "Each real powered wall contact gets its own cue; the ceiling stays silent.");
            }
            finally { MatchFlair.Presented -= Observe; }
        }
        [TestCase(false, 1)] [TestCase(true, 2)]
        public void EveryUnspunPoweredWallAnnouncesContact(bool overclock, int expected)
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 8, 2),
                overclock ? SlipperAffinity.OverclockBank : SlipperAffinity.BankShot);
            var strengths = new List<float>();
            void Observe(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            {
                if (kind != MatchFlair.Kind.BankShot) return;
                Assert.AreEqual(1, actor); Assert.AreEqual(shoe.transform.position, at);
                strengths.Add(strength);
            }
            MatchFlair.Presented += Observe;
            try
            {
                for (int i = 0; i < expected; i++) Bank(shoe);
                Assert.AreEqual(expected, strengths.Count);
                Assert.That(strengths, Is.All.EqualTo(BankShotContact.FlairStrength));
                Bank(shoe);
                Assert.AreEqual(expected, strengths.Count, "An unpowered unspun contact cannot pretend to be powered.");
            }
            finally { MatchFlair.Presented -= Observe; }
        }
        [Test] public void OrdinarySpunBankKeepsUnpoweredFlair()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 8, 2), SlipperAffinity.Normal, .9f);
            var strengths = new List<float>();
            void Observe(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            { if (kind == MatchFlair.Kind.BankShot) strengths.Add(strength); }
            MatchFlair.Presented += Observe;
            try
            {
                Bank(shoe); Bank(shoe);
                Assert.AreEqual(1, strengths.Count); Assert.AreEqual(0, strengths[0]);
            }
            finally { MatchFlair.Presented -= Observe; }
        }
        [Test] public void PoweredObstacleContactUsesResolvedPosition()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.right, new Vector3(10, 0, 0), SlipperAffinity.BankShot);
            var wall = Track(new GameObject("Bank contact wall"));
            wall.transform.position = new Vector3(3, 1, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 2, 4);
            Physics.SyncTransforms();
            var previous = new Vector3(1, 1, 0); shoe.transform.position = new Vector3(4, 1, 0);
            int contacts = 0; Vector3 position = Vector3.zero;
            void Observe(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            {
                if (kind != MatchFlair.Kind.BankShot) return;
                Assert.AreEqual(BankShotContact.FlairStrength, strength);
                contacts++; position = at;
            }
            MatchFlair.Presented += Observe;
            try
            {
                using (NetCue.SuppressRelay())
                    typeof(Slipper).GetMethod("BounceOffObstacles", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(shoe, new object[] { previous, .3f });
                Assert.AreEqual(1, contacts);
                Assert.AreEqual(shoe.transform.position, position);
                Assert.Less(position.x, 3); Assert.Greater(position.x, 2);
                Assert.Less(shoe.Velocity.x, 0);
            }
            finally { MatchFlair.Presented -= Observe; }
        }
        [UnityTest] public IEnumerator ContactLifetimeReleasesItsOwnedMaterial()
        {
            var cue = BankShotContact.Spawn(new Vector3(2, 1, 0), false, false);
            Assert.IsNotNull(cue);
            var material = cue.GetComponentInChildren<LineRenderer>().sharedMaterial;
            Assert.IsNotNull(material);
            yield return new WaitForSeconds(BankShotContact.Lifetime + .1f);
            yield return null;
            Assert.IsTrue(cue == null, "The contact object must retire automatically.");
            Assert.IsTrue(material == null, "The contact must release its exclusively owned material.");
        }
        [Test] public void OrdinaryCeilingContactRetainsExistingBankLimit()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 8, 2));
            shoe.transform.position = new Vector3(0, AIController.PlayableCeilingY + 1, 0);
            using (NetCue.SuppressRelay()) Bounds.Invoke(shoe, null);
            Assert.AreEqual(1, shoe.BankCount); Assert.AreEqual(1, shoe.ThrowerSlot);
            Bank(shoe); Assert.AreEqual(-1, shoe.ThrowerSlot);
        }
        [Test] public void PoweredCornerCountsOnceAndSecondNormalBankLosesCredit()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 2, 8), SlipperAffinity.BankShot);
            var incoming = shoe.Velocity.magnitude; Bank(shoe, true);
            Assert.AreEqual(1, shoe.BankCount); Assert.AreEqual(1, shoe.ThrowerSlot);
            Assert.AreEqual(incoming * .85f, shoe.Velocity.magnitude, .001f);
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity);
            Bank(shoe); Assert.AreEqual(2, shoe.BankCount); Assert.AreEqual(-1, shoe.ThrowerSlot);
        }
        [Test] public void OverclockThrowHasTwoLosingBanksThenClearsCreditAndCannotLeakToNextThrow()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            Assert.IsTrue(kit.RestoreTimedKit(ctx.Motor, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true)));
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            Assert.AreEqual(SlipperAffinity.OverclockBank, kit.BankShotAffinityFor(shoe));
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 2, 8), kit.BankShotAffinityFor(shoe));
            float speed = shoe.Velocity.magnitude; Bank(shoe);
            Assert.AreEqual(speed * .85f, shoe.Velocity.magnitude, .001f); Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
            speed = shoe.Velocity.magnitude; Bank(shoe);
            Assert.AreEqual(speed * .85f, shoe.Velocity.magnitude, .001f); Assert.AreEqual(1, shoe.ThrowerSlot);
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity); Bank(shoe); Assert.AreEqual(-1, shoe.ThrowerSlot);
            shoe.HostThrow(ctx.Motor, Vector3.up, Vector3.right * 10);
            Bank(shoe); Assert.AreEqual(1, shoe.ThrowerSlot); Bank(shoe); Assert.AreEqual(-1, shoe.ThrowerSlot);
        }
        [Test] public void AgedLoadRecoveryWaitsForItsOwnEquipmentAndFlightSnapshotConsumesIt()
        {
            var ctx = Actor(out var kit);
            kit.AttackingSkill.ApplyNetworkSnapshot(20, 0, true);
            Assert.IsTrue(kit.RestoreJoiningCharges(ctx.Motor, 5, 0));
            kit.AttackingSkill.Tick(ctx, 1); Assert.IsTrue(kit.IsOverchargeThrowActive);
            var shoe = Shoe(ctx); Assert.IsTrue(kit.IsBankShotLoadedFor(shoe));
            Assert.AreEqual(4, kit.AttackingSkill.DurationRemaining, .001f);
            Assert.AreEqual(19, kit.AttackingSkill.CooldownRemaining, .001f);
            Assert.IsFalse(kit.RestoreJoiningCharges(ctx.Motor, 8, 0));
            shoe.ApplySnapshotState(SlipperState.InFlight, null, Vector3.up, Quaternion.identity, Vector3.right * 10, 0, SlipperAffinity.BankShot, 1);
            Assert.IsFalse(kit.IsOverchargeThrowActive);
            Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
        }
        [TestCase(false)] [TestCase(true)] public void PoweredGuideMatchesActualWallFlight(bool overclock)
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.name = "FloorBank";
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.name = "BankWall";
            wall.transform.position = new Vector3(0, 2, 2); wall.transform.localScale = new Vector3(8, 4, .4f);
            Physics.SyncTransforms();
            var ctx = Actor(out var kit); var loaded = Shoe(ctx);
            ctx.Motor.Teleport(new Vector3(-8, .1f, -8));
            if (overclock) kit.RestoreTimedKit(ctx.Motor, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true));
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            var guide = TrajectoryPreview.AttachTo(ctx.Motor); Track(guide.gameObject); guide.enabled = false;
            var shoe = Track(new GameObject("Actual flight")).AddComponent<Slipper>(); shoe.enabled = false;
            var affinity = overclock ? SlipperAffinity.OverclockBank : SlipperAffinity.BankShot;
            var origin = new Vector3(0, 1, -2); var velocity = new Vector3(0, 3, 7);
            Assert.IsTrue(guide.TryPredictLanding(origin, velocity, .9f, out var predicted));
            var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            using (NetCue.SuppressRelay())
            {
                shoe.HostThrow(null, origin, velocity, affinity, .9f);
                for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
            }
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = predicted.y;
            Assert.Less(Vector3.Distance(actual, predicted), .12f);
        }
        [Test] public void BotPredictionAfterTwoPoweredBanksDoesNotRestoreFirstSpinBank()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.name = "FloorBank";
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var shoe = Track(new GameObject("Banked flight")).AddComponent<Slipper>(); shoe.enabled = false;
            shoe.HostThrow(null, Vector3.up, new Vector3(10, 4, 8), SlipperAffinity.OverclockBank, .9f);
            Bank(shoe); Bank(shoe);
            shoe.transform.position = new Vector3(0, 1, -2);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.name = "Third wall";
            wall.transform.position = new Vector3(-1, 2, -1); wall.transform.localScale = new Vector3(.3f, 4, 8);
            Physics.SyncTransforms();
            var query = typeof(AIController).GetMethod("TryPredictedLanding", BindingFlags.Static | BindingFlags.NonPublic);
            var args = new object[] { shoe, Vector3.zero };
            Assert.IsTrue((bool)query.Invoke(null, args)); var predicted = (Vector3)args[1];
            var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            using (NetCue.SuppressRelay()) for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = predicted.y;
            Assert.Less(Vector3.Distance(actual, predicted), .12f);
        }
    }
}
