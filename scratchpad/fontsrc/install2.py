import hashlib, os, shutil, sys
sys.path.insert(0, "scratchpad/fontsrc")
from install import FONT_META, TEXT_META, guid, SRC, ART, RES

print("Familjen Grotesk into Art/ and Resources/")
for folder in (ART, RES):
    for w in ("Regular", "Bold"):
        dst = f"{folder}/FamiljenGrotesk-{w}.ttf"
        shutil.copyfile(f"{SRC}/FamiljenGrotesk-{w}.ttf", dst)
        with open(dst + ".meta", "w", newline="\n") as fh:
            fh.write(FONT_META.format(guid=guid(dst), family="Familjen Grotesk"))
        print("  +", dst, os.path.getsize(dst), "bytes")

lic = f"{ART}/OFL-FamiljenGrotesk.txt"
shutil.copyfile(f"{SRC}/OFL-Familjen.txt", lic)
with open(lic + ".meta", "w", newline="\n") as fh:
    fh.write(TEXT_META.format(guid=guid(lic)))
print("  +", lic)

# Nunito goes out in the same commit. Leaving it would leave a 250 KB pair of unreferenced
# faces in the player and a second answer to "what is the sub font", which is the drift
# CLAUDE.md section 5 is about.
for folder in (ART, RES):
    for w in ("Regular", "Bold"):
        for p in (f"{folder}/Nunito-{w}.ttf", f"{folder}/Nunito-{w}.ttf.meta"):
            if os.path.exists(p): os.remove(p); print("  -", p)
for p in (f"{ART}/OFL-Nunito.txt", f"{ART}/OFL-Nunito.txt.meta"):
    if os.path.exists(p): os.remove(p); print("  -", p)
