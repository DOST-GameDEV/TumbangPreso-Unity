# Basilio (Dante) presentation: direction and the owner's verdicts

The spec is [plan.md](plan.md) (with [research.md](research.md)). This file records what the owner answered and decided,
in his words, and what each answer became. Newest round at the bottom.

## 1 · The plan's answers (owner, 2026-10-16: *"looks good pede na go"*)

The owner approved the plan as written, so every open question in plan section 7 takes the plan's proposal.

| # | Question | Answer | What it becomes |
|---|---|---|---|
| 1 | Concussed length | the Wiki's: 75% slower for **5 s** | `StatusRules.ConcussedSeconds` 5.0, the status, Boulder and Continental Drift texts; protocol 166 (commit c74ea937b). Reported to the owner for a veto |
| 2 | The knock opening | yes: on one knee, two calm knocks, his gold eye opens, then the punch | the cutscene's first beat, 7.0 s kept |
| 3 | The victims on screen | yes: the real players stumbling on their own drifting plates, then bracing as the fists come down; still ending on the fist | `HeroIntroductionScene.DanteTake.cs` |
| 4 | Bastion's look | waist-high basalt slabs plus a see-through heat field with a molten rim and a clear middle | Bastion's parapet |
| 5 | Unstoppable | keep the approved orbiting shields and fitted armour exactly; add the shatter-off, the plates assembling from the feet up, the refusal flash, the cooling warning and the crumble | `DanteCarapaceVisual`, `DanteStatusShatter` |
| 6 | Crest and motif | two hooked horns over a split stone, left horn chipped, shown three times only; a low synthesised carabao-horn (tambuli-like) call, an instrument, never a voice | the emblem and the theme's motif |
| 7 | The creature | a horned basalt mountain beast in his own family, a typed glb (`tools/build_dante_props.py`); turnaround to the owner BEFORE it goes into the scene | `Resources/Models/DanteProps/beast.glb` |

## 2 · Built so far

| Step (plan section 8) | State |
|---|---|
| 2 Concussed presentation | built: the victim's ochre rim (`Shaders/ConcussedVignette`), the double image in the grade, the muffle and a ringing cue (silent until the reworked sounds are released); observers see three typed basalt pebbles circling the head |
| 3 Earthbound | built: he leans into a shove, grit kicks off his heels, two heel furrows scraped along the real slide (measured), grey then crumbled after 2.5 s |
| 8 The creature | v2 modelled; turnaround sent for a verdict ([beast-turnaround-v2.png](beast-turnaround-v2.png)) |
| 9 Cutscene, first films (2026-10-17, internal, not sent) | filmed on his screen in a Bayan Plaza match (`DanteDriftFilm`, seven rounds). Fixed: he floated the whole scene (the phase grounded his already-sunk first frame; it now keeps a negative first lift); every plate was under Bayan's raised court (the stage now measures the court); the seams grew from the stage origin, not the fist; the knock, eye, punch and rise shots aimed at a smaller body (now framed from his head, his real knocking hand and the beast's eyes, `HeroIntroductionScene.DanteFrame.cs`); the sea outshone the seams; the near embers were blobs; the stomp lost the players. Rounds 8 to 10 (measured, `[DanteMeasure]` now logs each horn and the sockets): the horn never touches him (horn-right's root stays 0.7 m ahead of his hand, its tip passes 1.7 m to his right; the side camera only saw it in front of him), so it is unchanged; the eye lens looked through the muzzle (the face rises pitched 42 degrees nose up), so it now sits 1.8 m out along the face's own normal and opens wide for the horn tips first; the crest never drew at all (its mesh had no uv, and the glow fades by uv), so all three now show, the sky one smaller and higher so the whole emblem stands over the beast; the victims stood in the molten gap (a 1 degree rock sank each plate's far half under the sea, and a target straight ahead stood on the crack), so loose plates ride 0.12 m up, rock half as much, and targets stand within 1.2 m of their plate's middle. v1 sent (section 3) |

## 3 · Owner verdicts

| Version | Sent | The owner said | What changed |
|---|---|---|---|
| beast v2 turnaround | 2026-10-17 | (waiting) | |
| cutscene v1 (`dante_ult_v1.mp4`, his screen, frames 20 to 228, draft theme only) | 2026-10-17 | *"look ennding of ult wtf even is happening here"* (on the last frame: the stomp's ink-on-orange impact frame) | the stomp sat 0.12 s before the cut, so its two impact frames were the film's last picture. It lands at 6.53 now (the hang 6.25 to 6.50; his keys and the stage together) and the fists stay down 0.47 s, the seam racing at the players |
| kit v1 (`dante_kit_v1.mp4`: Earthbound, Boulder, Bastion, Unstoppable; his screen, the court, who it reaches; `DanteKitPlayProbe`, the armour's middle 6.6 s cut; no draft cue is wired yet, so it is nearly silent) | 2026-10-17 | *"make the hologram look more like its from earth and magma"* (on Bastion's heat field); *"what even is this it looks horrible i dont get wtf its supposed to do what it does what it communicates"*, *"refine this way more wtf"* (on Boulder's loaded slipper on his screen) | Boulder: v1's crust was only a grey plate pattern painted on the slipper, so nothing read as stone; now thirteen typed basalt chunks, each snapped to the slipper's real surface and big enough to overlap, pack round its heel, rims, sole and toe in the cast's three steps, the strap and buckle left showing, a molten joint under each (the painted seam layer is retired). Bastion: the clean pane with a yellow rim is now a broken crust of dark basalt cells round the rim with magma in the cracks, a lava glow welling off the slabs and a few embers, the middle still clear |
| cutscene v2 (`dante_ult_v2.mp4`: the stomp at 6.53, fists down to the cut) | 2026-10-17 | (waiting) | |
| kit v2 (`dante_kit_v2.mp4`: the stone Boulder, the earth and magma Bastion) | 2026-10-17 | (waiting) | |
