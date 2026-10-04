# Pending hero input at producer retirement

Date: 2026-10-03. Baseline source `75de684463693e18daa45c7c6a568b2cedca4533`.
Status: original seven causal failures and six passing controls are confirmed.
The reviewed candidate passes the same thirteen native cases on its first run.

## Defect contract

Focus loss, reader disable and an active practice bot becoming Idle retire
pending input. They must not turn a held hero aim into a release cast, preserve
a buffered press for later recovery, spend charges/cooldowns or end a power
that already activated. Ordinary release and ordinary short-stun buffering
retain their existing behavior. An obsolete reader attached to a remote body
must not close that body's received aim tell or reject same-token renewal.

Current reader cancellation reaches Carrier and CombatVerbs, and current bot
Idle cancellation reaches those same consumers. HeroAbilitySystem remains
enabled with its independent held-key and buffered-press state. Its Aim method
interprets the now-false input as release while CharacterMotor.CanAct remains
true. An instant press buffered during a short stun can also outlive that
producer exit. Actual HeroAbilitySystem disable currently clears aim
presentation but leaves its three buffered press timestamps.

The existing ClearPresentationInput clears pending aim and stamps without
resetting kit resources or active effects. It also closes received network aim
tokens. A producer callback therefore needs local custody guarding or a
narrower own-input retirement path. Actual ability-system disable already
closes received presentation today and should preserve that existing semantics.
No Runtime/Net or authored hero change is needed.

## Native baseline question and stopping condition

Main ran exactly `TumbangPreso.PlayTests.HeroInputRetirementTests` on a frozen
graphics-enabled worker and stopped at fresh 13-case XML and terminal guard
receipts. The original outcome is seven causal failures and six passing controls:

- Held-aim retirement on focus loss, reader disable and the public active-to-Idle
  practice boundary: three causal cases.
- Pending instant-skill buffer retirement at those same three producer exits:
  three causal cases.
- Ability-system disable/re-enable during a pending instant-skill buffer: one
  causal case.
- Deliberate touch release and ordinary stun buffering: two controls.
- Already activated power, duration, cooldown and charge preservation across
  focus loss and reader disable: two controls.
- Received remote aim and same-token renewal across obsolete-reader focus loss
  and disable: two controls.

The fixture uses actual TouchInput, PlayerInputReader and HeroAbilitySystem
consumer methods with a generic counting kit. Its hold casts only on release;
it cannot accidentally cap during setup. The minimal arena-classified scene
begins a real Round, selects Hero mode and checks PracticeMode=false before any
case. Motor movement and environmental simulation are excluded. The public
PracticeRange case supplies readiness only; the separate menu-operator fixture
exercises actual UI callbacks. Short-stun recovery is explicitly cleared within
the existing input-buffer window for determinism; no timed-expiry claim follows.

Original aim cases count the resulting activation through the shipping kit
cast path, rather than failing only on a private field. Cancellation must also
be synchronous before the next consumer Update. Resource checks occur
immediately across callbacks, so normal later duration ticking is not mistaken
for a reset. Public ApplyNetworkAim establishes observer-token controls; there
is no actual peer or transport claim. No authored hero is redesigned or used as
a fixture substitute.

Both hooks reset the world and restore provider, touch, launch/rule/sandbox and
stats state. No assertions or key-edge behavior should be weakened to produce
a pass. In particular, resetting the hero consumer's key-down history while a
still-enabled reader holds a key could manufacture a new press on re-enable.

## Limits

This gate measures native local input/callback/consumer state with
supplied world seams. It does not qualify physical alt-tab, OS suspension,
keyboard/controller/touchscreen hardware, actual peers, screenshots or human
appearance/feel approval. Original and candidate production snapshots and the
unchanged fixture identity remain separate in the native summary.

## Candidate boundary correction

The reader's existing offline/local-slot custody guard now clears pending hero
presentation/input before publishing release. An obsolete remote reader skips
that call, preserving received token renewal. The existing active-to-Idle
practice transition clears the same pending state before parking its input.
Actual HeroAbilitySystem disable uses ClearPresentationInput rather than
aim-only retirement, adding buffered-press retirement to its existing received
aim closure behavior. Networking remains excluded from practice editing.

The three calls preserve key-down history, kit identity, active effects,
cooldowns, charges and committed ultimate requests. There is no generic CanAct
change, network message/protocol change or authored hero adjustment. Source
review approved this narrow correction; the native cases qualify its scoped
local callback and consumer behavior.

## Native qualification

The [original XML](native-original/tests.xml) reports precisely seven retirement
failures and six passing preservation controls. Held-aim interruptions cast the
counting skill, retired buffered presses cast after recovery, and the ability
component retained its buffered press across disable/re-enable. The ordinary
release, ordinary stun buffer, active-effect/resource and received-aim renewal
controls passed. These are causal behavior failures, not missing startup or
fixture dependencies.

The [candidate XML](native-candidate/tests.xml) records 13/13 passed on its first
run with the same thirteen case names and unchanged fixture. There was no
assertion, fixture or tooling repair. The [native summary](native-summary.json)
checks the exact original failing names, both guard terminals, preservation
completed, leases released, and source/fixture hashes. Original worker runtime
source matches `75de68446` apart from line endings; all three candidate runtime
files and the test fixture match the frozen primary source exactly.

Main ran Unity 6000.5.8f1 Editor PlayMode on the separate qa-d validation worker
and profile `validation-qa-d-dddaa72bc5b9`. Original job
`e14c62a6c1494288a4484ca861b158ff` ended with expected test failure exit 2;
candidate job `f7023cb23c1d4f4885c777b126caa838` ended with exit 0. Both terminal
receipts are published alongside their XML. Native execution and worker writes
were coordinated by Main; this implementation unit launched no heavy job.

Only curated XML, terminal receipts and hash summary are published. Full logs,
source before-images and isolated validation preferences remain in the worker.
The generic fixture does not establish authored hero feel, physical OS/device
behavior, actual transport peers or a newly packaged player.
