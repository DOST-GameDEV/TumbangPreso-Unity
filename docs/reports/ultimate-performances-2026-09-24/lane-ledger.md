# Animation lane ledger (cloud session, 2026-09-24): owner instructions and plan

Resume point for this lane. Read this first after any compaction, then `progress.md`,
`plan.md`, `research.md` in this folder. The repository-wide rules (AGENTS.md, CLAUDE.md,
docs/TODO.md, docs/ACTIVE_REWORK_LEDGER.md) still apply.

## Where the work is

- Branch **`claude/animation-ultimates`** (owner: "commit all ur shit to ur own branch dont make
  many diff"), one draft PR into ASTRAReworks:
  https://github.com/DOST-GameDEV/TumbangPreso-Unity/pull/5 . Subscribed to its activity.
- Commits: sole author M4tyu633, `git commit -F`, no attribution trailers, no AI mentions, no em
  dashes. Rebase on origin/ASTRAReworks before pushing if it moved.
- No Unity licence on this cloud machine. Checks here: Roslyn compile script (scratchpad
  `rc/run.sh`, recreate if lost: Runtime + package sources against /opt/tump/unity DLLs) and the
  pose/stage sheets (`tools/author_ultimate_intros.py --preview <hero>`). Native checks owed to
  Windows (list in progress.md). Never paste or use credentials from chat (the auto-mode
  classifier blocks it; do not work around).

## Owner instructions, in order received (verbatim where quoted)

1. The assignment: "make animations better". Animation director for ALL animation, emphasis on
   distinctive ability casts and spectacular, expressive ultimates. Research real games (Roblox,
   fighting/action, expressive sports games; Sepak U and Blue Lock as interests, not templates),
   record specific moments and why they work. Review the entire cast in FPP and TPP (walk, run,
   strafe, starts/stops/turns, carry, throws, left/right pektus, retrieve, can raise, reactions,
   interruptions, recovery, climbing, interactions, acting, celebrations, ambient/presentation).
   Keep successful existing work; judge each character and action individually. Every ability an
   authored performance (prep, cast, action, follow-through, recovery), different gestures,
   silhouettes, paths, timing, impact language; FPP hands, TPP body, effect origin and sound fit
   together. Ultimates: each hero's stage briefly theirs; emotion, signature pose, shapes,
   materials, colours, camera, sound, voice; caster/others/spectator views; truthful handoff.
   Phaister laughing and levitating explicitly. 2.8 s is not a cap. Build on the real shared
   phase; reduced-motion version too. Publish reviewable commits and a PR; distinguish implemented,
   reviewed and unverified work.
2. "i want u to do each animation one by one and dont js spam copy paste stuff bcz it will be boring"
3. "can u give them all their own voice lines too or with actual audio and connect it to their
   story and personality", "make sure theres voicelines wherein they interact with each other and
   voicelines related to skills and yk js voicelines overall, use valorant as reference for when
   voiceliens are used or how theyre constructed, research other games too" (TODO VOICE-1)
4. "can u also optimize and clean up Claude.md without removing any important itstructions bcz
   its so long"
5. "js lock in ... try to get as much work as done as possible ... and js publish in a branch"
6. "KEEP GOING PLEASE! THOROUGHLY REFINE WHAT UVE DONE ALREADY I BET IT SUCKS"
7. "REFINE ANY OTHER ANIMATION THAT CAN BE REFINED/ STILL SUCKS PLEASE!"
8. "when you are done with everything (DO NOT DECIDE UR DONE WITH EVERYTHING BY JS BELIEVING U
   ARE, ACTUALLY MAKE EVERYTHING I ASKED U TO BE QUALITY) WHEN YOU ARE DONE WITH EEVRYTHING GIVE ME
   A HANDOFF PROMPT TO TEST AND REFINE UR WORK, it will run locally"
9. "refine all their skills VFX SFX and animation and everything too THOROUGHLY research how other
   games that are good in roblox and or actual games like valorant or overwatch make skill effects
   and try to author one that works in our world" (TODO SKILL-FX-1)
10. "i really want phaister's ult to be improve its cast/animation is awkward and ugly and circle
    doesnt look that amazing its js a purple empty circle", "thoroughly plan how to make each skill
    WAY better". (This is the LIVE Grand Coven: its cast clip and the ritual circle in play, not
    only the new introduction.)
11. "pls put all instructions i give as well as ur plan in a ledger incase compaction triggers"
    (this file)
12. "also create the ui and code for skill tree, ur supposed to unlock the other skills as u play the
    character more but for now keep it all unlocked and make it easy to lock again (keeping it all
    unlocked for testing)" (TODO SKILL-TREE-1; research the existing variant/loadout/unlock code
    first: `SettingsStore.NoteAbilityCast`, `EffectiveCastCue` alternates, the hub loadout screens)

## Plan and order

1. DONE: research (research.md), per-hero plan and durations (plan.md), shared plumbing, all seven
   new introductions (Phaister, Sean, Zack, Nemu, Dante, Cheska, Rafi), refinement pass 1 (stage
   sketch fixes). Evidence in progress.md.
2. NEXT: skill research (Valorant, Overwatch, Roblox battlegrounds; Riot VFX principles already
   in reports/visual-research-2026-09-23/findings.md) and a THOROUGH per-skill plan for all
   21 skills plus each ultimate's live part: `docs/reports/skill-performances-2026-09-24/plan.md`.
3. DONE for Grand Coven (cast clip, first person, circle; see skill-performances progress.md).
   Then implement, Phaister's LIVE Grand Coven first (owner complaint 10): its cast body clip
   (glb table in tools/author_hero_action.py, `hero-phaister-eclipse`), first-person gesture
   (`ViewmodelArms.CastGesture.cs`), the ritual circle (`HeroHazards.SpawnGrandCovenEclipse`,
   `CovenCircleBuild`), sound. Then every other skill one at a time, each with its own gesture,
   silhouette, VFX shape and sound.
4. Refinement pass 2 on the introductions: moving holds (no dead-still poses), overlapping action
   (head and arms lagging the torso), timing polish per hero.
5. Other animation review (instruction 7): hero cast clips and code-driven verbs (ThrowBody,
   TagBody, ResetRaise, locomotion) by pose sheets; fix what reads badly.
6. VOICE-1: research Valorant voice design and others; decide the audio route honestly (TTS
   engine if one installs at acceptable quality, else stylised voices from the formant synth);
   author lines per hero (skills, ultimates, interactions, match moments).
7. CLAUDE.md condensation (instruction 4): keep every rule, compress narratives, archive the
   original whole under docs/archive/, keep section numbers others point at.
8. Final: update TODO/ledger/progress, push, then the handoff prompt for local testing and
   refinement (instruction 8), including the exact native checks owed.
