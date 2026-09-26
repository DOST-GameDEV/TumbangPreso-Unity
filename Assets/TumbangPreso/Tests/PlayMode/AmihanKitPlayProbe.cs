using System;
using System.Collections;
using System.IO;
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
    /// ⚠️⚠️ AMIHAN, FILMED IN A MATCH (HERO-8, owner 2026-09-26: *"thoroughly make sure amihan's animations look great a bug i
    /// found last time was her yellow circle looked weird as fuck when she was floating"*, *"she didnt have a flaot animation too
    /// and any VFX to indicate her shit as well"*). `PaeteKitPlayProbe`'s films are the template (`HERO_KIT_METHOD.md` section 7):
    /// a real Hero Strike match on Bayan Plaza, every ability pressed through `InputIntent` as a player presses it, host-resolved,
    /// filmed on her screen, from the court and over the shoulder of whoever the wind hits, at a fixed 30 fps game clock, with
    /// every world cue logged for `tools/stitch_ability_film.py`. Runs only with TUMP_AMIHAN_FILM=1; frames under TUMP_EVIDENCE.
    /// </summary>
    public sealed class AmihanKitPlayProbe
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
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        private static void Face(CharacterMotor who, Vector3 toward)
        {
            var d = toward - who.transform.position; d.y = 0f;
            if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
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
