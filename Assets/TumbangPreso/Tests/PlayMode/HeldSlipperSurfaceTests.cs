using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class HeldSlipperSurfaceTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [DefaultExecutionOrder(10000)]
        private sealed class SurfaceWitness : MonoBehaviour
        {
            public Action Read;
            private void LateUpdate() => Read?.Invoke();
        }

        [UnityTest]
        public IEnumerator CurrentHeroesHoldEverySelectableShoeOnTheirVisiblePalmSurface()
        {
            yield return (IEnumerator)typeof(UltimateIntroductionProbe)
                .GetMethod("BuildHeroArtStage", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { "dante" });
            var actor = GameServices.Round.PlayerAt(1);
            var carrier = actor.GetComponent<Carrier>();
            var visual = actor.GetComponent<CharacterVisual>();
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            var book = RosterBook.Load();
            Camera.main.GetComponent<TumbangPreso.CameraSystem.CameraRig>().SetActive(false);
            foreach (var renderer in shoe.GetComponentsInChildren<MeshRenderer>())
                Object.Destroy(renderer.gameObject);
            yield return null;
            var rows = new List<string> { "hero,shoe,min_vertex_palm_plane_gap_m,mesh_vertices" };
            var failures = new List<string>();
            var witness = actor.gameObject.AddComponent<SurfaceWitness>();
            foreach (var hero in Roster.HeroPeople)
            {
                var person = book.FindPersonArt(hero.Id);
                visual.ApplyModel(person.Model, person.Tint, person.Clips, person.Palette, person.PetModel);
                actor.Intent.Clear(); actor.Intent.Parked = false;
                yield return null;
                foreach (var item in Roster.Slippers)
                {
                    var art = book.Slippers.First(a => a.Id == item.Id);
                    var model = Object.Instantiate(art.Model, shoe.transform);
                    shoe.SkinIndex = Roster.IndexIn(Roster.Slippers, item.Id);
                    ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                    var hand = visual.HandAnchor;
                    Assert.IsNotNull(hand, hero.Id);
                    Assert.AreSame(shoe, carrier.Held, "Actual owned shoe must stay held.");
                    // Inspect the actual rendered mesh. A follow-anchor check using
                    // CarrySupportExtent would repeat the implementation and miss a float.
                    float gap = float.PositiveInfinity; int count = 0;
                    bool sampled = false;
                    witness.Read = () =>
                    {
                        if (sampled) return;
                        foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                        {
                            Assert.IsNotNull(filter.sharedMesh, item.Id);
                            foreach (var vertex in filter.sharedMesh.vertices)
                            {
                                gap = Mathf.Min(gap, Vector3.Dot(filter.transform.TransformPoint(vertex) - hand.position, hand.up));
                                count++;
                            }
                        }
                        sampled = true;
                    };
                    yield return null; yield return null;
                    witness.Read = null;
                    Assert.IsTrue(sampled, "Read the rendered pose after its actual carry update.");
                    rows.Add(FormattableString.Invariant($"{hero.Id},{item.Id},{gap:F6},{count}"));
                    // The anchor already includes 3 mm of visible palm clearance.
                    // No shoe needs another centimetre of space above that anchor.
                    if (count == 0 || gap < -.003f || gap > .003f)
                    {
                        failures.Add(FormattableString.Invariant($"{hero.Id}/{item.Id}: actual sole gap {gap:F6}m ({count} vertices)"));
                        if (failures.Count <= 3)
                            typeof(SlipperContactTests).GetMethod("CaptureHand", BindingFlags.NonPublic | BindingFlags.Static)
                                .Invoke(null, new object[] { hero.Id + "-" + item.Id + "-sole-gap", hand });
                    }
                    Object.Destroy(model); yield return null;
                }
            }
            var output = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/slipper-all-skins";
            Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output, "actual-sole-contact.csv"), rows);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
