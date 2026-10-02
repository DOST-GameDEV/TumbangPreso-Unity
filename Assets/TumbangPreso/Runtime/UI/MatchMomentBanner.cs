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
        private Text _title, _detail;
        private Image _accent;
        private CourtPopupGraphic _plate;
        private MatchDirector _match;
        private MatchMoment _moment;
        private float _began, _duration;
        private readonly Queue<MatchMoment> _pending = new Queue<MatchMoment>();
        public const float Lifetime = 2.5f;
        public bool Showing => _group != null && _group.alpha > .001f;
        public string Phrase => _title != null ? _title.text : "";

        public static MatchMomentBanner Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "EarnedMoment");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .79f); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(660, 116);
            var banner = rect.gameObject.AddComponent<MatchMomentBanner>(); banner._rect = rect;
            banner._group = rect.gameObject.AddComponent<CanvasGroup>(); banner._group.blocksRaycasts = false;
            banner._group.interactable = false; banner._group.alpha = 0;
            banner._plate=rect.gameObject.AddComponent<CourtPopupGraphic>();banner._plate.Brush=true;banner._plate.color=CourtPresentationPalette.DeepRed;banner._plate.raycastTarget=false;
            banner._title = OwnerUiLayout.Text(rect, "MomentTitle", "", 44, OwnerUiLayout.TypeRole.Display);
            banner._title.alignment = TextAnchor.MiddleCenter; banner._title.raycastTarget = false;
            banner._title.color = CourtPresentationPalette.Paper; banner._title.supportRichText = false;
            banner._title.horizontalOverflow = HorizontalWrapMode.Overflow;
            banner._title.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Place(banner._title.rectTransform, 24, 0, 612, 73);
            banner._detail = OwnerUiLayout.Text(rect, "MomentPlayer", "", 25, OwnerUiLayout.TypeRole.Display);
            banner._detail.alignment = TextAnchor.MiddleCenter; banner._detail.raycastTarget = false;
            banner._detail.supportRichText = false; OwnerUiLayout.Place(banner._detail.rectTransform, 24, 72, 612, 36);
            banner._accent = OwnerUiLayout.Rect(rect, "PlayerAccent").gameObject.AddComponent<Image>();
            banner._accent.raycastTarget = false; OwnerUiLayout.Place(banner._accent.rectTransform, 55, 109, 550, 3);
            banner.Bind(); return banner;
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
        { if (_group != null) _group.alpha = 0; _duration = 0; _moment = default; _pending.Clear(); }
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
            _title.text = Title(moment.Kind);
            _plate.color=moment.Priority>=2?CourtPresentationPalette.Red:CourtPresentationPalette.DeepRed;
            _detail.text = PlayerIdentity.Label(moment.Actor) + " · " + SeatLabel.Raw(moment.Actor)
                + (moment.Bonus > 0 ? "   +" + moment.Bonus + " BONUS" : "");
            _detail.color = _accent.color = PlayerIdentity.Colour(moment.Actor);
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
            _rect.anchoredPosition = reduced ? Vector2.zero : new Vector2(-28 * (1 - arrive), -8 * (1 - leave));
            _rect.localScale = Vector3.one * (reduced ? 1 : 1 + .07f * (1 - arrive));
            _rect.localRotation = Quaternion.Euler(0, 0, reduced ? 0 : -3 * (1 - arrive));
        }
    }
}
