using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderSeatPresentationLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private sealed class Client : INetProvider
        {
            private readonly int _seat;
            public Client(int seat) { _seat = seat; }
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => _seat; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private MatchRpc _rpc;
        private GameObject _body, _shoeBody, _canBody;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private PlayerInputReader _reader;
        private Slipper _shoe;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_verbs);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _rpc = MatchRpc.Instance; typeof(MatchRpc).GetProperty("Instance").SetValue(null, null);
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Reader custody can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Reader custody original seat");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            GameServices.Round.Register(_motor); Role(1);
            _shoeBody = new GameObject("Reader custody owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>();
            _shoe.enabled = false; _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            _reader = _body.AddComponent<PlayerInputReader>(); yield return null;
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.CanAct()); Assert.Greater(Time.deltaTime, 0);
            Assert.IsNull(MatchRpc.Instance);
        }
        [UnityTearDown] public IEnumerator After()
        {
            GameServices.Round.Unregister(_motor);
            foreach (var go in new[] { _body, _shoeBody, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _rpc); NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Role(int round)
        {
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], round, true);
            GameServices.Round.ApplySnapshot(80, true, (round - 1) % Balance.PlayerCount, true);
            Assert.IsTrue(_motor.CanAct());
        }
        private void ThrowWindup()
        {
            _motor.Intent.Set(Verb.SpecialAbility, true); Step(_carrier); Step(_carrier);
            Assert.IsTrue(_carrier.IsCharging); Assert.Greater(_carrier.ChargeRatio, 0);
        }
        private void Defender()
        {
            Assert.IsTrue(_shoe.HostDisarm()); Role(2); _body.transform.position = Vector3.zero;
            Assert.IsTrue(_motor.IsDefender);
        }
        private void LungeWindup()
        {
            Defender(); _motor.Intent.Set(Verb.Lunge, true); Step(_verbs); Step(_verbs);
            Assert.Greater(_verbs.LungeChargeRatio, 0); Assert.Zero(_verbs.LungeCooldownLeft);
        }
        private static void Step(Component consumer) => consumer.GetType().GetMethod("Update", Hidden).Invoke(consumer, null);
        private void Withdraw()
        {
            Assert.IsTrue(_reader.enabled); _reader.enabled = false;
            Assert.IsFalse(_reader.enabled); Assert.IsTrue(_body.activeInHierarchy);
        }
        [Test] public void ObsoleteReaderRetiresOldThrowButPreservesReceivedThrowTell()
        {
            ThrowWindup(); NetAuthority.Provider = new Client(2);
            Assert.AreNotEqual(_motor.PlayerSlot, NetAuthority.LocalSlot);
            _carrier.ApplyObservedCharge(true, .7f, .2f); float observed = _carrier.ObservedChargePower;
            Assert.Greater(observed, 0); Withdraw();
            Assert.IsFalse(_carrier.IsCharging, "The retired producer kept its old local throw pending.");
            Assert.Zero(_carrier.ChargeRatio); Assert.AreSame(_shoe, _carrier.Held);
            Assert.AreEqual(observed, _carrier.ObservedChargePower, "Old-seat reader withdrawal erased the received throw tell.");
            Assert.Greater(_carrier.ObservedPektusSpin, 0);
        }
        [Test] public void ObsoleteReaderRetiresOldLungeButPreservesReceivedLungeTell()
        {
            LungeWindup(); NetAuthority.Provider = new Client(2);
            Assert.AreNotEqual(_motor.PlayerSlot, NetAuthority.LocalSlot);
            _verbs.ApplyObservedLungeCharge(true, .4f); Withdraw();
            Assert.Zero(_verbs.LungeChargeRatio, "The retired producer kept its old local lunge pending.");
            Assert.Zero(_verbs.LungeCooldownLeft); Assert.Zero(Contact);
            Assert.Greater(_verbs.ObservedLungeCharge, 0, "Old-seat reader withdrawal erased the received lunge tell.");
        }
        [Test] public void StillOwnedReaderWithdrawalRetiresThrowAndItsTell()
        {
            ThrowWindup(); NetAuthority.Provider = new Client(1); _carrier.ApplyObservedCharge(true, .7f, .2f); Withdraw();
            Assert.IsFalse(_carrier.IsCharging); Assert.Zero(_carrier.ChargeRatio);
            Assert.AreEqual(-1, _carrier.ObservedChargePower); Assert.Zero(_carrier.ObservedPektusSpin);
            Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void StillOwnedReaderWithdrawalRetiresLungeAndItsTell()
        {
            LungeWindup(); NetAuthority.Provider = new Client(1); _verbs.ApplyObservedLungeCharge(true, .4f); Withdraw();
            Assert.Zero(_verbs.LungeChargeRatio); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
            Assert.Zero(_verbs.LungeCooldownLeft); Assert.Zero(Contact);
        }
        [Test] public void ObsoleteReaderWithdrawalKeepsAlreadyCommittedLungeAndCooldown()
        {
            Defender(); Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            Assert.Greater(contact, 0); Assert.Greater(cooldown, 0);
            NetAuthority.Provider = new Client(2); Withdraw();
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            Assert.Zero(_verbs.LungeChargeRatio);
        }
    }
}
