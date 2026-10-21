using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuGhostReplicationTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host; public int Seat = 1;
            public bool IsHost => Host; public bool IsNetworked => true;
            public int LocalSlot => Seat; public int LocalPeerId => Seat; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previous;
        private Peer _peer;
        private MatchRpc _rpc;
        private CharacterMotor _owner;
        [UnitySetUp] public IEnumerator Before()
        {
            _previous = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            _peer = new Peer(); NetAuthority.Provider = _peer; GameServices.Ensure();
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            for (int seat = 0; seat < 4; seat++)
            {
                var motor = new GameObject("Ghost peer body " + seat).AddComponent<CharacterMotor>(); motor.enabled = false;
                motor.PlayerSlot = seat; motor.RoundActive = true; motor.Mode = GameMode.HeroStrike;
                motor.transform.position = Vector3.right * seat * 3;
                var system = motor.gameObject.AddComponent<HeroAbilitySystem>(); system.enabled = false;
                system.BindHero(seat == 0 ? "nemu" : seat == 1 ? "dante" : seat == 2 ? "cheska" : "rafi");
                GameServices.Round.Register(motor); if (seat == 0) _owner = motor;
            }
            GameServices.Round.BeginRound();
            GameServices.Round.Lata = new GameObject("Replicated can").AddComponent<Lata>();
            GameServices.Round.Lata.enabled = false;
            var router = new GameObject("Ghost frame receiver"); router.SetActive(false); _rpc = router.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(_rpc, 123L);
        }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous; }

        private static NemuGhostPose Pose(byte kind, long id, byte phase = 1) => new NemuGhostPose
        { Kind = kind, Id = id, Phase = phase, Position = new Vector3(2, 0, 4), Forward = Vector3.forward,
            Age = 4, PhaseAge = .1f, Target = -1, Progress = kind == 2 ? .14f : 0, Flags = (byte)(kind == 4 ? 1 : 0) };
        private NemuGhostFrame Frame(params NemuGhostPose[] ghosts) => new NemuGhostFrame
        { Seat = 0, Scope = new GameplayActionScope { Match = 123, Round = 1, Epoch = _owner.MovementEpoch },
            Sequence = 1, RoundClock = GameServices.Round.TimeLeft, Ghosts = ghosts };
        private void Deliver(NemuGhostFrame frame, ulong sender = NetworkManager.ServerClientId)
        {
            using var writer = new FastBufferWriter(NemuGhostFrame.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(frame);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnNemuGhostsMsg", Hidden).Invoke(_rpc, new object[] { sender, reader });
        }
        [Test] public void BoundedCodecRejectsMalformedCompleteFrames()
        {
            var fetcher = Pose(4, 8); fetcher.Flags = 2; var keeper = Pose(4, 9); keeper.Flags = 3;
            var frame = Frame(Pose(1, 1), Pose(1, 2), Pose(2, 3), Pose(2, 4), Pose(3, 5), Pose(3, 6), Pose(4, 7), fetcher, keeper);
            using var writer = new FastBufferWriter(NemuGhostFrame.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(frame); Assert.AreEqual(NemuGhostFrame.MaxWireBytes, writer.Length);
            bool Reads(byte[] bytes)
            { var reader = new FastBufferReader(bytes, Allocator.Temp); try { return NemuGhostFrame.TryRead(ref reader, out _); } finally { reader.Dispose(); } }
            var valid = writer.ToArray(); Assert.IsTrue(Reads(valid));
            Assert.IsFalse(Reads(valid.Take(valid.Length - 1).ToArray()));
            Assert.IsFalse(Reads(valid.Concat(new byte[] { 0 }).ToArray()));
            frame.Ghosts[0].Age = float.NaN; Assert.IsFalse(frame.IsValid);
            frame.Ghosts[0] = Pose(1, 2); Assert.IsFalse(frame.IsValid, "Duplicate lifetimes were accepted.");
            frame.Ghosts[0] = Pose(1, 1); frame.Ghosts[6].Flags = 0; Assert.IsFalse(frame.IsValid, "A dormant ghost of no skill was accepted.");
            frame.Ghosts[6].Flags = 1; frame.Ghosts[6].Phase = 2; Assert.IsFalse(frame.IsValid, "A dormant ghost cannot lunge or carry.");
            frame.Ghosts = new NemuGhostPose[NemuGhostFrame.MaxGhosts + 1]; Assert.IsFalse(frame.IsValid);
        }
        [UnityTest] public IEnumerator OwnerObserverAndSpectatorHydrateTheSameThreeGhostsWithoutCasting()
        {
            foreach (int local in new[] { 0, 1, -1 })
            {
                _peer.Seat = local;
                var blind = Pose(1, 10 + local + 1, 3); blind.Target = 2;
                var skitter = Pose(2, 30 + local + 1, 2);
                var lookout = Pose(3, 50 + local + 1); lookout.Progress = .65f; lookout.Flags = 1;
                var frame = Frame(blind, skitter, lookout); frame.Sequence = local == 0 ? 1 : local == 1 ? 2 : 3;
                Deliver(frame); yield return null;
                var b = NemuBlindfold.Live.Single(g => g.isActiveAndEnabled);
                Assert.AreSame(GameServices.Round.PlayerAt(2), b.Victim, "A nearby different character replaced the host's victim.");
                Assert.IsTrue(NemuSkitter.Live.Single(g => g.isActiveAndEnabled).Carrying);
                var l = NemuLookout.Live.Single(g => g.isActiveAndEnabled);
                Assert.AreEqual(.65f, l.Lift); Assert.IsTrue(l.Scared);
                Assert.AreEqual(0, _owner.AbilitySystem.Kit.Skill1.CooldownRemaining, "Hydration recast/spent the skill.");
                foreach (var ghost in new Component[] { b, l, NemuSkitter.Live.Single(g => g.isActiveAndEnabled) })
                    Assert.IsTrue(ghost.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.sharedMaterial != null), "A replicated ghost had no drawable renderer.");
            }
        }
        [UnityTest] public IEnumerator FullFramesRetireMissingGhostsAndRejectForgedStaleOrForeignState()
        {
            var frame = Frame(Pose(2, 100, 2)); Deliver(frame, 99); Assert.IsEmpty(NemuSkitter.Live);
            Deliver(frame); yield return null; Assert.IsTrue(NemuSkitter.Live.Single(g => g.isActiveAndEnabled).Carrying);
            var empty = Frame(); empty.Sequence = 2; empty.Scope.Round = 2; Deliver(empty);
            Assert.IsTrue(NemuSkitter.Live.Any(g => g.isActiveAndEnabled));
            empty.Scope.Round = 1; empty.Ghosts = new[] { Pose(2, 100) }; empty.Ghosts[0].Progress = float.NaN; Deliver(empty);
            Assert.IsTrue(NemuSkitter.Live.Single(g => g.isActiveAndEnabled).Carrying);
            empty.Ghosts = Array.Empty<NemuGhostPose>(); Deliver(empty); yield return null;
            Assert.IsFalse(NemuSkitter.Live.Any(g => g.isActiveAndEnabled));
            Deliver(frame); Assert.IsFalse(NemuSkitter.Live.Any(g => g.isActiveAndEnabled), "A stale frame resurrected an ended lifetime.");
        }
        [UnityTest] public IEnumerator BodyEpochAndKitChangesRetireOldGhosts()
        {
            var frame = Frame(Pose(3, 200)); Deliver(frame); yield return null;
            _owner.AdoptMovementEpoch(_owner.MovementEpoch + 1);
            typeof(MatchRpc).GetMethod("TickNemuGhosts", Hidden).Invoke(_rpc, null);
            Assert.IsFalse(NemuLookout.Live.Any(g => g.isActiveAndEnabled));
            frame.Sequence++; Deliver(frame); Assert.IsFalse(NemuLookout.Live.Any(g => g.isActiveAndEnabled));
            frame.Scope.Epoch = _owner.MovementEpoch; Deliver(frame); yield return null;
            _owner.AbilitySystem.BindHero("dante");
            typeof(MatchRpc).GetMethod("TickNemuGhosts", Hidden).Invoke(_rpc, null);
            Assert.IsFalse(NemuLookout.Live.Any(g => g.isActiveAndEnabled));
        }
        [UnityTest] public IEnumerator HostCaptureHydratesLateJoinAndTransportEndClearsEveryReplica()
        {
            _peer.Host = true;
            NemuBlindfold.Place(new Vector3(2, 0, 5), Vector3.forward, 0, 4);
            var skitter = NemuSkitter.Summon(new Vector3(3, 0, 2), Vector3.forward, 0); skitter.Grab(.2f, true);
            var lookout = NemuLookout.Summon(GameServices.Round.Lata.transform, Vector3.right, 0); lookout.Lift = .55f;
            var frame = (NemuGhostFrame)typeof(MatchRpc).GetMethod("CaptureNemuGhosts", Hidden).Invoke(_rpc, new object[] { 0 });
            Assert.IsTrue(frame.IsValid); Assert.AreEqual(3, frame.Ghosts.Length);
            WorldEffectSnapshot.ClearPersistentFields(); _peer.Host = false;
            Deliver(frame); yield return null;
            Assert.IsTrue(NemuSkitter.Live.Single(g => g.isActiveAndEnabled).Carrying);
            Assert.AreEqual(.55f, NemuLookout.Live.Single(g => g.isActiveAndEnabled).Lift);
            Assert.AreEqual(4f, NemuBlindfold.Live.Single(g => g.isActiveAndEnabled).Age, .1f);
            _rpc.CancelNemuGhostPresentation();
            Assert.IsFalse(NemuSkitter.Live.Any(g => g.isActiveAndEnabled));
            Assert.IsFalse(NemuLookout.Live.Any(g => g.isActiveAndEnabled));
            Assert.IsFalse(NemuBlindfold.Live.Any(g => g.isActiveAndEnabled));
        }
        /// <summary>SOULBOUND and PEEKER's ring (owner 2026-10-21): a dormant ghost's skill counts its 10 s window down, takes
        /// 15 s when reclaimed and 25 s when the window runs out, rides the host's ghost frames to every peer (faded and
        /// reclaimed apart), and Peeker's 2 m trigger ring is drawn while it waits.
        /// Bodies stand at x = 0 (Nemu), 3, 6 and 9.</summary>
        [UnityTest] public IEnumerator DormantGhostsHoldTheirSkillRideTheFramesAndPeekerDrawsItsRing()
        {
            _peer.Host = true; _peer.Seat = 0;
            var kit = _owner.AbilitySystem.Kit;
            var context = new AbilityContext(_owner, null, null);
            kit.Skill1.ApplyNetworkSnapshot(NecroRules.DontLookCooldown, 0);
            var dormant = NemuDormant.Spawn(NemuDormant.Ghost.Peeker, new Vector3(1.5f, 0, 1.5f), Vector3.forward, 0);
            yield return null;
            kit.Tick(context, 5f);
            Assert.AreEqual(dormant.TimeLeft, kit.Skill1.CooldownRemaining, .05f, "The tile should count the dormant window down.");
            Assert.AreEqual(0f, kit.AttackingSkill.CooldownRemaining, "Another skill's clock was touched.");

            var frame = (NemuGhostFrame)typeof(MatchRpc).GetMethod("CaptureNemuGhosts", Hidden).Invoke(_rpc, new object[] { 0 });
            Assert.IsTrue(frame.IsValid); Assert.AreEqual(1, frame.Ghosts.Length);
            Assert.AreEqual(4, frame.Ghosts[0].Kind); Assert.AreEqual((byte)NemuDormant.Ghost.Peeker, frame.Ghosts[0].Flags);

            dormant.Reclaim();
            kit.Tick(context, 1f);
            Assert.AreEqual(NecroRules.DontLookCooldown, kit.Skill1.CooldownRemaining, 1e-3f, "Reclaimed in time: the normal cooldown.");

            NemuBlindfold.Place(new Vector3(4.5f, 0, 6), Vector3.forward, 0, 4f);
            yield return null;
            Assert.AreEqual(1, TumbangPreso.Visual.NemuGhostRings.Count, "Peeker's armed trap keeps its trigger ring; a reclaimed ghost has none.");

            WorldEffectSnapshot.ClearPersistentFields(); _peer.Host = false; _peer.Seat = 1;
            yield return null;
            Deliver(frame); yield return null;
            var replica = NemuDormant.Live.Single(g => g.isActiveAndEnabled);
            Assert.AreEqual(NemuDormant.Ghost.Peeker, replica.Kind);
            Assert.AreEqual("Peeker", replica.Name);
            Assert.IsTrue(replica.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.sharedMaterial != null), "A dormant ghost had no drawable renderer.");

            // Its window runs out on the host: the faded frame reaches the owner, whose skill takes the longer cooldown.
            kit.Tick(context, .01f);
            Assert.AreEqual(replica.TimeLeft, kit.Skill1.CooldownRemaining, .05f);
            var faded = frame; faded.Sequence = 2; faded.Ghosts = (NemuGhostPose[])frame.Ghosts.Clone();
            faded.Ghosts[0].Phase = 3; faded.Ghosts[0].PhaseAge = .05f; faded.Ghosts[0].Age = NemuRules.DormantSeconds - .2f;
            Assert.IsTrue(faded.IsValid);
            Deliver(faded); yield return null;
            Assert.IsTrue(replica.Expired);
            kit.Tick(context, .01f);
            Assert.AreEqual(NecroRules.SoulboundMissedCooldown, kit.Skill1.CooldownRemaining, .02f, "Missed, the skill should take 25 s.");
        }
        [Test] public void ExpiredHydrationAndChangedGhostKindsCannotResurrectOldState()
        {
            var pose = Pose(1, 901); pose.Age = 29.8f;
            var frame = Frame(pose); frame.RoundClock += 1;
            Deliver(frame); Assert.IsFalse(NemuBlindfold.Live.Any(g => g.isActiveAndEnabled));
            frame = Frame(Pose(2, 902, 2)); frame.Sequence = 2; Deliver(frame);
            frame.Ghosts = new[] { Pose(3, 902) }; frame.Sequence = 3; Deliver(frame);
            Assert.IsFalse(NemuLookout.Live.Any(g => g.isActiveAndEnabled));
            frame.Ghosts = Array.Empty<NemuGhostPose>(); Deliver(frame);
            Assert.IsFalse(NemuSkitter.Live.Any(g => g.isActiveAndEnabled));
        }
#if UNITY_EDITOR
        [Test] public void MixedCurrentRosterCutscenesCopyActualParticipantsAndLeaveLiveBodiesAlone()
        {
            string[] heroes = { "dante", "cheska", "sean", "zack", "nemu", "phaister", "rafi", "amihan", "paete" };
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            var sourceModels = heroes.ToDictionary(id => id, id => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/TumbangPreso/Art/CharacterRedesign/" + id + "/" + id + "-redesign.glb"));
            foreach (var item in sourceModels) Assert.IsNotNull(item.Value, item.Key + " current model missing.");
            int copied = 0;
            for (int layout = 0; layout < 9; layout++)
            {
                for (int seat = 0; seat < 4; seat++)
                {
                    var visual = players[seat].GetComponent<Visual.CharacterVisual>() ?? players[seat].gameObject.AddComponent<Visual.CharacterVisual>();
                    visual.enabled = false;
                    visual.ApplyModel(sourceModels[seat == 0 ? "nemu" : heroes[(layout + seat - 1) % heroes.Length]], Color.white);
                    players[seat].transform.position = new Vector3((seat - 1) * 2f, 0, seat * 2f + 1);
                }
                var live = players.SelectMany(p => p.GetComponent<Visual.CharacterVisual>().Model.GetComponentsInChildren<Transform>(true)).ToArray();
                var positions = live.Select(t => t.localPosition).ToArray(); var rotations = live.Select(t => t.localRotation).ToArray();
                var parent = new GameObject("Mixed roster introduction");
                Visual.HeroIntroductionScene scene = null;
                try
                {
                    var track = new CameraSystem.MatchPoseHistory.Track(_owner, _owner.GetComponent<Visual.CharacterVisual>().Model);
                    track.Record(0); track.Record(.05f); var copy = track.Clone(parent.transform);
                    scene = new Visual.HeroIntroductionScene(parent.transform, "nemu", _owner, copy);
                    for (int seat = 1; seat < 4; seat++)
                    {
                        var holder = scene.Root.transform.Find("IntroductionOther-P" + (seat + 1));
                        Assert.IsNotNull(holder, "A selected character disappeared from the cutscene.");
                        int MeshVertices(Renderer renderer) => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh.vertexCount
                            : renderer.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount ?? 0;
                        var expected = players[seat].GetComponent<Visual.CharacterVisual>().Model.GetComponentsInChildren<Renderer>(true)
                            .Select(MeshVertices).Where(n => n > 0).Distinct().ToArray();
                        var actual = holder.GetComponentsInChildren<Renderer>(true).Select(MeshVertices).ToArray();
                        foreach (int count in expected) CollectionAssert.Contains(actual, count, "Another character's render geometry replaced this seat.");
                        Assert.IsEmpty(holder.GetComponentsInChildren<MonoBehaviour>(true), "The stage copied live gameplay scripts.");
                        copied++;
                    }
                    foreach (float at in new[] { 0f, 1f, 2.5f, 4f, 5.5f, scene.Seconds - .01f })
                    {
                        scene.Sample(at, audible: false);
                        foreach (var node in scene.Root.GetComponentsInChildren<Transform>(true))
                            Assert.IsTrue(float.IsFinite(node.position.sqrMagnitude), "A different rig produced an invalid cutscene pose.");
                    }
                    for (int i = 0; i < live.Length; i++)
                    { Assert.AreEqual(positions[i], live[i].localPosition); Assert.Less(Quaternion.Angle(rotations[i], live[i].localRotation), .01f); }
                }
                finally { scene?.Dispose(); Object.DestroyImmediate(parent); }
            }
            Assert.AreEqual(27, copied);
            TestContext.WriteLine("mixed_rosters=9 current_hero_models=9 copied_participants=27 sample_times=6 live_bone_mutations=0");
        }
#endif

        [UnityTest, Timeout(30000)] public IEnumerator HostDeliversCompleteFramesToTwoActualUdpClients() => Delivery(false);
        [UnityTest, Explicit, Category("OnlineNetwork"), Timeout(120000)] public IEnumerator RealRelayDeliversCompleteFramesToOwnerAndObserver() => Delivery(true);
        private IEnumerator Delivery(bool relay)
        {
            NetworkManager Make(string name)
            {
                var go = new GameObject(name); var transport = go.AddComponent<UnityTransport>();
                transport.SetConnectionData("127.0.0.1", 19865);
                var manager = go.AddComponent<NetworkManager>();
                manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = false };
                return manager;
            }
            var host = Make("Ghost UDP host"); var owner = Make("Ghost UDP owner"); var observer = Make("Ghost UDP observer");
            int receivedOwner = 0, receivedObserver = 0;
            try
            {
                if (relay)
                {
                    const string profile = "tp-ghost-relay-20261015";
                    if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    {
                        var init = UnityServices.InitializeAsync(new InitializationOptions().SetProfile(profile));
                        while (!init.IsCompleted) yield return null;
                        Assert.IsFalse(init.IsFaulted, "Relay service initialization failed: " + init.Exception?.GetBaseException().Message);
                    }
                    Assert.AreEqual(profile, AuthenticationService.Instance.Profile, "Refusing to use another account's live authentication session.");
                    var auth = AuthenticationService.Instance.SignInAnonymouslyAsync();
                    while (!auth.IsCompleted) yield return null;
                    Assert.IsFalse(auth.IsFaulted, "Relay test authentication failed: " + auth.Exception?.GetBaseException().Message);
                    var allocation = RelayService.Instance.CreateAllocationAsync(2);
                    while (!allocation.IsCompleted) yield return null;
                    Assert.IsFalse(allocation.IsFaulted, "Relay allocation failed: " + allocation.Exception?.GetBaseException().Message);
                    var code = RelayService.Instance.GetJoinCodeAsync(allocation.Result.AllocationId);
                    while (!code.IsCompleted) yield return null;
                    Assert.IsFalse(code.IsFaulted, "Relay code request failed.");
                    var first = RelayService.Instance.JoinAllocationAsync(code.Result);
                    var second = RelayService.Instance.JoinAllocationAsync(code.Result);
                    while (!first.IsCompleted || !second.IsCompleted) yield return null;
                    Assert.IsFalse(first.IsFaulted || second.IsFaulted, "Relay client allocation failed.");
                    ((UnityTransport)host.NetworkConfig.NetworkTransport).SetRelayServerData(allocation.Result.ToRelayServerData("dtls"));
                    ((UnityTransport)owner.NetworkConfig.NetworkTransport).SetRelayServerData(first.Result.ToRelayServerData("dtls"));
                    ((UnityTransport)observer.NetworkConfig.NetworkTransport).SetRelayServerData(second.Result.ToRelayServerData("dtls"));
                }
                Assert.IsTrue(host.StartHost()); Assert.IsTrue(owner.StartClient()); Assert.IsTrue(observer.StartClient());
                float deadline = Time.realtimeSinceStartup + 10;
                while ((!owner.IsConnectedClient || !observer.IsConnectedClient) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(owner.IsConnectedClient && observer.IsConnectedClient);
                owner.CustomMessagingManager.RegisterNamedMessageHandler("NemuGhosts", (sender, reader) =>
                { if (sender == 0 && NemuGhostFrame.TryRead(ref reader, out var f) && f.Ghosts.Length == 3) receivedOwner++; });
                observer.CustomMessagingManager.RegisterNamedMessageHandler("NemuGhosts", (sender, reader) =>
                { if (sender == 0 && NemuGhostFrame.TryRead(ref reader, out var f) && f.Ghosts.Length == 3) receivedObserver++; });
                typeof(MatchRpc).GetField("_nm", Hidden).SetValue(_rpc, host);
                var frame = Frame(Pose(1, 301), Pose(2, 302, 2), Pose(3, 303));
                typeof(MatchRpc).GetMethod("SendNemuGhostFrame", Hidden).Invoke(_rpc, new object[] { frame, null });
                deadline = Time.realtimeSinceStartup + 5;
                while ((receivedOwner == 0 || receivedObserver == 0) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, receivedOwner); Assert.AreEqual(1, receivedObserver);
            }
            finally { owner.Shutdown(); observer.Shutdown(); host.Shutdown(); }
            while (owner.IsListening || observer.IsListening || host.IsListening) yield return null;
            Object.Destroy(owner.gameObject); Object.Destroy(observer.gameObject); Object.Destroy(host.gameObject);
        }
    }
}
