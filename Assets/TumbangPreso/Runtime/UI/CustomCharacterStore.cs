using System;
using System.Collections.Generic;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Where the three custom characters live between launches.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE FIRST VERSION OF THIS FEATURE HAD NOWHERE TO PUT THEM.
    /// `CustomCharacterCreator.Profile` was a `static CustomCharacterProfile` field on a
    /// `MonoBehaviour`, so every character anybody made was gone the moment the game closed, and
    /// nothing in the project ever wrote one to disk. `docs/TODO.md` § 107 asks for *"3 characters
    /// u can save at once"*: **a character you cannot save is not a save slot.**
    ///
    /// ⚠️⚠️ ONE OWNER, LIKE `PaletteVariants`. The creator screen reads it, character select reads
    /// it, the lobby claim reads it and the match spawn reads it. Four readers and one writer is
    /// the shape that stopped a slipper changing colour per screen (`ToonSkin.ApplySlipper`); four
    /// readers each holding their own copy is the shape that caused it.
    ///
    /// ⚠️ THE PROFILE IS CACHED AND THE SETTINGS FILE IS THE TRUTH. `Reload` exists so a test or a
    /// settings reset can drop the cache; nothing else should need it.
    /// </summary>
    public static class CustomCharacterStore
    {
        private static CustomCharacterProfile _cached;

        /// <summary>
        /// The three slots, decoded from the settings file on first use.
        ///
        /// ⚠️ `EnsureSlots` FILLS ANY SHORTFALL RATHER THAN THE READER CHECKING A COUNT. A fresh
        /// account has no wires at all, an edited settings file can have one, and a build that
        /// once shipped two would have two. All three want the same answer.
        /// </summary>
        public static CustomCharacterProfile Profile
        {
            get
            {
                if (_cached != null) return _cached;

                var settings = Settings.SettingsStore.Current;
                var profile = new CustomCharacterProfile { Slots = new List<CustomCharacter>() };

                if (settings?.CustomCharacterWires != null)
                {
                    for (int i = 0; i < settings.CustomCharacterWires.Count
                                    && i < CustomCharacterRules.MaxSlots; i++)
                    {
                        string wire = settings.CustomCharacterWires[i];
                        if (string.IsNullOrEmpty(wire)) break;
                        profile.Slots.Add(CustomCharacterRules.DecodeWire(wire, i));
                    }
                }

                profile.EnsureSlots();
                profile.ActiveSlot = settings == null ? 0 : settings.ActiveCustomSlot;
                profile.EnsureSlots();

                _cached = profile;
                return _cached;
            }
        }

        /// <summary>Drops the cache so the next read comes off the settings file again.</summary>
        public static void Reload() => _cached = null;

        /// <summary>
        /// Whether the player is bringing their own character into the next match.
        ///
        /// ⚠️ IT IS OFF UNTIL SOMEBODY PRESSES USE THIS CHARACTER, so a fresh account plays as
        /// whoever they picked off the roster and the creator is somewhere you go rather than
        /// somewhere you are put. `CLAUDE.md` § 6.3: a door, not a detour.
        /// </summary>
        public static bool InUse
        {
            get => Settings.SettingsStore.Current?.UseCustomCharacter ?? false;
            set
            {
                var settings = Settings.SettingsStore.Current;
                if (settings == null || settings.UseCustomCharacter == value) return;
                settings.UseCustomCharacter = value;
                Settings.SettingsStore.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Raised whenever a slot, the active slot or <see cref="InUse"/> changes, so the
        /// screens showing a custom character redraw without polling. `PlayerHub` takes the same
        /// shape from `SocialStore.Changed`, and for the same reason: a per-frame rebuild is what
        /// cost `Hud` an eighth of a probe's frames.</summary>
        public static event Action Changed;

        /// <summary>Writes one slot back and saves. The character is CLONED on the way in, so the
        /// caller's working copy cannot keep mutating what is now on disk.</summary>
        public static void SetSlot(int slotIndex, CustomCharacter character)
        {
            var profile = Profile;
            profile.SetSlot(slotIndex, CustomCharacterRules.Normalise(character));
            Persist();
        }

        /// <summary>Which of the three walks into a match.</summary>
        public static void SetActiveSlot(int slotIndex)
        {
            var profile = Profile;
            profile.ActiveSlot = Math.Clamp(slotIndex, 0, CustomCharacterRules.MaxSlots - 1);
            Persist();
        }

        /// <summary>The character that would enter a match right now.</summary>
        public static CustomCharacter Active => Profile.GetActive();

        /// <summary>
        /// The active character as the one string a peer receives, or an empty string when the
        /// player is not using one.
        ///
        /// ⚠️ THE EMPTY STRING IS THE "PLAYING AS A ROSTER CHARACTER" ANSWER AND IT HAS TO BE, so
        /// a receiver on an older build reads a field it does not understand as nothing rather
        /// than as a broken hero. `PaletteRules.IsKnownVariant` records the same rule for palette
        /// ids: an id that does not resolve degrades, it does not blank.
        /// </summary>
        public static string ActiveWire()
            => InUse ? CustomCharacterRules.EncodeWire(Active) : "";

        private static void Persist()
        {
            var profile = Profile;
            var settings = Settings.SettingsStore.Current;
            if (settings == null) return;

            if (settings.CustomCharacterWires == null)
                settings.CustomCharacterWires = new List<string>();

            settings.CustomCharacterWires.Clear();
            for (int i = 0; i < profile.Slots.Count; i++)
                settings.CustomCharacterWires.Add(CustomCharacterRules.EncodeWire(profile.Slots[i]));

            settings.ActiveCustomSlot = profile.ActiveSlot;

            Settings.SettingsStore.Save();
            Changed?.Invoke();
        }
    }
}
