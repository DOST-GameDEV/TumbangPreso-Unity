using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// What the Payo map adds round the stage it borrows from the Arena (`ArenaStage` with `Stone` set).
    ///
    /// THE BREAK, IN STONE. The monoliths travel between layouts on the break's shared clock (`ArenaStage`'s own
    /// travel) and the break's camera cuts to them (`ArenaBreakCamera`); the Arena's light show is not here. This
    /// gives each block that LOCKS a thud the eye can see: a punch of the break's camera and a ring and a puff of
    /// pale dust off its top, on the frame its own window ends. Every peer fires the same lock on the same frame
    /// of the travel, from the clock it already has. ⚠️ NO SOUND YET: the Arena's cues are synthesized and are the
    /// stadium's; this map's are owed as real recordings.
    ///
    /// A SLIPPER NO BOT CAN FETCH. Some stones need a running jump (owner, 2026-10-12: wanted), and a bot does not
    /// jump a gap: it walks, and it will not step off an edge (`AIController.EdgeSense`). A bot's own slipper lying
    /// on a stone it cannot walk to would end its round, so the HOST sends such a slipper back to the nearest block
    /// of the walk set after `StrandedSeconds`, by the Arena's own return (`ArenaFallRecovery.HostSendAway`, the
    /// one a slipper that leaves the stage already takes). A human's slipper is left where it lies: fetching it is
    /// the jump. Nothing new is sent: the return is the existing map recovery.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class PayoStageShow : MonoBehaviour
    {
        public const float StrandedSeconds = 2.5f;

        /// <summary>A block locked this frame (the middle of its top, and whether it is the last of the move): what the
        /// map's sound hangs its thud on (`PayoSound`). Every peer raises it for itself, off the break's shared clock.</summary>
        public static event System.Action<Vector3, bool> Locked;
        private static readonly Color Dust = new Color(0.86f, 0.84f, 0.78f);

        private long _match = -1;
        private int _round = -1;
        private bool[] _locked = System.Array.Empty<bool>();
        private bool _undocked;
        private readonly Vector3[] _points = new Vector3[5];
        private readonly Dictionary<Slipper, float> _stranded = new Dictionary<Slipper, float>();
        private readonly List<Slipper> _gone = new List<Slipper>();
        private SliceRunner _slice;
        private float _nextLook;

        // The players' lift in a break (owner, 2026-10-19: "in arena, the players are lifted by the drones when the map
        // changes. do the same thing here."): Kanlit carries them, `PayoBreakLift`. Installed here so no scene is rebuilt.
        private void Awake() { if (GetComponent<PayoBreakLift>() == null) gameObject.AddComponent<PayoBreakLift>(); }

        private void LateUpdate()
        {
            var stage = ArenaStage.Instance;
            var hp = HalftimePresentation.Instance;
            if (stage == null || hp == null || !stage.TryBreak(out var beats) || !beats.Showing || beats.From == beats.To) { _match = -1; return; }

            if (hp.MatchId != _match || hp.CompletedRound != _round)
            {
                _match = hp.MatchId; _round = hp.CompletedRound; _undocked = false;
                _locked = new bool[stage.Pieces.Length];
                // A peer that joins in the middle of a break plays what is left and none of what it missed.
                for (int i = 0; i < _locked.Length; i++)
                {
                    var past = stage.MoveOf(i, beats.From, beats.To, beats.Travel);
                    _locked[i] = !ArenaStage.Locks(past.Motion) || past.T >= 1.0f;
                }
                _undocked = beats.Age >= beats.Undock;
            }

            if (!_undocked && beats.Age >= beats.Undock) { _undocked = true; ArenaBreakCamera.Punch(0.7f); }

            var fx = ArenaFx.Instance;
            int last = stage.LastLocking(beats.From, beats.To);
            for (int i = 0; i < _locked.Length && i < stage.Pieces.Length; i++)
            {
                if (_locked[i]) continue;
                var move = stage.MoveOf(i, beats.From, beats.To, beats.Travel);
                if (move.T < 1.0f) continue;
                _locked[i] = true;
                ArenaBreakCamera.Punch(i == last ? 1.8f : 1.0f);
                int count = stage.PiecePoints(i, beats.To, _points);
                Locked?.Invoke(count > 0 ? _points[0] : stage.transform.position, i == last);
                if (fx == null) continue;
                if (count == 0) continue;
                var shape = stage.Pieces[i].Shapes[beats.To];
                float reach = Mathf.Max(shape.SX, shape.SZ) * 0.6f;
                fx.Ring(_points[0] + Vector3.up * 0.06f, reach * 0.3f, reach * 1.25f, Dust, 0.5f * ArenaFx.Light, 0.7f);
                for (int k = 0; k < count; k++)
                    fx.Dots(_points[k] + Vector3.up * 0.1f, Vector3.up, 80.0f, 6, 1.0f, 3.2f, Dust, 0.5f, 0.6f, 1.3f, 0.5f, 1.2f);
            }
        }

        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || Time.time < _nextLook) return;
            _nextLook = Time.time + 0.25f;
            var stage = ArenaStage.Instance;
            var recovery = ArenaFallRecovery.Instance;
            var round = GameServices.Round;
            if (stage == null || recovery == null || round == null || !round.RoundActive || stage.Applied < 0) { _stranded.Clear(); return; }
            if (_slice == null) _slice = FindFirstObjectByType<SliceRunner>();
            if (_slice == null || _slice.Slippers == null) return;

            _gone.Clear();
            foreach (var entry in _stranded) if (entry.Key == null || !entry.Key.gameObject.activeSelf || entry.Key.State != SlipperState.Loose) _gone.Add(entry.Key);
            foreach (var shoe in _gone) _stranded.Remove(shoe);

            foreach (var shoe in _slice.Slippers)
            {
                if (shoe == null || !shoe.gameObject.activeSelf || shoe.State != SlipperState.Loose || shoe.Velocity.sqrMagnitude > 0.04f) continue;
                var owner = round.PlayerAt(shoe.OwnerSlot);
                if (owner == null || !owner.IsBot || !OffTheWalkSet(stage, shoe.transform.position)) { _stranded.Remove(shoe); continue; }
                if (!_stranded.TryGetValue(shoe, out float since)) { _stranded[shoe] = Time.time; continue; }
                if (Time.time - since < StrandedSeconds) continue;
                _stranded.Remove(shoe);
                recovery.HostSendAway(shoe, shoe.transform.position, 0.6f);
            }
        }

        /// <summary>True where the point rests on a block only a jump, a geyser or a lift reaches (the builder marks those `Bonus`).</summary>
        private static bool OffTheWalkSet(ArenaStage stage, Vector3 world)
        {
            Vector3 p = stage.transform.InverseTransformPoint(world);
            foreach (var piece in stage.Pieces)
            {
                if (piece == null || piece.Shapes == null || stage.Applied >= piece.Shapes.Length) continue;
                var shape = piece.Shapes[stage.Applied];
                if (!shape.Exists || !shape.Contains(p.x, p.z, 0.0f)) continue;
                if (Mathf.Abs(shape.HeightAt(p.x, p.z) - p.y) > 0.6f) continue;
                return shape.Bonus;
            }
            return false;
        }
    }
}
