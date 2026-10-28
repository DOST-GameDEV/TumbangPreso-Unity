using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // One fixed court camera per skill. Casts go through real input in a solo-authoritative round.
    // The ultimate retains its authored introduction before returning to the fixed court shot.
    public sealed class ZackSkillFilmProbe
    {
        private INetProvider _net;
        private string _settings;
        private CustomRules _rules;
        private bool _pinned, _bots, _spectator;
        private int _seat, _rate, _frame;
        private CharacterMotor _zack, _taya, _a, _b;
        private Camera _wide;
        private Camera _ui;
        private LateFrameShot _late;
        private RenderTexture _hdr, _ldr;
        private Texture2D _pixels;
        private string _output;
        private readonly StringBuilder _observations = new StringBuilder();
        private ZackHeroKit Kit => (ZackHeroKit)_zack.AbilitySystem.Kit;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider; _rate = Time.captureFramerate;
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            SharedUltimatePhase.FilmClock = null; Time.captureFramerate = _rate;
            if (_wide != null) Object.Destroy(_wide.gameObject);
            if (_ui != null) Object.Destroy(_ui.gameObject);
            if (_late != null) Object.Destroy(_late.gameObject);
            if (_pixels != null) Object.Destroy(_pixels);
            if (_hdr != null) { _hdr.Release(); Object.Destroy(_hdr); }
            if (_ldr != null) { _ldr.Release(); Object.Destroy(_ldr); }
            if (_output != null) File.WriteAllText(Path.Combine(_output, "observations.txt"), _observations.ToString());
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
        }

        private void Place(CharacterMotor body, Vector3 at, bool defending = false)
        {
            body.ClearStun(); body.ClearTrip(); body.Intent.Clear(); body.Intent.Parked = false;
            body.IsDefender = defending; body.Teleport(at);
            body.transform.rotation = Quaternion.LookRotation(Vector3.right);
            body.Intent.AimPoint = at + Vector3.right * 20;
        }

        private void Prepare(bool defending = false)
        {
            ZackLiveWire.RetireOwnedBy(-1);
            Kit.ResetForRound(new AbilityContext(_zack, _zack.GetComponent<Carrier>(), _zack.GetComponent<CombatVerbs>()));
            Place(_zack, new Vector3(-4, .12f, -5), defending);
            Place(_taya, new Vector3(7, .12f, 4), !defending);
            Place(_a, new Vector3(-7, .12f, 5)); Place(_b, new Vector3(7, .12f, 7));
            var carrier = _zack.GetComponent<Carrier>();
            if (defending) carrier.Held?.HostDisarm();
            else if (carrier.Held == null)
            {
                var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(s => s.SeatOfOrigin == _zack.PlayerSlot);
                shoe.transform.position = _zack.transform.position;
                carrier.HostPickUp(shoe);
                Assert.IsNotNull(carrier.Held, "The attacking review needs the real held slipper.");
            }
            _wide.transform.position = new Vector3(1, 3.7f, -13);
            _wide.transform.LookAt(new Vector3(0, .8f, -5));
        }

        private IEnumerator Take(string name, float seconds, Action<int> drive, bool owner = false, bool intro = false)
        {
            string folder = Path.Combine(_output, name); Directory.CreateDirectory(folder);
            var trace = new StringBuilder("frame,seconds,game_seconds,x,z,momentum,overclock,zapped,throttle,wires,backtrack_cooldown,wire_dark,a_zapped,b_zapped,pull_age,pull_x,pull_z,drawn_x,drawn_y,drawn_z,body_drawn,live_hidden,off_route,ring_x,ring_z,tag_x,tag_z,moments,rewind_copies,rewind_renderers,rewind_skinned,rewind_transforms,m0_x,m0_z,m0_on,m1_x,m1_z,m1_on,m2_x,m2_z,m2_on,m3_x,m3_z,m3_on,speed_streaks,max_streak_length,camera_fov,floor_marker_on,floor_x,floor_z,max_streak_curve\n");
            bool sawScene = false;
            // The stock review mode lets the world looks draw as an observer sees them. His private return echo is
            // withheld from this court camera; the victim take keeps his actual first-person view and HUD.
            var reviewRig = Camera.main.GetComponent<CameraRig>();
            reviewRig.SetDebugThirdPerson(!owner);
            if (!owner) reviewRig.SetDebugCameraLock(true);
            var overlays = new System.Collections.Generic.List<(Canvas canvas, RenderMode mode, Camera camera, float distance)>();
            var layers = new System.Collections.Generic.Dictionary<Transform, int>();
            if (owner)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    overlays.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                    foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
                    { layers[child] = child.gameObject.layer; child.gameObject.layer = 31; }
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _ui; canvas.planeDistance = 1;
                }
            }
            _late.Shoot = path =>
            {
                try
                {
                    Texture source = null;
                    if (intro)
                        foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                            if (image.name == "UltimateScene" && image.isActiveAndEnabled && image.enabled && image.texture != null)
                            { source = image.texture; sawScene = true; break; }
                    if (source == null)
                    {
                        var privateEchoes = owner ? Array.Empty<Renderer>() : Object.FindObjectsByType<ZackBacktrackEcho>(FindObjectsSortMode.None)
                            .SelectMany(e => e.GetComponentsInChildren<Renderer>(true)).ToArray();
                        var hidden = privateEchoes.Select(r => r.forceRenderingOff).ToArray();
                        foreach (var surface in privateEchoes) surface.forceRenderingOff = true;
                        try { PaeteKitPlayProbe.RenderFilmView(owner ? Camera.main : _wide, _hdr); }
                        finally { for (int i = 0; i < privateEchoes.Length; i++) privateEchoes[i].forceRenderingOff = hidden[i]; }
                        source = _hdr;
                    }
                    Graphics.Blit(source, _ldr);
                    if (owner) { Canvas.ForceUpdateCanvases(); _ui.Render(); }
                    var active = RenderTexture.active; RenderTexture.active = _ldr;
                    _pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); _pixels.Apply(); RenderTexture.active = active;
                    File.WriteAllBytes(path, _pixels.EncodeToJPG(90));
                }
                finally { _late.Pending = null; }
            };
            using var audio = new ZackFilmAudio(folder);
            try
            {
                int f = 0, count = Mathf.RoundToInt(seconds * 30);
                float gameStart = Time.time;
                _frame++; drive(0);
                Exception captureError = null;
                _late.Each = () =>
                {
                    if (f >= count || captureError != null) return;
                    try
                    {
                        if (!owner) Camera.main.transform.SetPositionAndRotation(_wide.transform.position, _wide.transform.rotation);
                        _late.Shoot(Path.Combine(folder, $"{f:D5}.jpg"));
                        audio.Frame();
                        var at = _zack.transform.position;
                        var pull = Object.FindAnyObjectByType<ZackBacktrackLook>();
                        var seen = pull != null ? pull.VisualFeet : at; var drawn = pull != null ? pull.RenderedFeet : at;
                        var plate = _zack.GetComponentInChildren<CharacterNameplate>();
                        var ringAt = plate != null ? plate.transform.Find("NameplateRing").position : at;
                        var tagAt = plate != null ? plate.transform.Find("NameplateLabel").position : at;
                        trace.Append(FormattableString.Invariant($"{f},{f/30f:F3},{Time.time-gameStart:F3},{at.x:F3},{at.z:F3},{Kit.MomentumSeconds:F3},{Kit.OverclockLeft:F3},{_zack.ZappedLeft:F3},{Kit.ThrottleLeft:F3},{ZackLiveWire.Active.Count},{Kit.Skill1.CooldownRemaining:F3},{(ZackLiveWire.Active.Any(w=>w.Dark)?1:0)},{(_a.IsZapped?1:0)},{(_b.IsZapped?1:0)},{(pull != null ? pull.Age : -1):F3},{seen.x:F3},{seen.z:F3},{drawn.x:F3},{drawn.y:F3},{drawn.z:F3},{(pull != null && pull.BodyDrawn ? 1 : 0)},{(pull != null && pull.LiveBodyHidden ? 1 : 0)},{(pull != null ? pull.OffRoute(drawn) : 0):F3},{ringAt.x:F3},{ringAt.z:F3},{tagAt.x:F3},{tagAt.z:F3},{(pull != null ? pull.MomentsDrawn : 0)},{(pull != null ? pull.transform.Cast<Transform>().Count(t=>t.GetComponent<LineRenderer>()==null) : 0)},{(pull != null ? pull.GetComponentsInChildren<Renderer>(true).Length : 0)},{(pull != null ? pull.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length : 0)},{(pull != null ? pull.GetComponentsInChildren<Transform>(true).Length - 1 : 0)}"));
                        for (int moment = 0; moment < 4; moment++)
                        {
                            Vector3 position = Vector3.zero; bool visible = false;
                            if (pull != null) pull.TryMoment(moment, out position, out visible);
                            trace.Append(FormattableString.Invariant($",{position.x:F3},{position.z:F3},{(visible ? 1 : 0)}"));
                        }
                        var streaks = pull != null ? pull.GetComponentsInChildren<LineRenderer>(true) : Array.Empty<LineRenderer>();
                        float longest = streaks.Where(s => s.enabled).Select(ZackKitAcceptanceProbe.LengthOf).DefaultIfEmpty(0).Max();
                        bool floorOn = TagExposure.OnFloor(_zack, .8f, out var floorFeet, out _, out _);
                        trace.AppendLine(FormattableString.Invariant($",{streaks.Count(s => s.enabled)},{longest:F3},{Camera.main.fieldOfView:F3},{(floorOn ? 1 : 0)},{floorFeet.x:F3},{floorFeet.z:F3},{streaks.Where(s => s.enabled).Select(ZackKitAcceptanceProbe.CurveOf).DefaultIfEmpty(0).Max():F3},{(Kit.Zapping ? 1 : 0)},{Kit.ZapTarget},{Kit.ZapLock01:F3},{(Kit.ZapGrounded ? 1 : 0)},{ZackRules.MomentumStep(Kit.MomentumSeconds)},{(_zack.GetComponentInChildren<CharacterAnimator>()?.GaitDashWeight ?? 0):F3},{(ZackLiveWire.Active.Any(w => w.Reeled) ? 1 : 0)},{(pull != null ? pull.PastMomentsDrawn : 0)},{_zack.Stamina.Ratio:F3}"));
                        f++;
                        if (f < count) { _frame++; drive(f); }
                    }
                    catch (Exception error) { captureError = error; }
                };
                while (f < count && captureError == null) yield return null;
                if (captureError != null) throw new InvalidOperationException("Native film capture failed", captureError);
                if (!intro) Assert.AreEqual(seconds, Time.time - gameStart, .15f, "The saved frame clock diverged from simulation.");
            }
            finally
            {
                _late.Each = null;
                _late.Shoot = null;
                foreach (var item in overlays)
                    if (item.canvas != null) { item.canvas.renderMode = item.mode; item.canvas.worldCamera = item.camera; item.canvas.planeDistance = item.distance; }
                foreach (var item in layers) if (item.Key != null) item.Key.gameObject.layer = item.Value;
            }
            File.WriteAllText(Path.Combine(folder, "frames.csv"), trace.ToString());
            if (intro) Assert.IsTrue(sawScene, "The actual Overclock introduction was not shown.");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        [UnityTest, Timeout(900000)] public IEnumerator FilmEachSkillFromOneAngle()
        {
            if (Environment.GetEnvironmentVariable("TUMP_ZACK_FILM") != "1") Assert.Ignore("Set TUMP_ZACK_FILM=1 to record.");
            _output = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "zack-skills");
            Directory.CreateDirectory(_output);
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            NetAuthority.Provider = new SoloProvider();
            Settings.SettingsStore.Current.ReducedEffects = false;
            Settings.SettingsStore.Current.MasterVolume = 1;
            Settings.SettingsStore.Current.SfxVolume = 1;
            Settings.SettingsStore.Current.MusicVolume = .25f;
            Settings.SettingsStore.Current.AmbienceVolume = .35f;
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            _zack = GameServices.Round.PlayerAt(GameLaunch.SoloSeat); _zack.IsBot = false;
            _taya = GameServices.Round.Players.First(p => p != _zack && p.IsDefender);
            _a = GameServices.Round.Players.First(p => p != _zack && p != _taya);
            _b = GameServices.Round.Players.First(p => p != _zack && p != _taya && p != _a);
            _zack.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack"); _zack.AbilitySystem.BindHero("zack");
            var art = RosterBook.Load().FindPersonArt("zack");
            _zack.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(_zack);
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(_zack, true); rig.SetAimSource(AimSource.Movement);
            _wide = new GameObject("Zack film fixed camera").AddComponent<Camera>();
            _wide.CopyFrom(Camera.main); _wide.enabled = false; _wide.tag = "Untagged"; _wide.fieldOfView = 55;
            _wide.cullingMask &= ~(1 << 5); _wide.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            _hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            _ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            _ui = new GameObject("Zack film HUD camera").AddComponent<Camera>();
            _ui.enabled = false; _ui.clearFlags = CameraClearFlags.Nothing; _ui.cullingMask = 1 << 31;
            _ui.orthographic = true; _ui.nearClipPlane = .01f; _ui.farClipPlane = 10; _ui.targetTexture = _ldr;
            _late = new GameObject("Zack late film capture").AddComponent<LateFrameShot>();
            Time.captureFramerate = 30; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + _frame / 30.0;
            bool backtrackOnly = Environment.GetEnvironmentVariable("TUMP_ZACK_BACKTRACK_ONLY") == "1";
            var only = Environment.GetEnvironmentVariable("TUMP_ZACK_TAKES");
            bool Want(string take) => string.IsNullOrEmpty(only) || only.Split(',').Any(t => take.StartsWith(t.Trim()));
            var animator = _zack.GetComponentInChildren<CharacterAnimator>();
            if (!backtrackOnly)
            {
                // MOMENTUM: walk into it, then sprint an L so the three stages, the dash gear and the stage-up rings all read on
                // the fixed court camera; then stop. The first-person take repeats the run from his own eyes (lens, edge
                // streaks, the Charge arc and its Momentum pips).
                foreach (var take in new[] { "01-momentum", "01b-momentum-first-person" })
                {
                    if (!Want(take)) continue;
                    bool eyes = take.EndsWith("first-person");
                    Prepare(); Place(_zack, new Vector3(-6, .12f, -6)); yield return new WaitForSeconds(.2f);
                    float maxMomentum = 0, maxDash = 0; int stageUps = 0, lastStage = 0;
                    bool streamerSeen = false, threeStreamers = false, arcSeen = false;
                    yield return Take(take, 5.2f, f =>
                    {
                        _zack.Intent.Move = f >= 12 && f < 48 ? Vector2.right : f >= 48 && f < 92 ? Vector2.up : f >= 92 && f < 126 ? Vector2.left : Vector2.zero;
                        _zack.Intent.Set(Verb.Sprint, f >= 48 && f < 126);
                        if (_zack.Intent.Move.sqrMagnitude > 0) _zack.Intent.AimPoint = _zack.transform.position + new Vector3(_zack.Intent.Move.x, 0, _zack.Intent.Move.y) * 20;
                        maxMomentum = Mathf.Max(maxMomentum, Kit.MomentumSeconds);
                        if (animator != null) maxDash = Mathf.Max(maxDash, animator.GaitDashWeight);
                        int stage = ZackRules.MomentumStep(Kit.MomentumSeconds);
                        if (stage > lastStage) stageUps++; lastStage = stage;
                        foreach (var look in Object.FindObjectsByType<ZackMomentumLook>(FindObjectsSortMode.None))
                        {
                            int streamers = 0;
                            foreach (var line in look.GetComponentsInChildren<LineRenderer>())
                            {
                                if (!line.enabled) continue;
                                if (line.name.StartsWith("LightningStreamer")) streamers++;
                                arcSeen |= line.name.StartsWith("BodyArc");
                            }
                            streamerSeen |= streamers > 0; threeStreamers |= streamers >= 3;
                        }
                    }, owner: eyes);
                    if (!eyes) Assert.IsTrue(streamerSeen && threeStreamers && arcSeen,
                        "Observers must see the speed language: streamers=" + streamerSeen + " three=" + threeStreamers + " arcs=" + arcSeen);
                    Assert.AreEqual(3, ZackRules.MomentumStep(maxMomentum));
                    Assert.GreaterOrEqual(stageUps, 3, "Each Momentum stage must arrive as its own beat.");
                    Assert.Greater(maxDash, .6f, "His sprint at full Momentum must flatten into the dash gear.");
                    Assert.Zero(Kit.MomentumSeconds);
                    _observations.AppendLine(FormattableString.Invariant($"{take}: three stages ({stageUps} stage-ups), dash weight up to {maxDash:F2}, reset on stopping."));
                }
            }
            foreach (var take in new[] { "02-backtrack", "02b-backtrack-first-person" })
            {
                if (!Want(take)) continue;
                bool eyes = take.EndsWith("first-person");
                Prepare(); yield return new WaitForSeconds(.2f);
                bool rewound = false; ZackBacktrackLook pull = null; Vector3 departure = default, landing = default;
                float offRoute = 0, lastLeft = float.PositiveInfinity; int drawnFrames = 0, closer = 0, frames = 0, pastChosen = 0, pastSeen = 0;
                // An L-shaped run: along the near line, then up-court. The rewind must retrace both legs, not cut across.
                yield return Take(take, 5.4f, f =>
                {
                    _zack.Intent.Move = f >= 9 && f < 58 ? Vector2.right : f >= 58 && f < 112 ? Vector2.up : Vector2.zero;
                    _zack.Intent.Set(Verb.Skill1, f >= 113 && f < 118);
                    if (f == 113) departure = _zack.transform.position;
                    rewound |= Kit.Skill1.CooldownRemaining > 20;
                    var live = Object.FindAnyObjectByType<ZackBacktrackLook>();
                    if (live != null && pull == null) { pull = live; landing = _zack.transform.position; pastChosen = live.PastMomentsChosen; }
                    if (live == null || !live.Pulling) return;
                    frames++; var drawn = live.RenderedFeet; float left = Vector3.Distance(Flat(drawn), Flat(landing));
                    if (left < lastLeft - .01f) closer++; lastLeft = left;
                    offRoute = Mathf.Max(offRoute, live.OffRoute(drawn));
                    pastSeen = Mathf.Max(pastSeen, live.PastMomentsDrawn);
                    if (live.BodyDrawn && live.LiveBodyHidden) drawnFrames++;
                }, owner: eyes);
                Assert.IsTrue(rewound); Assert.IsTrue(_zack.HoldingSlipper);
                Assert.IsNotNull(pull, "No rewind presentation was created by the input cast.");
                Assert.Greater(Vector3.Distance(Flat(departure), Flat(landing)), 5f, "The review run must give the return a visible distance.");
                Assert.GreaterOrEqual(frames, 12, "The pull must last long enough to read at normal speed.");
                Assert.Greater(closer, frames * 2 / 3, "The drawn body must travel steadily toward the old spot.");
                Assert.Less(offRoute, .6f, "The drawn body left his recorded route.");
                Assert.GreaterOrEqual(pastChosen, 2, "A long bent rewind must show at least two of his past moments.");
                if (!eyes) Assert.GreaterOrEqual(drawnFrames, frames - 1, "An observer must see the actual body travel, with the gameplay body hidden.");
                else Assert.Zero(drawnFrames, "His own first person must not draw his body across the view.");
                _observations.AppendLine(FormattableString.Invariant($"{take}: departure {departure}, landing {landing}, route {pull.RouteLength:F2} m, {frames} pull frames, {closer} closer, max off-route {offRoute:F2} m, observer body frames {drawnFrames}, past moments {pastChosen} chosen, up to {pastSeen} standing at once."));
            }

            if (backtrackOnly) yield break;
            // FULL THROTTLE (owner pick, 2026-10-26: "make it look like he's recharged"): he stands on an empty bar, taps it,
            // the bolts strike him and the charge band climbs his body; he bursts away at full Momentum, throws his slipper
            // mid-run, stops dead inside the lock (still full) and runs on; then the lock runs out and a stop costs it again.
            foreach (var take in new[] { "03-full-throttle", "03b-full-throttle-first-person" })
            {
                if (!Want(take)) continue;
                bool eyes = take.EndsWith("first-person");
                Prepare(); Place(_zack, new Vector3(-4, .12f, -Confinement.Radius - .4f)); Place(_taya, new Vector3(4, .12f, -1), true);
                yield return new WaitForSeconds(.2f);
                _zack.Stamina.Deplete();
                bool surged = false, fullAtOnce = false, heldThroughStop = true, heldThroughThrow = true, endedAfter = false;
                bool actuallyThrew = false, goldSurge = false;
                var thrownShoe = _zack.GetComponent<Carrier>().Held;
                Assert.IsNotNull(thrownShoe);
                Assert.IsFalse(_zack.IsInsideBox(), "The throw demonstration starts outside the danger zone.");
                int castFrame = -1;
                yield return Take(take, 5.4f, f =>
                {
                    _zack.Intent.Set(Verb.Skill2, f >= 18 && f < 22);
                    bool stopped = f >= 58 && f < 74;
                    var move = f >= 22 && f < 48 ? Vector2.right : f >= 48 && f < 58 ? Vector2.up
                        : f >= 74 && f < 98 ? Vector2.up : f >= 98 && f < 134 ? Vector2.left : Vector2.zero;
                    _zack.Intent.Move = move;
                    _zack.Intent.Set(Verb.Sprint, move.sqrMagnitude > 0);
                    // A short charge and release: the throw leaves his hand at frame 46.
                    _zack.Intent.Set(Verb.SpecialAbility, f >= 34 && f < 46);
                    if (move.sqrMagnitude > 0) _zack.Intent.AimPoint = _zack.transform.position + new Vector3(move.x, 0, move.y) * 20;
                    actuallyThrew |= thrownShoe != null && thrownShoe.State == SlipperState.InFlight
                        && thrownShoe.ThrowerSlot == _zack.PlayerSlot && _zack.GetComponent<Carrier>().Held == null;
                    foreach (var look in Object.FindObjectsByType<ZackThrottleLook>(FindObjectsSortMode.None).Where(l => l.BelongsTo(_zack)))
                    {
                        var core = look.transform.Find("SurgeBolt0")?.GetComponent<LineRenderer>();
                        var ink = look.transform.Find("SurgeBoltInk0")?.GetComponent<LineRenderer>();
                        if (core != null && ink != null && core.enabled && ink.enabled)
                            goldSurge |= core.sharedMaterial.color.g > .7f && core.sharedMaterial.renderQueue > ink.sharedMaterial.renderQueue;
                    }
                    if (Kit.Throttled && castFrame < 0)
                    {
                        castFrame = f; surged = true;
                        fullAtOnce = Kit.MomentumSeconds >= ZackRules.MomentumRampSeconds - 1e-3f && _zack.Stamina.Ratio > .99f;
                    }
                    if (Kit.Throttled && f > castFrame + 1)
                    {
                        if (stopped) heldThroughStop &= Kit.MomentumSeconds >= ZackRules.MomentumRampSeconds - 1e-3f;
                        if (f >= 47 && f < 58) heldThroughThrow &= Kit.MomentumSeconds >= ZackRules.MomentumRampSeconds - 1e-3f;
                    }
                    if (f >= 150 && !Kit.Throttled && Kit.MomentumSeconds <= 0f) endedAfter = true;
                }, owner: eyes);
                Assert.IsTrue(surged, "Full Throttle was never cast.");
                Assert.IsTrue(fullAtOnce, "Momentum and Charge must both be full the frame the lock starts.");
                Assert.IsTrue(heldThroughStop, "A stop inside the lock must not cost Momentum.");
                Assert.IsTrue(heldThroughThrow, "A throw inside the lock must not cost Momentum.");
                Assert.IsTrue(actuallyThrew, "The capture must release a real slipper, not pass while its throw is refused.");
                Assert.IsTrue(goldSurge, "The recharge must draw its gold core above its backing instead of a solid dark cable.");
                Assert.IsTrue(endedAfter, "After the lock a stop must cost Momentum again.");
                _observations.AppendLine(FormattableString.Invariant($"{take}: cast at frame {castFrame}; full Momentum and Charge at once; actual slipper release; held through a throw and a stop; gold core above backing; reset after the lock."));
            }

            // LIVE WIRE: a tap strings it; a timed jump clears it; a walker is Zapped and the wire goes dark but stays; a second
            // walker crosses the dark wire freely; it relights with the rearm crawl; he walks up, looks at it and holds to reel it
            // in. Then Overclocked he strings an overcharged wire that Zaps two crossers and stays lit.
            foreach (var take in new[] { "04-live-wire", "04b-live-wire-first-person" })
            {
                if (!Want(take)) continue;
                bool eyes = take.EndsWith("first-person");
                Prepare(true); Place(_zack, new Vector3(-2, .12f, -5), true);
                Place(_a, new Vector3(0, .12f, -7)); Place(_b, new Vector3(1, .12f, -5.8f));
                yield return new WaitForSeconds(.2f);
                bool firstZap = false, wentDark = false, stayed = false, darkCrossFree = false, relit = false, jumpedClear = false, reeled = false, overchargedTwo = false;
                ZackLiveWire wire = null;
                var wireAim = new Vector3(2, 0, -5);
                yield return Take(take, 13.5f, f =>
                {
                    _zack.Intent.AimPoint = wireAim;
                    if (eyes) FaceTo(_zack, wireAim);
                    if (f < 300) _zack.Intent.Set(Verb.Skill2, f >= 10 && f < 13);
                    if (wire == null) wire = ZackLiveWire.OwnedBy(_zack.PlayerSlot);
                    // b times a jump over it; a walks through and is Zapped.
                    if (f < 132)
                    {
                        _b.Intent.Move = f >= 42 && f < 65 ? Vector2.up : Vector2.zero;
                        _b.Intent.Set(Verb.Sprint, f >= 42 && f < 65); _b.Intent.Set(Verb.Jump, f >= 42 && f < 45);
                        if (f == 66) jumpedClear = !_b.IsZapped && wire != null && wire.IsArmed;
                        _a.Intent.Move = f >= 70 && f < 120 ? Vector2.up : Vector2.zero;
                    }
                    firstZap |= _a.IsZapped; wentDark |= wire != null && wire.Dark;
                    if (f == 128) stayed = wire != null && wire.IsLive;
                    // While dark, b walks back across it freely.
                    if (f >= 132 && f < 175) { _b.Intent.Move = Vector2.down; if (f == 174) darkCrossFree = !_b.IsZapped && wire != null && wire.Dark; }
                    else if (f >= 175 && f < 180) _b.Intent.Move = Vector2.zero;
                    if (f == 250) relit = wire != null && wire.IsArmed;
                    // He walks up to it, looks at its middle and holds to reel it in.
                    if (f >= 255 && f < 285) { _zack.Intent.Move = Vector2.right * .6f; wireAim = new Vector3(0, 0, -5); }
                    else if (f >= 285 && f < 300) _zack.Intent.Move = Vector2.zero;
                    if (f >= 300 && f < 300 + 18) { _zack.Intent.AimPoint = wire != null && wire.IsLive ? (wire.PostA + wire.PostB) * .5f : wireAim; _zack.Intent.Set(Verb.Skill2, true); }
                    else if (f >= 318 && f < 330) _zack.Intent.Set(Verb.Skill2, false);
                    reeled |= wire != null && wire.Reeled;
                    if (f == 330)
                    {
                        Place(_zack, new Vector3(-2, .12f, -5), true);
                        Place(_a, new Vector3(-.5f, .12f, -7)); Place(_b, new Vector3(.8f, .12f, -7.5f));
                        Kit.BeginOverclock(_zack); Kit.DefendingSkill.ApplyNetworkSnapshot(0, 1);
                        wireAim = new Vector3(2, 0, -5); wire = null;
                    }
                    if (f >= 330) _zack.Intent.Set(Verb.Skill2, f >= 338 && f < 341);
                    if (f >= 360) { _a.Intent.Move = Vector2.up; _b.Intent.Move = Vector2.up; }
                    if (f == 400) overchargedTwo = _a.IsZapped || _b.IsZapped;
                }, owner: eyes);
                Assert.IsTrue(jumpedClear, "A timed jump must clear the low wire without tripping it.");
                Assert.IsTrue(firstZap && wentDark && stayed, "A trip Zaps, darkens and leaves the wire in place.");
                Assert.IsTrue(darkCrossFree, "A dark wire lets the next attacker through.");
                Assert.IsTrue(relit, "The wire rearms after its dark window.");
                Assert.IsTrue(reeled, "Looking at the wire and holding reels it in.");
                Assert.IsTrue(ZackLiveWire.Active.Any(w => w.StrungOvercharged && w.IsArmed), "Overcharged stays lit.");
                _observations.AppendLine($"{take}: jump cleared, trip Zapped and darkened it, it stayed, a dark crossing was free, it rearmed, a held look reeled it in, overcharged stayed lit (crosser Zapped {overchargedTwo}).");
            }

            if (Want("06-zapped-victim"))
            {
                Prepare(); yield return new WaitForSeconds(.2f); bool seized = false, stayedInFirstPerson = true;
                yield return Take("06-zapped-victim", 2.6f, f =>
                {
                    if (f == 21) _zack.ApplyZapped();
                    _zack.Intent.Move = Vector2.up; seized |= _zack.IsZapped && !_zack.CanAct();
                    if (_zack.IsZapped) stayedInFirstPerson &= rig.Mode == CameraMode.Fpp;
                }, owner: true);
                Assert.IsTrue(seized); Assert.IsFalse(_zack.IsZapped); Assert.AreEqual(CameraMode.Fpp, rig.Mode);
                Assert.IsTrue(stayedInFirstPerson, "Shock must keep the victim in first person throughout the stun.");
                _observations.AppendLine("Zapped locked the victim and kept their first-person view.");
            }

            if (Want("05-overclock"))
            {
                Prepare(); Kit.AddUltimateCharge(Kit.UltimateCost);
                Place(_taya, new Vector3(3, .12f, -3), true);
                _wide.transform.position = new Vector3(-5, 3.2f, -10);
                _wide.transform.LookAt(new Vector3(1.8f, .9f, -3.8f));
                yield return new WaitForSeconds(.2f); bool charged = false, zapped = false, grounded = false; int chargedAt = -1;
                yield return Take("05-overclock", 13f, f =>
                {
                    _zack.Intent.Set(Verb.Ultimate, f >= 15 && f < 20);
                    if (!Kit.IsOverclocked) return;
                    charged = true; if (chargedAt < 0) chargedAt = f;
                    int t = f - chargedAt;
                    _zack.Intent.Move = t < 40 ? Vector2.right : Vector2.zero;
                    // Then he plants and holds the zap on the taya until it Zaps and grounds them, and lets go.
                    _zack.Intent.AimPoint = _taya.transform.position;
                    if (t >= 45) FaceTo(_zack, _taya.transform.position);
                    _zack.Intent.Set(Verb.Ultimate, t >= 50 && t < 50 + 66);
                    _taya.Intent.Move = t >= 50 && t < 70 ? Vector2.left * .4f : Vector2.zero;
                    zapped |= _taya.IsZapped; grounded |= Kit.ZapGrounded;
                }, intro: true);
                Assert.IsTrue(charged); Assert.IsTrue(zapped, "The held zap must Zap the rival it holds.");
                Assert.IsTrue(grounded);
                _observations.AppendLine("Actual ultimate introduction handed play back Overclocked; the held two-handed zap Zapped and grounded the taya.");
            }

            if (Want("05b-overclock-zap-first-person"))
            {
                Prepare(); Place(_taya, new Vector3(1.5f, .12f, -2), true); Place(_a, new Vector3(3, .12f, -6));
                Kit.BeginOverclock(_zack);
                yield return new WaitForSeconds(.2f); bool zapped = false, searched = false, stowed = false, bothHands = false; float fuelStart = Kit.OverclockLeft, fuelEnd = 0;
                var zapArms = rig.GetComponentInChildren<ViewmodelArms>(true);
                Assert.IsNotNull(zapArms);
                var viewShoe = zapArms.transform.Find("RightPivot/Arm/HeldSlipper");
                var leftZapPivot = zapArms.transform.Find("LeftPivot"); var rightZapPivot = zapArms.transform.Find("RightPivot");
                var realShoe = _zack.GetComponent<Carrier>().Held;
                Assert.IsNotNull(viewShoe); Assert.IsNotNull(realShoe);
                yield return Take("05b-overclock-zap-first-person", 6f, f =>
                {
                    // Sweep from empty court onto the taya, hold through the Zap, then let go.
                    var target = f < 30 ? _zack.transform.position + new Vector3(4, 0, -1) : _taya.transform.position;
                    _zack.Intent.AimPoint = target; FaceTo(_zack, target);
                    _zack.Intent.Set(Verb.Ultimate, f >= 12 && f < 120);
                    searched |= Kit.Zapping && Kit.ZapTarget < 0;
                    zapped |= _taya.IsZapped; fuelEnd = Kit.OverclockLeft;
                    if (Kit.Zapping && f >= 20)
                    {
                        Assert.AreSame(realShoe, _zack.GetComponent<Carrier>().Held, "Stowing the viewmodel must not change possession.");
                        stowed |= !viewShoe.gameObject.activeSelf;
                        if (zapArms.TryZackHandTips(out var left, out var right))
                        {
                            var l = rig.Camera.WorldToViewportPoint(left); var r = rig.Camera.WorldToViewportPoint(right);
                            bothHands |= l.z > 0 && r.z > 0 && l.x > 0 && l.x < 1 && r.x > 0 && r.x < 1
                                && l.y > 0 && l.y < 1 && r.y > 0 && r.y < 1 && Mathf.Abs(l.x - r.x) > .15f
                                && Vector3.Distance(leftZapPivot.localScale, rightZapPivot.localScale) < .02f;
                        }
                    }
                }, owner: true);
                Assert.IsTrue(searched, "Holding with nobody in the cone searches.");
                Assert.IsTrue(zapped);
                Assert.IsTrue(stowed && bothHands, "Both empty hands must be separated and visible while the zap is held.");
                Assert.AreSame(realShoe, _zack.GetComponent<Carrier>().Held);
                Assert.IsTrue(viewShoe.gameObject.activeSelf, "The held slipper must return after the zap pose releases.");
                _observations.AppendLine(FormattableString.Invariant($"05b: first-person zap searched, held and Zapped; fuel {fuelStart:F1} to {fuelEnd:F1} over 6 s."));
            }

            if (Want("07-zap-victim-first-person"))
            {
                // Another seat's Zack holds the zap on this screen's body: the victim's warning, lock bar and the Zap.
                Prepare(); var enemy = _taya;
                enemy.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack");
                enemy.AbilitySystem.BindHero("zack");
                var enemyArt = RosterBook.Load().FindPersonArt("zack");
                enemy.GetComponent<CharacterVisual>().ApplyModel(enemyArt.Model, enemyArt.Tint, enemyArt.Clips, enemyArt.Palette, enemyArt.PetModel);
                Place(enemy, new Vector3(1, .12f, -5), true); Place(_zack, new Vector3(-4, .12f, -5));
                FaceTo(enemy, _zack.transform.position); FaceTo(_zack, enemy.transform.position);
                var enemyKit = (ZackHeroKit)enemy.AbilitySystem.Kit;
                enemyKit.BeginOverclock(enemy);
                yield return new WaitForSeconds(.2f); bool warned = false, victimZapped = false;
                yield return Take("07-zap-victim-first-person", 4f, f =>
                {
                    FaceTo(_zack, enemy.transform.position);
                    // Host validation reads the body's aim too, not only the command context.
                    enemy.Intent.AimPoint = _zack.transform.position;
                    var ctx = new AbilityContext(enemy, null, null, enemy.transform.position, enemy.transform.forward, _zack.transform.position);
                    enemyKit.Ultimate.StepHeldCommand(ctx, f >= 15 && f < 95, f / 30f);
                    warned |= enemyKit.ZapTarget == _zack.PlayerSlot; victimZapped |= _zack.IsZapped;
                }, owner: true);
                Assert.IsTrue(warned && victimZapped, "The victim must see the held zap and then be Zapped.");
                _observations.AppendLine("07: the victim's screen named the zap, filled its lock bar and was Zapped.");
            }
        }

        private static void FaceTo(CharacterMotor body, Vector3 target)
        {
            var d = target - body.transform.position; d.y = 0;
            if (d.sqrMagnitude > .01f) body.transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
