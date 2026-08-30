using System;
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
            SlipperHighlight = Mathf.Clamp(SlipperHighlight, 0, SlipperHighlights.All.Length - 1);
            AntiAliasMode = Mathf.Clamp(AntiAliasMode, 0, AntiAliasModes.All.Length - 1);
            VSyncMode = Mathf.Clamp(VSyncMode, 0, VSyncModes.All.Length - 1);
            RenderStyle = Mathf.Clamp(RenderStyle, 0, RenderStyles.All.Length - 1);

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
