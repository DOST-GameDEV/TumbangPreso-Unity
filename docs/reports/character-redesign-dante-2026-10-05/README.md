# Character redesign prototype, Dante (Basilio): pictures, 2026-10-05

The handoff and the full record are in
[CHARACTER_REDESIGN_DANTE.md](../../CHARACTER_REDESIGN_DANTE.md). These are the final
renders of that day (version v11) plus one rejected reference.

All are Blender EEVEE renders that IMITATE the game's toon ramp and ink outline. None is a
Unity capture; the prototype has never been in the game. Every figure is in the rig's `idle`
pose. "His left" is the viewer's right: the gold eye, the scar and the horn are on his left.

A is the block hair (`dante-redesign.glb`), B the lock hair (`dante-redesign-spiky.glb`).
Everything except the hair is the same model and the same atlas.

| Image | What it shows |
|---|---|
| [01-original-and-redesign-A-B.png](01-original-and-redesign-A-B.png) | The original `team-dante.glb`, redesign A and redesign B, three-quarter, same scale and light |
| [02-turnaround-A-B.png](02-turnaround-A-B.png) | A (top row) and B (bottom row): front, three-quarter, side, back |
| [03-face-A-B.png](03-face-A-B.png) | Face close-ups of A and B, front and three-quarter |
| [04-head-variants.png](04-head-variants.png) | The original and the three head carvings (`block`, `carved`, `shaped`), front, three-quarter and side. `carved` is the one built. Shown with hair A |
| [05-at-10m.png](05-at-10m.png) | Original, A and B at about 150 px tall, as at 10 m in play, and the same shot as black silhouettes |
| [06-beside-the-golem.png](06-beside-the-golem.png) | Original, redesign A and Paete side by side, for a consistency check |
| [07-texture-sheet.png](07-texture-sheet.png) | The painted top half of the 2048 atlas: every island and swatch |
| [08-fix-hair-closeups.png](08-fix-hair-closeups.png) | The hair of A and B from behind and from above-front, after the painted shine was removed |
| [09-fix-ear-closeups.png](09-fix-ear-closeups.png) | Each ear of A and B, after hair and stubble paint was taken off them |
| [10-fix-hands-and-hem.png](10-fix-hands-and-hem.png) | Both hands, front and back, and the coat hem beside each hand |
| [11-close-pass.png](11-close-pass.png) | Chest, back, feet, both sides and a view from above, for A and B |
| [12-REJECTED-rounded-head.png](12-REJECTED-rounded-head.png) | REJECTED. The first pass: a fully rounded head and round limbs, beside the original. Kept only so the rejected direction is on record |

To make new ones: `tools/render_character_redesign_dante.py` then
`tools/sheet_character_redesign_dante.py`, with a new version name. Commands are in the
handoff, section 4.
