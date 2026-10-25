using System.Collections;
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
    /// <summary>
    /// Zack's speedster kit (2026-10-25 rework, `docs/reports/zack-kit-2026-10-25/plan.md`) on a real court: BACKTRACK keeps
    /// the slipper and the host checks the spot, AFTERIMAGE pops on the taya's touch, LIVE WIRE Zaps the first crosser (every
    /// crosser when overcharged), ZAPPED is a 1 s full stun that Dante's immunity refuses, and OVERCLOCKED lasts 25 s.
    /// </summary>
    public sealed class ZackKitAcceptanceProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;
        private CharacterMotor _caster;
        private AbilityChallengeProgress[] _challenges;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            _challenges = Settings.SettingsStore.Current.AbilityChallenges.ToArray();
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true; player.IsDefender = false;
                player.Teleport(new Vector3(10 + player.PlayerSlot * 2, .12f, -10));
            }
            _caster = GameServices.Round.PlayerAt(1); _caster.IsBot = true;
            _caster.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack");
            var art = RosterBook.Load().FindPersonArt("zack");
            _caster.GetComponent<Visual.CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            _caster.AbilitySystem.BindHero("zack");
            _caster.Teleport(new Vector3(0, .12f, -8)); _caster.transform.rotation = Quaternion.identity;
            _caster.Intent.Parked = false;
            yield return new WaitForSeconds(.2f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            ZackLiveWire.RetireOwnedBy(-1); ZackAfterimage.RetireOwnedBy(-1);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            var progress = Settings.SettingsStore.Current.AbilityChallenges;
            progress.Clear(); progress.AddRange(_challenges);
        }

        private ZackHeroKit Kit => (ZackHeroKit)_caster.AbilitySystem.Kit;
        private AbilityContext Context() => new AbilityContext(_caster, _caster.GetComponent<Carrier>(), _caster.GetComponent<CombatVerbs>());

        private static IEnumerator Steps(int count) { for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate(); }

        // ------------------------------------------------------------------ BACKTRACK

        [UnityTest, Timeout(60000)]
        public IEnumerator BacktrackSnapsBackWithTheSlipperAndTheHostChecksTheSpot()
        {
            var carrier = _caster.GetComponent<Carrier>();
            var held = carrier.Held;
            if (held == null)
            {
                held = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(s => s.SeatOfOrigin == _caster.PlayerSlot);
                carrier.HostPickUp(held);
            }
            Assert.IsNotNull(carrier.Held, "Backtrack's promise is the slipper in hand: the probe needs one.");
            // He runs 4.5 m along +x over 3.5 s. The body is moved straight, so only the kit's own ring records it.
            _caster.enabled = false;
            var start = _caster.transform.position;
            float began = Time.time;
            while (Time.time - began < 3.5f)
            {
                _caster.transform.position = start + Vector3.right * Mathf.Min(4.5f, (Time.time - began) * 1.3f);
                yield return null;
            }
            _caster.enabled = true;
            Assert.IsTrue(Kit.TryBacktrackPoint(_caster, out var spot), "Three seconds of path were not recorded.");
            Assert.Less(Vector3.Distance(new Vector3(spot.x, 0, spot.z), new Vector3(start.x + .65f, 0, start.z)), .5f,
                "The spot is not where he was 3 s ago.");
            Assert.IsTrue(Kit.AcceptsBacktrackPoint(_caster, spot));
            Assert.IsFalse(Kit.AcceptsBacktrackPoint(_caster, spot + Vector3.back * 6f), "The host took a spot it never saw him at.");
            Assert.IsFalse(Kit.AcceptsBacktrackPoint(_caster, spot + Vector3.up * 2f), "A matching flat point cannot float above the floor.");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = spot + new Vector3(0, .9f, .75f);
            wall.transform.localScale = new Vector3(12, 2, .1f);
            Physics.SyncTransforms();
            try
            {
                Assert.IsFalse(Kit.AcceptsBacktrackPoint(_caster, spot + Vector3.forward * 1.5f),
                    "The host accepted a destination through a wall from its recorded path.");
            }
            finally { Object.DestroyImmediate(wall); Physics.SyncTransforms(); }
            var ctx = Kit.Skill1.CaptureLocalCastContext(Context());
            var departure = _caster.transform.position;
            Assert.IsTrue(Kit.Skill1.CanActivate(ctx));
            Kit.Skill1.Activate(ctx);
            var pull = Object.FindAnyObjectByType<Visual.ZackBacktrackLook>();
            Assert.IsNotNull(pull);Assert.IsTrue(pull.Pulling);
            Assert.Less(Vector3.Distance(departure,pull.VisualFeet),.2f,"The visible body must start at the departure, not already at the landing.");
            Assert.Greater(Visual.ZackBacktrackLook.EyeOffset(_caster).magnitude,1f,"His view must be drawn back with the pull.");
            yield return null;
            var flat = _caster.transform.position - spot; flat.y = 0;
            Assert.Less(flat.magnitude, .25f, "He did not land on the spot.");
            Assert.AreSame(held, carrier.Held, "Backtrack must keep whatever he is holding.");
            Assert.AreEqual(ZackRules.BacktrackCooldown, Kit.Skill1.CooldownRemaining, .2f);
            Assert.Zero(Kit.MomentumSeconds, "The snap stops him: Momentum starts over.");
            Assert.IsNotNull(Object.FindFirstObjectByType<Visual.ZackBacktrackLook>(), "Everyone sees the rewind and the ring.");
            yield return new WaitForSeconds(.16f);
            Assert.Less(Vector3.Distance(pull.VisualFeet,spot),Vector3.Distance(departure,spot),"The body must travel toward its past along the recorded path.");
            Assert.IsEmpty(pull.GetComponentsInChildren<CharacterMotor>());
            Assert.IsEmpty(pull.GetComponentsInChildren<Collider>());
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(pull.Pulling);Assert.AreEqual(Vector3.zero,Visual.ZackBacktrackLook.EyeOffset(_caster));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator BacktrackDrawnBodyUsesAndRestoresCameraScopedReadability()
        {
            var history = Object.FindAnyObjectByType<CameraSystem.MatchPoseHistory>();
            float readyBy = Time.realtimeSinceStartup + 2;
            while (history?.ForSeat(_caster.PlayerSlot)?.Ready != true && Time.realtimeSinceStartup < readyBy) yield return null;
            Assert.IsTrue(history?.ForSeat(_caster.PlayerSlot)?.Ready == true);
            var from = _caster.transform.position; var to = from + Vector3.left * 4;
            var pull = Visual.ZackBacktrackLook.Play(_caster, from, to, new[] { to, from });
            _caster.Teleport(to, keepStatuses: true); pull.SealTeleport();
            yield return new WaitForSeconds(.16f);
            var surface = pull.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            var properties = new MaterialPropertyBlock(); surface.GetPropertyBlock(properties);
            properties.SetFloat("_TayaCue", .37f); properties.SetVector("_WorldBody", new Vector4(0, 0, 0, 7));
            surface.SetPropertyBlock(properties);
            var camera = new GameObject("Backtrack readability observer").AddComponent<Camera>(); camera.enabled = false;
            var visual = _caster.GetComponent<Visual.CharacterVisual>();
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            try
            {
                typeof(Visual.CharacterVisual).GetMethod("BeginReadability", hidden).Invoke(visual, new object[] { camera });
                surface.GetPropertyBlock(properties);
                Assert.AreEqual(pull.RenderedFeet.y, properties.GetVector("_WorldBody").x, .001f,
                    "The moving copy must receive the actor's scoped cue at its drawn height.");
                Assert.Zero(properties.GetVector("_WorldBody").w);
                Assert.AreNotEqual(.37f, properties.GetFloat("_TayaCue"));
            }
            finally
            {
                typeof(Visual.CharacterVisual).GetMethod("EndReadability", hidden).Invoke(visual, new object[] { camera });
                Object.Destroy(camera.gameObject);
            }
            surface.GetPropertyBlock(properties);
            Assert.AreEqual(.37f, properties.GetFloat("_TayaCue"), .001f, "The camera override must restore the copy's own block.");
            Assert.AreEqual(7, properties.GetVector("_WorldBody").w);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RewindMomentsMoveBackwardStayBoundedAndRetire()
        {
            var history = Object.FindAnyObjectByType<CameraSystem.MatchPoseHistory>();
            float readyBy = Time.realtimeSinceStartup + 2;
            while (history?.ForSeat(_caster.PlayerSlot)?.Ready != true && Time.realtimeSinceStartup < readyBy) yield return null;
            Assert.IsTrue(history?.ForSeat(_caster.PlayerSlot)?.Ready == true);
            var rig = Camera.main.GetComponent<CameraSystem.CameraRig>();
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            try
            {
                foreach (bool calm in new[] { false, true })
                {
                    rig.Follow(_caster); rig.SetDebugThirdPerson(true);
                    Settings.SettingsStore.Current.ReducedEffects = calm;
                    var from = _caster.transform.position; var to = from + Vector3.left * 4;
                    var rewind = Visual.ZackBacktrackLook.Play(_caster, from, to, new[] { to, from });
                    _caster.Teleport(to, keepStatuses: true); rewind.SealTeleport();
                    yield return new WaitForSeconds(.15f);
                    var streaks = rewind.GetComponentsInChildren<LineRenderer>(true);
                    Assert.AreEqual(2, rewind.transform.Cast<Transform>().Count(t => t.GetComponent<LineRenderer>() == null),
                        "Only the travelling body and one hologram may be copied.");
                    Assert.AreEqual(4, rewind.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                    Assert.AreEqual(1, rewind.MomentsDrawn);
                    Assert.IsFalse(rewind.TryMoment(1, out _, out _), "There is only one historical subject.");
                    Assert.IsTrue(Visual.TagExposure.OnFloor(_caster, .8f, out var cueFeet, out _, out _));
                    var markerError = cueFeet - rewind.VisualFeet; markerError.y = 0;
                    Assert.Less(markerError.magnitude, .08f, "The taggability floor brackets must follow the drawn body, not the instant teleport.");
                    if (calm) Assert.IsEmpty(streaks, "Reduced effects omit speed dashes.");
                    else Assert.That(streaks.Length, Is.InRange(1, 20), "Speed dashes must remain bounded.");
                    foreach (var streak in streaks)
                    {
                        Assert.IsTrue(streak.name.StartsWith("RewindStreak"));
                        Assert.AreEqual(2, streak.positionCount, "Dashes have no tether bends or rings.");
                        Assert.LessOrEqual(streak.widthMultiplier, .06f);
                        if (streak.enabled) Assert.LessOrEqual(Vector3.Distance(streak.GetPosition(0), streak.GetPosition(1)), 2.3f,
                            "A speed dash must never become a route-length cable.");
                    }
                    var memoryShader=Resources.Load<Shader>("Shaders/ZackTemporalMemory");
                    Assert.IsNotNull(memoryShader); Assert.IsTrue(memoryShader.isSupported);
                    Assert.Greater(rewind.GetComponentsInChildren<Renderer>(true).Count(v=>v.sharedMaterials.Any(m=>m.shader==memoryShader)),0,
                        "Past selves must use textured transparency instead of opaque bronze coats.");
                    Assert.IsEmpty(rewind.GetComponentsInChildren<Collider>(true));
                    Assert.IsEmpty(rewind.GetComponentsInChildren<CharacterMotor>(true));
                    int count = 1;
                    var before = new Vector3[count];
                    for (int i = 0; i < count; i++) Assert.IsTrue(rewind.TryMoment(i, out before[i], out _));
                    var initialLead = before[0] - rewind.RenderedFeet; initialLead.y = 0;
                    Assert.Greater(Vector3.Dot(initialLead, (to - from).normalized), .05f, "The hologram must lead toward the old spot.");
                    Debug.Log($"[Rewind speed cost] reduced={calm} bodies=2 streaks={streaks.Length} renderers={rewind.GetComponentsInChildren<Renderer>(true).Length} skinned={rewind.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
                    yield return new WaitForSeconds(.12f);
                    for (int i = 0; i < count; i++)
                    {
                        Assert.IsTrue(rewind.TryMoment(i, out var now, out _));
                        Assert.Less(Vector3.Distance(now, to), Vector3.Distance(before[i], to), "Every past moment must move toward the old location.");
                        var closingLead = now - rewind.RenderedFeet; closingLead.y = 0;
                        Assert.Less(closingLead.magnitude, initialLead.magnitude, "Zack must close the gap to his hologram.");
                    }
                    rig.SetDebugThirdPerson(false);
                    float baseFov = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                    rig.Camera.fieldOfView = baseFov;
                    yield return null;
                    if (calm) Assert.AreEqual(baseFov, rig.Camera.fieldOfView, .1f, "Reduced effects omit the rewind zoom.");
                    else Assert.Greater(rig.Camera.fieldOfView, baseFov, "Only the caster's first-person lens receives the travel kick.");
                    Assert.AreEqual(calm, Visual.TagExposure.OnFloor(_caster, .8f, out _, out _, out _),
                        "The caster's own floor brackets step aside only while the rewind zoom is active.");
                    Assert.AreEqual(0, Visual.ZackBacktrackLook.FovKick(GameServices.Round.PlayerAt(0)));
                    rig.SetDebugThirdPerson(true);
                    while (rewind != null && rewind.Age < .52f) yield return null;
                    Assert.IsNotNull(rewind);
                    Assert.IsTrue(Visual.TagExposure.OnFloor(_caster, .8f, out cueFeet, out _, out _));
                    markerError = cueFeet - rewind.VisualFeet; markerError.y = 0;
                    Assert.Less(markerError.magnitude, .08f, "The marker follows the projected catch overshoot too.");
                    yield return new WaitForSeconds(.35f);
                    Assert.IsTrue(rewind == null, "All temporal copies must retire after the catch.");
                    Assert.IsTrue(streaks.All(s => s == null), "All speed dashes must retire with their rewind.");
                    Assert.AreEqual(0, Visual.ZackBacktrackLook.FovKick(_caster), "The lens kick cannot outlive the effect.");
                }
            }
            finally { Settings.SettingsStore.Current.ReducedEffects = reduced; }
        }

        // ------------------------------------------------------------------ AFTERIMAGE

        [UnityTest, Timeout(60000)]
        public IEnumerator AfterimageRunsAheadAndPopsWhenTheTayaTouchesIt()
        {
            var taya = GameServices.Round.PlayerAt(0); taya.IsDefender = true;
            var carrier = _caster.GetComponent<Carrier>();
            if (carrier.Held == null)
            {
                var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(s => s.SeatOfOrigin == _caster.PlayerSlot);
                shoe.transform.position = _caster.transform.position;
                carrier.HostPickUp(shoe);
            }
            Assert.IsNotNull(carrier.Held, "This case must exercise a copy carrying a slipper.");
            // Picking up can change the model hierarchy. Exercise the real body copy after its history is ready.
            var poses = Object.FindAnyObjectByType<CameraSystem.MatchPoseHistory>();
            float readyBy = Time.realtimeSinceStartup + 2;
            while (poses?.ForSeat(_caster.PlayerSlot)?.Ready != true && Time.realtimeSinceStartup < readyBy) yield return null;
            Assert.IsTrue(poses?.ForSeat(_caster.PlayerSlot)?.Ready == true, "The held-body fixture must have pose history.");
            Assert.IsTrue(Kit.AttackingSkill.CanActivate(Context()));
            Kit.AttackingSkill.Activate(Context());
            var copy = ZackAfterimage.Active.Single(c => c.OwnerSlot == _caster.PlayerSlot);
            Assert.IsTrue(copy.IsDecoy);
            Assert.IsFalse(GameServices.Round.Players.Any(p => p != null && p.gameObject == copy.gameObject), "A copy is never a body.");
            Assert.IsEmpty(copy.GetComponentsInChildren<Collider>().Where(c => c.enabled), "A copy has no collider.");
            var history = Object.FindAnyObjectByType<CameraSystem.MatchPoseHistory>();
            var track = history != null ? history.ForSeat(_caster.PlayerSlot) : null;
            Debug.Log("[Afterimage shoe] holding=" + carrier.Held + " flag=" + copy.Capture().SecondScale
                + " history=" + (track != null && track.Ready) + " skinned=" + copy.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length
                + " anchor=" + _caster.GetComponent<Visual.CharacterVisual>().HandAnchor);
            Assert.IsNotNull(copy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "AfterimageHeldSlipper"),
                "The copy must draw the slipper its caster holds.");
            yield return new WaitForSeconds(.6f);
            Assert.Greater(Vector3.Distance(copy.Feet, _caster.transform.position), 2f, "The copy did not run.");
            taya.Teleport(copy.Feet);
            yield return Steps(3);
            Assert.IsTrue(copy == null || copy.Popped, "The taya's touch must pop the copy.");
            yield return new WaitForSeconds(ZackAfterimage.PopSeconds + .2f);
            Assert.IsFalse(ZackAfterimage.Active.Any(c => c != null && c.OwnerSlot == _caster.PlayerSlot));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator OverclockedAfterimageSendsTwoCopiesAndTheTayaCannotUseIt()
        {
            Kit.BeginOverclock(_caster);
            Kit.AttackingSkill.Activate(Context());
            Assert.AreEqual(2, ZackAfterimage.Active.Count(c => c.OwnerSlot == _caster.PlayerSlot));
            ZackAfterimage.RetireOwnedBy(-1);
            _caster.IsDefender = true;
            Assert.IsFalse(Kit.AttackingSkill.CanActivate(Context()), "Afterimage is an attacker's skill.");
            yield return null;
        }

        // ------------------------------------------------------------------ LIVE WIRE and ZAPPED

        private static IEnumerator Cross(CharacterMotor body, Vector3 from, Vector3 to)
        {
            body.Teleport(from); yield return Steps(2);
            body.Teleport(to); yield return Steps(2);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LiveWireZapsTheFirstCrosserAndSnaps()
        {
            _caster.IsDefender = true;
            Vector3 a = _caster.transform.position, b = a + Vector3.right * 4f;
            Assert.IsTrue(ZackLiveWire.CanPlace(_caster, a, b));
            Assert.IsFalse(ZackLiveWire.CanPlace(_caster, a, a + Vector3.right * 7f), "Longer than 6 m.");
            var wire = ZackLiveWire.Cast(_caster, a, b, false);
            Assert.IsNotNull(wire); Assert.IsFalse(wire.Overcharged);
            var first = GameServices.Round.PlayerAt(2); var second = GameServices.Round.PlayerAt(3);
            var mid = (a + b) * .5f;
            yield return Cross(first, mid + Vector3.forward, mid + Vector3.back);
            Assert.IsTrue(first.IsZapped, "The first attacker across is Zapped.");
            Assert.IsFalse(first.CanAct(), "Zapped: cannot move or act.");
            Assert.AreEqual(StatusRules.ZappedSeconds, first.ZappedLeft, .1f);
            Assert.IsTrue(wire == null || wire.Snapped, "A normal wire snaps on its first Zap.");
            yield return Cross(second, mid + Vector3.right * .5f + Vector3.forward, mid + Vector3.right * .5f + Vector3.back);
            Assert.IsFalse(second.IsZapped, "A snapped wire Zaps nobody else.");
            yield return new WaitForSeconds(StatusRules.ZappedSeconds + .15f);
            Assert.IsFalse(first.IsZapped, "Zapped lasts 1 s.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator OverchargedWireZapsEveryCrosserOnceAndHolds()
        {
            _caster.IsDefender = true;
            Vector3 a = _caster.transform.position, b = a + Vector3.right * 5f;
            var wire = ZackLiveWire.Cast(_caster, a, b, true);
            Assert.IsTrue(wire.Overcharged);
            var first = GameServices.Round.PlayerAt(2); var second = GameServices.Round.PlayerAt(3);
            var mid = (a + b) * .5f;
            yield return Cross(first, mid + Vector3.forward, mid + Vector3.back);
            yield return Cross(second, mid + Vector3.left + Vector3.forward, mid + Vector3.left + Vector3.back);
            Assert.IsTrue(first.IsZapped); Assert.IsTrue(second.IsZapped);
            Assert.IsTrue(wire != null && wire.IsLive, "An overcharged wire lasts its whole 10 s.");
            yield return new WaitForSeconds(StatusRules.ZappedSeconds + .2f);
            yield return Cross(first, mid + Vector3.back, mid + Vector3.forward);
            Assert.IsFalse(first.IsZapped, "Each attacker is Zapped once per wire.");
            var taya = GameServices.Round.PlayerAt(0); taya.IsDefender = true;
            yield return Cross(taya, mid + Vector3.right + Vector3.forward, mid + Vector3.right + Vector3.back);
            Assert.IsFalse(taya.IsZapped, "The taya's side never trips its own wire.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator WireUsesTheFeetHeightAtTheCrossingRatherThanTheLastEndpoint()
        {
            _caster.IsDefender = true;
            var a = _caster.transform.position; var b = a + Vector3.right * 4;
            var wire = ZackLiveWire.Cast(_caster, a, b, false);
            var runner = GameServices.Round.PlayerAt(2); runner.enabled = false;
            var mid = (a + b) * .5f; mid.y = wire.PostA.y;
            yield return Cross(runner, mid + Vector3.forward + Vector3.up * .8f,
                mid + Vector3.back + Vector3.up * .2f);
            Assert.IsFalse(runner.IsZapped, "He crossed at 0.5 m, above the low wire's 0.4 m contact reach.");
            Assert.IsTrue(wire.IsLive);
            yield return Cross(runner, mid + Vector3.back + Vector3.up * .32f,
                mid + Vector3.forward + Vector3.up * .24f);
            Assert.IsTrue(runner.IsZapped, "The lower crossing must still trip the wire.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ZappedIsAFullStunThatDantesImmunityRefuses()
        {
            var victim = GameServices.Round.PlayerAt(2);
            victim.ApplyZapped();
            Assert.IsTrue(victim.IsZapped); Assert.IsTrue(victim.IsStunned);
            Assert.AreEqual(StunElement.Shock, victim.StunElement);
            Assert.IsFalse(victim.CanAct());
            var dante = GameServices.Round.PlayerAt(3);
            dante.AbilitySystem.BindHero("dante");
            var ctx = new AbilityContext(dante, dante.GetComponent<Carrier>(), dante.GetComponent<CombatVerbs>());
            Assert.IsTrue(dante.AbilitySystem.Kit.TryActivateSkill1(ctx)); Assert.IsTrue(dante.AbilitySystem.IsImmuneToStuns);
            bool refused = false;
            dante.StatusRefused += (_, kind) => refused |= kind == StatusKind.Zapped;
            dante.ApplyZapped();
            Assert.IsFalse(dante.IsZapped); Assert.IsTrue(refused, "The refusal is raised so Dante's plate flashes.");
            yield return new WaitForSeconds(StatusRules.ZappedSeconds + .15f);
            Assert.IsFalse(victim.IsZapped);
        }

        // ------------------------------------------------------------------ OVERCLOCK

        [UnityTest, Timeout(60000)]
        public IEnumerator OverclockedLastsItsClockAndRecoversOnRejoin()
        {
            Kit.Skill1.ApplyNetworkSnapshot(20, 1);
            Kit.BeginOverclock(_caster);
            Assert.IsTrue(Kit.IsOverclocked); Assert.AreEqual(ZackRules.OverclockSeconds, Kit.OverclockLeft, .05f);
            Assert.LessOrEqual(Kit.Skill1.CooldownRemaining, ZackRules.BacktrackOverclockedCooldown + .01f,
                "Overclock's start clamps a running Backtrack to 3 s.");
            Assert.AreEqual(1.4f, Kit.MovementSpeedScale, .001f);
            Assert.IsTrue(Kit.UltimateAftermath(out float left, out float total)); Assert.AreEqual(ZackRules.OverclockSeconds, total);
            yield return new WaitForSeconds(.5f);
            Assert.Less(Kit.OverclockLeft, ZackRules.OverclockSeconds - .3f, "The clock runs.");
            var snapshot = Kit.CaptureTimedKit();
            Kit.ResetForRound(Context());
            Assert.IsFalse(Kit.IsOverclocked, "Round end ends it.");
            Assert.IsTrue(Kit.RestoreTimedKit(_caster, snapshot));
            Assert.AreEqual(snapshot.PassiveRemaining, Kit.OverclockLeft, .01f, "A joiner sees the same time left.");
            Assert.IsFalse(Kit.RestoreTimedKit(_caster, snapshot), "The same state twice is no change.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator BotCastsOverclockEarlyInTheRound()
        {
            var kit = Kit;
            Unlock("zack_ultimate"); kit.AddUltimateCharge(kit.UltimateCost);
            kit.Skill1.ApplyNetworkSnapshot(100, 1); kit.Skill2.ApplyNetworkSnapshot(100, 1);
            var brain = _caster.GetComponent<AIController>();
            if (brain == null) brain = _caster.gameObject.AddComponent<AIController>();
            Assert.IsTrue(_caster.CanAct()); Assert.IsTrue(kit.IsUltimateReady);
            Assert.GreaterOrEqual(GameServices.Round.TimeLeft, 15f, "The probe needs a round with time in it.");
            _caster.enabled = false; brain.enabled = true;
            float began = Time.time; bool requested = false;
            try
            {
                while (!kit.IsOverclocked && Time.time - began < 13)
                {
                    yield return null;
                    requested |= _caster.Intent.Pressed(Verb.Ultimate);
                    _caster.Intent.CommitFrame();
                }
                Assert.IsTrue(requested, "The bot sat on a ready Overclock with the round still young.");
                Assert.IsTrue(kit.IsOverclocked);
                kit.AddUltimateCharge(100); yield return new WaitForSeconds(.3f);
                Assert.Zero(kit.UltimateCharge, "The meter does not charge while Overclocked.");
            }
            finally { brain.enabled = false; _caster.enabled = true; }
        }

        private static void Unlock(string id)
        {
            var progress = Settings.SettingsStore.Current.AbilityChallenges;
            progress.RemoveAll(row => row.VariantId == id);
            progress.Add(new AbilityChallengeProgress { VariantId = id, Count = 999 });
        }
    }
}
