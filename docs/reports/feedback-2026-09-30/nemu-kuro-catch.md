# Kuro: Catch objective protection

The current Wiki replaces the former six-second fallible physical guard with
five seconds of can immunity, available only while the can is upright. The stable
nemu_skill2d ID, shared25-second basic cooldown and current companion art/cast/cue
remain. The display name and description now say Kuro: Catch.

## Owned protection

Lata retains the current live ability owner alongside its independent restoration
shield. ProtectionLeft reads the maximum current clock; Catch does not decrement
or overwrite the restoration timer. Only a resolving host or approved replica
playback/recovery can add the grant. Actual knockdown still resolves only on the
host. Expiry, cancellation and round reset release only this ability's grant.

The existing protection read/presentation uses that same clock. A late/missing
companion model cannot decide whether the objective is protected. Current companion
movement/scale/cast assets are reused; no new animation, VFX, SFX, model or map art
is authored. Host-confirmed delivery prevents unaccepted local casts from granting
protection. The shared prepared-world route restores only remaining time and
authoritative empty state without recasting, cues or another resource spend.

Approved recovery contexts are not marked ordinary approved cast playback in the
existing dispatcher. An explicit internal recovery projection therefore adopts the
clock on replicas too. If can state arrives later, ticking adopts its valid grant;
repeated owner registration does not rebuild the retained shell.

## Native evidence

Unity6000.5.8f1 Windows D3D11, guarded feedback-nemu-catch-1001 profile. Source
af43ffb62 plus the exact owned candidate;605 input hashes unchanged after final run.
These exercise actual Lata/kit/recovery callbacks in a controlled native services
world; the companion can be absent without affecting the objective contract.

- Baseline6cases:1pass/5fail. Actual can knockdown, downed activation, lifetime,
  owned cleanup and remaining-clock recovery reproduce the absent contract.
- First corrected6/6pass: five-second real knockdown immunity/expiry, shared
  cooldown, upright/defender eligibility, independent restore-shield preservation,
  round reset, unapproved observer refusal and host remaining-clock recovery.
- Two concrete follow-ups: approved replica recovery fails to project the clock;
  destroyed-can cleanup already passes. The latter hypothesis is rejected.
- The reproduced replica case passes1/1 after the projection fix, including expiry
  and continued refusal of a replica's attempted knockdown. Seven unchanged host/
  cleanup controls are reused. Eight distinct acceptance cases pass across retained
  receipts; this is not a single final8/8 suite. No fixture repair or disabled test.

Exact XML/input receipts are in nemu-catch-checks. Raw logs remain in the isolated
checkout Logs/feedback-0930/nemu-catch-*.log. Compatibility advances after integrating
the current contributor protocol. Actual peer transport, rendered companion/frame
qualification, player build and physical devices are still separate. The earlier
protocol103 LAN receipt does not cover Catch. Haunt and complete Nemu alignment
remain open in the same F0930-12 row.
