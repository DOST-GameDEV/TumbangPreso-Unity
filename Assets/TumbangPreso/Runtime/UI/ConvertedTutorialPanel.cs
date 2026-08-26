using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `tutorial.gd`, driving the converted `Tutorial.tscn`.
    ///
    /// ⚠️ IT TEACHES THE THESIS, NOT JUST THE KEYS. The tension is the retrieval, not the
    /// throw: throwing is safe and free, and getting your slipper back is what costs you. A
    /// player who learns only the controls plays the game backwards for their first match.
    /// </summary>
    public sealed class ConvertedTutorialPanel : ConvertedOverlay
    {
        private int _page;

        protected override void Wire()
        {
            SetText("BannerLabel", "HOW TO PLAY");

            OnClick("PrevButton", () => Turn(-1));
            OnClick("NextButton", () => Turn(1));
            OnClick("BackButton", Close);

            var training = MenuKit.WoodButton(transform, "START TRAINING",
                new Vector2(0.5f, 0.0f), new Vector2(0.0f, 58.0f), new Vector2(330.0f, 68.0f),
                StartTraining, "WoodPrimaryButton");
            training.name = "StartTrainingButton";

            ApplyPage();
        }

        private static void StartTraining()
        {
            GameLaunch.Reset();
            GameLaunch.GuidedTutorial = true;
            GameLaunch.PendingAction = "local";
            GameLaunch.SelectedMap = "eskinita";
            GameLaunch.SoloSeat = 1;

            SceneFlow.Networked = false;
            SceneFlow.SelectedMap = SceneFlow.Eskinita;
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            SceneFlow.Go(SceneFlow.Eskinita);
        }

        public void ResetToFirstPage()
        {
            _page = 0;
            ApplyPage();
        }

        /// <summary>⚠️ IT CLAMPS RATHER THAN WRAPPING. Wrapping from the last page back to the
        /// first reads as the panel having closed and reopened.</summary>
        private void Turn(int delta)
        {
            _page = Mathf.Clamp(_page + delta, 0, TutorialContent.Pages.Length - 1);
            ApplyPage();
        }

        private void ApplyPage()
        {
            var page = TutorialContent.Pages[_page];

            SetText("PageTitle", page.Title);
            SetText("PageLede", page.Lede);
            SetText("PageLabel", $"{_page + 1} / {TutorialContent.Pages.Length}");

            var rows = Node("Rows");
            if (rows == null) return;

            Clear(rows);
            _previews.Clear();

            // ⚠️⚠️ PAGE 1 IS A PREMISE CARD, NOT A LIST OF CHIPS, and converting it as rows is
            // what left the game's first teaching screen as four words on four brown plates.
            // `tutorial.gd::_build_premise_strip` puts the four objects the game is ABOUT across
            // one row in live 3D with their Filipino names under them, because this page has
            // nothing to hang a chip on: it is naming things, not explaining a rule.
            if (_page == 0) { BuildPremiseStrip(rows); return; }

            foreach (var row in page.Rows) Row(rows, row.Chip, row.Body);
        }

        private readonly List<ModelPreview> _previews = new List<ModelPreview>();

        private void BuildPremiseStrip(Transform rows)
        {
            var stripGo = new GameObject("PremiseStrip");
            stripGo.AddComponent<RectTransform>();
            stripGo.transform.SetParent(rows, false);

            var strip = stripGo.AddComponent<HorizontalLayoutGroup>();
            strip.childControlHeight = true;
            strip.childControlWidth = true;
            strip.childForceExpandHeight = false;
            strip.childForceExpandWidth = true;
            strip.childAlignment = TextAnchor.MiddleCenter;
            strip.spacing = 18.0f;

            stripGo.AddComponent<LayoutElement>().preferredHeight =
                TutorialContent.TileIconMinHeight + 96.0f;

            foreach (var tile in TutorialContent.PremiseTiles) BuildTile(stripGo.transform, tile);
        }

        private void BuildTile(Transform parent, TutorialContent.Tile tile)
        {
            var columnGo = new GameObject($"Tile_{tile.Fil}");
            columnGo.AddComponent<RectTransform>();
            columnGo.transform.SetParent(parent, false);

            var column = columnGo.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.childAlignment = TextAnchor.UpperCenter;
            column.spacing = 4.0f;

            var element = columnGo.AddComponent<LayoutElement>();
            element.preferredWidth = TutorialContent.TileWidth;
            element.flexibleWidth = 1.0f;

            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(columnGo.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredHeight = TutorialContent.TileIconMinHeight;

            var preview = iconGo.AddComponent<ModelPreview>();
            preview.Attach(iconRect);
            _previews.Add(preview);

            ShowSubject(preview, tile);

            Color role = tile.Offense ? UiTheme.Offense : UiTheme.Defense;

            var fil = MenuKit.Label(columnGo.transform, tile.Fil, TutorialContent.TileFilSize,
                                    role, Vector2.zero, Vector2.zero, Vector2.zero,
                                    TextAnchor.MiddleCenter);
            fil.gameObject.AddComponent<LayoutElement>().preferredHeight = 56.0f;

            var eng = MenuKit.Label(columnGo.transform, tile.Eng, TutorialContent.TileEngSize,
                                    UiTheme.CreamMuted, Vector2.zero, Vector2.zero, Vector2.zero,
                                    TextAnchor.MiddleCenter);
            eng.gameObject.AddComponent<LayoutElement>().preferredHeight = 32.0f;
        }

        /// <summary>
        /// Puts the right rig in the tile, through the same preview the character screen uses,
        /// so the framing and the material are the ones that screen would give the same subject.
        /// </summary>
        private static void ShowSubject(ModelPreview preview, TutorialContent.Tile tile)
        {
            var book = RosterBook.Load();
            if (book == null) return;

            // ⚠️ TURNABLE, BUT THE WHEEL STILL SCROLLS THE PAGE. 🧑: *"in tutorial allow us to
            // play around with the models like in char select"*. These four sit inside this
            // page's scroll view, so the preview takes the two gestures a scroller has no use
            // for and leaves the wheel alone. See EnableTileInteraction.
            preview.EnableTileInteraction();

            switch (tile.Kind)
            {
                case "can":
                    Put(preview, book.CanArt(0));
                    break;

                case "slipper":
                    int slipper = Roster.IndexIn(Roster.Slippers, TutorialContent.TileSlipperId);
                    Put(preview, book.SlipperArt(Mathf.Max(0, slipper)));
                    break;

                default:
                    Put(preview, book.PersonArt(tile.Index));
                    break;
            }

            // ⚠️ AFTER THE SUBJECT, because the tile framing multiplies the MEASURED shot and
            // showing a subject is what measures it. The other way round it is overwritten by
            // the frame that follows.
            preview.SetTileFraming(TutorialContent.TileZoom, uniformExtent: true);
        }

        /// <summary>
        /// ⚠️ THE CLIPS TRAVEL WITH THE MODEL. Without them the tile stands in the rig's bind
        /// pose, which is arms straight out: it reads as broken art, and it is also nearly twice
        /// as wide as the real silhouette so it wrecks the framing measured off it.
        /// </summary>
        private static void Put(ModelPreview preview, RosterEntryAsset art)
            => preview.Show(art == null ? null : art.Model, art == null ? null : art.Clips,
                            art == null ? null : art.Palette);
    }
}
