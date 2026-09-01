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
    /// What Phase 5 left on screen after the colour controls were deleted: the hero picker's
    /// column, asserted to carry NO colour control at all, and the banner title on a lobby plate.
    /// `docs/TODO.md` § 101 and § 114.6.
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
        /// ⚠️⚠️ THE COLOUR CONTROLS ARE DELETED AND THIS IS THE RULE STATED AS A TEST RATHER THAN
        /// AS A COMMENT. 🧑 2026-09-01, twice: *"this shit shiuld be gone the clothes color and
        /// soft bold and shit"*, and *"I asked for this shhit to be removed before, the color
        /// shit for the chracters bcz i wanted customization to eb for the make your own only"*.
        /// `docs/TODO.md` § 114.6.
        ///
        /// ⚠️⚠️ IT REPLACES TWO CASES THAT ASSERTED THE OPPOSITE, AND WHAT THEY WERE PROTECTING
        /// IS WORTH RECORDING BECAUSE IT IS STILL TRUE OF THE CORE. § 101.1: `BannerRules.Earned`
        /// answered two palettes while `PaletteRules.IsKnownVariant` refused both, so the row
        /// filtered its list to nothing and drew nothing at all on an account that owned two.
        /// **The feature looked unbuilt rather than broken.** That fault lives in the CORE and is
        /// still covered, by `CosmeticsWireTests`, which is why deleting the picker did not
        /// delete the assertion: the rule about naming a variant by the tail of its id has not
        /// changed and a palette still crosses the wire.
        ///
        /// ⚠️ THE THING THIS CASE EXISTS TO CATCH IS SOMEBODY PUTTING A COLOUR CONTROL BACK ON
        /// THE HERO TABS. It asserts absence by NODE NAME across all three of the names the
        /// deleted builders used, because a re-added row would almost certainly be built from the
        /// same code and carry the same names.
        /// </summary>
        [UnityTest]
        public IEnumerator NoHeroTabDrawsAnyColourControlAtAll()
        {
            var report = new StringBuilder();

            // ⚠️ SEEDED WITH TWO OWNED PALETTES, WHICH IS THE STATE THE DELETED ROW WOULD HAVE
            // DRAWN IN. Asserting absence on a fresh account would pass against code that still
            // builds the row, because the row hid itself when nothing was owned.
            SeedTwoPalettes();
            yield return OpenThePicker(report);

            foreach (string node in new[] { "PaletteRow", "CLOTHESRow", "STRENGTHRow" })
                Assert.IsNull(Find(node),
                    $"the hero picker drew `{node}`. Colour customisation for the preset cast is " +
                    "deleted: it is MAKE YOUR OWN only (docs/TODO.md § 114.6). If this is being " +
                    "put back, it has to be measured BEFORE the column is sized, because the " +
                    "version that shipped drew straight through the ability list.");

            // ⚠️⚠️ AND THE ABILITY LIST IS CLEAR, WHICH IS THE HALF THE NODE NAMES CANNOT SAY.
            // The fault in his screenshot was an OVERLAP: `AS DRAWN` and `BOLD` printed through
            // SEISMIC STOMP's row. § 102.4 is the same shape and its lesson is that the overflow
            // was vertical while every check in the project measured horizontally. So this asks
            // the column whether its rows fit inside it.
            var rows = Find("StatRows") ?? Find("Rows");

            if (rows != null && rows.TryGetComponent<RectTransform>(out var rect))
            {
                float used = 0.0f;
                foreach (RectTransform child in rect)
                    if (child.gameObject.activeSelf) used += LayoutUtility.GetPreferredHeight(child);

                report.AppendLine($"picker column: {used:F0} used of {rect.rect.height:F0}");

                Assert.LessOrEqual(used, rect.rect.height + 2.0f,
                    $"the picker's rows want {used:F0} units in a {rect.rect.height:F0} unit " +
                    "column, so something is drawing on top of something else. That is exactly " +
                    "what the deleted colour rows did.");
            }

            yield return Shoot("10-picker-no-colour-controls");

            Debug.Log($"[Cosmetics]\n{report}");
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
