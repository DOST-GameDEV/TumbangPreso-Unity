using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class AmbientLifeProbe
    {
        [UnityTest,Timeout(300000)]
        public IEnumerator EskinitaBirdsLandReactAndLeaveBeyondTheCourt()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);Time.timeScale=1;
            var life=Object.FindFirstObjectByType<AmbientLife>();var birds=life.Animals.Where(a=>a.Bird).ToArray();
            Assert.AreEqual(3,birds.Length);
            var who=GameServices.Round.PlayerAt(1);
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-13));
            var camera=new GameObject("Bird visit witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=58;camera.nearClipPlane=.04f;camera.farClipPlane=500;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var report=new StringBuilder("bird,view,seconds,x,y,z,visible,state\n");
            try
            {
                foreach(var spec in birds)
                {
                    Assert.AreEqual(3,spec.Perches.Length);var bird=life.transform.Find("Ambient "+spec.Id);
                    var perch=spec.Route[1];
                    void View(bool close)
                    {
                        var eye=perch+(close?new Vector3(1,.65f,-1.2f):new Vector3(2.4f,2.6f,-5.5f));
                        var at=perch+Vector3.up*(close?.14f:1.4f);
                        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));
                    }
                    void Sample(string label,float t,Transform subject,AmbientLife owner)
                    {
                        var p=subject.position;
                        report.AppendLine(FormattableString.Invariant($"{spec.Id},{label},{t:F3},{p.x:F3},{p.y:F3},{p.z:F3},{subject.gameObject.activeSelf},{owner.DescribeForReview(spec.Id)}"));
                    }
                    life.enabled=false;foreach(var entry in birds)life.transform.Find("Ambient "+entry.Id).gameObject.SetActive(false);
                    var legacyRoot=new GameObject("Legacy visit comparison");
                    try
                    {
                        var legacy=legacyRoot.AddComponent<AmbientLife>();legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,Bird=true}};
                        yield return null;legacy.StageBirdVisitForReview(spec.Id);yield return null;
                        var original=legacyRoot.transform.Find("Ambient "+spec.Id);View(true);
                        yield return GameplayShots.Render(camera,"Eskinita-"+spec.Id+"-before-perch",false,Output,width:960,height:540);
                        View(false);bool startled=false;
                        yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-"+spec.Id+"-before-visit",6,null,t=>
                        {
                            if(t>=1&&!startled){who.Teleport(perch+Vector3.right*1.2f);startled=true;}
                            Sample("before",t,original,legacy);
                        });
                    }
                    finally{Object.Destroy(legacyRoot);life.enabled=true;who.Teleport(new Vector3(0,.1f,-13));}
                    yield return null;life.StageBirdVisitForReview(spec.Id);View(true);yield return null;
                    yield return GameplayShots.Render(camera,"Eskinita-"+spec.Id+"-after-perch",false,Output,width:960,height:540);
                    View(false);bool approached=false;float farthest=0;bool vanishedNear=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-"+spec.Id+"-after-visit",8,null,t=>
                    {
                        if(t>=1&&!approached){who.Teleport(perch+Vector3.right*1.2f);approached=true;}
                        if(t>2)who.Teleport(new Vector3(0,.1f,-13));
                        float distance=Vector3.Distance(perch,bird.position);farthest=Mathf.Max(farthest,distance);
                        if(!bird.gameObject.activeSelf&&distance<24)vanishedNear=true;
                        Sample("after",t,bird,life);
                    });
                    Assert.Greater(farthest,15,spec.Id+" did not continue beyond the nearby departure point");
                    Assert.IsFalse(vanishedNear,spec.Id+" disappeared within the court's immediate surroundings");
                    life.StageBirdArrivalForReview(spec.Id,1);yield return new WaitForSeconds(.3f);
                    Time.timeScale=0;yield return null;var held=bird.position;
                    var wing=bird.GetComponentsInChildren<Transform>().First(b=>b.name=="WingL");var wingHeld=wing.localRotation;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.Less(Vector3.Distance(held,bird.position),.0001f);Assert.Less(Quaternion.Angle(wingHeld,wing.localRotation),.001f);
                    Time.timeScale=1;bool landed=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Eskinita-"+spec.Id+"-arrival",8,null,t=>
                    {
                        string state=life.DescribeForReview(spec.Id);
                        if(state.Contains("birdPhase=2"))
                        {landed=true;Assert.Less(Vector3.Distance(bird.position,spec.Perches[1]),.005f,"Landing missed its supported site");}
                        if(spec.FanWatch&&state.Contains("birdPhase=2"))Assert.IsFalse(state.StartsWith("peck|",StringComparison.Ordinal),"Fantail copied the seed-peck habit");
                        Sample("arrival",t,bird,life);
                    });
                    Assert.IsTrue(landed,spec.Id+" never settled on its alternative landing site");
                    Assert.IsEmpty(bird.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Time.timeScale=1;life.enabled=true;Object.Destroy(camera.gameObject);
                File.WriteAllText(Path.Combine(Output,"eskinita-bird-visits.csv"),report.ToString());
            }
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator BayanBirdsUseTheirOpenPlazaVisitSites()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);Time.timeScale=1;
            var life=Object.FindFirstObjectByType<AmbientLife>();var birds=life.Animals.Where(a=>a.Bird).ToArray();
            Assert.AreEqual(3,birds.Length);
            var who=GameServices.Round.PlayerAt(1);
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-13));
            var camera=new GameObject("Bird visit witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=58;camera.nearClipPlane=.04f;camera.farClipPlane=500;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var report=new StringBuilder("bird,view,seconds,x,y,z,visible,state\n");
            try
            {
                foreach(var spec in birds)
                {
                    Assert.AreEqual(3,spec.Perches.Length);var bird=life.transform.Find("Ambient "+spec.Id);
                    var perch=spec.Route[1];
                    void View(bool close)
                    {
                        var eye=perch+(close?new Vector3(1,.65f,-1.2f):new Vector3(2.4f,2.6f,-5.5f));
                        var at=perch+Vector3.up*(close?.14f:1.4f);
                        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));
                    }
                    void Sample(string label,float t,Transform subject,AmbientLife owner)
                    {
                        var p=subject.position;
                        report.AppendLine(FormattableString.Invariant($"{spec.Id},{label},{t:F3},{p.x:F3},{p.y:F3},{p.z:F3},{subject.gameObject.activeSelf},{owner.DescribeForReview(spec.Id)}"));
                    }
                    life.enabled=false;foreach(var entry in birds)life.transform.Find("Ambient "+entry.Id).gameObject.SetActive(false);
                    var legacyRoot=new GameObject("Legacy visit comparison");
                    try
                    {
                        var legacy=legacyRoot.AddComponent<AmbientLife>();legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,Bird=true}};
                        yield return null;legacy.StageBirdVisitForReview(spec.Id);yield return null;
                        var original=legacyRoot.transform.Find("Ambient "+spec.Id);View(true);
                        yield return GameplayShots.Render(camera,"Bayan-"+spec.Id+"-before-perch",false,Output,width:960,height:540);
                        View(false);bool startled=false;
                        yield return ImprovementEvidenceProbe.Record(camera,"Bayan-"+spec.Id+"-before-visit",6,null,t=>
                        {
                            if(t>=1&&!startled){who.Teleport(perch+Vector3.right*1.2f);startled=true;}
                            Sample("before",t,original,legacy);
                        });
                    }
                    finally{Object.Destroy(legacyRoot);life.enabled=true;who.Teleport(new Vector3(0,.1f,-13));}
                    yield return null;life.StageBirdVisitForReview(spec.Id);View(true);yield return null;
                    yield return GameplayShots.Render(camera,"Bayan-"+spec.Id+"-after-perch",false,Output,width:960,height:540);
                    View(false);bool approached=false;float farthest=0;bool vanishedNear=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Bayan-"+spec.Id+"-after-visit",8,null,t=>
                    {
                        if(t>=1&&!approached){who.Teleport(perch+Vector3.right*1.2f);approached=true;}
                        if(t>2)who.Teleport(new Vector3(0,.1f,-13));
                        float distance=Vector3.Distance(perch,bird.position);farthest=Mathf.Max(farthest,distance);
                        if(!bird.gameObject.activeSelf&&distance<24)vanishedNear=true;
                        Sample("after",t,bird,life);
                    });
                    Assert.Greater(farthest,15,spec.Id+" did not continue beyond the nearby departure point");
                    Assert.IsFalse(vanishedNear,spec.Id+" disappeared within the court's immediate surroundings");
                    // The old (0,-13) parking request confines the taya near
                    // (0,-7), where a valid low approach can be scared away.
                    // Keep the real avoidance behavior; stage a truly clear arrival.
                    var landing=spec.Perches[1];
                    var corner=new Vector3(landing.x<0?6:-6,landing.y,landing.z<0?6:-6);
                    foreach(var player in GameServices.Round.Players)
                    {player.Teleport(corner);Assert.Greater(Vector3.Distance(player.transform.position,landing),12,"Clear-arrival parking was confined too close");}
                    life.StageBirdArrivalForReview(spec.Id,1);yield return new WaitForSeconds(.3f);
                    Time.timeScale=0;yield return null;var held=bird.position;
                    var wing=bird.GetComponentsInChildren<Transform>().First(b=>b.name=="WingL");var wingHeld=wing.localRotation;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.Less(Vector3.Distance(held,bird.position),.0001f);Assert.Less(Quaternion.Angle(wingHeld,wing.localRotation),.001f);
                    Time.timeScale=1;bool landed=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Bayan-"+spec.Id+"-arrival",8,null,t=>
                    {
                        string state=life.DescribeForReview(spec.Id);
                        if(state.Contains("birdPhase=2"))
                        {landed=true;Assert.Less(Vector3.Distance(bird.position,spec.Perches[1]),.005f,"Landing missed its supported site");}
                        if(spec.FanWatch&&state.Contains("birdPhase=2"))Assert.IsFalse(state.StartsWith("peck|",StringComparison.Ordinal),"Fantail copied the seed-peck habit");
                        Sample("arrival",t,bird,life);
                    });
                    Assert.IsTrue(landed,spec.Id+" never settled on its alternative landing site");
                    Assert.IsEmpty(bird.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Time.timeScale=1;life.enabled=true;Object.Destroy(camera.gameObject);
                File.WriteAllText(Path.Combine(Output,"bayan-bird-visits.csv"),report.ToString());
            }
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator IlalimBirdsKeepTheirFlightsBelowTheGuideway()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);Time.timeScale=1;
            var life=Object.FindFirstObjectByType<AmbientLife>();var birds=life.Animals.Where(a=>a.Bird).ToArray();
            Assert.AreEqual(3,birds.Length);
            var who=GameServices.Round.PlayerAt(1);
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-13));
            var camera=new GameObject("Bird visit witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=58;camera.nearClipPlane=.04f;camera.farClipPlane=500;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var report=new StringBuilder("bird,view,seconds,x,y,z,visible,state\n");
            try
            {
                foreach(var spec in birds)
                {
                    Assert.AreEqual(3,spec.Perches.Length);var bird=life.transform.Find("Ambient "+spec.Id);
                    var perch=spec.Route[1];
                    void View(bool close)
                    {
                        var eye=perch+(close?new Vector3(1,.65f,-1.2f):new Vector3(2.4f,2.6f,-5.5f));
                        var at=perch+Vector3.up*(close?.14f:1.4f);
                        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));
                    }
                    void Sample(string label,float t,Transform subject,AmbientLife owner)
                    {
                        var p=subject.position;
                        if(label!="before")
                        {
                            Assert.Less(p.y+.4f,8,"Bird and wing allowance crossed the guideway underside");
                            float z=Mathf.Abs(p.z),rowDistance=Mathf.Abs(Mathf.Repeat(z-10+4,8)-4);
                            if(z>=9&&rowDistance<1)Assert.Less(Mathf.Abs(p.x)+.35f,3.75f,"Flight crossed a support-column row outside the centre gap");
                        }
                        report.AppendLine(FormattableString.Invariant($"{spec.Id},{label},{t:F3},{p.x:F3},{p.y:F3},{p.z:F3},{subject.gameObject.activeSelf},{owner.DescribeForReview(spec.Id)}"));
                    }
                    life.enabled=false;foreach(var entry in birds)life.transform.Find("Ambient "+entry.Id).gameObject.SetActive(false);
                    var legacyRoot=new GameObject("Legacy visit comparison");
                    try
                    {
                        var legacy=legacyRoot.AddComponent<AmbientLife>();legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,Bird=true}};
                        yield return null;legacy.StageBirdVisitForReview(spec.Id);yield return null;
                        var original=legacyRoot.transform.Find("Ambient "+spec.Id);View(true);
                        yield return GameplayShots.Render(camera,"Ilalim-"+spec.Id+"-before-perch",false,Output,width:960,height:540);
                        View(false);bool startled=false;
                        yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-"+spec.Id+"-before-visit",6,null,t=>
                        {
                            if(t>=1&&!startled){who.Teleport(perch+Vector3.right*1.2f);startled=true;}
                            Sample("before",t,original,legacy);
                        });
                    }
                    finally{Object.Destroy(legacyRoot);life.enabled=true;who.Teleport(new Vector3(0,.1f,-13));}
                    yield return null;life.StageBirdVisitForReview(spec.Id);View(true);yield return null;
                    yield return GameplayShots.Render(camera,"Ilalim-"+spec.Id+"-after-perch",false,Output,width:960,height:540);
                    View(false);bool approached=false;float farthest=0;bool vanishedNear=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-"+spec.Id+"-after-visit",8,null,t=>
                    {
                        if(t>=1&&!approached){who.Teleport(perch+Vector3.right*1.2f);approached=true;}
                        if(t>2)who.Teleport(new Vector3(0,.1f,-13));
                        float distance=Vector3.Distance(perch,bird.position);farthest=Mathf.Max(farthest,distance);
                        if(!bird.gameObject.activeSelf&&distance<24)vanishedNear=true;
                        Sample("after",t,bird,life);
                    });
                    Assert.Greater(farthest,15,spec.Id+" did not continue beyond the nearby departure point");
                    Assert.IsFalse(vanishedNear,spec.Id+" disappeared within the court's immediate surroundings");
                    // The old (0,-13) parking request confines the taya near
                    // (0,-7), where a valid low approach can be scared away.
                    // Keep the real avoidance behavior; stage a truly clear arrival.
                    var landing=spec.Perches[1];
                    var corner=new Vector3(landing.x<0?6:-6,landing.y,landing.z<0?6:-6);
                    foreach(var player in GameServices.Round.Players)
                    {player.Teleport(corner);Assert.Greater(Vector3.Distance(player.transform.position,landing),12,"Clear-arrival parking was confined too close");}
                    life.StageBirdArrivalForReview(spec.Id,1);yield return new WaitForSeconds(.3f);
                    Time.timeScale=0;yield return null;var held=bird.position;
                    var wing=bird.GetComponentsInChildren<Transform>().First(b=>b.name=="WingL");var wingHeld=wing.localRotation;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.Less(Vector3.Distance(held,bird.position),.0001f);Assert.Less(Quaternion.Angle(wingHeld,wing.localRotation),.001f);
                    Time.timeScale=1;bool landed=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"Ilalim-"+spec.Id+"-arrival",8,null,t=>
                    {
                        string state=life.DescribeForReview(spec.Id);
                        if(state.Contains("birdPhase=2"))
                        {landed=true;Assert.Less(Vector3.Distance(bird.position,spec.Perches[1]),.005f,"Landing missed its supported site");}
                        if(spec.FanWatch&&state.Contains("birdPhase=2"))Assert.IsFalse(state.StartsWith("peck|",StringComparison.Ordinal),"Fantail copied the seed-peck habit");
                        Sample("arrival",t,bird,life);
                    });
                    Assert.IsTrue(landed,spec.Id+" never settled on its alternative landing site");
                    Assert.IsEmpty(bird.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Time.timeScale=1;life.enabled=true;Object.Destroy(camera.gameObject);
                File.WriteAllText(Path.Combine(Output,"ilalim-bird-visits.csv"),report.ToString());
            }
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator SaBubongBirdsKeepTheirRoofAndCanopyPerches()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);Time.timeScale=1;
            var life=Object.FindFirstObjectByType<AmbientLife>();var birds=life.Animals.Where(a=>a.Bird).ToArray();
            Assert.AreEqual(3,birds.Length);
            var who=GameServices.Round.PlayerAt(1);
            foreach(var player in GameServices.Round.Players)player.Teleport(new Vector3(0,.1f,-13));
            var camera=new GameObject("Bird visit witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=58;camera.nearClipPlane=.04f;camera.farClipPlane=500;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var report=new StringBuilder("bird,view,seconds,x,y,z,visible,state\n");
            try
            {
                foreach(var spec in birds)
                {
                    Assert.AreEqual(3,spec.Perches.Length);
                    if(spec.FanWatch)Assert.IsTrue(spec.Perches.All(point=>point.y>3),"Fantail lost its supported canopy perches");var bird=life.transform.Find("Ambient "+spec.Id);
                    var perch=spec.Route[1];
                    void View(bool close)
                    {
                        var eye=perch+(close?new Vector3(1,.65f,-1.2f):new Vector3(2.4f,2.6f,-5.5f));
                        var at=perch+Vector3.up*(close?.14f:1.4f);
                        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));
                    }
                    void Sample(string label,float t,Transform subject,AmbientLife owner)
                    {
                        var p=subject.position;
                        report.AppendLine(FormattableString.Invariant($"{spec.Id},{label},{t:F3},{p.x:F3},{p.y:F3},{p.z:F3},{subject.gameObject.activeSelf},{owner.DescribeForReview(spec.Id)}"));
                    }
                    life.enabled=false;foreach(var entry in birds)life.transform.Find("Ambient "+entry.Id).gameObject.SetActive(false);
                    var legacyRoot=new GameObject("Legacy visit comparison");
                    try
                    {
                        var legacy=legacyRoot.AddComponent<AmbientLife>();legacy.Animals=new[]{new AmbientLife.Animal{Id=spec.Id,Model=spec.Model,Clips=spec.Clips,Route=spec.Route,Bird=true}};
                        yield return null;legacy.StageBirdVisitForReview(spec.Id);yield return null;
                        var original=legacyRoot.transform.Find("Ambient "+spec.Id);View(true);
                        yield return GameplayShots.Render(camera,"SaBubong-"+spec.Id+"-before-perch",false,Output,width:960,height:540);
                        View(false);bool startled=false;
                        yield return ImprovementEvidenceProbe.Record(camera,"SaBubong-"+spec.Id+"-before-visit",6,null,t=>
                        {
                            if(t>=1&&!startled){who.Teleport(perch+Vector3.right*1.2f);startled=true;}
                            Sample("before",t,original,legacy);
                        });
                    }
                    finally{Object.Destroy(legacyRoot);life.enabled=true;who.Teleport(new Vector3(0,.1f,-13));}
                    yield return null;life.StageBirdVisitForReview(spec.Id);View(true);yield return null;
                    yield return GameplayShots.Render(camera,"SaBubong-"+spec.Id+"-after-perch",false,Output,width:960,height:540);
                    View(false);bool approached=false;float farthest=0;bool vanishedNear=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"SaBubong-"+spec.Id+"-after-visit",8,null,t=>
                    {
                        if(t>=1&&!approached){who.Teleport(perch+Vector3.right*1.2f);approached=true;}
                        if(t>2)who.Teleport(new Vector3(0,.1f,-13));
                        float distance=Vector3.Distance(perch,bird.position);farthest=Mathf.Max(farthest,distance);
                        if(!bird.gameObject.activeSelf&&distance<24)vanishedNear=true;
                        Sample("after",t,bird,life);
                    });
                    Assert.Greater(farthest,15,spec.Id+" did not continue beyond the nearby departure point");
                    Assert.IsFalse(vanishedNear,spec.Id+" disappeared within the court's immediate surroundings");
                    // The old (0,-13) parking request confines the taya near
                    // (0,-7), where a valid low approach can be scared away.
                    // Keep the real avoidance behavior; stage a truly clear arrival.
                    var landing=spec.Perches[1];
                    var corner=new Vector3(landing.x<0?6:-6,landing.y,landing.z<0?6:-6);
                    foreach(var player in GameServices.Round.Players)
                    {player.Teleport(corner);Assert.Greater(Vector3.Distance(player.transform.position,landing),12,"Clear-arrival parking was confined too close");}
                    life.StageBirdArrivalForReview(spec.Id,1);yield return new WaitForSeconds(.3f);
                    Time.timeScale=0;yield return null;var held=bird.position;
                    var wing=bird.GetComponentsInChildren<Transform>().First(b=>b.name=="WingL");var wingHeld=wing.localRotation;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.Less(Vector3.Distance(held,bird.position),.0001f);Assert.Less(Quaternion.Angle(wingHeld,wing.localRotation),.001f);
                    Time.timeScale=1;bool landed=false;
                    yield return ImprovementEvidenceProbe.Record(camera,"SaBubong-"+spec.Id+"-arrival",8,null,t=>
                    {
                        string state=life.DescribeForReview(spec.Id);
                        if(state.Contains("birdPhase=2"))
                        {landed=true;Assert.Less(Vector3.Distance(bird.position,spec.Perches[1]),.005f,"Landing missed its supported site");}
                        if(spec.FanWatch&&state.Contains("birdPhase=2"))Assert.IsFalse(state.StartsWith("peck|",StringComparison.Ordinal),"Fantail copied the seed-peck habit");
                        Sample("arrival",t,bird,life);
                    });
                    Assert.IsTrue(landed,spec.Id+" never settled on its alternative landing site");
                    Assert.IsEmpty(bird.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Time.timeScale=1;life.enabled=true;Object.Destroy(camera.gameObject);
                File.WriteAllText(Path.Combine(Output,"sabubong-bird-visits.csv"),report.ToString());
            }
        }
    }
}
