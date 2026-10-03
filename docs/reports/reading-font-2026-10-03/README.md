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
