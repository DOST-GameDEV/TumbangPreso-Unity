# Exact owner-authored scroll icons

Use the supplied Scroll Up.png and Scroll Down.png unchanged:135x100RGBA each.
No generated redraw, crop, resampling or forced square. SHA256 checks prove the
runtime PNG bytes match the two supplied files exactly. Native import retains
135x100 dimensions, bilinear filtering and the existing transparency settings.

The tutorial keeps the same72-unit height as its other glyphs. Each scroll prompt
is97.2units wide, preserving135:100 instead of shrinking inside72x72. The slash
remains removed. Shared ability/recall/action-prompt consumers likewise reserve
extra width at their established height; ordinary bindings retain prior widths.
The previous unused generated sheet is removed from Resources, recoverable in Git.

Native EditMode12/12 passes in0.342s: both complete separate textures, exact
135x100 rectangles, centered pivots, cache/binding coverage and the actual tutorial
CurvePrompt/RebuildKeys/KeyCap row. Tutorial checks assert72high and97.2wide for
wheel prompts versus72x72 for ordinary keys. This is headless structural/sprite
identity evidence, not a fresh rendered gameplay screenshot. Supplied pixels
were visually inspected; human in-game appearance remains separate.

Compile completed/reloaded assemblies, then import reached the headroom guard.
First runtime reused those compiled inputs after idle cleanup. Final exit0/no
guard; treeRSS3,263,778,816/container7,122,071,552bytes. Frozen input hashes match;
settings/profile restored. No new gameplay or protocol change.
