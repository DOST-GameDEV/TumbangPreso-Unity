using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The in-match icon family on one sheet (TODO VISUAL-1.18).
    ///
    /// ⚠️ A PICTURE, NOT A PROOF OF LOOKING GOOD. Every power icon in match style and every
    /// `HudBadge` state and event glyph, each at the smallest HUD size (22 units at 960x540,
    /// the reading floor's own resolution) and at the deck's size, on the clock plate. The
    /// greyscale thumbnail made from it is what answers "can each be told from each other".
    /// The assertion here is only that every glyph actually draws geometry in match style.
    /// </summary>
    public sealed class HudIconSheetShots
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator EveryInMatchIconDrawsInOneFamily()
        {
            var owner = new GameObject("HudIconSheetOwner");
            try
            {
                var canvas = OwnerUiLayout.Canvas(owner.transform, "HudIconSheet", 500);
                var root = (RectTransform)canvas.transform;
                var plate = OwnerUiLayout.Rect(root, "SheetPlate").gameObject.AddComponent<HudCard>();
                OwnerUiLayout.Fill(plate.rectTransform); plate.color = HudDraw.Plate; plate.Radius = 0; plate.ShadowAlpha = 0;
                var powers = (AbilityGlyph[])Enum.GetValues(typeof(AbilityGlyph));
                var symbols = new System.Collections.Generic.List<TumpAbilitySymbol>();
                for (int i = 0; i < powers.Length; i++)
                {
                    int col = i % 10, row = i / 10;
                    foreach (var (size, top) in new[] { (44f, 40f), (22f, 96f) })
                    {
                        var s = OwnerUiLayout.Rect(root, "Power" + powers[i] + size).gameObject.AddComponent<TumpAbilitySymbol>();
                        OwnerUiLayout.Place(s.rectTransform, 60 + col * 180, top + row * 110, size, size);
                        s.HudStyle = true; s.Glyph = powers[i]; s.color = CourtPresentationPalette.Paper; s.raycastTarget = false;
                        symbols.Add(s);
                    }
                }
                var badges = ((HudBadge.Glyph[])Enum.GetValues(typeof(HudBadge.Glyph))).Where(g => g != HudBadge.Glyph.None).ToArray();
                for (int i = 0; i < badges.Length; i++)
                {
                    foreach (var (size, top) in new[] { (44f, 520f), (22f, 580f) })
                    {
                        var b = OwnerUiLayout.Rect(root, "Badge" + badges[i] + size).gameObject.AddComponent<HudBadge>();
                        OwnerUiLayout.Place(b.rectTransform, 60 + i * 140, top, size, size);
                        b.Detail = HudDraw.Plate; b.raycastTarget = false;
                        b.Show(badges[i], CourtPresentationPalette.Paper, Color.clear, UiTheme.Offense);
                    }
                }
                // VISUAL-1.3: the own-slipper recall ring with each clock: none, fetch warning
                // (gold, draining), penalty running (Offense orange) and a roof or lagoon return
                // (the owner's seat colour, draining).
                var clocks = new (float fill, Color colour)[]
                {
                    (-1, Color.white), (.6f, CourtPresentationPalette.Gold), (1, UiTheme.Offense), (.35f, PlayerIdentity.Colour(1)),
                };
                for (int i = 0; i < clocks.Length; i++)
                {
                    var mark = OwnerUiLayout.Rect(root, "RecallClock" + i).gameObject.AddComponent<SlipperRecallMark>();
                    OwnerUiLayout.Place(mark.rectTransform, 60 + i * 200, 700, SlipperRecall.MarkSize, SlipperRecall.MarkSize);
                    mark.Timer = clocks[i].fill; mark.TimerColour = clocks[i].colour; mark.raycastTarget = false;
                    mark.Bearing = i == 3 ? -Mathf.PI * .5f : (float?)null;
                }
                yield return null;
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(960, 540) })
                    yield return TumpUiCapture.Capture("HudIconSheet-" + size.x + "x" + size.y, canvas, size.x, size.y, false);
                foreach (var s in symbols)
                    Assert.Greater(s.canvasRenderer.GetMesh()?.vertexCount ?? 0, 0, s.name + " draws nothing in match style.");
            }
            finally { UnityEngine.Object.Destroy(owner); }
        }
    }
}
