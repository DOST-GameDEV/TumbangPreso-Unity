using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ThrowEquipmentClearanceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);

        [Test]
        public void EveryRetainedPersonCanPrepareEveryEquipmentWithoutEnteringTheHead()
            => Assert.IsEmpty(Review(), "Equipment entered an actual rigid head surface.");

        private static List<string> Review()
        {
            var book = RosterBook.Load();
            var rows = new List<string> { "person,shoe,worst_vertices,charge,spin" };
            var failures = new List<string>();
            foreach (var person in Roster.AllPeople)
            {
                var art = book.People.First(item => item.Id == person.Id);
                var actor = new GameObject("Equipment head clearance " + person.Id);
                CharacterAnimator driver = null;
                try
                {
                    var motor = actor.AddComponent<CharacterMotor>(); Call(motor, "Awake");
                    var carrier = actor.AddComponent<Carrier>(); Call(carrier, "Awake");
                    var visual = actor.AddComponent<CharacterVisual>(); Call(visual, "Awake");
                    visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette);
                    driver = actor.GetComponent<CharacterAnimator>(); Call(driver, "Awake");
                    var graph = (PlayableGraph)typeof(CharacterAnimator).GetField("_graph", Private).GetValue(driver);
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var head = new HeadSurfaceVolume(visual.Model.transform);
                    for (int index = 0; index < Roster.Slippers.Count; index++)
                    {
                        var prop = new GameObject("Equipment " + Roster.Slippers[index].Id);
                        try
                        {
                            var model = Object.Instantiate(book.SlipperArt(index).Model, prop.transform);
                            var shoe = prop.AddComponent<Slipper>(); shoe.SkinIndex = index;
                            var filter = model.GetComponentInChildren<MeshFilter>();
                            Assert.IsNotNull(filter);
                            var vertices = filter.sharedMesh.vertices;
                            carrier.NotifyEquipped(shoe);
                            int worst = 0; float worstCharge = 0, worstSpin = 0;
                            foreach (float charge in new[] { 0f, .05f, .15f, .35f, .65f, 1f })
                            foreach (float spin in new[] { -1f, 0f, 1f })
                            {
                                Call(driver, "RestoreChargeOffsets");
                                Call(driver, "Play", "holding-right", true, true);
                                Set(driver, "_weight", 1f); Call(driver, "Blend");
                                carrier.ApplyObservedCharge(true, charge * Balance.ChargeFullTime, spin);
                                Call(driver, "StepChargePose"); graph.Evaluate(0);
                                Call(driver, "LateUpdate"); Call(carrier, "RideAnchor");
                                var toHead = head.Bone.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                                int inside = 0;
                                foreach (var vertex in vertices)
                                    if (head.Contains(toHead.MultiplyPoint3x4(vertex))) inside++;
                                if (inside > worst) { worst = inside; worstCharge = charge; worstSpin = spin; }
                            }
                            rows.Add(FormattableString.Invariant($"{person.Id},{Roster.Slippers[index].Id},{worst},{worstCharge},{worstSpin}"));
                            if (worst > 0) failures.Add($"{person.Id}/{Roster.Slippers[index].Id}: {worst} at charge {worstCharge}, spin {worstSpin}");
                            carrier.NotifyEquipped(null);
                        }
                        finally { Object.DestroyImmediate(prop); }
                    }
                }
                finally
                {
                    if (driver != null) Call(driver, "ReleaseGraph");
                    Object.DestroyImmediate(actor);
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/equipment-head-surface-clearance.csv", rows);
            return failures;
        }

        private static (Transform Bone, Bounds Box) HeadVolume(Transform model)
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var head = skins.SelectMany(skin => skin.bones).First(bone => bone != null && bone.name == "head");
            bool found = false; var box = new Bounds();
            foreach (var skin in skins)
            {
                int bone = Array.IndexOf(skin.bones, head); if (bone < 0) continue;
                var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    float influence = (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                        + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
                    if (influence < .5f) continue;
                    var point = mesh.bindposes[bone].MultiplyPoint3x4(vertices[i]);
                    if (!found) { box = new Bounds(point, Vector3.zero); found = true; } else box.Encapsulate(point);
                }
            }
            Assert.True(found); box.Expand(-.006f); return (head, box);
        }
    }
}
