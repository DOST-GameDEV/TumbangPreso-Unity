"""Exports the live match HUD's textures from the drawing functions in boot_match.py into
Assets/TumbangPreso/Resources/UI/live-hud, so the game uses the pictures the boards were drawn with.

    py -3 tools/ui_artboards/export_live_hud.py

Shapes that the game recolours (the soft bands, the ring glows) are exported white with their full
strength in the alpha; the game tints them and sets the strength. Pieces drawn at twice the board
size say so in the game's loader.
"""
import hashlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import boot_match as bm  # noqa: E402

OUT = bm.UI / "live-hud"
META = (bm.UI / "boot-kit" / "thin-go.png.meta").read_text(encoding="utf-8")
WHITE = (255, 255, 255)


def save(name, im):
    path = OUT / (name + ".png")
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)
    meta = path.with_name(path.name + ".meta")
    if not meta.exists():
        guid = hashlib.md5(("live-hud/" + name).encode()).hexdigest()
        lines = META.splitlines()
        lines[1] = "guid: " + guid
        meta.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(name, im.size)


def main():
    # The timer's lime pill with its slab thinned, at twice the board size.
    save("timer-fill", bm.thin_pill(716, 104, keep=8))
    # The timer's track in dark grey, at the kit track's own size so the game slices it as it slices the kit's.
    t = bm.art("login3-tabs-track").convert("RGBA")
    src = t.convert("L")
    a_ = bm.np.asarray(src)[bm.np.asarray(t.getchannel("A")) > 128]
    lo, hi = float(bm.np.percentile(a_, 3)), float(bm.np.percentile(a_, 97))
    lum = src.point(lambda v: int(28 + max(0, min(1, (v - lo) / max(1, hi - lo))) * 92))
    save("timer-track", bm.Image.merge("RGBA", (lum, lum, lum.point(lambda v: min(255, v + 5)), t.getchannel("A"))))
    # Short tags: the thin button's two ends, cross-faded over a little middle, because squeezing its long middle smears.
    for kind in ("go", "yellow"):
        im = bm.np.asarray(bm.Image.open(OUT.parent / "boot-kit" / ("thin-%s.png" % kind)).convert("RGBA")).astype(float)
        w, cap, mid = im.shape[1], 235, 40
        fade = bm.np.linspace(0, 1, mid)[None, :, None]
        middle = im[:, cap:cap + mid] * (1 - fade) + im[:, w - cap - mid:w - cap] * fade
        row = bm.np.concatenate([im[:, :cap], middle, im[:, w - cap:]], axis=1)
        save("tag-" + kind, bm.Image.fromarray(row.round().astype("uint8"), "RGBA"))
    # One soft band for every notice and for the score corner.
    save("band", bm.glow_band_art(1024, 128, peak=255, colour=WHITE))
    save("ring-glow-skill", bm.ring_glow_art(352, 192, colour=WHITE, peak=255))
    save("ring-glow-ult", bm.ring_glow_art(472, 252, colour=WHITE, peak=255))
    # A stand-in can for CAN DOWN until a real render exists.
    save("can", bm.can_art(168))
    # The woven plates for other players' scores, Tab and the feed, at the boards' own size. The seat colour strip is
    # drawn by the game over the plate's first nine pixels, so one grey plate serves every seat; the game slices it
    # (a fixed hero section and lean end, a stretching middle) to the length each row needs.
    save("plate-grey", bm.edge_plate_art(276, 58, (0x80, 0x80, 0x84)))
    save("feed-plate", bm.feed_plate_art(372, 58, section=232))
    # The same plate with a shorter dark section, for a row with one hero (a cast, a restore, a penalty).
    save("feed-plate-short", bm.feed_plate_art(312, 58, section=172))
    # A seat's soft glow, white with full strength in the alpha; the game tints and clips it to the feed's dark section.
    save("seat-glow", bm.seat_glow_art(130, WHITE, peak=255))
    # The hand-cut white rim and pointed tip round a status that has just landed.
    save("rim-cut", bm.cut_rim_art(99))
    heroes()
    held()
    abilities()


def heroes():
    """Every approved portrait, trimmed to its ink and redrawn at twice the board size for the places it stands:
    `hero-plate` on the score plates (one height, so every hero centres the same way) and `hero-corner` with the
    crisp dark stroke in your score corner. The portraits themselves are never changed."""
    for path in sorted((bm.UI / "portraits").glob("*.png")):
        name = path.stem
        art = bm.trim(bm.Image.open(path).convert("RGBA"))
        k = 2 * (58 - 10) / art.height
        save("hero-plate/" + name, art.resize((max(1, round(art.width * k)), 2 * (58 - 10)), bm.Image.LANCZOS))
        corner = art.copy()
        corner.thumbnail((248, 248), bm.Image.LANCZOS)
        save("hero-corner/" + name, bm.stroked(corner, width=8))


def held():
    """The flat thing held in the score corner, tilted as the board tilts it and stroked, at twice the board size."""
    ids = {p.stem for p in (bm.UI / "slippers-flat").glob("*.png")} | {p.stem for p in (bm.UI / "portraits").glob("*.png")}
    for name in sorted(ids):
        flat_path = bm.UI / "slippers-flat" / (name + ".png")
        path = flat_path if flat_path.exists() else bm.UI / "portraits" / (name + ".png")
        flat = bm.trim(bm.Image.open(path).convert("RGBA"))
        flat.thumbnail((340, 340), bm.Image.LANCZOS)
        flat = flat.rotate(-20, bm.Image.BICUBIC, expand=True)
        save("held/" + name, bm.stroked(flat, width=8))


if __name__ == "__main__":
    main()


def abilities():
    """The round abilities and the F1 reference plates (owner, 2026-11-07), at twice the board size. The disc is drawn
    grey; the game lays the coloured edge (`disc-edge-*`, white, tinted gold or lime and filled round) and the
    cooldown's grey (`disc-inner-*`, white, filled from the bottom) over it. The plate and its header box are
    separate so a plate of any height keeps one header: `ref-head-cut` has the clean circle taken out round the mini
    disc (its centre 6 from the plate's left and 68 from its top, at board size), `ref-head` is whole, for the page."""
    from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps
    K = 2
    for name, d in (("skill", 112), ("ult", 148)):
        save("disc-" + name, bm.disc_plate_art(d * K, 0, (0, 0, 0), edge=5 * K))
        S, D = 3, d * K * 3
        body = Image.new("L", (D, D)); ImageDraw.Draw(body).ellipse((0, 0, D - 1, D - 1), fill=255)
        r = 5 * K * S
        inner = Image.new("L", (D, D)); ImageDraw.Draw(inner).ellipse((r, r, D - 1 - r, D - 1 - r), fill=255)
        edge = Image.new("RGBA", (D, D), (255, 255, 255, 0)); edge.putalpha(ImageChops.subtract(body, inner))
        save("disc-edge-" + name, edge.resize((d * K, d * K), Image.LANCZOS))
        cool = Image.new("RGBA", (D, D), (255, 255, 255, 0)); cool.putalpha(inner)
        save("disc-inner-" + name, cool.resize((d * K, d * K), Image.LANCZOS))
    # Plates at board size: the weave is cut at its own scale, and a crop twice as tall would run off its source.
    W, H = 600, 200
    save("ref-plate", bm.ref_plate_art(W, H, 0))
    save("ref-head-cut", header_box(W, bm.REF_HEAD, cut=(6, 68, bm.REF_DISC / 2 + bm.REF_GAP)))
    save("ref-head", header_box(W, bm.REF_HEAD, whole=True))


def header_box(w, head, cut=None, whole=False, rad=14):
    """The plate's nearly solid dark box alone, with its inner shadow, inside the plate's 3-unit stroke. `whole` rounds
    its bottom corners too (a plate that is all header)."""
    from PIL import Image, ImageChops, ImageDraw, ImageFilter
    S = 3
    h = head + (0 if whole else 12)
    W, H = w * S, h * S
    inner = Image.new("L", (W, H))
    bottom = (head if whole else head + 40) * S
    ImageDraw.Draw(inner).rounded_rectangle((3 * S, 3 * S, W - 1 - 3 * S, bottom - 1 - 3 * S), (rad - 3) * S, fill=255)
    bx = Image.new("L", (W, H))
    ImageDraw.Draw(bx).rectangle((0, 0, W, head * S), fill=255)
    if cut:
        cx, cy, r = cut
        ImageDraw.Draw(bx).ellipse(((cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S), fill=0)
    bx = ImageChops.multiply(bx, inner)
    im = Image.new("RGBA", (W, H), (0x2C, 0x2B, 0x35, 0))
    im.putalpha(bx.point(lambda v: int(v * .9)))
    shadow = ImageChops.invert(ImageChops.offset(bx, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    sh = ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), bx)
    dark = Image.new("RGBA", (W, H), (0x08, 0x07, 0x0C, 255)); dark.putalpha(sh)
    im.alpha_composite(dark)
    return im.resize((w, h), Image.LANCZOS)
