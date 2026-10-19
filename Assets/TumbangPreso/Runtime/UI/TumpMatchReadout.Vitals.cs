using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § THE VITALS PLATE: your portrait, your role's object and your stamina, bottom left
    /// (in-game UI overhaul, 2026-10-20).
    ///
    /// ⚠️⚠️ OVERWATCH'S CORNER, NOT A SECOND SCOREBOARD. The request was a HUD "similar to
    /// overwatch's simple ingame hud". Overwatch splits the screen by WHOSE the information is:
    /// the match at the top centre, the feed at the top right, your powers at the bottom right,
    /// and YOU (portrait and health) at the bottom left. Until this pass the bottom left held two
    /// sentences that were almost always switched off, and your stamina only appeared as an arc
    /// beside the reticle while it was draining, so nothing on screen said "this is you" except a
    /// honey tile hanging under one of four chips.
    ///
    /// ⚠️ NO WORDS ON IT. The portrait says who, the can or the slipper says the role (whole when
    /// the can stands or the slipper is in hand, faded when not, exactly the chip's rule), and the
    /// segments say how much running is left. `VISION.md` § 3: the match HUD carries no sentences.
    ///
    /// ⚠️ KEYBOARD AND PAD ONLY. A phone's move stick and its arc of verbs own the bottom-left
    /// corner (`TouchHud.StickCentreX/Y`, 330 units in), so on touch the plate is hidden and the
    /// stamina arc beside the reticle keeps its full job. Leaving it drawn would put the bar
    /// under the player's left thumb, which is § 6.2c's "measured against the rectangle the
    /// player actually sees" lesson in a corner.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _vitals;
        private HudCard _vitalsTile;
        private Image _vitalsPortrait;
        private HudBadge _vitalsRole;
        private HudVitalsBar _vitalsBar;
        private Sprite _vitalsSprite;

        /// <summary>The plate's base size, the deck's own 25 per cent (Feedback 2026-09-30), so
        /// the two bottom corners read as one size. `HudReadingLayout` scales on top of it.</summary>
        internal const float VitalsScale = 1.25f;

        /// <summary>Canvas units from the bottom of the screen to the top of the vitals plate,
        /// while it is drawn. The status stack starts above it.</summary>
        internal const float VitalsTop = 28 + 120 * VitalsScale + 14;

        public bool VitalsVisible => _vitals != null && _vitals.gameObject.activeInHierarchy;

        private void BuildVitals()
        {
            _vitals = OwnerUiLayout.Rect(_personalRoot, "Vitals");
            OwnerUiLayout.Fill(_vitals);
            var plate = OwnerUiLayout.Rect(_vitals, "VitalsPlate").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(plate.rectTransform, 40, 40, 360, 72);
            plate.Glass(HudDraw.Glass).raycastTarget = false;

            // The portrait stands taller than the plate on a leaning HONEY tile (honey means you, in
            // this HUD), the way Overwatch's hero portrait rises out of its health bar. Your seat
            // colour is a strip along its foot rather than the whole tile: P4's identity colour is a
            // blue (`PlayerIdentity`), which the palette rule allows as a small swatch and never as
            // a 90 x 100 surface.
            _vitalsTile = OwnerUiLayout.Rect(_vitals, "VitalsTile").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(_vitalsTile.rectTransform, 8, 12, 90, 100);
            _vitalsTile.Glass(HudDraw.Honey, HudDraw.Lean, 3, .4f); _vitalsTile.BorderWidth = 0; _vitalsTile.AccentHeight = 7;
            _vitalsTile.FollowContrast = false; _vitalsTile.raycastTarget = false;
            _vitalsPortrait = OwnerPortraitArt.Create(_vitals, "VitalsPortrait", "");
            OwnerUiLayout.Place(_vitalsPortrait.rectTransform, 15, 18, 80, 80);

            _vitalsRole = OwnerUiLayout.Rect(_vitals, "VitalsRole").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_vitalsRole.rectTransform, 112, 46, 28, 32);
            _vitalsRole.Detail = HudDraw.GlassDeep; _vitalsRole.Rim = Color.clear; _vitalsRole.raycastTarget = false;

            _vitalsBar = OwnerUiLayout.Rect(_vitals, "VitalsStamina").gameObject.AddComponent<HudVitalsBar>();
            OwnerUiLayout.Place(_vitalsBar.rectTransform, 110, 84, 278, 18);
            _vitalsBar.color = HudDraw.Ivory; _vitalsBar.raycastTarget = false;
        }

        /// <summary>Paints the plate for the local player; hidden on touch and for watchers.</summary>
        private void PaintVitals(CharacterMotor local, bool show)
        {
            if (_vitals == null) return;
            show &= !Hud.OnTouch && local != null && local.Stamina != null;
            if (_vitals.gameObject.activeSelf != show) _vitals.gameObject.SetActive(show);
            if (!show) return;

            var seat = PlayerIdentity.Colour(local.PlayerSlot);
            if (_vitalsTile.Accent != seat) { _vitalsTile.Accent = seat; _vitalsTile.SetVerticesDirty(); }
            var sprite = PortraitFor(local);
            if (_vitalsSprite != sprite) { _vitalsSprite = sprite; _vitalsPortrait.sprite = sprite; }
            _vitalsPortrait.enabled = sprite != null;

            // The chip's rule, so the corner and the bar can never disagree about your seat.
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            var ink = HudDraw.Ivory; var faded = ink; faded.a = .32f;
            if (local.IsSwimming) _vitalsRole.Show(HudBadge.Glyph.Wave, ink, Color.clear);
            else if (local.IsTripped || local.IsStunned) _vitalsRole.Show(HudBadge.Glyph.Star, HudDraw.Honey, Color.clear);
            else if (local.IsDefender) _vitalsRole.Show(lata == null || lata.IsUpright ? HudBadge.Glyph.Can : HudBadge.Glyph.CanDown,
                lata == null || lata.IsUpright ? ink : faded, Color.clear);
            else _vitalsRole.Show(HudBadge.Glyph.Slipper, local.HoldingSlipper ? ink : faded, Color.clear);

            // Fatigue turns the bar the arc's own orange and DRAINED its dull red, so the corner and
            // the reticle speak one colour language about one resource.
            bool drained = local.IsDrained, fatigued = local.Stamina.IsFatigued;
            var fill = drained ? new Color(.62f, .30f, .34f, 1f) : fatigued ? OwnerUiTheme.Current.Orange : HudDraw.Ivory;
            _vitalsBar.Set(local.Stamina.Ratio, fill);
        }

        /// <summary>The portrait art for an actor's character, or null for a custom one without art.</summary>
        private static Sprite PortraitFor(CharacterMotor actor)
        {
            var people = Roster.GetPeople(actor.Mode);
            return actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count
                ? OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id) : null;
        }
    }
}
