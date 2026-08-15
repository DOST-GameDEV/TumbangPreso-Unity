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

            AudioSource.PlayClipAtPoint(cue.Clip, position, cue.Volume);
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
