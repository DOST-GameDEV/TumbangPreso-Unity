using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// `CLAUDE.md` § 6.4 as a test for the UX-1 palette: no colour in `HubStyle` has more blue in it
    /// than red, so no blue, navy or cold grey can reach the hub's chrome. `UiTheme.Defense` is the
    /// one blue in the project and it is not, and must never be, in this list.
    /// </summary>
    public sealed class HubPaletteTests
    {
        [Test]
        public void NoHubColourIsBlueOrNavyOrCold()
        {
            foreach (var field in typeof(HubStyle).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(Color)) continue;
                var c = (Color)field.GetValue(null);
                Assert.LessOrEqual(c.b, c.r, "HubStyle." + field.Name + " has more blue than red (CLAUDE.md § 6.4).");
            }
            foreach (var property in typeof(HubStyle).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType != typeof(Color)) continue;
                var c = (Color)property.GetValue(null);
                Assert.LessOrEqual(c.b, c.r, "HubStyle." + property.Name + " has more blue than red (CLAUDE.md § 6.4).");
            }
        }

        [Test]
        public void TheTypeFloorIsTheBriefs()
        {
            Assert.AreEqual(28, HubStyle.Floor);
            Assert.GreaterOrEqual(HubStyle.Size(1), 28, "Size() must never draw under the floor.");
        }
    }
}
