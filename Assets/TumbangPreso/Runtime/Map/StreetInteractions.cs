using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A street character's offer to the LOCAL player, shown by the match HUD's interact prompt.
    ///
    /// ⚠️ SCENERY ONLY, AND EMPTY ON EVERY MAP THAT HAS NO SUCH CHARACTER. The Ilalim rebuild's
    /// sidewalk beggar (<see cref="SidewalkLife"/>) calls <see cref="Offer"/> on each frame the local
    /// player stands within reach; `TumpMatchReadout.Prompts` and `Hud.UpdateInteractPrompt` read
    /// <see cref="ActionFor"/> LAST, after every match prompt, so picking up a tsinelas or any
    /// hero prompt always wins; `TouchHud` shows its INTERACT control while an offer stands (in
    /// Classic it is otherwise hidden). Nothing here is networked, scores or changes a stat: the
    /// press itself is read by the offering component from the player's own `Verb.Interact`.
    /// With no offer (every other map) every reader behaves exactly as before.
    /// </summary>
    public static class StreetInteractions
    {
        private static CharacterMotor _who;
        private static string _action;
        private static int _frame = int.MinValue;

        /// <summary>Offer `action` (a short verb phrase, "Give a coin") to `who` for this frame.</summary>
        public static void Offer(CharacterMotor who, string action)
        {
            if (who == null || string.IsNullOrEmpty(action)) return;
            _who = who; _action = action; _frame = Time.frameCount;
        }

        /// <summary>True while an offer is live this frame or the last (script order is free).</summary>
        public static bool Offered => _who != null && Time.frameCount - _frame <= 1;

        /// <summary>The offered action for `local`, or null when nothing is offered to them.</summary>
        public static string ActionFor(CharacterMotor local)
            => local != null && Offered && local == _who ? _action : null;
    }
}
