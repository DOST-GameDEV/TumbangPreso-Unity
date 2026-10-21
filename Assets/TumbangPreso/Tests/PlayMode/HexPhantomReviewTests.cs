using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HexPhantomReviewTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        CharacterMotor _victim;
        CameraSystem.CameraRig _rig;
        IEnumerator Open()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var ready = Object.FindFirstObjectByType<ReadyGate>();
            ready.enabled = true; ready.StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            _victim = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
            // Match capture fixtures explicitly stage the viewed seat as human;
            // disabling its AI component alone does not change the motor's bot flag.
            _victim.IsBot = false;
            Debug.Log("[HexReview] victim=" + _victim.PlayerSlot + " solo=" + GameLaunch.SoloSeat
                + " networked=" + NetAuthority.IsNetworked + " local=" + NetAuthority.LocalSlot);
            _victim.Teleport(new Vector3(-3, .12f, -5));
            _victim.transform.rotation = Quaternion.identity;
            _rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            _rig.Follow(_victim); _rig.SetAimSource(CameraSystem.AimSource.Movement);
            typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_rig, 18f);
            _victim.Intent.Clear(); _victim.Intent.AimPoint = _victim.transform.position + Vector3.forward * 8;
            _victim.ApplyHexed();
            yield return new WaitForSeconds(.8f);
            Assert.IsTrue(_victim.IsHexed);
            Assert.IsNotNull(Object.FindFirstObjectByType<HexedPhantomSlippers>());
        }
        static GameObject[] Copies() => Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name == "phantom-slipper").Select(t => t.gameObject).ToArray();

        [UnityTest, Timeout(60000)]
        public IEnumerator VictimCopiesStayInsideThePlayableCourt()
        {
            yield return Open();
            Assert.IsNotEmpty(Copies(), "The centre scenario must show real phantom copies.");
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Hex-victim-centre-960x540", canvas, 960, 540, false, true);
            _victim.Teleport(new Vector3(0, .12f, AIController.PlayableMaxZ - .4f));
            _victim.Intent.AimPoint = _victim.transform.position + Vector3.forward * 8;
            yield return null; yield return null;
            Assert.IsFalse(_victim.IsHexed, "Teleport's existing spawn-settle clears statuses.");
            Assert.IsEmpty(Copies());
            _victim.ApplyHexed();
            yield return new WaitForSeconds(1.2f);
            // Looking out of the court may validly produce no plausible candidates.
            var copies = Copies(); Assert.LessOrEqual(copies.Length, 3);
            Debug.Log("[HexReview] bounds=" + new Vector4(AIController.PlayableMinX, AIController.PlayableMaxX,
                AIController.PlayableMinZ, AIController.PlayableMaxZ) + " copies=" +
                string.Join(";", copies.Select(c => c.transform.position.ToString("F2"))));
            var positions = copies.Select(c => c.transform.position).ToArray();
            foreach (var copy in copies)
            {
                Assert.IsEmpty(copy.GetComponentsInChildren<Collider>());
                Assert.IsNull(copy.GetComponent<Slipper>(), "A hallucination must never become a pickup.");
            }
            yield return TumpUiCapture.Capture("Hex-victim-edge-960x540", canvas, 960, 540, false, true);
            foreach (var p in positions)
            {
                Assert.That(p.x, Is.InRange(AIController.PlayableMinX, AIController.PlayableMaxX));
                Assert.That(p.z, Is.InRange(AIController.PlayableMinZ, AIController.PlayableMaxZ));
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LeavingTheVictimViewHidesCopiesAndExpiryRemovesThem()
        {
            yield return Open();
            Assert.IsNotEmpty(Copies());
            _rig.Follow(GameServices.Round.PlayerAt(2));
            yield return null; yield return null;
            Assert.IsFalse(Copies().SelectMany(c => c.GetComponentsInChildren<Renderer>())
                .Any(r => r.enabled && r.gameObject.activeInHierarchy), "Phantoms leak into another body's view.");
            _rig.Follow(_victim); yield return null; yield return null;
            Assert.IsTrue(Copies().SelectMany(c => c.GetComponentsInChildren<Renderer>())
                .Any(r => r.enabled && r.gameObject.activeInHierarchy));
            yield return new WaitForSeconds(_victim.HexedLeft + .1f);
            yield return null; yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<HexedPhantomSlippers>());
            Assert.IsEmpty(Copies());
        }
    }
}
