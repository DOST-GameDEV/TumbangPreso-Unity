# Above-can false contact

The baseline actual flying slipper knocks the visible tutorial can down while
metres above it. Lata.Connects previously tested only X/Z, creating an infinite
vertical contact column.

Contact now also requires overlap with the measured Visual mesh height plus the
existing slipper hit radius. The horizontal stance window is unchanged. Only
Visual meshes count, excluding world cues and restoration shields; the cached
interval refreshes when the skin model is replaced. The fallback uses the same
0.3 m height as the fallback cylinder. Protocol120 distinguishes new contacts.

Final two native PlayMode checks pass:
- Actual high overflight misses; actual body-height flight still knocks down.
- All six authored skins accept the top-minus-epsilon edge and reject top-plus,
  below-bottom and outside-horizontal edges through repeated model replacement.

Baseline one expected failure, final2/2, no fixture repair. Frozen final inputs
and owned source agree; no new OOM. This is native host-side simulation and mesh
boundary evidence, not a fresh player build or actual remote-peer qualification.
