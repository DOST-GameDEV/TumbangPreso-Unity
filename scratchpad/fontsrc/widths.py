# Does moving a row to the body face make it WIDER? That is 133.3's silent failure:
# MenuKit.Label overflows rather than wrapping, so a string that grew just draws over
# its neighbour and no probe that only checks "is the label on screen" can see it.
from fontTools.ttLib import TTFont

DAR = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"
NUN = "scratchpad/fontsrc/Nunito-Regular.ttf"
NUNB = "scratchpad/fontsrc/Nunito-Bold.ttf"

def advance(path, s):
    f = TTFont(path)
    upm = f["head"].unitsPerEm
    cmap = f.getBestCmap()
    hmtx = f["hmtx"]
    total = 0
    for ch in s:
        gn = cmap.get(ord(ch))
        if gn is None:
            gn = cmap.get(ord(" "))
        total += hmtx[gn][0]
    return total / upm      # in em widths

STRINGS = [
    "Master volume",
    "MASTER VOLUME",
    "Applies to every sound in the game, including the menus.",
    "PRESS START TO HOST A GAME",
    "SEARCHING FOR A MATCH",
    "Slams the ground and knocks every attacker back three metres.",
    "CHOOSE YOUR HERO",
    "EQUIPPED",
    "Invert Y",
    "BACK TO LOBBY",
    "Type a four-character code or an address, or pick a game below.",
]

print(f"{'string':64s} {'Daruma':>8s} {'Nunito':>8s} {'delta':>8s} {'NunBold':>8s} {'delta':>8s}")
print("-" * 108)
worst = 0.0
for s in STRINGS:
    d, n, b = advance(DAR, s), advance(NUN, s), advance(NUNB, s)
    dn = (n - d) / d * 100.0
    db = (b - d) / d * 100.0
    worst = max(worst, dn, db)
    label = s if len(s) <= 62 else s[:59] + "..."
    print(f"{label:64s} {d:8.2f} {n:8.2f} {dn:+7.1f}% {b:8.2f} {db:+7.1f}%")

print("-" * 108)
print(f"WORST GROWTH ANY STRING SEES: {worst:+.1f}%")
print()
print("Read this as: a label that exactly filled its box in Darumadrop now needs this much")
print("more room. Anything positive is a candidate overflow; MenuKit.Fit shrinks to the")
print("18-unit floor and then reports false, so the ones that matter are the ones with no")
print("Fit call and a fixed box.")
