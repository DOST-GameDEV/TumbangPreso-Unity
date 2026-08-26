using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The LRT-2 consist crossing the guideway over Ilalim ng Tulay, and the map's metronome.
    ///
    /// ⚠️⚠️ THIS IS A MECHANIC, NOT A BACKDROP ANIMATION. It shipped as a model sliding along Z
    /// with one whoosh, which is a screensaver: nothing on the street changed while it passed
    /// and no decision was different for having seen it. A map's one recurring event is the
    /// cheapest depth there is, because every player learns its period inside a single round
    /// and can then plan against it.
    ///
    /// What it does now, in three phases:
    ///
    ///   WARNING  (3.0 s)  the toast, the rail hum, the shadow sweeping in from the south
    ///   OVERHEAD (2.7 s)  the pass itself
    ///   idle     (rest)   back to `Interval`
    ///
    /// ⚠️⚠️ AND THE TWO MODES ANSWER IT DIFFERENTLY. See `OverheadPassWindow`: Hero Strike gets
    /// double cooldown rate while the consist is over the street, Classic gets Street Hype and
    /// the spectacle. `docs/VISION.md` § 1.1 is why, and it is not negotiable: Classic does not
    /// receive powers, from a hero kit or from a map.
    /// </summary>
    public sealed class LrtTrainFlyby : MonoBehaviour
    {
        [Header("Train Movement Settings")]
        [Tooltip("Seconds between train passes.")]
        public float Interval = 24.0f;

        [Tooltip("Initial delay before the first train pass.")]
        public float InitialDelay = 5.0f;

        [Tooltip("Speed of the train crossing the viaduct (m/s).")]
        public float Speed = 18.0f;

        [Tooltip("Start Z position of the train.")]
        public float StartZ = -48.0f;

        [Tooltip("End Z position where the train disappears.")]
        public float EndZ = 48.0f;

        [Tooltip("Track X offset. Measured from the dual-track guideway centre.")]
        public float TrackX = -2.35f;

        [Tooltip("Train root Y. Unity normalises the imported prefab root to its wheel underside.")]
        public float TrackY = 9.190f;

        /// <summary>
        /// How far down the street the consist counts as "overhead".
        ///
        /// ⚠️ MEASURED FROM THE PLAY AREA, NOT PICKED. The walls stand at z = +/-16.5 and the
        /// chalk box ends at +/-7.0. A window keyed to the train origin lasted only 1.83 s at
        /// 18 m/s and closed while half the consist was still above the street. This half-range
        /// is wall half-length 16.5 plus consist half-length 7.8. The full window is therefore
        /// `(33.0 + 15.6) / 18 = 2.70 s`, from nose entering to tail leaving.
        /// </summary>
        public float OverheadHalfZ = 24.3f;

        /// <summary>Seconds of warning before the consist reaches the overhead window.</summary>
        public float WarningLead = 3.0f;

        private float _timer;
        private bool _isRunning;
        private float _currentZ;
        private bool _whooshPlayed;
        private bool _warned;
        private bool _windowOpen;
        private bool _hypeAwarded;

        private void Start()
        {
            _timer = Interval - InitialDelay;
            _isRunning = false;
            transform.position = new Vector3(TrackX, TrackY, StartZ);
            OverheadPassWindow.Clear();
        }

        // ⚠️ THE WINDOW IS A STATIC AND THIS IS THE ONLY THING THAT WRITES IT. Leaving a 2x
        // cooldown rate behind on the way out would follow the player into the next match on a
        // different map, where nothing would ever put it back.
        private void OnDisable() => OverheadPassWindow.Clear();
        private void OnDestroy() => OverheadPassWindow.Clear();

        private void Update()
        {
            if (!_isRunning)
            {
                _timer += Time.deltaTime;
                if (_timer < Interval) return;

                _timer = 0.0f;
                _isRunning = true;
                _currentZ = StartZ;
                _whooshPlayed = false;
                _warned = false;
                _windowOpen = false;
                _hypeAwarded = false;
                return;
            }

            _currentZ += Speed * Time.deltaTime;
            transform.position = new Vector3(TrackX, TrackY, _currentZ);

            float warnAt = -OverheadHalfZ - Speed * WarningLead;

            if (!_warned && _currentZ >= warnAt)
            {
                _warned = true;
                OverheadPassWindow.SetWarning(true);
                Announce();
            }

            bool overhead = _currentZ >= -OverheadHalfZ && _currentZ <= OverheadHalfZ;

            if (overhead != _windowOpen)
            {
                _windowOpen = overhead;
                OverheadPassWindow.SetOverhead(overhead);

                if (overhead)
                {
                    OverheadPassWindow.SetWarning(false);
                    OnWindowOpened();
                }
            }

            if (!_whooshPlayed && _currentZ >= -18.0f)
            {
                _whooshPlayed = true;
                GameServices.Audio?.PlayAtVaried("sfx_fire_whoosh", transform.position, 0.85f, 1.05f, 0.85f);
                ImpactBurst.SpawnAt(new Vector3(TrackX, TrackY - 0.5f, _currentZ));
            }

            DriveRumble();

            if (_currentZ < EndZ) return;

            _isRunning = false;
            _windowOpen = false;
            OverheadPassWindow.SetOverhead(false);
            OverheadPassWindow.SetWarning(false);
            StopRumble();
            transform.position = new Vector3(TrackX, TrackY, StartZ);
        }

        // ------------------------------------------------------------------ § THE PASS
        //
        // ⚠️⚠️ THE CONSIST NOW CARRIES ITS OWN MOVING SOURCE, AND BEFORE THIS THE SOUND NEVER
        // MOVED AT ALL. 🧑 2026-08-26: *"pls make the sfx of the train passing by better ... make
        // it feel like its getting farther and add sound or movement to screen to make it
        // realistic? bcz usually when it passes by u feel the shaking"*.
        //
        // ⚠️ WHY THE OLD ONE COULD NOT DO IT, WHICH IS NOT THAT THE CLIP WAS WRONG. Both cues
        // went through `AudioDirector.PlayAtVaried`, which parks a POOLED VOICE AT A FIXED
        // POSITION and plays it there. That is the right call for an impact, which happens at a
        // point and is over. The train travels from z = -48 to z = +48 while its sound is
        // playing: 96 m in 5.3 s at 18 m/s. Fired once at the position the nose happened to hold
        // at that instant, the sound stayed at that spot for the whole pass, so it faded by the
        // listener WALKING, never by the train leaving. There is no amount of better clip that
        // fixes a stationary emitter.
        //
        // ⚠️ SO: A LOOPING SOURCE PARENTED TO THE CONSIST. Unity then does the approach and the
        // recede for free off the transform, and `dopplerLevel` does the pitch shift on the way
        // past, which is the half of "getting farther" that a volume ramp alone cannot fake.
        //
        // ⚠️⚠️ AND IT PLAYS `sfx_lrt_rumble`, NOT `sfx_lrt_pass`, WHICH IS NOT A DETAIL.
        // The first version of this looped `sfx_lrt_pass` and that was wrong in a way no amount
        // of falloff tuning would have hidden: that cue is a ONE-SHOT, 2.70 s long, beginning and
        // ending on a sample value of ZERO because it was authored with a fade in and a fade out.
        // The pass lasts 5.33 s, so the loop dropped the train to silence at 2.70 s and swelled
        // it back from nothing while it was directly overhead. `sfx_lrt_rumble` is a 2.0 s bed
        // with no envelope at all, filtered circularly so its ends match, and every tonal
        // component completes a whole number of cycles inside the loop. See the synth in
        // `tools/generate_ability_audio.py` for the construction.
        //
        // ⚠️ `sfx_lrt_pass` KEEPS ITS OWN JOB, which is the distant announcement in `Announce`.
        // It is a good one-shot; it was only ever a bad loop.
        //
        // ⚠️ LINEAR ROLLOFF, NOT LOGARITHMIC, AND IT IS MEASURED FROM THE MAP. The guideway sits
        // 9.19 m up and the play area is 33 m of street; logarithmic falloff drops most of its
        // range inside the first few metres, so the consist would be at full volume across the
        // entire arena and then vanish. Linear from 12 to 70 spans the map: audible from the far
        // wall, loudest overhead, gone by the time the tail clears the boundary traffic.
        private const float RumbleMinDistance = 12.0f;

        private const float RumbleMaxDistance = 70.0f;

        /// <summary>
        /// How hard the street shakes directly under the consist.
        ///
        /// ⚠️ 0.30, AGAINST `CameraRig.Shake`'s 0.35 DEFAULT AND THE 0.45x AN IMPACT GETS. A
        /// train passing overhead is a rumble, not a hit: it lasts the whole 2.7 s window, and
        /// anything near impact strength for that long is unplayable rather than atmospheric.
        /// It is also scaled by distance below, so this is the value only for a player standing
        /// directly beneath the guideway.
        /// </summary>
        private const float ShakePeak = 0.30f;

        private AudioSource _rumble;
        private bool _rumbleReady;

        /// <summary>
        /// ⚠️ BUILT ON FIRST USE, NOT IN `Start`. `GameServices.Audio` is installed by the match
        /// bootstrap and this component lives on the map, so a `Start`-time lookup races it and
        /// silently leaves the train mute for the whole session. Asking on the first pass is
        /// 24 s later and cannot lose that race.
        /// </summary>
        private void EnsureRumble()
        {
            if (_rumbleReady) return;
            _rumbleReady = true;

            if (GameServices.Audio == null) return;

            // ⚠️ CALLED ON `GameServices.Audio` RATHER THAN ON A LOCAL, DELIBERATELY.
            // `AudioCueCheck.CallSitePattern` is anchored on the literal `Audio.` receiver, so a
            // cue asked for through a local named anything else is a call site the check cannot
            // see, and it would then report `sfx_lrt_pass` as a file nothing plays.
            if (!GameServices.Audio.TryGetClip("sfx_lrt_rumble", out var clip, out float mix)) return;

            var go = new GameObject("LrtRumble");
            go.transform.SetParent(transform, false);

            _rumble = go.AddComponent<AudioSource>();
            _rumble.clip = clip;
            _rumble.loop = true;
            _rumble.playOnAwake = false;
            _rumble.spatialBlend = 1.0f;
            _rumble.rolloffMode = AudioRolloffMode.Linear;
            _rumble.minDistance = RumbleMinDistance;
            _rumble.maxDistance = RumbleMaxDistance;

            // ⚠️ ABOVE 1. The consist is the only thing in the game moving fast enough for
            // doppler to be audible at all, and at 18 m/s the true shift is about 5 per cent,
            // which nobody hears. This is the one place exaggerating it is honest: the effect
            // being sold is "it went past me", not a physics reading.
            _rumble.dopplerLevel = 2.2f;

            _rumbleMix = mix;
        }

        private float _rumbleMix = 1.0f;

        /// <summary>
        /// The pass, every frame it is running: the rumble's level and the shake under it.
        ///
        /// ⚠️⚠️ BOTH ARE DRIVEN OFF THE SAME DISTANCE, so the loudest moment and the hardest
        /// shake are the same moment by construction rather than by two timers agreeing. That is
        /// the whole reason it reads as one heavy object rather than as a sound plus an effect.
        /// </summary>
        private void DriveRumble()
        {
            EnsureRumble();

            var listener = UnityEngine.Camera.main;
            if (listener == null) return;

            float distance = Vector3.Distance(listener.transform.position, transform.position);

            if (_rumble != null)
            {
                if (!_rumble.isPlaying) _rumble.Play();

                // The player's slider is read every frame rather than cached, because it can be
                // moved in the pause panel while a train is mid-pass.
                float slider = GameServices.Audio != null ? GameServices.Audio.SfxVolume : 1.0f;
                _rumble.volume = _rumbleMix * slider;
            }

            // ⚠️ THE SHAKE IS RE-ARMED EVERY FRAME RATHER THAN FIRED ONCE. `CameraRig.Shake`
            // takes a `Max()` against what is already running and drains on a timer, so a single
            // call at the start of a 2.7 s window would have decayed to nothing within a fifth
            // of a second. Re-arming with a SHORT duration each frame is what makes it a
            // sustained rumble whose strength tracks the consist instead of a one-off jolt.
            if (distance > RumbleMaxDistance) return;

            float nearness = 1.0f - Mathf.Clamp01((distance - RumbleMinDistance)
                                                  / (RumbleMaxDistance - RumbleMinDistance));

            // Squared, so the shake is genuinely local to the pass. Linear left the whole street
            // trembling for the entire approach, which is the "screensaver" failure this file's
            // header already argues against, moved into the camera.
            float strength = ShakePeak * nearness * nearness;
            if (strength < 0.01f) return;

            var rig = listener.GetComponent<CameraSystem.CameraRig>();
            rig?.Shake(strength, 0.12f);
        }

        /// <summary>
        /// ⚠️ THE RUMBLE STOPS WITH THE CONSIST. It is a looping source on an object that gets
        /// teleported back to `StartZ` and left there for the rest of the interval; leaving it
        /// playing would put a train under the south wall for 24 s.
        /// </summary>
        private void StopRumble()
        {
            if (_rumble != null && _rumble.isPlaying) _rumble.Stop();
        }

        /// <summary>
        /// ⚠️ THE WARNING IS A TOAST, NOT A COMIC POPUP. A popup is placed in the world and
        /// competes for the four-slot callout budget that `ComicPopup` evicts against; the
        /// train is a whole-map event with no position on the street, and it fires every 24 s.
        /// Spending a callout slot on it every cycle would push out the score and cast callouts
        /// that the budget exists to protect.
        /// </summary>
        private void Announce()
        {
            // ⚠️⚠️ THIS READ `"ui_move"`, WHICH IS NOT A CUE AND NEVER WAS. There is no
            // `ui_move.wav` anywhere in the project and no entry for it in `AudioCues.Live`, so
            // every pass logged `[Audio] no cue registered for 'ui_move'` and played nothing.
            // The map's signature 24 s event, the thing a player is meant to learn the period
            // of, had no sound for its whole life. `AudioCueCheck` could not see it either,
            // because it compared DECLARED cues against files and never call sites against
            // declarations; it does now.
            // ⚠️ THE DISTANT WARNING, AND IT IS A ONE-SHOT ON PURPOSE. This fires once, three
            // seconds out, from where the consist is at that moment, and it is meant to be heard
            // from the far end of the street: it is the "parating na" and nothing else. The
            // travelling part of the sound is `sfx_lrt_rumble` on the moving source, which is a
            // different cue doing a different job, so the two no longer double each other the
            // way one clip played twice would have.
            GameServices.Audio?.PlayAtVaried("sfx_lrt_pass", transform.position,
                                             0.94f, 1.06f, 0.62f);

            if (Hud.Instance == null) return;

            Hud.Instance.ShowToast(SceneFlow.SelectedMode == GameMode.HeroStrike
                ? "PARATING NA  ·  OVERCLOCK WINDOW"
                : "PARATING NA  ·  ILALIM NG TULAY",
                WarningLead * 0.9f);
        }

        private void OnWindowOpened()
        {
            if (SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                if (Hud.Instance != null)
                    Hud.Instance.ShowToast("OVERCLOCK  ·  COOLDOWNS x2", 1.6f);

                return;
            }

            // Classic. Cosmetic only, and only for the local player, which is what ReportStyle
            // already enforces.
            if (_hypeAwarded) return;

            _hypeAwarded = true;
            var round = GameServices.Round;
            if (round == null) return;

            foreach (var seat in round.Players)
            {
                if (seat == null) continue;
                Hud.ReportStyle(seat.PlayerSlot, 4.0f, "ILALIM NG TULAY");
            }
        }
    }
}
