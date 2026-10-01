using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// ⚠️ YOUR OWN STATUSES, IN THE LEFT STACK (owner's status table, 2026-09-25): each one is its
    /// icon, its name and the owner's tooltip ("Disabled Slipper Retrieval"), with a ring draining
    /// round the icon. VISUAL-1.6 moved every timed row off text and into shapes; a status is the
    /// exception that names itself, because the tooltip is the only thing that tells a player WHY
    /// the grab has stopped working. Every live status is retained. Every label is at least 28 units.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private static readonly int StatusChips = StatusRules.All.Count;
        private readonly RectTransform[] _chip = new RectTransform[StatusChips];
        private readonly Image[] _chipIcon = new Image[StatusChips];
        private readonly HudRing[] _chipRing = new HudRing[StatusChips];
        private readonly Text[] _chipName = new Text[StatusChips];
        private readonly Text[] _chipTip = new Text[StatusChips];
        private readonly List<StatusKind> _liveStatuses = new List<StatusKind>(4);

        private readonly Vector2[] _chipFrom = new Vector2[StatusChips];
        private readonly Vector2[] _chipTarget = new Vector2[StatusChips];
        private readonly float[] _chipMovedAt = new float[StatusChips];
        private float _statusPromptMaxWidth = 1100;

        private void BuildStatusChips()
        {
            for (int i = 0; i < StatusChips; i++)
            {
                var chip = OwnerUiLayout.Rect(_root, "StatusChip" + i);
                Pin(chip, new Vector2(0, 0), new Vector2(230, 75 + i * 120), new Vector2(420, 110));
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
                Pin(_chipName[i].rectTransform, new Vector2(0, .5f), new Vector2(260, 28), new Vector2(300, 38));
                _chipTip[i] = Ink(chip, "StatusTooltip", "", 28, false); _chipTip[i].alignment = TextAnchor.MiddleLeft;
                Pin(_chipTip[i].rectTransform, new Vector2(0, .5f), new Vector2(260, -18), new Vector2(300, 60));
                _chipTip[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                _chip[i] = chip; chip.gameObject.SetActive(false);
            }
        }

        private void PaintStatusChips(CharacterMotor local, bool show)
        {
            if (_chip[0] == null) return;
            StatusIcons.Live(show ? local : null, _liveStatuses);
            var settings = Settings.SettingsStore.Current;
            float scale = Mathf.Max(Settings.GameSettings.ValidHudScale(settings.HudScale), settings.LargerText ? 1.2f : 1f);
            int rows = Mathf.Max(1, Mathf.FloorToInt((_root.rect.height - 180 * scale) / (120 * scale)));
            int columns = Mathf.Max(1, Mathf.CeilToInt(_liveStatuses.Count / (float)rows));
            float occupied = 20 + columns * 440 * scale;
            float centre = columns == 1 ? .5f : Mathf.Clamp01((occupied + _root.rect.width - 20) * .5f / _root.rect.width);
            _statusPromptMaxWidth = columns == 1 ? 1100 : Mathf.Max(240, (_root.rect.width - occupied - 40) / scale);
            if (_promptRoot != null) _promptRoot.anchorMin = _promptRoot.anchorMax = new Vector2(centre, columns == 1 ? .28f : .36f);
            if (_warningRoot != null) _warningRoot.anchorMin = _warningRoot.anchorMax = new Vector2(centre, .66f);
            for (int i = 0; i < StatusChips; i++)
            {
                // A catalog slot belongs to one status, so inserting/removing another
                // cannot transfer its ring, entry motion or expiry to the wrong effect.
                var kind = StatusRules.All[i].Kind;
                int order = _liveStatuses.IndexOf(kind);
                bool on = order >= 0;
                bool entered = on && !_chip[i].gameObject.activeSelf;
                if (_chip[i].gameObject.activeSelf != on) _chip[i].gameObject.SetActive(on);
                if (!on) continue;
                var target = new Vector2(20 + (210 + order / rows * 440) * scale,
                    (75 + order % rows * 120) * scale);
                _chip[i].localScale = Vector3.one * scale;
                if (entered || Vector2.SqrMagnitude(_chipTarget[i] - target) > .01f)
                {
                    _chipFrom[i] = entered ? target - Vector2.up * 24 * scale : _chip[i].anchoredPosition;
                    _chipTarget[i] = target; _chipMovedAt[i] = Time.unscaledTime;
                }
                float move = settings.ReducedUiMotion ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - _chipMovedAt[i]) / .2f));
                _chip[i].anchoredPosition = Vector2.Lerp(_chipFrom[i], target, move);
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
