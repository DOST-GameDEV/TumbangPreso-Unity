using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// CONCUSSED is felt and seen (`docs/reports/dante-presentation-2026-10-16/plan.md` section 3): observers see three pebbles
    /// circling the victim's head for exactly as long as the status lasts; the victim's own screen effects follow the clock.
    /// </summary>
    public sealed class DanteConcussedPresentationTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        CharacterMotor _victim;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup() { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }

        IEnumerator Open()
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike)); GameServices.Ensure(); GameServices.Round.Clear();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.localScale = new Vector3(30, 1, 30); floor.transform.position = Vector3.down * .5f;
            var can = Track(new GameObject("Concussed can")); can.transform.position = new Vector3(6, 0, 6); GameServices.Round.Lata = can.AddComponent<Lata>();
            var go = Track(new GameObject("Concussed victim", typeof(CharacterController)));
            _victim = go.AddComponent<CharacterMotor>(); _victim.PlayerSlot = 2; _victim.Mode = GameMode.HeroStrike; _victim.IsBot = true;
            go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>();
            var cc = go.GetComponent<CharacterController>(); go.transform.position = new Vector3(0, -(cc.center.y - cc.height * .5f - cc.skinWidth) + .05f, 0);
            GameServices.Round.Register(_victim); GameServices.Match.StartMatch(); GameServices.Round.BeginRound(); Physics.SyncTransforms();
            yield return new WaitForSeconds(.1f);
        }

        [Test] public void TheVictimsScreenFollowsTheClock()
        {
            Assert.AreEqual(0f, ConcussedPerception.Envelope(StatusRules.ConcussedSeconds), 1e-4f, "Nothing on the first frame: it settles in.");
            Assert.AreEqual(1f, ConcussedPerception.Envelope(StatusRules.ConcussedSeconds - .2f), 1e-4f);
            Assert.AreEqual(1f, ConcussedPerception.Envelope(1.0f), 1e-4f, "Held until the last second.");
            Assert.AreEqual(.5f, ConcussedPerception.Envelope(.5f), 1e-4f, "Out over the last second.");
            Assert.AreEqual(0f, ConcussedPerception.Envelope(0f), 1e-4f);
            Assert.IsNotNull(Shader.Find("TumbangPreso/ConcussedVignette"), "The victim's rim shader must exist.");
        }

        [UnityTest] public IEnumerator ObserversSeeThreePebblesForTheWholeStatus()
        {
            yield return Open();
            _victim.ApplyConcussed();
            Assert.IsTrue(_victim.IsConcussed);
            yield return null;
            var ring = _victim.GetComponentInChildren<ConcussedPebbles>();
            Assert.IsNotNull(ring, "A Concussed player must carry the pebble ring.");
            Assert.IsNull(_victim.GetComponentInChildren<DizzyStars>(), "Concussed shows stone, not the generic stars.");
            Assert.AreEqual(3, ring.GetComponentsInChildren<MeshRenderer>().Length, "Three pebbles.");
            yield return new WaitForSeconds(StatusRules.ConcussedSeconds - .4f);
            Assert.IsTrue(_victim.IsConcussed && ring != null, "The ring lives as long as the 5 s status.");
            yield return new WaitForSeconds(.6f);
            yield return null;
            Assert.IsFalse(_victim.IsConcussed);
            Assert.IsTrue(ring == null, "The ring ends with the status.");
        }
    }
}
