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
    /// ⚠️⚠️ RAGO'S WHOLE KIT AS ONE VIDEO (owner, 2026-10-24: *"show me video of all his skills"*). `PaeteKitPlayProbe`'s ultimate
    /// film is the template (`HERO_KIT_METHOD.md` section 7): a real Hero Strike round on the Eskinita, his court, every ability
    /// pressed through `InputIntent` as a player presses it and resolved by the host, at a fixed 30 fps game clock, with every
    /// world cue logged for `tools/stitch_ability_film.py`. Three views of the one take:
    ///   owner/   HIS screen (seat 1, a human in his own body), the MAYON AWAKENS cutscene included;
    ///   wide/    one hand-placed shot per skill, from behind the caster and to one side (the clips are cut from it);
    ///   victim/  over the shoulder of whoever the current skill is aimed at, so the hit is seen as it is felt.
    /// The take, in order: LAVA RAM into a body; MOLTEN THROW at the can and the taya refused while it is
    /// hot; MAGMA FIST from the taya's seat (bound to Sean for that beat); MAYON AWAKENS; a shove bouncing off Molten; the
    /// VOLCANIC BOMB on the can. If the thrown slipper misses the can, the can is knocked over and blasted on the host's own
    /// path (`Lata.HostMoltenHit`) so the hot can is still filmed, and `staged.txt` says so.
    /// Runs only with TUMP_SEAN_FILM=1; frames under TUMP_EVIDENCE or Logs, in `sean-kit-film/`.
    /// </summary>
    public sealed class SeanKitFilmProbe
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
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

        /// <summary>Binds Sean's kit AND his body to a seat, the way `PaeteKitPlayProbe.Paete` does (a re-bound kit alone films
        /// somebody else's body casting his skills).</summary>
        private static CharacterMotor DressAsSean(CharacterMotor who)
        {
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "sean");
            who.AbilitySystem.BindHero("sean");
            var art = RosterBook.Load().FindPersonArt("sean");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (who.PlayerSlot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(who);
            return who;
        }

        private static void Place(CharacterMotor who, Vector3 at, Vector3 facing)
        {
            who.Teleport(at);
            var d = facing - at; d.y = 0;
            if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
            who.Intent.Clear(); who.Intent.Parked = false; who.Intent.AimPoint = facing + Vector3.up;
            who.ClearStun(); who.ClearTrip();
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        [UnityTest, Timeout(900000)]
        public IEnumerator FilmAllHisSkills()
        {
            string mode = Environment.GetEnvironmentVariable("TUMP_SEAN_FILM");
            if (mode != "1" && mode != "ram" && mode != "throw" && mode != "fist") Assert.Ignore("Film only: set TUMP_SEAN_FILM=1 (the whole kit), ram, throw or fist.");
            bool ramOnly = mode == "ram";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "sean-kit-film");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            foreach (string view in new[] { "owner", "wide", "victim" }) Directory.CreateDirectory(Path.Combine(root, view));

            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            Vector3 lane = Vector3.back;   // the run up to the can, along the lane
            Vector3 At(float along, float across = 0f, float up = .12f) => can + lane * along + Vector3.right * across + Vector3.up * up;

            var sean = DressAsSean(round.PlayerAt(GameLaunch.SoloSeat));
            sean.IsBot = false;
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            var taya = round.Players.First(p => p.IsDefender);
            var a = round.Players.First(p => p != sean && !p.IsDefender);
            var b = round.Players.First(p => p != sean && p != a && !p.IsDefender);
            foreach (var p in round.Players) { p.Intent.Clear(); p.Intent.Parked = true; }
            Place(taya, At(-3f, 3f), can);
            Place(b, At(3f, 4f), can);

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("SeanFilmWide", 50);
            var victimCam = Make("SeanFilmVictim", 60);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Save(Texture source, string view, int index)
            {
                Graphics.Blit(source, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(90));
            }
            void Shoot(Camera c, string view, int index) { PaeteKitPlayProbe.RenderFilmView(c, hdr); Save(hdr, view, index); }

            var notes = new StringBuilder();
            var staged = new StringBuilder();
            int frame = 0;
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            bool sawScene = false, canDownOnThrow = false, bombKnocked = false;
            float moltenAt = -1f, shoveAt = -1f, bombAt = -1f;
            CharacterMotor watched = a;
            Vector3 focus = At(5f);
            try
            {
                float End = ramOnly ? 3.4f : mode == "throw" ? 13f : mode == "fist" ? 17.5f : 44f;
                int frames = (int)(30 * End);
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    bool Beat(float at) => f == (int)Math.Round(at * 30);

                    // ---- LAVA RAM into a body (0 to 3.5) ----
                    if (Beat(0f)) { Place(sean, At(10.5f), can); Place(a, At(4.2f), At(10.5f)); watched = a; focus = At(6f); }   // 6.3 m: the long charge, then the hit
                    if (t >= .4f && t < .8f) sean.Intent.Set(Verb.Skill1, true);
                    if (Beat(.8f)) sean.Intent.Set(Verb.Skill1, false);

                    // (3.5 to 6.5: nothing. The owner, 2026-10-24: no empty ram and no staged wall in the film; the wall daze is
                    // `SeanKitPlayProbe`'s to prove.)

                    // ---- MOLTEN THROW at the can, then the taya at a hot can (6.5 to 13) ----
                    // From 8.5 m: a throw is refused inside the 7 m box round the can (`ThrowRules.CanThrow`), and from 6 m it never left his hand.
                    // The body the ram threw still stood in the lane, and the coated slipper burst on it 5 m out: off to one side.
                    if (Beat(6.5f)) { Place(a, At(4f, 3.2f), can); Place(sean, At(8.5f), can); watched = taya; focus = can; Place(taya, At(-2.5f, 2.5f), can); }
                    if (t >= 6.8f && t < 7.0f) sean.Intent.Set(Verb.Skill2, true);
                    if (Beat(7.0f)) sean.Intent.Set(Verb.Skill2, false);
                    if (t >= 7.5f && t < 9.05f) { sean.Intent.AimPoint = lata.transform.position + Vector3.up * .3f; sean.Intent.Set(Verb.SpecialAbility, true); }
                    if (Beat(9.05f)) sean.Intent.Set(Verb.SpecialAbility, false);   // a full charge (`Balance.ChargeFullTime`): 0.6 s fell 5 m short
                    if (Beat(9.9f))
                    {
                        canDownOnThrow = !lata.IsUpright;
                        if (lata.IsUpright)
                        {
                            var shoes = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
                            var near = shoes.OrderBy(x => Flat(x.transform.position - lata.transform.position).magnitude).FirstOrDefault();
                            staged.AppendLine(FormattableString.Invariant($"9.9 s: nearest slipper {(near != null ? Flat(near.transform.position - lata.transform.position).magnitude : -1):F2} m from the can, at {(near != null ? near.transform.position : Vector3.zero)}; can at {lata.transform.position}; Rago at {sean.transform.position}"));
                            lata.HostKnockDown(sean.PlayerSlot); lata.HostMoltenHit(-lane);
                            staged.AppendLine("9.9 s: the thrown slipper missed the can; it was knocked over and blasted on the host's path (Lata.HostMoltenHit) so the hot can is filmed.");
                        }
                    }
                    if (Beat(10.1f)) Place(taya, Flat(lata.transform.position) + Vector3.right * .7f + Vector3.up * .12f, lata.transform.position);
                    if (t >= 10.3f && t < 12.6f) taya.Intent.Set(Verb.Grab, true);   // refused while hot, then the reset
                    if (Beat(12.6f)) taya.Intent.Set(Verb.Grab, false);

                    // ---- MAGMA FIST from the taya's seat (13 to 17.5) ----
                    if (Beat(13f))
                    {
                        DressAsSean(taya);
                        Place(sean, At(6f, -3.5f), can);
                        Place(taya, At(1.2f), At(6f)); Place(b, At(5.6f, .4f), can);
                        watched = b; focus = At(4f);
                    }
                    if (t >= 13.3f && t < 13.9f) { taya.Intent.AimPoint = b.transform.position + Vector3.up; taya.Intent.Set(Verb.Skill2, true); }   // the prayer, placed on them
                    if (Beat(13.9f)) taya.Intent.Set(Verb.Skill2, false);

                    // ---- MAYON AWAKENS, Molten, a shove that does not move him, the VOLCANIC BOMB (17.5 to 44) ----
                    if (Beat(17.5f))
                    {
                        if (!lata.IsUpright) staged.AppendLine("17.5 s: the can was still down; the ultimate's beat started anyway.");
                        Place(taya, At(-3f, 3f), can); taya.Intent.Parked = true;
                        Place(sean, At(4.2f), can);
                        Place(a, At(4.2f, 1.6f), can); Place(b, At(3.0f, -1.4f), can);
                        kit.AddUltimateCharge(100);
                        watched = a; focus = At(4f);
                    }
                    if (t >= 17.8f && t < 18.05f) sean.Intent.Set(Verb.Ultimate, true);
                    if (Beat(18.05f)) sean.Intent.Set(Verb.Ultimate, false);
                    // One second into Molten (after the hand-back): a shove that cannot move him.
                    if (kit.IsMolten && moltenAt < 0) moltenAt = t;
                    if (moltenAt >= 0 && shoveAt < 0 && t >= moltenAt + 1f)
                    {
                        shoveAt = t;
                        Place(b, sean.transform.position + lane * 1.1f, sean.transform.position);
                        b.Intent.Set(Verb.Lunge, true); watched = b;
                        notes.AppendLine(FormattableString.Invariant($"{t:F2} s: Molten, then the shove on him"));
                    }
                    if (shoveAt >= 0 && t >= shoveAt + .2f) b.Intent.Set(Verb.Lunge, false);
                    // Two seconds after the shove: the VOLCANIC BOMB on E, aimed at the can.
                    if (shoveAt >= 0 && bombAt < 0 && t >= shoveAt + 2f)
                    {
                        bombAt = t;
                        sean.Intent.AimPoint = lata.transform.position; sean.Intent.Set(Verb.Skill2, true);
                        watched = a; focus = can;
                        notes.AppendLine(FormattableString.Invariant($"{t:F2} s: the bomb"));
                    }
                    if (bombAt >= 0 && t >= bombAt + .15f) sean.Intent.Set(Verb.Skill2, false);
                    bombKnocked |= bombAt >= 0 && !lata.IsUpright;
                    // Four seconds after the bomb is enough to see it land and the Molten end; the first film held 12 s of nothing.
                    if (bombAt >= 0 && t >= bombAt + 4f) break;

                    yield return null;

                    // HIS screen: the cutscene's own picture while it is up, his live camera otherwise.
                    RawImage scene = null;
                    foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                        if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) scene = image;
                    if (scene != null)
                    {
                        if (!sawScene) cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_sean,1,0.2"));
                        sawScene = true; Save(scene.texture, "owner", f);
                    }
                    else if (Camera.main != null) Shoot(Camera.main, "owner", f);

                    // ⚠️ ONE HAND-PLACED SHOT PER SKILL (owner, 2026-10-24: "separate clips", "di ung iba iba pa angle"). From behind
                    // the caster, off to one side and above, so his body never hides who he hits; under the alley's wires.
                    // `tools/stitch_ability_film.py` films this view and the clips are cut from it at the beats below.
                    (Vector3 eye, Vector3 look) shot =
                        t < 6.5f ? (At(12.5f, 2.3f, 2.6f), At(5.5f, 0f, .7f))     // LAVA RAM
                        : t < 13f ? (At(11f, 2.4f, 2.8f), At(2f, 0f, .4f))       // MOLTEN THROW and the hot can
                        : t < 17.5f ? (At(3.4f, 2.9f, 2.0f), At(3.4f, 0f, .7f))   // MAGMA FIST, side-on: his prayer at one side, the puddle at the other
                        : (At(11f, 3.4f, 3.6f), At(2f, 0f, .5f));                // the blast, Molten, the VOLCANIC BOMB
                    wide.transform.position = shot.eye; wide.transform.LookAt(shot.look);
                    var who = watched != null ? watched : a;
                    var toFocus = Flat(focus - who.transform.position);
                    toFocus = toFocus.sqrMagnitude > .01f ? toFocus.normalized : Vector3.forward;
                    var side = Vector3.Cross(Vector3.up, toFocus);
                    victimCam.transform.position = who.transform.position - toFocus * 2.6f + side * 1.3f + Vector3.up * 1.7f;
                    victimCam.transform.LookAt(who.transform.position + toFocus * 2.0f + Vector3.up * .9f);
                    Shoot(wide, "wide", f);
                    Shoot(victimCam, "victim", f);
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                File.WriteAllText(Path.Combine(root, "notes.txt"), notes.ToString());
                File.WriteAllText(Path.Combine(root, "staged.txt"), staged.Length > 0 ? staged.ToString() : "nothing staged: every beat came from the pressed input\n");
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(victimCam.gameObject);
                hdr.Release(); ldr.Release();
            }
            Debug.Log("[SeanKitFilmProbe] can down on the throw: " + canDownOnThrow + "; cutscene seen: " + sawScene + "; bomb knocked the can: " + bombKnocked);
            if (mode == "1") Assert.IsTrue(sawScene, "The MAYON AWAKENS cutscene never came up on his screen.");
        }
    }
}
