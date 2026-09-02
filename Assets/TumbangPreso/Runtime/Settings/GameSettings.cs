using System;
using System.Collections.Generic;
using System.IO;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// Everything the player can change, and the one file it persists to.
    ///
    /// ⚠️⚠️ IT WRITES A JSON FILE, NOT PlayerPrefs, AND THE REASON IS THE DEDICATED SERVER.
    /// PlayerPrefs on Windows is the registry, on Linux it is a file under a config directory
    /// that assumes a normal user home. A headless Linux server build often runs in a container,
    /// and anything that assumes a writable home or a display is a
    /// failure that only ever appears in production. A JSON file next to the persistent data
    /// path is inspectable, diffable, and can simply fail to load without taking the server
    /// with it.
    ///
    /// ⚠️ A FAILED LOAD MUST NOT BE FATAL. Defaults are correct for a server, so an
    /// unreadable or missing settings file logs and continues. A server that refuses to boot
    /// because a cosmetic preference file is absent is worse than one running on defaults.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        // -------------------------------------------------------------------
        // IDENTITY
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ CAPPED AT 14, AND THE CAP IS A RULE RATHER THAN A DRAW-TIME CLAMP. Nothing
        /// clips a name when it is drawn: a "LOLA PACIN..." on a card is the layout bug wearing
        /// a disguise, and it gets found by a player rather than by a probe. Names are
        /// sanitised ONCE, on the host, on arrival, and every layout is designed against the
        /// worst case this permits.
        ///
        /// ⚠️ AND IT IS THE SAME 14 THE BOT NAMES USE, not a second number. Bot and human names
        /// sit on the same rows (the scoreboard, the toasts, the role swap cards), and two
        /// different caps on one row is how a layout gets tuned against one and broken by the
        /// other. The longest authored roster name is LOLA PACING at 11, so this bounds a
        /// future addition rather than anything shipping today.
        /// </summary>
        public string PlayerName = "";

        // ⚠️ THE ACCOUNT PROFILE IS STORED BESIDE THE OFFLINE TOKEN, NOT IN PlayerPrefs.
        // The same JSON file already has the required properties: it survives a restart, works
        // in a disconnected LAN venue, and can fail without preventing the game from booting.
        // PlayerAccount reconciles these fields with the authenticated profile when UGS answers.
        public string AccountPlayerId = "";
        public string AccountUsername = "";
        public string AccountDiscriminator = "";
        public string AccountBio = "";
        public string AccountCountry = "";
        public string AccountPronouns = "";
        public string AccountEmail = "";
        public string AccountCreatedUtc = "";
        public bool AccountHasPassword = false;
        public bool AccountUpgradeOfferPending = false;
        public bool AccountUpgradeOfferShown = false;

        /// <summary>
        /// Whether this machine has ever answered the boot account screen.
        ///
        /// ⚠️⚠️ IT IS WHY THE SCREEN IS SHOWN ONCE AND NEVER AGAIN, AND IT REVERSES A RULE THAT
        /// WAS WRITTEN DOWN TWICE. `FUTURE.md` PHASE 1 says *"never block a first-time player on
        /// a form"* and `docs/TODO.md` § 92.3 calls the boot behaviour *"the one thing that must
        /// not move"*. 🧑 moved it, 2026-08-31: *"i want this like pubg but they have ann option
        /// to continue right as a guest"*.
        ///
        /// ⚠️ THE OLD RULE WAS ABOUT A FORM AND THIS IS A CHOICE, WHICH IS THE DISTINCTION THAT
        /// MAKES BOTH TRUE. What § 92.3 refused was a panel with six fields and a password box
        /// appearing unasked; what PUBG Mobile actually does, and what this is, is one screen
        /// asking one question with a one-press escape on it. **The escape is the whole
        /// argument**: if CONTINUE AS GUEST is ever not one press, or ever needs the network,
        /// this has become the thing the old rule was protecting against.
        ///
        /// ⚠️⚠️ AND IT MUST DEFAULT TO FALSE FOR EVERY EXISTING PLAYER, WHICH IS FREE HERE AND
        /// WOULD NOT BE IF IT WERE INVERTED. A `bool` absent from a saved `settings.json`
        /// deserialises to false, so everybody who already has the game sees the screen once on
        /// the next launch. That is correct: they have never been asked.
        /// </summary>
        public bool AccountChoiceMade = false;

        // -------------------------------------------------------------------
        // THE BANNER. `docs/TODO.md` § 98, `FUTURE.md` PHASE 5.
        //
        // ⚠️⚠️ IT IS STORED LOCALLY ON PURPOSE, AND PUTTING IT ON `PlayerProfile` INSTEAD WOULD
        // HAVE DELETED IT ON EVERY MATCH. That document is round-tripped through
        // `ugs/cloud-code/match-record.js`, and `CareerStore.AdoptRemoteProfile` REPLACES the
        // local copy with whatever the endpoint answers. The deployed script does not know these
        // fields, so it would answer without them and every submitted match would silently strip
        // the player's banner. **§ 94.2b is the entry about a deployed script being behind the
        // repository; this is what that costs if you add a field before the endpoint knows it.**
        //
        // ⚠️ SO THE ORDER IS FIXED: the endpoint learns the fields FIRST, then the profile
        // carries them, then this moves. Until then a banner is visible to the player who chose
        // it and to nobody else, which is worth having on its own and is honest about its limits.
        // § 98.2 step 2.
        //
        // ⚠️ FOUR STRINGS AND A LIST, MIRRORING `BannerSelection`. The core owns the rules and
        // this is only the drawer; anything that has to be argued about goes there, per
        // `FUTURE.md` § 0.5 rule 3.
        // -------------------------------------------------------------------
        public string BannerTitleId = "";
        public string BannerBadgeId = "";
        public string BannerBorderId = "";
        public string BannerPaletteId = "";
        public string[] BannerTrackers = new string[0];

        /// <summary>
        /// The palette each character is wearing, remembered per character.
        ///
        /// ⚠️⚠️ `FUTURE.md` PHASE 5 CALLS THIS *"one extra that is worth more than it costs"* and
        /// it is the difference between a cosmetic somebody sets once and one they set every
        /// match until they stop bothering. Switching character must not mean re-dressing.
        ///
        /// ⚠️ A LIST OF PAIRS RATHER THAN A DICTIONARY, because `JsonUtility` cannot serialise a
        /// `Dictionary` and answers an empty one with no error. Every settings field in this file
        /// has to survive a round trip through that serialiser; a type it silently drops is a
        /// setting that silently resets. `Loadouts` is the accessor so no caller walks the list.
        /// </summary>
        public List<CharacterLoadout> CharacterLoadouts = new List<CharacterLoadout>();

        /// <summary>
        /// The three custom characters, each as ONE wire string.
        ///
        /// ⚠️⚠️ STRINGS RATHER THAN A `List&lt;CustomCharacterProfile&gt;`, AND `JsonUtility` IS
        /// WHY. `CustomCharacter` is written entirely in auto-properties, and `JsonUtility`
        /// serialises FIELDS ONLY: it would have written three empty objects, read three empty
        /// objects back, and reported no error at all. That is the same silent-reset failure the
        /// paragraph above records for `Dictionary`, and it is why every field in this file has to
        /// be checked against that serialiser rather than against the C# type system.
        ///
        /// ⚠️⚠️ AND THE CODEC IS THE SAME ONE THE WIRE USES, WHICH IS THE POINT. `LookCodec` and
        /// `BannerCodec` are both "one string with a version letter" for the same reason:
        /// `CustomCharacterRules.DecodeWire` refuses a version it does not recognise and answers a
        /// default character rather than guessing at fields it cannot name. **A save file written
        /// by a newer build therefore degrades to a default rather than to a corrupt hero**, and
        /// the disk format and the network format cannot drift apart because there is only one.
        ///
        /// ⚠️ EMPTY OR SHORT IS "NEVER MADE ONE" AND IS FILLED IN BY
        /// `CustomCharacterProfile.EnsureSlots`, never by throwing. A settings file is a plain
        /// text file on the player's disk and every read of it has to survive being edited.
        /// </summary>
        public List<string> CustomCharacterWires = new List<string>();

        /// <summary>Which of the three is the one that walks into a match. Clamped on read.</summary>
        public int ActiveCustomSlot = 0;

        /// <summary>
        /// Whether the player is bringing their own character rather than a roster one.
        ///
        /// ⚠️ IT IS A SEPARATE FLAG RATHER THAN A SENTINEL IN `CharacterPick`, because
        /// `CharacterPick` is an INDEX INTO A WIRE-FACING LIST (`Roster`'s header: append, never
        /// reorder) and adding a nineteenth entry to that list to mean "custom" would change what
        /// index 18 resolves to on every build that has not shipped yet. The custom character
        /// travels as its own field with its own id.
        /// </summary>
        public bool UseCustomCharacter = false;

        /// <summary>
        /// Which ability variant each hero has equipped in each slot.
        ///
        /// ⚠️⚠️ THIS IS THE THING THE LOADOUT SCREEN DID NOT HAVE, WHICH IS WHY ITS EQUIP BUTTON
        /// HAD NO LISTENER. `docs/TODO.md` § 108.3. A screen for choosing something with no store
        /// behind it cannot choose, and the missing store is the reason rather than an oversight
        /// beside it.
        ///
        /// ⚠️ WHAT IS WRITTEN HERE IS A WISH AND NOT A FACT. `HeroBuildRules.Equipped` checks
        /// every id against the hero, the slot, the budget rule and the ledger before honouring
        /// it, and it runs on the RECEIVING side for a peer's build too. `settings.json` is a
        /// plain text file on the player's disk and this one decides a gameplay number.
        /// </summary>
        public List<HeroBuild> HeroBuilds = new List<HeroBuild>();

        /// <summary>
        /// Successful casts toward the twelve Practice-safe ability challenges.
        /// ⚠️ A LIST, NOT A DICTIONARY: `JsonUtility` silently drops dictionaries. The same
        /// constraint shapes `HeroBuilds` immediately above it.
        /// </summary>
        public List<AbilityChallengeProgress> AbilityChallenges =
            new List<AbilityChallengeProgress>();

        // -------------------------------------------------------------------
        // TELEMETRY. `docs/TODO.md` § 90.3.
        // -------------------------------------------------------------------

        /// <summary>
        /// The opt-out, which the Settings screen shows and which stops ALL sending.
        ///
        /// ⚠️⚠️ IT DEFAULTS TO ON AND THE SETTINGS SCREEN SAYS WHAT IS COLLECTED, which is the
        /// whole of `FUTURE.md` § 19.3's fourth build item. Turning it off must not merely stop
        /// the upload: `TelemetrySink` stops COUNTING, so an opted-out player accumulates nothing
        /// to send later and nothing that a future version could decide to flush. An opt-out that
        /// only gates the transmit is one refactor away from being no opt-out at all.
        /// </summary>
        public bool TelemetryEnabled = true;

        /// <summary>
        /// How far through the first-launch funnel this install has got. -1 is "not launched yet",
        /// which is what makes the first boot a measurable event rather than an assumption.
        ///
        /// ⚠️ IT ONLY GOES FORWARD, per `TelemetryRules.FurthestFunnelStep`, and it is an INDEX
        /// into an append-only list. Reordering `TelemetryEvents.Funnel` silently rewrites what
        /// every stored value here means.
        /// </summary>
        public int TelemetryFunnelStep = -1;

        /// <summary>
        /// The furthest funnel step a batch carrying it has actually been delivered.
        ///
        /// ⚠️⚠️ IT IS A SEPARATE NUMBER FROM `TelemetryFunnelStep` BECAUSE THIS GAME'S FIRST
        /// SESSION IS OFTEN OFFLINE, AND WITHOUT IT THE FUNNEL WOULD UNDERCOUNT EXACTLY THE
        /// PLAYERS IT MATTERS MOST FOR. A first launch in a hall with no internet records the
        /// step locally, sends nothing, and with one number would never send it again: the server
        /// would report an install that reached the menu offline as an install that never
        /// launched. `TelemetrySink` re-notes everything between this and `TelemetryFunnelStep`
        /// on the first signed-in session, and the endpoint is idempotent about funnel steps (the
        /// first timestamp wins), so a step delivered twice costs nothing.
        /// </summary>
        public int TelemetryFunnelSent = -1;

        // -------------------------------------------------------------------
        // AUDIO
        // -------------------------------------------------------------------

        /// <summary>
        /// Where the three sliders sit, 0 to 1. ⚠️ THIS IS THE KNOB POSITION, NOT A GAIN. Read
        /// <see cref="MasterGain"/> before multiplying any of these into an `AudioSource.volume`.
        /// </summary>
        public float MasterVolume = DefaultVolume;
        public float SfxVolume = DefaultVolume;
        public float MusicVolume = DefaultVolume;

        public const float DefaultVolume = 0.8f;

        /// <summary>
        /// Turns a slider POSITION into an amplitude.
        ///
        /// ⚠️⚠️ A VOLUME SLIDER WIRED STRAIGHT TO AMPLITUDE FEELS BROKEN, AND THAT IS 🧑
        /// 2026-08-29: *"audio in settings is also broken, even when i lower its still very very
        /// loud"*. Nothing was broken in the wiring: the slider is authored 0 to 1, the setter
        /// writes on every value change, and every one of the five places that plays a sound
        /// reads the value live. The fault is the CURVE. Amplitude is not loudness. Half the
        /// slider is half the amplitude, which is -6 dB, which a listener hears as "slightly
        /// quieter"; the default sits at 0.8, which is -1.9 dB, so the top third of the groove
        /// does almost nothing audible and the whole control reads as inert.
        ///
        /// ⚠️ SQUARING IS THE FIX AND IT IS THE STANDARD ONE. Perceived loudness goes
        /// roughly as amplitude to the 0.6, so squaring the position lands close to
        /// proportional: the knob at 0.5 is a quarter of the amplitude (-12 dB, about half as
        /// loud), at 0.2 it is 0.04 (-28 dB, nearly silent), and at 1.0 it is still exactly 1.0,
        /// so nothing gets QUIETER at full than it was before this change. The default 0.8 moves
        /// from -1.9 dB to -3.9 dB, which is the only thing anybody will notice on an untouched
        /// install, and it is the direction that makes the rest of the groove usable.
        ///
        /// ⚠️ ONE CONVERSION, READ BY ALL FIVE PLAYERS. `AudioDirector`, `VoiceDirector`,
        /// `MusicDirector`, `BootSting` and `SplashScreen` each multiplied the raw fields
        /// together in their own line. Five copies of a curve is five places for the next one to
        /// be missed, and the boot sting and the splash are exactly the two that play before a
        /// player can reach the settings panel to turn them down.
        /// </summary>
        public static float Gain(float sliderPosition)
        {
            float p = Mathf.Clamp01(sliderPosition);
            return p * p;
        }

        /// <summary>The master fader's amplitude. See <see cref="Gain"/>.</summary>
        public float MasterGain => Gain(MasterVolume);

        /// <summary>
        /// What a sound effect is multiplied by: the sfx fader under the master fader.
        ///
        /// ⚠️ THE TWO FADERS ARE CURVED SEPARATELY AND THEN MULTIPLIED, which is what a mixer
        /// does. Curving their product instead would make each fader's feel depend on where the
        /// other one happens to sit.
        /// </summary>
        public float SfxGain => Gain(SfxVolume) * Gain(MasterVolume);

        /// <summary>The music bed's amplitude, on the same rule as <see cref="SfxGain"/>.</summary>
        public float MusicGain => Gain(MusicVolume) * Gain(MasterVolume);

        // -------------------------------------------------------------------
        // CAMERA
        // -------------------------------------------------------------------

        public float MouseSensitivity = 1.0f;
        public bool InvertY = false;

        // -------------------------------------------------------------------
        // DISPLAY
        // -------------------------------------------------------------------

        public bool Fullscreen = true;

        /// <summary>
        /// Whether the pad rumbles on a knockdown, a tag, being tagged, and the can going back up.
        ///
        /// ⚠️⚠️ A `bool` WHOSE INITIALISER IS `true`, WHICH IS THE SAFE SHAPE HERE AND IS WORTH
        /// SAYING OUT LOUD. <see cref="RenderStyle"/> carries the rule this obeys: `JsonUtility`
        /// constructs the object before it overwrites the fields the file carries, so a
        /// `settings.json` written by an older build inherits the field initialiser. For an INDEX
        /// that means a default of row 0 silently turns a feature off for everybody upgrading; for
        /// a bool it means the initialiser IS the upgrade behaviour, and `true` is the one that
        /// matches what a player would expect the first time they plug a pad in.
        ///
        /// ⚠️ IT IS A SETTING RATHER THAN ALWAYS-ON BECAUSE `docs/FUTURE.md` § 16.2 IS AN
        /// ACCESSIBILITY LIST AND A HAPTIC NOBODY CAN TURN OFF IS ON IT. See
        /// <see cref="InputLayer.Rumble"/> for the four cues and why they are four rather than one.
        /// </summary>
        public bool Rumble = true;

        /// <summary>
        /// Which anti-aliasing mode to render at, as an index into
        /// <see cref="AntiAliasModes.All"/>. 0 is Off.
        ///
        /// ⚠️⚠️ IT IS A SETTING RATHER THAN A CONSTANT BECAUSE THE QUALITY LEVELS ALREADY
        /// DISAGREED AND NOTHING RECONCILED THEM. `QualitySettings.asset` carried MSAA on two of
        /// its six levels and none on the other four, and every offscreen probe in the project
        /// built its RenderTexture with 4 or 8 samples regardless. So the sample count a player
        /// got depended on a quality level nothing in this game ever shows them, while every
        /// image the project judged itself by was anti-aliased. One stored index, applied in one
        /// place, is what makes the two answerable with the same question.
        ///
        /// ⚠️ STORED AS AN INT for the reason <see cref="AiDifficulty"/> and
        /// <see cref="SlipperHighlight"/> both record: the settings file is read back by builds
        /// whose list may have grown a row, and an int with a clamp survives that.
        /// </summary>
        public int AntiAliasMode = AntiAliasModes.Default;

        /// <summary>
        /// Whether the game waits for the display before showing a frame, as an index into
        /// <see cref="VSyncModes.All"/>.
        ///
        /// ⚠️ STORED AS AN INT WITH A CLAMP, like every other mode index on this object, because a
        /// settings file written by an older build is read back by a newer one whose list may have
        /// grown a row. See <see cref="VSyncModes"/> for why the half-refresh row is the one worth
        /// having and the one people leave out.
        /// </summary>
        public int VSyncMode = VSyncModes.Default;

        /// <summary>
        /// Which look the game is drawn in, as an index into <see cref="RenderStyles.All"/>.
        /// 0 is Toon, the shipped ink look.
        ///
        /// ⚠️⚠️ IT IS THE ONE INDEX ON THIS CLASS WHOSE DEFAULT IS ROW 0, AND THAT IS SAFE HERE
        /// FOR THE EXACT REASON IT IS UNSAFE EVERYWHERE ELSE. <see cref="AntiAliasMode"/> and
        /// <see cref="SlipperHighlight"/> both default AWAY from their row 0, because
        /// `JsonUtility` constructs the object before it overwrites the fields the file carries,
        /// so an older `settings.json` inherits the field initialiser and a 0 there would silently
        /// turn a feature off for everybody upgrading. Row 0 of the style table IS what those
        /// older builds were already drawing, so the upgrade lands on no change at all.
        ///
        /// ⚠️ THE DEFAULT IS NOT A TASTE. Chromatic is an experiment being evaluated against the
        /// shipped look, and a player who never opens this screen has to see the shipped look.
        /// `RenderStyles.Default` carries the full reasoning and `LobbyAndSettingsTests` asserts
        /// the upgrade path rather than trusting it.
        ///
        /// ⚠️ STORED AS AN INT with a clamp, for the reason <see cref="AiDifficulty"/> records:
        /// the settings file is read back by builds whose list may have grown a row.
        /// </summary>
        public int RenderStyle = RenderStyles.Default;

        /// <summary>
        /// Which colour § THE LANDED HIGHLIGHT lights a rested tsinelas in, as an index into
        /// <see cref="SlipperHighlights.All"/>. 0 is Off.
        ///
        /// ⚠️⚠️ THE COLOUR IS THE ONE PART OF THE FEATURE THAT IS LOCAL, AND IT IS NEVER
        /// REPLICATED. Two peers running Red and Yellow light the same slippers in different
        /// colours, which is correct: this is an accessibility choice about one player's
        /// screen, not a fact about the match. A shared colour would be worse than no setting,
        /// because it would let one player change what everybody else sees.
        ///
        /// ⚠️ STORED AS AN INT for the reason <see cref="AiDifficulty"/> records: the settings
        /// file is read back by builds whose palette may have grown a row, and an int with a
        /// clamp survives that.
        /// </summary>
        public int SlipperHighlight = SlipperHighlights.Default;

        // -------------------------------------------------------------------
        // MATCH
        // -------------------------------------------------------------------

        /// <summary>0 easy, 1 normal, 2 hard, 3 none. Normal by default.
        ///
        /// ⚠️ 3 IS AN ABSENCE OF BOTS, NOT A FOURTH TIER, and it is at the END of the range on
        /// purpose. See <see cref="AIController.NoBotsIndex"/>: this int is saved to disk and
        /// replicated over the wire, so inserting a value ahead of the existing three would
        /// re-read every saved setting one tier out.</summary>
        public int AiDifficulty = 1;

        /// <summary>
        /// PHASE 12: the match FORMAT the lobby was last left on. 0 standard, 1 last tsinelas,
        /// 2 mirror.
        ///
        /// ⚠️ STORED AS AN INT for the reason <see cref="AiDifficulty"/> records: this file is
        /// read back by builds whose list may have grown a row, and an int with a clamp survives
        /// that where an enum name does not. `CustomGameRules.Parse` clamps it on the way in.
        ///
        /// ⚠️ AND IT IS A PREFERENCE, NOT THE MATCH'S ANSWER. `SceneFlow.SelectedFormat` is what
        /// a running match reads and `MatchRpc.SelectFormatServerRpc` is what a room agrees on;
        /// this is only what the lobby opens showing.
        /// </summary>
        public int MatchFormat;

        // -------------------------------------------------------------------
        // PICKS. Carried into a match by GameLaunch.
        // -------------------------------------------------------------------

        /// <summary>⚠️ -1 IS A LEGITIMATE VALUE meaning "no pick", and it resolves to neutral
        /// everywhere. It must not be normalised to 0, which is a real entry.</summary>
        public int CharacterPick = -1;
        public int CanPick = -1;
        public int SlipperPick = -1;

        /// <summary>
        /// Stable identity for reconnection, minted once and kept.
        ///
        /// ⚠️ THIS IS WHAT LETS A DROPPED PLAYER GET THEIR SEAT BACK, so it must survive a
        /// restart. It is not a secret and it is not an account: it identifies a returning
        /// peer to a host that is still running the match they left.
        /// </summary>
        public string PlayerToken = "";

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ SANITISE ONCE, ON THE HOST, ON ARRIVAL. Empty is legal and falls back to the seat
        /// label, so nothing that draws a name needs a null check.
        /// </summary>
        public static string SanitiseName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            string trimmed = raw.Trim();

            // Strip control characters and newlines: a name is one line on a card.
            var sb = new System.Text.StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
                if (!char.IsControl(c)) sb.Append(c);

            string clean = sb.ToString().Trim();
            if (clean.Length > Balance.PlayerNameMax)
                clean = clean.Substring(0, Balance.PlayerNameMax);

            return clean;
        }

        /// <summary>
        /// Push the saved settings at the systems that own them.
        ///
        /// ⚠️ FULLSCREEN WAS ONLY EVER APPLIED WHEN THE BUTTON WAS PRESSED, so the saved value
        /// was written to disk, read back on the next launch, displayed correctly on the
        /// settings screen — and the game still opened in whatever mode Unity felt like. A
        /// setting that survives a restart everywhere except in the actual window is worse
        /// than one that does not save at all.
        ///
        /// Volumes need no push: the music bed, the announcer and the SFX all read the
        /// sliders live, which is what makes dragging one audible immediately.
        ///
        /// ⚠️ ANTI-ALIASING IS PUSHED HERE FOR EXACTLY THE FULLSCREEN REASON. It is two engine
        /// switches rather than a value something reads back (`QualitySettings.antiAliasing` and
        /// the flag `Visual.PostAntiAlias` runs off), so a mode that is only stored is a mode
        /// that survives a restart everywhere except in the picture.
        ///
        /// ⚠️ THE RENDER STYLE IS PUSHED HERE FOR THE SAME REASON AND ONE MORE. Two of its three
        /// switches are statics that a render callback reads, and the third is a GLOBAL shader
        /// float, which lives in the graphics device rather than in this object: nothing restores
        /// it on its own, so a style that is only stored is a style that never reaches a single
        /// pixel. This is also what makes the panel's BACK button undo a pick, since
        /// `SettingsStore.Restore` re-applies the snapshot through here.
        /// </summary>
        public void Apply()
        {
            ApplyDisplay();
            AntiAliasModes.Apply(AntiAliasMode);
            VSyncModes.Apply(VSyncMode);
            RenderStyles.Apply(RenderStyle);
            AIController.ApplyDifficulty(AiDifficulty);

            // ⚠️ PUSHED RATHER THAN POLLED, exactly as `AntiAliasModes.FxaaActive` is. The rumble
            // calls sit in the middle of a scoring event, and the first read of
            // `SettingsStore.Current` loads and validates this whole file off disk.
            InputLayer.Rumble.Enabled = Rumble;
        }

        /// <summary>
        /// Applies the display preference without carrying a small window's backbuffer into
        /// fullscreen. Borderless fullscreen uses the desktop resolution, so both the 3D view
        /// and screen-space UI remain pixel sharp on the player's monitor.
        /// </summary>
        public void ApplyDisplay()
        {
            if (Application.isBatchMode) return;

            if (Fullscreen)
            {
                int width = Display.main != null && Display.main.systemWidth > 0
                    ? Display.main.systemWidth
                    : Screen.currentResolution.width;
                int height = Display.main != null && Display.main.systemHeight > 0
                    ? Display.main.systemHeight
                    : Screen.currentResolution.height;

                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                return;
            }

            // Keep windowed mode comfortably inside a 1080p desktop while preserving the
            // game's authored 16:9 layout. Do not reuse a 4K fullscreen size for a window.
            const int preferredWidth = 1600;
            const int preferredHeight = 900;
            int windowWidth = Mathf.Min(preferredWidth, Screen.currentResolution.width);
            int windowHeight = Mathf.Min(preferredHeight, Screen.currentResolution.height);
            Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.Windowed);
        }

        public void Validate()
        {
            if (AccountRules.TryDisplayName(PlayerName, out string accountName))
                PlayerName = accountName;
            else
                PlayerName = SanitiseName(PlayerName);

            AccountDiscriminator = AccountRules.Discriminator(AccountDiscriminator,
                string.IsNullOrEmpty(AccountPlayerId) ? PlayerToken : AccountPlayerId);
            AccountBio = AccountRules.Bio(AccountBio);
            AccountCountry = AccountRules.Country(AccountCountry);
            AccountPronouns = AccountRules.Pronouns(AccountPronouns);

            MasterVolume = Mathf.Clamp01(MasterVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.1f, 5.0f);
            AiDifficulty = Mathf.Clamp(AiDifficulty, 0, AIController.NoBotsIndex);
            MatchFormat = Mathf.Clamp(MatchFormat, 0, (int)Core.MatchFormat.Mirror);
            SlipperHighlight = Mathf.Clamp(SlipperHighlight, 0, SlipperHighlights.All.Length - 1);
            AntiAliasMode = Mathf.Clamp(AntiAliasMode, 0, AntiAliasModes.All.Length - 1);
            VSyncMode = Mathf.Clamp(VSyncMode, 0, VSyncModes.All.Length - 1);
            RenderStyle = Mathf.Clamp(RenderStyle, 0, RenderStyles.All.Length - 1);

            CharacterLoadouts ??= new List<CharacterLoadout>();
            HeroBuilds ??= new List<HeroBuild>();
            AbilityChallenges ??= new List<AbilityChallengeProgress>();

            if (string.IsNullOrEmpty(PlayerToken)) PlayerToken = MintToken();
        }

        /// <summary>
        /// ⚠️ NOT Guid.NewGuid().ToString() BY ACCIDENT. It is deliberately opaque and long
        /// enough that two players on one LAN cannot collide, and it is never shown to a
        /// player, so readability is not a goal.
        /// </summary>
        public static string MintToken() => Guid.NewGuid().ToString("N");
    }

    /// <summary>Loads, holds and saves <see cref="GameSettings"/>.</summary>
    public static class SettingsStore
    {
        private static GameSettings _current;

        /// <summary>
        /// Raised when the player picks a different landed-highlight colour or switches it off,
        /// so every tsinelas already lying on the arena repaints NOW rather than at its next
        /// landing.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE THE SETTINGS PANEL IS REACHABLE FROM THE IN-MATCH PAUSE
        /// MENU. Without it the control appears to do nothing: the player changes the colour,
        /// closes the pause menu, and every slipper on the ground is still lit the old way
        /// until somebody throws one. "Takes effect next round" reads as the control being
        /// broken, which is the reachable-and-does-nothing failure the Godot board's own rule
        /// forbids.
        ///
        /// ⚠️ SUBSCRIBERS MUST UNSUBSCRIBE. This is a static event and a Slipper is destroyed
        /// at every round reset, so a slipper that subscribes and never detaches keeps its
        /// whole object graph alive and is written to after it is gone.
        /// </summary>
        public static event Action SlipperHighlightChanged;

        /// <summary>
        /// Announce a highlight change. Called by whatever wrote the setting, rather than from
        /// a property setter, because the field is public and serialised by
        /// <see cref="JsonUtility"/>, which cannot see through a property.
        /// </summary>
        public static void RaiseSlipperHighlightChanged() => SlipperHighlightChanged?.Invoke();

        public static GameSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        public static string Path =>
            System.IO.Path.Combine(Application.persistentDataPath, "settings.json");

        public static void Load()
        {
            _current = new GameSettings();

            try
            {
                if (File.Exists(Path))
                {
                    string json = File.ReadAllText(Path);
                    var loaded = JsonUtility.FromJson<GameSettings>(json);
                    if (loaded != null) _current = loaded;
                }
            }
            catch (Exception e)
            {
                // ⚠️ NOT FATAL. See the class note: defaults are correct for a server, and a
                // build that refuses to start over a cosmetic preferences file is worse than
                // one running on defaults.
                Debug.LogWarning($"[Settings] could not read {Path}, using defaults: {e.Message}");
            }

            bool mintedIdentity = string.IsNullOrEmpty(_current.PlayerToken);
            _current.Validate();

            // Validate mints the reconnect identity, but an identity that only lives in RAM
            // changes on every cold launch. Persist it immediately instead of waiting for the
            // player to happen to open and save a settings screen.
            if (mintedIdentity) Save();

            // ⚠️ VALIDATE THEN APPLY, IN THAT ORDER. Applying an unclamped value read off disk
            // would push a nonsense difficulty index straight into the AI.
            _current.Apply();
        }

        /// <summary>
        /// The palette this character is wearing, and the one place that answers it.
        ///
        /// ⚠️⚠️ IT ASKS `LoadoutRules` RATHER THAN TRUSTING THE FILE, so a palette that was
        /// legitimately equipped and is no longer owned stops being worn. That is not
        /// hypothetical: `settings.json` is a plain text file on the player's disk, and the whole
        /// ownership model is that a cosmetic id means nothing without a profile that earned it.
        /// **The same call runs on the receiving side for a peer's palette**, which is why the
        /// rule is in the core and not here.
        ///
        /// ⚠️ AN UNKNOWN CHARACTER ID ANSWERS THE DEFAULT rather than throwing. The roster gains
        /// characters and a settings file outlives a build.
        /// </summary>
        public static string PaletteFor(string characterId)
        {
            var settings = Current;
            if (settings?.CharacterLoadouts == null) return PaletteRules.DefaultId;

            string wanted = "";
            foreach (var row in settings.CharacterLoadouts)
                if (row != null && row.CharacterId == characterId) { wanted = row.PaletteId; break; }

            return LoadoutRules.PaletteFor(GameServices.Career?.Profile, characterId, wanted);
        }

        /// <summary>Remembers a character's palette. Saves, because a cosmetic choice a player
        /// makes and then loses on quit is worse than one they cannot make.</summary>
        public static void SetPaletteFor(string characterId, string paletteId)
        {
            var row = LoadoutFor(characterId);
            if (row == null) return;

            row.PaletteId = paletteId ?? "";
            Save();
        }

        /// <summary>
        /// The loadout row for a character, created on demand, or null when there is no settings
        /// file yet.
        ///
        /// """ + WW + """ONE ACCESSOR, BECAUSE PHASE 5 TURNED ONE FIELD INTO FIVE. `SetPaletteFor`
        /// used to walk the list itself and the customiser would have been a second walk, a third
        /// for the slipper and a fourth for the lata. `docs/TODO.md` """ + S + """ 94.1 is the entry about
        /// "which line is mine" having four hand-written copies that all agreed on the wrong
        /// value, and this is the same list with the same trap in it.
        /// </summary>
        public static CharacterLoadout LoadoutFor(string characterId)
        {
            var settings = Current;
            if (settings == null || string.IsNullOrEmpty(characterId)) return null;

            settings.CharacterLoadouts ??= new List<CharacterLoadout>();
            return LoadoutRules.RowFor(settings.CharacterLoadouts, characterId);
        }

        /// <summary>The checked build this install may bring for one hero.</summary>
        public static HeroBuild HeroBuildFor(string heroId)
        {
            var settings = Current;
            if (settings == null || string.IsNullOrEmpty(heroId)) return null;
            settings.HeroBuilds ??= new List<HeroBuild>();
            return HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
        }

        /// <summary>
        /// A build safe to enter play: each requested id has passed the local Practice challenge.
        /// The returned object is a copy so resolving a stale or edited setting does not rewrite
        /// disk merely because a match started.
        /// </summary>
        public static HeroBuild CheckedHeroBuildFor(string heroId)
        {
            var settings = Current;
            var wanted = HeroBuildFor(heroId);
            var counters = settings?.AbilityChallenges;
            var one = HeroBuildRules.Equipped(wanted, heroId, 1, counters);
            var two = HeroBuildRules.Equipped(wanted, heroId, 2, counters);
            return new HeroBuild
            {
                HeroId = heroId ?? "",
                Slot1VariantId = one?.Id ?? "",
                Slot2VariantId = two?.Id ?? "",
            };
        }

        /// <summary>
        /// Records a successful local cast. Saves only when the capped counter actually changes,
        /// so a completed challenge does not turn every cast into a disk write.
        /// </summary>
        public static void NoteAbilityCast(string heroId, int slot)
        {
            var settings = Current;
            if (settings == null) return;
            settings.AbilityChallenges ??= new List<AbilityChallengeProgress>();
            if (HeroBuildRules.NoteSuccessfulCast(settings.AbilityChallenges, heroId, slot)) Save();
        }

        /// <summary>
        /// The whole look this character is wearing, checked against what the account owns.
        ///
        /// """ + W + """SAME REASONING AS `PaletteFor` ONE METHOD UP, WHICH THIS REPLACES AT EVERY CALL
        /// SITE THAT DRAWS A CHARACTER: `settings.json` is a plain text file on the player's disk,
        /// so a palette that was legitimately equipped and is no longer owned stops being worn.
        /// The dial is clamped rather than checked, because a hue is not a reward.
        /// </summary>
        public static CharacterLook LookFor(string characterId)
        {
            var settings = Current;
            if (settings?.CharacterLoadouts == null) return CharacterLook.Default;

            CharacterLoadout found = null;
            foreach (var row in settings.CharacterLoadouts)
                if (row != null && row.CharacterId == characterId) { found = row; break; }

            if (found == null) return CharacterLook.Default;

            var wanted = new CharacterLook(found.PaletteId, found.HueDegrees, found.SaturationPercent);
            return LoadoutRules.LookFor(GameServices.Career?.Profile, characterId, wanted);
        }

        /// <summary>Remembers the free colour dial for one character.</summary>
        public static void SetLookFor(string characterId, int hueDegrees, int saturationPercent)
        {
            var row = LoadoutFor(characterId);
            if (row == null) return;

            row.HueDegrees = PaletteRules.ClampHue(hueDegrees);
            row.SaturationPercent = PaletteRules.ClampSaturation(saturationPercent);
            Save();
        }

        /// <summary>
        /// The slipper and lata this character carries, falling back to the global pick.
        ///
        /// """ + WW + """-1 MEANS "NEVER CHOSEN FOR THIS CHARACTER" AND FALLS BACK RATHER THAN
        /// DEFAULTING TO ENTRY 0. Entry 0 of each prop list is the neutral one (`CLAUDE.md` """ + S + """ 4),
        /// so defaulting would silently strip every player of the slipper they had picked the
        /// first time this field shipped, and it would look like the game had forgotten it.
        /// </summary>
        public static int SlipperPickFor(string characterId)
        {
            var settings = Current;
            if (settings?.CharacterLoadouts == null) return settings?.SlipperPick ?? 0;

            foreach (var row in settings.CharacterLoadouts)
                if (row != null && row.CharacterId == characterId && row.SlipperPick >= 0)
                    return row.SlipperPick;

            return settings.SlipperPick;
        }

        public static int CanPickFor(string characterId)
        {
            var settings = Current;
            if (settings?.CharacterLoadouts == null) return settings?.CanPick ?? 0;

            foreach (var row in settings.CharacterLoadouts)
                if (row != null && row.CharacterId == characterId && row.CanPick >= 0)
                    return row.CanPick;

            return settings.CanPick;
        }

        /// <summary>Remembers the props this character carries. """ + W + """ **THE GLOBAL PICK IS
        /// WRITTEN TOO**, so a character with no row of its own inherits the last thing the player
        /// chose rather than the neutral entry.</summary>
        public static void SetPropsFor(string characterId, int slipperPick, int canPick)
        {
            var settings = Current;
            if (settings == null) return;

            settings.SlipperPick = slipperPick;
            settings.CanPick = canPick;

            var row = LoadoutFor(characterId);
            if (row != null)
            {
                row.SlipperPick = slipperPick;
                row.CanPick = canPick;
            }

            Save();
        }

        public static void Save()
        {
            if (_current == null) return;
            _current.Validate();

            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(_current, prettyPrint: true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] could not write {Path}: {e.Message}");
            }
        }

        /// <summary>Test seam, so a suite does not read or write the real user file.</summary>
        public static void OverrideForTests(GameSettings s)
        {
            _current = s;
            _current?.Validate();
        }

        /// <summary>
        /// Puts a snapshot back, for the settings panel's DISCARD.
        ///
        /// ⚠️ IT DOES NOT SAVE, AND THAT IS THE POINT. Nothing has been written since the panel
        /// opened, so the FILE is already correct; what has drifted is the RUNNING process,
        /// where a volume is live on the bus and a difficulty is live on the bots. This restores
        /// the values and re-applies them, and deliberately leaves the disk alone.
        /// </summary>
        public static void Restore(GameSettings snapshot)
        {
            if (snapshot == null) return;

            _current = snapshot;
            _current.Validate();
            _current.Apply();

            // ⚠️ THE DISCARD HAS TO REPAINT TOO. Cycling the highlight colour and then pressing
            // BACK restores the field, and without this the arena keeps showing the colour the
            // player just rejected until something else triggers a repaint.
            RaiseSlipperHighlightChanged();
        }
    }
}
