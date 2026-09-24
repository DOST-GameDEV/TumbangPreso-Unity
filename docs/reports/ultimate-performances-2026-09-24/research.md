# Ultimate performances: research

REFINE-2.11, 2026-09-24. Owner brief: every hero gets a memorable moment in which the stage briefly
feels like theirs; research first, then a per-hero plan, then implementation; Phaister laughing and
levitating is an explicit direction; 2.8 s is not a cap; one hero at a time, *"dont js spam copy
paste stuff bcz it will be boring"*.

## 0 · What this research could and could not see

This pass ran on a cloud Ubuntu machine with no video playback. **No footage was watched.** The
evidence below is written sources (wikis, publisher pages, articles about developer talks), the
repository's own earlier reference record (Sepak U screenshots in
`docs/reports/visual-research-2026-09-23/findings.md`), and general knowledge that is labelled as
such. The footage links are listed so the Windows machine can check the claims frame by frame. Where
a claim is a reading of a text rather than an observation, it says so.

## 1 · References and what each one teaches

| Reference | What the source says (and where) | What it teaches TUMP |
|---|---|---|
| **Mario Strikers: Battle League, Hyper Strikes** (sports game, per-character super shot) | The game drops into a cel-shaded cutscene; *"three freeze-frames of their face are shown, with the third one showing a close-up of their eyes covered in flames"*; then a character-specific action: Peach cartwheels and rolls the ball along her arms, Bowser breathes fire on the ball, Donkey Kong eats a banana in one bite before throwing, Rosalina kicks the ball out through the stadium dome to a ball-shaped planet, Wario shakes his rear at the ball ([Super Mario Wiki, Hyper Strike](https://www.mariowiki.com/Hyper_Strike)). Footage to check: [all Hyper Strikes](https://www.youtube.com/watch?v=3Vnwytk_VCg). | The closest genre match: a SPORT game's super is an ACTING beat, not an explosion. Every one is a small piece of personality (Peach's grace, DK's appetite, Wario's rudeness) before the ball goes. The stage changes style for the duration (cel-shade), which is the "stage belongs to them" device. The shared frame (three face beats) keeps a common grammar so the personal part reads as personal. |
| **Street Fighter / Guilty Gear super activation** (general knowledge; frame counts not verified this session) | Activation freezes the game for a short superflash while the background dims or changes behind the lit character; the long cinematic plays only on a landed hit. Arc System Works' GDC 2015 talk (*Guilty Gear Xrd's Art Style*, J. C. Motomura, [ASW page](https://www.arcsystemworks.com/guilty-gear-xrds-art-style-the-x-factor-between-2d-and-3d-talk-from-gdc-2015-is-now-available-online/)) describes blocking anticipation, strike and follow-through poses first, holding keyed poses rather than interpolating, and checking readability at low frame counts ([Kotaku summary](https://kotaku.com/breaking-down-the-animation-of-guilty-gear-and-dragonba-1836849279)). | Two ideas. (1) The world recedes and the character stays lit: the stage darkens BEHIND them. (2) Poses first, holds between them: a performance is a chain of strong readable shapes with time to register, not constant drift. TUMP's seven-bone rig is well suited to held poses. |
| **Super Smash Bros. Ultimate, Final Smashes** | Sakurai (E3 2018 Direct): every Final Smash reworked to be *"simple and straight to the point"*, universally faster, transformations turned into single cinematic attacks ([SmashWiki](https://www.ssbwiki.com/Final_Smash); [Wikipedia](https://en.wikipedia.org/wiki/Super_Smash_Bros._Ultimate)). | The counterweight to "longer is fine": a cinematic that pauses everyone is watched many times per session by people who did not cast it. Length must be paid for with content, and the shortest version that lands the personality wins. |
| **Jujutsu Shenanigans (Roblox), Domain Expansion** | After a short hand-sign windup (1.25 s; 1.5 s for two), a large dome forms whose interior carries the user's technique as an environment; a popup appears for everyone within the radius ([Sportskeeda domain guide](https://www.sportskeeda.com/roblox-news/jujutsu-shenanigans-0-2-domain-guide), [Sabertooth mechanics guide](https://sabertoothgames.com/jujutsu-shenanigans-domain-expansion-mechanics-explained/)). Footage: [every domain explained](https://www.youtube.com/watch?v=17QqdFjhVwo). | The most direct version of "the stage belongs to them": the caster's world REPLACES the court for a moment. A GESTURE (the hand sign) starts it, which is a body silhouette the player learns. In TUMP this becomes a per-hero backdrop drawn behind the caster in the introduction only, never on the live court. |
| **Blue Lock: Rivals (Roblox), awakenings** | Each style has an awakening cutscene before new moves unlock; during it, actions do not stop except walking and abilities ([Blue Lock: Rivals wiki, Mechanics](https://bluelockrivals-roblox.fandom.com/wiki/Mechanics), search summary; the page itself refused automated fetch). | Owner-named reference. The awakening is a PERSONALITY moment first (the player's chosen character "becomes" their best self) and it is repeated often, so it is short and pose-led. TUMP pauses the whole court, which is fairer for a 1-vs-3 game than letting the round run under a cutscene. |
| **The Strongest Battlegrounds (Roblox), awakenings** | The Tech Prodigy awakening delayed an update, reportedly cost over $10,000 and was carried by a single animator ([TSB wiki, Awakening](https://the-strongest-battlegrounds-rblx.fandom.com/wiki/Awakening), via search summary). | Evidence that the Roblox audience values these moments enough for studios to spend on them, and that they are authored per character, not templated. |
| **Genshin Impact, Elemental Bursts** | Five-star characters get their own mini cutscene on cast; the cutscene is skipped on water or near enemies, but the animation still runs and takes the same time to trigger the effect ([Genshin wiki, Elemental Burst](https://genshin-impact.fandom.com/wiki/Elemental_Burst) and [HoYoLAB note](https://www.hoyolab.com/article/212433), via search). | Exactly TUMP's existing rule for a blocked camera: a fallback view with THE SAME duration, so timing stays fair. Kept. |
| **Inazuma Eleven: Victory Road, hissatsu** | Special moves trigger an animation every time; a full gauge triggers a more dramatic "Hype Animation" ([Operation Sports](https://www.operationsports.com/how-special-moves-work-in-inazuma-eleven-victory-road/)). | Tiers of the same move are normal. TUMP's tiers are the full cinematic and the reduced-motion/effects version, and both must be the same length and both must be good. |
| **Overwatch, ultimate voice lines** | Enemy and ally hear different lines; the line is a split-second threat cue and carries each hero's language and personality ([Sakkou guide](https://sakkou-productions.com/complete-guide-to-overwatch-ultimate-voice-lines-every-heros-iconic-sound-cues-explained/); [forum thread on inconsistency](https://us.forums.blizzard.com/en/overwatch/t/ultimate-line-inconsistency-and-more/976960)). | The voice belongs INSIDE the performance at the gesture it describes (Phaister's laugh while she laughs), not as a separate stinger afterwards. |
| **Sepak U** (repo record, official screenshots) | During a super the HUD hides and the summoned spectacle is drawn as an ink sketch BEHIND the action, so the gameplay objects keep the colour peak. | The backdrop is a background: darker and less saturated than the caster, and gone before play resumes. |

## 2 · Principles taken into TUMP

1. **Acting before effects.** Each performance is a sequence of poses that says who the hero is. The
   effect layers illustrate a gesture the body is already making.
2. **Held poses with time to read.** Three to five strong shapes per performance, each held long
   enough to register (0.25 to 0.6 s). Movement between them is quick. This is the Xrd lesson and
   the owner's HOME method rule "every beat breathes".
3. **The stage recedes behind them.** A per-hero backdrop in the introduction's render copy only,
   darker and less saturated than the character. The live court is never changed by the intro.
4. **One signature silhouette per hero** a player could recognise from the backdrop shadow alone.
5. **Camera is part of the acting** but never the aim: the intro renders from its own camera into an
   overlay (existing contract), cuts are few (two or three) and motivated by the performance.
6. **Voice inside the gesture.** A hero's existing ultimate voice plays at the moment the body does
   the thing the voice is (Phaister's laugh during her laugh).
7. **Truthful handoff.** The last pose flows into what the live ability does first (Sean's coil into
   the leap, Dante's load into the forward fissure, Nemu pointing Kuro ahead, Rafi's arm sending the
   wave). The shared boundary still decides when the real ability starts.
8. **Length is earned per hero and bounded.** The pause stops all four players, so it is chosen from
   the hero's content and personality. Zack, the casual speed player, stays the shortest.

## 3 · Duration decision

| Hero | Seconds | Why this length |
|---|---|---|
| Zack | 2.8 | Personality is ease and speed; a long wind-up would contradict him. Same as the old shared value. |
| Cheska | 3.2 | Quiet and precise: one reading of the space, one precise hand phrase, a snap. |
| Sean | 3.4 | Patient focus is the character: the held look before the burst needs its own half second. |
| Rafi | 3.4 | A tease (feint, beckon) and a wave rising; both beats need a hold. |
| Dante | 3.8 | "Refuses to be rushed": weight is shown by slowness; the mountain lift needs time to carry mass. |
| Nemu | 3.8 | Two characters acting (Nemu distracted, Kuro nudging, the knowing look, the transformation). |
| Phaister | 4.2 | The owner's explicit direction: a chuckle that grows into a laugh, a rise off the ground, the eclipse, a landing. She is the one hero who enjoys the setup more than winning. |

A cohort of simultaneous casts uses the LONGEST member's duration, derived identically on every peer
from the accepted commits (seat to hero), so the shared boundary stays one number. Range 2.8 to 4.2 s
keeps the worst case within 1.5 s of the old value.
