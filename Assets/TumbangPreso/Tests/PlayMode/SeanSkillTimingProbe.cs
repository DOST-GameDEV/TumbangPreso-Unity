using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SeanSkillTimingProbe
    {
        [UnityTest]
        public IEnumerator ThrowConversionSkillsDoNotSpendAChargeWhileDefending()
        {
            var defender = GameServices.Round.Players.First(p => p.IsDefender);
            defender.Intent.Parked = false;
            defender.Teleport(GameServices.Round.Lata.transform.position + Vector3.back);
            defender.ClearStun(); defender.ClearTrip();
            var context = new AbilityContext(defender, defender.GetComponent<Carrier>(), defender.GetComponent<CombatVerbs>());
            foreach (string hero in new[] { "sean", "zack" })
            foreach (var variant in HeroLoadoutRules.VariantsFor(hero, 2))
            {
                defender.AbilitySystem.BindHero(hero, new HeroBuild { HeroId = hero, Slot2VariantId = variant.Id });
                var kit = defender.AbilitySystem.Kit;
                int charges = kit.Skill2.ChargesRemaining;
                Assert.IsTrue(defender.CanAct(), "The role gate must be tested on an actor who can otherwise act.");
                var answer = kit.CastSkill2(context);
                Debug.Log($"[DefenderThrowConversion] {variant.Id}: {answer}, charges {charges}->{kit.Skill2.ChargesRemaining}");
                Assert.AreEqual(HeroKit.CastOutcome.CannotAct, answer, variant.Id + " armed an unusable defender throw");
                Assert.AreEqual(charges, kit.Skill2.ChargesRemaining, variant.Id + " spent a charge for a role that cannot throw");
                Assert.IsFalse(kit.Skill2.IsActive, variant.Id + " left an unusable charge effect alive");
                yield return null;
            }
        }

        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;
        private CharacterMotor _caster;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(6, .12f, 7 + player.PlayerSlot));
            }
            _caster = GameServices.Round.PlayerAt(1);
            _caster.IsBot = true;
            _caster.Teleport(new Vector3(0, .12f, -8)); _caster.transform.rotation = Quaternion.identity;
            _caster.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "sean");
            _caster.AbilitySystem.BindHero("sean"); _caster.AbilitySystem.Kit.AddUltimateCharge(100);
            _caster.Intent.Parked = false;
            _caster.Intent.AimPoint = new Vector3(0, 1, 7);
            yield return new WaitForSeconds(.2f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator JoiningIgnitionRestoresOnlyItsRemainingWindowWithoutSpending()
        {
            var kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            kit.Skill2.ApplyNetworkSnapshot(0,1);
            float bank=kit.UltimateCharge;
            Assert.IsTrue(kit.RestoreJoiningIgnition(_caster,1.2f));
            yield return null;yield return null;
            Assert.IsTrue(kit.IsIgnitionCannonActive);
            Assert.AreEqual(1,kit.Skill2.ChargesRemaining);Assert.AreEqual(bank,kit.UltimateCharge);
            Assert.IsNotNull(_caster.GetComponent<Carrier>().Held.GetComponentInChildren<Visual.SeanIgnitionVisual>());
            float remaining=kit.Skill2.DurationRemaining;
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,10));
            Assert.AreEqual(remaining,kit.Skill2.DurationRemaining,.001f);
            yield return new WaitForSeconds(1.3f);
            Assert.IsFalse(kit.IsIgnitionCannonActive);
            Assert.IsNull(_caster.GetComponent<Carrier>().Held.GetComponentInChildren<Visual.SeanIgnitionVisual>());
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,10),"A duplicate joining record rearmed an expired charge.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LateJoiningChargeCannotOverwriteAConsumedShotOrANewerCast()
        {
            var shoe=_caster.GetComponent<Carrier>().Held;
            var kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            shoe.ApplySnapshotState(SlipperState.InFlight,null,new Vector3(0,1,-5),Quaternion.identity,
                Vector3.forward*8,0,SlipperAffinity.FireExplosive,_caster.PlayerSlot);
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,5),"A stale initial record rearmed an accepted fire release.");
            Assert.IsFalse(kit.IsIgnitionCannonActive);
            _caster.AbilitySystem.BindHero("sean");kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            Assert.IsTrue(shoe.HostForceEquip(_caster));
            _caster.Intent.Set(Verb.Skill2,true);yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2,false);
            Assert.IsTrue(kit.IsIgnitionCannonActive);
            float remaining=kit.Skill2.DurationRemaining;
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,.5f));
            Assert.AreEqual(remaining,kit.Skill2.DurationRemaining,.001f,"An older record shortened the new cast.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator IgnitionLeavesTheHandWhenTheEmpoweredThrowIsReleased()
        {
            var carrier = _caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            _caster.Intent.Set(Verb.Skill2, true);
            yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2, false);
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            Assert.True(kit.IsIgnitionCannonActive, "The real skill press did not load Ignition.");
            Assert.IsNotNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                "The loaded fire is missing from the actual held slipper.");
            _caster.Intent.Set(Verb.SpecialAbility, true);
            yield return new WaitForSeconds(.5f);
            _caster.Intent.Set(Verb.SpecialAbility, false);
            bool fireFlight = false;
            float start = Time.time;
            while (Time.time - start < .25f)
            {
                fireFlight |= shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.FireExplosive;
                yield return null;
            }
            var stale = _caster.GetComponentsInChildren<ParticleSystem>();
            int liveHandEmitters = 0;
            foreach (var ps in stale)
                if (ps.name == "Vfx_HandAura_FireEmber" && ps.isEmitting) liveHandEmitters++;
            File.WriteAllText("Logs/sean-ignition-release.csv", FormattableString.Invariant(
                $"fire_flight,charged,held,live_hand_emitters\n{fireFlight},{kit.IsIgnitionCannonActive},{carrier.Held != null},{liveHandEmitters}\n"));
            Assert.True(fireFlight, "The actual released slipper never entered empowered flight.");
            Assert.IsNull(carrier.Held);
            Assert.False(kit.IsIgnitionCannonActive);
            Assert.Zero(liveHandEmitters, "Ignition is still emitting on the empty hand after consumption.");
            Assert.IsNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                "The carried ember survived its empowered release.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator AfterburnTradesTravelForAHeatWakeThatLastsLonger()
        {
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var travel = new float[2]; var lives = new float[2];
            try
            {
                challenges.RemoveAll(row => row.VariantId == "sean.1.afterburn");
                challenges.Add(new AbilityChallengeProgress { VariantId = "sean.1.afterburn", Count = 999 });
                for (int variant = 0; variant < 2; variant++)
                {
                    _caster.AbilitySystem.BindHero("sean", new HeroBuild { HeroId = "sean",
                        Slot1VariantId = variant == 0 ? "sean.1.rush" : "sean.1.afterburn" });
                    _caster.Teleport(new Vector3(0, .12f, -8));
                    _caster.Intent.Clear(); _caster.Intent.Parked = false;
                    float start = Time.time, firstHeat = -1, lastHeat = -1;
                    bool accepted = false;
                    while (Time.time - start < 4.8f)
                    {
                        _caster.Intent.Set(Verb.Skill1, Time.time - start < .2f);
                        accepted |= _caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Skill1) == HeroKit.CastOutcome.Cast;
                        travel[variant] = Mathf.Max(travel[variant], _caster.transform.position.z + 8);
                        var fields = Object.FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None);
                        if (fields.Length > 0)
                        {
                            if (firstHeat < 0) firstHeat = Time.time;
                            lastHeat = Time.time;
                            foreach (var field in fields)
                            {
                                Assert.AreEqual(1, field.Radius, .001f, "Afterburn changed the trail's reach.");
                                Assert.IsNotNull(field.GetComponentInChildren<Visual.SeanHeatGround>());
                            }
                        }
                        yield return null;
                    }
                    Assert.True(accepted); Assert.Greater(firstHeat, 0);
                    lives[variant] = lastHeat - firstHeat;
                }
                File.WriteAllText("Logs/sean-afterburn-comparison.csv", FormattableString.Invariant(
                    $"variant,travel,heat_life\nrush,{travel[0]},{lives[0]}\nafterburn,{travel[1]},{lives[1]}\n"));
                Assert.Greater(travel[0], travel[1] + .8f);
                Assert.Greater(lives[1], lives[0] + .6f);
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FlareShotReleasesFasterWithAShorterArmingWindow()
        {
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var speeds = new float[2]; var windows = new float[2];
            var shoe = _caster.GetComponent<Carrier>().Held;
            try
            {
                challenges.RemoveAll(row => row.VariantId == "sean.2.flare");
                challenges.Add(new AbilityChallengeProgress { VariantId = "sean.2.flare", Count = 999 });
                for (int variant = 0; variant < 2; variant++)
                {
                    _caster.AbilitySystem.BindHero("sean", new HeroBuild { HeroId = "sean",
                        Slot2VariantId = variant == 0 ? "sean.2.cannon" : "sean.2.flare" });
                    Assert.True(shoe.HostForceEquip(_caster));
                    _caster.Teleport(new Vector3(0, .12f, -8)); _caster.Intent.Clear(); _caster.Intent.Parked = false;
                    _caster.Intent.AimPoint = new Vector3(0, 1.2f, 7);
                    yield return new WaitForSeconds(.3f);
                    _caster.Intent.Set(Verb.Skill2, true);
                    yield return new WaitForSeconds(.2f);
                    _caster.Intent.Set(Verb.Skill2, false);
                    var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
                    Assert.True(kit.IsIgnitionCannonActive);
                    windows[variant] = kit.Skill2.Duration;
                    _caster.Intent.Set(Verb.SpecialAbility, true);
                    yield return new WaitForSeconds(.5f);
                    _caster.Intent.Set(Verb.SpecialAbility, false);
                    float released = Time.time;
                    while (Time.time - released < .3f)
                    {
                        if (shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.FireExplosive)
                            speeds[variant] = Mathf.Max(speeds[variant], shoe.Velocity.magnitude);
                        yield return null;
                    }
                    Assert.False(kit.IsIgnitionCannonActive);
                    yield return new WaitForSeconds(1.2f);
                }
                File.WriteAllText("Logs/sean-flare-comparison.csv", FormattableString.Invariant(
                    $"variant,flight_speed,arming_window\ncannon,{speeds[0]},{windows[0]}\nflare,{speeds[1]},{windows[1]}\n"));
                Assert.Greater(speeds[0], 8);
                Assert.Greater(speeds[1], speeds[0] * 1.18f);
                Assert.Less(windows[1], windows[0] - 2);
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ReplicatedFireReleaseConsumesOnceAndCleansItsFlightEffect()
        {
            var shoe = _caster.GetComponent<Carrier>().Held;
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            _caster.Intent.Set(Verb.Skill2, true);
            yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2, false);
            Assert.True(kit.IsIgnitionCannonActive);
            Vector3 at = new Vector3(0, 1.2f, -6);
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.FireExplosive, _caster.PlayerSlot);
            Assert.False(kit.IsIgnitionCannonActive, "The accepted remote fire throw retained its carried charge.");
            var flight = shoe.transform.Find("FireSlipperVfx"); Assert.IsNotNull(flight);
            // A repeated flight snapshot cannot consume a subsequently loaded charge.
            kit.IsIgnitionCannonActive = true;
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.FireExplosive, _caster.PlayerSlot);
            Assert.True(kit.IsIgnitionCannonActive);
            Assert.AreSame(flight, shoe.transform.Find("FireSlipperVfx"));
            shoe.ApplySnapshotState(SlipperState.Loose, null, at, Quaternion.identity,
                Vector3.zero, 0, SlipperAffinity.Normal, -1);
            yield return null;
            Assert.IsNull(shoe.transform.Find("FireSlipperVfx"));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator IgnitionCleansUpOnExpiryRefusalAndHeroReplacement()
        {
            var shoe = _caster.GetComponent<Carrier>().Held;
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            var context = new AbilityContext(_caster, _caster.GetComponent<Carrier>(), _caster.GetComponent<CombatVerbs>());
            for (int scenario = 0; scenario < 3; scenario++)
            {
                kit.Skill2.RefillForSandbox();
                _caster.Intent.Set(Verb.Skill2, true);
                yield return new WaitForSeconds(.2f);
                _caster.Intent.Set(Verb.Skill2, false);
                var visual = shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>();
                Assert.IsNotNull(visual, "The loaded visual never appeared.");
                var source = shoe.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>();
                foreach (var part in visual.GetComponentsInChildren<Renderer>())
                    Assert.AreEqual(source.shadowCastingMode, part.shadowCastingMode,
                        "A hidden body slipper exposed its effect in first person.");
                if (scenario == 0) yield return new WaitForSeconds(kit.Skill2.Duration + .1f);
                else if (scenario == 1) kit.Skill2.RollBackPredictedCast(context);
                else _caster.AbilitySystem.BindHero("zack");
                yield return null; yield return null;
                Assert.IsNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                    "The ember survived expiry, refusal or hero replacement: " + scenario);
                // BindHero replaces the kit instance. Inspect the live kit after
                // replacement, not an obsolete instance retained only by this test.
                if (scenario < 2) Assert.False(kit.IsIgnitionCannonActive);
                else Assert.IsInstanceOf<ZackHeroKit>(_caster.AbilitySystem.Kit);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SupernovaWaitsForLandingDuringAnElevatedCast()
        {
            _caster.Teleport(new Vector3(0, 20, -8));
            _caster.Intent.Set(Verb.Ultimate, true);
            float start = Time.time, impactHeight = -1;
            bool impact = false, groundedAtImpact = false, accepted = false;
            while (Time.time - start < 4)
            {
                if (Time.time - start > .2f) _caster.Intent.Set(Verb.Ultimate, false);
                accepted |= _caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate) == HeroKit.CastOutcome.Cast;
                var crater = Object.FindFirstObjectByType<HeroHazards.SupernovaCraterComponent>();
                if (crater != null)
                {
                    impact = true; groundedAtImpact = _caster.IsGrounded; impactHeight = _caster.transform.position.y;
                    break;
                }
                yield return null;
            }
            File.WriteAllText("Logs/sean-supernova-contact.csv", FormattableString.Invariant(
                $"accepted,impact,grounded,height,elapsed\n{accepted},{impact},{groundedAtImpact},{impactHeight},{Time.time-start}\n"));
            Assert.True(accepted, "The elevated real ultimate press was not accepted.");
            Assert.True(impact, "The ultimate never produced its landing impact.");
            Assert.True(groundedAtImpact, "The timeout detonated Supernova while Sean was still airborne.");
        }
    }
}

namespace TumbangPreso.PlayTests
{
    // Small native world for the signature migration; no authored map import.
    public sealed class StokeStepChecks
    {
        private readonly System.Collections.Generic.List<GameObject> _built = new System.Collections.Generic.List<GameObject>();
        private INetProvider _old;
        private CharacterMotor _body;
        private SeanHeroKit _kit;
        private AbilityContext _ctx;
        private static readonly System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _old = NetAuthority.Provider; NetAuthority.Provider = null;
            yield return PlayModeWorld.Reset(); GameServices.Ensure(); GameServices.Round.Clear();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0, -.1f, 0); floor.transform.localScale = new Vector3(30, .2f, 30);
            var go = Track(new GameObject("Stoke native body"));
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = new Vector3(0, .8f, 0);
            _body = go.AddComponent<CharacterMotor>(); _body.enabled = false; _body.PlayerSlot = 1; _body.IsBot = true;
            var system = go.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("sean");
            _kit = (SeanHeroKit)system.Kit; _ctx = new AbilityContext(_body, null, null);
            GameServices.Round.Register(_body); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            _body.Teleport(new Vector3(0, .02f, -3)); _body.Intent.Parked = false;
            Physics.SyncTransforms(); for (int i = 0; i < 12; i++) Step();
            Assert.IsTrue(_body.IsGrounded, "Native controller did not settle on its floor.");
        }
        [UnityTearDown] public IEnumerator After()
        {
            _kit?.Skill1.Reset(); foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear();
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _old;
        }
        private GameObject Track(GameObject go) { _built.Add(go); return go; }
        private void Step() => typeof(CharacterMotor).GetMethod("FixedUpdate", Flags).Invoke(_body, null);
        private Vector3 Impulse => (Vector3)typeof(CharacterMotor).GetField("_externalVelocity", Flags).GetValue(_body);
        private void Cast() { using (NetCue.SuppressRelay()) Assert.AreEqual(HeroKit.CastOutcome.Cast, _kit.CastSkill1(_ctx)); }
        [Test] public void GroundedRefusalAndAnticipationUseRealAcceptedAim()
        {
            _body.transform.forward = Vector3.forward; Cast();
            Assert.AreEqual(30, _kit.Skill1.Cooldown); Assert.IsTrue(_kit.Skill1.IsWindingUp);
            Assert.IsFalse(_body.CanMove()); Assert.IsFalse(_body.CanAct()); Assert.Less(Impulse.magnitude, .001f);
            _body.transform.forward = Vector3.right;
            _kit.Skill1.Tick(_ctx, .17f); Assert.Less(Impulse.magnitude, .001f);
            using (NetCue.SuppressRelay()) _kit.Skill1.Tick(_ctx, .02f);
            Assert.Greater(Impulse.z, 10); Assert.Less(Mathf.Abs(Impulse.x), .001f); Assert.Zero(Impulse.y);
            _kit.Skill1.Reset(); typeof(CharacterMotor).GetField("_grounded", Flags).SetValue(_body, false);
            Assert.IsFalse(_kit.Skill1.CanActivate(_ctx)); Assert.Zero(_kit.Skill1.CooldownRemaining);
        }
        [Test] public void ActualBurstCannotSteerAndRecoversWithoutDamageFields()
        {
            _body.transform.forward = Vector3.forward; Cast();
            using (NetCue.SuppressRelay()) _kit.Skill1.Tick(_ctx, .18f);
            Vector3 start = _body.transform.position; _body.Intent.Move = Vector2.right;
            for (int i = 0; i < 18; i++) { Step(); _kit.Skill1.Tick(_ctx, Time.fixedDeltaTime); }
            Vector3 delta = _body.transform.position - start;
            Assert.That(delta.z, Is.InRange(1.75f, 2.05f)); Assert.Less(Mathf.Abs(delta.x), .015f); Assert.Less(Mathf.Abs(delta.y), .1f);
            Assert.IsFalse(_body.CanAct()); Assert.IsFalse(_body.CanMove());
            Assert.Zero(Object.FindObjectsByType<HeroHazards.FireTrailComponent>().Length);
            for (int i = 0; i < 16; i++) { Step(); _kit.Skill1.Tick(_ctx, Time.fixedDeltaTime); }
            Assert.IsTrue(_body.CanAct()); Assert.IsTrue(_body.CanMove());
        }
        [Test] public void WallBlocksBurstAndWindupTagCannotRelaunch()
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = _body.transform.position + Vector3.forward * 1.1f + Vector3.up;
            wall.transform.localScale = new Vector3(3, 2, .2f); Physics.SyncTransforms();
            _body.transform.forward = Vector3.forward; Cast(); using (NetCue.SuppressRelay()) _kit.Skill1.Tick(_ctx, .18f);
            float start = _body.transform.position.z;
            for (int i = 0; i < 35; i++) { Step(); _kit.Skill1.Tick(_ctx, Time.fixedDeltaTime); }
            Assert.Less(_body.transform.position.z - start, .85f); Assert.IsTrue(_body.CanMove());
            _kit.Skill1.Reset(); Cast(); _body.ApplyTagged(); _kit.Skill1.Tick(_ctx, .3f);
            Assert.IsFalse(_kit.Skill1.IsWindingUp); Assert.IsFalse(_kit.Skill1.IsActive); Assert.Greater(_kit.Skill1.CooldownRemaining, 29);
            _body.ClearStun(); _kit.Skill1.Tick(_ctx, .3f); Assert.IsFalse(_kit.Skill1.IsActive); Assert.IsTrue(_body.CanAct());
        }
        [Test] public void BodyAndConfinementBlockTravelWithoutStagger()
        {
            var other = Track(new GameObject("Stoke obstacle body"));
            var capsule = other.AddComponent<CharacterController>(); capsule.height = 1.6f; capsule.radius = .35f; capsule.center = new Vector3(0, .8f, 0);
            var rival = other.AddComponent<CharacterMotor>(); rival.enabled = false; rival.PlayerSlot = 2;
            rival.Teleport(_body.transform.position + Vector3.forward * 1.1f); GameServices.Round.Register(rival); Physics.SyncTransforms();
            _body.transform.forward = Vector3.forward; Cast(); using (NetCue.SuppressRelay()) _kit.Skill1.Tick(_ctx, .18f);
            float before = _body.transform.position.z;
            for (int i = 0; i < 35; i++) { Step(); _kit.Skill1.Tick(_ctx, Time.fixedDeltaTime); }
            Assert.Less(_body.transform.position.z - before, .7f); Assert.IsFalse(rival.IsStunned);
            other.SetActive(false); _kit.Skill1.Reset(); _body.IsDefender = true; _body.Teleport(new Vector3(0, .02f, 6.6f));
            for (int i = 0; i < 12; i++) Step();
            Cast(); using (NetCue.SuppressRelay()) _kit.Skill1.Tick(_ctx, .18f);
            for (int i = 0; i < 35; i++) { Step(); _kit.Skill1.Tick(_ctx, Time.fixedDeltaTime); }
            Assert.LessOrEqual(_body.transform.position.z, Balance.ConfinementRadius + .01f);
            Assert.IsTrue(_body.CanMove());
        }
        [Test] public void RejectedPredictionAndOtherKitsDoNotKeepCommitmentGates()
        {
            Cast(); _kit.Skill1.RollBackPredictedCast(_ctx);
            Assert.IsTrue(_body.CanAct()); Assert.IsTrue(_body.CanMove()); Assert.Zero(_kit.Skill1.CooldownRemaining);
            foreach(string hero in new[]{"cheska","dante","amihan","phaister","paete","nemu","zack","rafi"})
            {
                _body.AbilitySystem.BindHero(hero);
                Assert.IsFalse(_body.AbilitySystem.Kit.BlocksOwnActions,hero);
                Assert.IsFalse(_body.AbilitySystem.Kit.BlocksOwnLocomotion,hero);
                Assert.IsTrue(_body.CanAct(),hero); Assert.IsTrue(_body.CanMove(),hero);
            }
        }
        [Test] public void AgedRecoveryCannotDuplicateImpulseOrExtendGate()
        {
            var state = new HeroMovementState { Remaining = .2f, Wake = Array.Empty<Vector3>() };
            Assert.IsTrue(_kit.RestoreJoiningMovement(_body, state, .05f));
            Assert.Less(Impulse.magnitude, .001f); Assert.IsFalse(_body.CanAct());
            Assert.IsTrue(_kit.RestoreJoiningMovement(_body, state, 0)); Assert.That(_kit.Skill1.DurationRemaining, Is.EqualTo(.15f).Within(.001));
            _kit.Skill1.Tick(_ctx, .16f); Assert.IsTrue(_body.CanAct());
            Assert.IsFalse(_kit.RestoreJoiningMovement(_body, state, 0));
            _kit.Skill1.Reset(); Assert.IsTrue(_body.CanMove());
            state.UntilNextEmission = .1f; Assert.IsFalse(_kit.RestoreJoiningMovement(_body, state, 0));
        }
    }
}
