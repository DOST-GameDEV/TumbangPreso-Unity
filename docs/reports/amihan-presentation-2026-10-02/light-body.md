# Amihan's light body: footsteps, the floating run and every verb

Owner, October 2: *"make amihans footstteps feel light and when she runs let her
float a bit and make it look like she flying"*, then *"think abt how hhigh shhe hhas
to float and also consider how shhe would jump tag throw etc and move"*. Asked
whether that includes sound, the owner chose a new airy step sound.

This replaces one line of the earlier direction ("never floats for show") for her
ordinary run only. Featherfall stays her only real flight.

## Her body, measured

Read from `team-amihan.glb` at the shipped person scale (2.38):

| Measure | Value | What it means |
|---|---|---|
| Height | 1.87 m | Mostly head: the head bone sits at 0.82 m and the head reaches the top |
| Hip pivot (leg reach) | 0.42 m | Short legs; a few centimetres of gap is a large share of a leg |
| Shoulder | 0.69 m | Arms are short and hang beside a wide coat |

Gameplay heights the float must never be confused with:

| Thing | Height | Source |
|---|---|---|
| A jump's apex | 0.84 m, 0.58 s in the air | `Balance.JumpVelocity` 5.8, `Gravity` 20 |
| Featherfall | 2.8 m, untaggable | `AmihanRules.UpdraftHeight`, `CharacterMotor.IsTaggable` |
| Any jump or float below that | taggable | the reach is flat distance |

## How high she floats, and why

- **Walking: 2 cm.** Her feet still touch. Lightness comes from quick feet that
  spend their time under her, no stomp and the existing skip.
- **Running: soles 12 to 19 cm above the court.** The run's rigid legs scissor
  under her; the shared foot plant drops the hips about 7 cm when the legs are
  furthest apart. The float rises by that much at the same moments, so her hips
  and head glide level while the legs swing in the air beneath. Level hips with
  moving legs is what reads as flying rather than bouncing.
- **Why not higher.** At 25 cm and above the gap starts to read as a jump from the
  court camera, and a taya could mistake an ordinary run for Featherfall, which is
  the one state where she cannot be tagged. 19 cm is under a quarter of a jump and
  under a fifteenth of Featherfall. Her shadow and player ring stay on the court,
  so where she stands is never in doubt.
- **Why not lower.** At the court camera a 0.18 m gap is about 7 pixels at 720p and
  11 at 1080p, enough to see light under her shoes beside her shadow. Below about
  8 cm the float disappears into the gait's own motion.
- **Speed.** The float follows the run weight, so a jog lifts a little and a sprint
  lifts fully. It rises over 0.22 s and settles over 0.2 s, independent of the
  gait's 0.08 s handover, so starting any action never drops her two frames.

## Every verb with the float

| Verb | What her body does | Why |
|---|---|---|
| Idle | Feet on the court | Stillness is where the float means nothing |
| Walk | Light quick feet, skip kept, 2 cm | "Light footsteps" |
| Run, sprint | Lean into the travel, arms swept back and out like the wind is behind her, legs scissoring and trailing, soles 12 to 19 cm up | "Float a bit, look like she is flying" |
| Carrying a slipper | Same float; the carrying arm keeps its existing carry pose | The retrieval stays readable and the shoe rides with her hand |
| Throw (charge and release) | She settles onto her toes while charging, throws from the court, and lifts again only when she runs on | A throw must come from where the slipper actually leaves; a planted throw also reads as intent |
| Quick tag (punch) | The float settles slowly, so a 0.34 s tag barely dips | A light chaser, not a stomp |
| Dash tag (lunge) | The dive takes her down; the float settles under it | The lunge is already a whole-body dive |
| Jump | No crouch drop: the float hands into the real jump and fades over 0.35 s while she rises; in the air her legs trail back together and she leans into the travel | Her jump continues her glide instead of restarting from the ground |
| Landing | Lands on her toes with a small 6 percent squash instead of the shared up to 30 percent | Light landing; the real landing time is unchanged |
| Slide, shove, stunned, tripped, rooted, frozen, swimming, edge recovery, emotes, cutscenes | No float | Gravity and other systems own those bodies |
| Featherfall | Its own flight layer owns her; the run float is off | Two layers must never fight over the root |
| Her own first-person view | The camera does not float; her hands glide with a softer, rounder bob instead of a dip at each contact | A floating camera is nausea and lies about her eye height |

Nothing above changes gameplay: speed, capsule, contact, tag reach, throw origin,
landing time and network state are untouched. This is presentation on every peer,
derived from the same replicated body state.

## Her footstep sound

`step_amihan`, a footstep-family cue (not a skill cue, so the September 29 skill
sound switch does not silence it). One per stride cycle as before, built from her
own wind instrument: a short soft cloth brush, a small breath of air rising in
pitch, and a quiet sandal tick far below the shared rubber step. It is synthesized
and seeded (no external samples), provisional until the owner hears it in play.
Amplitude numbers are measurements, not listening approval.

## Not done here (named follow ups)

- A bespoke tag reach and throw shape for her (the shared limb-pointing layers
  serve everyone today). The float now hands into both cleanly.
- The shared `land` thump still plays on her harder landings.
