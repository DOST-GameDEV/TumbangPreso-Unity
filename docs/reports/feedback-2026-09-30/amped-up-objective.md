# Amped-Up objective cooldown reduction

The confirmed Wiki passive was absent: a real OnLataKnocked objective award
left Zack's signature cooldown at20seconds instead of15. Add an explicit
objective callback, separate from practice/refill AddUltimateCharge and network
resource hydration. Zack reduces running basic cooldowns by5seconds per awarded
objective unit, including existing.15throw/.5retrieval income. Cooldowns floor
at zero; charges, effect clocks and ultimate resources are not changed.

Host-only reduction would leave a live predicted owner waiting: ordinary resource
snapshots intentionally cannot lower that owner's cooldown. A new40byte reliable
ObjectiveCooldown grant carries match/round/epoch, seat, sequence, income and the
host's processed skill-request watermark. Trusted current-world grants apply
once. Each owner slot checks its latest request, including settled requests, so
a delayed earlier objective cannot discount a newer cast. Observer slots apply
the same discount. Malformed suffix/truncation and wrong authority are rejected
before sequence/state changes. Protocol123requires matching builds.

Windows Unity6000.5.8f1 native baseline1/1fails at the real objective callback.
Final6/6passes: actual fractional/whole awards and refill exclusion; owner grant
and duplicate handling; newer pending/settled prediction guards; bad scopes/
income/bindings without sequence poisoning; actual named handler with consumed
NGO envelope/truncation/suffix/authority/loopback; and resource floors/charges/
ultimate/other-kit/practice controls. All726frozen input hashes remain unchanged.
New32hex script metadata imports natively.3497/39940terminal; guarded profiles
and shared input preferences preserved. No native tooling repair; a stray edit
signature was corrected before compilation/run.
[Raw receipts](amped-up-checks).

This is the confirmed passive unit, not completion of Zack's kit. Legacy Magnet
still needs the planned cooldown-based role migration and Overclock's15point/
match-long state remains open. New Wiki blank-slot entries remain Proposed until
implemented. No actual123peer or whole-match balance/performance claim; earlier
121rejoin and122Frostbite checks retain their exact scopes. No authored visual,
sound, animation, model, map or loading change.
