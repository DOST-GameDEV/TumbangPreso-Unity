# Solo handover retargets existing local UI

The normal solo F-key path moved controls/camera but did not rebind the pause
watcher or HUD. The native F2 baseline accepted the second seat while pause
still referenced the first. ApplySlots now binds existing Hud/PauseWatcher and
the optional legacy YouCard to the same claimed actor, retaining spectator exit.
No new UI, shortcut, layout, animation/model/effect/sound or loading change.
Networked switching refusal and existing ownership/control rules remain.

Unity6000.5.8f1/D3D11 isolated sourcece36e32fa plus owned overlays. One keyboard
baseline failed; the same case final1/1 passes control/pause/HUD references.
753 frozen final inputs unchanged; no tooling repair or broad rerun. This is
state/input-path evidence, not rendered-HUD/hardware/network/tournament approval.
Optional legacy YouCard uses the existing network rebind calls but was absent
from this small fixture; its rendered presentation is not claimed qualified.
Raw XML and scoped result are retained. Native jobs57973/47894 terminal;
named profiles and shared input preferences preserved. Current126player results
predate this fix and remain separate.
