using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class AudioDirector
    {
        private AudioLowPassFilter _hauntedFilter;
        public bool HauntedMuffle => _hauntedFilter != null && _hauntedFilter.enabled;
        private void UpdateHauntedAudio(Camera view)
        {
            // Nemu's PRUSISYON muffles the possessed player's hearing too, a little less than Haunted (the ghost is inside them).
            float possessed = isActiveAndEnabled ? Visual.PossessedPerception.Weight(view) : 0f;
            // Basilio's CONCUSSED muffles the victim's hearing under the ringing (`Visual.ConcussedPerception`).
            float concussed = isActiveAndEnabled ? Visual.ConcussedPerception.Weight(view) : 0f;
            bool active = isActiveAndEnabled && (Visual.HauntedPerception.Applies(view) || possessed > 0f || concussed > 0f);
            if (!active)
            { if (_hauntedFilter != null) _hauntedFilter.enabled = false; return; }
            if (_hauntedFilter == null)
            {
                _hauntedFilter = _ears.gameObject.AddComponent<AudioLowPassFilter>();
                _hauntedFilter.cutoffFrequency = 1400f;
                _hauntedFilter.lowpassResonanceQ = 1f;
            }
            _hauntedFilter.cutoffFrequency = Visual.HauntedPerception.Applies(view) ? 1400f
                : Mathf.Min(Mathf.Lerp(22000f, 2200f, possessed), Mathf.Lerp(22000f, Visual.ConcussedPerception.MuffleHz, concussed));
            _hauntedFilter.enabled = true;
        }
        private void OnDisable()
        { if (_hauntedFilter != null) _hauntedFilter.enabled = false; }
    }
}
