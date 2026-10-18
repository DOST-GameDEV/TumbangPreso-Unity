# Recorded map previews

Map selection and voting display the output of the actual MapPreviewSurface
camera. They retain its pose,58-degree lens,16:9 framing,26-second seven-degree
sway, lighting, grade, outlines and ambient motion. Lobby characters still use
the live camera. Login warms clip metadata and first-frame posters asynchronously;
only the selected background opens a decoder. Full map scenes are not warmed
solely to display these recordings.

## When a map changes

Regenerate that map's video and poster after changing its geometry, materials,
lighting, ambient motion or preview framing. Keep the scene ID from SceneFlow.Maps,
not its display name. Capture1920x1080 at30fps for780 frames with the live HDR/MSAA
target. The encoder uses H.264,CRF14,yuv420p,slower preset,fast-start and no audio.
The poster is the first original rendered frame, with no image downscaling.

Work in a validation-only checkout with its own physical Assets/Library and
company/product identity. Follow [Testing](TESTING.md) to preserve source inputs,
named-profile files and Editor preferences before launch and restore their exact
bytes after the process ends. Keep generated differences and failure evidence.
Use the installed project-matching Unity version and one heavy job per machine.

The recorder runs only in batch mode. Use a fresh internal output directory;
existing media is preserved. Replace these paths and map IDs with your isolated
checkout, matching installed Editor and installed ffmpeg:

```powershell
& $unity -batchmode -force-d3d11 -projectPath $isolatedProject `
  -executeMethod TumbangPreso.EditorTools.MapPreviewRecorder.Record `
  -tp-preview-maps 'Eskinita,Arena' `
  -tp-preview-output 'Logs/preview-capture-new' `
  -tp-preview-encoder $ffmpeg `
  -tp-profile $isolatedProfile -logFile 'Logs/preview-capture-new.log'
```

Omitting the map argument records all registered maps. The recorder starts from
an empty scene, uses the real live preview despite existing recordings, and
unloads its owned scene before the next map. It writes a loop, poster, encoder
log and capture-source fingerprint per map. A successful exit is recording
evidence, not a gameplay test.

Before frame0 the recorder yields two frames for EnvColourPass.Start and the
world look's first ground-discovery Update. Starting sooner records a dark
startup frame that flashes again on every loop. Check the log's ground-ready
transition and captured look weight, rather than treating scene-load completion
as visual readiness. Current captures use look weight1; changing a player's
live lighting preference does not recolour an already-recorded movie.

Inspect the native frames, first/last-frame seam and a complete normal-speed loop.
Use ffprobe to check1920x1080,30/1 fps,780 frames and26 seconds. Test actual Unity
decoding, immediate poster display, map switching, hidden/reopened previews and
reduced motion. Preserve authored quality; do not shrink footage to evade a
repository limit. Each Git blob must be below100MiB; a more efficient encoder
preset can retain the same CRF and dimensions.

## Media and source receipts

After inspection, copy the checked pair to:

```text
Assets/TumbangPreso/Resources/UI/map-previews/<scene>-loop.mp4
Assets/TumbangPreso/Resources/UI/map-previews/<scene>-poster.png
```

Retain the existing GUIDs on regeneration. Import the poster uncompressed,
2048 maximum, no mipmaps, no non-power-of-two rescaling, sRGB, clamp and bilinear.
Check the video import without transcoding away its captured detail.

After importing that exact pair into the matching isolated source, call
`MapPreviewFreshness.WriteReceipt(sceneId, capturedFingerprint)` from an Editor
script, passing the captured source file's value. It refuses a changed map/look
and records exact media hashes and capture dimensions. Include the resulting
`<scene>-source.json` and Unity metadata with the map/media commit. Never stamp an
unknown old clip as current. Existing recordings may adopt a new fingerprint
format only after the actual captured source and current dependency/camera
contracts are proved equivalent.

`MapPreviewFreshness.SourceDescription(sceneId)` exposes the canonical inputs
behind a fingerprint for an evidence-backed comparison. Effective selection
camera values are included; map labels/descriptions are not rendering inputs.
The gameplay/replay installer is excluded because isolated preview capture holds
`MatchInstaller.PreviewOnly` and begins from a fresh empty PlayMode. Attached map
scripts and all actual visual dependencies still invalidate their footage.
Preserve the source description and exact source revision when demonstrating a
format migration; changing a fingerprint is not evidence of equivalent footage.

Use **Tumbang Preso / Maps / Validate Recorded Previews** before publication.
This explicit check validates all registered map receipts without recording
footage, loading arena scenes or adding a build execution barrier. Fingerprints
include scene dependencies/import settings, attached map scripts and their
partials, shared look code, dynamic visual resources and the preview's
camera/environment methods, including world-look/cue profiles and lighting-style
definitions. Git's CRLF
conversion is ignored for code/YAML; binary assets and media remain byte-exact.

Windows native H.264 playback is checked. Linux currently retains the same sharp
poster when its portable clip is absent; portable-codec and device acceptance
must be reported separately. A matching hash does not establish visual approval,
seam quality or gameplay correctness.
