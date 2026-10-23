using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// THE BROADCAST CARDS, for spectators only (owner, 2026-10-22). A player's HUD stays minimal; a spectator has no
    /// kit of their own to watch, so each chip also carries:
    ///   * the seat's ultimate meter along its bottom edge, gold and breathing when the ultimate is ready;
    ///   * a TAYA tab on the taya's chip that drops in when the role moves to them.
    /// Read from the replicated kit charge and role every peer has.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private readonly RectTransform[] _ultTracks = new RectTransform[4], _ultFills = new RectTransform[4], _tayaTabs = new RectTransform[4];
        private readonly Image[] _ultFillImages = new Image[4];
        private readonly float[] _ultShown = new float[4];
        private readonly float[] _tayaTabAt = { -100, -100, -100, -100 };
        private readonly bool[] _tayaTabOn = new bool[4];

        private void BuildSpectatorCard(int i, RectTransform row)
        {
            var track = OwnerUiLayout.Rect(row, "UltTrack").gameObject.AddComponent<Image>();
            // A faint light groove, so an empty meter still reads as a meter on the dark chip.
            track.color = new Color(1, 1, 1, .32f); track.raycastTarget = false;
            OwnerUiLayout.Place(track.rectTransform, 6, ChipHeight - 8, ChipWidth - 12, 6);
            var fill = OwnerUiLayout.Rect(track.rectTransform, "UltFill").gameObject.AddComponent<Image>();
            fill.raycastTarget = false; fill.color = PlayerIdentity.Colour(i);
            var f = fill.rectTransform; f.anchorMin = new Vector2(0, 0); f.anchorMax = new Vector2(0, 1); f.pivot = new Vector2(0, .5f);
            f.anchoredPosition = Vector2.zero; f.sizeDelta = new Vector2(0, 0);
            _ultTracks[i] = track.rectTransform; _ultFills[i] = f; _ultFillImages[i] = fill;
            track.gameObject.SetActive(false);

            var tab = OwnerUiLayout.Rect(row, "TayaTab");
            // On the chip's top edge: the bar sits 22 units from the top of the screen, and a tab above it was off-screen.
            OwnerUiLayout.Place(tab, ChipWidth - 84, -14, 80, 26);
            var plate = tab.gameObject.AddComponent<HudCard>();
            plate.Glass(UiTheme.Taya, HudDraw.Lean, 2, .3f).raycastTarget = false; plate.BorderWidth = 0; plate.FollowContrast = false;
            var word = OwnerUiLayout.Text(tab, "TayaTabWord", "TAYA", 20, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(word.rectTransform); word.alignment = TextAnchor.MiddleCenter; word.color = HudDraw.Ivory;
            word.raycastTarget = false; word.fontStyle = FontStyle.Italic;
            // The display face's line is taller than the tab: let the word overflow rather than truncate to nothing.
            word.horizontalOverflow = HorizontalWrapMode.Overflow; word.verticalOverflow = VerticalWrapMode.Overflow;
            var ink = word.gameObject.AddComponent<Outline>(); ink.effectColor = UiTheme.InGameOutline; ink.effectDistance = new Vector2(1.5f, -1.5f);
            _tayaTabs[i] = tab; tab.gameObject.SetActive(false);
        }

        private readonly CharacterMotor[] _cardActors = new CharacterMotor[4];
        private readonly bool[] _cardDefenders = new bool[4];
        private bool _cardSpectating;

        /// <summary>The 10 Hz chip pass records the seat; the frame pass draws it (see `PaintSpectatorCards`).</summary>
        private void NoteSpectatorCard(int i, CharacterMotor actor, bool defender, bool spectating)
        { _cardActors[i] = actor; _cardDefenders[i] = defender; _cardSpectating = spectating; }

        private void PaintSpectatorCards()
        {
            for (int i = 0; i < 4; i++)
                if (_scoreRows[i] != null && _scoreRows[i].gameObject.activeInHierarchy)
                    PaintSpectatorCard(i, _cardActors[i], _cardDefenders[i], _cardSpectating);
        }

        private void PaintSpectatorCard(int i, CharacterMotor actor, bool defender, bool spectating)
        {
            if (_ultTracks[i] == null) return;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            var kit = actor != null && actor.AbilitySystem != null ? actor.AbilitySystem.Kit : null;
            bool meter = spectating && kit != null && kit.Ultimate != null;
            _ultTracks[i].gameObject.SetActive(meter);
            if (meter)
            {
                float ratio = kit.UltimateRatio;
                _ultShown[i] = still ? ratio : Mathf.MoveTowards(_ultShown[i], ratio, Time.unscaledDeltaTime * 1.5f);
                float width = _ultTracks[i].rect.width * _ultShown[i];
                _ultFills[i].sizeDelta = new Vector2(width, 0);
                bool ready = kit.IsUltimateReady;
                float breathe = ready && !still ? .75f + .25f * Mathf.Sin(Time.unscaledTime * 6f) : 1;
                var c = ready ? HudDraw.Honey : PlayerIdentity.Colour(actor.PlayerSlot);
                c.a = breathe; _ultFillImages[i].color = c;
            }
            bool tab = spectating && defender;
            if (tab != _tayaTabOn[i]) { _tayaTabOn[i] = tab; _tayaTabAt[i] = Time.unscaledTime; }
            float k = still ? 1 : Mathf.Clamp01((Time.unscaledTime - _tayaTabAt[i]) / .3f);
            bool drawn = tab || k < 1;
            _tayaTabs[i].gameObject.SetActive(drawn);
            if (drawn)
            {
                float e = tab ? 1 - (1 - k) * (1 - k) : 1 - k * k;
                _tayaTabs[i].localScale = Vector3.one * (tab ? Mathf.Lerp(1.5f, 1, e) : e);
                _tayaTabs[i].localRotation = Quaternion.Euler(0, 0, (1 - e) * (tab ? -12 : 12));
            }
        }
    }
}
