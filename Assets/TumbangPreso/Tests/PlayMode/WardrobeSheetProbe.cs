using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A contact sheet of the whole wardrobe, on the real rig, through the real shader.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `CLAUDE.md` § 6.1 IS NOT SATISFIED BY ONE SHOT OF ONE COMBINATION.
    /// *"SHOW, DO NOT DESCRIBE. A model change with no render attached cannot be judged."* The
    /// creator screen's own probe photographs whatever the first slot happens to be wearing, which
    /// is one hat out of twelve and one cut out of twelve. **A wardrobe is judged as a set**: the
    /// question is not whether the beanie works, it is whether the beanie reads differently from
    /// the bucket hat at the size a player sees them.
    ///
    /// ⚠️⚠️ AND THE FIRST RUN OF IT FOUND TWO FAULTS NO TEST COULD. `docs/TODO.md` § 110.9: the
    /// scalp shells were authored to the head's full depth and drew as helmets over the forehead,
    /// and the torso frame was taken from `body-mesh`, whose half width is the whole ARM SPAN, so
    /// every shirt painted the character's forearms.
    ///
    /// ⚠️ ONE ROW PER CATEGORY, FRONT ON, AT THE SAME DISTANCE. A sheet whose cells are framed
    /// differently is a sheet you cannot compare across, which is the whole reason to make one.
    /// </summary>
    public class WardrobeSheetProbe
    {
        private const string ShotDir = "Logs/ui";

        /// <summary>⚠️ 320 SQUARE PER CELL. A head is about 90 px tall in play
        /// (`docs/Voxel_Person_Guide.md` § 6), so a cell much smaller than this is a sheet that
        /// cannot answer the one question it is for.</summary>
        private const int Cell = 320;

        private GameObject _host;
        private ModelPreview _preview;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhotographTheWholeWardrobe()
        {
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            Directory.CreateDirectory(ShotDir);

            _host = new GameObject("WardrobeSheetHost");

            var canvasGo = new GameObject("SheetCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(_host.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ⚠️ THE CANVAS IS GIVEN A REAL SIZE, because `ModelPreview.EnsureTexture` sizes its
            // render target from the finished panel rect and a rect of zero is a texture of zero.
            // Its own header records that trap: a rect measured on the frame it is switched on is
            // 0 wide.
            var canvasRect = (RectTransform)canvasGo.transform;
            canvasRect.sizeDelta = new Vector2(Cell, Cell);

            var stageGo = new GameObject("Stage", typeof(RectTransform));
            stageGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)stageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _preview = stageGo.AddComponent<ModelPreview>();
            _preview.Attach(rect);

            // ⚠️⚠️ THE PREVIEW'S OWN CAMERA IS THE ONLY ONE THAT CAN SEE THIS SUBJECT, AND THE
            // FIRST RUN OF THIS PROBE WROTE 87 BLACK PNGs BECAUSE IT BUILT ITS OWN.
            // `ModelPreview` parks its subject on `ModelPreview.Stage`, five hundred metres under
            // the world, on `PreviewLayer` 30, lit by two lights it owns and culled to that layer.
            // A camera built beside it renders an empty black frame and nothing anywhere errors.
            // `StepForCapture` exists for exactly this and its own header says so: *"IT IS THE REAL
            // COMPONENT OR IT IS WORTHLESS. A probe that rebuilds this camera, its two lights, its
            // ambient and its grade beside it is a SECOND implementation, free to agree with the
            // screen while the screen is wrong."*
            _preview.SetTileFraming(1.0f);

            yield return Row("hair", VoxelWardrobe.Hairstyles, VoxelAnchor.Head, 0.82f, 0.80f);
            yield return Row("face", VoxelWardrobe.Expressions, VoxelAnchor.Head, 0.82f, 0.66f);
            yield return Row("marks", VoxelWardrobe.Marks, VoxelAnchor.Head, 0.82f, 0.66f);
            yield return Row("hats", VoxelWardrobe.Headwear, VoxelAnchor.Head, 0.86f, 0.86f);
            yield return Row("eyes", VoxelWardrobe.Eyewear, VoxelAnchor.Head, 0.82f, 0.66f);
            yield return Row("tops", VoxelWardrobe.Tops, VoxelAnchor.Torso, 0.58f, 1.00f);
            yield return Row("bottoms", VoxelWardrobe.Bottoms, VoxelAnchor.Torso, 0.40f, 1.00f);
            yield return Row("neck", VoxelWardrobe.Neckwear, VoxelAnchor.Torso, 0.70f, 0.86f);
            yield return Row("shoes", VoxelWardrobe.Footwear, VoxelAnchor.LegLeft, 0.16f, 0.90f);
        }

        /// <summary>
        /// One category, one PNG per entry, named for what it is.
        ///
        /// ⚠️ THE FILE NAME CARRIES THE ENTRY'S OWN NAME, so a sheet read three weeks later still
        /// says which box is which. `CLAUDE.md` § 6.1's versioning rule is about not overwriting;
        /// this is the other half, which is not having to count along a row to identify a cell.
        /// </summary>
        private IEnumerator Row(string category, (string Name, VoxelPart[] Parts)[] table,
                                VoxelAnchor anchor, float aim, float zoom)
        {
            var book = RosterBook.Load();
            Assert.IsNotNull(book, "no RosterBook, so there is no model to dress");

            var art = book.FindPersonArt(CustomCharacterRules.CustomCharacterId);
            if (art == null) art = book.PersonArt(0, GameMode.Classic);
            Assert.IsNotNull(art, "no art for the custom character or for roster entry 0");

            var palette = Palette(art.Palette);

            for (int i = 0; i < table.Length; i++)
            {
                _preview.Show(art.Model, art.Clips, palette, art.PetModel);

                var subject = _preview.Subject;
                Assert.IsNotNull(subject, "ModelPreview.Show built no subject");

                VoxelDresser.Undress(subject);
                VoxelDresser.Dress(subject, anchor, table[i].Parts, palette,
                                   ToonSkin.PersonOutlineWidth);

                if (anchor == VoxelAnchor.LegLeft)
                    VoxelDresser.Dress(subject, VoxelAnchor.LegRight, table[i].Parts, palette,
                                       ToonSkin.PersonOutlineWidth);

                _preview.LookAt(aim, zoom);

                // ⚠️ THREE FRAMES: the dress lands, the framing recomputes in `LateUpdate`, and
                // the camera draws at the new distance on the frame after that. Two is the
                // stretched-preview fault `UiRuntimeShots.Capture` records.
                for (int f = 0; f < 3; f++) yield return null;

                string safe = table[i].Name.ToLowerInvariant()
                    .Replace(' ', '-').Replace(',', ' ').Replace("/", "-").Trim();

                yield return Shoot($"wardrobe-{category}-{i:00}-{safe}_v1");
            }
        }

        /// <summary>⚠️ THE SAME PALETTE THE CREATOR BUILDS, so the sheet is a picture of what a
        /// player sees rather than of the authored `.tres`.</summary>
        private static Color[] Palette(Color[] authored)
        {
            if (authored == null || authored.Length < PaletteRules.SlotCount) return authored;

            var palette = new Color[authored.Length];
            System.Array.Copy(authored, palette, authored.Length);

            Ramp(palette, PaletteRules.SkinSlots, CustomCharacterScreen.SkinColour(8));
            Ramp(palette, new[] { 10, 11, 12 }, CustomCharacterScreen.HairColour(1));
            Ramp(palette, new[] { 4, 5, 6 }, CustomCharacterScreen.ClothColour(0));
            Ramp(palette, new[] { 0, 1, 2 }, CustomCharacterScreen.ClothColour(11));

            palette[3] = UiTheme.WoodEdge;
            palette[7] = UiTheme.Amber;
            palette[9] = UiTheme.Cream;

            return palette;
        }

        private static void Ramp(Color[] palette, int[] slots, Color basis)
        {
            palette[slots[0]] = basis * 1.14f;
            palette[slots[1]] = basis * 0.78f;
            palette[slots[2]] = basis * 1.14f;
        }

        private IEnumerator Shoot(string name)
        {
            yield return null;

            _preview.StepForCapture();

            var target = _preview.Target;
            if (target == null)
            {
                Debug.LogWarning($"[Wardrobe] {name}: the preview has no render target yet.");
                yield break;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var shot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), shot.EncodeToPNG());
            Object.Destroy(shot);
        }
    }
}
