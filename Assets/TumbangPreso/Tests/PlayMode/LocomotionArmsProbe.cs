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
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The arms while running, on the real map with the real motor, photographed side-on.
    ///
    /// 🧑 2026-09-24: *"my biggest issue is where the hands are and what they do when u run"*. The
    /// question this answers: do the free arms hang clear of the body and swing against the legs in a
    /// sprint and a walk, and while carrying does the OFF hand swing while the slipper hand stays put?
    /// `CharacterAnimator.LocomotionArms.cs` is what it checks. The pictures in
    /// `Logs/locomotion-arms/` are the review; the numbers are the floor under them.
    /// </summary>
    public sealed class LocomotionArmsProbe
    {
        private bool _bots, _spectator; private int _seat;
        private const string Output = "Logs/locomotion-arms";

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset(); Directory.CreateDirectory(Output);
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
        }

        [UnityTest]
        public IEnumerator FreeArmsSwingClearOfTheBodyAndTheCarryHandHolds()
        {
            // Spectating, so no seat is the hidden first-person body: every carrier is drawn.
            GameLaunch.Spectator = true; GameLaunch.AllBots = true;
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var players = GameServices.Round.Players.ToList();
            var carrying = players.First(p => p.GetComponent<Carrier>().Held != null);
            var empty = players.First(p => p.GetComponent<Carrier>().Held == null);
            foreach (var p in players) if (p != carrying && p != empty) p.Teleport(new Vector3(20 + p.PlayerSlot * 3, .2f, -20));

            var report = new StringBuilder("case,frames,amount,strideMin,strideMax,leftFwdMin,leftFwdMax,rightFwdMin,rightFwdMax,leftSpreadMin,rightSpreadMin\n");
            var witness = new GameObject("Arm swing witness").AddComponent<Camera>(); witness.enabled = false;
            try
            {
                yield return Run(empty, "empty-sprint", true, new Vector3(-3, .2f, -9));
                yield return Run(empty, "empty-walk", false, new Vector3(-3, .2f, -9));
                yield return Run(carrying, "carry-sprint", true, new Vector3(3, .2f, -9));
            }
            finally { File.WriteAllText(Path.Combine(Output, "arms.csv"), report.ToString()); Object.Destroy(witness.gameObject); }

            IEnumerator Run(CharacterMotor who, string name, bool sprint, Vector3 from)
            {
                who.Teleport(from); who.ClearStun(); who.ClearTrip(); who.transform.rotation = Quaternion.identity;
                who.Intent.Parked = false; who.Stamina.RefillAndClearFatigue();
                var input = who.gameObject.AddComponent<MotionInput>(); input.Move = Vector2.up; input.Sprint = sprint;
                var anim = who.GetComponent<CharacterAnimator>();
                bool holding = who.GetComponent<Carrier>().Held != null;
                yield return new WaitForSeconds(.9f);
                // ⚠️ SAMPLED IN LateUpdate, LAST. A coroutine resumes BEFORE LateUpdate, where the arm layer
                // poses the bones, so the first three runs of this probe measured and photographed the clip
                // underneath the layer and never the drawn result.
                var late = who.gameObject.AddComponent<LateSampler>();
                late.Begin(anim, witness, Path.Combine(Output, name), 6, .07f);
                yield return new WaitForSeconds(1.0f);
                late.enabled = false;
                Object.Destroy(input);
                report.AppendLine(FormattableString.Invariant($"{name},{late.Frames},{late.Amount:F2},{late.SMin:F2},{late.SMax:F2},{late.LMin:F1},{late.LMax:F1},{late.RMin:F1},{late.RMax:F1},{late.LSpread:F1},{late.RSpread:F1}"));
                report.AppendLine($"# {name} layer {anim.ArmSwingDiagnostics} along {anim.LeftArmAlong} {anim.RightArmAlong}");
                // Written before the asserts: an assert inside this nested coroutine skips the outer finally.
                File.WriteAllText(Path.Combine(Output, "arms.csv"), report.ToString());
                Object.Destroy(late);
                Assert.Greater(late.Frames, 10, $"{name}: nothing was sampled.");
                Assert.Greater(late.Amount, .6f, $"{name}: the arm swing was not drawn while moving.");
                Assert.Greater(late.SMax - late.SMin, 1f, $"{name}: the stride read off the legs never swung.");
                float need = sprint ? 60f : 30f;
                Assert.Greater(late.LMax - late.LMin, need, $"{name}: the left arm barely swung.");
                Assert.Greater(late.LSpread, 6f, $"{name}: the left arm hugged the body.");
                if (holding) { Assert.Less(late.RMax, 50f, $"{name}: the slipper is still held out in front."); Assert.Less(late.RMax - late.RMin, 35f, $"{name}: the carrying hand swung the slipper about."); }
                else { Assert.Greater(late.RMax - late.RMin, need, $"{name}: the right arm barely swung."); Assert.Greater(late.RSpread, 6f, $"{name}: the right arm hugged the body."); }
                who.Teleport(from + Vector3.right * 30);
                yield return null;
            }
        }

        /// <summary>(forward of hanging, out from the body), degrees, in the character's frame.</summary>
        private static Vector2 Hang(Transform body, Transform arm, Vector3 along)
        {
            var d = body.InverseTransformDirection(arm.TransformDirection(along));
            return new Vector2(Mathf.Atan2(d.z, -d.y) * Mathf.Rad2Deg, Mathf.Atan2(Mathf.Abs(d.x), -d.y) * Mathf.Rad2Deg);
        }

        /// <summary>Measures the drawn arms and photographs them after every other LateUpdate.</summary>
        [DefaultExecutionOrder(10000)] private sealed class LateSampler : MonoBehaviour
        {
            public int Frames; public float Amount = 1, SMin = 9, SMax = -9, LMin = 999, LMax = -999, RMin = 999, RMax = -999, LSpread = 999, RSpread = 999;
            private CharacterAnimator _anim; private Camera _cam; private string _prefix; private int _shots, _taken; private float _every, _began;
            public void Begin(CharacterAnimator anim, Camera cam, string prefix, int shots, float every)
            { _anim = anim; _cam = cam; _prefix = prefix; _shots = shots; _every = every; _began = Time.time; }
            private void LateUpdate()
            {
                if (_anim == null || _anim.SwingArmLeft == null || _anim.SwingArmRight == null) return;
                Frames++; Amount = Mathf.Min(Amount, _anim.LocomotionArmAmount);
                SMin = Mathf.Min(SMin, _anim.StrideSwing); SMax = Mathf.Max(SMax, _anim.StrideSwing);
                var l = Hang(transform, _anim.SwingArmLeft, _anim.LeftArmAlong); var r = Hang(transform, _anim.SwingArmRight, _anim.RightArmAlong);
                LMin = Mathf.Min(LMin, l.x); LMax = Mathf.Max(LMax, l.x); RMin = Mathf.Min(RMin, r.x); RMax = Mathf.Max(RMax, r.x);
                LSpread = Mathf.Min(LSpread, l.y); RSpread = Mathf.Min(RSpread, r.y);
                if (_taken < _shots && Time.time - _began >= _taken * _every) Shoot();
            }
            private void Shoot()
            {
                var at = transform.position;
                var eye = at + transform.right * 2.3f + transform.forward * .6f + Vector3.up * .9f;
                _cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at + Vector3.up * .6f - eye));
                // The local first-person body draws shadows only; show it for the photograph and put it back.
                var hidden = GetComponentsInChildren<Renderer>().Where(x => x.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly).ToList();
                foreach (var h in hidden) h.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                var rt = RenderTexture.GetTemporary(720, 720, 24);
                var was = RenderTexture.active; _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(720, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 720, 720), 0, 0); tex.Apply();
                File.WriteAllBytes($"{_prefix}-{_taken}.png", tex.EncodeToPNG());
                _cam.targetTexture = null; RenderTexture.active = was; RenderTexture.ReleaseTemporary(rt); Object.Destroy(tex);
                foreach (var h in hidden) h.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                _taken++;
            }
        }

        [DefaultExecutionOrder(-300)] private sealed class MotionInput : MonoBehaviour
        {
            public Vector2 Move; public bool Sprint;
            private void Update()
            {
                var who = GetComponent<CharacterMotor>();
                who.Intent.Move = Move; who.Intent.Set(Verb.Sprint, Sprint);
                who.Intent.FaceAimPoint = true; who.Intent.AimPoint = who.transform.position + who.transform.forward * 20;
            }
        }
    }
}
