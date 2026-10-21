using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest]
        public IEnumerator AcceptedTagUsesContactInkAndRetainsRecoveryAndPause()
        {
            yield return Load(SceneFlow.Eskinita);
            SettingsStore.Current.FlashIntensity = 1;
            SettingsStore.Current.ReducedEffects = false;
            SettingsStore.Current.ReducedUiMotion = false;
            WorldCueProfile.Current.InkEffects = 1;
            var taya = GameServices.Round.PlayerAt(0);
            var victim = GameServices.Round.PlayerAt(1);
            var can = GameServices.Round.Lata;
            yield return new WaitForSeconds(can.ProtectionLeft + .05f);
            taya.Teleport(can.transform.position + Vector3.back * 3.2f);
            victim.Teleport(can.transform.position + Vector3.back * 2.2f);
            taya.transform.forward = victim.transform.forward = Vector3.forward;
            victim.ClearStun(); victim.ClearTrip();
            for (int i = 2; i < 4; i++)
                GameServices.Round.PlayerAt(i).Teleport(can.transform.position + new Vector3(-5, 0, i * 2));
            yield return null; Physics.SyncTransforms();
            Assert.IsTrue(victim.IsTaggable());
            Vector3 acceptedAt = victim.transform.position;
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, Vector3.forward));
            Assert.IsTrue(victim.IsTagged); Assert.Greater(victim.StunLeft, .3f);
            var cue = Object.FindAnyObjectByType<TagContactAccent>();
            Assert.IsNotNull(cue, "Accepted tag did not reach the replacement effect.");
            Assert.IsNull(GameObject.Find("~ImpactBurst"), "The old particle burst still runs on tags.");
            Assert.AreEqual(0, cue.GetComponentsInChildren<Collider>().Length);
            var strokes = cue.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(3, strokes.Length);
            Assert.Less(Vector3.Distance(cue.transform.position, acceptedAt + Vector3.up * .8f), .6f);
            var material = strokes[0].sharedMaterial;
            Assert.IsTrue(material.shader.isSupported);
            Hitstop.End(); Time.timeScale = 0;
            cue.Sample(.025f);
            var observer = StageCamera(acceptedAt + new Vector3(2, 1.5f, -2.5f), acceptedAt + Vector3.up * .8f);
            yield return GameplayShots.Render(observer, "tag-contact-observer", false, Output, victim, 960, 540);
            var rig = Camera.main.GetComponent<CameraSystem.CameraRig>();
            rig.Follow(taya); yield return null;
            yield return GameplayShots.Render(Camera.main, "tag-contact-taya-fpp", false, Output, victim, 960, 540);
            float beforeRecovery = victim.StunLeft;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.IsTrue(cue != null, "Paused contact effect expired on wall time.");
            Assert.AreEqual(beforeRecovery, victim.StunLeft, .001f);
            Object.Destroy(observer.gameObject);
            Time.timeScale = 1;
            yield return new WaitForSeconds(TagContactAccent.Lifetime + .08f);
            yield return null;
            Assert.IsTrue(cue == null); Assert.IsTrue(material == null, "Contact material leaked.");
            Assert.Less(victim.StunLeft, beforeRecovery);
        }

        [UnityTest]
        public IEnumerator ContactInkHonorsComfortAndRetiresWithRound()
        {
            yield return Load(SceneFlow.Eskinita);
            var actor = GameServices.Round.PlayerAt(0);
            var victim = GameServices.Round.PlayerAt(1);
            SettingsStore.Current.FlashIntensity = 0;
            Assert.IsNull(TagContactAccent.Play(actor, victim, victim.transform.position));
            SettingsStore.Current.FlashIntensity = 1;
            SettingsStore.Current.ReducedUiMotion = true;
            SettingsStore.Current.ReducedEffects = true;
            WorldCueProfile.Current.InkEffects = 1;
            var cue = TagContactAccent.Play(actor, victim, victim.transform.position);
            Assert.IsNotNull(cue);
            var strokes = cue.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(2, strokes.Length);
            cue.Sample(.08f);
            Assert.IsTrue(strokes.All(line => line.transform.localPosition == Vector3.zero));
            Assert.Less(strokes[0].startColor.a, .2f);
            var camera = StageCamera(cue.transform.position + new Vector3(1, .5f, -2), cue.transform.position);
            Time.timeScale = 0;
            yield return GameplayShots.Render(camera, "tag-contact-comfort", false, Output, victim, 960, 540);
            Time.timeScale = 1;
            GameServices.Round.EndRound();
            yield return null; yield return null;
            Assert.IsTrue(cue == null, "A retired round left contact ink alive.");
            Object.Destroy(camera.gameObject);
        }
    }
}
