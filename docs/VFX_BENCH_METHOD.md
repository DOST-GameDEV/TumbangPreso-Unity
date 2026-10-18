# VFX bench method: how an ability's effect gets made and refined

Written 2026-10-11, after the trails, the bench whirlwind and Amihan's WHIRLWIND on the court were approved. This is the
process that produced the effects the owner accepted, the craft notes on what makes an effect good (section 1a), and
the mistakes that cost rounds before it settled. Read it before
making or reworking any ability effect. Read `AGENTS.md`, `docs/Art_Direction.md` sections 0 and 0.1 and
`docs/HERO_KIT_METHOD.md` first; this file is the method, those are the rules.

Status of the work lives in section 9. The continuation prompt is not in this file; a handoff prompt goes in chat.

## 1. The one finding everything else follows from

**Work built against a real reference, checked side by side, reached the owner's bar. Work designed from our own heads
did not.** The owner, comparing the two on 2026-10-11: "if you compare what we have from our attempt to copy the
references, the vfx is really subpar."

Why the gap exists:

- A reference has already settled the design: shape, layering, values, timing. We only execute, and we can measure how
  close we are.
- The owner supplied the eye. The fifth trail came right only after he gave its construction in his own words ("the 5th
  is one ribbon, fold applied, copied as is, then shifted down"). The whirlwind's horn came right only after he named
  the noise and showed it.
- Our own judgement from stills is a weak judge. The lotus, scorch and smoke were graded by us; the smoke took two
  passes on our own guesses and is still not there.

So "from scratch" never means "from imagination". It means our own assets built on a recipe read off a reference.

## 1a. Craft notes: what actually makes an effect good

These are what the approved effects have and the rejected ones lacked. Each line names where it was learned.

**Shape**

- **One readable silhouette first, then secondary forms, then detail.** The whirlwind reads at a glance as a funnel on a
  point over a ground swirl with rings round it. The rejected gale rework was "a mound of palm fronds": detail with no
  silhouette.
- **Few broad forms beat many thin ones.** Wire, strands and fronds read as noise. The reference effects are a handful
  of wide shells and ribbons.
- **Contrast the shapes against each other.** The whirlwind's horn widens upward while its tapes widen downward, so the
  whole is an hourglass. Parts that all taper the same way read as one lump.
- **Proportion is part of the look.** The same whirlwind stretched tall and thin became "three teal fountains" and was
  rejected. Scale evenly or not at all.
- **Size it against a body.** An effect that is meant to threaten should stand well over a character (the approved
  whirlwind is two and a half characters tall). Knee-high reads as decoration.
- **Asymmetry and a bend.** The reference horn bends in an S and ends back over its foot; one side is crisp and
  stepped, the other peels into blades. A straight symmetric cone reads as geometry.

**Surface and texture**

- **The look lives in mask textures scrolled along meshes, coloured by a ramp.** Not in particle counts and not in
  mesh detail. Every approved effect is this.
- **Paint masks by construction, not by noise soup.** The fifth trail is one ribbon, folded, copied unchanged, shifted
  down. Knowing the construction is what made it match.
- **Large and simple.** One octave of noise with big cells. The first instinct is always to add octaves and detail;
  the references do not.
- **Stretch and skew the texture along the motion.** Round cells read as blobs ("too blob-y"). Cells drawn out about
  3 to 1 along the spin read as wind. At 5 to 1 they become regular zebra stripes, which is too far.
- **A streak is bright along its spine and falls off to its edges and tips.** A flat plateau with a uniform soft rim
  reads as a pill.
- **Transparency is a material.** Open cells that show the far side through the near side give depth for free. An
  opaque body reads as a solid object, not as air or light.
- **Soft inside, crisp in places.** Not everything soft and not everything hard. The reference horn is soft silk with a
  crisp outline down one side and crisp long edges on its tapes.
- **Different parts are different materials.** Saturated silk for the horn, flat milky bands for the tapes, a dim veil
  on the ground, saturated motes. One material everywhere reads flat.
- **Layer two copies at different sizes and speeds** for depth: the horn has a second skin 16 percent wider turning at
  another rate.

**Value and colour**

- **Three steps: a near-white core, a body colour, a faint veil.** Design where each goes.
- **White belongs in thin lines and small hearts, never in broad areas.** The first whirlwind sample was "blown to white
  through the whole middle"; one later horn had a flat white slab. Both read as overexposed. Yet "some parts should
  reach bright whites": no white at all reads dull.
- **Measure clipping.** The approved bench whirlwind clips well under 1 percent of the frame to white.
- **Mix a soft layer with a crisp one.** The approved horn is the soft noise body with the original's thin bright veins
  inside it. Neither alone was right; he asked for the mix and approved it at once.
- **Vary the hue between two named extremes.** A single flat hue reads as a tint. The court whirlwind wanders between
  green and blue round a teal middle, per piece and over time.
- **Name the colour by how it reads in the scene.** The same values read teal on the dark bench and cyan in daylight.

**Edges and endings**

- **Dissolve by erosion, not by fading alpha.** Shapes should be eaten by a second texture (the cloud map) so they tear.
- **Make an edge wispy with the noise itself and with a fade, together.** Toward the whirlwind's mouth the empty cells
  grow in number and size AND the whole surface fades, so the last tongues thin into nothing. Either alone failed:
  a fade alone is a soft cut-off, cells alone leave islands at full strength ("you arent fading out the rim").
- **Every layer needs the ending.** The islands he circled were on the back copy of the horn, which had no fade.
- **Nothing ends on a mesh edge.** A cell cut by the edge of its mesh reads as a flat paddle.

**Motion**

- **Spin comes from the texture running along the shape plus real rotation**, faster at the foot than the top.
- **Layers move at different speeds and directions.** One speed everywhere reads as a rigid object turning.
- **A life has a gather, a snap, a body and a proper death.** It unravels, lifts and thins. It never blinks off and
  nothing with weight moves on a straight tween.
- **A still shows no motion.** When the reference is one picture, spin speed, birth and death are our own design; say so.

**Restraint and reading in play**

- **Few, deliberate motes.** About sixteen on the whirlwind, more saturated than anything else, placed where the
  reference has them. A particle cloud is the opposite of designed.
- **Remove anything that reads as geometry.** A dark flat blade behind the horn and a slab at its mouth were both
  leftover shells; taking them out mattered more than adding anything.
- **It must read in daylight without bloom**, from the target's eyes first, and it must not blind the caster or the
  victim.
- **It must not lie.** What is drawn on the ground is what hits.
- **It belongs to this game.** Cute, blocky, painted: stylised forms and flat readable values, not realistic weather
  or realistic ice (`docs/Art_Direction.md` section 0).

**What did not work, so nobody repeats it**

- Designing from imagination and grading it ourselves.
- Repainting look-alikes of effects that were already approved.
- Smoke as a heap of solid shaded balls: more balls did not help, and two passes of shading tweaks gave a modest step.
- Softening, widening or dimming parts on our own judgement after he had said they looked good.
- Feathering whole cells to grey when he asked for more white: 62 percent of the tile was not black but only 29 percent
  was actually bright, so the horn came out thin and dim.

## 2. The order of work for one effect

1. **Read the ability.** Its class, its rule numbers in `Packages/com.tumbangpreso.core`, where its look is built, and
   what the caster, a target and a bystander each see. Write down the gameplay shape the picture must tell the truth
   about: footprint, path, how long it lasts.
2. **Get a reference before building.** Ask the owner for one first; his picks set the bar (the trails video, the
   whirlwind picture). A still is enough for shape and materials; it shows no motion, so say that motion is our design.
   Outside references are study only: never bought, downloaded as assets, traced or shipped.
3. **Read the reference yourself, from the picture and from enlarged crops of each part.** Measure it in pixels
   (the whirlwind's foot to mouth was 1 to 7 by measurement, where a glance said 1 to 3). Put the picture and the crops
   on the first board, so the owner can see what was looked at.
4. **Map each part to a bench effect we already have** (section 4) before painting anything. New textures only for what
   the bench truly lacks, and say so first.
5. **Texture board first.** Each mask beside the matching crop of the reference at the same size, with coverage numbers,
   before it goes on a mesh.
6. **One sample on the dark bench, beside the reference at the same size.** Rough is fine. Seen beats good.
7. **Refine on the owner's notes, one change set per film.** Keep every earlier state filmable by name.
8. **Only then the court:** daylight, a real map, characters for scale, three views, old look beside new.
9. **Lock what he approves, the moment he approves it** (section 5).
10. **Then make it honest in the rules** if the picture and the hit area disagree (section 7).

## 3. Showing the owner

- **Show a film the moment a piece exists**, as one pass at 1x then one at 0.5x. Look at the stills yourself first and
  say plainly what is wrong with it. He asked for a sample in ten minutes and later in five; both were met by skipping
  everything except the render, and saying what was skipped.
- **Always a side-by-side at one size**: reference, the previous state, the new state, at the same moments. A change he
  cannot compare is a change he cannot judge.
- **When a note has two readings, board both and let him point.** "Celled" was read as a net of thin lines; he meant
  soft blobs with holes. One board with both candidates would have saved a round.
- **Give numbers with a look claim**: percent of a mask that is white, percent of the frame clipped (`fxclip.py`),
  size against a character's height.
- **Say what was not seen.** A session that looked at two stills did not see the film in motion. Nobody has cast an
  effect in Play until the owner does.
- Films over 30 MB do not reach a phone; send a smaller copy as well.

## 4. Build from the bench, directly

The owner, when a session was repainting look-alikes of approved effects: "are you making the session copy off of your
effects? why not just give it directly".

- **Locked means do not CHANGE. It never meant do not USE.** Call the approved effects as they are.
- The bench lives in `Assets/TumbangPreso/Runtime/Visual/FxStudy/` (`FxStudyKit`, `FxStudyTrails`, `FxStudyWisp`,
  `FxStudyWhirlwind`, `FxStudyCircle`, `FxStudyLotus`, `FxStudyScorch`, `FxStudySmoke`), its shaders are
  `Resources/Shaders/FxStudy*.shader`, its masks are `Resources/Textures/UltimateFx/`, painted by
  `tools/build_ultimate_fx_textures.py`.
- What each is good for: trail ribbons for anything swept along a path or round a ring; the wisp's shells and its two
  maps (shape and erosion) for soft bodies that travel and are eaten; `noise_simplex_cells` for a wound, streaked
  surface; the wisp's dish for ground swirls; `fleck_sheet` and `glow_soft` for motes.
- **Call the bench code, do not copy it.** If a bench class is hard-wired to the bench, give it a small public entry
  point (visibility, or a table lifted out of a constructor) that changes no value and no order, and prove the bench
  still renders the same.
- A game-side effect is the bench object at a scale and a colour, through inputs the object already has. If the bench
  needs a new input, its default must leave the bench film identical.
- **Never stretch an approved shape to fit a hit area.** Three whirlwinds stretched tall and thin to fit a 0.9 m band
  were rejected on sight ("this looks horrible"). Keep the proportions and change the size, or change the rule.

## 5. Locks: what approval means

- **When he says it looks good, freeze it exactly as filmed.** No further pass on our own critique, including the
  faults we listed ourselves.
- **Back it up at once** under `tools/cutscene_rework/approved/<effect>_<tag>/`: the classes as filmed, its own
  textures and painter, frames, the film, and a README with his words and the date. Put a KEEP comment with his words
  beside the code.
- **Guard it.** After any change to shared bench code, re-film the approved effect and difference it against the kept
  frames. `tools/cutscene_rework/fxsame.py <effect> <tag>` does this (trails against t32, whirlwind against h8) and must
  read SAME. `fxparts.py` does the same for a part of an effect that was approved before the rest.
- **Approval can be partial.** "Rest of the sfx looks good" locked the tapes, ground and motes while the horn was still
  being redone. Lock the part, keep working on the rest, prove the part did not move.
- **A terse remark usually means keep it and add one thing.** Do the smaller change.
- **Shared code is how a lock gets broken by accident.** Fixing the scorch's smoke changed the wisp's smoke because both
  used one class. New behaviour on shared code is opt-in, default off.

## 6. Daylight is not the bench

- The bench is a dark floor with bloom. The game is a sunlit court and **no game camera has bloom**; the owner has not
  decided on adding it. An effect must look finished without it. Film both ways side by side.
- Additive light goes pale on a bright road. The ribbon shader's cover share (`DayLight` on the court whirlwind, 0.50
  against the bench's 0.85) holds colour. That one input was the whole daylight adjustment for the whirlwind.
- Colours shift: teal on the bench read as cyan on the court. Judge colour on the court.
- Check the target's view first, and what the victim sees while inside the effect.
- Report cost for every court version: draw calls and triangles, beside the old look's.

## 7. The picture must tell the truth

- The visible footprint is the hit area. A player dodges what they see.
- Position comes from the same sum the contact uses, so every peer draws it where it hits.
- If the approved look cannot fit the rule, **ask before changing the rule**. For WHIRLWIND the owner allowed it:
  "feel free to rework the ability itself". Then do the whole job: the Core constants, the contact, the telegraph, the
  bots' numbers, snapshots, the description text, the tests (updated, not deleted), the docs, and
  `NetSession.ProtocolVersion` up by one, since old and new builds would disagree about who was hit.
- Tie the size to one constant, so the picture and the rule cannot drift apart.
- Until the rule lands, say plainly that the picture is bigger than what hits.

## 8. Working practice

- The owner's editor is open on this worktree and he plays in it. Compile outside Unity before every save; never start,
  restart or kill Unity unless he says so; film only when it is not in Play. `tools/cutscene_rework/fxbench.sh
  <effect> <tag> <draft dir>` runs the whole cycle for a bench effect and `gale.sh` for Amihan's court film; they hold
  the one film lock.
- One hero's files per session. Brief a session with the owner's words verbatim, the reference file itself (never only
  a description of it), the hard limits, and the order of hand-backs. Ask for an early hand-back, before polish.
- Read every hand-back and look at its stills before passing anything on. A session's own verdict is a claim.
- Paete's abilities are done and are not to be touched. The film tool is only named for him.
- `Logs/` is ignored by git. Old films pile up fast (17 GB in three days); thin them to the latest of each, by moving,
  never by guessing what is unused. Folders that a document names stay whole.
- Nothing is committed or pushed until he asks.

## 9. Where things stand (2026-10-11)

| Effect | State |
|---|---|
| Trails (five ribbons) | Approved, locked at t32 |
| Ice crystals | Approved (first pass) |
| Bench whirlwind | Approved at h8 ("that looks amazing as is"); colour alone released so it can match the court |
| Amihan WHIRLWIND on the court | Look approved at one2: one whirlwind, 4.2 m ground swirl, greener teal with hue wander. Default on in this worktree; `AmihanGaleFront.Whirlwinds = false` restores the old gale |
| WHIRLWIND rules | Being changed to a circle of radius 2.1 m to match the picture; not verified in Play |
| Amihan DRIFT (her E) | Rework started, look only, from the bench parts |
| Ice lotus, magic circle | Built by us without a reference; awaiting his verdict |
| Scorch | Good except its smoke |
| Smoke clusters | Not there |
| Wisp | Smoke restored to what he saw; a small difference remains where it shares two masks with the approved trails |

Open decisions that are his: bloom on the game cameras, and which bench effect goes to which hero.

## Amihan's WHIRLWIND: the rule that goes with the picture (2026-10-11)

The owner had the ability redrawn as one large whirlwind and allowed the rule to change to fit it ("feel free to
rework the ability itself"). The numbers live in `AmihanRules` (Core); one constant, `WhirlwindRadius`, sizes the
contact, the start, the reach, the aiming ring, the bots' reach and the drawn whirlwind.

| | before | now |
|---|---|---|
| what hits | bowed band 3.2 m wide, 0.9 m deep, bow 0.9 m | circle under the whirlwind, radius 2.1 m (lane 4.2 m) |
| body allowance | 0.3 m at the two sides | 0.3 m all round |
| born | 1.0 m ahead | middle 2.4 m ahead (radius + 0.3 m; near edge 0.3 m in front of her feet) |
| far reach | 15.2 m | 18.25 m |
| speed, life, cooldown | 5.5 m/s, 2.5 s, 35 s | unchanged |
| Whirled, immunity, once per victim per cast | | unchanged |

`NetSession.ProtocolVersion` went from 158 to 159 with this, since the host decides who is Whirled.
