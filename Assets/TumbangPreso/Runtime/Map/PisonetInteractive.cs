using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Interactive Pisonet arcade booth that plays coin clinks and displays funny retro gaming callouts.
    /// </summary>
    public sealed class PisonetInteractive : MonoBehaviour
    {
        public Light ScreenLight;
        private float _nextTime;

        private static readonly string[] Callouts =
        {
            "+5 COIN!",
            "10 MINS ADDED!",
            "GG WP!",
            "INSERT COIN",
            "CROSSFIRE!",
            "DOTA 2 TIME!"
        };

        private void OnCollisionEnter(Collision collision) => TriggerArcade(collision.gameObject);
        private void OnTriggerEnter(Collider other) => TriggerArcade(other.gameObject);

        private void TriggerArcade(GameObject go)
        {
            if (Time.time < _nextTime) return;
            var motor = go.GetComponentInParent<CharacterMotor>();
            if (motor == null) return;

            _nextTime = Time.time + 3.0f;

            string callout = Callouts[Random.Range(0, Callouts.Length)];
            ComicPopup.Spawn(transform.position + Vector3.up * 1.5f, callout, UI.UiTheme.Highlight, 1.2f);
            GameServices.Audio?.PlayAtVaried("score_award", transform.position, 1.25f, 1.45f, 0.8f);

            if (ScreenLight != null)
            {
                ScreenLight.color = Color.white;
                ScreenLight.intensity = 2.0f;
                Invoke(nameof(ResetScreen), 0.35f);
            }
        }

        private void ResetScreen()
        {
            if (ScreenLight != null)
            {
                ScreenLight.color = new Color(0.0f, 0.9f, 1.0f);
                ScreenLight.intensity = 1.2f;
            }
        }
    }
}
