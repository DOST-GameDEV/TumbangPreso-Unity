# UI placeholder implementation record

The user requested a functional placeholder UI, preserving a later art remake by
the team's UI artist. They explicitly deferred final tests and player builds to
the next chat. This record describes implementation and evidence boundaries.

## Implemented

- Home with profile, character/equipment, settings and Play routes.
- Separate rules selection before practice, ranked or custom entry; tutorial is
  in the play area. Ranked retains the Hero Strike ladder and does not auto-host LAN.
- Character maker hidden and guarded, with its authoring code, data and assets retained.
- Darumadrop, Kawit Extended and Lydian roles; dynamic font imports; Lydian signed
  descent correction with unchanged glyph outlines. Owner embedding permission recorded.
- Placeholder paper shapes/icons and icon-only Back/Close treatment that preserves
  original callbacks and hit rectangles.
- Drawn street artwork following the supplied sketch/logo, with independent tree
  and sun motion layers and complete-image fallback. Rejected native-game footage
  and realistic porch artwork do not ship as menu resources.
- Illustrated startup display with requested random 5-15 second dwell, real
  readiness retained, and artwork-triggered optional lore/tips that hold while read.
- Low/Balanced/High graphics choices and the earlier result raycast-plane repair.
- Lore, font usage, UI structure, original reference assets and a revised execution plan.
- English role/equipment instructions, tutorial and ability descriptions, rank and
  reward labels. Serialized identifiers, thresholds and gameplay logic are retained.

## Evidence actually obtained

Earlier checkpoints passed Core 559/559; EditMode 442/442, then 444/444 and
446/446; isolated InputSurfaceProbe 5/5; focused home flow 2/2 before the final
illustration/loading replacement. All eight editor checks and all gating source
audits passed before that replacement. The non-gating cue-audio audit retained
six previously recorded findings. These are not a final-state qualification.

Static review covered changed C# syntax, source diffs and resource presence.
Transparent tree/sun images were inspected as RGBA with a 0-255 alpha range.
The complete image and clean plate are 1672x941, matching the layer canvases.
UI image imports disable mipmaps/compression and retain native resolution.

No further tests or player build were run after the user's final instruction.
Final loading/lore interaction, motion-layer composition, all resized layouts,
controller navigation, the final English-copy changes and the exact Windows player
remain to be verified. No final compile is claimed by the syntax-only review.

## Immediate next work

Use IMPROVEMENT_PLAN V0, then its gameplay/world phases. Run actual navigation and
raycast checks, including loading story open/next/close, before relying on the UI.
Inspect final type at 1280x720, short wide, 4:3 and ultrawide. Revisit inherited
screens if the new faces expose clipping or spacing problems. Do not lower the
readability floor or discard a failing test to obtain a green result.

The plan may be revised when evidence suggests a better result. Its invariant is
the user's game and constraints, not any particular proposed implementation.
