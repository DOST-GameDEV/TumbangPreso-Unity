using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class Slipper
    {
        private readonly RaycastHit[] _pressureHits = new RaycastHit[32];
        private void ResolveEmpoweredThrowImpact()
        {
            const float radius = EmpoweredThrowRules.Radius;
            var centre = transform.position;
            // With the s2 look on, the burst is drawn by `SeanThrowFire` on every peer (this method runs on the host only, so
            // the old blast below was never seen by anyone else); its sound is the same relayed cue the old blast played.
            if (Visual.SeanThrowFire.Painted) { Visual.SeanThrowFire.Burst(this, centre); NetCue.Play("sfx_slipper_burst", centre); }
            else Abilities.HeroHazards.CreateExplosionVisual(centre, radius, "",
                Abilities.HeroHazards.ExplosionStyle.Ignition);
            if (!NetAuthority.ShouldResolve() || GameServices.Round?.RoundActive != true) return;
            var round = GameServices.Round;
            foreach (var body in round.Players)
            {
                if (body == null || body.PlayerSlot == _throwerSlot || body.IsTagged
                    || !body.gameObject.activeInHierarchy) continue;
                var target = body.transform.position + Vector3.up * .4f;
                var delta = target - centre;
                if (delta.sqrMagnitude > radius * radius || !PressureLineClear(centre, target, body, null)) continue;
                delta.y = 0;
                if (delta.sqrMagnitude < .0001f) delta = Vector3.forward;
                float speed = Mathf.Sqrt(2 * Balance.Friction * EmpoweredThrowRules.MaximumPushDistance);
                body.ApplyResolvedImpact(delta.normalized * speed);
            }
            var can = round.Lata;
            var caster = round.PlayerAt(_throwerSlot);
            if (can == null || caster == null || caster.IsDefender) return;
            var canPoint = can.transform.position + Vector3.up * .2f;
            if ((canPoint - centre).sqrMagnitude <= radius * radius
                && PressureLineClear(centre, canPoint, null, can)) can.HostKnockDown(_throwerSlot);
        }

        private bool PressureLineClear(Vector3 from, Vector3 to, CharacterMotor target, Lata can)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < .0001f) return true;
            int count = Physics.RaycastNonAlloc(from, delta.normalized, _pressureHits,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == _pressureHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var collider = _pressureHits[i].collider;
                if (collider.GetComponentInParent<Slipper>() == this) continue;
                if (target != null && collider.GetComponentInParent<CharacterMotor>() == target) continue;
                if (can != null && collider.GetComponentInParent<Lata>() == can) continue;
                return false;
            }
            return true;
        }
    }
}
