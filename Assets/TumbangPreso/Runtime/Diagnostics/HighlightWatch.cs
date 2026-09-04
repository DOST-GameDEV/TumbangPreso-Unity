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
    /// `RoundDirector.Players` is the list the game already walks and the state is four floats.
    /// `HudPerformanceProbe` exists because a single HUD string rebuilt per frame cost the 6x
    /// probe an eighth of its frames, so the budget for a per-frame watcher is stated rather than
    /// assumed.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class HighlightWatch : MonoBehaviour
    {
        /// <summary>
        /// How close each attacker has been to the taya since it last became "near".
        ///
        /// ⚠️⚠️ THE MINIMUM IS WHAT MAKES THE MARKER HONEST. Reporting the distance at the moment
        /// the attacker leaves the radius would report 1.30 m every single time, because that IS
        /// the radius; what a person watching calls a close call is how close it actually got, and
        /// that number is only knowable by keeping the minimum while it was inside.
        /// </summary>
        private readonly float[] _closest = new float[Balance.PlayerCount];
        private readonly bool[] _inside = new bool[Balance.PlayerCount];

        private void OnEnable()
        {
            for (int i = 0; i < _closest.Length; i++)
            {
                _closest[i] = float.MaxValue;
                _inside[i] = false;
            }
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive) return;

            var taya = Taya(round);
            if (taya == null) return;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round.PlayerAt(slot);
                if (unit == null || unit.IsDefender)
                {
                    _inside[slot] = false;
                    continue;
                }

                // ⚠️⚠️ ONLY WHILE THEY COULD ACTUALLY HAVE BEEN CAUGHT. `IsTaggable` is the same
                // question the tag itself asks, so a player standing next to the taya during a
                // respawn, a stun or the warm-up buffer is not "escaping" anything. Without this
                // the first frame of every round would report four close calls.
                if (!unit.IsTaggable())
                {
                    _inside[slot] = false;
                    _closest[slot] = float.MaxValue;
                    continue;
                }

                Vector3 a = unit.transform.position;
                Vector3 b = taya.transform.position;
                a.y = 0.0f;
                b.y = 0.0f;

                float d = Vector3.Distance(a, b);

                if (d <= HighlightRules.CloseCallMetres)
                {
                    _inside[slot] = true;
                    if (d < _closest[slot]) _closest[slot] = d;
                    continue;
                }

                // ⚠️ THE MARKER FIRES ON THE WAY OUT, NOT ON THE WAY IN, because that is the frame
                // the claim becomes true: while they are still inside it, the taya may yet catch
                // them and the moment is a tag rather than an escape.
                if (!_inside[slot]) continue;

                _inside[slot] = false;
                float closest = _closest[slot];
                _closest[slot] = float.MaxValue;

                if (closest <= HighlightRules.CloseCallMetres)
                    MatchHighlights.NoteCloseCall(slot, taya.PlayerSlot, closest);
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
