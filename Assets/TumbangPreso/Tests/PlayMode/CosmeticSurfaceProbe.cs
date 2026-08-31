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
    /// The two surfaces Phase 5 produced that a player can press or read: the COLOURS row on the
    /// character picker, and the banner title on a lobby plate. `docs/TODO.md` § 101.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `FUTURE.md` § 0.5b SAYS A PHASE MAY NOT CALL A SCREEN DONE WITHOUT
    /// ONE, AND BECAUSE THE SAME SECTION SAYS A GREEN PROBE IS NOT A GOOD SCREEN. So this does
    /// both halves it can do: it asserts the control exists, is on screen and clears the
    /// 18-unit type floor, **and it writes a PNG for a person to look at**. The half it cannot do
    /// is the judgement, which is `CLAUDE.md` § 6.2c's four questions and a pair of eyes.
    ///
    /// ⚠️⚠️ THE PROFILE IS SEEDED IN MEMORY AND PUT BACK IN TEARDOWN. The editor and the built
    /// player share `Application.persistentDataPath`, so `career.json` is literally the file he
    /// plays with; `CloudEndpointActionProbe`'s header records the same hazard one service
    /// further out. **Nothing here saves and nothing here submits**, and the mastery list is
    /// restored even when a case fails.
    ///
    /// ⚠️ AND THE ROW ONLY EXISTS WHEN SOMETHING IS OWNED, WHICH IS WHY THE SEED IS THE FIRST
    /// STEP RATHER THAN A CONVENIENCE. A probe that opened the picker on a fresh account would
    /// find no COLOURS row and would be correct to.
    /// </summary>
    public class CosmeticSurfaceProbe
    {
        private const string ShotDir = "Logs/ui";

        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private List<MasteryRecord> _mastery;

        /// <summary>
        /// ⚠️⚠️ THE WHOLE LOADOUT LIST, NOT ONE CHARACTER'S ENTRY, AND THE FIRST VERSION OF THIS
        /// PROBE WROTE INTO HIS REAL `settings.json` BECAUSE IT SAVED ONE. `SettingsStore.
        /// SetPaletteFor` **saves**, deliberately — a cosmetic choice lost on quit is worse than
        /// one you cannot make — and the editor shares `Application.persistentDataPath` with the
        /// built player, so the file this probe wrote is the file he plays with. It restored
        /// `dante` and the picker had recoloured `berto`, so a palette he never chose survived the
        /// run. **A probe that presses a button that persists has to put the whole store back**,
        /// not the row it expected to be touched. `CloudEndpointActionProbe` carries the same
        /// lesson one service further out.
        /// </summary>
        private List<CharacterLoadout> _loadouts;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();

            // ⚠️ THE CAREER GOES BACK EXACTLY AS IT WAS. See the class header: this is his real
            // profile object in the editor's process.
            var profile = GameServices.Career?.Profile;
            if (profile != null && _mastery != null) profile.Mastery = _mastery;

            if (_loadouts != null)
            {
                var settings = Settings.SettingsStore.Current;
                if (settings != null)
                {
                    settings.CharacterLoadouts = _loadouts;
                    Settings.SettingsStore.Save();
                }
                _loadouts = null;
            }

            yield return null;
        }

        /// <summary>
        /// ⚠️⚠️ MASTERY 15 ON ZACK, WHICH IS BOTH PALETTES. Seeding one would draw DEFAULT plus
        /// one swatch, and a row of two proves nothing about the spacing of a row of three.
        /// </summary>
        private void SeedTwoPalettes()
        {
            var profile = GameServices.Career?.Profile;
            Assert.IsNotNull(profile, "no career profile, so nothing owns a palette to draw");

            _mastery = profile.Mastery;
            SnapshotLoadouts();

            profile.Mastery = new List<MasteryRecord>
            {
                new MasteryRecord { Id = "zack", Level = 15 },
            };

            var owned = BannerRules.Earned(profile);
            int palettes = 0;
            foreach (var reward in owned)
                if (reward.Kind == RewardKind.Palette) palettes++;

            Assert.AreEqual(2, palettes,
                "zack mastery 15 no longer pays two palettes, so this probe is seeding against " +
                "a table that has moved. ProgressionRules.MasteryTable is the list.");
        }

        /// <summary>⚠️ A COPY, NOT THE LIVE LIST. Handing the same `List` back in teardown would
        /// restore whatever the probe appended to it.</summary>
        private void SnapshotLoadouts()
        {
            var settings = Settings.SettingsStore.Current;
            if (settings?.CharacterLoadouts == null) { _loadouts = new List<CharacterLoadout>(); return; }

            _loadouts = new List<CharacterLoadout>(settings.CharacterLoadouts.Count);

            foreach (var row in settings.CharacterLoadouts)
                if (row != null)
                    _loadouts.Add(new CharacterLoadout
                    {
                        CharacterId = row.CharacterId,
                        PaletteId = row.PaletteId,
                    });
        }

        /// <summary>
        /// ⚠️⚠️ THE ASSERTION THAT WOULD HAVE CAUGHT § 101.1 FROM THE UI SIDE. Before the fix,
        /// `BannerRules.Earned` answered two palettes and `PaletteRules.IsKnownVariant` refused
        /// both, so `RefreshPaletteRow` filtered the list to nothing and drew no row at all — on
        /// an account that owns two. **The feature would have looked unbuilt rather than broken**,
        /// which is the failure mode § 91.8 is about.
        /// </summary>
        [UnityTest]
        public IEnumerator TheColoursRowDrawsOneSwatchPerOwnedPaletteAndOneForDefault()
        {
            var report = new StringBuilder();

            SeedTwoPalettes();
            yield return OpenThePicker(report);

            var row = Find("PaletteRow");
            Assert.IsNotNull(row,
                "the picker drew no COLOURS row for an account that owns two palettes. Either " +
                "the ownership check refused an id it should know (docs/TODO.md § 101.1) or the " +
                "row is no longer built in RefreshTraits.");

            int swatches = 0;
            foreach (Transform child in row.transform)
                if (child.name.StartsWith("Swatch_")) swatches++;

            Assert.AreEqual(3, swatches,
                $"the COLOURS row drew {swatches} swatches. Two owned palettes plus DEFAULT is " +
                "three, and DEFAULT is not optional: without it there is no way back from a " +
                "variant, which CLAUDE.md 6.3 calls a dead end.");

            // ⚠️ THE CAPTION CLEARS THE FLOOR LIKE EVERY OTHER LABEL IN THE GAME.
            // `MenuKit.MinReadableUnits` is 18 and `PlayerHubLayoutProbe` records what failing it
            // cost the first build of the hub screens.
            var caption = row.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(caption, "the COLOURS row has no caption");
            Assert.GreaterOrEqual(caption.fontSize, MenuKit.MinReadableUnits,
                $"the COLOURS caption is {caption.fontSize} units, under the {MenuKit.MinReadableUnits} floor.");

            report.AppendLine($"colours row: {swatches} swatches, caption {caption.fontSize}u");

            // ⚠️⚠️ THE SWATCHES ARE NOT ALL THE SAME COLOUR, WHICH IS THE ASSERTION THE WHOLE
            // CONTROL TURNS ON. `RepresentativeSlot` picks by saturation precisely because slot 0
            // is an off-white on several of the cast, and three identical grey squares is a
            // control that looks broken rather than subtle. **A row of swatches that cannot be
            // told apart is worse than no row.**
            var colours = new List<Color>();
            foreach (Transform child in row.transform)
            {
                if (!child.name.StartsWith("Swatch_")) continue;
                var image = child.GetComponent<Image>();
                if (image != null) colours.Add(image.color);
            }

            for (int i = 0; i < colours.Count; i++)
                for (int j = i + 1; j < colours.Count; j++)
                {
                    float apart = Mathf.Abs(colours[i].r - colours[j].r)
                                + Mathf.Abs(colours[i].g - colours[j].g)
                                + Mathf.Abs(colours[i].b - colours[j].b);

                    Assert.Greater(apart, 0.05f,
                        $"swatch {i} and swatch {j} are the same colour ({colours[i]}). The " +
                        "variants are hue rotations of one slot, so this means the slot the " +
                        "swatch samples carries no hue: see ConvertedCharacterSelect." +
                        "RepresentativeSlot.");
                }

            yield return Shoot("10-picker-colours");

            // ⚠️ AND THE PRESSED STATE, because a control with no visible selection is a control
            // a player presses twice. The ring is a separate object precisely so this can be
            // asserted without measuring the swatch's own colour.
            foreach (Transform child in row.transform)
            {
                if (!child.name.StartsWith("Swatch_") || child.name.EndsWith("default")) continue;

                var button = child.GetComponent<Button>();
                Assert.IsNotNull(button, $"{child.name} is not pressable");
                button.onClick.Invoke();
                break;
            }

            for (int i = 0; i < 4; i++) yield return null;

            yield return Shoot("11-picker-colours-picked");

            Debug.Log($"[Cosmetics]\n{report}");
        }

        /// <summary>
        /// ⚠️⚠️ THE EMPTY STATE, WHICH `FUTURE.md` § 0.5b QUESTION 3 SAYS GETS DESIGNED RATHER
        /// THAN LEFT OVER, AND WHICH IS THE STATE EVERY NEW PLAYER IS IN. A fresh account owns no
        /// palette, so there must be no COLOURS row at all: a caption with one dead swatch beside
        /// it is the fifteen rows of `0/0 (needs 10 throws)` that taught a new player the game was
        /// broken (`docs/TODO.md` § 92.1 fault 4).
        ///
        /// ⚠️ AND IT PHOTOGRAPHS THE COLUMN WITHOUT THE ROW, so the two shots in `Logs/ui/` are a
        /// before and after of the same screen. `CLAUDE.md` § 6.2b's first row: every state, not
        /// the one you built first.
        /// </summary>
        [UnityTest]
        public IEnumerator AFreshAccountGetsNoColoursRowAtAll()
        {
            var report = new StringBuilder();

            var profile = GameServices.Career?.Profile;
            Assert.IsNotNull(profile, "no career profile to empty");

            _mastery = profile.Mastery;
            SnapshotLoadouts();
            profile.Mastery = new List<MasteryRecord>();

            yield return OpenThePicker(report);

            Assert.IsNull(Find("PaletteRow"),
                "an account that owns no palette was still shown a COLOURS row. A control whose " +
                "only option is the one you already have is not a control.");

            yield return Shoot("12-picker-no-colours");

            Debug.Log($"[Cosmetics] empty state {report}");
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE THING A LAYOUT PROBE CAN SAY ABOUT THE LOBBY PLATE: the title strip and
        /// the TAYA FIRST strip do not overlap. They stack, and `SetSeat` computes the tag's
        /// offset from whether the title is showing rather than from a second constant, which is
        /// the kind of arithmetic that is correct at exactly one combination of states if nobody
        /// checks it. `docs/TODO.md` § 92.1 fault 3 is the same shape one screen over.
        /// </summary>
        [UnityTest]
        public IEnumerator ATitleAndATayaTagStackRatherThanOverlap()
        {
            var host = new GameObject("PlateProbeHost", typeof(RectTransform));
            var canvas = MenuKit.BuildCanvas(host.transform, "PlateProbeCanvas");

            // ⚠️⚠️ BUILT THROUGH REFLECTION, AND THE ALTERNATIVE IS RULED OUT BY NAME.
            // `LobbyNameplates.Attach` refuses without a `MapPreviewSurface` and a `LobbyCast`,
            // which are a live 3D preview and a lobby full of bodies; making `Construct` public
            // would be adding a seam to shipping code so a test can reach it, which
            // `FUTURE.md` § 4.5.6 forbids and `PhaseSurfaceLayoutProbe` records taking the same
            // decision for `ShowProgression`.
            //
            // ⚠️ `LateUpdate` EARLY-RETURNS ON THE NULL SURFACE, so the plates never project and
            // never move. That is exactly what this case wants: it is asking about the STACK
            // inside a plate, not about where the plate lands over a head.
            var plates = canvas.gameObject.AddComponent<LobbyNameplates>();

            var type = typeof(LobbyNameplates);
            const System.Reflection.BindingFlags Hidden =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("_surfaceRect", Hidden).SetValue(plates, (RectTransform)canvas.transform);
            type.GetMethod("Construct", Hidden).Invoke(plates, null);

            yield return null;

            plates.SetSeat(0, "MATTHEW", "TAGA-KANTO", ready: true, taya: true, you: true);

            yield return null;

            var title = FindUnder(host, "Title0");
            var tag = FindUnder(host, "Tag0");

            Assert.IsNotNull(title, "the plate built no title strip");
            Assert.IsNotNull(tag, "the plate built no taya strip");
            Assert.IsTrue(title.activeInHierarchy, "a wearing player got no title strip");

            var titleRect = title.GetComponent<RectTransform>();
            var tagRect = tag.GetComponent<RectTransform>();

            // Both strips hang below the plate with a top pivot, so a lower Y is further down.
            Assert.Less(tagRect.anchoredPosition.y,
                        titleRect.anchoredPosition.y - (titleRect.sizeDelta.y * 0.5f),
                        "TAYA FIRST is drawn over the title strip rather than under it. Both " +
                        "hang off the plate's bottom edge, so the tag's offset has to account " +
                        "for the title when the title is showing.");

            // ⚠️ AND THE OTHER STATE. A player wearing nothing must not leave a gap where the
            // title would have been, or every plate in the lobby without a banner reads as
            // broken. This is the state most players are in.
            plates.SetSeat(1, "GUEST", "", ready: false, taya: true, you: false);
            yield return null;

            var bare = FindUnder(host, "Title1");
            var bareTag = FindUnder(host, "Tag1");

            Assert.IsFalse(bare != null && bare.activeInHierarchy,
                "a player wearing no title still got a title strip, which is an empty box under " +
                "their name.");

            Assert.AreEqual(-4.0f, bareTag.GetComponent<RectTransform>().anchoredPosition.y, 0.01f,
                "the taya tag kept the title's offset on a plate with no title, so it floats " +
                "away from the name with nothing in the gap.");

            Object.Destroy(host);
            yield return null;
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        private IEnumerator OpenThePicker(StringBuilder report)
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            yield return new WaitForSecondsRealtime(1.0f);

            // ⚠️ CLASSIC, because it is the shorter of the two columns and therefore the one the
            // COLOURS row is most likely to push off the bottom of. Hero Strike's column is
            // already sized from measured ability rows.
            SceneFlow.SelectedMode = GameMode.Classic;

            // ⚠️⚠️ OPENED THROUGH `CharacterButton`, NOT BY ACTIVATING THE NODE, AND THE
            // DIFFERENCE IS A WHOLE SCREEN. `ConvertedMatchSetup.OpenCharacterSelect` also calls
            // `SetAsLastSibling` on the panel, and its own note says why: the lobby's runtime
            // chrome is built after the authored panel, so hierarchy order alone draws the rail
            // and the tabs over the picker's backdrop. **A probe that activates the node skips
            // that and photographs a screen with somebody else's furniture on top of it** —
            // which is `CLAUDE.md` § 6.2b's whole complaint, committed by the probe rather than
            // by the game. The first run of this file did exactly that and produced a shot with
            // MATCH SETTINGS drawn through the CHOOSE button.
            var opener = FindButton("CharacterButton");
            Assert.IsNotNull(opener, "MatchSetup has no CharacterButton to open the picker with");
            opener.onClick.Invoke();

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "pressing CharacterButton opened no CharacterSelectPanel");
            Assert.IsTrue(panel.activeInHierarchy, "the picker did not open");

            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            _camera = Camera.main;

            if (_camera == null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (cam == null || cam.targetTexture != null) continue;
                    _camera = cam;
                    break;
                }

            Assert.IsNotNull(_camera, "no camera in MatchSetup to render through");

            // ⚠️ THE SAME TRICK EVERY UI PROBE IN THIS PROJECT USES, and the only one that works
            // in batch mode: `Screen.SetResolution` does nothing offscreen, so the canvas renders
            // through a camera whose target texture is the resolution.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
                _canvases.Add(c);
            }

            _target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = _target;

            for (int i = 0; i < 4; i++) yield return null;

            report.AppendLine($"picker open, camera {_camera.name}, {_canvases.Count} canvases");
        }

        /// <summary>⚠️ NO `WaitForEndOfFrame`. It never resumes in batch mode and hangs the whole
        /// run with no log and no xml; `PlayerHubLayoutProbe.Shoot` records the same.</summary>
        private IEnumerator Shoot(string name)
        {
            yield return null;

            if (_camera == null || _target == null) yield break;

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

            Debug.Log($"[Cosmetics] shot {name}");
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static Button FindButton(string name)
        {
            var go = Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static GameObject FindUnder(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
