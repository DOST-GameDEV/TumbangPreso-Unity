using System;
using System.Collections.Generic;
using System.Reflection;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// BACKTRACK DOWN THE PATH A MATCH USES, OUTSIDE PLAY. `backtrack` films `ZackBacktrackLook.Stage`, which has no pose
    /// history and so never built the drawn body, the lead hologram or the past moments. That is why an approved film could
    /// not show what the owner saw in a match. Here the kit's own `BacktrackAbility` picks the spot and runs `OnActivate`,
    /// which runs `ZackBacktrackLook.Play` and `CharacterMotor.Teleport`, against a real `MatchPoseHistory.Track`.
    ///
    /// ⚠️ WHAT STANDS IN, AND ONLY THIS:
    ///   * the history object lives in the open scene for the film and is destroyed with it, because `Echo.Of` cannot find one in a preview
    ///     scene, and its track and the kit's path are written by reflection (`SamplePath` and its `LateUpdate` need a round);
    ///   * the run is his holder moved along the route with his model holding one pose, since `CharacterAnimator` needs Play;
    ///   * the clock: once `Play` has returned, the look is switched to its staged clock and stepped by `StagedStep`.
    ///
    /// ⚠️ NOT IN THE PICTURE: his seat ring and tag (`CharacterNameplate` needs a round), the catchable fill
    /// (`CharacterVisual.Readability` needs a round and a rendering camera rig), the lens kick, Momentum, Overclock.
    ///
    /// Columns: OLD is `PastCopies` on, what a match drew; NEW is the switch as it ships. `backtracklivefloor` is the same
    /// on a plain flat floor laid over the street.
    /// </summary>
    internal static partial class ZackKitFilm
    {
        private sealed class Rewinder
        {
            public GameObject Holder;
            public CharacterMotor Motor;
            public ZackHeroKit Kit;
            public Transform Fx;
            public ZackBacktrackLook Look;
        }

        private static Vector3 AlongRoute(Vector3[] route, float distance)
        {
            for (int i = 1; i < route.Length; i++)
            {
                float leg = Vector3.Distance(route[i - 1], route[i]);
                if (distance <= leg) return Vector3.Lerp(route[i - 1], route[i], leg > 1e-5f ? distance / leg : 0f);
                distance -= leg;
            }
            return route[route.Length - 1];
        }

        private static Rewinder MakeRewinder(Stage s, Plan p, Transform under, Transform fx, int slot, MatchPoseHistory history, Vector3[] route, float speed)
        {
            var r = new Rewinder { Fx = fx };
            r.Holder = s.Person(p, under, "zack", route[0], 0f);
            var model = ModelOf(r.Holder);
            r.Motor = r.Holder.AddComponent<CharacterMotor>();
            var controller = r.Holder.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            // `Awake` does not run in edit mode, and `Teleport` writes through this.
            Slot(typeof(CharacterMotor), "_cc").SetValue(r.Motor, controller);
            r.Holder.AddComponent<Carrier>();
            var visual = r.Holder.AddComponent<CharacterVisual>();
            Slot(typeof(CharacterVisual), "_instance").SetValue(visual, model);
            var system = r.Holder.AddComponent<HeroAbilitySystem>();
            r.Motor.PlayerSlot = slot;
            r.Kit = new ZackHeroKit();
            typeof(HeroAbilitySystem).GetProperty("Kit", Any).SetValue(system, r.Kit);

            float seconds = ZackRules.BacktrackSeconds;
            var track = new MatchPoseHistory.Track(r.Motor, model);
            ((MatchPoseHistory.Track[])Slot(typeof(MatchPoseHistory), "_tracks").GetValue(history))[slot] = track;
            void PlaceAt(float u)
            {
                var at = AlongRoute(route, u * speed);
                var ahead = AlongRoute(route, u * speed + 0.1f) - at; ahead.y = 0f;
                r.Holder.transform.position = at;
                if (ahead.sqrMagnitude > 1e-5f) r.Holder.transform.rotation = Quaternion.LookRotation(ahead, Vector3.up);
            }
            for (float u = 0f; u < seconds; u += MatchPoseHistory.Interval) { PlaceAt(u); track.Record(u - seconds); }

            // The kit's own ring of path samples, as `SamplePath` leaves it after the same run.
            var points = (Vector3[])Slot(typeof(ZackHeroKit), "_pathPoints").GetValue(r.Kit);
            var times = (float[])Slot(typeof(ZackHeroKit), "_pathTimes").GetValue(r.Kit);
            float interval = ZackRules.BacktrackSampleInterval;
            int n = Mathf.Min(points.Length, Mathf.FloorToInt(seconds / interval) + 1);
            for (int i = 0; i < n; i++) { points[i] = AlongRoute(route, i * interval * speed); times[i] = i * interval; }
            Slot(typeof(ZackHeroKit), "_pathHead").SetValue(r.Kit, n - 1);
            Slot(typeof(ZackHeroKit), "_pathCount").SetValue(r.Kit, n);
            Slot(typeof(ZackHeroKit), "_pathClock").SetValue(r.Kit, (n - 1) * interval);

            PlaceAt(seconds);
            track.Record(0f);
            return r;
        }

        // The press: the same two calls a cast makes on the owner's screen, then the look is taken onto the film's clock.
        private static void PressBacktrack(Rewinder r, bool copies, Plan p)
        {
            bool keep = ZackBacktrackLook.PastCopies;
            ZackBacktrackLook.PastCopies = copies;
            try
            {
                var ability = r.Kit.Skill1;
                var cast = ability.CaptureLocalCastContext(new AbilityContext(r.Motor, r.Motor.GetComponent<Carrier>(), null));
                var from = r.Motor.transform.position;
                var activate = ability.GetType().GetMethod("OnActivate", Any);
                try { activate.Invoke(ability, new object[] { cast }); }
                catch (TargetInvocationException failure)
                {
                    // `Play` registers the look before `Teleport` runs. A fault after that is the editor's missing match, so it
                    // is written down and the body is put where the teleport would put it; a fault before it stops the film.
                    p.Notes.Add("the cast threw: " + failure.InnerException);
                    r.Motor.transform.position = cast.AimPoint;
                }
                var live = (Dictionary<int, ZackBacktrackLook>)typeof(ZackBacktrackLook).GetField("Live", Any).GetValue(null);
                r.Look = live[r.Motor.PlayerSlot];
                r.Look.transform.SetParent(r.Fx, false);
                Slot(typeof(ZackBacktrackLook), "_staged").SetValue(r.Look, true);
                var rendered = r.Look.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                p.Notes.Add((copies ? "old" : "new") + " column press: from " + from + " to " + cast.AimPoint + ", the motor now at " + r.Motor.transform.position
                    + "; drawn: " + r.Look.DrawnPainted + " painted, " + r.Look.PaintedEchoes + " painted echoes, " + r.Look.PastMomentsChosen
                    + " past moments, lead hologram " + r.Look.TryMoment(0, out _, out _) + ", " + rendered + " skinned renderers under the look");
            }
            finally { ZackBacktrackLook.PastCopies = keep; }
        }

        private static void BuildBacktrackLive(Stage s, Plan p, bool plainFloor)
        {
            Vector3 to = s.Ground(-4f, -6f), corner = s.Ground(-4f, 4f), from = s.Ground(4f, 4f);
            var route = new[] { to, Vector3.Lerp(to, corner, 0.5f), corner, Vector3.Lerp(corner, from, 0.5f), from };
            const float speed = 6f, press = 0.25f;
            var historyGo = new GameObject("~ZackKitHistory"); // Not hidden: `FindAnyObjectByType` skips hidden objects. The film destroys it and clears the scene's changed mark.
            p.Roots.Add(historyGo);
            var history = historyGo.AddComponent<MatchPoseHistory>();
            if (plainFloor)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                s.In(floor, p);
                floor.transform.position = new Vector3(0f, Mathf.Max(to.y, corner.y, from.y) + 0.01f, 0f);
                floor.transform.localScale = Vector3.one * 8f;
                var paint = new Material(Shader.Find("Standard")) { color = new Color(0.62f, 0.60f, 0.56f), hideFlags = HideFlags.DontSave };
                paint.SetFloat("_Glossiness", 0.1f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = paint;
            }
            var taya = s.Person(p, null, "dante", s.Ground(-7.5f, -0.5f), 60f);
            var old = MakeRewinder(s, p, p.Old.transform, p.OldFx.transform, 0, history, route, speed);
            var neu = MakeRewinder(s, p, p.New.transform, p.NewFx.transform, 1, history, route, speed);
            bool pressed = false;
            p.Seconds = press + ZackBacktrackLook.PullSeconds + ZackBacktrackLook.CatchSeconds + 0.55f;
            p.Notes.Add("live path: BacktrackAbility.CaptureLocalCastContext and OnActivate, ZackBacktrackLook.Play, CharacterMotor.Teleport; pressed at " + press.ToString("0.00") + " s after an 18 m run with one corner"
                + (plainFloor ? "; a plain flat floor over the street" : ""));
            p.Clock = t =>
            {
                if (pressed || t < press) return;
                pressed = true;
                PressBacktrack(old, true, p);
                PressBacktrack(neu, false, p);
            };
            p.Pose = (t, view, eye) =>
            {
                foreach (var r in new[] { old, neu })
                    if (r.Look != null) r.Look.StagedStep(t - press, view == 0, eye);
            };
            p.Views = () =>
            {
                var at = neu.Look != null ? neu.Look.VisualFeet : from;
                Vector3 fwd = Vector3.right, centre = s.Ground(0f, -1f) + Vector3.up * 0.9f;
                return new[]
                {
                    new View("his eyes", at + Vector3.up * Eye + fwd * 0.12f, at + Vector3.up * 1.1f + fwd * 6f, 70f, old.Holder, neu.Holder),
                    new View("the taya's eyes", taya.transform.position + Vector3.up * 1.6f, at + Vector3.up * 0.9f, 50f, taya),
                    new View("side", centre + new Vector3(2f, 1.8f, -14f), centre, 52f),
                    new View("6 m follow", at + new Vector3(-4.2f, 1.7f, -4.2f), at + Vector3.up * 0.9f, 45f),
                    new View("above", centre + new Vector3(0f, 19f, -0.5f), centre, 52f),
                };
            };
        }
    }
}
