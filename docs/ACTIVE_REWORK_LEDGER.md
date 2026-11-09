# Current checkpoint

TJ-CADENA-1109, `tj-cadena-fixes`: throw arc, seven-second grace, four-second
gold protection with a faster final pulse, clearer UI and a refined can raise.
Core 1/1, Edit 5/5, nine exact Play cases passed across focused runs.
Logs/tj-cadena-fixes/ and native TJ captures in Logs/shots-native-ui/.
Player `efc0a5ff5` built and opened. Logs/windows-build-20261109-181212/job.json.
Next: owner feel review.
Actual peer and physical controller checks remain open.

CONTROLLER-NATIVE-1107, main: native DualSense/Edge HID reports reach the current
gameplay/UI bindings. Fixed disabled UI-action confirm/back fallback and added
bounded controller discovery/first-input logs. Input-only isolated EditMode
evidence: [report](reports/controller-native-2026-11-07.md),
`Logs/controller-native-20261107/`. No physical Sony controller was available.
Next action for the reported failure: USB and Bluetooth hardware acceptance,
retaining the `[Controller]` Player.log lines if it still fails.

BANAUE-PERF-1105 is complete on main. [TODO](TODO.md) remains the only active queue.

- Done: catch-clip encode on a worker, `WorldGround` nearest-hit stepping, ultimate live-root rescans at 10 Hz,
  Banaue far-house LOD (scene and builder), Paete escape (peel, debris, per-body bands, squirm layer).
- [Evidence, measurements and limits](reports/banaue-perf-2026-11-05/README.md); raw logs in
  `Logs/banaue-perf-20261105/`. Exact tests only: EditMode 4/4, PlayMode 4/4 and 4/4.
- Next action: prewarm each picked hero's `HeroIntroductionScene` during loading (Dante 217 ms on the start frame).
- Probe: `TumbangPreso.exe -tp-framebudget DIR -tp-maps Payo -tp-seconds 300 -tp-trace -tp-spike-scripts` in a
  development build names the scripts behind every frame over 35 ms. Development players go outside `Builds/Windows`
  only temporarily and are deleted after; restore the four build-touched settings files after a direct build.
- Keep canonical checkout on main. No reset, clean or force push.
- [Previous checkpoint](reports/banaue-perf-2026-11-05/checkpoint-before.md).

- Also on main: the other machine's movement, ability reference and tooltip publication (cf9cbb565); its
  owner review is open. [Its checkpoint](reports/banaue-perf-2026-11-05/checkpoint-other-machine.md).
