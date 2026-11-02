using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § YOUR SCORE CORNER, bottom left (live HUD, 2026-11-01; `boot_match.vitals` and `own_score`).
    ///
    /// Your hero stands bare on a soft lime band with the thing you play with tilted flat behind
    /// it (the tsinelas you throw, or the can you guard, from `Resources/UI/slippers-flat` with the
    /// baked portrait as the fallback), your score in painted lime lettering and your place in yellow
    /// above it. It replaces the glass plate, the honey tile and the foot strip. Your score is the one
    /// number you read all match, so it lives here and not in the top bar.
    ///
    /// ⚠️ NO WORDS ON IT. `VISION.md` § 3: the match HUD carries no sentences. A stun or a swim
    /// still shows as the honey star or the wave beside the hero.
    ///
    /// ⚠️ KEYBOARD AND PAD ONLY. A phone's move stick and its arc of verbs own the bottom-left
    /// corner (`TouchHud.StickCentreX/Y`, 330 units in), so on touch the corner is hidden.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _vitals;
        private Image _vitalsBand;
        private Image _vitalsPortrait, _vitalsObject;
        private HudBadge _vitalsRole;
        private Text _ownScore, _ownPlace, _ownGain;
        private Sprite _vitalsSprite, _vitalsObjectSprite;
        private int _vitalsSkinSlot = -1, _vitalsSkin = -1;
        private bool _vitalsSkinFound;
        private float _vitalsScanAt;

        /// <summary>The corner is drawn at the boards' own size; `HudReadingLayout` scales on top of it.</summary>
        internal const float VitalsScale = 1f;

        /// <summary>Canvas units from the bottom of the screen to the top of the corner's art, while it
        /// is drawn. The status stack starts above it.</summary>
        internal const float VitalsTop = 215;

        public bool VitalsVisible => _vitals != null && _vitals.gameObject.activeInHierarchy;

        /// <summary>Places a rect by a board position (1920 x 1080, y down) measured from the bottom-left corner.</summary>
        private static void AtCorner(RectTransform rect, float x, float boardY, float w, float h, float pivotX = .5f)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(pivotX, .5f);
            rect.anchoredPosition = new Vector2(x, 1080 - boardY); rect.sizeDelta = new Vector2(w, h);
        }

        private void BuildVitals()
        {
            _vitals = OwnerUiLayout.Rect(_personalRoot, "Vitals");
            _vitals.anchorMin = _vitals.anchorMax = Vector2.zero; _vitals.pivot = Vector2.zero;
            _vitals.anchoredPosition = Vector2.zero; _vitals.sizeDelta = new Vector2(1, 1);
            // A soft lime band behind it all, melting away at both ends.
            _vitalsBand = LiveHud.Band(_vitals, "VitalsBand", LiveHud.Lime, .55f);
            AtCorner(_vitalsBand.rectTransform, 220, 1019, 520, 70);

            // The held thing: flat, tilted and behind the hero.
            _vitalsObject = OwnerUiLayout.Rect(_vitals, "VitalsObject").gameObject.AddComponent<Image>();
            AtCorner(_vitalsObject.rectTransform, 150, 960, 170, 170);
            _vitalsObject.rectTransform.localRotation = Quaternion.Euler(0, 0, -20);
            _vitalsObject.preserveAspect = true; _vitalsObject.raycastTarget = false; _vitalsObject.enabled = false;

            _vitalsPortrait = OwnerPortraitArt.Create(_vitals, "VitalsPortrait", "");
            AtCorner(_vitalsPortrait.rectTransform, 86, 992, 124, 124);

            // A drawn fallback for an object with no baked icon, and the stun or swim mark.
            _vitalsRole = OwnerUiLayout.Rect(_vitals, "VitalsRole").gameObject.AddComponent<HudBadge>();
            AtCorner(_vitalsRole.rectTransform, 160, 1010, 36, 42);
            _vitalsRole.Detail = HudDraw.GlassDeep; _vitalsRole.Rim = Color.clear; _vitalsRole.raycastTarget = false;

            _ownScore = LiveHud.Lettering(_vitals, "YourScore", "", 74, LiveHud.Lime, LiveHud.LimeInk, LiveHud.LimeInk, 7, 3, 8);
            _ownScore.alignment = TextAnchor.MiddleLeft;
            AtCorner(_ownScore.rectTransform, 156, 1000, 330, 100, 0);
            _ownPlace = LiveHud.Lettering(_vitals, "YourPlace", "", 34, LiveHud.Yellow, LiveHud.YellowInk, LiveHud.YellowInk, 3, 2, 3);
            _ownPlace.alignment = TextAnchor.MiddleLeft;
            AtCorner(_ownPlace.rectTransform, 156, 951, 160, 50, 0);
            _ownGain = LiveHud.Lettering(_vitals, "YourGain", "", 40, LiveHud.Lime, LiveHud.LimeInk, LiveHud.LimeInk, 4, 2, 3);
            _ownGain.alignment = TextAnchor.MiddleLeft; _ownGain.enabled = false;
            AtCorner(_ownGain.rectTransform, 300, 990, 200, 60, 0);
        }

        /// <summary>Paints the corner for the local player; hidden on touch and for watchers.</summary>
        private void PaintVitals(CharacterMotor local, bool show)
        {
            if (_vitals == null) return;
            show &= !Hud.OnTouch && local != null;
            if (_vitals.gameObject.activeSelf != show) _vitals.gameObject.SetActive(show);
            if (!show) return;

            // The hero carries the crisp dark stroke (baked into `live-hud/hero-corner`, since the portraits are never changed).
            var sprite = CornerHero(local, out bool heroBaked);
            if (_vitalsSprite != sprite)
            {
                _vitalsSprite = sprite; _vitalsPortrait.sprite = sprite;
                var size = sprite != null && heroBaked ? sprite.rect.size * .5f : new Vector2(124, 124);
                AtCorner(_vitalsPortrait.rectTransform, 86, 992, size.x, size.y);
            }
            _vitalsPortrait.enabled = sprite != null;

            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            bool whole = local.IsDefender ? lata == null || lata.IsUpright : local.HoldingSlipper;
            var objectSprite = local.IsDefender ? CanIcon(lata) : SlipperIcon(local.PlayerSlot);
            if (_vitalsObjectSprite != objectSprite)
            {
                _vitalsObjectSprite = objectSprite; _vitalsObject.sprite = objectSprite;
                // A stroked flat picture comes already tilted; a stand-in portrait is tilted here.
                bool flat = objectSprite != null && HeldBaked.Contains(objectSprite);
                var size = flat ? objectSprite.rect.size * .5f : new Vector2(170, 170);
                AtCorner(_vitalsObject.rectTransform, 150, 960, size.x, size.y);
                _vitalsObject.rectTransform.localRotation = flat ? Quaternion.identity : Quaternion.Euler(0, 0, -20);
            }
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

        // ---- your score: a number that rolls up and pops, a place that pops when the lead changes, and the points that rose to it.
        private const float OwnRollSeconds = .45f, OwnPopSeconds = .35f, OwnGainSeconds = .9f;
        private bool _ownSeen;
        private int _ownFrom, _ownTarget, _ownShown = int.MinValue, _ownPlaceRank;
        private float _ownRollAt = -10, _ownPopAt = -10, _placePopAt = -10, _gainAt = -10;

        /// <summary>The local player's score and rank, once per refresh. Names the change; never decides one.</summary>
        private void NoteOwnScore(int score, int rank)
        {
            if (_ownScore == null) return;
            float now = Time.unscaledTime;
            if (!_ownSeen) { _ownSeen = true; _ownFrom = _ownTarget = score; }
            else if (score != _ownTarget)
            {
                _ownFrom = _ownShown == int.MinValue ? _ownTarget : _ownShown; _ownTarget = score;
                _ownRollAt = _ownPopAt = now;
            }
            if (rank != _ownPlaceRank)
            {
                if (_ownPlaceRank > 0 && rank < _ownPlaceRank) _placePopAt = now;
                _ownPlaceRank = rank;
                _ownPlace.text = rank >= 1 && rank <= 4 ? PlaceWords[rank - 1] : "";
            }
        }

        private static readonly string[] PlaceWords = { "1ST", "2ND", "3RD", "4TH" };

        /// <summary>The +points that rise out of your score when an award lands.</summary>
        private void StartOwnGain(int points)
        {
            if (_ownGain == null || points == 0) return;
            _ownGain.text = (points > 0 ? "+" : "") + points; _ownGain.enabled = true; _gainAt = Time.unscaledTime;
        }

        /// <summary>For tests: the +points rising out of your score, or empty.</summary>
        public string OwnGainText => _ownGain != null && _ownGain.enabled ? _ownGain.text : "";

        private void PaintOwnScore()
        {
            if (_ownScore == null || !_vitals.gameObject.activeInHierarchy) return;
            float now = Time.unscaledTime;
            bool still = LiveHud.Still;
            float roll = still ? 1 : Mathf.Clamp01((now - _ownRollAt) / OwnRollSeconds);
            int shown = _ownFrom + Mathf.RoundToInt((_ownTarget - _ownFrom) * LiveHud.EaseOut(roll));
            if (roll >= 1) shown = _ownTarget;
            if (shown != _ownShown) { _ownShown = shown; _ownScore.text = ScoreTextForChip(shown); }
            float pop = still ? 0 : Mathf.Clamp01((now - _ownPopAt) / OwnPopSeconds);
            _ownScore.rectTransform.localScale = Vector3.one * (1 + .15f * Mathf.Sin(pop * Mathf.PI) * (1 - pop * .3f) * (pop < 1 ? 1 : 0));
            float placePop = still ? 1 : Mathf.Clamp01((now - _placePopAt) / OwnPopSeconds);
            _ownPlace.rectTransform.localScale = Vector3.one * (1 + .15f * Mathf.Sin(placePop * Mathf.PI) * (placePop < 1 ? 1 : 0));
            if (_ownGain.enabled)
            {
                float u = (now - _gainAt) / OwnGainSeconds;
                if (u >= 1) _ownGain.enabled = false;
                else
                {
                    var at = _ownGain.rectTransform.anchoredPosition;
                    at.y = 1080 - 990 + (still ? 0 : 46 * LiveHud.EaseOut(u));
                    _ownGain.rectTransform.anchoredPosition = at;
                    var c = _ownGain.color; c.a = u < .6f ? 1 : Mathf.Clamp01((1 - u) / .4f); _ownGain.color = c;
                }
            }
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

        /// <summary>The picture of the can in play, which the taya guards.</summary>
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
            return entry != null ? FlatIcon(entry.Id) : null;
        }

        private static readonly System.Collections.Generic.HashSet<Sprite> HeldBaked = new System.Collections.Generic.HashSet<Sprite>();
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> FlatArt = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>The flat picture of a prop, or its baked portrait when it has no flat one (a pasip).
        /// Answered once per prop: a missing file is not looked for again every frame.</summary>
        private static Sprite FlatIcon(string id)
        {
            if (FlatArt.TryGetValue(id, out var cached) && cached != null) return cached;
            var baked = LiveHud.Picture("held/" + id);
            if (baked != null) { HeldBaked.Add(baked); FlatArt[id] = baked; return baked; }
            var art = OwnerPortraitArt.Get("UI/slippers-flat/" + id) ?? OwnerPortraitArt.Get("UI/portraits/" + id);
            if (art != null) FlatArt[id] = art;
            return art;
        }

        /// <summary>The stroked hero for the score corner, or the plain portrait when no stroked copy was made (a custom character).</summary>
        private static Sprite CornerHero(CharacterMotor actor, out bool baked)
        {
            var people = Roster.GetPeople(actor.Mode);
            Sprite art = null;
            if (actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count)
                art = LiveHud.Picture("hero-corner/" + people[actor.CharacterIndex].Id);
            baked = art != null;
            return baked ? art : PortraitFor(actor);
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
