# Final owner menu and login sources

Original supplied PNGs are preserved here. Main background/buttons are finalized;
change only the requested effects. The clean main background supersedes the first
export with a white top edge. login-background-woven.png is the new TUMP (2).png.

Latest login references are signup-reference-v2.png (TUMP7) and signin-reference-
v2.png (TUMP8). No email field. Sign-up: username/password/confirmation, original
eye icons, terms, primary, OR, Guest. Sign-in: username/password and primary only.
Top tabs change actual content. The redundant Already played/New player row is gone.

Extraction preserves source pixels and native dimensions. Run these repo-relative
scripts with Python/Pillow/NumPy/SciPy:

- tools/extract_owner_menu_edits.py for the original separated main/login pieces.
- tools/extract_login_v2.py for final template pieces and measured layout/face data.
- tools/author_menu_ground_mask.py for the sandy plane and foreground-object mask.

Sky separation uses tools/author_menu_sky_cutout.py with the free developer
requirements in tools/menu_matting_requirements.txt. Use a dedicated virtual
environment, not a global dependency upgrade. It derives alpha/background data
using PyMatting closed-form matting and multilevel background estimation. The RGB
source photo is never overwritten. Runtime compositing keeps opaque foreground
and correctly replaces the old background contribution at soft edges/wires.
Documentation: https://pymatting.github.io/alpha.html and
https://pymatting.github.io/foreground.html.

Then use the guarded Unity runner to execute
TumbangPreso.EditorTools.OwnerMenuEditsAuthor.Prepare. TextureImporter APIs import
full-size, uncompressed sRGB artwork and linear mask data. Generated clouds use
mipmaps/trilinear filtering to avoid shimmer when minified. Original supplied
buttons retain their pixel/aspect contracts. Generated cloud sources, prompts and
hashes are in generated-clouds. Two independent sprites drift behind the scene.

OwnerLoginLayout consumes the measured layout JSON. Captions align to painted
front faces. Equal new divider strokes flank OR. OwnerPaintedAction moves/scales
art uniformly inside stationary click targets. Reduced motion is still. Menu
music begins after startup login releases the visible home, not during loading.

Do not revive the rejected faint/localized dust, old-cloud warps, gold-matted
wires, broad cover patches, email field, old woven-symbol background or duplicated
links. The masks are data for the specific supplied1920x1080 composition; reauthor
and visually check them if the source changes. Actual owner approval is separate
from passing tests. Never use a usage reset. Resume gameplay after UI delivery.
