import hashlib, io, os, shutil, sys
sys.path.insert(0, "scratchpad/fontsrc")
from install import FONT_META, TEXT_META, guid, SRC, ART, RES

# ---- files -----------------------------------------------------------------------------
for folder in (ART, RES):
    for w in ("Regular", "Bold"):
        dst = f"{folder}/WorkSans-{w}.ttf"
        shutil.copyfile(f"{SRC}/WorkSans-{w}.ttf", dst)
        with open(dst + ".meta", "w", newline="\n") as fh:
            fh.write(FONT_META.format(guid=guid(dst), family="Work Sans"))
        print("  +", dst, os.path.getsize(dst) // 1024, "KB")

lic = f"{ART}/OFL-WorkSans.txt"
if os.path.exists("scratchpad/fontsrc/OFL-WorkSans.txt"):
    shutil.copyfile("scratchpad/fontsrc/OFL-WorkSans.txt", lic)
    with open(lic + ".meta", "w", newline="\n") as fh:
        fh.write(TEXT_META.format(guid=guid(lic)))
    print("  +", lic)

for folder in (ART, RES):
    for w in ("Regular", "Bold"):
        for p in (f"{folder}/FamiljenGrotesk-{w}.ttf", f"{folder}/FamiljenGrotesk-{w}.ttf.meta"):
            if os.path.exists(p):
                os.remove(p); print("  -", p)
for p in (f"{ART}/OFL-FamiljenGrotesk.txt", f"{ART}/OFL-FamiljenGrotesk.txt.meta"):
    if os.path.exists(p):
        os.remove(p); print("  -", p)

# ---- code ------------------------------------------------------------------------------
edits = [
    ("Assets/TumbangPreso/Runtime/UI/MenuKit.cs", [
        ("/// <summary>Familjen Grotesk. A word somebody READS: a sentence, a settings row, a\n"
         "            /// caption, a chat line, a form field and its hint, a secondary button, a list\n"
         "            /// row.</summary>",
         "/// <summary>Work Sans. A word somebody READS: a sentence, a settings row, a\n"
         "            /// caption, a chat line, a form field and its hint, a secondary button, a list\n"
         "            /// row.</summary>"),
        ("/// <summary>Familjen Grotesk Regular. See <see cref=\"Face.Body\"/>.</summary>",
         "/// <summary>Work Sans Regular. See <see cref=\"Face.Body\"/>.</summary>"),
        ('Load("UI/fonts/FamiljenGrotesk-Regular", "Familjen Grotesk Regular")',
         'Load("UI/fonts/WorkSans-Regular", "Work Sans Regular")'),
        ("/// Familjen Grotesk Bold, as a SEPARATE FILE rather than as a font style.",
         "/// Work Sans Bold, as a SEPARATE FILE rather than as a font style."),
        ('Load("UI/fonts/FamiljenGrotesk-Bold", "Familjen Grotesk Bold")',
         'Load("UI/fonts/WorkSans-Bold", "Work Sans Bold")'),
    ]),
    ("Assets/TumbangPreso/Runtime/UI/CreditsContent.cs", [
        ("It is the DISPLAY face now and Familjen Grotesk carries the reading.",
         "It is the DISPLAY face now and Work Sans carries the reading."),
        ("Familjen Grotesk (text) — Copyright 2021 The Familjen Grotesk Project Authors "
         "(github.com/Familjen-Sthlm/Familjen-Grotesk).",
         "Work Sans (text) — Copyright 2019 The Work Sans Project Authors "
         "(github.com/weiweihuanghuang/Work-Sans)."),
    ]),
    ("Assets/TumbangPreso/Tests/PlayMode/PaperPurityProbe.cs", [
        ("Familjen Grotesk ships Bold as a", "Work Sans ships Bold as a"),
    ]),
]

for path, pairs in edits:
    t = io.open(path, encoding="utf-8").read()
    for a, b in pairs:
        if a not in t:
            print("  MISS in", path, "->", a[:70])
        t = t.replace(a, b)
    io.open(path, "w", encoding="utf-8", newline="\n").write(t)
    print("  ~", path)
