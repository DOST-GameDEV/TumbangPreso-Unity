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
        private void Awake()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
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
            AudioSource.PlayClipAtPoint(cue.Clip, position, cue.Volume * SfxScale());
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
