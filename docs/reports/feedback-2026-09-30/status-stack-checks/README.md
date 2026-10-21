# Live status stacking

The three-status cap is removed. Each catalog status retains its own UI identity,
actual timer ring and full name/tooltip. Live entries stack upward, wrap into
columns when vertical space fills, and smoothly move after removal/insertion.
Refreshing a status does not replay entry motion. Reduced motion snaps. Dense
columns reserve space for action/warning text, which wraps without shrinking
below the existing typography floor.

Haunted was missing from StatusIcons.Live and had no sprite. Its actual timer now
appears with an occluded-eye icon generated through the existing status-art
pipeline. Only the new PNG was generated; existing icon assets were preserved.
New metadata GUID b1846c1380194b968cd921f69cea0ace was natively imported.

Two distinct native cases pass: ordinary three-status/action/progress layout,
and ten simultaneously applied real statuses across small/wide, normal/enlarged
views with no card overlaps. Dense checks also cover shared Haunted timer fill,
expiry, reflow and refresh identity. Pixel review caught the missing icon after
the first2/2; only the dense case was repeated after adding the real sprite and
import assertion (1/1). Frozen final inputs unchanged; no tooling repair/new OOM.

Images intentionally stress ten statuses. The large staged player and overlapping
world effects are outside this HUD claim; this does not approve whole-scene VFX.
No new player/peer or human approval claim. Protocol120 is unchanged by this UI.
Announcement scoring/duration remains unfinished in the combined Feedback row.
