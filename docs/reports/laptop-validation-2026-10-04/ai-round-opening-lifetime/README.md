# Bot round opening lifetime

Inactive bots return from Update before StepHeroAbilities, so its inactive-round
reset cannot run. OnRoundStarted also left the previous opening clock and pending
decision intact. After a completed round, a bot could enter the next one with its
authored opening delay already satisfied and an old Skill1 decision still held.

The fix resets the opening clock and forgets that decision in the subscribed
round-start handler. It preserves authored kit behavior, opening/jitter constants,
cast cadence, ultimate timing, hero input and finalized artwork.

Unity 6000.5.8f1 on laptop gamergmae ran the same five EditMode cases directly in
the independent qa-a worker. Original: four causal failures and one control pass;
candidate: five passes, zero skips. Cases drive real MatchDirector.AdvanceRound
events and the actual inactive AI Update path; temporary human ownership retains
its held hero key. Same-round inactivity preserves opening/cadence state.

Both runs have 3460 frozen inputs; maps differ only in AIController.cs. Runtime,
fixture and meta stayed unchanged during each run. Both generated one identical
ProjectAuditor settings rewrite, separately recorded. QualitySettings was restored
byte exactly and nine existing product EditorPrefs restored. Original PID29204
exited2; candidate PID19636 exited0. No memory admission or external timeout wrapper.

This qualifies the private clock/decision lifecycle through native Unity events;
it does not establish rendered cast behavior, all hero/map combinations, physical
input or full tournament acceptance. The transferred 91aad release predates this
fix. PRACTICE-BOT-RESUME-1002 operator acceptance remains open.
