using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Interactive Pares food cart with bubbling soup aroma and humorous Manila street food banter.
    /// </summary>
    public sealed class StreetParesInteractive : MonoBehaviour
    {
        private float _nextTime;

        private static readonly string[] ParesQuotes =
        {
            "MAINIT NA PARES!",
            "UNLI RICE BOSS!",
            "EXTRA CHILI!",
            "SARAP NG CALDO!",
            "SOLID DITO IDOL!"
        };

        private void OnCollisionEnter(Collision collision) => TriggerPares(collision.gameObject);
        private void OnTriggerEnter(Collider other) => TriggerPares(other.gameObject);

        private void TriggerPares(GameObject go)
        {
            if (Time.time < _nextTime) return;
            var motor = go.GetComponentInParent<CharacterMotor>();
            if (motor == null) return;

            _nextTime = Time.time + 3.5f;

            string quote = ParesQuotes[Random.Range(0, ParesQuotes.Length)];
            ComicPopup.Spawn(transform.position + Vector3.up * 2.0f, quote, UI.UiTheme.Highlight, 1.2f);
            GameServices.Audio?.PlayAtVaried("slipper_bounce", transform.position, 1.1f, 1.3f, 0.9f);
            ImpactBurst.SpawnAt(transform.position + Vector3.up * 1.2f);
        }
    }
}
