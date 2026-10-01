# Bot companion observation delay

Tag selection already considers all RoundDirector.Bodies, including companion
attackers. Observe only sampled Players, so At(companion) fell back to its current
transform. Companions bypassed the existing position reaction delay even though
ordinary players were delayed. Native baseline confirms both Normal and Astig:
a4m move reads immediately as4m for the companion, while player beliefs update
only0.131m and0.276m respectively on the same10ms observation step.

Observe now samples the existing cached Bodies list. Both actor types use the
same tier/lapse response. Own position remains exact. Cached position/velocity
belongs to the observed body reference as well as its seat: a replacement in
that seat cannot inherit a dead body's position or prediction velocity. At and
AheadOf validate that identity; the first observation of a fresh body is exact.

No new allocation-producing scene query, AI tier/tuning, input/skill rule,
Phaister/Paete mechanic, model, animation, VFX, SFX, map or loading change.
The additional identity map is bounded by the existing body seats. This changes
host AI observation behavior, not client prediction/wire semantics; protocol114
remains current. No measured FPS/whole-game allocation improvement is claimed.

## Evidence

Native baseline2/2fails at the intended defect, with ordinary actors correctly
lagged and companions reading immediate truth. Final4/4passes: Normal/Astig
matching response, own player feet exact, fresh body in reused seat starts at its
own position and then lags normally, and companion bot feet remain exact.
No fixture/tooling repair or unrelated suite run. Real AIController Observe/At
and RoundDirector companion registration are exercised; the fixture drives
positions directly and does not pretend to be a whole match.

Sourcefcd3507cf plus three owned source/test overlays; native6000.5.8f1/D3D11
imported candidate baseaecc0ee2.655final hashes have no drift; owned files match
the tested candidate. Both jobs terminal and profile/input preferences preserved.
XML/input/runner receipts are in bot-companion-observation-checks; raw logs stay
in isolated Logs/feedback-0930/bot-companion-observation*.log.

Whole-match difficulty/taste, actual peer/player behavior, and Haunted sensor/
unknown-target fairness remain separate. This fixes the companion reaction-lag
bypass and does not claim every bot perception path is complete.
