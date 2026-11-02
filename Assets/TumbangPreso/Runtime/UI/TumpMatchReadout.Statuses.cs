using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § YOUR OWN STATUSES, a row of small coral rings right of your score (live HUD, 2026-11-01;
    /// `boot_match.statuses` and `status_landed`).
    ///
    /// Each ring is the effect's icon on a dark disc with a coral arc that drains from the effect's real
    /// remaining time (`StatusLeft` over `StatusTotal`) and the whole seconds painted below it. The
    /// tall rows of icon, name and explanation are gone: a ring says WHICH effect and HOW LONG, and the
    /// words only appear when they are news.
    ///
    /// ⚠️ WHEN ONE LANDS, ITS RING SWELLS WITH A HAND-CUT WHITE RIM AND POINTED TIP and its name and
    /// description show over it for about three seconds, then fade. The rules the owner settled:
    /// the effect soonest to expire goes first; its words never outlast it; an effect that ends while
    /// waiting is dropped from the queue; an effect with under 1.5 s left only pops its ring; and a
    /// REFRESHED effect (already on, time put back) shows nothing again. Effects landing together take
    /// turns, about two seconds each, the last holding three.
    ///
    /// ⚠️ HOLD THE INFO KEY (TAB) AND EVERY ACTIVE EFFECT IS LISTED with its name and description, so a
    /// player can look up what is stopping them without waiting for a landing. Nothing here changes a
    /// status, its icon or its effects; the HUD only reads them.
    ///
    /// Object names (`StatusChip{i}`, `StatusIcon`, `StatusName`, `StatusTooltip`, `StatusTimeChip`,
    /// `StatusTime`) are kept because the capture boards and the probes address them by name.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private static readonly int StatusChips = StatusRules.All.Count;
        private readonly RectTransform[] _chip = new RectTransform[StatusChips];
        private readonly RectTransform[] _chipBody = new RectTransform[StatusChips];
        private readonly Image[] _chipIcon = new Image[StatusChips];
        private readonly HudRing[] _chipRing = new HudRing[StatusChips];
        private readonly HudCard[] _chipDisc = new HudCard[StatusChips];
        private readonly Image[] _chipRim = new Image[StatusChips];
        private readonly Text[] _chipName = new Text[StatusChips];
        private readonly Text[] _chipTip = new Text[StatusChips];
        private readonly Text[] _chipTime = new Text[StatusChips];
        private readonly Text[] _listName = new Text[StatusChips];
        private readonly Text[] _listTip = new Text[StatusChips];
        private readonly float[] _chipTipHeight = new float[StatusChips];
        private readonly int[] _chipSeconds = new int[StatusChips];
        private readonly bool[] _chipWas = new bool[StatusChips];
        private readonly float[] _chipPopAt = new float[StatusChips], _chipFocus = new float[StatusChips], _chipMovedAt = new float[StatusChips];
        private readonly Vector2[] _chipFrom = new Vector2[StatusChips], _chipTarget = new Vector2[StatusChips];
        private readonly bool[] _chipPlaced = new bool[StatusChips];
        private readonly int[] _statusQueue = new int[StatusChips];
        private int _statusQueued, _statusLanding = -1;
        private float _statusLandingAt;
        private RectTransform _statusList;
        private OwnerUiGlyph _zappedFallback;
        private readonly List<StatusKind> _liveStatuses = new List<StatusKind>(4);
        private float _statusPromptMaxWidth = 1100;

        private const int RingsPerRow = 6;
        private const float RingStepX = 86, RingRowStep = 100, RingFirstX = 402, RingY = 82, TextWrap = 440, MinTextSeconds = 1.5f;
        private const float LandSeconds = 3f, QueuedSeconds = 2f, FocusScale = 84f / 72f;

        private static void CentreAnchors(RectTransform rect)
            => rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);

        private static void BottomLeft(RectTransform rect, Vector2 at, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.anchoredPosition = at; rect.sizeDelta = size;
        }

        private void BuildStatusChips()
        {
            _statusList = OwnerUiLayout.Rect(_root, "StatusList");
            BottomLeft(_statusList, Vector2.zero, new Vector2(1, 1));
            for (int i = 0; i < StatusChips; i++)
            {
                var kind = StatusRules.All[i].Kind;
                var chip = OwnerUiLayout.Rect(_root, "StatusChip" + i);
                CentreAnchors(chip); chip.anchorMin = chip.anchorMax = Vector2.zero;
                chip.anchoredPosition = new Vector2(RingFirstX, RingY); chip.sizeDelta = new Vector2(84, 84);

                // The hand-cut rim stands behind the ring and shows only while the effect has just landed. The
                // art has its pointed tip on top, so the ring's centre sits 8 below the art's centre.
                _chipRim[i] = OwnerUiLayout.Rect(chip, "StatusRim").gameObject.AddComponent<Image>();
                _chipRim[i].sprite = LiveHud.Picture("rim-cut"); _chipRim[i].raycastTarget = false; _chipRim[i].enabled = false;
                CentreAnchors(_chipRim[i].rectTransform);
                _chipRim[i].rectTransform.anchoredPosition = new Vector2(0, 8); _chipRim[i].rectTransform.sizeDelta = new Vector2(99, 115);

                var body = _chipBody[i] = OwnerUiLayout.Rect(chip, "StatusBody");
                CentreAnchors(body); body.anchoredPosition = Vector2.zero; body.sizeDelta = new Vector2(72, 72);
                _chipDisc[i] = OwnerUiLayout.Rect(body, "StatusDisc").gameObject.AddComponent<HudCard>();
                _chipDisc[i].Glass(new Color(LiveHud.Glass.r, LiveHud.Glass.g, LiveHud.Glass.b, 95 / 255f), 0, 36, 0);
                _chipDisc[i].BorderWidth = 0; _chipDisc[i].FollowContrast = false; _chipDisc[i].raycastTarget = false;
                OwnerUiLayout.Fill(_chipDisc[i].rectTransform);
                _chipRing[i] = OwnerUiLayout.Rect(body, "StatusRing").gameObject.AddComponent<HudRing>();
                _chipRing[i].Thickness = 6; _chipRing[i].raycastTarget = false;
                OwnerUiLayout.Fill(_chipRing[i].rectTransform);
                _chipIcon[i] = OwnerUiLayout.Rect(body, "StatusIcon").gameObject.AddComponent<Image>();
                _chipIcon[i].raycastTarget = false; _chipIcon[i].preserveAspect = true;
                CentreAnchors(_chipIcon[i].rectTransform);
                _chipIcon[i].rectTransform.anchoredPosition = Vector2.zero; _chipIcon[i].rectTransform.sizeDelta = new Vector2(54, 54);
                if (kind == StatusKind.Zapped)
                {
                    // No supplied sprite exists for this status yet; the existing scalable lock
                    // mark keeps an ability lock legible at a glance until one does.
                    _zappedFallback = OwnerUiGlyph.Create(body, "StatusLock", OwnerUiGlyph.Mark.Lock, HudDraw.Honey);
                    CentreAnchors(_zappedFallback.rectTransform);
                    _zappedFallback.rectTransform.anchoredPosition = Vector2.zero; _zappedFallback.rectTransform.sizeDelta = new Vector2(34, 34);
                    _zappedFallback.enabled = false;
                }

                // The seconds, painted white on a dark edge, centred under the ring.
                var timeChip = OwnerUiLayout.Rect(chip, "StatusTimeChip");
                CentreAnchors(timeChip); timeChip.anchoredPosition = new Vector2(0, -44); timeChip.sizeDelta = new Vector2(80, 32);
                _chipTime[i] = LiveHud.Lettering(timeChip, "StatusTime", "", 24, Color.white, LiveHud.Glass, LiveHud.Glass, 3, 3, 1);
                OwnerUiLayout.Fill(_chipTime[i].rectTransform);
                _chipSeconds[i] = -1;

                // The words that show over the ring when the effect lands: name in painted lettering, description below it.
                _chipTip[i] = LiveHud.Fact(chip, "StatusTooltip", StatusIcons.Tooltip(kind), 24, Color.white);
                _chipTip[i].alignment = TextAnchor.LowerLeft; _chipTip[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                BottomLeft(_chipTip[i].rectTransform, new Vector2(8, 105), new Vector2(TextWrap, 30));
                // A little over the measure, so a line is never clipped when the text is laid out at another canvas scale.
                _chipTipHeight[i] = Mathf.Max(30, Mathf.Ceil(_chipTip[i].preferredHeight * 1.3f) + 4);
                _chipTip[i].rectTransform.sizeDelta = new Vector2(TextWrap, _chipTipHeight[i]);
                _chipName[i] = LiveHud.Lettering(chip, "StatusName", StatusIcons.Name(kind).ToUpperInvariant(), 36, Color.white, LiveHud.Glass, LiveHud.Glass, 4, 3, 3);
                _chipName[i].alignment = TextAnchor.LowerLeft;
                BottomLeft(_chipName[i].rectTransform, new Vector2(8, 105 + _chipTipHeight[i] + 4), new Vector2(TextWrap, 50));
                _chipName[i].enabled = _chipTip[i].enabled = false;

                // The Tab list's own copy, stacked above the row.
                _listName[i] = LiveHud.Lettering(_statusList, "ListName" + i, StatusIcons.Name(kind).ToUpperInvariant(), 32, Color.white, LiveHud.Glass, LiveHud.Glass, 4, 3, 3);
                _listName[i].alignment = TextAnchor.LowerLeft;
                BottomLeft(_listName[i].rectTransform, Vector2.zero, new Vector2(TextWrap, 44));
                _listTip[i] = LiveHud.Fact(_statusList, "ListTip" + i, StatusIcons.Tooltip(kind), 24, Color.white);
                _listTip[i].alignment = TextAnchor.LowerLeft; _listTip[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                BottomLeft(_listTip[i].rectTransform, Vector2.zero, new Vector2(TextWrap, _chipTipHeight[i]));
                _listName[i].enabled = _listTip[i].enabled = false;
                _chip[i] = chip; chip.gameObject.SetActive(false);
            }
        }

        /// <summary>A ring has just come on. It always pops; its words wait their turn when it has time to read them.</summary>
        private void StatusLanded(int slot, float left)
        {
            _chipPopAt[slot] = Time.unscaledTime;
            if (left >= MinTextSeconds && _statusQueued < _statusQueue.Length) _statusQueue[_statusQueued++] = slot;
        }

        private void StatusDropQueued(int slot)
        {
            for (int k = 0; k < _statusQueued; k++)
                if (_statusQueue[k] == slot) { _statusQueue[k] = _statusQueue[--_statusQueued]; return; }
        }

        private void PaintStatusChips(CharacterMotor local, bool show)
        {
            if (_chip.Length == 0 || _chip[0] == null) return;
            StatusIcons.Live(show ? local : null, _liveStatuses);
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            bool still = LiveHud.Still;
            bool tab = show && BoardHeld;

            // Landings: an effect that was off and is now on. A refresh changes nothing here.
            for (int i = 0; i < StatusChips; i++)
            {
                var kind = StatusRules.All[i].Kind;
                bool on = _liveStatuses.IndexOf(kind) >= 0;
                if (on && !_chipWas[i]) StatusLanded(i, local.StatusLeft(kind));
                else if (!on && _chipWas[i]) { StatusDropQueued(i); if (_statusLanding == i) _statusLanding = -1; }
                _chipWas[i] = on;
            }

            // Whose words are up: the current one until its time is out or its effect is, then the waiting effect that expires first.
            if (_statusLanding >= 0)
            {
                var kind = StatusRules.All[_statusLanding].Kind;
                float span = _statusQueued > 0 ? QueuedSeconds : LandSeconds;
                if (!_chipWas[_statusLanding] || local.StatusLeft(kind) <= 0 || now - _statusLandingAt >= span) _statusLanding = -1;
            }
            while (_statusLanding < 0 && _statusQueued > 0)
            {
                int best = -1; float soonest = float.MaxValue;
                for (int k = 0; k < _statusQueued; k++)
                {
                    int slot = _statusQueue[k];
                    float left = _chipWas[slot] ? local.StatusLeft(StatusRules.All[slot].Kind) : 0;
                    if (left < MinTextSeconds) { _statusQueue[k--] = _statusQueue[--_statusQueued]; continue; }
                    if (left < soonest) { soonest = left; best = slot; }
                }
                if (best < 0) break;
                StatusDropQueued(best);
                _statusLanding = best; _statusLandingAt = now; _chipPopAt[best] = now;
            }

            for (int i = 0; i < StatusChips; i++)
            {
                var kind = StatusRules.All[i].Kind;
                int order = _liveStatuses.IndexOf(kind);
                bool on = order >= 0;
                if (_chip[i].gameObject.activeSelf != on) _chip[i].gameObject.SetActive(on);
                if (!on) { _chipFocus[i] = 0; _chipPlaced[i] = false; SetListText(i, false); continue; }

                // Rings sit in a row right of the score (six to a row, so a crowd never runs under the ultimate) and ease to a new place when another leaves.
                var target = new Vector2(RingFirstX + (order % RingsPerRow) * RingStepX, RingY + (order / RingsPerRow) * RingRowStep);
                if (!_chipPlaced[i]) { _chipPlaced[i] = true; _chipFrom[i] = _chipTarget[i] = target; _chipMovedAt[i] = now - 1; }
                else if ((_chipTarget[i] - target).sqrMagnitude > .01f) { _chipFrom[i] = _chip[i].anchoredPosition; _chipTarget[i] = target; _chipMovedAt[i] = now; }
                var at = Vector2.Lerp(_chipFrom[i], _chipTarget[i], still ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - _chipMovedAt[i]) / .2f)));
                if (_chip[i].anchoredPosition != at) _chip[i].anchoredPosition = at;

                var sprite = StatusIcons.For(kind);
                if (_chipIcon[i].sprite != sprite) _chipIcon[i].sprite = sprite;
                _chipIcon[i].enabled = sprite != null;
                if (kind == StatusKind.Zapped && _zappedFallback != null) _zappedFallback.enabled = sprite == null;
                float total = Mathf.Max(0.01f, local.StatusTotal(kind));
                float left = Mathf.Max(0, local.StatusLeft(kind));
                _chipRing[i].Set(Mathf.Clamp01(left / total));
                _chipRing[i].Paint(LiveHud.Coral, new Color(1, 1, 1, .16f));
                int whole = Mathf.CeilToInt(left);
                if (_chipSeconds[i] != whole) { _chipSeconds[i] = whole; _chipTime[i].text = whole.ToString(System.Globalization.CultureInfo.InvariantCulture); }

                // The swell: a landing ring grows to 84 with the rim behind it, overshooting once; an ordinary pop is a small bump.
                bool landing = _statusLanding == i && !tab;
                if (landing) _chipFocus[i] = 1; else _chipFocus[i] = Mathf.MoveTowards(_chipFocus[i], 0, still ? 1 : dt / .2f);
                float focus = _chipFocus[i];
                float swell = landing ? (still ? 1 : LiveHud.Pop((now - _chipPopAt[i]) / .3f)) : focus;
                float bump = !landing && focus <= 0 && !still ? Mathf.Sin(Mathf.Clamp01((now - _chipPopAt[i]) / .4f) * Mathf.PI) * .12f : 0;
                float scale = 1 + (FocusScale - 1) * swell + bump;
                _chipBody[i].localScale = new Vector3(scale, scale, 1);
                bool rim = focus > 0;
                if (_chipRim[i].enabled != rim) _chipRim[i].enabled = rim;
                if (rim)
                {
                    _chipRim[i].rectTransform.localScale = Vector3.one * Mathf.Max(.01f, swell);
                    _chipRim[i].color = new Color(1, 1, 1, Mathf.Clamp01(focus * 2));
                }
                var disc = new Color(LiveHud.Glass.r, LiveHud.Glass.g, LiveHud.Glass.b, Mathf.Lerp(95 / 255f, 1, focus));
                if (_chipDisc[i].color != disc) _chipDisc[i].color = disc;
                // Only the lowered seconds show while swollen, and the words over it.
                var seconds = (RectTransform)_chipTime[i].rectTransform.parent;
                var lowered = new Vector2(0, Mathf.Lerp(-44, -58, focus));
                if (seconds.anchoredPosition != lowered) seconds.anchoredPosition = lowered;
                seconds.localScale = Vector3.one * Mathf.Lerp(1, 26f / 24f, focus);

                if (_chipName[i].enabled != landing) _chipName[i].enabled = _chipTip[i].enabled = landing;
                if (landing)
                {
                    float span = _statusQueued > 0 ? QueuedSeconds : LandSeconds;
                    float alpha = still ? 1 : Mathf.Clamp01((now - _statusLandingAt) / .15f);
                    alpha = Mathf.Min(alpha, Mathf.Clamp01(Mathf.Min(span - (now - _statusLandingAt), left) / .25f));
                    var tint = Color.white; tint.a = alpha;
                    if (_chipName[i].color != tint) { _chipName[i].color = tint; _chipTip[i].color = tint; }
                }
                SetListText(i, tab);
            }
            if (tab) PlaceStatusList();
        }

        private void SetListText(int slot, bool on)
        {
            if (_listName[slot].enabled != on) _listName[slot].enabled = _listTip[slot].enabled = on;
        }

        /// <summary>Stacks the Tab list's entries upward from just above the row, in the row's order.</summary>
        private void PlaceStatusList()
        {
            float y = RingY + 62 + Mathf.Max(0, (_liveStatuses.Count - 1) / RingsPerRow) * RingRowStep;
            for (int order = 0; order < _liveStatuses.Count; order++)
            {
                int slot = IndexOfStatus(_liveStatuses[order]);
                if (slot < 0) continue;
                _listTip[slot].rectTransform.anchoredPosition = new Vector2(RingFirstX - 26, y);
                y += _chipTipHeight[slot] + 4;
                _listName[slot].rectTransform.anchoredPosition = new Vector2(RingFirstX - 34, y);
                y += 44 + 12;
            }
        }

        private static int IndexOfStatus(StatusKind kind)
        {
            for (int i = 0; i < StatusChips; i++) if (StatusRules.All[i].Kind == kind) return i;
            return -1;
        }

        /// <summary>For tests: whose words are up over the row, or -1.</summary>
        internal int StatusLandingSlot => _statusLanding;
    }
}
