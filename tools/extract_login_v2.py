"""Extract the owner's final blank sign-up/sign-in compositions at native size."""
from pathlib import Path
import hashlib,json
import numpy as np
from scipy import ndimage
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15'
REGIONS={
 'signup-reference-v2.png':{
  'login2-logo':(736,21,426,290),'login2-tabs-left':(748,330,400,92),
  'login2-user-up':(671,449,554,82),'login2-pass-up':(671,545,554,80),
  'login2-confirm':(671,642,554,81),'login2-check':(673,728,26,29),
  'login2-primary-up':(732,803,432,95),'login2-guest':(732,946,432,96)},
 'signin-reference-v2.png':{
  'login2-tabs-right':(754,331,392,92),'login2-user-in':(671,539,554,81),
  'login2-pass-in':(671,637,554,81),'login2-primary-in':(732,798,432,95)}}

def main():
    background=np.array(Image.open(SOURCE/'login-background-woven.png').convert('RGB')).astype(float)
    out=SOURCE/'login-v2';out.mkdir(exist_ok=True);entries=[]
    for file,regions in REGIONS.items():
        original=SOURCE/file;rgb=np.array(Image.open(original).convert('RGB')).astype(float)
        for name,(x,y,w,h) in regions.items():
            crop=rgb[y:y+h,x:x+w];back=background[y:y+h,x:x+w]
            foreground=np.max(np.abs(crop-back),axis=2)>20
            labels,count=ndimage.label(foreground)
            sizes=np.bincount(labels.ravel());sizes[0]=0
            foreground=labels==sizes.argmax()
            distance=ndimage.distance_transform_edt(foreground);core=distance>=3
            if not core.any():core=foreground
            nearest=ndimage.distance_transform_edt(~core,return_distances=False,return_indices=True)
            colour=crop[nearest[0],nearest[1]];direction=colour-back
            denominator=np.sum(direction*direction,axis=2)
            opacity=np.divide(np.sum((crop-back)*direction,axis=2),denominator,
                out=np.zeros_like(denominator),where=denominator>1)
            error=np.max(np.abs(back+opacity[:,:,None]*direction-crop),axis=2)
            near=ndimage.distance_transform_edt(~foreground)<=2
            edge=near&(distance<3)&(opacity>0)&(opacity<.98)&(error<6)
            alpha=np.where(foreground,255,0).astype(np.uint8);colours=crop.copy()
            alpha[edge]=np.rint(np.clip(opacity[edge],0,1)*255).astype(np.uint8);colours[edge]=colour[edge]
            rgba=np.dstack((np.uint8(np.clip(colours,0,255)),alpha))
            ys,xs=np.where(alpha>5);box=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
            rgba=rgba[box[1]:box[3],box[0]:box[2]]
            target=out/(name+'.png');Image.fromarray(rgba).save(target)
            entry={'name':name,'x':x+box[0],'y':y+box[1],'width':rgba.shape[1],'height':rgba.shape[0],
                   'faceX':rgba.shape[1]/2,'faceY':rgba.shape[0]/2,'source':file,
                   'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'resized':False}
            if 'primary' in name or 'guest' in name:
                face=(rgba[:,:,3]>230)&(rgba[:,:,1]>170)&(rgba[:,:,0]<230)&(rgba[:,:,2]<130) if 'primary' in name else \
                     (rgba[:,:,3]>230)&(rgba[:,:,1]>180)&(rgba[:,:,0]>220)&(rgba[:,:,2]<140)
                fy,fx=np.where(face);entry['faceX']=float((fx.min()+fx.max())/2);entry['faceY']=float((fy.min()+fy.max())/2)
            entries.append(entry)
            if 'tabs-' in name:
                face=(rgba[:,:,3]>230)&(rgba[:,:,0]>120)&(rgba[:,:,1]>165)&(rgba[:,:,2]<145)
                fy,fx=np.where(face);entry['faceX']=float((fx.min()+fx.max())/2);entry['faceY']=float((fy.min()+fy.max())/2)
            print(name,entry['x'],entry['y'],entry['width'],entry['height'])
    (out/'login-layout-v2.json').write_text(json.dumps({'pieces':entries},indent=2),encoding='utf-8')

if __name__=='__main__':main()
