import sys, numpy as np, cv2, glob
mode=sys.argv[1]
fs=sorted(glob.glob(f"frames_{mode}/*.png"))
prev=None; ch=[]; px=[]
for f in fs:
    im=cv2.imread(f).astype(np.int16)
    if prev is not None:
        d=np.abs(im-prev).max(-1); ch.append(d.mean()); px.append(int((d>3).sum()))
    prev=im
ch=np.array(ch); px=np.array(px)
print(mode,'mean change/frame %.4f  median changed px(>3) %d  max %d'%(ch.mean(),np.median(px),px.max()))
# spikes: frames whose change > 2.5x the median of neighbours
med=np.median(ch); sp=[i for i in range(2,len(ch)-2) if ch[i]>2.5*np.median(ch[i-2:i+3]) and ch[i]>med*1.5]
print('spikes',sp[:20])
# 0.8s apart changed pixels (as in the owner's 22,705 number, at half res so x4)
a=cv2.imread(fs[0]).astype(np.int16); b=cv2.imread(fs[24]).astype(np.int16)
print('changed px 0.8s apart (full-res equiv) %d'%(4*int((np.abs(a-b).max(-1)>3).sum())))
