# Replace the generic tag burst with contact ink

The owner requested replacing the out-of-place particle effect on a catch/tag.
Accepted MatchFlair.Tag now draws three short angular amber/brown marks at the
same torso contact used by the hand gesture. The old ImpactBurst is removed
only from tags; block feedback, stun stars, hit confirmation, score, recovery,
voice and existing network presentation routing are retained.

The effect creates no particles/colliders, consumes no random stream and owns
its one material through VfxRenderTag. It retires after 0.24 gameplay seconds or
round retirement, holds during pause/presentation freeze and reduces motion,
count and intensity through existing comfort settings. No new shader/texture or
unused replay API is introduced.

Graphics-enabled Windows Unity 6000.5.8f1 native 7724 passes 2/2. Actual
HostResolvePunch tags a legal attacker, reaches the replacement and retains
recovery. Pause holds both the effect and recovery; resume expires the effect
and its material. A separate comfort/round-retirement case verifies zero-flash
suppression and reduced static marks. The native parent and restoration are terminal;
202 generated owned UI metadata changes were restored exactly.

Observer pixels show compact contact marks beside the torso without obscuring
it. The FPP capture contains marks after the victim has left the frame, so it
does not prove placement at the exact contact instant. Comfort's captured cue
is not clearly visible; this is not a clarity/human-approval claim. Static
capture timing is explicit. Existing catch replay hides live VfxRenderTag;
current packaged peer/replay contact and human motion acceptance stay OPEN.

The current full Windows 151 artifact 89cc9f295 predates this effect. Adjacent
native results and images qualify this source unit only, not that artifact.
