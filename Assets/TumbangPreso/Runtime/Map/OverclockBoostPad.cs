using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// PC Express Overclock Turbo Pad: Gives characters a timed 1.5x speed surge when stepped on
    /// (`CharacterMotor.BeginSpeedBoost`). Each peer boosts only the motors it simulates; the
    /// motor refuses the rest, so nothing is on the wire.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OverclockBoostPad : MonoBehaviour
    {
        [Header("Boost Settings")]
        public float SpeedMultiplier = 1.5f;
        public float BoostDuration = 2.2f;
        public float Cooldown = 4.0f;

        [Header("Visuals")]
        public Light PadLight;

        private readonly Dictionary<CharacterMotor, float> _cooldowns = new Dictionary<CharacterMotor, float>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (PadLight != null)
            {
                // RGB cycle hue over time
                float hue = Mathf.Repeat(Time.time * 0.4f, 1.0f);
                PadLight.color = Color.HSVToRGB(hue, 0.9f, 1.0f);
            }
        }

        private void OnTriggerEnter(Collider other) => TryBoost(other);
        private void OnTriggerStay(Collider other) => TryBoost(other);

        private void TryBoost(Collider other)
        {
            if (other == null) return;

            var motor = other.GetComponentInParent<CharacterMotor>();
            if (motor == null || motor.IsStunned || motor.IsTripped) return;

            if (_cooldowns.TryGetValue(motor, out float nextAllowed) && Time.time < nextAllowed)
                return;

            // Refused on a peer that does not simulate this body: no cooldown and no cue there.
            if (!motor.BeginSpeedBoost(SpeedMultiplier, BoostDuration)) return;

            _cooldowns[motor] = Time.time + Cooldown;

            // Audio & visual cues
            GameServices.Audio?.PlayAtVaried("sfx_super_ready", motor.transform.position, 1.1f, 1.3f, 0.9f);
            ComicPopup.Spawn(motor.transform.position + Vector3.up * 1.0f, "OVERCLOCKED!", UI.UiTheme.Highlight, 1.2f);
            ImpactBurst.SpawnAt(motor.transform.position);
        }
    }
}
