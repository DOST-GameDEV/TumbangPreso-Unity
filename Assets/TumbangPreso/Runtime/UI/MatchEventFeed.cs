using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Visual;

namespace TumbangPreso.UI
{
    public sealed class MatchEventFeed : MonoBehaviour
    {
        public const int Capacity = 3;
        public const float Lifetime = 4f;
        private readonly Text[] _rows = new Text[Capacity];
        private readonly string[] _words = new string[Capacity];
        private readonly Color[] _colours = new Color[Capacity];
        private readonly float[] _expires = new float[Capacity];
        private Lata _lata;
        private MatchDirector _match;
        private int _round;
        private string _lastKey;
        private float _lastAt;
        public int Count { get; private set; }
        public string Entry(int index) => index >= 0 && index < Count ? _words[index] : "";

        public static MatchEventFeed Create(RectTransform parent)
        {
            var root = OwnerUiLayout.Rect(parent, "MatchEventFeed");
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 1);
            root.anchoredPosition = new Vector2(-30, -240); root.sizeDelta = new Vector2(450, 132);
            var feed = root.gameObject.AddComponent<MatchEventFeed>();
            for (int i = 0; i < Capacity; i++)
            {
                var row = OwnerUiLayout.Text(root, "Event" + i, "", 24, OwnerUiLayout.TypeRole.Reading);
                row.alignment = TextAnchor.MiddleRight; row.raycastTarget = false;
                row.supportRichText = false; row.horizontalOverflow = HorizontalWrapMode.Wrap;
                OwnerUiLayout.Place(row.rectTransform, 0, i * 42, 450, 40);
                var edge = row.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(.035f, .06f, .045f, .95f); edge.effectDistance = new Vector2(1.5f, -1.5f);
                feed._rows[i] = row; row.enabled = false;
            }
            return feed;
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
            foreach (var row in _rows) if (row != null) row.enabled = false;
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
            if (upright && Live) Add(GameServices.Match.DefenderSlot, "RESTORED LATA", "restore");
        }
        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            Bind();
            if (!Live) return;
            switch (kind)
            {
                case MatchFlair.Kind.LataDown: Add(actor, "DOWNED LATA", "can"); break;
                case MatchFlair.Kind.Tag: Add(actor, "CAUGHT " + PlayerIdentity.Label(subject), "tag" + subject); break;
                case MatchFlair.Kind.Block: Add(subject, actor >= 0 ? "BLOCKED " + PlayerIdentity.Label(actor) : "DEFLECTED SLIPPER", "block" + actor); break;
            }
        }
        private void Add(int actor, string words, string kind)
        {
            if (actor < 0 || actor >= Core.Balance.PlayerCount) return;
            float now = Time.unscaledTime; string key = actor + ":" + kind;
            if (_lastKey == key && now - _lastAt < .15f) return;
            _lastKey = key; _lastAt = now;
            for (int i = Capacity - 1; i > 0; i--)
            { _words[i] = _words[i-1]; _colours[i] = _colours[i-1]; _expires[i] = _expires[i-1]; }
            _words[0] = PlayerIdentity.Label(actor) + "  " + words;
            _colours[0] = PlayerIdentity.Colour(actor); _expires[0] = now + Lifetime;
            Count = Mathf.Min(Capacity, Count + 1); Paint(now);
        }
        private void Update() { Bind(); Paint(Time.unscaledTime); }
        private void Paint(float now)
        {
            while (Count > 0 && _expires[Count - 1] <= now) Count--;
            for (int i = 0; i < Capacity; i++)
            {
                if (_rows[i] == null) continue;
                bool show = i < Count && Live; _rows[i].enabled = show;
                if (!show) continue;
                _rows[i].text = _words[i]; var c = _colours[i];
                c.a = Mathf.Clamp01((_expires[i] - now) / .55f); _rows[i].color = c;
            }
        }
    }
}
