using System.Collections.Generic;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>Enlarges complete HUD groups around their screen anchors, preserving edge margins.</summary>
    public sealed class HudReadingLayout : MonoBehaviour
    {
        private struct Group
        {
            public RectTransform Rect;
            public Vector2 Position, Size, Flow;
            public Vector3 Scale;
        }
        private readonly List<Group> _groups = new List<Group>();
        private float _scale = -1;

        public static void Watch(RectTransform target, Vector2 flow = default)
        {
            var layout = target.gameObject.AddComponent<HudReadingLayout>();
            layout._groups.Add(new Group { Rect = target, Position = target.anchoredPosition,
                Size = target.sizeDelta, Scale = target.localScale, Flow = flow });
            layout.Refresh();
        }

        public static void Install(RectTransform root)
        {
            var layout = root.gameObject.AddComponent<HudReadingLayout>();
            string[] names = { "MatchScores", "RoundClock", "CanReadout", "LocalState", "ContextualAction",
                "PowerSeals", "MatchEventFeed", "SpectatorReadout", "SandboxState", "MatchToast",
                "Reticle", "HitConfirmation", "Countdown", "CalloutCaption", "TimedStatus0", "TimedStatus1", "TimedStatus2", "TimedStatus3" };
            foreach (string name in names)
            {
                var rect = root.Find(name) as RectTransform;
                if (rect != null) layout._groups.Add(new Group { Rect = rect, Position = rect.anchoredPosition,
                    Size = rect.sizeDelta, Scale = rect.localScale });
            }
            layout.Refresh();
        }

        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            var settings = SettingsStore.Current;
            float scale = Mathf.Max(GameSettings.ValidHudScale(settings.HudScale), settings.LargerText ? 1.2f : 1f);
            if (Mathf.Approximately(_scale, scale)) return;
            _scale = scale;
            foreach (var group in _groups)
            {
                var rect = group.Rect;
                if (rect == null) continue;
                rect.localScale = group.Scale * scale;
                var position = group.Position + (Vector2.Scale(rect.pivot - rect.anchorMin, group.Size) + group.Flow) * (scale - 1);
                // Flow secondary notices beneath the enlarged panels, and keep the
                // action prompt above the ability deck rather than enlarging into it.
                if (rect.name.StartsWith("TimedStatus", System.StringComparison.Ordinal)) position.y -= 320 * (scale - 1);
                if (rect.name == "MatchEventFeed") position.y -= 170 * (scale - 1);
                if (rect.name == "ContextualAction") position.y += 320 * (scale - 1);
                rect.anchoredPosition = position;
            }
        }
    }
}
