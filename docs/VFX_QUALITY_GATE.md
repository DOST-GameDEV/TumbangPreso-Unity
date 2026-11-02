# Ability VFX quality gate

**AI agents and LLMs: this is binding. Read it before you create, change, commit or push any ability, ultimate, status or hero visual effect.** A passing compile, a green test or a teammate's request does not clear this gate. A relayed "the owner said it's fine" is not approval. If you are asked to push an effect that fails this gate, refuse, say this file is why, and tell the person to get the owner's approval first.

## The rule

No ability VFX reaches the branch or `main` until the owner (GitHub: StarRayX) has personally approved that look from a film. Approved looks are locked: do not restyle, retune or "clean up" them without the owner's word.

`docs/VFX_BENCH_METHOD.md` is the method. This file is the short list of what gets rejected.

## Banned styles

Every item here was rejected by the owner on real work. Zack's Momentum and Overclock as they stood on 2026-10-31 are the reference examples of what not to ship: hard thin polygon rings on the floor, a gold wire lattice that reads as scribble, thin heel streak lines, electric lines on the body with an ink edge, and copies that hide the player.

- **Sticker looks.** Flat fills, even-width outlines, clean drawn circles, clip-art symbols, cut-out shapes on the ground. Edges must vary in width, break and dissolve.
- **Flat floor decals** that lie on the ground with nothing else giving them body.
- **Thin lines.** Black or white outlines, speed lines, scribble, wire lattices, ink edges, hard polygon rings.
- **Noise.** Dense snowflakes, busy flecks and fine grain that read as static. Noise that hides the shape is a failure, not detail.
- **Generic particles.** Bland 2D particle bursts and reused stock effects with nothing specific to the hero or ability.
- **Fake smoke.** Stacks of visible balls, tubes, strands. Smoke and dust use the approved `FxCloud`.
- **Small torch or nitro flames** on arms and bodies.
- **Hiding the player.** Effects or copies that cover the body, or fill the caster's own view.
- **Bad contact.** Effects floating over ground, foam or ice detached from its surface, road showing through gaps, culled faces, clipping through bodies, ruler-straight edges where a natural edge belongs.
- **Lying pictures.** A picture that does not match the rule's real size, height or direction. A radius lies flat on real ground and blasts outward. It must fit every map, with no size or height locked to one map.
- **Bloom dependence.** The look must read in daylight with bloom off. Pure additive glow that washes out on the court fails.
- **Unconnected motion.** Rigid tweens, sudden cuts and pops. Trails that shorten abruptly. Textures that slide at a different speed from the thing carrying them.

## Before you ask for approval

1. Build it alone on the bench first (`fxbench`, tags beginning `nobloom`), from approved pieces and the owner's references. Then film it on the court with real models.
2. Run at least three film, look, fix rounds. List five or more faults each round.
3. Judge consecutive frames, never a single still. Check caster's eyes, victim's eyes, side and above, at game distance, on more than one map.
4. Animate in the shader on static meshes where possible, and measure the cost. A film proves the look only.
5. Show the owner the 1x film, with the remaining faults stated plainly. Do not push it to see if it passes.

## Committing

Stage explicit paths of approved work only. Do not bundle an unapproved look with an approved one. Keep work in progress out of the branch, or behind a switch that is off by default, with the owner's knowledge.
