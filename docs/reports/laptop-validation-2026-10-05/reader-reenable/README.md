# Retire presses received while the player input reader is disabled

Previously, enabling the reader could cast Basilio's E or spend Q on its owned
slipper using a key pressed while the reader was disabled. The input actions
continue processing device events during that interval, but OnEnable only
subscribed to device changes; OnDisable had sampled buttons before the new press.

OnEnable now calls the existing DiscardMenuButtonsUntilRelease after subscribing.
It captures current held buttons, retires buffered intent/toggles and the old look
frame through the same existing recovery method. A held E/Q waits for observed
release; a fresh command then works. No focus authority, input backend, network,
kit, cooldown or authored presentation is redesigned.

## Actual native causal evidence

Source basef5bf1d0ad4a026f07809e0f7b1af82cc7615bab8, laptop gamergmae/Windows11,
Unity6000.5.8f1/D3D11, isolated warm qa-a worker and named validation profile.

Original Unity26736/parent50728 ran three focused cases on unchanged production:

- FAIL: reader disabled, E pressed and processed, reader enabled; actual15s
  signature became active without a fresh press.
- FAIL: reader disabled, Q pressed and processed, reader enabled; actual owned
  slipper became Concussed instead of remaining Normal.
- PASS: a fresh E after re-enable still reached the actual signature consumer.

Candidate Unity12488/parent89048 runs all16 HeroQuickTapTests and passes16/16,
no skips. The two causal cases also prove release followed by a fresh E/Q works.
The13 existing related quick-tap, ordinary held, actual motor/kit/slipper,
TEXT-only negative, menu and touch controls remain green. Re-running those related
controls is justified because OnEnable also affects initial reader activation.

New tests queue native InputSystem keyboard states while the producer is disabled,
then let real input/player/ability loops run. They use the actual carrier, owned
slipper, hero kit and round; they do not force InputSystem.Update, call a cast
directly or invoke Update manually in these new cases. The existing fixture's
controlled focus setup is retained; this is synthetic device-state behavior,
not physical keyboard or two-machine acceptance.

Both parents are terminal. Each protects19316inputs and preserves/restores265
generated metadata/Auditor changes, Quality and13existing preference values.
Exact tested runtime/test bytes match the published source after LF normalization.
Original failures, final XML and raw hashes are retained; no assertions were
weakened and no unrelated production files were repaired.

## Remaining limits

This qualifies the disabled-producer press lifetime and fresh-command recovery.
It does not resolve the owner's uncertain physical E/Q report, network Request
timeout/host-kick, focus callbacks on every device, quit fault or full tournament
acceptance. Desktop owns its separate Home-card/UI and network source lanes.
