using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Reduces particle density without removing ability silhouettes, zones or contact cues.</summary>
    public sealed class ComfortParticles : MonoBehaviour
    {
        private ParticleSystem _particles;
        private ParticleSystem.Burst[] _bursts;
        private float _rate, _distance;
        private bool _reduced;
        private float _fraction;
        public static void Configure(ParticleSystem particles)
        {
            var comfort = particles.gameObject.AddComponent<ComfortParticles>();
            comfort._particles = particles;
            var emission = particles.emission;
            comfort._rate = emission.rateOverTimeMultiplier; comfort._distance = emission.rateOverDistanceMultiplier;
            comfort._bursts = new ParticleSystem.Burst[emission.burstCount]; emission.GetBursts(comfort._bursts);
            // Looping auras are status cues. Keep more of their characteristic motion.
            comfort._fraction = particles.main.loop ? .6f : .35f;
            comfort.Apply(SettingsStore.Current.ReducedEffects);
        }
        private void Update()
        {
            if (_reduced != SettingsStore.Current.ReducedEffects) Apply(SettingsStore.Current.ReducedEffects);
        }
        private void Apply(bool reduced)
        {
            _reduced = reduced;
            float fraction = reduced ? _fraction : 1;
            var emission = _particles.emission;
            emission.rateOverTimeMultiplier = _rate * fraction;
            emission.rateOverDistanceMultiplier = _distance * fraction;
            var bursts = new ParticleSystem.Burst[_bursts.Length];
            for (int i = 0; i < bursts.Length; i++)
            {
                var burst = _bursts[i]; var count = burst.count;
                if (reduced)
                {
                    if (count.mode == ParticleSystemCurveMode.Constant) count.constant = Mathf.Max(1, Mathf.Ceil(count.constant * fraction));
                    else if (count.mode == ParticleSystemCurveMode.TwoConstants)
                    { count.constantMin = Mathf.Max(1, Mathf.Ceil(count.constantMin * fraction)); count.constantMax = Mathf.Max(count.constantMin, Mathf.Ceil(count.constantMax * fraction)); }
                    else count.curveMultiplier *= fraction;
                    burst.count = count;
                }
                bursts[i] = burst;
            }
            emission.SetBursts(bursts);
        }
    }
}
