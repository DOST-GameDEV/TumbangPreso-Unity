using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// ⚠️ YOUR OWN STATUSES, UNDER THE RETICLE (owner's status table, 2026-09-25): each one is its
    /// icon, its name and the owner's tooltip ("Disabled Slipper Retrieval"), with a ring draining
    /// round the icon. VISUAL-1.6 moved every timed row off text and into shapes; a status is the
    /// exception that names itself, because the tooltip is the only thing that tells a player WHY
    /// the grab has stopped working. Two chips at most. Every label is at least 28 units.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private const int StatusChips = 2;
        private readonly RectTransform[] _chip = new RectTransform[StatusChips];
        private readonly Image[] _chipIcon = new Image[StatusChips];
        private readonly HudRing[] _chipRing = new HudRing[StatusChips];
        private readonly Text[] _chipName = new Text[StatusChips];
        private readonly Text[] _chipTip = new Text[StatusChips];
        private readonly List<StatusKind> _liveStatuses = new List<StatusKind>(4);

        private void BuildStatusChips()
        {
            for (int i = 0; i < StatusChips; i++)
            {
                var chip = OwnerUiLayout.Rect(_root, "StatusChip" + i);
                Pin(chip, new Vector2(.5f, .5f), new Vector2(i == 0 ? -250 : 250, -170), new Vector2(470, 92));
                var plate = OwnerUiLayout.Rect(chip, "StatusPlate").gameObject.AddComponent<HudCard>();
                plate.color = HudDraw.Plate; plate.Radius = 22; plate.raycastTarget = false;
                OwnerUiLayout.Fill(plate.rectTransform);
                _chipRing[i] = OwnerUiLayout.Rect(chip, "StatusRing").gameObject.AddComponent<HudRing>();
                _chipRing[i].Thickness = 6; _chipRing[i].raycastTarget = false;
                Pin(_chipRing[i].rectTransform, new Vector2(0, .5f), new Vector2(50, 0), new Vector2(84, 84));
                _chipIcon[i] = OwnerUiLayout.Rect(chip, "StatusIcon").gameObject.AddComponent<Image>();
                _chipIcon[i].raycastTarget = false; _chipIcon[i].preserveAspect = true;
                Pin(_chipIcon[i].rectTransform, new Vector2(0, .5f), new Vector2(50, 0), new Vector2(66, 66));
                _chipName[i] = Ink(chip, "StatusName", "", 30, true); _chipName[i].alignment = TextAnchor.MiddleLeft;
                Pin(_chipName[i].rectTransform, new Vector2(0, .5f), new Vector2(275, 20), new Vector2(360, 40));
                _chipTip[i] = Ink(chip, "StatusTooltip", "", 28, false); _chipTip[i].alignment = TextAnchor.MiddleLeft;
                Pin(_chipTip[i].rectTransform, new Vector2(0, .5f), new Vector2(275, -20), new Vector2(360, 40));
                _chip[i] = chip; chip.gameObject.SetActive(false);
            }
        }

        private void PaintStatusChips(CharacterMotor local, bool show)
        {
            if (_chip[0] == null) return;
            StatusIcons.Live(show ? local : null, _liveStatuses);
            int count = Mathf.Min(StatusChips, _liveStatuses.Count);
            for (int i = 0; i < StatusChips; i++)
            {
                bool on = i < count;
                if (_chip[i].gameObject.activeSelf != on) _chip[i].gameObject.SetActive(on);
                if (!on) continue;
                var kind = _liveStatuses[i];
                // One chip centres; two sit either side of the reticle's column.
                _chip[i].anchoredPosition = new Vector2(count == 1 ? 0 : (i == 0 ? -250 : 250), -170);
                var sprite = StatusIcons.For(kind);
                if (_chipIcon[i].sprite != sprite) _chipIcon[i].sprite = sprite;
                _chipIcon[i].enabled = sprite != null;
                _chipName[i].text = StatusIcons.Name(kind);
                _chipTip[i].text = StatusIcons.Tooltip(kind);
                float total = Mathf.Max(0.01f, local.StatusTotal(kind));
                _chipRing[i].Set(Mathf.Clamp01(local.StatusLeft(kind) / total));
                _chipRing[i].Paint(StatusColour(kind), new Color(0, 0, 0, .38f));
            }
        }

        /// <summary>The ring colour: the element that did it. The tag is warm gold here, because
        /// this is the victim's own HUD and the taya's colour would read as an ally marker.</summary>
        private static Color StatusColour(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Whirled: return UiTheme.HeroWindBright;
                case StatusKind.Chilled: return new Color(.82f, .95f, .86f);
                case StatusKind.Frozen: return new Color(.92f, 1f, .94f);
                default: return CourtPresentationPalette.Gold;
            }
        }
    }
}
