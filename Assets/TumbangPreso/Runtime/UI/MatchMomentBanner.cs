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
        private RectTransform _rect, _tagRect;
        private CanvasGroup _group;
        private Text _title, _detail, _bonus;
        private Image _accent, _portrait, _band;
        private PaintedLetters _letters;
        // Owner, October 31: "parang ang repetitive and bland nila". Live HUD, 2026-11-01 (`boot_match.moment`): a moment is
        // painted lettering on a soft band beside the scorer's face, in capitals, with the bonus on a green tag below. The
        // top moment (tier 3) is yellow with a crown on the face; the others are red. Each family keeps its own way in.
        private enum Family { Catch, Knockdown, Accuracy, Lead }
        private Family _family;
        private int _glyphs;
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
        public float LaneHeight => _rect != null ? 120 * Reading() : 0;
        /// <summary>The moment's canonical name ("TRIPLE CATCH"); the sticker draws it in title case.</summary>
        public string Phrase => _phrase;
        /// <summary>True while the bonus badge rides above the tile.</summary>
        public bool HasBadge => _tagRect != null && _tagRect.gameObject.activeSelf;

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
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(660, StickerHeight);
            var banner = rect.gameObject.AddComponent<MatchMomentBanner>(); banner._rect = rect;
            banner._group = rect.gameObject.AddComponent<CanvasGroup>(); banner._group.blocksRaycasts = false;
            banner._group.interactable = false; banner._group.alpha = 0;
            banner._band = LiveHud.Band(rect, "MomentBand", LiveHud.Coral, .55f);
            Centre(banner._band.rectTransform);
            banner._portrait = OwnerPortraitArt.Create(rect, "MomentPortrait", "");
            Centre(banner._portrait.rectTransform);
            banner._title = LiveHud.Lettering(rect, "MomentTitle", "", 62, LiveHud.WarnFill, LiveHud.WarnInk, LiveHud.WarnInk, 9, 5, 6);
            Centre(banner._title.rectTransform);
            banner._letters = banner._title.GetComponent<PaintedLetters>();
            banner._tagRect = LiveHud.Tag(rect, "MomentBonus", "", Hub.Knit.ThinGo, LiveHud.GoInk, 30);
            Centre(banner._tagRect);
            banner._bonus = banner._tagRect.GetComponentInChildren<Text>(); banner._bonus.name = "MomentBonusPoints";
            banner._crown = OwnerUiLayout.Rect(rect, "MomentCrown").gameObject.AddComponent<HudBadge>();
            banner._crown.raycastTarget = false; banner._crown.RimWidth = 3; banner._crown.Rim = LiveHud.YellowInk;
            Centre(banner._crown.rectTransform);
            banner._accent = OwnerUiLayout.Rect(rect, "PlayerAccent").gameObject.AddComponent<Image>();
            banner._accent.raycastTarget = false; banner._accent.enabled = false;
            banner._detail = OwnerUiLayout.Text(rect, "MomentPlayer", "", 28, OwnerUiLayout.TypeRole.Reading);
            banner._detail.enabled = false; banner._detail.raycastTarget = false; banner._detail.supportRichText = false;
            banner.Bind(); return banner;
        }

        private static void Centre(RectTransform r) => r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);

        /// <summary>"FIRST KNOCKDOWN" drawn as "First Knockdown!": the event voice, not a shout.</summary>
        private static string Spoken(string phrase)
        {
            var words = phrase.ToLowerInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words) + "!";
        }

        private static Family FamilyOf(MatchMomentKind kind)
        {
            switch (kind)
            {
                case MatchMomentKind.SingleCatch: case MatchMomentKind.DoubleCatch: case MatchMomentKind.TripleCatch: case MatchMomentKind.MultiCatch: return Family.Catch;
                case MatchMomentKind.FirstKnockdown: case MatchMomentKind.LateKnockdown: case MatchMomentKind.MultiKnockdown: return Family.Knockdown;
                case MatchMomentKind.AccurateThree: case MatchMomentKind.AccurateFive: return Family.Accuracy;
                default: return Family.Lead;
            }
        }
        // Chosen by the moment's own sequence number, so every peer draws the same words. `Phrase` keeps the canonical name.
        private static string Shout(MatchMoment moment, string phrase)
        {
            string[] others;
            switch (moment.Kind)
            {
                case MatchMomentKind.SingleCatch: others = new[] { "Huli Ka!", "Gotcha!" }; break;
                case MatchMomentKind.DoubleCatch: others = new[] { "Dalawa Agad!" }; break;
                case MatchMomentKind.TripleCatch: others = new[] { "Walang Takas!" }; break;
                case MatchMomentKind.FirstKnockdown: others = new[] { "Tumba!" }; break;
                case MatchMomentKind.LateKnockdown: others = new[] { "Tumba Pa!" }; break;
                case MatchMomentKind.AccurateThree: others = new[] { "Sapul!" }; break;
                case MatchMomentKind.AccurateFive: others = new[] { "Asintado!" }; break;
                default: return Spoken(phrase);
            }
            int pick = (int)(moment.Sequence % (others.Length + 1));
            return pick == 0 ? Spoken(phrase) : others[pick - 1];
        }
        private static int PipsOf(MatchMoment moment)
        {
            switch (moment.Kind)
            {
                case MatchMomentKind.AccurateThree: return 3;
                case MatchMomentKind.AccurateFive: return 5;
                case MatchMomentKind.DoubleCatch: return 2;
                case MatchMomentKind.TripleCatch: return 3;
                case MatchMomentKind.MultiCatch: return Mathf.Clamp(moment.Count, 4, 5);
                default: return 0;
            }
        }

        private const float FaceSize = 84;
        private float _titleWidth;

        private void Layout()
        {
            _title.fontSize = _tier == 3 ? 74 : 62;
            _glyphs = 0;
            foreach (char c in _title.text) if (!char.IsWhiteSpace(c)) _glyphs++;
            _titleWidth = Mathf.Ceil(_title.preferredWidth);
            _title.rectTransform.sizeDelta = new Vector2(_titleWidth + 24, _title.fontSize * 1.4f);
            float width = _titleWidth + FaceSize + 10, left = -width * .5f;
            _portrait.rectTransform.sizeDelta = new Vector2(FaceSize, FaceSize);
            _portrait.rectTransform.anchoredPosition = new Vector2(left + FaceSize * .5f, 0);
            _title.rectTransform.anchoredPosition = new Vector2(left + FaceSize + 10 + _titleWidth * .5f, 0);
            _band.rectTransform.sizeDelta = new Vector2(width + 300, _title.fontSize * .9f);
            _band.rectTransform.anchoredPosition = new Vector2(0, -_title.fontSize * .06f);
            bool bonus = !string.IsNullOrEmpty(_bonus.text);
            _tagRect.gameObject.SetActive(bonus);
            if (bonus) { LiveHud.SizeTag(_tagRect, _bonus); _tagRect.anchoredPosition = new Vector2(0, -_title.fontSize * 1.1f); }
            _rect.sizeDelta = new Vector2(width + 300, _title.fontSize * 1.4f);
            _crown.gameObject.SetActive(_tier == 3);
            if (_tier == 3)
            {
                _crown.rectTransform.sizeDelta = new Vector2(64, 50);
                _crown.rectTransform.anchoredPosition = new Vector2(left + FaceSize * .5f + 6, FaceSize * .5f + 8);
                _crown.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
                _crown.Show(HudBadge.Glyph.Crown, LiveHud.Yellow, Color.clear);
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
                case MatchMomentKind.MultiKnockdown: return "MULTI DOWN";
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
            _title.text = Shout(moment, _phrase).ToUpperInvariant();
            _tier = TierOf(moment.Kind);
            _family = FamilyOf(moment.Kind);
            bool top = _tier == 3;
            _title.color = top ? LiveHud.Yellow : LiveHud.WarnFill;
            _letters.Edge = _letters.Slab = top ? LiveHud.YellowInk : LiveHud.WarnInk;
            var band = top ? LiveHud.Yellow : LiveHud.Coral; band.a = .55f; _band.color = band;
            _detail.text = PlayerIdentity.Label(moment.Actor) + " · " + SeatLabel.Raw(moment.Actor)
                + (moment.Bonus > 0 ? "   +" + moment.Bonus + " bonus" : "");
            _bonus.text = moment.Bonus > 0 ? "+" + moment.Bonus : "";
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
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            float leave = Mathf.Clamp01(1 - (_duration - age) / .2f);
            _group.alpha = (1 - leave) * (reduced ? Mathf.Clamp01(age / .15f) : 1);
            // The words stamp in letter by letter, the band wipes out from the middle, and the face, the crown and
            // the bonus pop last. Out is a quick fade and rise.
            _letters.SetAge(reduced ? float.PositiveInfinity : age, _glyphs);
            float stamped = _letters.StampSeconds(_glyphs);
            _band.rectTransform.localScale = new Vector3(reduced ? 1 : LiveHud.EaseOut((age - .04f) / .28f), 1, 1);
            float faceU = Mathf.Clamp01((age - stamped * .5f) / .3f);
            _portrait.rectTransform.localScale = Vector3.one * (reduced ? 1 : LiveHud.Pop(faceU));
            _portrait.rectTransform.localRotation = Quaternion.Euler(0, 0, reduced ? 0 : Mathf.Sin(faceU * 22) * Mathf.Exp(-5 * faceU) * 8);
            if (_tier == 3) _crown.rectTransform.localScale = Vector3.one * (reduced ? 1 : LiveHud.Pop((age - stamped * .5f - .08f) / .3f));
            _tagRect.localScale = Vector3.one * (reduced ? 1 : LiveHud.Pop((age - stamped) / .3f));
            // Each family keeps its own way in: a knockdown skids in, good aim rises, the lead drops from above.
            float land = reduced ? 0 : 1 - LiveHud.EaseOut(age / .3f);
            Vector2 way = _family == Family.Knockdown ? new Vector2(-150 * land, 0)
                : _family == Family.Accuracy ? new Vector2(0, -56 * land)
                : _family == Family.Lead ? new Vector2(0, 70 * land) : Vector2.zero;
            _rect.anchoredPosition = Rest + way + (reduced ? Vector2.zero : new Vector2(0, 26 * leave * leave));
            _rect.localScale = Vector3.one * Reading();
        }
    }
}
