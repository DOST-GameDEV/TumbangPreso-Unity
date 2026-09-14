# Owner-painted match UI

The main HUD, power deck, pause menu, round-change screen, training panel,
results and gameplay touch surfaces now use the owner theme. Existing referee,
ability, scoring, input, progression and session actions remain responsible for
behavior. Prior visual builders are retained inactive. The held reference from
U4 remains, and the HUD does not put patterned wallpaper over the arena.

New compact deep-red score strips use actual portraits; the clock has red
Darumadrop numerals; local role/stamina, can state, contextual/recovery prompts
and compact skill meters retain their data. Ultimate has a distinct seal shape.
Live color writers were updated with the construction, preventing old theme
colors from returning every tick. Element-specific stun coats and game effects
retain their existing gameplay colors, rather than being turned into UI colors.

Pause keeps input parking, child settings and proper return behavior. Training
uses native text/key glyphs and adds real skip/quit callbacks alongside existing
keyboard shortcuts. Pixel-perfect rendering and moderately larger header/footer
fonts fixed the first capture's soft lettering. The renderer progress bar follows
the lesson controller, and completion respects reduced interface motion.

Round/results use actual portraits, standings, map choices and rematch actions.
Ties now say TIE instead of an ambiguous equals sign. An absent match summary
has a clear empty state. Friend/report actions remain implemented but were not
submitted in testing. Rank badge artwork remains assigned to U7.

Touch has new native surfaces and source-palette icons. Only a graphical binding
was added to TouchButton; SetHeld, pointer handlers, drag offsets, TouchStick
movement and all input-catalogue entries remain unchanged. Editor toolbar and
approved controller callouts continue to work with these surfaces.

## Focused evidence

Fresh receipts and original Unity images are in owner-ui-u6-evidence.

- Pause1/1: source-art menu, child settings discard and return/unpark.
- HUD1/2 initially: Hero live/held/spectator passed; Classic expected established
  ScoreRow names. Restored that naming; Classic/recovery/round/dismissal then1/1.
- Trainingv1 reached real skip but failed a test that set renderer progress then
  waited while the real Move lesson correctly overwrote it with0. Renderer setter
  is now checked synchronously; no production progression was weakened.
- Trainingv2 fresh XML1/1 and Unity log requested exit0, but the native process
  returned1 after normal-looking shutdown. No fatal/crash trace was found. This
  discrepancy remains a U8/native-qualification limitation, not a clean-exit claim.
- Resultsv1 passed1/1. Final results/touchv2 passed2/2, including useful empty
  state, four portraits, player list, map choice, actual rematch and touch/controller
  return, cancel rollback and retained input entries.

Named profiles/shared input preferences restored. Unity reserialized two deferred
Inday arm assets with trailing whitespace only; git diff -w was empty and only
those exact files were restored. No geometry was changed or Desktop build replaced.

These cases do not certify all17lessons, all6kits, physical controllers/phones,
external social/ranking services or the complete online flow. The spectator case
checks HUD visibility/clean feed, not the later cinematic camera revamp. The
training renderer setter case is not a hardware movement-input test.

## Critique and remaining work

The interface now shares source palette, fonts and meaningful control families;
the court remains visible in normal play. The first results blank state and
training type softness were corrected. Invented paper frames are still more
geometric than the source brushwork; U8 must improve them deliberately. At115%
custom touch scale, nearby shapes can approach/overlap, so evaluate default
placement and oversized layouts without silently replacing saved coordinates.
Some small utility icons and repeated alternative-skill symbols need final art
review. Source PNGs must stay unaltered throughout that refinement.

Continue U7 profile/friends/career/history/account/rank, then U8 complete visual,
motion, accessibility/input and native-player qualification. Resume gameplay only
after the full UI overhaul from GAMEPLAY_RESUME_AFTER_UI.md.
