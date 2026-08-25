# The Godot repo's boards, carried over

⚠️⚠️ **THESE ARE COPIES AND THE GODOT REPO IS THE ORIGINAL.** Edit them there, then copy
them here. Two editable copies of a source of truth is exactly the failure `CLAUDE.md` §3
describes for the balance layer, and it would be worse here, because a colour law that
drifts between two repos produces a game that disagrees with itself and nothing fails.

They live here because 🧑 2026-08-18 asked for them, and the reason he asked is the more
useful half of it:

> *"what made the godot game good is that everything is within theme, saturated and shit,
> i think theres a theme board there too or smth. ur shit isnt. the unity has no theme and
> shit it js tries to copy"*

That is a fair reading of what a port does by default. Matching a reference frame pixel by
pixel gets you a copy of one screenshot; it does not give the project a rule it can apply
to the next thing anybody adds. These files are the rules.

| File | What it decides |
|---|---|
| `Art_Direction.md` | The colour law, the scale and height laws, arena geometry, and which tool produces which asset. **§1 is the one that never bends: orange is OFFENSE, blue is DEFENCE, and nothing else in the frame may sit near those hues.** |
| `Design.md` | The balance source of truth for BOTH projects. A number in the code must match a number here or one of the two is a bug. `docs/Port_Plan.md` §7.1 lists the four that currently disagree. |
| `HUMAN.md` | The standing instructions from 🧑 in his own words, which is the record of what has already been asked for and rejected. |
| `Handoff_Open_Issues.md` | What was still open on the Godot side when the port started. |
| `refs/` | The reference art the props were drawn from. History: `Art_Direction.md` §4a records that the drawing-derived slippers were deleted and must not be rebuilt. |

## What the Unity side already carries, and where

The theme is not missing here, it is spread out, which is most of why it did not feel like
one. What maps to what:

| Godot | Unity |
|---|---|
| `scripts/ui/ui_theme.gd` | `Assets/TumbangPreso/Runtime/UI/UiTheme.cs` — all four bands, including the full `ENV_*` set |
| `scripts/systems/env_toon_pass.gd` | `Assets/TumbangPreso/Runtime/Visual/EnvColourPass.cs` — the facade tints, roof atlases and road tint |
| `assets/models/materials/toon.gdshader` + `outline.gdshader` | `Assets/TumbangPreso/Shaders/Toon.shader` — one shader, two passes |
| `person_palette.gdshader` + `person_*.tres` | the same `.tres` files, read by `RosterBookBuilder` into `RosterEntryAsset.Palette` |
| The Environment's `adjustment_*` block | `Visual.MapGrade` in the scene, applied by `Visual.ColourGrade` on the camera |

## The colour space, written down once

⚠️ **THREE SEPARATE BUGS ON 2026-08-18 WERE THE SAME MISTAKE**, and this table is here so a
fourth is not written. The project renders in **Linear**. Unity converts sRGB to linear for
you on exactly one path — a property declared `Color` in a shader's `Properties` block, set
through `SetColor` — and on no other.

| Value | Kind | Rule |
|---|---|---|
| A swatch (a palette entry, a facade paint) | colour | convert to linear before it is shaded. `SetColor` does it; `SetVectorArray` does **not**, which is what made the whole cast pale. |
| A tint (`RoadTint`, a facade multiply) | ratio | do the multiply in sRGB and convert the PRODUCT once, the way `mat.albedo_color = base * tint` does in Godot. |
| An energy (`ambient_light_energy`, `light_energy`) | quantity of light | multiply in LINEAR, then convert back for the field that expects sRGB. |

`Visual.ToonSkin.ToShading`, `Visual.EnvColourPass.Tinted` and
`EditorTools.MapKit.TscnImporter.Energised` are the three places that implement those three
rows, and each carries the measurement that found it.

## How a look claim gets settled here

Not by eye. `Logs/shots-godot/` holds frames captured out of the running Godot build;
`Assets/TumbangPreso/Tests/PlayMode/GameplayShots.cs` captures the matching frames out of
this one, and `tools/compare_tone.py` reports the difference per band with a number on it.

```bash
python tools/compare_tone.py
```

When something is off and the cause is not obvious,
`Assets/TumbangPreso/Tests/PlayMode/ToneSweep.cs` photographs a whole parameter sweep in one
play run and `tools/read_sweep.py` reads the answer off it. That pair is what found the sun
being imported upside down after three passes had blamed the grade.
