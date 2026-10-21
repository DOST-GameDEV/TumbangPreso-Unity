# Temporary Nunito Bold reading face

Owner October3 request: replace all active Lydian Regular use with Nunito Bold
for the time being. This is a reversible font selection, not an asset deletion.

Changed OwnerUiTheme's serialized reading-font reference, its missing-reference
fallback, and OwnerUiArtAuthor's theme regeneration. Loading tips already use
Nunito Bold. A source/GUID scan found no other active Lydian reference in Assets.
Original Lydian font data, metadata and licence credit remain available. Existing
Work Sans routes and display/accent typography are outside this replacement.

Three native checks passed: theme/loading face identity, fallback identity,
and actual tutorial paragraphs using the new font without clipping. The latter
also exercises the production HUD layout and retains the display-heading face.

Native PlayMode3/3 passed in0.8069602s at16:31:58–59UTC. The actual tutorial
HUD image at960x540 was inspected: the long role-ability paragraph fits its
expanded card, headings remain distinct, and skip/quit controls stay visible.
All five frozen changed inputs match. Exit0, but the compile/test process hit
the memory guard during shutdown (tree4636033024/container8204226560bytes).
Both isolated settings restored exactly; this is not a guard-free run.
No full-screen inventory, packaged player or physical-device acceptance claimed.

## Smaller reading size after owner review

The owner found the replacement too large. Reading-role text now targets85percent
of its authored size, preserving the existing28-unit small-window reading floor
without enlarging already-smaller labels. Tutorial body30 becomes28; headings
and accent labels keep their sizes. Existing loading-tip sizing is unchanged.

The first26-unit candidate failed the existing14px small-window reading guard
at960x540. That guard was retained; the production sizing was corrected to keep
its floor. Final3/3 checks pass in0.7767573s at16:44:01–02UTC, with an inspected
actual tutorial image. Both compile/test processes hit the memory guard during
shutdown; final exit0, tree4669259776/container8348479488bytes. Six changed input
hashes match, and both isolated settings restored. Whole-screen inventory and
packaged-device acceptance remain separate.
