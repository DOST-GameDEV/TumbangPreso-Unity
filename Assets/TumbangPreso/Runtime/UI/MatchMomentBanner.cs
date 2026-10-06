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
        private HudCard _plate, _tag, _portraitTile;
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
        public float LaneHeight => _rect != null ? _rect.sizeDelta.y * Reading() : 0;
        /// <summary>The moment's canonical name ("TRIPLE CATCH"); the sticker draws it in title case.</summary>
        public string Phrase => _phrase;

        // ⚠️ UI REVAMP 2026-10-06: AN EARNED MOMENT IS ONE RED TOY TILE. The scorer's own portrait
        // on their seat colour, the phrase in the display face, and the bonus on a honey badge
        // pressed onto the corner; nothing hangs underneath it. A bigger catch or knockdown
        // (priority 2) turns the tile honey, so the step up is seen before it is read. It lands
        // with a short slap that Reduced UI Motion removes. The player's name is still composed
        // into `MomentPlayer` for logs and captions; the picture carries who it was.
        private const float Tilt = 0f, StickerHeight = 92, TagHeight = 0;
        public static MatchMomentBanner Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "EarnedMoment");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .79f); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(660, StickerHeight);
            var banner = rect.gameObject.AddComponent<MatchMomentBanner>(); banner._rect = rect;
            banner._group = rect.gameObject.AddComponent<CanvasGroup>(); banner._group.blocksRaycasts = false;
            banner._group.interactable = false; banner._group.alpha = 0;
            banner._plate = rect.gameObject.AddComponent<HudCard>();
            banner._plate.Toy(HudDraw.Alarm, HudDraw.AlarmSide, 9, 18, .45f).raycastTarget = false; banner._plate.FollowContrast = false;
            banner._portraitTile = OwnerUiLayout.Rect(rect, "MomentPortraitTile").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(banner._portraitTile.rectTransform, 12, 12, StickerHeight - 24, StickerHeight - 24);
            banner._portraitTile.Toy(Color.white, Color.clear, 0, 11, 0).raycastTarget = false; banner._portraitTile.Sheen = false;
            banner._portraitTile.FollowContrast = false;
            banner._portrait = OwnerPortraitArt.Create(rect, "MomentPortrait", "");
            OwnerUiLayout.Place(banner._portrait.rectTransform, 15, 15, StickerHeight - 30, StickerHeight - 30);
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
            banner._accent = OwnerUiLayout.Rect(rect, "PlayerAccent").gameObject.AddComponent<Image>();
            banner._accent.raycastTarget = false; banner._accent.enabled = false;
            banner._detail = OwnerUiLayout.Text(rect, "MomentPlayer", "", 28, OwnerUiLayout.TypeRole.Reading);
            banner._detail.enabled = false; banner._detail.raycastTarget = false; banner._detail.supportRichText = false;
            banner.Bind(); return banner;
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
            var mode = _title.horizontalOverflow; _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            float words = Mathf.Ceil(_title.preferredWidth) + 12; _title.horizontalOverflow = mode;
            float lead = StickerHeight + 4;
            float width = Mathf.Clamp(lead + words + 34, 320, 1100);
            _rect.sizeDelta = new Vector2(width, StickerHeight);
            OwnerUiLayout.Place(_title.rectTransform, lead, 0, width - lead - 22, StickerHeight);
            bool bonus = !string.IsNullOrEmpty(_bonus.text);
            _tag.gameObject.SetActive(bonus);
            if (bonus)
            {
                float badge = Mathf.Max(84, _bonus.preferredWidth + 28);
                OwnerUiLayout.Place(_tag.rectTransform, width - badge + 26, -26, badge, 46);
            }
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
        { if (_group != null) _group.alpha = 0; _duration = 0; _moment = default; _phrase = ""; _pending.Clear(); }
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
            bool big = moment.Priority >= 2;
            _plate.color = big ? HudDraw.Honey : HudDraw.Alarm; _plate.Side = big ? HudDraw.HoneySide : HudDraw.AlarmSide; _plate.SetVerticesDirty();
            _title.color = big ? HudDraw.Brown : HudDraw.Cream;
            _detail.text = PlayerIdentity.Label(moment.Actor) + " · " + SeatLabel.Raw(moment.Actor)
                + (moment.Bonus > 0 ? "   +" + moment.Bonus + " bonus" : "");
            _bonus.text = moment.Bonus > 0 ? "+" + moment.Bonus : "";
            _tag.color = big ? HudDraw.Cream : HudDraw.Honey; _tag.Side = big ? HudDraw.CreamSide : HudDraw.HoneySide; _tag.SetVerticesDirty();
            PaintPortrait(moment.Actor);
            Layout();
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
            // The slap: lands from 118% and a few degrees further round, settling on its tilt.
            _rect.anchoredPosition = Rest + (reduced ? Vector2.zero : new Vector2(0, -8 * (1 - leave)));
            _rect.localScale = Vector3.one * Reading() * (reduced ? 1 : 1 + .18f * (1 - arrive));
            _rect.localRotation = Quaternion.Euler(0, 0, Tilt + (reduced ? 0 : -6 * (1 - arrive)));
        }
    }
}
