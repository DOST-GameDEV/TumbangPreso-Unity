# Nemu Kuro: Fetch delivery

## Corrected behavior

The current Wiki says Kuro brings Nemu's slipper beside her for pickup. The former
return path called HostForceEquip, silently skipping normal pickup and its status
checks. Delivery now leaves it loose beside Nemu and uses the normal landing helper.
A real pickup still goes through Slipper.HostGrab. Stable ID nemu_skill2, shared
25-second cooldown, existing art, movement speed, loose-slipper targeting and
8-second errand budget are preserved. This unit is not full-kit qualification.

Bug hunting in the same return path reproduced two additional failures: a slipper
picked up during the return was still repositioned under Kuro, and cancellation
left the loose slipper floating. The fetch now stops before touching changed-owner,
inactive, held or thrown equipment; carried loose slippers land on expiry/cancel.
Defender interception retains its existing distance/CanAct rule and grounds the
shoe through the same authored rest-height and playable-bounds landing helper.
Only the host moves or lands the slipper. Existing slipper snapshots publish it.
Protocol95 excludes older peers that still implement automatic equipment.

Source: [Abilities - Technical](https://docs.google.com/document/d/1jvr7NLzhHrbw-wrG676AeOkoTxJf4GokkfmxpO0ddLg/edit?tab=t.0),
read 2026-09-30. The broader Feedback row remains unfinished. Catch, Haunt and the
remaining Wiki reconciliation still need work. Google Doc writes remain paused.

## Native evidence

Unity6000.5.8f1 Linux64, graphics with Mesa llvmpipe, guarded isolated
cloud-nemu-fetch profile. The candidate includes incoming tutorial source from
625af762 unchanged, plus the published Sit unit and this explicit Fetch overlay.

- Baseline:5 cases,1 passed and4 failed. Failures reproduced automatic equipment,
  movement of an already-held shoe, floating cancellation, and the old display name.
- Corrected candidate:7/7 passed. In addition to those cases, normal HostGrab after
  delivery succeeds, expiry lands without equipment, and changed ownership wins.
- Two additional authority/interception cases pass2/2: an actionable defender drops
  the shoe at interception; an observer cannot move or land the host-owned shoe.
- Frozen input hashes remained unchanged in all three runs. No fixture repair or
  unrelated test changes. No repeated unchanged broad regression.

These are real Unity component/companion tests with a controlled network provider.
They are not actual peer transport, reconnect, physical-device, rendered-player
or full-player-build qualification. Tutorial behavior was not rerun; its source
compiled in this integrated candidate. Protected generated metadata stays isolated.

## Evidence hashes

- baseline.xml: SHA-256 b7924c488a2046bebb267b251a6916063461fc462aa6fa2cf6bb0f34219312f4
- fixed.xml: SHA-256 3bf8163a07751f7865a3cdd0150731a82525d647139979f16662857a25c8f43a
- authority.xml: SHA-256 b094f6a43647b252148ef71d486177bbadb1ddfe777608b30bd4bad0913df214
- baseline-inputs.json: SHA-256 3eb2ffb78c1e6f5ce9744c8237068f7a2fae5155a06a6db9c6024556514b8a91
- fixed-inputs.json: SHA-256 4645f2982b631df566d27ec2e06fa8ff6b57f9344608fa00696216ccdab2c400
- authority-inputs.json: SHA-256 fb667844863a8ed49d02bdeb7d92bcd3f4c0826f2e5e39be2a6dd80a115ef112
