using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // One earned central phrase. The side feed keeps parallel ordinary events;
    // unrelated qualifications queue; a higher catch supersedes its lower recognition.
    public sealed class MatchMomentBanner : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private Text _title, _detail, _bonus;
        private Image _accent, _portrait;
        private HudCard _plate, _tag, _portraitTile, _wingLeft, _wingRight;
        private HudBurst _burst;
        private HudConfetti _confetti;
        private HudBadge _crown;
        private int _tier = 1;
        /// <summary>1 nice, 2 big, 3 legendary: how grand the current moment is drawn.</summary>
        public int Tier => _tier;
        private string _phrase = "";
        private MatchDirector _match;
        private MatchMoment _moment;
        private float _began, _duration;
        private readonly Queue<MatchMoment> _pending = new Queue<MatchMoment>();
        public const float Lifetime = 2.5f;
        public bool Showing => _group != null && _group.alpha > .001f;
        /// <summary>Where the sticker settles, in its anchor's units; the HUD's message lanes set it.</summary>
        public Vector2 Rest;
        /// <summary>The settled height in canvas units, including the accessibility size.</summary>
        public float LaneHeight => _rect != null ? (_rect.sizeDelta.y + (_tier == 3 ? 16 : 0)) * Reading() : 0;
        /// <summary>The moment's canonical name ("TRIPLE CATCH"); the sticker draws it in title case.</summary>
        public string Phrase => _phrase;
        /// <summary>True while the bonus badge rides above the tile.</summary>
        public bool HasBadge => _tag != null && _tag.gameObject.activeSelf;

        // ⚠️ UI REVAMP 2026-10-06: EARNED MOMENTS ESCALATE IN THREE TIERS, the way a MOBA escalates
        // from a kill to a legendary streak. Every tier is the same toy tile with the scorer's own
        // portrait, the phrase in the display face and the bonus on a honey badge; what grows is
        // the ceremony around it.
        //   1 NICE (single catch, three on target, takes the lead): a compact red tile, a quick slap.
        //   2 BIG (first knockdown, double catch, five on target, late knockdown): a larger red tile,
        //     ribbon wings behind it, a honey sunburst and a harder slap that settles with a shake.
        //   3 LEGENDARY (triple catch, multi catch, multi knockdown): the gold tile with a crown, a
        //     turning cream and honey sunburst and toy-cube confetti in the scorer's seat colour.
        // Reduced UI Motion keeps each tier's look and removes the slap, shake, turning and confetti.
        // The sunburst is the banner's own graphic so it always draws behind the tile.
        private const float Tilt = 0f, StickerHeight = 92, TagHeight = 0;
        private static readonly float[] TierHeight = { 0, 84, 92, 100 };
        private static readonly int[] TierTitle = { 0, 54, 60, 66 };

        internal static int TierOf(MatchMomentKind kind)
        {
            switch (kind)
            {
                case MatchMomentKind.TripleCatch: case MatchMomentKind.MultiCatch: case MatchMomentKind.MultiKnockdown: return 3;
                case MatchMomentKind.FirstKnockdown: case MatchMomentKind.DoubleCatch:
                case MatchMomentKind.AccurateFive: case MatchMomentKind.LateKnockdown: return 2;
                default: return 1;
            }
        }

        public static MatchMomentBanner Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "EarnedMoment");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .79f); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(660, StickerHeight);
            var banner = rect.gameObject.AddComponent<MatchMomentBanner>(); banner._rect = rect;
            banner._group = rect.gameObject.AddComponent<CanvasGroup>(); banner._group.blocksRaycasts = false;
            banner._group.interactable = false; banner._group.alpha = 0;
            banner._burst = rect.gameObject.AddComponent<HudBurst>(); banner._burst.raycastTarget = false; banner._burst.Reach = 0;
            banner._wingLeft = Wing(rect, "MomentWingLeft", .55f); banner._wingRight = Wing(rect, "MomentWingRight", -.55f);
            banner._plate = OwnerUiLayout.Rect(rect, "MomentTile").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(banner._plate.rectTransform);
            banner._plate.Toy(HudDraw.Alarm, HudDraw.AlarmSide, 9, 18, .45f).raycastTarget = false; banner._plate.FollowContrast = false;
            banner._portraitTile = OwnerUiLayout.Rect(rect, "MomentPortraitTile").gameObject.AddComponent<HudCard>();
            banner._portraitTile.Toy(Color.white, Color.clear, 0, 11, 0).raycastTarget = false; banner._portraitTile.Sheen = false;
            banner._portraitTile.FollowContrast = false;
            banner._portrait = OwnerPortraitArt.Create(rect, "MomentPortrait", "");
            banner._title = OwnerUiLayout.Text(rect, "MomentTitle", "", 58, OwnerUiLayout.TypeRole.Display);
            banner._title.alignment = TextAnchor.MiddleLeft; banner._title.raycastTarget = false;
            banner._title.color = HudDraw.Cream; banner._title.supportRichText = false;
            banner._title.horizontalOverflow = HorizontalWrapMode.Overflow;
            banner._title.verticalOverflow = VerticalWrapMode.Overflow;
            banner._tag = OwnerUiLayout.Rect(rect, "MomentBonus").gameObject.AddComponent<HudCard>();
            banner._tag.Toy(HudDraw.Honey, HudDraw.HoneySide, 5, 10, .4f).raycastTarget = false; banner._tag.Sheen = false;
            banner._tag.FollowContrast = false;
            banner._bonus = OwnerUiLayout.Text(banner._tag.transform, "MomentBonusPoints", "", 36, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(banner._bonus.rectTransform); banner._bonus.alignment = TextAnchor.MiddleCenter; banner._bonus.color = HudDraw.Brown;
            banner._bonus.horizontalOverflow = HorizontalWrapMode.Overflow; banner._bonus.verticalOverflow = VerticalWrapMode.Overflow;
            banner._crown = OwnerUiLayout.Rect(rect, "MomentCrown").gameObject.AddComponent<HudBadge>();
            banner._crown.raycastTarget = false; banner._crown.RimWidth = 3; banner._crown.Rim = HudDraw.Brown;
            banner._confetti = OwnerUiLayout.Rect(rect, "MomentConfetti").gameObject.AddComponent<HudConfetti>();
            banner._confetti.raycastTarget = false; OwnerUiLayout.Fill(banner._confetti.rectTransform);
            banner._accent = OwnerUiLayout.Rect(rect, "PlayerAccent").gameObject.AddComponent<Image>();
            banner._accent.raycastTarget = false; banner._accent.enabled = false;
            banner._detail = OwnerUiLayout.Text(rect, "MomentPlayer", "", 28, OwnerUiLayout.TypeRole.Reading);
            banner._detail.enabled = false; banner._detail.raycastTarget = false; banner._detail.supportRichText = false;
            banner.Bind(); return banner;
        }

        private static HudCard Wing(RectTransform parent, string name, float slant)
        {
            var wing = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<HudCard>();
            wing.Toy(HudDraw.AlarmDeep, HudDraw.AlarmSide, 7, 10, .35f).raycastTarget = false;
            wing.Slant = slant; wing.Sheen = false; wing.FollowContrast = false; wing.gameObject.SetActive(false);
            return wing;
        }

        /// <summary>"FIRST KNOCKDOWN" drawn as "First Knockdown!": the event voice, not a shout.</summary>
        private static string Spoken(string phrase)
        {
            var words = phrase.ToLowerInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words) + "!";
        }

        private void Layout()
        {
            float height = TierHeight[_tier];
            _title.fontSize = TierTitle[_tier];
            var mode = _title.horizontalOverflow; _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            float words = Mathf.Ceil(_title.preferredWidth) + 12; _title.horizontalOverflow = mode;
            float pad = 12 + (_tier - 1) * 2, tile = height - pad * 2, lead = pad + tile + 16;
            float width = Mathf.Clamp(lead + words + 34, 320, 1200);
            _rect.sizeDelta = new Vector2(width, height);
            OwnerUiLayout.Place(_portraitTile.rectTransform, pad, pad, tile, tile);
            OwnerUiLayout.Place(_portrait.rectTransform, pad + 3, pad + 3, tile - 6, tile - 6);
            OwnerUiLayout.Place(_title.rectTransform, lead, 0, width - lead - 22, height);
            bool bonus = !string.IsNullOrEmpty(_bonus.text);
            _tag.gameObject.SetActive(bonus);
            if (bonus)
            {
                float badge = Mathf.Max(84, _bonus.preferredWidth + 28);
                OwnerUiLayout.Place(_tag.rectTransform, width - badge + 26, -26, badge, 46);
            }
            bool wings = _tier >= 2;
            _wingLeft.gameObject.SetActive(wings); _wingRight.gameObject.SetActive(wings);
            if (wings)
            {
                float wingWidth = 96 + (_tier - 2) * 30, wingHeight = height * .62f;
                OwnerUiLayout.Place(_wingLeft.rectTransform, -wingWidth + 34, (height - wingHeight) * .5f + 10, wingWidth, wingHeight);
                OwnerUiLayout.Place(_wingRight.rectTransform, width - 34, (height - wingHeight) * .5f + 10, wingWidth, wingHeight);
                var face = _tier == 3 ? HudDraw.HoneySide : HudDraw.AlarmDeep;
                Color side = _tier == 3 ? (Color)new Color32(150, 100, 20, 255) : HudDraw.AlarmSide;
                foreach (var wing in new[] { _wingLeft, _wingRight }) { wing.color = face; wing.Side = side; wing.SetVerticesDirty(); }
            }
            _crown.gameObject.SetActive(_tier == 3);
            if (_tier == 3)
            {
                OwnerUiLayout.Place(_crown.rectTransform, (width - 64) * .5f, -40, 64, 48);
                _crown.Show(HudBadge.Glyph.Crown, HudDraw.Honey, Color.clear);
            }
            // The sunburst covers the tile and its wings; the confetti bursts from the tile's centre.
            _burst.Rays = _tier == 3 ? 18 : 14;
            _burst.color = _tier == 3 ? new Color(1, .96f, .86f, .8f) : new Color(1, .77f, .25f, .6f);
            _burst.Second = _tier == 3 ? new Color(1, .77f, .25f, .8f) : new Color(1, .96f, .86f, .35f);
            OwnerUiLayout.Place(_confetti.rectTransform, 0, 0, width, height);
        }

        private void PaintPortrait(int actor)
        {
            Sprite art = null;
            var who = GameServices.Round != null ? GameServices.Round.PlayerAt(actor) : null;
            if (who != null)
            {
                var people = Roster.GetPeople(who.Mode);
                if (who.CharacterIndex >= 0 && who.CharacterIndex < people.Count)
                    art = OwnerPortraitArt.Get("UI/portraits/" + people[who.CharacterIndex].Id);
            }
            _portrait.sprite = art; _portrait.enabled = art != null;
            _portraitTile.color = PlayerIdentity.Colour(actor); _portraitTile.SetVerticesDirty();
        }
        private void Bind()
        {
            if (_match == GameServices.Match) return;
            if (_match != null) _match.MomentPresented -= OnMoment;
            _match = GameServices.Match;
            if (_match != null) _match.MomentPresented += OnMoment;
            Hide();
        }
        private void OnEnable() => Bind();
        private void OnDisable()
        { if (_match != null) _match.MomentPresented -= OnMoment; _match = null; Hide(); }
        private void Hide()
        { if (_group != null) _group.alpha = 0; _duration = 0; _moment = default; _phrase = ""; _pending.Clear(); _confetti?.Stop(); _burst?.Set(0, 0); }
        private static string Title(MatchMomentKind kind)
        {
            switch (kind)
            {
                case MatchMomentKind.FirstKnockdown: return "FIRST KNOCKDOWN";
                case MatchMomentKind.AccurateThree: return "THREE ON TARGET";
                case MatchMomentKind.AccurateFive: return "FIVE ON TARGET";
                case MatchMomentKind.DoubleCatch: return "DOUBLE CATCH";
                case MatchMomentKind.TripleCatch: return "TRIPLE CATCH";
                case MatchMomentKind.LateKnockdown: return "LATE KNOCKDOWN";
                case MatchMomentKind.MultiKnockdown: return "MULTI KNOCKDOWN";
                case MatchMomentKind.SingleCatch: return "SINGLE CATCH";
                case MatchMomentKind.MultiCatch: return "MULTI CATCH";
                default: return "TAKES THE LEAD";
            }
        }
        private void OnMoment(MatchMoment moment)
        {
            if (_group == null || !moment.IsValid || !isActiveAndEnabled) return;
            if (_duration > 0 && (_moment.MatchId != moment.MatchId || _moment.Round != moment.Round)) Hide();
            if (_duration > 0)
            {
                if (SupersedesCatch(moment, _moment))
                {
                    ReplaceQueuedCatch(moment, false);
                    Begin(moment);
                    return;
                }
                if (!ReplaceQueuedCatch(moment, true)) _pending.Enqueue(moment);
                return;
            }
            Begin(moment);
        }
        private static int CatchSize(MatchMoment moment)
        {
            switch (moment.Kind)
            {
                case MatchMomentKind.SingleCatch: return 1;
                case MatchMomentKind.DoubleCatch: return 2;
                case MatchMomentKind.TripleCatch: return 3;
                case MatchMomentKind.MultiCatch: return Mathf.Max(4, moment.Count);
                default: return 0;
            }
        }
        private static bool SupersedesCatch(MatchMoment newer, MatchMoment older)
            => newer.MatchId == older.MatchId && newer.Round == older.Round
                && newer.Actor == older.Actor && newer.Sequence > older.Sequence
                && CatchSize(older) > 0 && CatchSize(newer) > CatchSize(older);
        private bool ReplaceQueuedCatch(MatchMoment newer, bool keepQueued)
        {
            bool replaced = false;
            int count = _pending.Count;
            for (int i = 0; i < count; i++)
            {
                var queued = _pending.Dequeue();
                if (!SupersedesCatch(newer, queued)) _pending.Enqueue(queued);
                else
                {
                    if (keepQueued && !replaced) _pending.Enqueue(newer);
                    replaced = true;
                }
            }
            return replaced;
        }
        private void Begin(MatchMoment moment)
        {
            _moment = moment; _began = Time.unscaledTime; _duration = Lifetime;
            _phrase = Title(moment.Kind);
            _title.text = Spoken(_phrase);
            _tier = TierOf(moment.Kind);
            bool big = _tier == 3;
            _plate.color = big ? HudDraw.Honey : HudDraw.Alarm; _plate.Side = big ? HudDraw.HoneySide : HudDraw.AlarmSide; _plate.SetVerticesDirty();
            _title.color = big ? HudDraw.Brown : HudDraw.Cream;
            _detail.text = PlayerIdentity.Label(moment.Actor) + " · " + SeatLabel.Raw(moment.Actor)
                + (moment.Bonus > 0 ? "   +" + moment.Bonus + " bonus" : "");
            _bonus.text = moment.Bonus > 0 ? "+" + moment.Bonus : "";
            _tag.color = big ? HudDraw.Cream : HudDraw.Honey; _tag.Side = big ? HudDraw.CreamSide : HudDraw.HoneySide; _tag.SetVerticesDirty();
            PaintPortrait(moment.Actor);
            Layout();
            _burst.Set(0, 0);
            if (_tier == 3 && !Settings.SettingsStore.Current.ReducedUiMotion)
                _confetti.Burst(PlayerIdentity.Colour(moment.Actor), HudDraw.Honey, HudDraw.Cream);
            else _confetti.Stop();
            Paint();
        }
        private void Update()
        {
            Bind();
            if (_duration <= 0) return;
            if (_match == null || !_match.MatchInProgress || _match.IsWarmupBuffer
                || _moment.MatchId != _match.PresentationMatchId || _moment.Round != _match.RoundNumber)
            { Hide(); return; }
            Paint();
        }
        private static float Reading()
        {
            var settings = Settings.SettingsStore.Current;
            return Mathf.Max(Settings.GameSettings.ValidHudScale(settings.HudScale), settings.LargerText ? 1.2f : 1f);
        }
        private void Paint()
        {
            float age = Time.unscaledTime - _began;
            if (age >= _duration)
            {
                if (_pending.Count > 0) Begin(_pending.Dequeue());
                else Hide();
                return;
            }
            float enter = Mathf.Clamp01(age / .18f), leave = Mathf.Clamp01((_duration - age) / .2f);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            _group.alpha = Mathf.Min(reduced ? 1 : enter, leave);
            float arrive = 1 - Mathf.Pow(1 - enter, 3);
            // The slap grows with the tier, and the bigger tiers settle with a short shake.
            float slap = _tier == 3 ? .34f : _tier == 2 ? .26f : .18f;
            float shake = reduced || _tier < 2 ? 0 : Mathf.Sin(age * 70f) * Mathf.Clamp01(1 - (age - .18f) / .3f) * (age > .18f ? 1 : 0) * (_tier == 3 ? 7 : 4);
            _rect.anchoredPosition = Rest + (reduced ? Vector2.zero : new Vector2(shake, -8 * (1 - leave)));
            _rect.localScale = Vector3.one * Reading() * (reduced ? 1 : 1 + slap * (1 - arrive));
            _rect.localRotation = Quaternion.Euler(0, 0, Tilt + (reduced ? 0 : -6 * (1 - arrive)));
            if (_tier >= 2)
            {
                float reach = (reduced ? 1 : arrive) * (_tier == 3 ? 1.55f : 1.15f);
                float turn = reduced || _tier < 3 ? 0 : age * 24f;
                _burst.Set(turn, reach);
            }
            else _burst.Set(0, 0);
        }
    }
}
