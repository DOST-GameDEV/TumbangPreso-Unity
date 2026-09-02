using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    /// Phase 7's only surface, at the nine resolutions every other UI probe drives.
    ///
    /// ⚠️⚠️ IT ASSERTS THE FOUR THINGS `FUTURE.md` § 0.5b's PHASE 7 ROW NAMES, RATHER THAN ONLY
    /// THAT THE LABELS FIT. That row's trap is unusually specific and is a list of failures rather
    /// than a principle: *"a spinner is not a state. Say the mode, the time elapsed, and how to
    /// cancel, and never block the menu behind it."* A layout probe that only measured boxes would
    /// pass a card that said SEARCHING and nothing else, which is the exact screen that row
    /// forbids. **`CLAUDE.md` § 6.2: a green layout probe is not a good screen**, and this is the
    /// most that a probe CAN ask about this one.
    ///
    /// ⚠️ THE PICTURE IS STILL OWED AND IS STILL A PERSON'S JOB. `PhotographTheQueue` writes a
    /// PNG to `Logs/ui/` for review, the same way `PlayerHubLayoutProbe.PhotographEveryScreen`
    /// does, because the probe asks whether the screen is a screen and the picture asks whether it
    /// can be read.
    /// </summary>
    public class QueueCardLayoutProbe
    {
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

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            var blank = SceneManager.CreateScene($"QueueCardBlank{Time.frameCount}");
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
        /// Builds the card and drives the matchmaker into a searching state without touching the
        /// network.
        ///
        /// ⚠️⚠️ THE STATE IS SET BY REFLECTION AND `Matchmaker.Start` IS NOT CALLED, AND THE
        /// ALTERNATIVE IS DANGEROUS RATHER THAN MERELY SLOW. `Start` subscribes to the live
        /// browse loop, hosts a Relay lobby when it finds nothing, and publishes this machine into
        /// the real online pool. A layout probe that did that would put a lobby nobody is in on
        /// the service every time the suite runs, and `PhaseSurfaceLayoutProbe`'s XP case records
        /// the same argument about a probe that would have submitted a fabricated match to his
        /// live account.
        ///
        /// ⚠️ THE STRINGS STILL COME OUT OF THE SHIPPING CODE. `QueueCard.Refresh` builds every
        /// one of them from `MatchmakingRules`, so what is measured is the game's own formatting
        /// rather than text this file invented.
        /// </summary>
        private IEnumerator BuildSearchingCard(float elapsed)
        {
            _host = new GameObject("QueueProbeHost");
            var canvas = MenuKit.BuildCanvas(_host.transform, "QueueProbeCanvas");
            yield return null;

            var queue = Net.Matchmaker.Ensure();

            // ⚠️ THE SETTERS ARE PRIVATE, SO THEY ARE FETCHED WITH `GetSetMethod(true)`.
            // `PropertyInfo.SetValue` looks up the PUBLIC setter and throws when there is none,
            // which is a failure that reads like the property having been renamed.
            SetPrivate(queue, "State", Net.QueueState.Searching);
            SetPrivate(queue, "Elapsed", elapsed);

            var card = QueueCard.Build(canvas.transform);
            yield return null;

            var refresh = typeof(QueueCard).GetMethod("Refresh",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(refresh, "QueueCard.Refresh is gone or renamed");
            refresh.Invoke(card, null);

            yield return null;
        }

        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheQueueCardFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();

            yield return BuildSearchingCard(37.0f);

            var canvas = Root("QueueProbeCanvas");
            Assert.IsNotNull(canvas, "the probe built no canvas");

            // ⚠️⚠️ § 114.14, AND THIS FILE IS WHERE IT BELONGS BECAUSE THIS IS THE SCREEN THAT
            // SHIPPED BROKEN. Everything below measures rows against the card; this asks whether
            // the card has anything to be measured against at all, which is the one question that
            // was green through the whole life of § 114.13's fault.
            RectParentage.AssertEveryRectHasARectParent(canvas, "the queue card");

            yield return Drive();

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                int measured = Measure(canvas, name, report);
                Assert.Greater(measured, 0,
                    $"{name}: the queue card drew no labels, so this proves nothing");

                var state = Find(canvas, "QueueState");
                Assert.IsNotNull(state, "the card has no QueueState plate");
                AssertInside((RectTransform)canvas, (RectTransform)state, name, "the queue card");

                report.AppendLine($"{name,-14} {w}x{h}  queue card ok, {measured} labels");
            }

            Debug.Log($"[QueueCardLayoutProbe]\n{report}");
        }

        /// <summary>
        /// ⚠️⚠️ THE FOUR THINGS § 0.5b'S PHASE 7 ROW DEMANDS, AS FOUR ASSERTIONS. Each one is a
        /// screen that has shipped somewhere and been rejected for exactly this.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCardSaysTheModeTheClockTheWayOutAndWhyABadRoundIsSurvivable()
        {
            yield return BuildSearchingCard(37.0f);

            var canvas = Root("QueueProbeCanvas");
            var everything = new StringBuilder();

            foreach (var label in canvas.GetComponentsInChildren<Text>(true))
                everything.Append(label.text).Append('\n');

            string text = everything.ToString();

            // 1. The mode.
            StringAssert.Contains(MenuKit.ModeLabel(GameMode.Classic), text,
                "the queue card never says which mode it is queueing for. A player who queued " +
                "from the wrong tab finds out at the character select. FUTURE.md § 0.5b, phase 7.");

            // 2. The time elapsed, which is what makes it a state rather than a spinner.
            StringAssert.Contains("37s", text,
                "the queue card does not say how long it has been searching. A spinner is not a " +
                "state: after forty seconds it is indistinguishable from a frozen game.");

            // 3. The widening, in words, so a long queue reads as progress.
            StringAssert.Contains(MatchmakingRules.SearchLabel(1500, 37.0f), text,
                "the card does not say what band it is searching, so the widening is invisible " +
                "and the bar underneath it is decoration. FUTURE.md § 7.");

            // 4. How to cancel.
            var cancel = Find(canvas, "CancelQueueButton");
            Assert.IsNotNull(cancel,
                "the queue card has no way out. CLAUDE.md § 6.3: a dead end is a bug, and this " +
                "is a screen a player is looking at precisely because they are waiting.");
            Assert.IsTrue(cancel.GetComponent<Button>().interactable, "CANCEL is not pressable");

            // 5. And the sentence the game has never said out loud.
            StringAssert.Contains("everyone defends exactly once", text,
                "the queue does not tell the player that the taya rotates. FUTURE.md § 7 asks " +
                "for it by name and INSPIRATION.md § 4.5 is titled 'the taya rotation is a gift " +
                "and nobody knows it'. It is why a bad first round is not a lost match.");
        }

        /// <summary>
        /// ⚠️⚠️ THE CARD MUST NOT BLOCK THE MENU BEHIND IT, WHICH IS THE OTHER HALF OF § 0.5b'S
        /// TRAP AND IS INVISIBLE TO EVERY OTHER ASSERTION IN THIS FILE. A full-screen scrim is
        /// what `LobbyJoinPanel` has and is right there; copying it here would eat every click on
        /// the lobby while somebody waits, including the chat they are talking in and the join
        /// code they are reading out. `CLAUDE.md` § 6.2c question 4: anything covering the screen
        /// is also eating clicks.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCardBlocksItsOwnRectangleAndNothingElse()
        {
            yield return BuildSearchingCard(5.0f);

            var canvas = Root("QueueProbeCanvas");
            yield return Drive();
            yield return Resize(1920, 1080);

            var canvasRect = (RectTransform)canvas;

            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget) continue;

                var rect = graphic.rectTransform.rect;
                float area = rect.width * rect.height;
                float screen = canvasRect.rect.width * canvasRect.rect.height;

                Assert.Less(area, screen * 0.5f,
                    $"'{graphic.name}' is a raycast target covering {area / screen:P0} of the " +
                    "canvas. The queue card is a plate in a corner, not a modal: a scrim here " +
                    "would swallow every click on the lobby underneath while somebody waits. " +
                    "docs/TODO.md § 103.3.");
            }
        }

        /// <summary>
        /// ⚠️ THE DOOR AND THE CARD ARE TWO STATES OF ONE CONTROL AND ARE NEVER BOTH ON SCREEN.
        /// A QUICK MATCH button drawn over a card that says SEARCHING is a second door to a place
        /// the player is already standing in, which is the fault `FUTURE.md` § 0.5b bans by name.
        /// </summary>
        [UnityTest]
        public IEnumerator TheDoorAndTheCardAreNeverBothOnScreen()
        {
            _host = new GameObject("QueueProbeHost");
            var canvas = MenuKit.BuildCanvas(_host.transform, "QueueProbeCanvas");
            yield return null;

            var queue = Net.Matchmaker.Ensure();

            var card = QueueCard.Build(canvas.transform);
            var refresh = typeof(QueueCard).GetMethod("Refresh",
                BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var value in new[] { Net.QueueState.Idle, Net.QueueState.Searching,
                                          Net.QueueState.Joining, Net.QueueState.Cancelled })
            {
                SetPrivate(queue, "State", value);
                refresh.Invoke(card, null);
                yield return null;

                var door = Find(canvas.transform, "QuickMatchButton");
                var plate = Find(canvas.transform, "QueueState");

                Assert.IsNotNull(door, "the QUICK MATCH door is gone");
                Assert.IsNotNull(plate, "the queue card is gone");

                // ⚠️⚠️ THE INVARIANT IS "NOT TWO LIVE CONTROLS", NOT "NOT TWO ACTIVE OBJECTS",
                // AND THE OLD WORDING FAILED THE SHIPPED DESIGN. `QueueCard.Refresh`'s own header
                // says why: **the door is DIMMED, not hidden**, because QUICK MATCH shares a
                // `HorizontalLayoutGroup` with JOIN and CHAT and taking it off the row makes the
                // other two jump sideways under the player's hand the instant it is pressed. And
                // in the un-docked path the geometry settles it outright: the door is 64 units
                // tall centred at `DoorCentreY` 96, so it spans 64 to 128, and the card is 348
                // tall centred at 238, so it spans 64 to 412. **The card covers the door
                // completely and is built after it, so it draws on top.**
                //
                // ⚠️ `activeSelf` WAS THEREFORE ASKING THE WRONG QUESTION: an object that is
                // covered by an opaque plate and cannot be pressed is not "on screen" in any sense
                // a player would recognise, which is exactly the reading `docs/TODO.md` § 120.9
                // flagged and could not settle without a picture. 🧑 2026-09-02, on this class of
                // failure: *"u can change the tests to fit our shit bcz they might be stale af"*.
                //
                // **What must never happen is two controls a player can press that mean opposite
                // things**, which is what this asserts now.
                bool doorIsLive = door.gameObject.activeInHierarchy
                                  && door.GetComponent<Button>() != null
                                  && door.GetComponent<Button>().interactable;

                Assert.IsFalse(doorIsLive && plate.gameObject.activeSelf,
                    $"in state {value} the queue card is up and its own door is still pressable, "
                    + "so the player can join a queue they are already in.");

                Assert.IsTrue(door.gameObject.activeSelf || plate.gameObject.activeSelf,
                    $"in state {value} the queue has no visible control at all, which is a " +
                    "feature nobody can find. CLAUDE.md § 6.3.");
            }
        }

        /// <summary>Writes a PNG for a person to look at. ⚠️ THE PROBE ASKS WHETHER THE SCREEN IS
        /// A SCREEN; THE PICTURE ASKS WHETHER IT CAN BE READ.</summary>
        [UnityTest]
        public IEnumerator PhotographTheQueue()
        {
            yield return BuildSearchingCard(37.0f);
            yield return Drive();

            // ⚠️ THE SHAPE HE ACTUALLY PLAYS AT, not only 16:9. `CLAUDE.md` § 6.2b: `Fullscreen`
            // is false in his settings and all nine probe resolutions are taller than his window.
            yield return Resize(1600, 720);

            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "ui");
            System.IO.Directory.CreateDirectory(dir);

            var shot = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            _camera.Render();
            shot.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            string path = System.IO.Path.Combine(dir, "queue_card_v1.png");
            System.IO.File.WriteAllBytes(path, shot.EncodeToPNG());
            Debug.Log($"[QueueCardLayoutProbe] wrote {path}");

            Object.Destroy(shot);
            yield return null;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ ONE HELPER FOR BOTH PRIVATE SETTERS, so a renamed property fails once with a
        /// sentence rather than three times with a null reference.
        /// </summary>
        private static void SetPrivate(object target, string property, object value)
        {
            var info = target.GetType().GetProperty(property,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(info, $"Matchmaker.{property} is gone or renamed");

            var setter = info.GetSetMethod(nonPublic: true);
            Assert.IsNotNull(setter, $"Matchmaker.{property} has no setter at all any more");

            setter.Invoke(target, new[] { value });
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

        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            for (int i = 0; i < 3; i++) yield return null;
        }

        private static Transform Root(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        private static Transform Find(Transform scope, string name)
        {
            foreach (var t in scope.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static int Measure(Transform root, string resolution, StringBuilder report)
        {
            int measured = 0;

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;

                var rect = label.rectTransform.rect;
                if (rect.width <= 1.0f) continue;

                // ⚠️⚠️ THE FLOOR IS `PaperKit.Caption`, NOT `MinReadableUnits`, AND THE REASONING
                // IS WRITTEN OUT IN `PlayerHubLayoutProbe.Measure` RATHER THAN COPIED HERE. The
                // short version: `PaperKit`'s header states 16 as a deliberate step for a
                // restatement, this file asserted 18, and the two disagreed in writing while
                // `QueueCard.Row`'s three `PaperKit.Caption` call sites were byte-identical for a
                // month (`docs/TODO.md` § 120.9). **Anything off the four-step scale still fails**,
                // including anything between 16 and 18, which is what a hand-typed size looks like.
                Assert.GreaterOrEqual(label.fontSize, PaperKit.Caption,
                    $"{resolution} queue: '{label.name}' is {label.fontSize} units, under the " +
                    $"{PaperKit.Caption}-unit floor of the paper type scale. docs/TODO.md § 121.8.");

                Assert.IsFalse(label.fontSize > PaperKit.Caption
                               && label.fontSize < MenuKit.MinReadableUnits,
                    $"{resolution} queue: '{label.name}' is {label.fontSize} units, which is not " +
                    "a step on the scale.");

                // ⚠️ A WRAPPING LABEL IS MEASURED VERTICALLY AND NOT HORIZONTALLY, WHICH IS
                // § 102.4 APPLIED RATHER THAN QUOTED. A wrapped label's preferred WIDTH is inside
                // its box by definition, so every horizontal check skips it and the overflow that
                // actually happens is a second line drawn below the row. The taya sentence on
                // this card is exactly that shape.
                if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                {
                    Assert.LessOrEqual(label.preferredHeight, rect.height + 1.0f,
                        $"{resolution} queue: '{label.name}' wraps to " +
                        $"{label.preferredHeight:0} px in a {rect.height:0} px row, so its last " +
                        "line draws below its own box. docs/TODO.md § 102.4.");
                }
                else
                {
                    Assert.LessOrEqual(label.preferredWidth, rect.width + 1.0f,
                        $"{resolution} queue: '{label.name}' reading \"{label.text}\" needs " +
                        $"{label.preferredWidth:0} px and was given {rect.width:0}.");
                }

                measured++;
            }

            report.AppendLine($"  measured {measured}");
            return measured;
        }

        private static void AssertInside(RectTransform canvas, RectTransform target,
                                         string resolution, string what)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var bounds = new Vector3[4];
            canvas.GetWorldCorners(bounds);

            for (int i = 0; i < 4; i++)
            {
                Assert.GreaterOrEqual(corners[i].x, bounds[0].x - 1.0f,
                    $"{resolution}: {what} runs off the left edge");
                Assert.LessOrEqual(corners[i].x, bounds[2].x + 1.0f,
                    $"{resolution}: {what} runs off the right edge");
                Assert.GreaterOrEqual(corners[i].y, bounds[0].y - 1.0f,
                    $"{resolution}: {what} runs off the bottom edge");
                Assert.LessOrEqual(corners[i].y, bounds[2].y + 1.0f,
                    $"{resolution}: {what} runs off the top edge");
            }
        }
    }
}
