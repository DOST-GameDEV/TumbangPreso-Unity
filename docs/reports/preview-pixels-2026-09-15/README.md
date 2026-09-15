# Preview textures follow the displayed panel size

The baseline failed because a panel spanning1200 physical pixels still used a
600px render texture. ModelPreview sized its target from local layout units and
ignored the canvas/display scale.

The target now uses projected panel corners to measure display scale, preserving
the authored aspect ratio. Both dimensions share the existing2048-pixel cap.
Replaced owned textures are released and destroyed. Models, palettes, framing
intent, selection state and saved choices are unchanged.

The focused resolution/aspect/cap regression passed after the change. The actual
picker's preview/save/cancel route also passed with ten PC sizes and the current
people/can/slipper content.4K character and equipment captures were inspected for
framing; render-target dimensions were verified directly. No physical4K monitor
claim or native-player qualification is implied by those Editor captures.

Receipts: baseline9e522dbb082e, fixed333d2d3fd101, actual picker32f484be137c.
Nativev4 remains the preserved candidate and predates this source update. Include
the change in the next candidate after the current UI batch is qualified.
