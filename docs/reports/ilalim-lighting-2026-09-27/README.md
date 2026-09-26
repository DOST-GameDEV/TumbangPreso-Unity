# Ilalim ng Tulay lighting report, 2026-09-27

Owner report: on Ilalim ng Tulay the lighting and some objects change depending on the view
angle and the distance. The owner's frames show the street losing its sun shadows and going
flat and bluish when the camera stands farther from the shop rows, and the shadows returning
closer in.

## What was measured (commit 62a4529b plus the loading work in this branch)

The metric at every pose is the frame's mean luminance with the map's `Sun` casting shadows,
minus the same frame with that light's shadows off. Zero means the sun's shadows were not
drawn at that pose.

| Path | Poses | Poses with no sun shadow |
|---|---|---|
| PlayMode, match camera rendered offscreen, 121 positions x 8 yaws | 968 | 4, all with the camera inside a character body |
| PlayMode, same, Low / Balanced / High x Standard / Nostalgic | 6 x 96 | 0 |
| PlayMode, same, MSAA 1 / 2 / 4 / 8 | 4 x 96 | 0 |
| Mac player, real back buffer, map loaded directly (`-tp-shadowsweep`) | 392 | 0 |
| Mac player, real back buffer, through HOME and the loading curtain (`-tp-shadowsweep-hub`) | 392 | 0 |

`player-hub-route-sweep.txt` is the last row's raw output; `player-contact-sheet.jpg` is a
sample of its frames.

## Eliminated

- The match-end portrait's `PreviewKey` directional light (soft shadows, layer 30 only) is
  live in the arena, but switching it off changes nothing at any pose.
- Fog: Ilalim's look starts fog at 36 m.
- Occlusion culling: the scene has no occlusion data.
- Graphics tier, lighting style, MSAA count.

## Not yet reproduced

The owner's frames came from the Unity editor's Play mode. The editor also renders the Scene
view camera, and `WorldLookPresentation` swaps shader globals per camera in `onPreCull` /
`onPostRender`, so editor-only interleaving is the leading remaining suspect. The owner was
asked whether the same thing happens in a built player and which settings were in use.
