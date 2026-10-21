using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class LooseSlipperSurfaceTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator RestingSupportIsStableAcrossHeldAndTumblingRootRotations()
        {
            yield return MapRetrievalProbe.Load("IlalimNgTulay", GameMode.HeroStrike);
            foreach(var actor in GameServices.Round.Players){actor.Intent.Clear();actor.Intent.Parked=true;}
            var book=RosterBook.Load();
            var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.OwnerSlot==1);
            shoe.HostBeginMapRecovery();
            foreach(var renderer in shoe.GetComponentsInChildren<MeshRenderer>(true))Object.Destroy(renderer.gameObject);
            yield return null;
            foreach(var entry in Roster.Slippers)
            {
                var model=Object.Instantiate(book.Slippers.First(a=>a.Id==entry.Id).Model,shoe.transform);
                model.name="Visual";
                shoe.HostFinishMapRecoveryAt(new Vector3(0,1,0));yield return null;
                float upright=shoe.RestHeight;
                foreach(var rotation in new[]{Quaternion.Euler(65,25,15),Quaternion.Euler(0,45,90),Quaternion.Euler(170,120,80)})
                {
                    shoe.transform.rotation=rotation;
                    Assert.That(shoe.RestHeight,Is.EqualTo(upright).Within(.0001f),entry.Id+" held/flight pose must not change resting support");
                }
                shoe.transform.rotation=Quaternion.identity;
                Object.Destroy(model);yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NewMapLooseShoesRestOnActualCollisionSurfaces()
        {
            var rows = new List<string>{"map,shoe,min_visible_mesh_to_ground_m,vertices"};
            var failures = new List<string>();
            int pairs = 0;
            foreach (string map in new[]{"IlalimNgTulay","LagoonCove","Kanto"})
            {
                yield return MapRetrievalProbe.Load(map, GameMode.HeroStrike);
                foreach (var actor in GameServices.Round.Players)
                { actor.Intent.Clear(); actor.Intent.Parked=true; }
                var book=RosterBook.Load();
                var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.OwnerSlot==1);
                shoe.HostBeginMapRecovery();
                foreach(var renderer in shoe.GetComponentsInChildren<MeshRenderer>(true))
                    Object.Destroy(renderer.gameObject);
                yield return null;
                foreach(var entry in Roster.Slippers)
                {
                    var art=book.Slippers.First(a=>a.Id==entry.Id);
                    var model=Object.Instantiate(art.Model,shoe.transform);
                    shoe.SkinIndex=Roster.IndexIn(Roster.Slippers,entry.Id);
                    ToonSkin.ApplySlipper(model,ToonSkin.PropOutlineWidth);
                    shoe.HostFinishMapRecoveryAt(new Vector3(0,1,0));
                    yield return null;
                    Assert.AreEqual(SlipperState.Loose,shoe.State,map+"/"+entry.Id);
                    float gap=float.PositiveInfinity;int count=0;
                    foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 at=filter.transform.TransformPoint(vertex);
                        var hits=Physics.RaycastAll(at+Vector3.up*.25f,Vector3.down,1f,~0,QueryTriggerInteraction.Ignore)
                            .Where(h=>h.collider.GetComponentInParent<CharacterMotor>()==null
                                &&h.collider.GetComponentInParent<Slipper>()==null
                                &&h.collider.GetComponentInParent<Lata>()==null).OrderBy(h=>h.distance).ToArray();
                        Assert.IsNotEmpty(hits,map+"/"+entry.Id+" must have a real support surface");
                        gap=Mathf.Min(gap,at.y-hits[0].point.y);count++;
                    }
                    pairs++;rows.Add(FormattableString.Invariant($"{map},{entry.Id},{gap:F6},{count}"));
                    if(count==0||Mathf.Abs(gap)>.003f)failures.Add(map+"/"+entry.Id+": "+gap.ToString("F6")+" m");
                    Object.Destroy(model);yield return null;
                }
                yield return PlayModeWorld.Reset();
            }
            string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/loose-slipper-surface";
            Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"ground-contact.csv"),rows);
            Assert.AreEqual(Roster.Slippers.Count()*3,pairs);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
