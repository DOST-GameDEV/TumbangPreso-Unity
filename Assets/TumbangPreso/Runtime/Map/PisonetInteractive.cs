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
            // ⚠️⚠️ THIS PLAYED `score_award` AND THE BOOTH AWARDS NOTHING. No `AddScore`, no
            // `Hud.ReportStyle`, nothing: it is scenery. Two independent wrongs came out of that
            // one cue name and both are player-facing.
            //
            // ⚠️⚠️ FIRST, `score_award` MEANS "YOUR SCORE JUST CHANGED". It is exactly what
            // `Hud.OnScored` plays for a knockdown, a tag and a penalty, so a piece of street
            // furniture was telling the player they had earned something, in a game whose whole
            // hype layer exists to say precisely that.
            //
            // ⚠️⚠️ SECOND, IT IS IN `AudioCues.DuckTriggers`, SO BRUSHING PAST DUCKED THE MUSIC.
            // `AudioDirector.DuckIfAnnouncement` is hooked in the PLAY path by design, so that
            // the countdown and the round end do not each have to remember the bed exists; the
            // cost of that design is that anything borrowing an announcement's cue name inherits
            // its duck silently. `BuildPisonetRow` builds THREE terminals on Ilalim ng Tulay and
            // this re-arms every 3.0 s, so a scrap by the shopfronts could push the OST down over
            // and over for as long as it lasted.
            //
            // ⚠️ `ui_click` IS NOT A DUCK TRIGGER AND CLAIMS NOTHING ABOUT THE SCOREBOARD, which
            // is the whole of the argument for it. The pitch window is unchanged: 1.25 to 1.45 is
            // already what was turning this into a coin blip. Whether a click is the RIGHT coin
            // sound is 🧑's ear per `CLAUDE.md` § 6, and it is in `Attention.md` § 18.
            GameServices.Audio?.PlayAtVaried("ui_click", transform.position, 1.25f, 1.45f, 0.8f);

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
