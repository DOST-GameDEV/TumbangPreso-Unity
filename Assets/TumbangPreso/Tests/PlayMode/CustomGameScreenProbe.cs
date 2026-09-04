using System.Collections;
using System.Collections.Generic;
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
    /// Phase 12's surface, at the nine resolutions every other UI probe drives.
    ///
    /// ⚠️⚠️ IT ASSERTS THE ANSWERS TO `CLAUDE.md` § 6.2'S FOUR QUESTIONS AND NOT ONLY THAT THE
    /// LABELS FIT, because § 6.2a is blunt that a green layout probe is not a good screen: seven
    /// readability faults were true at once on a screen where every label fitted its box. What a
    /// probe CAN ask here is whether the four structural promises the screen makes are still
    /// kept, and each of the four below is a failure this repository has actually shipped:
    ///
    ///  1. **The headline exists and says what this match is.** § 92's six-button panel is what a
    ///     screen with no leading thing looks like.
    ///  2. **The conditional rows are absent rather than greyed** when they do not apply, which is
    ///     § 6.2's third question and the reason THE ROOM is a closed group at all.
    ///  3. **The one action refuses an unplayable rule set, and says why.** § 108's EQUIP button
    ///     with no listener is a dead control that looked fine; a dead control that is CORRECTLY
    ///     dead and silent is the same fault wearing a better excuse.
    ///  4. **The ranked line is present in both states.** `CustomGameRules.CanBeRanked`'s header
    ///     calls itself the one rule in that file that is not negotiable, and a player finding out
    ///     on the results board is finding out too late.
    ///
    /// ⚠️ THE PICTURE IS STILL OWED AND IS STILL A PERSON'S JOB. `PhotographTheScreen` writes a
    /// PNG to `Logs/ui/` for review, the same way `PlayerHubLayoutProbe.PhotographEveryScreen`
    /// does. The probe asks whether the screen is a screen; the picture asks whether it reads.
    /// </summary>
    public class CustomGameScreenProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        /// <summary>⚠️ THE SAME NINE `AspectRatioProbes`, `QueueCardLayoutProbe` AND
        /// `PlayerHubLayoutProbe` USE. A tenth list would be a tenth screen.</summary>
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
        private CustomGameScreen _screen;
        private CustomRules _restore;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();

            if (_screen != null) Object.Destroy(_screen.gameObject);
            if (_host != null) Object.Destroy(_host);

            // ⚠️ THE SESSION'S RULES GO BACK. `SceneFlow.SelectedRules` is a session static that
            // the match reads, so a probe that left a 1-round 30-second Mirror match selected
            // would hand the NEXT suite a game nobody set up. `docs/TODO.md` § 126.8's whole
            // class of fault is state outliving the test that made it.
            if (_restore != null) SceneFlow.SetSelectedRules(_restore);

            yield return null;
        }

        private IEnumerator OpenScreen(CustomRules rules)
        {
            _restore ??= SceneFlow.SelectedRules.Clone();

            SceneFlow.SetSelectedRules(rules);

            _host = new GameObject("CustomGameProbeHost");
            _screen = CustomGameScreen.Ensure();
            _screen.Open();

            // Two frames: one for the build, one for the layout groups to run.
            yield return null;
            yield return null;
        }

        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryRowFitsItsBoxAtEveryShippedResolution()
        {
            yield return OpenScreen(CustomGameRules.Defaults(GameMode.HeroStrike));

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            // ⚠️⚠️ § 114.14, AND IT IS THE ONE QUESTION EVERY OTHER ASSERTION IS BLIND TO.
            // A rect whose parent is not a rect resolves against nothing and lands in the middle
            // of the window at whatever size it was authored, which is a layout that measures
            // perfectly and draws in a heap.
            RectParentage.AssertEveryRectHasARectParent(canvas, "the custom game screen");

            yield return Drive();

            var report = new StringBuilder();

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                int measured = Measure(canvas, name, report);
                Assert.Greater(measured, 0,
                    $"{name}: the custom game screen drew no labels, so this proves nothing");

                report.AppendLine($"{name,-14} {w}x{h}  {measured} labels");
            }

            Debug.Log($"[CustomGameScreenProbe]\n{report}");
        }

        /// <summary>
        /// § 6.2 question 1: what is the ONE thing on this screen.
        ///
        /// ⚠️ THE HEADLINE IS ASSERTED TO CARRY THE FACTS RATHER THAN TO BE NON-EMPTY. A label
        /// reading "CUSTOM GAME" would pass an emptiness check and tell the player nothing about
        /// the match they are about to start, which is the whole job of that line.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHeadlineSaysWhatThisMatchActuallyIs()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.Format = MatchFormat.LastTsinelas;
            rules.Rounds = 3;
            rules.RoundSeconds = 60;
            rules.Tsinelas = 2;

            yield return OpenScreen(rules);

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            string headline = HeadlineText(canvas);
            Assert.IsNotNull(headline, "the screen has no headline at all");

            StringAssert.Contains("CLASSIC", headline,
                "the headline does not say which MODE this is, and Classic and Hero Strike are " +
                "two different games (docs/VISION.md § 1)");

            StringAssert.Contains(CustomGameRules.FormatName(MatchFormat.LastTsinelas), headline,
                "the headline does not name the format");

            StringAssert.Contains("3 rounds", headline,
                "the headline does not say how many rounds are being played");

            StringAssert.Contains("60s", headline,
                "the headline does not say how long a round is");

            StringAssert.Contains("2 tsinelas", headline,
                "the headline does not carry the tsinelas stock, which is the one number LAST " +
                "TSINELAS STANDING is about");
        }

        /// <summary>
        /// § 6.2 question 3: what is on screen that the player does not need right now.
        ///
        /// ⚠️⚠️ ABSENT, NOT GREYED, AND THE DIFFERENCE IS THE POINT. A disabled TSINELAS EACH row
        /// under a STANDARD match is furniture that teaches the player about a setting that does
        /// not exist for them. `UiRows.Section`'s own note is the mechanism: *"a closed group is
        /// not built, it is not hidden"*.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTsinelasRowExistsOnlyUnderTheFormatItBelongsTo()
        {
            yield return OpenScreen(CustomGameRules.Defaults(GameMode.HeroStrike));

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            Assert.IsNull(RowNamed(canvas, "TSINELAS EACH"),
                "a STANDARD match is offering a TSINELAS EACH row, which does nothing. " +
                "SceneFlow.SelectedTsinelas is read only under LAST TSINELAS STANDING.");

            var last = CustomGameRules.Defaults(GameMode.HeroStrike);
            last.Format = MatchFormat.LastTsinelas;

            SceneFlow.SetSelectedRules(last);
            _screen.Close();
            _screen.Open();
            yield return null;
            yield return null;

            Assert.IsNotNull(RowNamed(canvas, "TSINELAS EACH"),
                "LAST TSINELAS STANDING has no control for its own stock, so the format ships " +
                "with a number nothing can change. docs/TODO.md § 130.13.");
        }

        /// <summary>
        /// § 6.2 and `docs/TODO.md` § 108: the one action is never dead and never silent.
        /// </summary>
        [UnityTest]
        public IEnumerator AnUnplayableRuleSetRefusesTheActionAndSaysWhy()
        {
            var bad = CustomGameRules.Defaults(GameMode.HeroStrike);
            bad.Private = true;
            bad.Password = "no";           // under MinPasswordLength

            yield return OpenScreen(bad);

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            Assert.IsNotEmpty(CustomGameRules.Refusal(bad),
                "this test's own fixture is playable, so it proves nothing");

            var action = ButtonNamed(canvas, "USE THESE RULES");
            Assert.IsNotNull(action, "the screen has no primary action");
            Assert.IsFalse(action.interactable,
                "the screen offers USE THESE RULES on a rule set the match will refuse");

            string shown = RefusalText(canvas);
            Assert.IsNotEmpty(shown,
                "the action is refused and nothing on screen says why. docs/TODO.md § 108: a " +
                "control that does nothing when pressed is the bug, and a silent refusal is the " +
                "same control with a better excuse.");

            Assert.AreEqual(CustomGameRules.Refusal(bad), shown,
                "the screen is showing a refusal it wrote itself instead of the core's. A second " +
                "copy of a sentence is docs/TODO.md § 5's drift rule.");
        }

        /// <summary>
        /// ⚠️ RANKED IS SAID IN BOTH STATES, NOT ONLY THE BAD ONE. A line that appears only when
        /// something is wrong is a warning; a line that is always there is a fact, and this one
        /// has to be readable BEFORE eight rounds are played.
        /// </summary>
        [UnityTest]
        public IEnumerator TheRankedLineIsPresentWhicheverWayItReads()
        {
            yield return OpenScreen(CustomGameRules.Defaults(GameMode.HeroStrike));

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            string shipped = RankedText(canvas);
            Assert.IsNotEmpty(shipped, "the shipped rules say nothing about the ladder");
            StringAssert.Contains("rank", shipped.ToLowerInvariant(),
                "the ranked line does not mention the rank");

            var custom = CustomGameRules.Defaults(GameMode.HeroStrike);
            custom.Rounds = 3;

            Assert.IsFalse(CustomGameRules.CanBeRanked(custom),
                "this test's own fixture is still rankable, so it proves nothing");

            SceneFlow.SetSelectedRules(custom);
            _screen.Close();
            _screen.Open();
            yield return null;
            yield return null;

            string changed = RankedText(canvas);
            Assert.IsNotEmpty(changed, "a custom rule set says nothing about the ladder");
            Assert.AreNotEqual(shipped, changed,
                "the ranked line reads the same for a shipped rule set and a three-round one, " +
                "so it is decoration rather than a fact");
        }

        [UnityTest]
        public IEnumerator PhotographTheScreen()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Format = MatchFormat.LastTsinelas;

            yield return OpenScreen(rules);

            var canvas = Root("CustomGameCanvas");
            Assert.IsNotNull(canvas, "the custom game screen built no canvas");

            yield return Drive();
            yield return Resize(1920, 1080);
            yield return null;

            System.IO.Directory.CreateDirectory("Logs/ui");
            Shoot("Logs/ui/custom-game_v1.png");
        }

        // -------------------------------------------------------------------
        // § HELPERS, the same ones `QueueCardLayoutProbe` uses.
        // -------------------------------------------------------------------

        private void Shoot(string path)
        {
            var tex = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            var was = RenderTexture.active;

            RenderTexture.active = _target;
            _camera.Render();
            tex.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            tex.Apply();
            RenderTexture.active = was;

            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);

            Debug.Log($"[CustomGameScreenProbe] wrote {path}");
        }

        private IEnumerator Drive()
        {
            if (_camera == null)
            {
                _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
                if (_host != null) _camera.transform.SetParent(_host.transform, false);
            }

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
            yield return null;
        }

        /// <summary>
        /// Point the capture camera at a new shape and let the layout actually settle.
        ///
        /// ⚠️⚠️ THREE FRAMES WAS NOT ENOUGH AND THE FIRST RUN OF THIS PROBE IS THE RECEIPT.
        /// `docs/TODO.md` § 131.8: the very first execution failed on ONE row at ONE resolution,
        /// *"'Label' reading \"No bots · open to anybody with the code\" needs 306 px and
        /// was given 16"*. **16 px is not a narrow column, it is a rect that has never been laid
        /// out**: `UiRows.Section`'s shut-group summary is anchored from `ValueColumn` 0.56 to the
        /// right margin, so on a settled list it is around 368 units and on an unsettled one it
        /// is whatever the parent was before the resize.
        ///
        /// ⚠️ IT IS THE FIRST RESOLUTION IN THE LIST THAT FAILED, WHICH IS THE TELL. The other
        /// eight passed because by then the scroll rect had been driven for several frames. A
        /// bound that depends on how many frames have happened to elapse is not a bound.
        ///
        /// ⚠️ `Canvas.ForceUpdateCanvases` PLUS A REBUILD ON THE CONTENT, NOT MORE `yield return
        /// null`. Waiting longer would have hidden this one and left the next person the same
        /// afternoon: a `ScrollRect` with a `ContentSizeFitter` under an `AspectSafeCanvas`
        /// settles when it is TOLD to, and the two calls below are what tell it. The frames after
        /// are for anything that reacts to the rebuild rather than causes it.
        /// </summary>
        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            for (int i = 0; i < 3; i++) yield return null;

            Canvas.ForceUpdateCanvases();

            foreach (var group in Object.FindObjectsByType<UnityEngine.UI.LayoutGroup>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (group != null && group.transform is RectTransform rt)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        private static Transform Root(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// ⚠️ ROWS ARE FOUND BY THEIR LABEL TEXT, NOT BY A GAMEOBJECT NAME. `UiRows.Row` names its
        /// objects after the label it was handed, but that is an implementation detail of that
        /// file; the LABEL is what the player reads and is what this probe is actually asking
        /// about. It also means a row renamed on screen fails here, which is correct.
        /// </summary>
        private static Text RowNamed(Transform scope, string label)
        {
            foreach (var t in scope.GetComponentsInChildren<Text>(true))
                if (t != null && string.Equals(t.text, label, System.StringComparison.Ordinal))
                    return t;

            return null;
        }

        private static Button ButtonNamed(Transform scope, string label)
        {
            foreach (var b in scope.GetComponentsInChildren<Button>(true))
            {
                var text = b.GetComponentInChildren<Text>(true);
                if (text != null && text.text != null &&
                    text.text.Contains(label, System.StringComparison.OrdinalIgnoreCase))
                    return b;
            }

            return null;
        }

        private static string TextContaining(Transform scope, string fragment)
        {
            foreach (var t in scope.GetComponentsInChildren<Text>(true))
            {
                if (t == null || string.IsNullOrEmpty(t.text)) continue;
                if (t.text.Contains(fragment, System.StringComparison.OrdinalIgnoreCase))
                    return t.text;
            }

            return "";
        }

        private static string HeadlineText(Transform scope) => TextContaining(scope, " · ");

        private static string RankedText(Transform scope) => TextContaining(scope, "rank");

        private static string RefusalText(Transform scope)
        {
            // The refusal sentences all start with "A " or "Last Tsinelas" or "There are";
            // asking for the shared word is what keeps this from restating them.
            string found = TextContaining(scope, "characters, or empty");
            if (!string.IsNullOrEmpty(found)) return found;

            found = TextContaining(scope, "rounds.");
            if (!string.IsNullOrEmpty(found)) return found;

            return TextContaining(scope, "seconds.");
        }

        private static int Measure(Transform root, string resolution, StringBuilder report)
        {
            int measured = 0;

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;

                var rect = label.rectTransform.rect;
                if (rect.width <= 1.0f) continue;

                // ⚠️ THE FLOOR IS `PaperKit.Caption` FOR `QueueCardLayoutProbe.Measure`'S REASON,
                // WHICH IS WRITTEN OUT THERE RATHER THAN COPIED HERE. Short version: `PaperKit`'s
                // header states 16 as a deliberate step and `MenuKit.MinReadableUnits` is 18, the
                // two disagree in writing, and § 121.8 is the open entry holding that decision.
                // **Anything off the scale still fails**, including anything between 16 and 18.
                Assert.GreaterOrEqual(label.fontSize, PaperKit.Caption,
                    $"{resolution} custom game: '{label.name}' is {label.fontSize} units, under " +
                    $"the {PaperKit.Caption}-unit floor. docs/TODO.md § 121.8.");

                if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                {
                    Assert.LessOrEqual(label.preferredHeight, rect.height + 1.0f,
                        $"{resolution} custom game: '{label.name}' wraps to " +
                        $"{label.preferredHeight:0} px in a {rect.height:0} px row.");
                }
                else
                {
                    Assert.LessOrEqual(label.preferredWidth, rect.width + 1.0f,
                        $"{resolution} custom game: '{label.name}' reading \"{label.text}\" " +
                        $"needs {label.preferredWidth:0} px and was given {rect.width:0}.");
                }

                measured++;
            }

            return measured;
        }
    }
}
