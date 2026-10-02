# Reuse replay prop-refresh workspace and avoid lookup captures

BindProps ran every0.2seconds and built a new desired list plus captured lookup
delegates each refresh. It now reuses a private scratch list and explicit lookup
helper. The native slipper query, prop ordering/eligibility, existing track reuse,
new-track creation and unsafe-window decisions remain. Clear scratch references
after copying and on disable. No art, scoring, protocol or replay schema change.

Original49483 and candidate16571 pass the same4 native EditMode cases, first
runs/no repairs/retries: stable binding preserves track/history/safe window,
visual replacement gets new track/unsafe marker, removal+return cannot reuse
old track, warmed timing control. Exact3 owned files, all18443 protected
qualification assets unchanged in post48364. Guards preserve profiles/input and
release leases. Current shipping1003c predates this source optimization.

Seven warmed2000-call batches: median3.69255 to
3.16330us/call (14.33% lower in this fixture).
GC.GetAllocatedBytesForCurrentThread reports0 for BOTH versions despite the
original allocation sites. The installed Mono BCL routes this API to InternalCall;
no managed body explains its behavior. Counter output is uninformative, not a
validated allocation delta or zero-allocation claim. Structural allocation removal
is based on actual code; only timing change is quantified. No player FPS or
historical208/270ms-frame mechanism/improvement claim.
