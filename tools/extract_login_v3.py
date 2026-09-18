"""Cut the September 18 login out of the owner's own element sheet.

⚠️⚠️ THE SHEET IS THE SOURCE AND THE COMPOSITIONS ARE ONLY THE RULER.
`login-elements.png` is her whole login drawn once with a transparent
background, so every plate here is lifted by its alpha with no matting error at
all. That is strictly better than the September 15 pass, which had to solve an
alpha against the woven background for every piece, and it is why this file is
mostly a table. 41.png and 43.png are read only to find out WHERE each piece
goes and to cut the three things the sheet does not carry.

⚠️⚠️ THE SHEET IS ALIGNED WITH THE COMPOSITION AND THAT WAS MEASURED, NOT
ASSUMED. Compositing each sheet piece onto the woven background at its own sheet
coordinates and comparing against 41.png gives 0.4% disagreeing pixels for the
logo, 0% for the rules and 5 to 8% for the fields and the tabs, and every one of
those percentages is the text she typed on top. So a piece's sheet box IS its
layout box.

⚠️⚠️ EXCEPT THE TWO BUTTONS, WHICH SHE SCALED. The green and orange plates sit
in the sheet at 389x84 and 389x85 and in the composition at 424x92 and 422x92.
Searching scale and offset together fits them at 1.090 and 1.085 with the
residual being exactly their captions, so it is a uniform scale of her art and
not a stretch. The layout carries the composition's size and the runtime draws
the sprite into it with preserveAspect.

Four pieces are not in the sheet and are matted out of the compositions with the
same solver `tools/extract_login_v2.py` uses:

    login3-eye-off   the struck-through eye, which only exists in 45.png
    login3-google    the Google plate, sign-in only
    login3-key       the key beside FORGOT PASSWORD?
    login3-tabs-pill the green pill, separated so it can slide

Run with Python/Pillow/NumPy/SciPy, then
TumbangPreso.EditorTools.OwnerMenuEditsAuthor.Prepare for the import settings.
"""
from pathlib import Path
import hashlib
import json
import numpy as np
from scipy import ndimage
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/ui/owner-ui-edits-2026-09-18'
WOVEN = ROOT / 'ArtSource/ui/owner-ui-edits-2026-09-15/login-background-woven.png'
TARGET = ROOT / 'Assets/TumbangPreso/Resources/UI/owner-menu-edits'

# Her element sheet, measured by alpha. The box is also the layout box.
SHEET = {
    'login3-logo': (759, 22, 424, 279),
    'login3-tabs': (752, 306, 393, 89),
    'login3-field-user': (675, 415, 547, 78),
    'login3-field-pass': (675, 520, 547, 78),
    'login3-field-confirm': (675, 625, 547, 78),
    'login3-primary': (763, 797, 389, 84),
    'login3-guest': (763, 928, 389, 85),
    'login3-rule-left': (671, 898, 241, 5),
    'login3-rule-right': (991, 899, 242, 6),
    'login3-checkbox': (1316, 767, 21, 24),
    'login3-invalid': (1313, 431, 29, 30),
    'login3-eye': (1648, 438, 32, 22),
}

# The pale fill inside every field plate, which is what the eye icons sit on.
FIELD_FILL = np.array([223., 212., 215.])

# The pill inside the tabs plate, measured by its own colour.
PILL = (20, 15, 181, 57)


def solve_alpha(crop, back):
    """The September 15 matting solver, unchanged, for pieces on a known ground.

    Foreground colour per edge pixel is taken from the nearest solid pixel and
    the opacity that best explains the observed blend is solved for.
    """
    foreground = np.max(np.abs(crop - back), axis=2) > 20
    labels, _ = ndimage.label(foreground)
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    foreground = labels == sizes.argmax()
    foreground = ndimage.binary_fill_holes(foreground)
    distance = ndimage.distance_transform_edt(foreground)
    core = distance >= 3
    if not core.any():
        core = foreground
    nearest = ndimage.distance_transform_edt(~core, return_distances=False, return_indices=True)
    colour = crop[nearest[0], nearest[1]]
    direction = colour - back
    denominator = np.sum(direction * direction, axis=2)
    opacity = np.divide(np.sum((crop - back) * direction, axis=2), denominator,
                        out=np.zeros_like(denominator), where=denominator > 1)
    error = np.max(np.abs(back + opacity[:, :, None] * direction - crop), axis=2)
    near = ndimage.distance_transform_edt(~foreground) <= 2
    edge = near & (distance < 3) & (opacity > 0) & (opacity < .98) & (error < 6)
    alpha = np.where(foreground, 255, 0).astype(np.uint8)
    colours = crop.copy()
    alpha[edge] = np.rint(np.clip(opacity[edge], 0, 1) * 255).astype(np.uint8)
    colours[edge] = colour[edge]
    return np.dstack((np.uint8(np.clip(colours, 0, 255)), alpha))


def save(name, rgba, entries, x, y, width=None, height=None, note=''):
    path = TARGET / (name + '.png')
    Image.fromarray(rgba).save(path)
    entries.append({
        'name': name, 'x': int(x), 'y': int(y),
        'width': int(width if width is not None else rgba.shape[1]),
        'height': int(height if height is not None else rgba.shape[0]),
        'sourceWidth': int(rgba.shape[1]), 'sourceHeight': int(rgba.shape[0]),
        'faceX': float((width if width is not None else rgba.shape[1]) / 2),
        'faceY': float((height if height is not None else rgba.shape[0]) / 2),
        'note': note,
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    })
    print(f"  {name:<22} {rgba.shape[1]}x{rgba.shape[0]} -> "
          f"{entries[-1]['width']}x{entries[-1]['height']} at {x},{y}")


def tabs_pieces(sheet, entries):
    """The track and the pill, separated so the pill can slide between halves.

    ⚠️ THE TRACK'S LEFT END IS MIRRORED FROM ITS RIGHT END, WHICH IS THE ONLY
    PLACE IN THIS FILE THAT DOES NOT SHIP HER PIXELS EXACTLY AS DRAWN. The pill
    covers the left 201 columns in the only plate she supplied, so no other copy
    of that end cap exists in the sheet. Mirroring the right 192 leaves nine
    columns in the flat middle of the trough, which are blended between the two
    real neighbours. The alternative was baking the pill in and cross-fading two
    plates, which cannot slide, and sliding is what the control depicts.
    """
    x, y, w, h = SHEET['login3-tabs']
    plate = np.array(sheet.crop((x, y, x + w, y + h))).astype(float)
    px, py, pw, ph = PILL

    # The pill, matted against the trough it lies in.
    pad = 3
    box = plate[py - pad:py + ph + pad, px - pad:px + pw + pad]
    trough = np.median(plate[10:h - 10, pw + 40:w - 12, :3].reshape(-1, 3), axis=0)
    pill = solve_alpha(box[..., :3], np.tile(trough, box.shape[:2] + (1,)))
    save('login3-tabs-pill', pill, entries, x + px - pad, y + py - pad,
         note='slides between the two halves of login3-tabs-track')

    # The track, with the pill's footprint rebuilt from the far end.
    track = plate.copy()
    right = track[:, px + pw:w]
    mirrored = right[:, ::-1]
    take = min(mirrored.shape[1], px + pw)
    track[:, :take] = mirrored[:, mirrored.shape[1] - take:]
    gap_start, gap_end = take, px + pw
    if gap_end > gap_start:
        left_edge = track[:, gap_start - 1:gap_start]
        right_edge = track[:, gap_end:gap_end + 1]
        ramp = np.linspace(0, 1, gap_end - gap_start + 2)[1:-1][None, :, None]
        track[:, gap_start:gap_end] = left_edge * (1 - ramp) + right_edge * ramp
    save('login3-tabs-track', np.uint8(np.clip(track, 0, 255)), entries, x, y,
         note='pill footprint rebuilt from the mirrored far end')


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(SOURCE / 'login-elements.png').convert('RGBA')
    woven = np.array(Image.open(WOVEN).convert('RGB')).astype(float)
    signup = np.array(Image.open(SOURCE / '41.png').convert('RGB')).astype(float)
    signin = np.array(Image.open(SOURCE / '43.png').convert('RGB')).astype(float)
    filled = np.array(Image.open(SOURCE / '45.png').convert('RGB')).astype(float)
    entries = []

    print('from her element sheet:')
    # The two action plates are the one scaled pair; everything else is 1:1.
    placed = {'login3-primary': (424, 92), 'login3-guest': (422, 92)}
    for name, (x, y, w, h) in SHEET.items():
        rgba = np.array(sheet.crop((x, y, x + w, y + h)))
        width, height = placed.get(name, (w, h))
        note = 'uniform 1.09 scale measured against her composition' if name in placed else ''
        save(name, rgba, entries, x, y, width, height, note)

    print('separated so it can move:')
    tabs_pieces(sheet, entries)

    print('matted out of the compositions, not in the sheet:')
    # ⚠️ ONE BOX FOR BOTH EYES. The open eye measures 31x21 and the struck one
    # 26x21 at a slightly different centre, so cutting each to its own bounds
    # would make the icon jump when the player toggles it. A shared box means
    # swapping the sprite swaps only the drawing.
    eye_box = (1155, 544, 45, 34)
    ex, ey, ew, eh = eye_box
    ground = np.tile(FIELD_FILL, (eh, ew, 1))
    save('login3-eye-open', solve_alpha(signin[ey:ey + eh, ex:ex + ew], ground), entries, ex, ey,
         note='shares its box with login3-eye-shut so the toggle does not jump')
    save('login3-eye-shut', solve_alpha(filled[ey:ey + eh, ex:ex + ew], ground), entries, ex, ey,
         note='shares its box with login3-eye-open')

    gx, gy, gw, gh = 746, 845, 424, 93
    save('login3-google', solve_alpha(signin[gy:gy + gh, gx:gx + gw], woven[gy:gy + gh, gx:gx + gw]),
         entries, gx, gy, note='sign-in only')

    kx, ky, kw, kh = 683, 636, 31, 28
    save('login3-key', solve_alpha(signin[ky:ky + kh, kx:kx + kw], woven[ky:ky + kh, kx:kx + kw]),
         entries, kx, ky, note='beside FORGOT PASSWORD?')

    (TARGET / 'login-layout-v3.json').write_text(
        json.dumps({'source': 'ArtSource/ui/owner-ui-edits-2026-09-18', 'pieces': entries}, indent=2),
        encoding='utf-8')
    print(f'\nlogin-layout-v3.json  {len(entries)} pieces')


if __name__ == '__main__':
    main()
