"""Plot measured scene inventory for human spatial review, without editing Unity.

Footprint overlaps are AABB candidates, not proof of mesh intersection. The
versioned diagrams support inspection of real first-person/architecture views.
"""
import argparse
import collections
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--inventory', required=True)
parser.add_argument('--out', required=True)
parser.add_argument('--map', choices=['Eskinita','BayanPlaza','IlalimNgTulay'])
args = parser.parse_args()
out = Path(args.out)
out.mkdir(parents=True, exist_ok=True)
report = {}

for name in ([args.map] if args.map else ['Eskinita', 'BayanPlaza', 'IlalimNgTulay']):
    data = json.loads((Path(args.inventory) / (name + '.json')).read_text())
    surfaces = [s for s in data['surfaces'] if s['enabled']]
    buildings = [s for s in surfaces if any(x in s['asset'] for x in
                 ['/building-', '/low-detail-building-', 'env_church_facade',
                  'env_bell_tower', 'env_municipal_hall', 'env_sari_sari_store'])]
    solids = [s for s in data['solids'] if s['enabled'] and not s['trigger']
              and s['max']['y'] > .35 and s['min']['y'] < 2]
    foliage = [s for s in surfaces if '/Broadleaf_' in s['path'] or '/CivicGardenTree' in s['path']]
    signs = collections.Counter()
    for s in surfaces:
        if '/Karatula/' in s['path']:
            parts = s['path'].split('/')
            index = parts.index('Karatula')
            signs['/'.join(parts[:index + 2])] += 1

    overlaps = []
    for i, a in enumerate(buildings):
        for b in buildings[i + 1:]:
            dx = min(a['max']['x'], b['max']['x']) - max(a['min']['x'], b['min']['x'])
            dz = min(a['max']['z'], b['max']['z']) - max(a['min']['z'], b['min']['z'])
            if dx > .4 and dz > .4:
                overlaps.append({'a': a['path'], 'b': b['path'], 'xz_overlap_area': round(dx * dz, 2)})
    report[name] = {'active_surfaces': len(surfaces), 'building_meshes': len(buildings),
                    'sign_renderer_counts': dict(signs), 'building_aabb_candidates': overlaps}

    for view, extent in [('local', 28), ('district', 65)]:
        canvas = Image.new('RGB', (1500, 1550), '#e7e4da')
        draw = ImageDraw.Draw(canvas, 'RGBA')
        font_path = 'Assets/TumbangPreso/Resources/UI/fonts/WorkSans-Regular.ttf'
        font = ImageFont.truetype(font_path, 13)
        title_font = ImageFont.truetype(font_path, 22)
        margin, span = 100, 1300

        def point(x, z):
            return (margin + (x + extent) / (2 * extent) * span,
                    margin + (extent - z) / (2 * extent) * span)

        def tint(color, alpha):
            if color == 'none': return None
            color = color.lstrip('#')
            return tuple(int(color[i:i+2], 16) for i in (0, 2, 4)) + (round(alpha*255),)

        def footprint(s, color, edge, alpha, zorder):
            lo, hi = s['min'], s['max']
            if lo['x'] > extent or hi['x'] < -extent or lo['z'] > extent or hi['z'] < -extent:
                return
            corners = (*point(max(-extent,lo['x']),min(extent,hi['z'])),
                       *point(min(extent,hi['x']),max(-extent,lo['z'])))
            draw.rectangle(corners, fill=tint(color,alpha), outline=tint(edge,.8), width=1)

        for s in surfaces:
            if s['max']['y'] < .32 and (s['max']['x']-s['min']['x']) * (s['max']['z']-s['min']['z']) > 3:
                footprint(s, '#d0cbc0', '#c8c4ba', .45, 1)
        for s in buildings:
            footprint(s, '#bbae96', '#60533e', .9, 2)
            if view == 'local' and abs(s['position']['x']) < 26 and abs(s['position']['z']) < 26:
                draw.text(point((s['min']['x']+s['max']['x'])/2,
                                (s['min']['z']+s['max']['z'])/2),
                          s['path'].split('/')[-1], font=font, fill='#3a3327', anchor='mm')
        for s in foliage:
            footprint(s, '#688568', '#52674c', .2, 3)
        for s in solids:
            footprint(s, 'none', '#b45540', .8, 4)
        for s in surfaces:
            if 'Pisonet_Kiosk_' in s['path'] or '/PC_Express_Store/' in s['path']:
                footprint(s, '#81759a', '#53406d', .5, 5)
        cx, cy = point(0,0)
        draw.ellipse((cx-6,cy-6,cx+6,cy+6),fill='#1b7180')
        draw.text((cx+10,cy-20),'CAN',font=font,fill='#1b7180')
        spacing = 5 if view == 'local' else 10
        for tick in range(-extent//spacing*spacing,extent+1,spacing):
            if tick < -extent: continue
            tx,ty=point(tick,tick)
            draw.line((tx,margin,tx,margin+span),fill=(70,65,55,35))
            draw.line((margin,ty,margin+span,ty),fill=(70,65,55,35))
            draw.text((tx,margin+span+8),str(tick),font=font,fill='#3a3327',anchor='mt')
            draw.text((margin-12,ty),str(tick),font=font,fill='#3a3327',anchor='rm')
        draw.text((750,25),f'{name}: measured {view} footprint review',font=title_font,fill='#302c24',anchor='mt')
        draw.text((750,58),'Buildings tan | canopy bounds green | near-ground solids red | retail purple',font=font,fill='#302c24',anchor='mt')
        draw.text((750,1470),'World X / Z in metres; north is +Z. Bounds overlaps need in-engine inspection.',font=font,fill='#302c24',anchor='mt')
        canvas.save(out / f'{name}-{view}.png')

(out / 'findings.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
for name, item in report.items():
    print(name, 'buildings', item['building_meshes'], 'potential AABB overlaps',
          len(item['building_aabb_candidates']), 'sign renderers', sum(item['sign_renderer_counts'].values()))
