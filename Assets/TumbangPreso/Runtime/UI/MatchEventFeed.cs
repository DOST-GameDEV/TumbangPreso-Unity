using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.Visual;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The right-edge event feed (TODO VISUAL-1.4).
    ///
    /// ⚠️⚠️ PICTOGRAMS, NOT SENTENCES. The feed was three lines of words ("P2  DOWNED LATA",
    /// "P1  BLOCKED P3") in the reading face. The owner, 2026-09-23: "i really dont wanna have
    /// to communicate by using text". A row is now WHO, WHAT and, where there is one, WHOM:
    /// the actor's portrait on their seat colour, the event pictogram (`HudBadge` Knock,
    /// Restore, Tag, Block), and the other player's portrait. That is the kill feed shape every
    /// competitive shooter has taught the player already (Valorant's top right).
    ///
    /// ⚠️ <see cref="Entry"/> STILL RETURNS THE WORDS. They are the row's record, not its
    /// picture: tests assert attribution through it and a caption or a log can read it, but
    /// nothing draws it in play.
    ///
    /// ⚠️ A ROW ON ITS OWN DARK PLATE, NOT FLOATING TEXT. Seat colours are pale (cyan, salmon,
    /// yellow, lilac) and vanish over sky; the plate is `HudDraw.Plate`, the clock's plate, so
    /// the feed is one family with the match bar and turns black under High contrast by itself.
    /// </summary>
    public sealed class MatchEventFeed : MonoBehaviour
    {
        public const int Capacity = 3;
        public const float Lifetime = 4f;
        private const float RowHeight = 54, Chip = 44, GlyphSize = 42, Pad = 5, Gap = 7, RowStep = 60, Width = 450;

        private sealed class Row
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public HudCard Plate;
            public HudCard[] Swatch = new HudCard[2];
            public Image[] Portrait = new Image[2];
            public Text[] Tag = new Text[2];
            public HudBadge Glyph;
        }
        private struct Item { public string Words; public int Actor, Other; public HudBadge.Glyph Glyph; public Color Accent; public float Expires, Born; }

        private readonly Row[] _rows = new Row[Capacity];
        private readonly Item[] _items = new Item[Capacity];
        private Lata _lata;
        private MatchDirector _match;
        private int _round;
        private string _lastKey;
        private float _lastAt;
        public int Count { get; private set; }
        public string Entry(int index) => index >= 0 && index < Count ? _items[index].Words : "";
        /// <summary>The pictogram drawn for row <paramref name="index"/>, for tests.</summary>
        public HudBadge.Glyph GlyphAt(int index) => index >= 0 && index < Count ? _items[index].Glyph : HudBadge.Glyph.None;

        public static MatchEventFeed Create(RectTransform parent)
        {
            var root = OwnerUiLayout.Rect(parent, "MatchEventFeed");
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 1);
            root.anchoredPosition = new Vector2(-30, -240); root.sizeDelta = new Vector2(Width, RowStep * Capacity);
            var feed = root.gameObject.AddComponent<MatchEventFeed>();
            for (int i = 0; i < Capacity; i++) feed._rows[i] = BuildRow(root, i);
            return feed;
        }

        private static Row BuildRow(RectTransform root, int i)
        {
            var row = new Row();
            row.Root = OwnerUiLayout.Rect(root, "Event" + i);
            OwnerUiLayout.Place(row.Root, 0, i * RowStep, Width, RowHeight);
            row.Group = row.Root.gameObject.AddComponent<CanvasGroup>();
            row.Group.blocksRaycasts = false; row.Group.interactable = false; row.Group.alpha = 0;
            row.Plate = OwnerUiLayout.Rect(row.Root, "EventPlate").gameObject.AddComponent<HudCard>();
            row.Plate.color = HudDraw.Plate; row.Plate.Radius = 14; row.Plate.raycastTarget = false;
            for (int k = 0; k < 2; k++)
            {
                var swatch = OwnerUiLayout.Rect(row.Root, k == 0 ? "ActorChip" : "OtherChip").gameObject.AddComponent<HudCard>();
                swatch.Radius = 10; swatch.ShadowAlpha = 0; swatch.FollowContrast = false; swatch.raycastTarget = false;
                row.Swatch[k] = swatch;
                row.Portrait[k] = OwnerPortraitArt.Create(swatch.transform, "Portrait", "");
                OwnerUiLayout.Place(row.Portrait[k].rectTransform, 2, 2, Chip - 4, Chip - 4);
                // Only drawn when a seat has no portrait art (a custom character without one).
                var tag = OwnerUiLayout.Text(swatch.transform, "SeatTag", "", 28, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Fill(tag.rectTransform); tag.alignment = TextAnchor.MiddleCenter;
                tag.color = HudDraw.CardInk; tag.verticalOverflow = VerticalWrapMode.Overflow; tag.raycastTarget = false;
                row.Tag[k] = tag;
            }
            row.Glyph = OwnerUiLayout.Rect(row.Root, "EventGlyph").gameObject.AddComponent<HudBadge>();
            row.Glyph.Detail = HudDraw.Plate; row.Glyph.raycastTarget = false;
            return row;
        }

        private void OnEnable() => MatchFlair.Presented += OnMoment;
        private void OnDisable()
        {
            MatchFlair.Presented -= OnMoment;
            if (_lata != null) _lata.UprightChanged -= OnCanState;
            _lata = null; Clear();
        }
        private void Clear()
        {
            Count = 0; _lastKey = null;
            foreach (var row in _rows) if (row != null) row.Group.alpha = 0;
        }
        private void Bind()
        {
            var match = GameServices.Match;
            int number = match != null ? match.RoundNumber : 0;
            if (_match != match || _round != number) { Clear(); _match = match; _round = number; }
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            if (_lata == lata) return;
            if (_lata != null) _lata.UprightChanged -= OnCanState;
            _lata = lata;
            if (_lata != null) _lata.UprightChanged += OnCanState;
        }
        private bool Live => GameServices.Round != null && GameServices.Round.RoundActive &&
                             GameServices.Match != null && !GameServices.Match.IsWarmupBuffer;
        private void OnCanState(bool upright)
        {
            if (upright && Live) Add(GameServices.Match.DefenderSlot, -1, "RESTORED LATA", "restore", HudBadge.Glyph.Restore, UiTheme.Defense);
        }
        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            Bind();
            if (!Live) return;
            switch (kind)
            {
                case MatchFlair.Kind.LataDown: Add(actor, -1, "DOWNED LATA", "can", HudBadge.Glyph.Knock, UiTheme.Offense); break;
                case MatchFlair.Kind.Tag: Add(actor, subject, "CAUGHT " + PlayerIdentity.Label(subject), "tag" + subject, HudBadge.Glyph.Tag, UiTheme.Defense); break;
                case MatchFlair.Kind.Block:
                    Add(subject, actor, actor >= 0 ? "BLOCKED " + PlayerIdentity.Label(actor) : "DEFLECTED SLIPPER", "block" + actor,
                        HudBadge.Glyph.Block, UiTheme.Offense);
                    break;
            }
        }
        private void Add(int actor, int other, string words, string kind, HudBadge.Glyph glyph, Color accent)
        {
            if (actor < 0 || actor >= Core.Balance.PlayerCount) return;
            float now = Time.unscaledTime; string key = actor + ":" + kind;
            if (_lastKey == key && now - _lastAt < .15f) return;
            _lastKey = key; _lastAt = now;
            for (int i = Capacity - 1; i > 0; i--) _items[i] = _items[i - 1];
            _items[0] = new Item
            {
                Words = PlayerIdentity.Label(actor) + "  " + words, Actor = actor,
                Other = other >= 0 && other < Core.Balance.PlayerCount ? other : -1,
                Glyph = glyph, Accent = accent, Expires = now + Lifetime, Born = now,
            };
            Count = Mathf.Min(Capacity, Count + 1);
            for (int i = 0; i < Count; i++) Layout(_rows[i], _items[i]);
            Paint(now);
        }

        /// <summary>Right-aligns the row's chips and sizes its plate to what it carries.</summary>
        private static void Layout(Row row, Item item)
        {
            bool two = item.Other >= 0;
            float width = Pad * 2 + Chip + Gap + GlyphSize + (two ? Gap + Chip : 0);
            float x = Width - width;
            OwnerUiLayout.Place(row.Plate.rectTransform, x, 0, width, RowHeight);
            float y = (RowHeight - Chip) * .5f;
            Seat(row, 0, item.Actor, x + Pad, y);
            OwnerUiLayout.Place(row.Glyph.rectTransform, x + Pad + Chip + Gap, (RowHeight - GlyphSize) * .5f, GlyphSize, GlyphSize);
            row.Glyph.Show(item.Glyph, CourtPresentationPalette.Paper, Color.clear, item.Accent);
            row.Swatch[1].gameObject.SetActive(two);
            if (two) Seat(row, 1, item.Other, x + Pad + Chip + Gap + GlyphSize + Gap, y);
        }

        private static void Seat(Row row, int k, int slot, float x, float y)
        {
            OwnerUiLayout.Place(row.Swatch[k].rectTransform, x, y, Chip, Chip);
            row.Swatch[k].color = PlayerIdentity.Colour(slot);
            Sprite portrait = null;
            var actor = GameServices.Round != null ? GameServices.Round.PlayerAt(slot) : null;
            if (actor != null)
            {
                var people = Roster.GetPeople(actor.Mode);
                if (actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count)
                    portrait = OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id);
            }
            row.Portrait[k].sprite = portrait; row.Portrait[k].enabled = portrait != null;
            row.Tag[k].text = PlayerIdentity.Label(slot); row.Tag[k].enabled = portrait == null;
        }

        private void Update() { Bind(); Paint(Time.unscaledTime); }
        private void Paint(float now)
        {
            while (Count > 0 && _items[Count - 1].Expires <= now) Count--;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            for (int i = 0; i < Capacity; i++)
            {
                var row = _rows[i]; if (row == null) continue;
                bool show = i < Count && Live;
                if (!show) { row.Group.alpha = 0; continue; }
                var item = _items[i];
                float fadeIn = still ? 1 : Mathf.Clamp01((now - item.Born) / .14f);
                row.Group.alpha = Mathf.Min(fadeIn, Mathf.Clamp01((item.Expires - now) / .55f));
                // A new row slides in from the edge, so the eye catches it without a flash.
                row.Root.anchoredPosition = new Vector2((1 - fadeIn) * 36, row.Root.anchoredPosition.y);
            }
        }
    }
}
