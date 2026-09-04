"""
Desaturate a frame at Rec. 601, for docs/TODO.md 127 and VISION.md 16.1's second-channel test:
"if the taya cannot be picked out of a desaturated frame, the second channel is not there yet."

WHY REC. 601 AND NOT REC. 709: AbilityShowcaseProbe measures luminance with 0.299/0.587/0.114,
and this is the same question asked of the same frames. Two weightings would mean a marker could
pass the probe's bound and fail the picture for a reason that is arithmetic rather than design.

WHY A SCRIPT RATHER THAN AN EYE: a colourblind reader is not simulated by "looking at it in
black and white" from memory. The output is the frame with hue removed and nothing else changed,
so the only thing left to judge is whether the SHAPE and the VALUE separate the two roles.

CLAUDE.md 6.1: version the filename every time. This writes <name>-grey.png beside the source
and refuses to overwrite unless asked, because a chat client caches by filename and a silently
overwritten render means the whole review is conducted against an image that no longer exists.

    python scratchpad/greyscale.py Logs/shots-play/role-markers-v1.png
"""

import os
import struct
import sys
import zlib


def _unfilter(raw, width, height, bpp):
    """Undo PNG per-scanline filters. Returns a flat bytearray of RGB(A) rows."""
    stride = width * bpp
    out = bytearray(stride * height)
    pos = 0

    for y in range(height):
        ftype = raw[pos]
        pos += 1
        line = raw[pos:pos + stride]
        pos += stride

        row = out[y * stride:(y + 1) * stride]
        prev = out[(y - 1) * stride:y * stride] if y else bytearray(stride)

        for x in range(stride):
            a = row[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            v = line[x]

            if ftype == 0:
                row[x] = v
            elif ftype == 1:
                row[x] = (v + a) & 0xFF
            elif ftype == 2:
                row[x] = (v + b) & 0xFF
            elif ftype == 3:
                row[x] = (v + ((a + b) >> 1)) & 0xFF
            elif ftype == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                row[x] = (v + pr) & 0xFF
            else:
                raise ValueError("unknown PNG filter %d on row %d" % (ftype, y))

        out[y * stride:(y + 1) * stride] = row

    return out


def _chunks(data):
    pos = 8
    while pos < len(data):
        (length,) = struct.unpack(">I", data[pos:pos + 4])
        kind = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        yield kind, body
        pos += 12 + length


def _write_png(path, width, height, bpp, pixels):
    stride = width * bpp
    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter 0, no prediction: the file is read once, not shipped
        raw += pixels[y * stride:(y + 1) * stride]

    def chunk(kind, body):
        return (struct.pack(">I", len(body)) + kind + body
                + struct.pack(">I", zlib.crc32(kind + body) & 0xFFFFFFFF))

    colour = 6 if bpp == 4 else 2
    head = struct.pack(">IIBBBBB", width, height, 8, colour, 0, 0, 0)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", head))
        f.write(chunk(b"IDAT", zlib.compress(bytes(raw), 9)))
        f.write(chunk(b"IEND", b""))


def desaturate(src, dst=None, force=False):
    with open(src, "rb") as f:
        data = f.read()

    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit("%s is not a PNG" % src)

    idat = bytearray()
    width = height = bpp = None

    for kind, body in _chunks(data):
        if kind == b"IHDR":
            width, height, depth, colour = struct.unpack(">IIBB", body[:10])
            if depth != 8 or colour not in (2, 6):
                raise SystemExit(
                    "only 8-bit RGB/RGBA PNGs are handled; this one is depth %d colour %d. "
                    "The capture pipeline writes RGBA, so a different shape means the frame "
                    "did not come from Render()." % (depth, colour))
            bpp = 3 if colour == 2 else 4
        elif kind == b"IDAT":
            idat += body
        elif kind == b"IEND":
            break

    pixels = _unfilter(zlib.decompress(bytes(idat)), width, height, bpp)

    for i in range(0, len(pixels), bpp):
        r, g, b = pixels[i], pixels[i + 1], pixels[i + 2]
        y = int(0.299 * r + 0.587 * g + 0.114 * b + 0.5)
        y = 0 if y < 0 else (255 if y > 255 else y)
        pixels[i] = pixels[i + 1] = pixels[i + 2] = y

    if dst is None:
        root, ext = os.path.splitext(src)
        dst = root + "-grey" + ext

    if os.path.exists(dst) and not force:
        raise SystemExit(
            "%s already exists. CLAUDE.md 6.1: version the filename rather than overwriting a "
            "render, because chat clients cache by name and the review would be conducted "
            "against an image that is no longer on disk. Pass --force if you mean it." % dst)

    _write_png(dst, width, height, bpp, pixels)
    print("%s  ->  %s   (%dx%d, Rec. 601)" % (src, dst, width, height))
    return dst


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if a != "--force"]
    if not args:
        raise SystemExit(__doc__)

    desaturate(args[0],
               args[1] if len(args) > 1 else None,
               force="--force" in sys.argv)
