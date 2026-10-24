using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class Slipper
    {
        /// <summary>
        /// Sean's MOLTEN THROW striking something (the `FireExplosive` affinity, kept on the wire as it was). It pushes nobody:
        /// the throw's payoff is the can (`Lata.HostMoltenHit`, from the can branch of the flight step). What every peer sees
        /// here is the coat splashing off the slipper as it hits.
        /// </summary>
        private void ResolveEmpoweredThrowImpact()
        {
            var centre = transform.position;
            if (Visual.SeanThrowFire.Painted) { Visual.SeanThrowFire.Burst(this, centre); NetCue.Play("sfx_slipper_burst", centre); }
            else Abilities.HeroHazards.CreateExplosionVisual(centre, .9f, "", Abilities.HeroHazards.ExplosionStyle.Ignition);
        }
    }
}
