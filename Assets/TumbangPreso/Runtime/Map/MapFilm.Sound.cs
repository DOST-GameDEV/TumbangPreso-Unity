using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    public abstract partial class MapFilm
    {
        // ------------------------------------------------------------------ the sound
        //
        // Each film has its own score (tools/synth_map_film_sfx.py: numpy and scipy, fixed seeds, nothing recorded or
        // downloaded) played from the film's clock, so a peer that rolls late or skips hears the bar that belongs
        // to the picture. Its cues play on the film's own sources, over the map's own ambience, which keeps playing.

        /// <summary>The score's clip under Resources/Sfx; null for none.</summary>
        protected abstract string Score { get; }
        /// <summary>The score's level this frame, 0 to 1 (a director ducks it under a moment).</summary>
        protected float ScoreLevel = 0.85f;

        private AudioSource _score, _cues;
        private AudioClip _scoreClip;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        private void BuildSound()
        {
            if (_score == null) { _score = Source("Map film score"); _cues = Source("Map film cues"); }
            _scoreClip = Score != null ? Clip(Score) : null;
            ScoreLevel = 0.85f;
        }

        private AudioSource Source(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0.0f;
            source.bypassListenerEffects = true; source.bypassReverbZones = true;
            return source;
        }

        /// <summary>A clip of the film's own, read under the curtain so its first play does not hitch.</summary>
        protected AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_clips.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("Sfx/" + name);
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            _clips[name] = clip;
            return clip;
        }

        private static float Level => GameServices.Audio != null ? GameServices.Audio.SfxVolume : 1.0f;

        /// <summary>One cue, now (only once the clock runs).</summary>
        protected void Cue(string name, float volume, float pitch = 1.0f) => Cue(Clip(name), volume, pitch);

        /// <summary>For a recording of a film (`MapFilmTests` renders each to video): every cue as it is called, with
        /// the film's age, its volume and its pitch, played or not.</summary>
        public static System.Action<AudioClip, float, float, float> Heard;

        protected void Cue(AudioClip clip, float volume, float pitch = 1.0f)
        {
            if (clip != null && !double.IsNaN(Began)) Heard?.Invoke(clip, Age, volume, pitch);
            if (clip == null || _cues == null || double.IsNaN(Began) || HeldAge >= 0.0f) return;
            _cues.pitch = pitch;
            _cues.PlayOneShot(clip, Mathf.Clamp01(volume * Level));
        }

        private void StartScore()
        {
            if (_score == null || _scoreClip == null) return;
            _score.clip = _scoreClip; _score.loop = false; _score.volume = 0.0f;
            _score.time = Mathf.Clamp(Age, 0.0f, _scoreClip.length - 0.05f);
            _score.Play();
        }

        private void SeekScore(float age)
        {
            if (_score == null || _scoreClip == null || !_score.isPlaying) return;
            _score.time = Mathf.Clamp(age, 0.0f, _scoreClip.length - 0.05f);
        }

        private void Sound(float age)
        {
            if (_score == null || !_score.isPlaying) return;
            var t = Timeline;
            _score.volume = Mathf.Clamp01(ScoreLevel) * Level * Span(age, 0.0f, 0.4f) * (1.0f - Span(age, t.Handoff, t.End + 0.2f));
        }

        private void StopSound()
        {
            if (_score != null) _score.Stop();
            if (_cues != null) _cues.Stop();
        }
    }
}
