# Arrival camera retains first-person visibility ownership

Late camera activation or binding the local seat called `CameraRig.SetActive`
and exposed gameplay arms while the arrival cinematic was still introducing the
players. Its paused camera update returned before hiding those arms again.

Arrival now explicitly owns camera visibility until its existing return blend
reaches the gameplay handoff. Rig activation/follow updates retain their desired
state, but cannot enable the viewmodel, disable the presentation camera or hide
the local world body while arrival owns the shot. The existing handoff and
cancellation restore normal gameplay visibility. Disabled cinematic motion
retains its existing accessibility behavior. No network wire or gameplay change.

Native original19592/session62682: two causal failures and two controls passed.
Same candidate21944/session86612:4/4 passed, exit0. Public late activation/follow
calls are exercised with active arrival; normal FPP and cancellation remain.
Both jobs terminal, quality/input/profile restored,19256 frozen inputs stable
after202 retained generated GUI importers are restored exactly.

This proves the visibility lifetime in Unity. Actual multiplayer arrival film
and current packaged acceptance remain open rather than reusing older148 footage.
