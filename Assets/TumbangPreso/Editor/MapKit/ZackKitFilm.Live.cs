using System;
using System.Collections.Generic;
using System.Reflection;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// MOMENTUM AND OVERCLOCK, AS THE GAME HAS THEM NOW, OUTSIDE PLAY. Neither look has a staged entry point: each is a
    /// `LateUpdate` on a real `CharacterMotor` and `ZackHeroKit`, reading `Time`. So the film builds the real components on a
    /// posed Zack and calls their own `LateUpdate` once a picture, and nothing of what they draw is written here.
    ///
    /// ⚠️ WHAT STANDS IN, AND ONLY THIS:
    ///   * the clock. `Time` does not move inside one editor call, so before each call the look's own stored times are aged
    ///     by one film step and `Time.deltaTime` is taken back out of its own accumulators;
    ///   * the pose history. No match means no `MatchPoseHistory`, so a real `Track` records this body at the game's
    ///     `Interval` and the real `Echo` copies are handed to the look by reflection;
    ///   * the spark bursts. They are real `ZackFx.Sparks`, moved out of the owner's scene and stepped by their own `Step`;
    ///   * the loop sound of Overclock is removed, since nothing should sound in the editor;
    ///   * the end of Overclock: the kit's `Release` calls `Destroy`, which edit mode refuses, so its one spark burst is fired
    ///     with the same numbers and the look is switched off.
    ///
    /// ⚠️ NOT IN THE PICTURE: his run cycle and the dash lean (`CharacterAnimator` needs Play), so the body and every copy of
    /// it hold one pose while they travel; the stretch at the boom; his own first person (the edge streaks and the lens
    /// need his camera rig); the zap (it needs `GameServices.Round` for a rival and a live `CharacterAnimator` for his arms).
    ///
    /// Columns. `momentum`: OLD is `BodyJets` on, as a check that this film would show the jets; NEW is the switch as it
    /// ships. `overclock` and `overclockend`: OLD is Momentum alone, NEW is Momentum and Overclocked together, as a match
    /// draws them.
    /// </summary>
    internal static partial class ZackKitFilm
    {
        private sealed class LiveBody
        {
            public GameObject Holder, Model;
            public Transform Fx;
            public CharacterMotor Motor;
            public ZackHeroKit Kit;
            public MatchPoseHistory.Track Track;
            public ZackMomentumLook Momentum;
            public ZackOverclockLook Overclock;
            public bool Jets;
            public float Stopped;
            public readonly List<(MonoBehaviour burst, float age)> Sparks = new List<(MonoBehaviour, float)>();
        }

        private static FieldInfo Slot(Type type, string name)
            => type.GetField(name, Any) ?? throw new MissingFieldException(type.Name, name);
        private static float GetF(object o, string name) => (float)Slot(o.GetType(), name).GetValue(o);
        private static void SetF(object o, string name, float value) => Slot(o.GetType(), name).SetValue(o, value);

        private static ZackFx.Echo EchoOf(LiveBody b, Transform parent)
        {
            var copy = b.Track.Clone(parent);
            if (copy == null) throw new InvalidOperationException("The pose track would not clone his body.");
            var make = typeof(ZackFx.Echo).GetConstructor(Any, null, new[] { typeof(MatchPoseHistory.Track), typeof(MatchPoseHistory.Copy) }, null);
            var echo = (ZackFx.Echo)make.Invoke(new object[] { b.Track, copy });
            echo.Coat(1.0f);
            return echo;
        }

        private static LiveBody MakeLive(Stage s, Plan p, Transform under, Transform fx, int slot, Vector3 at, bool jets, bool overclocked, float fuel)
        {
            var b = new LiveBody { Fx = fx, Jets = jets };
            b.Holder = s.Person(p, under, "zack", at, 0f);
            b.Model = ModelOf(b.Holder);
            b.Motor = b.Holder.AddComponent<CharacterMotor>();
            var controller = b.Holder.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            b.Holder.AddComponent<CharacterSquashStretch>();
            var system = b.Holder.AddComponent<HeroAbilitySystem>();
            b.Motor.PlayerSlot = slot;
            b.Kit = new ZackHeroKit();
            typeof(HeroAbilitySystem).GetProperty("Kit", Any).SetValue(system, b.Kit);
            b.Track = new MatchPoseHistory.Track(b.Motor, b.Model);
            b.Track.Record(-0.10f); b.Track.Record(-0.05f);

            // The old column keeps the old look so the two can be laid side by side; the new one follows the switches.
            bool oldColumn = under == p.Old.transform;
            b.Momentum = ZackMomentumLook.Attach(b.Motor, b.Kit, oldColumn ? false : ZackMomentumLook.Painted);
            b.Momentum.transform.SetParent(fx, false);
            if (oldColumn || !ZackMomentumLook.Painted)
            {
                Slot(typeof(ZackMomentumLook), "_smear").SetValue(b.Momentum, EchoOf(b, b.Momentum.transform));
                var ghosts = (ZackFx.Echo[])Slot(typeof(ZackMomentumLook), "_ghosts").GetValue(b.Momentum);
                for (int i = 0; i < ghosts.Length; i++) ghosts[i] = EchoOf(b, b.Momentum.transform);
            }

            if (overclocked)
            {
                SetFuel(b, fuel);
                b.Overclock = ZackOverclockLook.Attach(b.Motor, b.Kit, ZackOverclockLook.Painted);
                b.Overclock.transform.SetParent(fx, false);
                var sound = Slot(typeof(ZackOverclockLook), "_current");
                var bed = sound.GetValue(b.Overclock) as Object;
                sound.SetValue(b.Overclock, null);
                if (bed != null) Object.DestroyImmediate(((Component)bed).gameObject);
                if (!ZackOverclockLook.Painted)
                {
                    var frames = (ZackFx.Echo[])Slot(typeof(ZackOverclockLook), "_ghosts").GetValue(b.Overclock);
                    for (int i = 0; i < frames.Length; i++) frames[i] = EchoOf(b, b.Overclock.transform);
                }
            }
            return b;
        }

        private static void SetFuel(LiveBody b, float fuel)
            => Slot(typeof(ZackHeroKit), "<OverclockLeft>k__BackingField").SetValue(b.Kit, fuel);

        // One picture of a look: its stored times aged by the film's step, its own LateUpdate called, its sparks collected.
        private static void StepLive(LiveBody b)
        {
            float editorDt = Time.deltaTime;
            void Age(object look, params string[] names) { foreach (var n in names) SetF(look, n, GetF(look, n) - editorDt + Step); }
            void Near(object look, params string[] names) { foreach (var n in names) SetF(look, n, GetF(look, n) - Step); }

            bool keep = ZackMomentumLook.BodyJets;
            ZackMomentumLook.BodyJets = b.Jets;
            try
            {
                var m = b.Momentum;
                Age(m, "_age", "_ringAge", "_boomAge");
                Near(m, "_nextHeel", "_nextEmber");
                var route = (float[])Slot(typeof(ZackMomentumLook), "_routeTime").GetValue(m);
                for (int i = 0; i < route.Length; i++) route[i] -= Step;
                var strike = (float[])Slot(typeof(ZackMomentumLook), "_streamAge").GetValue(m);
                for (int i = 0; i < strike.Length; i++) strike[i] += Step - editorDt;
                Invoke(m);
                if (b.Overclock != null && b.Overclock.gameObject.activeSelf)
                {
                    Age(b.Overclock, "_age");
                    Near(b.Overclock, "_nextFoot");
                    Invoke(b.Overclock);
                }
            }
            finally { ZackMomentumLook.BodyJets = keep; }

            var active = SceneManager.GetActiveScene();
            if (active.isLoaded)
                foreach (var root in active.GetRootGameObjects())
                    if (root.name == "ZackSparks")
                    {
                        root.transform.SetParent(b.Fx, true);
                        foreach (var c in root.GetComponents<MonoBehaviour>()) b.Sparks.Add((c, 0f));
                    }
            for (int i = b.Sparks.Count - 1; i >= 0; i--)
            {
                var (burst, age) = b.Sparks[i];
                age += Step;
                if (age >= 0.42f) { Object.DestroyImmediate(burst.gameObject); b.Sparks.RemoveAt(i); continue; }
                b.Sparks[i] = (burst, age);
                burst.GetType().GetMethod("Step", Any).Invoke(burst, new object[] { age });
            }
        }

        private static void Invoke(MonoBehaviour look)
        {
            try { look.GetType().GetMethod("LateUpdate", Any).Invoke(look, null); }
            catch (TargetInvocationException failure) { throw failure.InnerException ?? failure; }
        }

        private static View[] LiveViews(Vector3 centre, GameObject taya, GameObject body)
        {
            var at = body.transform.position; var fwd = body.transform.forward; var right = body.transform.right;
            return new[]
            {
                new View("the taya's eyes", taya.transform.position + Vector3.up * 1.6f, at + Vector3.up * 1.0f, 50f, taya),
                new View("side", centre + new Vector3(1.5f, 2.6f, -13f), centre + Vector3.up * 0.9f, 52f),
                new View("above", centre + new Vector3(0f, 16f, -0.5f), centre, 50f),
                new View("chase", at - fwd * 3.2f + right * 1.3f + Vector3.up * 1.5f, at + Vector3.up * 0.8f, 52f),
                new View("heels, low", at + fwd * 1.0f + right * 2.4f + Vector3.up * 0.45f, at + Vector3.up * 0.45f - fwd * 0.8f, 55f),
            };
        }

        // The lap round the taya, shared by both columns. Speed is the attacker's run times his real kit scale. Stops at `stopAt`.
        private static void Drive(Stage s, Plan p, LiveBody old, LiveBody neu, Vector3 centre, GameObject taya, float startAt, float stopAt, float startFuel, float seededMomentum)
        {
            const float radius = 4.5f;
            float last = -1f, travelled = 0f, recorded = -0.05f;
            bool ended = false;
            foreach (var b in new[] { old, neu }) Slot(typeof(ZackHeroKit), "_momentum").SetValue(b.Kit, seededMomentum);
            Vector3 On(float along) { float a = along / radius; var q = centre + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius; return s.Ground(q.x, q.z); }
            Quaternion Heading(float along) { float a = along / radius; return Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up); }
            void Place() { foreach (var b in new[] { old, neu }) b.Holder.transform.SetPositionAndRotation(On(travelled), Heading(travelled)); }
            Place();
            p.Pose = (t, view, eye) =>
            {
                if (t <= last) return;
                last = t;
                bool running = t >= startAt && t < stopAt;
                foreach (var b in new[] { old, neu })
                {
                    float seconds = b.Kit.MomentumSeconds, stopped = b.Stopped;
                    seconds = ZackRules.StepMomentum(seconds, ref stopped, running, Step);
                    b.Stopped = stopped;
                    Slot(typeof(ZackHeroKit), "_momentum").SetValue(b.Kit, seconds);
                }
                if (running) travelled += Balance.AttackerRunSpeed * ZackRules.SpeedScale(neu.Kit.MomentumSeconds, neu.Overclock != null && neu.Kit.IsOverclocked) * Step;
                Place();
                if (t - recorded >= MatchPoseHistory.Interval - 1e-4f)
                {
                    recorded = t;
                    foreach (var b in new[] { old, neu }) b.Track.Record(t);
                }
                if (neu.Overclock != null && !ended)
                {
                    float fuel = startFuel - t;
                    if (fuel <= 0f)
                    {
                        ended = true;
                        SetFuel(neu, 0f);
                        if (ZackOverclockLook.Painted) neu.Overclock.Release();
                        else
                        {
                            ZackFx.Sparks(neu.Holder.transform.position + Vector3.up * 0.9f, 0.5f, 4, neu.Motor.PlayerSlot);
                            neu.Overclock.gameObject.SetActive(false);
                        }
                    }
                    else SetFuel(neu, fuel);
                }
                StepLive(old);
                StepLive(neu);
            };
            p.Views = () => LiveViews(centre, taya, neu.Holder);
        }

        private static void BuildMomentum(Stage s, Plan p)
        {
            Vector3 centre = s.Ground(0f, 0f);
            var taya = s.Person(p, null, "dante", centre, 0f);
            var start = s.Ground(0f, -4.5f);
            var old = MakeLive(s, p, p.Old.transform, p.OldFx.transform, 0, start, true, false, 0f);
            var neu = MakeLive(s, p, p.New.transform, p.NewFx.transform, 1, start, ZackMomentumLook.BodyJets, false, 0f);
            const float startAt = 0.4f, stopAt = 4.0f;
            p.Seconds = stopAt + 0.9f;
            p.Notes.Add("staged: stands to " + startAt + " s, runs a 4.5 m lap to " + stopAt + " s, stops. Stage 1 at " + (startAt + ZackRules.MomentumStageOne)
                + " s, stage 2 at " + (startAt + ZackRules.MomentumStageTwo) + " s, full at " + (startAt + ZackRules.MomentumRampSeconds) + " s");
            p.Notes.Add("old column: BodyJets on (the check); new column: BodyJets as it ships, " + ZackMomentumLook.BodyJets);
            Drive(s, p, old, neu, centre, taya, startAt, stopAt, 0f, 0f);
        }

        private static void BuildOverclock(Stage s, Plan p, bool ending)
        {
            Vector3 centre = s.Ground(0f, 0f);
            var taya = s.Person(p, null, "dante", centre, 0f);
            var start = s.Ground(0f, -4.5f);
            float fuel = ending ? 6.0f : ZackRules.OverclockSeconds;
            var old = MakeLive(s, p, p.Old.transform, p.OldFx.transform, 0, start, ZackMomentumLook.BodyJets, false, 0f);
            var neu = MakeLive(s, p, p.New.transform, p.NewFx.transform, 1, start, ZackMomentumLook.BodyJets, true, fuel);
            float startAt = ending ? 0f : 0.4f, stopAt = ending ? 7.0f : 4.0f;
            p.Seconds = ending ? 7.0f : 4.9f;
            p.Notes.Add(ending ? "staged: running at full Momentum from 6 s of fuel; stutter from 5 s left (1 s in), out at 6 s, run on to 7 s"
                               : "staged: fresh out of the cutscene with 25 s of fuel, stands to 0.4 s, runs a 4.5 m lap to 4 s, stops");
            p.Notes.Add("old column: Momentum alone; new column: Momentum and Overclocked; the zap and the cutscene are not in this film");
            Drive(s, p, old, neu, centre, taya, startAt, stopAt, fuel, ending ? ZackRules.MomentumRampSeconds : 0f);
        }
    }
}
