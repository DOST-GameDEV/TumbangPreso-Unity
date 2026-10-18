# Corrected Cove and Kanto handoff: visual comparison

Read-only inspection of fresh `Logs/map-opening-visual1007/handoff-wide-fixed1080/frames`, primary native47708/session53356. All22 Cove and19 Kanto PNGs postdate launch and their PNG headers are1920x1080. Metadata aspect1.7778 is the explicitly set world-camera lens; the640x480 batch screen does not qualify player-window or overlay layout. No reviewer source changes, launches or tabs.

Inspected Cove018/020/021 and Kanto014/016/017/018 against the original `new-map-wide1080` Cove020/Kanto017 failures.

- **Cove020 at43.331s:** the taya is fully visible left of center and the can is clearly separated to the right. The court/hut horizon is upright and no foreground object blocks either subject. Original020 showed tilted huts and empty court with both subjects absent. Corrected eye(9.14,4.69,1.64), forward(-.91,-.28,-.31), fov57.23.
- **Cove021 at43.879s:** all four players are contained in the whole-court return view. No clipping or environment occlusion is visible.
- **Kanto017 at61.353s:** all four players and the can remain visible in a level elevated orbit. Trees, benches and traffic stay outside the subject lines of sight. Original017 showed a banked street with the cast/can absent. Corrected eye(8.05,7.48,-10.09), forward(-.58,-.40,.71), fov70.50.
- **Kanto018 at61.959s:** the whole-court view contains all four players with no new foreground obstruction or subject crop.
- Both last-frame metadata reach eye(0,9,-14), forward(0,-.44,.90), fov78. The can aligns partly behind the taya in that returned gameplay view; this is the inherited endpoint alignment rather than new orbit/environment occlusion. Do not claim the can is completely unobscured at the endpoint.

The previously demonstrated empty/banked handoff is absent from these corrected inspected frames. No new visual handoff blocker is apparent. Portraits remain unchanged and the previous separate portrait-composition concern is not resolved or expanded by this review.

Limits: roughly0.6-second snapshots cannot prove all intermediate motion, collision freedom, easing quality, multi-frame endpoint stability or frame pacing. Camera-only images omit screen-overlay captions/ink. Root's separate continuous10-case numerical regression is stronger trajectory evidence but is not replaced by this visual inspection. No packaged-player or overall presentation approval follows from this slice.
