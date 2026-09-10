# Nemu gameplay and recovery refinement

Phantom Veil now maintains a measured 20% movement boost rather than applying a
per-frame impulse that the motor erased. A slipper already held when casting no
longer immediately cancels it; a new pickup does. Its presentation uses a body/FPP
rim and short trails instead of a strong purple point light.

Astral Hijack sweeps a body-sized route, follows the floor and checks its recall
landing. Its camera cannot pass through a solid wall. Passing contact staggers
once per 1.25 seconds per target rather than refreshing stun every frame.
Cancelled/denied projection cleans up without teleporting; normal recast retains
recall. Feeding and returning Kuro refuse possession before spending the charge.

Devouring Seance owns a seven-second lifetime. Reset cancels its actual field,
not just its meter. The full live-input check measured a victim closing from
1.4960 to 0.4786 m and a loose slipper from 0.8242 to 0.1200 m in 0.45 seconds,
without moving the can. The familiar's arms reach asymmetrically while feeding.

Ghost-step body/FPP preparation now agrees. Eight existing audio cues were timed,
filtered and reduced in peak/RMS from reproducible checkpoint inputs. See
nemu-audio-timing.json. No audio listening approval is claimed.

Verification: Core 559/559; Nemu complete-cycle PlayMode 11/11, including all three
actual casts plus ten behavioral contracts. Full EditMode initially 480/481 due
to copy length; focused copy rerun follows the shortened text. Earlier collision
batch 9/9 and all fourteen source gates passed. Profiles were preserved by the
Unity runner. These are local tests, not LAN or human playtest approval.

Open: controlled-familiar network replication, authority and reconnect proof,
ordinary overlap/full-speed player review, broader map lighting and final build.

Copy follow-up `nemu-copy-edit-v2.xml`: 24/24 passed after shortening text.
