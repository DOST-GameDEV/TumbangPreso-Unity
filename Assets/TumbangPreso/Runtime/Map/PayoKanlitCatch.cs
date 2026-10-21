using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S CATCHER, CHOSEN: put on the stage by `PayoSceneBuilder` with `ArenaFallRecovery.NoDrone` set, so the
    /// Arena's drone and its beam are not drawn on this map and a `PayoKanlit` is drawn instead (2026-10-18; the bubble
    /// spirit `PayoBubbleSpiritCatch` and the hawk `PayoCatchBird` before it, kept, not used). Every peer, every frame:
    /// each body whose replicated `EdgeKind` is `Drone` gets a Kanlit (one per body, kept and reused) and is attended.
    /// It then lets go, hops and flies off by itself.
    /// ⚠️ PRESENTATION ONLY: the catch, the carry, its timing and the freeze are `ArenaFallRecovery` and
    /// `CharacterMotor`'s, untouched. Nothing is made until the first carry; nothing is made per frame.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class PayoKanlitCatch : MonoBehaviour
    {
        private readonly Dictionary<CharacterMotor, PayoKanlit> _buddies = new Dictionary<CharacterMotor, PayoKanlit>();
        private Transform _root;
        private bool _failed;

        /// <summary>How many have been made this scene (a probe's count).</summary>
        public int Made => _buddies.Count;

        private void OnDisable()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null; _buddies.Clear();
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null || _failed) return;
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            var players = round.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var who = players[i];
                if (who == null || !who.gameObject.activeInHierarchy || who.EdgeKind != EdgeRecoveryKind.Drone) continue;
                if (!_buddies.TryGetValue(who, out var buddy) || buddy == null)
                {
                    if (_root == null) _root = new GameObject("Payo Kanlit catchers (made at run time)") { hideFlags = HideFlags.DontSave }.transform;
                    buddy = PayoKanlit.Build(_root);
                    if (buddy == null) { _failed = true; return; }
                    _buddies[who] = buddy;
                }
                buddy.Attend(who);
            }
            PayoKanlit.Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
        }
    }
}
