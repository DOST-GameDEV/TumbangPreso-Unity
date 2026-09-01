using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Every tab of the player hub and the sign-in screen, measured at nine resolutions.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE SCREENS IT REPLACED WERE FOUND BROKEN BY 🧑 PLAYING, NOT BY A
    /// TEST. He photographed the career page with its buttons running off the bottom edge and a
    /// stray CLASSIC label drawn straight through the HERO STRIKE tab, and the account page with
    /// six fields and six equal buttons on one block. `docs/TODO.md` § 92 has all five faults.
    /// **Three of the five were layout faults that a measurement would have caught the day they
    /// were written**, and the reason none did is that there was no probe for these screens: the
    /// UI probes cover the HUD (`HudOverflowProbe`), the character screen and the hero picker.
    ///
    /// ⚠️⚠️ THE OVERFLOW CHECK IS THE POINT, AND IT IS THE SAME ONE `HudOverflowProbe` MAKES FOR
    /// THE SAME REASON. `MenuKit.Label` sets `horizontalOverflow = Overflow`, so a string that
    /// does not fit does not wrap and does not shrink: it draws straight over whatever is beside
    /// it. That is precisely the CLASSIC-through-HERO-STRIKE artefact in the screenshot, and it is
    /// silent, because nothing errors and the label is still "there".
    ///
    /// ⚠️ IT DRIVES THE REAL SCREENS RATHER THAN A FIXTURE. `PlayerNameplate.Install` builds the
    /// hub and the hub builds the sign-in screen, so this exercises the one path the menu uses.
    /// A probe against a hand-built copy proves the copy.
    ///
    /// ⚠️ `GameServices.Account` AND `.Career` ARE NULL HERE AND THAT IS A CASE WORTH COVERING
    /// RATHER THAN A LIMITATION. A player who boots with no connection sees exactly this: an
    /// account that has not resolved and a career with nothing in it. Every empty state on these
    /// screens is therefore measured, which is the state the old career page got most wrong.
    /// </summary>
    public class PlayerHubLayoutProbe
    {
        /// <summary>
        /// ⚠️ ONE FILE PER CASE. The first version wrote all three to one path and the last
        /// test to finish silently overwrote the other two, so the evidence on disk described one
        /// of the three runs and nothing said which. A report that can be overwritten by a
        /// passing sibling is a report nobody can quote.
        /// </summary>
        private const string OutDir = "Logs";

        /// <summary>⚠️ PNGs GO IN THEIR OWN FOLDER, not beside the text reports. They are for a
        /// person to look at and `CLAUDE.md` § 6.1 wants every iteration versioned by NAME, so a
        /// folder that can be cleared and re-shot in one go is what that rule needs.</summary>
        private const string ShotDir = "Logs/ui";

        /// <summary>⚠️ THE SAME NINE `AspectRatioProbes` AND `HudOverflowProbe` USE. If this list
        /// ever disagrees with theirs, one of the three files is testing a screen the game does
        /// not ship.</summary>
        private static readonly (int W, int H, string Name)[] Resolutions =
        {
            (1280,  720, "16:9 720p"),
            (1600,  900, "16:9 900p"),
            (1920, 1080, "16:9 1080p"),
            (2560, 1440, "16:9 1440p"),
            (1366,  768, "16:9 laptop"),
            (1920, 1200, "16:10"),
            (2560, 1080, "21:9"),
            (3440, 1440, "21:9 1440p"),
            (1024,  768, "4:3"),
        };

        private GameObject _host;
        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        /// <summary>
        /// ⚠️⚠️ THE REAL `settings.json` IS RESTORED, BECAUSE THIS PROBE WRITES TO IT. The editor
        /// and the built player share `Application.persistentDataPath`, so
        /// `SettingsStore.Current` here IS the player's saved settings, and the boot-screen case
        /// below both clears and sets `AccountChoiceMade` and `SignInScreen` saves when it is
        /// answered. Leaving it set would mean the player never sees the screen they have not
        /// answered; leaving it clear would mean they see it again after answering.
        /// </summary>
        [SetUp]
        public void RememberTheAccountChoice()
        {
            var settings = Settings.SettingsStore.Current;
            _savedChoiceMade = settings != null && settings.AccountChoiceMade;
            _savedBooted = SceneFlow.BootedThroughSplash;
        }

        private bool _savedChoiceMade;
        private bool _savedBooted;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var settings = Settings.SettingsStore.Current;
            if (settings != null) settings.AccountChoiceMade = _savedChoiceMade;

            // ⚠️⚠️ RESTORED IN TEARDOWN AND NOT AT THE END OF THE CASE, BECAUSE A FAILING CASE
            // NEVER REACHES ITS OWN LAST LINE. The boot case sets `BootedThroughSplash` to claim
            // a launch; the first version put it back afterwards, that case failed on an
            // unrelated assertion, and the flag stayed true for the remainder of the run.
            // `UiClickProbe` and `SettingsWheelProbe` then went red with the boot screen over the
            // settings panel, **three suites away from the actual fault, blaming shipped code
            // that was fine**. `docs/TODO.md` § 91.5 is the same lesson about a static and its
            // own suite; a teardown is the only place a restore is guaranteed to run.
            SceneFlow.BootedThroughSplash = _savedBooted;

            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            yield return null;
        }

        /// <summary>
        /// Builds the real hub, walks all four tabs at all nine resolutions, and fails on the
        /// first label that does not fit the box it was given.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryTabFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var hub = _host.GetComponent<PlayerHub>();
            Assert.IsNotNull(hub, "the nameplate did not install a hub");
            hub.Open();
            yield return null;

            // ⚠️⚠️ § 114.14. Everything below measures a label against its own box; this asks
            // whether the boxes have boxes. `SignInScreen.BuildLogo` is one of the three recorded
            // instances of the fault, and the hub is the largest code-built surface in the game.
            RectParentage.AssertEveryRectHasARectParent(Root("PlayerHubCanvas"), "the player hub");

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                // ⚠️ EVERY TAB, NOT THE ONE THAT HAPPENS TO BE OPEN. The tabs are rebuilt on
                // switch rather than kept alive, so a fault on ACCOUNT is invisible while PROFILE
                // is showing, which is how a screen ships broken on one tab.
                // ⚠️ FRIENDS IS IN THE LOOP FROM THE DAY IT SHIPPED. `docs/TODO.md` § 92 records
                // three of five faults being layout faults a measurement would have caught the
                // day they were written, and the reason none did is that the screens had no probe.
                foreach (var tab in new[] { "PROFILE", "FRIENDS", "CAREER", "MATCHES", "ACCOUNT" })
                {
                    Press(tab);
                    yield return null;
                    yield return null;

                    int checked_ = Measure(Root("PlayerHubCanvas"), name, $"hub/{tab}", report);
                    Assert.Greater(checked_, 0,
                        $"{name}: the {tab} tab drew no labels at all, so this proves nothing.");

                    // ⚠️⚠️ COUNTING LABELS WAS NOT ENOUGH AND AN EMPTY LIST PASSED BECAUSE OF
                    // IT. The header, the four tab buttons and the footer are all labels, so a
                    // tab whose entire content failed to render still cleared "some labels were
                    // measured". That is exactly what happened: `UiRows.ScrollList` built its
                    // mask on a fully transparent graphic, which masks everything out, and the
                    // first screenshot showed chrome over an empty field. **Assert on the thing
                    // the screen is for.**
                    Assert.Greater(Rows(), 0,
                        $"{name}: the {tab} tab has no rows or sections in its list. The chrome " +
                        "can draw perfectly with nothing in it.");
                }
            }

            Write("tabs", report);
        }

        /// <summary>
        /// The sign-in screen, which is the one 🧑 named the reference for.
        ///
        /// ⚠️ IT IS A SEPARATE CASE BECAUSE IT IS A SEPARATE SCREEN. Folding it into the tab loop
        /// would hide which of the two failed behind one assertion message.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSignInScreenFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var signIn = _host.GetComponent<SignInScreen>();
            Assert.IsNotNull(signIn, "the hub did not install a sign-in screen");
            signIn.Open();
            yield return null;

            // ⚠️ § 114.14, on the screen `BuildLogo` drew a three-hundred-pixel wordmark through
            // the form on. `FitInParent` sizes against the PARENT, so a fitter whose parent has
            // no rect fits against nothing.
            RectParentage.AssertEveryRectHasARectParent(Root("SignInCanvas"), "the sign-in screen");

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);
                yield return null;

                int checked_ = Measure(Root("SignInCanvas"), name, "signin", report);
                Assert.Greater(checked_, 0, $"{name}: the sign-in screen drew no labels.");
            }

            Write("signin", report);
        }

        /// <summary>
        /// ⚠️⚠️ THE NAMEPLATE REPLACED TWO BUTTONS THAT WERE IN THE WRONG PLACE, so "is it on the
        /// screen" is the assertion this whole redesign turns on. 🧑: *"look wtf why are these
        /// buttons here"*. A nameplate that runs off the left edge at 4:3 is the same bug with
        /// better art.
        /// </summary>
        [UnityTest]
        public IEnumerator TheNameplateStaysOnScreenAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);
                yield return null;

                var plate = Find("Nameplate");
                Assert.IsNotNull(plate, "the nameplate was never built");

                var canvas = plate.GetComponentInParent<Canvas>();
                AssertInside((RectTransform)canvas.transform, (RectTransform)plate.transform,
                             name, "the nameplate");

                report.AppendLine($"{name,-14} {w}x{h}  nameplate ok");
            }

            Write("nameplate", report);
        }

        /// <summary>
        /// LOGIN, step 3 of the boot sequence: it appears on EVERY launch, one press leaves it,
        /// and a returning player is not asked anything at all.
        ///
        /// ⚠️⚠️ THIS IS THE TEST THAT MAKES THE BOOT GATE ACCEPTABLE, AND WITHOUT IT THE FEATURE
        /// SHOULD NOT SHIP. `FUTURE.md` PHASE 1's rule is *"never block a first-time player on a
        /// form"* and `docs/TODO.md` § 92.3 called the boot behaviour the one thing that must not
        /// move. 🧑 moved it twice, and the only reason all three positions can be true is that
        /// **CONTINUE AS GUEST is one press and needs no network.** That is a property, so it
        /// gets an assertion rather than a paragraph.
        ///
        /// ⚠️⚠️ AND `GameServices.Account` IS NULL IN THIS PROBE, WHICH IS THE POINT RATHER THAN
        /// A LIMITATION. It is the state of a machine that has never reached the service: no
        /// account, no session, nothing to await. **If CONTINUE AS GUEST ever needs an account to
        /// work, this case goes red**, which is exactly the regression worth catching, because
        /// the venue at the nationals has no internet and this screen is now in front of the game.
        ///
        /// ⚠️⚠️ THE "ONCE PER MACHINE" HALF OF THIS CASE IS GONE AND IT WAS DELETED ON PURPOSE,
        /// NOT LOST. `docs/TODO.md` § 114.5: 🧑 asked for LOGIN on every launch, with a player who
        /// already has an account passed through on its own. So the second install now asserts
        /// the OPPOSITE of what it used to: the screen is there again, and it leaves by itself.
        ///
        /// ⚠️ IT DRIVES `SignInScreen` DIRECTLY RATHER THAN THROUGH `PlayerNameplate`, because
        /// the plate no longer owns this step and a fixture that goes through a component the
        /// game does not install is a fixture that can pass while the real path is broken.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLoginStepAppearsEveryLaunchAndOnePressLeavesIt()
        {
            var report = new StringBuilder();
            var settings = Settings.SettingsStore.Current;
            Assert.IsNotNull(settings, "there are no settings to record the choice in");

            settings.AccountChoiceMade = false;

            _host = new GameObject("BootProbeHost");
            var signIn = _host.AddComponent<SignInScreen>();
            signIn.Install();
            signIn.OpenAtBoot();
            yield return null;
            yield return null;

            // ⚠️ `SignInRoot`, NOT `SignInCanvas`. `Close` deactivates the ROOT and leaves the
            // canvas alone, so asserting on the canvas asks whether the screen exists rather than
            // whether it is showing, and it answers yes for ever. The first version of this case
            // reported "one press did not leave the screen" against a press that worked.
            var root = Root("SignInRoot");
            Assert.IsNotNull(root, "the sign-in screen built no root");
            Assert.IsTrue(root.gameObject.activeInHierarchy,
                "the LOGIN step did not appear. It is step 3 of five and every launch gets it.");

            // ⚠️ THE CAPTION IS ASSERTED, NOT JUST THE BUTTON. At boot the same control means
            // "keep the account you already have"; from the ACCOUNT tab it means the TOURNAMENT
            // guest, which parks the owner's profile. Two behaviours behind one word is the
            // confusion this screen was rebuilt to remove.
            var guest = ButtonReading(root, "CONTINUE AS GUEST");
            Assert.IsNotNull(guest,
                "there is no CONTINUE AS GUEST on the boot screen. It is the one press that " +
                "makes a boot gate acceptable rather than a wall.");

            // ⚠️ HIDDEN, NOT ABSENT. The button is built once and its caption and visibility
            // change with the mode, so `GetComponentsInChildren<Button>(true)` finds it either
            // way. The first version of this assertion looked for absence and failed against
            // code that was correct, which is a test reporting its own wrong question.
            var back = ButtonReading(root, "BACK");
            Assert.IsFalse(back != null && back.gameObject.activeInHierarchy,
                "the boot screen shows BACK, which at boot dismisses to nothing at all");

            // ⚠️ AND WITH NO ACCOUNT ATTACHED THERE IS NO WELCOME-BACK STATE. A machine that has
            // never signed in must meet the form, not a greeting addressed to nobody.
            var greeting = ButtonReading(root, "CONTINUE");
            Assert.IsFalse(greeting != null && greeting.gameObject.activeInHierarchy,
                "an unattached machine was greeted by name rather than shown the form");

            guest.onClick.Invoke();
            yield return null;

            Assert.IsFalse(root.gameObject.activeInHierarchy,
                "one press of CONTINUE AS GUEST did not leave the screen");
            Assert.IsTrue(settings.AccountChoiceMade,
                "the answer was not recorded, and `ShouldOfferUpgrade` reads that flag");

            // ⚠️⚠️ THE SECOND LAUNCH ASSERTS THE NEW RULE, WHICH IS THE OPPOSITE OF THE OLD ONE.
            // The screen comes back. What must NOT come back is a question: with an account
            // attached it is the welcome-back state, and it lets go on its own.
            Object.DestroyImmediate(_host);
            _host = new GameObject("BootProbeHostAgain");
            var second = _host.AddComponent<SignInScreen>();
            second.Install();
            second.OpenAtBoot();
            yield return null;
            yield return null;

            var secondRoot = Root("SignInRoot");
            Assert.IsNotNull(secondRoot, "the second launch built no sign-in root");
            Assert.IsTrue(secondRoot.gameObject.activeInHierarchy,
                "LOGIN did not appear on the second launch. It is every launch now, not once " +
                "per machine (docs/TODO.md § 114.5).");

            report.AppendLine("login step shown on both launches, one press left the first");
            Write("boot-account", report);
        }

        /// <summary>The first button under `root` whose label reads exactly `label`, or null.</summary>
        private static Button ButtonReading(Transform root, string label)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label) return button;
            }

            return null;
        }

        /// <summary>
        /// Photographs every screen at 1920x1080 and writes them to `Logs/ui/`.
        ///
        /// ⚠️⚠️ `CLAUDE.md` § 6.1: SHOW, DO NOT DESCRIBE. A UI change with no render attached
        /// cannot be judged, and describing a layout in prose is the slowest possible way to be
        /// told it is wrong. 🧑 asked for exactly this: *"send pics of updated ui!"*. The rule was
        /// written for models and it is the same rule here.
        ///
        /// ⚠️⚠️ IT SEEDS A REAL CAREER FIRST, because the interesting screen is the one with
        /// numbers on it. `GameServices.Ensure` has already run by the time a PlayMode test
        /// starts (it is a `BeforeSceneLoad` hook), so the career exists and is empty;
        /// `ProfileRules.Apply` fills it with the same call the game uses, which also means the
        /// picture shows real formatting of real derived rates rather than placeholder text.
        ///
        /// ⚠️ IT IS NOT AN ASSERTION AND IT MUST NOT BECOME ONE. It exists to produce evidence a
        /// person looks at. The assertions live in the three cases above; a test that fails
        /// because a picture changed is a test nobody can keep green.
        /// </summary>
        [UnityTest]
        public IEnumerator PhotographEveryScreen()
        {
            var report = new StringBuilder();

            // ⚠️⚠️ THE PICTURE IS TAKEN OVER THE MENU, BECAUSE THE HUB IS A 93 PER CENT SCRIM
            // AND A SCRIM IS ONLY AS HONEST AS WHAT IS BEHIND IT. These shots used to be taken
            // in whatever scene the previous suite happened to leave loaded, and once the other
            // probes started blanking the scene after themselves they were taken over an EMPTY
            // one, which renders Unity's default blue-grey clear colour. 🧑, on the result:
            // *"i lowk liked the light brown bg earlier fuck that blue shti"*. **Nothing about
            // the screen had changed and the evidence said otherwise**, which is the worst thing
            // a piece of evidence can do.
            //
            // ⚠️⚠️ AND IT IS NOT ONLY ABOUT THE COLOUR. `UiRows.Band` is 3.5 per cent white,
            // measured against the live street specifically, and its own note says a number
            // tuned against one background is not a number. Photographing the zebra over a flat
            // clear colour measures it against a background the game never has.
            yield return LoadTheMenu(report);
            yield return Boot(report);

            // ⚠️⚠️ EVERY OTHER CANVAS IS SWITCHED OFF BEFORE A SINGLE SHOT IS TAKEN. PlayMode
            // runs these cases after other suites have loaded scenes, so the first render of the
            // career tab had the MULTIPLAYER setup screen drawn through it, join code field and
            // all. **A screenshot with another screen in it cannot be judged**, which is the
            // whole point of taking one, and the fix belongs here rather than in the game: the
            // game is never in two screens at once and the probe was.
            foreach (var c in _canvases)
                if (c != null && !c.name.StartsWith("PlayerHub") && !c.name.StartsWith("SignIn")
                    && !c.name.StartsWith("Nameplate"))
                    c.enabled = false;

            // ⚠️ THE MODE IS STATED RATHER THAN INHERITED. `PlayerHub._mode` reads
            // `SceneFlow.SelectedMode`, which is process state any earlier case could have left
            // anywhere, and a fixture that seeds one mode while the hub opens on another draws
            // the empty career. Say it out loud so the two cannot drift.
            SceneFlow.SelectedMode = GameMode.HeroStrike;

            SeedCareer(report);

            var hub = _host.GetComponent<PlayerHub>();
            var signIn = _host.GetComponent<SignInScreen>();

            yield return Resize(1920, 1080);

            // The nameplate on its own, which is what the title screen grew.
            yield return Shoot("01-nameplate");

            hub.Open();
            yield return null;
            yield return Shoot("02-hub-profile");

            // ⚠️⚠️ THE FRIENDS TAB IS PHOTOGRAPHED IN ITS EMPTY STATE, WHICH IS THE STATE EVERY
            // PLAYER MEETS IT IN AND THE ONE `FUTURE.md` § 0.5b QUESTION 3 SAYS GETS DESIGNED
            // LAST AND SEEN FIRST. This probe has no second account and no service session, so
            // what it can photograph is exactly what a new player sees: nobody yet, and a
            // sentence saying where friends come from.
            Press("FRIENDS");
            yield return null;
            yield return null;
            yield return Shoot("13-hub-friends-empty");

            Press("CAREER");
            yield return null;
            yield return null;
            yield return Shoot("03-hub-career-collapsed");

            // ⚠️ ONE GROUP OPENED, WHICH IS THE STATE THE COLLAPSING EXISTS FOR. A picture of
            // everything shut says nothing about what happens when you press one.
            Press("+  ATTACK");
            yield return null;
            yield return null;
            yield return Shoot("04-hub-career-open");

            Press("MATCHES");
            yield return null;
            yield return null;
            yield return Shoot("05-hub-matches");

            Press("ACCOUNT");
            yield return null;
            yield return null;
            yield return Shoot("06-hub-account");

            signIn.Open();
            yield return null;
            yield return null;
            yield return Shoot("07-signin");

            // ⚠️⚠️ THE BOOT SCREEN GETS ITS OWN PICTURE, AND NOT HAVING ONE IS EXACTLY HOW IT
            // SHIPPED BROKEN. § 97 opened this screen at boot over the LIVE MENU, and every shot
            // taken of it before that was `Open()` over an empty scene at 1920x1080. 🧑 launched
            // the 00:24 player and got the form floating over a fully lit title screen with the
            // nameplate drawn across it: *"i opened the game what the fuclk is this"*.
            // **The two states are different screens and only one of them had ever been looked
            // at.**
            //
            // ⚠️ AND IT IS SHOT AT HIS WINDOW SHAPE AS WELL AS AT 1080. `Fullscreen` is false in
            // his `settings.json`, so the game he actually plays is a short wide window, and the
            // nine probe resolutions are all taller than it. A screen that only exists at 16:9 is
            // a screen nobody in this room has seen.
            // ⚠️⚠️ THE MENU CANVAS GOES BACK ON FOR THESE TWO SHOTS, AND SWITCHING IT OFF IS
            // WHY THE PROBE COULD NOT SEE THE BUG. Every other shot in this method disables the
            // other canvases so the screen under test is not photographed through somebody
            // else's, which is right for a screen the player reaches by pressing something.
            // **The boot screen is DEFINED by appearing over the menu**, so a clean shot of it is
            // a shot of a situation that never happens. In the shipped 00:24 player it drew as a
            // floating form over a fully lit title screen, and this probe was green.
            //
            // ⚠️ IT IS `CLAUDE.md` § 6.2b's SECOND ROW AS CODE: over the real background, never
            // an empty scene. The first version of this render obeyed the letter of "take a
            // picture" and photographed a screen that does not exist.
            foreach (var c in _canvases)
                if (c != null && c.name.StartsWith("MainMenu")) c.enabled = true;

            signIn.OpenAtBoot();
            yield return null;
            yield return null;
            yield return Shoot("08-signin-at-boot-over-the-menu");

            // ⚠️ AND AT THE SHAPE HE ACTUALLY PLAYS AT. `Fullscreen` is false in his settings, so
            // the game he opens is a short wide window and every one of the nine probe
            // resolutions is taller than it. § 6.2b's third row.
            yield return Resize(1502, 721);
            yield return Shoot("09-signin-at-boot-windowed");
            yield return Resize(1920, 1080);

            Write("shots", report);

            // ⚠️⚠️ THIS CASE IS THE ONLY ONE IN THE CLASS THAT LOADS A SCENE, SO IT IS THE ONLY
            // ONE THAT HAS TO PUT ONE BACK. Cases run in name order, so `TheNameplate...` and
            // `TheSignIn...` come after this one and would otherwise boot inside a MainMenu whose
            // nameplate this case deleted and whose canvases it switched off. **The suite that
            // changed the world is the one that has to restore it**, which is the same rule
            // `MatchRecordIdentityProbe` and `PhaseSurfaceLayoutProbe` already follow.
            yield return Blank();
        }

        /// <summary>Replaces every loaded scene with one empty one.</summary>
        private static IEnumerator Blank()
        {
            var blank = SceneManager.CreateScene($"HubProbeBlank{Time.frameCount}");
            SceneManager.SetActiveScene(blank);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == blank || !scene.isLoaded) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                yield return ProbeWait.Done(unload, "scene unload");
            }

            yield return null;
        }

        /// <summary>
        /// Fills the career with four finished matches so the screens have something to draw.
        ///
        /// ⚠️ THE SAME `ProfileRules.Apply` THE GAME USES, so every rate on the picture is derived
        /// the way it will be in play. Writing numbers straight into `CareerTotals` would produce
        /// a screenshot of a state the game cannot reach.
        /// </summary>
        private static void SeedCareer(StringBuilder report)
        {
            var career = GameServices.Career;
            if (career?.Profile == null) { report.AppendLine("no career to seed"); return; }

            string me = TumbangPreso.Net.CareerStore.LocalPlayerId;
            string[] heroes = { "zack", "sean", "zack", "cheska" };

            for (int m = 0; m < 12; m++)
            {
                var players = new PlayerMatchStats[Balance.PlayerCount];
                for (int i = 0; i < players.Length; i++)
                    players[i] = new PlayerMatchStats
                    {
                        Slot = i,
                        PlayerId = i == 0 ? me : $"bot-{i}",
                        Handle = i == 0 ? "You#0000" : $"Player {i + 1}#111{i}",
                        IsBot = i != 0,
                        CharacterId = i == 0 ? heroes[m % heroes.Length] : "totoy",
                        SlipperId = "tsinelas",
                        Score = 400 - ((i + m) % 4) * 90,
                        ScoreAtFinalRound = 300 - ((i + m) % 4) * 70,
                        Throws = 24 + i * 3,
                        Knockdowns = 6 + i,
                        Retrievals = 14 + i,
                        RetrievalsUnderPressure = 5 + i,
                        Tags = 4 + i,
                        Sabotages = 2,
                        RoundsDefended = 1,
                        DefenceTicks = 46,
                        ShoveAttempts = 9,
                        ShoveHits = 5,
                        LungeAttempts = 7,
                        LungeHits = 3,
                        DistanceTravelled = 520.0f,
                        TimeToFirstThrow = 6.4f,
                        LongestLastAttacker = 11.5f,
                        ActiveRounds = Balance.Rounds,
                    };

                var record = new MatchRecord
                {
                    MatchId = $"shot-{m}",

                    // ⚠️⚠️ HERO STRIKE, AND IT WAS CLASSIC. `PlayerHub._mode` follows
                    // `SceneFlow.SelectedMode` now (`docs/TODO.md` § 114.12), which defaults to
                    // Hero Strike: it is the mode the lobby lands in and the mode the ranked
                    // ladder is on. A fixture seeded in the OTHER mode makes the career tab draw
                    // its empty state, and this case then reported that a group header was
                    // missing from a tab that had correctly decided it had nothing to show.
                    // ⚠️ IT ALSO MEANS THE ABILITY BUILDS GROUP IS PHOTOGRAPHED AT LAST, which is
                    // Hero Strike only and is how § 114.12's overflowing subtitle was found.
                    Mode = GameMode.HeroStrike.ToString(),
                    MapId = m % 2 == 0 ? "eskinita" : "ilalim_ng_tulay",
                    Rounds = Balance.Rounds,
                    DurationSeconds = 372.0f,
                    PlayedUtc = System.DateTime.UtcNow.AddHours(-m).ToString("O"),
                    WinningSlot = m % 4,
                    Online = true,
                    DefenderByRound = new[] { 0, 1, 2, 3 },
                    Players = players,
                };

                MatchRecordRules.Normalise(record);

                // ⚠️ THE CAREER IS SEEDED AND THE HISTORY IS NOT. `CareerStore.History` is an
                // `IReadOnlyList` by design, and the only public way to add to it is `Record`,
                // which also queues, saves and flushes to the endpoint. A probe must not post
                // twelve invented matches to a live career, so the MATCHES tab is photographed in
                // its empty state and that is the honest picture of it.
                ProfileRules.Apply(career.Profile, record, me);
            }

            report.AppendLine($"seeded 12 matches, xp {career.Profile.Xp}");
        }

        /// <summary>
        /// ⚠️⚠️ NO `WaitForEndOfFrame`, AND THIS HUNG A WHOLE RUN BEFORE THE LINE WAS REMOVED.
        /// In `-batchmode` there is no display and `WaitForEndOfFrame` never resumes, so the
        /// coroutine simply stops: Unity sat there with the log frozen, wrote no `.xml`, and had
        /// to be killed. It is the ordinary way to wait for a frame to finish drawing and it is
        /// wrong here, because **nothing is drawing a frame**: `Camera.Render` is an explicit
        /// draw into a target texture and needs no frame boundary at all.
        ///
        /// ⚠️ AND IT IS THE SAME SHAPE AS `CLAUDE.md` § 7's `-nographics` warning: an editor API
        /// that assumes a screen, in a run that has none, failing by doing nothing rather than by
        /// reporting anything.
        /// </summary>
        private IEnumerator Shoot(string name)
        {
            yield return null;

            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _target;

            var shot = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;

            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), shot.EncodeToPNG());
            Object.Destroy(shot);

            Debug.Log($"[PlayerHub] shot {name}");
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        /// <summary>
        /// Loads the title screen and removes its own nameplate, so `Boot` can build exactly one.
        ///
        /// ⚠️⚠️ THE MENU HAS A `PlayerNameplate` OF ITS OWN AND TWO OF THEM IS A BROKEN PROBE,
        /// NOT A COSMETIC PROBLEM. `Find("Nameplate")` and `Root("PlayerHubCanvas")` both answer
        /// by NAME, so with two live instances they can answer with the menu's, whose hub is
        /// closed, and the probe then measures a screen nobody opened. That failure has already
        /// happened once in this file's history and it reported "the PROFILE tab drew no labels
        /// at all" about a tab that was fine.
        ///
        /// ⚠️ THE MENU'S OWN CANVASES ARE LEFT ALONE HERE and switched off by
        /// `PhotographEveryScreen`'s existing filter, so what survives behind the scrim is the
        /// lit street and not PLAY / SETTINGS / TUTORIAL / QUIT. That is what a player sees with
        /// the hub open, because the hub covers the menu.
        /// </summary>
        private static IEnumerator LoadTheMenu(StringBuilder report)
        {
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            int removed = 0;
            foreach (var plate in Object.FindObjectsByType<PlayerNameplate>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(plate.gameObject);
                removed++;
            }

            // ⚠️⚠️ AND THE MENU'S OWN LOGIN STEP, WHICH IS NEW ON 2026-09-01 AND IS THE SAME
            // CLASS OF HAZARD ONE SCREEN ALONG. `ConvertedMainMenu.OfferTheLoginStep` installs a
            // `SignInScreen` at boot, `Root("SignInRoot")` answers by NAME, and this probe builds
            // its own; with two live instances a case can measure the menu's, which is closed,
            // and report "the sign-in screen drew no labels" about a screen that is fine. That is
            // the exact failure the nameplate paragraph above records, and it moved with the door.
            int logins = 0;
            foreach (var screen in Object.FindObjectsByType<SignInScreen>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(screen);
                logins++;
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.name == "SignInCanvas")
                    Object.DestroyImmediate(canvas.gameObject);
            }

            yield return null;
            report.AppendLine(
                $"loaded MainMenu, removed {removed} nameplate(s) and {logins} login screen(s)");
        }

        private IEnumerator Boot(StringBuilder report)
        {
            _host = new GameObject("HubProbeHost");

            // ⚠️⚠️ THE BOOT ACCOUNT SCREEN IS SWITCHED OFF FOR EVERY CASE EXCEPT ITS OWN, AND
            // WITHOUT THIS EVERY OTHER CASE IN THIS FILE FAILS. `PlayerNameplate.Install` now
            // opens `SignInScreen` when `AccountChoiceMade` is false, and `SignInScreen.Opened`
            // hides the hub root, so a probe that measures hub tabs would be measuring a hidden
            // screen and reporting "drew no labels". ⚠️ The flag is FALSE on this machine because
            // it is absent from the real `settings.json`, which is exactly the state a first-time
            // player is in, so this is not a hypothetical.
            var settings = Settings.SettingsStore.Current;
            if (settings != null) settings.AccountChoiceMade = true;

            var nameplate = _host.AddComponent<PlayerNameplate>();
            nameplate.Install();
            yield return null;

            // ⚠️ THE SCENE'S OWN CAMERA IF THERE IS ONE, so a shot taken over the menu renders
            // the street the menu is looking at. A bare probe camera at the origin renders the
            // clear colour and nothing else, which is correct for the three assertion cases
            // below (they run in an empty scene and only measure rects) and is exactly wrong for
            // a picture.
            // ⚠️ `Camera.main` NEEDS THE `MainCamera` TAG AND THE MENU'S IS NOT NECESSARILY
            // TAGGED. The first version used `Camera.main` alone, found null in `MainMenu`, built
            // a bare camera at the origin, and photographed the hub over an empty clear colour
            // again: the fix for the blue background produced a black one. Falling back to any
            // camera in the scene before building one is what actually renders the street.
            _camera = Camera.main;

            if (_camera == null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (cam == null || cam.targetTexture != null) continue;
                    _camera = cam;
                    break;
                }

            if (_camera == null)
            {
                _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
                _camera.transform.SetParent(_host.transform, false);
            }

            report.AppendLine($"camera: {_camera.name}");

            // ⚠️ THE SAME TRICK `AspectRatioProbes` USES, AND IT IS THE ONLY ONE THAT WORKS IN
            // BATCH MODE: `Screen.SetResolution` does nothing to an offscreen run, so the canvas
            // is switched to render through a camera whose target texture is the resolution.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
                _canvases.Add(c);
            }

            Assert.IsNotEmpty(_canvases, "no overlay canvas to resize: the probe would prove nothing");
            report.AppendLine($"canvases driven: {_canvases.Count}");
        }

        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            // Three frames: the scaler recomputes in its own Update, the layout rebuild lands the
            // frame after, and a ContentSizeFitter inside a ScrollRect settles on the third.
            for (int i = 0; i < 3; i++) yield return null;
        }

        private void Press(string label)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label) { button.onClick.Invoke(); return; }
            }

            Assert.Fail($"no button reading '{label}' on the hub");
        }

        /// <summary>
        /// ⚠️⚠️ `preferredWidth` IS THE MEASUREMENT AND A FONT METRIC IS NOT. It is what THIS
        /// string, in THIS font, at THIS size, with THESE generator settings will actually lay out
        /// to, which is the same thing `MenuKit.Fit`, `Hud.WorstCaseNameWidth` and
        /// `HudOverflowProbe` all ask. Anything else is an estimate of the thing that broke.
        ///
        /// ⚠️ A ZERO-WIDTH BOX IS SKIPPED RATHER THAN FAILED. A label inside a layout group that
        /// has not run yet reports a rect of 0 and would fail every assertion for a reason that
        /// has nothing to do with the string.
        /// </summary>
        private static int Measure(Transform root, string resolution, string screen,
                                   StringBuilder report)
        {
            int measured = 0;
            int worst = 0;
            string worstName = "";

            // ⚠️⚠️ SCOPED TO THIS SCREEN'S CANVAS, AND THE FIRST FULL RUN IS WHY. It searched
            // the whole scene, so it measured `SettingsSummary` on the settings panel, found it
            // authored at 16 units, and failed BOTH of this file's cases with the name of a label
            // neither of them draws. **A probe that reports another screen's fault under this
            // screen's name sends the next reader to the wrong file.** The 16-unit label is a
            // real finding and belongs to whoever owns that panel; `docs/TODO.md` § 92.6 records
            // it rather than this probe silently owning it.
            Assert.IsNotNull(root, $"{resolution} {screen}: the canvas was never built");

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                if (label.color.a < 0.05f) continue;
                if (!label.isActiveAndEnabled) continue;

                float room = label.rectTransform.rect.width;
                if (room <= 1.0f) continue;

                measured++;

                Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                    $"{resolution} {screen}: '{label.name}' is authored at {label.fontSize} " +
                    $"units, below the {MenuKit.MinReadableUnits}-unit floor.");

                // ⚠️⚠️ A WRAPPING LABEL IS CHECKED VERTICALLY, AND UNTIL 2026-08-31 IT WAS NOT
                // CHECKED AT ALL. `continue` was the whole treatment: a wrapped label's preferred
                // WIDTH is inside its box by definition — that is what wrapping means — so the
                // check below says nothing about it, and it was skipped instead of being asked
                // the question that does apply. **`UiRows.Row` then drew every two-line hint
                // below its own zebra band and over the row underneath**, on every screen built
                // from that file, with this probe green. `docs/TODO.md` § 102.
                //
                // ⚠️ IT IS THE SAME FAULT AS § 95 ROTATED NINETY DEGREES: a label a long way from
                // the readable floor that still does not fit the box it was given, silently,
                // because nothing compared the one dimension that mattered.
                if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                {
                    float tall = label.rectTransform.rect.height;
                    if (tall <= 1.0f) continue;

                    Assert.LessOrEqual(label.preferredHeight, tall + 1.0f,
                        $"{resolution} {screen}: '{label.name}' wraps to " +
                        $"{label.preferredHeight:F0} units in a {tall:F0}-unit box for " +
                        $"\"{label.text}\". It draws below its own row and over the next one. " +
                        "Either the sentence is shorter or the row grows: UiRows.Row does the " +
                        "second, so a failure here means the growth did not reach this label.");

                    continue;
                }

                float needed = label.preferredWidth;
                int over = Mathf.RoundToInt(needed - room);

                if (over > worst) { worst = over; worstName = label.name; }

                Assert.LessOrEqual(needed, room + 1.0f,
                    $"{resolution} {screen}: '{label.name}' needs {needed:F0} units for " +
                    $"\"{label.text}\" and was given {room:F0}. It does not wrap and does not " +
                    "shrink, so it draws over whatever is beside it.");
            }

            report.AppendLine($"{resolution,-14} {screen,-14} {measured,3} labels" +
                              (worst > 0 ? $"   worst spare {-worst} ({worstName})" : ""));
            return measured;
        }

        private static void AssertInside(RectTransform canvas, RectTransform what,
                                         string resolution, string described)
        {
            var canvasRect = canvas.rect;
            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);

                Assert.GreaterOrEqual(local.x, canvasRect.xMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.xMin - local.x:F0} units off the LEFT.");
                Assert.LessOrEqual(local.x, canvasRect.xMax + 0.5f,
                    $"{resolution}: {described} runs {local.x - canvasRect.xMax:F0} units off the RIGHT.");
                Assert.GreaterOrEqual(local.y, canvasRect.yMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.yMin - local.y:F0} units off the BOTTOM.");
                Assert.LessOrEqual(local.y, canvasRect.yMax + 0.5f,
                    $"{resolution}: {described} runs {local.y - canvasRect.yMax:F0} units off the TOP.");
            }
        }

        /// <summary>How many rows and section headings the open tab actually built.</summary>
        private static int Rows()
        {
            int found = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,
                                                                  FindObjectsSortMode.None))
                if (t.name.StartsWith("Row_") || t.name.StartsWith("Section_")) found++;
            return found;
        }

        /// <summary>The canvas a case is about, by name.</summary>
        private static Transform Root(string canvas)
        {
            var go = Find(canvas);
            return go != null ? go.transform : null;
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static void Write(string name, StringBuilder report)
        {
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, $"player-hub-{name}.txt"), report.ToString());
            Debug.Log($"[PlayerHub] {name}\n" + report);
        }
    }
}
