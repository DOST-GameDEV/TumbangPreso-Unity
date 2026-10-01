# Solo shortcuts respect menu and presentation ownership

DebugPlayerSwitcher read raw F-keys even when a pause/settings surface or held
presentation owned input. Actual F2 baseline cases changed DrivenSlot0to1 behind
the open pause menu and during PresentationClock.Hold. That can change control
and camera ownership while the user is operating a menu or watching a cast.

The existing shortcut path now refuses network sessions, open Panels, held
presentations and blocked gameplay clock. It uses existing state, adds no new
manager or UI, and preserves ordinary solo shortcuts after release. No clip,
effect, model, sound, map, loading or network protocol change.

Unity6000.5.8f1/D3D11 isolated source788ede121+owned overlays. Baseline2failures;
same two final cases2/2pass, including fresh F2 after menu/hold release and
offline menu clock/input restoration.753frozen inputs unchanged; no tooling
repair or broad suite. This does not qualify rendered cinematics, physical
hardware or whole demo/tournament readiness. Raw XML retained; jobs21241/18683
terminal with named profile/input preservation.
