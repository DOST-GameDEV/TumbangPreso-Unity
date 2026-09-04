import hashlib, os, shutil

SRC = "scratchpad/fontsrc"
ART = "Assets/TumbangPreso/Art/ui/fonts"
RES = "Assets/TumbangPreso/Resources/UI/fonts"

# A deterministic GUID per asset path, so a re-run of this script cannot mint a second
# identity for a file that is already committed and referenced.
def guid(path):
    return hashlib.md5(("tumbangpreso/" + path).encode("utf-8")).hexdigest()

FONT_META = """fileFormatVersion: 2
guid: {guid}
TrueTypeFontImporter:
  externalObjects: {{}}
  serializedVersion: 4
  fontSize: 32
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames:
  - {family}
  fallbackFontReferences: []
  customCharacters: 
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

TEXT_META = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def place(src, dst, family):
    shutil.copyfile(src, dst)
    with open(dst + ".meta", "w", newline="\n") as fh:
        fh.write(FONT_META.format(guid=guid(dst), family=family))
    print("  ", dst, os.path.getsize(dst), "bytes")

print("Nunito into Art/ and Resources/ (MenuKit loads from Resources; Art is the source copy)")
for folder in (ART, RES):
    place(f"{SRC}/Nunito-Regular.ttf", f"{folder}/Nunito-Regular.ttf", "Nunito")
    place(f"{SRC}/Nunito-Bold.ttf",    f"{folder}/Nunito-Bold.ttf",    "Nunito")

# The licence ships beside the Art copy only. A .txt under Resources/ would be baked into
# the player as a TextAsset for no reason; the licence is a repository obligation, not a
# runtime one, and the CREDITS SCREEN is where a player is told (CreditsContent).
lic = f"{ART}/OFL-Nunito.txt"
shutil.copyfile(f"{SRC}/OFL.txt", lic)
with open(lic + ".meta", "w", newline="\n") as fh:
    fh.write(TEXT_META.format(guid=guid(lic)))
print("  ", lic)
