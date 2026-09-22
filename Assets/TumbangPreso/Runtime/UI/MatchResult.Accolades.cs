using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Two accolades per player on the match-end board (TODO VISUAL-1.16).
    ///
    /// ⚠️⚠️ EVERY NUMBER COMES FROM THE MATCH RECORD, AND NOTHING IS INVENTED. Rocket League's
    /// podium gives each player two earned lines; TUMP's are read off the same
    /// `PlayerMatchStats` line the career stores (`MatchStatsCollector`, `MatchRecord`), in a
    /// fixed order of what the game is about: knockdowns, catches, retrievals made with the
    /// taya close, sabotages, then plain retrievals. A player shows their first two non-zero
    /// ones. Zero is never drawn as an accolade, and a player with none shows none.
    ///
    /// ⚠️ A GLYPH AND A NUMBER, NOT A WORD. The pictograms are the feed's own (`HudBadge` Knock,
    /// Tag, Slipper, Star), so the match end speaks the language the match taught. A retrieval
    /// under pressure is the slipper on the taya's blue disc and a plain retrieval the slipper on
    /// the dark plate, which separates them by lightness as well as by colour.
    ///
    /// ⚠️ THE RECORD MAY LAND AFTER THE BOARD OPENS (on a client it usually does), so the slots
    /// are built empty and filled from `OnRecordReady`, the same path `YourMatchSummary` uses.
    /// </summary>
    public sealed partial class MatchResult
    {
        private readonly HudBadge[,] _accoladeGlyphs = new HudBadge[4, 2];
        private readonly Text[,] _accoladeCounts = new Text[4, 2];
        private readonly HudCard[] _finishSwatches = new HudCard[4];
        private readonly HudBadge[] _finishCrowns = new HudBadge[4];
        private int[] _accoladeOrder = { 0, 1, 2, 3 };

        private void BuildAccolades(RectTransform row, int i, float x, float y)
        {
            for (int k = 0; k < 2; k++)
            {
                var glyph = _accoladeGlyphs[i, k] = OwnerUiLayout.Rect(row, "Accolade" + i + k).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(glyph.rectTransform, x + k * 170, y, 48, 48);
                glyph.Detail = HudDraw.Plate; glyph.raycastTarget = false;
                var count = _accoladeCounts[i, k] = OwnerUiLayout.Text(row, "AccoladeCount" + i + k, "", 34, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(count.rectTransform, x + k * 170 + 56, y, 100, 48);
                count.color = HudDraw.CardInk; count.alignment = TextAnchor.MiddleLeft; count.verticalOverflow = VerticalWrapMode.Overflow;
                glyph.enabled = false; count.enabled = false;
            }
        }

        /// <summary>The accolades a line earns, best first, at most two.</summary>
        internal static List<(HudBadge.Glyph glyph, Color backing, int count)> AccoladesFor(PlayerMatchStats line)
        {
            var list = new List<(HudBadge.Glyph, Color, int)>();
            if (line == null) return list;
            void Add(HudBadge.Glyph g, Color backing, int n) { if (n > 0 && list.Count < 2) list.Add((g, backing, n)); }
            Add(HudBadge.Glyph.Knock, Color.clear, line.Knockdowns);
            Add(HudBadge.Glyph.Tag, Color.clear, line.Tags);
            Add(HudBadge.Glyph.Slipper, UiTheme.Defense, line.RetrievalsUnderPressure);
            Add(HudBadge.Glyph.Star, Color.clear, line.Sabotages);
            Add(HudBadge.Glyph.Slipper, HudDraw.Plate, line.Retrievals - line.RetrievalsUnderPressure);
            return list;
        }

        /// <summary>Fills each ranked row's accolades from the record, by seat.</summary>
        private void PaintAccolades(MatchRecord record)
        {
            if (_accoladeGlyphs[0, 0] == null) return;
            for (int i = 0; i < 4; i++)
            {
                PlayerMatchStats line = null;
                if (record != null && i < _accoladeOrder.Length)
                    foreach (var candidate in record.Players) if (candidate != null && candidate.Slot == _accoladeOrder[i]) { line = candidate; break; }
                var earned = AccoladesFor(line);
                for (int k = 0; k < 2; k++)
                {
                    bool show = k < earned.Count;
                    _accoladeGlyphs[i, k].enabled = show; _accoladeCounts[i, k].enabled = show;
                    if (!show) continue;
                    var (glyph, backing, count) = earned[k];
                    var fill = glyph == HudBadge.Glyph.Slipper ? CourtPresentationPalette.Paper : HudDraw.CardInk;
                    var accent = glyph == HudBadge.Glyph.Tag ? UiTheme.Defense : UiTheme.Offense;
                    // The burst glyphs sit on the cream card as they are; the slipper needs its disc.
                    _accoladeGlyphs[i, k].Show(glyph, glyph == HudBadge.Glyph.Knock || glyph == HudBadge.Glyph.Tag ? CourtPresentationPalette.Paper : fill,
                        backing, accent);
                    _accoladeCounts[i, k].text = count.ToString();
                }
            }
        }
    }
}
