# Next Round:3.5seconds

The latest Feedback request changes ordinary Next Round from5seconds to3.5.
Halftime remains10seconds with the same recorded-highlight/fallback/standings
path. The host-authored began timestamp still defines a single shared boundary;
remaining-time labels observe that clock rather than starting their own timer.

Only BreakDuration changes in the presenter. Protocol133 prevents older peers
from independently deriving a5second break from the same packet. Packet fields,
recording format13, replay playback and all gameplay rules are unchanged.
Matching rebuilt clients are required; this is not a new actual-peer claim.

## Verification scope

The baseline scheduled-break case fails exactly at the new3.5second expectation,
observing5seconds from the original runtime. Existing test assertions were
updated to the owner's new duration and corresponding elapsed/late-time bounds.
Import/compilation is separated from each native graphics process under the
same memory guard. Individual candidate receipts are retained in evidence.

The focused acceptance set covers ordinary advancement, unchanged10second
halftime/fallback, final-match no-break, frozen world pixels and actual keyboard/
mouse input, and a simulated late client's refusal to advance or restart an
already consumed boundary. Existing actual-replay footage is retained; no new
full-player, actual network peer, hardware performance or human-approval claim.

## Final result

All three focused cases pass, each in a fresh graphics process with identical
frozen inputs: scheduled boundary16.905s, frozen-input/frame6.533s, late-client
2.959s. Outer durations45/35/30seconds; all guard reasons null. No tooling repair
or retry was needed. The scheduled case observes ordinary round advancement in
its3.4-4.0s tolerance, keeps halftime10s, rejects duplicate boundaries and does not
open a break after the final round. The other two retain real keyboard/mouse
refusal, exact frozen pixels, no early dismissal, and late-client non-authority.
The960x540 Next Round capture was inspected. This is three focused native cases,
not an assertion that every spectator/replay test or new matching player passed.
