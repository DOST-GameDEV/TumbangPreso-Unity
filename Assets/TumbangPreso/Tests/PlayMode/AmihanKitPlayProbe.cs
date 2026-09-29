using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Netcode;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN, FILMED IN A MATCH (HERO-8, owner 2026-09-26: *"thoroughly make sure amihan's animations look great a bug i
    /// found last time was her yellow circle looked weird as fuck when she was floating"*, *"she didnt have a flaot animation too
    /// and any VFX to indicate her shit as well"*). `PaeteKitPlayProbe`'s films are the template (`HERO_KIT_METHOD.md` section 7):
    /// a real Hero Strike match on Bayan Plaza, every ability pressed through `InputIntent` as a player presses it, host-resolved,
    /// filmed on her screen, from the court and over the shoulder of whoever the wind hits, at a fixed 30 fps game clock, with
    /// every world cue logged for `tools/stitch_ability_film.py`. Runs only with TUMP_AMIHAN_FILM=1; frames under TUMP_EVIDENCE.
    /// </summary>
    public sealed partial class AmihanKitPlayProbe
    {
        private INetProvider _net;
        private readonly StringBuilder _log = new StringBuilder();

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            _log.Clear();
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(10 + player.PlayerSlot * 2, .12f, -10));
            }
        }

        [UnityTearDown] public IEnumerator After()
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/amihan-play.csv", _log.ToString());
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private void Note(string claim, object value) => _log.AppendLine(FormattableString.Invariant($"{claim},{value}"));

        /// <summary>
        /// A seat played as Amihan: her kit AND her body (the Paete film's lesson, 2026-09-26: re-binding only the kit left a
        /// human casting his skills), and the local seat's first-person arms matched to her.
        /// </summary>
        private static CharacterMotor Amihan(int slot, Vector3 at)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "amihan");
            who.AbilitySystem.BindHero("amihan");
            var art = RosterBook.Load().FindPersonArt("amihan");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (slot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(who);
            who.Teleport(at); who.transform.rotation = Quaternion.identity;
            who.Intent.Parked = false; who.IsBot = slot != GameLaunch.SoloSeat;
            return who;
        }

        /// <summary>The Paete film's witness render: every body as a spectator sees it, never the private first-person arms.</summary>
        private static void RenderFilmView(Camera c, RenderTexture target)
        {
            bool witness = c != Camera.main;
            var bodies = new System.Collections.Generic.List<Renderer>();
            var arms = new System.Collections.Generic.List<Renderer>();
            if (witness)
            {
                foreach (var p in GameServices.Round.Players)
                    foreach (var r in p.GetComponentsInChildren<Renderer>())
                        if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                        { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; bodies.Add(r); }
                foreach (var a in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                    foreach (var r in a.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; arms.Add(r); }
            }
            try
            {
                var before = c.targetTexture; c.targetTexture = target; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
            }
            finally
            {
                foreach (var r in bodies) if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                foreach (var r in arms) if (r != null) r.enabled = true;
            }
        }

        private sealed class Film : IDisposable
        {
            public readonly string Root;
            private readonly RenderTexture _hdr, _ldr;
            private readonly Texture2D _pixels;
            public readonly StringBuilder Cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            public int Frame;
            private readonly Action<string, Vector3, float, float> _heard;
            private readonly int _previousRate;
            public Film(string name, params string[] views)
            {
                Root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", name);
                foreach (string view in views) Directory.CreateDirectory(Path.Combine(Root, view));
                _hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
                _ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                _heard = (id, at, pitch, gain) =>
                    Cues.AppendLine(FormattableString.Invariant($"{Frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
                AudioDirector.WorldCuePlayed += _heard;
                _previousRate = Time.captureFramerate;
                Time.captureFramerate = 30;
            }
            public void Save(Texture source, string view)
            {
                Graphics.Blit(source, _ldr);
                var active = RenderTexture.active; RenderTexture.active = _ldr;
                _pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); _pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(Root, view, $"{Frame:D5}.jpg"), _pixels.EncodeToJPG(92));
            }
            public void Shoot(Camera c, string view) { RenderFilmView(c, _hdr); Save(_hdr, view); }
            public static Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            public void Dispose()
            {
                AudioDirector.WorldCuePlayed -= _heard;
                File.WriteAllText(Path.Combine(Root, "cues.csv"), Cues.ToString());
                Time.captureFramerate = _previousRate;
                _hdr.Release(); _ldr.Release();
                Object.Destroy(_hdr); Object.Destroy(_ldr); Object.Destroy(_pixels);
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        private static void Face(CharacterMotor who, Vector3 toward)
        {
            var d = toward - who.transform.position; d.y = 0f;
            if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
        }

        private static IEnumerator PressSkill(CharacterMotor actor, Verb verb)
        {
            actor.Intent.Set(verb, true);
            actor.Intent.BufferPress(verb);
            yield return null;
            actor.Intent.Set(verb, false);
            yield return null;
        }

        private sealed class FlightClient : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot { get; set; }
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallTakeoffReceiptsCannotResurrectOrUnwindNewerInput()
        {
            yield return LocalAttacker();
            var actor = Amihan(GameLaunch.SoloSeat, new Vector3(0, .12f, -11.5f));
            yield return null;
            var previous = NetAuthority.Provider;
            NetAuthority.Provider = new FlightClient { LocalSlot = actor.PlayerSlot };
            var system = actor.AbilitySystem;
            var context = new AbilityContext(actor, actor.GetComponent<Carrier>(), actor.GetComponent<CombatVerbs>());
            var intent = typeof(MatchRpc).GetMethod("ValidFeatherfallIntent", BindingFlags.Static | BindingFlags.NonPublic);
            bool Valid(long episode) => (bool)intent.Invoke(null, new object[] { actor, 1, episode });
            void Predict(long request)
            {
                Assert.IsTrue(Valid(0));
                Assert.AreEqual(HeroKit.CastOutcome.Cast, system.Kit.CastSkill2(context));
                system.TrackSkillRequest(1, request);
                Assert.AreEqual(request, actor.FlightEpisode);
                Assert.IsFalse(Valid(0), "A new-takeoff intent must not become a recast of active flight.");
                Assert.IsTrue(Valid(request));
            }
            void Recast(long request)
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast, system.Kit.CastSkill2(context));
                system.TrackSkillRequest(1, request);
                Assert.IsFalse(actor.IsAloft);
            }
            try
            {
                Predict(501); Recast(502);
                float currentCooldown = system.Kit.Skill2.CooldownRemaining;
                Assert.IsTrue(system.PendingSkillReceipt(1, 501));
                Assert.IsTrue(system.ResolveSkillReceipt(1, 501, false, 0, 0));
                Assert.IsFalse(actor.IsFlying);
                Assert.AreEqual(0, actor.FlightEpisode);
                Assert.AreEqual(currentCooldown, system.Kit.Skill2.CooldownRemaining, "An older A denial overwrote B's pending readiness.");
                Assert.IsFalse(Valid(501), "A denied takeoff's dependent recast became a fresh takeoff.");
                Assert.IsFalse(system.ResolveSkillReceipt(1, 501, false, 0, 0));
                Assert.IsTrue(system.ResolveSkillReceipt(1, 502, false, 0, 0));

                Predict(601); Recast(602);
                Assert.IsTrue(system.ResolveSkillReceipt(1, 601, true, 0, 0));
                Assert.IsFalse(actor.IsAloft, "A acceptance after local B re-lifted the body.");
                Assert.AreEqual(601, actor.FlightEpisode);
                Assert.AreEqual(40, system.Kit.Skill2.CooldownRemaining);
                Assert.IsTrue(system.ResolveSkillReceipt(1, 602, true, 39, 0));
                Assert.IsFalse(system.ResolveSkillReceipt(1, 601, false, 0, 0));
                system.ResetKit();

                Predict(701); Recast(702);
                Assert.IsTrue(system.ResolveSkillReceipt(1, 702, false, 0, 0));
                Assert.IsTrue(system.PendingSkillReceipt(1, 701));
                Assert.IsTrue(system.ResolveSkillReceipt(1, 701, false, 35, 0));
                Assert.AreEqual(0, system.Kit.Skill2.CooldownRemaining, "Out-of-order old A overwrote settled B's resources.");
                Assert.IsFalse(actor.IsFlying);
                Predict(801);
                Assert.IsFalse(system.ResolveSkillReceipt(1, 701, false, 0, 0));
                Assert.IsTrue(actor.IsAloft, "An old denial unwound newer C.");
                Assert.AreEqual(801, actor.FlightEpisode);
                system.ResetKit();
                Assert.IsFalse(Valid(801), "Reset retained a prior-round recast identity.");
                Assert.AreEqual(0, actor.FlightEpisode);
                Assert.IsTrue(((AmihanHeroKit)system.Kit).RestoreFeatherfall(actor, 0, actor.FlightCeiling, 0, 0, -41));
                Predict(901);
                Assert.IsTrue(system.ResolveSkillReceipt(1, 901, false, 0, 0));
                Assert.IsFalse(actor.IsFlying);
                Assert.AreEqual(-41, actor.FlightEpisode, "A refused prediction lost its accepted predecessor's pose identity.");
                Assert.IsFalse(actor.RestoreFlight(1, actor.FlightCeiling, 1, -41), "A refusal lost the predecessor's terminal marker.");
                Assert.IsFalse(system.ResolveSkillReceipt(1, 901, false, 0, 0));
            }
            finally
            {
                system.ResetKit();
                NetAuthority.Provider = previous;
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallRestoreKeepsAbsoluteHeightAndThePausedClock()
        {
            yield return LocalAttacker();
            var actor = Amihan(GameLaunch.SoloSeat, new Vector3(0, 2.1f, -11.5f));
            yield return null;
            var kit = (AmihanHeroKit)actor.AbilitySystem.Kit;
            kit.AttackingSkill.ApplyNetworkSnapshot(31, 0);
            int casts = 0;
            void Heard(string id, Vector3 at, float pitch, float gain)
            { if (id == "sfx_cast_amihan_updraft") casts++; }
            AudioDirector.WorldCuePlayed += Heard;
            var hold = typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic);
            var release = typeof(PresentationClock).GetMethod("Release", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                var before = actor.transform.position;
                Assert.IsTrue(kit.RestoreFeatherfall(actor, 3, 2.92f, 1, 10, -5));
                Assert.AreEqual(before, actor.transform.position, "Restore added a second lift to the received position.");
                Assert.AreEqual(2.92f, actor.FlightCeiling);
                actor.Intent.Move = Vector2.right * .7f;
                yield return new WaitForSeconds(.2f);
                actor.Intent.Move = Vector2.zero;
                hold.Invoke(null, null);
                before = actor.transform.position;
                float clock = GameServices.Round.TimeLeft;
                float pausedRemaining = kit.AttackingSkill.DurationRemaining;
                float pausedCooldown = kit.AttackingSkill.CooldownRemaining;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.AreEqual(clock, GameServices.Round.TimeLeft, .001f);
                Assert.AreEqual(pausedRemaining, kit.AttackingSkill.DurationRemaining, .001f);
                Assert.AreEqual(pausedCooldown, kit.AttackingSkill.CooldownRemaining, .001f);
                Assert.AreEqual(before, actor.transform.position);
                Transform torso = null;
                foreach (var skin in actor.GetComponent<CharacterVisual>().Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if (skin.enabled) foreach (var bone in skin.bones) if (bone != null && bone.name == "torso") torso = bone;
                Assert.IsNotNull(torso);
                var pausedPose = torso.localRotation;
                yield return null; yield return null;
                Assert.Less(Quaternion.Angle(pausedPose, torso.localRotation), .01f, "Flight offsets accumulated while the graph was paused.");
                Assert.IsTrue(kit.RestoreFeatherfall(actor, 2.5f, 2.92f, 1, 11, -5));
                Assert.AreEqual(1, actor.GetComponents<AmihanFlightPose>().Length);
                Assert.AreEqual(1, actor.GetComponentsInChildren<AmihanHoverRing>().Length);
                Assert.AreEqual(0, casts);
                Assert.IsFalse(kit.RestoreFeatherfall(actor, float.NaN, 2.92f, 1, 12, -5));
                Assert.IsFalse(kit.RestoreFeatherfall(actor, 6, 2.92f, 1, 12, -5));
                Assert.IsFalse(kit.RestoreFeatherfall(actor, 2, float.PositiveInfinity, 1, 12, -5));
                var pose = actor.GetComponent<AmihanFlightPose>();
                pose.enabled = false;
                var unlayered = torso.localRotation;
                pose.enabled = true;
                typeof(AmihanFlightPose).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pose, null);
                Assert.Greater(Quaternion.Angle(unlayered, torso.localRotation), .1f, "The cleanup check never exercised a real flight lean.");
                Assert.IsTrue(kit.RestoreFeatherfall(actor, 0, 2.92f, 2, 12, -5));
                Assert.IsFalse(actor.IsAloft);
                Assert.IsTrue(actor.IsFlying);
                Assert.IsTrue(kit.RestoreFeatherfall(actor, 0, 2.92f, 0, 13, -5));
                Assert.IsFalse(actor.IsFlying);
                Assert.Less(Quaternion.Angle(unlayered, torso.localRotation), .01f, "Interrupted flight left a residual lean on the paused rig.");
            }
            finally
            {
                release.Invoke(null, null);
                AudioDirector.WorldCuePlayed -= Heard;
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallReplicaRejectsOldEpisodesEvenWhenAllAirbornePacketsAreLost()
        {
            yield return LocalAttacker();
            var actor = Amihan(GameLaunch.SoloSeat, new Vector3(0, .12f, -11.5f));
            yield return null;
            var previous = NetAuthority.Provider;
            NetAuthority.Provider = new FlightClient { LocalSlot = (actor.PlayerSlot + 1) % 4 };
            try
            {
                var ground = actor.transform.position;
                Assert.IsFalse(actor.AcceptsFlightPoseEpisode(201), "A predicted pose must wait for accepted takeoff identity.");
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(100));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true);
                actor.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
                var identify = typeof(MatchRpc).GetMethod("IdentifyFeatherfallTakeoff", BindingFlags.Static | BindingFlags.NonPublic);
                identify.Invoke(null, new object[] { actor, 1, 201L });
                Assert.IsTrue(actor.AcceptsFlightPoseEpisode(201));
                actor.EndFlight();
                identify.Invoke(null, new object[] { actor, 1, 202L });
                Assert.AreEqual(201, actor.FlightEpisode, "A rapid recast replaced the takeoff identity.");
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(actor.IsFlying, "Cached preflight grounded state ended a new descent.");
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(101));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true);
                Assert.IsTrue(actor.IsFlying, "A preflight grounded receipt ended the episode.");
                var evidence = new object[] { false, 0L };
                typeof(CharacterMotor).GetMethod("FlightPoseEvidence", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actor, evidence);
                Assert.AreEqual(true, evidence[0]);
                Assert.AreEqual(0L, evidence[1], "Forwarding relabelled a preflight pose with the current flight key.");
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(102));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true, 200);
                Assert.IsFalse(actor.AcceptNetworkPoseSerial(101), "An out-of-order pose must be rejected before application.");
                Assert.IsTrue(actor.IsFlying);
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(103));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true, 201);
                Assert.IsFalse(actor.IsFlying);
                Assert.IsTrue(actor.IsGrounded);
                Assert.IsFalse(actor.RestoreFlight(1, actor.FlightCeiling, 104, 201), "A completed episode was lifted again.");
                Assert.IsFalse(actor.RestoreFlight(2, actor.FlightCeiling, 104, 201), "A completed episode replayed descent.");
                actor.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
                identify.Invoke(null, new object[] { actor, 1, -22L });
                actor.EndFlight();
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(104));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true, 201);
                Assert.IsTrue(actor.IsFlying, "The previous flight's landing ended the second flight.");
                Assert.IsTrue(actor.AcceptNetworkPoseSerial(105));
                actor.ApplyNetworkTransform(ground, 0, Vector3.zero, true, false, true, -22);
                Assert.IsFalse(actor.IsFlying, "A host/bot negative episode failed to land.");
                actor.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
                identify.Invoke(null, new object[] { actor, 1, 301L });
                actor.AdoptMovementEpoch(actor.MovementEpoch + 1);
                Assert.IsFalse(actor.IsFlying);
                Assert.AreEqual(0, actor.FlightEpisode);
            }
            finally { NetAuthority.Provider = previous; }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallHardStatusesKeepTheMovementEpisodeAfterLandingAndExpiry()
        {
            yield return LocalAttacker();
            var owner = Amihan(GameLaunch.SoloSeat, new Vector3(0, .12f, -11.5f));
            CharacterMotor host = null;
            foreach (var player in GameServices.Round.Players)
                if (player != owner && !player.IsDefender) { host = Amihan(player.PlayerSlot, new Vector3(4, .12f, -11.5f)); break; }
            Assert.IsNotNull(host);
            host.Intent.Parked = true;
            yield return new WaitForSeconds(.2f);
            var authority = NetAuthority.Provider;
            var client = new FlightClient { LocalSlot = owner.PlayerSlot };
            var ownerKit = (AmihanHeroKit)owner.AbilitySystem.Kit;
            var hostKit = (AmihanHeroKit)host.AbilitySystem.Kit;
            var ownerContext = new AbilityContext(owner, owner.GetComponent<Carrier>(), owner.GetComponent<CombatVerbs>());
            var hostContext = new AbilityContext(host, host.GetComponent<Carrier>(), host.GetComponent<CombatVerbs>());
            long episode = 910;
            void ClearStatusPair()
            {
                NetAuthority.Provider = authority;
                host.EndRooted(); host.ClearStun(); host.ClearTrip();
                NetAuthority.Provider = client;
                owner.ApplyNetworkStatuses(0, 0, 0);
                owner.ApplyNetworkState(0, 0, StunElement.None, 3, 0, 0, 0, 0, 0, 100, 0, 0);
            }
            void RestorePair(byte phase)
            {
                ClearStatusPair();
                episode++;
                Assert.IsTrue(hostKit.RestoreFeatherfall(host, 0, 2.92f, phase, 0, episode));
                Assert.IsTrue(ownerKit.RestoreFeatherfall(owner, 0, 2.92f, phase, 0, episode));
                Assert.AreEqual(0, hostKit.AttackingSkill.DurationRemaining);
                Assert.AreEqual(0, ownerKit.AttackingSkill.DurationRemaining);
            }
            void StoppedWithMatchingIdentity()
            {
                Assert.IsFalse(host.IsFlying, "The authority's hard status did not stop flight.");
                Assert.IsFalse(owner.IsFlying, "The network status did not stop the owner's expired flight.");
                Assert.AreEqual(episode, host.FlightEpisode, "The authority invalidated a still-current movement key.");
                Assert.AreEqual(episode, owner.FlightEpisode);
                Assert.IsTrue(host.AcceptsFlightPoseEpisode(owner.FlightEpisode), "SubmitMove's episode gate would permanently reject this owner.");
            }
            try
            {
                // These are separate state copies, not a simulated transport. Zero duration is
                // intentional: HeroAbility.Tick cannot repair either settled or expired flight.
                foreach (byte phase in new byte[] { 0, 2 })
                {
                    RestorePair(phase);
                    NetAuthority.Provider = authority;
                    host.ApplyRooted(1);
                    NetAuthority.Provider = client;
                    owner.ApplyNetworkStatuses(0, 0, host.RootedLeft);
                    Assert.IsTrue(host.IsRooted && owner.IsRooted);
                    StoppedWithMatchingIdentity();
                    ClearStatusPair();
                    var before = owner.transform.position;
                    owner.Intent.Move = Vector2.right;
                    yield return new WaitForSeconds(.25f);
                    owner.Intent.Move = Vector2.zero;
                    Assert.Greater(Vector3.Distance(Flat(before), Flat(owner.transform.position)), .1f, "Movement did not resume after roots cleared.");
                    Assert.IsTrue(host.AcceptsFlightPoseEpisode(owner.FlightEpisode));
                }

                RestorePair(2);
                NetAuthority.Provider = authority;
                host.ApplyTagged();
                NetAuthority.Provider = client;
                owner.ApplyNetworkState(host.StunLeft, StatusRules.TaggedSeconds, StunElement.None, 3, 0, 0, 0, 0, 0, 100, 0, 0);
                Assert.IsTrue(host.IsTagged && owner.IsTagged);
                StoppedWithMatchingIdentity();

                RestorePair(2);
                NetAuthority.Provider = authority;
                host.ApplyTrip(1);
                NetAuthority.Provider = client;
                owner.ApplyNetworkState(0, 0, StunElement.None, 3, 0, host.TripLeft, 1, 0, 0, 100, 0, 0);
                Assert.IsTrue(host.IsTripped && owner.IsTripped);
                StoppedWithMatchingIdentity();

                ClearStatusPair();
                episode++;
                Assert.IsTrue(hostKit.RestoreFeatherfall(host, 1, 2.92f, 1, 0, episode));
                Assert.IsTrue(ownerKit.RestoreFeatherfall(owner, 1, 2.92f, 1, 0, episode));
                NetAuthority.Provider = authority;
                host.ApplyStagger(.7f, StunElement.Ice, 3);
                NetAuthority.Provider = client;
                owner.ApplyNetworkState(host.StunLeft, .7f, StunElement.Ice, 3, 0, 0, 0, 0, 0, 100, 0, 0);
                hostKit.AttackingSkill.Tick(hostContext, .02f);
                ownerKit.AttackingSkill.Tick(ownerContext, .02f);
                Assert.AreEqual(2, host.FlightPhase);
                Assert.AreEqual(2, owner.FlightPhase, "An ordinary Ice hold must glide, not abruptly stop flight.");
                Assert.Greater(host.StunLeft, 0);
                Assert.Greater(owner.StunLeft, 0);
                Assert.AreEqual(episode, host.FlightEpisode);
                Assert.AreEqual(episode, owner.FlightEpisode);

                ClearStatusPair();
                Assert.IsTrue(ownerKit.RestoreFeatherfall(owner, 1, 2.92f, 1, 0, episode));
                owner.AbilitySystem.ResetKit();
                Assert.AreEqual(0, owner.FlightEpisode, "OnCancelled lost the actor before Reset could invalidate its episode.");
                host.AdoptMovementEpoch(host.MovementEpoch + 1);
                Assert.AreEqual(0, host.FlightEpisode);
                Assert.IsFalse(host.AcceptsFlightPoseEpisode(episode));
            }
            finally
            {
                owner.Intent.Move = Vector2.zero;
                NetAuthority.Provider = authority;
                owner.AbilitySystem.ResetKit(); host.AbilitySystem.ResetKit();
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallSnapshotRefreshCoalescesProgressAndRejectsObsoleteTickets()
        {
            var root = new GameObject("Featherfall refresh state");
            root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            var network = root.AddComponent<NetworkManager>();
            var replacement = new GameObject("Replacement inactive transport");
            replacement.SetActive(false);
            var otherNetwork = replacement.AddComponent<NetworkManager>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object Call(string name, params object[] args) => typeof(MatchRpc).GetMethod(name, flags).Invoke(router, args);
            void Field(string name, object value) => typeof(MatchRpc).GetField(name, flags).SetValue(router, value);
            object Read(string name) => typeof(MatchRpc).GetField(name, flags).GetValue(router);
            long Queue(ulong peer) => (long)Call("QueueSnapshotReply", peer);
            bool Take(ulong peer, long ticket, float at) => (bool)Call("TakeSnapshotReply", peer, ticket, at);
            bool Budget(long request, long skillEvent) => (bool)Call("RecordFeatherfallRefresh", 1, 0, 0L, request, skillEvent);
            try
            {
                // The exact admission/claim helpers used by immediate and delayed replies.
                long first = Queue(17);
                Assert.IsTrue(Take(17, first, 10));
                long delayed = Queue(17);
                Assert.IsFalse(Take(17, delayed, 10.49f));
                Assert.AreEqual(0, Queue(17), "A second request created another deferred reply.");
                Assert.IsTrue(Take(17, delayed, 10.5f));
                Assert.IsFalse(Take(17, delayed, 10.5f), "Immediate and queued paths both claimed the same reply.");
                long departed = Queue(17);
                Call("CancelSnapshotReply", 17UL);
                long returned = Queue(17);
                Assert.Greater(returned, departed);
                Assert.IsFalse(Take(17, departed, 11), "A departed connection's callback claimed a reused peer ID.");
                Assert.IsTrue(Take(17, returned, 11));

                Assert.IsTrue(Budget(2, 20), "B must qualify the first refresh.");
                Assert.IsFalse(Budget(2, 20));
                Assert.IsTrue(Budget(3, 20), "C overtaking B's response needs one fresh budget.");
                Assert.IsFalse(Budget(2, 20));
                Assert.IsFalse(Budget(3, 20));
                Assert.IsTrue(Budget(3, 21));
                Assert.IsFalse(Budget(3, 21), "Repeated rejected packets rearmed a request without input/event progress.");

                Field("_nm", network);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 12345L);
                int round = GameServices.Match.RoundNumber;
                ulong scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle.GetRawData();
                Field("_preparationFollowupNetwork", network);
                Field("_preparationFollowupMessages", network.CustomMessagingManager);
                Field("_preparationFollowupClient", network.LocalClientId);
                Field("_preparationFollowupRound", round);
                Field("_preparationFollowupScene", scene);
                Field("_preparationFollowupMatch", 12345L);
                Assert.IsTrue((bool)Call("PreparationRefreshScopeMatches", round));
                Field("_preparationFollowupPending", true);
                Field("_preparationFollowupScene", scene + 1UL);
                Assert.IsFalse((bool)Call("PreparationRefreshScopeMatches", round),
                    "An old pending scene must not suppress a fresh scope's refresh.");
                Assert.AreEqual(true, Read("_preparationFollowupPending"));
                Field("_preparationFollowupScene", scene);
                Field("_preparationFollowupMatch", 12346L);
                Assert.IsFalse((bool)Call("PreparationRefreshScopeMatches", round));
                Field("_preparationFollowupMatch", 12345L);
                Assert.IsFalse((bool)Call("PreparationRefreshScopeMatches", round + 1));
                Field("_preparationFollowupClient", network.LocalClientId + 1);
                Assert.IsFalse((bool)Call("PreparationRefreshScopeMatches", round));
                Field("_preparationFollowupClient", network.LocalClientId);
                Field("_preparationFollowupTicket", 8L);
                Assert.IsFalse((bool)Call("CompletePreparationRefresh", 7L, round));
                Assert.AreEqual(true, Read("_preparationFollowupPending"), "An old callback cleared a newer pending refresh.");
                Assert.IsFalse((bool)Call("CompletePreparationRefresh", 8L, round), "An inactive disconnected receiver sent a refresh.");
                Assert.AreEqual(false, Read("_preparationFollowupPending"));

                long oldManager = Queue(17);
                router.Initialize(otherNetwork);
                long newManager = Queue(17);
                Assert.IsFalse(Take(17, oldManager, 12));
                Assert.IsTrue(Take(17, newManager, 12));
                long disabled = Queue(17);
                Call("OnDisable");
                Assert.IsFalse(Take(17, disabled, 13));
                Assert.IsFalse((bool)Call("SnapshotPeerConnected", 17UL), "An unconnected peer qualified for a host reply.");
                Note("featherfall_refresh_state_contract", true);
            }
            finally { Object.Destroy(root); Object.Destroy(replacement); }
            yield return null;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FeatherfallUnknownSwimmingOwnerHydratesIdentityWithoutRevivingMotion()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            yield return LocalAttacker();
            var water = new Vector3(-13, RooftopPool.SurfaceY - RooftopPool.FloatDepth, 6);
            var dry = new Vector3(0, .12f, 6);
            var owner = Amihan(GameLaunch.SoloSeat, water);
            CharacterMotor host = null;
            foreach (var player in GameServices.Round.Players)
                if (player != owner && !player.IsDefender) { host = Amihan(player.PlayerSlot, dry + Vector3.up * 2); break; }
            Assert.IsNotNull(host);
            Assert.IsTrue(owner.IsSwimming, "The identity check requires the actual existing water volume.");
            Assert.AreEqual(0, owner.FlightEpisode);
            var ownerKit = (AmihanHeroKit)owner.AbilitySystem.Kit;
            var hostKit = (AmihanHeroKit)host.AbilitySystem.Kit;
            Assert.IsTrue(hostKit.RestoreFeatherfall(host, 2, 2.92f, 1, 100, -10));
            Assert.IsFalse(host.IsSwimming);
            Assert.IsFalse(host.AcceptsFlightPoseEpisode(owner.FlightEpisode));
            var root = new GameObject("Interrupted flight snapshot receiver");
            root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            var previous = NetAuthority.Provider;
            NetAuthority.Provider = new FlightClient { LocalSlot = owner.PlayerSlot };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            void Field(string name, object value) => typeof(MatchRpc).GetField(name, flags).SetValue(router, value);
            void Snapshot(int generation, long episode, byte phase, long eventId)
            {
                Field("_lastWorldFieldGeneration", generation);
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(12345L); writer.WriteValueSafe(generation);
                writer.WriteValueSafe(owner.MovementEpoch); writer.WriteValueSafe(eventId); writer.WriteValueSafe(0L);
                writer.WriteValueSafe(GameServices.Round.TimeLeft); writer.WriteValueSafe(2.92f);
                writer.WriteValueSafe(phase); writer.WriteValueSafe((ulong)(100 + generation)); writer.WriteValueSafe(episode);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                typeof(MatchRpc).GetMethod("ReadFeatherfallSnapshot", flags).Invoke(router, new object[]
                    { reader, owner.PlayerSlot, GameServices.Match.RoundNumber, phase == 1 ? 2f : 0f, 0f, 0f, false });
            }
            int flightCues = 0;
            void Heard(string id, Vector3 at, float pitch, float gain)
            { if (id == "sfx_cast_amihan_updraft" || id == "sfx_amihan_updraft_settle") flightCues++; }
            AudioDirector.WorldCuePlayed += Heard;
            try
            {
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 12345L);
                Field("_worldFieldRound", GameServices.Match.RoundNumber);
                typeof(MatchRpc).GetMethod("AdoptFeatherfallRoundClock", flags).Invoke(router, new object[] { GameServices.Match.RoundNumber });
                var before = owner.transform.position;
                bool grounded = owner.IsGrounded;
                Snapshot(1, -10, 1, 20);
                Assert.AreEqual(-10, owner.FlightEpisode, "An interrupted unknown owner never learned the accepted pose key.");
                Assert.IsFalse(owner.IsFlying);
                Assert.AreEqual(0, ownerKit.AttackingSkill.DurationRemaining);
                Assert.AreEqual(before, owner.transform.position);
                Assert.AreEqual(grounded, owner.IsGrounded, "Identity hydration invented ground contact.");
                Assert.IsTrue(host.AcceptsFlightPoseEpisode(owner.FlightEpisode));
                Assert.IsTrue(host.IsAloft, "The test did not keep the host airborne until the admitted position arrived.");
                NetAuthority.Provider = previous;
                host.ApplyNetworkTransform(owner.transform.position, 0, Vector3.zero, false, false, true, owner.FlightEpisode);
                Assert.IsTrue(host.IsSwimming);
                Assert.IsFalse(host.IsFlying, "The host replica did not stop at the admitted swimming position.");
                Assert.AreEqual(-10, host.FlightEpisode);
                NetAuthority.Provider = new FlightClient { LocalSlot = owner.PlayerSlot };
                owner.ApplyNetworkTransform(dry, 0, Vector3.zero, false, false, true, owner.FlightEpisode);
                Assert.IsFalse(owner.IsSwimming);
                Snapshot(2, -10, 1, 21);
                Assert.IsFalse(owner.IsFlying, "A later active snapshot revived the interrupted episode after leaving water.");
                Snapshot(3, -10, 2, 21);
                Assert.IsFalse(owner.IsFlying, "A terminal episode replayed descent/landing.");
                Assert.AreEqual(0, flightCues);
                Snapshot(4, -22, 1, 22);
                Assert.IsTrue(owner.IsAloft, "The terminal marker poisoned a genuinely newer accepted flight.");
                Assert.AreEqual(-22, owner.FlightEpisode);
                Snapshot(5, -10, 0, 21);
                Assert.IsTrue(owner.IsAloft, "An older snapshot unwound newer C.");
                Assert.AreEqual(-22, owner.FlightEpisode);
                owner.EndFlight();
                ownerKit.AttackingSkill.Tick(new AbilityContext(owner, owner.GetComponent<Carrier>(), owner.GetComponent<CombatVerbs>()), AmihanRules.UpdraftSeconds);
                Assert.AreEqual(0, ownerKit.AttackingSkill.DurationRemaining);
                owner.transform.position = water;
                typeof(CharacterMotor).GetMethod("StepFlightVertical", flags).Invoke(owner, new object[] { .02f });
                Assert.IsTrue(owner.IsSwimming);
                Assert.IsFalse(owner.IsFlying, "An owner entering water after expiry retained descending motion.");
                Assert.AreEqual(-22, owner.FlightEpisode);
                Snapshot(6, 1, 0, 30);
                Assert.IsFalse(owner.RestoreFlight(1, 2.92f, 107, 1));
                typeof(MatchRpc).GetMethod("ResetFeatherfallTransport", flags).Invoke(router, null);
                Assert.AreEqual(0, owner.FlightEpisode);
                owner.ApplyNetworkTransform(dry, 0, Vector3.zero, false, false, true, 0);
                typeof(MatchRpc).GetMethod("AdoptFeatherfallRoundClock", flags).Invoke(router, new object[] { GameServices.Match.RoundNumber });
                Snapshot(1, 1, 1, 31);
                Assert.IsTrue(owner.IsAloft, "An old transport's terminal request ID poisoned its legitimate reused ID.");
                Assert.AreEqual(1, owner.FlightEpisode);
                Note("featherfall_unknown_swimmer_identity", true);
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= Heard;
                NetAuthority.Provider = previous;
                owner.AbilitySystem.ResetKit(); host.AbilitySystem.ResetKit();
                Object.Destroy(root);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallTimedKitHonoursGenerationRequestsAndKitIdentity()
        {
            yield return LocalAttacker();
            var actor = Amihan(GameLaunch.SoloSeat, new Vector3(0, .12f, -11.5f));
            yield return null;
            // Keep the receiver inactive: no singleton, handlers or real transport are replaced.
            var routerObject = new GameObject("FeatherfallSnapshotReceiver");
            routerObject.SetActive(false);
            var router = routerObject.AddComponent<MatchRpc>();
            var previous = NetAuthority.Provider;
            NetAuthority.Provider = new FlightClient { LocalSlot = actor.PlayerSlot };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            void Field(string name, object value) => typeof(MatchRpc).GetField(name, flags).SetValue(router, value);
            void Clock() => typeof(MatchRpc).GetMethod("AdoptFeatherfallRoundClock", flags).Invoke(router, new object[] { GameServices.Match.RoundNumber });
            void Snapshot(int generation, float remaining, byte phase = 1, int epoch = -1, long request = 0, long eventId = 20)
            {
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(12345L); writer.WriteValueSafe(generation);
                writer.WriteValueSafe(epoch < 0 ? actor.MovementEpoch : epoch); writer.WriteValueSafe(eventId);
                writer.WriteValueSafe(request); writer.WriteValueSafe(GameServices.Round.TimeLeft);
                writer.WriteValueSafe(2.92f); writer.WriteValueSafe(phase); writer.WriteValueSafe(100UL); writer.WriteValueSafe(-10L);
                Assert.AreEqual(57, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                typeof(MatchRpc).GetMethod("ReadFeatherfallSnapshot", flags).Invoke(router, new object[]
                    { reader, actor.PlayerSlot, GameServices.Match.RoundNumber, remaining, 0f, 0f, false });
            }
            void Play(long eventId, long request, long flightIntent, int round = -1)
            {
                var cast = new SkillCastMessage
                {
                    Seat = actor.PlayerSlot, Slot = 1, AbilityId = new FixedString64Bytes(actor.AbilitySystem.Kit.Skill2.Id),
                    Position = actor.transform.position, Forward = Vector3.forward, AimPoint = Vector3.forward * 3,
                    Match = 12345, Round = round < 0 ? GameServices.Match.RoundNumber : round,
                    Request = request, Event = eventId, FlightIntent = flightIntent, Reactivation = flightIntent != 0
                };
                using var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(cast);
                Assert.AreEqual(108 + cast.AbilityId.Length, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                typeof(MatchRpc).GetMethod("OnPlayAbilityMsg", flags).Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
            }
            void RequestRoundTrip(long flightIntent)
            {
                var cast = new SkillCastMessage
                {
                    Seat = actor.PlayerSlot, Slot = 1, AbilityId = new FixedString64Bytes(actor.AbilitySystem.Kit.Skill2.Id),
                    Position = actor.transform.position, Forward = Vector3.forward, AimPoint = Vector3.forward * 3,
                    Match = 12345, Round = GameServices.Match.RoundNumber, Request = 7,
                    FlightIntent = flightIntent, Reactivation = flightIntent != 0
                };
                using var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(cast);
                Assert.AreEqual(108 + cast.AbilityId.Length, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                var input = reader;
                Assert.IsTrue(SkillCastMessage.TryRead(ref input, out var decoded));
                Assert.IsTrue(decoded.IsValid(false));
                Assert.AreEqual(actor.PlayerSlot, decoded.Seat); Assert.AreEqual(1, decoded.Slot);
                Assert.AreEqual(actor.transform.position, decoded.Position); Assert.AreEqual(Vector3.forward, decoded.Forward);
                Assert.AreEqual(Vector3.forward * 3, decoded.AimPoint); Assert.AreEqual(0, decoded.HeldSeconds);
                Assert.IsFalse(decoded.HasFamiliar); Assert.AreEqual(Vector3.zero, decoded.FamiliarPosition);
                Assert.AreEqual(12345, decoded.Match); Assert.AreEqual(GameServices.Match.RoundNumber, decoded.Round);
                Assert.AreEqual(7, decoded.Request); Assert.AreEqual(flightIntent, decoded.FlightIntent);
                Assert.AreEqual(actor.AbilitySystem.Kit.Skill2.Id, decoded.AbilityId.ToString());
                Assert.AreEqual(flightIntent != 0, decoded.Reactivation);
            }
            try
            {
                RequestRoundTrip(0); RequestRoundTrip(7); RequestRoundTrip(-10);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 12345L);
                Field("_lastWorldFieldGeneration", 1); Field("_worldFieldRound", GameServices.Match.RoundNumber);
                Snapshot(1, 3);
                Assert.IsFalse(actor.IsFlying, "An unadopted local round clock qualified a flight snapshot.");
                Clock(); Snapshot(1, 3);
                var kit = (AmihanHeroKit)actor.AbilitySystem.Kit;
                Assert.AreEqual(3, kit.AttackingSkill.DurationRemaining);
                Snapshot(1, 5);
                Assert.AreEqual(3, kit.AttackingSkill.DurationRemaining, "The same generation restarted flight.");
                Field("_lastWorldFieldGeneration", 2);
                Snapshot(2, 5, epoch: actor.MovementEpoch + 1);
                Assert.AreEqual(3, kit.AttackingSkill.DurationRemaining);
                Snapshot(2, 2.5f);
                Assert.AreEqual(2.5f, kit.AttackingSkill.DurationRemaining, "A valid later resync on the same kit was refused.");
                Field("_skillRequestSequence", 7L); Field("_skillRequestScopeFloor", 5L);
                actor.EndFlight();
                Field("_lastWorldFieldGeneration", 3); Snapshot(3, 3, request: 6);
                Assert.IsFalse(actor.IsAloft, "An older host snapshot revived a newer predicted descent.");
                Snapshot(3, 0, phase: 2, request: 7);
                Assert.IsFalse(actor.IsAloft);
                actor.AbilitySystem.BindHero("amihan");
                Field("_lastWorldFieldGeneration", 4); Snapshot(4, 3, request: 7);
                Assert.IsFalse(actor.IsFlying, "A same-hero replacement accepted the previous kit's snapshot.");
                Clock(); Snapshot(4, 2, request: 7);
                Assert.IsTrue(actor.IsAloft);
                var events = (long[])typeof(MatchRpc).GetField("_lastSkillEvent", flags).GetValue(router);
                events[actor.PlayerSlot] = 21;
                Field("_lastWorldFieldGeneration", 5); Snapshot(5, 5, request: 7, eventId: 20);
                Assert.AreEqual(2, actor.AbilitySystem.Kit.AttackingSkill.DurationRemaining);
                typeof(MatchRpc).GetMethod("ResetFeatherfallSnapshots", flags).Invoke(router, null);
                Field("_lastWorldFieldGeneration", 1); Clock();
                Snapshot(1, 1, request: 7, eventId: 21);
                Assert.AreEqual(1, actor.AbilitySystem.Kit.AttackingSkill.DurationRemaining, "A fresh transport generation was poisoned by the previous session.");
                ((FlightClient)NetAuthority.Provider).LocalSlot = (actor.PlayerSlot + 1) % 4;
                Play(21, 8, 0);
                Assert.AreEqual(-10, actor.FlightEpisode, "A delayed same-frame takeoff replayed after its snapshot watermark.");
                var current = (AmihanHeroKit)actor.AbilitySystem.Kit;
                current.RestoreFeatherfall(actor, 0, 2.92f, 0, 100, -10);
                Play(22, 8, -10);
                Assert.IsFalse(actor.IsFlying, "An accepted recast became takeoff after the observer already landed.");
                Play(23, 9, 0);
                Assert.IsTrue(actor.IsAloft);
                Assert.AreEqual(9, actor.FlightEpisode);
                Play(24, 10, -10);
                Assert.AreEqual(9, actor.FlightEpisode, "An older flight's recast changed newer C.");
                Assert.IsTrue(actor.IsAloft);
                Play(25, 11, 0, GameServices.Match.RoundNumber - 1);
                Assert.AreEqual(9, actor.FlightEpisode, "A prior-round intent crossed the round boundary.");
                Play(25, 11, 9);
                Assert.IsFalse(actor.IsAloft);
                Assert.AreEqual(9, actor.FlightEpisode);
            }
            finally
            {
                NetAuthority.Provider = previous;
                Object.Destroy(routerObject);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallMovesThrowsAndSettlesAtGroundContact()
        {
            yield return LocalAttacker();
            var actor = Amihan(GameLaunch.SoloSeat, Flat(GameServices.Round.Lata.transform.position) + new Vector3(0, .12f, -11.5f));
            yield return null;
            var carrier = actor.GetComponent<Carrier>();
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            Assert.IsFalse(actor.IsInsideBox());
            Assert.AreEqual("amihan_skill2", actor.AbilitySystem.Kit.Skill2.Id);
            Assert.AreEqual("FEATHERFALL", actor.AbilitySystem.Kit.Skill2.Name);
            Assert.AreEqual("FEATHERFALL", actor.AbilitySystem.Kit.Skill2.EffectiveName);
            Assert.AreEqual(5, actor.AbilitySystem.Kit.Skill2.Duration);
            Assert.AreEqual(40, actor.AbilitySystem.Kit.Skill2.Cooldown);
            int settles = 0;
            bool soundedOnGround = true;
            void Heard(string id, Vector3 at, float pitch, float gain)
            {
                if (id != "sfx_amihan_updraft_settle") return;
                settles++;
                soundedOnGround &= actor.IsGrounded && !actor.IsAloft;
            }
            AudioDirector.WorldCuePlayed += Heard;
            try
            {
                float began = Time.time;
                yield return PressSkill(actor, Verb.Skill2);
                Assert.IsTrue(actor.IsAloft);
                yield return new WaitForSeconds(.8f);
                var support = actor.GetComponentInChildren<AmihanHoverRing>();
                Assert.IsNotNull(support);
                Assert.IsNull(support.transform.Find("FeetRing"));
                Assert.IsNull(support.transform.Find("FeetRingOuter"));
                Assert.IsNotNull(actor.GetComponent<AmihanFlightPose>());
                var marker = actor.GetComponentInChildren<CharacterNameplate>()?.transform.Find("NameplateRing");
                Assert.IsNotNull(marker);
                Assert.Less(marker.position.y, actor.transform.position.y - 1.5f, "The role marker followed her feet into the air.");
                var before = actor.transform.position;
                actor.Intent.Move = Vector2.right;
                yield return new WaitForSeconds(.35f);
                actor.Intent.Move = Vector2.zero;
                Assert.Greater(Vector3.Distance(Flat(before), Flat(actor.transform.position)), .3f);
                Assert.IsTrue(actor.IsAloft);
                actor.Intent.AimPoint = GameServices.Round.Lata.transform.position;
                Face(actor, actor.Intent.AimPoint);
                actor.Intent.Set(Verb.SpecialAbility, true);
                yield return new WaitForSeconds(.6f);
                Assert.IsTrue(actor.IsAloft, "The throw setup landed before release.");
                actor.Intent.Set(Verb.SpecialAbility, false);
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.IsNull(carrier.Held, "The actual airborne throw was not accepted.");
                shoe.ApplySnapshotState(SlipperState.Loose, null, actor.transform.position,
                    Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, actor.PlayerSlot);
                Assert.IsFalse(shoe.IsGrabbableIgnoringReach(actor), "Aloft flight allowed an otherwise owned loose slipper.");
                while (actor.IsAloft && Time.time - began < 5.5f) yield return null;
                Assert.IsFalse(actor.IsAloft);
                Assert.IsTrue(shoe.IsGrabbableIgnoringReach(actor), "Choosing to descend must restore retrieval eligibility.");
                Assert.That(Time.time - began, Is.InRange(4.9f, 5.25f));
                Assert.AreEqual(0, settles, "The landing cue played at descent start.");
                float until = Time.time + 2;
                while ((!actor.IsGrounded || actor.IsFlying) && Time.time < until) yield return null;
                Assert.IsTrue(actor.IsGrounded);
                Assert.IsFalse(actor.IsFlying);
                yield return new WaitForSeconds(.5f);
                // ⚠️ The landing cue was this test's other witness, and every hero skill sound is deleted (2026-09-29, the owner:
                // *"can we delete all skill abilities sfx"*, `AudioCues.IsSkillSfx`). The landing is proved by the body above; the
                // cue must stay silent until the sounds are reworked.
                Assert.AreEqual(Audio.AudioCues.SkillSfxOn ? 1 : 0, settles);
                Assert.IsTrue(soundedOnGround);
                Assert.IsNull(actor.GetComponent<AmihanFlightPose>());
                Assert.IsNull(actor.GetComponentInChildren<AmihanHoverRing>());
                Note("featherfall_landed", true);
            }
            finally { AudioDirector.WorldCuePlayed -= Heard; }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FeatherfallEarlyDescentResetAndReplacementCleanTheFlight()
        {
            yield return LocalAttacker();
            var start = Flat(GameServices.Round.Lata.transform.position) + new Vector3(0, .12f, -11.5f);
            var actor = Amihan(GameLaunch.SoloSeat, start);
            yield return null;
            yield return PressSkill(actor, Verb.Skill2);
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(actor.IsAloft);
            yield return PressSkill(actor, Verb.Skill2);
            Assert.IsFalse(actor.IsAloft, "Recast must begin the early descent despite the cooldown.");
            Assert.IsTrue(actor.IsFlying);
            actor.AbilitySystem.ResetKit();
            Assert.IsFalse(actor.IsFlying, "A reset during the glide left flight attached.");
            yield return new WaitForSeconds(.4f);
            Assert.IsNull(actor.GetComponent<AmihanFlightPose>());
            Assert.IsNull(actor.GetComponentInChildren<AmihanHoverRing>());

            actor.Teleport(start);
            yield return PressSkill(actor, Verb.Skill2);
            yield return new WaitForSeconds(.7f);
            yield return PressSkill(actor, Verb.Grab);
            Assert.IsFalse(actor.IsAloft, "Grab must request descent through the role ability.");
            actor.AbilitySystem.ResetKit();
            actor.Teleport(start);
            yield return new WaitForSeconds(.4f);
            yield return PressSkill(actor, Verb.Skill2);
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(actor.IsAloft);
            actor.AbilitySystem.BindHero("sean");
            Assert.IsFalse(actor.IsFlying, "Replacing the kit retained the previous hero's flight.");
            yield return new WaitForSeconds(.4f);
            Assert.IsNull(actor.GetComponent<AmihanFlightPose>());
            Assert.IsNull(actor.GetComponentInChildren<AmihanHoverRing>());

            actor = Amihan(GameLaunch.SoloSeat, start);
            yield return null;
            yield return PressSkill(actor, Verb.Skill2);
            yield return new WaitForSeconds(.7f);
            actor.ApplyStagger(.5f, StunElement.Ice, 3);
            yield return null;
            Assert.IsFalse(actor.IsAloft, "A stun must end the held flight.");
            actor.AbilitySystem.ResetKit();
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(actor.IsFlying);
            Assert.IsNull(actor.GetComponent<AmihanFlightPose>());
            Assert.IsNull(actor.GetComponentInChildren<AmihanHoverRing>());
            Note("featherfall_cancel_cleanup", true);
        }

        /// <summary>
        /// ⚠️ HER THREE SKILLS IN ONE MATCH (owner, 2026-09-26). DRIFT twice through a bystander (the two charges), FEATHERFALL
        /// with a drift through the air and a throw from it at the can, then the taya's WHIRLWIND rolling through a player
        /// carrying a slipper. Filmed on her screen (`owner/`), from the court (`wide/`) and beside whoever the wind hits
        /// (`victim/`), each at 30 fps game time, every world cue logged.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHerSkillsInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_AMIHAN_FILM") != "1") Assert.Ignore("Film only: set TUMP_AMIHAN_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            int flySeat = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            Vector3 start = can + new Vector3(0f, .12f, -11.5f);
            var flyer = Amihan(flySeat, start);
            var defender = Amihan(taya, can + new Vector3(-3.5f, .12f, -2.5f));
            var local = round.PlayerAt(GameLaunch.SoloSeat);
            // A bystander in the dash line, and a slipper carrier in the gale's path.
            CharacterMotor bystander = null, holder = null;
            foreach (var p in round.Players)
            {
                if (p == flyer || p == defender) continue;
                if (holder == null && p.GetComponent<Carrier>().Held != null) holder = p;
                else if (bystander == null) bystander = p;
            }
            if (bystander == null) foreach (var p in round.Players) if (p != flyer && p != defender && p != holder) bystander = p;
            bystander?.Teleport(start + new Vector3(0.35f, 0f, 3.2f));
            if (bystander != null) { bystander.Intent.Parked = true; Face(bystander, start); }
            holder?.Teleport(can + new Vector3(0.5f, .12f, -2.8f));
            if (holder != null) { holder.Intent.Parked = true; Face(holder, can + new Vector3(-3.5f, 0, -2.5f)); }
            Face(flyer, can); Face(defender, holder != null ? holder.transform.position : can);

            var wide = Film.Make("AmihanSkillsWide", 50);
            var shoulder = Film.Make("AmihanSkillsShoulder", 60);
            var victimCam = Film.Make("AmihanSkillsVictim", 60);
            bool drifted = false, flew = false, threw = false, whirled = false;
            float peak = 0f;
            var carried = flyer.GetComponent<Carrier>().Held;
            using (var film = new Film("amihan-skills-film", "owner", "wide", "victim"))
            {
                try
                {
                    const int frames = (int)(30 * 13.0f);
                    for (int f = 0; f < frames; f++)
                    {
                        film.Frame = f;
                        float t = f / 30f;
                        // DRIFT, twice (0.5 and 1.4): along her facing, through the bystander.
                        flyer.Intent.Set(Verb.Skill1, (t > .5f && t < .6f) || (t > 1.4f && t < 1.5f));
                        // FEATHERFALL at 3.0; then drift forward and a little right through the air, throw at the can at 5.6.
                        flyer.Intent.Set(Verb.Skill2, t > 3.0f && t < 3.12f);
                        flyer.Intent.Move = t > 3.8f && t < 6.4f ? new Vector2(0.35f, 0.75f) : Vector2.zero;
                        if (t > 5.2f) { flyer.Intent.AimPoint = round.Lata.transform.position; Face(flyer, can); }
                        flyer.Intent.Set(Verb.SpecialAbility, t > 5.3f && t < 5.9f);
                        // WHIRLWIND from the taya at 9.4, at the carrier.
                        defender.Intent.Set(Verb.Skill2, t > 9.4f && t < 9.52f);
                        yield return null;
                        drifted |= Vector3.Distance(Flat(flyer.transform.position), Flat(start)) > 3f && t < 3f;
                        flew |= flyer.IsFlying;
                        if (flyer.IsFlying) peak = Mathf.Max(peak, flyer.transform.position.y);
                        threw |= carried != null && flyer.GetComponent<Carrier>().Held == null && t > 5.2f && t < 8f;
                        whirled |= holder != null && holder.IsWhirled;

                        CharacterMotor acting = t < 9.0f ? flyer : defender;
                        CharacterMotor hit = t < 9.0f ? bystander : holder;
                        // The court: each skill framed from where its path reads.
                        if (t < 2.6f) { wide.transform.position = start + new Vector3(6.0f, 2.4f, 2.5f); wide.transform.LookAt(start + new Vector3(0f, 1f, 3.2f)); }
                        else if (t < 9.0f) { wide.transform.position = flyer.transform.position + new Vector3(7.5f, 1.2f, -2.5f); wide.transform.LookAt(flyer.transform.position + new Vector3(0f, 0.2f, 1.5f)); }
                        else { wide.transform.position = can + new Vector3(4.5f, 3.2f, -9.5f); wide.transform.LookAt(can + new Vector3(-1.5f, .8f, -2.6f)); }
                        if (acting == local && Camera.main != null) film.Shoot(Camera.main, "owner");
                        else
                        {
                            var fwd = acting.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                            shoulder.transform.position = acting.transform.position - fwd * 3.2f + Vector3.up * 2.1f + Vector3.Cross(Vector3.up, fwd) * .9f;
                            shoulder.transform.LookAt(acting.transform.position + fwd * 4f + Vector3.up * 1f);
                            film.Shoot(shoulder, "owner");
                        }
                        film.Shoot(wide, "wide");
                        if (hit != null)
                        {
                            var toHer = Flat(acting.transform.position - hit.transform.position);
                            toHer = toHer.sqrMagnitude > .01f ? toHer.normalized : Vector3.back;
                            var side = Vector3.Cross(Vector3.up, toHer);
                            victimCam.transform.position = hit.transform.position + side * 3.0f - toHer * 1.0f + Vector3.up * 1.5f;
                            victimCam.transform.LookAt(hit.transform.position + toHer * 1.2f + Vector3.up * 1.0f);
                            film.Shoot(victimCam, "victim");
                        }
                    }
                }
                finally
                {
                    Object.Destroy(wide.gameObject); Object.Destroy(shoulder.gameObject); Object.Destroy(victimCam.gameObject);
                }
            }
            Note("skills_film_drifted", drifted); Note("skills_film_flew", flew); Note("skills_film_peak_m", peak);
            Note("skills_film_threw_from_air", threw); Note("skills_film_whirled", whirled);
            Assert.IsTrue(drifted, "DRIFT did not carry her.");
            Assert.IsTrue(flew, "FEATHERFALL never took her up.");
            Assert.IsTrue(whirled || holder == null, "WHIRLWIND never Whirled the carrier.");
        }
    }
}
