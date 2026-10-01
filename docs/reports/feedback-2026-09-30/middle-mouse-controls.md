# Latest Actions Layout, 2026-09-30

The owner edited the same Feedback row again. Current defaults/order are left
mouse Throw/Tag, middle mouse Shove/Lunge, right mouse Retrieve/Reset, wheel up
Curve Right, wheel down Curve Left and F Interact/Ready. Interact has a normal
settings row; the separate Hold Interact label stays retired. Binding/action IDs,
saved overrides, pad and touch mappings are retained.

Ready suppression follows actual shared controls. A held F consumed by Ready
requires release before interaction. An older saved Lunge/Ready shared control
keeps the same protection, while independent middle mouse remains separate.
The interaction read still occurs before possession's existing early return.
No kit or gameplay outcome behavior was changed; protocol97 remains.

Four distinct native EditMode cases pass: defaults, requested order, conflicts
and an older saved Grab override. The first filter accidentally named an absent
conflict case and ran3cases. The correct conflict case then passed1/1; the three
unchanged cases were not repeated. One native real mouse/wheel/F case passes,
including middle-click intent and Ready hold/release/fresh F interaction. This
is local input evidence, not physical-device or real-peer certification.

- [Defaults/order/older override](checks/middle-edit.xml)
- [Binding conflicts](checks/middle-conflicts.xml)
- [Actual input/Ready context](checks/middle-play.xml)
- [Frozen inputs](checks/middle-inputs.json)
