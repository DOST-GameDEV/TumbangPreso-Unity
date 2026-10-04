# Haunt radial purple sight

The victim camera now uses radial camera distance rather than a flat eye-depth
slice. Projection-based reconstruction follows camera FOV/aspect/lens shift.
The existing2.5m clear/7m obscured range is preserved. Distant vision blends
toward Nemu's existing deep-purple ink instead of multiplying into black.
Status timing, actions, audio, target detection, HUD marker hiding and remote/
spectator isolation are unchanged. No packet or protocol change.

## Native rendering evidence

Original shader: both controlled equal-depth aspect cases fail radial falloff;
un-Haunted control passes. Original image is visibly flat. Candidate4/4 pass:
16:9 and4:3 radial/purple output, unchanged un-Haunted output, and an actual
CameraRig/ColourGrade victim camera with real geometry/depth. The latter also
checks spectator refusal, other-seat refusal and expiry turning the effect off.
Candidate captures inspected. This is a small rendering stage, not a full court,
packaged-player, hardware, multiplayer or human acceptance claim.

## Resource limits retained

Original runtime produced its3-case XML then hit the container-headroom guard.
The first candidate compile also hit that guard. One bounded candidate retry
retired an exact stale compiler(1,107,632,128 RSS bytes) and used clean-cache
advice. The final4-case XML passed in.917s and images were captured; the guard
then recorded container headroom during shutdown. Exit0 does not erase that
guard event: peak tree4,449,648,640 bytes, container7,579,090,944. Owned children
were retired and original EditorSettings/profile restored. Frozen source/asset
hashes match. This is completed behavioral/render evidence with a guarded
shutdown, not an entirely guard-free process. No unchanged rerun or guard
weakening was used to conceal the limit.
