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
            // ⚠️⚠️ A TOP AND A BOTTOM ARE EACH TWO TABLES NOW, AND SHOOTING ONLY THE TORSO HALF
            // WOULD BE A PICTURE OF HALF A GARMENT. `docs/TODO.md` § 113 hung the sleeves on the
            // arm bones and the trouser legs on the leg bones, which is what makes them follow the
            // limb they cover; a sheet that dressed the torso alone would show every long-sleeved
            // top with bare arms and read as a regression.
            //
            // ⚠️ THE BOTTOMS ROW PULLS BACK TO 1.28, because a pair of track pants now reaches the
            // ankle and the old 1.00 framed a hip. A cell that crops the hem cannot answer the one
            // question this row exists to ask, which is where the cloth stops.
            yield return Row("tops", VoxelWardrobe.Tops, VoxelAnchor.Torso, 0.58f, 1.00f,
                             VoxelWardrobe.TopSleeves, VoxelAnchor.SleeveLeft,
                             VoxelAnchor.SleeveRight);

            yield return Row("bottoms", VoxelWardrobe.Bottoms, VoxelAnchor.Torso, 0.34f, 1.28f,
                             VoxelWardrobe.BottomLegs, VoxelAnchor.LegLeft, VoxelAnchor.LegRight);
            yield return Row("neck", VoxelWardrobe.Neckwear, VoxelAnchor.Torso, 0.70f, 0.86f);

            // ⚠️⚠️ THE TENTH ROW, AND ITS ABSENCE IS WHAT LET SIX WEARABLES SHIP AGAINST A FRAME
            // THAT WAS WRONG IN TWO AXES. `docs/TODO.md` § 112.10: every wristband was authored
            // `V` -1.08 to 1.08 against a frame that runs 0 to 1, so each one was a 310 mm slab
            // hung off the bottom of the arm, and the arm frame's depth was measured from the
            // shoulder JOINT rather than from the limb. **Nine categories were photographed and
            // this was the one that was not**, which is exactly how long it stayed wrong.
            //
            // ⚠️ IT IS `ArmRight` BECAUSE THAT IS WHERE THE WARDROBE HANGS IT: the throwing arm,
            // and the one on screen in first person. Photographing the left would be a picture of
            // a bone nothing is parented to.
            yield return Row("wrist", VoxelWardrobe.Wristwear, VoxelAnchor.ArmRight, 0.52f, 0.62f);

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
                                VoxelAnchor anchor, float aim, float zoom,
                                (string Name, VoxelPart[] Parts)[] limbs = null,
                                VoxelAnchor limbLeft = VoxelAnchor.SleeveLeft,
                                VoxelAnchor limbRight = VoxelAnchor.SleeveRight)
        {
            var book = RosterBook.Load();
            Assert.IsNotNull(book, "no RosterBook, so there is no model to dress");

            // ⚠️⚠️ `BaseRigId`, NOT `CustomCharacterId`, AND PHOTOGRAPHING THE WRONG ONE WOULD BE
            // A SHEET OF THE RIG THIS WARDROBE IS NO LONGER AUTHORED AGAINST. `custom` resolves
            // `team-custom.glb`, a dressed hero with hair and a sando baked in; `custom_base`
            // resolves the bald, bare rig every V in `VoxelWardrobe` is now measured from.
            // `docs/TODO.md` § 112.
            var art = book.FindPersonArt(CustomCharacterRules.BaseRigId);
            if (art == null) art = book.PersonArt(0, GameMode.Classic);
            Assert.IsNotNull(art, "no art for custom_base or for roster entry 0. Run "
                + "tools/build_base_voxel.py and then RosterBookBuilder.Build.");

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

                // ⚠️ THE SAME SET ON BOTH LIMBS, which is what the game does
                // (`CustomCharacterOutfit.Dress`). Shooting one arm would make a mirrored sleeve
                // invisible, and `VoxelAnchor.SleeveLeft` exists precisely because an unsigned
                // frame draws one of the pair back to front.
                if (limbs != null && i < limbs.Length)
                {
                    VoxelDresser.Dress(subject, limbLeft, limbs[i].Parts, palette,
                                       ToonSkin.PersonOutlineWidth);
                    VoxelDresser.Dress(subject, limbRight, limbs[i].Parts, palette,
                                       ToonSkin.PersonOutlineWidth);
                }

                _preview.LookAt(aim, zoom);

                // ⚠️ THREE FRAMES: the dress lands, the framing recomputes in `LateUpdate`, and
                // the camera draws at the new distance on the frame after that. Two is the
                // stretched-preview fault `UiRuntimeShots.Capture` records.
                for (int f = 0; f < 3; f++) yield return null;

                string safe = table[i].Name.ToLowerInvariant()
                    .Replace(' ', '-').Replace(',', ' ').Replace("/", "-").Trim();

                // ⚠️ `_v2` BECAUSE `_v1` IS ON DISK AND IN THE CHAT SCROLLBACK. `CLAUDE.md` § 6.1:
                // chat clients cache images by filename, so overwriting a render leaves the
                // previous one on screen and the whole review is conducted against an image that
                // no longer exists. The `_v1` sheet is of the DRESSED rig and is the before.
                yield return Shoot($"wardrobe-{category}-{i:00}-{safe}_v3");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SAME PALETTE THE CREATOR BUILDS, THROUGH THE SAME METHOD, WHICH IS THE
        /// WHOLE VALUE OF THE SHEET. It used to reimplement the ramp beside the screen's copy, so
        /// this probe was free to agree with a screen that was wrong. `CustomCharacterOutfit` is
        /// the one owner now (`docs/TODO.md` § 112) and both call it. Same argument
        /// `ModelPreview.StepForCapture`'s header makes about the camera: *"IT IS THE REAL
        /// COMPONENT OR IT IS WORTHLESS."*
        /// </summary>
        private static Color[] Palette(Color[] authored)
        {
            if (authored == null || authored.Length < PaletteRules.SlotCount) return authored;

            // ⚠️ ONE FIXED SUBJECT SO EVERY CELL IS COMPARABLE. A sheet whose skin tone changes
            // per row is a sheet you cannot read a silhouette off. Mid kayumanggi, raven hair,
            // kalye red top, denim bottom.
            var subject = new CustomCharacter
            {
                SkinToneIndex = 8,
                HairColorIndex = 1,
                TopColorIndex = 0,
                BottomColorIndex = 11,
            };

            return CustomCharacterOutfit.PaletteFor(authored, subject);
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
