import air, numpy as np, sys
ys,xs=np.mgrid[0:330:4,960:1840:4].astype(np.float32)
op=air.SKY[0:330:4,960:1840:4]
def cov(t):
    s=air.air_params(t); a=np.zeros_like(xs)
    for rect,fade,flip in ((s.far,s.fade[1],False),(s.mid,s.fade[2],True),(s.near,s.fade[0],False)):
        c=air.cloud_at(xs,ys,rect,t,0,1,flip); a=np.maximum(a,c[...,3]*fade)
    return float((a*op).sum()/op.sum())
ts=np.arange(0,1500,5.0); c=np.array([cov(t) for t in ts])
print('share of sky covered: min %.2f p10 %.2f median %.2f max %.2f; time <5%% covered: %.0f%%'%(c.min(),np.percentile(c,10),np.median(c),c.max(),100*(c<.05).mean()))
# longest empty stretch
run=best=0
for v in c:
    run = run+5 if v<.05 else 0; best=max(best,run)
print('longest empty stretch', best,'s')
