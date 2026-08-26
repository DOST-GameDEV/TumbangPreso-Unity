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
        private void Awake()
        {
            gameObject.AddComponent<AudioListener>();

            LoadCuesFromResources();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => KeepOneListener();
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
        /// ⚠️ THIS LOOP CANNOT SEE `mine`, AND THAT IS FINE NOW. `mine` lives on a
        /// `HideAndDontSave` object, so `FindObjectsByType` never returns it even with
        /// `FindObjectsInactive.Include` — that exclusion is what let this object's listener and
        /// BootSting's coexist unseen (see the ⚠️ on Awake). The loop no longer needs to find
        /// `mine`; it only needs to find and disable whatever a SCENE brings, which is an
        /// ordinary object and not hidden. `mine` is held by direct reference and is never a
        /// member of the set this searches, so the `listener == mine` check below only ever
        /// short-circuits nothing found; it is kept because a false positive here (disabling the
        /// real listener) would be silent, and the guard costs nothing to leave in.
        /// </summary>
        private void KeepOneListener()
        {
            var mine = GetComponent<AudioListener>();

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
                var go = new GameObject($"Voice{_voices.Count}");
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

        /// <summary>Read fresh on every play, so moving a slider is audible on the next sound
        /// rather than after a scene change.</summary>
        private static float SfxScale()
        {
            var s = Settings.SettingsStore.Current;
            return s.SfxVolume * s.MasterVolume;
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
