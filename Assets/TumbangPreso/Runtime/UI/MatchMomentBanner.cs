using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // One earned central phrase. The side feed keeps parallel ordinary events;
    // this channel drops stale/lower-priority phrases instead of queueing them.
    public sealed class MatchMomentBanner : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private Text _title, _detail;
        private Image _accent;
        private MatchDirector _match;
        private MatchMoment _moment;
        private float _began, _duration;
        public bool Showing => _group != null && _group.alpha > .001f;
        public string Phrase => _title != null ? _title.text : "";

        public static MatchMomentBanner Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "EarnedMoment");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .76f); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(800, 148);
            var banner = rect.gameObject.AddComponent<MatchMomentBanner>(); banner._rect = rect;
            banner._group = rect.gameObject.AddComponent<CanvasGroup>(); banner._group.blocksRaycasts = false;
            banner._group.interactable = false; banner._group.alpha = 0;
            rect.gameObject.AddComponent<MomentPlate>().raycastTarget = false;
            banner._title = OwnerUiLayout.Text(rect, "MomentTitle", "", 54, OwnerUiLayout.TypeRole.Display);
            banner._title.alignment = TextAnchor.MiddleCenter; banner._title.raycastTarget = false;
            banner._title.color = OwnerUiTheme.Current.Pale; banner._title.supportRichText = false;
            banner._title.horizontalOverflow = HorizontalWrapMode.Overflow;
            banner._title.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Place(banner._title.rectTransform, 24, 0, 752, 98);
            banner._detail = OwnerUiLayout.Text(rect, "MomentPlayer", "", 28, OwnerUiLayout.TypeRole.Reading);
            banner._detail.alignment = TextAnchor.MiddleCenter; banner._detail.raycastTarget = false;
            banner._detail.supportRichText = false; OwnerUiLayout.Place(banner._detail.rectTransform, 24, 98, 752, 39);
            banner._accent = OwnerUiLayout.Rect(rect, "PlayerAccent").gameObject.AddComponent<Image>();
            banner._accent.raycastTarget = false; OwnerUiLayout.Place(banner._accent.rectTransform, 70, 141, 660, 4);
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
        { if (_group != null) _group.alpha = 0; _duration = 0; _moment = default; }
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
                default: return "TAKES THE LEAD";
            }
        }
        private void OnMoment(MatchMoment moment)
        {
            if (_group == null || !moment.IsValid || !isActiveAndEnabled) return;
            if (_duration > 0 && Time.unscaledTime - _began < .7f && moment.Priority < _moment.Priority) return;
            _moment = moment; _began = Time.unscaledTime; _duration = moment.Priority >= 2 ? 1.65f : 1.25f;
            _title.text = Title(moment.Kind);
            _detail.text = PlayerIdentity.Label(moment.Actor) + " · " + SeatLabel.Raw(moment.Actor)
                + (moment.Bonus > 0 ? "   +" + moment.Bonus + " CHAIN BONUS" : "");
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
            if (age >= _duration) { Hide(); return; }
            float enter = Mathf.Clamp01(age / .18f), leave = Mathf.Clamp01((_duration - age) / .2f);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            _group.alpha = Mathf.Min(reduced ? 1 : enter, leave);
            float arrive = 1 - Mathf.Pow(1 - enter, 3);
            _rect.anchoredPosition = reduced ? Vector2.zero : new Vector2(-28 * (1 - arrive), -8 * (1 - leave));
            _rect.localScale = Vector3.one * (reduced ? 1 : 1 + .07f * (1 - arrive));
            _rect.localRotation = Quaternion.Euler(0, 0, reduced ? 0 : -3 * (1 - arrive));
        }
        [RequireComponent(typeof(CanvasRenderer))]
        private sealed class MomentPlate : MaskableGraphic
        {
            protected override void OnPopulateMesh(VertexHelper helper)
            {
                helper.Clear(); var r = GetPixelAdjustedRect(); Color c = new Color(.137f, .114f, .129f, .92f);
                helper.AddVert(new Vector2(r.xMin+25,r.yMin),c,Vector2.zero);
                helper.AddVert(new Vector2(r.xMax-20,r.yMin+5),c,Vector2.zero);
                helper.AddVert(new Vector2(r.xMax,r.yMax-8),c,Vector2.zero);
                helper.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);
                helper.AddTriangle(0,1,2); helper.AddTriangle(0,2,3);
            }
        }
    }
}
