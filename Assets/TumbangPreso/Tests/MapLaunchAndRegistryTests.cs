using NUnit.Framework;
using System.Linq;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class MapLaunchAndRegistryTests
    {
        [Test]
        public void ActiveMapsHaveApprovedNamesAndExcludeTheReworkPool()
        {
            var expected = new[] { SceneFlow.Eskinita, SceneFlow.IlalimNgTulay,
                SceneFlow.LagoonCove, SceneFlow.Kanto, SceneFlow.Arena, SceneFlow.Payo };
            var names = new[] { "TONDO", "ERMITA", "SITANGKAI", "BGC", "THE PHILIPPINE ARENA", "PAYO" };
            CollectionAssert.AreEqual(expected, SceneFlow.Maps);
            Assert.AreEqual(expected.Length, SceneFlow.MapRegistry.Length);
            Assert.AreEqual(expected.Length, GameLaunch.Maps.Length);
            var included = UnityEditor.EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            CollectionAssert.AreEquivalent(expected, included
                .Where(path => path.StartsWith("Assets/TumbangPreso/Scenes/Maps/"))
                .Select(System.IO.Path.GetFileNameWithoutExtension));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], SceneFlow.MapRegistry[i].Id);
                Assert.AreEqual(expected[i], GameLaunch.Maps[i].Scene);
                Assert.AreEqual(names[i], SceneFlow.MapRegistry[i].Name);
                Assert.AreEqual(names[i], GameLaunch.Maps[i].Name);
                string path = "Assets/TumbangPreso/Scenes/Maps/" + expected[i] + ".unity";
                CollectionAssert.Contains(included, path);
                Assert.IsTrue(System.IO.File.Exists(path), expected[i]);
            }
            CollectionAssert.DoesNotContain(included, "Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");
            CollectionAssert.DoesNotContain(included, "Assets/TumbangPreso/Scenes/Maps/SaBubong.unity");
            Assert.IsFalse(included.Any(path => path.Contains("EskinitaClassic")));
        }

        [TestCase("BayanPlaza")]
        [TestCase("bayan_plaza")]
        [TestCase("SaBubong")]
        [TestCase("sa_bubong")]
        [TestCase("EskinitaClassic_2026-10-09")]
        public void RetiredMapsCannotEnterThroughDirectLaunch(string map)
        {
            Assert.Throws<System.ArgumentException>(() => NetBootstrap.AdoptLaunchMap(map));
        }

        [Test]
        public void DirectLaunchPublishesTheSameMapTheHostWillLoad()
        {
            string selected=SceneFlow.SelectedMap,legacy=GameLaunch.SelectedMap;
            try
            {
                foreach(var entry in GameLaunch.Maps)
                {
                    SceneFlow.SelectedMap=SceneFlow.Eskinita;
                    Assert.AreEqual(entry.Scene,NetBootstrap.AdoptLaunchMap(entry.Id));
                    Assert.AreEqual(entry.Scene,SceneFlow.SelectedMap);
                    Assert.AreEqual(entry.Id,GameLaunch.SelectedMap);
                }
            }
            finally{SceneFlow.SelectedMap=selected;GameLaunch.SelectedMap=legacy;}
        }

        [Test]
        public void DestroyedRegisteredPlayerIsNotReturnedToMessageHandlers()
        {
            var go=new GameObject("Registry test");var body=new GameObject("Departed body");
            try
            {
                var round=go.AddComponent<RoundDirector>();var motor=body.AddComponent<CharacterMotor>();int slot=motor.PlayerSlot;
                round.Register(motor);Assert.AreSame(motor,round.PlayerAt(slot));
                Object.DestroyImmediate(body);body=null;
                Assert.IsNull(round.PlayerAt(slot),"A destroyed Unity component is not a live network message target");
            }
            finally{Object.DestroyImmediate(go);if(body!=null)Object.DestroyImmediate(body);}
        }
    }
}
