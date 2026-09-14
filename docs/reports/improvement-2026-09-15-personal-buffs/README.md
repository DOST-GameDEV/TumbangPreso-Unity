# Remaining personal-effect snapshots

Dante Carapace and Nemu Veil can now restore an already accepted remaining
window into a fresh kit. Each real cast or initial hydration settles its own
guard, so later joining records cannot rearm an expired/cancelled effect or
replace a newer cast. Restoration uses existing mature art and selected
sidegrade grants without replaying the opening surge, sound or resource spend.

The targeted packet is now named TimedKit and requires protocol36. Sean and
Zack remain supported with the same independent charge semantics.

## Verification

Three distinct focused PlayMode contracts pass: default/heavy armor protection
and cleanup; default/Long Fade carry rules, no new surge, and pickup cancellation;
newer-cast precedence, hero replacement and normal expiry. The first run was2/3
because the test incorrectly expected tag immunity while holding a slipper.
The actual existing rule intentionally remains vulnerable while held. The
corrected expectation also verifies empty-hand immunity after disarming and
loss of protection after a new pickup. Its focused rerun passed1/1. Both XMLs
are retained; production carry rules were not changed.

Protocol assertion1/1 passed, wire audit72 messages/0 mismatches, and actual
protocol36 host refused a35 client. Internal player build succeeded:
Builds/PersonalBuffReview/TumbangPreso.exe.
Runtime SHA256:0cdda833ca710667dd11d6fe59667b760c95ea938ac55f598837e41a96618200.
Build log: Logs/personal-buff-build-v1.log. Guard7b2d0ad6aee0 preserved the named
Editor profile and shared input preferences. Native named profiles were restored.

Each native case uses three separate players. Seat1 presses the real skill over
a150ms one-way link. Seat2 reconstructs only its own local copy of that kit and
requests the host's current world state. Real buff durations stay unchanged.

| Case | Returned observer live/visual samples | Host / owner live samples | Observer expiry offset |
| --- | --- | --- | --- |
| Carapace |94/94|100/99|+52ms|
| Heavy Plating |129/129|135/135|-13ms|
| Veil |49/49|55/55|-13ms|
| Long Fade |75/75|80/80|+17ms|

Every case retained cooldown/bank, normal expiry, single presentation and correct
protection. Heavy Plating and Long Fade retain their.70/.65 movement grants on
the host and controlling owner. A watching client intentionally does not mutate
the speed stack of a remote body; it follows authoritative movement instead.
The initial Plating evaluator falsely required that observer stack to be.70.
The pre-refresh trace already showed1.0 and CharacterMotor.MayMutateGameplayState
explains why. The evaluator was corrected to assert that authority boundary;
the original failed result and re-evaluated same trace are both retained. No
production mutation was made to satisfy this expectation.

## Critique and limits

This is real transport/snapshot proof with a reconstructed observer kit, not a
cold process restart or same-process disconnect/reconnect qualification. The
brief Veil window is shorter than typical cold boot; changing its real duration
to make that test work would invalidate the evidence. Controller-owning hydration
is covered locally here and by the earlier charge transport cases, not by a new
native returning-owner armor/veil run. That distinction remains explicit.

Existing approved art is reused with its mature timeline. Numerical passes do
not establish new art or human-feel approval. Persistent ground fields, pending
windups and broader reconnect/rematch lifecycle remain in the non-UI queue.
