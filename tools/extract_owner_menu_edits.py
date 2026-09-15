"""Extract the supplied flat-white sheets without resizing or redrawing their art."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15'
OUTPUT=SOURCE/'extracted'
REGIONS={
    'buttons_mainmenu.png':{
        'main-logo':(134,38,658,435),'main-play':(164,454,413,156),
        'main-tutorial':(166,613,446,122),'main-settings':(165,738,497,122),
        'main-quit':(175,862,316,123)},
    'updatedbuttons_login.png':{
        'login-logo':(741,37,425,286),'login-tabs':(751,339,399,96),
        'login-field1':(675,459,553,82),'login-field2':(675,553,553,83),
        'login-field3':(675,652,553,82),'login-checkbox':(685,750,26,28),
        'login-primary':(736,813,431,96),'login-rule-left':(679,932,245,10),
        'login-rule-right':(999,934,246,10),'login-guest':(736,955,431,99)}
}


def remove_white_backing(rgb):
    # The flat sheet includes near-white export noise as well as exact white.
    # Treat only its connected exterior as backing, not light paint inside art.
    pale=np.min(rgb,axis=2)>=248
    labels,_=ndimage.label(pale)
    exterior=np.unique(np.concatenate((labels[0],labels[-1],labels[:,0],labels[:,-1])))
    paper=pale & np.isin(labels,exterior[exterior!=0])
    foreground=~paper
    alpha=np.where(foreground,255,0).astype(np.uint8)
    result=rgb.copy()
    # Recover only white-matted antialias pixels consistent with a nearby opaque
    # colour. Interior pixels and thin authored strokes remain byte-identical.
    distance=ndimage.distance_transform_edt(foreground)
    core=distance>=4
    if np.any(core):
        nearest=ndimage.distance_transform_edt(~core,return_distances=False,return_indices=True)
        colour=rgb[nearest[0],nearest[1]].astype(float)
        direction=255-colour
        denominator=np.sum(direction*direction,axis=2)
        opacity=np.divide(np.sum((255-rgb.astype(float))*direction,axis=2),denominator,
            out=np.ones_like(denominator),where=denominator>1)
        predicted=opacity[:,:,None]*colour+(1-opacity[:,:,None])*255
        residual=np.max(np.abs(predicted-rgb),axis=2)
        edge=foreground & (distance<4) & (opacity>0) & (opacity<.98) & (residual<=8)
        result[edge]=np.rint(colour[edge]).astype(np.uint8)
        alpha[edge]=np.rint(opacity[edge]*255).astype(np.uint8)
    else:
        edge=np.zeros(paper.shape,dtype=bool)
    rgba=np.dstack((result,alpha))
    return rgba,int(edge.sum())


def main():
    OUTPUT.mkdir(exist_ok=True)
    manifest=[]
    for file,regions in REGIONS.items():
        original=SOURCE/file
        rgb=np.array(Image.open(original).convert('RGB'))
        for name,(x,y,w,h) in regions.items():
            crop=rgb[y:y+h,x:x+w]
            rgba,edges=remove_white_backing(crop)
            target=OUTPUT/(name+'.png');Image.fromarray(rgba).save(target)
            assert Image.open(target).size==(w,h)
            manifest.append({'name':name,'source':file,'source_sha256':hashlib.sha256(original.read_bytes()).hexdigest(),
                'source_xywh':[x,y,w,h],'output':target.name,'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
                'white_matte_edge_pixels_recovered':edges,'resized':False})
    (OUTPUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(f'Extracted {len(manifest)} pieces at their original pixel dimensions.')


if __name__=='__main__':
    main()
