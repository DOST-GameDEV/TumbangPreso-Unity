using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteDriftPresentationTests
    {
        readonly List<GameObject> _objects=new List<GameObject>();
        GameObject Keep(GameObject go){_objects.Add(go);return go;}
        [UnitySetUp] public IEnumerator Before(){yield return PlayModeWorld.Reset();}
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _objects)if(go!=null)Object.Destroy(go);
            _objects.Clear();yield return PlayModeWorld.Reset();
        }
        DanteDriftVisual Build(Vector3 origin,Vector3 forward,float width=8,float reach=16)
        {
            var parent=Keep(new GameObject("Drift presentation only"));
            return DanteDriftVisual.Build(parent.transform,origin,forward,width,reach);
        }
        static string Geometry(Transform root)
        {
            var text=new StringBuilder();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                foreach(var p in filter.sharedMesh.vertices)
                    text.Append(p.x.ToString("R",CultureInfo.InvariantCulture)).Append(',')
                        .Append(p.z.ToString("R",CultureInfo.InvariantCulture)).Append(';');
            return text.ToString();
        }
        static string State(Transform root)
        {
            var text=new StringBuilder();
            foreach(var node in root.GetComponentsInChildren<Transform>(false))
                text.Append(node.gameObject.activeSelf).Append(node.localPosition.ToString("F6"))
                    .Append(node.localRotation.ToString("F6")).Append(node.localScale.ToString("F6"));
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(false))
                foreach(var colour in filter.sharedMesh.colors)
                    text.Append(colour.a.ToString("R",CultureInfo.InvariantCulture)).Append(';');
            return text.ToString();
        }
        [Test] public void FiveBandsHaveDistinctAuthoredGeometry()
        {
            var view=Build(Vector3.zero,Vector3.forward);
            Assert.AreEqual(GeoRules.DriftBlasts,view.transform.childCount);
            var shapes=new HashSet<string>();
            for(int i=0;i<view.transform.childCount;i++)shapes.Add(Geometry(view.transform.GetChild(i)));
            Assert.AreEqual(5,shapes.Count,"All five bands must have their own branches, not one repeated stamp.");
            Assert.IsEmpty(view.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(view.GetComponentsInChildren<DanteDriftWave>(true));
        }
        [Test] public void AcceptedBandBoundariesAndRewindAreDeterministic()
        {
            var origin=new Vector3(2,.12f,-5);var forward=new Vector3(.6f,0,.8f);
            var view=Build(origin,forward);var right=Vector3.Cross(Vector3.up,forward);
            var random=UnityEngine.Random.state;
            for(int band=0;band<5;band++)
            {
                float boundary=band*GeoRules.DriftInterval;
                view.StepTo(boundary-.001f);Assert.IsFalse(view.transform.GetChild(band).gameObject.activeSelf);
                view.StepTo(boundary+.001f);Assert.IsTrue(view.transform.GetChild(band).gameObject.activeSelf);
            }
            view.StepTo(.73f);string direct=State(view.transform);
            view.StepTo(1.9f);view.StepTo(.73f);Assert.AreEqual(direct,State(view.transform));
            Assert.AreEqual(random,UnityEngine.Random.state,"Sampling must not consume gameplay random state.");
            view.StepTo(GeoRules.DriftSeconds);
            foreach(Transform band in view.transform)Assert.IsFalse(band.gameObject.activeSelf);
            view.StepTo(.2f);
            foreach(var filter in view.GetComponentsInChildren<MeshFilter>(true))
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var offset=filter.transform.TransformPoint(vertex)-origin;
                    Assert.LessOrEqual(Mathf.Abs(Vector3.Dot(offset,right)),8.05f);
                    Assert.That(Vector3.Dot(offset,forward),Is.InRange(-.05f,16.05f));
                }
        }
        [UnityTest] public IEnumerator GeneratedGeometryIsRetiredWithItsWave()
        {
            var view=Build(Vector3.zero,Vector3.forward);
            var meshes=view.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
            Assert.IsNotEmpty(meshes);Object.Destroy(view.gameObject);yield return null;yield return null;
            foreach(var mesh in meshes)Assert.IsTrue(mesh==null,"Runtime fracture mesh survived its owner.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator ActualCourtObserverSeesTheForwardFracture()
        {
            int oldQuality=QualitySettings.GetQualityLevel();
            int low=Array.FindIndex(QualitySettings.names,n=>n.Equals("Low",StringComparison.OrdinalIgnoreCase));
            if(low>=0)QualitySettings.SetQualityLevel(low,true);
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
                Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
                foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
                foreach(var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
                foreach(var actor in GameServices.Round.Players)
                {actor.Intent.Clear();actor.Teleport(new Vector3(5,.12f,-6));actor.enabled=false;}
                var caster=GameServices.Round.PlayerAt(1);caster.Teleport(new Vector3(0,.12f,-6));
                caster.transform.rotation=Quaternion.identity;caster.AbilitySystem.BindHero("dante");
                var art=RosterBook.Load().FindPersonArt("dante");
                caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                var victim=GameServices.Round.PlayerAt(0);victim.Teleport(new Vector3(1,.12f,5));
                var witness=Keep(new GameObject("Fault observer")).AddComponent<Camera>();witness.enabled=false;
                witness.transform.position=new Vector3(5,4,-7);witness.transform.LookAt(new Vector3(0,.4f,2));witness.fieldOfView=65;
                var context=new AbilityContext(caster,caster.GetComponent<Carrier>(),caster.GetComponent<CombatVerbs>());
                var ultimate=caster.AbilitySystem.Kit.Ultimate;ultimate.Activate(context);ultimate.Tick(context,ultimate.Windup+.01f);
                var wave=Object.FindAnyObjectByType<DanteDriftWave>();Assert.IsNotNull(wave);wave.enabled=false;
                string directory=Environment.GetEnvironmentVariable("TUMP_DRIFT_FILM")??"Logs/drift-presentation";
                for(int frame=0;frame<36;frame++)
                {
                    if(frame>0)wave.SendMessage("Advance",1f/15);
                    yield return GameplayShots.Render(witness,"observer-"+frame.ToString("D3"),false,directory,caster,960,540);
                }
                Assert.AreEqual(5,wave.ReleasedBands);Assert.IsTrue(victim.IsConcussed);
                for(int i=0;i<4;i++)
                {
                    float yaw=-18+i*12;var direction=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                    var overlap=Build(caster.transform.position+Vector3.right*(i-.5f),direction,wave.HalfWidth,wave.Reach);
                    overlap.StepTo(.48f+i*.06f);
                }
                yield return GameplayShots.Render(witness,"four-wave-overlap",false,directory,caster,960,540);
            }
            finally{QualitySettings.SetQualityLevel(oldQuality,true);}
        }
    }
}
