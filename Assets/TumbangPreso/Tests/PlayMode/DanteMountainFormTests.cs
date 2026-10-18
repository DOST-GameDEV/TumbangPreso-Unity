using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// MOUNTAIN FORM, the 30 s after CONTINENTAL DRIFT (`DanteHeroKit`'s note): the clock and its end, the immunity, the x1.25
    /// throw, the stagger and launch on a player hit (both body paths in `Slipper.FixedUpdate`), the mark cleared by a new throw
    /// and a deflection, the round reset, the rejoin restore mid-form and mid-shed, and the clip variant's fallback.
    /// </summary>
    public sealed class DanteMountainFormTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<GameObject> _built = new List<GameObject>();
        CharacterMotor _dante, _victim;
        HeroAbilitySystem _powers;
        DanteHeroKit _kit;
        AbilityContext _ctx;
        INetProvider _provider;

        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1; public int LocalPeerId => 0; public bool IsSeatlessReferee => false; }

        [UnitySetUp] public IEnumerator Before() { _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset(); NetAuthority.Provider = new Solo(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider; }
        [TearDown] public void Cleanup() { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }

        CharacterMotor Body(int slot, Vector3 at, string name)
        {
            var go = Track(new GameObject(name, typeof(CharacterController)));
            var motor = go.AddComponent<CharacterMotor>(); motor.PlayerSlot = slot; motor.Mode = GameMode.HeroStrike; motor.IsBot = false;
            go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>();
            var cc = go.GetComponent<CharacterController>();
            go.transform.position = new Vector3(at.x, -(cc.center.y - cc.height * .5f - cc.skinWidth) + .05f, at.z);
            GameServices.Round.Register(motor); return motor;
        }

        IEnumerator Open(bool dressed = false)
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike)); GameServices.Ensure(); GameServices.Round.Clear();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.localScale = new Vector3(40, 1, 40); floor.transform.position = Vector3.down * .5f;
            var can = Track(new GameObject("Mountain can")); can.transform.position = new Vector3(12, 0, 12); GameServices.Round.Lata = can.AddComponent<Lata>();
            _dante = Body(1, new Vector3(0, 0, -4), "Mountain Dante");
            // 6.5 m in front: past the 0.25 s a throw ignores bodies (4.3 m at this speed), and inside its drop (1.4 m by here).
            _victim = Body(2, new Vector3(0, 0, 2.5f), "Mountain target");
            // Offline, every body that is not this machine's seat is a bot and simulated here; a remote human would take the knock
            // through the network relay instead, which an offline test has none of.
            _victim.IsBot = true;
            if (dressed)
            {
                var art = RosterBook.Load().FindPersonArt("dante");
                _dante.gameObject.AddComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            }
            _powers = _dante.gameObject.AddComponent<HeroAbilitySystem>(); _powers.BindHero("dante"); _powers.enabled = false;
            _kit = (DanteHeroKit)_powers.Kit;
            _ctx = new AbilityContext(_dante, _dante.GetComponent<Carrier>(), _dante.GetComponent<CombatVerbs>());
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound(); Physics.SyncTransforms();
            _dante.IsDefender = false; _victim.IsDefender = false;
            yield return new WaitForSeconds(.2f);
        }

        /// <summary>The ultimate as it is released offline: the windup ends and `OnActivate` begins the form (no cutscene).</summary>
        void Release()
        {
            _kit.Ultimate.Activate(_ctx); _kit.Ultimate.Tick(_ctx, _kit.Ultimate.Windup + .01f);
            Assert.IsTrue(_kit.IsMountainForm, "CONTINENTAL DRIFT leaves him in MOUNTAIN FORM.");
            Assert.AreEqual(GeoRules.MountainSeconds, _kit.MountainLeft, .02f);
        }

        /// <summary>The form on its own, without the cascade (which would knock the target about first).</summary>
        void Begin()
        {
            typeof(DanteHeroKit).GetMethod("BeginMountain", Hidden).Invoke(_kit, new object[] { _dante, false });
            Assert.IsTrue(_kit.IsMountainForm);
        }

        Slipper Equip(CharacterMotor who, string name)
        {
            var shoe = Track(new GameObject(name)).AddComponent<Slipper>(); shoe.OwnerSlot = who.PlayerSlot; shoe.SeatOfOrigin = who.PlayerSlot;
            Assert.IsTrue(shoe.HostForceEquip(who)); return shoe;
        }

        static bool Armed(Slipper shoe) => (bool)typeof(Slipper).GetField("_mountainHit", Hidden).GetValue(shoe);

        [UnityTest] public IEnumerator TheFormLastsThirtySecondsAndThenEnds()
        {
            yield return Open(); Release();
            _kit.Tick(_ctx, GeoRules.MountainSeconds - .1f);
            Assert.IsTrue(_kit.IsMountainForm); Assert.AreEqual(.1f, _kit.MountainLeft, .01f);
            Assert.IsTrue(_powers.IsImmuneToStuns && _powers.IsImmuneToStatuses);
            _kit.Tick(_ctx, .2f);
            Assert.IsFalse(_kit.IsMountainForm); Assert.AreEqual(0f, _kit.MountainLeft);
            Assert.IsFalse(_powers.IsImmuneToStuns, "The immunity ends with the form.");
            _kit.Tick(_ctx, 5f); Assert.AreEqual(0f, _kit.MountainLeft, "The clock does not run on below zero or restart.");
        }

        [UnityTest] public IEnumerator TheFormCleansesAndIsImmuneUntilItEnds()
        {
            yield return Open();
            _dante.ApplyConcussed(); _dante.ApplyStagger(2f); Assert.IsTrue(_dante.IsConcussed && _dante.IsStunned);
            Release();
            Assert.IsFalse(_dante.IsConcussed, "It begins with a cleanse."); Assert.IsFalse(_dante.IsStunned);
            _dante.ApplyStagger(2f); _dante.ApplyConcussed();
            Assert.IsFalse(_dante.IsStunned, "Immune to stuns in the form."); Assert.IsFalse(_dante.IsConcussed, "And to statuses.");
            var v = _dante.Velocity; _dante.ApplyResolvedImpact(Vector3.right * 10f);
            Assert.AreEqual(v, _dante.Velocity, "A knock does not move him either.");
            _kit.Tick(_ctx, GeoRules.MountainSeconds + .1f);
            _dante.ApplyStagger(2f); Assert.IsTrue(_dante.IsStunned, "After the form a stun lands again.");
        }

        [UnityTest] public IEnumerator HisThrowFliesAQuarterFasterOnlyInTheForm()
        {
            yield return Open();
            var carrier = _dante.GetComponent<Carrier>();
            var aim = _dante.transform.position + new Vector3(0f, 1f, 9f);
            var plain = Equip(_dante, "Plain throw");
            carrier.HostThrowAt(_dante.transform.position + Vector3.up, aim, .5f);
            Assert.AreEqual(SlipperState.InFlight, plain.State); Assert.IsFalse(Armed(plain), "A plain throw is not a rock.");
            float plainSpeed = plain.Velocity.magnitude; Object.DestroyImmediate(plain.gameObject);
            yield return new WaitForSeconds(.6f);
            Begin();
            var rock = Equip(_dante, "Rock throw");
            carrier.HostThrowAt(_dante.transform.position + Vector3.up, aim, .5f);
            Assert.AreEqual(SlipperState.InFlight, rock.State);
            Assert.AreEqual(plainSpeed * GeoRules.MountainThrowScale, rock.Velocity.magnitude, plainSpeed * .01f, "x1.25 in MOUNTAIN FORM.");
            Assert.IsTrue(Armed(rock), "And it carries the rock's hit.");
        }

        IEnumerator ThrowAt(CharacterMotor target, bool defender)
        {
            target.IsDefender = defender;
            Begin();
            var rock = Equip(_dante, defender ? "Rock at taya" : "Rock at player");
            _dante.GetComponent<Carrier>().HostThrowAt(_dante.transform.position + Vector3.up, _dante.transform.position + new Vector3(0f, 1f, 8f), .6f);
            // ⚠️ THE TARGET STANDS WHERE THIS FLIGHT COMES DOWN TO ITS CHEST, measured from the flight itself (`Balance.Gravity`),
            // rather than at a guessed distance: a throw's arc depends on its charge and the aim solve, and a body inside the
            // first `Balance.ThrowerIgnoreTime` is passed without a test.
            var v0 = rock.Velocity; var p0 = rock.transform.position; float chest = target.transform.position.y;
            float drop = .5f * Balance.Gravity, t = (v0.y + Mathf.Sqrt(v0.y * v0.y + 4f * drop * (p0.y - chest))) / (2f * drop);
            Assert.Greater(t, Balance.ThrowerIgnoreTime + .05f);
            var landing = new Vector3(p0.x + v0.x * t, chest, p0.z + v0.z * t);
            target.Teleport(landing); Physics.SyncTransforms();
            // The taya is held inside the box round the can (`CLAUDE.md` section 4): their motor is paused where they wait, so
            // the box does not pull them off the rock's path (and the can stays out of it). The block and the stagger are the
            // host's, on the body, and do not need the motor to run.
            if (defender) target.enabled = false;
            float nearest = float.MaxValue; var start = rock.transform.position; var speed = rock.Velocity;
            for (int i = 0; i < 90 && Armed(rock); i++)
            {
                yield return new WaitForFixedUpdate();
                nearest = Mathf.Min(nearest, Vector3.Distance(rock.transform.position, target.transform.position + Vector3.up * .8f));
            }
            Assert.IsFalse(Armed(rock), $"The rock reached the body (from {start} at {speed}, nearest {nearest:F2} m, now {rock.State} at {rock.transform.position}).");
        }

        [UnityTest] public IEnumerator ARockStaggersAndLaunchesThePlayerItHits()
        {
            yield return Open(); _victim.enabled = true;
            yield return ThrowAt(_victim, false);
            Assert.IsTrue(_victim.IsStunned, "Staggered."); Assert.LessOrEqual(_victim.StunLeft, GeoRules.MountainHitStun + .01f);
            var hitAt = _victim.transform.position; float top = hitAt.y;
            for (float s = 0f; s < .3f; s += Time.deltaTime) { yield return null; top = Mathf.Max(top, _victim.transform.position.y); }
            Assert.Greater(_victim.transform.position.z - hitAt.z, .8f, "Knocked away along the throw.");
            Assert.Greater(top - hitAt.y, .15f, "And lifted off the court.");
        }

        [UnityTest] public IEnumerator ARockTheTayaBlocksStillStaggersAndLaunchesThem()
        {
            yield return Open(); _victim.enabled = true;
            yield return ThrowAt(_victim, true);
            Assert.IsTrue(_victim.IsStunned, "The taya's body block takes the rock's hit too.");
        }

        [UnityTest] public IEnumerator AnImmuneBodyRefusesTheRock()
        {
            yield return Open(); _victim.enabled = true;
            var other = _victim.gameObject.AddComponent<HeroAbilitySystem>(); other.BindHero("dante"); other.enabled = false;
            var otherCtx = new AbilityContext(_victim, _victim.GetComponent<Carrier>(), _victim.GetComponent<CombatVerbs>());
            Assert.IsTrue(other.Kit.TryActivateSkill1(otherCtx)); Assert.IsTrue(other.IsImmuneToStuns);
            yield return ThrowAt(_victim, false);
            var hitAt = _victim.transform.position;
            for (float s = 0f; s < .3f; s += Time.deltaTime) yield return null;
            Assert.IsFalse(_victim.IsStunned); Assert.Less(Vector3.Distance(_victim.transform.position, hitAt), .3f, "Neither the stagger nor the launch.");
        }

        [UnityTest] public IEnumerator ANewThrowAndADeflectionClearTheMark()
        {
            yield return Open(); Begin();
            var carrier = _dante.GetComponent<Carrier>();
            var rock = Equip(_dante, "Marked rock");
            carrier.HostThrowAt(_dante.transform.position + Vector3.up, _dante.transform.position + new Vector3(4f, 1f, 9f), .5f);
            Assert.IsTrue(Armed(rock));
            rock.Deflect(Vector3.left * 4f, 1f);
            Assert.IsFalse(Armed(rock), "A deflected rock carries his hit back at nobody.");
            rock.HostArmMountainHit(); Assert.IsTrue(Armed(rock));
            rock.HostThrow(null, rock.transform.position, Vector3.forward * 6f);
            Assert.IsFalse(Armed(rock), "Any new throw starts unmarked (an environmental one here).");
        }

        [UnityTest] public IEnumerator TheRoundResetTakesTheArmourAndHisClipsOff()
        {
            yield return Open(dressed: true); Release();
            var animator = _dante.GetComponentInChildren<CharacterAnimator>();
            Assert.IsNotNull(Object.FindAnyObjectByType<DanteMountainArmour>(), "The suit is on him.");
            Assert.AreEqual(CharacterAnimator.MountainVariant, animator.ClipVariant);
            _kit.ResetForRound(_ctx); yield return null;
            Assert.IsFalse(_kit.IsMountainForm);
            Assert.IsNull(Object.FindAnyObjectByType<DanteMountainArmour>(), "No armour carried into a round.");
            Assert.IsNull(Object.FindAnyObjectByType<DanteWardBadge>(), "Nor its countdown.");
            Assert.IsNull(animator.ClipVariant, "Nor the armoured walk.");
        }

        [UnityTest] public IEnumerator TheArmourIsGoneOnceItHasShed()
        {
            yield return Open(dressed: true); Release();
            var animator = _dante.GetComponentInChildren<CharacterAnimator>();
            _kit.Tick(_ctx, GeoRules.MountainSeconds + .05f);
            Assert.IsNull(animator.ClipVariant, "His own clips are back as the form ends.");
            Assert.IsNotNull(Object.FindAnyObjectByType<DanteMountainArmour>(), "The last stones are still settling.");
            _kit.Tick(_ctx, DanteMountainArmour.DebrisSeconds); yield return null;
            Assert.IsNull(Object.FindAnyObjectByType<DanteMountainArmour>(), "Gone with its debris.");
        }

        [UnityTest] public IEnumerator AModelRebuiltMidFormIsDressedAgain()
        {
            yield return Open(dressed: true); Release();
            _kit.Tick(_ctx, 5f);
            var first = Object.FindAnyObjectByType<DanteMountainArmour>(); Assert.IsNotNull(first);
            var art = RosterBook.Load().FindPersonArt("dante");
            // A changed pick (here the tint) rebuilds the model; an identical one is refused as a repeat.
            _dante.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint * .98f, art.Clips, art.Palette, art.PetModel);
            yield return null;
            Assert.IsTrue(first.Orphaned, "The old bones, and every stone on them, went with the old model.");
            _kit.Tick(_ctx, .1f); yield return null;
            var second = Object.FindAnyObjectByType<DanteMountainArmour>();
            Assert.IsNotNull(second); Assert.AreNotSame(first, second); Assert.IsFalse(second.Orphaned);
            Assert.AreEqual(5.1f, second.Age, .05f, "On the same clock, not a fresh suit forming.");
            Assert.AreEqual(CharacterAnimator.MountainVariant, _dante.GetComponentInChildren<CharacterAnimator>().ClipVariant);
        }

        [UnityTest] public IEnumerator ARejoinRestoresTheFormMidwayAndMidShed()
        {
            yield return Open(dressed: true); Release();
            _kit.Tick(_ctx, 10f);
            var midway = _kit.CaptureTimedKit();
            Assert.AreEqual(20f, midway.PassiveRemaining, .02f);
            _kit.Tick(_ctx, 15f);
            var shedding = _kit.CaptureTimedKit();
            Assert.AreEqual(5f, shedding.PassiveRemaining, .02f);

            _kit.ResetForRound(_ctx); yield return null;
            Assert.IsTrue(_kit.RestoreTimedKit(_dante, midway));
            Assert.AreEqual(20f, _kit.MountainLeft, .02f); Assert.IsTrue(_powers.IsImmuneToStuns);
            var armour = Object.FindAnyObjectByType<DanteMountainArmour>(); Assert.IsNotNull(armour);
            Assert.AreEqual(DanteMountainArmour.CutEndAt + 10f, armour.Age, .05f, "Joined at the suit as it is now, formed.");
            Assert.AreEqual(CharacterAnimator.MountainVariant, _dante.GetComponentInChildren<CharacterAnimator>().ClipVariant);
            Assert.IsFalse(_kit.RestoreTimedKit(_dante, midway), "The same state twice changes nothing.");

            Assert.IsTrue(_kit.RestoreTimedKit(_dante, shedding)); yield return null;
            armour = Object.FindAnyObjectByType<DanteMountainArmour>(); Assert.IsNotNull(armour);
            Assert.AreEqual(5f, _kit.MountainLeft, .02f);
            Assert.Greater(armour.Age, armour.EndAt - DanteMountainArmour.ShedSeconds, "Joined part way through the shedding.");
            Assert.IsNotNull(GameObject.Find("DanteMountainDebris"), "What had already come off lies on the court.");

            var over = new TimedKitSnapshot(_kit.Skill1, 0, passiveRemaining: 0f, passiveCapacity: GeoRules.MountainSeconds);
            Assert.IsTrue(_kit.RestoreTimedKit(_dante, over)); yield return null;
            Assert.IsFalse(_kit.IsMountainForm); Assert.IsNull(Object.FindAnyObjectByType<DanteMountainArmour>());
        }

        [UnityTest] public IEnumerator TheVariantFallsBackToThePlainClipWhereItHasNone()
        {
            yield return Open(dressed: true);
            var animator = _dante.GetComponentInChildren<CharacterAnimator>();
            var playing = typeof(CharacterAnimator).GetField("_playing", Hidden);
            animator.ClipVariant = CharacterAnimator.MountainVariant;
            animator.PlayOneShot("jump");
            Assert.AreEqual("mountain-jump", playing.GetValue(animator), "A clip with a mountain variant plays it.");
            Assert.AreEqual("jump", animator.CurrentClipName, "Under its plain name.");
            animator.PlayOneShot("pick-up");
            Assert.AreEqual("pick-up", playing.GetValue(animator), "A clip without one plays itself rather than nothing.");
            animator.ClipVariant = null;
            animator.PlayOneShot("jump");
            Assert.AreEqual("jump", playing.GetValue(animator));
        }
    }
}
