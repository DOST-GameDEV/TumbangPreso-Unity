using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The four status icons (owner's status table, 2026-09-25), drawn by
    /// `tools/build_ability_icons.py` into `Resources/UI/status-icons`. A status sits on a round
    /// badge so it never reads as an ability icon: an ability is something you DO, a status is
    /// something done TO you. The tooltip is the owner's column, verbatim, from `Core.StatusRules`.
    /// </summary>
    public static class StatusIcons
    {
        private static readonly Dictionary<StatusKind, Sprite> Cache = new Dictionary<StatusKind, Sprite>();

        public static Sprite For(StatusKind kind)
        {
            if (kind == StatusKind.None) return null;
            if (Cache.TryGetValue(kind, out var sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("UI/status-icons/Status" + kind);
            Cache[kind] = sprite;
            return sprite;
        }

        public static string Name(StatusKind kind) => StatusRules.For(kind)?.Name ?? "";
        public static string Tooltip(StatusKind kind) => StatusRules.For(kind)?.Tooltip ?? "";

        /// <summary>The statuses running on a body, strongest first (a hold before a slow).</summary>
        public static void Live(CharacterMotor body, List<StatusKind> into)
        {
            into.Clear();
            if (body == null) return;
            if (body.IsTagged) into.Add(StatusKind.Tagged);
            if (body.IsFrozen) into.Add(StatusKind.Frozen);
            if (body.IsWhirled) into.Add(StatusKind.Whirled);
            if (body.IsChilled) into.Add(StatusKind.Chilled);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();
    }
}
