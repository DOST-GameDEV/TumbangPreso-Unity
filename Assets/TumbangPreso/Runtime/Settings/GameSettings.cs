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
    /// that assumes a normal user home. The Singapore VPS runs a headless Linux server build,
    /// often in a container, and anything that assumes a writable home or a display is a
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

        // -------------------------------------------------------------------
        // MATCH
        // -------------------------------------------------------------------

        /// <summary>0 easy, 1 normal, 2 hard. Normal by default.</summary>
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
            Screen.fullScreen = Fullscreen;
            AIController.ApplyDifficulty(AiDifficulty);
        }

        public void Validate()
        {
            PlayerName = SanitiseName(PlayerName);

            MasterVolume = Mathf.Clamp01(MasterVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.1f, 5.0f);
            AiDifficulty = Mathf.Clamp(AiDifficulty, 0, 2);

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

            _current.Validate();

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
        }
    }
}
