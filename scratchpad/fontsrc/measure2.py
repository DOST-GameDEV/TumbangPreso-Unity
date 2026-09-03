import sys, os
sys.path.insert(0, "scratchpad/fontsrc")
import measure as M
SRC = "scratchpad/fontsrc"
DARUMA = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"
CANDS = [
    ("Darumadrop One", DARUMA, None),
    ("Nunito (current)", f"{SRC}/Nunito-Regular.ttf", f"{SRC}/Nunito-Bold.ttf"),
    ("Hanken Grotesk",   f"{SRC}/HankenGrotesk-Regular.ttf",   f"{SRC}/HankenGrotesk-Bold.ttf"),
    ("Familjen Grotesk", f"{SRC}/FamiljenGrotesk-Regular.ttf", f"{SRC}/FamiljenGrotesk-Bold.ttf"),
    ("Archivo",          f"{SRC}/Archivo-Regular.ttf",         f"{SRC}/Archivo-Bold.ttf"),
    ("Work Sans",        f"{SRC}/WorkSans-Regular.ttf",        f"{SRC}/WorkSans-Bold.ttf"),
    ("Figtree",          f"{SRC}/Figtree-Regular.ttf",         f"{SRC}/Figtree-Bold.ttf"),
]
for n, r, b in CANDS:
    if not os.path.exists(r):
        print("MISSING", r); continue
    M.report(n, r, b)
