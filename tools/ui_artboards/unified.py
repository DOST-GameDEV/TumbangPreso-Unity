"""The unified UI style for the revamp artboards (owner, 2026-10-30).

Two type faces only: Darumadrop One for display, Grandstander for everything else
(SemiBold for labels, buttons, tabs and numbers; Regular for sentences). The dark knit
plates (HOME's maroon name plate and wine mode card, tiled across large areas, plus the
rose grain laid over them) become a flat fill in the plate's own colour with the subtle
halftone print the Replays screen stands on. A full-screen dark knit ground becomes that
screen's whole ground: deep maroon, a soft bloom and the halftone. The bright knit pieces
are the owner's HOME art and stay as they are.
"""
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

DISPLAY = "DarumadropOne-Regular"
STRONG, BODY = "GrandstanderRoman-SemiBold", "GrandstanderRoman-Regular"

DARK_FACES = ("name_plate_face_knit", "mode_card_face_knit")
DARK_SLABS = ("name_plate_slab_knit", "mode_card_slab_knit")
GRAIN = "knit_grain"

# The Replays board (`HubScenery.Wash`): deep ground, bloom, a 34-unit halftone at 30 degrees.
GROUND, BLOOM = (0x2A, 0x06, 0x11), (0x7A, 0x18, 0x30)
PITCH, ANGLE = 34, 0.52

# Every slab sits this far straight below its face (owner, 2026-10-30).
DROP = 12


def text_face(layer):
    """Grandstander weight for a label; Darumadrop stays Darumadrop."""
    if layer["font"] == DISPLAY:
        return DISPLAY
    if layer["font"] in ("DINNextLTArabic-Light", "Nunito-Regular", "WorkSans-Regular"):
        return BODY
    words = layer["text"].split()
    sentence = any(c.islower() for c in layer["text"]) and len(words) >= 4
    return BODY if sentence else STRONG


def halftone(size, dot, strength):
    """The print: dots on a 34-unit screen, swelling toward the lower right."""
    w, h = size
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    cs, sn = math.cos(ANGLE), math.sin(ANGLE)
    reach = int(math.hypot(w, h))
    for u in range(-reach, reach, PITCH):
        for v in range(-reach, reach, PITCH):
            x = w / 2 + u * cs - v * sn
            y = h / 2 + u * sn + v * cs
            if not (0 <= x < w and 0 <= y < h):
                continue
            k = min(1.0, max(0.0, (x / w * 0.6 + y / h * 0.4 - 0.35) / 0.65))
            r = PITCH * 0.42 * k
            if r >= 1.2:
                draw.ellipse((x - r, y - r, x + r, y + r), fill=dot + (int(255 * strength),))
    return image


def plate_colour(path):
    """The plate's own colour: the median of its opaque pixels."""
    pixels = np.asarray(Image.open(path).convert("RGBA")).reshape(-1, 4)
    solid = pixels[pixels[:, 3] > 200][:, :3]
    if len(solid) == 0:
        return (0x56, 0x07, 0x1D)
    return tuple(int(c) for c in np.median(solid, axis=0))


def flat(path, colour, target):
    """The plate's silhouette filled with one colour, keeping its soft edge."""
    source = Image.open(path).convert("RGBA")
    filled = Image.new("RGBA", source.size, colour + (255,))
    filled.putalpha(source.getchannel("A"))
    filled.save(target)


def wash(target_dir, w, h):
    """The Replays ground at full screen: three layers a designer can tune separately."""
    ground = Image.new("RGBA", (w, h), GROUND + (255,))
    ground.save(target_dir / "wash_ground.png")
    mask = Image.new("L", (w, h), 0)
    cx, cy = w * 0.5, h * 0.45
    ImageDraw.Draw(mask).ellipse((cx - 1300, cy - 750, cx + 1300, cy + 750), fill=int(255 * 0.85))
    bloom = Image.new("RGBA", (w, h), BLOOM + (255,))
    bloom.putalpha(mask.filter(ImageFilter.GaussianBlur(260)))
    bloom.save(target_dir / "wash_bloom.png")
    halftone((w, h), (0x14, 0x02, 0x08), 0.55 * 0.18 * 3).save(target_dir / "wash_halftone.png")


def face_after(layers, n):
    """The face a slab sits under: the next knit face on the same object."""
    for later in layers[n + 1:n + 4]:
        if later.get("knit", "").endswith("_face_knit") and later["path"] == layers[n]["path"]:
            return later
    return None


def drop_slab(slab, face, colour, target):
    """The face's own outline, DROP units straight down: the slab's knit where it reaches,
    its colour where the game's slab never drew."""
    shape = Image.open(face["file"]).convert("RGBA").getchannel("A")
    w, h = shape.size
    knit = Image.open(slab["file"]).convert("RGBA")
    base = Image.new("RGBA", (w, h), colour + (255,))
    patch = knit.crop((face["x"] - slab["x"], face["y"] + DROP - slab["y"],
                       face["x"] - slab["x"] + w, face["y"] + DROP - slab["y"] + h))
    base.alpha_composite(patch)
    base.putalpha(shape)
    base.save(target)
    return dict(slab, file=str(target), x=face["x"], y=face["y"] + DROP, w=w, h=h)


def restyle(layers, board_dir):
    """Rewrites one artboard's layer list in the unified style. Returns the new list."""
    work = Path(board_dir) / "unified"
    work.mkdir(exist_ok=True)
    result = []
    for n, layer in enumerate(layers):
        knit = layer.get("knit", "")
        face = face_after(layers, n) if knit.endswith("_slab_knit") else None
        if face is not None and not (face["w"] >= 1900 and face["h"] >= 1000):
            # Every slab drops straight down instead of out to the lower right.
            dark = knit in DARK_SLABS
            colour = plate_colour(layer["file"])
            target = work / ("slab%03d.png" % n)
            dropped = drop_slab(layer, face, colour, target)
            if dark:
                flat(target, colour, target)
                dropped["name"] = layer["name"] + " (flat)"
            result.append(dropped)
            continue
        if layer["kind"] == "text":
            layer["ps"] = text_face(layer)
            layer["fit"] = "shrink" if layer["ps"] != DISPLAY else "match"
            layer["style"] = layer["style"].replace("Bold", "").replace("And", "") or "Normal"
            result.append(layer)
            continue
        knit = layer.get("knit", "")
        if layer["kind"] != "raster" or not knit:
            result.append(layer)
            continue
        if knit.startswith(GRAIN):
            continue
        full = layer["w"] >= 1900 and layer["h"] >= 1000
        if knit in DARK_SLABS and full:
            continue
        if knit in DARK_FACES and full:
            if not (work / "wash_ground.png").exists():
                wash(work, 1920, 1080)
            for part, name in (("wash_ground", "Ground"), ("wash_bloom", "Bloom"), ("wash_halftone", "Halftone print")):
                result.append(dict(layer, file=str(work / (part + ".png")), x=0, y=0, w=1920, h=1080, name=name, knit=""))
            continue
        if knit in DARK_FACES or knit in DARK_SLABS:
            colour = plate_colour(layer["file"])
            fill = work / ("fill%03d.png" % n)
            flat(layer["file"], colour, fill)
            result.append(dict(layer, file=str(fill), name=layer["name"] + " (flat)"))
            if knit in DARK_FACES:
                dot = tuple(int(c * 0.55) for c in colour)
                tone = work / ("print%03d.png" % n)
                halftone((layer["w"], layer["h"]), dot, 0.45).save(tone)
                result.append(dict(layer, file=str(tone), name="Halftone print", clip=True))
            continue
        result.append(layer)
    return result
