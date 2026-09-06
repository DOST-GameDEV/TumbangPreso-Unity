using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// One-shot positional audio with per-cue mix levels.
    ///
    /// ⚠️⚠️ A REGISTERED CUE WITH NO CALLER IS THE FAILURE THIS TYPE IS SHAPED AROUND.
    /// In the Godot build `slipper_land` was registered with its own mix level and had NEVER
    /// had a caller: a throw that hit a body played one sound, a throw that hit the can
    /// played another, and a throw that simply MISSED, which was 38 of 71 flights in the
    /// baseline and by far the most common outcome, landed in total silence. The one shot
    /// whose result the attacker most needs to hear was the one the game said nothing about.
    ///
    /// So <see cref="Play"/> logs an unregistered cue rather than failing quietly, and
    /// <see cref="WarnUnplayedCues"/> reports registered cues that were never fired in a
    /// session. A cue nobody plays is either a missing call or a dead registration, and both
    /// are worth finding from a probe rather than from a player.
    /// </summary>
    /// ⚠️ THE EXECUTION ORDER IS FOR THE EARS AND NOTHING ELSE. See `LateUpdate`: the listener
    /// copies the pose `CameraRig.LateUpdate` has just written, and two `LateUpdate`s with no
    /// declared order run in whichever order Unity felt like.
    [DefaultExecutionOrder(1000)]
    public sealed class AudioDirector : MonoBehaviour
    {
        private struct Cue
        {
            public AudioClip Clip;
            public float Volume;
            public bool EverPlayed;
        }

        private readonly Dictionary<string, Cue> _cues = new Dictionary<string, Cue>();

        /// <summary>
        /// ⚠️⚠️ WITHOUT AN AudioListener THE WHOLE GAME IS SILENT, AND NOTHING WARNS YOU.
        /// Unity only puts a listener on the camera a NEW scene is created with; a camera added
        /// from code has none. Every scene here is generated, so every scene had a camera and
        /// no listener, and the built game played not one sound. There is no error, no warning,
        /// and every cue reports as played.
        ///
        /// The services object owns one and it persists, so it cannot be forgotten per scene.
        /// </summary>
        /// ⚠️⚠️ UNCONDITIONAL, NOT "IF NONE EXISTS". It used to check
        /// `FindFirstObjectByType&lt;AudioListener&gt;() == null` first, on the theory that some
        /// other system might already have one. In practice the only other system that ever did
        /// was `BootSting`, which built its own on a `HideAndDontSave` GameObject so it could be
        /// heard before this object existed at all. `FindFirstObjectByType` does not return
        /// objects with that flag, which was measured directly: logging the query at the moment
        /// Unity's own "There are 2 audio listeners" warning was firing showed it finding ZERO
        /// listeners while two real, enabled ones were alive, one on `~BootSting` and one on
        /// `~GameServices`. Both were invisible to the very query meant to prevent duplicates, so
        /// both existed for the entire session and neither was ever disabled. `BootSting` no
        /// longer creates a listener of its own; it calls `GameServices.Ensure()` so this object
        /// exists first, and this is now the ONLY place in the game a listener is ever created.
        /// ⚠️⚠️ AND SINCE 2026-09-06 THE LISTENER IS ON ITS OWN CHILD RATHER THAN ON THIS
        /// OBJECT, WHICH IS FORCED BY THE VOICE POOL AND NOT A TIDINESS PREFERENCE.
        /// `TakeVoice` parents every pooled voice to THIS transform, and a pooled voice is
        /// parked at a world position and left there for the length of an impact. So moving this
        /// object to follow the player would drag every ringing one-shot along with it: a
        /// slipper landing behind you would travel with your ears and never fall behind you at
        /// all. The ears are a sibling of the voices, not their parent.
        private void Awake()
        {
            BuildEars();

            LoadCuesFromResources();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => KeepOneListener();
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE LISTENER SAT AT WORLD ORIGIN FOR THE WHOLE PORT, AND EVERY 3D CUE IN THE
        /// GAME WAS THEREFORE PANNED FROM THE MIDDLE OF THE MAP RATHER THAN FROM THE PLAYER.
        /// `docs/TODO.md` § 150.7 has the measurement. Every pooled voice is `spatialBlend = 1`
        /// with a 2 m to 32 m linear rolloff, and the listener was added to `~GameServices`,
        /// which is created at the origin and never moved, parented or rotated. Two consequences,
        /// and the second is the one that mattered:
        ///
        /// - **Attenuation** ran 1.00 at the centre of the box to 0.74 at a corner
        ///   (`Balance.ConfinementRadius` 7.0, so a corner is 9.9 m out): audible, modest.
        /// - ⚠️⚠️ **Panning was anchored to the WORLD.** A player standing at `(-5, 0, 0)` heard
        ///   a slipper land at `(+5, 0, 0)`, ten metres directly in FRONT of them, panned hard
        ///   RIGHT, because that is world `+X` of the origin. Front and back did not exist at
        ///   all. `docs/VISION.md` § 0 is why that is not cosmetic here: the whole tension of
        ///   this game is the run back in for your tsinelas, and hearing which side the taya is
        ///   closing from is part of that read.
        ///
        /// ⚠️ IT IS STILL EXACTLY ONE LISTENER. `KeepOneListener` is unchanged in intent: the
        /// answer to a stale listener is not a second one per camera.
        /// </summary>
        private void BuildEars()
        {
            var go = new GameObject("Ears");
            go.transform.SetParent(transform, false);

            _ears = go.AddComponent<AudioListener>();
        }

        private AudioListener _ears;

        /// <summary>
        /// The ears ride the camera this machine is actually looking through.
        ///
        /// ⚠️⚠️ `Camera.main` IS THE RIGHT QUESTION HERE AND A `CameraRig` REFERENCE IS NOT,
        /// BECAUSE THE RIG IS ONLY ONE OF THE FOUR THINGS THAT CAN BE THE LOCAL VIEW.
        /// `MatchInstaller` tags the gameplay camera, the spectator camera and the watch camera
        /// `MainCamera` in turn, and `DebugPlayerSwitcher`'s own header records the consequence
        /// from the other side: *"`Camera.main` IS THE SPECTATOR'S OWN OBJECT WHENEVER ONE IS
        /// UP"*. So this one lookup already covers FPP, TPP, the emote orbit (the same rig
        /// swinging out and back), the spectator rig, a possession change, a role change, a seat
        /// change and a scene transition, and it needs no notification from any of them: the tag
        /// moves and the ears follow on the next frame.
        ///
        /// ⚠️ AND IT IS ASKED EVERY FRAME RATHER THAN CACHED. A cached camera is a reference
        /// that outlives the match it belonged to, which is § 149.8 and § 150.1's fault twice
        /// over; `Camera.main` is an engine-side cached lookup invalidated when a tag or an
        /// enabled flag moves, so asking it is cheaper than being wrong about it.
        ///
        /// ⚠️ WITH NO CAMERA THE EARS GO HOME TO THE RIG ROOT, which is the origin, which is
        /// exactly the pre-2026-09-06 behaviour. A headless probe and the dedicated server both
        /// take that branch, so nothing about their measured audio changes.
        ///
        /// ⚠️ `[DefaultExecutionOrder]` PUTS THIS AFTER `CameraRig.LateUpdate`, WHICH IS WHERE
        /// THE CAMERA POSE IS WRITTEN. Without it the two `LateUpdate`s run in an undefined
        /// order and the ears would sometimes carry the previous frame's pose. One frame is
        /// inaudible; a pose that alternates between two frames' worth of head rotation is a
        /// stereo image that jitters, and it would only ever show up as "the audio feels loose".
        /// </summary>
        private void LateUpdate()
        {
            if (_ears == null) return;

            var head = UnityEngine.Camera.main;

            if (head == null)
            {
                _ears.transform.localPosition = Vector3.zero;
                _ears.transform.localRotation = Quaternion.identity;
                return;
            }

            _ears.transform.SetPositionAndRotation(head.transform.position,
                                                   head.transform.rotation);
        }

        /// <summary>
        /// ⚠️⚠️ EXACTLY ONE LISTENER, AND UNITY WILL NOT ENFORCE IT FOR YOU. This object owns a
        /// listener and survives scene changes; a scene that brings its own — an arena camera, a
        /// menu camera, a preview rig — makes two, and Unity's response is a per-frame warning
        /// plus undefined behaviour about which one actually hears. It surfaced as a test
        /// failure rather than as a bug report, which is the only reason it was seen at all.
        ///
        /// ⚠️ THE OTHERS ARE DISABLED, NOT DESTROYED. They belong to scenes this object does not
        /// own, and destroying a component out of somebody else's scene is how a re-import
        /// silently puts it back.
        ///
        /// ⚠️⚠️ THE `listener == mine` GUARD USED TO SHORT-CIRCUIT NOTHING AND IS NOW THE ONLY
        /// THING KEEPING THE GAME AUDIBLE. This note read *"THIS LOOP CANNOT SEE `mine`, AND
        /// THAT IS FINE NOW"*, and that was true while the listener lived on `~GameServices`
        /// itself: the root is `HideAndDontSave` and `FindObjectsByType` never returns those, so
        /// the search could not reach it however it was written. **The ears are an ordinary child
        /// object now** (see `BuildEars`, and the voice-pool reason it had to move), so this
        /// search DOES return them, and without the guard the very first scene load would
        /// disable the game's only listener and everything would go silent with no warning and
        /// no error. Do not "simplify" the check away because the comment above it once said it
        /// did nothing.
        ///
        /// ⚠️ THE EARS ARE HELD BY DIRECT REFERENCE, NOT LOOKED UP. `GetComponent` on this object
        /// answers null now, and a null `mine` would disable every listener in the scene and
        /// re-enable none of them.
        /// </summary>
        private void KeepOneListener()
        {
            var mine = _ears;

            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include,
                                                                      FindObjectsSortMode.None))
            {
                if (listener == mine) continue;
                listener.enabled = false;
            }

            if (mine != null) mine.enabled = true;
        }

        /// <summary>
        /// ⚠️ CUES LOAD THEMSELVES. Nothing in the game called Register, so even with a
        /// listener present every PlayAt would have logged "no cue registered" and played
        /// nothing. Loading from Resources means a new sound file is playable the moment it is
        /// dropped in, which is how the team actually works.
        /// </summary>
        private void LoadCuesFromResources()
        {
            foreach (var cue in Audio.AudioCues.Live)
            {
                string stem = Audio.AudioCues.FileStemFor(cue);
                var clip = Resources.Load<AudioClip>($"Sfx/{stem}");

                if (clip == null) continue;

                _cues[cue] = new Cue
                {
                    Clip = clip,
                    Volume = DbToLinear(Audio.AudioCues.TrimFor(cue)),
                    EverPlayed = false,
                };
            }

            Debug.Log($"[Audio] loaded {_cues.Count} of {Audio.AudioCues.Live.Count} cues.");
        }

        /// <summary>The mix table is in dB; Unity wants linear gain.</summary>
        private static float DbToLinear(float db) => Mathf.Pow(10.0f, db / 20.0f);

        public void Register(string id, AudioClip clip, float volume = 1.0f)
        {
            // ⚠️ A DELIVERY'S EXTENSION LIES, AND THIS HAS COST A SESSION TWICE. Voice
            // arrived as AAC-in-3GP named .wav, and the soundtrack as MP3 named .wav. The
            // engine loads the mislabelled file as null, and a full folder with correct
            // names and correct wiring produces no sound at all, which is indistinguishable
            // from "not recorded yet". Sniff magic bytes at import; never trust the suffix.
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] cue '{id}' registered with a null clip. " +
                                 "If the file exists on disk, check its REAL format: a " +
                                 "mislabelled container loads as null and is silent.");
                return;
            }

            _cues[id] = new Cue { Clip = clip, Volume = volume, EverPlayed = false };
        }

        public void PlayAt(string id, Vector3 position)
            => PlayAtVaried(id, position, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The clip and its authored mix level, for a caller that has to drive its own
        /// <see cref="AudioSource"/> rather than fire a one-shot.
        ///
        /// ⚠️⚠️ THIS EXISTS FOR SOUNDS THAT MOVE, AND THERE IS EXACTLY ONE SO FAR. `PlayAtVaried`
        /// parks a pooled voice at a FIXED position and plays it there; that is right for an
        /// impact, which happens at a point, and wrong for the LRT consist, which travels 96 m
        /// across the map while its sound is playing. A one-shot fired at the train's position
        /// when it entered stayed where it was fired, so the pass never got nearer or further
        /// away. 🧑 2026-08-26: *"make it feel like its getting farther"*.
        ///
        /// ⚠️ THE MIX LEVEL COMES OUT WITH THE CLIP, AND THE CALLER MUST APPLY IT. Returning only
        /// the clip would route a sound around the authored mix and the player's SFX slider,
        /// which is the exact fault the note in `PlayAtVaried` records being fixed. Multiply by
        /// this AND by <see cref="SfxVolume"/>.
        /// </summary>
        public bool TryGetClip(string id, out AudioClip clip, out float mixLevel)
        {
            clip = null;
            mixLevel = 0.0f;

            if (!_cues.TryGetValue(id, out var cue) || cue.Clip == null)
            {
                Debug.LogWarning($"[Audio] no cue registered for '{id}'.");
                return false;
            }

            // ⚠️ IT COUNTS AS PLAYED. `WarnUnplayedCues` exists to catch a cue that is declared
            // and never fired; a cue driven through here is fired, just not by this class, and
            // leaving the flag alone would report the train's own sound as dead every run.
            cue.EverPlayed = true;
            _cues[id] = cue;

            clip = cue.Clip;
            mixLevel = cue.Volume;
            return true;
        }

        /// <summary>The player's SFX slider, for a caller driving its own source.</summary>
        public float SfxVolume => SfxScale();

        /// <summary>
        /// Plays a world cue with a small pitch window. Repeated slippers, footsteps and
        /// impacts otherwise expose that they are the exact same recording within seconds.
        /// The volume multiplier is intentionally clamped: this is expression inside the
        /// authored mix, not a route around its headroom.
        /// </summary>
        public void PlayAtVaried(string id, Vector3 position, float pitchMin = 0.94f,
                                 float pitchMax = 1.06f, float volumeScale = 1.0f)
        {
            if (!_cues.TryGetValue(id, out var cue))
            {
                Debug.LogWarning($"[Audio] no cue registered for '{id}'.");
                return;
            }

            cue.EverPlayed = true;
            _cues[id] = cue;

            // ⚠️⚠️ THE SLIDERS WERE BEING IGNORED ENTIRELY. Every sound played at its cue's
            // mix level regardless of what the player set, so turning SFX down did nothing
            // while the music and the announcer both obeyed. The mix level is the cue's
            // RELATIVE weight; the sliders scale all of them together.
            var voice = TakeVoice();

            voice.transform.position = position;
            voice.clip = cue.Clip;
            voice.pitch = Random.Range(Mathf.Min(pitchMin, pitchMax),
                                       Mathf.Max(pitchMin, pitchMax));
            voice.volume = cue.Volume * SfxScale() * Mathf.Clamp(volumeScale, 0.0f, 1.25f);
            voice.Play();

            DuckIfAnnouncement(id);
        }

        /// <summary>
        /// § THE NON-DIEGETIC ROUTE. A cue that is not IN the world: a score award, the round
        /// end, a menu click, the hitmarker.
        ///
        /// ⚠️⚠️ THIS HAD TO BE ADDED ON THE SAME DAY THE LISTENER LEARNED TO MOVE, AND WITHOUT
        /// IT THAT FIX BREAKS EVERY UI SOUND IN THE GAME. Seven call sites fire a cue at
        /// `Vector3.zero` (`score_award` three times, `match_win`, `round_end`, `MenuSfx`, the
        /// hitmarker), and until 2026-09-06 that worked precisely BECAUSE the listener never
        /// moved: a cue at the origin was a cue at the listener, so it played centred at full
        /// volume. With ears that follow the player those same calls become 3D sounds sitting at
        /// the middle of the map, attenuating and panning as the player walks. **The menu click
        /// would have panned.**
        ///
        /// ⚠️⚠️ IT IS A NAMED SECOND ROUTE AND NOT A `spatialBlend` PARAMETER ON THE FIRST ONE,
        /// AND THAT IS DELIBERATE. `docs/TODO.md` § 150.9 records the existing shape as a genuine
        /// strength: `PlayAt`, `PlayAtVaried` and `PlayImpact` all REQUIRE a position, so a
        /// stationary 2D world cue is impossible to write by accident. Relaxing that into an
        /// optional argument would hand every future call site the chance to make the mistake
        /// silently. A caller now has to say which of the two things it means, out loud, in the
        /// method name.
        ///
        /// ⚠️ THE POOLS ARE SEPARATE FOR THE REASON `WorldVoices` GIVES. `default_bus_layout.tres`
        /// pooled 20 voices as 8 UI plus 12 world, and the split is what stops a fight stealing
        /// the menu's click or a menu stealing a hit. Sharing one pool would put the hitmarker
        /// in the queue behind twelve ringing impacts, which is exactly the moment it is needed.
        /// </summary>
        public void PlayUi(string id, float volumeScale = 1.0f)
            => PlayUiVaried(id, 1.0f, 1.0f, volumeScale);

        /// <summary>The 2D route with a pitch window, for a UI cue that repeats. See
        /// <see cref="PlayUi"/> for why this is a separate route rather than a flag.</summary>
        public void PlayUiVaried(string id, float pitchMin = 1.0f, float pitchMax = 1.0f,
                                 float volumeScale = 1.0f)
        {
            if (!_cues.TryGetValue(id, out var cue))
            {
                Debug.LogWarning($"[Audio] no cue registered for '{id}'.");
                return;
            }

            cue.EverPlayed = true;
            _cues[id] = cue;

            var voice = TakeUiVoice();

            voice.clip = cue.Clip;
            voice.pitch = Random.Range(Mathf.Min(pitchMin, pitchMax),
                                       Mathf.Max(pitchMin, pitchMax));
            voice.volume = cue.Volume * SfxScale() * Mathf.Clamp(volumeScale, 0.0f, 1.25f);
            voice.Play();

            DuckIfAnnouncement(id);
        }

        /// <summary>
        /// A raw clip on the 2D route, for the one caller that holds a clip rather than a cue id.
        ///
        /// ⚠️ IT EXISTS FOR `SplashScreen`'S BOOT STING FALLBACK, which used
        /// `AudioSource.PlayClipAtPoint(clip, Vector3.zero)`. That helper always builds a 3D
        /// source, so it is the eighth site the moving listener would have broken, and it is the
        /// one the `Vector3.zero` grep finds last because it does not go through this class at
        /// all. ⚠️ The volume is passed through rather than looked up: this clip has no cue row
        /// and therefore no authored trim, and inventing one here would be a second mix table.
        /// </summary>
        public void PlayClipUi(AudioClip clip, float volume)
        {
            if (clip == null) return;

            var voice = TakeUiVoice();

            voice.clip = clip;
            voice.pitch = 1.0f;
            voice.volume = Mathf.Clamp01(volume);
            voice.Play();
        }

        /// <summary>
        /// ⚠️ THE LIFT IS POLLED, NOT EVENT-DRIVEN, and that is the cheaper correct answer.
        /// The round clock has no "fifteen seconds left" event to subscribe to, and adding one
        /// would put an audio concern into the rules layer. `SetLift` is idempotent, so calling
        /// it every frame with the same answer costs a comparison.
        /// </summary>
        private void Update() => UpdateMusicLift();

        /// <summary>
        /// Two restrained layers for the few match-defining impacts. The low-pitched layer
        /// supplies weight while the primary keeps the event recognisable. A very short music
        /// duck makes room for the transient without making the whole mix louder.
        /// </summary>
        public void PlayImpact(string primary, string weightLayer, Vector3 position,
                               float energy = 1.0f)
        {
            energy = Mathf.Clamp01(energy);
            PlayAtVaried(primary, position, 0.96f, 1.04f, Mathf.Lerp(0.82f, 1.0f, energy));

            if (!string.IsNullOrEmpty(weightLayer) && weightLayer != primary)
                PlayAtVaried(weightLayer, position, 0.72f, 0.84f,
                             Mathf.Lerp(0.28f, 0.52f, energy));

            GameServices.Music?.Duck(Mathf.Lerp(-2.5f, -5.0f, energy),
                                     Mathf.Lerp(0.10f, 0.20f, energy));
        }

        /// <summary>
        /// Duck the bed if this cue is one of the announcements that should push it down.
        ///
        /// ⚠️ CALLED FROM THE PLAY PATH, NOT FROM THE CALLERS. See `AudioCues.DuckTriggers`:
        /// the whole value of the table is that the countdown, the round end and the score
        /// award do not each have to remember to duck.
        /// </summary>
        private static void DuckIfAnnouncement(string cue)
        {
            if (!Audio.AudioCues.DucksMusic(cue)) return;

            GameServices.Music?.Duck(Audio.AudioCues.MusicDuckDb, Audio.AudioCues.MusicDuckHold);
        }

        /// <summary>
        /// § THE INTENSITY LIFT, driven from the round clock.
        ///
        /// ⚠️⚠️ THE AUDIO ASKS THE ROUND, IT DOES NOT KEEP ITS OWN CLOCK. That is the same rule
        /// the HUD follows and for the same reason: a second opinion about how long is left will
        /// eventually disagree with the scoreboard, and the player believes the scoreboard.
        ///
        /// ⚠️ AND IT IS GATED ON THE ROUND BEING LIVE. Without that, the bed lifts during the
        /// between-round buffer, when `TimeLeft` is sitting at whatever the last round ended on.
        /// </summary>
        private void UpdateMusicLift()
        {
            var music = GameServices.Music;
            if (music == null) return;

            var round = GameServices.Round;

            float pressure = 0.0f;

            if (round != null && round.RoundActive && round.TimeLeft > 0.0f
                && round.TimeLeft <= Audio.MusicDirector.PressureSecondsLeft)
            {
                float left = round.TimeLeft;
                float final = Audio.MusicDirector.LiftSecondsLeft;
                float start = Audio.MusicDirector.PressureSecondsLeft;

                // The first fifteen seconds build to 45 percent, then the last fifteen carry
                // the decisive rise. Both are gain on the already-playing source, so the music
                // never cuts or restarts under the clock.
                pressure = left > final
                    ? Mathf.Lerp(0.0f, 0.45f, (start - left) / Mathf.Max(0.01f, start - final))
                    : Mathf.Lerp(0.45f, 1.0f, (final - left) / Mathf.Max(0.01f, final));
            }

            music.SetPressure(pressure);
        }

        /// <summary>
        /// How many world one-shots may ring at once.
        ///
        /// ⚠️⚠️ POLYPHONY IS BOUNDED HERE AND IT WAS NOT BOUNDED AT ALL. `PlayClipAtPoint`
        /// creates a fresh AudioSource per call and destroys it when the clip ends, so the port
        /// had NO ceiling on concurrent voices: every cue that fired got its own source and they
        /// all summed. That is the second half of B-121, which `audio_manager.gd` states
        /// directly — *"Voices SUM. Four concurrent voices is normal in a fight"* — and it is
        /// why the distortion was reported during play rather than in the menu.
        ///
        /// `default_bus_layout.tres` pools 20 (8 UI + 12 world). Twelve is the world half, which
        /// is what this plays.
        ///
        /// ⚠️ THE OLDEST IS STOLEN, NOT THE NEWEST DROPPED. A pile-up is exactly when the most
        /// recent event matters most: dropping the new sound would silence the hit that caused
        /// the pile-up and leave the footsteps that preceded it ringing.
        /// </summary>
        private const int WorldVoices = 12;

        private readonly List<AudioSource> _voices = new List<AudioSource>(WorldVoices);
        private int _nextVoice;

        private AudioSource TakeVoice()
        {
            // Prefer a voice that has finished on its own.
            foreach (var free in _voices)
                if (!free.isPlaying) return free;

            if (_voices.Count < WorldVoices)
            {
                // ⚠️⚠️ `WorldVoice`, NOT `Voice`, AND THE RENAME IS A COLLISION BEING BROKEN
                // RATHER THAN A TIDY-UP. `Audio.VoiceDirector` is a SECOND component on this same
                // `~GameServices` object, so it shares this transform, and `BuildVoices` there
                // creates its two announcer sources as children literally named `Voice0` and
                // `Voice1` at `spatialBlend = 0`. **Two systems were naming their children
                // identically under one parent.** Nothing in the game noticed, because both hold
                // direct references and neither looks anything up by name; what it cost was a
                // probe. `AudioListenerProbe` reached for `Voice0`, got the ANNOUNCER's 2D source
                // parked at the origin, and reported the world route as flat and unpositioned:
                // two failures that both described the fix being broken when it was not.
                //
                // ⚠️ THE THREE POOLS READ AS THREE THINGS NOW: `WorldVoice*` is 3D one-shots,
                // `UiVoice*` is the non-diegetic route, and `Voice*` is the announcer. Do not
                // give a fourth pool a name that starts with any of them.
                var go = new GameObject($"WorldVoice{_voices.Count}");
                go.transform.SetParent(transform, false);

                var made = go.AddComponent<AudioSource>();

                // ⚠️ 3D, LIKE `PlayClipAtPoint` WAS. These cues are positional: a shove across
                // the arena must not be as loud as one at your shoulder. A pooled source
                // defaults to 2D, so this is not a preference, it is preserving the behaviour
                // the call site already had.
                made.spatialBlend = 1.0f;
                made.playOnAwake = false;
                made.dopplerLevel = 0.0f;
                made.rolloffMode = AudioRolloffMode.Linear;
                made.minDistance = 2.0f;
                made.maxDistance = 32.0f;

                _voices.Add(made);
                return made;
            }

            // Full and all ringing: steal round-robin, which is the oldest start.
            var stolen = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            return stolen;
        }

        /// <summary>
        /// The UI half of `default_bus_layout.tres`'s twenty: 8 UI plus 12 world.
        ///
        /// ⚠️ 2D AND PARKED AT THE RIG ROOT. A `spatialBlend = 0` source ignores its transform
        /// entirely, so the position is documentation rather than behaviour; it is parented here
        /// so it lives and dies with the services object like every other voice.
        /// </summary>
        private const int UiVoices = 8;

        private readonly List<AudioSource> _uiVoices = new List<AudioSource>(UiVoices);
        private int _nextUiVoice;

        private AudioSource TakeUiVoice()
        {
            foreach (var free in _uiVoices)
                if (!free.isPlaying) return free;

            if (_uiVoices.Count < UiVoices)
            {
                var go = new GameObject($"UiVoice{_uiVoices.Count}");
                go.transform.SetParent(transform, false);

                var made = go.AddComponent<AudioSource>();

                made.spatialBlend = 0.0f;
                made.playOnAwake = false;
                made.dopplerLevel = 0.0f;

                _uiVoices.Add(made);
                return made;
            }

            var stolen = _uiVoices[_nextUiVoice];
            _nextUiVoice = (_nextUiVoice + 1) % _uiVoices.Count;
            return stolen;
        }

        /// <summary>Read fresh on every play, so moving a slider is audible on the next sound
        /// rather than after a scene change.</summary>
        private static float SfxScale()
        {
            // ⚠️ `SfxGain`, NOT the two raw fields multiplied. See `GameSettings.Gain`: a
            // slider wired straight to amplitude reads as inert over its top third.
            return Settings.SettingsStore.Current.SfxGain;
        }

        /// <summary>Call from a probe at the end of a match run.</summary>
        public void WarnUnplayedCues()
        {
            foreach (var kv in _cues)
            {
                if (kv.Value.EverPlayed) continue;
                Debug.LogWarning($"[Audio] cue '{kv.Key}' was registered but never played. " +
                                 "That is either a missing call site or a dead registration.");
            }
        }
    }
}
