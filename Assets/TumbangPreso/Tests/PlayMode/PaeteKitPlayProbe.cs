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
    /// ⚠️⚠️ PAETE, PLAYED (HERO-9 step 2): a real Hero Strike match on Bayan Plaza, every ability pressed
    /// through `InputIntent` exactly as a player or a bot presses it, host-resolved as in a match. It
    /// answers the questions the kit had never been asked in play: does the vine reel him, does the
    /// seedling grow, fire on the second press and come out of the ground to an opponent's Interact
    /// hold only after its 15 s, does THORN HARVEST take slippers even out of a hand, does the sentry root the
    /// bodies in 9 m, does 7 s of Interact break a player free, and does a tag free a rooted player.
    /// Each case writes what it saw to `Logs/paete-play.csv`, one row per claim.
    /// </summary>
    public sealed class PaeteKitPlayProbe
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
            File.AppendAllText("Logs/paete-play.csv", _log.ToString());
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private void Note(string claim, object value) => _log.AppendLine(FormattableString.Invariant($"{claim},{value}"));

        private static CharacterMotor Paete(int slot, Vector3 at)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "paete");
            who.AbilitySystem.BindHero("paete");
            who.Teleport(at); who.transform.rotation = Quaternion.identity;
            who.Intent.Parked = false; who.IsBot = true;
            return who;
        }

        private static IEnumerator Press(CharacterMotor who, Verb verb, float seconds = .15f)
        {
            float start = Time.time;
            while (Time.time - start < seconds) { who.Intent.Set(verb, true); yield return null; }
            who.Intent.Set(verb, false);
            yield return null;
        }

        private static IEnumerator Hold(CharacterMotor who, Verb verb, float seconds, Action each = null)
        {
            float start = Time.time;
            while (Time.time - start < seconds) { who.Intent.Set(verb, true); each?.Invoke(); yield return null; }
            who.Intent.Set(verb, false);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheVinesReelHimForward()
        {
            var paete = Paete(1, new Vector3(0, .12f, -9));
            Vector3 start = paete.transform.position;
            paete.Intent.AimPoint = start + new Vector3(0, 1.2f, 8f);
            bool drew = false;
            yield return Press(paete, Verb.Skill1);
            float t0 = Time.time;
            while (Time.time - t0 < 1.4f) { drew |= Object.FindFirstObjectByType<Visual.PaeteVineReach>() != null; yield return null; }
            float travelled = Vector3.Distance(Flat(start), Flat(paete.transform.position));
            Note("vine_drew", drew); Note("vine_travel_m", travelled);
            Assert.IsTrue(drew, "No vine was drawn.");
            Assert.Greater(travelled, 3.0f, "Liana Leap did not carry him toward the anchor.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSeedlingGrowsFiresAndComesOutOnlyWhenLoose()
        {
            var paete = Paete(1, new Vector3(0, .12f, -9));
            paete.Intent.AimPoint = paete.transform.position + new Vector3(0, 0, 4f);
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(.8f);
            var plant = PaetePlant.OwnedBy(paete.PlayerSlot);
            Note("plant_spawned", plant != null);
            Assert.IsNotNull(plant, "Bakya Bloom planted nothing.");

            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds);
            Assert.IsTrue(plant.ShotReady, "The seedling never grew its first wooden slipper.");
            var lata = GameServices.Round.Lata;
            paete.Intent.AimPoint = lata.transform.position;
            yield return Press(paete, Verb.Skill2);
            yield return null;
            bool fired = Object.FindFirstObjectByType<PaeteWoodenSlipper>() != null;
            Note("plant_fired", fired);
            Assert.IsTrue(fired, "The second press did not throw a wooden slipper.");

            // An opponent at the plant before its 15 s: the hold does nothing.
            var puller = GameServices.Round.PlayerAt(2);
            puller.Teleport(plant.transform.position + new Vector3(0, .12f, -1.0f)); puller.Intent.Parked = false;
            yield return Hold(puller, Verb.Interact, 1.5f);
            Note("plant_survives_early_pull", PaetePlant.OwnedBy(paete.PlayerSlot) != null);
            Assert.IsNotNull(PaetePlant.OwnedBy(paete.PlayerSlot), "The plant came out inside its rooted 15 s.");

            while (!plant.Pullable) yield return null;
            float peak = 0;
            yield return Hold(puller, Verb.Interact, PaeteRules.PlantPullSeconds + .4f, () => peak = Mathf.Max(peak, puller.PullingPlantProgress));
            yield return new WaitForSeconds(1.1f);
            Note("pull_progress_peak", peak); Note("plant_pulled", PaetePlant.OwnedBy(paete.PlayerSlot) == null);
            Assert.Greater(peak, .8f, "The pull never showed progress.");
            Assert.IsNull(PaetePlant.OwnedBy(paete.PlayerSlot), "Holding Interact at a loose seedling did not pull it out.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ThornsTakeASlipperOutOfAHand()
        {
            var round = GameServices.Round;
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            var paete = Paete(taya, new Vector3(0, .12f, -4));
            yield return null;
            Assert.IsTrue(paete.AbilitySystem.Kit.IsDefending, "The taya's kit is not in its defending role.");
            CharacterMotor holder = null;
            foreach (var p in round.Players) if (p != paete && p.GetComponent<Carrier>().Held != null) { holder = p; break; }
            Assert.IsNotNull(holder);
            var shoe = holder.GetComponent<Carrier>().Held;
            holder.Teleport(new Vector3(4.5f, .12f, -4));
            yield return new WaitForSeconds(.2f);
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(1.4f);
            float home = Vector3.Distance(Flat(shoe.transform.position), Flat(paete.transform.position));
            Note("thorn_took_from_hand", holder.GetComponent<Carrier>().Held != shoe); Note("thorn_slipper_distance_m", home);
            Assert.AreNotSame(shoe, holder.GetComponent<Carrier>().Held, "THORN HARVEST left the slipper in the holder's hand.");
            Assert.Less(home, 2.2f, "The slipper was not hauled home.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSentryRootsAndATagOrSevenSecondsFreesThem()
        {
            var round = GameServices.Round;
            var paete = Paete(1, new Vector3(0, .12f, -9));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var a = round.PlayerAt(2); var b = round.PlayerAt(3);
            a.Teleport(new Vector3(2.5f, .12f, -4)); b.Teleport(new Vector3(-2.5f, .12f, -4));
            a.Intent.Parked = b.Intent.Parked = false;
            paete.Intent.AimPoint = new Vector3(0, 0, -4);
            yield return Press(paete, Verb.Ultimate, .2f);
            float t0 = Time.time;
            while (Time.time - t0 < 8f && !(a.IsRooted && b.IsRooted)) yield return null;
            Note("sentry_rooted_a", a.IsRooted); Note("sentry_rooted_b", b.IsRooted);
            Assert.IsTrue(a.IsRooted && b.IsRooted, "The sentry did not root both bodies inside 9 m.");
            Assert.Less(Vector3.Distance(Flat(a.transform.position), new Vector3(0, 0, -4)), 2.0f, "A caught body was not dragged in.");

            // Rooted: no movement.
            Vector3 held = a.transform.position;
            a.Intent.Move = new Vector2(1, 0);
            yield return new WaitForSeconds(.5f);
            a.Intent.Move = Vector2.zero;
            Note("rooted_moved_m", Vector3.Distance(held, a.transform.position));
            Assert.Less(Vector3.Distance(held, a.transform.position), .15f, "A rooted body walked.");

            // A tag frees b at once.
            b.ApplyTagged();
            yield return null;
            Note("tag_frees", !b.IsRooted);
            Assert.IsFalse(b.IsRooted, "A tag did not end Rooted.");

            // Seven seconds of Interact frees a; the struggle shows while they hold.
            bool struggled = false;
            yield return Hold(a, Verb.Interact, PaeteRules.BreakFreeHoldSeconds + .3f, () => struggled |= a.IsStruggling);
            Note("struggle_seen", struggled); Note("break_free", !a.IsRooted);
            Assert.IsTrue(struggled, "Holding Interact while rooted did not struggle.");
            Assert.IsFalse(a.IsRooted, "Seven seconds of Interact did not break free.");
        }

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN NEVER STANDS ON THE CAN (owner, 2026-09-27: *"make it so that it cant block the can too (dont let it be
        /// placed in a place it STANDS on can)"*). Aimed straight at the lata through real input, the tree comes up at least
        /// `PaeteRules.SentryCanClearance` from it, toward him, and still catches the body standing beside the can.
        /// </summary>
        [UnityTest]
        public IEnumerator AimedAtTheCanTheGuardianComesUpBesideIt()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Assert.IsNotNull(lata, "No lata on the court.");
            var can = Flat(lata.transform.position);
            var paete = Paete(1, can + new Vector3(0, .12f, -6));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var a = round.PlayerAt(2);
            a.Teleport(can + new Vector3(2.8f, .12f, 1f)); a.Intent.Parked = false;
            paete.Intent.AimPoint = lata.transform.position;
            yield return Press(paete, Verb.Ultimate, .2f);
            float t0 = Time.time;
            PaeteSentry sentry = null;
            while (Time.time - t0 < 8f && (sentry = Object.FindFirstObjectByType<PaeteSentry>()) == null) yield return null;
            Assert.IsNotNull(sentry, "The ultimate never raised its guardian.");
            float gap = Vector3.Distance(Flat(sentry.Centre), Flat(lata.transform.position));
            Note("sentry_can_gap_m", gap);
            Assert.GreaterOrEqual(gap, PaeteRules.SentryCanClearance - .05f, "The guardian came up on the can.");
            Assert.Less(Flat(sentry.Centre).z, can.z, "Aimed at the can, the guardian was not pushed back toward him.");
            while (Time.time - t0 < 9f && !a.IsRooted) yield return null;
            Note("sentry_can_still_catches", a.IsRooted);
            Assert.IsTrue(a.IsRooted, "Pushed off the can, it no longer caught the body standing beside it.");
        }

        /// <summary>
        /// ⚠️ A FILM, NOT A CLAIM (owner, 2026-09-26: *"in the video can u try to record as well people getting
        /// pulled and rooted towards it"*). Three players stand 5 to 6 m from where the seed will land; he casts
        /// MAKILING'S EMBRACE through real input and the whole of it is recorded: the introduction on his own
        /// screen (`owner/`), and the tree rising, the drag, the embrace and the sleep from a wide camera.
        /// Frames: `Logs/improvement-baseline-v1/paete-sentry-film/`. Runs only with TUMP_PAETE_FILM=1.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator FilmTheSentryPullingAndRootingThem()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            var round = GameServices.Round;
            // Paete is the local player (seat 1, `GameLaunch.SoloSeat`), a human with his own model, so
            // `owner/` is HIS screen, the introduction and all. (The first film left him a bot in the default
            // body: no introduction plays for a bot, and his arms were someone else's.)
            var paete = HumanPaete(new Vector3(0, .12f, -11));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var others = new[] { round.PlayerAt(0), round.PlayerAt(2), round.PlayerAt(3) };
            var stands = new[] { new Vector3(5.6f, .12f, -2.2f), new Vector3(-5.2f, .12f, -1.6f), new Vector3(1.8f, .12f, 2.2f) };
            for (int i = 0; i < others.Length; i++)
            {
                others[i].Teleport(stands[i]); others[i].Intent.Parked = false;
                others[i].transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -3) - stands[i]);
            }
            paete.Intent.AimPoint = new Vector3(0, 0, -3);
            var camera = new GameObject("PaeteSentryFilm").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 58; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            bool rootedAll = false;
            yield return ImprovementEvidenceProbe.Record(camera, "paete-sentry-film", 16f, paete,
                drive: t =>
                {
                    paete.Intent.Set(Verb.Ultimate, t < .25f);
                    rootedAll |= others[0].IsRooted && others[1].IsRooted && others[2].IsRooted;
                },
                witnessOffset: new Vector3(6f, 7f, 16f), witnessLookHeight: 2.6f);
            Object.Destroy(camera.gameObject);
            Note("film_all_rooted", rootedAll);
            Assert.IsTrue(rootedAll, "The film never showed all three rooted.");
        }

        /// <summary>
        /// ⚠️⚠️ THE ULTIMATE AS A VIDEO (HERO-9 "Film the cutscene ON HIS SCREEN in a match", and the owner,
        /// 2026-09-26: *"give me A video of his finished ULT animation + ppl getting pulled in too"*). Three
        /// views of ONE cast, each frame written as a JPG for `ffmpeg`:
        ///   owner/   HIS screen: `UltimatePhaseView` draws the cutscene through its own camera into a RawImage
        ///            named "UltimateScene", which `Camera.main` (all `Record` ever rendered) cannot see, so while
        ///            that picture is up its texture is copied; otherwise the live camera is rendered.
        ///   wide/    a fixed high shot of the court: the tree erupting and all three dragged in.
        ///   victim/  over the shoulder of one of the players it catches.
        /// ⚠️ `Time.captureFramerate` = 30 fixes game time per frame, so the film plays at true game speed even
        /// though the editor renders three views at a few frames a second (the old films ran 7 to 17 fps real
        /// time and read as slow motion). Runs only with TUMP_PAETE_FILM=1; frames under TUMP_EVIDENCE or Logs.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmTheUltimateOnHisScreen()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "paete-ult-film");
            foreach (string view in new[] { "owner", "wide", "victim" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var paete = HumanPaete(new Vector3(0, .12f, -11));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var others = new[] { round.PlayerAt(0), round.PlayerAt(2), round.PlayerAt(3) };
            var stands = new[] { new Vector3(5.6f, .12f, -2.2f), new Vector3(-5.2f, .12f, -1.6f), new Vector3(1.8f, .12f, 2.2f) };
            for (int i = 0; i < others.Length; i++)
            {
                others[i].Teleport(stands[i]); others[i].Intent.Parked = false;
                others[i].transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -3) - stands[i]);
            }
            paete.Intent.AimPoint = new Vector3(0, 0, -3);
            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("PaeteUltWide", 52);
            var victimCam = Make("PaeteUltVictim", 62);
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
            void Shoot(Camera c, string view, int index)
            {
                var before = c.targetTexture; c.targetTexture = hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
                Save(hdr, view, index);
            }
            bool rootedAll = false, sawScene = false, struggled = false, brokeOut = false, wasRooted = false;
            // ⚠️ THE MATCH CLOCK MUST NOT RUN UNDER A CUTSCENE (owner, 2026-09-26 night: *"ALSO match time should pause during
            // cutscenes"*). Read the round clock when the cutscene comes up, on its last frame, and a second after it hands back.
            float clockAtScene = -1f, clockAtSceneEnd = -1f, clockAfter = -1f; int lastSceneFrame = -1;
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            // The cutscene runs on the phase's wall clock; give it the film's frame clock so it lasts its real 5.0 s.
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            // Every world cue with its film time, so the video gets the game's own sound mixed in afterwards.
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            try
            {
                // ⚠️ 18 s (was 15): the 6.5 s cutscene, the hand-back catch, and then the caught player in `victim/` holds Interact
                // for the full break-out and gets out, on camera (owner, 2026-09-27: *"did u also animate already hhow theyre supposed
                // to get out of the tree by holding a button ?"*, *"can u show that in vid too?"*).
                const int frames = 30 * 18;
                var victim = others[1];
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    paete.Intent.Set(Verb.Ultimate, t < .25f);
                    // The filmed player fights free: Interact held from the moment they are held until the roots let go.
                    bool fighting = victim.IsRooted;
                    victim.Intent.Set(Verb.Interact, fighting);
                    struggled |= victim.IsStruggling;
                    if (wasRooted && !victim.IsRooted && !victim.IsTagged) brokeOut = true;
                    wasRooted = victim.IsRooted;
                    yield return null;
                    rootedAll |= others[0].IsRooted && others[1].IsRooted && others[2].IsRooted;
                    RawImage scene = null;
                    foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                        if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) scene = image;
                    if (scene != null)
                    {
                        // His theme plays from the introduction's own AudioSource, not as a world cue: log it once.
                        if (!sawScene) { cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_paete,1,0.2")); clockAtScene = round.TimeLeft; }
                        sawScene = true; Save(scene.texture, "owner", f);
                        clockAtSceneEnd = round.TimeLeft; lastSceneFrame = f;
                    }
                    else if (Camera.main != null) Shoot(Camera.main, "owner", f);
                    // Wide: far and high enough that the whole 9 m tree and all three stands fit.
                    wide.transform.position = new Vector3(11f, 7.5f, 9.5f);
                    wide.transform.LookAt(new Vector3(0, 3.0f, -3));
                    // Victim: from their side at knee-to-chest height, so the drag and the roots on their shins
                    // read (behind the head, the first cut, showed only the back of a head).
                    var toTree = new Vector3(0, 0, -3) - victim.transform.position; toTree.y = 0;
                    toTree = toTree.sqrMagnitude > .01f ? toTree.normalized : Vector3.back;
                    var side = Vector3.Cross(Vector3.up, toTree);
                    victimCam.transform.position = victim.transform.position + side * 3.4f - toTree * 1.2f + Vector3.up * 1.5f;
                    victimCam.transform.LookAt(victim.transform.position + toTree * 0.8f + Vector3.up * 1.0f);
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
            Debug.Log("[PaeteKitPlayProbe] shot report: " + UltimatePhaseView.LastShotReport);
            File.WriteAllText(Path.Combine(root, "shots.txt"), UltimatePhaseView.LastShotReport);
            string clock = FormattableString.Invariant($"round clock: {clockAtScene:F3} s left as the cutscene came up, {clockAtSceneEnd:F3} on its last frame, {clockAfter:F3} one second after it handed back");
            File.WriteAllText(Path.Combine(root, "clock.txt"), clock + System.Environment.NewLine);
            Debug.Log("[PaeteKitPlayProbe] " + clock);
            Note("ult_film_clock_during_cutscene_moved_s", clockAtScene - clockAtSceneEnd);
            Note("ult_film_saw_cutscene", sawScene);
            Note("ult_film_all_rooted", rootedAll);
            Note("ult_film_victim_struggled", struggled);
            Note("ult_film_victim_broke_out", brokeOut);
            Assert.IsTrue(sawScene, "The cutscene picture never came up on his screen.");
            Assert.IsTrue(rootedAll, "The film never showed all three rooted.");
            Assert.IsTrue(struggled && brokeOut, "The filmed player never struggled free of the roots by holding Interact.");
            Assert.Less(Mathf.Abs(clockAtScene - clockAtSceneEnd), 0.05f, "The match clock ran during the cutscene: " + clock);
            Assert.Greater(clockAtSceneEnd - clockAfter, 0.5f, "The match clock did not run again after the cutscene: " + clock);
        }

        /// <summary>
        /// ⚠️ PAETE'S BOTS, MEASURED (HERO-9 "Bots ... Not yet measured in `BotBehaviourProbe`"). The seeded
        /// whole-match probe picks its cast during the load, so Paete may never sit in it; this seats him in all
        /// four chairs with the real `AIController` pressing the real buttons, for two rounds so the taya turns
        /// over and both role abilities come up, and counts every ability each seat used (a cooldown or charge
        /// spent, the ultimate by its bar emptying, `BotBehaviourProbe.Count`'s rule). The bar is topped up every
        /// 20 s so the sentry's own decision (two bodies in 9 m) is what is tested, not the meter. Game time is
        /// stepped at `BotBehaviourProbe`'s 1/60 s, the only rate its bots are measured at.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator PaeteBotsUseEveryAbility()
        {
            var seats = new CharacterMotor[4];
            for (int i = 0; i < 4; i++)
            {
                seats[i] = Paete(i, new Vector3(-4.5f + 3f * i, .12f, -6f + (i % 2) * 3f));
                seats[i].Intent.Clear();
            }
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = true;
            var used = new System.Collections.Generic.Dictionary<string, int>();
            var lastCooldown = new System.Collections.Generic.Dictionary<HeroAbility, float>();
            var lastCharges = new System.Collections.Generic.Dictionary<HeroAbility, int>();
            var lastBar = new float[4];
            void Bump(string id) { used.TryGetValue(id, out int n); used[id] = n + 1; }
            float previousStep = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            try
            {
                const int frames = 60 * 190;
                for (int f = 0; f < frames; f++)
                {
                    if (f % (60 * 20) == 0) foreach (var who in seats) who.AbilitySystem.Kit.AddUltimateCharge(100);
                    yield return null;
                    for (int s = 0; s < 4; s++)
                    {
                        var kit = seats[s].AbilitySystem.Kit;
                        foreach (var ability in kit.AllAbilities)
                        {
                            if (ability == null || ability == kit.Ultimate) continue;
                            if (ability.UsesCharges)
                            {
                                if (lastCharges.TryGetValue(ability, out int before) && ability.ChargesRemaining < before) Bump(ability.Id);
                                lastCharges[ability] = ability.ChargesRemaining;
                            }
                            else
                            {
                                lastCooldown.TryGetValue(ability, out float before);
                                if (ability.CooldownRemaining > before + .01f) Bump(ability.Id);
                                lastCooldown[ability] = ability.CooldownRemaining;
                            }
                        }
                        if (lastBar[s] > kit.UltimateCost * .5f && kit.UltimateCharge <= .01f) Bump(kit.Ultimate.Id);
                        lastBar[s] = kit.UltimateCharge;
                    }
                }
            }
            finally { Time.captureDeltaTime = previousStep; }
            var kitIds = seats[0].AbilitySystem.Kit.AllAbilities;
            foreach (var ability in kitIds) { used.TryGetValue(ability.Id, out int n); Note("bot_uses_" + ability.Id, n); }
            string tally = string.Join(", ", System.Linq.Enumerable.Select(used, kv => kv.Key + "=" + kv.Value));
            Debug.Log("[PaeteKitPlayProbe] bot uses: " + tally);
            foreach (var ability in kitIds)
                Assert.IsTrue(used.ContainsKey(ability.Id), $"Paete's bots never used {ability.Id} in two rounds ({tally}).");
        }

        /// <summary>The local seat as Paete, human, in his own model (arms and all), for the films.</summary>
        private static CharacterMotor HumanPaete(Vector3 at)
        {
            var paete = Paete(GameLaunch.SoloSeat, at);
            paete.IsBot = false;
            var art = RosterBook.Load().FindPersonArt("paete");
            paete.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            return paete;
        }

        /// <summary>
        /// ⚠️ A FILM (HERO-9: *"a first-person vine capture"*): LIANA LEAP from his own eyes, the vines leaving
        /// the viewmodel hands, and the same leap from beside him. Runs only with TUMP_PAETE_FILM=1.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator FilmTheVineFromHisOwnEyes()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            var paete = HumanPaete(new Vector3(0, .12f, -9));
            paete.Intent.AimPoint = paete.transform.position + new Vector3(0, 1.2f, 8f);
            var camera = new GameObject("PaeteVineFilm").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 55; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            yield return ImprovementEvidenceProbe.Record(camera, "paete-vine-film", 3.2f, paete,
                drive: t => paete.Intent.Set(Verb.Skill1, t > .5f && t < .7f),
                witnessOffset: new Vector3(4.2f, 1.6f, 1.2f), witnessLookHeight: 1.1f);
            Object.Destroy(camera.gameObject);
            Note("film_vine", true);
        }

        /// <summary>
        /// ⚠️ A FILM OF HIS OTHER THREE SKILLS IN ONE MATCH (owner, 2026-09-27: *"ok can u show me hhow his other skills look like? and
        /// them working ty"*). Real input, game time fixed at 30 fps, every world cue logged for the mp4:
        ///   0.5   LIANA LEAP: the vines out to 8 m ahead, reeling him in.
        ///   2.6   BAKYA BLOOM: the pitcher planted between him and the can; it grows its wooden clog (3 s); the second press fires it
        ///         at the can.
        ///   9.9   THORN HARVEST: a second Paete, the taya, stamps; the slipper in a player's hand 4.5 m away is caught, held, hauled home.
        /// Views: `owner/` is his own screen when the acting Paete is the local seat, else a camera over the acting Paete's shoulder;
        /// `wide/` is a court camera placed for each skill. Runs only with TUMP_PAETE_FILM=1; frames under TUMP_EVIDENCE or Logs.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHisSkillsInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "paete-skills-film");
            foreach (string view in new[] { "owner", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var lata = round.Lata;
            Assert.IsNotNull(lata, "No lata on the court.");
            var can = Flat(lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            // The attacking Paete is the local seat when it can be (his own screen), else another attacker seat.
            int attackSeat = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            // ⚠️ Film s1 started him behind the plaza's monument and never turned him to his plant: each skill has its own clear
            // spot now, facing what it acts on (the ultimate film's lane for the vine, a spot facing the plant and the can for the
            // bloom, the taya in the box facing the slipper's holder for the thorns), with a cut between.
            Vector3 start = can + new Vector3(0f, .12f, -12f);
            Vector3 bloomAt = can + new Vector3(-2.5f, .12f, -8.5f);
            var attacker = attackSeat == GameLaunch.SoloSeat ? HumanPaete(start) : Paete(attackSeat, start);
            var defender = Paete(taya, can + new Vector3(-3f, .12f, -1.5f));
            CharacterMotor holder = null;
            foreach (var p in round.Players)
                if (p != attacker && p != defender && p.GetComponent<Carrier>().Held != null) { holder = p; break; }
            if (holder != null) { holder.Teleport(can + new Vector3(1.5f, .12f, -1.5f)); holder.Intent.Parked = true; }
            var local = round.PlayerAt(GameLaunch.SoloSeat);
            Vector3 plantSpot = can + new Vector3(-1.5f, 0f, -4.8f);
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
            var wide = Make("PaeteSkillsWide", 50);
            var shoulder = Make("PaeteSkillsShoulder", 60);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Shoot(Camera c, string view, int index)
            {
                var before = c.targetTexture; c.targetTexture = hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
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
            bool planted = false, fired = false, reeled = false;
            float fireAt = -1f;
            Slipper shoe = holder != null ? holder.GetComponent<Carrier>().Held : null;
            try
            {
                const int frames = (int)(30 * 13.5f);
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // LIANA LEAP.
                    attacker.Intent.AimPoint = start + new Vector3(0f, 1.2f, 8f);
                    if (f == 72) attacker.Teleport(bloomAt);
                    if (t >= 2.4f) { attacker.Intent.AimPoint = plantSpot; Face(attacker, fireAt > 0f && t >= fireAt - .3f ? lata.transform.position : (plantSpot + can) * .5f); }
                    Face(defender, holder != null ? holder.transform.position : can);
                    attacker.Intent.Set(Verb.Skill1, t > .5f && t < .7f);
                    // BAKYA BLOOM: plant, wait for the clog, fire it at the can.
                    var plant = PaetePlant.OwnedBy(attacker.PlayerSlot);
                    planted |= plant != null;
                    if (plant != null && plant.ShotReady && fireAt < 0f && t > 6.0f) fireAt = t + .3f;
                    if (fireAt > 0f && t >= fireAt - .2f) attacker.Intent.AimPoint = lata.transform.position;
                    bool pressPlant = t > 2.6f && t < 2.8f, pressFire = fireAt > 0f && t > fireAt && t < fireAt + .2f;
                    attacker.Intent.Set(Verb.Skill2, pressPlant || pressFire);
                    // THORN HARVEST.
                    defender.Intent.Set(Verb.Skill2, t > 9.9f && t < 10.1f);
                    yield return null;
                    reeled |= Vector3.Distance(Flat(attacker.transform.position), Flat(start)) > 3f;
                    // Film s2 found a building-sized dark shell from the plant's landing on: name every big renderer that is not the map.
                    if (f == 120)
                    {
                        var big = new StringBuilder();
                        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        {
                            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.bounds.size.magnitude < 6f) continue;
                            var top = r.transform.root.name;
                            if (top == "BayanPlaza" || top.StartsWith("Map")) continue;
                            string path = r.name; for (var q = r.transform.parent; q != null; q = q.parent) path = q.name + "/" + path;
                            big.AppendLine(FormattableString.Invariant($"{path}  size {r.bounds.size}  centre {r.bounds.center}  {r.GetType().Name}"));
                        }
                        File.WriteAllText(Path.Combine(root, "big-renderers.txt"), big.ToString());
                    }
                    fired |= Object.FindFirstObjectByType<PaeteWoodenSlipper>() != null;

                    // The cameras, per skill.
                    CharacterMotor acting = t < 9.6f ? attacker : defender;
                    if (t < 2.4f) { wide.transform.position = start + new Vector3(6.5f, 2.6f, 4.5f); wide.transform.LookAt(start + new Vector3(0f, 1f, 4.5f)); }
                    else if (t < 9.6f) { wide.transform.position = can + new Vector3(5.5f, 4.2f, -11f); wide.transform.LookAt(can + new Vector3(-1.2f, .8f, -4.5f)); }
                    else { wide.transform.position = can + new Vector3(-.75f, 3.6f, -9f); wide.transform.LookAt(can + new Vector3(-.75f, .8f, -1.5f)); }
                    if (acting == local && Camera.main != null) Shoot(Camera.main, "owner", f);
                    else
                    {
                        var fwd = acting.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                        shoulder.transform.position = acting.transform.position - fwd * 3.2f + Vector3.up * 2.1f + Vector3.Cross(Vector3.up, fwd) * .9f;
                        shoulder.transform.LookAt(acting.transform.position + fwd * 4f + Vector3.up * 1f);
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
            bool snatched = shoe != null && holder.GetComponent<Carrier>().Held != shoe;
            Note("skills_film_reeled", reeled); Note("skills_film_planted", planted); Note("skills_film_fired", fired); Note("skills_film_snatched", snatched);
            Assert.IsTrue(reeled, "LIANA LEAP did not carry him.");
            Assert.IsTrue(planted, "BAKYA BLOOM planted nothing.");
            Assert.IsTrue(fired, "BAKYA BLOOM never fired its wooden slipper.");
            Assert.IsTrue(snatched, "THORN HARVEST left the slipper in the holder's hand.");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    }
}
