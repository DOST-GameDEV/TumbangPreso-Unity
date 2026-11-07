# Current checkpoint

FEEL-PICKUP-1107, feel/retrieval-arc: dwell bends before the grab; six contexts
lift the street shoe. Opening skip frees the pointer and shows its binding.
4 unique Edit cases and 9 exact Play cases passed: Logs/feel-pickup-revision-20261107/.
No new HOST-ONLY findings; baseline failures remain. Next: build with
tools/build_windows.py --launch, then owner feel review before merge. Peers unverified.

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
