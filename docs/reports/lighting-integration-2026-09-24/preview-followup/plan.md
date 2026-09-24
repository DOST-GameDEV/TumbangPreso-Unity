# Preview lighting branch follow-up, 2026-09-24

Owner asked to track and merge lighting/peak-bright-overhaul. Source advanced
from 50f1fc255 to 8d73471f3 (five commits). Current ASTRA baseline 4e2c5bc6d already
has an independently qualified preview look (ec1d3a9cd and follow-up), so the two
owner classes conflict. Preserve the established explicit-sun/tagged-camera
preview API, same-map reuse, immediate release and cached-ground property-block
ownership fix. Integrate incoming HDR render target, active-scene restoration
guard, surviving-root/sun selection and its five-map preview test. Do not install
two look components or restore the old darker environment. Keep source history.

No UI layout/art change. Resolve only the two conflicting owner classes, keep the
incoming test, and add a before-LDR/after-HDR pair through the same camera. Its
existing five-map cycle covers cache revisit, menu-camera isolation and sun
handback. Also verify that destroying a preview after switching the active scene
does not overwrite that new scene's ambient/fog. One focused run, exact XML and
actual image/grey review; one fixture repair allowance, zero used. No broad suite,
unchanged shop/card rerun or intermediate build. Resume Bayan ginger cat after
this requested branch integration. Animal/map/gameplay parents remain open.

Second source advance: 429643416 adds a public selected KeyLight for screen-space
edges, replacing their ambiguous menu fallback. Port to our cached explicit sun,
not the other branch's duplicate backing fields. Keep same-binary look-off native
probe and its Mac-only receipts. One focused five-map case asserts the rendered
edge material vector with an unrelated portrait light present. Prior qualified
HDR views are the same-camera baseline; moving ambient content/time may differ,
so this is visual review, not an isolated pixel-difference measurement. No new
camera fixture or intermediate native build. Resume tuxedo cat afterward.
