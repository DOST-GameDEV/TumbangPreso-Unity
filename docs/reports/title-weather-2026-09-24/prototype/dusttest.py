import air, numpy as np, sys
from multiprocessing import Pool
ZONES=[(1000,570,550,100),(0,984,800,96),(1760,700,155,365)]
SOLIDS=[(1590,695,130,205),(1335,727,110,20),(50,990,130,60)]
BASE=air.PLATE  # dust test disables the air shader, so the base is the plain plate
def dust(t, old):
    img=BASE.copy()
    air.splat_puffs(img, air.old_dust_at(t) if old else air.wisps_at(t)+air.grains_at(t)+air.motes_at(t), 1.0)
    return img
def run(args):
    t0, old = args
    changed=[np.zeros((z[3],z[2]),bool) for z in ZONES]; solid=0
    for k in range(4):
        d=(np.abs(dust(t0+k*.8,old)-BASE).max(-1)*255)>3
        for i,(x,y,w,hh) in enumerate(ZONES): changed[i]|=d[y:y+hh,x:x+w]
        for (x,y,w,hh) in SOLIDS: solid+=int(d[y:y+hh,x:x+w].sum())
    return [int(c.sum()) for c in changed], solid
if __name__=='__main__':
    old = len(sys.argv)>1 and sys.argv[1]=='old'
    starts=[(float(t),old) for t in np.linspace(0.7,600,120)]
    with Pool(11) as p: res=p.map(run,starts)
    arr=np.array([r[0] for r in res]); sol=[r[1] for r in res]
    print('old' if old else 'new','zone min',arr.min(0),'p10',np.percentile(arr,10,axis=0).astype(int),'median',np.median(arr,0).astype(int),'fails',(arr<=30).any(1).sum(),'of',len(arr),'solid hits',sum(sol))
