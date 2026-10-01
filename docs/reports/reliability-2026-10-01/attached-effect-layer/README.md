# Attached aura inherits its host render layer

AbilityVfx.AttachAura parented its emitter but left its GameObject on layer0.
Unity layers do not inherit through parenting, so a host on preview layer30
had an aura excluded by its own preview camera. One line assigns the host layer.
All particle shape/colour/rate/lifetime/material settings remain unchanged; no
authored VFX/SFX/model/animation/map/lighting assets are edited.

Native Unity6000.5.8f1/D3D11 actual particle/scoped-camera check. First baseline
and final stopped on a fixture random-seed assertion, not product evidence.
One bounded fixture repair stops/clears the emitter before setting seed. Corrected
baseline explicitly uses committed AbilityVfx and fails host30/aura0. Same final
1/1passes supported material, live particles, matching layer and non-black pixel
readback through a camera rendering only layer30.771frozen inputs unchanged.
Failed fixture results retained; no hidden repair or broad repeated suite.

Sourcee618fa7fe+owned overlay, protocol127 unchanged. This qualifies the actual
attached-aura render path, not all effects/screens/hardware/player/network
readiness. Jobs38782/34837/21130/75623 terminal; profiles/input preserved. Broader
loading remains with friend; shader/effect bug fixes and optimization continue.
