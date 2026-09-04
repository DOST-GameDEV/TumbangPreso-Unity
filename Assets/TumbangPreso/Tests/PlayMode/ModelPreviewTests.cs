using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The CHARACTER screen's model preview: framed to the panel, posed, and movable.
    ///
    /// ⚠️⚠️ ALL FOUR OF THESE SHIPPED BROKEN TOGETHER AND NONE OF THEM FAILED ANY CHECK. The
    /// report was *"model isnt movable and its stretched"*, against a screen whose own hint line
    /// reads "Drag to turn the view · scroll to zoom · right-click to reset":
    ///
    ///  1. Nothing in the project ever called `Orbit` or `Zoom`, so all three controls were lies.
    ///  2. The render target was a fixed 512x640 stretched across a panel of another shape.
    ///  3. The framing fitted height alone and ignored the aspect entirely.
    ///  4. No clip was playing, so the subject stood in its bind pose with its arms straight out.
    ///
    /// The assertions below are the mechanism rather than the picture, because a picture cannot
    /// fail a build. The pictures are written beside them for the reader.
    /// </summary>
    public class ModelPreviewTests
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string OutDir = "Logs/shots-preview";

        [UnityTest]
        public IEnumerator TheCharacterPreviewIsFramedPosedAndMovable()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");

            panel.SetActive(true);

            // The preview builds its target from the panel's rect, which is 0 until the first
            // layout pass, and re-frames on the frame after a subject is shown.
            for (int i = 0; i < 20; i++) yield return null;

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "The character panel built no ModelPreview.");

            Assert.IsNotNull(preview.Subject,
                "Nothing was instanced to look at. The roster book has no model for this pick.");

            // ---- 2. THE PICTURE IS NOT SQUASHED --------------------------------------------
            var rect = ((RectTransform)preview.transform).rect;
            Assert.IsNotNull(preview.Target, "The preview has no render target.");

            float panelAspect = rect.width / rect.height;
            float targetAspect = (float)preview.Target.width / preview.Target.height;

            Assert.AreEqual(panelAspect, targetAspect, 0.02f,
                $"The render target is {preview.Target.width}x{preview.Target.height} on a " +
                $"{rect.width:F0}x{rect.height:F0} panel, so every subject is stretched by the " +
                "ratio between them.");

            // ---- 4. THE SUBJECT IS POSED, NOT IN ITS BIND POSE ------------------------------
            var animator = preview.Subject.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator,
                "No Animator on the previewed model, so no clip can be playing and the screen " +
                "shows the rig's T-pose.");

            // ⚠️ THE POSE MUST ACTUALLY MOVE. An Animator with a null Avatar accepts an
            // animation output and drives nothing, so "there is an Animator" and "a clip is
            // playing" are different claims and only the second one matters. The rig's bind
            // pose is arms straight out, which is the T-pose the screen must never show.
            var bone = DeepestChild(preview.Subject.transform);
            Quaternion pose = bone.localRotation;

            for (int i = 0; i < 30; i++) yield return null;

            Assert.Greater(Quaternion.Angle(pose, bone.localRotation), 0.01f,
                $"'{bone.name}' has not moved in 30 frames, so no clip is playing and the " +
                "preview is showing the rig's bind pose.");

            Capture("character-person");

            // ---- 1. A DRAG REACHES IT ------------------------------------------------------
            var input = preview.GetComponentInChildren<ModelPreviewInput>(true);
            Assert.IsNotNull(input,
                "The preview surface has no input component, so the panel's own hint line " +
                "promises three controls that do not exist.");

            var camera = preview.PreviewCamera;
            Assert.IsNotNull(camera);

            Quaternion before = camera.transform.rotation;

            input.OnDrag(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                delta = new Vector2(90.0f, 0.0f),
            });

            yield return null;

            Assert.Greater(Quaternion.Angle(before, camera.transform.rotation), 1.0f,
                "Dragging the preview did not move the camera.");

            Capture("character-dragged");

            // ---- ZOOM ----------------------------------------------------------------------
            float distance = Vector3.Distance(camera.transform.position, Vector3.zero);

            input.OnScroll(new PointerEventData(EventSystem.current)
            {
                scrollDelta = new Vector2(0.0f, 1.0f),
            });

            yield return null;

            Assert.AreNotEqual(distance,
                Vector3.Distance(camera.transform.position, Vector3.zero),
                "The wheel did not dolly the preview camera.");

            // ---- RIGHT-CLICK RESTORES THE FRAMED SHOT --------------------------------------
            input.OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right,
            });

            yield return null;

            // ⚠️ A FEW DEGREES OF SLACK, ON PURPOSE. The framing is re-measured from the POSED
            // bounds, and the idle clip is still running, so the subject's height-to-width ratio
            // drifts slightly between frames and the pitch lerp follows it. The claim being made
            // is "right-click puts the shot back", not "to the bit".
            Assert.Less(Quaternion.Angle(before, camera.transform.rotation), 5.0f,
                "Right-click did not restore the auto-framed shot.");

            Capture("character-reset");
        }

        /// <summary>
        /// The cast animates in a real match.
        ///
        /// ⚠️⚠️ "THE WHOLE CAST STANDS PERFECTLY STILL" HAS BITTEN THIS PORT TWICE, once because
        /// the clips were stripped from the build and once because glTFast emits an Animator
        /// with no Avatar and an animation output bound to one drives nothing at all. Neither
        /// failure logs anything: the characters simply stand in their bind pose, which on these
        /// rigs is arms out, and it reads as unfinished art rather than as a bug. This asserts
        /// the only thing that actually distinguishes the two: a bone that moves.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCastAnimatesInAMatch()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var seat = Object.FindFirstObjectByType<CharacterMotor>();
            Assert.IsNotNull(seat, "The arena built no seats.");

            var skinned = seat.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(skinned, "The seat has no skinned model.");

            var animator = seat.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "The seat's model has no Animator.");

            Assert.IsNotNull(animator.avatar,
                "The Animator has no Avatar, so its animation output binds to nothing and the " +
                "character stands in its bind pose for the whole match.");

            // ⚠️ A BONE, NOT MERELY THE DEEPEST CHILD.  parks a HandAnchor
            // under the hand bone, and that node is deeper than any bone and never rotates on
            // its own: sampling it asserts that a static child is static, which it always is.
            var bone = DeepestBone(skinned);
            Assert.IsNotNull(bone, "The skinned renderer has no bones.");

            Quaternion pose = bone.localRotation;

            for (int i = 0; i < 40; i++) yield return null;

            Assert.Greater(Quaternion.Angle(pose, bone.localRotation), 0.01f,
                $"'{bone.name}' has not moved in 40 frames of a live match.");
        }

        /// <summary>
        /// The REPLACED character on the CHARACTER screen: the new mesh, posed, in its palette.
        ///
        /// ⚠️⚠️ THE SELECT SCREEN IS A SECOND CONSUMER OF THE ROSTER ART AND IT FAILS
        /// DIFFERENTLY FROM THE MATCH. Both resolve a pick through `RosterBook`, so a swapped
        /// model reaches both for free, but the screen instances the prefab ITSELF rather than
        /// going through `CharacterVisual`: it applies its own scale, its own yaw and its own
        /// `ToonSkin` call, and it plays the idle clip through `PlayIdle` instead of the
        /// Playables graph. So "it works in a match" is not evidence that it works here, and the
        /// screen is where a player meets a character first.
        ///
        /// 🧑 2026-08-17, on the first swapped rig: *"i want u to make sure the mdoel works
        /// everuywhere even in char select"*.
        ///
        /// ⚠️ IT ASSERTS THE PALETTE IS LIVE, not merely supplied. `_UsePalette` is the flag
        /// `ToonSkin` sets only when it is handed sixteen colours, and with it at zero the
        /// character renders in the atlas's stock colours while every name and meter around it
        /// stays correct. That is the exact failure mode the cast sheet was hiding.
        /// </summary>
        [UnityTest]
        public IEnumerator TheReplacedCharacterShowsOnTheSelectScreen()
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");

            panel.SetActive(true);

            for (int i = 0; i < 20; i++) yield return null;

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "The character panel built no ModelPreview.");

            var book = RosterBook.Load();
            Assert.IsNotNull(book, "No RosterBook. Run RosterBookBuilder.Build.");

            foreach (var (replacedId, replacedMesh) in HeroCharacters)
            {
                int index = Core.Roster.IndexIn(Core.Roster.People, replacedId);
                Assert.GreaterOrEqual(index, 0, $"No roster entry '{replacedId}'.");

                var art = book.PersonArt(index);
                Assert.IsNotNull(art, $"The roster book has no art for '{replacedId}'.");

                Assert.IsNotNull(art.Model, $"'{replacedId}' has no model.");
                Assert.AreEqual(replacedMesh, art.Model.name,
                    $"The select screen would show the retired CC0 rig for {replacedId}.");

                Assert.IsNotNull(art.Palette);
                Assert.AreEqual(16, art.Palette.Length,
                    "A short palette reads past its end for whichever slot is missing.");

                preview.Show(art.Model, art.Clips, art.Palette);

                for (int i = 0; i < 20; i++) yield return null;

                Assert.IsNotNull(preview.Subject, "Nothing was instanced to look at.");

                var skinned = preview.Subject.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsNotNull(skinned, "The previewed model has no skinned mesh.");

                // The two bones the game hunts by name, checked HERE as well as in the probe because
                // this is the path that runs in a build.
                foreach (string wanted in new[] { "arm-right", "head" })
                {
                    bool found = false;
                    foreach (var b in skinned.bones) found |= b != null && b.name == wanted;

                    Assert.IsTrue(found, $"The previewed rig has no '{wanted}' bone.");
                }

                var poses = new System.Collections.Generic.Dictionary<Transform, Quaternion>();
                foreach (var b in skinned.bones)
                {
                    if (b != null) poses[b] = b.localRotation;
                }

                for (int i = 0; i < 30; i++) yield return null;

                float maxAngle = 0.0f;
                foreach (var kvp in poses)
                {
                    float angle = Quaternion.Angle(kvp.Value, kvp.Key.localRotation);
                    if (angle > maxAngle) maxAngle = angle;
                }

                Assert.Greater(maxAngle, 0.01f,
                    "No bone in the rig has moved in 30 frames, so the select screen is showing " +
                    "this character's bind pose.");

                bool palettedAny = false;

                foreach (var r in preview.Subject.GetComponentsInChildren<Renderer>())
                {
                    foreach (var material in r.sharedMaterials)
                    {
                        if (material == null || !material.HasProperty("_UsePalette")) continue;
                        palettedAny |= material.GetFloat("_UsePalette") > 0.5f;
                    }
                }

                Assert.IsTrue(palettedAny,
                    "No material on the preview has the palette switched on, so this character is " +
                    "wearing the atlas's stock colours on the screen the player picks from.");

                Capture($"character-replaced-{replacedId}");
            }
        }

        /// <summary>The Hero Strike cast and the custom meshes they alone must wear.</summary>
        private static readonly (string Id, string Mesh)[] HeroCharacters = new[]
        {
            ("dante", "team-dante"),
            ("cheska", "team-cheska"),
            ("sean", "team-sean"),
            ("zack", "team-zack"),
            ("nemu", "team-nemu"),
        };

        [UnityTest]
        public IEnumerator ClassicCharacterSelectDrawsTheGodotCastAndBackdrop()
        {
            Directory.CreateDirectory(OutDir);
            Settings.SettingsStore.Current.CharacterPick = 0;
            UI.SceneFlow.SelectedMode = Core.GameMode.Classic;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");
            panel.SetActive(false);
            panel.SetActive(true);
            for (int i = 0; i < 30; i++) yield return null;

            var name = FindIn(panel.transform, "CharValueLabel")?.GetComponent<UnityEngine.UI.Text>();
            Assert.IsNotNull(name, "Classic select has no character name label.");
            Assert.AreEqual("BERTO", name.text, "Classic select did not open on the Classic roster.");

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview?.Subject, "Classic select built no preview subject.");
            StringAssert.AreEqualIgnoringCase("character-male-f(Clone)", preview.Subject.name,
                "Classic index zero is not the Godot BERTO model.");

            var backdrop = FindIn(panel.transform, "Backdrop")?.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(backdrop, "Classic select has no backdrop image.");
            Assert.IsNotNull(backdrop.sprite,
                "The Godot slate-to-midnight gradient was flattened to a solid colour.");

            Capture("character-classic-godot");
            UI.SceneFlow.SelectedMode = Core.GameMode.HeroStrike;
        }

        [UnityTest]
        public IEnumerator HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes()
        {
            Settings.SettingsStore.Current.CharacterPick = 0;
            UI.SceneFlow.SelectedMode = Core.GameMode.HeroStrike;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");
            panel.SetActive(false);
            panel.SetActive(true);
            for (int i = 0; i < 20; i++) yield return null;

            var rows = FindIn(panel.transform, "TraitRows");
            Assert.IsNotNull(rows, "Hero select has no loadout area.");

            string copy = string.Empty;
            foreach (var label in rows.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                copy += label.text + "\n";

            // ⚠️⚠️ THE NAMES ARE ASKED OF THE KIT, NOT SPELLED OUT HERE. This used to hard-code
            // three strings and went red the day an ability was renamed, which is a test failing
            // for a reason that has nothing to do with what it is checking. What it is actually
            // asserting is that a player choosing a hero can see the WHOLE kit named on this
            // screen rather than one third of it, and that survives any amount of renaming.
            //
            // ⚠️ IT IS ALSO WHY THE PICKER LISTS ALL THREE. An earlier pass showed a ribbon of
            // three glyphs with a details card under it carrying only the SELECTED power, so two
            // of every hero's three abilities were invisible until clicked. On the one screen
            // whose entire job is "what does this hero do", that is the wrong trade.
            var danteKit = Abilities.HeroAbilitySystem.CreateKitFor("dante");

            StringAssert.Contains(danteKit.Skill1.Name, copy,
                "Dante's first skill is not named on the Hero picker.");
            StringAssert.Contains(danteKit.Skill2.Name, copy,
                "Dante's second skill is not named on the Hero picker.");
            StringAssert.Contains(danteKit.Ultimate.Name, copy,
                "Dante's ultimate is not named on the Hero picker.");
            StringAssert.Contains(danteKit.Skill1.Summary, copy,
                "The selected power is named but never explained.");
            StringAssert.DoesNotContain("SPEED", copy,
                "Hero select still exposes Classic SPEED attributes.");
            StringAssert.DoesNotContain("POWER", copy,
                "Hero select still exposes Classic POWER attributes.");
            StringAssert.DoesNotContain("GRIT", copy,
                "Hero select still exposes Classic GRIT attributes.");

            Capture("character-hero-abilities");
        }

        [Test]
        public void ClassicRosterUsesTheApprovedClassicRigsAndNoHeroMeshes()
        {
            var book = RosterBook.Load();
            Assert.IsNotNull(book, "No RosterBook. Run RosterBookBuilder.Build.");

            var expected = new[]
            {
                "character-male-f", "character-female-f",
                "character-male-a", "character-female-a",
                "character-male-b", "character-female-b",
                "character-male-c", "character-female-c",
                "character-male-d", "character-female-d",
                "character-male-e", "character-female-e",
            };

            Assert.AreEqual(expected.Length, Core.Roster.ClassicPeople.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                var art = book.PersonArt(i, Core.GameMode.Classic);
                Assert.IsNotNull(art, $"Classic index {i} has no art entry.");
                Assert.IsNotNull(art.Model, $"Classic index {i} has no model.");
                Assert.AreEqual(expected[i], art.Model.name,
                    $"{Core.Roster.ClassicPeople[i].Name} is not using the Godot Classic rig.");
                StringAssert.DoesNotStartWith("team-", art.Model.name,
                    "A Hero Strike mesh leaked into the Classic cast.");
            }
        }

        /// <summary>The bone furthest down the rig, so the sample is a limb rather than the
        /// root the clip may deliberately leave still.</summary>
        private static Transform DeepestBone(SkinnedMeshRenderer skinned)
        {
            Transform best = null;
            int depth = -1;

            foreach (var bone in skinned.bones)
            {
                if (bone == null) continue;

                int d = 0;
                for (var step = bone; step != null; step = step.parent) d++;

                if (d <= depth) continue;

                depth = d;
                best = bone;
            }

            return best;
        }

        /// <summary>A bone well down the rig, so the sample is a limb rather than the root the
        /// clip may deliberately leave still.</summary>
        private static Transform DeepestChild(Transform root)
        {
            Transform best = root;
            int depth = 0;

            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                int d = 0;
                for (var step = t; step != root && step != null; step = step.parent) d++;

                if (d <= depth) continue;

                depth = d;
                best = t;
            }

            return best;
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ AN OVERLAY CANVAS IS INVISIBLE TO Camera.Render, so it is flipped to
        /// ScreenSpaceCamera first and put in front of the near plane. Same rule UiRuntimeShots
        /// carries, and the reason a capture can come back as an empty scene.
        /// </summary>
        private static void Capture(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }

            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
