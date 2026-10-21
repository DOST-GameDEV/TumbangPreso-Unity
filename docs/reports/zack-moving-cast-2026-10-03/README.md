# Closed Circuit walking legs, October 3

Closed Circuit permits the caster to walk while acquiring a target. Its authored
upper-body action previously suppressed the leg gait layer, so the physical
motor moved but the model's feet froze. The mobile-action exception now includes
hero-zack-circuit alongside the existing Dante guard action. Other committed
actions, including slide, continue to own their authored legs.

No mechanics, timing, stamina, clip assets, first-person gesture, target rules or
protocol changed. Network protocol remains142.

## Evidence

- Original real-motor regression failed: the accepted caster moved more than
  0.5m while gait weight stayed0. The test was preserved while match-entry work
  took priority, then reused unchanged for this fix.
- Candidate ClosedCircuitTests:11/11 passed in1.7845132s, exit0/no guard. This
  includes actual walking and leg-bone movement, standing cast/contact timing,
  the actual owner gesture/palm attachment, cancellation and existing targeting
  controls. Walking asserts gait weight above0.8 and leg rotation above10degrees.
  Peak tree RSS3656441856bytes/container7309180928bytes.
- Six EditMode controls passed in0.4933287s, exit0/no guard: existing Dante mobile
  guard with/without a slipper, committed-slide gait suppression, and three
  shipped Closed Circuit body/owner/asset checks. Peak tree RSS3504807936bytes/
  container6594527232bytes.
- Fifteen frozen candidate inputs match main and the isolated native overlay.
  The second run changes only the added support-pose test among those inputs;
  the production source from the11-case run remains identical. Both settings
  files and named profiles were restored.

These are native component, actual motor/rig and authored-asset checks. They are
not a newly captured moving film, full-court playtest, packaged build, actual
network-peer test, performance measurement or human taste approval. Existing
standing cast graphics do not establish the moving variant's visual acceptance.
