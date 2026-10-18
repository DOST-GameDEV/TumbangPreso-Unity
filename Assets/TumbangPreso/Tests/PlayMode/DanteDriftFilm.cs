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
    /// <summary>
    /// FILM, not a gate (`docs/HERO_KIT_METHOD.md` section 7): Dante's CONTINENTAL DRIFT in a real Hero Strike match on Bayan
    /// Plaza at true game speed (30 frames per game second, the phase on the film clock). Views: his screen (the cutscene, then
    /// his own first person), the court from his right, and a player the cascade catches. Three players stand ahead of him in
    /// the cascade's rectangle so the cutscene stages them on their plates. Runs only with TUMP_DANTE_FILM=1; frames under
    /// TUMP_EVIDENCE (or Logs)/dante-drift, with timing.csv, cues.csv, shots.txt and summary.txt for
    /// `tools/stitch_ability_film.py`. TUMP_DANTE_FILM_OWNER_ONLY=1 films his screen alone (iteration rounds).
    /// </summary>
    [Category("Review")]
    public sealed class DanteDriftFilm
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            if (Environment.GetEnvironmentVariable("TUMP_DANTE_FILM") != "1") yield break;
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
        }

        [UnityTearDown] public IEnumerator After()
        {
            SharedUltimatePhase.FilmClock = null;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
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
            public RenderTexture Target => _hdr;
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
            public void Render(Camera c)
            {
                var before = c.targetTexture; c.targetTexture = _hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
            }
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

        private static RawImage UltimateOverlay()
        {
            foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) return image;
            return null;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>This seat as <paramref name="hero"/>: the kit and the model.</summary>
        private static void Bind(CharacterMotor body, string hero)
        {
            body.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            body.AbilitySystem.BindHero(hero);
            var art = RosterBook.Load().FindPersonArt(hero);
            if (art != null) body.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
        }

        [UnityTest, Timeout(1200000)]
        public IEnumerator FilmContinentalDriftInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_DANTE_FILM") != "1") Assert.Ignore("Film only: set TUMP_DANTE_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            // TUMP_DANTE_FILM_POV=1 (owner, 2026-10-17: *"cutscene -> fpp view ng after cutscene of someone nna natamaan"*): the
            // player it hits is this machine's own seat, so "his screen" is THEIRS: the cutscene, then their own first person as
            // the ground comes for them, with everything their screen really does. Dante casts from a bot's seat.
            bool pov = Environment.GetEnvironmentVariable("TUMP_DANTE_FILM_POV") == "1";
            var local = round.PlayerAt(GameLaunch.SoloSeat);
            // Dante 6 m short of the can, facing it; three others ahead of him inside the cascade's lane.
            var dante = pov ? round.Players.First(p => p != null && p != local) : local;
            Bind(dante, "dante");
            var origin = can + new Vector3(0f, .12f, -6.0f);
            dante.Teleport(origin); dante.transform.rotation = Quaternion.identity;
            dante.Intent.Clear(); dante.Intent.Parked = false; dante.IsBot = pov;
            dante.Intent.AimPoint = origin + Vector3.forward * 8f; dante.Intent.FaceAimPoint = true;
            // Never Dante as the ones it hits (owner: *"dont use dante as a bot na tatamaann bcz its confusinng"*).
            var others = round.Players.Where(p => p != null && p != dante).OrderBy(p => p == local ? 1 : 0).ToArray();
            string[] looks = { "amihan", "paete", "cheska" };
            for (int i = 0; i < others.Length; i++) Bind(others[i], looks[i % looks.Length]);
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(local);
            // Scattered, never a tidy group (owner, 2026-10-17: "EVERYONE isnt always gonnna be in the same place"): one near on
            // his left, one mid-lane far to his right, one deep. TUMP_DANTE_FILM_GROUPED=1 keeps the old tight group.
            var spots = Environment.GetEnvironmentVariable("TUMP_DANTE_FILM_GROUPED") == "1"
                ? new[] { new Vector3(-1.6f, 0f, 4.2f), new Vector3(1.4f, 0f, 6.0f), new Vector3(-.4f, 0f, 8.4f) }
                : new[] { new Vector3(-5.2f, 0f, 3.0f), new Vector3(5.0f, 0f, 7.0f), new Vector3(-1.2f, 0f, 11.5f) };
            for (int i = 0; i < others.Length && i < spots.Length; i++)
            {
                others[i].Teleport(origin + spots[i]);
                others[i].Intent.Clear(); others[i].Intent.Parked = true;
                var d = Flat(origin - others[i].transform.position);
                if (d.sqrMagnitude > .01f) others[i].transform.rotation = Quaternion.LookRotation(d.normalized);
            }
            // In POV the local seat stands mid-lane on his right (the third band reaches them 0.3 s into play: the ground is seen
            // coming), looking back at him.
            if (pov)
            {
                var spot = origin + spots[1];
                var mid = others.First(p => p != local && (p.transform.position - spot).sqrMagnitude < .1f);
                mid.Teleport(local.transform.position); local.Teleport(spot);
                local.transform.rotation = Quaternion.LookRotation(Flat(origin - spot).normalized);
                local.Intent.Clear(); local.Intent.Parked = true; local.IsBot = false;
            }
            var victim = pov ? local : others[0];
            dante.AbilitySystem.Kit.AddUltimateCharge(dante.AbilitySystem.Kit.UltimateCost);
            Camera.main.GetComponent<CameraRig>().Follow(pov ? local : dante);
            yield return null;

            var wide = Film.Make("DanteWide", 50);
            var caught = Film.Make("DanteCaught", 60);
            var sideCam = Film.Make("DanteStageSide", 40);
            var highCam = Film.Make("DanteStageHigh", 50);
            var timing = new StringBuilder().AppendLine("frame,seconds,phase,overlay,age,shot,victim_concussed,round_clock");
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            int sceneFirst = -1, sceneLast = -1; float clockIn = -1, clockOut = -1;
            bool concussed = false;
            string root = null;
            try
            {
                bool side = Environment.GetEnvironmentVariable("TUMP_DANTE_FILM_SIDE") == "1";
                using (var film = new Film("dante-drift", "owner", "wide", "victim", "side", "high"))
                {
                    root = film.Root;
                    int frames = int.TryParse(Environment.GetEnvironmentVariable("TUMP_DANTE_FILM_FRAMES"), out int asked) && asked > 0 ? asked : 30 * 12;
                    bool ownerOnly = Environment.GetEnvironmentVariable("TUMP_DANTE_FILM_OWNER_ONLY") == "1";
                    for (int f = 0; f < frames; f++)
                    {
                        frame = f; film.Frame = f;
                        float t = f / 30f;
                        dante.Intent.Set(Verb.Ultimate, t > .6f && t < .8f);
                        if (t > .6f && t < .64f) dante.Intent.BufferPress(Verb.Ultimate);
                        dante.Intent.AimPoint = dante.transform.position + Vector3.forward * 8f;
                        yield return null;
                        var phase = SharedUltimatePhase.Instance;
                        bool inPhase = phase != null && phase.Active;
                        var overlay = UltimateOverlay();
                        if (overlay != null)
                        {
                            if (sceneFirst < 0) { sceneFirst = f; clockIn = round.TimeLeft; film.Cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_dante,1,0.2")); }
                            sceneLast = f; clockOut = round.TimeLeft;
                        }
                        concussed |= victim.IsConcussed;
                        timing.AppendLine(FormattableString.Invariant(
                            $"{f},{t:F3},{inPhase},{overlay != null},{UltimatePhaseView.LastAge:F3},{UltimatePhaseView.LastShot},{victim.IsConcussed},{round.TimeLeft:F3}"));

                        if (overlay != null) film.Save(overlay.texture, "owner");
                        else { film.Render(Camera.main); film.Save(film.Target, "owner"); }
                        // THE STAGE FROM OUTSIDE (TUMP_DANTE_FILM_SIDE=1, review only): the render copies seen square from his
                        // right and from high behind, so poses, the court's height and the beast under him can be judged.
                        if (side && overlay != null)
                        {
                            var copies = GameObject.Find("~UltimateRenderCopies");
                            var stageRoot = copies != null ? copies.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "IntroductionScene-dante") : null;
                            if (stageRoot != null)
                            {
                                var drawn = copies.GetComponentsInChildren<Renderer>(true).Where(r => r.forceRenderingOff).ToArray();
                                foreach (var r in drawn) r.forceRenderingOff = false;
                                var at = stageRoot.position;
                                if (f == sceneFirst) Debug.Log(FormattableString.Invariant($"[DanteFilm] stage root y {at.y:F3}; court under it {Slipper.GroundY(at + Vector3.up * .3f):F3}; live feet {dante.transform.position.y:F3}"));
                                // Measured in the stage's own space: his head, both hands, his lowest point, the beast's top and eyes.
                                if ((f - sceneFirst) % 3 == 0)
                                {
                                    Vector3 L(Vector3 w) => Quaternion.Inverse(stageRoot.rotation) * (w - at);
                                    var bones = copies.GetComponentsInChildren<Transform>(true);
                                    var cast = bones.FirstOrDefault(x => x.name.StartsWith("RecordedCast"));
                                    var head = cast != null ? cast.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "head") : null;
                                    var hand = cast != null ? cast.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "HandAnchor") : null;
                                    var armL = cast != null ? cast.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "arm-left") : null;
                                    var skins = cast != null ? cast.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
                                    float low = float.PositiveInfinity, top = float.NegativeInfinity;
                                    var baked = new Mesh(); var verts = new System.Collections.Generic.List<Vector3>();
                                    foreach (var s in skins)
                                    {
                                        if (!s.enabled || s.sharedMesh == null) continue;
                                        s.BakeMesh(baked); baked.GetVertices(verts);
                                        var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
                                        foreach (var vertex in verts) { float y = m.MultiplyPoint3x4(vertex).y; low = Mathf.Min(low, y); top = Mathf.Max(top, y); }
                                    }
                                    Object.Destroy(baked);
                                    var beast = bones.FirstOrDefault(x => x.name == "MountainBeast");
                                    string beastText = "";
                                    if (beast != null && beast.gameObject.activeInHierarchy)
                                    {
                                        var rs = beast.GetComponentsInChildren<Renderer>(true);
                                        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                                        beastText = FormattableString.Invariant($" beast min {L(b.min)} max {L(b.max)}");
                                        // Its horns and sockets on their own (film r7: a horn through him at 3.6, the eye lens on rock).
                                        foreach (var part in new[] { "horn-left", "horn-right", "glow-eyes" })
                                        {
                                            var node = beast.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == part);
                                            var prs = node != null ? node.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                                            if (prs.Length == 0) continue;
                                            var pb = prs[0].bounds; foreach (var r in prs) pb.Encapsulate(r.bounds);
                                            beastText += FormattableString.Invariant($" {part} min {L(pb.min)} max {L(pb.max)} centre {L(pb.center)}");
                                        }
                                    }
                                    Debug.Log(FormattableString.Invariant(
                                        $"[DanteMeasure] age {UltimatePhaseView.LastAge:F2} head {(head != null ? L(head.position) : Vector3.zero)} hand {(hand != null ? L(hand.position) : Vector3.zero)} armL {(armL != null ? L(armL.position) : Vector3.zero)} low {low - at.y:F3} top {top - at.y:F3}{beastText}"));
                                }
                                sideCam.transform.position = at + stageRoot.rotation * new Vector3(5.5f, 1.4f, .6f);
                                sideCam.transform.LookAt(at + Vector3.up * 1.0f + stageRoot.rotation * new Vector3(0f, 0f, .6f));
                                film.Render(sideCam); film.Save(film.Target, "side");
                                highCam.transform.position = at + stageRoot.rotation * new Vector3(7f, 9f, -9f);
                                highCam.transform.LookAt(at + stageRoot.rotation * new Vector3(0f, 1f, 3f));
                                film.Render(highCam); film.Save(film.Target, "high");
                                foreach (var r in drawn) r.forceRenderingOff = true;
                            }
                        }
                        if (ownerOnly) continue;

                        // THE COURT: high on his right, the whole lane ahead in frame.
                        wide.transform.position = origin + new Vector3(9.0f, 7.0f, -2.0f);
                        wide.transform.LookAt(origin + new Vector3(0f, .6f, 5.0f));
                        film.Render(wide); film.Save(film.Target, "wide");

                        // A CAUGHT PLAYER: behind and beside them, looking back down the lane at him.
                        var v = victim.transform.position;
                        var toHim = Flat(dante.transform.position - v); toHim = toHim.sqrMagnitude > .01f ? toHim.normalized : Vector3.back;
                        caught.transform.position = v - toHim * 2.6f + Vector3.Cross(Vector3.up, toHim) * 1.3f + Vector3.up * 1.9f;
                        caught.transform.LookAt(v + toHim * 2.5f + Vector3.up * .8f);
                        film.Render(caught); film.Save(film.Target, "victim");
                    }
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                dante.Intent.Clear();
                Object.Destroy(wide.gameObject); Object.Destroy(caught.gameObject);
                Object.Destroy(sideCam.gameObject); Object.Destroy(highCam.gameObject);
                if (root != null)
                {
                    File.WriteAllText(Path.Combine(root, "timing.csv"), timing.ToString());
                    File.WriteAllText(Path.Combine(root, "shots.txt"), UltimatePhaseView.LastShotReport ?? "");
                }
            }
            string summary = FormattableString.Invariant($"scene frames {sceneFirst}-{sceneLast} ({(sceneLast - sceneFirst + 1) / 30f:F2} s); clock {clockIn:F3} to {clockOut:F3}; victim seat {victim.PlayerSlot} concussed {concussed}");
            if (root != null) File.WriteAllText(Path.Combine(root, "summary.txt"), summary + Environment.NewLine);
            Debug.Log("[DanteFilm] " + summary);
            Assert.GreaterOrEqual(sceneFirst, 0, "The cutscene never came up.");
            Assert.AreEqual(clockIn, clockOut, 1e-4f, "The round clock ran under the cutscene.");
            Assert.Less((sceneLast - sceneFirst + 1) / 30f, 7.0f + .2f, "The cutscene ran long.");
        }
    }
}
