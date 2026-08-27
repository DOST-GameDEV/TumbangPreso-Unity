using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The ring bolted up next to the LRT column, and Ilalim ng Tulay's one skill toy.
    ///
    /// ⚠️⚠️ IT AWARDS NOTHING AND MUST KEEP AWARDING NOTHING. `MatchDirector.AddScore` is the
    /// one function that creates a point in this game, host side, and that is what makes a
    /// point uncreatable on a client. A map prop that hands out score is a second path into the
    /// scoreboard and it would be the only one. This fires a callout and, in Classic, Street
    /// Hype, which is cosmetic by construction: `Hud.ReportStyle` cannot reach the score.
    ///
    /// ⚠️ THE CROSSING IS TESTED AGAINST THE PREVIOUS POSITION, NOT AGAINST AN OVERLAP. A
    /// tsinelas moves 24 m/s at a full throw and the ring is 0.04 m thick, so at 60 fps the
    /// slipper travels 0.4 m per frame and a trigger volume the size of the ring is missed
    /// roughly nine frames out of ten. This is the same reason the rest of the game resolves
    /// contact by distance rather than by `OnTriggerEnter`: 16 of 36 measured overlaps failed
    /// to land, and that was on bodies moving far slower than this.
    /// </summary>
    public sealed class BridgeHoop : MonoBehaviour
    {
        [Tooltip("Ring centre, in this object's local space. Measured from env_basketball_ring.obj.")]
        public Vector3 RingCentre = new Vector3(0.0f, 3.07f, 0.0f);

        [Tooltip("Ring radius. The model's is 0.25; the catch is slightly generous.")]
        public float RingRadius = 0.30f;

        [Tooltip("Street Hype awarded in Classic. Cosmetic; see the class note.")]
        public float HypeReward = 12.0f;

        [Tooltip("Seconds before the same hoop can be scored again, so one rattle is one basket.")]
        public float Cooldown = 1.2f;

        private readonly Dictionary<Slipper, Vector3> _previous = new Dictionary<Slipper, Vector3>();
        private float _readyAt;

        private Vector3 WorldCentre => transform.TransformPoint(RingCentre);

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null) return;

            Vector3 centre = WorldCentre;

            foreach (var slipper in Object.FindObjectsByType<Slipper>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (slipper == null) continue;

                Vector3 now = slipper.transform.position;

                if (!_previous.TryGetValue(slipper, out Vector3 before))
                {
                    _previous[slipper] = now;
                    continue;
                }

                _previous[slipper] = now;

                if (slipper.State != SlipperState.InFlight) continue;
                if (Time.time < _readyAt) continue;

                // Downward through the ring plane only. A slipper on the way UP through a hoop
                // is not a basket in any game anybody has played.
                if (before.y <= centre.y || now.y > centre.y) continue;

                float span = before.y - now.y;
                if (span <= 0.0001f) continue;

                float t = (before.y - centre.y) / span;
                Vector3 crossing = Vector3.Lerp(before, now, t);

                float dx = crossing.x - centre.x;
                float dz = crossing.z - centre.z;
                if (dx * dx + dz * dz > RingRadius * RingRadius) continue;

                Score(slipper, centre);
                break;
            }

            // The table is keyed on live slippers, and a match rebuilds them between rounds.
            if (_previous.Count <= Balance.PlayerCount * 2) return;

            _previous.Clear();
        }

        private void Score(Slipper slipper, Vector3 centre)
        {
            _readyAt = Time.time + Cooldown;

            // ⚠️⚠️ THIS READ `"sfx_lata_hit"`, WHICH IS NOT A DECLARED CUE AND HAS NO
            // FILE, so the hoop bonus has never made a sound. The event is a scoring
            // award, the popup beside it says TRES!, and `score_award` is the cue this
            // game already uses to say "you just earned something".
            GameServices.Audio?.PlayAtVaried("score_award", centre, 1.18f, 1.32f, 0.85f);
            ComicPopup.Spawn(centre + Vector3.down * 0.5f, "TRES!", UiTheme.Highlight, 1.35f,
                             ComicPopup.Weight.Cast);
            ImpactBurst.SpawnAt(centre);

            // ⚠️ CREDIT GOES TO THE THROWER, NOT TO WHOEVER OWNS THE TSINELAS. A slipper that
            // has been shoved, banked or knocked out of somebody's hand still belongs to its
            // owner; the person who earned the shot is the one who threw it.
            int slot = slipper.ThrowerSlot;
            if (slot < 0) return;

            // ⚠️ NOT RELAYED. Every peer runs this trigger from its own copy of the slipper, so
            // the thrower's screen awards it directly; a relay on top would pay it twice.
            Hud.ReportStyle(slot, HypeReward, "TRES SA ILALIM", relay: false);
        }
    }
}
