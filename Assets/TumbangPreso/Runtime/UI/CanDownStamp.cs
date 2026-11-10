using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// CAN DOWN, painted in yellow lettering on a soft yellow band with a can each side (live HUD, 2026-11-01;
    /// `boot_match.can_down`). The cans are a stand-in sprite until a real render of the can exists.
    ///
    /// Owner, 2026-10-22: the knockdown is the attackers' payoff and it has to read on every screen
    /// ("everything has to be animated", anticipation, impact, aftermath). Driven by the replicated
    /// `MatchFlair.Kind.LataDown`, so every peer and spectator gets the same beat; it decides nothing.
    ///   * the letters stamp in, the band wipes out from the middle, the cans pop last with a tilt wobble;
    ///   * the screen's edges flash in the thrower's seat colour and breathe out;
    ///   * it holds, then fades and rises away.
    /// One at a time: a second knockdown restarts it in the new thrower's colour.
    /// </summary>
    public sealed class CanDownStamp : MonoBehaviour
    {
        private const float Hold = 1f, Exit = .25f, SlamIn = .11f;
        private const float EdgeThickness = 30f;
        private static CanDownStamp _live;

        private LiveNotice _notice;
        private readonly Image[] _edges = new Image[4];
        private Color _seat;
        private float _born, _life;

        /// <summary>True while the notice stands, so the HUD's message lanes can make room for it.</summary>
        public static bool Showing => _live != null;
        /// <summary>The room it takes under the timer, measured from its top.</summary>
        public const float LaneHeight = 100;
        public static void SetLaneTop(float top) { if (_live != null) _live._notice.Rest = new Vector2(0, -(top + 46)); }

        public static void Play(int throwerSlot)
        {
            var round = GameServices.Round;
            var owner = round != null && round.Lata != null ? round.Lata.transform : null;
            if (owner == null || !round.RoundActive) return;
            if (_live != null) Destroy(_live.gameObject);
            var canvas = OwnerUiLayout.Canvas(owner, "~CanDownStamp", 170);
            CleanFeedLayer.Attach(canvas.gameObject);
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            _live = canvas.gameObject.AddComponent<CanDownStamp>();
            _live.Build((RectTransform)canvas.transform, PlayerIdentity.Colour(throwerSlot));
        }

        private void Build(RectTransform root, Color seat)
        {
            _seat = seat; _born = Time.unscaledTime;
            for (int i = 0; i < 4; i++)
            {
                var edge = OwnerUiLayout.Rect(root, "CanDownEdge" + i).gameObject.AddComponent<Image>();
                edge.raycastTarget = false;
                var r = edge.rectTransform;
                bool vertical = i >= 2;
                r.anchorMin = new Vector2(i == 3 ? 1 : 0, i == 1 ? 1 : 0);
                r.anchorMax = new Vector2(i == 2 ? 0 : 1, i == 0 ? 0 : 1);
                r.pivot = new Vector2(i == 3 ? 1 : 0, i == 1 ? 1 : 0);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = vertical ? new Vector2(EdgeThickness, 0) : new Vector2(0, EdgeThickness);
                _edges[i] = edge;
            }
            _notice = LiveNotice.Create(root, "CanDownPlate", 74, LiveHud.Yellow, LiveHud.YellowInk, LiveHud.YellowInk, 9, 5, 6, LiveHud.Yellow, LiveHud.Picture("can"));
            _notice.Title.name = "CanDownTitle";
            _notice.SetTitle("CAN DOWN");
            _notice.Rest = new Vector2(0, -(130 + 46));
            _life = _notice.Duration + Hold + Exit;
            Animate();
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (Time.unscaledTime - _born >= _life || round == null || !round.RoundActive) { Destroy(gameObject); return; }
            Animate();
        }

        private void OnDestroy() { if (_live == this) _live = null; }

        private void Animate()
        {
            float t = Time.unscaledTime - _born;
            float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;
            // The edges: a hard flash on the slam, two quick breaths, gone with the notice.
            float edge = t < SlamIn ? 0 : Mathf.Exp(-(t - SlamIn) * 4.5f) * (.75f + .25f * Mathf.Cos((t - SlamIn) * 18f));
            edge *= .55f * flash;
            foreach (var e in _edges) { var c = _seat; c.a = edge; e.color = c; }
            float exitAt = _life - Exit;
            _notice.Pose(t, t > exitAt ? Mathf.Clamp01((t - exitAt) / Exit) : 0, false);
        }
    }
}
