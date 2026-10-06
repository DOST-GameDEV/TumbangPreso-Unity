# Spectator POV keys retain camera ownership

After the public MatchInstaller watch handoff, the solo debug switcher still consumed F1-F4 and enabled a gameplay reader while disabling the spectator. Original Unity22256 reproduces both launch-flag and actual-watch-only cases; normal solo gameplay handover passes. The switcher-only correction passes all three cases in3440.

A separate check then exposes the other half: Camera.StepBroadcastKeys suppresses POV keys whenever the switcher merely exists. Unity19864 fails with a null watched subject even after body takeover is prevented. Final18008 passes all four cases: F2 chooses the actual seat's spectator POV, the watcher stays enabled, its body reader stays disabled and ordinary solo handover still works.

The switcher now yields to launch/HUD spectator state or an enabled watcher. Its cached camera lookup only runs on a seat shortcut. The active spectator consumes its POV keys without the old per-frame debug-component scan. No gameplay skills, wire contracts or authored assets change. Virtual keyboard input and the actual consumers/public handoff qualify this boundary; physical keys, pad/touch interaction and every camera stage are separate acceptance.

All four terminal runs restore21152 frozen inputs and216 known native metadata/settings deltas each, input/editor preferences, quality settings and isolated profile seed. Both failures and intermediate passes remain retained. Laptop owns Director and its separate natural map/roster/body-clearance investigation.
