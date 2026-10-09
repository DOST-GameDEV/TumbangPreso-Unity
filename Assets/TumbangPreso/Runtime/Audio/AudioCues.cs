using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Audio
{
    /// <summary>
    /// Every sound the game can make, its mix level, and what it actually resolves to on disk.
    ///
    /// ⚠️ TRANSCRIBED FROM audio_manager.gd, INCLUDING THE MIX LEVELS. The dB values are not
    /// taste: the SFX bus was measured CLIPPING at +2.0 dBFS with music silent, and these trims
    /// are what answered it. A cue restored to 0 dB because it "sounded quiet in isolation" is
    /// how that bug comes back, and it only reappears in a real match where impacts, the tag,
    /// voice and the music bed all land at once.
    /// </summary>
    public static class AudioCues
    {
        /// <summary>
        /// ⚠️ SIX CUE NAMES ARE ALIASES, NOT MISSING FILES. The call sites in the gameplay code
        /// already used these names before the sounds existed, so rather than rename call sites
        /// across several files (or synthesise six near-duplicate wavs), the names resolve to
        /// the real file. Anything that checks "does every cue have a file" MUST resolve
        /// aliases first or it reports six false orphans.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Aliases =
            new Dictionary<string, string>
            {
                { "hit_body",       "bump" },
                { "can_knockdown",  "lata_knockdown" },
                { "reset_complete", "reset_channel_complete" },
                { "pickup",         "grab" },
                { "throw_release",  "throw_whoosh" },
                { "court_escape",   "throw_whoosh" },
                { "court_skid",     "slide_scrape" },
            };

        /// <summary>
        /// Per-cue trim in dB, 0 where unlisted.
        ///
        /// ⚠️ THE TWO EXTREMES ARE BOTH DELIBERATE AND BOTH ANNOTATED IN THE SOURCE.
        /// `ui_hover` sits at -8 because it fires on every mouse movement across a menu, and
        /// `land` at -6 because it fires constantly and has to sit under everything. A sound
        /// that plays continuously is mixed for the hundredth time it is heard, not the first.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, float> TrimDb =
            new Dictionary<string, float>
            {
                // ⚠⚠ THE THREE ZONE ENDINGS SIT LOWEST IN THE WHOLE MIX, AND THAT IS THE
                // POINT OF THEM. An expiry is INFORMATION, not an event: it has to be audible to
                // somebody who has been watching that patch of ground and must never compete
                // with whatever is being cast at the same moment. A cast and its own ending at
                // the same trim would make a spent zone sound like a second ability.

                // The three that replaced borrowed cues keep the weight of what they are: a
                // wall landing and a wall failing are both events a player has to act on, and
                // the sheet spreading is the quietest of the three because it is the least
                // urgent thing to know about.
                // A bell is the one cue in the game that is meant to be HEARD OVER the fight
                // rather than located in it, and it is also the longest at 2.2 s. Lower than the
                // other casts so a 2 s tail does not sit on top of everything that follows it.

                // The affliction fires per victim per hex, so it is mixed as a status rather
                // than as an event: four people standing in one circle must not stack into a wall.

                // ⚠️⚠️ THE TRAIN IS ONE CUE NOW AND IT IS A REAL RECORDING, NOT A SYNTH.
                // 🧑 2026-08-27: *"i keep reporting its broken and i give up on it ... replace
                // train passing by sound and train sound as a whole with this"*, with a 10.55 s
                // field recording. Both synthesised cues are gone: the distant one-shot warning
                // AND the looping bed. One sound, on the moving source, for the whole pass.
                //
                // ⚠️ IT KEEPS THE BED'S MIX ROW RATHER THAN THE ONE-SHOT'S. It is 10.55 s of
                // sustained noise, which is the shape the -16 was measured for: the bed shipped
                // with NO row, fell back to 0.0 dB, and was reported off the build as *"a loud
                // wind soudn that plays randomly"*. A long sound is mixed as a background
                // whatever it is called.
                //
                // ⚠️ -8, UP FROM -16 (2026-10-01). 🧑, on the rebuilt Ilalim: *"does the train/lrt
                // move and have a loud LRT sfx?"*, then "proceed" on making it louder. The -16 was
                // set while the clip still played for the whole traverse from 70 m; the source now
                // reaches only 44 m (`LrtTrainFlyby.RumbleMaxDistance`), so it arrives with the
                // warning toast and leaves with the consist, and a louder pass no longer reads as
                // random wind. The deck hides the consist from every place a player can stand,
                // so this recording and the shake ARE the train.
                //
                // ⚠️ 0, UP AGAIN (2026-10-04): "the actual train sfx still isnt audible". Since the
                // -8, the pass moved to the Ambience slider times `AmbientGainScale` 0.75, and the
                // recording is quiet in itself (RMS 0.10), so -8 came out near 0.2 of full scale.
                { "sfx_lrt_pass", 0.0f },

                // ⚠️⚠️ THE SIX ULTIMATE THEMES ARE BEDS, NOT EVENTS, AND ARE MIXED AS BEDS.
                // The train row above records what happens to a sustained cue with no row:
                // `TrimDb` falls back to 0.0, which is correct for a one-shot and completely
                // wrong for a three-second sustain. These run UNDER a payload that is already
                // the loudest thing in the game, so anything above about -14 turns the moment
                // an ultimate lands into mush.
                //
                // ⚠️ PHAISTER'S SITS 2 dB HOTTER THAN THE OTHER FIVE, ON PURPOSE. Hers plays
                // during the BUILD, before the payload arrives, so for its first second and a
                // half it is not competing with anything: it is the only thing saying an
                // ultimate is coming. The other five start on the same frame as their blast.
                { "sfx_step_deck", -8.0f }, { "sfx_swim_stroke", -9.0f }, { "sfx_lagoon_lap", -12.0f },
                // The Arena's show (owner 2026-10-05, `tools/synth_arena_show_sfx.py`). The reveal's boom is the
                // loudest moment of the break and the lock is its percussion, so those two are mixed as events.
                // The alarm is 2.3 s of sustained tone and the roar 3.6 s of crowd: beds, mixed as beds (the
                // train's row above records what a sustained cue with no trim does). The thruster fires once
                // per moving platform inside a quarter second, so it is mixed like a status.
                { "sfx_arena_alarm", -9.0f }, { "sfx_arena_undock", -4.0f }, { "sfx_arena_thruster", -11.0f },
                { "sfx_arena_lock", -3.0f }, { "sfx_arena_reveal", -2.0f }, { "sfx_arena_crowd_roar", -10.0f },
                { "sfx_arena_pyro", -7.0f }, { "sfx_arena_drone_ping", -7.0f }, { "sfx_arena_drone_set", -5.0f },
                // The stage's furniture (owner 2026-10-05, `tools/synth_arena_pad_sfx.py`): events a player causes, mixed as events.
                { "sfx_arena_pad_jump", 0.0f }, { "sfx_arena_pad_speed", 0.0f }, { "sfx_arena_boost", -1.0f }, { "sfx_arena_stamina", 0.0f },
                // Paete's LIANA LEAP, reworked from real recordings (owner 2026-10-07, `tools/install_paete_skill_sfx.py`): a cast
                // a player makes, mixed as an event.
                { "sfx_cast_paete_vine", -1.0f }, { "sfx_paete_vine_catch", -1.0f }, { "sfx_paete_vine_land", -2.0f },
                // Paete's ultimate cutscene, one 9.0 s track of real recordings (owner 2026-10-08, `tools/build_paete_ult_sfx.py` b).
                { "sfx_ult_theme_paete", 0.0f },
                // The rescue drone's toy voice (2026-10-05). Its hum is struck again every half second of a
                // carry and up to four carries can run at once, so it is mixed as a bed; the rising whistle of
                // the haul and the zip away are one each per carry and sit under the lock-on and the ding.
                { "sfx_arena_drone_hum", -14.0f }, { "sfx_arena_drone_beam", -9.0f }, { "sfx_arena_drone_zip", -9.0f },
                // The Arena's slipper balloon (`tools/synth_arena_balloon_sfx.py`). The pop is a once-a-match
                // event and is mixed as one; a squeak and the boing sound together on a hit, so each sits under
                // an event; the creak and the hiss are beds.
                { "sfx_arena_balloon_fly", -8.0f }, { "sfx_arena_balloon_squeak_a", -6.0f }, { "sfx_arena_balloon_squeak_b", -6.0f },
                { "sfx_arena_balloon_squeak_c", -6.0f }, { "sfx_arena_balloon_boing", -5.0f }, { "sfx_arena_balloon_creak", -10.0f },
                { "sfx_arena_balloon_pop", -1.0f }, { "sfx_arena_balloon_hiss", -8.0f }, { "sfx_arena_slipper_return", -7.0f },
                // The Arena's crowd and its public address (owner 2026-10-05: "there should be reverbey crowd cheers,
                // chants, and an announcer"). `Map.ArenaCrowdAudio` plays every one on its own sources: the crowd on the
                // Ambience slider, the PA's stings on the Announcer's.
                //
                // ⚠️ THE CROWD IS REAL RECORDINGS SINCE 2026-10-05 (`tools/build_arena_crowd_from_recordings.py`, sources in
                // `Resources/Sfx/ARENA_CROWD_SOURCES.md`), AND THESE ROWS WERE RESET WITH IT. The owner on the synthesised
                // crowd: "i dont hear ... reactions to certain happenings in the game". Measured against these rows, that
                // was the mix: a block's cheer and a throw's gasp played 10 to 16 dB under a murmur at -3. So the murmur is
                // at -6 and gives way further as the stands rise, and a reaction sits 0 to 4 dB under an event.
                // The five beds are loops levelled by RMS (0.13 to 0.17), so these rows ARE their loudness. A reaction is
                // levelled by the RMS of its loudest 0.4 s (0.20 to 0.27; peak under 0.85), not by its peak: a roar and a
                // clap of the same peak are 10 dB apart to an ear. A reaction's takes (`_2`, `_3`) share its row's value.
                // NOBODY HAS HEARD THESE YET: they are the knobs for that session.
                // The resting bed far under the reactions (owner, 2026-10-05: "the crowd doesnt react.. its just the same cheer ambience all
                // throughout"). The real cause was the layers' mix (`ArenaCrowdAudio.Ramp`): the roar never fell under half. With that mended the calm bed is 10 dB under an eruption.
                { "sfx_arena_crowd_bed_calm", -10.0f }, { "sfx_arena_crowd_bed_lively", -5.0f }, { "sfx_arena_crowd_bed_roar", -1.0f },
                { "sfx_arena_crowd_bed_tension", -5.0f }, { "sfx_arena_crowd_bed_applause", -3.0f },
                { "sfx_arena_crowd_erupt", 0.0f }, { "sfx_arena_crowd_erupt_2", 0.0f }, { "sfx_arena_crowd_erupt_3", 0.0f },
                { "sfx_arena_crowd_cheer", -1.0f }, { "sfx_arena_crowd_cheer_2", -1.0f }, { "sfx_arena_crowd_cheer_3", -1.0f },
                { "sfx_arena_crowd_ooh", -1.0f }, { "sfx_arena_crowd_ooh_2", -1.0f }, { "sfx_arena_crowd_ooh_3", -1.0f },
                { "sfx_arena_crowd_aww", -2.0f }, { "sfx_arena_crowd_aww_2", -2.0f }, { "sfx_arena_crowd_aww_3", -2.0f },
                { "sfx_arena_crowd_gasp", -4.0f }, { "sfx_arena_crowd_gasp_2", -4.0f },
                { "sfx_arena_crowd_laugh", -2.0f }, { "sfx_arena_crowd_laugh_2", -2.0f }, { "sfx_arena_crowd_save", 0.0f },
                { "sfx_arena_chant_stomp", -3.0f }, { "sfx_arena_chant_claps", -3.0f }, { "sfx_arena_chant_claps_fast", -3.0f },
                { "sfx_arena_chant_ooh_hey", -3.0f }, { "sfx_arena_chant_drums", -3.0f }, { "sfx_arena_chant_horns", -5.0f },
                { "sfx_arena_pa_chime", -5.0f }, { "sfx_arena_pa_organ", -6.0f }, { "sfx_arena_pa_horn", -5.0f }, { "sfx_arena_pa_fanfare", -5.0f },
                // Amihan (2026-09-25, `tools/build_amihan_audio.py`). The gather is a sustained
                // pressure rise under 2.5 s of telegraph and is mixed as a bed; the release is the
                // loudest moment of her kit and is mixed as an ultimate payload.
                // The two new statuses sound on the victim, mixed like `sfx_hex_afflict`: several
                // can land inside a second.
                // Paete (HERO-9, `tools/build_paete_audio.py`). The sentry burst is his biggest
                // moment and is mixed as an ultimate payload; the command sits under the seedling's
                // own fire; the roots on a victim are a status, three can land inside a second.
                // v5 (direction.md 5.14): each haul of the guardian crawling out of the court, three per cast, under the burst.
                // The roster rework (ABILITY-2, `tools/build_rework_audio.py`): one recipe per ability and status.
                // PHAISTER, THE WITCH (HERO-10, 2026-09-27, `tools/build_phaister_audio.py`): the swarm's arrival, the doll's
                // three fates, the moonlight's two ends.
                // v3, the voodoo kit (plan 9.5): the two locks, the mark and the snap, DRAIN's wring, HEX's stab; the two
                // statuses sound on the victim like the others.

                // ⚠️ THE BREAK IS MIXED LIKE A STATUS, NOT AN EVENT, for the same reason
                // `sfx_hex_afflict` is: this is a 1-vs-3 game and a nova can hold three people
                // at once, so three breaks can land inside a second. The arrival sits with the
                // casts because there is only ever one of her.
                // ⚠️⚠️ THE WEATHER BEDS ARE THE QUIETEST THINGS IN THE GAME AND THAT IS WHAT
                // "UNDER" MEANS. Each runs for two to three seconds under a payload cue, a hero
                // voice and whatever the fight is already doing; the LRT bed's own row records
                // what happens to a sustained sound with no trim, which is that it *"arrived
                // louder than every ability payload in the game"* and was reported as a loud
                // random noise. The storm is the loudest of the six because a thunder crack is
                // an event rather than a bed, and the seance is the quietest because a seance
                // that announced itself would be somebody else's ultimate.
                // The maw is an event and carries a full mix; the flight home is punctuation on
                // an animation the player is already watching, so it sits well under it.


                { "sfx_stun_break",  -9.0f },


                { "ui_hover",       -8.0f },
                // ⚠️ THREE OF THE FOUR NEW CUES FIRE WHILE SOMEBODY IS TYPING, so they sit at
                // or under the hover, not at the click. A validation chime mixed like an event
                // is what makes a player turn menu sound off. Only `ui_start` is an event: it
                // is the title screen letting go, and nothing else is happening on that frame.
                { "ui_tick",       -13.0f },
                { "ui_valid",      -12.0f },
                { "ui_toggle",      -9.0f },
                { "ui_start",       -3.0f },
                { "land",           -6.0f },
                { "step_rubber",   -11.0f },
                // Amihan's own light step (owner 2026-10-02, `tools/build_amihan_steps.py`): quieter than the rubber slap.
                { "step_amihan",   -14.0f },
                // ⚠️ Amihan's reworked skill sounds (her Airburst theme and release, Featherfall's three) were taken out again on the
                // owner's instruction (2026-10-03: *"i meant remove all sfx"*). `tools/build_amihan_ult_audio.py` keeps the recipes.
                { "slide_scrape",   -6.0f },
                { "grab",           -6.0f },
                { "throw_charge",   -5.0f },
                { "slipper_bounce", -4.0f },
                { "jump",           -4.0f },
                { "throw_whoosh",   -4.0f },
                { "ui_click",       -3.0f },
                { "slipper_land",   -3.0f },
                { "dash",           -3.0f },
                { "bump_swing",     -6.0f },
                { "lata_impact",     0.0f },
                { "lata_seal",       0.0f },
                { "match_win",       0.0f },
            };

        /// <summary>
        /// The five .wav files left behind when `scripts/abilities/**` was deleted outright in
        /// the design pivot (eight verbs nobody asked for). Every caller went with it; the
        /// files did not.
        ///
        /// They are listed rather than silently dropped because they are exactly the failure
        /// this whole registry exists to catch, in its other direction: a FILE with no cue,
        /// where the usual bug is a cue with no file. `slipper_land` shipped registered,
        /// mixed and completely silent for weeks because nothing ever called it, and it was
        /// the single most common outcome in the game, 38 of 71 flights.
        ///
        /// ⚠️⚠️ THIS LIST AND `Live` NOW OVERLAP, AND THE HEADING USED TO DENY IT. It read
        /// "THESE FIVE SHIP AND CAN NEVER PLAY" with a "DO NOT PORT THEM AS LIVE CUES"
        /// underneath, while all five sat in `Live` a few lines below and three of them were
        /// being fired by Hero Strike every round. `docs/TESTING.md` repeated the claim.
        /// Reading either one and believing it costs a session: the obvious next move is to
        /// delete "dead" audio that the game is actually playing.
        ///
        /// ⚠️ WHAT IS TRUE AS OF 2026-08-25: Hero Strike reached for these because it had
        /// nothing else, and it now has `sfx_quake_slam`, `sfx_thunder_impact`,
        /// `sfx_frost_nova`, `sfx_possess_enter`, `sfx_possess_exit` and `sfx_slipper_burst`.
        /// `ability_bagsak_bomb` and `ability_flick_dash` are free again.
        ///
        /// ⚠️⚠️ UPDATED 2026-08-26, AND THE OLD VERSION OF THIS PARAGRAPH WAS WRONG IN A WAY
        /// WORTH RECORDING. It read that `ability_shatter_trap` *"is still live on the ice
        /// barricade, where it genuinely fits"*. It was live on the barricade AND on the ice
        /// sheet, so two different powers shared one cue, and it is the sound of something
        /// BREAKING fired at the moment something is BUILT. Both now have their own
        /// (`sfx_barricade_raise`, `sfx_ice_form`); `docs/TODO.md` § 20 has the account.
        ///
        /// ⚠️ WHAT IT STILL DOES IS THE SLIP: a player losing their footing on Cheska's sheet,
        /// which is the one use of it that was ever the right shape. So it remains a survivor
        /// rather than an orphan, on one call site instead of three, and this list is a history
        /// of where the files came from rather than a claim that none of them plays.
        /// </summary>
        public static readonly IReadOnlyList<string> DeletedAbilityCues = new[]
        {
            "ability_bagsak_bomb",
            "ability_bakya_bash",
            "ability_flick_dash",
            "ability_shatter_trap",
            "ability_spin_guard",
        };

        /// <summary>Every cue the live game can fire. Aliases included; they are real names.</summary>
        public static readonly IReadOnlyList<string> Live = new[]
        {
            // The lata, which is what the whole game is built around.
            "lata_impact", "lata_knockdown", "lata_seal",
            "reset_channel_start", "reset_channel_complete",

            // Bodies.
            "bump", "tag", "downed", "jump", "land", "dash", "guard_block", "respawn",
            "step_rubber", "step_amihan", "slide_scrape",
            "stamina_empty",

            // ⚠️⚠️ EVERY HERO SKILL SOUND IS DELETED (2026-09-29), ON THE OWNER'S INSTRUCTION. 🧑: *"also all ur skill sfx
            // suck shit what is that HAHAHA even paete's"*, *"dont put sfx for all skills for now"*, *"will rework them at a lter
            // date"*, then *"can we delete all skill abilities sfx ty haha"*. 149 files went: every cast, loadout variant, status,
            // payload, zone, ultimate theme and ultimate weather cue of every hero (the list is `IsSkillSfx`). Their call sites
            // stay and play nothing (`Audible`), so the rework registers new files under the same names and they are heard again.
            // The generators that made them (`tools/generate_*_audio.py`, `tools/build_*_audio.py`) are kept for that rework;
            // do not rerun them into `Resources/Sfx` until the owner asks for skill sounds back.
            "sfx_hitmarker", "sfx_super_ready",

            // ⚠️ THE MAP EVENT. `LrtTrainFlyby` called `ui_move` for two months and there has
            // never been a `ui_move.wav`, so every pass wrote `[Audio] no cue registered` to the
            // log and the one recurring event on Ilalim ng Tulay was silent.
            //
            // ⚠️⚠️ IT IS A FIELD RECORDING NOW AND IT IS THE ONLY TRAIN CUE. 10.55 s, long
            // enough to cover the 3.0 s warning plus the 5.33 s traverse without looping, played
            // on the source parented to the consist so the approach and the recede come off the
            // transform. See `LrtTrainFlyby` § THE PASS for why a moving emitter is the whole
            // point and why `sfx_lrt_rumble` no longer exists.
            "sfx_lrt_pass",

            // ⚠️⚠️ `sfx_lrt_rumble` IS DELETED, AND THE ENTRY ABOVE IT USED TO SAY "THE TRAIN IS
            // TWO CUES, NOT ONE". It was: a distant synthesised one-shot for the warning and a
            // seamless synthesised bed for the traverse, both built by
            // `tools/generate_ability_audio.py`. 🧑 reported the result broken repeatedly and
            // then closed it himself on 2026-08-27: *"i keep reporting its broken and i give up
            // on it ... replace train passing by sound and train sound as a whole with this"*.
            // A real recording is one sound, so splitting it across two cues would put the same
            // 10.55 s of audio on screen twice, a few tenths apart, which is a flam. The synth
            // that made both is removed from the generator so a regeneration cannot bring the
            // old pair back over the recording.

            // ⚠️ THE 2026-08-26 SPARSE PASS, AND IT IS TWO CUES BECAUSE THE BAR IS TWO CUES
            // WIDE. 🧑: *"Find where a sound is missing, but keep it sparse. The bar is a player
            // having to guess whether something happened. Nothing that already reads visually."*
            // Everything else audited either had a cue or was already unmistakable on screen.
            //
            //  * `sfx_blink_arrive`: Phaister's blink plays at the DEPARTURE only, and after the
            //    rebuild the far end can be 5.5 m away. Whoever is standing there heard nothing.
            //  * `sfx_stun_break`: `docs/TODO.md` § 23 built a whole mash-out system and ended it
            //    in silence. The pips are on a card the player is not looking at while three
            //    people run at them.
            "sfx_stun_break",

            // Hero Vocal Shouts & Grunts.
            "hero_dante_ult", "hero_dante_grunt",
            "hero_cheska_ult", "hero_cheska_grunt",
            "hero_sean_ult", "hero_sean_grunt",
            "hero_zack_ult", "hero_zack_grunt",
            "hero_nemu_ult", "hero_nemu_grunt",

            // ⚠️⚠️ THE SIXTH HERO'S OWN VOICE, AND SHE SHIPPED WITH NEMU'S. `docs/TODO.md`
            // § 21.4 left this open because the generator that makes these was believed to be
            // present and unseeded; it was in fact absent from the repository entirely, which
            // also meant `tools/generate_ability_audio.py` could not import it and would not run
            // from a clean clone. `tools/generate_hero_audio.py` now exists, is seeded per cue,
            // and refuses to overwrite a shipped file unless asked.
            //
            // ⚠️ NEMU AND PHAISTER ARE THE ONLY PAIR SHARING AN ELEMENT (§ 21.5 makes the same
            // point about her aura), so a borrowed voice blurred exactly the two characters
            // least able to afford it.
            "hero_phaister_ult", "hero_phaister_grunt",

            // The lagoon deck (Lagoon Cove): footsteps on the boards, a swimmer's stroke, the water at the piles.
            "sfx_step_deck", "sfx_swim_stroke", "sfx_lagoon_lap",

            // The Arena (ARENA-1, owner 2026-10-05: "map transformation is so dull, theres no emphasis on it",
            // "more vfx overall in the map, including the drone stuff"). The stage's transformation between
            // rounds in its three beats (the alarm, the undock, a thruster and a lock per platform, the
            // reveal), the stands' roar, the pyro, and the rescue drone's lock-on, hover, haul, set-down and exit. Every one is
            // played by each peer for itself from state it already has (`Map.ArenaShow`, `ArenaDrone`,
            // `ArenaAmbience`), never through `NetCue`: nothing about them is on the wire.
            "sfx_arena_alarm", "sfx_arena_undock", "sfx_arena_thruster", "sfx_arena_lock", "sfx_arena_reveal",
            "sfx_arena_crowd_roar", "sfx_arena_pyro", "sfx_arena_drone_ping", "sfx_arena_drone_set",
            "sfx_arena_pad_jump", "sfx_arena_pad_speed", "sfx_arena_boost", "sfx_arena_stamina",
            // Paete's reworked LIANA LEAP: the cast goes the way every cast cue goes; the catch and the landing are played
            // by each peer for itself from its own vine (`Visual.PaeteVineReach`), never through `NetCue`.
            "sfx_cast_paete_vine", "sfx_paete_vine_catch", "sfx_paete_vine_land",
            // Paete's ultimate cutscene (`HeroIntroductionScene.StartSound` plays it on the phase's own clock, never through `NetCue`).
            "sfx_ult_theme_paete",
            "sfx_arena_drone_hum", "sfx_arena_drone_beam", "sfx_arena_drone_zip",
            // The Arena's slipper balloon and a slipper set back on the stage (`Map.ArenaBalloon`,
            // `Map.ArenaFallRecovery`): each peer plays them for itself, never through `NetCue`.
            "sfx_arena_balloon_fly", "sfx_arena_balloon_squeak_a", "sfx_arena_balloon_squeak_b", "sfx_arena_balloon_squeak_c",
            "sfx_arena_balloon_boing", "sfx_arena_balloon_creak", "sfx_arena_balloon_pop", "sfx_arena_balloon_hiss",
            "sfx_arena_slipper_return",
            // The Arena's crowd and public address (`Map.ArenaCrowdAudio`): five seamless beds, six reactions in two or
            // three takes each (`_2`, `_3`: one is drawn each time), a self-save's roar, six chants and four PA stings.
            // The crowd is cut from CC0 recordings (`tools/build_arena_crowd_from_recordings.py`); the stings are
            // `tools/synth_arena_crowd_sfx.py --no-vo --only pa`. Each peer plays them for itself from events it already
            // has, never through `NetCue`. The announcer's own takes as the stadium plays them are not cues: they are
            // `Resources/ArenaPa/pa_<take>`, found by the take's name.
            // ⚠️ THREE NAMES ARE DELIBERATELY ABSENT: `sfx_arena_chant_tumbang_preso`, `sfx_arena_chant_taya` and
            // `sfx_arena_chant_tumba`. The game's own word chants cannot be cut from another crowd, and the synthesised
            // ones were rejected and deleted. `ArenaCrowdAudio.Recorded` plays a clapping or drumming pattern in their
            // place. When the team records one, put the .wav in `Resources/Sfx` and its name back in this list (and a
            // row in `TrimDb`, about -3): nothing else has to change.
            "sfx_arena_crowd_bed_calm", "sfx_arena_crowd_bed_lively", "sfx_arena_crowd_bed_roar", "sfx_arena_crowd_bed_tension",
            "sfx_arena_crowd_bed_applause",
            "sfx_arena_crowd_erupt", "sfx_arena_crowd_erupt_2", "sfx_arena_crowd_erupt_3",
            "sfx_arena_crowd_cheer", "sfx_arena_crowd_cheer_2", "sfx_arena_crowd_cheer_3",
            "sfx_arena_crowd_ooh", "sfx_arena_crowd_ooh_2", "sfx_arena_crowd_ooh_3",
            "sfx_arena_crowd_aww", "sfx_arena_crowd_aww_2", "sfx_arena_crowd_aww_3",
            "sfx_arena_crowd_gasp", "sfx_arena_crowd_gasp_2", "sfx_arena_crowd_laugh", "sfx_arena_crowd_laugh_2",
            "sfx_arena_crowd_save",
            "sfx_arena_chant_stomp", "sfx_arena_chant_claps", "sfx_arena_chant_claps_fast",
            "sfx_arena_chant_ooh_hey", "sfx_arena_chant_drums", "sfx_arena_chant_horns",
            "sfx_arena_pa_chime", "sfx_arena_pa_organ", "sfx_arena_pa_horn", "sfx_arena_pa_fanfare",

            // The shove has a dedicated cloth/rubber cue; body contact retains its alias.
            "hit_body", "bump_swing",

            // The slipper.
            "throw_whoosh", "throw_charge", "slipper_land", "slipper_bounce", "grab",
            "can_knockdown", "reset_complete", "pickup", "throw_release",
            "court_escape", "court_skid",

            // Match state.
            "countdown_tick", "countdown_go", "round_win", "round_lose", "match_win",
            "round_end", "score_award",

            // UI.
            "ui_click", "ui_hover", "ui_back", "ui_error",

            // ⚠️ FOUR STATES THAT HAD NO SOUND, ADDED 2026-09-18 WITH THE LOGIN AND TITLE
            // REWORK. Nothing above was replaced: `docs/Asset_Sourcing.md` § 5.5 records that
            // swapping cues this game already had was rejected by name. These are the gaps
            // `MenuSfx`'s header describes, where a converted screen went silent for something
            // the Godot build made a noise for. `tools/build_ui_cues.py` generates them.
            "ui_tick", "ui_toggle", "ui_valid", "ui_start",

            // The boot sting. ⚠️ It is a separate stream rather than audio on the video
            // because Godot 4's only core video codec is Theora and the clip was exported
            // with no audio track. In Unity the video can carry its own audio, so this is one
            // of the few places the port can SIMPLIFY rather than transcribe. Left as a cue
            // for now so the boot screen keeps working either way.
            "boot_sting",
        };

        public static readonly IReadOnlyDictionary<string, string> Music =
            new Dictionary<string, string>
            {
                { "menu",  "ost_menu.mp3" },
                { "match", "ost_match.mp3" },
                { "tutorial", "ost_tutorial.wav" },
            };

        public const float MusicCrossfadeTime = 1.5f;

        // -------------------------------------------------------------------
        // § THE CUES THAT ARE THEMSELVES THE DUCK TRIGGER. `audio_manager.gd` 4.6.
        //
        // ⚠️⚠️ THE DUCK IS HOOKED WHERE THE SOUND IS PLAYED, SO NO OTHER FILE HAS TO KNOW IT
        // EXISTS. The Godot original's note says exactly this: every one of these already goes
        // through `play()` from the HUD and the match code, so hooking the duck at that one
        // choke point means the countdown does not have to be taught about the music bed, and
        // a screen added later gets the behaviour for free.
        //
        // ⚠️ THESE ARE ANNOUNCEMENTS, NOT IMPACTS. `PlayImpact` already ducks by its own tiny
        // amount scaled to the hit; that is a transient getting out of its own way. This list
        // is the countdown, the round end, the win and the score award, which are the moments
        // the bed must get out of the way of INFORMATION.
        // -------------------------------------------------------------------

        public const float MusicDuckDb = -10.0f;
        public const float MusicDuckHold = 0.5f;

        private static readonly HashSet<string> DuckTriggers = new HashSet<string>
        {
            "countdown_tick", "countdown_go", "round_end", "match_win", "round_lose",
            "score_award",
        };

        /// <summary>Whether playing this cue should duck the music bed under it.</summary>
        public static bool DucksMusic(string cue) => cue != null && DuckTriggers.Contains(cue);

        /// <summary>
        /// Every name a caller may legitimately ask for: the live catalogue plus the six aliases.
        ///
        /// ⚠️ IT EXISTS BECAUSE OF THE WIRE, NOT BECAUSE OF THE MIXER. `MatchRpc`'s cue relay
        /// takes a cue name off a client and fans it out to every peer, and a name is a string.
        /// Without a catalogue check the host relays whatever arrives: an unknown id is a silent
        /// miss on four machines, and a long one is a long string sent four more times. Locally a
        /// bad cue name is a typo somebody hears once; across the wire it is somebody else's
        /// input.
        /// </summary>
        private static readonly HashSet<string> KnownNames = BuildKnownNames();

        private static HashSet<string> BuildKnownNames()
        {
            var set = new HashSet<string>(Live);
            foreach (var kv in Aliases) set.Add(kv.Key);
            foreach (var kv in Music) set.Add(kv.Key);
            return set;
        }

        // -------------------------------------------------------------------
        // § HERO SKILL SOUNDS ARE DELETED (2026-09-29)
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ EVERY HERO SKILL SOUND IS DELETED, ON THE OWNER'S INSTRUCTION, UNTIL THEY ARE REWORKED. 🧑 2026-09-29: *"also all
        /// ur skill sfx suck shit what is that HAHAHA even paete's"*, *"dont put sfx for all skills for now"*, *"will rework them at
        /// a lter date"*, *"can we delete all skill abilities sfx ty haha"*. Every cast, variant, status, payload, zone, ultimate
        /// theme and ultimate weather cue (`IsSkillSfx`) has no file and no registration; the base game's sounds (the can, the
        /// slipper, bodies, tags, footsteps, the map, UI, music) and the heroes' voices are untouched. The kits still NAME their
        /// cues, and `AudioDirector` and `HeroIntroductionScene` ask <see cref="Audible"/> first, so a skill cue plays nothing,
        /// warns nothing and reaches no film, replay or relay listener. The rework registers new files under those names and sets
        /// this true.
        /// </summary>
        public static bool SkillSfxOn = false;

        private static readonly string[] SkillSfxPrefixes =
        {
            "sfx_cast_", "sfx_var_", "sfx_ult_theme_", "sfx_sky_", "sfx_status_", "ability_",
            "sfx_phaister_", "sfx_paete_", "sfx_amihan_", "sfx_nemu_", "sfx_cheska_", "sfx_dante_", "sfx_kuro_", "sfx_rafi_",
        };

        /// <summary>The element and payload cues the kits and their hazards play (`HeroHazards`, the kits' payloads).</summary>
        private static readonly HashSet<string> SkillSfxCues = new HashSet<string>
        {
            "sfx_explosion_heavy", "sfx_lightning_strike", "sfx_ice_freeze", "sfx_fire_whoosh", "sfx_ghost_teleport",
            "sfx_quake_slam", "sfx_thunder_impact", "sfx_frost_nova", "sfx_possess_enter", "sfx_possess_exit", "sfx_slipper_burst",
            "sfx_ice_form", "sfx_barricade_raise", "sfx_ice_shatter", "sfx_ice_thaw", "sfx_void_close", "sfx_magma_cool",
            "sfx_eclipse_toll", "sfx_hex_cast", "sfx_hex_afflict", "sfx_blink_arrive", "sfx_coven_summon",
        };

        /// <summary>A hero skill's sound (a cast, a variant, a status, a payload, an ultimate's theme or weather).</summary>
        public static bool IsSkillSfx(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return false;
            if (SkillSfxCues.Contains(cue)) return true;
            foreach (var prefix in SkillSfxPrefixes)
                if (cue.StartsWith(prefix, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// ⚠️ THE REWORKED SKILL SOUNDS, released from the switch one by one as each is rebuilt. AIRBURST v3 (2026-10-03,
        /// `docs/reports/amihan-presentation-2026-10-02/airburst-v3.md`, `tools/build_amihan_ult_audio.py`): Amihan's ultimate
        /// only. Every other hero's skill sounds, and her other skills, stay off.
        /// </summary>
        private static readonly HashSet<string> ReworkedSkillSfx = new HashSet<string>
        {
            // Amihan's were removed on the owner's instruction (2026-10-03, *"i meant remove all sfx"*).
            // ⚠️ THE REWORK BEGINS WITH PAETE, ONE ABILITY AT A TIME, EACH PICKED BY THE OWNER'S EAR FROM DRAFTS FIRST. LIANA
            // LEAP, 2026-10-07: *"lets try option E"* (real CC0 recordings, `tools/build_paete_skill_sfx.py`, the sources
            // in `tools/paete_sfx_sources.json`). Nothing else of his, and no other hero, is audible yet.
            "sfx_cast_paete_vine", "sfx_paete_vine_catch", "sfx_paete_vine_land",
            // MAKILING'S EMBRACE's cutscene, 2026-10-08: of three soundtracks drafted over the 9 s film he said *"B but the trunk
            // going up sound feels so light"* (b is the physical sounds with a voice for her: a singing bowl, a choir's held
            // note, chimes, a gong; its heaves were then given a rumble, a heavy log and a bass drum under them). One track,
            // `tools/build_paete_ult_sfx.py`; its times are the cutscene's (`HeroIntroductionScene.Paete.cs`, `PaeteRealAt`).
            "sfx_ult_theme_paete",
        };

        public static bool IsReworkedSkillSfx(string cue) => !string.IsNullOrEmpty(cue) && ReworkedSkillSfx.Contains(cue);

        /// <summary>False for a skill sound while they are switched off (<see cref="SkillSfxOn"/>), unless it was reworked.</summary>
        public static bool Audible(string cue) => SkillSfxOn || !IsSkillSfx(cue) || IsReworkedSkillSfx(cue);

        public static bool IsKnown(string cue) => !string.IsNullOrEmpty(cue) && KnownNames.Contains(cue);

        /// <summary>Resolve a cue name to the file stem that actually exists on disk.</summary>
        public static string FileStemFor(string cue)
        {
            if (cue == null) return null;
            return Aliases.TryGetValue(cue, out var real) ? real : cue;
        }

        /// <summary>
        /// ⚠️⚠️ B-121 — HEADROOM. EVERY SFX VOICE IS ATTENUATED BY THIS AND IT IS THE FIX FOR
        /// THE DISTORTION REPORT. 🧑 on this build: *"audio feels sabog or distorted if that
        /// makes sense"*, *"the audio feels so off in the unity gaem"*. `audio_manager.gd`
        /// carries this constant with a measurement beside it: the SFX bus was measured at
        /// **peak +2.0 dBFS** during a real match, i.e. over full scale, i.e. digital clipping,
        /// which is what a buzz IS. The port carried the per-cue `TrimDb` table across and left
        /// the headroom behind, so it reproduced the mix BALANCE without the gap that mix was
        /// designed to sit inside.
        ///
        /// Two independent causes, and the trim table only answers the first:
        ///
        ///   1. Every source is normalised to peak 0.85, so a trim above 0 dB clips on its own.
        ///      The table below is already all &lt;= 0, so that half came across.
        ///   2. VOICES SUM. Four concurrent voices is normal in a fight, and four sounds each
        ///      peaking at 0.85 go well past 1.0 together however well behaved each is alone.
        ///      That needs a real gap, and nothing here provided one.
        ///
        /// ⚠️ DO NOT REMOVE IT TO "MAKE THE GAME LOUDER". The .gd says the same, and says why:
        /// that is precisely the change that caused the bug. The player's own sliders are the
        /// volume control; this is the mix.
        /// </summary>
        public const float HeadroomDb = -7.0f;

        /// <summary>
        /// The cue's own trim, WITH the headroom already in it.
        ///
        /// ⚠️ THE CLAMP IS A BACKSTOP, NOT DECORATION. `_trim()` in the .gd clamps for the same
        /// reason: every source is normalised to 0.85 peak, so there is no headroom above 0 dB
        /// to boost into and a positive trim added later would clip on its own before any
        /// summing. To make one sound stand out, pull the others down.
        /// </summary>
        public static float TrimFor(string cue)
        {
            float db = cue != null && TrimDb.TryGetValue(cue, out var t) ? t : 0.0f;
            return Mathf.Min(db, 0.0f) + HeadroomDb;
        }
    }
}
