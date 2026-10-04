using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;

namespace TumbangPreso.Tests
{
    public sealed class CustomCharacterSlotRecoveryTests
    {
        private GameSettings _previous;
        [SetUp] public void Before() { _previous = SettingsStore.Current; }
        [TearDown] public void After()
        {
            SettingsStore.OverrideForTests(_previous);
            CustomCharacterStore.Reload();
        }

        private static string Saved(int slot) => CustomCharacterRules.EncodeWire(
            new CustomCharacter { Name = "Saved " + slot, TopColorIndex = slot + 3 });

        [TestCase(0, null)]
        [TestCase(0, "")]
        [TestCase(1, null)]
        [TestCase(1, "")]
        public void MissingSlotPreservesLaterSavedCharacters(int missing, string empty)
        {
            var wires = new List<string> { Saved(0), Saved(1), Saved(2) };
            wires[missing] = empty;
            SettingsStore.OverrideForTests(new GameSettings { CustomCharacterWires = wires, ActiveCustomSlot = 2 });
            CustomCharacterStore.Reload();
            var loaded = CustomCharacterStore.Profile;
            Assert.AreEqual("Saved 2", loaded.GetActive().Name, "A missing earlier wire must not discard the active saved character.");
            for (int slot = 0; slot < 3; slot++)
            {
                var expected = slot == missing ? new CustomCharacterProfile().Slots[slot] : CustomCharacterRules.DecodeWire(wires[slot], slot);
                Assert.AreEqual(CustomCharacterRules.EncodeWire(expected), CustomCharacterRules.EncodeWire(loaded.Slots[slot]), "Slot " + slot);
            }
            CollectionAssert.AreEqual(wires, SettingsStore.Current.CustomCharacterWires, "Loading must not rewrite the stored slot list.");
        }

        [Test]
        public void CompleteSavedProfileRetainsAllSlots()
        {
            var wires = new List<string> { Saved(0), Saved(1), Saved(2) };
            SettingsStore.OverrideForTests(new GameSettings { CustomCharacterWires = wires, ActiveCustomSlot = 1 });
            CustomCharacterStore.Reload();
            var profile = CustomCharacterStore.Profile;
            Assert.AreEqual("Saved 1", profile.GetActive().Name);
            for (int slot = 0; slot < 3; slot++) Assert.AreEqual(wires[slot], CustomCharacterRules.EncodeWire(profile.Slots[slot]));
        }

        [Test]
        public void ShortProfileKeepsDistinctDefaultSlots()
        {
            SettingsStore.OverrideForTests(new GameSettings { CustomCharacterWires = new List<string> { Saved(0) } });
            CustomCharacterStore.Reload();
            var profile = CustomCharacterStore.Profile;
            var defaults = new CustomCharacterProfile();
            Assert.AreEqual(Saved(0), CustomCharacterRules.EncodeWire(profile.Slots[0]));
            for (int slot = 1; slot < 3; slot++) Assert.AreEqual(CustomCharacterRules.EncodeWire(defaults.Slots[slot]), CustomCharacterRules.EncodeWire(profile.Slots[slot]));
        }
    }
}
