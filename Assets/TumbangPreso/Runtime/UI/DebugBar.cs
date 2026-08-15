using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The debug switcher's on-screen readout, converted from `scripts/ui/debug_bar.gd`.
    ///
    /// ⚠️ DEBUG-ONLY. It removes itself in a release build even if nobody runs a removal
    /// checklist, exactly as the original does.
    ///
    /// ⚠️⚠️ DELIBERATELY UGLY, AND THAT IS THE DESIGN. Plain white on a black strip, with NO
    /// reference to <see cref="UiTheme"/> and no styling of its own, so nobody mistakes it
    /// for shipping UI and so deleting it can never leave a hole in the real design system.
    /// **Do not restyle this to match the game.** If it ever looks like it belongs, somebody
    /// will ship it.
    /// </summary>
    public sealed class DebugBar : MonoBehaviour
    {
        /// <summary>One slot since the 2026-07-29 input overhaul — the old two-slot bindings
        /// addressed a second slot that no longer exists.</summary>
        public const string KeysText = "F1-F4 drive · Tab cycle · F6 reset";

        private Text _slots;
        private Text _keys;

        private void Awake()
        {
            // Same self-disable as the switcher: gone in a release build regardless.
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            Build();
        }

        private void OnEnable()
        {
            // §3.5.4: after a role swap you need to see at a glance which unit you are now
            // driving, because the role flips underneath you.
            if (GameServices.Match != null) GameServices.Match.RoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot) => RefreshFromSwitcher();

        /// <summary>Called by the switcher. Debug registers with debug; no gameplay script
        /// names either one.</summary>
        public void Refresh(string drivenText)
        {
            if (_slots != null) _slots.text = $"DEBUG  ▶ {drivenText}";
        }

        private void RefreshFromSwitcher()
        {
            var switcher = FindFirstObjectByType<DebugPlayerSwitcher>();
            if (switcher != null) switcher.RefreshReadout();
        }

        private void Build()
        {
            var canvasGo = new GameObject("DebugBarCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;   // over everything, including the pause overlay

            var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(canvasGo.transform, false);
            var stripRect = strip.GetComponent<RectTransform>();
            stripRect.anchorMin = new Vector2(0.0f, 1.0f);
            stripRect.anchorMax = new Vector2(1.0f, 1.0f);
            stripRect.pivot = new Vector2(0.5f, 1.0f);
            stripRect.sizeDelta = new Vector2(0.0f, 22.0f);
            stripRect.anchoredPosition = Vector2.zero;
            strip.GetComponent<Image>().color = Color.black;

            _slots = MakeLabel(strip.transform, "Slots", TextAnchor.MiddleLeft, 8.0f);
            _keys = MakeLabel(strip.transform, "Keys", TextAnchor.MiddleRight, -8.0f);
            _keys.text = KeysText;
        }

        private static Text MakeLabel(Transform parent, string name, TextAnchor align, float xPad)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 1.0f);
            rect.offsetMin = new Vector2(xPad > 0 ? xPad : 0, 0);
            rect.offsetMax = new Vector2(xPad < 0 ? xPad : 0, 0);

            var t = go.GetComponent<Text>();

            // The built-in font, NOT the game's. See the class note: this must not inherit
            // anything from the project's design system.
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 13;
            t.color = Color.white;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;

            return t;
        }
    }
}
