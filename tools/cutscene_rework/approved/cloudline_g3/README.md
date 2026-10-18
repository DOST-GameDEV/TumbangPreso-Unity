# The cloud, approved 2026-10-11

Toon smoke as a looping emitter of solid balls (`FxCloud`, `FxStudyCloud.shader`), built once and moved by the
shader, with its own line round its lobes. Bench names: `cloudline` (this folder), `cloud` (the same without the
line, frames in `../cloud_g1/`), `cloudlinedark`, `clouddark`, `cloudburst`.

The owner's words, in order, on 2026-10-11:

- of the size over a ball's life (film c6): "ye size looks good"
- of the first version stood in a match: "the cloud is really laggy. are you redering this via shader or something else"
- of that version under the game's ambient occlusion: "i feel like an outline shader would work better instead of ao"
- "remember this needs to be heavily optimized"
- of this version, stood in a match on F12: "looks good. i want a smaller version of this to replace the 2d clouds in
  dante's ability"

What is locked: the size curve (`SizeOverLife`, peak at 60 to 75 percent of life), the two flat tones and their hard
step, the line (`FxStudyCloud.Line` 0.022 m, `_LineSize` 0.05 to 0.20, `_LinePush` 0.010), 130 balls a column, the
tones of the two columns, no crease tone, no part in the game's ambient occlusion.

Check a later film against it: `py -3 tools/cutscene_rework/fxsame.py cloudline <tag>` and `... cloud <tag>` from the
worktree root. Film g4, made after the class was given `FxCloud.Build` for other effects to call, measured 0.000 of
255 against these frames.

Files here: the class, the shader and the F12 demo as they were (`*.txt`), thirteen frames of film g3, the film, and
the sheet beside his picture.
