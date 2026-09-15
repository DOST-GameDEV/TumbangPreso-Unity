# Direct native control rehearsal

Status: pickup/shove defect fixed and both native control sequences PASS.
Captured pickup, flight, recovery and defender restoration views were inspected.
This completes the named automated control cases below, with the fixture limits
retained; whole-kit, physical-device and human play-feel criteria remain open.

The opt-in --gameplay-only runner enters Guest, Play and a bot match through the
real UI, then drives the shipped keyboard/mouse action bindings on a temporary
cloned InputActionAsset. It observes the actual motor, carrier and combat outcomes.
No production gameplay rule or reserved networking file is changed by this fixture.

Observed passing outcomes in each mode:

- W moves the actual local body, Shift sprints, Space jumps and fresh Space presses
  complete a real trip recovery.
- Held/released left mouse winds up and launches the held slipper. The player walks
  to that same settled object and X retrieves it.
- X lands a shove on a stationary rival attacker and physically displaces them.
- Right mouse performs a legal retrieval slide and collects the staged loose shoe.
- After an explicit fixture role advance, X channels and restores a fallen can.
- Left mouse resolves one legal punch tag; a held/released right mouse lunge resolves
  the next legal tag through the normal round rules.

Limits are deliberate: bots are disabled after entry; positions, target availability,
initial camera pitch, a fallen can and the role advance are staged. Successful
throw/pickup/shove/slide/channel/tag outcomes are never injected. This is synthetic
input inside a real Windows player, not physical-device certification, a freeform
human match or whole-kit balance/feel approval. The earlier nativev24 loop separately
qualifies actual timed results/rematch and8-round defaults.

Build preparation receipts: v25 first failed at an ambiguous InputDevice type;
fixed namespace. v25b and v26 built successfully but were not used as passing
runtime evidence. Source review removed a mistaken Rigidbody assumption and made
can protection/actual channel progress and shove displacement explicit prerequisites.
The current ledger identifies the corrected build/run and any remaining failures.

Nativev27 reached Classic walk/sprint/jump/get-up/throw and X retrieval of the same
slipper, then failed the later deliberate shove. The retrieved screenshot shows
SHOVE CD1.7s before that deliberate press, and the stamina bar has already fallen.
This is preserved failed evidence, not a passing full direct-control run. Current
mechanism under test: Carrier clears the consumed-pickup flag every rendered
Update, while InputIntent.JustPressed stays true until the next physics commit.
Another rendered Update can therefore spend the pickup press on a shove.

The first focused probe stopped at its own precondition because Component.SendMessage
broadcasts to every component on the GameObject, including disabled input readers.
The corrected probe invokes only the two private consumer methods. The native
driver's cloned-reader setup was likewise narrowed to that reader's Awake alone,
so it cannot reinitialize unrelated components.

The corrected local reproduction failed with shove cooldown7.5 and stamina35 on
the second consumer update, after a clean pickup at stamina60. Guard1ada6d085ef1.
Carrier now retains successful pickup press ownership until release. Fixed local
case PASSED1/1, guarda20bea70ae7c: all three pre-physics updates leave cooldown0 and
stamina60; release/fresh press still lands a shove and spends exactly25stamina.
No cooldown, stamina-cost, physics, input-binding or networking rule was changed.
Nativev28 then PASSED both modes, including an explicit no-shove-cooldown and
no-stamina-loss assertion immediately after pickup, legitimate shove displacement,
slide retrieval, actual can channel/restore and both legal tag inputs. Build:
Builds/direct-gameplay-v28/TumbangPreso.exe,1135MB/45s, guardf3958cd94150.
Runtime SHA256:70A8566AB6070C283E70D72176962A89D01D47EA28B893150FDF0706B1F6081B.
Native PID8504 exited0, shared input unchanged, zero pre-existing named-profile
files. All recorded control views are1280x720 windowed; the earlier failure was
1920x1080 and is not a matched performance comparison. No FPS claim is made.
The final local fixture also restores pinned rules and launch flags; it PASSED1/1
again in Logs/pickup-press-v4.xml, guarde9c91d2e9de0. Exact failed/passing XML,
native result receipts and selected original captures are preserved alongside.

Visible follow-up: the quick fixture role advance can display an irrelevant
remaining SHOVE CD after becoming defender. HUD role filtering needs a narrow
review; do not change actual cooldown durations just to hide that label.
