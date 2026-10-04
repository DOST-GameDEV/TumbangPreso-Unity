using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class QuickCircuitPresentationTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        INetProvider previousNet;
        [UnitySetUp] public IEnumerator Before()
        { previousNet = NetAuthority.Provider; yield return PlayModeWorld.Reset(); NetAuthority.Provider = new SoloProvider(); }
        [UnityTearDown] public IEnumerator After()
        { foreach (var go in objects) if (go != null) Object.Destroy(go); objects.Clear(); yield return PlayModeWorld.Reset(); NetAuthority.Provider = previousNet; }
        GameObject Keep(GameObject go) { objects.Add(go); return go; }

        [UnityTest, Timeout(45000)] public IEnumerator BothCutDirectionsPlayTheShippingBodyAndRecover()
        {
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = Vector3.down * .5f; floor.transform.localScale = new Vector3(30, 1, 30);
            var go = Keep(new GameObject("Quick Circuit presentation actor", typeof(CharacterController)));
            var cc = go.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
            var motor = go.AddComponent<CharacterMotor>(); motor.PlayerSlot = 1; motor.Mode = GameMode.HeroStrike; motor.IsBot = true;
            motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack");
            go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>();
            var system = go.AddComponent<HeroAbilitySystem>(); system.BindHero("zack");
            var art = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            go.AddComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            var animator = go.GetComponent<CharacterAnimator>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(motor);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(100, true, 0, true);
            var witness = Keep(new GameObject("Quick Circuit witness")).AddComponent<Camera>(); witness.enabled = false; witness.fieldOfView = 42;
            witness.clearFlags = CameraClearFlags.SolidColor; witness.backgroundColor = new Color(.16f, .18f, .2f);
            var light = Keep(new GameObject("Quick Circuit light")).AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, -25, 0);
            foreach (int side in new[] { -1, 1 })
            {
                system.Kit.ResetForRound(new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>()));
                motor.Teleport(new Vector3(0, .12f, 0)); motor.transform.rotation = Quaternion.identity; motor.Intent.Clear(); motor.Intent.Parked = false;
                yield return new WaitForSeconds(.1f);
                Vector3 start = motor.transform.position; bool accepted = false, bodyPlayed = false;
                System.Action<float> drive = t =>
                {
                    bool press = t < .1f;
                    motor.Intent.FaceAimPoint = true; motor.Intent.AimPoint = motor.transform.position + Vector3.forward * 20;
                    motor.Intent.Move = press ? new Vector2(side, 0) : Vector2.zero;
                    motor.Intent.Set(Verb.Skill1, press);
                    if (press) motor.Intent.BufferPress(Verb.Skill1);
                    accepted |= system.LastAnswer(HeroAbilitySystem.Slot.Skill1) == HeroKit.CastOutcome.Cast;
                    bodyPlayed |= animator.CurrentClipName == "hero-zack-sprint";
                };
                if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")))
                    yield return ImprovementEvidenceProbe.Record(witness, side < 0 ? "quick-cut-left" : "quick-cut-right", 1.4f, motor, drive, new Vector3(2.8f, 1.3f, 4));
                else
                { float begin = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - begin < 1.4f) { drive(Time.realtimeSinceStartup - begin); yield return null; } }
                Assert.IsTrue(accepted, "Real skill input was not accepted.");
                Assert.IsTrue(bodyPlayed, "Accepted cut did not play the shipping body action.");
                Assert.That((motor.transform.position.x - start.x) * side, Is.InRange(1.5f, 2.8f));
                Assert.Less(Mathf.Abs(motor.transform.position.z - start.z), .1f);
                Assert.IsFalse(animator.IsPlayingAction, "The body did not recover from its single cut.");
                Assert.IsFalse(system.Kit.Skill1.IsActive);
            }
        }
    }
}
