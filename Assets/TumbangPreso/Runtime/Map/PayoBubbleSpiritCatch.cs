using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S CATCHER, CHOSEN: put on the stage by `PayoSceneBuilder` with `ArenaFallRecovery.NoDrone` set, so the
    /// Arena's drone and its beam are not drawn on this map and a `PayoBubbleSpirit` is drawn instead (2026-10-18; the
    /// hawk, `PayoCatchBird`, before it). Every peer, every frame: each body whose replicated `EdgeKind` is `Drone` gets
    /// a spirit (one per body, kept and reused) and is attended. The spirit then pops, bows and sinks by itself.
    /// ⚠️ PRESENTATION ONLY: the catch, the carry, its timing and the freeze are `ArenaFallRecovery` and
    /// `CharacterMotor`'s, untouched. Nothing is made until the first carry; nothing is made per frame.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class PayoBubbleSpiritCatch : MonoBehaviour
    {
        private readonly Dictionary<CharacterMotor, PayoBubbleSpirit> _spirits = new Dictionary<CharacterMotor, PayoBubbleSpirit>();
        private Transform _root;
        private bool _failed;

        /// <summary>How many spirits have been made this scene (a probe's count).</summary>
        public int Made => _spirits.Count;

        private void OnDisable()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null; _spirits.Clear();
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
                if (!_spirits.TryGetValue(who, out var spirit) || spirit == null)
                {
                    if (_root == null) _root = new GameObject("Payo bubble spirits (made at run time)") { hideFlags = HideFlags.DontSave }.transform;
                    spirit = PayoBubbleSpirit.Build(_root);
                    if (spirit == null) { _failed = true; return; }
                    _spirits[who] = spirit;
                }
                spirit.Attend(who);
            }
            PayoBubbleSpirit.Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
        }
    }
}
