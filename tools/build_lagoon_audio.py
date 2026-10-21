"""Original quiet wood/water foley; never rewrites other map or hero audio."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np
from build_rafi_audio import RATE, noise, bubble

ROOT=Path(__file__).resolve().parents[1]


def write(name,signal):
    signal=np.asarray(signal);t=np.arange(len(signal))/RATE
    signal*=np.minimum(1,t/.005)*np.minimum(1,(t[-1]-t)/.018)
    signal*=min(1,.60/max(.0001,np.max(np.abs(signal))))
    path=ROOT/'Assets/TumbangPreso/Resources/Sfx'/f'{name}.wav'
    with wave.open(str(path),'wb') as out:
        out.setnchannels(1);out.setsampwidth(2);out.setframerate(RATE);out.writeframes((signal*32767).astype('<i2').tobytes())
    return {'cue':name,'seconds':len(signal)/RATE,'peak':float(np.max(np.abs(signal))),
            'rms':float(np.sqrt(np.mean(signal**2))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}


if __name__=='__main__':
    t=np.arange(int(RATE*.22))/RATE
    wood=sum(np.sin(2*np.pi*f*t)*np.exp(-t/decay)*gain for f,decay,gain in [(112,.055,.23),(237,.029,.11),(421,.013,.04)])
    wood+=noise(t,7201,6)*np.exp(-t/.014)*.25
    rows=[write('sfx_step_deck',wood)]
    t=np.arange(int(RATE*.48))/RATE
    stroke=noise(t,7202,18)*np.sin(np.pi*t/.48)**2*1.35+bubble(t,.12,520,.075)*.08
    rows.append(write('sfx_swim_stroke',stroke))
    t=np.arange(int(RATE*1.15))/RATE
    lap=noise(t,7203,53)*np.sin(np.pi*t/1.15)**2*.9+bubble(t,.52,270,.12)*.04
    rows.append(write('sfx_lagoon_lap',lap))
    result={'provenance':'Original deterministic synthesis. No external samples or paid API.',
            'listening':'Technical measurements only; native mix/listening acceptance remains open.','cues':rows}
    (ROOT/'docs/reports/full-backlog-2026-09-21/lagoon-audio-authoring.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('Authored three quiet lagoon foley cues.')
