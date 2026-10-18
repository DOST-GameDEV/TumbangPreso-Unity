# Keep spectator controls discoverable after hiding them

The native HUD disabled its entire spectator readout when controls were hidden, including the only hint for showing them again. Original Unity18732 reproduces that failure; visible-control and normal-player controls pass. Candidate9268 passes all three cases and renders the actual readout at960x540 and1600x680. Both images were inspected: the compact bound C show-controls line fits. These are isolated UI renders without a live map background or physical-device interaction.

Hidden controls now leave only the current binding plus show controls. The expanded state keeps its existing status/hide/clean-feed hints. Normal players receive no spectator readout. Clean Feed retains its existing whole-canvas gate, so the hint does not override it. No camera, hero, input binding or network contract changes.

Both terminal runs restore21150 frozen inputs and216 native metadata/settings deltas each, shared input/editor preferences, quality settings and the isolated seed. Failure evidence is preserved. Broader spectator help, touch/operator flow and continuous viewing quality remain open.
