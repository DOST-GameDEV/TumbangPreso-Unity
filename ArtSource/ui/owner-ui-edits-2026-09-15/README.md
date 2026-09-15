# Owner menu artwork, September15

The clean background/reference supersede the first exports with a white top row.
The original supplied files and source hashes remain preserved beside this file.
The new Downloads/TUMP (3).png is MAIN MENU art; it must never replace the older
login background with the same original filename.

Run `python tools/extract_owner_menu_edits.py` from this checkout to extract the
five main-menu and ten login pieces. The conversion removes connected white-sheet
backing and recovers antialias opacity near outlines. It does not resize, redraw,
recolour interior paint, compress or warp the buttons. Extracted manifest.json
records source crop coordinates, hashes and recovered edge counts.

Run `python tools/author_menu_sky_mask.py` to regenerate the sky distance data
from this exact source illustration. It records distance to fixed foreground
objects; it is not replacement artwork. Its manifest and linear PNG live in
derived/. Both authoring scripts use Pillow, NumPy and SciPy.

Use the project's guarded Unity runner with executeMethod
`TumbangPreso.EditorTools.OwnerMenuEditsAuthor.Prepare` to import runtime copies.
This uses TextureImporter APIs: full source dimensions, uncompressed sRGB colour,
no mipmaps and preserved alpha. Background remains byte-identical to the clean PNG.
Never hand-edit sprite importer metadata or overwrite canonical source files.

Runtime consumers: OwnerMenuArt, HomeCourtView and SignInScreen.OwnerPainted.
Main button labels remain editable Darumadrop text; positions and sampled ink are
measured from reference_mainmenu_clean.png. OwnerPaintedAction animates a child
uniformly, keeping the click target stationary. Credits remains under Settings.

OwnerRoadDust places faint drifting dust only on the painted middle-distance road.
It maps source coordinates through the background's crop, stays behind all controls,
and disappears with ReducedUiMotion. The wall, street, buildings and props stay fixed. OwnerMenuClouds applies a slow
continuous sky flow through OwnerMenuSky.shader, attenuated by the baked distance
data around occluders. There is no duplicate-image crossfade. Reduced motion
returns to the original still. The mask imports as linear data, not sRGB art.

The approved login background/composition and real account/Guest flow remain.
Updated field pieces already contain icons; additional glyphs are suppressed.
Music starts on visible home after startup loading/login. The separate brief studio
cue still belongs to the engine logo and stops at illustrated loading.
