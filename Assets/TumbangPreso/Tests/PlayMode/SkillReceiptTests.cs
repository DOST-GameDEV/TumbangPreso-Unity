using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SkillReceiptTests
    {
        private sealed class AimProbeAbility : HeroAbility
        {
            public int Activations, PrivatePresents, BodyPresents, BodyEnds;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public AimProbeAbility(string id) : base(id, id, "", 1) => AimByHolding(1, 3);
            protected override void OnActivate(AbilityContext context) => Activations++;
            public override void PresentAim(CharacterMotor caster, Vector3 at, float held) => PrivatePresents++;
            public override void PresentAimBody(CharacterMotor caster, float held) => BodyPresents++;
            public override void EndAimBody() => BodyEnds++;
        }
        private sealed class AimProbeKit : HeroKit
        {
            public AimProbeKit() : base("aim-probe", "aim-probe")
            { Skill1 = new AimProbeAbility("aim-one"); Skill2 = new AimProbeAbility("aim-two"); }
        }

        private sealed class ResourceProbeAbility : HeroAbility
        {
            public int Activations;
            public override AbilityNetworkMode NetworkMode => Id == "ultimate"
                ? AbilityNetworkMode.SharedUltimate : AbilityNetworkMode.Predicted;
            public ResourceProbeAbility(string id) : base(id, id, "", 30, charges: 3) { }
            protected override void OnActivate(AbilityContext context) { Activations++; }
        }
        private sealed class ResourceProbeKit : HeroKit
        {
            public ResourceProbeKit(string hero = "resource-probe") : base(hero, "Resources")
            {
                Skill1 = new ResourceProbeAbility("signature");
                AttackingSkill = new ResourceProbeAbility("attack");
                DefendingSkill = new ResourceProbeAbility("defend");
                Ultimate = new ResourceProbeAbility("ultimate");
            }
        }

        private sealed class UltimateLifetimeProbe : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            public int Activations, Bindings;
            public long PhaseAtActivation;
            public UltimateLifetimeProbe(bool delayed) : base("phase-probe", "Phase", "", 0)
            { Windup = delayed ? .1f : 0; }
            protected override void OnActivate(AbilityContext context)
            { Activations++; PhaseAtActivation = AcceptedUltimatePhase; }
            protected override void OnAcceptedUltimatePhase(long phaseId) { Bindings++; }
        }

        private sealed class TimedProbeAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public TimedProbeAbility(string id) : base(id, "Presentation label", "", 30, 20)
            { SupportsPendingSnapshot = true; }
            protected override void OnActivate(AbilityContext context) { Assert.Fail("Recovery cast an ability."); }
        }

        private sealed class TimedProbeKit : HeroKit, ITimedKitReplication
        {
            public int Restorations;
            public bool Accept = true;
            public TimedKitSnapshot Last;
            public TimedProbeKit(string personal = "personal") : base("timed-probe", "Presentation name")
            {
                Skill1 = new TimedProbeAbility("signature");
                AttackingSkill = new TimedProbeAbility(personal);
                DefendingSkill = new TimedProbeAbility("defending");
                Ultimate = new TimedProbeAbility("ultimate");
            }
            public TimedKitSnapshot CaptureTimedKit() => new TimedKitSnapshot(AttackingSkill, 5, Ultimate, 10);
            public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
            { Restorations++; Last = state; return Accept; }
        }

        private INetProvider _provider;
        private sealed class PredictingOwner : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private sealed class ObservingHost : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        [UnitySetUp] public IEnumerator Before()
        { _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider; }

        [Test]
        public void FamiliarEffectCodecBoundsTheWorldAndAcceptedAbilityIdentity()
        {
            var kit = new NemuHeroKit();
            var state = new FamiliarEffectState
            {
                Seat = 1, Scope = new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }, Phase = 4,
                HeroId = new FixedString64Bytes(kit.HeroId), AbilityId = new FixedString64Bytes(kit.Ultimate.Id),
                Position = new Vector3(0, 0, 2), RoundClock = 100, Remaining = 4, Yaw = 90,
            };
            byte[] bytes;
            using (var writer = new FastBufferWriter(FamiliarEffectState.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(state); bytes = writer.ToArray();
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try
                {
                    Assert.IsTrue(FamiliarEffectState.TryRead(ref reader, out var decoded));
                    Assert.IsTrue(decoded.MatchesKit(kit)); Assert.AreEqual(state.Phase, decoded.Phase);
                    Assert.AreEqual(state.Scope.Epoch, decoded.Scope.Epoch); Assert.AreEqual(state.Position, decoded.Position);
                }
                finally { reader.Dispose(); }
            }
            bool Reads(byte[] payload)
            {
                var reader = new FastBufferReader(payload, Allocator.Temp);
                try { return FamiliarEffectState.TryRead(ref reader, out _); }
                finally { reader.Dispose(); }
            }
            var oversized = (byte[])bytes.Clone(); oversized[28] = 62; oversized[29] = 0;
            Assert.IsFalse(Reads(oversized));
            var truncated = new byte[bytes.Length - 1]; System.Array.Copy(bytes, truncated, truncated.Length);
            Assert.IsFalse(Reads(truncated));
            var trailing = new byte[bytes.Length + 1]; System.Array.Copy(bytes, trailing, bytes.Length);
            Assert.IsFalse(Reads(trailing));
            Assert.IsTrue(state.TryAge(100, kit.Ultimate.Duration, out float held)); Assert.AreEqual(4, held);
            Assert.IsTrue(state.TryAge(98, kit.Ultimate.Duration, out float aged)); Assert.AreEqual(2, aged);
            Assert.IsTrue(state.TryAge(101, kit.Ultimate.Duration, out float ahead)); Assert.AreEqual(4, ahead);
            Assert.IsFalse(state.TryAge(-1, kit.Ultimate.Duration, out _));
            var bad = state; bad.RoundClock = float.NaN; Assert.IsFalse(bad.IsValid);
            bad = state; bad.Remaining = 999; Assert.IsFalse(bad.TryAge(100, kit.Ultimate.Duration, out _));
            bad = state; bad.Position.x = float.PositiveInfinity; Assert.IsFalse(bad.IsValid);
            bad = state; bad.Phase = 0; Assert.IsFalse(bad.IsValid);
            bad = state; bad.HeroId = new FixedString64Bytes("other"); Assert.IsFalse(bad.MatchesKit(kit));
            bad = state; bad.AbilityId = new FixedString64Bytes("other"); Assert.IsFalse(bad.MatchesKit(kit));
            state.HeroId = new FixedString64Bytes(new string('h', 61));
            state.AbilityId = new FixedString64Bytes(new string('a', 61));
            using (var writer = new FastBufferWriter(FamiliarEffectState.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(state); Assert.AreEqual(FamiliarEffectState.MaxWireBytes, writer.Length);
                Assert.IsTrue(Reads(writer.ToArray()));
            }
        }

        [Test]
        public void FamiliarRecoveryMovesAnActiveHauntAndCannotResurrectItsCompletedLifetime()
        {
            var system = Owner("nemu"); var body = system.GetComponent<CharacterMotor>();
            var kit = (NemuHeroKit)system.Kit;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            var visual = body.gameObject.AddComponent<Visual.CharacterVisual>(); visual.enabled = false;
            var art = RosterBook.Load().FindPersonArt("nemu"); Assert.IsNotNull(art.PetModel);
            var petRoot = Object.Instantiate(art.PetModel);
            var pet = petRoot.GetComponent<Visual.GhostPetCompanion>() ?? petRoot.AddComponent<Visual.GhostPetCompanion>();
            pet.Bind(body.transform); pet.enabled = false;
            var root = new GameObject("Familiar recovery"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var apply = typeof(MatchRpc).GetMethod("ApplyFamiliarEffect", hidden);
            bool Apply(FamiliarEffectState value) => (bool)apply.Invoke(router, new object[] { value, 100f });
            var state = new FamiliarEffectState
            {
                Seat = 1, Scope = new GameplayActionScope { Match = 123, Round = 1, Epoch = body.MovementEpoch }, Phase = 4,
                HeroId = new FixedString64Bytes(kit.HeroId), AbilityId = new FixedString64Bytes(kit.Ultimate.Id),
                Position = new Vector3(0, 0, 2), RoundClock = 100, Remaining = 4, Yaw = 90,
            };
            try
            {
                Assert.IsFalse(Apply(state), "A missing companion must not consume the lifetime.");
                typeof(Visual.CharacterVisual).GetProperty("Companion").SetValue(visual, pet);
                var bad = state; bad.Scope.Match = 122; Assert.IsFalse(Apply(bad));
                bad.Scope.Match = 123; bad.Scope.Round = 2; Assert.IsFalse(Apply(bad));
                bad.Scope.Round = 1; bad.Scope.Epoch++; Assert.IsFalse(Apply(bad));
                bad = state; bad.HeroId = new FixedString64Bytes("other"); Assert.IsFalse(Apply(bad));
                bad = state; bad.AbilityId = new FixedString64Bytes("other"); Assert.IsFalse(Apply(bad));
                bad = state; bad.Remaining = kit.Ultimate.Duration + 1; Assert.IsFalse(Apply(bad));
                GameServices.Round.ApplySnapshot(100, false, 0, true);
                Assert.IsFalse(Apply(state), "A non-live round must not restore the effect.");
                GameServices.Round.ApplySnapshot(100, true, 0, true);
                Assert.IsEmpty(Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None));
                kit.AddUltimateCharge(3); float meter = kit.UltimateCharge;
                Assert.IsTrue(Apply(state)); Assert.IsTrue(pet.IsDevouring);
                Assert.AreEqual(4, kit.Ultimate.AcceptedUltimatePhase); Assert.AreEqual(4, kit.Ultimate.DurationRemaining, .001f);
                Assert.AreEqual(meter, kit.UltimateCharge);
                Assert.IsEmpty(Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None));
                typeof(HeroAbility).GetProperty("DurationRemaining").SetValue(kit.Skill2, 2f);
                Assert.IsFalse(Apply(state)); Assert.AreEqual(2, kit.Skill2.DurationRemaining);
                var moved = state; moved.RoundClock = 99; moved.Position = new Vector3(2, 0, 2); moved.Remaining = 3;
                Assert.IsTrue(Apply(moved)); Assert.AreEqual(moved.Position.x, pet.DevourGround.x, .001f);
                Assert.AreEqual(3, kit.Ultimate.DurationRemaining, .001f);
                Assert.IsFalse(Apply(state), "An older movement snapshot rewound the chase.");
                moved.RoundClock = 98; moved.Remaining = 0;
                Assert.IsTrue(Apply(moved)); Assert.IsFalse(pet.IsDevouring);
                Assert.IsFalse(Apply(state), "A terminal lifetime was resurrected by delayed movement.");
                bad = state; bad.Phase = 3; Assert.IsFalse(Apply(bad));
                var context = new AbilityContext(body, body.GetComponent<Carrier>(), body.GetComponent<CombatVerbs>());
                kit.Ultimate.EndEarly(context); pet.StopDevouring();
                Assert.IsFalse(Apply(state), "A completed lifetime was resurrected.");
                Assert.IsFalse(pet.IsDevouring); Assert.AreEqual(0, kit.Ultimate.DurationRemaining);
                Assert.AreEqual(2, kit.Skill2.DurationRemaining);
                typeof(HeroAbility).GetProperty("Windup").SetValue(kit.Ultimate, .4f);
                using (NetCue.SuppressRelay()) kit.Ultimate.Activate(context);
                kit.Ultimate.AdoptUltimatePhase(5);
                Assert.IsTrue(kit.Ultimate.IsWindingUp);
                var closedDuringWindup = state; closedDuringWindup.Phase = 5;
                closedDuringWindup.Remaining = 0; closedDuringWindup.RoundClock = 97;
                Assert.IsTrue(Apply(closedDuringWindup));
                Assert.IsFalse(kit.Ultimate.IsWindingUp);
                using (NetCue.SuppressRelay()) kit.Ultimate.Tick(context, 1);
                Assert.IsFalse(kit.Ultimate.IsActive, "Delayed activation restarted a terminal lifetime.");
                Assert.IsFalse(pet.IsDevouring);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(petRoot); }
        }

        [Test]
        public void TimedRecoveryCodecBoundsIdentitiesAndAgesTheOwningChannels()
        {
            var kit = new TimedProbeKit();
            var state = TimedKitState.Capture(kit, kit.CaptureTimedKit(), 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }, 1, 100);
            byte[] bytes;
            using (var writer = new FastBufferWriter(TimedKitState.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(state); bytes = writer.ToArray();
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsTrue(TimedKitState.TryRead(ref reader, out state)); }
                finally { reader.Dispose(); }
            }
            kit.SetRole(true, default);
            Assert.IsTrue(state.TryResolve(kit, 98, out var aged));
            Assert.AreSame(kit.AttackingSkill, aged.PersonalAbility);
            Assert.AreEqual(3, aged.PersonalRemaining); Assert.AreEqual(8, aged.UltimateRemaining);
            Assert.IsTrue(state.TryResolve(kit, 100, out var held));
            Assert.AreEqual(5, held.PersonalRemaining); Assert.AreEqual(10, held.UltimateRemaining);
            Assert.IsTrue(state.TryResolve(kit, 101, out var ahead)); Assert.AreEqual(5, ahead.PersonalRemaining);
            Assert.IsFalse(state.TryResolve(kit, -1, out _));
            Assert.IsFalse(state.TryResolve(new TimedProbeKit("replacement"), 98, out _));
            var invalid = state; invalid.PersonalRemaining = float.NaN; Assert.IsFalse(invalid.IsValid);
            invalid = state; invalid.RoundClock = float.PositiveInfinity; Assert.IsFalse(invalid.IsValid);
            invalid = state; invalid.UltimateId = default; Assert.IsFalse(invalid.IsValid);
            invalid = state; invalid.PersonalRemaining = 999; Assert.IsFalse(invalid.TryResolve(kit, 98, out _));

            bool Reads(byte[] payload)
            {
                var reader = new FastBufferReader(payload, Allocator.Temp);
                try { return TimedKitState.TryRead(ref reader, out _); }
                finally { reader.Dispose(); }
            }
            var oversized = (byte[])bytes.Clone(); oversized[28] = 62; oversized[29] = 0;
            Assert.IsFalse(Reads(oversized));
            var truncated = new byte[bytes.Length - 1]; System.Array.Copy(bytes, truncated, truncated.Length);
            Assert.IsFalse(Reads(truncated));
            var trailing = new byte[bytes.Length + 1]; System.Array.Copy(bytes, trailing, bytes.Length);
            Assert.IsFalse(Reads(trailing));
            var invalidBoolean = (byte[])bytes.Clone(); invalidBoolean[invalidBoolean.Length - 5] = 2; // Permanent flag, before the passive float.
            Assert.IsFalse(Reads(invalidBoolean));
            var maximum = state;
            maximum.HeroId = new FixedString64Bytes(new string('h', 61));
            maximum.PersonalId = new FixedString64Bytes(new string('p', 61));
            maximum.UltimateId = new FixedString64Bytes(new string('u', 61));
            using (var writer = new FastBufferWriter(TimedKitState.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(maximum);
                Assert.AreEqual(TimedKitState.MaxWireBytes, writer.Length);
                Assert.IsTrue(Reads(writer.ToArray()));
            }
        }

        [Test]
        public void TimedRecoveryRejectsOldScopesWrongBindingsAndReplayedNoOps()
        {
            var system = Owner("dante"); var body = system.GetComponent<CharacterMotor>();
            var kit = new TimedProbeKit(); typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, kit);
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(98, true, 0, true);
            var root = new GameObject("Timed recovery receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var apply = typeof(MatchRpc).GetMethod("ApplyTimedKitState", hidden);
            bool Apply(TimedKitState value) => (bool)apply.Invoke(router, new object[] { value, 98f });
            var state = TimedKitState.Capture(kit, kit.CaptureTimedKit(), 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = body.MovementEpoch }, 1, 100);
            try
            {
                var bad = state; bad.Sequence = 100; bad.Scope.Match = 122; Assert.IsFalse(Apply(bad));
                bad.Scope.Match = 123; bad.Scope.Round = 2; Assert.IsFalse(Apply(bad));
                bad.Scope.Round = 1; bad.Scope.Epoch++; Assert.IsFalse(Apply(bad));
                bad.Scope = state.Scope; bad.HeroId = new FixedString64Bytes("other"); Assert.IsFalse(Apply(bad));
                bad.HeroId = state.HeroId; bad.PersonalId = new FixedString64Bytes("defending"); Assert.IsFalse(Apply(bad));
                Assert.AreEqual(0, kit.Restorations);
                Assert.IsTrue(Apply(state)); Assert.AreEqual(1, kit.Restorations);
                Assert.AreEqual(3, kit.Last.PersonalRemaining); Assert.AreEqual(8, kit.Last.UltimateRemaining);
                Assert.IsFalse(Apply(state)); Assert.AreEqual(1, kit.Restorations);
                kit.Accept = false; state.Sequence = 3;
                Assert.IsTrue(Apply(state)); Assert.AreEqual(2, kit.Restorations);
                state.Sequence = 2; Assert.IsFalse(Apply(state));
                body.AdoptMovementEpoch(body.MovementEpoch + 1);
                state.Sequence = 4; Assert.IsFalse(Apply(state));
                state.Scope.Epoch = body.MovementEpoch; Assert.IsTrue(Apply(state));
                typeof(MatchRpc).GetMethod("ResetTimedKitTransport", hidden).Invoke(router, null);
                state.Sequence = 1; Assert.IsTrue(Apply(state));
                Assert.AreEqual(4, kit.Restorations);
                GameServices.Round.ApplySnapshot(98, false, 0, true);
                state.Sequence = 2; Assert.IsFalse(Apply(state));
                state.PersonalRemaining = 0; state.UltimateRemaining = 0;
                Assert.IsTrue(Apply(state), "Inactive rounds still accept authoritative empty hydration.");
                Assert.AreEqual(5, kit.Restorations);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void FreshCheskaRecoveryRestoresFrostbiteWithoutRecastingOrRefundingResources()
        {
            var system = Owner("cheska"); var body = system.GetComponent<CharacterMotor>();
            var kit = (CheskaHeroKit)system.Kit;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(98, true, 0, true);
            kit.AttackingSkill.ApplyNetworkSnapshot(30, 0, true);
            var root = new GameObject("Frostbite recovery receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var apply = typeof(MatchRpc).GetMethod("ApplyTimedKitState", hidden);
            bool Apply(TimedKitState value) => (bool)apply.Invoke(router, new object[] { value, 98f });
            var state = TimedKitState.Capture(kit, new TimedKitSnapshot(kit.AttackingSkill, 7), 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = body.MovementEpoch }, 1, 100);
            try
            {
                Assert.IsTrue(state.IsValid);
                Assert.IsTrue(Apply(state), "A valid returning Cheska cannot bind her live Frostbite state.");
                Assert.IsTrue(kit.IsFrostbiteLoaded);
                Assert.AreEqual(5, kit.AttackingSkill.DurationRemaining, .001f);
                Assert.AreEqual(30, kit.AttackingSkill.CooldownRemaining, .001f);
                Assert.AreEqual(0, kit.UltimateCharge);
                Assert.IsFalse(Apply(state), "The same recovery sequence must not refresh the load.");
                kit.CancelFrostbiteWindow(); kit.Tick(new AbilityContext(body, null, null), .01f);
                state.Sequence = 2; state.PersonalRemaining = 8;
                Assert.IsTrue(Apply(state), "A valid no-op should retire the new sequence.");
                Assert.IsFalse(kit.IsFrostbiteLoaded, "Late hydration resurrected an already-spent load.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void FrostbiteRecoveryRejectsInvalidLoadsWithoutPoisoningTheFirstValidState()
        {
            var system = Owner("cheska"); var body = system.GetComponent<CharacterMotor>();
            var kit = (CheskaHeroKit)system.Kit;
            var replication = (ITimedKitReplication)kit;
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f, CryoRules.FrostbiteLoadSeconds + 1 })
                Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, invalid)));
            Assert.IsFalse(replication.RestoreTimedKit(null, new TimedKitSnapshot(kit.AttackingSkill, 5)));
            body.IsDefender = true;
            Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 5)));
            body.IsDefender = false;
            Assert.IsTrue(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 5)));
            Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 8)));
            Assert.AreEqual(5, kit.AttackingSkill.DurationRemaining);
            Assert.AreSame(kit.AttackingSkill, replication.CaptureTimedKit().PersonalAbility);
            Assert.AreEqual(5, replication.CaptureTimedKit().PersonalRemaining);
        }

        [Test] public void FrostbiteRecoveryExpiresAndResetsWithoutGrantingASecondLoad()
        {
            var system = Owner("cheska"); var body = system.GetComponent<CharacterMotor>();
            var kit = (CheskaHeroKit)system.Kit; var replication = (ITimedKitReplication)kit;
            var ctx = new AbilityContext(body, null, null);
            Assert.IsTrue(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 2)));
            kit.Tick(ctx, 2.1f);
            Assert.IsFalse(kit.IsFrostbiteLoaded); Assert.IsFalse(kit.AttackingSkill.IsActive);
            Assert.AreEqual(0, replication.CaptureTimedKit().PersonalRemaining);
            Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 7)));
            kit.ResetForRound(ctx);
            Assert.IsTrue(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 3)));
            kit.Reset(); Assert.IsFalse(kit.IsFrostbiteLoaded);
            Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 0)));
            Assert.IsFalse(replication.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 5)), "Authoritative empty state must close late hydration.");
        }

        [Test] public void AmpedUpUsesTheRealObjectiveAwardWithoutDiscountingPracticeRefills()
        {
            var system = Owner("zack"); var body = system.GetComponent<CharacterMotor>();
            NetAuthority.Provider = new ObservingHost();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            var kit = system.Kit;
            kit.Skill1.ApplyNetworkSnapshot(20, 0, true);
            system.OnLataKnocked();
            Assert.AreEqual(15, kit.Skill1.CooldownRemaining, .001f, "The awarded objective point never reached Amped-Up.");
            Assert.AreEqual(1, kit.UltimateCharge);
            system.OnThrowReleased();
            Assert.AreEqual(15f, kit.Skill1.CooldownRemaining, .001f, "Throws no longer grant objective income.");
            system.OnOwnSlipperRetrieved();
            Assert.AreEqual(15f, kit.Skill1.CooldownRemaining, .001f, "Retrieval no longer grants objective income.");
            kit.AddUltimateCharge(kit.UltimateCost);
            Assert.AreEqual(15f, kit.Skill1.CooldownRemaining, .001f, "A non-objective practice/refill changed cooldowns.");
        }

        private MatchRpc ObjectiveReceiver(out HeroAbilitySystem system, out GameplayActionScope scope)
        {
            system = Owner("zack"); var body = system.GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(100, true, 0, true);
            system.Kit.Skill1.ApplyNetworkSnapshot(20, 0, true); system.Kit.AttackingSkill.ApplyNetworkSnapshot(8, 1, true);
            var root = new GameObject("Objective grant receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            scope = new GameplayActionScope { Match = 123, Round = 1, Epoch = body.MovementEpoch }; return router;
        }
        private static bool ObjectiveGrant(MatchRpc router, GameplayActionScope scope, long sequence, float amount = 1, long processed = 0)
            => (bool)typeof(MatchRpc).GetMethod("ApplyObjectiveCooldown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(router, new object[] { scope, 1, sequence, amount, processed });

        [Test] public void AmpedUpOwnerGrantIsScopedAndIdempotent()
        {
            var router = ObjectiveReceiver(out var system, out var scope);
            Assert.IsTrue(ObjectiveGrant(router, scope, 1));
            Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
            Assert.AreEqual(3, system.Kit.AttackingSkill.CooldownRemaining);
            Assert.AreEqual(1, system.Kit.AttackingSkill.ChargesRemaining);
            Assert.AreEqual(0, system.Kit.UltimateCharge);
            Assert.IsFalse(ObjectiveGrant(router, scope, 1));
            Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
        }

        [Test] public void AmpedUpDoesNotDiscountANewerPredictedOrSettledCast()
        {
            var router = ObjectiveReceiver(out var system, out var scope);
            Assert.IsTrue(system.TrackSkillRequest(0, 2));
            Assert.IsTrue(ObjectiveGrant(router, scope, 1, processed: 1));
            Assert.AreEqual(20, system.Kit.Skill1.CooldownRemaining);
            Assert.AreEqual(3, system.Kit.AttackingSkill.CooldownRemaining);
            Assert.IsTrue(ObjectiveGrant(router, scope, 2, processed: 2));
            Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
            system.ResolveSkillReceipt(0, 2, true, 15, 0);
            Assert.IsTrue(ObjectiveGrant(router, scope, 3, processed: 1));
            Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining, "Settling a cast must not let an older award discount it.");
        }

        [Test] public void AmpedUpBadScopesAmountsAndBindingsCannotPoisonTheCurrentGrant()
        {
            var router = ObjectiveReceiver(out var system, out var scope); var bad = scope;
            bad.Match--; Assert.IsFalse(ObjectiveGrant(router, bad, 99));
            bad = scope; bad.Round++; Assert.IsFalse(ObjectiveGrant(router, bad, 99));
            bad = scope; bad.Epoch++; Assert.IsFalse(ObjectiveGrant(router, bad, 99));
            foreach (float amount in new[] { 0f, -1f, 2f, float.NaN, float.PositiveInfinity })
                Assert.IsFalse(ObjectiveGrant(router, scope, 99, amount));
            Assert.IsFalse(ObjectiveGrant(router, scope, 99, processed: -1));
            GameServices.Round.ApplySnapshot(100, false, 0, true);
            Assert.IsFalse(ObjectiveGrant(router, scope, 99));
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            Assert.IsTrue(ObjectiveGrant(router, scope, 1));
            system.BindHero("cheska"); Assert.IsFalse(ObjectiveGrant(router, scope, 2));
        }

        [Test] public void AmpedUpNamedPacketHonorsEnvelopeFramingAndHostAuthority()
        {
            var router = ObjectiveReceiver(out var system, out var scope);
            byte[] Packet(long sequence)
            {
                using var writer = new FastBufferWriter(48, Allocator.Temp);
                writer.WriteValueSafe(0UL); writer.WriteNetworkSerializable(scope); writer.WriteValueSafe(1);
                writer.WriteValueSafe(sequence); writer.WriteValueSafe(1f); writer.WriteValueSafe(0L); return writer.ToArray();
            }
            void Receive(byte[] bytes, ulong sender = 0)
            {
                using var reader = new FastBufferReader(bytes, Allocator.Temp); reader.Seek(8);
                typeof(MatchRpc).GetMethod("OnObjectiveCooldownMsg", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(router, new object[] { sender, reader });
            }
            var bytes = Packet(1); Receive(bytes, 1); Assert.AreEqual(20, system.Kit.Skill1.CooldownRemaining);
            var shortBytes = new byte[bytes.Length - 1]; System.Array.Copy(bytes, shortBytes, shortBytes.Length);
            Receive(shortBytes); Assert.AreEqual(20, system.Kit.Skill1.CooldownRemaining);
            var suffix = new byte[bytes.Length + 1]; System.Array.Copy(bytes, suffix, bytes.Length);
            Receive(suffix); Assert.AreEqual(20, system.Kit.Skill1.CooldownRemaining);
            Receive(bytes); Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
            Receive(bytes); Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
            NetAuthority.Provider = new ObservingHost(); Receive(Packet(2)); Assert.AreEqual(15, system.Kit.Skill1.CooldownRemaining);
        }

        [Test] public void AmpedUpNeverBanksCooldownOrChangesChargesUltimateAndOtherKits()
        {
            var system = Owner("zack"); var kit = system.Kit;
            kit.Skill1.ApplyNetworkSnapshot(1, 0, true); kit.AttackingSkill.ApplyNetworkSnapshot(3, 1, true);
            kit.Ultimate.ApplyNetworkSnapshot(9, 0, true);
            kit.OnObjectiveAwarded(1); kit.OnObjectiveAwarded(1);
            Assert.AreEqual(0, kit.Skill1.CooldownRemaining); Assert.AreEqual(0, kit.AttackingSkill.CooldownRemaining);
            Assert.AreEqual(1, kit.AttackingSkill.ChargesRemaining); Assert.AreEqual(9, kit.Ultimate.CooldownRemaining);
            var other = new CheskaHeroKit(); other.Skill1.ApplyNetworkSnapshot(20, 0, true); other.OnObjectiveAwarded(1);
            Assert.AreEqual(20, other.Skill1.CooldownRemaining);
            kit.Skill1.ApplyNetworkSnapshot(20, 0, true); kit.PracticeMode = true; system.OnLataKnocked();
            Assert.AreEqual(20, kit.Skill1.CooldownRemaining);
        }

        [Test] public void OverclockUsesTheWikiCostAndSelfTarget()
        {
            var kit = new ZackHeroKit();
            Assert.AreEqual(15, kit.UltimateCost, "Overclock still uses the retired twenty-point price.");
            Assert.IsFalse(kit.Ultimate.HoldToAim, "The Wiki strike targets Zack, not a remote ground ring.");
            Assert.AreEqual("OVERCLOCK", kit.Ultimate.Name);
        }

        [Test] public void OverclockRemainsAfterItsOldSevenSecondWindowAndRoundReset()
        {
            var system = Owner("zack"); var body = system.GetComponent<CharacterMotor>();
            NetAuthority.Provider = new ObservingHost();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(100, true, 0, true);
            var kit = (ZackHeroKit)system.Kit; var context = new AbilityContext(body, null, null);
            using (NetCue.SuppressRelay())
            {
                kit.Ultimate.Activate(context); kit.Tick(context, kit.Ultimate.Windup + .01f);
                kit.Tick(context, 30);
            }
            Assert.IsTrue(kit.IsOverclocked, "The accepted upgrade expired instead of lasting for the match.");
            kit.ResetForRound(context);
            Assert.IsTrue(kit.IsOverclocked, "A round boundary erased the match-long upgrade.");
            kit.ResetForMatch(context); Assert.IsFalse(kit.IsOverclocked);
        }

        [Test] public void OverclockRecoveryIsPermanentWithoutStrikeOrResourceRecast()
        {
            var system = Owner("zack"); var body = system.GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(98, false, 0, true);
            var kit = (ZackHeroKit)system.Kit;
            var root = new GameObject("Permanent recovery"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            var state = TimedKitState.Capture(kit, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true), 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = body.MovementEpoch }, 1, 100);
            var apply = typeof(MatchRpc).GetMethod("ApplyTimedKitState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsTrue((bool)apply.Invoke(router, new object[] { state, 98f }));
            Assert.IsTrue(kit.IsOverclocked); Assert.IsTrue(kit.Ultimate.IsPersistentActive);
            Assert.IsFalse(kit.IsThunderstrikeActive, "Permanent upgrade must not resurrect the old raw throw-speed window.");
            Assert.AreEqual(0, kit.UltimateCharge); Assert.AreEqual(0, kit.Ultimate.DurationRemaining);
            Assert.IsEmpty(Object.FindObjectsByType<Visual.DirectedLightningBolt>());
            Assert.IsFalse((bool)apply.Invoke(router, new object[] { state, 98f }));
            kit.AddUltimateCharge(20); Assert.AreEqual(0, kit.UltimateCharge); Assert.IsFalse(kit.IsUltimateReady);
            Assert.IsFalse(kit.TryActivateUltimate(new AbilityContext(body, null, null)));
            kit.ResetForRound(new AbilityContext(body, null, null)); Assert.IsTrue(kit.IsOverclocked);
        }

        [Test] public void PermanentStateCodecIsExplicitBoundedAndCannotBindOtherUltimates()
        {
            var kit = new ZackHeroKit();
            var state = TimedKitState.Capture(kit, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true), 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }, 1, 100);
            byte[] bytes;
            using (var writer = new FastBufferWriter(TimedKitState.MaxWireBytes, Allocator.Temp))
            { writer.WriteNetworkSerializable(state); bytes = writer.ToArray(); var reader = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsTrue(TimedKitState.TryRead(ref reader, out var decoded)); Assert.IsTrue(decoded.UltimatePermanent); }
                finally { reader.Dispose(); } }
            Assert.IsTrue(state.TryResolve(kit, 0, out var aged)); Assert.IsTrue(aged.UltimatePermanent);
            var bad = state; bad.UltimateRemaining = 1; Assert.IsFalse(bad.IsValid);
            bad = state; bad.UltimatePending = true; Assert.IsFalse(bad.IsValid);
            var other = new TimedProbeKit(); bad = TimedKitState.Capture(other, other.CaptureTimedKit(), 1, state.Scope, 2, 100);
            bad.UltimateRemaining = 0; bad.UltimatePermanent = true; Assert.IsFalse(bad.TryResolve(other, 98, out _));
            bytes[bytes.Length - 5] = 2; // Permanent flag precedes the appended passive float.
            var malformed = new FastBufferReader(bytes, Allocator.Temp);
            try { Assert.IsFalse(TimedKitState.TryRead(ref malformed, out _)); }
            finally { malformed.Dispose(); }
        }

        [Test] public void ZappedDisablesPowersForFiveSecondsWithoutStoppingMovementOrStacking()
        {
            var system = Owner("cheska"); var body = system.GetComponent<CharacterMotor>();
            NetAuthority.Provider = new ObservingHost();
            var context = new AbilityContext(body, null, null);
            body.ApplyZapped(); body.ApplyZapped();
            Assert.AreEqual(5, body.ZappedLeft); Assert.IsTrue(body.CanAct()); Assert.IsFalse(body.IsStunned);
            Assert.IsFalse(system.Kit.Skill1.CanActivate(context)); Assert.IsFalse(system.Kit.Ultimate.CanActivate(context));
            var step = typeof(CharacterMotor).GetMethod("StepStatuses", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            step.Invoke(body, new object[] { 5.1f }); Assert.IsFalse(body.IsZapped);
            body.ApplyZapped(); body.CleanseStatuses(); Assert.IsFalse(body.IsZapped);
            body.ApplyNetworkStatuses(0, 0, 0, 0, 4); Assert.AreEqual(4, body.ZappedLeft);
            body.ClearStatuses(); Assert.IsFalse(body.IsZapped);
        }

        [Test] public void OverclockSelfStrikeZapsNearbyRivalsWithoutTheRetiredShockImpulse()
        {
            var system = Owner("zack"); var caster = system.GetComponent<CharacterMotor>();
            NetAuthority.Provider = new ObservingHost(); GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(caster);
            var victim = new GameObject("Nearby zapped rival").AddComponent<CharacterMotor>(); victim.enabled = false; victim.PlayerSlot = 2;
            victim.transform.position = Vector3.left * 3; GameServices.Round.Register(victim);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(100, true, 0, true);
            var kit = (ZackHeroKit)system.Kit; var context = new AbilityContext(caster, null, null);
            using (NetCue.SuppressRelay()) { kit.Ultimate.Activate(context); kit.Tick(context, kit.Ultimate.Windup + .01f); }
            Assert.IsTrue(victim.IsZapped); Assert.AreEqual(5, victim.ZappedLeft);
            Assert.IsFalse(victim.IsStunned); Assert.IsFalse(caster.IsZapped); Assert.IsTrue(kit.IsOverclocked);
        }

        [Test]
        public void ResourceSnapshotsMapBothRolesByIdentityAndRejectPartialOrMalformedSets()
        {
            var host = new ResourceProbeKit();
            host.Skill1.ApplyNetworkSnapshot(7, 1); host.AttackingSkill.ApplyNetworkSnapshot(11, 2);
            host.DefendingSkill.ApplyNetworkSnapshot(21, 0); host.Ultimate.ApplyNetworkSnapshot(4, 2);
            host.AddUltimateCharge(9);
            var source = AbilityResourceSnapshot.Capture(host, 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }, 1);
            AbilityResourceSnapshot decoded;
            using (var writer = new FastBufferWriter(AbilityResourceSnapshot.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(source);
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsTrue(AbilityResourceSnapshot.TryRead(ref reader, out decoded)); }
                finally { reader.Dispose(); }
            }
            var target = new ResourceProbeKit(); target.SetRole(true, default);
            Assert.IsTrue(decoded.TryApply(target, true));
            Assert.IsTrue(target.IsDefending);
            Assert.AreEqual(21, target.Skill2.CooldownRemaining);
            Assert.AreEqual(11, target.IdleRoleSkill.CooldownRemaining);
            Assert.AreEqual(2, target.AttackingSkill.ChargesRemaining);
            Assert.AreEqual(0, target.DefendingSkill.ChargesRemaining);
            Assert.AreEqual(9, target.UltimateCharge);
            var bad = decoded; bad.Abilities = (AbilityResourceSnapshot.Entry[])decoded.Abilities.Clone();
            bad.UltimateCharge = 1; bad.Abilities[0].Cooldown = 0;
            bad.Abilities[3].Id = new FixedString64Bytes("unknown");
            Assert.IsFalse(bad.TryApply(target, true));
            Assert.AreEqual(9, target.UltimateCharge); Assert.AreEqual(7, target.Skill1.CooldownRemaining);
            bad.Abilities[3].Id = bad.Abilities[0].Id;
            using (var writer = new FastBufferWriter(AbilityResourceSnapshot.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(bad);
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsFalse(AbilityResourceSnapshot.TryRead(ref reader, out _)); }
                finally { reader.Dispose(); }
            }
            bad = decoded; bad.UltimateCharge = float.NaN; Assert.IsFalse(bad.IsValid);
            using (var writer = new FastBufferWriter(64, Allocator.Temp))
            {
                writer.WriteValueSafe(1); writer.WriteNetworkSerializable(source.Scope); writer.WriteValueSafe(1L);
                writer.WriteValueSafe((ushort)1); writer.WriteValueSafe((byte)'x'); writer.WriteValueSafe(1f);
                writer.WriteValueSafe((byte)(AbilityResourceSnapshot.MaxAbilities + 1));
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsFalse(AbilityResourceSnapshot.TryRead(ref reader, out _)); }
                finally { reader.Dispose(); }
            }
        }

        [Test]
        public void ResourceReceiverScopesOrdersAndPreservesOwnerPredictionUntilIntermission()
        {
            var system = Owner("dante"); var body = system.GetComponent<CharacterMotor>();
            var kit = new ResourceProbeKit(); typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, kit);
            GameServices.Ensure(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(50, true, 0, true);
            var root = new GameObject("Ability resources receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            var receive = typeof(MatchRpc).GetMethod("OnSyncAbilityMsg", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var host = new ResourceProbeKit(); host.Skill1.ApplyNetworkSnapshot(5, 3); host.AddUltimateCharge(7);
            kit.Skill1.ApplyNetworkSnapshot(20, 0);
            var snapshot = AbilityResourceSnapshot.Capture(host, 1,
                new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }, 1);
            void Deliver(AbilityResourceSnapshot value, ulong sender = 0)
            {
                using var writer = new FastBufferWriter(AbilityResourceSnapshot.MaxWireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(value);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { sender, reader });
            }
            try
            {
                var bad = snapshot; bad.Sequence = 100; Deliver(bad, 9);
                bad.Scope.Match = 122; Deliver(bad);
                bad.Scope.Match = 123; bad.Scope.Round = 2; Deliver(bad);
                bad.Scope.Round = 1; bad.Scope.Epoch = 1; Deliver(bad);
                bad.Scope.Epoch = 0; bad.HeroId = new FixedString64Bytes("another-kit"); Deliver(bad);
                Assert.AreEqual(0, kit.UltimateCharge);
                Deliver(snapshot);
                Assert.AreEqual(7, kit.UltimateCharge);
                Assert.AreEqual(20, kit.Skill1.CooldownRemaining); Assert.AreEqual(0, kit.Skill1.ChargesRemaining);
                snapshot.Abilities[0].Cooldown = 30; Deliver(snapshot);
                Assert.AreEqual(20, kit.Skill1.CooldownRemaining, "Duplicate resource state reapplied.");
                snapshot.Sequence = 2; Deliver(snapshot); Assert.AreEqual(30, kit.Skill1.CooldownRemaining);
                GameServices.Round.ApplySnapshot(50, false, 0, true);
                snapshot.Sequence = 3; snapshot.Abilities[0].Cooldown = 0; Deliver(snapshot);
                Assert.AreEqual(0, kit.Skill1.CooldownRemaining); Assert.AreEqual(3, kit.Skill1.ChargesRemaining);
                snapshot.Sequence = 2; snapshot.Abilities[0].Cooldown = 30; Deliver(snapshot);
                Assert.AreEqual(0, kit.Skill1.CooldownRemaining);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static MatchRpc ClockReceiver()
        {
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 3, true);
            var root = new GameObject("Match clock receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 321L);
            return router;
        }

        private static void DeliverClock(MatchRpc router, MatchClockMessage message, ulong sender = 0)
        {
            using var writer = new FastBufferWriter(MatchClockMessage.WireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(message);
            Assert.AreEqual(MatchClockMessage.WireBytes, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnSyncTimeMsg", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(router, new object[] { sender, reader });
        }

        [Test]
        public void MatchClockReceiverRejectsStaleScopesSequencesAndTruncatedValues()
        {
            NetAuthority.Provider = new PredictingOwner();
            var router = ClockReceiver(); float original = PresentationClock.RequestedScale;
            var message = new MatchClockMessage { Match = 321, Round = 3, Sequence = 5, Scale = 0 };
            try
            {
                PresentationClock.RequestScale(1);
                DeliverClock(router, message, 9);
                var bad = message; bad.Match--; DeliverClock(router, bad);
                bad = message; bad.Round--; DeliverClock(router, bad);
                bad = message; bad.Scale = float.NaN; DeliverClock(router, bad);
                bad = message; bad.Sequence = 0; DeliverClock(router, bad);
                using (var writer = new FastBufferWriter(4, Allocator.Temp))
                {
                    writer.WriteValueSafe(0f);
                    using var reader = new FastBufferReader(writer, Allocator.Temp);
                    typeof(MatchRpc).GetMethod("OnSyncTimeMsg", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic).Invoke(router, new object[] { 0UL, reader });
                }
                Assert.AreEqual(1, PresentationClock.RequestedScale);
                DeliverClock(router, message); Assert.AreEqual(0, PresentationClock.RequestedScale);
                message.Sequence = 4; message.Scale = 1; DeliverClock(router, message);
                Assert.AreEqual(0, PresentationClock.RequestedScale);
                message.Sequence = 6; message.Scale = .5f; DeliverClock(router, message);
                Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                message.Scale = 0; DeliverClock(router, message);
                Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 322L);
                message.Match = 322; message.Sequence = 1; message.Scale = 1; DeliverClock(router, message);
                Assert.AreEqual(1, PresentationClock.RequestedScale);
            }
            finally { Hitstop.End(); PresentationClock.RequestScale(original); Object.DestroyImmediate(router.gameObject); }
        }

        [Test]
        public void MatchClockCaptureAndRefreshExcludeLocalHitstopAndPreserveAnActiveHold()
        {
            NetAuthority.Provider = new ObservingHost();
            var router = ClockReceiver(); float original = PresentationClock.RequestedScale;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.NonPublic;
            var hold = typeof(PresentationClock).GetMethod("Hold", hidden | System.Reflection.BindingFlags.Static);
            var release = typeof(PresentationClock).GetMethod("Release", hidden | System.Reflection.BindingFlags.Static);
            try
            {
                Settings.SettingsStore.Current.ReducedEffects = false;
                PresentationClock.RequestScale(1); Hitstop.Trigger(.08f, .05f);
                Assert.AreEqual(.05f, Time.timeScale);
                var message = (MatchClockMessage)typeof(MatchRpc).GetMethod("CaptureMatchClock",
                    hidden | System.Reflection.BindingFlags.Instance).Invoke(router, null);
                Assert.AreEqual(1, message.Scale);
                NetAuthority.Provider = new PredictingOwner(); DeliverClock(router, message);
                Assert.IsTrue(Hitstop.Active); Assert.AreEqual(.05f, Time.timeScale);
                Hitstop.End(); hold.Invoke(null, null);
                message.Sequence++; message.Scale = .5f; DeliverClock(router, message);
                Assert.AreEqual(0, Time.timeScale); Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                release.Invoke(null, null); Assert.AreEqual(.5f, Time.timeScale);
            }
            finally
            {
                release.Invoke(null, null); Hitstop.End(); PresentationClock.RequestScale(original);
                Settings.SettingsStore.Current.ReducedEffects = reduced; Object.DestroyImmediate(router.gameObject);
            }
        }

        [Test]
        public void ClockRequestsKeepIndependentPeerSequencesWithinTheirWorldScope()
        {
            var router = ClockReceiver();
            var accept = typeof(MatchRpc).GetMethod("AcceptClockRequest", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var message = new MatchClockMessage { Match = 321, Round = 3, Sequence = 9, Scale = 0 };
            bool Accept(ulong peer, MatchClockMessage value) => (bool)accept.Invoke(router, new object[] { peer, value });
            try
            {
                Assert.IsTrue(Accept(7, message)); Assert.IsFalse(Accept(7, message)); Assert.IsTrue(Accept(8, message));
                message.Sequence--; Assert.IsFalse(Accept(7, message));
                message.Match--; Assert.IsFalse(Accept(8, message)); message.Match++;
                message.Round--; Assert.IsFalse(Accept(8, message));
                GameServices.Match.ApplySnapshot(new int[4], 4, true);
                message.Round = 4; message.Sequence = 1; Assert.IsTrue(Accept(7, message));
            }
            finally { Object.DestroyImmediate(router.gameObject); }
        }

        [Test]
        public void UltimatePhaseIdentitySurvivesWindupAndBindsImmediateSentriesOnce()
        {
            var system = Owner("paete"); var body = system.GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Match.AdoptPresentationMatch(123);
            var context = new AbilityContext(body, null, null, Vector3.zero, Vector3.forward, Vector3.forward * 6);
            var delayed = new UltimateLifetimeProbe(true);
            try
            {
                delayed.Activate(context); Assert.IsTrue(delayed.IsWindingUp);
                delayed.AdoptUltimatePhase(7); delayed.AdoptUltimatePhase(7);
                Assert.AreEqual(1, delayed.Bindings);
                delayed.Tick(context, .2f);
                Assert.AreEqual(1, delayed.Activations); Assert.AreEqual(7, delayed.PhaseAtActivation);
                delayed.Reset(); Assert.AreEqual(0, delayed.AcceptedUltimatePhase);
                delayed.Activate(context); Assert.AreEqual(0, delayed.AcceptedUltimatePhase);
                delayed.Reset();

                NetAuthority.Provider = new ObservingHost();
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(HeroKit).GetMethod("AdoptUltimateReservation", hidden).Invoke(system.Kit, null);
                var cast = new UltimateCommit(1, 1, Vector3.zero, Vector3.forward, Vector3.forward * 6, 0).WithIdentity(system.Kit);
                typeof(HeroAbilitySystem).GetMethod("ExecuteSharedUltimate", hidden).Invoke(system, new object[] { cast, false, 9L });
                var sentries = Object.FindObjectsByType<PaeteSentry>();
                Assert.AreEqual(1, sentries.Length); Assert.AreEqual(9, sentries[0].InstanceId);
                Assert.AreEqual(9, system.Kit.Ultimate.AcceptedUltimatePhase);
                system.Kit.Ultimate.AdoptUltimatePhase(9);
                Assert.AreEqual(1, Object.FindObjectsByType<PaeteSentry>().Length);
            }
            finally { delayed.Reset(); system.ResetKitForMatch(); }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator FreshSentryReceivesCapturedTargetsBeforeBirthWithoutLocalSelectionOrRecatching()
        {
            var owner = Owner("paete").GetComponent<CharacterMotor>(); owner.PlayerSlot = 0;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(owner);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Match.AdoptPresentationMatch(123);
            CharacterMotor Body(int slot, Vector3 at)
            {
                var body = new GameObject("Sentry wire target " + slot).AddComponent<CharacterMotor>();
                body.PlayerSlot = slot; body.enabled = false; body.transform.position = at;
                GameServices.Round.Register(body); return body;
            }
            var first = Body(1, Vector3.right); var bystander = Body(2, Vector3.right * 20);
            var missing = Body(3, Vector3.left);
            NetAuthority.Provider = new ObservingHost();
            var host = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true);
            host.AdoptInstance(7); host.enabled = false;
            var state = SentryTargetState.Capture(host); Assert.IsTrue(state.IsValid); Assert.AreEqual(10, state.Mask);
            Object.DestroyImmediate(host.gameObject);
            first.transform.position = Vector3.right * 20; bystander.transform.position = Vector3.right;
            GameServices.Round.Unregister(missing); Object.DestroyImmediate(missing.gameObject);
            NetAuthority.Provider = new PredictingOwner();
            var previous = MatchRpc.Instance;
            var root = new GameObject("Sentry targets receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            var instance = typeof(MatchRpc).GetProperty("Instance");
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnSentryTargetsMsg", hidden);
            void Deliver(SentryTargetState value, ulong sender = 0)
            {
                using var writer = new FastBufferWriter(SentryTargetState.WireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(value); Assert.AreEqual(25, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { sender, reader });
            }
            try
            {
                instance.SetValue(null, router); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
                var replica = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true);
                replica.AdoptInstance(7);
                var heldField = typeof(PaeteSentry).GetField("_held", hidden);
                var held = (System.Collections.Generic.List<CharacterMotor>)heldField.GetValue(replica);
                Assert.IsEmpty(held, "A nearby local bystander was inferred before host selection.");
                var bad = state; bad.Match = 122; Deliver(bad);
                bad = state; bad.Round = 2; Deliver(bad);
                bad = state; bad.Mask = 1; Deliver(bad);
                bad = state; bad.Mask = 16; Deliver(bad); Deliver(state, 9);
                using (var writer = new FastBufferWriter(25, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(state); var valid = writer.ToArray();
                    foreach (int length in new[] { 24, 26 })
                    {
                        var malformed = new byte[length]; System.Array.Copy(valid, malformed, System.Math.Min(valid.Length, length));
                        using var reader = new FastBufferReader(malformed, Allocator.Temp);
                        receive.Invoke(router, new object[] { 0UL, reader });
                    }
                }
                Assert.IsFalse(replica.HasCapturedTargets);
                Object.DestroyImmediate(replica.gameObject);
                Deliver(state); // Accepted selection precedes this peer's tree.
                replica = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true);
                replica.AdoptInstance(7);
                held = (System.Collections.Generic.List<CharacterMotor>)heldField.GetValue(replica);
                CollectionAssert.AreEqual(new[] { first }, held); Assert.AreEqual(10, replica.Capture().TargetMask);
                bad = state; bad.Mask = 4; Deliver(bad);
                CollectionAssert.AreEqual(new[] { first }, held);
                var next = state; next.Instance = 8; next.Mask = 4; Deliver(next);
                var other = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true); other.AdoptInstance(8);
                CollectionAssert.AreEqual(new[] { bystander }, (System.Collections.Generic.List<CharacterMotor>)heldField.GetValue(other));
                Assert.AreEqual(10, replica.Capture().TargetMask);
                Object.DestroyImmediate(other.gameObject);
                var late = Body(3, Vector3.back * 20);
                var bind = typeof(PaeteSentry).GetMethod("BindRestoredTargets", hidden);
                bind.Invoke(replica, null); bind.Invoke(replica, null);
                CollectionAssert.AreEquivalent(new[] { first, late }, held);
                typeof(PaeteSentry).GetField("_age", hidden).SetValue(replica, 1f);
                typeof(PaeteSentry).GetMethod("Update", hidden).Invoke(replica, null);
                Assert.IsTrue((bool)typeof(PaeteSentry).GetField("_catchCuePlayed", hidden).GetValue(replica));
                NetAuthority.Provider = new ObservingHost();
                typeof(PaeteSentry).GetMethod("FixedUpdate", hidden).Invoke(replica, null);
                Assert.IsFalse(first.IsRooted); Assert.IsFalse(late.IsRooted);
                NetAuthority.Provider = new PredictingOwner();
                var captured = replica.Capture(); Assert.AreEqual(7, captured.InstanceId);
                Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { captured }, .1f));
                var restored = PaeteSentry.Find(state); Assert.IsNotNull(restored);
                Assert.IsTrue(restored.HasCapturedTargets); Assert.AreEqual(7, restored.Capture().InstanceId);
                Deliver(bad); Assert.AreEqual(10, restored.Capture().TargetMask, "Birth replay overwrote restored state.");
                typeof(PaeteSentry).GetMethod("Update", hidden).Invoke(restored, null);
                Assert.IsFalse((bool)typeof(PaeteSentry).GetField("_catchCuePlayed", hidden).GetValue(restored), "Recovery replayed the catch cue.");
            }
            finally { instance.SetValue(null, previous); Object.Destroy(root); }
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator SentryResetRetiresOnlyItsFreshAndRecoveredOwnedInstances()
        {
            var first = Owner("paete").GetComponent<CharacterMotor>(); first.PlayerSlot = 0;
            var second = Owner("paete").GetComponent<CharacterMotor>(); second.PlayerSlot = 1;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(first); GameServices.Round.Register(second);
            var firstKit = (PaeteHeroKit)first.AbilitySystem.Kit;
            var secondKit = (PaeteHeroKit)second.AbilitySystem.Kit;
            firstKit.Ultimate.Activate(new AbilityContext(first, null, null, Vector3.zero, Vector3.forward, Vector3.forward * 6));
            secondKit.Ultimate.Activate(new AbilityContext(second, null, null, Vector3.right * 4, Vector3.forward, new Vector3(4, 0, 6)));
            PaeteSentry own = null, other = null;
            foreach (var sentry in Object.FindObjectsByType<PaeteSentry>())
            {
                if (sentry.OwnerSlot == 0) own = sentry;
                if (sentry.OwnerSlot == 1) other = sentry;
            }
            Assert.IsNotNull(own); Assert.IsNotNull(other);
            new PaeteHeroKit().Reset();
            yield return null;
            Assert.IsTrue(own != null && other != null, "An unused kit reset retired another kit's world effects.");
            firstKit.Reset();
            Assert.IsFalse(own.gameObject.activeSelf); Assert.IsTrue(other.gameObject.activeSelf);
            yield return null;
            Assert.IsTrue(own == null); Assert.IsTrue(other != null);
            var recovered = PaeteSentry.Spawn(Vector3.zero, Vector3.forward * 6, 0, age: 1, restoredTargets: 0);
            firstKit.RebindWorldEffects(first);
            firstKit.Reset();
            Assert.IsFalse(recovered.gameObject.activeSelf); Assert.IsTrue(other.gameObject.activeSelf);
            yield return null;
            Assert.IsTrue(recovered == null); Assert.IsTrue(other != null);
            secondKit.Reset();
            yield return null;
            Assert.IsTrue(other == null);
        }

        [Test]
        public void SentryTargetMaskUsesTheWorldFieldReceiverAndRejectsInvalidSeats()
        {
            NetAuthority.Provider = new PredictingOwner();
            var field = new WorldEffectSnapshot.Field { Type = WorldEffectSnapshot.Kind.Sentry,
                Position = Vector3.zero, Forward = Vector3.forward, Owner = 0,
                Duration = PaeteRules.SentryLifeSeconds + .6f, Remaining = 5,
                Radius = PaeteRules.SentryRadius, TargetMask = 10 };
            Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            var bad = field; bad.TargetMask = 16; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            bad = field; bad.TargetMask = 1; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            bad = field; bad.Type = WorldEffectSnapshot.Kind.Plant; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            var root = new GameObject("Sentry field receiver"); root.SetActive(false);
            var receiver = root.AddComponent<MatchRpc>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var batchField = typeof(MatchRpc).GetField("_worldFieldBatch", flags);
            var receive = typeof(MatchRpc).GetMethod("OnWorldFieldItemMsg", flags);
            void Deliver(byte mask, bool includeMask)
            {
                using var writer = new FastBufferWriter(70, Allocator.Temp);
                writer.WriteValueSafe(7); writer.WriteValueSafe(0); writer.WriteValueSafe((int)field.Type);
                writer.WriteValueSafe(field.Position); writer.WriteValueSafe(field.Forward);
                writer.WriteValueSafe(field.Duration); writer.WriteValueSafe(field.Remaining);
                writer.WriteValueSafe(field.Radius); writer.WriteValueSafe(field.Owner);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(false);
                writer.WriteValueSafe(0L);
                if (includeMask) writer.WriteValueSafe(mask);
                Assert.AreEqual(includeMask ? 70 : 69, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(receiver, new object[] { NetworkManager.ServerClientId, reader });
            }
            try
            {
                foreach (byte mask in new byte[] { 16, 1 })
                {
                    var rejected = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, rejected);
                    Deliver(mask, true); Assert.IsFalse(rejected.Finish(7, out _));
                }
                var truncated = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, truncated);
                Deliver(10, false); Assert.IsFalse(truncated.Finish(7, out _));
                var accepted = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, accepted);
                Deliver(10, true); Assert.IsTrue(accepted.Finish(7, out var fields));
                Assert.AreEqual(10, fields[0].TargetMask);
                Assert.AreEqual(field.Remaining, fields[0].Remaining);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RestoredSentryKeepsCapturedSeatsAndBindsLateBodiesWithoutAnotherCatch()
        {
            var owner = Owner("paete").GetComponent<CharacterMotor>(); owner.PlayerSlot = 0;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(owner);
            CharacterMotor Body(int slot, Vector3 at)
            {
                var body = new GameObject("Sentry target " + slot).AddComponent<CharacterMotor>();
                body.PlayerSlot = slot; body.enabled = false; body.transform.position = at;
                GameServices.Round.Register(body); return body;
            }
            var first = Body(1, Vector3.right);
            var lateHost = Body(3, Vector3.left);
            NetAuthority.Provider = new ObservingHost();
            var original = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true);
            original.enabled = false;
            var field = original.Capture();
            Assert.AreEqual(10, field.TargetMask);
            first.transform.position = Vector3.right * 20;
            var bystander = Body(2, Vector3.forward);
            GameServices.Round.Unregister(lateHost); Object.Destroy(lateHost.gameObject);
            NetAuthority.Provider = new PredictingOwner();
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { field }, 1f));
            var restored = Object.FindFirstObjectByType<PaeteSentry>(); Assert.IsNotNull(restored);
            restored.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var held = (System.Collections.Generic.List<CharacterMotor>)typeof(PaeteSentry).GetField("_held", flags).GetValue(restored);
            CollectionAssert.AreEqual(new[] { first }, held, "Recovery selected a nearby bystander instead of the host's distant target.");
            Assert.AreEqual(10, restored.Capture().TargetMask, "A not-yet-installed seat was lost from recovery state.");
            var late = Body(3, Vector3.back * 20);
            var update = typeof(PaeteSentry).GetMethod("Update", flags);
            update.Invoke(restored, null); update.Invoke(restored, null);
            CollectionAssert.AreEqual(new[] { first, late }, held, "Late seats must bind once regardless of current distance.");
            var tree = restored.GetComponentInChildren<Visual.PaeteSentryBody>();
            var targets = (System.Collections.IList)typeof(Visual.PaeteSentryBody).GetField("_targets", flags).GetValue(tree);
            Assert.AreEqual(2, targets.Count, "Repeated binding added another authored limb.");
            NetAuthority.Provider = new ObservingHost();
            typeof(PaeteSentry).GetMethod("FixedUpdate", flags).Invoke(restored, null);
            foreach (var body in new[] { first, late, bystander })
            { Assert.IsFalse(body.IsCarried); Assert.IsFalse(body.IsRooted); }
            yield return null;
        }

        [Test]
        public void VoodooBodyPrefixIsBoundedAndLeavesTheExistingAimTailIntact()
        {
            var state = new VoodooBodySnapshot { Drained = 1, Hexed = 2, MarkKind = 2, MarkSource = 0,
                MarkAge = 12, ReachKind = 1, ReachTarget = 2, ReachElapsed = 1 };
            Assert.IsTrue(state.IsValid(1));
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            Assert.AreEqual(VoodooBodySnapshot.WireBytes, writer.Length);
            writer.WriteNetworkSerializable(default(AbilityAimSnapshot));
            using (var reader = new FastBufferReader(writer, Allocator.Temp))
            {
                var input = reader;
                Assert.IsTrue(VoodooBodySnapshot.TryRead(ref input, 1, out var restored));
                Assert.AreEqual(state.MarkAge, restored.MarkAge);
                Assert.AreEqual(state.ReachTarget, restored.ReachTarget);
                Assert.IsTrue(AbilityAimSnapshot.TryRead(ref input, out _));
            }
            using var shortWriter = new FastBufferWriter(VoodooBodySnapshot.WireBytes - 1, Allocator.Temp);
            for (int i = 0; i < VoodooBodySnapshot.WireBytes - 1; i++) shortWriter.WriteValueSafe((byte)0);
            using (var reader = new FastBufferReader(shortWriter, Allocator.Temp))
            { var input = reader; Assert.IsFalse(VoodooBodySnapshot.TryRead(ref input, 1, out _)); }
            var bad = state; bad.Drained = float.NaN; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.Hexed = -1; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.MarkKind = 3; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.MarkSource = 1; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.ReachTarget = 99; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.ReachSucceeded = true; Assert.IsFalse(bad.IsValid(1));
        }

        private static HeroAbilitySystem Owner(string hero)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -.25f, 0);
            floor.transform.localScale = new Vector3(40, .5f, 40);
            var owner = new GameObject("Receipt owner");
            var motor = owner.AddComponent<CharacterMotor>();
            motor.PlayerSlot = 1; motor.enabled = false;
            var system = owner.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero(hero);
            NetAuthority.Provider = new PredictingOwner();
            return system;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PlantRemovalNamesItsLifetimeAndCannotResurrectAfterLateInstallation()
        {
            var system = Owner("paete");
            GameServices.Ensure();
            var motor = system.GetComponent<CharacterMotor>();
            long firstId = System.DateTime.UtcNow.Ticks;
            void Plant(long identity)
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    Vector3.zero, Vector3.forward, new Vector3(0, 0, 9), 0, false, "paete_skill2", false));
                system.Kit.Skill2.AdoptAcceptedCastEvent(identity, false);
            }
            Plant(firstId);
            var first = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.IsNotNull(first); Assert.AreEqual(firstId, first.InstanceId);
            system.Kit.Skill2.AdoptAcceptedCastEvent(firstId + 1, true);
            Assert.AreEqual(firstId, first.InstanceId, "A fire command relabeled the plant's birth.");
            Plant(firstId + 2);
            var replacement = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.AreNotSame(first, replacement);
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId);
            Assert.IsFalse(replacement.IsPulled, "An old removal pulled the current replacement.");
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId + 2);
            Assert.IsTrue(replacement.IsPulled);
            var late = PaetePlant.Restore(new Vector3(0, 0, 9), motor.PlayerSlot, 16, 3, firstId + 2);
            Assert.IsTrue(late.IsRetiring); Assert.IsFalse(late.gameObject.activeSelf,
                "A queued cast/recovery resurrected an already-retired lifetime.");
            var current = PaetePlant.Restore(new Vector3(0, 0, 9), motor.PlayerSlot, 16, 3, firstId + 3);
            var captured = current.Capture();
            Assert.AreEqual(firstId + 3, captured.InstanceId);
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { captured }, .1f));
            var restored = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.IsNotNull(restored); Assert.AreEqual(firstId + 3, restored.InstanceId);
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId + 2);
            Assert.IsFalse(restored.IsPulled);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ScopedUltimateImpactReachesOnlyTheVictimsCameraWithoutGameplayMutation()
        {
            var caster = Owner("cheska").GetComponent<CharacterMotor>(); caster.PlayerSlot = 0;
            var victim = new GameObject("Impact victim").AddComponent<CharacterMotor>();
            victim.PlayerSlot = 1; victim.enabled = false;
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.Clear(); GameServices.Round.Register(caster); GameServices.Round.Register(victim);
            var camera = new GameObject("Impact view", typeof(Camera)).AddComponent<CameraSystem.CameraRig>();
            camera.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(CameraSystem.CameraRig).GetField("_character", flags).SetValue(camera, victim);
            var hold = typeof(CameraSystem.CameraRig).GetField("_holdLeft", flags);
            var root = new GameObject("Impact receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            var receive = typeof(MatchRpc).GetMethod("OnFlairMsg", flags);
            void Deliver(long match, int round, int subject)
            {
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe((byte)Visual.MatchFlair.Kind.UltimateImpact);
                writer.WriteValueSafe(0); writer.WriteValueSafe(subject);
                writer.WriteValueSafe(Vector3.forward); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(match); writer.WriteValueSafe(round);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
            }
            float scale = Time.timeScale;
            Deliver(122, 1, 1); Assert.AreEqual(0f, hold.GetValue(camera));
            Deliver(123, 2, 1); Assert.AreEqual(0f, hold.GetValue(camera));
            Deliver(123, 1, 0); Assert.AreEqual(0f, hold.GetValue(camera), "An impact on another body changed this view.");
            Deliver(123, 1, 1); Assert.AreEqual(.11f, (float)hold.GetValue(camera), .001f);
            Assert.AreEqual(scale, Time.timeScale); Assert.IsFalse(victim.IsStunned); Assert.IsFalse(victim.IsRooted);
            Object.Destroy(camera.gameObject); Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator VoodooSyncPreservesHostPoolAndCarriesReachOutcomeWithoutTheTargetSnapshot()
        {
            var system = Owner("phaister");
            var body = system.GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Register(body);
            var root = new GameObject("Voodoo body receiver"); root.SetActive(false);
            var receiver = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(receiver, 123L);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnSyncUnitMsg", flags);
            var state = new VoodooBodySnapshot { Drained = 2, Hexed = 3, MarkKind = 2, MarkSource = 0,
                MarkAge = 12, ReachKind = 1, ReachTarget = 2, ReachElapsed = 1 };
            void Deliver(ulong serial, VoodooBodySnapshot data, long match = 123, int round = 1, int epoch = 0, float haunted = 0)
            {
                using var writer = new FastBufferWriter(304, Allocator.Temp);
                writer.WriteValueSafe(1);
                writer.WriteNetworkSerializable(new GameplayActionScope { Match = match, Round = round, Epoch = epoch });
                writer.WriteValueSafe(serial);
                writer.WriteValueSafe(true); // Explicit authoritative owner correction.
                writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(0f); writer.WriteValueSafe(Vector3.zero);
                writer.WriteValueSafe(true);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe((int)StunElement.None);
                writer.WriteValueSafe(1); writer.WriteValueSafe(0);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(42f); writer.WriteValueSafe(1f); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(0); writer.WriteValueSafe(0);
                writer.WriteValueSafe((byte)0); writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(Vector3.forward);
                writer.WriteValueSafe((byte)0); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f);
                writer.WriteValueSafe((byte)0); writer.WriteValueSafe((byte)0);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(0L); writer.WriteValueSafe(haunted);
                writer.WriteValueSafe(0f); // Zapped status.
                writer.WriteNetworkSerializable(data); writer.WriteNetworkSerializable(default(AbilityAimSnapshot));
                Assert.AreEqual(217 + VoodooBodySnapshot.WireBytes, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(receiver, new object[] { NetworkManager.ServerClientId, reader });
            }
            Deliver(100, state, match: 122);
            Deliver(100, state, match: 124);
            Deliver(100, state, round: 0);
            Deliver(100, state, round: 2);
            Deliver(100, state, epoch: -1);
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.IsHexed);
            Assert.IsFalse(body.IsVoodooReaching);
            Assert.AreEqual(0, body.MovementEpoch);
            Deliver(1, state);
            Assert.IsTrue(body.IsDrained); Assert.IsTrue(body.IsHexed);
            Assert.IsTrue(body.Stamina.RecoveryBlocked);
            Assert.AreEqual(42, body.Stamina.Current, "A fresh status edge erased the host's resource correction.");
            Assert.AreEqual(VoodooMarkKind.Hex, body.VoodooMark); Assert.AreEqual(0, body.VoodooMarkSource);
            Assert.IsTrue(body.IsVoodooReaching);
            int ended = 0; bool success = false;
            body.VoodooReachEnded += (_, marked) => { ended++; success = marked; };
            Assert.IsNull(GameServices.Round.PlayerAt(2));
            var finish = new VoodooBodySnapshot { MarkSource = -1, ReachTarget = -1, ReachSucceeded = true };
            Deliver(2, finish);
            Assert.AreEqual(1, ended); Assert.IsTrue(success, "Reach outcome depended on another body's later packet.");
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.Stamina.RecoveryBlocked);
            Deliver(1, state);
            Assert.IsFalse(body.IsDrained, "An older body serial restored expired voodoo state.");
            var invalid = state; invalid.Drained = float.NaN;
            Deliver(3, invalid);
            Assert.IsFalse(body.IsDrained); Assert.AreEqual(1, ended);
            Deliver(3, finish, haunted: 3);
            Assert.AreEqual(3f, body.HauntedLeft, .001f);
            Deliver(4, finish, haunted: float.NaN);
            Deliver(4, finish, haunted: -1);
            Deliver(4, finish, haunted: StatusRules.HauntedSeconds + .1f);
            Assert.AreEqual(3f, body.HauntedLeft, .001f, "Invalid Haunted packets mutated current state.");
            Deliver(4, finish, haunted: 2);
            Assert.AreEqual(2f, body.HauntedLeft, .001f, "Invalid packets consumed the pose serial.");
            Deliver(5, finish, haunted: 0);
            Assert.IsFalse(body.IsHaunted);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedVoodooClocksExpireButOnlyTheHostResolvesWaitingCurses()
        {
            Assert.GreaterOrEqual(NetSession.ProtocolVersion, 75);
            var body = Owner("phaister").GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            var step = typeof(CharacterMotor).GetMethod("StepStatuses",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            void Tick(float dt) => step.Invoke(body, new object[] { dt });

            body.ApplyNetworkVoodoo(.3f, .4f, 0, -1, 0, 0, -1, 0);
            Assert.IsTrue(body.Stamina.RecoveryBlocked);
            Tick(.15f);
            Assert.AreEqual(.15f, body.StatusLeft(StatusKind.Drained), .001f);
            Assert.AreEqual(.25f, body.StatusLeft(StatusKind.Hexed), .001f);
            Tick(.3f);
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.IsHexed);
            Assert.IsFalse(body.Stamina.RecoveryBlocked, "Received Drained outlived its timer.");

            int ended = 0;
            body.VoodooReachEnded += (_, __) => ended++;
            body.ApplyNetworkVoodoo(0, 0, (byte)VoodooMarkKind.Drain, 0, VoodooRules.DrainDelaySeconds - .05f,
                (byte)VoodooMarkKind.Drain, 0, VoodooRules.ReachSeconds - .05f);
            Tick(.1f);
            Assert.AreEqual(VoodooMarkKind.Drain, body.VoodooMark, "A replica resolved the host's waiting mark.");
            Assert.IsFalse(body.IsDrained);
            Assert.IsTrue(body.IsVoodooReaching); Assert.AreEqual(0, ended);
            Assert.AreEqual(VoodooRules.PassiveSpeedScale(false), body.StatusSpeedScale, .001f);

            body.BodySpeedScale = VoodooRules.DollSpeedScale;
            body.ClearStatuses();
            Assert.AreEqual(VoodooRules.DollSpeedScale, body.BodySpeedScale, "Round cleanup erased body identity.");
            NetAuthority.Provider = new ObservingHost();
            body.ApplyNetworkVoodoo(0, 0, (byte)VoodooMarkKind.Drain, 0, VoodooRules.DrainDelaySeconds - .05f,
                0, -1, 0);
            Tick(.1f);
            Assert.AreEqual(VoodooMarkKind.None, body.VoodooMark);
            Assert.IsTrue(body.IsDrained); Assert.IsTrue(body.Stamina.RecoveryBlocked);
            body.CleanseStatuses();
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.Stamina.RecoveryBlocked);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedRootsOwnTheirRestraintWithoutASentryAndAcceptLateFacingOnce()
        {
            var system = Owner("sean");
            var motor = system.GetComponent<CharacterMotor>();
            motor.ApplyNetworkStatuses(0, 0, 7);
            yield return null;
            var coil = motor.GetComponentInChildren<Visual.PaeteRootCoil>();
            Assert.IsNotNull(coil);
            Assert.IsNull(Object.FindFirstObjectByType<PaeteSentry>());
            Assert.IsEmpty(coil.GetComponentsInChildren<Collider>());
            var marks = motor.GetComponent<Visual.StatusBodyMarks>();
            Assert.AreSame(coil, marks.EnsureRootedRestraint(new Vector3(-10, 0, 0)));
            Assert.Greater(Vector3.Dot(motor.transform.forward, Vector3.right), .99f);
            motor.transform.rotation = Quaternion.identity;
            marks.EnsureRootedRestraint(new Vector3(-10, 0, 0));
            Assert.Greater(Vector3.Dot(motor.transform.forward, Vector3.forward), .99f,
                "Repeated sentry updates must not keep turning a player who can aim freely.");
            motor.ApplyNetworkStatuses(0, 0, 9);
            yield return null;
            Assert.AreSame(coil, motor.GetComponentInChildren<Visual.PaeteRootCoil>());
            motor.ApplyNetworkStatuses(0, 0, 0);
            yield return null; yield return null;
            Assert.IsTrue(coil == null); Assert.IsTrue(motor.CanMove());
            motor.ApplyNetworkStatuses(0, 0, 7);
            yield return null;
            coil = motor.GetComponentInChildren<Visual.PaeteRootCoil>();
            Assert.IsNotNull(coil);
            motor.gameObject.SetActive(false);
            yield return null;
            Assert.IsTrue(coil == null, "A disabled body retained its old restraint.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedFrozenStateOwnsOneRestraintWithoutCastingOrBlockingAfterThaw()
        {
            var system = Owner("sean");
            var motor = system.GetComponent<CharacterMotor>(); motor.PlayerSlot = 2;
            void Snapshot(float left) => motor.ApplyNetworkState(left, left, left > 0 ? StunElement.Ice : StunElement.None,
                9, 0, 0, 0, 0, 0, 100, 0, 0);
            Snapshot(1f);
            yield return null;
            var prison = HeroHazards.SpawnIceCubePrison(motor.transform, 1);
            Assert.IsNotNull(prison);
            Assert.AreSame(prison, HeroHazards.SpawnIceCubePrison(motor.transform, 1), "The host cast duplicated the body-owned status picture.");
            Assert.IsEmpty(prison.GetComponentsInChildren<Collider>());
            Snapshot(2f);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(prison != null, "The initial visual timer overruled a received status refresh.");
            Snapshot(0);
            yield return null; yield return null;
            Assert.IsTrue(prison == null);
            Assert.IsTrue(motor.CanMove(), "Thaw presentation mutated movement state.");
            Snapshot(2f);
            yield return null;
            prison = HeroHazards.SpawnIceCubePrison(motor.transform, 2);
            motor.gameObject.SetActive(false);
            yield return null;
            Assert.IsTrue(prison == null, "Disabling a body left its restraint in the world.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator NetworkVerbRefusalDoesNotAddRefundToAnAuthoritativePool()
        {
            var owner = Owner("dante");
            var motor = owner.GetComponent<CharacterMotor>();
            var verbs = owner.gameObject.AddComponent<CombatVerbs>(); verbs.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            motor.Stamina.ApplyNetworkSnapshot(50, 0, 0);
            typeof(CombatVerbs).GetField("_shoveCooldown", flags).SetValue(verbs, 2f);
            verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Shove, refundResources: false);
            Assert.AreEqual(0, verbs.ShoveCooldownLeft);
            Assert.AreEqual(50, motor.Stamina.Current, "Refusal refunded a cost already absent from the authoritative pool.");
            typeof(CombatVerbs).GetField("_slideCooldown", flags).SetValue(verbs, 2f);
            motor.Commit(1f);
            verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Slide, refundResources: false);
            Assert.AreEqual(0, verbs.SlideCooldownLeft);
            Assert.AreEqual(0, motor.CommitLeft);
            Assert.AreEqual(50, motor.Stamina.Current);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteHostResourceClocksRecoverAndDrainWithoutMovingTheReplica()
        {
            var root = new GameObject("Remote resource clock");
            var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.PlayerSlot = 2; motor.RoundActive = true;
            NetAuthority.Provider = new ObservingHost();
            Assert.IsFalse(motor.IsLocallySimulated());
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var tick = typeof(CharacterMotor).GetMethod("FixedUpdate", flags);
            var position = root.transform.position;
            motor.Stamina.Spend(Balance.StaminaMax);
            Assert.IsTrue(motor.Stamina.IsFatigued);
            for (int i = 0; i < 500; i++) tick.Invoke(motor, null);
            Assert.IsFalse(motor.Stamina.IsFatigued);
            Assert.AreEqual(0, motor.Stamina.SpeedZones.Count);
            Assert.AreEqual(Balance.StaminaMax, motor.Stamina.Current, .01f);

            motor.ApplyNetworkResourceIntent(6);
            for (int i = 0; i < 10; i++) tick.Invoke(motor, null);
            Assert.Less(motor.Stamina.Current, Balance.StaminaMax);
            Assert.AreEqual(position, root.transform.position, "Resource authority must not simulate a second body movement.");
            float spent = motor.Stamina.Current;
            motor.AdoptMovementEpoch(motor.MovementEpoch + 1);
            tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "Old movement intent survived the new body epoch.");
            motor.ApplyNetworkResourceIntent(6);
            typeof(CharacterMotor).GetField("_netResourceIntentUntil", flags).SetValue(motor, Time.unscaledTime - 1);
            tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "A stale pose kept draining stamina indefinitely.");

            NetAuthority.Provider = new PredictingOwner();
            for (int i = 0; i < 100; i++) tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "An observing client simulated host-owned resource clocks.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteInteractionHoldsNeedHostTimeAndExpireWithInputOrEpoch()
        {
            var root = new GameObject("Remote interaction hold");
            var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.PlayerSlot = 2; motor.RoundActive = true;
            NetAuthority.Provider = new ObservingHost();
            GameServices.Ensure(); GameServices.Round.Register(motor);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var step = typeof(CharacterMotor).GetMethod("StepStatuses", flags);
            motor.ApplyRooted(30);
            motor.HostBreakFree(); Assert.IsTrue(motor.IsRooted, "A completion request skipped the host hold clock.");
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 10; i++) step.Invoke(motor, new object[] { .1f });
            float earned = motor.BreakFreeProgress; Assert.Greater(earned, 0);
            motor.ApplyNetworkResourceIntent(0);
            step.Invoke(motor, new object[] { 1f });
            Assert.AreEqual(earned, motor.BreakFreeProgress, .001f, "Release must preserve root progress without adding time.");
            motor.ApplyNetworkResourceIntent(8);
            typeof(CharacterMotor).GetField("_netResourceIntentUntil", flags).SetValue(motor, Time.unscaledTime - 1);
            step.Invoke(motor, new object[] { 1f });
            Assert.AreEqual(earned, motor.BreakFreeProgress, .001f, "An expired input lease completed a hold.");
            motor.ApplyNetworkResourceIntent(8);
            motor.AdoptMovementEpoch(motor.MovementEpoch + 1);
            Assert.IsFalse(motor.InteractionHeldForSimulation);
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 100 && motor.IsRooted; i++) step.Invoke(motor, new object[] { .1f });
            Assert.IsFalse(motor.IsRooted, "The host did not finish a real remote hold without a second completion request.");

            var plant = PaetePlant.Restore(motor.transform.position, 0, PaeteRules.PlantRootedSeconds + 1, 3);
            plant.enabled = false;
            var pull = typeof(PaetePlant).GetMethod("StepPullers", flags);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A nearby client pulled a plant without holding.");
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 14; i++) pull.Invoke(plant, new object[] { .1f });
            Assert.IsTrue(PaetePlant.HostTryUproot(motor));
            motor.ApplyNetworkResourceIntent(0);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A released hold remained actionable until the next Update.");
            pull.Invoke(plant, new object[] { .1f });
            motor.ApplyNetworkResourceIntent(8);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A plant pull must restart after release.");
            Object.Destroy(root); Object.Destroy(plant.gameObject);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteAimPresentsOnlyBodyTellsAndRejectsClosedOrOlderHolds()
        {
            var system = Owner("phaister");
            var motor = system.GetComponent<CharacterMotor>();
            motor.PlayerSlot = 2; motor.RoundActive = true;
            var kit = new AimProbeKit();
            typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, kit);
            system.enabled = true;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var update = typeof(HeroAbilitySystem).GetMethod("Update", flags);
            var one = (AimProbeAbility)kit.Skill1; var two = (AimProbeAbility)kit.Skill2;
            var aim = new AbilityAimSnapshot
            { Slot = 1, AbilityId = new FixedString64Bytes(one.Id), Held = .4f, Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 7) };
            system.ApplyNetworkAim(aim); update.Invoke(system, null);
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            Assert.Greater(one.BodyPresents, 0); Assert.Zero(one.PrivatePresents); Assert.Zero(one.Activations);
            system.CloseNetworkAim(0, aim.Token);
            system.ApplyNetworkAim(aim);
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 8);
            system.ApplyNetworkAim(aim);
            system.CloseNetworkAim(0, AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 7));
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill1), "Old release cancelled a new hold.");
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 6);
            system.ApplyNetworkAim(aim);
            Assert.AreEqual(AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 8), system.CaptureAimPresentation().Token);

            aim.Slot = 2; aim.AbilityId = new FixedString64Bytes(two.Id);
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 9);
            system.ApplyNetworkAim(aim); update.Invoke(system, null);
            system.CloseNetworkAim(0, AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 10));
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill2), "Another slot's cast ended this hold.");
            typeof(HeroAbilitySystem).GetField("_networkAimUntil", flags).SetValue(system, Time.unscaledTime - 1);
            update.Invoke(system, null);
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill2));
            Assert.Greater(two.BodyEnds, 0);
            system.ApplyNetworkAim(aim);
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill2), "A fresh packet could not renew an expired lease.");
            system.ResetKit();
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill2));
            Assert.Zero(one.Activations + two.Activations, "Visual hold replication cast a gameplay ability.");
            system.enabled = false;
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator EarlierConfirmedEffectsSurviveNewerRequestsWithoutDuplicateReplay()
        {
            var system = Owner("cheska");
            var motor = system.GetComponent<CharacterMotor>();
            var skill = system.Kit.Skill1;
            var first = new AbilityContext(motor, null, null, Vector3.zero, Vector3.forward, Vector3.forward * 5);
            var second = new AbilityContext(motor, null, null, Vector3.zero, Vector3.right, Vector3.right * 5);
            skill.Activate(first); Assert.IsTrue(system.TrackSkillRequest(0, 101));
            skill.Activate(second); Assert.IsTrue(system.TrackSkillRequest(0, 102));
            Assert.IsTrue(system.PendingSkillReceipt(0, 101));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            Assert.IsTrue(system.ResolveSkillReceipt(0, 102, false, 4, 0));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 102,
                second.Position, second.Forward, second.AimPoint, .55f));

            skill.Activate(first); system.TrackSkillRequest(0, 103);
            skill.Activate(second); system.TrackSkillRequest(0, 104);
            float cooldown = skill.CooldownRemaining;
            Assert.IsTrue(system.ResolveSkillReceipt(0, 103, false, 0, 0));
            Assert.AreEqual(cooldown, skill.CooldownRemaining, "An older refusal rewrote newer resources.");
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 104,
                second.Position, second.Forward, second.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            skill.Activate(first); system.TrackSkillRequest(0, 105);
            system.ResetKit();
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 105,
                first.Position, first.Forward, first.AimPoint, .55f));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DeferredPlantWaitsForItsObjectAndCommandsCannotReplaceOrCancelIt()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            motor.transform.position = new Vector3(-10, .12f, -10);
            var pose = new AbilityContext(motor, null, null, motor.transform.position,
                Vector3.forward, new Vector3(-10, .12f, -6));
            var skill = system.Kit.Skill2;
            skill.Activate(pose); Assert.IsTrue(system.TrackSkillRequest(1, 201));
            float remaining = skill.DurationRemaining;
            skill.Tick(pose, 2);
            Assert.AreEqual(remaining, skill.DurationRemaining, "A missing unconfirmed plant ended the active skill.");
            var canPredict = typeof(HeroAbilitySystem).GetMethod("CanPredictSkill",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsFalse((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 201,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            yield return null;
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant);
            Assert.IsTrue((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));

            Assert.IsTrue(system.TrackSkillRequest(1, 202, reactivation: true));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 202,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual(1, PaetePlant.Live.Count, "A command confirmation planted a second tree.");
            Assert.IsTrue(system.TrackSkillRequest(1, 203, reactivation: true));
            Assert.IsTrue(system.ResolveSkillReceipt(1, 203, false, 3, 0));
            Assert.IsTrue(skill.IsActive, "A denied command cancelled the earlier accepted plant skill.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
        }

        [UnityTest]
        public IEnumerator RefusedFreeRecallDoesNotCreateAChargeAndEligibilityDoesNotMutateHeldTime()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);actor.AbilitySystem.BindHero("nemu");yield return null;
            var system=actor.AbilitySystem;var kit=system.Kit;
            var context=new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));Assert.AreEqual(0,kit.Skill2.ChargesRemaining);
            Assert.IsTrue(kit.Skill2.IsActive);Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));
            kit.Skill2.RollBackPredictedCast(context,refundResources:false);
            Assert.AreEqual(0,kit.Skill2.ChargesRemaining,"A free reactivation has no charge to refund");
            float heldBefore=kit.Skill1.HeldSecondsOnCast;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,1.1f));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(0,kit.Skill1.CooldownRemaining);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill1(context));
            float cooldown=kit.Skill1.CooldownRemaining;
            Assert.AreEqual(HeroKit.CastOutcome.Cooling,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,2));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(cooldown,kit.Skill1.CooldownRemaining);
            system.TrackSkillRequest(0,1);system.TrackSkillRequest(0,2);
            Assert.IsFalse(system.PendingSkillReceipt(0,1));Assert.IsTrue(system.PendingSkillReceipt(0,2));
            system.ResetKit();Assert.IsFalse(system.PendingSkillReceipt(0,2),"Round resets retire outstanding predictions");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ApprovedPlantPlaybackDoesNotRecheckReplicaReadinessOrDeferAnUnpredictedCast()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            var at = new Vector3(-10, .12f, -10);
            var aim = at + Vector3.forward * 4;
            motor.transform.position = at;
            var skill = system.Kit.Skill2;
            skill.Activate(new AbilityContext(motor, null, null, at, Vector3.forward, aim));
            Assert.IsNull(PaetePlant.OwnedBy(1), "Local prediction must still await acceptance.");
            system.ResetKit();

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant, "An unpredicted approved owner cast must initialize its world effect.");
            Assert.IsTrue(skill.IsActive);
            Assert.IsFalse(plant.ShotReady);
            Assert.IsFalse(plant.Fire(aim), "An ordinary command must still respect reload.");
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: true));
            Assert.AreEqual(0, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length,
                "Replica clock drift must not swallow a host-approved command.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual("hero-paete-command", skill.CastAction);
            Assert.IsFalse(plant.Fire(aim), "Playback must not unlock further local shots.");
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator JoinedStatusPresentationNeedsNoOriginalCastAndCleansUpPerBody()
        {
            var system = Owner("phaister");
            var first = system.GetComponent<CharacterMotor>();
            var second = new GameObject("Other joined body").AddComponent<CharacterMotor>();
            second.PlayerSlot = 2; second.enabled = false;
            // No original cast and no RoundDirector: these are the received joining timers.
            first.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("HexMark"));

            first.gameObject.SetActive(false);
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length,
                "Despawn must clean only that body's presentation.");
            first.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);

            first.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            yield return null;
            yield return null;
            Assert.IsNull(GameObject.Find("HexMark"));
            foreach (var moon in Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None))
                Assert.IsTrue(moon.Ending, "The authored end must follow received status expiry.");
            Object.Destroy(first.gameObject);
            Object.Destroy(second.gameObject);
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator AcceptedCastsWaitForTheirBodyAndKeepExplicitCommandIntentExactlyOnce()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            GameServices.Ensure();
            GameServices.Round.Clear();
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            var root = new GameObject("Delayed body receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnPlayAbilityMsg", flags);
            var flush = typeof(MatchRpc).GetMethod("FlushPendingSkills", flags);
            int Pending() => ((ICollection)typeof(MatchRpc).GetField("_pendingSkillCasts", flags).GetValue(router)).Count;
            var cast = new SkillCastMessage
            {
                Seat = 1, Slot = 1, AbilityId = new FixedString64Bytes("paete_skill2"), Match = 123, Round = 1,
                Event = 1, Position = new Vector3(-10, .12f, -10), Forward = Vector3.forward,
                AimPoint = new Vector3(-10, .12f, -6)
            };
            void Send()
            {
                using var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(cast);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
            }
            Send(); Send();
            Assert.AreEqual(1, Pending(), "A duplicate queued the same accepted cast twice.");
            cast.Event = 2; cast.Reactivation = true; Send();
            Assert.AreEqual(2, Pending());
            var staleWorld = new WorldSnapshotHeader { SkillEvent = 2 };
            Assert.IsTrue((bool)typeof(MatchRpc).GetMethod("WorldSnapshotNeedsRefresh", flags)
                .Invoke(router, new object[] { staleWorld }), "A world restore raced ahead of pending casts.");
            GameServices.Round.Register(motor);
            flush.Invoke(router, null);
            Assert.Zero(Pending());
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant);
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);
            Send();
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);

            var skill = system.Kit.Skill2;
            var context = new AbilityContext(motor, null, null);
            skill.Tick(context, PaeteRules.PlantLifeSeconds + 1);
            Assert.IsFalse(skill.IsActive);
            cast.Event = 3; Send();
            Assert.AreSame(plant, PaetePlant.OwnedBy(1), "An accepted command became a new plant after timer drift.");
            Assert.AreEqual(2, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(HeroKit.CastOutcome.Missing, system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                cast.Position, cast.Forward, cast.AimPoint, 0, false, "paete_skill2d", false));

            GameServices.Round.Clear(); cast.Event = 4; cast.Reactivation = false; Send();
            Assert.AreEqual(1, Pending());
            GameServices.Match.ApplySnapshot(new int[4], 2, true);
            flush.Invoke(router, null);
            Assert.Zero(Pending(), "A queued cast survived its round scope.");
            GameServices.Round.Register(motor);
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Object.Destroy(root);
            yield return null;
        }

        [Test]
        public void NewTransportCanReceiveTheSameActiveCohortWithoutReusingHostIdentities()
        {
            var system = Owner("dante");
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(system.GetComponent<CharacterMotor>());
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            var previousRouter = MatchRpc.Instance; var previousPhase = SharedUltimatePhase.Instance;
            float scale = PresentationClock.RequestedScale;
            var root = new GameObject("Reconnected cohort"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); var phase = root.AddComponent<SharedUltimatePhase>();
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var instance = typeof(MatchRpc).GetProperty("Instance"); var phaseInstance = typeof(SharedUltimatePhase).GetProperty("Instance");
            var receive = typeof(SharedUltimatePhase).GetMethod("ReceiveTimed", hidden);
            var commits = new[] { new UltimateCommit(3, 1, Vector3.zero, Vector3.forward, Vector3.up, 0,
                heroId: "missing", abilityId: "missing_ultimate") };
            void Receive(long id) => receive.Invoke(phase, new object[] { 123L, 1, id, SharedUltimatePhase.Now, .5f, commits, 50f, 5d });
            try
            {
                instance.SetValue(null, router); phaseInstance.SetValue(null, phase);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
                Receive(5); Assert.IsTrue(phase.Active); phase.Cancel(); Receive(5);
                Assert.IsFalse(phase.Active, "Ordinary cancellation must retain same-transport duplicate protection.");
                Receive(6); Assert.IsTrue(phase.Active); Assert.IsTrue(PresentationClock.Held);
                typeof(SharedUltimatePhase).GetField("_sequence", hidden).SetValue(phase, 17L);
                typeof(HeroAbilitySystem).GetField("_pendingUltimateRequest", hidden).SetValue(system, 9L);
                typeof(MatchRpc).GetMethod("ResetUltimateTransport", hidden).Invoke(router, null);
                Assert.IsFalse(phase.Active); Assert.IsFalse(PresentationClock.Held);
                Assert.IsFalse(system.UltimateRequestPending);
                Assert.AreEqual(0, phase.PhaseId); Assert.AreEqual(17, typeof(SharedUltimatePhase).GetField("_sequence", hidden).GetValue(phase));
                Receive(6); Assert.IsTrue(phase.Active); Assert.AreEqual(6, phase.PhaseId);
            }
            finally
            {
                phase.Cancel(); PresentationClock.RequestScale(scale);
                instance.SetValue(null, previousRouter); phaseInstance.SetValue(null, previousPhase);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UltimateCommitCodecBoundsIdentityAndPreservesTheRequestBodyScope()
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var write = typeof(MatchRpc).GetMethod("WriteUltimateCommit", flags);
            var read = typeof(MatchRpc).GetMethod("TryReadUltimateCommit", flags);
            string hero = new string('h', 61), ability = new string('a', 61);
            var cast = new UltimateCommit(1, 4, Vector3.zero, Vector3.forward, Vector3.up, .5f,
                heroId: hero, abilityId: ability);
            var scope = new GameplayActionScope { Match = 123, Round = 1, Epoch = 2 };
            byte[] bytes;
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(scope); write.Invoke(null, new object[] { writer, cast });
                Assert.AreEqual(16 + 199, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                reader.ReadNetworkSerializable(out GameplayActionScope restored);
                Assert.IsTrue(restored.Matches(123, 1, 2));
                var args = new object[] { reader, default(UltimateCommit) };
                Assert.IsTrue((bool)read.Invoke(null, args));
                var decoded = (UltimateCommit)args[1];
                Assert.AreEqual(hero, decoded.HeroId.ToString()); Assert.AreEqual(ability, decoded.AbilityId.ToString());
                Assert.AreEqual(cast.Request, decoded.Request); Assert.AreEqual(cast.Held, decoded.Held);
            }
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            { write.Invoke(null, new object[] { writer, cast }); bytes = writer.ToArray(); }
            bool Reads(byte[] payload)
            {
                using var reader = new FastBufferReader(payload, Allocator.Temp);
                var args = new object[] { reader, default(UltimateCommit) };
                return (bool)read.Invoke(null, args);
            }
            var oversized = (byte[])bytes.Clone(); oversized[73] = 62; oversized[74] = 0;
            Assert.IsFalse(Reads(oversized));
            var truncated = new byte[bytes.Length - 1]; System.Array.Copy(bytes, truncated, truncated.Length);
            Assert.IsFalse(Reads(truncated)); Assert.IsFalse(Reads(new byte[76]));
            Assert.Throws<System.InvalidOperationException>(() => AbilityNetworking.Validate(new ResourceProbeKit(new string('x', 62))));
        }

        [Test]
        public void UltimatePreparationAndExecutionRequireTheCommittedHeroAndAbility()
        {
            var system = Owner("dante"); var body = system.GetComponent<CharacterMotor>();
            var other = new ResourceProbeKit("other-probe");
            typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, other);
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(50, true, 0, true);
            var root = new GameObject("Ultimate identity preparation"); root.SetActive(false);
            var phase = root.AddComponent<SharedUltimatePhase>();
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var cast = new UltimateCommit(1, 1, Vector3.zero, Vector3.forward, Vector3.up, 0,
                heroId: "resource-probe", abilityId: "ultimate");
            try
            {
                typeof(SharedUltimatePhase).GetProperty("MatchId").SetValue(phase, GameServices.Match.PresentationMatchId);
                typeof(SharedUltimatePhase).GetProperty("Round").SetValue(phase, 1);
                var commits = (System.Collections.Generic.List<UltimateCommit>)typeof(SharedUltimatePhase).GetField("_commits", hidden).GetValue(phase);
                commits.Add(cast);
                var prepare = typeof(SharedUltimatePhase).GetMethod("PrepareActors", hidden);
                Assert.IsFalse((bool)prepare.Invoke(phase, null), "A different hero with the same ability ID was accepted.");
                typeof(HeroKit).GetMethod("AdoptUltimateReservation", hidden).Invoke(other, null);
                typeof(HeroAbilitySystem).GetMethod("ExecuteSharedUltimate", hidden).Invoke(system, new object[] { cast, false, 1L });
                Assert.AreEqual(0, ((ResourceProbeAbility)other.Ultimate).Activations);
                Assert.IsTrue(other.Ultimate.ReservedForIntroduction);
                var matching = new ResourceProbeKit();
                typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, matching);
                commits[0] = new UltimateCommit(1, 1, Vector3.zero, Vector3.forward, Vector3.up, 0,
                    heroId: matching.HeroId, abilityId: "changed-ultimate");
                Assert.IsFalse((bool)prepare.Invoke(phase, null));
                commits[0] = cast;
                Assert.IsTrue((bool)prepare.Invoke(phase, null));
                Assert.IsTrue(matching.Ultimate.ReservedForIntroduction);
                Assert.AreEqual(0, ((ResourceProbeAbility)matching.Ultimate).Activations, "Preparation cast the ability early.");
            }
            finally { system.ResetKitForMatch(); Object.DestroyImmediate(root); }
        }

        [Test]
        public void UltimateReceiverKeepsTheHostDurationWhileTheCasterIsMissing()
        {
            NetAuthority.Provider = new PredictingOwner();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            Assert.IsNull(GameServices.Round.PlayerAt(3));
            var previousRouter = MatchRpc.Instance; var previousPhase = SharedUltimatePhase.Instance;
            float original = PresentationClock.RequestedScale;
            var root = new GameObject("Timed cohort receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>(); var phase = root.AddComponent<SharedUltimatePhase>();
            var instance = typeof(MatchRpc).GetProperty("Instance");
            var phaseInstance = typeof(SharedUltimatePhase).GetProperty("Instance");
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnUltimatePhaseMsg", hidden | System.Reflection.BindingFlags.Instance);
            var write = typeof(MatchRpc).GetMethod("WriteUltimateCommit", hidden | System.Reflection.BindingFlags.Static);
            void Deliver(long sequence, float duration, ulong sender = 0, bool trailing = false)
            {
                using var writer = new FastBufferWriter(256, Allocator.Temp);
                writer.WriteValueSafe(123L); writer.WriteValueSafe(1); writer.WriteValueSafe(sequence);
                writer.WriteValueSafe(SharedUltimatePhase.Now - 3.2); writer.WriteValueSafe(duration);
                writer.WriteValueSafe(.5f); writer.WriteValueSafe(50f); writer.WriteValueSafe(1);
                var cast = new UltimateCommit(3, 1, Vector3.zero, Vector3.forward, Vector3.up, 0,
                    heroId: "missing", abilityId: "missing_ultimate");
                write.Invoke(null, new object[] { writer, cast });
                if (trailing) writer.WriteValueSafe((byte)1);
                Assert.AreEqual(44 + 77 + cast.HeroId.Length + cast.AbilityId.Length + (trailing ? 1 : 0), writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { sender, reader });
            }
            try
            {
                instance.SetValue(null, router); phaseInstance.SetValue(null, phase);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
                Deliver(100, 5, 9);
                Deliver(100, 5, trailing: true);
                foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 31f }) Deliver(100, invalid);
                Assert.IsFalse(phase.Active);
                Deliver(1, 5);
                Assert.IsTrue(phase.Active, "Missing caster replaced the host's five-second introduction with the short fallback.");
                Assert.AreEqual(5, phase.Duration); Assert.AreEqual(1, phase.PhaseId);
                Assert.IsTrue(PresentationClock.Held); Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                Deliver(1, 1); Assert.IsTrue(phase.Active); Assert.AreEqual(5, phase.Duration);
                Deliver(2, 1); Assert.IsFalse(phase.Active); Assert.AreEqual(2, phase.PhaseId);
                Assert.AreEqual(1, phase.Duration); Assert.IsFalse(PresentationClock.Held);
                Deliver(2, 5); Assert.IsFalse(phase.Active, "A duplicate terminal phase restarted playback.");
            }
            finally
            {
                phase.Cancel(); PresentationClock.RequestScale(original);
                instance.SetValue(null, previousRouter); phaseInstance.SetValue(null, previousPhase);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ExpiredUltimateCohortsRetireOlderPlaybackAndKeepTheirTerminalIdentity()
        {
            Owner("phaister");
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            var previousRouter = MatchRpc.Instance;
            var root = new GameObject("Expired cohort receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            var phase = root.AddComponent<SharedUltimatePhase>();
            var instance = typeof(MatchRpc).GetProperty("Instance");
            var match = typeof(MatchRpc).GetProperty("PresentationMatchId");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(SharedUltimatePhase).GetMethod("Receive", flags);
            var commits = new[] { new UltimateCommit(3, 1, Vector3.zero, Vector3.forward, Vector3.up, 0) };
            try
            {
                instance.SetValue(null, router); match.SetValue(router, 123L);
                typeof(SharedUltimatePhase).GetProperty("MatchId").SetValue(phase, 123L);
                typeof(SharedUltimatePhase).GetProperty("Round").SetValue(phase, 1);
                typeof(SharedUltimatePhase).GetProperty("PhaseId").SetValue(phase, 4L);
                typeof(SharedUltimatePhase).GetProperty("Active").SetValue(phase, true);
                void Receive(long id, long sequence, bool expired) => receive.Invoke(phase,
                    new object[] { id, 1, sequence, SharedUltimatePhase.Now - (expired ? 100 : 0), 1f, commits, 50f });
                Receive(123, 5, true);
                Assert.IsFalse(phase.Active); Assert.AreEqual(5, phase.PhaseId);
                Receive(123, 5, false);
                Assert.IsFalse(phase.Active, "A duplicate terminal phase restarted its introduction.");
                match.SetValue(router, 456L);
                Receive(456, 1, true);
                Assert.AreEqual(456, phase.MatchId); Assert.AreEqual(1, phase.PhaseId);
                Receive(456, 1, false);
                Assert.IsFalse(phase.Active, "A late first phase failed to adopt its new match identity.");
            }
            finally { instance.SetValue(null, previousRouter); Object.Destroy(root); }
            yield return null;
        }
    }
}
