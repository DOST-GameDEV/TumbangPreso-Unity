using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso.Tests
{
    public sealed class MotionContinuityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _seat;
        private CharacterAnimator _driver;
        private CharacterMotor _motor;
        private GameObject _model;
        private RosterEntryAsset _entry;

        [SetUp]
        public void SetUp()
        {
            _entry = Resources.Load<RosterBook>("RosterBook").People.First(p => p.Id == "sean");
            _seat = new GameObject("Motion contract");
            _motor = _seat.AddComponent<CharacterMotor>();
            Invoke(_motor, "Awake");
            _driver = _seat.AddComponent<CharacterAnimator>();
            Invoke(_driver, "Awake");
            _model = Object.Instantiate(_entry.Model, _seat.transform);
            _driver.Bind(_model, _entry.Clips);
            var graph = Get<PlayableGraph>(_driver, "_graph");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            graph.Evaluate(0);
        }

        [TearDown] public void TearDown() { if (_seat != null) Object.DestroyImmediate(_seat); }

        [Test]
        public void RebindingRetiresThePreviousGraphAndItsAction()
        {
            var previous = Get<PlayableGraph>(_driver, "_graph");
            _driver.PlayAction("slide");
            Assert.IsTrue(_driver.IsPlayingAction);
            _driver.Bind(_model, _entry.Clips);
            Assert.IsFalse(previous.IsValid(), "The old graph still owns an animation output.");
            Assert.IsFalse(_driver.IsPlayingAction, "The previous model's action survives the swap.");
            Assert.IsTrue(Get<PlayableGraph>(_driver, "_graph").IsValid());
        }

        [Test]
        public void InterruptingACrossfadeStartsAtThePoseAlreadyOnScreen()
        {
            var graph = Get<PlayableGraph>(_driver, "_graph");
            _driver.PlayOneShot("holding-right");
            var mixer = Get<AnimationMixerPlayable>(_driver, "_mixer");
            mixer.SetInputWeight(0, .7f); mixer.SetInputWeight(1, .3f);
            Set(_driver, "_weight", .3f);
            graph.Evaluate(.04f);
            var arm = _model.GetComponentsInChildren<Transform>().First(t => t.name == "arm-right");
            Quaternion before = arm.localRotation;
            _driver.PlayOneShot("pick-up");
            graph.Evaluate(0);
            Assert.Less(Quaternion.Angle(before, arm.localRotation), .1f,
                "Discarding the partially blended outgoing clip snapped the arm.");
        }

        [Test]
        public void ACarryingWalkMovesBothLegsWithoutSwingingTheGrip()
        {
            Set(_motor, "_grounded", true);
            Set(_motor, "_velocity", new Vector3(0, 0, 2.4f));
            _motor.HoldingSlipper = true;
            Invoke(_driver, "Play", "holding-right", true, true);
            Set(_driver, "_weight", 1f);
            Invoke(_driver, "Blend");
            Set(_driver, "_gaitWeight", 1f);
            var graph = Get<PlayableGraph>(_driver, "_graph");
            var arm = _model.GetComponentsInChildren<Transform>().First(t => t.name == "arm-right");
            var leg = _model.GetComponentsInChildren<Transform>().First(t => t.name == "leg-right");
            Set(_driver, "_gaitPhase", .25f);
            Invoke(_driver, "StepGait"); graph.Evaluate(0);
            Quaternion firstLeg = leg.localRotation, grip = arm.localRotation;
            Set(_driver, "_gaitPhase", .75f);
            Invoke(_driver, "StepGait"); graph.Evaluate(0);
            Assert.Greater(Quaternion.Angle(firstLeg, leg.localRotation), 10f, "The moving carrier has frozen legs.");
            Assert.Less(Quaternion.Angle(grip, arm.localRotation), .1f, "The gait mask reaches the carrying arm.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MobileCarapaceCastKeepsTheFeetWalkingAndTheUpperBodyCasting(bool holding)
        {
            var art=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="dante");
            var old=_model;_model=Object.Instantiate(art.Model,_seat.transform);_driver.Bind(_model,art.Clips);Object.DestroyImmediate(old);
            var graph=Get<PlayableGraph>(_driver,"_graph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            Set(_motor,"_grounded",true);Set(_motor,"_velocity",new Vector3(0,0,2.4f));_motor.HoldingSlipper=holding;
            _driver.PlayOneShot("hero-dante-roar");Assert.True(_driver.IsPlayingAction,"The actual guard cast did not bind.");
            Set(_driver,"_weight",1f);Invoke(_driver,"Blend");
            var front=(AnimationClipPlayable)Invoke(_driver,"Front");front.SetSpeed(0);front.SetTime(.18f);
            var leg=_model.GetComponentsInChildren<Transform>().First(t=>t.name=="leg-right");
            var arm=_model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-right");
            Set(_driver,"_gaitPhase",.25f);Invoke(_driver,"AdvanceGait",.1f);graph.Evaluate(0);
            var first=leg.localRotation;var upper=arm.localRotation;
            Set(_driver,"_gaitPhase",.75f);Invoke(_driver,"AdvanceGait",.1f);graph.Evaluate(0);
            Assert.Greater(Quaternion.Angle(first,leg.localRotation),10f,"A mobile armor cast froze both feet while the motor kept moving.");
            Assert.Less(Quaternion.Angle(upper,arm.localRotation),.1f,"Walking replaced the guard's upper-body cast.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BackpedalingReversesActualFootTravelWithoutDisturbingTheCarryGrip(bool holding)
        {
            Set(_motor,"_grounded",true);_motor.HoldingSlipper=holding;
            var graph=Get<PlayableGraph>(_driver,"_graph");
            var leg=_model.GetComponentsInChildren<Transform>().First(t=>t.name=="leg-right");
            var arm=_model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-right");
            var skin=_model.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.bones.Contains(leg));
            int index=System.Array.IndexOf(skin.bones,leg);var vertices=skin.sharedMesh.vertices;
            var weights=skin.sharedMesh.boneWeights;var bind=skin.sharedMesh.bindposes[index];
            var foot=Enumerable.Range(0,vertices.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.99f)
                .Select(i=>bind.MultiplyPoint3x4(vertices[i])).ToArray();
            float bottom=foot.Min(v=>v.y);var sole=foot.Where(v=>v.y<bottom+.01f).ToArray();
            var point=sole.Aggregate(Vector3.zero,(sum,v)=>sum+v)/sole.Length;
            float Travel(float direction)
            {
                Invoke(_driver,"Play",holding?"holding-right":"walk",true,true);
                Set(_driver,"_weight",1f);Invoke(_driver,"Blend");
                Set(_motor,"_velocity",new Vector3(0,0,2.4f*direction));
                for(int i=0;i<30;i++){Invoke(_driver,"AdvanceGait",1f/60);graph.Evaluate(1f/60);}
                Set(_driver,"_gaitPhase",.12f);Invoke(_driver,"AdvanceGait",0f);
                var front=(AnimationClipPlayable)Invoke(_driver,"Front");front.SetTime(front.GetAnimationClip().length*.12f);
                graph.Evaluate(0);var before=leg.TransformPoint(point);var grip=arm.localRotation;
                Invoke(_driver,"AdvanceGait",.06f);graph.Evaluate(.06f);
                if(holding)Assert.Less(Quaternion.Angle(grip,arm.localRotation),.1f,"Foot direction changed the held grip.");
                return Vector3.Dot(leg.TransformPoint(point)-before,_model.transform.forward);
            }
            float forward=Travel(1),backward=Travel(-1);
            Assert.Less(forward*backward,-.000001f,
                "Backward travel uses the same foot sweep as forward travel: "+forward+", "+backward);
        }

        [TestCase(false, 2.53f, 3.795f)]
        [TestCase(true, 5.06f, 7.59f)]
        public void GaitFollowsObservedRoleSpeedRatherThanALocalSprintKey(bool defender, float walk, float sprint)
        {
            _motor.CharacterIndex = -1;
            _motor.IsDefender = defender;
            Set(_motor, "_grounded", true);
            Set(_motor, "_velocity", new Vector3(walk, 0, 0));
            Invoke(_driver, "StepGait");
            Assert.IsFalse(Get<bool>(_driver, "_running"));
            Set(_motor, "_velocity", new Vector3(sprint, 0, 0));
            Invoke(_driver, "StepGait");
            Assert.IsTrue(Get<bool>(_driver, "_running"));
        }

        [Test]
        public void RecoverySettlesEquallyAtThirtySixtyAndOneFortyFourFrames()
        {
            var arms = _seat.AddComponent<ViewmodelArms>();
            arms.EnsureBuilt();
            var pivot = Get<Transform>(arms, "_rightPivot");
            Vector3 expected = Vector3.zero;
            foreach (int fps in new[] { 30, 60, 144 })
            {
                pivot.localPosition = Vector3.one;
                for (int i = 0; i < fps; i++)
                    Invoke(arms, "StepToward", Vector3.zero, Quaternion.identity, Vector3.one, .2f / fps);
                if (fps == 30) expected = pivot.localPosition;
                else Assert.Less(Vector3.Distance(expected, pivot.localPosition), .00001f);
            }
        }

        /// <summary>
        /// ⚠️ THE CADENCE IS EACH CHARACTER'S OWN STRIDE TIMES ITS OWN GLIDE (2026-09-27). This used to measure the shared `walk` and
        /// `sprint` clips' feet against the calibration; the legs are now posed from each body's `GaitStyle` (the clips' legs are
        /// never drawn while walking), so the question is whether the body travels what its drawn stride covers, times the slide
        /// that character is allowed (`Gait.Glide`: these sub-metre bodies at 2.3 to 4.2 m/s would otherwise take 4 to 8 steps
        /// a second). The glide is capped so no character skates: at most 1.7, a stride covering at least 59 per cent of the
        /// ground. And every body must resolve to its OWN style, never the fallback.
        /// </summary>
        [Test]
        public void GaitCadenceMatchesTheActualFootTravelOnEveryRosterRig()
        {
            var failures = new System.Collections.Generic.List<string>();
            var rows = new System.Collections.Generic.List<string> { "id,style,gait,stride_cycle_metres,calibrated_cycle_metres,glide" };
            foreach (var entry in Resources.Load<RosterBook>("RosterBook").People)
            {
                var old = _model;
                _model = Object.Instantiate(entry.Model, _seat.transform);
                _model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                _driver.GaitSource = entry.Model.name;
                _driver.Bind(_model, entry.Clips);
                Object.DestroyImmediate(old);
                var style = _driver.Style;
                if (style == GaitStyles.Custom && !entry.Id.StartsWith("custom"))
                    failures.Add($"{entry.Id} ({entry.Model.name}) walks with the fallback style, not its own");
                var skin = _model.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.bones.Any(b => b.name == "leg-left"));
                int index = System.Array.FindIndex(skin.bones, b => b.name == "leg-left");
                float reach = (skin.sharedMesh.bindposes[index].inverse.MultiplyPoint3x4(Vector3.zero).y - skin.sharedMesh.bounds.min.y)
                    * skin.transform.TransformVector(Vector3.up).magnitude;
                foreach (string gait in new[] { "walk", "sprint" })
                {
                    var g = gait == "walk" ? style.Walk : style.Run;
                    float stride = 2f * reach * (Mathf.Sin(g.LegForward * Mathf.Deg2Rad) + Mathf.Sin(g.LegBack * Mathf.Deg2Rad)) * Mathf.Cos(g.Stance * Mathf.Deg2Rad);
                    var clip = Get<System.Collections.Generic.Dictionary<string, AnimationClip>>(_driver, "_clips")[gait];
                    float calibrated = clip.length * Get<float>(_driver, gait == "walk" ? "_walkReference" : "_runReference");
                    rows.Add($"{entry.Id},{style.Name},{gait},{stride:F4},{calibrated:F4},{g.Glide:F2}");
                    if (stride < .1f || Mathf.Abs(calibrated - stride * Mathf.Max(1f, g.Glide)) > stride * .02f)
                        failures.Add($"{entry.Id}/{gait}: stride {stride:F3} m x glide {g.Glide:F2}, calibration moves the body {calibrated:F3} m");
                    float cap = style.Floats ? 3f : 1.7f;
                    if (g.Glide < 1f || g.Glide > cap)
                        failures.Add($"{entry.Id}/{gait}: glide {g.Glide:F2} is outside 1 to {cap} (feet skate, or the cadence is a blur)");
                }
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines("Logs/gait-cadence.csv",rows);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static object Invoke(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}
