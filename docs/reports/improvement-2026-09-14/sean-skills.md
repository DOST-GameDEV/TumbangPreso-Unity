# Sean skill timing, heat and empowered throw review

Work follows the original Sean model, outfit, authored clips and three distinct
jobs: forward rush, loaded throw and committed leap/landing. No character mesh,
hand geometry, save ID, objective rule or equipment trajectory has been replaced.
This report is an active checkpoint, not completion of the larger rework.

## Reproduced defects

The original three-slot capture only pressed Ignition. A real empowered release
showed that it consumed the gameplay flag but left its hand emitter running.
The elevated ultimate also detonated while airborne: caster Y47.12653 at1.805656s.
Both failed in Logs/sean-timing-baseline-v1.xml, with preserved before CSVs.
The diagnostic baseline began at60m; the bounded regression uses20m so the real
landing falls inside its4s observation window. Neither is a proposed map height.

The first correction passes the actual throw and elevated landing checks. Its
fresh recording revealed a separate visibility problem: CameraRig had cached
the held body mesh before the new ember was attached. The body ember therefore
appeared beside its FPP copy. The effect now follows its source renderer's
visibility and shadow mode, including hidden body props.

The complete throw video then exposed pink flight lighting/trail and a world
slipper filling the eye on the release frame. UiTheme.HeroFireBright was pink,
and the physical launch starts15cm ahead of the eye. The flight correction uses
amber/ember world colours and retains the physical launch point. CameraRig keeps
the released world mesh hidden from its FPP owner until the mesh support extent
clears the eye by25cm. This does not move the projectile through nearby cover.

Finally, three separate players on a150ms one-way owner link confirmed that
the host consumed Ignition while both clients retained its charge. The accepted
flight was visible in all three traces. The first trusted fire-flight snapshot
now consumes the previous holder's charge and creates the peer flight effect.
Repeated flight snapshots cannot consume a subsequently loaded charge or restart
the trail. Its effect clears when the projectile leaves fire flight.

## Presentation

- Ignition has three compact curling flame tips on the actual slipper, with
  distinct toe/side/heel placement. Body and FPP derive placement from their own
  mesh bounds. The pink BoltHead symbol and fixed unbound hand particles are gone.
- Flame Rush leaves a low directional heat wake and cooling ash along its real
  path. The repeated circular fringe and floor ornaments are removed. Its visible
  ground footprint does not shrink inside an unchanged active hazard radius.
- Supernova retains its leap poses, holds the dive while airborne, and starts
  contact/recovery at the actual grounded impact. It leaves a broken hot edge
  and scattered cooling heat across the landing area. The old orange badge,
  generic early column and camera blast on keypress are removed.
- Model stretch/squash is reduced from35/45/40percent to6/6/8percent. The existing
  body and FPP cast poses supply most of the movement.
- World fire uses amber and warm ember colours. The roster/UI accent remains
  independent. Flight has a shorter, thinner trail and restrained local light.

The ground forms share heat as a material detail. Rush's primary form is a short
linear wake; Supernova's is the landing boundary and cooling area. Neither uses
the carried ember as a scaled floor decoration. The effect budget is bounded,
and generated meshes/materials use the existing explicit ownership helpers.

## Evidence so far

- Timing-v2:3/3 pass, profilec848b4a12bd0. Actual fire flight, charge consumption,
  no lingering emitter, grounded20m diagnostic landing, real three-slot capture.
- Heat-v4:5/5 pass, profilec681378b6ce9. Also covers expiry, refusal, hero
  replacement, source visibility and raised-floor placement. Owner/body sequences
  under Logs/sean-heat-kit-v4 were inspected and encoded at captured timestamps.
- Full throw-v1:1/1 pass, profile35dd02e37811, images and videos under
  Logs/sean-ignition-throw-v1. The first released frame is28/t1.9681. This is the
  evidence that exposed the additional pink flight and eye intersection.
- Actual delayed Ignition baseline: host463/owner449/observer454samples, five
  fire-flight samples on each, stale charge on both clients in the post-throw
  comparison window. Logs/sean-net-ignite-baseline-v1/result.json.

The first ground pass failed because a new MaterialPropertyBlock was allocated
in a MonoBehaviour field initializer. Allocation moved to Awake. A replacement
test also inspected an obsolete kit boolean; it now checks the live replacement
kit and absence of the old visual. Expiry/refusal still require the live flag
to clear. No production BindHero behavior was changed to satisfy that test.

Flight-v2 compiled no tests because a new SeanHeroKit reference lacked its
namespace qualifier. It was qualified before the replacement run. Failed logs
remain available; neither failed attempt is counted as a gameplay pass.

## Remaining acceptance work

The latest flight/snapshot correction passed6/6 focused checks, including the
full throw recording, both-mode handling and snapshot replay. The actual first
released frame was inspected: the shoe no longer fills the eye. Profile receipt
fa5208222244. Internal buildv2 succeeded, profile61ce6816a649,
RuntimeSHA70fd0694d08c4c472f3eed6505e50a795163cd506d2a797f6563b139b87cb936,
Builds/SeanSkillReview/TumbangPreso.exe. Protocol31; Desktop unchanged.

Actual delayed three-player Ignition now passes: host462/owner451/observer454
samples,5/6/5fire-flight samples, no stale charge, possession or carried ember
in the post-throw window. All7existing named-profile files were restored.
Actual delayed Supernova also passes:458/447/452samples, one crater per peer,
first crater after local ground contact, no final crater or cast pose. Measured
rise3.354/3.125/2.892m includes each peer's sampling and motion smoothing; it is
not a claim of identical frame-by-frame flight. All8named-profile files restored.
Both use150ms each direction on the owner link. Results are in sean-evidence.
Both helpers and their owned players exited. The earlier build is superseded.
The recorded ordinary motion has been reviewed. Remaining variant, interruption
and overlapping counterplay coverage is tracked separately before closing the
complete six-kit review. Tests alone are not artistic or human play-feel approval.

The larger queue continues after this checkpoint. Inday FPP remains deferred;
UI stays last within the existing work; the chosen seventh hero B and map C
remain LAST LAST.


## Variant comparison follow-up

The two focused real-input comparisons pass alongside the Zack baseline capture
in Logs/sean-variants-zack-baseline-v1.xml (3/3 total), profile9e538377979e.
Afterburn traveled2.2599m versus4.0921m for Rush. Its observed whole wake lived
4.3519s versus3.4527s, including the staggered patch emission. Each patch retained
its1m gameplay radius and the new native heat presentation.
Flare's sampled released speed was15.6634m/s versus13.0679m/s, with a7.5s arming
window versus10s. Those are observed flight samples, not an exact input-speed
ratio or an impact-radius/counterplay test. CSVs are in sean-evidence.
