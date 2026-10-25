using TumbangPreso.Audio;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Local current bed for a replicated live effect. Never broadcasts or changes simulation.</summary>
    public sealed class ZackCurrentAudio : MonoBehaviour
    {
        private AudioSource _source;
        private string _cue;
        private float _mix;
        private bool _paused;

        public static ZackCurrentAudio Attach(Transform parent, string cue)
        {
            var go = new GameObject("CurrentAudio");
            go.transform.SetParent(parent, false);
            var bed = go.AddComponent<ZackCurrentAudio>();
            bed._cue = cue;
            bed._source = go.AddComponent<AudioSource>();
            bed._source.playOnAwake = false;
            bed._source.loop = true;
            bed._source.spatialBlend = 1;
            bed._source.dopplerLevel = 0;
            bed._source.rolloffMode = AudioRolloffMode.Linear;
            bed._source.minDistance = 2;
            bed._source.maxDistance = 22;
            return bed;
        }

        public void Step(Vector3 at, bool live, float gain = 1)
        {
            transform.position = at;
            var audio = GameServices.Audio;
            if (!live || audio == null || !AudioCues.Audible(_cue))
            { _source.Stop(); _paused = false; return; }
            if (_source.clip == null)
            {
                if (!audio.TryGetClip(_cue, out var clip, out _mix)) return;
                _source.clip = clip;
            }
            _source.volume = _mix * audio.SfxVolume * Mathf.Clamp01(gain);
            if (PresentationClock.BlocksInput)
            {
                if (!_paused) { _source.Pause(); _paused = true; }
                return;
            }
            if (_paused) { _source.UnPause(); _paused = false; }
            if (!_source.isPlaying) _source.Play();
        }
        private void OnDisable() { if (_source != null) _source.Stop(); _paused = false; }
    }
}
