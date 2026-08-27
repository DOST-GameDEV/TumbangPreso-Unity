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
    ///   WARNING  (3.0 s)  the toast and the shadow sweeping in from the south
    ///   OVERHEAD (2.7 s)  the pass itself
    ///   idle     (rest)   back to `Interval`
    ///
    /// ⚠️ THE SOUND IS ONE FIELD RECORDING ON A MOVING SOURCE, and it starts when the consist
    /// spawns rather than at the warning, because the recording carries its own approach. It
    /// used to be three synthesised cues. See § THE PASS.
    ///
    /// ⚠️⚠️ AND THE TWO MODES ANSWER IT DIFFERENTLY. See `OverheadPassWindow`: Hero Strike gets
    /// double cooldown rate while the consist is over the street, Classic gets Street Hype and
    /// the spectacle. `docs/VISION.md` § 1.1 is why, and it is not negotiable: Classic does not
    /// receive powers, from a hero kit or from a map.
    /// </summary>
    public sealed class LrtTrainFlyby : MonoBehaviour
    {
        [Header("Train Movement Settings")]
        /// <summary>
        /// ⚠️⚠️ 300, AND IT HAS BEEN RAISED THREE TIMES ON THE SAME COMPLAINT. It shipped at 24,
        /// went to 78 and then 150 on *"i want train to play rarely / like maybe when they open
        /// the game"*, and reaches 300 on 🧑 2026-08-27: *"make it play very rarely"*. At 24 s a
        /// 90 s round carried three or four passes, so the map's signature event was arriving
        /// every twenty seconds and had stopped being an event. At 300 a Classic match (4 x 90 s)
        /// sees the opener and about one more; a Hero Strike match (8 x 90 s) sees the opener and
        /// two.
        ///
        /// ⚠️ IT IS STILL LEARNABLE, WHICH IS THE PROPERTY THIS FILE'S HEADER PROTECTS: "every
        /// player learns its period inside a single round". `InitialDelay` below is what keeps
        /// that true at any interval: the FIRST pass is what teaches the map has one.
        ///
        /// ⚠️ AND IT IS A BALANCE CHANGE, NOT ONLY A MOOD ONE. `OverheadPassWindow` gives Hero
        /// Strike double cooldown rate while the consist is overhead, so this cuts the number of
        /// overclock windows in a whole match to two or three. `docs/TODO.md` section 5 already
        /// records that the overclock window has never been measured against a match; this makes
        /// measuring it more urgent rather than less.
        /// </summary>
        [Tooltip("Seconds between train passes.")]
        public float Interval = 300.0f;

        /// <summary>
        /// ⚠️ THE FIRST PASS COMES EARLY ON PURPOSE, which is the other half of *"like maybe
        /// when they open the game"*. A rare event that a player might never see in their first
        /// match is not a rare event, it is a missing one. Opening with a pass teaches the map
        /// has one; `Interval` then makes it something you wait for rather than something you
        /// tune out.
        /// </summary>
        [Tooltip("Initial delay before the first train pass.")]
        public float InitialDelay = 20.0f;

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

            // ⚠️⚠️ THE `sfx_fire_whoosh` THAT USED TO FIRE HERE IS GONE, AND IT WAS THE THIRD
            // TRAIN SOUND. A fire cue, borrowed, played from a fixed point as the consist crossed
            // z = -18, on top of a distant warning one-shot and a looping bed. Three sounds for
            // one object, two of them synthesised and one of them about fire, is most of why 🧑
            // reported this repeatedly and finally said *"i keep reporting its broken and i give
            // up on it"*. The recording is the train; the burst stays because it is a picture.
            if (!_whooshPlayed && _currentZ >= -18.0f)
            {
                _whooshPlayed = true;
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
        // ⚠️⚠️ IT PLAYS ONE CUE, ONCE, WITHOUT LOOPING, AND THAT IS THE 2026-08-27 CHANGE.
        // 🧑: *"i keep reporting its broken and i give up on it ... replace train passing by
        // sound and train sound as a whole with this"*, with a 10.55 s field recording. The train
        // had THREE synthesised sounds: a distant one-shot warning (`sfx_lrt_pass`), a 2.0 s
        // seamless bed looped on this source (`sfx_lrt_rumble`), and a borrowed `sfx_fire_whoosh`
        // fired from a fixed point at z = -18. Two of the three are deleted and the third is
        // replaced.
        //
        // ⚠️⚠️ THE CLIP IS TIME-ALIGNED TO THE PASS BY ARITHMETIC, NOT BY EAR, AND IT NEEDS NO
        // OFFSET. Measured on the recording, its loudest quarter-second is at **2.70 s** (RMS
        // 0.234 against 0.10 for the carriage tail). The consist spawns at z = -48 and reaches
        // the street at 18 m/s, so it is overhead at **48 / 18 = 2.67 s** after the source
        // starts. Starting the clip when the run starts therefore puts the recording's own pass
        // within 0.03 s of the real one. If `Speed`, `StartZ` or the clip ever change, this is
        // the sum to redo.
        //
        // ⚠️ AND THE LOOP IS OFF. A 10.55 s clip outlasts the 5.33 s traverse, so there is
        // nothing to loop; the previous note here records what looping an enveloped one-shot did
        // (silence at 2.70 s, then a swell from nothing while the train was directly overhead)
        // and turning the loop off is what makes that unreachable rather than merely unlikely.
        //
        // ⚠️ LINEAR ROLLOFF, NOT LOGARITHMIC, AND IT IS MEASURED FROM THE MAP. The guideway sits
        // 9.19 m up and the play area is 33 m of street; logarithmic falloff drops most of its
        // range inside the first few metres, so the consist would be at full volume across the
        // entire arena and then vanish. Linear from 12 to 70 spans the map: audible from the far
        // wall, loudest overhead, gone by the time the tail clears the boundary traffic.
        private const float RumbleMinDistance = 12.0f;

        /// ⚠️⚠️ 44, DOWN FROM 70, BECAUSE 70 MADE IT AUDIBLE FROM THE MOMENT IT SPAWNED.
        /// The consist starts at z = -48 and the arena is centred on the origin, so at 70 m of
        /// range the bed was already playing before the warning toast fired and kept playing
        /// well after the tail had left. Combined with a bed that had no mix trim at all, that is
        /// the *"loud wind soudn that plays randomly"* off the played build: not random, just
        /// audible for the whole 5.3 s traverse at full level. At 44 the sound arrives with the
        /// warning and leaves with the train.
        private const float RumbleMaxDistance = 44.0f;

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
        private bool _rumbleStarted;

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
            if (!GameServices.Audio.TryGetClip("sfx_lrt_pass", out var clip, out float mix)) return;

            var go = new GameObject("LrtRumble");
            go.transform.SetParent(transform, false);

            _rumble = go.AddComponent<AudioSource>();
            _rumble.clip = clip;
            _rumble.loop = false;
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
                // ⚠️ ONCE PER PASS, NOT "WHENEVER IT IS NOT PLAYING". With the old looping bed
                // those were the same sentence. With a one-shot they are not: a clip that ended
                // would be restarted on the next frame, which is the enveloped-loop fault the
                // section header records, rebuilt out of a restart instead of a loop flag.
                if (!_rumbleStarted)
                {
                    _rumbleStarted = true;
                    _rumble.time = 0.0f;
                    _rumble.Play();
                }

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
            _rumbleStarted = false;
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
            // ⚠️⚠️ THE DISTANT ONE-SHOT THAT USED TO FIRE HERE IS DELETED, AND THE ANNOUNCEMENT
            // IS NOW THE RECORDING ITSELF. The old pair was a synthesised warning played from a
            // fixed point plus a synthesised bed on the moving source; the note above §THE PASS
            // argued they were "a different cue doing a different job". With ONE real recording
            // that argument inverts: playing the same 10.55 s of audio twice, a few tenths apart,
            // is a flam rather than an announcement. The recording has its own approach, and the
            // consist is already 54 m out and audible when this fires.
            //
            // ⚠️ THE TOAST STAYS AND IS NOW THE ONLY THING THIS METHOD DOES. It is the readable
            // half of the warning, and the Hero Strike overclock window hangs off it.
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
                // ⚠️ NOT RELAYED. The consist is simulated independently on every peer off the
                // same `Interval`, so each screen reaches this line itself.
                Hud.ReportStyle(seat.PlayerSlot, 4.0f, "ILALIM NG TULAY", relay: false);
            }
        }
    }
}
