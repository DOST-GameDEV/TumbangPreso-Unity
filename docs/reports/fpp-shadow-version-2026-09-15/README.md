# First-person slipper shadows and visible version labels

Owner requested both corrections on2026-09-15 during pending-cast qualification.
Implementation and focused engine checks are complete and published with the
nativev16 source checkpoint. No Claude-reserved file was changed.

## First-person slipper

ViewmodelArms created its camera-mounted HeldSlipper MeshRenderer with Unity's
default shadow casting On. Its copied world materials therefore allowed that
presentation object to cast an independent scene shadow. The renderer now starts
with ShadowCastingMode.Off. World slipper and body settings are unchanged.
Sean Ignition and Zack Magnet attachments already inherit their source renderer's
shadow mode, so they follow the first-person copy without changing the world copy.

The first probe failed compilation from a missing CharacterVisual namespace;
this was corrected, not counted as a runtime result. The corrected baseline
failed on the actual first-person HeldSlipper: expected Off, observed On.

The fixed focused PlayMode probe passed1/1, covering four states: Zack plain and
charged, Sean plain and charged. The shoe stayed visible, charge attachments were
actually present, all first-person renderers cast no shadow, and the actual world
shoe retained its prior shadow mode. The ordinary camera captures were inspected.
This verifies the shared slipper presentation, not every character's hand design.

Receipts: before-v2 guard0fec51aa9d0c; fixed guard8f7d879995ec. The fixed run
restored2existing named-profile files and0shared Editor input values. Earlier
baseline imagery had nearby actors crowding the frame; they were parked away for
the final capture. Do not present those images as a controlled pixel A/B.

## Version display

GameVersion.ApplyTo now hides the Text widget and clears its contents. Existing
AttachTo callers retain a valid hidden reference. Current and retained home/HUD
builders use that helper. VersionStamp applies it in Awake and OnEnable, covering
the older authored menu scenes and their reactivation before rendering.

Application.version, GameVersion.Value, build identity and network version checks
remain intact. This removes the visible label, not the metadata needed to identify
binaries. No unrelated UI layout or login artwork changed. Current source search
found no separate direct version display outside these shared routes. Compilation
and the arena's UI initialization ran with the focused shadow probe; this is not
a claim that every menu was newly captured or a full UI suite was rerun.
