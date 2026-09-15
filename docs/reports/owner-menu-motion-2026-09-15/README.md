# Visible road dust and slow cloud motion

Status: implemented, visually reviewed and qualified in local checks and Windows v21.
The revised downloadable video is delivered for owner review; approval is not assumed.
This revises the particles the owner could not see in v20's shared video and adds
his explicit request for slow natural background clouds.

The supplied original artwork remains unchanged. Five low dust gusts and24warm
motes now have enough size and contrast to survive a small shared video. Mote
heights vary irregularly; all remain along the middle-distance road. Buttons and
foreground props remain clear. Reduced motion removes the drifting particles.

The sky uses a single continuous flow, with a linear distance mask protecting
roofs, wires, poles and foliage. The slow unscaled cycle starts at about one source
pixel per second; the flow eases to zero around stationary objects. The36pixel
maximum with64pixel smoothing avoids folding. This gently drifts/deforms the
painted cloud shapes; it does not regenerate the scene or move the whole image.
The owner's Reduced motion preference restores the original static sky.

## Iterations rejected during visual critique

Earlier crossfades between two displaced pictures left duplicate cloud/leaf
contours. A second geometric-holdout crossfade left small fins at cloud edges.
Their numerical motion/static-region tests passed; the visible artifacts made
both unacceptable. Example captures are retained here. The final continuous flow
uses one displaced sample and a smooth distance ramp instead of image crossfades.

## Current evidence

- Stronger-dust home test:1/1PASS, all ten existing PC viewport checks retained.
- Sky flow v4:1/1PASS. Positive24pixel drift changes11877sky pixels with zero
  changes outside the sky opening. Named actual roof/pole/tree samples stay fixed.
  Reverse36pixel drift also preserves the surrounding scene. Inspected both
  close crops: no former duplicate contours or fins. Reduced motion resets flow.
- Windows v21: build succeeded, 1097 MB in 57 seconds. Native menu-only review
  passed, including 1678 silent loading/login frames, Guest starting menu music,
  three physical window sizes, normal/reduced motion and Settings/Credits/Play/Back.
- The 12.07-second H.264 MP4 is 1920x1080, encoded at CRF16 from timestamped native
  frames. Compressed-frame crops were inspected: warm dust motes remain visible.
  Normal capture: 412 sampled frames, 377 dust vertices, cloud drive from
  1.84234 to 14.34624 source pixels horizontally. Reduced mode is stationary.
  Recording readback/encoding overhead means this is not an FPS benchmark.
- Runtime DLL SHA256:
  28a45da4aaa11418e74c0d6af48c533720c1ba52be0b052edd38ae73f4b15114.
  Build guard57207207845a; native PID10124 exited. Shared standalone input settings
  were unchanged. No Desktop replacement, paid tools or C4 networking edits.

After this requested motion revision, resume the saved full gameplay queue.
