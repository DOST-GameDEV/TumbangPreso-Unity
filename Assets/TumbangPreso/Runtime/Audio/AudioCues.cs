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
                { "bump_swing",     "dash" },
                { "can_knockdown",  "lata_knockdown" },
                { "reset_complete", "reset_channel_complete" },
                { "pickup",         "grab" },
                { "throw_release",  "throw_whoosh" },
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
                { "sfx_ice_thaw",   -11.0f },
                { "sfx_magma_cool", -11.0f },
                { "sfx_void_close",  -9.0f },

                // The three that replaced borrowed cues keep the weight of what they are: a
                // wall landing and a wall failing are both events a player has to act on, and
                // the sheet spreading is the quietest of the three because it is the least
                // urgent thing to know about.
                // A bell is the one cue in the game that is meant to be HEARD OVER the fight
                // rather than located in it, and it is also the longest at 2.2 s. Lower than the
                // other casts so a 2 s tail does not sit on top of everything that follows it.
                { "sfx_eclipse_toll", -6.0f },

                // The affliction fires per victim per hex, so it is mixed as a status rather
                // than as an event: four people standing in one circle must not stack into a wall.
                { "sfx_hex_afflict", -10.0f },
                { "sfx_hex_cast",     -4.0f },

                { "sfx_ice_form",    -5.0f },
                { "sfx_barricade_raise", -3.0f },
                { "sfx_ice_shatter", -3.0f },

                { "ui_hover",       -8.0f },
                { "land",           -6.0f },
                { "grab",           -6.0f },
                { "throw_charge",   -5.0f },
                { "slipper_bounce", -4.0f },
                { "jump",           -4.0f },
                { "throw_whoosh",   -4.0f },
                { "ui_click",       -3.0f },
                { "slipper_land",   -3.0f },
                { "dash",           -3.0f },
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
            "stamina_empty",

            // Hero Abilities & Special Effects.
            "ability_bagsak_bomb", "ability_bakya_bash", "ability_flick_dash",
            "ability_shatter_trap", "ability_spin_guard",
            "sfx_explosion_heavy", "sfx_lightning_strike", "sfx_ice_freeze",
            "sfx_fire_whoosh", "sfx_ghost_teleport", "sfx_hitmarker", "sfx_super_ready",

            // ⚠️ THE MAP EVENT. `LrtTrainFlyby` called `ui_move` for two months and there has
            // never been a `ui_move.wav`, so every pass wrote `[Audio] no cue registered` to the
            // log and the one recurring event on Ilalim ng Tulay was silent. It is 2.70 s long
            // because `OverheadPassWindow.PassSeconds` is.
            "sfx_lrt_pass",

            // ⚠️⚠️ THE PAYLOADS, WHICH ARE NOT THE CASTS. Every kit already fired its own
            // element on the CAST and then shared two leftovers for what actually happened:
            // `CreateExplosion` played `ability_bagsak_bomb` for a 2.2 m stomp, a 4.5 m
            // fissure, a 4.8 m supernova AND a thrown slipper, and `CreateThunderstrike`
            // played `ability_flick_dash`, which is a dash. Both of those are in
            // `DeletedAbilityCues` below: the biggest moments in the game were carried by
            // sounds written for a system that no longer exists. `tools/generate_ability_audio.py`
            // makes these six and says what each one is shaped like and why.
            "sfx_quake_slam", "sfx_thunder_impact", "sfx_frost_nova",
            "sfx_possess_enter", "sfx_possess_exit", "sfx_slipper_burst",

            // ⚠️⚠️ THE ZONE LIFECYCLE SET, AND REGISTERING IT IS THE HALF THAT IS EASY TO MISS.
            // A .wav on disk is not a cue: `AudioDirector.PlayAtVaried` looks the id up in this
            // registry and warns `no cue registered` if it is absent, which is a WARNING and not
            // an exception, so the ability simply plays nothing and the game carries on. The
            // note on `sfx_lrt_pass` above records the same thing costing two months of silence
            // on the one recurring event on Ilalim ng Tulay. The PlayMode run is what caught
            // these six, one line per cast.
            //
            // ⚠️ WHAT THEY REPLACE: `SpawnIceBarricade` and `SpawnIceSheet` BOTH opened on
            // `ability_shatter_trap`, which is a cue for something BREAKING fired at the moment
            // something is BUILT, and `IceBarricadeComponent.Shatter` played `slipper_land`, a
            // rubber sandal hitting the road, for a wall of ice failing. The other three are
            // endings: every hazard in `HeroHazards` ticked down and called `Destroy` in
            // silence. `docs/TODO.md` § 20.
            "sfx_ice_form", "sfx_barricade_raise", "sfx_ice_shatter",
            "sfx_ice_thaw", "sfx_void_close", "sfx_magma_cool",

            // ⚠️⚠️ PHAISTER'S ULTIMATE, WHICH ARRIVED CALLING `sfx_ghost_appear`: A CUE WITH NO
            // FILE AND NO REGISTRATION. It is the same silent-cue fault `sfx_lrt_pass` above
            // records, on the biggest moment in the newest kit. `docs/TODO.md` § 21.
            "sfx_eclipse_toll", "sfx_hex_cast", "sfx_hex_afflict",

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

            // The shove and the block, via aliases.
            "hit_body", "bump_swing",

            // The slipper.
            "throw_whoosh", "throw_charge", "slipper_land", "slipper_bounce", "grab",
            "can_knockdown", "reset_complete", "pickup", "throw_release",

            // Match state.
            "countdown_tick", "countdown_go", "round_win", "round_lose", "match_win",
            "round_end", "score_award",

            // UI.
            "ui_click", "ui_hover", "ui_back", "ui_error",

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
