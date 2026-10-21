# Grounded slipper circle

The owner requested that the circle appear only after the slipper lands.
SlipperRecall now requires the existing authoritative Loose state; Held and
InFlight states cannot draw its ring or off-screen circle. Existing owner, role,
Haunted, screen-takeover and pickup-range handoff rules remain unchanged.
This is a presentation gate, not a flight, pickup or network-rule change.

Native focused case1/1 passes0.7677504s10:24:06UTC. It verifies held-hidden,
actual host flight-hidden, actual physics landing-visible, received relaunch
hidden, received landing-visible and immediate pickup-hidden. Flight/ground
pixels inspected. Frozen inputs unchanged, settings restored, Editor exit0,
no OOM increase. Existing full-scene recall shot expectation updated to hide the
in-flight marker; the full-scene suite and actual-peer review were not rerun.
