"""The CALM pilot's review sheets, from the shots of tools/payo_pieces/calm_review.py.

  python tools/payo_pieces/calm_sheet.py piece <name> <shots dir> <out.png> [note]
  python tools/payo_pieces/calm_sheet.py together <shots dir> <out.png> [note]
"""
import sys

from PIL import Image, ImageDraw, ImageFont


def font(n, bold=True):
    try:
        return ImageFont.truetype("arialbd.ttf" if bold else "arial.ttf", n)
    except OSError:
        return ImageFont.load_default()


def grid(rows, title, out, note=""):
    """rows: [(label, [(caption, path), ...])]"""
    ims = [[(c, Image.open(p).convert("RGB")) for c, p in cells] for _, cells in rows]
    w, h = ims[0][0][1].size
    ncol = max(len(r) for r in ims)
    bar, lab = 56, 34
    sheet = Image.new("RGB", (ncol * w + 8 * (ncol - 1), bar + len(rows) * (h + lab)), (22, 26, 32))
    d = ImageDraw.Draw(sheet)
    d.text((14, 12), title, font=font(28), fill=(242, 236, 220))
    d.text((14 + 17 * len(title), 20), note, font=font(17, False), fill=(196, 206, 206))
    for r, ((label, _), cells) in enumerate(zip(rows, ims)):
        y = bar + r * (h + lab)
        for k, (cap, im) in enumerate(cells):
            x = k * (w + 8)
            d.text((x + 10, y + 7), "%s   %s" % (label, cap), font=font(18), fill=(236, 236, 228))
            sheet.paste(im.resize((w, h)), (x, y + lab))
    sheet.save(out)
    print("SHEET", out)


if __name__ == "__main__":
    a = sys.argv[1:]
    if a[0] == "piece":
        name, shots, out = a[1], a[2], a[3]
        note = a[4] if len(a) > 4 else ""
        S = lambda k: "%s/%s_%s.png" % (shots, name, k)
        rows = [(dist, [("OLD (approved)", S("old_" + dist)), ("CALM", S("calm_" + dist))]) for dist in ("8m", "40m", "120m")]
        rows.append(("8m", [("CALM, normal map OFF (albedo only)", S("calm_8m_nonormal")), ("CALM normal map alone, on flat grey", S("calm_8m_normalonly"))]))
        grid(rows, name.upper() + "  old vs calm", out, note or "warm late-afternoon light, same camera; calm shots have the normal map on")
    else:
        shots, out = a[1], a[2]
        note = a[3] if len(a) > 3 else ""
        rows = [(dist, [("OLD three", "%s/together_old_%s.png" % (shots, dist)), ("CALM three", "%s/together_calm_%s.png" % (shots, dist))]) for dist in ("60m", "150m")]
        grid(rows, "PILOT: house tower, gate portal, monolith together", out, note)
