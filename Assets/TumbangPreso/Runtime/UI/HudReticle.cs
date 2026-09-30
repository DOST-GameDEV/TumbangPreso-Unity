using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The reticle as the personal-state hub (TODO VISUAL-1.6).
    ///
    /// ⚠️⚠️ THE EYE IS ALREADY HERE, SO THE STATE COMES HERE. Charging a throw used to put a bar
    /// and two sentences at the bottom prompt ("Release to throw", "Pektus left · 34%", "Move
    /// the aim sideways for pektus"), and the throw and tag cooldowns were "THROW CD · 1.0s"
    /// lines under the reticle. Every one of them asked the player to look away from the aim
    /// point at the exact moment the aim point mattered. They are shapes on the reticle now:
    ///
    /// - a dot and four short ticks, drawn, instead of a "+" in the display face;
    /// - a CHARGE ring that appears at the charge floor (`Balance.ChargeMinPower`, a third of
    ///   the way round) and fills to full power, clockwise from twelve like every timer here;
    /// - a PEKTUS tick: a short arc outside the ring on the side the throw will curve, as long
    ///   as the spin is strong;
    /// - a COOLDOWN sweep: a thin outer arc draining while a verb (throw, shove, lunge, tag)
    ///   cannot be used yet;
    /// - REFUSED: the whole reticle greys while a throw would be refused (the can is protected),
    ///   which is the existing rule shown rather than a new one.
    ///
    /// ⚠️ EVERY PIECE HAS A BLACK KEEL, because the reticle crosses sky, chalk and asphalt within
    /// one sweep of the mouse (AGENTS: black in-game outlines).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudReticle : MaskableGraphic
    {
        public float Charge;          // 0 when not charging, otherwise the charge ratio.
        public float Pektus;          // signed spin, negative curves left.
        public float Cooldown;        // 0..1 remaining share of the longest verb cooldown.
        public bool Refused;
        public bool InReach;          // taya only (VISUAL-1.2): a catchable attacker is in reach.
        public static readonly Color Keel = new Color(0, 0, 0, .85f);
        public static readonly Color RefusedInk = new Color(.62f, .60f, .58f, .9f);
        private Text _chargeCaption;
        private int _captionPercent = -1, _captionState = -1;
        public string ChargeCaption => _chargeCaption != null && _chargeCaption.enabled ? _chargeCaption.text : "";

        protected override void OnEnable()
        { base.OnEnable(); if (_chargeCaption != null) _chargeCaption.enabled = Charge > 0; }
        protected override void OnDisable()
        { if (_chargeCaption != null) _chargeCaption.enabled = false; base.OnDisable(); }

        private void PaintChargeCaption(float charge, bool refused)
        {
            if (_chargeCaption == null)
            {
                _chargeCaption = OwnerUiLayout.Text(transform, "ThrowChargeState", "", 22, OwnerUiLayout.TypeRole.Display);
                _chargeCaption.alignment = TextAnchor.MiddleCenter;
                _chargeCaption.horizontalOverflow = HorizontalWrapMode.Overflow;
                _chargeCaption.verticalOverflow = VerticalWrapMode.Overflow;
                var rect = _chargeCaption.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0, -56); rect.sizeDelta = new Vector2(192, 28);
                var edge = _chargeCaption.gameObject.AddComponent<Outline>();
                edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(1, -1);
            }
            bool charging = charge > 0;
            _chargeCaption.enabled = charging && isActiveAndEnabled;
            int percent = Mathf.Clamp(Mathf.FloorToInt(charge * 100 + .0001f), 0, 100);
            int state = !charging ? 0 : refused ? 1 : charge >= .999f ? 2 : 3;
            if (state == _captionState && percent == _captionPercent) return;
            _captionState = state; _captionPercent = percent;
            _chargeCaption.text = state == 0 ? "" : state == 1 ? "WAIT" : state == 2 ? "FULL · RELEASE" : percent + "%";
            _chargeCaption.color = state == 1 ? RefusedInk : state == 2 ? CourtPresentationPalette.Gold : CourtPresentationPalette.Paper;
        }

        public void Set(float charge, float pektus, float cooldown, bool refused, bool inReach)
        {
            PaintChargeCaption(charge, refused);
            if (Mathf.Abs(charge - Charge) < .004f && Mathf.Abs(pektus - Pektus) < .01f
                && Mathf.Abs(cooldown - Cooldown) < .004f && refused == Refused && inReach == InReach) return;
            Charge = charge; Pektus = pektus; Cooldown = cooldown; Refused = refused; InReach = inReach;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var c = GetPixelAdjustedRect().center;
            var ink = Refused ? RefusedInk : color;
            var gold = CourtPresentationPalette.Gold;
            // Dot and ticks.
            HudDraw.Disc(vh, c, 4.2f, Keel, 16); HudDraw.Disc(vh, c, 2.6f, ink, 16);
            float gap = Charge > 0 ? 9 : 7, len = 8;
            for (int k = 0; k < 4; k++)
            {
                float a = k * 90 * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                HudDraw.Bar(vh, c + d * (gap - 1.5f), c + d * (gap + len + 1.5f), 5.5f, Keel);
            }
            for (int k = 0; k < 4; k++)
            {
                float a = k * 90 * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                HudDraw.Bar(vh, c + d * gap, c + d * (gap + len), 2.6f, InReach ? UiTheme.Defense : ink);
            }
            // Cooldown sweep: thin, outside everything, drains clockwise.
            if (Cooldown > .001f)
            {
                HudDraw.Arc(vh, c, 36, 31, 90, 360 * Cooldown, Keel, 72);
                HudDraw.Arc(vh, c, 35, 32, 90, 360 * Cooldown, new Color(ink.r, ink.g, ink.b, .85f), 72);
            }
            // Charge ring.
            if (Charge > 0)
            {
                HudDraw.Arc(vh, c, 28, 20, 90, 360, new Color(0, 0, 0, .45f), 72);
                HudDraw.Arc(vh, c, 27, 21, 90, 360 * Mathf.Clamp01(Charge), Refused ? RefusedInk : Charge >= .999f ? gold : ink, 72);
                if (Mathf.Abs(Pektus) > .08f)
                {
                    // Left spin draws on the left of the ring, right on the right, centred on
                    // the horizontal, up to a quarter turn long at full spin.
                    float span = Mathf.Lerp(18, 90, Mathf.Clamp01(Mathf.Abs(Pektus)));
                    float mid = Pektus < 0 ? 180 : 0;
                    HudDraw.Arc(vh, c, 40, 32, mid + span * .5f, span, Keel, 72);
                    HudDraw.Arc(vh, c, 38.5f, 33.5f, mid + span * .5f, span, Refused ? RefusedInk : gold, 72);
                }
            }
            // Taya ready tick (VISUAL-1.2): a small filled chevron above the reticle.
            if (InReach)
            {
                HudDraw.Fan(vh, c + new Vector2(0, 30), new[] { c + new Vector2(-9, 36), c + new Vector2(9, 36), c + new Vector2(0, 24) }, Keel);
                HudDraw.Fan(vh, c + new Vector2(0, 31), new[] { c + new Vector2(-6, 34.5f), c + new Vector2(6, 34.5f), c + new Vector2(0, 26.5f) }, UiTheme.Defense);
            }
        }
    }
}
