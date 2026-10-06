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
    ///
    /// ⚠️ UI REVAMP 2026-10-06: NO MORE FIXED GREY SLABS. The owner rejected a stack of identical
    /// 500 x 118 panels. A status is now a MEDALLION (paper rim, ink face, the element's ring and
    /// icon) with an ink TAB behind it that is only as wide and tall as its own name, seconds and
    /// explanation. Each catalogue entry measures its tab once, from its real tooltip, so a long
    /// explanation gets its second line and a short one does not pay for it.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private static readonly int StatusChips = StatusRules.All.Count;
        private readonly RectTransform[] _chip = new RectTransform[StatusChips];
        private readonly Image[] _chipIcon = new Image[StatusChips];
        private readonly HudRing[] _chipRing = new HudRing[StatusChips];
        private readonly Text[] _chipName = new Text[StatusChips];
        private readonly Text[] _chipTip = new Text[StatusChips];
        private readonly Text[] _chipTime = new Text[StatusChips];
        private readonly int[] _chipTenths = new int[StatusChips];
        private readonly Vector2[] _chipSize = new Vector2[StatusChips];
        private readonly Vector2[] _orderedSize = new Vector2[StatusChips];
        private readonly Vector2[] _orderedPositions = new Vector2[StatusChips];
        private OwnerUiGlyph _zappedFallback;
        private readonly List<StatusKind> _liveStatuses = new List<StatusKind>(4);

        private readonly Vector2[] _chipFrom = new Vector2[StatusChips];
        private readonly Vector2[] _chipTarget = new Vector2[StatusChips];
        private readonly float[] _chipMovedAt = new float[StatusChips];
        private float _statusPromptMaxWidth = 1100;

        private const float Medal = 92, TabLeft = 104, TabRight = 20, TipWrap = 372, StatusColumn = 520, StatusMargin = 24, StatusGap = 10;

        /// <summary>"CHILLED" drawn as "Chilled": the catalogue keeps its own spelling.</summary>
        internal static string StatusTitle(string name)
            => string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1) + name.Substring(1).ToLowerInvariant();

        private void BuildStatusChips()
        {
            for (int i = 0; i < StatusChips; i++)
            {
                var kind = StatusRules.All[i].Kind;
                var chip = OwnerUiLayout.Rect(_root, "StatusChip" + i);
                Pin(chip, new Vector2(0, 0), new Vector2(StatusMargin + 250, 75 + i * 128), new Vector2(500, Medal));

                var plate = OwnerUiLayout.Rect(chip, "StatusPlate").gameObject.AddComponent<HudCard>();
                plate.Toy(HudDraw.Cream, HudDraw.CreamSide, 8, 16, .42f).raycastTarget = false;

                var rim = OwnerUiLayout.Rect(chip, "StatusMedal").gameObject.AddComponent<HudCard>();
                rim.Toy(HudDraw.Brown, HudDraw.BrownSide, 4, Medal * .29f, .3f).raycastTarget = false; rim.Sheen = false;
                Pin(rim.rectTransform, new Vector2(0, .5f), new Vector2(Medal * .5f, 0), new Vector2(Medal, Medal));
                var face = OwnerUiLayout.Rect(chip, "StatusFace").gameObject.AddComponent<HudCard>();
                face.Toy(HudDraw.Brown, Color.clear, 0, (Medal - 10) * .29f, 0).raycastTarget = false; face.Sheen = false; face.enabled = false;
                Pin(face.rectTransform, new Vector2(0, .5f), new Vector2(Medal * .5f, 0), new Vector2(Medal - 10, Medal - 10));

                _chipRing[i] = OwnerUiLayout.Rect(chip, "StatusRing").gameObject.AddComponent<HudRing>();
                _chipRing[i].Thickness = 6; _chipRing[i].raycastTarget = false;
                Pin(_chipRing[i].rectTransform, new Vector2(0, .5f), new Vector2(Medal * .5f, 0), new Vector2(Medal - 14, Medal - 14));
                _chipIcon[i] = OwnerUiLayout.Rect(chip, "StatusIcon").gameObject.AddComponent<Image>();
                _chipIcon[i].raycastTarget = false; _chipIcon[i].preserveAspect = true;
                Pin(_chipIcon[i].rectTransform, new Vector2(0, .5f), new Vector2(Medal * .5f, 0), new Vector2(62, 62));
                if (kind == StatusKind.Zapped)
                {
                    // No supplied sprite exists for this status yet; the existing scalable lock
                    // mark keeps an ability lock legible at a glance until one does.
                    _zappedFallback = OwnerUiGlyph.Create(chip, "StatusLock", OwnerUiGlyph.Mark.Lock, HudDraw.Honey);
                    Pin(_zappedFallback.rectTransform, new Vector2(0, .5f), new Vector2(Medal * .5f, 0), new Vector2(40, 40));
                    _zappedFallback.enabled = false;
                }

                _chipName[i] = OwnerUiLayout.Text(chip, "StatusName", StatusTitle(StatusIcons.Name(kind)), 34, OwnerUiLayout.TypeRole.Display);
                _chipName[i].color = HudDraw.Brown; _chipName[i].alignment = TextAnchor.MiddleLeft;
                _chipName[i].horizontalOverflow = HorizontalWrapMode.Overflow; _chipName[i].verticalOverflow = VerticalWrapMode.Overflow;
                _chipTime[i] = OwnerUiLayout.Text(chip, "StatusTime", "", 28, OwnerUiLayout.TypeRole.Display);
                _chipTime[i].alignment = TextAnchor.MiddleRight; _chipTime[i].color = Color.Lerp(StatusColour(kind), HudDraw.Brown, .55f);
                _chipTime[i].horizontalOverflow = HorizontalWrapMode.Overflow; _chipTime[i].verticalOverflow = VerticalWrapMode.Overflow;
                _chipTenths[i] = -1;
                _chipTip[i] = OwnerUiLayout.Text(chip, "StatusTooltip", StatusIcons.Tooltip(kind), 28, OwnerUiLayout.TypeRole.Reading);
                _chipTip[i].color = HudDraw.BrownMuted; _chipTip[i].alignment = TextAnchor.UpperLeft;
                _chipTip[i].horizontalOverflow = HorizontalWrapMode.Wrap; _chipTip[i].verticalOverflow = VerticalWrapMode.Overflow;
                _chipTip[i].lineSpacing = .95f;

                // Measure this status's own tab once: name line, then its real explanation.
                const float nameHeight = 40, timeWidth = 70;
                Pin(_chipTip[i].rectTransform, new Vector2(0, .5f), Vector2.zero, new Vector2(TipWrap, 40));
                float tipWidth = Mathf.Min(TipWrap, Mathf.Ceil(_chipTip[i].preferredWidth) + 12);
                _chipTip[i].rectTransform.sizeDelta = new Vector2(tipWidth, 40);
                float tipHeight = Mathf.Ceil(_chipTip[i].preferredHeight * 1.08f) + 6;
                float content = Mathf.Max(tipWidth, _chipName[i].preferredWidth + 16 + timeWidth);
                float width = TabLeft + content + TabRight, height = Mathf.Max(Medal, 10 + nameHeight + tipHeight + 12);
                _chipSize[i] = new Vector2(width, height);
                chip.sizeDelta = _chipSize[i];
                OwnerUiLayout.Place(plate.rectTransform, Medal * .5f, 4, width - Medal * .5f, height - 8);
                float top = height * .5f - 10;
                Pin(_chipName[i].rectTransform, new Vector2(0, .5f), new Vector2(TabLeft + (content - timeWidth - 8) * .5f, top - nameHeight * .5f),
                    new Vector2(content - timeWidth - 8, nameHeight));
                Pin(_chipTime[i].rectTransform, new Vector2(0, .5f), new Vector2(TabLeft + content - timeWidth * .5f, top - nameHeight * .5f),
                    new Vector2(timeWidth, nameHeight));
                Pin(_chipTip[i].rectTransform, new Vector2(0, .5f), new Vector2(TabLeft + tipWidth * .5f, top - nameHeight - 2 - tipHeight * .5f),
                    new Vector2(tipWidth, tipHeight));
                _chip[i] = chip; chip.gameObject.SetActive(false);
            }
        }

        private void PaintStatusChips(CharacterMotor local, bool show)
        {
            if (_chip[0] == null) return;
            StatusIcons.Live(show ? local : null, _liveStatuses);
            var settings = Settings.SettingsStore.Current;
            float scale = Mathf.Max(Settings.GameSettings.ValidHudScale(settings.HudScale), settings.LargerText ? 1.2f : 1f);
            for (int i = 0; i < StatusChips; i++)
            {
                int order = _liveStatuses.IndexOf(StatusRules.All[i].Kind);
                if (order >= 0) _orderedSize[order] = _chipSize[i];
            }
            // Stack upward from the bottom-left corner by each tab's real height; a dense set
            // opens another column instead of running under the match bar.
            float bottom = StatusMargin, limit = Mathf.Max(Medal + StatusMargin, _root.rect.height / scale - 200);
            int column = 0;
            for (int order = 0; order < _liveStatuses.Count; order++)
            {
                var size = _orderedSize[order];
                if (bottom > StatusMargin && bottom + size.y > limit) { column++; bottom = StatusMargin; }
                _orderedPositions[order] = new Vector2(StatusMargin + (column * StatusColumn + size.x * .5f) * scale, (bottom + size.y * .5f) * scale);
                bottom += size.y + StatusGap;
            }
            int columns = _liveStatuses.Count == 0 ? 1 : column + 1;
            float occupied = 20 + columns * StatusColumn * scale;
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
                var target = _orderedPositions[order];
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
                if (kind == StatusKind.Zapped && _zappedFallback != null) _zappedFallback.enabled = sprite == null;
                float total = Mathf.Max(0.01f, local.StatusTotal(kind));
                float left = Mathf.Max(0, local.StatusLeft(kind));
                _chipRing[i].Set(Mathf.Clamp01(left / total));
                _chipRing[i].Paint(StatusColour(kind), new Color(1, 1, 1, .16f));
                // Rewritten only when the shown tenth changes, never every frame.
                int tenths = Mathf.CeilToInt(left * 10);
                if (_chipTenths[i] != tenths)
                {
                    _chipTenths[i] = tenths;
                    _chipTime[i].text = (tenths * .1f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";
                }
            }
        }

        /// <summary>The ring colour: the element that did it. The tag is warm honey here, because
        /// this is the victim's own HUD and the taya's colour would read as an ally marker.</summary>
        private static Color StatusColour(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Whirled: return UiTheme.HeroWindBright;
                case StatusKind.Chilled: return new Color(.70f, .92f, 1f);
                case StatusKind.Frozen: return new Color(.86f, .97f, 1f);
                default: return HudDraw.Honey;
            }
        }
    }
}
