# Current Windows151 visible startup and join recovery

Full Windows release source89cc9f295/protocol151 built with Options.None, exited0
and retained258 files/2667632603bytes. Per-build generated203 UI metadata/EOL
changes were retained then restored to frozen bytes. Actual graphics player20584
used a new named profile and no network bootstrap or direct menu/game method.

Observed actual pointer route: Guest login -> painted title -> Home. Adjacent
frames show the Soraya/Nemu Home loop in different poses/camera positions.
This first visible entry animated. The Windows decoder reported timestamp/colour
fallback warnings, with no HubSceneVideo failure warning; those warnings are
not certified harmless on every hardware/clip combination.

Custom -> JOIN ROOM -> CODE -> typed the earlier closed test code WAD6 -> JOIN
showed “No game answered to 'WAD6'.” Switching to Internet browser worked after
the failure. Empty browser at that moment is not evidence of discovery failure:
no current151 public test host was running. This is negative-path recovery,
not successful151 peer admission or clean non-host movement acceptance.

The player exited normally and settings/input were restored. The manifest hash,
runtime identity and exact result are retained in receipt.json. Desktop release
was untouched. Automatic plant delivery, current multiplayer camera arrival and
controlled non-host movement remain separate paired/operator checks.
