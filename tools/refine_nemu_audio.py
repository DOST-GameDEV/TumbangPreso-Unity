"""Refine Nemu's existing baked cues around phase, possession and transformation.
Uses recorded/synthesised inputs at a1ac87b; never recursively processes its output.
Signal/timing checks are not listening approval. Existing can and UI cues are kept.
"""
from pathlib import Path
import io,json,subprocess,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1];BASE='a1ac87b';REL='Assets/TumbangPreso/Resources/Sfx';RATE=44100

def load(name):
 data=subprocess.check_output(['git','show',f'{BASE}:{REL}/{name}.wav'],cwd=ROOT)
 with wave.open(io.BytesIO(data)) as w:
  assert w.getsampwidth()==2 and w.getnchannels()==1 and w.getframerate()==RATE
  return np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768

def cut(a,start,seconds):return a[int(start*RATE):int((start+seconds)*RATE)].copy()
def band(a,low,high):
 f=np.fft.rfftfreq(len(a),1/RATE);gain=1/(1+(low/np.maximum(f,.01))**4)/(1+(f/high)**6)
 return np.fft.irfft(np.fft.rfft(a)*gain,n=len(a))
def env(a,attack,release):
 a=a.copy();n=min(len(a),int(attack*RATE));m=min(len(a),int(release*RATE))
 if n:a[:n]*=np.sin(np.linspace(0,np.pi/2,n))**2
 if m:a[-m:]*=np.cos(np.linspace(0,np.pi/2,m))**2
 return a

def rms(a):return float(np.sqrt(np.mean(a*a)))
def stats(a):return dict(seconds=round(len(a)/RATE,4),peak=round(float(max(abs(a))),5),rms=round(rms(a),5),peak_seconds=round(float(np.argmax(abs(a)))/RATE,4))
names=['sfx_cast_nemu_veil','sfx_cast_nemu_hijack','sfx_cast_nemu_seance','sfx_possess_enter','sfx_possess_exit','sfx_kuro_unbound','sfx_kuro_return','sfx_ult_theme_nemu']
source={n:load(n) for n in names}
recipes={
 'sfx_cast_nemu_veil':env(band(cut(source['sfx_cast_nemu_veil'],.38,.38),170,5000),.012,.14),
 'sfx_cast_nemu_hijack':env(band(cut(source['sfx_cast_nemu_hijack'],.06,.42),130,5000),.014,.14),
 'sfx_cast_nemu_seance':env(band(cut(source['sfx_cast_nemu_seance'],.28,.40),180,4500),.14,.025),
 'sfx_possess_enter':env(band(cut(source['sfx_possess_enter'],.55,.45),100,4200),.02,.10),
 'sfx_possess_exit':env(band(cut(source['sfx_possess_exit'],.35,.50),95,4300),.012,.23),
 'sfx_kuro_unbound':env(band(cut(source['sfx_kuro_unbound'],1.12,1.08),65,3600),.07,.32),
 'sfx_kuro_return':env(band(source['sfx_kuro_return'],120,4700),.016,.20),
 'sfx_ult_theme_nemu':env(band(cut(source['sfx_ult_theme_nemu'],.65,2.35),85,3800),.25,.70),
}
report=[]
for name,a in recipes.items():
 old=source[name];a-=a.mean();a=env(a,.002,.005)
 factor=.70 if name.startswith('sfx_cast') else .72 if 'theme' in name else .94
 gain=min(max(abs(old))*factor/max(max(abs(a)),1e-9),rms(old)*factor/max(rms(a),1e-9));a*=gain
 assert np.isfinite(a).all() and max(abs(a))<.9 and abs(a[0])<1e-5 and abs(a[-1])<1e-5
 with wave.open(str(ROOT/REL/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
 report.append(dict(cue=name,before=stats(old),after=stats(a)))
p=ROOT/'docs/reports/improvement-2026-09-10/nemu-audio-timing.json'
p.write_text(json.dumps(dict(source_commit=BASE,auditory_approval=False,cues=report),indent=2)+'\n')
print(json.dumps(report,indent=2))
