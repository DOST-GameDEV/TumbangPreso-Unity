using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed partial class AmbientLifeProbe
    {
        private bool _bots,_spectator,_pinned;private int _seat,_quality;private CustomRules _rules;
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_AMBIENT_REVIEW")??"Logs/ambient-life-v1";
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;_quality=GraphicsProfiles.Current;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GraphicsProfiles.Apply(_quality);
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator EskinitaDogChoosesActivitiesAndRecoversFromIntrusion()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="aspin-tan");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            // The later separately authored tabby now also has a habitat.
            Assert.IsFalse(spec.QuietCat,"The dog must retain its own activity presentation");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var dog=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Dog activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=dog;
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy dog comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;dog.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=spec.WalkSpeed,RunSpeed=spec.RunSpeed,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Eskinita-dog-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-dog-before",6);
                Object.Destroy(legacyRoot);yield return null;
                dog.gameObject.SetActive(true);life.enabled=true;follow.Target=dog;
                yield return GameplayShots.Render(camera,"Eskinita-dog-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Eskinita-dog-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(dog.position+Vector3.right*2.5f);
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                Vector3 last=dog.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,dog.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=dog.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(dog.position.x,Is.InRange(-8.2f,-6.0f));
                    var p=dog.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-dog-activities",12);
                Assert.Greater(travelled,1,"Dog never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Dog snapped between habitat locations");
                var before=dog.position;observer.Teleport(before+Vector3.right*1.1f);
                yield return new WaitForSeconds(1.5f);
                Assert.Greater(Vector3.Distance(before,dog.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-dog-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=dog.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,dog.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Eskinita dog] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"eskinita-dog-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator BayanCreamAspinUsesItsPlazaHabitat()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="aspin-cream");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            Assert.IsFalse(spec.QuietCat,"The dog must retain its own activity presentation");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var dog=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Dog activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=dog;
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy dog comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;dog.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=.45f,RunSpeed=spec.RunSpeed,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Bayan-dog-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-dog-before",6);
                Object.Destroy(legacyRoot);yield return null;
                dog.gameObject.SetActive(true);life.enabled=true;follow.Target=dog;
                yield return GameplayShots.Render(camera,"Bayan-dog-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Bayan-dog-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(dog.position+Vector3.right*2.5f);
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                Vector3 last=dog.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,dog.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=dog.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(dog.position.x,Is.InRange(-10.0f,-7.6f));
                    var p=dog.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-dog-activities",12);
                Assert.Greater(travelled,1,"Dog never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Dog snapped between habitat locations");
                var before=dog.position;observer.Teleport(before+Vector3.right*1.1f);
                yield return new WaitForSeconds(1.5f);
                Assert.Greater(Vector3.Distance(before,dog.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-dog-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=dog.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,dog.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Bayan dog] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"bayan-dog-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator IlalimPatchedAspinUsesItsStorefrontHabitat()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="aspin-patched");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            Assert.IsFalse(spec.QuietCat,"The dog must retain its own activity presentation");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var dog=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Dog activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=dog;
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy dog comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;dog.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=.45f,RunSpeed=spec.RunSpeed,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Ilalim-dog-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-dog-before",6);
                Object.Destroy(legacyRoot);yield return null;
                dog.gameObject.SetActive(true);life.enabled=true;follow.Target=dog;
                yield return GameplayShots.Render(camera,"Ilalim-dog-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Ilalim-dog-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(dog.position+Vector3.right*2.5f);
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                Vector3 last=dog.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,dog.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=dog.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(dog.position.x,Is.InRange(-10.9f,-8.8f));
                    var p=dog.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-dog-activities",12);
                Assert.Greater(travelled,1,"Dog never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Dog snapped between habitat locations");
                var before=dog.position;observer.Teleport(before+Vector3.right*1.1f);
                yield return new WaitForSeconds(1.5f);
                Assert.Greater(Vector3.Distance(before,dog.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-dog-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=dog.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,dog.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Ilalim dog] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"ilalim-dog-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator EskinitaTabbyInvestigatesWatchesAndRetreats()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="pusakal-tabby");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            // The later separately authored tabby now also has a habitat.
            Assert.IsTrue(spec.QuietCat,"This cat must use its own quiet tail and timings");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var cat=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Cat activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=cat;follow.Offset=new Vector3(-1.7f,.9f,1.7f);
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy cat comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;cat.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=.32f,RunSpeed=1.8f,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Eskinita-cat-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-cat-before",6);
                Object.Destroy(legacyRoot);yield return null;
                cat.gameObject.SetActive(true);life.enabled=true;follow.Target=cat;
                yield return GameplayShots.Render(camera,"Eskinita-cat-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Eskinita-cat-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(cat.position+Vector3.left*2.5f);
                Assert.Greater(Vector3.Distance(observer.transform.position,cat.position),2.3f,"Stationary observer staging must survive player confinement");
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                int watch=Array.FindIndex(spec.Activities,site=>site.Kind==AmbientLife.Activity.Watch);
                life.StageActivityForReview(spec.Id,watch);yield return new WaitForSeconds(.6f);
                StringAssert.Contains("activity=Watch",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Eskinita-cat-watches",false,Output,width:960,height:540);
                life.StageActivityForReview(spec.Id,investigate);yield return new WaitForSeconds(.3f);
                Vector3 last=cat.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,cat.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=cat.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(cat.position.x,Is.InRange(6.0f,8.2f));
                    var p=cat.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-cat-activities",12);
                Assert.Greater(travelled,.75f,"Cat never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Cat snapped between habitat locations");
                var before=cat.position;observer.Teleport(before+Vector3.left*1.1f);
                yield return new WaitForSeconds(2.1f);
                Assert.Greater(Vector3.Distance(before,cat.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-cat-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=cat.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,cat.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Eskinita cat] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"eskinita-cat-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator BayanGingerCatUsesItsPavingAndWatchSites()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="pusakal-ginger");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            Assert.IsTrue(spec.QuietCat,"This cat must use its own quiet tail and timings");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var cat=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Cat activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=cat;follow.Offset=new Vector3(-1.7f,.9f,1.7f);
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy cat comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;cat.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=.32f,RunSpeed=1.8f,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Bayan-cat-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-cat-before",6);
                Object.Destroy(legacyRoot);yield return null;
                cat.gameObject.SetActive(true);life.enabled=true;follow.Target=cat;
                yield return GameplayShots.Render(camera,"Bayan-cat-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Bayan-cat-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(cat.position+Vector3.left*2.5f);
                Assert.Greater(Vector3.Distance(observer.transform.position,cat.position),2.3f,"Stationary observer staging must survive player confinement");
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                int watch=Array.FindIndex(spec.Activities,site=>site.Kind==AmbientLife.Activity.Watch);
                life.StageActivityForReview(spec.Id,watch);yield return new WaitForSeconds(.6f);
                StringAssert.Contains("activity=Watch",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Bayan-cat-watches",false,Output,width:960,height:540);
                life.StageActivityForReview(spec.Id,investigate);yield return new WaitForSeconds(.3f);
                Vector3 last=cat.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,cat.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=cat.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(cat.position.x,Is.InRange(7.6f,10.0f));
                    var p=cat.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-cat-activities",12);
                Assert.Greater(travelled,.75f,"Cat never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Cat snapped between habitat locations");
                var before=cat.position;observer.Teleport(before+Vector3.left*1.1f);
                yield return new WaitForSeconds(2.1f);
                Assert.Greater(Vector3.Distance(before,cat.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Bayan-cat-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=cat.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,cat.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Bayan cat] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"bayan-cat-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator IlalimTuxedoCatUsesItsStorefrontWatchSites()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=Object.FindFirstObjectByType<AmbientLife>();
            var spec=life.Animals.Single(a=>a.Id=="pusakal-tuxedo");
            Assert.Greater(spec.Habitat.Length,8);Assert.GreaterOrEqual(spec.Activities.Length,3);
            Assert.IsTrue(spec.QuietCat,"This cat must use its own quiet tail and timings");
            Assert.IsTrue(life.Animals.Where(a=>a.Bird).All(a=>a.Habitat.Length==0));
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-10));
            var cat=life.transform.Find("Ambient "+spec.Id);
            var camera=new GameObject("Cat activity witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=cat;follow.Offset=new Vector3(-1.7f,.9f,1.7f);
            var samples=new StringBuilder("seconds,x,y,z,state\n");
            var legacyRoot=new GameObject("Legacy cat comparison");
            try
            {
                // Same scene, source model and follow camera. The legacy animal
                // is a temporary witness, not a new shipped scene population.
                life.enabled=false;cat.gameObject.SetActive(false);
                var legacy=legacyRoot.AddComponent<AmbientLife>();
                legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,
                    WalkSpeed=.32f,RunSpeed=1.8f,WalkCycleSpeed=spec.WalkCycleSpeed,RunCycleSpeed=spec.RunCycleSpeed,
                    PeeWaypoint=spec.PeeWaypoint,PeeTarget=spec.PeeTarget}};
                yield return null;follow.Target=legacyRoot.transform.Find("Ambient "+spec.Id);
                yield return GameplayShots.Render(camera,"Ilalim-cat-before",false,Output,width:960,height:540);
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-cat-before",6);
                Object.Destroy(legacyRoot);yield return null;
                cat.gameObject.SetActive(true);life.enabled=true;follow.Target=cat;
                yield return GameplayShots.Render(camera,"Ilalim-cat-after",false,Output,width:960,height:540);
                int investigate=Array.FindIndex(spec.Activities,s=>s.Kind==AmbientLife.Activity.Investigate);
                Assert.GreaterOrEqual(investigate,0);life.StageActivityForReview(spec.Id,investigate);
                yield return new WaitForSeconds(.8f);
                StringAssert.Contains("activity=Investigate",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Ilalim-cat-investigates",false,Output,width:960,height:540);
                var observer=GameServices.Round.PlayerAt(1);observer.Teleport(cat.position+Vector3.left*2.5f);
                Assert.Greater(Vector3.Distance(observer.transform.position,cat.position),2.3f,"Stationary observer staging must survive player confinement");
                yield return new WaitForSeconds(.4f);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                observer.Teleport(new Vector3(0,.1f,-10));
                int watch=Array.FindIndex(spec.Activities,site=>site.Kind==AmbientLife.Activity.Watch);
                life.StageActivityForReview(spec.Id,watch);yield return new WaitForSeconds(.6f);
                StringAssert.Contains("activity=Watch",life.DescribeForReview(spec.Id));
                yield return GameplayShots.Render(camera,"Ilalim-cat-watches",false,Output,width:960,height:540);
                life.StageActivityForReview(spec.Id,investigate);yield return new WaitForSeconds(.3f);
                Vector3 last=cat.position;float travelled=0,maxStep=0;
                follow.Sample=()=>
                {
                    float step=Vector3.Distance(last,cat.position);travelled+=step;maxStep=Mathf.Max(maxStep,step);last=cat.position;
                    // A node/edge authored over this surface must remain supported.
                    Assert.That(cat.position.x,Is.InRange(8.8f,10.9f));
                    var p=cat.position;samples.AppendLine(FormattableString.Invariant($"{Time.time:F3},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(spec.Id)}"));
                };
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-cat-activities",12);
                // This compact pavement has a valid 0.707m trip. The other cats'
                // fixed 0.75m minimum would reject reaching that nearby watch.
                var investigateAt=spec.Habitat[spec.Activities[investigate].Node].Point;
                float shortestTrip=spec.Activities.Where(site=>site!=spec.Activities[investigate]).Min(site=>Vector3.Distance(investigateAt,spec.Habitat[site.Node].Point));
                Assert.Greater(travelled,Mathf.Min(.75f,shortestTrip*.6f),"Cat never left its investigation for another activity");
                Assert.Less(maxStep,.35f,"Cat snapped between habitat locations");
                var before=cat.position;observer.Teleport(before+Vector3.left*1.1f);
                yield return new WaitForSeconds(2.1f);
                Assert.Greater(Vector3.Distance(before,cat.position),.55f,"Close intrusion did not cause retreat");
                observer.Teleport(new Vector3(0,.1f,-10));
                yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-cat-recovery",6);
                StringAssert.Contains("panic=0.00",life.DescribeForReview(spec.Id));
                Time.timeScale=0;yield return null;var paused=cat.position;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(paused,cat.position),.0001f);
                Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
                Debug.Log("[Ilalim cat] activity departure, stationary observer, intrusion retreat/recovery and pause passed; travel="+travelled.ToString("F2"));
            }
            finally
            {
                Time.timeScale=1;Object.Destroy(camera.gameObject);if(legacyRoot!=null)Object.Destroy(legacyRoot);
                File.WriteAllText(Path.Combine(Output,"ilalim-cat-activity.csv"),samples.ToString());
            }
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator LagoonBirdsFlyGlideAndPause()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);Time.timeScale=1;GraphicsProfiles.Apply(1);
            var life=GameObject.Find("Lagoon/Coastal bird life").GetComponent<AmbientLife>();
            Assert.AreEqual(3,life.Animals.Length);Assert.IsTrue(life.Animals.All(a=>a.Bird&&a.AerialWander));
            foreach(var spec in life.Animals)life.StageBirdVisitForReview(spec.Id);
            var birds=life.Animals.Select(a=>life.transform.Find("Ambient "+a.Id)).ToArray();
            Assert.IsEmpty(life.GetComponentsInChildren<Collider>());
            var last=birds.Select(t=>t.position).ToArray();var travel=new float[birds.Length];
            var wing=birds[0].GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="WingL");Assert.IsNotNull(wing);
            var firstWing=wing.localRotation;float wingMotion=0;
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            bool close=true,flap=false,glide=false;
            Camera.CameraCallback pin=cam=>
            {
                if(cam!=camera)return;var at=birds[0].position+Vector3.up*.16f;
                var eye=close?at+birds[0].forward*1.9f+birds[0].right*1.1f+Vector3.up*.65f:new Vector3(2,1.9f,3);
                cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));cam.fieldOfView=close?45:70;
            };
            Camera.onPreCull+=pin;var report=new StringBuilder("time,id,x,y,z,state\n");
            try
            {
                float end=Time.time+12,nextSample=0;
                while(Time.time<end)
                {
                    yield return null;
                    for(int i=0;i<birds.Length;i++)
                    {
                        var point=birds[i].position;travel[i]+=Vector3.Distance(point,last[i]);last[i]=point;
                        Assert.IsTrue(life.Animals[i].FlightBounds.Contains(point),life.Animals[i].Id+" left its aerial habitat");
                    }
                    wingMotion=Mathf.Max(wingMotion,Quaternion.Angle(firstWing,wing.localRotation));
                    string state=life.DescribeForReview(life.Animals[0].Id);
                    if(Time.time>=nextSample)
                    {nextSample=Time.time+.25f;for(int i=0;i<birds.Length;i++){var p=birds[i].position;report.AppendLine(FormattableString.Invariant($"{Time.time:F3},{life.Animals[i].Id},{p.x:F3},{p.y:F3},{p.z:F3},{life.DescribeForReview(life.Animals[i].Id)}"));}}
                    if(!flap&&state.Contains("glide=False"))
                    {yield return GameplayShots.Render(camera,"Lagoon-bird-flap",false,Output,width:960,height:720);flap=true;}
                    if(!glide&&state.Contains("glide=True"))
                    {yield return new WaitForSeconds(.3f);yield return GameplayShots.Render(camera,"Lagoon-bird-glide",false,Output,width:960,height:720);glide=true;}
                }
                Assert.IsTrue(flap&&glide,"Actual wingbeat and glide states were not both observed");
                Assert.Greater(wingMotion,15,"Wing clip did not animate");
                for(int i=0;i<travel.Length;i++)Assert.Greater(travel[i],8,life.Animals[i].Id+" remained stationary");
                close=false;yield return GameplayShots.Render(camera,"Lagoon-birds-world-scale",false,Output,width:1280,height:720);
                Time.timeScale=0;yield return null;var paused=birds.Select(t=>t.position).ToArray();
                yield return new WaitForSecondsRealtime(.25f);
                for(int i=0;i<birds.Length;i++)Assert.Less(Vector3.Distance(paused[i],birds[i].position),.0001f,"Paused bird moved");
                Debug.Log("[Lagoon birds] native wing motion "+wingMotion+", travelled "+string.Join(",",travel.Select(x=>x.ToString("F2")))+"m; pause and habitat bounds held.");
            }
            finally{Time.timeScale=1;Camera.onPreCull-=pin;File.WriteAllText(Path.Combine(Output,"lagoon-bird-flight.csv"),report.ToString());}
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator OriginalAnimalsRenderAnimateAndReactOnTheirMaps()
        {
            var report=new StringBuilder("map,id,width,height,depth,start_x,start_y,start_z,end_x,end_y,end_z,travel,player_x,player_y,player_z,state\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();Assert.IsNotNull(life,map+" has no ambient integration");
                    Assert.AreEqual(map==SceneFlow.SaBubong?3:5,life.Animals.Length);
                    var who=GameServices.Round.PlayerAt(1);var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    var witness=new GameObject("Ambient review camera").AddComponent<Camera>();
                    witness.enabled=false;witness.nearClipPlane=.04f;witness.farClipPlane=400;witness.fieldOfView=52;
                    witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    try
                    {
                        foreach(var spec in life.Animals.Where(a=>!a.Bird))
                        {
                            var animal=life.transform.Find("Ambient "+spec.Id);Assert.IsNotNull(animal);
                            var start=animal.position;
                            var renderers=animal.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(renderers);
                            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                            Assert.That(bounds.size.y,Is.InRange(.35f,1.1f),"Animal scale is outside the intended street cast relationship");
                            var inward=new Vector3(-Mathf.Sign(start.x),0,0);
                            var playerAt=start+inward*3.8f;playerAt.y=Slipper.GroundY(playerAt);who.Teleport(playerAt);
                            var view=start+animal.forward*2+inward*.9f+Vector3.up*.9f;
                            witness.transform.SetPositionAndRotation(view,Quaternion.LookRotation(start+Vector3.up*.4f-view));
                            yield return GameplayShots.Render(witness,map+"-"+spec.Id+"-context",false,Output);
                            var input=who.gameObject.AddComponent<ApproachInput>();who.Intent.Parked=false;input.Move=-new Vector2(inward.x,inward.z);
                            // Carrying has acceleration and movement penalties.
                            // A fixed .4s approach stopped3.24m away and never
                            // entered the animal's3m response radius.
                            float until=Time.time+3;
                            while(Time.time<until&&Vector3.Distance(who.transform.position,start)>2.45f)yield return null;
                            input.Move=Vector2.zero;
                            Assert.Less(Vector3.Distance(who.transform.position,start),2.7f,"The staged player approach never reached the response zone");
                            yield return new WaitForSeconds(1.6f);input.enabled=false;Object.Destroy(input);who.Intent.Clear();
                            var end=animal.position;
                            var player=who.transform.position;
                            report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3},{start.x:F3},{start.y:F3},{start.z:F3},{end.x:F3},{end.y:F3},{end.z:F3},{Vector3.Distance(start,end):F3},{player.x:F3},{player.y:F3},{player.z:F3},{life.DescribeForReview(spec.Id)}"));
                            Assert.Greater(Vector3.Distance(start,end),.7f,map+"/"+spec.Id+" did not move away during the approach; "+life.DescribeForReview(spec.Id)+" player="+player);
                            yield return GameplayShots.Render(rig.Camera,map+"-"+spec.Id+"-owner-after-approach",false,Output);
                        }
                        // Stage each bird for art and proximity evidence; this does
                        // not claim to test the random arrival distribution.
                        foreach(var spec in life.Animals.Where(a=>a.Bird))
                        {
                            who.Teleport(new Vector3(0,Slipper.GroundY(new Vector3(0,0,-10)),-10));
                            life.StageBirdVisitForReview(spec.Id);yield return new WaitForSeconds(.1f);
                            var animal=life.transform.Find("Ambient "+spec.Id);Assert.IsNotNull(animal);
                            Assert.IsNotEmpty(animal.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                            var target=animal.position;
                            var view=target+animal.forward*1.5f+Vector3.right+Vector3.up*.8f;
                            witness.transform.SetPositionAndRotation(view,Quaternion.LookRotation(target+Vector3.up*.12f-view));
                            yield return GameplayShots.Render(witness,map+"-"+spec.Id+"-visit",false,Output);
                            who.Teleport(target+Vector3.right*1.5f);
                            yield return new WaitForSeconds(.55f);
                            Assert.Greater(Vector3.Distance(animal.position,target),.7f,map+"/"+spec.Id+" did not fly away from the nearby player");
                        }
                    }
                    finally{Object.Destroy(witness.gameObject);}
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"animals.csv"),report.ToString());}
        }
        [UnityTest,Timeout(240000)]
        public IEnumerator GroundAnimalGaitsAndStyleAtOrdinarySpeed()
        {
            var report=new StringBuilder("map,id,maximum_leg_rotation,minimum_ground_gap,maximum_ground_gap\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();var who=GameServices.Round.PlayerAt(1);
                    foreach(var spec in life.Animals.Where(a=>!a.Bird))
                    {
                        var animal=life.transform.Find("Ambient "+spec.Id);
                        var leg=animal.GetComponentsInChildren<Transform>().First(t=>t.name=="FrontL");
                        var rest=leg.localRotation;float motion=0,minGap=float.PositiveInfinity,maxGap=float.NegativeInfinity;
                        who.Teleport(new Vector3(0,0,-10));
                        var camera=new GameObject("Animal motion camera").AddComponent<Camera>();
                        camera.enabled=false;camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
                        camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                        var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=animal;
                        var mesh=new Mesh();float next=0;bool approached=false;
                        var privateArms=Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None).SelectMany(a=>a.GetComponentsInChildren<Renderer>()).ToArray();
                        var enabled=privateArms.Select(r=>r.enabled).ToArray();foreach(var r in privateArms)r.enabled=false;
                        follow.Sample=()=>
                        {
                            motion=Mathf.Max(motion,Quaternion.Angle(rest,leg.localRotation));
                            if(Time.time<next)return;next=Time.time+.05f;
                            float bottom=float.PositiveInfinity;
                            foreach(var skin in animal.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {skin.BakeMesh(mesh);foreach(var v in mesh.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(v).y);}
                            // A short support ray cannot select an overhead awning.
                            float ground=animal.position.y;
                            foreach(var hit in Physics.RaycastAll(animal.position+Vector3.up*.25f,Vector3.down,.6f,~0,QueryTriggerInteraction.Ignore))
                                if(hit.collider.GetComponentInParent<CharacterMotor>()==null&&hit.normal.y>.7f)ground=Mathf.Max(ground,hit.point.y);
                            float gap=bottom-ground;
                            minGap=Mathf.Min(minGap,gap);maxGap=Mathf.Max(maxGap,gap);
                        };
                        try
                        {
                            yield return ImprovementEvidenceProbe.Record(camera,map+"-"+spec.Id+"-motion",5,null,t=>
                            {
                                if(t>.7f&&!approached){who.Teleport(animal.position+Vector3.right*2);approached=true;}
                            });
                            report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{motion:F3},{minGap:F4},{maxGap:F4}"));
                            Assert.Greater(motion,10,spec.Id+" slides along its route without animating its legs");
                            Assert.Greater(minGap,-.045f,spec.Id+" feet sink through the route support");
                            Assert.Less(minGap,.025f,spec.Id+" floats for its entire gait");
                        }
                        finally
                        {
                            foreach(var pair in privateArms.Select((r,i)=>(r,i)))if(pair.r!=null)pair.r.enabled=enabled[pair.i];
                            Object.Destroy(mesh);Object.Destroy(camera.gameObject);
                        }
                    }
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"gait-contact.csv"),report.ToString());}
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator DogSurfacePauseFacesRaisedLegAndInterruptsForPlayers()
        {
            var report=new StringBuilder("map,id,leg_rotation,surface_side_dot,state_after_approach\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();var who=GameServices.Round.PlayerAt(1);
                    var spec=life.Animals.Single(a=>!a.Bird&&a.Id.StartsWith("aspin",StringComparison.Ordinal));
                    Assert.GreaterOrEqual(spec.PeeWaypoint,0,map+" dog has no sensible surface pause");
                    var animal=life.transform.Find("Ambient "+spec.Id);
                    var leg=animal.GetComponentsInChildren<Transform>().First(t=>t.name=="BackR");
                    var rest=leg.localRotation;
                    foreach(var player in GameServices.Round.Players)if(player!=null)player.Teleport(new Vector3(0,0,-10));
                    life.StageDogPauseForReview(spec.Id);
                    float deadline=Time.time+3;
                    while(Time.time<deadline&&!life.DescribeForReview(spec.Id).StartsWith("pee|",StringComparison.Ordinal))yield return null;
                    yield return new WaitForSeconds(.8f);
                    var arc=animal.GetComponentInChildren<LineRenderer>();Assert.IsNotNull(arc);
                    Assert.IsTrue(arc.enabled,"The staged leg lift did not reach its brief middle phase");
                    float angle=Quaternion.Angle(rest,leg.localRotation);Assert.Greater(angle,20,"The dog never lifted its hind leg");
                    var side=leg.position-animal.position;side.y=0;
                    var target=spec.PeeTarget-animal.position;target.y=0;
                    float dot=Vector3.Dot(side.normalized,target.normalized);Assert.Greater(dot,0,"The lifted leg faces away from its surface");
                    var camera=new GameObject("Dog pause review camera").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=52;
                    camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    var view=animal.position+animal.forward*1.9f-target.normalized*1.5f+Vector3.up*.9f;
                    camera.transform.SetPositionAndRotation(view,Quaternion.LookRotation(animal.position+Vector3.up*.35f-view));
                    yield return GameplayShots.Render(camera,map+"-dog-surface-pause",false,Output);Object.Destroy(camera.gameObject);
                    var start=animal.position;who.Teleport(start-target.normalized*1.5f);
                    yield return new WaitForSeconds(.2f);
                    Assert.IsFalse(arc.enabled,"Surface arc persists after a person approaches");
                    StringAssert.Contains("peeing=False",life.DescribeForReview(spec.Id));
                    yield return new WaitForSeconds(1.2f);
                    Assert.Greater(Vector3.Distance(start,animal.position),.6f,"Interrupted dog did not retreat");
                    report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{angle:F2},{dot:F3},{life.DescribeForReview(spec.Id)}"));
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"dog-surface-pause.csv"),report.ToString());}
        }
        [DefaultExecutionOrder(9999)] private sealed class AnimalCamera:MonoBehaviour
        {
            public Transform Target;public Action Sample;
            public Vector3 Offset=new Vector3(1.7f,.9f,1.7f);
            private void LateUpdate()
            {
                if(Target==null)return;
                var at=Target.position+Offset;
                transform.SetPositionAndRotation(at,Quaternion.LookRotation(Target.position+Vector3.up*.4f-at));Sample?.Invoke();
            }
        }
        [DefaultExecutionOrder(-300)] private sealed class ApproachInput:MonoBehaviour
        {
            public Vector2 Move;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;}
        }
    }
}
