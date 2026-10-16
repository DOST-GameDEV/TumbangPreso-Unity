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

## 3 · Owner verdicts

| Version | Sent | The owner said | What changed |
|---|---|---|---|
| beast v2 turnaround | 2026-10-17 | (waiting) | |
