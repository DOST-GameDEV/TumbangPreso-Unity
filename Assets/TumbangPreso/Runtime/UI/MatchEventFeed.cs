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
    /// yellow, lilac) and vanish over sky.
    ///
    /// ⚠️ LIVE HUD 2026-11-01: THE ROWS ARE THE SCORE PLATES ON THE RIGHT EDGE (`boot_match.events`).
    /// The same woven grey plate, flush with the screen edge, with a nearly solid dark section holding
    /// the two heroes and the event glyph between them. Each hero's seat colour is a soft glow wider than
    /// the section and clipped to it, and what the event was worth sits on the green tag in its own part
    /// of the plate, with the same 24-unit gaps as the Tab board. Rows are 58 tall on a 70 step.
    /// </summary>
    public sealed class MatchEventFeed : MonoBehaviour
    {
        public const int Capacity = 3;
        public const float Lifetime = 3f;
        private const float RowHeight = 58, RowStep = 70, Width = 450;
        // The dark section holds the actor at 64, the glyph at 124 and the other player at 184; a row with no
        // other player closes the section 60 earlier. The points get their own part with 24 round them.
        private const float SectionTwo = 232, SectionOne = 172, GlyphBox = 30, HeroOne = 64, GlyphCentre = 124, HeroTwo = 184, TagHeight = 44, Room = 24;
        private const float PlateCapRight = 12;
        private static readonly Color GlowTint = new Color(1, 1, 1, 125 / 255f);

        private sealed class Row
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Plate;
            public RectTransform Clip;
            public Image[] Glow = new Image[2];
            public RectTransform[] Swatch = new RectTransform[2];
            public Image[] Portrait = new Image[2];
            public Text[] Tag = new Text[2];
            public RectTransform Tab;
            public Text TabLabel;
            public HudBadge Glyph;
            public Image Ability;
            public Text Points;
        }
        private struct Item { public string Words, Key; public int Actor, Other, Points; public HudBadge.Glyph Glyph; public Sprite Icon; public bool Ultimate; public Color Accent; public float Expires, Born, StackFromY, StackMovedAt, BumpAt; }

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
            root.anchoredPosition = new Vector2(RailX(Hud.OnTouch), RailTop(Hud.OnTouch)); root.sizeDelta = new Vector2(Width, RowStep * Capacity);
            var feed = root.gameObject.AddComponent<MatchEventFeed>(); feed._placedForTouch = Hud.OnTouch;
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
            row.Plate = OwnerUiLayout.Rect(row.Root, "EventPlate").gameObject.AddComponent<Image>();
            row.Plate.type = Image.Type.Sliced; row.Plate.raycastTarget = false;
            // Each hero's seat colour, wider than the section and clipped by it.
            row.Clip = OwnerUiLayout.Rect(row.Root, "SeatGlows");
            row.Clip.gameObject.AddComponent<RectMask2D>();
            for (int k = 0; k < 2; k++)
            {
                var glow = row.Glow[k] = OwnerUiLayout.Rect(row.Clip, k == 0 ? "ActorGlow" : "OtherGlow").gameObject.AddComponent<Image>();
                glow.sprite = LiveHud.Picture("seat-glow"); glow.raycastTarget = false; glow.color = GlowTint;
            }
            for (int k = 0; k < 2; k++)
            {
                var swatch = OwnerUiLayout.Rect(row.Root, k == 0 ? "ActorChip" : "OtherChip");
                row.Swatch[k] = swatch;
                row.Portrait[k] = OwnerPortraitArt.Create(swatch, "Portrait", "");
                row.Portrait[k].preserveAspect = true;
                // Only drawn when a seat has no portrait art (a custom character without one).
                var tag = OwnerUiLayout.Text(swatch, "SeatTag", "", 28, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Fill(tag.rectTransform); tag.alignment = TextAnchor.MiddleCenter;
                tag.color = HudDraw.Ivory; tag.verticalOverflow = VerticalWrapMode.Overflow; tag.raycastTarget = false;
                row.Tag[k] = tag;
            }
            row.Glyph = OwnerUiLayout.Rect(row.Root, "EventGlyph").gameObject.AddComponent<HudBadge>();
            row.Glyph.Detail = HudDraw.GlassDeep; row.Glyph.raycastTarget = false;
            // A cast row shows the ability's own icon where an event row shows its glyph.
            row.Ability = OwnerUiLayout.Rect(row.Root, "EventAbility").gameObject.AddComponent<Image>();
            row.Ability.raycastTarget = false; row.Ability.preserveAspect = true; row.Ability.enabled = false;
            // What the event was worth: the same green tag as the Tab board's, a penalty's in the alarm red.
            row.Tab = LiveHud.Tag(row.Root, "EventPointsTag", "+100", Hub.Knit.ThinGo, LiveHud.GoInk, 28);
            row.Tab.anchorMin = row.Tab.anchorMax = new Vector2(0, 1); row.Tab.pivot = new Vector2(0, 1);
            row.TabLabel = row.Tab.GetComponentInChildren<Text>();
            row.Points = row.TabLabel; row.Points.name = "EventPoints";
            return row;
        }

        private void OnEnable() { MatchFlair.Presented += OnMoment; Abilities.HeroAbilitySystem.CastPresented += OnCast; }
        private void OnDisable()
        {
            MatchFlair.Presented -= OnMoment; Abilities.HeroAbilitySystem.CastPresented -= OnCast;
            if (_scoredMatch != null) _scoredMatch.Scored -= OnScored;
            _scoredMatch = null;
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
            if (_scoredMatch != match)
            {
                if (_scoredMatch != null) _scoredMatch.Scored -= OnScored;
                _scoredMatch = match;
                if (_scoredMatch != null) _scoredMatch.Scored += OnScored;
            }
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
            if (this == null || _rows[0] == null) return;
            if (upright && Live) Add(GameServices.Match.DefenderSlot, -1, "RESTORED LATA", "restore", HudBadge.Glyph.Restore, UiTheme.Defense, 0);
        }
        private MatchDirector _scoredMatch;

        /// <summary>
        /// Casts, ultimates and penalties (owner, 2026-10-22), pictograms first: the caster's chip and the ability's own
        /// icon, gold for an ultimate. Every peer presents every accepted cast once (`CastPresented`).
        /// </summary>
        private void OnCast(CharacterMotor caster, Abilities.HeroAbility ability, bool ultimate)
        {
            if (this == null) { Abilities.HeroAbilitySystem.CastPresented -= OnCast; return; }
            if (_rows[0] == null || caster == null || ability == null) return;
            Bind();
            if (!Live) return;
            Add(caster.PlayerSlot, -1, (ultimate ? "ULT " : "") + ability.LiveName.ToUpperInvariant(),
                (ultimate ? "ult:" : "cast:") + ability.Id, HudBadge.Glyph.None, ultimate ? HudDraw.Honey : PlayerIdentity.Colour(caster.PlayerSlot),
                0, AbilityIcons.For(ability.Glyph), ultimate);
        }

        /// <summary>
        /// A penalty is a row with the seat, what it is for and what it has cost. A penalty that keeps ticking (the can
        /// left down) keeps ONE row counting up and punching on each tick, never a column of identical rows.
        /// </summary>
        private void OnScored(int slot, ScoreEvent e)
        {
            if (this == null || _rows[0] == null) return;
            if (!Live) return;
            HudBadge.Glyph glyph;
            switch (e)
            {
                case ScoreEvent.CanDownPenalty: glyph = HudBadge.Glyph.CanDown; break;
                case ScoreEvent.UnretrievedSlipperPenalty: glyph = HudBadge.Glyph.Slipper; break;
                case ScoreEvent.TayaCampPenalty: glyph = HudBadge.Glyph.Tag; break;
                default: return;
            }
            Add(slot, -1, "PENALTY " + e, "penalty:" + e, glyph, HudDraw.Alarm, MatchRules.PointsFor(e), null, false, accumulate: true);
        }

        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            // A feed destroyed without its OnDisable (an editor session with domain reload off keeps static listeners)
            // lets go of the event instead of throwing on every tag; one whose rows were never built draws nothing.
            if (this == null) { MatchFlair.Presented -= OnMoment; return; }
            if (_rows[0] == null) return;
            Bind();
            if (!Live) return;
            switch (kind)
            {
                case MatchFlair.Kind.LataDown: Add(actor, -1, "DOWNED LATA", "can", HudBadge.Glyph.Knock, UiTheme.Offense,
                    MatchRules.PointsFor(ScoreEvent.LataKnocked)); break;
                case MatchFlair.Kind.Tag: Add(actor, subject, "CAUGHT " + PlayerIdentity.Label(subject), "tag" + subject, HudBadge.Glyph.Tag, UiTheme.Defense,
                    MatchRules.PointsFor(ScoreEvent.Tag)); break;
                case MatchFlair.Kind.Block:
                    Add(subject, actor, actor >= 0 ? "BLOCKED " + PlayerIdentity.Label(actor) : "DEFLECTED SLIPPER", "block" + actor,
                        HudBadge.Glyph.Block, UiTheme.Offense, 0);
                    break;
            }
        }
        private void Add(int actor, int other, string words, string kind, HudBadge.Glyph glyph, Color accent, int points,
                         Sprite icon = null, bool ultimate = false, bool accumulate = false)
        {
            if (actor < 0 || actor >= Core.Balance.PlayerCount) return;
            float now = Time.unscaledTime; string key = actor + ":" + kind;
            if (accumulate)
                for (int i = 0; i < Count; i++)
                    if (_items[i].Key == key && _items[i].Expires > now)
                    {
                        _items[i].Points += points; _items[i].Expires = now + Lifetime; _items[i].BumpAt = now;
                        Layout(_rows[i], _items[i]); Paint(now);
                        return;
                    }
            if (_lastKey == key && now - _lastAt < .15f) return;
            _lastKey = key; _lastAt = now;
            for (int i = Capacity - 1; i > 0; i--)
            {
                _items[i] = _items[i - 1];
                _items[i].StackFromY = _rows[i - 1].Root.anchoredPosition.y;
                _items[i].StackMovedAt = now;
            }
            _items[0] = new Item
            {
                Words = PlayerIdentity.Label(actor) + "  " + words, Actor = actor,
                Other = other >= 0 && other < Core.Balance.PlayerCount ? other : -1,
                Glyph = glyph, Accent = accent, Points = points, Expires = now + Lifetime, Born = now,
                StackFromY = RowStep * .35f, StackMovedAt = now, Key = key, Icon = icon, Ultimate = ultimate, BumpAt = now,
            };
            Count = Mathf.Min(Capacity, Count + 1);
            for (int i = 0; i < Count; i++) Layout(_rows[i], _items[i]);
            Paint(now);
        }

        /// <summary>Sizes the row's plate to what it carries and lays the heroes, glyph and points on it, flush with the right edge.</summary>
        private static void Layout(Row row, Item item)
        {
            bool two = item.Other >= 0;
            string words = item.Points > 0 ? "+" + item.Points : item.Points < 0 ? item.Points.ToString() : "";
            row.Points.text = words;
            float section = two ? SectionTwo : SectionOne;
            float tag = 0;
            if (words.Length > 0) { LiveHud.SizeTag(row.Tab, row.TabLabel); tag = row.Tab.sizeDelta.x; }
            float cap = section + 18 + PlateCapRight;
            float width = Mathf.Max(cap, words.Length > 0 ? section + Room + tag + Room : section + 28);
            float x = Width - width;
            row.Plate.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + (two ? "feed-plate" : "feed-plate-short"), new Vector4(section + 18, 0, PlateCapRight, 0));
            OwnerUiLayout.Place(row.Plate.rectTransform, x, 0, width, RowHeight + 5);
            // The glows are wider than the dark section and clipped to it.
            OwnerUiLayout.Place(row.Clip, x + 4, 3, section, RowHeight - 6);
            PlaceGlow(row.Glow[0], HeroOne - 4, item.Actor);
            row.Glow[1].gameObject.SetActive(two);
            if (two) PlaceGlow(row.Glow[1], HeroTwo - 4, item.Other);
            Seat(row, 0, item.Actor, x + HeroOne);
            // The event's glyph in cream between them; a cast shows the ability's own icon.
            float glyphX = x + GlyphCentre;
            OwnerUiLayout.Place(row.Glyph.rectTransform, glyphX - GlyphBox * .5f, (RowHeight - GlyphBox) * .5f - 1, GlyphBox, GlyphBox);
            row.Glyph.Show(item.Icon != null ? HudBadge.Glyph.None : item.Glyph, item.Points < 0 ? HudDraw.Alarm : LiveHud.Cream,
                Color.clear, PlayerIdentity.Colour(item.Actor));
            row.Ability.enabled = item.Icon != null;
            if (item.Icon != null)
            {
                row.Ability.sprite = item.Icon; row.Ability.color = item.Ultimate ? HudDraw.Honey : LiveHud.Cream;
                OwnerUiLayout.Place(row.Ability.rectTransform, glyphX - GlyphBox * .5f - 2, (RowHeight - GlyphBox - 4) * .5f - 1, GlyphBox + 4, GlyphBox + 4);
            }
            row.Swatch[1].gameObject.SetActive(two);
            if (two) Seat(row, 1, item.Other, x + HeroTwo);
            bool tagged = words.Length > 0;
            row.Tab.gameObject.SetActive(tagged);
            if (tagged)
            {
                row.Tab.anchoredPosition = new Vector2(x + section + Room, -(RowHeight - TagHeight) * .5f - 1);
                row.Tab.sizeDelta = new Vector2(tag, TagHeight);
                row.Points.color = item.Points < 0 ? HudDraw.Alarm : LiveHud.GoInk;
            }
        }

        private static void PlaceGlow(Image glow, float centreInClip, int slot)
        {
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = glow.rectTransform.pivot = new Vector2(0, .5f);
            glow.rectTransform.sizeDelta = new Vector2(130, 130);
            glow.rectTransform.anchoredPosition = new Vector2(centreInClip - 65, 0);
            var colour = PlayerIdentity.Colour(slot); colour.a = GlowTint.a;
            glow.color = colour;
        }

        /// <summary>A hero stood on the plate: the same height for everyone, centred by its own ink on the section.</summary>
        private static void Seat(Row row, int k, int slot, float centreX)
        {
            Sprite portrait = null; bool baked = false;
            var actor = GameServices.Round != null ? GameServices.Round.PlayerAt(slot) : null;
            if (actor != null)
            {
                var people = Roster.GetPeople(actor.Mode);
                if (actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count)
                {
                    portrait = LiveHud.Picture("hero-plate/" + people[actor.CharacterIndex].Id);
                    baked = portrait != null;
                    if (portrait == null) portrait = OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id);
                }
            }
            var size = portrait != null && baked ? portrait.rect.size * .5f : new Vector2(RowHeight - 10, RowHeight - 10);
            OwnerUiLayout.Place(row.Swatch[k], centreX - size.x * .5f, 5, size.x, size.y);
            OwnerUiLayout.Fill(row.Portrait[k].rectTransform);
            row.Portrait[k].sprite = portrait; row.Portrait[k].enabled = portrait != null;
            row.Tag[k].text = PlayerIdentity.Label(slot); row.Tag[k].enabled = portrait == null;
        }

        /// <summary>
        /// ⚠️ 2026-10-20 OVERHAUL: THE FEED TAKES OVERWATCH'S TOP-RIGHT CORNER on keyboard and pad,
        /// 30 units under the top edge, beside the slimmer team bar rather than under the old one.
        /// Its rows are sized to what they carry and right-aligned, so even a two-portrait row
        /// (about 200 units) ends well clear of the bar's right end at every supported width. On a
        /// phone it keeps its old height: the touch chips (EMOTE and the rest) own the top right.
        /// </summary>
        private static float TopFor(bool touch) => PeerConnectionNotice.TopFor(touch);
        /// <summary>The plates sit flush with the screen's right edge, 130 units down (`boot_match.events`); a phone keeps its old margin.</summary>
        private static float RailX(bool touch) => touch ? -30 : 0;
        private static float RailTop(bool touch) => touch ? TopFor(true) : -130;
        private bool _placedForTouch;
        private bool _connectionSpace;
        public void ReserveConnectionSpace(bool reserved)
        {
            if (_connectionSpace == reserved) return;
            _connectionSpace = reserved; PlaceRail(Hud.OnTouch);
        }
        private void PlaceRail(bool touch)
        {
            _placedForTouch = touch;
            var root = (RectTransform)transform;
            root.anchoredPosition = new Vector2(RailX(touch),
                // Entry badges/score flyouts extend above the row's layout rect.
                _connectionSpace ? TopFor(touch) - PeerConnectionNotice.CardHeight - 40 : RailTop(touch));
            GetComponentInParent<HudReadingLayout>()?.RebasePlacement(root);
        }

        private void Update()
        {
            bool touch = Hud.OnTouch;
            if (touch != _placedForTouch)
            {
                PlaceRail(touch);
            }
            Bind(); Paint(Time.unscaledTime);
        }
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
                float stack = still ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - item.StackMovedAt) / .2f));
                row.Root.anchoredPosition = new Vector2((1 - fadeIn) * 36,
                    Mathf.Lerp(item.StackFromY, -i * RowStep, stack));
                // A landing row, or a penalty ticking again, punches once; an ultimate punches harder.
                float bump = still ? 0 : Mathf.Clamp01(1 - (now - item.BumpAt) / .22f);
                row.Root.localScale = Vector3.one * (1 + (item.Ultimate ? .16f : .09f) * Mathf.Sin(bump * Mathf.PI));
            }
        }
    }
}
