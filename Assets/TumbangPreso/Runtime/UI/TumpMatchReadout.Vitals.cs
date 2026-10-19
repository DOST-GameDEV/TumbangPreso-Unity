using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § THE IDENTITY PLATE: your portrait and the thing you carry, bottom left (in-game UI
    /// overhaul, 2026-10-20).
    ///
    /// ⚠️⚠️ OVERWATCH'S CORNER, NOT A SECOND SCOREBOARD. Overwatch splits the screen by WHOSE the
    /// information is: the match at the top centre, the feed at the top right, your powers at the
    /// bottom right, and YOU at the bottom left. Until this pass the bottom left held two sentences
    /// that were almost always switched off, so nothing on screen said "this is you" except a
    /// honey bar under one of four chips.
    ///
    /// ⚠️⚠️ NO STAMINA BAR, BY REQUEST. The first version of this plate drew stamina as ten leaning
    /// segments, Overwatch's health bar. Owner, 2026-10-20: "remove healthbar in bottom left corner.
    /// but keep the player icon along with the slipper icon. just make it look better together
    /// without the healthbar." Stamina is the reticle arc's job again on every device
    /// (`TumpMatchReadout.StaminaArc`), and the plate is two pictures on one slab:
    ///
    /// - your PORTRAIT on a leaning honey tile (honey means you in this HUD), standing taller than
    ///   the slab the way Overwatch's hero portrait rises out of its corner;
    /// - the OBJECT you play with, as its own baked 3D icon from `Resources/UI/portraits`: your
    ///   equipped tsinelas when you throw, the can you guard when you are taya. Whole while the
    ///   slipper is in hand or the can stands, faded while it is away or down, which is the chip's
    ///   rule, so the corner and the top bar never disagree;
    /// - one seat-colour strip along the foot of both, which is what makes them read as one plate.
    ///   The seat colour is a strip and never the tile: P4's identity colour is a blue
    ///   (`PlayerIdentity`), which the palette rule allows as a small swatch only.
    ///
    /// ⚠️ NO WORDS ON IT. `VISION.md` § 3: the match HUD carries no sentences. A stun or a swim
    /// still shows as the honey star or the wave on the slab.
    ///
    /// ⚠️ KEYBOARD AND PAD ONLY. A phone's move stick and its arc of verbs own the bottom-left
    /// corner (`TouchHud.StickCentreX/Y`, 330 units in), so on touch the plate is hidden.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _vitals;
        private HudCard _vitalsTile, _vitalsFoot;
        private Image _vitalsPortrait, _vitalsObject;
        private HudBadge _vitalsRole;
        private Sprite _vitalsSprite, _vitalsObjectSprite;
        private int _vitalsSkinSlot = -1, _vitalsSkin = -1;
        private bool _vitalsSkinFound;
        private float _vitalsScanAt;

        /// <summary>The plate's base size, the deck's own 25 per cent (Feedback 2026-09-30), so
        /// the two bottom corners read as one size. `HudReadingLayout` scales on top of it.</summary>
        internal const float VitalsScale = 1.25f;

        /// <summary>Canvas units from the bottom of the screen to the top of the plate, while it
        /// is drawn. The status stack starts above it.</summary>
        internal const float VitalsTop = 28 + 120 * VitalsScale + 14;

        public bool VitalsVisible => _vitals != null && _vitals.gameObject.activeInHierarchy;

        private void BuildVitals()
        {
            _vitals = OwnerUiLayout.Rect(_personalRoot, "Vitals");
            OwnerUiLayout.Fill(_vitals);
            // One slab under both pictures; the portrait tile overlaps its left end.
            var plate = OwnerUiLayout.Rect(_vitals, "VitalsPlate").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(plate.rectTransform, 30, 26, 166, 86);
            plate.Glass(HudDraw.Glass).raycastTarget = false;

            _vitalsTile = OwnerUiLayout.Rect(_vitals, "VitalsTile").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(_vitalsTile.rectTransform, 8, 6, 92, 106);
            _vitalsTile.Glass(HudDraw.Honey, HudDraw.Lean, 3, .4f); _vitalsTile.BorderWidth = 0;
            _vitalsTile.FollowContrast = false; _vitalsTile.raycastTarget = false;
            _vitalsPortrait = OwnerPortraitArt.Create(_vitals, "VitalsPortrait", "");
            OwnerUiLayout.Place(_vitalsPortrait.rectTransform, 14, 14, 84, 84);

            // The object: the baked icon of the equipped tsinelas or the guarded can.
            _vitalsObject = OwnerUiLayout.Rect(_vitals, "VitalsObject").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(_vitalsObject.rectTransform, 106, 28, 84, 84);
            _vitalsObject.preserveAspect = true; _vitalsObject.raycastTarget = false; _vitalsObject.enabled = false;

            // A drawn fallback for an object with no baked icon, and the stun or swim mark.
            _vitalsRole = OwnerUiLayout.Rect(_vitals, "VitalsRole").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_vitalsRole.rectTransform, 130, 50, 36, 42);
            _vitalsRole.Detail = HudDraw.GlassDeep; _vitalsRole.Rim = Color.clear; _vitalsRole.raycastTarget = false;

            _vitalsFoot = OwnerUiLayout.Rect(_vitals, "VitalsFoot").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(_vitalsFoot.rectTransform, 12, 112, 182, 7);
            _vitalsFoot.Glass(HudDraw.Honey, HudDraw.Lean, 1, .3f); _vitalsFoot.BorderWidth = 0;
            _vitalsFoot.FollowContrast = false; _vitalsFoot.raycastTarget = false;
        }

        /// <summary>Paints the plate for the local player; hidden on touch and for watchers.</summary>
        private void PaintVitals(CharacterMotor local, bool show)
        {
            if (_vitals == null) return;
            show &= !Hud.OnTouch && local != null;
            if (_vitals.gameObject.activeSelf != show) _vitals.gameObject.SetActive(show);
            if (!show) return;

            var seat = PlayerIdentity.Colour(local.PlayerSlot);
            if (_vitalsFoot.color != seat) _vitalsFoot.color = seat;
            var sprite = PortraitFor(local);
            if (_vitalsSprite != sprite) { _vitalsSprite = sprite; _vitalsPortrait.sprite = sprite; }
            _vitalsPortrait.enabled = sprite != null;

            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            bool whole = local.IsDefender ? lata == null || lata.IsUpright : local.HoldingSlipper;
            var objectSprite = local.IsDefender ? CanIcon(lata) : SlipperIcon(local.PlayerSlot);
            if (_vitalsObjectSprite != objectSprite) { _vitalsObjectSprite = objectSprite; _vitalsObject.sprite = objectSprite; }
            _vitalsObject.enabled = objectSprite != null;
            var tint = Color.white; tint.a = whole ? 1 : .32f;
            if (_vitalsObject.color != tint) _vitalsObject.color = tint;

            // A player who cannot act is the more urgent fact; otherwise the badge only stands in
            // for an object that has no baked icon.
            var ink = HudDraw.Ivory; var faded = ink; faded.a = .32f;
            if (local.IsSwimming) _vitalsRole.Show(HudBadge.Glyph.Wave, ink, Color.clear);
            else if (local.IsTripped || local.IsStunned) _vitalsRole.Show(HudBadge.Glyph.Star, HudDraw.Honey, Color.clear);
            else if (objectSprite != null) _vitalsRole.Show(HudBadge.Glyph.None, Color.clear, Color.clear);
            else if (local.IsDefender) _vitalsRole.Show(whole ? HudBadge.Glyph.Can : HudBadge.Glyph.CanDown, whole ? ink : faded, Color.clear);
            else _vitalsRole.Show(HudBadge.Glyph.Slipper, whole ? ink : faded, Color.clear);
        }

        /// <summary>The baked icon of the tsinelas this seat throws. The skin is fixed for a match,
        /// so the scene is searched every quarter second only until this seat's slipper exists and
        /// never again after (`CLAUDE.md` § 7.1). ⚠️ The HUD is built before `MatchInstaller` spawns
        /// the slippers, and the first version, retrying once a second, was still drawing the
        /// fallback glyph in the first native capture.</summary>
        private Sprite SlipperIcon(int slot)
        {
            if (slot != _vitalsSkinSlot) { _vitalsSkinSlot = slot; _vitalsSkin = -1; _vitalsSkinFound = false; _vitalsScanAt = 0; }
            if (!_vitalsSkinFound && Time.unscaledTime >= _vitalsScanAt)
            {
                _vitalsScanAt = Time.unscaledTime + .25f;
                foreach (var slipper in FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (slipper != null && (slipper.SeatOfOrigin == slot || (slipper.SeatOfOrigin < 0 && slipper.OwnerSlot == slot)))
                    { _vitalsSkin = slipper.SkinIndex; _vitalsSkinFound = true; break; }
            }
            return _vitalsSkinFound ? IconFor(Roster.Slippers, _vitalsSkin) : null;
        }

        /// <summary>The baked icon of the can in play, which the taya guards.</summary>
        private static Sprite CanIcon(Lata lata) => lata != null ? IconFor(Roster.Cans, lata.SkinIndex) : null;

        /// <summary>
        /// ⚠️ AN UNPICKED PROP WEARS ENTRY 0, SO ITS ICON IS ENTRY 0's. A player who never chose a
        /// tsinelas carries skin -1, and `RosterBook.Resolve` dresses any index outside the list in
        /// entry 0 (`CLAUDE.md` § 4: entry 0 of each prop list stays neutral). The first version
        /// answered null for -1 and drew the fallback glyph beside a perfectly ordinary tsinelas.
        /// </summary>
        private static Sprite IconFor(System.Collections.Generic.IReadOnlyList<RosterEntry> list, int index)
        {
            var entry = Roster.At(list, index) ?? (list.Count > 0 ? list[0] : null);
            return entry != null ? OwnerPortraitArt.Get("UI/portraits/" + entry.Id) : null;
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
