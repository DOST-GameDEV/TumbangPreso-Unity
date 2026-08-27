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

        // -------------------------------------------------------------------
        // AUDIO
        // -------------------------------------------------------------------

        public float MasterVolume = DefaultVolume;
        public float SfxVolume = DefaultVolume;
        public float MusicVolume = DefaultVolume;

        public const float DefaultVolume = 0.8f;

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
        /// </summary>
        public void Apply()
        {
            ApplyDisplay();
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
            PlayerName = SanitiseName(PlayerName);

            MasterVolume = Mathf.Clamp01(MasterVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.1f, 5.0f);
            AiDifficulty = Mathf.Clamp(AiDifficulty, 0, AIController.NoBotsIndex);
            SlipperHighlight = Mathf.Clamp(SlipperHighlight, 0, SlipperHighlights.All.Length - 1);

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
