using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Watches for the moments that are the ABSENCE of something, which no call site can raise.
    ///
    /// ⚠️⚠️ A CLOSE CALL IS A TAG THAT DID NOT HAPPEN. Every other kind on
    /// `HighlightKind` has a place in the code where it occurs: a bank shot, a block, a
    /// knockdown, an ultimate, and `MatchFlair` already announces every one of them to every
    /// peer. "Got inside the taya's reach and got out again" occurs nowhere: it is a distance
    /// that closed and then opened, and the only way to see it is to look every frame.
    ///
    /// ⚠️⚠️ IT RUNS ON EVERY PEER AND WRITES NOTHING AUTHORITATIVE. Positions are replicated and
    /// the taya is derived (`docs/VISION.md` § 4), so every machine can answer this for itself,
    /// and `MatchHighlights` is a local record by design. Nothing here awards, moves or decides.
    ///
    /// ⚠️ IT COSTS ONE DISTANCE PER ATTACKER PER FRAME, three of them, and no allocation:
    /// `RoundDirector.Players` is the list the game already walks and the state is a fixed four-seat set.
    /// `HudPerformanceProbe` exists because a single HUD string rebuilt per frame cost the 6x
    /// probe an eighth of its frames, so the budget for a per-frame watcher is stated rather than
    /// assumed.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class HighlightWatch : MonoBehaviour
    {
        // One episode per attacker. The wider exit boundary prevents distance jitter
        // from inventing repeated escapes. All timing is simulation time.
        private readonly float[] _closest = new float[Balance.PlayerCount];
        private readonly bool[] _inside = new bool[Balance.PlayerCount];
        private readonly Vector3[] _previous = new Vector3[Balance.PlayerCount];
        private readonly bool[] _sampled = new bool[Balance.PlayerCount];
        private RoundDirector _round;
        private Vector3 _previousTaya;
        private bool _tayaSampled;
        private int _roundNumber = -1, _tayaSlot = -1;
        private const float ExitMargin = .45f;

        private void OnEnable()
        {
            Clear();
            Visual.MatchFlair.Presented += OnPresented;
        }
        private void OnDisable()
        {
            Visual.MatchFlair.Presented -= OnPresented;
            Clear();
        }
        private void Clear()
        {
            _tayaSampled = false;
            for (int i = 0; i < _inside.Length; i++)
            { ClearSeat(i); _sampled[i] = false; }
        }
        private void ClearSeat(int slot)
        { _inside[slot] = false; _closest[slot] = float.MaxValue; }
        private void OnPresented(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != Visual.MatchFlair.Kind.Tag || subject < 0 || subject >= _inside.Length) return;
            // Tag teleport is not an escape, regardless of event/state arrival order.
            ClearSeat(subject);
            MatchHighlights.ResetEvasion(subject);
        }

        private void Update()
        {
            var round = GameServices.Round;
            int number = GameServices.Match != null ? GameServices.Match.RoundNumber : -1;
            var taya = round != null ? Taya(round) : null;
            if (_round != round || _roundNumber != number || _tayaSlot != (taya != null ? taya.PlayerSlot : -1))
            {
                Clear(); MatchHighlights.ResetEvasions();
                _round = round; _roundNumber = number; _tayaSlot = taya != null ? taya.PlayerSlot : -1;
            }
            // IsTaggable alone deliberately excludes the can rule. Actual tags require
            // the upright can and an able taya as well. Protection only protects the can.
            if (round == null || !round.RoundActive || round.Lata == null || !round.Lata.IsUpright ||
                taya == null || !taya.CanAct()) { Clear(); return; }
            if (Time.deltaTime <= 0) return;
            if (_tayaSampled && Vector3.Distance(taya.transform.position, _previousTaya) > Mathf.Max(1f, Time.deltaTime * 24f)) Clear();
            _previousTaya = taya.transform.position; _tayaSampled = true;
            var combat = taya.GetComponent<CombatVerbs>();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round.PlayerAt(slot);
                if (unit == null || unit.IsDefender) { ClearSeat(slot); continue; }
                Vector3 position = unit.transform.position;
                bool jumped = _sampled[slot] && Vector3.Distance(position, _previous[slot]) >
                    Mathf.Max(1f, Time.deltaTime * 24f);
                _previous[slot] = position; _sampled[slot] = true;
                if (jumped) { ClearSeat(slot); continue; }
                Vector3 to = position - taya.transform.position; to.y = 0;
                float d = to.magnitude;
                bool safeExit = !unit.IsInsideBox() && unit.HoldingSlipper && !unit.IsStunned;
                if (!unit.IsTaggable() && !safeExit) { ClearSeat(slot); continue; }
                if (_inside[slot])
                {
                    _closest[slot] = Mathf.Min(_closest[slot], d);
                    if (safeExit || d >= HighlightRules.CloseCallMetres + ExitMargin)
                    {
                        float closest = _closest[slot]; ClearSeat(slot);
                        MatchHighlights.NoteCloseCall(slot, taya.PlayerSlot, closest);
                    }
                }
                else if (!safeExit && d <= HighlightRules.CloseCallMetres && combat != null &&
                    combat.PunchCooldownLeft <= 0 &&
                    Combat.InCone(d, Vector3.Angle(taya.transform.forward, to), Balance.PunchRange, Balance.PunchArcDeg))
                {
                    _inside[slot] = true; _closest[slot] = d;
                }
            }
        }

        private static CharacterMotor Taya(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && p.IsDefender) return p;

            return null;
        }

        /// <summary>
        /// How far the nearest taya is from a point, or -1 when there is no taya.
        ///
        /// ⚠️ IT IS STATIC AND LIVES HERE RATHER THAN ON `MatchHighlights`, because the pickup
        /// and the knockdown both need it and neither of them should have to walk the roster to
        /// answer a question this component already answers every frame.
        /// </summary>
        public static float MetresFromTaya(Vector3 at)
        {
            var round = GameServices.Round;
            if (round == null) return -1.0f;

            var taya = Taya(round);
            if (taya == null) return -1.0f;

            Vector3 a = at;
            Vector3 b = taya.transform.position;
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b);
        }
    }
}
