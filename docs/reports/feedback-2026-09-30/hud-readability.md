# Ready prompt and shared ability readability

Harry's new HUD Changes report requests a larger Xelu Interact/Ready glyph,
Ready Up wording without the warmup subtitle, and50percent larger ability controls
and key prompts at the existing margins. The owner explicitly clarified that the
Paete/Phaister protection is character-specific; authorized shared changes include
them. Their mechanics, descriptions and authored effects were not modified.

## Changes

- Ready Up uses the actual saved Ready binding's Xelu image,64canvas units high,
  alongside the label. The pill fits the combined glyph/words. Existing separate
  Ready overrides remain truthful; neither binding nor saved controls are reset.
- Keyboard and controller glyphs follow their own current binding. Touch shows
  Ready Up without a keyboard image. Unknown glyphs retain the binding text fallback.
- Ending the ready window removes the glyph and restores the original action-prompt
  rectangle; recovery, interaction and spectator behavior keep their own routes.
- The existing live power group is1.5times larger around its authored anchor. Its
  right/bottom margins and centered touch placement stay unchanged. Accessibility
  scaling composes with the new base. The separate held-description sheet is unchanged.
- Keep the reading hint within the deck width. Move the existing practice F7 status
  above the enlarged deck/hint so it cannot cover the power icons and keycaps.

## Evidence

Unity6000.5.8f1, LinuxOpenGL/llvmpipe, isolated named profile and guarded launch,
mip2 texture residency. Final2/2 native cases pass in8.48seconds, runner exit0;
no extra OOM. Four960x540/1600x680 gameplay-backed captures inspected.

The checks exercise keyboard Ready rebinding toF10, controller Ready rebinding,
no keyboard glyph on touch, ready-window exit and spectator hiding. Power checks
cover Paete/Phaister/Zack, unchanged skill names/descriptions, actual1.5times deck
bounds, same corner margins,1.2accessibility scaling, touch placement and hide.
The practice status is asserted above the reading hint.

First run:1pass/1failure. The fixture wrongly expected rebinding Interact to move
an already independent saved Ready binding; the product correctly retained that
legacy override. The repaired case targets the actual Ready action. Capture review
also exposed practice-status overlap with the newly larger deck; the product fix
and geometry assertion are in the passing final run. Existing role animation was
allowed to settle before screenshots instead of altering authored motion.

[Raw results, input hashes and captures](hud-readability-checks/). Cloud character
material artifacts are still visible and are not approved character visuals.
These are focused native UI/input-presentation checks, not a new player build,
physical-controller/touch certification or a full multiplayer qualification.
