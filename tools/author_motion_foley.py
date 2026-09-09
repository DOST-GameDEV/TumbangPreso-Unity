"""Make restrained movement cues from the game's retained rubber/contact recordings.

Original can, hover, landing and dash files remain unchanged. The new slide is a
short scrape with no catch sound; only the authoritative pickup plays the catch.
"""
from pathlib import Path
import wave
import numpy as np

folder=Path(__file__).resolve().parents[1]/"Assets/TumbangPreso/Resources/Sfx"
rate=44100
def read(name):
    with wave.open(str(folder/(name+".wav"))) as w:
        assert w.getsampwidth()==2 and w.getnchannels()==1 and w.getframerate()==rate
        x=np.frombuffer(w.readframes(w.getnframes()),dtype="<i2").astype(float)/32768
    return x-x.mean()
def write(name,x,peak):
    x=x-x.mean();x*=peak/max(1e-8,np.abs(x).max())
    fade=min(441,len(x)//4);x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
    x*=peak/max(1e-8,np.abs(x).max())
    with wave.open(str(folder/(name+".wav")),"wb") as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate)
        w.writeframes((np.clip(x,-.999,.999)*32767).astype("<i2").tobytes())
    print(name,"seconds",round(len(x)/rate,3),"peak",round(float(abs(x).max()),4))

land=read("land")
write("step_rubber",np.convolve(land,np.ones(7)/7,"same"),.36)
n=round(.34*rate);t=np.arange(n)/rate
dash=read("dash");rubber=read("slipper_land")
air=np.interp(np.linspace(0,len(dash)-1,n),np.arange(len(dash)),dash)
contact=np.interp(np.linspace(0,len(rubber)-1,n),np.arange(len(rubber)),rubber)
rough=np.random.default_rng(910).normal(0,1,n)
rough=np.convolve(rough,np.ones(11)/11,"same")-np.convolve(rough,np.ones(91)/91,"same")
envelope=(1-np.exp(-t*95))*np.maximum(0,1-t/.34)**1.8
write("slide_scrape",(.22*air+.55*contact+.06*rough)*envelope,.58)
