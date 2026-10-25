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
            ZackAfterimage.RetireOwnedBy(-1); ZackLiveWire.RetireOwnedBy(-1);
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
            var trace = new StringBuilder("frame,seconds,game_seconds,x,z,momentum,overclock,zapped,copies,wires,backtrack_cooldown,wire_snapped,a_zapped,b_zapped\n");
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
            try
            {
                int f = 0, count = Mathf.RoundToInt(seconds * 30);
                float gameStart = Time.time;
                _frame++; drive(0);
                _late.Each = () =>
                {
                    if (f >= count) return;
                    _late.Shoot(Path.Combine(folder, $"{f:D5}.jpg"));
                    var at = _zack.transform.position;
                    trace.AppendLine(FormattableString.Invariant($"{f},{f/30f:F3},{Time.time-gameStart:F3},{at.x:F3},{at.z:F3},{Kit.MomentumSeconds:F3},{Kit.OverclockLeft:F3},{_zack.ZappedLeft:F3},{ZackAfterimage.Active.Count},{ZackLiveWire.Active.Count},{Kit.Skill1.CooldownRemaining:F3},{(ZackLiveWire.Active.Any(w=>w.Snapped)?1:0)},{(_a.IsZapped?1:0)},{(_b.IsZapped?1:0)}"));
                    f++;
                    if (f < count) { _frame++; drive(f); }
                };
                while (f < count) yield return null;
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

        [UnityTest, Timeout(900000)] public IEnumerator FilmEachSkillFromOneAngle()
        {
            if (Environment.GetEnvironmentVariable("TUMP_ZACK_FILM") != "1") Assert.Ignore("Set TUMP_ZACK_FILM=1 to record.");
            _output = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "zack-skills");
            Directory.CreateDirectory(_output);
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            NetAuthority.Provider = new SoloProvider();
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
            Prepare(); yield return new WaitForSeconds(.2f);
            float maxMomentum = 0;
            yield return Take("01-momentum", 4.8f, f =>
            {
                _zack.Intent.Move = f >= 15 && f < 123 ? Vector2.right : Vector2.zero;
                maxMomentum = Mathf.Max(maxMomentum, Kit.MomentumSeconds);
            });
            Assert.AreEqual(3, ZackRules.MomentumStep(maxMomentum));
            Assert.AreEqual(-5f, _zack.transform.position.z, .05f, "The review run must stay across the fixed court shot.");
            Assert.Greater(_zack.transform.position.x, 4f);
            Assert.Zero(Kit.MomentumSeconds); _observations.AppendLine("Momentum reached all three steps and reset on stopping.");

            Prepare(); yield return new WaitForSeconds(.2f); bool rewound = false;
            yield return Take("02-backtrack", 5.4f, f =>
            {
                _zack.Intent.Move = f >= 9 && f < 112 ? Vector2.right : Vector2.zero;
                _zack.Intent.Set(Verb.Skill1, f >= 113 && f < 118);
                rewound |= Kit.Skill1.CooldownRemaining > 20;
            });
            Assert.IsTrue(rewound); Assert.IsTrue(_zack.HoldingSlipper);
            _observations.AppendLine("Backtrack accepted through input and kept the held slipper.");

            Prepare(); Place(_taya, new Vector3(3.5f, .12f, -5), true);
            yield return new WaitForSeconds(.2f); bool copySeen = false, popped = false;
            yield return Take("03-afterimage", 3.8f, f =>
            {
                _zack.Intent.Set(Verb.Skill2, f >= 15 && f < 20);
                copySeen |= ZackAfterimage.Active.Count > 0;
                popped |= ZackAfterimage.Active.Any(c => c.Popped);
            });
            Assert.IsTrue(copySeen && popped); _observations.AppendLine("Afterimage ran into the taya and popped.");

            Prepare(true); Place(_zack, new Vector3(-2, .12f, -5), true);
            Place(_a, new Vector3(0, .12f, -7)); Place(_b,new Vector3(1,.12f,-5.8f));
            yield return new WaitForSeconds(.2f); bool firstZap = false, snapped = false, jumpedClear = false;
            yield return Take("04-live-wire", 9f, f =>
            {
                _zack.Intent.AimPoint = new Vector3(2, 0, -5);
                _zack.Intent.Set(Verb.Skill2, f >= 10 && f < 33);
                _a.Intent.Move = f >= 70 && f < 120 ? Vector2.up : Vector2.zero;
                if(f<132)
                {
                    _b.Intent.Move=f>=42&&f<65?Vector2.up:Vector2.zero;
                    _b.Intent.Set(Verb.Sprint,f>=42&&f<65);_b.Intent.Set(Verb.Jump,f>=42&&f<45);
                    if(f==66)jumpedClear=!_b.IsZapped&&ZackLiveWire.Active.Any(w=>w.IsLive);
                }
                firstZap |= _a.IsZapped; snapped |= ZackLiveWire.Active.Any(w => w.Snapped);
                if (f == 132)
                {
                    Place(_a, new Vector3(-.5f, .12f, -7)); Place(_b, new Vector3(.8f, .12f, -7.5f));
                    Kit.BeginOverclock(_zack); Kit.DefendingSkill.ApplyNetworkSnapshot(0, 1);
                }
                if (f >= 132) _zack.Intent.Set(Verb.Skill2, f >= 140 && f < 163);
                if (f >= 174) { _a.Intent.Move = Vector2.up; _b.Intent.Move = Vector2.up; }
            });
            Assert.IsTrue(jumpedClear,"A timed jump must clear the low wire without consuming it."); Assert.IsTrue(firstZap && snapped); Assert.IsTrue(ZackLiveWire.Active.Any(w => w.Overcharged && !w.Snapped));
            _observations.AppendLine("A timed jump cleared the low wire; walking through zapped and snapped it; overcharged stayed intact.");

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

            Prepare(); Kit.AddUltimateCharge(Kit.UltimateCost);
            yield return new WaitForSeconds(.2f); bool charged = false; int chargedAt = -1;
            yield return Take("05-overclock", 10f, f =>
            {
                _zack.Intent.Set(Verb.Ultimate, f >= 15 && f < 20);
                if (Kit.IsOverclocked)
                {
                    charged = true; if (chargedAt < 0) chargedAt = f;
                    _zack.Intent.Move = f - chargedAt < 45 ? Vector2.right : Vector2.zero;
                    _zack.Intent.Set(Verb.Skill2, f - chargedAt >= 55 && f - chargedAt < 60);
                }
            }, intro: true);
            Assert.IsTrue(charged); _observations.AppendLine("Actual ultimate introduction handed play back Overclocked.");
        }
    }
}
