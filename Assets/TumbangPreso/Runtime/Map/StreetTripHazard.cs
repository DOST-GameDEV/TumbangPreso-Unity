using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Interactive environmental hazard (loose extension cords, oil/pares broth slicks,
    /// discarded PC component boxes, road trenches) that trips characters who run across them.
    /// When tripped, the character falls flat on the ground for 2-3 seconds before standing up.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StreetTripHazard : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [Tooltip("Seconds the tripped character stays down on the ground / getting up.")]
        public float TripDuration = 2.5f;

        [Tooltip("Minimum horizontal move speed required to trigger a trip (m/s).")]
        public float MinSpeedToTrip = 1.0f;

        [Tooltip("Cooldown in seconds before the same character can be tripped by this hazard again.")]
        public float Cooldown = 3.5f;

        [Tooltip("Text displayed in the comic popup callout when tripped.")]
        public string PopupText = "TRIPPED!";

        [Tooltip("Color of the impact burst / splash puff.")]
        public Color BurstColor = new Color(0.85f, 0.82f, 0.75f, 1.0f);

        [Tooltip("Radius registered with AI HazardMap so bots attempt to steer around it.")]
        public float HazardRadius = 1.2f;

        private readonly Dictionary<CharacterMotor, float> _cooldowns = new Dictionary<CharacterMotor, float>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Register with HazardMap so bots navigate around the obstacle
            HazardVolume.Attach(gameObject, HazardRadius, -1);
        }

        private void OnTriggerEnter(Collider other) => TryTrip(other);
        private void OnTriggerStay(Collider other) => TryTrip(other);

        private void TryTrip(Collider other)
        {
            if (other == null) return;

            var motor = other.GetComponentInParent<CharacterMotor>();
            if (motor == null || motor.IsTripped) return;

            // Must have some horizontal movement speed or be sprinting/dashing
            Vector3 flatVel = motor.Velocity;
            flatVel.y = 0.0f;
            if (flatVel.magnitude < MinSpeedToTrip) return;

            if (_cooldowns.TryGetValue(motor, out float nextAllowed) && Time.time < nextAllowed)
                return;

            _cooldowns[motor] = Time.time + Cooldown;

            // Apply trip and floor knockdown
            motor.ApplyTrip(TripDuration);

            // Audio cue
            GameServices.Audio?.PlayAtVaried("shove", motor.transform.position, 0.85f, 1.15f, 1.0f);

            // Comic popup callout
            ComicPopup.Spawn(motor.transform.position + Vector3.up * 1.0f, PopupText, UI.UiTheme.Danger, 1.2f);

            // Dust / water splash burst
            ImpactBurst.SpawnAt(motor.transform.position);
        }
    }
}
