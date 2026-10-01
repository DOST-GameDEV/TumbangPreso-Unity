using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class AudioDirector
    {
        private AudioLowPassFilter _hauntedFilter;
        public bool HauntedMuffle => _hauntedFilter != null && _hauntedFilter.enabled;
        private void UpdateHauntedAudio(Camera view)
        {
            bool active = isActiveAndEnabled && Visual.HauntedPerception.Applies(view);
            if (!active)
            { if (_hauntedFilter != null) _hauntedFilter.enabled = false; return; }
            if (_hauntedFilter == null)
            {
                _hauntedFilter = _ears.gameObject.AddComponent<AudioLowPassFilter>();
                _hauntedFilter.cutoffFrequency = 1400f;
                _hauntedFilter.lowpassResonanceQ = 1f;
            }
            _hauntedFilter.enabled = true;
        }
        private void OnDisable()
        { if (_hauntedFilter != null) _hauntedFilter.enabled = false; }
    }
}
