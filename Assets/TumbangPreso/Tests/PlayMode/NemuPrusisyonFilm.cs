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
    /// FILM, not a gate (`docs/HERO_KIT_METHOD.md` section 7): Nemu's PRUSISYON in a real Hero Strike match on Bayan Plaza at
    /// true game speed (30 frames per game second, the phase on the film clock). Views: her screen (the cutscene, then her own
    /// first person), a possessed player from behind, that player's OWN screen (hands, ink, grey), and the court. The bots
    /// play after the hand-back, so the contest is shown against real goal-seeking input. Runs only with TUMP_NEMU_FILM=1;
    /// frames under TUMP_EVIDENCE (or Logs)/nemu-prusisyon, with timing.csv and cues.csv for `tools/stitch_ability_film.py`.
    /// </summary>
    [Category("Review")]
    public sealed class NemuPrusisyonFilm
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_FILM") != "1") yield break;
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

        [UnityTest, Timeout(1200000)]
        public IEnumerator FilmPrusisyonInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_FILM") != "1") Assert.Ignore("Film only: set TUMP_NEMU_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            // Nemu, attacking, 5 m short of the can; three others round the court ahead of her.
            var nemu = round.PlayerAt(GameLaunch.SoloSeat);
            nemu.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "nemu");
            nemu.AbilitySystem.BindHero("nemu");
            var art = RosterBook.Load().FindPersonArt("nemu");
            nemu.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(nemu);
            var origin = can + new Vector3(0f, .12f, -5.0f);
            nemu.Teleport(origin); nemu.transform.rotation = Quaternion.identity;
            nemu.Intent.Clear(); nemu.Intent.Parked = false; nemu.IsBot = false;
            var others = round.Players.Where(p => p != null && p != nemu).ToArray();
            var spots = new[] { new Vector3(-3.2f, 0f, 3.8f), new Vector3(2.8f, 0f, 4.6f), new Vector3(.6f, 0f, 7.2f) };
            for (int i = 0; i < others.Length && i < spots.Length; i++)
            {
                others[i].Teleport(origin + spots[i]);
                others[i].Intent.Clear(); others[i].Intent.Parked = false;
                var d = Flat(origin - others[i].transform.position);
                if (d.sqrMagnitude > .01f) others[i].transform.rotation = Quaternion.LookRotation(d.normalized);
            }
            var victim = others.FirstOrDefault(p => !p.IsDefender) ?? others[0];
            nemu.AbilitySystem.Kit.AddUltimateCharge(100);
            Camera.main.GetComponent<CameraRig>().Follow(nemu);
            yield return null;

            var wide = Film.Make("NemuWide", 50);
            var chase = Film.Make("NemuChase", 58);
            var inside = Film.Make("NemuVictimEyes", 70);
            var vignette = new Material(Shader.Find("TumbangPreso/PossessedVignette"));
            var timing = new StringBuilder().AppendLine("frame,seconds,phase,overlay,possessed,victim_left,victim_grip,round_clock");
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            int sceneFirst = -1, sceneLast = -1; float clockIn = -1, clockOut = -1;
            bool brainsOn = false;
            var lastDir = Vector2.up;
            string root = null;
            try
            {
                using (var film = new Film("nemu-prusisyon", "owner", "victim", "victimscreen", "wide"))
                {
                    root = film.Root;
                    // Iteration rounds may film fewer frames and only her screen (TUMP_NEMU_FILM_FRAMES, TUMP_NEMU_FILM_OWNER_ONLY=1);
                    // the preview film is the default, every view for 17 s.
                    int frames = int.TryParse(Environment.GetEnvironmentVariable("TUMP_NEMU_FILM_FRAMES"), out int asked) && asked > 0 ? asked : 30 * 17;
                    bool ownerOnly = Environment.GetEnvironmentVariable("TUMP_NEMU_FILM_OWNER_ONLY") == "1";
                    for (int f = 0; f < frames; f++)
                    {
                        frame = f; film.Frame = f;
                        float t = f / 30f;
                        nemu.Intent.Set(Verb.Ultimate, t > .6f && t < .8f);
                        yield return null;
                        var phase = SharedUltimatePhase.Instance;
                        bool inPhase = phase != null && phase.Active;
                        var overlay = UltimateOverlay();
                        if (overlay != null)
                        {
                            if (sceneFirst < 0) { sceneFirst = f; clockIn = round.TimeLeft; film.Cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_nemu,1,0.2")); }
                            sceneLast = f; clockOut = round.TimeLeft;
                        }
                        // After the hand-back the bots play: the ghost contests their real steering.
                        if (!brainsOn && sceneLast >= 0 && overlay == null && !inPhase)
                        {
                            brainsOn = true;
                            foreach (var p in others) { var brain = p.GetComponent<AIController>(); if (brain != null) brain.enabled = true; }
                        }
                        timing.AppendLine(FormattableString.Invariant(
                            $"{f},{t:F3},{inPhase},{overlay != null},{victim.IsPossessed},{victim.PossessedLeft:F2},{victim.PossessionGrip:F3},{round.TimeLeft:F3}"));

                        // HER SCREEN: the cutscene, then her own first person.
                        if (overlay != null) film.Save(overlay.texture, "owner");
                        else { film.Render(Camera.main); film.Save(film.Target, "owner"); }

                        if (ownerOnly) continue;
                        // Follow a player who is still possessed (a Geo shield's cleanse is real counterplay and ends it early).
                        if (!victim.IsPossessed) { var still = others.FirstOrDefault(p => p.IsPossessed); if (still != null) victim = still; }
                        // A POSSESSED PLAYER, from behind and above.
                        var v = victim.transform.position;
                        var back = Flat(victim.transform.forward); back = back.sqrMagnitude > .01f ? back.normalized : Vector3.forward;
                        chase.transform.position = v - back * 3.2f + Vector3.Cross(Vector3.up, back) * 1.2f + Vector3.up * 2.4f;
                        chase.transform.LookAt(v + Vector3.up * 1.0f + back * 1.5f);
                        film.Render(chase); film.Save(film.Target, "victim");

                        // THAT PLAYER'S OWN SCREEN: their eyes, their grey, their hands.
                        var presence = victim.GetComponentInChildren<PossessedPresence>(true);
                        var hidden = presence != null ? presence.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.name != "Footprint0" && !r.name.StartsWith("Footprint") && !r.transform.parent.name.StartsWith("Moth")).ToArray() : new Renderer[0];
                        var body = victim.GetComponent<CharacterVisual>()?.Model?.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray() ?? new Renderer[0];
                        foreach (var r in hidden) r.enabled = false;
                        foreach (var r in body) r.enabled = false;
                        inside.transform.position = v + Vector3.up * 1.35f + back * .2f;
                        inside.transform.rotation = Quaternion.LookRotation(back + Vector3.down * .12f, Vector3.up);
                        float elapsed = victim.IsPossessed ? victim.PossessionElapsed : NemuRules.PossessedSeconds;
                        float weight = victim.IsPossessed ? Mathf.Clamp01(elapsed / NemuRules.SettleSeconds) * Mathf.Clamp01((NemuRules.PossessedSeconds - elapsed) / NemuRules.LeaveSeconds) : 0f;
                        inside.GetComponent<ColourGrade>().SetEventGrade(Mathf.Lerp(1f, PossessedPerception.Brightness, weight), Mathf.Lerp(1f, PossessedPerception.Saturation, weight));
                        film.Render(inside);
                        foreach (var r in hidden) r.enabled = true;
                        foreach (var r in body) r.enabled = true;
                        if (weight > 0f)
                        {
                            if (victim.PossessionPull(out var pull))
                            {
                                var right = Flat(inside.transform.right).normalized; var ahead = Flat(inside.transform.forward).normalized;
                                var seen = new Vector2(Vector3.Dot(pull, right), Vector3.Dot(pull, ahead));
                                if (seen.sqrMagnitude > 1e-5f) lastDir = seen.normalized;
                            }
                            var dir = lastDir;
                            vignette.SetVector("_PullDir", new Vector4(dir.x, dir.y, 0f, 0f));
                            vignette.SetFloat("_Presence", weight);
                            vignette.SetFloat("_Tell", NemuRules.TellAmount(elapsed, victim.PlayerSlot));
                            vignette.SetFloat("_Surge", NemuRules.SurgeEnvelope(elapsed, victim.PlayerSlot));
                            vignette.SetFloat("_Grip", victim.PossessionGrip);
                            vignette.SetFloat("_Aspect", 16f / 9f);
                            Graphics.Blit(null, film.Target, vignette);
                        }
                        film.Save(film.Target, "victimscreen");

                        // THE COURT.
                        wide.transform.position = origin + new Vector3(8.5f, 7.5f, -2.5f);
                        wide.transform.LookAt(origin + new Vector3(0f, .6f, 4.0f));
                        film.Render(wide); film.Save(film.Target, "wide");
                    }
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                nemu.Intent.Clear();
                Object.Destroy(wide.gameObject); Object.Destroy(chase.gameObject); Object.Destroy(inside.gameObject); Object.Destroy(vignette);
                if (root != null) File.WriteAllText(Path.Combine(root, "timing.csv"), timing.ToString());
            }
            string summary = FormattableString.Invariant($"scene frames {sceneFirst}-{sceneLast} ({(sceneLast - sceneFirst + 1) / 30f:F2} s); clock {clockIn:F3} to {clockOut:F3}; victim seat {victim.PlayerSlot}");
            if (root != null) File.WriteAllText(Path.Combine(root, "summary.txt"), summary + Environment.NewLine);
            Debug.Log("[NemuFilm] " + summary);
            Assert.GreaterOrEqual(sceneFirst, 0, "The cutscene never came up.");
            Assert.AreEqual(clockIn, clockOut, 1e-4f, "The round clock ran under the cutscene.");
            // The owner's limit for her cutscene is 5 to 7 s (2026-10-14).
            Assert.Less((sceneLast - sceneFirst + 1) / 30f, 7.0f + .2f, "The cutscene ran long.");
        }
    }
}
