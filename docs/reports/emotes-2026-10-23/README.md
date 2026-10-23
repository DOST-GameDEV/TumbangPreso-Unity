# Emotes, second set (2026-10-23)

Owner: "thoroughly research emotes in games like fortnite, pubg, cod and etc and give our game completely new
emotes", "thoroughly refine it in terms of animation", "refine the ui and button for emotes too", and from the
morning: "make sure the animation looks good for each character kasi iba iba size nila".

## What the research says

| Game | What players use | What makes it work |
|---|---|---|
| Fortnite | Default Dance, Take The L, Orange Justice, Laugh It Up, plus short gestures | One idea per emote, a silhouette you can read from across the map, dances on a steady beat that loop seamlessly. A six-slot wheel: hold the key, aim, release. |
| PUBG / PUBG Mobile | ROFL (laughing), Thanks (blown kiss), Bow of Respect, Surrender | Social signals more than dances: laughing at someone, thanking, showing respect after a fight. |
| Call of Duty / Warzone | Killcam taunts and gestures ("Bravo", "Oh Snap!", "Good Job") | Short, aimed at a person, with the punch in the first half second because the camera may cut away. |
| Overwatch | Per-hero emotes and poses | Personality comes from the body: how far it leans, how big the arms go, the timing of the hit. |

Animation principles the sources agree on: anticipation before every hit (even one beat of wind-up makes a move
read), poses pushed until they read as a black silhouette, committed exaggeration (timid exaggeration reads as a
mistake), and a held contact pose before the recovery.

Sources: [Sportskeeda, top Fortnite emotes](https://sportskeeda.com/esports/top-5-popular-fortnite-emotes-time),
[Sportskeeda, OG Fortnite emotes](https://www.sportskeeda.com/fortnite/10-og-fortnite-emotes-time),
[Epic, Fortnite emote wheels](https://www.epicgames.com/help/c-202300000001636/c-202300000001721/how-do-i-change-my-fortnite-emote-wheels-a202300000014892),
[GamersDecide, PUBG Mobile emotes](https://gamersdecide.com/articles/pubg-mobile-best-emotes),
[Sportskeeda, PUBG Mobile Lite emotes](https://www.sportskeeda.com/esports/5-best-pubg-mobile-lite-emotes-time),
[1v9, PUBG Mobile emotes ranked](https://1v9.gg/blog/pubg-mobile-best-emotes-ranked),
[Insider Gaming, Warzone emote](https://insider-gaming.com/everyone-favourite-warzone-emote-coming-back/),
[80.lv, fluid and powerful animations](https://80.lv/articles/tips-on-making-fluid-and-powerful-animations),
[GDC Vault, Powerful and Effective Animation](https://gdcvault.com/play/1021657/Powerful-and-Effective-Animation-for),
[300mind, radial menus in game design](https://300mind.studio/blog/radial-menus-in-game-design/),
[Inven Global, Overwatch animation](https://www.invenglobal.com/articles/1187/how-overwatchs-first-person-animation-breathed-life-into-heroes).

## The set

One feeling per slice, on the old wire ids (they cross the network and never change):

| Id | Wheel | Feeling | Motion | Loops |
|---|---|---|---|---|
| yes | LET'S GO | Excited | Crouch, jump with both fists punched up, pump to the ears, second jump, one fist pumped | yes |
| no | NO WAY | Frustrated | Both hands clamped on the head, elbows out, head shaking; a sag; then both arms out at the sky: "why?!" | yes |
| sit | TOO EASY | Smug | Hop down, lean back on one elbow, other hand behind the head, legs out, slow look round | holds |
| crouch | HA HA | Laughing at someone | Point, doubled over, head thrown back, hand slapping the knee | yes |
| dance | DANCE | Fun | Two low loose-armed street steps, then both arms up waving over a hip sway; bounce every beat | yes |
| tpose | COME AT ME | Taunt | Chest out, chin up, both hands beckoning twice, arms spread wide with a boxer's bounce | yes |
| bow | GG | Respect | Fist pressed into the palm, a deep bow, upright with the salute held and a nod | holds |

Fitted per rig: each hero's arm reach is measured off its own bind (`EmoteFit`, logged by the bake as
`[EmoteFit] <rig>: reach r, short s`) and the poses that need the hands high or at the face push the arch, the lean
and the head further on the short-armed heroes.

The two poses that put a hand ON the head (NO WAY's clamp, TOO EASY's pillow) are solved on each rig at bake time
(`ArmSolver`): the arm and elbow angles are swept on the rig itself, the hand's own vertices riding the forearm,
and the pose landing nearest a target on that rig's head box wins, logged as `clamp (x, y, z) fold f`. Four rounds
of hand-keyed angles had all left the hands at the collar. On Dante, Amihan and Phaister no arm angle lifts the hand
past about a fifth of the head (their heads start at the shoulder and their arms are short), so their clamp lands on
the cheeks and their pillow at the nape; Nemu and Paete reach the sides and back of the head.

Bots: knocking the can down now plays HA HA at the taya (owner: "laughing at taya"); being tagged and losing
play NO WAY; the other celebrations and taunts pick from the set as before.

## Traps found while keying

- The arm bones are T-posed and keyed in Euler order ZXY, so the arm's X channel is only a forward swing while the
  arm hangs; with the arm up at the side it twists the arm about its own length. The elbow hinge always folds
  toward the arm's front, so fists by the ears need a level arm twisted X -90 (the hinge then folds up), and fists
  meeting at the chest need a hanging arm twisted Y +-70 (the hinge then folds across). Keying "arm up, fold 110"
  without the twist folds the forearms forward instead.
- OPEN, needs the owner's OK (finalized hero): Paete's soles render as black slabs whenever they face the camera,
  in the sit and in any hop. The model has sole faces and the atlas under them is plain brown (105, 73, 45), so it
  is his toon/palette shading, not the pose or the mesh. Splaying the sit's legs did not hide it.
- Lowering the sit's legs would lift the seat: these kneeless rigs are grounded on their lowest vertex, the heels.
- The poses were first checked in a Python stick-figure preview of the same axes
  (`tools/emote_pose_preview.py`) before the native strips. It is wrong for raised arms: trust the strips, and for a
  hand that must land somewhere, solve it on the rig (`ArmSolver`).

## The wheel

Owner, 2026-10-23: the wheel "sucks, it isn't in the theme of anything else in the game HUD". It was still the hub's
wood with an amber rim after the match HUD moved to smoked glass (`docs/UI_DESIGN_METHOD.md`, glass HUD). It is now
glass like every live HUD panel: the warm smoked fill with its cream hairline and short shadow, ivory pictures and
words outlined black, the hub in the denser glass of a keycap's well, and honey on one thing only, the slice about
to play (a honey band along its outer edge, its picture and word, and the hub's pose and name). It does not lean: a
sheared ring would stop pointing where the stick points. High contrast swaps the glass for the black plate.

A pose picture (`EmoteGlyphs`) leads each slice with a short word under it; the hub repeats the chosen pose large
with its full name. It pops open, the chosen slice moves out and grows, it ticks on each new slice and clicks on
the pick. On a phone the finger that pressed EMOTE drags the pointer (before this, a phone could open the wheel
but never steer it).

## Evidence

Strips: `Logs/motion/<hero>_emote.<id>_quarter_vN.png` with `.txt` traces (`EmoteMotionAuthor.WireAndReview`).
Video: every emote on every hero in motion, from about the gameplay camera's height
(`EmoteReel.Run`, then `python tools/encode_emote_reel.py`): `Logs/emote-reel/<hero>_emotes.mp4` and
`all_heroes_emotes.mp4`.
Wheel: `EmoteMotionAuthor.ReviewWheel` photographs the real wheel with each slice chosen into `Logs/emote-wheel/`.
Tests: `TumbangPreso.Tests.EmoteMotionTests` (EditMode).
