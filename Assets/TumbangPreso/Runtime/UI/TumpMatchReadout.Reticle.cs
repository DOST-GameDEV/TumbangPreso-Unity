using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Feeds the drawn reticle (TODO VISUAL-1.6). `HudReticle`'s header has the design.
    ///
    /// ⚠️ EVERY VALUE HERE IS READ FROM THE SAME PLACE THE RULES READ IT. The charge and spin are
    /// the carrier's own (`Carrier.ChargeRatio`, `CurrentPektusSpin`); "refused" is the can's
    /// protection, which is what refuses a throw at it; the cooldown sweep is the longest of the
    /// verb rows `StatusStack` already builds from the verbs' own timers; the taya's reach tick
    /// asks `Combat.InCone` with the punch's own range and arc, which is the host's test. A
    /// reticle that guessed would teach the player a reach the game does not have.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private HudReticle _reticle;
        private static readonly string[] VerbCooldowns = { "THROW CD", "SHOVE CD", "LUNGE CD", "TAG CD" };
        private bool _reticleShot;
        private float _shotCharge, _shotSpin, _shotCooldown;
        private bool _shotRefused, _shotReach;

        /// <summary>Review captures only: holds the reticle in one state until
        /// <see cref="EndReticleShot"/>, so every state can be photographed in one frame.</summary>
        public void ReticleForShot(float charge, float spin, float cooldown, bool refused, bool reach)
        { _reticleShot = true; _shotCharge = charge; _shotSpin = spin; _shotCooldown = cooldown; _shotRefused = refused; _shotReach = reach; }
        public void EndReticleShot() => _reticleShot = false;

        private void PaintReticle()
        {
            if (_reticle == null || !_reticle.enabled) return;
            if (_reticleShot) { _reticle.Set(_shotCharge, _shotSpin, _shotCooldown, _shotRefused, _shotReach); return; }
            var local = _aimOwner; var round = GameServices.Round;
            if (local == null || round == null) { _reticle.Set(0, 0, 0, false, false); return; }
            bool charging = _aimCarrier != null && _aimCarrier.IsCharging;
            float charge = charging ? Mathf.Max(.02f, _aimCarrier.ChargeRatio) : 0;
            float spin = charging ? _aimCarrier.CurrentPektusSpin : 0;
            bool refused = charging && round.Lata != null && round.Lata.IsProtected;

            float cooldown = 0;
            foreach (var row in _statusRows)
            {
                if (!row.Timed || row.Total <= 0 || System.Array.IndexOf(VerbCooldowns, row.Label) < 0) continue;
                cooldown = Mathf.Max(cooldown, Mathf.Clamp01(row.Remaining / row.Total));
            }
            _reticle.Set(charge, spin, cooldown, refused, TayaInReach(local, round));
        }

        /// <summary>True while the local taya faces a catchable attacker inside the punch cone.</summary>
        private static bool TayaInReach(CharacterMotor local, RoundDirector round)
        {
            if (!local.IsDefender || round.Lata == null || !round.Lata.IsUpright) return false;
            var from = local.transform.position; var facing = local.transform.forward;
            foreach (var other in round.Players)
            {
                if (other == null || other == local || other.IsDefender || !other.IsTaggable()) continue;
                var to = other.transform.position - from; to.y = 0;
                float angle = Vector3.Angle(new Vector3(facing.x, 0, facing.z), to);
                if (Combat.InCone(to.magnitude, angle, Balance.PunchRange, Balance.PunchArcDeg)) return true;
            }
            return false;
        }
    }
}
