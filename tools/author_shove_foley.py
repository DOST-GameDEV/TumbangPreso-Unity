"""Author a short cloth/rubber shove tell from retained project recordings.

Does not replace the original dash, contact or landing recordings. The existing
bump_swing event id now has its own sound rather than aliasing the lunge rush.
"""
from pathlib import Path
import wave,hashlib,json
import numpy as np
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/TumbangPreso/Resources/Sfx'
def read(name):
 with wave.open(str(folder/(name+'.wav'))) as w:
  assert (w.getframerate(),w.getnchannels(),w.getsampwidth())==(44100,1,2)
  return np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
n=round(.17*44100);t=np.arange(n)/44100
cloth=read('dash');rubber=read('land')
a=np.interp(np.linspace(0,len(cloth)-1,n),np.arange(len(cloth)),cloth)
b=np.interp(np.linspace(0,len(rubber)-1,n),np.arange(len(rubber)),rubber)
x=np.convolve(.65*a+.35*b,np.ones(9)/9,'same')
x*=np.sin(np.pi*np.minimum(1,t/.17))**.8
x-=x.mean();x*=.42/max(1e-9,np.max(np.abs(x)))
with wave.open(str(folder/'bump_swing.wav'),'wb') as w:
 w.setnchannels(1);w.setsampwidth(2);w.setframerate(44100);w.writeframes((x*32767).astype('<i2').tobytes())
print(json.dumps({'seconds':n/44100,'peak':float(np.max(np.abs(x))),'sources':{k:hashlib.sha256((folder/(k+'.wav')).read_bytes()).hexdigest() for k in ['dash','land']}}))
