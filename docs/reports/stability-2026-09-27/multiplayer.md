# Multiplayer investigation, 2026-09-27

## Published lifecycle fixes

Initial source review at `04886cc4` found that host starts could resume after STOP
or a newer operation, and matchmaking continuations could publish stale success
after cancellation. Published fixes use operation ownership through shutdown,
host/Relay awaits and matchmaking completion. Pending queue joins wait for both
connection and an assigned seat; cancelling an old attempt cannot stop its successor.

This batch is no longer native-unrun: the first focused lifecycle group passed
11/11, pending join 1/1, and the post-merge EditMode group 37/37. Exact revisions,
checks and limits are in [validation.md](validation.md). These controlled local
tests are not live Relay, two-peer reconnect or service-timing qualification.

Room-title fixes are published in `367a78ae`: known LAN directory titles are
retained by the current session, room code and attempt, including controller
recreation and transport restart. See [joiner title](qa04-joiner-title.md) and
[QA2](qa2-validation.md). Physical typing and actual peer coverage remain separate.

## Client-only skill report

The owner reports that many skills may fail in multiplayer or affect only the
casting client. No specific affected set or reliable reproduction was supplied.
Keep three questions separate: did the host accept the input, did authoritative
gameplay occur, and did the owner/observer receive its presentation and state?
Do not weaken host authority or infer success from local effects.

A source audit of the current integration candidate reports 93 message envelopes
with zero count/type mismatches. The authority scan's RafiWaterField alert is
caller-gated; the Paete pull-sound alert has an existing PlantPulled receive route.
Those alerts do not establish product defects.

Two specific findings remain OPEN:
- Dante's ultimate calls HitFeel.Land inside its host-only impact loop, so that
  local feedback does not execute on remote peers. Authoritative impact resolution
  is separate and remains host-owned; this is not evidence that damage/status fails.
- PlayAbility advances its event watermark before null-conditionally applying to
  the installed body. An event received before that body exists can be consumed
  without playing. The conditional source loss is real, but actual occurrence and
  its proper recovery require a focused reproduction before changing routing.

The current flight unit includes protocol-61 episode/receipt/snapshot ordering.
Its exact implementation and qualification status are in
[Featherfall](../amihan-kit-2026-09-27/featherfall.md). It does not establish that
every kit works across real peers.

## Sean after Cheska's ultimate

QA-15 remains unresolved. The expected Ice freeze is 2.5 seconds after impact,
followed by reduced speed; the ultimate introduction is a separate interval.
Source inspection has not proved a permanent Sean-specific movement lock.
[Sean investigation](sean-freeze.md) records competing explanations and the opt-in
two-process diagnostic. Controlled movement bypasses physical input and bots, so
a future pass would not cover every interpretation of the tester report.

Required peer qualification includes host, owning client and observer outcomes,
join/reconnect state, stale-packet rejection and interrupted ability recovery on
matching builds. No real-player acceptance result is claimed here.
