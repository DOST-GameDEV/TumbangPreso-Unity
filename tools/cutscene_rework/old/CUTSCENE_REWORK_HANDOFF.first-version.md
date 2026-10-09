# Ultimate cutscene reworks: handoff

Written 2026-10-08 by the session that reworked Paete's. It is for a NEW session that will rework the other eight
heroes' ultimate cutscenes, in batches of three. Paete's is the worked example; its full record, with every owner quote,
is `docs/CHARACTER_REDESIGN_DANTE.md` section 15.11 (read it to the end). Read `AGENTS.md`, `docs/HERO_KIT_METHOD.md` and
`docs/SKILL_NETWORK_CONTRACT.md` first.

## The job

The owner, of Paete's cutscene after its models were only repainted: *"this is essentially just the same cutscene textured
better ... it could use some more creativity on it"*. What he then approved or asked for, in order, is the brief for
every other hero: a bolder staging idea chosen from options, real weight in the timing, sound picked by ear, and every
detail checked from where the camera really stands.

**Batches (the owner asked for threes). Order is a proposal: ask him before starting.**

| Batch | Heroes | Length now | State of the cutscene |
|---|---|---|---|
| 1 | Dante, Cheska, Sean | 3.8, 3.2, 3.4 s | Short, about 100 to 140 lines of staging each: the most to gain |
| 2 | Zack, Nemu, Rafi | 2.8, 3.8, 3.4 s | Short; Nemu's has Kuro, Rafi's has water in its own shader |
| 3 | Amihan, Phaister | 5.9, 6.0 s | Already directed at length in September (about 1,300 lines each): ask whether these want a new idea or only weight, sound and polish |

Dante, Cheska, Sean and Nemu each have a second table (`<hero>-held`): the performance while holding a slipper.

## How each cutscene is done (what worked on Paete)

1. **Read the hero's own history first.** `docs/reports/<hero>-kit-*/direction.md` and the comments in
   `Runtime/Visual/HeroIntroductionScene.<Hero>.cs` hold what he asked for by name. Keep it unless he takes it back, and
   say when a new idea conflicts with it.
2. **Pitch two or three staging ideas in plain words and let him pick.** Paete's were: she is vast in the sky; the camera
   rides the roots underground; her hand raises the tree. He chose a combination and changed one ("a god visible in the
   sky"). Do not build before he picks.
3. **Build one idea at a time and film it at once.** Request `introfx <tag>` only works for Paete today
   (`Editor/MapKit/PaeteAbilityFilm.Intro.cs`): generalise it to take a hero id as the first step of batch 1. A tag
   beginning `wall` adds walls and a roof; add what each hero needs (other players for a hit, the can).
4. **Weight.** His verdict on a cut that squeezed new set pieces into the old length: *"it looks like its sped up. theres
   not much weight to the timing of things"*. He chose to lengthen. Give arrivals and travel the time; keep blows fast.
   Paete's clock is an uneven table, not an even stretch (`PaeteClockAt`/`PaeteRealAt`).
5. **Sound, by his ear.** Three whole-cutscene soundtracks from real CC0 recordings, each muxed onto the CURRENT film
   (`tools/build_paete_ult_sfx.py`, `tools/mux_paete_ult_video.py`; make a builder per hero in that style). He picks, asks
   for changes, then says install. Synthesised sounds are not wanted. Mux on the newest picture every time.
6. **Then the details he will find in Play:** things hidden behind the hero's own body, effects centred on a spot nobody
   stands on, a glow copy a frame behind its body, grown or attached pieces that miss the hero's glow, poses that read
   wrong from behind. Look for these yourself, from the real cameras, before he does.

## Hard rules that cost time when broken

- **Length is network timing.** Every peer holds the match for the longest accepted performance. Changing any hero's
  length needs `UltimatePerformance.MaxSeconds` (9.0 now) to cover it and a `NetSession.ProtocolVersion` bump (155 now).
  Ask him before any length change: it is a rules question.
- **The table is data.** Body keys, lift and shots are in `tools/author_ultimate_intros.py`; never edit
  `Resources/UltimateIntros/*.txt`. Regenerating rewrites every hero's file: `git checkout` the ones you did not mean to
  change. The film must `AssetDatabase.Refresh()` before reading it (it does now).
- **The world is paused in a cutscene** (`Time.timeScale` 0). Everything is posed from the scene clock; nothing on
  `Update` or `Time.deltaTime`. An effect that must run on its own is stepped by the stage (`PaeteFx.StepStage`).
- **The body is posed and rendered by hand.** Any second skinned copy of it (a glow shell) needs
  `forceMatrixRecalculationPerRender`, or it draws a pose behind.
- **Each shot is judged for a clear line at its END** and may be mirrored or pushed in (`UltimatePhaseView.ChooseShot`).
  Keep anything that must stay in frame on the line through the hero; a shot that goes somewhere odd (underground) must
  end back in the open.
- **Shader globals outlive what set them.** `NearFade.OpenSky` closes itself when not re-asked each frame; copy that
  pattern for any global a cutscene sets.
- **Play resumes from the last pose.** The live ability's clip starts from it, and that clip is BAKED: change the table,
  then `bake <tag>`.
- **Enclosed maps.** Anything far off or in the sky is hidden by walls. Paete's opens a window through the map with the
  map's own dither (`NearFade.OpenSky`); it reaches only surfaces on `TumbangPreso/NearFade`. He refused the version that
  moved the far thing close (*"its weird that shes so close"*).

## Working with the owner (from this session)

- His editor is open on this worktree and he plays in it. Every .cs save compiles live and a save during Play can reload
  scripts mid-match. Compile outside Unity first (`cc_rt.sh`, `cc_ed.sh` in the system temp folder; `exit 0` is clean) and
  keep saves few. Never run or kill Unity. Films only run outside Play.
- Show a picture or a video the moment a piece exists. Say plainly what has not been seen or heard.
- Short feedback means a small change to the thing he names. When he circles something, ZOOM IN on that before changing
  anything: this session guessed wrong twice at "a mismatch" (the sky, then brightness) before finding the glow copy was
  offset from the body.
- Rules, lengths and gameplay get a question first. Look and motion just get done.
- Use background agents for separate pieces with separate files; check their films and compile yourself. Agents skip
  checks unless told to list what they did not verify.
- Nothing is committed (about 700 changed paths on `QoLUpdates`). Do not commit or push until he asks; then ask what to
  include, and never include his own files (listed in section 15.11's prompt).

## What is still open on Paete (do not lose these)

- NOT SEEN IN A REAL MATCH BY ANYONE: the sky opening on a shipped map, the underground shot under a real court and
  mirrored, the planted arms and their glow in first person, the vine bones' live motion (amplitudes are first guesses),
  the tree facing its caster at the hand-back, his straightened kneeling arms meeting the court.
- Sound: the cutscene plays `ult_b3`. BAKYA BLOOM's sounds are drafted, not installed (he liked a and c, "too harsh";
  a softer blend `d` was being made); two of its moments have no cue name in the game yet. Thorn Harvest and the live
  tree are silent.
- Body-clip passes for Bakya Bloom and Thorn Harvest were never started.

## Prompt for the new session

```
Read AGENTS.md, docs/HERO_KIT_METHOD.md, docs/SKILL_NETWORK_CONTRACT.md, then docs/CUTSCENE_REWORK_HANDOFF.md in full,
then docs/CHARACTER_REDESIGN_DANTE.md section 15.11 to its end (Paete's cutscene, the worked example).

Job: rework the other eight heroes' ultimate cutscenes the way Paete's was, in batches of three. Start by proposing the
batch order from the handoff and asking me to confirm it. For batch 1, first make the cutscene film work for any hero.
Then, per hero: read what I asked for before, pitch me two or three staging ideas in plain words, and build only what I
pick. Show me a film the moment a piece exists. Sounds are real CC0 recordings picked by my ear from a video.

Rules: my editor is open on this worktree and I play in it, so compile outside Unity before every save and keep saves
few; never run or kill Unity. Ask before any change to a cutscene's length (it is network timing) or to rules. No em
dashes. Do not commit or push until I ask. Say plainly what you have not seen or heard.
```
