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
    /// - a compact filled centre dot, with no weapon-like cardinal ticks;
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
        private int _ownerSlot = -1;
        private float _fullPulseLeft, _releasePulseLeft;
        private const float FullPulseSeconds = .18f, ReleasePulseSeconds = .22f;
        public float ReleasePulseRemaining => _releasePulseLeft;
        public void SetOwner(int slot) => _ownerSlot = slot;
        private static bool Still => Settings.SettingsStore.Current.ReducedUiMotion
            || Settings.SettingsStore.Current.ReducedEffects;
        public float AimRadius => 3.2f + (Still ? 0 :
            0.4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_fullPulseLeft / FullPulseSeconds))
            + .7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_releasePulseLeft / ReleasePulseSeconds)));

        private void OnPresented(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != Visual.MatchFlair.Kind.Throw || actor != _ownerSlot || _ownerSlot < 0
                || !isActiveAndEnabled || canvas == null || !canvas.isActiveAndEnabled || Still) return;
            // The existing host-confirmed presentation event is the receipt. A cancelled
            // charge or refused input never invents a successful-release pulse.
            _releasePulseLeft = ReleasePulseSeconds;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (_fullPulseLeft <= 0 && _releasePulseLeft <= 0) return;
            if (Still) { _fullPulseLeft = _releasePulseLeft = 0; SetVerticesDirty(); return; }
            if (Time.deltaTime <= 0) return;
            _fullPulseLeft = Mathf.Max(0, _fullPulseLeft - Time.deltaTime);
            _releasePulseLeft = Mathf.Max(0, _releasePulseLeft - Time.deltaTime);
            SetVerticesDirty();
        }
        private int _captionPercent = -1, _captionState = -1;
        public string ChargeCaption => _chargeCaption != null && _chargeCaption.enabled ? _chargeCaption.text : "";

        protected override void OnEnable()
        {
            base.OnEnable();
            Visual.MatchFlair.Presented += OnPresented;
            if (_chargeCaption != null) _chargeCaption.enabled = Charge > 0;
        }
        protected override void OnDisable()
        {
            Visual.MatchFlair.Presented -= OnPresented;
            _fullPulseLeft = _releasePulseLeft = 0; _ownerSlot = -1;
            if (_chargeCaption != null) _chargeCaption.enabled = false;
            base.OnDisable();
        }

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
            if (charge >= .999f && Charge < .999f && !refused && !Still)
                _fullPulseLeft = FullPulseSeconds;
            if (charge <= 0 || refused) _fullPulseLeft = 0;
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
            // Current Feedback: a filled dot precisely on the existing aim centre.
            float radius = AimRadius;
            HudDraw.Disc(vh, c, radius + 1.5f, Keel, 32);
            HudDraw.Disc(vh, c, radius, InReach ? UiTheme.Defense : ink, 32);
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
            // Reach is shown by the dot's role tint, never a gun hitmarker.
        }
    }
}
