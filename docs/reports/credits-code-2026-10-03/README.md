# Credits eight-direction bonus — October 3, 2026

In Credits, enter Up Up Down Down Left Right Left Right (no B/A). Keyboard arrows and gamepad D-pad work; mouse/touch directional drags support pointer users. Partial input resets when Credits closes. One completed sequence requests a fixed 5,000 Tansan from the authoritative wallet, then plays existing `match_win` Victory cue after confirmed success. A new complete entry grants again. No match rules or real-money flow change.

The client never fabricates a balance. Offline/request errors show the wallet status and retain the request receipt for retry. The server retains 64 separate Credits receipts without evicting task claims. Each request is fixed at 5,000 regardless of client amount; duplicates within this receipt window do not pay again. Signed-in wallet required. Existing temporary 999999 playtest top-up is unchanged.

Evidence: managed715/715 (six sequence cases plus existing suite), Node wallet checks including exact amount, duplicate/case-normalized retry, repeated new request, invalid request, cross-player receipt separation, task-claim preservation and integer cap. Native5/5, 0.5057942s, keyboard/D-pad/mouse, close/reopen and closed-screen controls; synthetic offline fixtures never credit a live profile. Exit0 with shutdown cgroup guard, both project settings restored.

Limits: no real signed-in reward/audio playback acceptance or physical device test. Backend publication must be separately verified; source push alone does not activate the server action.

## Additional input coverage

Fresh headless8/8 at18:04:34–35 UTC (0.8023294s), guard-free with both settings restored. Adds synthetic touch swipes, rejection of tiny pointer drags and ambiguous simultaneous directions. Original keyboard/D-pad/mouse/lifecycle cases retained. This is device-event testing, not physical touchscreen acceptance.
