# UI composition redesign from the original layout studies

**LATEST owner correction, 2026-09-15: KEEP THE PREVIOUS LOGIN LOOK.** The owner
says the old login already looked good and was not part of the requested redesign.
Preserve its original centred artwork, form, colours, tabs and actions. Redesign
the OTHER screens. Startup still has no Back, Guest remains account-free, and PC
size support remains required without replacing this approved login composition.
The uncommitted gate/form split was withdrawn and all login source restored to
9bba95a1 (identical to pulled4c6b852c). Do not reinstate that proposal after compaction.


Active after pull4c6b852c on2026-09-15. The owner rejected the repeated orange
wallpaper/main-menu assets. U1-U7 implementation is useful functional groundwork,
not accepted visual design. Preserve service actions, data and original art files.

Owner explicitly requires all PC sizes, not only4:3. Keep scale/anchor/reflow
behavior continuous and test representative1280x720,1366x768,1920x1080,1920x1200,
1280x960,2560x1440,3440x1440,3840x1080 and3840x2160. Include smaller resizable
windows when practical. This is coverage across supported geometry, not proof
for every monitor. Artwork crops/letterboxes without distortion; control layout
and navigation stay usable. Inspect focal-art cropping on extreme ultrawide.

## Reference interpretation

PDF39: an illustration and a compact account form have separate roles.
PDF40: loading is a full illustration with a short readable tip, not a menu shell.
PDF41: home is a calm scene with a short side menu and the can as visual focus.
PDF42: the player hub supports a clear play action without filling the middle.
PDF43: character identity dominates one side; abilities and optional lore the other.
PDF44-48: equipment uses a gallery, category navigation and focused detail views.
PDF49: varied illustrated destinations, not another row of identical pills.

Use the new art's brush edges, red/green/ochre text roles and warm palette. Keep
Darumadrop readable for large display controls, Kawit for accents and Lydian for
reading/input. Preserve startup's no-Back rule and its usable Guest entrance.
Do not invent account capabilities or turn a sketch's store/currency into features.

## Distinct screen jobs

- Home: left-side logo/navigation over a quiet portion of a new hand-painted
  neighbourhood court scene. Can/slipper composition on the right, restrained
  scene motion, one obvious Play action, quiet tutorial/settings/credits/quit.
- Play: an illustrated mode spread with distinct Classic/Hero character staging.
  Route choices have compact native icon/label controls. No account buttons or
  repeated orange backdrop. Actual mode and route semantics stay separate.
- Loading: its own wide narrative artwork and small tip/progress area. Text and
  progress are native; the background contains no baked labels or fake controls.
- Startup account: art/form split inspired by39 and the new account reference.
  Existing field/tab artwork can remain specifically here with original pixels.
  Guest stays obvious. No Back; do not copy this layout into home/settings.
- Preparation/browser: a gathering board and map-led lobby, with distinct rows
  for players, routes and room state. Preserve authoritative ready/join/cancel.
- Character/skill: large genuine character preview/portrait, name/origin and
  separate ability strip/detail. Optional lore expands without blocking play.
- Equipment: thumbnail gallery and side category rail, with a focused preview
  and honest handling details. Distinct cards and selection, not action-pill grids.
- Settings: quiet reading/control workspace, grouped navigation and aligned
  actual sliders/toggles. Approved controller illustration and18callouts retained.
- Player hub/results/ranks: personal record/scorecard compositions; live data,
  team standings and match outcome lead. Avoid invented rewards or social actions.
- HUD/pause/dialogs: gameplay remains visible; stable HUD anchors, clear modal
  ownership, compact contextual surfaces. No full-screen decorative wallpaper.

## First batch and evidence

Implement home/play/loading in coherent steps, beginning with the home scene.
Generate supplemental raster artwork using the built-in tool and keep versioned
source/prompts/critique under ArtSource/ui/composition-redesign-2026-09-15.
Keep logo, labels, controls and geometry native/editable. Use nonvisual helpers
for layout/lifetime/input; do not copy a universal visual silhouette everywhere.

Inspect each generated image before integrating. Reject over-detailed/photoreal,
generic fantasy, noisy orange-pattern or baked-UI output. Inspect the actual Unity
composition after entry settles, at1080p and a smaller/awkward aspect. Preserve
normal-speed timing and distinguish runtime captures from artwork references.

Run only affected route/layout/motion checks. ReducedUiMotion requires separate
preference-transition and steady-state checks, not a weakened scale assertion.
Respect account/profile drafts, async state, focus/Back, scrolling and touch
coordinates. No real account/social/delete operations solely for qualification.

After all redesigned UI is qualified, continue GAMEPLAY_RESUME_AFTER_UI.md,
deferred Inday later and selected heroB/mapC LAST LAST. No agents/Figma/resets.
