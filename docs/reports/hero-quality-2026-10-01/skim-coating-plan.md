# Skim loaded-shoe cue

The coat must mean the exact held shoe is prepared, not simply that a cooldown
is running. Observe RafiHeroKit.IsSkimLoadedFor and its real remaining duration;
never author a second gameplay clock. Reuse the established world/FPP attachment
pattern used by Sean and Zack, with independent Rafi geometry.

A narrow teal meniscus follows the shoe's actual mesh bounds: broad rounded toe,
narrow waist, tucked heel. Preserve the authored skin and shoe silhouette, with
no opaque sphere, extra collider, free-floating halo or material replacement.
Build once per activation/target and fade in over0.18seconds. Drain in the final
0.4seconds of the real load. Throw, expiry, transfer and role/kit reset remove it.
Restore from existing timed-kit state, without changing packets or authority.

Native acceptance: both real world shoe and first-person copy, true loaded state,
no colliders, visible strength, no duplicate attachments, natural expiry and
release cleanup, actual owner/witness views and unchanged shoe identity. Include
one scoped visual review of the previously shipped coating gesture with proper
observer prop visibility. This does not add a gameplay wind-up. SFX and ground
wake remain separate. One focused pass, one bounded tooling repair maximum.
