"""A CALMER copy of the approved pieces' painted textures (owner, 2026-10-17: "the textures of everything, when put
together look too noisy. it doesnt fit our stylized hand illustrated design").

The approved pieces are not touched. This writes copies to Logs/payo/pieces/calm/<piece>.glb whose atlases are
simplified the way a painter would: small speckle, moss dots, stain flecks and fine cracks are taken out (an
edge-keeping smoothing that keeps the large shapes, the carvings' outlines and the ink), colours are pulled into larger
flat areas, and the paint is lifted a little toward the soft, saturated, gradient look of the references.

Three steps, the middle one in system Python (Blender's Python has no Pillow):
  blender -b --python tools/payo_calm.py -- export <piece> ...     atlases out to Logs/payo/pieces/calm/raw/
  py -3 tools/payo_calm.py calm [strength]                          raw/ -> soft/
  blender -b --python tools/payo_calm.py -- pack <piece> ...        glbs with the soft atlases -> Logs/payo/pieces/calm/
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CALM = os.path.join(ROOT, "Logs", "payo", "pieces", "calm")
RAW, SOFT = os.path.join(CALM, "raw"), os.path.join(CALM, "soft")


def calm(strength=1.0):
    import numpy as np
    from PIL import Image, ImageFilter
    os.makedirs(SOFT, exist_ok=True)
    for name in sorted(os.listdir(RAW)):
        im = Image.open(os.path.join(RAW, name)).convert("RGB")
        w, h = im.size
        # 1. take out the speckle: median, then an edge-keeping smooth (several small median passes keep outlines)
        k = 5 if max(w, h) >= 1024 else 3
        sm = im
        for _ in range(int(2 * strength) + 1):
            sm = sm.filter(ImageFilter.MedianFilter(k))
        sm = sm.filter(ImageFilter.ModeFilter(k))
        # 2. pull colours into larger flat areas: quantise softly in a perceptual-ish space, then blend back a little
        q = sm.quantize(colors=48, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
        a = np.asarray(sm, np.float32) / 255.0
        b = np.asarray(q, np.float32) / 255.0
        c = a * 0.35 + b * 0.65
        # 3. a soft gradient in place of texture noise: a very wide blur of the result, mixed in lightly
        wide = np.asarray(Image.fromarray((c * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(max(w, h) / 160.0)), np.float32) / 255.0
        c = c * 0.82 + wide * 0.18
        # 4. a touch more saturation and lift, so the paint reads as illustration, not as worn surface
        grey = c.mean(axis=2, keepdims=True)
        c = np.clip(grey + (c - grey) * 1.12, 0, 1)
        c = np.clip(c * 1.03 + 0.01, 0, 1)
        Image.fromarray((c * 255 + 0.5).astype(np.uint8)).save(os.path.join(SOFT, name))
        print("calmed", name, im.size)


if __name__ == "__main__" and "bpy" not in sys.modules:
    try:
        import bpy  # noqa: F401
    except ImportError:
        bpy = None
    if bpy is None:
        if len(sys.argv) > 1 and sys.argv[1] == "calm":
            calm(float(sys.argv[2]) if len(sys.argv) > 2 else 1.0)
        sys.exit(0)

import bpy  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
mode, pieces = args[0], args[1:]
A = os.path.join(ROOT, "ArtSource", "payo", "pieces-approved-2026-10-12")
B = os.path.join(ROOT, "Logs", "payo", "pieces", "build")


def find(name):
    for d in os.listdir(A):
        p = os.path.join(A, d, name + ".glb")
        if os.path.exists(p):
            return p
    for d in ("stone", "terrace"):
        p = os.path.join(B, d, name + ".glb")
        if os.path.exists(p):
            return p
    raise FileNotFoundError(name)


os.makedirs(RAW, exist_ok=True)
for piece in pieces:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=find(piece))
    imgs = [im for im in bpy.data.images if im.size[0] > 0]
    for k, im in enumerate(imgs):
        fn = "%s__%d.png" % (piece, k)
        if mode == "export":
            im.filepath_raw = os.path.join(RAW, fn); im.file_format = "PNG"; im.save()
        else:
            soft = os.path.join(SOFT, fn)
            if os.path.exists(soft):
                im.filepath = soft; im.reload(); im.pack()
    if mode == "pack":
        bpy.ops.export_scene.gltf(filepath=os.path.join(CALM, piece + ".glb"), export_format="GLB")
    print("DONE", mode, piece, len(imgs))
