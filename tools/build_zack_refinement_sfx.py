"""Original local audition cues for Zack's pullback and low electric tripwire. No samples or game registration."""
from pathlib import Path
import argparse,json,wave
import numpy as np
SR=48000
rng=np.random.default_rng(25102026)
def clock(s):return np.arange(round(s*SR))/SR
def tone(t,a,b):
 f=np.geomspace(a,b,len(t));p=2*np.pi*np.cumsum(f)/SR
 return np.sin(p)+.23*np.sin(p*2.013)
def noise(t,lo,hi):
 x=rng.normal(size=len(t));freq=np.fft.rfftfreq(len(t),1/SR)
 window=np.clip((freq-lo)/max(1,lo*.4),0,1)*np.clip((hi-freq)/max(1,hi*.3),0,1)
 return np.fft.irfft(np.fft.rfft(x)*window,n=len(t))
def sparks(t,n):
 x=np.zeros(len(t))
 for at in rng.uniform(0,max(.001,t[-1]-.005),n):
  i=int(at*SR);k=min(170,len(x)-i);x[i:i+k]+=rng.normal(size=k)*np.exp(-np.arange(k)/22)*rng.uniform(.2,.7)
 return x

def backtrack():
 t=clock(.46);u=t/.42
 # Inward suction and climbing dry contacts, with a low current underneath. It closes into the landing cue.
 e=np.sin(np.minimum(u,1)*np.pi*.5)**1.8*np.clip((.46-t)/.04,0,1)
 return .16*tone(t,180,1250)*e+.28*noise(t,350,4800)*e*(.35+.65*np.maximum(0,np.sin(2*np.pi*(11*t+45*t*t))))+.25*sparks(t,24)*e

def land():
 t=clock(.26);return .52*noise(t,900,10500)*np.exp(-t/ .018)+.32*tone(t,190,65)*np.exp(-t/.045)+.16*sparks(t,5)*np.exp(-t/.12)

def string():
 t=clock(.56);click=noise(t,1400,10000)*np.exp(-t/.009)
 at=np.maximum(0,t-.12);pluck=(np.sin(2*np.pi*360*at)+.4*np.sin(2*np.pi*725*at))*np.exp(-at/.13)*(t>=.12)
 whip=noise(t,600,6500)*np.sin(np.pi*np.clip(t/.17,0,1))**2*(t<.17)
 return .5*click+.24*pluck+.28*whip+.1*sparks(t,9)*np.exp(-t/.2)

def zap():
 t=clock(.28);gate=(np.sin(2*np.pi*75*t)>-.2).astype(float)
 return .45*noise(t,1100,12500)*np.exp(-t/.012)+.24*noise(t,350,5200)*gate*np.exp(-t/.075)+.16*tone(t,470,135)*np.exp(-t/.06)+.14*sparks(t,16)*np.exp(-t/.12)

def snap():
 t=clock(.38);return .38*noise(t,1000,9000)*np.exp(-t/.012)+.35*tone(t,265,90)*np.exp(-t/.085)+.12*noise(t,150,1600)*np.exp(-t/.09)

def hum():
 t=clock(3);carrier=np.sin(2*np.pi*60*t)+.38*np.sin(2*np.pi*120*t)+.17*np.sin(2*np.pi*240*t)
 return .3*carrier*(.8+.2*np.sin(2*np.pi*7*t))+.08*sparks(t,16)

def write(p,x):
 x=x-np.mean(x);x=np.tanh(x*1.3);x=x/max(np.max(np.abs(x)),1e-6)*10**(-3/20)
 # A little asymmetric current, not a wide synthetic whoosh. Mono remains coherent.
 y=np.column_stack((x,np.roll(x,9)*.94));fade=min(240,len(y)//2);y[:fade]*=np.linspace(0,1,fade)[:,None];y[-fade:]*=np.linspace(1,0,fade)[:,None]
 with wave.open(str(p),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((y*32767).astype('<i2').tobytes())
 return {'file':p.name,'seconds':len(x)/SR,'peak_dbfs':20*np.log10(np.max(np.abs(y))+1e-12)}
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--out',required=True);args=a.parse_args();out=Path(args.out);out.mkdir(parents=True,exist_ok=True)
 recipes={'backtrack-pull':backtrack,'backtrack-catch':land,'wire-string':string,'wire-zap':zap,'wire-snap':snap,'wire-current-loop':hum}
 manifest=[write(out/(name+'.wav'),fn()) for name,fn in recipes.items()]
 (out/'audition-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8');print(json.dumps(manifest,indent=2))
