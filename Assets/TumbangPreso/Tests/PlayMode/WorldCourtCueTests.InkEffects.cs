using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest] public IEnumerator InkEffectsKeepTheCanClearAndContactsGrounded()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var can=GameServices.Round.Lata;var camera=Camera.main;var rig=camera.GetComponent<CameraRig>();
            foreach(var actor in GameServices.Round.Players)actor.Teleport(new Vector3(6,actor.transform.position.y,6));
            yield return new WaitForSeconds(can.ProtectionLeft+.05f);
            can.HostKnockDown(1);yield return new WaitForSeconds(.5f);rig.enabled=false;
            camera.transform.position=can.transform.position+new Vector3(2.2f,1.7f,-3.1f);camera.transform.LookAt(can.transform.position+Vector3.up*.15f);camera.fieldOfView=65;
            var collar=can.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="DownCollar");
            var rng=Random.state;
            var confetti=PaperConfetti.Play(can.transform.position,24);Assert.IsNotNull(confetti);
            Assert.AreEqual(rng,Random.state,"Cosmetic paper must not advance gameplay RNG.");
            Assert.IsEmpty(confetti.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(confetti.GetComponentsInChildren<Collider>());
            Assert.AreEqual(24,confetti.PaperCount);
            foreach(var paper in confetti.GetComponentsInChildren<MeshFilter>())
            {
                Assert.AreEqual(6,paper.sharedMesh.vertexCount);
                var view=camera.WorldToViewportPoint(paper.transform.position);
                Assert.IsTrue(view.z<=0 || Mathf.Abs(view.x-.5f)>=.18f || Mathf.Abs(view.y-.5f)>=.22f,"Paper starts outside the central view.");
            }
            confetti.enabled=false;confetti.Sample(.2f);
            var replayParent=new GameObject("Recorded dust witness");
            var dust=CourtContactDust.Play("land",can.transform.position,replayParent.transform);
            Assert.IsNotNull(dust);Assert.IsTrue(WorldGround.TryBelow(can.transform.position,.35f,1.35f,out float ground));
            Assert.AreEqual(ground+.045f,dust.transform.position.y,.001f);
            dust.Sample(.10f);Assert.Greater(dust.GetComponent<ParticleSystem>().particleCount,0);
            var accentCount=Object.FindObjectsByType<CanContactAccent>().Length;
            GameServices.Audio.PlayAtVaried("slide_scrape",can.transform.position);
            Assert.Greater(Object.FindObjectsByType<CourtContactDust>().Length,1,"The ordinary event creates its local dust.");
            Assert.AreEqual(accentCount,Object.FindObjectsByType<CanContactAccent>().Length,"A slide must not fabricate can success.");
            Time.timeScale=0;
            try
            {
                foreach(string state in new[]{"before","after","comfort"})
                {
                    bool before=state=="before";WorldCueProfile.Current.HeroObjects=before?0:1;WorldCueProfile.Current.InkEffects=before?0:1;
                    Settings.SettingsStore.Current.ReducedEffects=state=="comfort";
                    Settings.SettingsStore.Current.HighContrastHud=state=="comfort";Settings.SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                    if(confetti!=null)Object.DestroyImmediate(confetti.gameObject);
                    confetti=before?null:PaperConfetti.Play(can.transform.position,24);
                    if(confetti!=null){confetti.enabled=false;confetti.Sample(.2f);}
                    if(dust!=null)Object.DestroyImmediate(dust.gameObject);dust=before?null:CourtContactDust.Play("land",can.transform.position,replayParent.transform);
                    if(dust!=null)dust.Sample(.10f);
                    foreach(var other in Object.FindObjectsByType<CourtContactDust>())if(other!=dust)other.ShowForCapture(false);
                    yield return null;Assert.AreEqual(before,collar.gameObject.activeSelf);
                    CanContactAccent.Play(can.transform.position,false);
                    var accent=Object.FindObjectsByType<CanContactAccent>().Last();accent.enabled=false;Private(accent,"Draw",.15f);
                    Assert.AreEqual("TumbangPreso/InkContact",accent.GetComponentInChildren<LineRenderer>().sharedMaterial.shader.name);
                    Assert.IsTrue(accent.GetComponentInChildren<LineRenderer>().sharedMaterial.shader.isSupported);
                    yield return GameplayShots.Render(camera,"ink-effects-"+state,false,Output,null,960,540);
                    Object.DestroyImmediate(accent.gameObject);
                }
                WorldCueProfile.Current.InkEffects=0;
                Assert.IsNull(CourtContactDust.Play("land",can.transform.position));Assert.IsNull(PaperConfetti.Play(can.transform.position));
                WorldCueProfile.Current.InkEffects=1;Settings.SettingsStore.Current.ReducedEffects=true;
                var calm=PaperConfetti.Play(can.transform.position,24);Assert.AreEqual(8,calm.PaperCount);Object.DestroyImmediate(calm.gameObject);
                Assert.IsNull(CourtContactDust.Play("land",new Vector3(0,100,0)),"No dust hanging in empty air.");
                Assert.IsNull(CourtContactDust.Play("throw_charge",can.transform.position));
            }
            finally{Object.Destroy(replayParent);if(confetti!=null)Object.Destroy(confetti.gameObject);Time.timeScale=1;rig.enabled=true;}
            yield return new WaitForSeconds(CourtContactDust.Life+.1f);
            Assert.IsEmpty(Object.FindObjectsByType<CourtContactDust>());
        }
    }
}
