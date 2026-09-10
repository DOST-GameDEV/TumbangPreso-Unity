"""Align Cheska's current baked ice layers to preparation, impact and thaw.

Inputs are the baked recorded/synthesised cues at a275138, so a repeat cannot process its own
output. The original Freesound download folder is not required. Source attribution
remains in docs/Asset_Sourcing.md and tools/build_ability_audio.py. No can/UI cues
are touched. This is signal/timing work, not a claim of listening approval.
"""
from pathlib import Path
import io,json,subprocess,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
BASE='a275138'
REL='Assets/TumbangPreso/Resources/Sfx'
RATE=44100

def load(name):
 data=subprocess.check_output(['git','show',f'{BASE}:{REL}/{name}.wav'],cwd=ROOT)
 with wave.open(io.BytesIO(data)) as w:
  assert w.getsampwidth()==2 and w.getnchannels()==1 and w.getframerate()==RATE
  return np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(np.float64)/32768

def cut(a,start,length):
 return a[int(start*RATE):int((start+length)*RATE)].copy()

def filter_band(a,low,high):
 f=np.fft.rfftfreq(len(a),1/RATE)
 gain=(1/(1+(low/np.maximum(f,.01))**4))*(1/(1+(f/high)**6))
 return np.fft.irfft(np.fft.rfft(a)*gain,n=len(a))

def envelope(a,attack,release):
 a=a.copy();n=min(len(a),int(attack*RATE));m=min(len(a),int(release*RATE))
 if n:a[:n]*=np.sin(np.linspace(0,np.pi/2,n))**2
 if m:a[-m:]*=np.cos(np.linspace(0,np.pi/2,m))**2
 return a

def stretch(a,seconds):
 return np.interp(np.linspace(0,len(a)-1,int(seconds*RATE)),np.arange(len(a)),a)

def rms(a):return float(np.sqrt(np.mean(a*a)))

def stats(a):return dict(seconds=round(len(a)/RATE,4),peak=round(float(np.max(np.abs(a))),5),rms=round(rms(a),5),peak_seconds=round(float(np.argmax(np.abs(a)))/RATE,4))

source={n:load(n) for n in ['sfx_cast_cheska_sheet','sfx_ice_form','sfx_cast_cheska_barricade','sfx_barricade_raise','sfx_cast_cheska_nova','sfx_frost_nova','sfx_ult_theme_cheska','sfx_ice_shatter']}
recipes={
 'sfx_cast_cheska_sheet':envelope(filter_band(cut(source['sfx_cast_cheska_sheet'],0,.19),350,6200),.008,.09),
 'sfx_ice_form':envelope(filter_band(cut(source['sfx_ice_form'],.15,.65),110,9500),.018,.23),
 'sfx_cast_cheska_barricade':envelope(filter_band(cut(source['sfx_cast_cheska_barricade'],.71,.25),190,6200),.008,.09),
 'sfx_barricade_raise':envelope(filter_band(cut(source['sfx_barricade_raise'],.36,.59),75,8000),.008,.24),
 'sfx_cast_cheska_nova':envelope(filter_band(stretch(cut(source['sfx_cast_cheska_nova'],.83,.57)[::-1],.40),400,7800),.25,.035),
 'sfx_ult_theme_cheska':envelope(filter_band(cut(source['sfx_ult_theme_cheska'],.35,1.35),160,6200),.18,.55),
}
# One immediate brittle transient over a short low-pressure body. The old impact
# sounded for 1.5 s while the meaningful radial release was over in half a second.
impact=filter_band(cut(source['sfx_frost_nova'],0,.85),70,1600)*.45
crack=filter_band(source['sfx_ice_shatter'],600,10500)
impact[:len(crack)]+=crack*.75
recipes['sfx_frost_nova']=envelope(impact,.004,.40)
report=[]
for name,a in recipes.items():
 old=source[name];a-=a.mean();a=envelope(a,.002,.005)
 # Never exceed the old peak or whole-cue RMS. Preparation/theme layers are
 # deliberately quieter so overlap leaves room for the objective and impact.
 factor=.62 if name.startswith('sfx_cast') else .72 if 'theme' in name else .94
 gain=min(float(np.max(abs(old)))*factor/max(float(np.max(abs(a))),1e-9),rms(old)*factor/max(rms(a),1e-9))
 a*=gain
 assert np.isfinite(a).all() and max(abs(a))<.90 and abs(a[0])<1e-5 and abs(a[-1])<1e-5
 with wave.open(str(ROOT/REL/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
 report.append(dict(cue=name,before=stats(old),after=stats(a)))
path=ROOT/'docs/reports/improvement-2026-09-10/cheska-audio-timing.json'
path.write_text(json.dumps(dict(source_commit=BASE,auditory_approval=False,cues=report),indent=2)+'\n')
print(json.dumps(report,indent=2))
