"""Original, deterministic water cues. Only writes Rafi's seven new files."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
RATE=44100


def noise(t,seed,width):
    raw=np.random.default_rng(seed).normal(0,1,len(t))
    return np.convolve(raw,np.ones(width)/width,mode='same')


def bubble(t,at,pitch,decay=.10):
    local=np.maximum(0,t-at)
    phase=2*np.pi*pitch*(local-.22*local*local)
    return np.sin(phase)*np.exp(-local/decay)*(t>=at)


def author(name,seconds,shape,seed):
    t=np.arange(round(seconds*RATE))/RATE
    if shape=='cut':
        envelope=np.sin(np.pi*np.clip(t/.34,0,1))**1.7
        signal=noise(t,seed,11)*envelope*2.6+bubble(t,.18,710,.047)*.16
    elif shape=='tight':
        envelope=np.sin(np.pi*np.clip(t/.25,0,1))**1.2
        signal=noise(t,seed,5)*envelope*1.5+bubble(t,.13,920,.035)*.13
    elif shape=='mirror':
        signal=sum(bubble(t,at,pitch,.13)*gain for at,pitch,gain in [(0,440,.30),(.12,590,.22),(.27,750,.16),(.42,590,.07)])
        signal+=noise(t,seed,32)*np.sin(np.pi*t/seconds)**2*.42
    elif shape=='long':
        signal=sum(bubble(t,at,pitch,.17)*gain for at,pitch,gain in [(0,390,.26),(.16,520,.19),(.38,640,.12),(.66,430,.07)])
        signal+=noise(t,seed,40)*np.sin(np.pi*t/seconds)**2*.44
    elif shape=='impact':
        signal=noise(t,seed,8)*np.exp(-t/.09)*1.4+bubble(t,.01,890,.04)*.18
    elif shape=='wave':
        envelope=np.where(t<.55,(t/.55)**1.4,np.exp(-(t-.55)/.37))
        signal=noise(t,seed,48)*envelope*3.2+noise(t,seed+1,8)*envelope*.35
        signal+=bubble(t,.55,145,.19)*.16
    else:
        # A restrained rising water gather; no borrowed voice or dramatic explosion.
        signal=noise(t,seed,74)*(np.sin(np.pi*t/seconds)**2)*1.5
        for at,pitch in [(.15,220),(.64,294),(1.15,330),(1.82,440)]:signal+=bubble(t,at,pitch,.28)*.17
    edge=np.minimum(1,t/.008)*np.minimum(1,(seconds-t)/.035)
    signal*=edge
    peak=float(np.max(np.abs(signal)))
    signal*=min(1,.67/max(.0001,peak))
    target=ROOT/'Assets/TumbangPreso/Resources/Sfx'/f'{name}.wav'
    with wave.open(str(target),'wb') as output:
        output.setnchannels(1);output.setsampwidth(2);output.setframerate(RATE)
        output.writeframes((signal*32767).astype('<i2').tobytes())
    return {'cue':name,'seconds':seconds,'peak':float(np.max(np.abs(signal))),
            'rms':float(np.sqrt(np.mean(signal**2))),'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}


if __name__=='__main__':
    rows=[('sfx_cast_rafi_current',.42,'cut',7101),('sfx_cast_rafi_mirror',.74,'mirror',7102),
          ('sfx_cast_rafi_breakwater',1.55,'wave',7103),('sfx_rafi_intercept',.25,'impact',7104),
          ('sfx_ult_theme_rafi',2.8,'gather',7105),('sfx_var_rafi_tightcut',.30,'tight',7106),
          ('sfx_var_rafi_longwake',1.02,'long',7107)]
    result={'provenance':'Original deterministic synthesis; no external samples or paid API.',
            'listening':'Not yet auditioned in the game mix. Peak/RMS are technical measurements, not listening approval.',
            'cues':[author(*row) for row in rows]}
    (ROOT/'docs/reports/full-backlog-2026-09-21/rafi-audio-authoring.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('Authored seven distinct Rafi cues; other audio untouched.')
