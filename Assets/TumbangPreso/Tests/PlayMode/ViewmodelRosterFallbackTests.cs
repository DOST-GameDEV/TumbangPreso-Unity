using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ViewmodelRosterFallbackTests
    {
        static readonly FieldInfo Cached = typeof(RosterBook).GetField("_cached", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo Tried = typeof(RosterBook).GetField("_tried", BindingFlags.Static | BindingFlags.NonPublic);
        readonly List<Object> _owned = new();
        object _priorBook, _priorTried;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _priorBook = Cached.GetValue(null); _priorTried = Tried.GetValue(null);
        }
        [TearDown] public void After()
        {
            Cached.SetValue(null, _priorBook); Tried.SetValue(null, _priorTried);
            foreach (var value in _owned) if (value != null) Object.DestroyImmediate(value);
            _owned.Clear();
        }
        T Own<T>(T value) where T : Object { _owned.Add(value); return value; }
        [TestCase("zack", "missing")]
        [TestCase("rafi", "missing")]
        [TestCase("zack", "null-list")]
        [TestCase("zack", "null-row")]
        [TestCase("zack", "matched-control")]
        public void CatalogFailuresKeepTheAuthoredIdentityFallback(string hero, string state)
        {
            var art = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            Assert.IsNotNull(art); Assert.IsNotNull(art.Model);
            var actor = Own(new GameObject("Roster fallback actor"));
            var motor = actor.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.Mode = GameMode.HeroStrike; motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            var visual = actor.AddComponent<CharacterVisual>();
            var root = new GameObject("Visual").transform; root.SetParent(actor.transform, false);
            visual.SetModelRoot(root); visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            RosterBook book = null;
            if (state != "missing")
            {
                book = Own(ScriptableObject.CreateInstance<RosterBook>());
                if (state == "null-list") book.People = null;
                else if (state == "null-row") book.People.Add(null);
                else
                {
                    var row = Own(ScriptableObject.CreateInstance<RosterEntryAsset>());
                    row.Id = "rafi"; row.Model = art.Model; book.People.Add(row);
                }
            }
            Cached.SetValue(null, book); Tried.SetValue(null, true);
            var owner = Own(new GameObject("Roster fallback arms")); owner.AddComponent<Camera>();
            var arms = owner.AddComponent<ViewmodelArms>();
            Assert.DoesNotThrow(() => arms.MatchCharacter(motor));
            var identity = typeof(ViewmodelArms).GetField("_currentHeroId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arms);
            Assert.AreEqual(state == "matched-control" ? "rafi" : hero, identity);
        }
    }
}
