# LIGHT-1.9 on the Mac: the bright look at the owner's window and at the Mac's own screen

Native macOS player built by `GameBuilder.BuildMac` from the commit that adds the window
frame to `WorldGraphicsProbe`, Apple M5, Metal. Every map, Balanced tier (the shipped
default), four parked players, the same seat and standing spot as the 1920x1080 matrix.
The window frame is the real path a player sees: the rig's camera straight to the back
buffer, with the viewmodel and the HUD composited over it. Each window is measured for 120
uncapped frames with the look on, photographed, then measured again with
`WorldCueProfile.WorldLighting` 0 in the same binary on the same window.

## The owner's short wide window, 1600x680

Requested 1600x680, and the player reported 1600x680 on every map (`window.csv`).

| Map | Look on, mean / p95 ms | Look off, mean / p95 ms | Look cost, mean |
|---|---|---|---|
| Eskinita | 2.48 / 4.18 | 2.26 / 2.76 | +0.22 |
| Bayan Plaza | 2.04 / 2.17 | 1.93 / 2.01 | +0.11 |
| Ilalim ng Tulay | 5.12 / 5.49 | 4.91 / 5.39 | +0.21 |
| Sa Bubong | 2.27 / 2.38 | 2.12 / 2.25 | +0.15 |
| Lagoon | 2.54 / 2.78 | 2.39 / 2.54 | +0.16 |

The five frames are in `owner-window-1600x680/`. Inspected: every map carries the look at
this shape (bright sky, coloured haze, no black ink on the world), and the HUD and the
first-person arms sit where they do at 16:9. Two things read at this shape exactly as the
LIGHT-1.6 taste list already describes them: Sa Bubong is the palest and flattest of the
five, and the dark brown first-person arms carry a near-black hull line.

## The Mac's own fullscreen, 2940x1912

No size requested; the default profile is fullscreen, so `GameSettings.ApplyDisplay` put
the player on the desktop's backing resolution, 2940x1912 (the display reports itself as
2560x1664 Retina, and macOS scales the frame down to it).

| Map | Look on, mean / p95 ms | Look off, mean / p95 ms | Look cost, mean |
|---|---|---|---|
| Eskinita | 10.26 / 37.30 | 8.95 / 33.37 | +1.31 |
| Bayan Plaza | 8.93 / 33.38 | 8.20 / 27.50 | +0.73 |
| Ilalim ng Tulay | 14.21 / 47.70 | 13.32 / 43.40 | +0.88 |
| Sa Bubong | 10.84 / 38.60 | 9.73 / 34.19 | +1.11 |
| Lagoon | 11.31 / 42.98 | 10.01 / 38.01 | +1.30 |

The look costs 0.73 to 1.31 ms here, 2.3 to 3.5 times each map's 1080p cost in the
windowed run (0.32 to 0.50 ms), in line with the 2.7 times more pixels its bloom chain and
edges have to cover. **The p95 is three to four times the
mean with the look OFF as well** (27.5 to 43.4 ms), so the hitching belongs to fullscreen
at this resolution on this Mac, not to the look. It is recorded in `docs/TODO.md` beside
LIGHT-1.9 rather than chased here. `mac-fullscreen-2940x1912/contact-sheet.png` shows the
five frames at quarter size; the look renders correctly at this shape too.

## Why the first attempt photographed the wrong shape

The first run asked for `-screen-width 1600 -screen-height 680` and got 2940x1912, because
`GameSettings.ApplyDisplay` overrides the command line at boot (fullscreen to the desktop
resolution, windowed to 1600x900). The probe now re-applies the requested size as a window
before the frame and writes the requested, reported and captured sizes side by side, which
is how the mismatch was caught instead of being filed as the owner's shape.

## What this does not cover

- Metal, not Direct3D. The Windows tournament machine is still owed the same run:
  `tools/graphics_review.py` now defaults `--window` to 1600x680, so one run there measures
  the matrix and photographs the owner's shape on D3D.
- A parked warm-up frame, not combat. The same limit as the 1920x1080 matrix.
- The 1920x1080 matrix in these two runs agrees with the earlier Mac numbers: look cost
  0.32 to 0.50 ms on Balanced in the windowed run.

# LIGHT-1.6: the dark-skin hull as a choice

`WorldLookProfile.CastInkFloor` keeps the cast hull at no less than that share of the
luminance of the colour it frames. **It ships at 0, which is today's hull exactly**, because
this is the owner's call. `WorldCourtCueTests.BrightLookDarkHullChoiceCaptures` renders the
choice: seat 1 with a fresh profile's character and slipper (the brown-skinned arms and black
loafer of the native probe frame), first person at 1600x680 and the close cast shot, on Ilalim
and Eskinita, at floors 0, 0.25 and 0.35 and then 0 again. PlayMode 1/1 on the Mac. The floor
moves 0.35 to 0.69 per cent of each frame; 0 against 0 moves 0 pixels.

Arm edge on the first-person frame (pixels within 2 px of skin that the floor moved):

| | Floor 0 (today) | 0.25 | 0.35 |
|---|---|---|---|
| Ilalim, darkest tenth | (25,19,18) | (47,32,31) | (55,38,35) |
| Ilalim, median | (74,55,38) | (87,60,42) | (95,63,44) |
| Eskinita, darkest tenth | (13,9,12) | (33,29,29) | (41,34,33) |
| Eskinita, median | (74,46,29) | (87,52,34) | (96,56,38) |

`hull-choice/arm-edge-zoom.png` shows it at 3x: a near-black line at 0, a dark brown one at
0.25, a slightly lighter brown at 0.35. At native size the change is subtle; the edge stops
reading as ink rather than becoming an obvious brown line. The black loafer's edge moves from
near black to a dark grey, (15,15,17) to (30,30,34) at 0.25 on Ilalim, because its checks are
dark grey rather than black.

It is not only dark skin. In the cast shot, sorting each moved pixel by the brightest colour
within 3 px of it, 0.25 also lifts the hulls around tan skin, greys and greens (2,006 moved
pixels on Ilalim), and 0.35 adds the orange skin and shirts (3,329). Any floor is a change to
every hull darker than it, which is what the owner is choosing.

**A fade that spared near-black colours was tried and removed.** `ViewmodelArms.SkinMangKanor`
(49,36,29) sits at luminance 0.020, level with dark hair, so any band that spares black spares
the darkest skin too, and on these brown arms it cancelled most of the lift (darkest tenth only
(55,39,30) at 0.35 in that run). The first capture also photographed the wrong person: the
editor's saved character pick was light-skinned, which is why the test now copies a fresh
profile's picks.
