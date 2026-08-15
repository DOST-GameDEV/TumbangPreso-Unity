using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Tells the player which of the four units they are actually driving, and what that unit
    /// can do right now. Converted from `scripts/ui/you_card.gd`.
    ///
    /// Q-5, from a "role unclear on rejoin" gap: nothing else on the HUD says which unit is
    /// YOURS — the scores row shows every seat, not your seat.
    ///
    /// ⚠️ IT POLLS RATHER THAN LISTENING, ON PURPOSE. The debug switcher reassigns who is
    /// driving at runtime, and the debug-code contract (Dev_Plan.md §0.3, rule 11) forbids
    /// gameplay code from referencing anything debug-only — even a signal. Polling a public
    /// value stays correct with zero coupling in either direction. **Do not "optimise" this
    /// into an event subscription.**
    ///
    /// ⚠️ "YOU" AND THE HOLD ROW WERE DELETED FROM THE ORIGINAL. 🧑 2026-08-01: *"too ugly,
    /// too much stuff happening in that box. confusing"*. Do not add a header back.
    /// </summary>
    public sealed class YouCard : MonoBehaviour
    {
        public const float RefreshInterval = 0.15f;

        /// <summary>How long the "just became ready again" flash lasts. Deliberately NOT
        /// shared with the 3D hit flash — that one is about a hit landing on a mesh, this is
        /// a 2D meter filling back up, and there is no reason the two move in lockstep.</summary>
        public const float ReadyFlashDuration = 0.2f;

        private static readonly Vector2 BottomLeft = new Vector2(0.0f, 0.0f);

        private CharacterMotor _character;
        private Carrier _carrier;
        private float _refreshLeft;

        private Image _card;
        private Text _class;
        private Text _detail;
        private Text _chargeKey;
        private Image _chargeFill;
        private Text _resetKey;
        private Image _resetFill;

        private bool _isAttackerPerson;
        private bool _isDefenderPerson;

        public void Bind(CharacterMotor local) => _character = local;

        private void Awake() => Build();

        private void Update()
        {
            _refreshLeft -= Time.deltaTime;
            if (_refreshLeft <= 0.0f)
            {
                _refreshLeft = RefreshInterval;
                Refresh();
            }

            UpdateMeters();
        }

        public void Refresh()
        {
            if (_character == null)
            {
                _card.transform.parent.gameObject.SetActive(false);
                return;
            }

            _card.transform.parent.gameObject.SetActive(true);

            bool isDefense = _character.IsDefender;

            _class.text = isDefense ? "TAYA (DEFENDER)" : "ATTACKER";
            _detail.text = _character.DisplayName();

            // §4.2: the accent tracks ROLE, never identity.
            Color accent = isDefense ? UiTheme.Defense : UiTheme.Offense;
            _card.color = UiTheme.WoodDeep;
            _class.color = UiTheme.Cream;
            _detail.color = accent;

            _isAttackerPerson = _character.IsPerson && !isDefense;
            _isDefenderPerson = _character.IsPerson && isDefense;

            if (_isAttackerPerson) _chargeKey.text = "[LMB]";
            if (_isDefenderPerson) _resetKey.text = "RIGHTING LATA [E]";

            _carrier = _character.GetComponent<Carrier>();

            // ⚠️ ONLY THE ROW THAT APPLIES TO THIS ROLE IS SHOWN. An attacker has no righting
            // channel and a taya has no throw charge; drawing an empty meter for the verb you
            // do not have is the "too much stuff happening" the box was cut down for.
            _chargeKey.transform.parent.gameObject.SetActive(_isAttackerPerson);
            _resetKey.transform.parent.gameObject.SetActive(_isDefenderPerson);
        }

        private void UpdateMeters()
        {
            if (_character == null) return;

            if (_isAttackerPerson && _chargeFill != null)
            {
                float ratio = _carrier != null ? _carrier.ChargeRatio : 0.0f;
                SetFill(_chargeFill, ratio);
            }

            if (_isDefenderPerson && _resetFill != null)
            {
                // The righting channel lives on the Carrier alongside the throw charge —
                // both are "hold this key and a bar fills", and they are mutually exclusive
                // because no unit is attacker and taya at once.
                float ratio = _carrier != null ? _carrier.ChannelRatio : 0.0f;
                SetFill(_resetFill, ratio);
            }
        }

        private static void SetFill(Image fill, float ratio)
        {
            var rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1.0f);
        }

        private void Build()
        {
            var canvasGo = new GameObject("YouCardCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(canvasGo.transform, false);
            _card = cardGo.GetComponent<Image>();
            _card.color = UiTheme.WoodDeep;

            var rect = _card.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = BottomLeft;
            rect.sizeDelta = new Vector2(420, 150);
            rect.anchoredPosition = new Vector2(40, 40);

            _class = MenuKit.Label(cardGo.transform, "", 26, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), new Vector2(0, 44), new Vector2(380, 34),
                TextAnchor.MiddleLeft);

            _detail = MenuKit.Label(cardGo.transform, "", 22, UiTheme.Offense,
                new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(380, 30),
                TextAnchor.MiddleLeft);

            (_chargeKey, _chargeFill) = BuildMeter(cardGo.transform, "Charge", -26);
            (_resetKey, _resetFill) = BuildMeter(cardGo.transform, "Reset", -26);
        }

        private static (Text, Image) BuildMeter(Transform parent, string name, float y)
        {
            var row = new GameObject($"{name}Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            MenuKit.Place(row.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(380, 30));

            var key = MenuKit.Label(row.transform, "", 18, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), new Vector2(-110, 0), new Vector2(150, 26),
                TextAnchor.MiddleLeft);

            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(row.transform, false);
            trackGo.GetComponent<Image>().color = UiTheme.WoodDark;
            MenuKit.Place(trackGo.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(70, 0), new Vector2(200, 16));

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var fill = fillGo.GetComponent<Image>();
            fill.color = UiTheme.Highlight;

            var fr = fill.rectTransform;
            fr.anchorMin = new Vector2(0.0f, 0.0f);
            fr.anchorMax = new Vector2(0.0f, 1.0f);
            fr.pivot = new Vector2(0.0f, 0.5f);
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;

            return (key, fill);
        }
    }
}
