# Map intro films: method and quality bars

Owner brief (2026-10-22, TODO `TOURNAMENT-FEEDBACK-1022`, "Map intro films"): every map in the pool opens a
match with its own short film, each with its own mood, built to `Runtime/Map/ArenaIntro.cs`'s quality. Owner,
2026-10-23: *"make everyone as expressive as possible and show emotions excited sad etc and laughing at taya"*,
*"make showing emotion and expressiveness and readability one of the main bars"*, *"thoroughly direct everything"*,
*"focus on one by one doing it and refining"*.

Status: Tondo (`TondoFilm`), Ermita (`ErmitaFilm`) and Sitangkai (`SitangkaiFilm`) are built. BGC and Banaue are
not; their maps still play the arrival's own shots until a director is added (see "Adding a map" below).

## The bars, in order

1. **Emotion and readability.** The cast has flat faces, so feeling lives in the body: lean, head, arms, hops.
   Push every pose past what feels like enough in a still; give it an anticipation, a peak and a settle in time.
   Every beat must read from the wide shot. Who is who (seat-colour chips), who is taya (the TAYA plate in the
   Defense colour), and what each person feels (acting) must all read without the score and without words.
2. **Direction.** One idea per shot, written down first as a beat table (seconds, full / short), with the cut
   points and the camera move for each. Cuts take a breath of ink (`Cut`). No shot of an empty frame: if a
   subject has left, cut. No body in front of the subject: frame with `Framed`, which scores views by how much
   other bodies cover the subject on screen.
3. **The map's own place and mood.** Use what the map already has (its consist, its lots, its chalk, its
   houses, its sidewalk people) before building anything, and build what you add in the world's blocky toon look.
   The light is a mood (`Mood` + `Tint`), not a filter: sky, sun, ambient, fog, lamps and grade together.
4. **Sound on the picture.** Each film's score is synthesised to its beat table (`tools/synth_map_film_sfx.py`),
   and every hit lands on a cut or an action. Recorded cues already in the project may be used (the kid's
   "Taya!" in Tondo is `SidewalkLife.KidTaya`); never synthesise a human voice.
5. **The contract.** Full and short (reduced motion) cuts, attackers close on YOU, the taya's and a watcher's
   screens hold the taya for the same length, a skip, everything on the shared clock, spectators included, and
   everything put back by `End`. All of this is in the base: a director cannot forget it.

## The code

- `Map/MatchOpening.cs`: what the arrival calls (`IMatchOpening`): the Arena's `ArenaIntro` (wrapped, never edited),
  or this map's `MapFilm`, or null. `MatchOpening.HidesUi` is read by `TumpMatchReadout` and `SpectatorCutscene`.
- `Map/MapFilm.cs`: the contract and the clock. `Age` is a pure function of `SharedUltimatePhase.Now - Began`;
  `HeldAge` and `Seek` exist for probes and the skip. `Ensure(mapId)` picks the director.
- `MapFilm.Cast.cs`: the players' models (`Place`, `Walk`, `Home`, `Animate`, `Greet`, `Ground`, `ShowSeat`,
  `ClearStart`, `Floor`), owned things (`Own`, `Hide`, `Ghost` for a drawn-only copy of a scene object), extras
  (`Person` builds a roster rig the way `SidewalkLife` does, clips sampled by film time), props (`Prop` always goes
  through `ToonSkin`: a plain URP/Lit material draws magenta here), glows (`Glow`, `Billboard`), lamps (`Lamp`).
- `MapFilm.Acting.cs`: the acting layer. `Act(seat, Feel)` after `Animate`. Vocabulary: `HandsIn`, `Flinch`,
  `Shock`, `Slump`, `Cheer`, `LaughAt`, `DoubledOver`, `FistPump`, `Defiant`, `HeadShake`, `Glare`, `ArmsCrossed`,
  blended with `Feel.Lerp` and shaped in time with `Hit`, `Smooth`, `Span`, `Back`. Arms are aimed in world space
  (`AimArm`), so a feeling reads the same on every rig.
- `MapFilm.Picture.cs`: `Shoot(eye, aim, fov, roll)`, `Punch`, `ClearEye`, `FrameSeat` / `Framed`, the close, the
  return home, and the light (`Mood`, `Tint`, all snapshotted and restored).
- `MapFilm.Overlay.cs`: the film's own canvas (order 151): letterbox, title (`TitleAt`), seat chips (`ChipAt`,
  `ChipRoles` hides the role pictogram until the taya is revealed), the TAYA plate (`TayaAt`), big words
  (`WordAt`), the close caption (YOU or the seat, a role band), the skip ring, `Flash`, `Darken`. All posed from
  `Age`, never a timer; text outlined in `UiTheme.InGameOutline`.
- `MapFilm.Sound.cs`: `Score` (Resources/Sfx), `Cue`, `ScoreLevel`; `Heard` reports every cue to a recording.
- `CharacterNameplate`: the ground rings are down through any map film (they would sit on the marks while the
  models walk elsewhere, and colour the taya before the reveal).

## The loop (one film at a time)

1. Survey the map (spawns, can, lights, the named props, the scene's own components; render a few reference
   views). Every map so far shares the court layout: the can at the origin, the taya 2.5 m south facing north, the
   three attackers 9 m north at x = -1.8, 0, 1.8 facing south.
2. Write the beat table in the director's header, then the director.
3. Synthesise its sound to the same table.
4. Run only this film's tests, by exact name (`MapFilmTests.<Map>FullFilmBeats`, `...ShortFilmBeats`,
   `...TayaAndWatcherHoldTheTaya`, `...PlaysIntoTheRound`, `...Recording`):
   `python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile compile-check-1022 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.MapFilmTests.<Name>;..." -testResults Logs/<n>.xml -logFile Logs/<n>.log`
   with `bash Logs/handoff-drafts-1022/watchdog.sh Logs/<n>.log` beside it. Never edit `Assets/` while it runs;
   draft the next change in a scratch folder and copy it in after.
5. Look at every beat frame (Logs/map-films/<map>/*.png) as a director: is the feeling readable from here, is
   anyone in front of the subject, is the frame empty, does the light say the mood. Write the notes, fix, rerun.
6. Render the recording: `py -3 tools/render_map_film.py <MapId>` (frames at 24 a second on the film's own clock,
   the score and every cue mixed at their film times) and watch it at normal speed.

## Traps already paid for

- Capture after LateUpdate (`LateFrameShot` / `FilmFrames`): the film poses after the animator.
- `WaitForEndOfFrame` hangs in batch mode; URP never fires `Camera.onPreRender`.
- Overlay canvases are not in a camera render: `FilmFrames` puts only the film's own canvas on an ungraded UI camera.
- The tests disable the `ReadyGate` and mark an arrival active (`ArrivalActive`): otherwise the round counts in
  behind the film and the nameplates show.
- A spot light at intensity 9 bleaches a toon body to a pale ghost (owner: "is this a bug"); keep film lamps at 2 to 4.
- Choose a framing once per shot (`Framed`), never every frame: the best view hops as bodies move.
- Under a held clock, animate by the film's own time (`FrameDelta`), not the wall clock.
- The chips' role pictogram and the nameplate rings reveal the taya; hide them until the film reveals it.
- A static-batched map (the cove) cannot be copied: in play a renderer's `sharedMesh` is the whole batch (Sitangkai
  r1 to r3 "copied" 54 m and then 227 m of boats). Build the prop in the toon kit (`Prop`) instead.
- Find the ground with the map's own ground collider, not "the highest floor": a palm crown is a floor to a ray.
- Extras need the acting layer too (`Act(Extra, Feel)`, built with `Facing(extra)`/`Side(extra)`). Chibi arms are
  short: "arms up" stays inside the head's silhouette; spread them into a wide V.
- `FrameSeat`/`Framed` score only the players: an extra (the lola) can stand across the subject. Frame by hand when one
  is near.
- A point read from behind the pointer is hidden by their own head; shoot it front three-quarter, the arm crossing the
  frame toward the target. A doubled-over laugh and a point at the lens both read as blobs; shoot laughs three-quarter.
- A hero's pet follows the body, not the posed model, and switches its own renderers on: `ForceOff` holds it off.

## Adding a map

Write `<Name>Film : MapFilm` (Region, Accent, Score, CloseAt, Prepare, Stage, optionally TayaShot and Restore),
add it to `MapFilm.Ensure`, add its sound to `tools/synth_map_film_sfx.py` and its score to
`tools/render_map_film.py`'s table, add its tests to `MapFilmTests`, and record the map in TODO.

The owner's brief for the remaining two:
- BGC (Kanto), city adrenaline: fast-cut traffic time-lapse, arrivals by jeepney, bike and on foot, a tower billboard
  shows the VS cards, a traffic light turning green means go. (`KantoTraffic.Drivers`, `LaneFrame`, `Jeepney`,
  `Signals` with the `signal_red/amber/green` lamp slots; it is frozen under the hold, so pose a time-lapse from
  `LaneFrame` and put every body back.)
- Banaue (Payo), mythic and ceremonial: mist over the terraces at dawn, gong beats as the camera climbs, torches
  light the ring, a carved stone glyph glows to reveal the taya. (`PayoFire`, `PayoHaze`, `PayoNight`; the plaza's
  sun disc at the origin.)
