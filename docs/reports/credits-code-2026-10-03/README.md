# Credits eight-direction bonus — October 3, 2026

In Credits, enter Up Up Down Down Left Right Left Right (no B/A). Keyboard arrows and gamepad D-pad work; mouse/touch directional drags support pointer users. Partial input resets when Credits closes. One completed sequence requests a fixed 5,000 Tansan from the authoritative wallet, then plays existing `match_win` Victory cue after confirmed success. A new complete entry grants again. No match rules or real-money flow change.

The client never fabricates a balance. Offline/request errors show the wallet status and retain the request receipt for retry. The server retains 64 separate Credits receipts without evicting task claims. Each request is fixed at 5,000 regardless of client amount; duplicates within this receipt window do not pay again. Signed-in wallet required. Existing temporary 999999 playtest top-up is unchanged.

Evidence: managed715/715 (six sequence cases plus existing suite), Node wallet checks including exact amount, duplicate/case-normalized retry, repeated new request, invalid request, cross-player receipt separation, task-claim preservation and integer cap. Native5/5, 0.5057942s, keyboard/D-pad/mouse, close/reopen and closed-screen controls; synthetic offline fixtures never credit a live profile. Exit0 with shutdown cgroup guard, both project settings restored.

Limits: no real signed-in reward/audio playback acceptance or physical device test. Backend publication must be separately verified; source push alone does not activate the server action.

## Additional input coverage

Fresh headless8/8 at18:04:34–35 UTC (0.8023294s), guard-free with both settings restored. Adds synthetic touch swipes, rejection of tiny pointer drags and ambiguous simultaneous directions. Original keyboard/D-pad/mouse/lifecycle cases retained. This is device-event testing, not physical touchscreen acceptance.

## Pending and unsuccessful reward feedback

The Credits screen now acknowledges the request immediately with “Claiming +5,000 Tansan...” while the authoritative award is pending. Busy/offline/account-change outcomes use explicit retry/sign-in text, so a stale success status from another wallet action cannot appear as a new bonus confirmation. Existing request timeout20s was already present in CloudCode and was not changed. Reward amount, deduplication and Victory success gate remain unchanged.

Fresh native headless13/13 (original8 input/lifecycle cases plus5 failure-message cases) at18:53:24–25 UTC,1.0329521s; exit0/guard-free. Peak tree3,339,427,840/cgroup6,391,848,960. Both settings restored and six inputs verified. Initial compile-heavy run hit the unchanged headroom guard after compilation, before XML; retained separately. Live server activation is still blocked403 and has not been claimed.
