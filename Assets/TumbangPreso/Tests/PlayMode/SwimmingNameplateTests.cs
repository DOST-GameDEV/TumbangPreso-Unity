using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SwimmingNameplateTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator RoleMarkerFollowsWaterAndReturnsToTheCapsuleFloor()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            SceneFlow.Networked = false;
            yield return SceneManager.LoadSceneAsync("SaBubong");
            yield return new WaitForSecondsRealtime(.4f);
            var actor = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == 1);
            var brain = actor.GetComponent<AIController>(); if (brain != null) brain.enabled = false;
            actor.enabled = false;
            var capsule = actor.GetComponent<CharacterController>(); capsule.enabled = false;
            var plate = actor.GetComponentInChildren<CharacterNameplate>();
            var ring = plate.transform.Find("NameplateRing");
            var label = plate.transform.Find("NameplateLabel");
            Vector3 landLocal = ring.localPosition;
            Vector3 landLabel = label.localPosition;
            actor.transform.position = new Vector3(-13, RooftopPool.SurfaceY - RooftopPool.FloatDepth, 6);
            yield return null;
            yield return null;
            Assert.IsTrue(actor.IsSwimming);
            Assert.That(ring.position.y, Is.EqualTo(RooftopPool.SurfaceY + CharacterNameplate.RingFloorMargin).Within(.002f));
            Assert.Greater(label.position.y, RooftopPool.SurfaceY + .20f);
            Assert.IsFalse(ring.GetComponentsInChildren<Collider>().Any(c => c.enabled), "UI marker must never become physical support.");
            actor.transform.position = new Vector3(0, 0, 6);
            yield return null;
            Assert.IsFalse(actor.IsSwimming);
            Assert.Less(Vector3.Distance(ring.localPosition, landLocal), .001f);
            Assert.Less(Vector3.Distance(label.localPosition, landLabel), .001f);
        }
    }
}
