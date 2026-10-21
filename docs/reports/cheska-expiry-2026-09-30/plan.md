# Cheska expiry presentation

## Scope and source of truth

The owner reopened the requested field-melt and wall-shatter work after the
prior actionable feedback. This is presentation work, not a kit redesign.
Read the current Wiki and runtime separately: the Wiki requests Cold Feet for
7.5seconds, but RosterReworkRules.CryoRules currently supplies5seconds. That
mechanical discrepancy remains in the owner-reserved ability work. Visual thaw
must follow whatever Duration it receives, including restored ages; test both
values without changing the rule. Glacial Wall is an arc with three accepted
slipper hits and an8second current runtime lifetime. Do not change those
numbers, status ownership, targeting, cooldowns, hit counts or networking.

Finalized Paete/Phaister presentation stays protected. This pass has no reason to
touch either. Skill sounds remain removed under the more specific audio direction;
existing silent call sites are not permission to restore them.

## Research and what carries across

- [Riot's VFX art education](https://www.riotgames.com/en/artedu/visual-effects)
  frames clarity, satisfying action and thematic coherence as simultaneous goals.
  For TUMP, expiry must say that the space is becoming safe without hiding the
  can, slipper or player. More particles alone do not solve that problem.
- [VALORANT shader/clarity engineering](https://www.riotgames.com/en/news/valorant-shaders-and-gameplay-clarity)
  preserves gameplay-critical shape across quality levels and budgets each effect.
  Keep Cold Feet's actual boundary readable while its interior thaws. Decorative
  breakup must not move the hazard, invent an impact or become another opaque field.
- [Mei's official ability description](https://overwatch.blizzard.com/en-us/heroes/mei/)
  distinguishes a solid wall from an area effect. Use that distinction: the wall
  loses solid mass; the ground field loses frost coverage. Do not copy Overwatch
  mechanics, scale, damage or character design.
- [Official Ayaka demo](https://www.youtube.com/watch?v=5RCsr1Y8UZA) was located and
  its verified Genshin channel confirmed. This browser loaded the page but did
  not decode video (readyState0). No new frame-by-frame observation is claimed.
  The existing HERO_KIT_METHOD and ultimate-performance research remain prior
  recorded references, not newly inspected footage. Take their sequencing and
  critique method, not their hero shapes or particle counts.

## Current implementation critique

Cold Feet already has authored draped skin, fracture veins and a distinct edge.
Its last0.7seconds reduce the skin uniformly, veins fade over0.45seconds, and the
edge fades over0.1seconds. This communicates disappearance but lacks a visible
melt front. Reuse the authored flat footprint; do not restore the rejected raised
platform or central spike.

Wall shatter currently deletes the wall and emits eight small radial fragments
near one center. That loses the arc's spatial identity. A later wall pass should
break from the actual authored slabs, with varied chunks and a brief downward
settle. It must retire collision immediately as today. Do not implement a second
wall or keep an invisible blocker while debris plays.

## First coherent unit: Cold Feet thaw

Meaning: the cold loosens its grip on the street, exposing the original ground.

| Beat | Visible treatment | Gameplay/UI/audio contract |
|---|---|---|
| Tell | Preserve the existing formation and full boundary | No new cast delay or prompt |
| Release | Existing accepted field appears on its actual footprint | Current authority and owner unchanged |
| Travel/contact | Preserve current ground drape and frost veins | No new radius or collision |
| Linger | Calm, thin ice; little motion during the useful lifetime | Can, shoes and bodies remain readable |
| Dissipate | In the existing final0.7seconds, an uneven broad melt front opens the surface; fracture traces retire with it | Full hazard edge remains until its existing final exit; no early safe-space claim |

Keep the melt broad and restrained, with a few coherent areas rather than noisy
per-pixel glitter. No new postprocess, particles, gameplay objects, sound or input.
Use the existing shader and IVfxTimeline so recorded/restored views can sample it.
Nova uses the same material family: its current outward wave must remain unchanged.

## Planned paths and acceptance

First inspect current native owner/observer views before implementing. Planned
presentation paths: Runtime/Visual/FrostSurfacePresentation.cs and
Shaders/FrostSurface.shader under Assets/TumbangPreso; a focused
Tests/PlayMode/CheskaExpiryPresentationTests.cs and metadata; match test partition.
No HeroHazards or ability-rule edits in this first unit. Claim before editing.

- Earlier lifetime renders match the current calm field
- Late-life coverage retreats non-uniformly, rather than only lowering all alpha
- Boundary size/position remains the actual hazard's until expiry
- Sample0, middle, late and expired states; repeated/restore sampling is deterministic
- Nova's separate wave remains unchanged
- Inspect real court captures from gameplay-scale views; compare Low/readability
- Check material/mesh ownership and cleanup; no silent persistent allocation
- Publish only after native behavior/render checks, then update the same Feedback
  row. Human verified is left for the testers

A generated concept may help compare directions after the actual baseline is
captured. Treat it as a sketch and critique it against the native frame; never use
its apparent geometry or lighting as evidence that the game does that.

## Second unit: Glacial Wall breakup

Keep the real three-hit/expiry trigger and immediate collision removal. The
current center-only puff does not account for the4.2metre arc. Hand-place three
unequal authored ice fragments per actual slab, at different heights and offsets,
so the existing wall's width, rotation and street placement carry into the break.
Reuse the authored ice shard, not new cubes, circles of identical particles or a
copy of another hero's visual vocabulary.

-0.00seconds: collision is already retired by the unchanged hazard owner; fragments
  occupy the source slabs' actual world-space locations
-0.00-0.12: a short asymmetric split, mostly outward/sideways, with little upward lift
-0.12-0.55: chunks fall and turn, exposing the route quickly; no rigidbodies,
  colliders, force, camera shake, new sound or gameplay footprint
-0.55-0.80: remaining chips shrink/thaw near the ground, then the owned visual dies

Use15individually specified recipes for the five-slab arc, preserving a three-slab
legacy fallback. One timeline owner samples their age; no15independent physics
updates. The split must remain readable without a flash covering the whole court.

Runtime scope: CheskaIceVisuals.cs, a new CheskaWallBreak.cs/meta, and only the
wall presentation call in HeroHazards.Shatter to pass its actual Transform. The
existing replicated IceShatter flair already calls each peer's wall.Shatter, so
no wire format or hit semantics change. Restraint thaw stays on its old path.

Acceptance: first two accepted hits retain collision; third retires it at once;
fragments span the actual arc instead of one center; no physical debris; repeated
Shatter cannot duplicate the effect; cleanup finishes; native court film inspected.
Check the same visual entry from the existing flair route without claiming new
actual-peer qualification. Preserve unrelated HeroHazards edits during integration.

## Concept critique

[Concept only](concept-only.png) was generated from the actual baseline frame.
Adopt its spatially distributed uneven chunks and clean absence of a flash.
Reject its opaque/smoothed ice, nearly intact pillars at the first split, extra
fragment clutter and lingering puddles. Keep the actual authored translucent
shard mesh, fixed15recipe budget and complete0.8second cleanup. This concept is
not a game capture and does not establish implementation or native quality.
