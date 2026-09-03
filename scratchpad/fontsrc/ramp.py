# Derives the paper ramp from the measured brand colours, and prints the contrast ratios so
# the numbers written into UiTheme are measured rather than eyeballed. CLAUDE.md 6.4's own
# receipt is a near-black NAVY that looked black in a code review for the life of the file.
def hx(s): return tuple(int(s[i:i+2],16) for i in (0,2,4))
def hs(t): return "%02x%02x%02x" % tuple(max(0,min(255,round(c))) for c in t)
def lighten(c,t): return tuple(v+(255-v)*t for v in c)
def darken(c,t):  return tuple(v*(1-t) for v in c)

def lum(c):
    def f(v):
        v/=255.0
        return v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4
    r,g,b=[f(v) for v in c]
    return 0.2126*r+0.7152*g+0.0722*b

def ratio(a,b):
    la,lb=lum(a),lum(b)
    hi,lo=max(la,lb),min(la,lb)
    return (hi+0.05)/(lo+0.05)

HONEY = hx("fcd39f"); RED = hx("980715"); ARMY = hx("b3a828")
CHART = hx("d6ce01"); PERS = hx("fd8041"); GOLD = hx("f5b521")

ramp = {
    "Paper      (page)":      lighten(HONEY,0.55),
    "PaperWarm  (recessed)":  lighten(HONEY,0.28),
    "PaperEdge  (halo)":      HONEY,
    "PaperSunk  (pressed)":   darken(HONEY,0.12),
}
inks = {
    "PaperInk      d45": darken(RED,0.45),
    "PaperInk      d55": darken(RED,0.55),
    "PaperInkSoft  d20": darken(RED,0.20),
    "PaperInkSoft  d05": darken(RED,0.05),
}
stage = {
    "Stage (char select ground)": darken(ARMY,0.72),
    "StageWarm":                  darken(ARMY,0.62),
}

print("PAPER RAMP, derived from Honey Quartz fcd39f")
for k,v in ramp.items(): print(f"  {k:24s} #{hs(v)}")
print("\nINK candidates, derived from the outline red 980715")
for k,v in inks.items():
    print(f"  {k:20s} #{hs(v)}   on Paper {ratio(v,ramp['Paper      (page)']):5.1f}:1"
          f"   on PaperWarm {ratio(v,ramp['PaperWarm  (recessed)']):5.1f}:1")
print("\nSTAGE, derived from Army b3a828")
for k,v in stage.items():
    print(f"  {k:28s} #{hs(v)}   Honey on it {ratio(HONEY,v):5.1f}:1")
print("\nACTION: Chartreuse d6ce01 as a button face")
print(f"  ink on chartreuse   {ratio(darken(RED,0.45),CHART):5.1f}:1")
print(f"  honey on chartreuse {ratio(HONEY,CHART):5.1f}:1   <- too low if under 4.5")
print(f"\nKHAKI candidate (Honey desaturated toward Army): ", end="")
KHAKI = tuple((HONEY[i]*0.72 + ARMY[i]*0.28) for i in range(3))
print(f"#{hs(KHAKI)}  ink on it {ratio(darken(RED,0.45),KHAKI):5.1f}:1")

print("\n" + "="*70)
print("INK, as a MIX of the two darkest brand colours rather than as pure red.")
print("Pure red darkened reads as RED TEXT at body size, which is an error colour in")
print("every UI convention. Mixing the outline red with Army's olive gives a warm dark")
print("that belongs to the palette and reads as ink.")
mix = tuple(RED[i]*0.55 + ARMY[i]*0.45 for i in range(3))
PAPER = lighten(HONEY,0.55); WARM = lighten(HONEY,0.28)
for t in (0.40,0.48,0.55,0.62):
    c = darken(mix,t)
    print(f"  darken {t:.2f}  #{hs(c)}   on Paper {ratio(c,PAPER):5.1f}:1   on Warm {ratio(c,WARM):5.1f}:1")
print("  soft:")
for t in (0.00,0.08,0.15,0.22):
    c = darken(mix,t)
    print(f"  darken {t:.2f}  #{hs(c)}   on Paper {ratio(c,PAPER):5.1f}:1   on Warm {ratio(c,WARM):5.1f}:1")
