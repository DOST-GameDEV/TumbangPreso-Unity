# Frostbite delivery

## Fix

The generic slipper impact handler cleared every affinity before either body-hit
branch called HostFrostbite. A legitimately frosted throw therefore hit a defender
or attacker without applying Frozen. Both branches now apply the frost payload
before generic impact cleanup. The payload is still spent once, and ordinary body
blocks retain their existing behavior. No authored visual, sound or animation changed.

Frostbite now requires a slipper in hand, matching the current Wiki. The existing
attacker, ready-resource and CanAct gates remain. Protocol101 and the normal skill
fingerprint require matching updated builds; no packet layout changed.

This completes the Frozen-hit part of F0930-09. Cold Feet duration, the complete
Cheska specification and other character alignment remain separate open work.

## Evidence

Unity6000.5.8f1 Windows D3D11, guarded isolated feedback-frostbite-1001 profile.
Source676950acd plus the explicit owned candidate,584 hashed inputs. Real Carrier
release and Slipper.FixedUpdate run against actual loaded Eskinita bodies.

- Initial baseline4/4failed: empty-hand activation reproduced; three throws were
  inside the unchanged0.25-second release grace and did not contact their targets.
- One bounded fixture repair places the target6m away in a lane clear of the can.
  Corrected baseline1passed/3failed: ordinary block passes; both frosted body hits
  reach their requested body but have StunElement.None; empty-hand gate still fails.
- Fixed candidate4/4passes: defender and attacker receive Ice for the prescribed
  Frozen duration, release consumes the loaded state, impact consumes affinity,
  empty-hand/defender/inactive-round activation is refused and neutral block stays
  unfrozen. Frozen source inputs have no non-metadata drift after the run.

Raw logs remain in the isolated checkout Logs/feedback-0930/frostbite-*.log.
Checks and exact input receipts are retained in frostbite-checks beside this report.
No actual-peer transport, target player build, physical device or human approval
is established by these component/flight checks. No paid service session was started.
