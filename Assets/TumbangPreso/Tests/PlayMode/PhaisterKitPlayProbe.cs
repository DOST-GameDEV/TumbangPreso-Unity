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
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER, PLAYED AND FILMED (HERO-10): a real Hero Strike match on Bayan Plaza, her skills pressed through
    /// `InputIntent` as a player presses them, host-resolved as in a match, filmed at 30 fps (`Time.captureFramerate`) from her
    /// own screen and a court camera, every world cue logged for the mp4 (`tools/stitch_ability_film.py`). The method is
    /// `docs/HERO_KIT_METHOD.md` section 7: film it in a match and send the video; a green test is not a verdict.
    /// Runs only with TUMP_PHAISTER_FILM=1; frames under TUMP_EVIDENCE (default Logs).
    /// </summary>
    public sealed class PhaisterKitPlayProbe
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
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
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        /// <summary>Her body AND her kit on a seat (the Paete lesson: re-binding only the kit filmed a human casting her skills).</summary>
        private static CharacterMotor Phaister(int slot, Vector3 at)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "phaister");
            who.AbilitySystem.BindHero("phaister");
            var art = RosterBook.Load().FindPersonArt("phaister");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (slot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(who);
            who.Teleport(at); who.transform.rotation = Quaternion.identity;
            who.Intent.Parked = false; who.IsBot = slot != GameLaunch.SoloSeat;
            return who;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        /// <summary>
        /// ⚠️ HER THREE SKILLS IN ONE MATCH, IN THE ORDER OF THE PLAN (v8 timings: every hold is long enough to judge its tell and
        /// its aim picture, film v7's were 0.6 s):
        ///   0.4 to 1.5   VANISHING ACT: she holds to aim 5 m down a clear lane (her sigil, the three moths, her tell) and lets go;
        ///                the barang swarm carries her.
        ///   2.4 to 3.4   MANIKA MISCHIEF: she holds the doll up and pricks it, aimed at an attacker 4.5 m away, and lets go; it steals
        ///                their look, flies back to her left hand (her screen shows it in her first-person hand) and she holds it.
        ///   8.0          SPOTLIGHT PIN: a second Phaister, the taya, stabs her pin at two attackers in front of her; the moonlight
        ///                drops on each and follows them.
        ///   10.6 to 11.2 THE MISS: a third Phaister throws a doll at empty court; it lands, sits up, looks round and crumbles.
        /// Views: `owner/` is her own screen (the local seat) and, for the taya's pin and the miss, a camera over the caster's right
        /// shoulder; `wide/` is a court camera placed per skill.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHerSkillsInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-skills-film-" + tag);
            foreach (string view in new[] { "owner", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            int me = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            int victimSeat = -1, fourth = -1;
            foreach (var p in round.Players)
                if (p.PlayerSlot != taya && p.PlayerSlot != me) { if (victimSeat < 0) victimSeat = p.PlayerSlot; else fourth = p.PlayerSlot; }

            Vector3 start = can + new Vector3(0f, .12f, -12f);
            var her = Phaister(me, start);
            var witch = Phaister(taya, can + new Vector3(-2f, .12f, -1f));
            var victim = round.PlayerAt(victimSeat);
            victim.Teleport(can + new Vector3(1.0f, .12f, -2.5f)); victim.Intent.Parked = true;
            var other = fourth >= 0 ? Phaister(fourth, can + new Vector3(6f, .12f, 4f)) : null;
            if (other != null) other.Intent.Parked = true;
            void Face(CharacterMotor who, Vector3 toward)
            {
                var d = toward - who.transform.position; d.y = 0f;
                if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
            }

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("PhaisterWide", 50);
            var shoulder = Make("PhaisterShoulder", 60);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Shoot(Camera c, string view, int index)
            {
                PaeteKitPlayProbe.RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            bool moved = false, cursed = false, exposed = false, missed = false;
            Vector3 missFrom = can + new Vector3(3.5f, .12f, 4.5f);
            // Over a caster's right shoulder, far enough back to see her whole arm and what it does in front of her (film v7 put the
            // lens 3 m behind her head at head height, and her back filled the frame).
            // v9b: a front three-quarter on the caster (her face, the pin coming out of her hat, the stab, the doll in her hand): the
            // over-the-shoulder views before it were her hat filling the frame.
            void OverShoulder(CharacterMotor who)
            {
                var fwd = who.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                var right = Vector3.Cross(Vector3.up, fwd);
                shoulder.transform.position = who.transform.position + fwd * 3.0f + right * 2.0f + Vector3.up * 1.55f;
                shoulder.transform.LookAt(who.transform.position + Vector3.up * 1.05f + fwd * 0.8f);
            }
            try
            {
                const int frames = (int)(30 * 14.0f);
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // VANISHING ACT, down the lane.
                    if (t < 2.2f) { her.Intent.AimPoint = start + new Vector3(0f, 0f, 5f); Face(her, start + new Vector3(0f, 0f, 8f)); }
                    her.Intent.Set(Verb.Skill1, t > .4f && t < 1.5f);
                    // MANIKA MISCHIEF, at the victim, who is put straight ahead of her own view (v9: the body turned back to where her
                    // camera looked after each `Face`, so the doll left 90 degrees off the victim on the court camera).
                    if (f == 66)
                    {
                        var ahead = her.transform.forward; ahead.y = 0f; ahead = ahead.sqrMagnitude > .01f ? ahead.normalized : Vector3.forward;
                        victim.Teleport(her.transform.position + ahead * 4.6f);
                    }
                    if (t >= 2.2f && t < 7.0f) { her.Intent.AimPoint = victim.transform.position; Face(her, victim.transform.position); }
                    her.Intent.Set(Verb.Skill2, t > 2.4f && t < 3.4f);
                    // SPOTLIGHT PIN: the attackers are placed in front of the taya, and she stabs.
                    if (f == 210)
                    {
                        her.Teleport(witch.transform.position + new Vector3(-0.8f, 0f, -4.2f));
                        victim.Teleport(witch.transform.position + new Vector3(1.1f, 0f, -3.6f));
                    }
                    if (t >= 7.0f && t < 10.5f) { Face(witch, witch.transform.position + Vector3.back); Face(her, witch.transform.position); witch.Intent.AimPoint = witch.transform.position + Vector3.back * 4f; }
                    witch.Intent.Set(Verb.Skill2, t > 8.0f && t < 8.2f);
                    // THE MISS: the third Phaister throws at empty court.
                    if (other != null)
                    {
                        if (f == 315) { other.Teleport(missFrom); other.Intent.Parked = false; }
                        if (t >= 10.5f) { Face(other, missFrom + Vector3.right * 6f); other.Intent.AimPoint = missFrom + Vector3.right * 5.0f; }
                        other.Intent.Set(Verb.Skill2, t > 10.6f && t < 11.2f);
                    }
                    yield return null;
                    moved |= Vector3.Distance(Flat(her.transform.position), Flat(start)) > 3f;
                    cursed |= victim.IsDisoriented;
                    exposed |= her.IsVulnerable || victim.IsVulnerable;
                    missed |= t > 11.2f && Object.FindFirstObjectByType<PhaisterManika>() != null;

                    // Cameras per skill.
                    if (t < 2.2f) { wide.transform.position = start + new Vector3(5.5f, 2.2f, 2.5f); wide.transform.LookAt(start + new Vector3(0f, 0.9f, 2.5f)); }
                    else if (t < 3.9f)
                    {
                        // v8b: her tell and the throw from in front of her and to her right (v8 watched her back, where the doll in
                        // her hand and the pin going in could not be seen).
                        var toVictim = victim.transform.position - her.transform.position; toVictim.y = 0f; toVictim.Normalize();
                        var side = Vector3.Cross(Vector3.up, toVictim);
                        wide.transform.position = her.transform.position + toVictim * 2.6f + side * 1.9f + Vector3.up * 1.5f;
                        wide.transform.LookAt(her.transform.position + Vector3.up * 1.05f + toVictim * 0.6f);
                    }
                    else if (t < 7.0f)
                    {
                        Vector3 mid = (her.transform.position + victim.transform.position) * 0.5f;
                        wide.transform.position = mid + new Vector3(5.0f, 2.6f, -1.5f); wide.transform.LookAt(mid + Vector3.up * 0.9f);
                    }
                    else if (t < 10.5f) { wide.transform.position = witch.transform.position + new Vector3(4.5f, 3.4f, -5.5f); wide.transform.LookAt(witch.transform.position + new Vector3(0f, 0.8f, -2.5f)); }
                    else
                    {
                        // The miss: close on where the doll comes down (about 5 m out), low, so it can be seen sitting up and looking round.
                        var land = missFrom + Vector3.right * 4.8f;
                        wide.transform.position = land + new Vector3(0.4f, 1.1f, -2.4f); wide.transform.LookAt(land + Vector3.up * 0.25f + Vector3.left * 0.6f);
                    }
                    if (t < 7.0f && Camera.main != null) Shoot(Camera.main, "owner", f);
                    else
                    {
                        OverShoulder(t < 10.5f || other == null ? witch : other);
                        Shoot(shoulder, "owner", f);
                    }
                    Shoot(wide, "wide", f);
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(shoulder.gameObject);
                hdr.Release(); ldr.Release();
            }
            File.WriteAllText(Path.Combine(root, "claims.csv"), FormattableString.Invariant($"moved,{moved}\ncursed,{cursed}\nexposed,{exposed}\nmissed_doll_seen,{missed}\n"));
            Assert.IsTrue(moved, "VANISHING ACT did not carry her.");
            Assert.IsTrue(cursed, "MANIKA MISCHIEF never Disoriented its target.");
            Assert.IsTrue(exposed, "SPOTLIGHT PIN left nobody Vulnerable.");
        }

        /// <summary>
        /// ⚠️ HER CURSES ON A VICTIM'S OWN SCREEN (HERO-10, v8; the owner's third view, "A CAUGHT PLAYER"). The local seat is the
        /// victim and a bot Phaister does it to them, so `Camera.main` IS their screen:
        ///   0.3 to 1.3  she aims MANIKA MISCHIEF at them and lets go; the doll takes their look; for 4 s they hallucinate and a doll of
        ///               themselves peeks in at the edge of their frame (plan 4.2 row 8).
        ///   6.0 to 6.8  she aims OMEN beside them, 2.6 m up, and lets go: the cutscene on their screen, then the eye's cast (a black
        ///               butterfly marks them), the pull (their frame veiled and drawn toward the eye, butterflies at its edges), the end.
        /// Views: `victim/` their screen (the cutscene overlay copied in while it is up), `wide/` the court. Runs only with TUMP_PHAISTER_FILM=1.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHerCursesOnAVictimsScreen()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-victim-film-" + tag);
            foreach (string view in new[] { "victim", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var me = round.PlayerAt(GameLaunch.SoloSeat);
            int witchSeat = -1;
            foreach (var p in round.Players) if (p != me && !p.IsDefender) { witchSeat = p.PlayerSlot; break; }
            Assert.GreaterOrEqual(witchSeat, 0);
            var mine = new Vector3(0f, .12f, -6f);
            me.Teleport(mine); me.transform.rotation = Quaternion.identity; me.Intent.Parked = true;
            var her = Phaister(witchSeat, mine + new Vector3(0.6f, 0f, 5.2f));
            her.transform.rotation = Quaternion.LookRotation(Vector3.back);
            her.AbilitySystem.Kit.AddUltimateCharge(100);
            // The victim looks at her, so the doll comes at their face.
            void LookAtHer() { var d = her.transform.position - me.transform.position; d.y = 0f; if (d.sqrMagnitude > .01f) me.transform.rotation = Quaternion.LookRotation(d.normalized); }
            LookAtHer();
            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("VictimWide", 52);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Save(Texture source, string view, int index)
            {
                Graphics.Blit(source, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            void Shoot(Camera c, string view, int index) { PaeteKitPlayProbe.RenderFilmView(c, hdr); Save(hdr, view, index); }
            bool cursed = false, sawScene = false, pulled = false;
            var eyeAt = mine + new Vector3(2.4f, 0f, 1.6f);
            var aim = eyeAt + Vector3.up * 2.6f;
            float startDistance = Vector3.Distance(Flat(me.transform.position), Flat(eyeAt));
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            try
            {
                const int frames = 30 * 20;
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    if (t < 6.0f) { her.Intent.AimPoint = me.transform.position; var d = me.transform.position - her.transform.position; d.y = 0f; her.transform.rotation = Quaternion.LookRotation(d.normalized); }
                    her.Intent.Set(Verb.Skill2, t > .3f && t < 1.3f);
                    if (t >= 6.0f) her.Intent.AimPoint = aim;
                    her.Intent.Set(Verb.Ultimate, t > 6.0f && t < 6.8f);
                    if (t < 6.0f) LookAtHer();
                    yield return null;
                    cursed |= me.IsDisoriented;
                    pulled |= t > 7f && Vector3.Distance(Flat(me.transform.position), Flat(eyeAt)) < startDistance - 1.0f;
                    UnityEngine.UI.RawImage scene = null;
                    foreach (var image in Object.FindObjectsByType<UnityEngine.UI.RawImage>())
                        if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) scene = image;
                    if (scene != null)
                    {
                        if (!sawScene) cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_phaister,1,0.2"));
                        sawScene = true; Save(scene.texture, "victim", f);
                    }
                    else if (Camera.main != null) Shoot(Camera.main, "victim", f);
                    Vector3 mid = (me.transform.position + her.transform.position) * 0.5f;
                    wide.transform.position = mid + new Vector3(7.5f, 4.2f, -2.5f);
                    wide.transform.LookAt(mid + Vector3.up * 1.2f);
                    Shoot(wide, "wide", f);
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject);
                hdr.Release(); ldr.Release();
            }
            File.WriteAllText(Path.Combine(root, "claims.csv"), "cursed," + cursed + Environment.NewLine + "saw_scene," + sawScene + Environment.NewLine + "pulled," + pulled + Environment.NewLine);
            Assert.IsTrue(cursed, "MANIKA MISCHIEF never Disoriented the local player.");
            Assert.IsTrue(sawScene, "The OMEN cutscene never came up on the victim's screen.");
        }

        /// <summary>
        /// ⚠️ OMEN ON HER SCREEN (HERO-10): the cutscene overlay copied in on her own view, then the live eye, filmed with the court
        /// and a caught player, the round clock read as the cutscene comes up, on its last frame and a second after (it must not
        /// run under the cutscene). `SharedUltimatePhase.FilmClock` gives the cutscene the film's frame clock so it lasts its
        /// real length. Runs only with TUMP_PHAISTER_FILM=1.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmOmenOnHerScreen()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-omen-film-" + tag);
            foreach (string view in new[] { "owner", "wide", "victim" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var her = Phaister(GameLaunch.SoloSeat, new Vector3(0, .12f, -11));
            her.AbilitySystem.Kit.AddUltimateCharge(100);
            var centre = new Vector3(0, 0, -4);
            // HERO-10: she hangs the eye in the air (owner: *"put ppl on the air"*); the aim's y is the height.
            var aim = centre + Vector3.up * 3.2f;
            var others = new System.Collections.Generic.List<CharacterMotor>();
            foreach (var p in round.Players) if (p != her) others.Add(p);
            var stands = new[] { new Vector3(4.6f, .12f, -2.4f), new Vector3(-4.2f, .12f, -1.8f), new Vector3(1.4f, .12f, 1.6f) };
            for (int i = 0; i < others.Count && i < stands.Length; i++)
            {
                others[i].Teleport(stands[i]); others[i].Intent.Parked = false;
                others[i].transform.rotation = Quaternion.LookRotation(centre - stands[i]);
            }
            her.Intent.AimPoint = aim;
            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("OmenWide", 52);
            var victimCam = Make("OmenVictim", 62);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Save(Texture source, string view, int index)
            {
                Graphics.Blit(source, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            void Shoot(Camera c, string view, int index) { PaeteKitPlayProbe.RenderFilmView(c, hdr); Save(hdr, view, index); }
            bool sawScene = false, pulled = false;
            float clockAtScene = -1f, clockAtSceneEnd = -1f, clockAfter = -1f; int lastSceneFrame = -1;
            float startDistance = others.Count > 1 ? Vector3.Distance(Flat(others[1].transform.position), Flat(centre)) : 0f;
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            try
            {
                const int frames = 30 * 16;
                var victim = others.Count > 1 ? others[1] : others[0];
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    her.Intent.AimPoint = aim;
                    // v8: held 1.2 s, so her aim picture (the ring, the ghost eye at its height, the line of lights) and her tell show.
                    her.Intent.Set(Verb.Ultimate, t < 1.2f);
                    yield return null;
                    pulled |= Vector3.Distance(Flat(victim.transform.position), Flat(centre)) < startDistance - 1.5f;
                    UnityEngine.UI.RawImage scene = null;
                    foreach (var image in Object.FindObjectsByType<UnityEngine.UI.RawImage>())
                        if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) scene = image;
                    if (scene != null)
                    {
                        // Her theme plays from the introduction's own AudioSource, not as a world cue: log it once for the mp4.
                        if (!sawScene) { clockAtScene = round.TimeLeft; cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_phaister,1,0.2")); }
                        sawScene = true; Save(scene.texture, "owner", f);
                        clockAtSceneEnd = round.TimeLeft; lastSceneFrame = f;
                    }
                    else if (Camera.main != null) Shoot(Camera.main, "owner", f);
                    wide.transform.position = centre + new Vector3(9.5f, 7.0f, 9.0f);
                    wide.transform.LookAt(centre + Vector3.up * 2.0f);
                    var toEye = centre - victim.transform.position; toEye.y = 0;
                    toEye = toEye.sqrMagnitude > .01f ? toEye.normalized : Vector3.back;
                    var side = Vector3.Cross(Vector3.up, toEye);
                    victimCam.transform.position = victim.transform.position + side * 3.4f - toEye * 1.4f + Vector3.up * 1.6f;
                    victimCam.transform.LookAt(victim.transform.position + toEye * 1.0f + Vector3.up * 1.0f);
                    Shoot(wide, "wide", f);
                    Shoot(victimCam, "victim", f);
                    if (lastSceneFrame >= 0 && f == lastSceneFrame + 30) clockAfter = round.TimeLeft;
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(victimCam.gameObject);
                hdr.Release(); ldr.Release();
            }
            string clock = FormattableString.Invariant($"round clock: {clockAtScene:F3} s left as the cutscene came up, {clockAtSceneEnd:F3} on its last frame, {clockAfter:F3} one second after");
            File.WriteAllText(Path.Combine(root, "clock.txt"), clock + Environment.NewLine + "cutscene frames through " + lastSceneFrame + Environment.NewLine);
            File.WriteAllText(Path.Combine(root, "claims.csv"), "saw_scene," + sawScene + Environment.NewLine + "pulled," + pulled + Environment.NewLine);
            Assert.IsTrue(sawScene, "The cutscene never came up on her screen.");
            Assert.IsTrue(pulled, "OMEN never pulled the filmed player in.");
            Assert.Less(Mathf.Abs(clockAtScene - clockAtSceneEnd), 0.05f, "The match clock ran during the cutscene: " + clock);
        }

        // ============================================================================================ v3, THE VOODOO KIT

        /// <summary>Jump a body's voodoo mark to this age (the HEX fuse is 10 s; a film does not wait it out).</summary>
        private static void AgeMark(CharacterMotor body, float age)
            => typeof(CharacterMotor).GetField("_markAge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(body, age);

        private static void FaceTo(CharacterMotor who, Vector3 toward)
        {
            var d = toward - who.transform.position; d.y = 0f;
            if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
        }

        /// <summary>
        /// ⚠️⚠️ HER v3 KIT, FILMED IN A MATCH (HERO-10, plan 9; the owner's table of 2026-09-27), from her screen and the court:
        ///   0.4 to 1.3   TELEPORT down the lane: her sigil and the moths while she aims, then she is there.
        ///   2.3          CURSE: DRAIN at an attacker 5 m ahead: the slipper to her belt, her arm up at their chest, the thread
        ///                whipping out and piercing, 2 s of hold (the cord tightening, its stitches crawling to her), the mark,
        ///                the knot over them, her two-handed wring, DRAINED at 1.5 s.
        ///   6.6          a second Phaister reaches for them and they are carried out of reach at 7.4: the thread frays and snaps.
        ///   8.6          CURSE: HEX by the taya Phaister: her arm high, the doll at her cheek, the thread to their eyes, the violet
        ///                button over them filling (its 10 s fuse jumped for the film at 11.0), armed and throbbing, and her
        ///                recast at 12.4: the stab, HEXED.
        /// Views: `owner/` her screen until 6.5, then over the shoulder of whichever Phaister is casting; `wide/` the court.
        /// Runs only with TUMP_PHAISTER_FILM=1; frames under TUMP_EVIDENCE (default Logs), folder `phaister-voodoo-film-TAG`.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHerVoodooKitInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-voodoo-film-" + tag);
            foreach (string view in new[] { "owner", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            int me = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            int victimSeat = -1, fourth = -1;
            foreach (var p in round.Players)
                if (p.PlayerSlot != taya && p.PlayerSlot != me) { if (victimSeat < 0) victimSeat = p.PlayerSlot; else fourth = p.PlayerSlot; }
            Assert.GreaterOrEqual(fourth, 0, "The film needs four seats.");

            Vector3 start = can + new Vector3(0f, .12f, -12f);
            var her = Phaister(me, start);
            var witch = Phaister(taya, can + new Vector3(-2f, .12f, -1f));
            var other = Phaister(fourth, can + new Vector3(7f, .12f, 5f));
            other.Intent.Parked = true; witch.Intent.Parked = false;
            var victim = round.PlayerAt(victimSeat);
            victim.Teleport(can + new Vector3(6f, .12f, -4f)); victim.Intent.Parked = true;

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("VoodooWide", 50);
            var shoulder = Make("VoodooShoulder", 58);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Shoot(Camera c, string view, int index)
            {
                PaeteKitPlayProbe.RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            // A front three-quarter on a caster: her face, her reaching arm and the thread leaving it.
            void FrontThreeQuarter(CharacterMotor who, Vector3 target)
            {
                var fwd = target - who.transform.position; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                var right = Vector3.Cross(Vector3.up, fwd);
                shoulder.transform.position = who.transform.position + fwd * 2.4f - right * 2.6f + Vector3.up * 1.7f;
                shoulder.transform.LookAt(who.transform.position + Vector3.up * 1.0f + fwd * 1.4f);
            }
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            bool moved = false, reached = false, drained = false, snapped = false, hexed = false, stowed = false;
            try
            {
                const int frames = (int)(30 * 15.0f);
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // TELEPORT, down the lane.
                    if (t < 2.0f) { her.Intent.AimPoint = start + new Vector3(0f, 0f, 5f); FaceTo(her, start + new Vector3(0f, 0f, 8f)); }
                    her.Intent.Set(Verb.Skill1, t > .4f && t < 1.3f);
                    // DRAIN: the victim straight ahead of her own view, 5 m.
                    if (f == 60)
                    {
                        var ahead = her.transform.forward; ahead.y = 0f; ahead = ahead.sqrMagnitude > .01f ? ahead.normalized : Vector3.forward;
                        victim.Teleport(her.transform.position + ahead * 5.0f);
                        FaceTo(victim, her.transform.position);
                    }
                    if (t >= 2.0f && t < 6.5f) FaceTo(her, victim.transform.position);
                    her.Intent.Set(Verb.Skill2, t > 2.3f && t < 2.4f);
                    // THE SNAP: the fourth seat, a Phaister, reaches for the victim alone (v12 staged it with the taya in her cone,
                    // and nearest-her-facing rightly took the taya), then turns her back on them at 7.4: the thread snaps.
                    if (f == 190)
                    {
                        victim.Teleport(can + new Vector3(5f, .12f, -8f));
                        other.Teleport(can + new Vector3(1f, .12f, -10f)); other.Intent.Parked = false;
                    }
                    if (t >= 6.3f && t < 7.4f) FaceTo(other, victim.transform.position);
                    else if (t >= 7.4f && t < 8.2f) FaceTo(other, other.transform.position * 2f - victim.transform.position);
                    other.Intent.Set(Verb.Skill2, t > 6.6f && t < 6.7f);
                    // HEX: the taya Phaister, the victim 4.5 m in front of her.
                    if (f == 246) { victim.Teleport(witch.transform.position + new Vector3(0.6f, 0f, -4.5f)); FaceTo(victim, witch.transform.position); }
                    if (t >= 8.2f) FaceTo(witch, victim.transform.position);
                    witch.Intent.Set(Verb.Skill2, (t > 8.6f && t < 8.7f) || (t > 12.4f && t < 12.5f));
                    if (f == 330 && victim.VoodooMark == VoodooMarkKind.Hex) AgeMark(victim, VoodooRules.HexArmSeconds - 0.6f);
                    yield return null;
                    moved |= Vector3.Distance(Flat(her.transform.position), Flat(start)) > 1.8f;
                    reached |= her.IsVoodooReaching;
                    stowed |= her.StowsCarriedSlipper && her.HoldingSlipper;
                    drained |= victim.IsDrained;
                    snapped |= t > 7.4f && t < 8.2f && !other.IsVoodooReaching && victim.VoodooMarkSource != other.PlayerSlot
                               && !other.VoodooReachSucceeded;
                    hexed |= victim.IsHexed;

                    // Cameras per beat.
                    if (t < 2.0f) { wide.transform.position = start + new Vector3(5.5f, 2.2f, 2.5f); wide.transform.LookAt(start + new Vector3(0f, 0.9f, 2.5f)); }
                    else if (t < 6.5f)
                    {
                        Vector3 mid = (her.transform.position + victim.transform.position) * 0.5f;
                        var line = victim.transform.position - her.transform.position; line.y = 0f; line.Normalize();
                        var side = Vector3.Cross(Vector3.up, line);
                        wide.transform.position = mid + side * 6.0f + Vector3.up * 2.4f - line * 1.0f;
                        wide.transform.LookAt(mid + Vector3.up * 0.9f);
                    }
                    else if (t < 8.2f)
                    {
                        Vector3 mid = (other.transform.position + victim.transform.position) * 0.5f;
                        wide.transform.position = mid + new Vector3(1.5f, 3.2f, -7.5f); wide.transform.LookAt(mid + Vector3.up * 0.9f);
                    }
                    else
                    {
                        Vector3 mid = (witch.transform.position + victim.transform.position) * 0.5f;
                        var line = victim.transform.position - witch.transform.position; line.y = 0f; line.Normalize();
                        var side = Vector3.Cross(Vector3.up, line);
                        wide.transform.position = mid - side * 5.5f + Vector3.up * 2.6f; wide.transform.LookAt(mid + Vector3.up * 1.2f);
                    }
                    if (t < 6.5f && Camera.main != null) Shoot(Camera.main, "owner", f);
                    else
                    {
                        if (t < 8.2f) FrontThreeQuarter(other, victim.transform.position);
                        else FrontThreeQuarter(witch, victim.transform.position);
                        Shoot(shoulder, "owner", f);
                    }
                    Shoot(wide, "wide", f);
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(shoulder.gameObject);
                hdr.Release(); ldr.Release();
            }
            File.WriteAllText(Path.Combine(root, "claims.csv"), FormattableString.Invariant(
                $"moved,{moved}\nreached,{reached}\nslipper_at_belt,{stowed}\ndrained,{drained}\nsnapped,{snapped}\nhexed,{hexed}\n"));
            Assert.IsTrue(moved, "TELEPORT did not carry her.");
            Assert.IsTrue(reached, "DRAIN never started a reach.");
            Assert.IsTrue(drained, "DRAIN never drained its target.");
            Assert.IsTrue(snapped, "The second Phaister's reach never snapped.");
            Assert.IsTrue(hexed, "HEX's recast never hexed its target.");
        }

        /// <summary>
        /// ⚠️ HER CURSES ON THE VICTIM'S OWN SCREEN (HERO-10 v3, the owner's third view). The local seat is the victim:
        ///   0.5          an attacking Phaister reaches for them with DRAIN (the thread coming at them from her hand, 2 s), the
        ///                mark, their stamina arc shaking as she wrings, then DRAINED: the arc pinned, two pins stamped over it.
        ///   5.5          the taya Phaister reaches with HEX (the thread to their eyes), the fuse jumped at 8.0, her recast at 9.0:
        ///                HEXED, phantom slippers on their screen among the real ones (no shadow), for 7.5 s.
        /// Views: `victim/` their screen, `wide/` the court. Runs only with TUMP_PHAISTER_FILM=1.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHerVoodooOnAVictimsScreen()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-voodoo-victim-film-" + tag);
            foreach (string view in new[] { "victim", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            var me = round.PlayerAt(GameLaunch.SoloSeat);
            Assert.IsFalse(me.IsDefender, "The film expects the local seat to attack.");
            int drainerSeat = -1, tayaSeat = -1;
            foreach (var p in round.Players)
            {
                if (p.IsDefender) tayaSeat = p.PlayerSlot;
                else if (p != me && drainerSeat < 0) drainerSeat = p.PlayerSlot;
            }
            var mine = can + new Vector3(1.5f, .12f, -5.5f);
            me.Teleport(mine); me.Intent.Parked = true;
            // v12 left this seat a bot, so the phantom slippers (the local human's alone) never drew.
            me.IsBot = false;
            var drainer = Phaister(drainerSeat, mine + new Vector3(-1.0f, 0f, -5.0f));
            var witch = Phaister(tayaSeat, can + new Vector3(-1.5f, .12f, -1.5f));
            witch.Intent.Parked = false;
            foreach (var p in round.Players)
                if (p != me && p != drainer && p != witch) p.Teleport(can + new Vector3(8f, .12f, 6f));

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("VoodooVictimWide", 52);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            // ⚠️ THE HUD IS IN THE VICTIM'S FRAME (DRAINED's pins live on the stamina arc): overlay canvases are composited after
            // the camera and never reach a camera render, so for the film they are moved onto a UI camera drawn over each frame
            // (`GameplayShots`' way) and put back after.
            // ⚠️ v13 found the HUD's canvases on the Default layer, not UI: the camera draws whatever layers they are on, from far
            // below the court so nothing of the world is inside its 10 m.
            var uiCam = new GameObject("VoodooVictimUi").AddComponent<Camera>();
            uiCam.transform.position = new Vector3(0f, -500f, 0f);
            uiCam.enabled = false; uiCam.clearFlags = CameraClearFlags.Depth; uiCam.cullingMask = 0;
            uiCam.nearClipPlane = 0.01f; uiCam.farClipPlane = 10f; uiCam.targetTexture = ldr;
            var flipped = new System.Collections.Generic.List<(Canvas canvas, Camera camera, float plane)>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas != null && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.gameObject.activeInHierarchy)
                {
                    flipped.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCam; canvas.planeDistance = 1f;
                    uiCam.cullingMask |= 1 << canvas.gameObject.layer;
                }
            File.WriteAllText(Path.Combine(root, "hud.txt"), "canvases drawn over the victim's view: " + flipped.Count + Environment.NewLine);
            yield return null;
            yield return null;
            void Shoot(Camera c, string view, int index, bool hud = false)
            {
                PaeteKitPlayProbe.RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                if (hud) { Canvas.ForceUpdateCanvases(); uiCam.Render(); }
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            bool drained = false, hexed = false, phantoms = false;
            try
            {
                const int frames = 30 * 17;
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    var caster = t < 5.0f ? drainer : witch;
                    FaceTo(me, caster.transform.position);
                    if (t < 5.0f) FaceTo(drainer, me.transform.position);
                    drainer.Intent.Set(Verb.Skill2, t > 0.5f && t < 0.6f);
                    if (t >= 5.0f) FaceTo(witch, me.transform.position);
                    if (f == 150) me.Teleport(witch.transform.position + new Vector3(0.8f, 0f, -4.6f));
                    witch.Intent.Set(Verb.Skill2, (t > 5.5f && t < 5.6f) || (t > 9.0f && t < 9.1f));
                    if (f == 240 && me.VoodooMark == VoodooMarkKind.Hex) AgeMark(me, VoodooRules.HexArmSeconds - 0.5f);
                    yield return null;
                    drained |= me.IsDrained;
                    hexed |= me.IsHexed;
                    phantoms |= GameObject.Find("~HexedPhantomSlippers") != null && GameObject.Find("phantom-slipper") != null;
                    if (Camera.main != null) Shoot(Camera.main, "victim", f, hud: true);
                    Vector3 mid = (me.transform.position + caster.transform.position) * 0.5f;
                    wide.transform.position = mid + new Vector3(6.5f, 3.6f, -2.0f);
                    wide.transform.LookAt(mid + Vector3.up * 1.0f);
                    Shoot(wide, "wide", f);
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                foreach (var (canvas, camera, plane) in flipped)
                    if (canvas != null) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = camera; canvas.planeDistance = plane; }
                Object.Destroy(uiCam.gameObject);
                Object.Destroy(wide.gameObject);
                hdr.Release(); ldr.Release();
            }
            File.WriteAllText(Path.Combine(root, "claims.csv"), FormattableString.Invariant($"drained,{drained}\nhexed,{hexed}\nphantoms,{phantoms}\n"));
            Assert.IsTrue(drained, "DRAIN never drained the local player.");
            Assert.IsTrue(hexed, "HEX never hexed the local player.");
            Assert.IsTrue(phantoms, "HEXED showed no phantom slippers on the victim's screen.");
        }

        /// <summary>
        /// ⚠️ THE REACH, STAGED FOR REVIEW (HERO-10 v3, 2026-09-29). The owner on film v19: *"this animation dont look that good yet"*,
        /// *"lock in thoroughly improve also hthe doll is floating"*. A whole match film costs minutes a round; the reach's pose, the
        /// doll in her hand and the soul drawn out of the victim are judged here from every side first, then filmed. Each curse is
        /// pressed once at a victim 4.5 m in front of the caster and photographed at five moments (the lock, early and late in the
        /// hold, the mark, the gulp after it) from her FRONT three-quarter, her SIDE, BEHIND her toward the victim, and (DRAIN, the
        /// local seat) HER OWN SCREEN. Frames: `phaister-reach-review-TAG/{drain,hex}_{view}_{ms}.jpg`. Runs only with
        /// TUMP_PHAISTER_FILM=1.
        /// </summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator ReviewHerReachFromEverySide()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PHAISTER_FILM") != "1") Assert.Ignore("Film only: set TUMP_PHAISTER_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_PHAISTER_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "phaister-reach-review-" + tag);
            Directory.CreateDirectory(root);
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            int me = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            int victimSeat = -1;
            foreach (var p in round.Players) if (p.PlayerSlot != taya && p.PlayerSlot != me && victimSeat < 0) victimSeat = p.PlayerSlot;
            var victim = round.PlayerAt(victimSeat);
            victim.Intent.Parked = true;

            var cam = new GameObject("ReachReview").AddComponent<Camera>();
            cam.CopyFrom(Camera.main); cam.enabled = false; cam.tag = "Untagged"; cam.fieldOfView = 42; cam.cullingMask &= ~(1 << 5);
            cam.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Shoot(Camera c, string name)
            {
                PaeteKitPlayProbe.RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, name + ".jpg"), pixels.EncodeToJPG(90));
            }
            void Place(CharacterMotor caster, string view)
            {
                Vector3 at = caster.transform.position, to = victim.transform.position;
                var line = to - at; line.y = 0f; line.Normalize();
                var side = Vector3.Cross(Vector3.up, line);
                Vector3 mid = (at + to) * 0.5f;
                switch (view)
                {
                    case "front": cam.transform.position = at + line * 2.6f - side * 2.2f + Vector3.up * 1.5f; cam.transform.LookAt(at + Vector3.up * 1.0f + line * 0.6f); break;
                    case "side": cam.transform.position = mid + side * 6.2f + Vector3.up * 1.6f; cam.transform.LookAt(mid + Vector3.up * 0.9f); break;
                    default: cam.transform.position = at - line * 2.2f + side * 2.0f + Vector3.up * 2.8f; cam.transform.LookAt(mid + Vector3.up * 0.8f); break;
                }
            }
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            var moments = new[] { 4, 18, 40, 62, 68 };   // frames after the press: the lock, the hold, late hold, the mark, the gulp
            bool reachedDrain = false, reachedHex = false;
            try
            {
                foreach (string curse in new[] { "drain", "hex" })
                {
                    bool drain = curse == "drain";
                    var caster = Phaister(drain ? me : taya, can + new Vector3(drain ? -3f : 3f, .12f, -7f));
                    caster.Intent.Parked = false;
                    victim.Teleport(caster.transform.position + new Vector3(0f, 0f, 4.5f));
                    FaceTo(victim, caster.transform.position);
                    FaceTo(caster, victim.transform.position);
                    for (int w = 0; w < 6; w++) yield return null;
                    for (int f = 0; f <= 70; f++)
                    {
                        FaceTo(caster, victim.transform.position);
                        caster.Intent.Set(Verb.Skill2, f == 1 || f == 2);
                        yield return null;
                        if (drain) reachedDrain |= caster.IsVoodooReaching; else reachedHex |= caster.IsVoodooReaching;
                        if (Array.IndexOf(moments, f) < 0) continue;
                        int ms = Mathf.RoundToInt(f / 30f * 1000f);
                        foreach (string view in new[] { "front", "side", "behind" }) { Place(caster, view); Shoot(cam, $"{curse}_{view}_{ms:D4}"); }
                        if (drain && Camera.main != null) Shoot(Camera.main, $"{curse}_screen_{ms:D4}");
                    }
                    caster.Intent.Clear(); caster.Intent.Parked = true;
                    caster.Teleport(can + new Vector3(10f, .12f, 8f));
                    for (int w = 0; w < 90; w++) yield return null;
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
                Object.Destroy(cam.gameObject);
                hdr.Release(); ldr.Release();
            }
            Assert.IsTrue(reachedDrain, "DRAIN never reached.");
            Assert.IsTrue(reachedHex, "HEX never reached.");
        }
    }
}
