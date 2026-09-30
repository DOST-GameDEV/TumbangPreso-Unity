# Defender Lunge, 2026-09-30

Owner feedback requests a faster, farther general defender lunge. Full charge now
targets3m instead of1m. Speed is sqrt(2 * friction * distance), about13.416m/s.
Charge0.5s, active0.45s, radius1.3m, cooldown1.5s and partial-power rules are retained.
Physics still moves and sweeps the actual body; no teleport or wider tag radius.

Bot tier approach distances retain their previous margins around the derived
reach. Safe celebrations retain3.7m clearance. Retrieval pressure uses the same
4.3m standing reach. The28m/s move-budget ceiling is unchanged. Protocol97 refuses
mismatched builds. Hero kits, loading, art, motion and effects were not changed.

## Evidence

The focused engine-free check covered balance, movement budget, bot tuning,
retrieval stats and sabotage relationships.56 cases passed; the pressure-radius
literal still asserted the old2.3m and failed. Its current4.3m assertion passes
on the final single-case check. The earlier constant-only PressureRadius declaration
also needed converting to readonly when speed became a derived value.

Five native D3D11 PlayMode cases exercised the real input and host path. Both modes'
input travel and host travel/repeat refusal passed. The initial target case had
no held slipper and no live match for awarding points; the fixture now explicitly
establishes those gameplay preconditions. Only its near/far sweep cases were rerun,
both passing. Three unchanged travel cases were not repeated.

- [Initial native results](checks/lunge.xml),4 passed/1 fixture failure
- [Final near/far sweep](checks/lunge-final.xml),2/2 passed in3.1093152s
- [Frozen runtime inputs](checks/lunge-inputs.json)
- [Initial Core results](checks/lunge-core.trx)
- [Final pressure check](checks/lunge-core-final.trx)

The actual CharacterController travels between2.7m and3.1m on the fixed-step model;
a3.7m-away eligible attacker is tagged once and a5.8m-away attacker is not. This is
local runtime evidence. Real peers, physical hardware and match fairness remain
separate qualifications; no blanket multiplayer or bot-quality claim is made.
