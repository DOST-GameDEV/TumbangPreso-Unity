using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ContactTimingRevisionTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly CharacterMotor[] _actors = new CharacterMotor[4];
        private INetProvider _provider;
        private GameObject Make(string name) { var go = new GameObject(name); _owned.Add(go); return go; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _provider = NetAuthority.Provider; NetAuthority.Provider = null;
            GameServices.Ensure(); GameServices.Round.Clear();
            for (int i = 0; i < 4; i++)
            {
                var go = Make("Timing seat " + i); var cc = go.AddComponent<CharacterController>();
                cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                var actor = go.AddComponent<CharacterMotor>(); actor.enabled = false; actor.PlayerSlot = i;
                actor.Mode = GameMode.Classic; actor.IsDefender = i == 0; actor.RoundActive = true;
                go.AddComponent<Carrier>().enabled = false; go.AddComponent<CombatVerbs>().enabled = false;
                actor.transform.position = new Vector3(i * 3, .05f, 0); _actors[i] = actor;
                GameServices.Round.Register(actor);
            }
            GameServices.Round.Lata = Make("Timing can").AddComponent<Lata>();
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(90, true, 0, true); Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in _owned) if (go != null) Object.DestroyImmediate(go); _owned.Clear();
            NetAuthority.Provider = _provider; Hitstop.End(); PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }
        private CombatVerbs Verbs(int slot) => _actors[slot].GetComponent<CombatVerbs>();
        private static void Step(CombatVerbs verbs) => typeof(CombatVerbs).GetMethod("Update", Hidden).Invoke(verbs, null);
        [Test] public void ThrowChargeIsOneAndAHalfSecondsAndLungeRetainsItsRequestedRange()
        {
            Assert.AreEqual(1.5f, Balance.ChargeFullTime);
            Assert.Less(ThrowRules.ChargeRatio(1.25f), 1);
            Assert.AreEqual(1, ThrowRules.ChargeRatio(1.5f));
            Assert.AreEqual(.5f, Balance.LungeChargeTime);
            Assert.AreEqual(.5f, Combat.LungeCooldownFor(0)); Assert.AreEqual(2.5f, Combat.LungeCooldownFor(1));
        }
        [TestCase(true, true)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(false, false)]
        public void PunchRecoveryDependsOnAnActualTag(bool hostCall, bool hit)
        {
            _actors[1].transform.position = new Vector3(0, .05f, hit ? 1 : 5);
            var shoe = Make("Taggable carried slipper").AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = shoe.SeatOfOrigin = 1; shoe.HostForceEquip(_actors[1]); Physics.SyncTransforms();
            Assert.IsTrue(_actors[0].CanAct()); Assert.IsTrue(_actors[1].IsTaggable());
            if (hostCall) Assert.IsTrue(Verbs(0).HostResolvePunch(_actors[0].transform.position, Vector3.forward));
            else { _actors[0].Intent.Set(Verb.SpecialAbility, true); Step(Verbs(0)); }
            Assert.AreEqual(hit ? .25f : .5f, Verbs(0).PunchCooldownLeft, .0001f);
            Assert.AreEqual(hit ? .25f : .5f, Verbs(0).PunchCooldownDuration, .0001f);
            Assert.AreEqual(hit, _actors[1].IsTagged);
        }
        [Test] public void CanDownContactDoesNotEarnTheShortHitRecovery()
        {
            _actors[1].transform.position = new Vector3(0, .05f, 1);
            var shoe = Make("Can-down carried slipper").AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = shoe.SeatOfOrigin = 1; shoe.HostForceEquip(_actors[1]); Physics.SyncTransforms();
            GameServices.Round.Lata.HostKnockDown(-1);
            Assert.IsTrue(Verbs(0).HostResolvePunch(_actors[0].transform.position, Vector3.forward));
            Assert.AreEqual(.5f, Verbs(0).PunchCooldownLeft); Assert.IsFalse(_actors[1].IsTagged);
        }
        [TestCase(true)] [TestCase(false)] public void ShoveRecoveryUsesSevenAndAHalfOnHitAndHalfOnMiss(bool hit)
        {
            _actors[1].transform.position = new Vector3(0, .05f, -4);
            _actors[2].transform.position = new Vector3(0, .05f, hit ? -3 : 6); Physics.SyncTransforms();
            Assert.IsTrue(Verbs(1).HostResolveShove(_actors[1].transform.position, Vector3.forward));
            Assert.AreEqual(hit ? 7.5f : .5f, Verbs(1).ShoveCooldownLeft, .0001f);
        }
        [Test] public void RemovedSlideCannotSpendStaminaMoveOrGrabFromAnyEntry()
        {
            var actor = _actors[1]; actor.transform.position = new Vector3(0, .05f, -4);
            var shoe = Make("Removed slide target").AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = shoe.SeatOfOrigin = 1; shoe.transform.position = actor.transform.position + Vector3.forward * 2;
            Physics.SyncTransforms(); float stamina = actor.Stamina.Current; var position = actor.transform.position;
            Assert.IsFalse(Verbs(1).SlideMayStartFrom(position, Vector3.forward, out var target)); Assert.IsNull(target);
            Assert.IsFalse(Verbs(1).HostResolveSlide(position, Vector3.forward));
            actor.Intent.Set(Verb.Lunge, true); Step(Verbs(1));
            Assert.IsFalse(Verbs(1).SlideActive); Assert.Zero(Verbs(1).SlideCooldownLeft);
            Assert.AreEqual(stamina, actor.Stamina.Current); Assert.AreEqual(position, actor.transform.position);
            Assert.IsNull(actor.GetComponent<Carrier>().Held); Assert.IsFalse(actor.IsCommitted);
            Verbs(1).RollBackRefusedVerb(MatchRpc.DeniedVerb.Slide);
            Assert.AreEqual(stamina, actor.Stamina.Current, "Retired denial cannot grant a stamina refund.");
        }
        [Test] public void HitConfirmationKeepsElapsedTimeAndIsIdempotent()
        {
            var verbs = Verbs(0);
            typeof(CombatVerbs).GetField("_punchCooldown", Hidden).SetValue(verbs, .4f);
            verbs.ConfirmPunchResult(true); Assert.AreEqual(.15f, verbs.PunchCooldownLeft, .0001f);
            verbs.ConfirmPunchResult(true); Assert.AreEqual(.15f, verbs.PunchCooldownLeft, .0001f);
            typeof(CombatVerbs).GetField("_shoveCooldown", Hidden).SetValue(verbs, 7.3f);
            verbs.ConfirmShoveResult(false); Assert.AreEqual(.3f, verbs.ShoveCooldownLeft, .0001f);
            verbs.ConfirmShoveResult(false); Assert.AreEqual(.3f, verbs.ShoveCooldownLeft, .0001f);
        }
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 0; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        [TestCase("hit")] [TestCase("miss")] [TestCase("sender")] [TestCase("scope")]
        [TestCase("truncated")] [TestCase("trailing")] [TestCase("stale")] [TestCase("other-seat")]
        [TestCase("unsupported")] [TestCase("duplicate")] [TestCase("shove-miss")]
        public void ContactRecoveryReceiptsRequireTheCurrentOwnedRequest(string mode)
        {
            var previousRpc = MatchRpc.Instance;
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, null);
            var rpc = Make("Contact recovery router").AddComponent<MatchRpc>(); rpc.enabled = false;
            try
            {
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
                NetAuthority.Provider = new Client();
                var verb = mode == "shove-miss" ? MatchRpc.DeniedVerb.Shove : MatchRpc.DeniedVerb.Punch;
                long request = (long)typeof(MatchRpc).GetMethod("BeginVerbRequest", Hidden).Invoke(rpc, new object[] { 0, verb });
                if (mode == "stale") typeof(MatchRpc).GetMethod("BeginVerbRequest", Hidden).Invoke(rpc, new object[] { 0, verb });
                var scope = (GameplayActionScope)typeof(MatchRpc).GetMethod("CaptureActionScope", Hidden).Invoke(rpc, new object[] { 0 });
                if (mode == "scope") scope.Round++;
                typeof(CombatVerbs).GetField("_punchCooldown", Hidden).SetValue(Verbs(0), .5f);
                typeof(CombatVerbs).GetField("_shoveCooldown", Hidden).SetValue(Verbs(0), 7.5f);
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(mode == "other-seat" ? 1 : 0); writer.WriteValueSafe(request);
                writer.WriteValueSafe((byte)(mode == "unsupported" ? MatchRpc.DeniedVerb.Slide : verb));
                if (mode != "truncated") { writer.WriteValueSafe((byte)(mode == "miss" || mode == "shove-miss" ? 0 : 1)); writer.WriteNetworkSerializable(scope); }
                if (mode == "trailing") writer.WriteValueSafe((byte)99);
                void Receive()
                {
                    using var reader = new FastBufferReader(writer, Allocator.Temp);
                    typeof(MatchRpc).GetMethod("OnContactRecoveryMsg", Hidden).Invoke(rpc,
                        new object[] { mode == "sender" ? 99UL : NetworkManager.ServerClientId, reader });
                }
                Receive();
                Assert.AreEqual(mode == "hit" || mode == "duplicate" ? .25f : .5f, Verbs(0).PunchCooldownLeft, .0001f);
                if (mode == "shove-miss") Assert.AreEqual(.5f, Verbs(0).ShoveCooldownLeft, .0001f);
                if (mode == "duplicate")
                {
                    typeof(CombatVerbs).GetField("_punchCooldown", Hidden).SetValue(Verbs(0), .5f);
                    Receive(); Assert.AreEqual(.5f, Verbs(0).PunchCooldownLeft, "Duplicate result must not touch a later timer.");
                }
            }
            finally { Object.DestroyImmediate(rpc.gameObject); typeof(MatchRpc).GetProperty("Instance").SetValue(null, previousRpc); }
        }

    }
}
