using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class AudioDirector
    {
        private AudioSource _courtAir;
        private float _courtTarget, _courtLevel, _courtCueMix;
        public bool IsInReplayMix => _replayMixDepth>0;
        public float CourtDangerLevel => _courtAir != null && _courtAir.isPlaying ? _courtAir.volume : 0;
        // Personal, non-positional air beneath real footsteps. This never relays or
        // enters WorldCuePlayed: a replay must not inherit the live viewer's danger.
        public void SetCourtDanger(float gain,bool immediate=false)
        {
            _courtTarget=Mathf.Clamp(gain,0,.2f);
            if(!immediate)return;
            _courtLevel=_courtTarget;
            if(_courtAir!=null && _courtLevel<=.001f){_courtAir.Stop();_courtAir.volume=0;}
        }
        private void UpdateCourtDanger()
        {
            float target=_replayMixDepth>0?0:_courtTarget;
            _courtLevel=Mathf.MoveTowards(_courtLevel,target,Time.unscaledDeltaTime*.35f);
            if(_courtLevel<=.001f || SfxVolume<=.001f || _replayMixDepth>0)
            {if(_courtAir!=null){_courtAir.Stop();_courtAir.volume=0;}return;}
            if(_courtAir==null)
            {
                if(!TryGetClip("throw_whoosh",out var clip,out _courtCueMix))return;
                var go=new GameObject("Court danger air");go.transform.SetParent(transform,false);
                _courtAir=go.AddComponent<AudioSource>();_courtAir.playOnAwake=false;
                _courtAir.loop=true;_courtAir.spatialBlend=0;_courtAir.pitch=.4f;_courtAir.dopplerLevel=0;_courtAir.clip=clip;
                var low=go.AddComponent<AudioLowPassFilter>();low.cutoffFrequency=240;low.lowpassResonanceQ=1;
            }
            _courtAir.volume=_courtLevel*_courtCueMix*SfxVolume;
            if(!_courtAir.isPlaying)_courtAir.Play();
        }
    }
}
