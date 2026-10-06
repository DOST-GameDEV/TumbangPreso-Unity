using System;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A choice in the in-match menus (pause, broadcast, training), drawn as the match HUD's toy
    /// tile instead of the front end's stickers (UI revamp 2026-10-06).
    ///
    /// ⚠️ IT IS A REAL `Button`. Mouse, pad, keyboard and touch reach it through the existing
    /// EventSystem and `MenuNav`, exactly like the controls it replaces; only the drawing moved.
    ///
    /// Ordinary choices are cream with a tan side, the one primary choice is honey, a
    /// destructive one is the refusal red. Focus is a thick brown bevelled ring with the tile
    /// lifted; press sinks the face into its side; disabled flattens and fades. Reduced UI
    /// Motion keeps every state and drops the movement.
    /// </summary>
    public sealed class MatchMenuButton : Button
    {
        public enum Kind { Ordinary, Primary, Destructive }

        public HudCard Face, Ring;
        public Text Label;
        public RectTransform Body;
        public Kind Role;
        public event Action Focused;

        private const float Depth = 8, Bevel = 16;
        private float _lift, _liftTarget, _press, _pressTarget;
        private bool _attended;
        private float _quietUntil;

        public static MatchMenuButton Create(Transform parent, string name, string words, Kind role, Action onClick, int size = 40)
        {
            var root = OwnerUiLayout.Rect(parent, name);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0);
            var button = root.gameObject.AddComponent<MatchMenuButton>();
            button.targetGraphic = hit; button.Role = role;
            button.Body = OwnerUiLayout.Rect(root, "Body"); OwnerUiLayout.Fill(button.Body);
            button.Ring = OwnerUiLayout.Rect(button.Body, "FocusRing").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(button.Ring.rectTransform);
            button.Ring.rectTransform.offsetMin = new Vector2(-8, -8 - Depth); button.Ring.rectTransform.offsetMax = new Vector2(8, 8);
            button.Ring.Toy(Color.clear, Color.clear, 0, Bevel + 5, 0); button.Ring.Sheen = false;
            button.Ring.Border = HudDraw.Brown; button.Ring.BorderWidth = 0; button.Ring.FollowContrast = false; button.Ring.raycastTarget = false;
            button.Face = OwnerUiLayout.Rect(button.Body, "Face").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(button.Face.rectTransform);
            button.Face.Toy(HudDraw.Cream, HudDraw.CreamSide, Depth, Bevel, .42f).raycastTarget = false;
            button.Label = OwnerUiLayout.Text(button.Body, "Label", words, size, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(button.Label.rectTransform);
            button.Label.rectTransform.offsetMin = new Vector2(24, 2); button.Label.rectTransform.offsetMax = new Vector2(-24, -2);
            button.Label.alignment = TextAnchor.MiddleCenter; button.Label.horizontalOverflow = HorizontalWrapMode.Wrap;
            button.Label.verticalOverflow = VerticalWrapMode.Overflow;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.Paint();
            return button;
        }

        protected override void Awake() { base.Awake(); transition = Transition.None; }

        protected override void OnEnable()
        {
            base.OnEnable();
            _quietUntil = Time.unscaledTime + .3f; _lift = _liftTarget = 0; _press = _pressTarget = 0;
            Paint(); Apply();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (!(eventData is PointerEventData)) Focused?.Invoke();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (!Application.isPlaying || Face == null) return;
            bool focused = state == SelectionState.Selected;
            bool attended = focused || state == SelectionState.Highlighted || state == SelectionState.Pressed;
            _liftTarget = attended && state != SelectionState.Pressed ? 1 : 0;
            _pressTarget = state == SelectionState.Pressed ? 1 : 0;
            Ring.BorderWidth = focused ? 5 : state == SelectionState.Highlighted ? 3 : 0;
            Ring.SetVerticesDirty();
            if (attended != _attended)
            {
                _attended = attended;
                if (attended && Time.unscaledTime >= _quietUntil && state != SelectionState.Pressed) MenuSfx.Hover();
            }
            Paint();
            if (instant || Settings.SettingsStore.Current.ReducedUiMotion) { _lift = _liftTarget; _press = _pressTarget; }
            Apply();
        }

        /// <summary>Face, side and words for the role and the interactable state.</summary>
        public void Paint()
        {
            if (Face == null) return;
            bool live = IsInteractable();
            Color fill = Role == Kind.Primary ? HudDraw.Honey : Role == Kind.Destructive ? HudDraw.Alarm : HudDraw.Cream;
            Color side = Role == Kind.Primary ? HudDraw.HoneySide : Role == Kind.Destructive ? HudDraw.AlarmSide : HudDraw.CreamSide;
            Color ink = Role == Kind.Destructive ? Color.white : HudDraw.Brown;
            if (!live) { fill = Color.Lerp(fill, HudDraw.CreamSide, .55f); ink.a = .5f; }
            Face.color = fill; Face.Side = side; Face.Depth = live ? Depth : 2; Face.SetVerticesDirty();
            if (Label != null) Label.color = ink;
        }

        private void Update()
        {
            if (Mathf.Approximately(_lift, _liftTarget) && Mathf.Approximately(_press, _pressTarget)) return;
            float step = Time.unscaledDeltaTime * 14;
            _lift = Mathf.MoveTowards(_lift, _liftTarget, step);
            _press = Mathf.MoveTowards(_press, _pressTarget, step * 1.6f);
            Apply();
        }

        /// <summary>Lifted when attended, sunk into its own side when pressed.</summary>
        private void Apply()
        {
            if (Body == null || Face == null) return;
            float sink = (Depth - 2) * _press;
            Body.anchoredPosition = new Vector2(0, 3 * _lift - sink);
            if (IsInteractable() && !Mathf.Approximately(Face.Depth, Depth - sink)) { Face.Depth = Depth - sink; Face.SetVerticesDirty(); }
        }
    }
}
