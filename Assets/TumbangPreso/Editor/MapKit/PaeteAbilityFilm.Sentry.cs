using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// MAKILING'S EMBRACE acted out: the court breaking, the claws, the three hauls, the crown, the wake; then the
        /// catch (its limbs lashing out to two players, one far and one near, and dragging them in), the roots taking
        /// their legs, the squeeze, one of them straining and breaking out, the watch, the roots letting the other go,
        /// and the sleep with what it leaves in the air. What `Abilities.PaeteSentry` and `StatusBodyMarks` do at each
        /// moment, done here by hand with the same calls.
        ///
        /// ⚠️ ONE FILM, TWO OF THE GAME'S CLOCKS. In a match the crawl out is seen in the introduction, and play picks up
        /// with the tree standing and awake and catches at once (`PaeteSentry.BodyLead`, the hand-back). So here the
        /// catch is held back with the body's own `CatchLead` until the tree is up: the arrival is filmed as a cast with
        /// no introduction would show it, and the catch as every match shows it, from a standing tree.
        ///
        /// ⚠️ THE PRISONERS ARE STAND-INS, BUILT AS THE GAME BUILDS A BODY: an unscaled root carrying the `CharacterMotor`
        /// (so the tree and the roots can read where it is and whether it is held) with the roster's Sean under it at the
        /// cast's 2.38. The first film put the motor ON the scaled model, so the roots, which are the motor's children,
        /// were drawn 2.38 times too big and swallowed the body to the head; in a match they never were that size.
        /// Nothing carries them: the film slides each in over the rules' own arrival time, and roots it when it arrives.
        /// No cutscene, no Makiling.
        /// </summary>
        private sealed class SentryFilm : FxFilm
        {
            // Where each stands when the tree reaches for it: one far and one near, both on the cameras' side.
            private static readonly Vector3[] CaughtAt = { new Vector3(4.05f, 0f, 3.25f), new Vector3(-2.30f, 0f, 2.35f) };
            // The catch waits this long past the rules' 0.3 s, so the tree is standing; and the film's tree lives this long.
            private const float Lead = 2.0f, Life = 8.6f;
            private const float BreakAt = 6.0f, StrainFor = 1.4f;
            private PaeteSentryBody _body;
            private readonly List<CharacterMotor> _prisoners = new List<CharacterMotor>();
            private readonly List<PaeteRootCoil> _coils = new List<PaeteRootCoil>();
            private readonly List<float> _arrive = new List<float>();
            private readonly List<bool> _bound = new List<bool>();
            private float _last = -1f;
            private bool _broke, _slept;

            private static float CatchAt => PaeteRules.SentryCatchSeconds + Lead;
            private float BoundAt(int i) => CatchAt + (i < _arrive.Count ? _arrive[i] : 0.8f);

            public override string Name => "sentryfx";
            // Past the tree's own life: the leaves and petals it let go of are still coming down.
            public override float Seconds => 12.0f;
            public override float[] StripTimes => new[]
            {
                0.07f, 0.30f,                               // the breach; the claws gripping
                0.47f, 0.87f, 1.27f,                        // the three hauls, each just after it starts
                1.56f, 1.84f,                               // the crown shaken at the last stop; the wake
                CatchAt + 0.07f, CatchAt + 0.26f,           // the grasp and the limbs lashing; the catch
                CatchAt + 0.55f,                            // the drag, the band cinched
                BoundAt(0) + 0.10f, BoundAt(0) + 0.42f,     // the roots coming up loose; cinched
                BoundAt(0) + 1.75f,                         // the squeeze
                BreakAt - 0.3f, BreakAt + 0.04f, BreakAt + 0.25f, // straining; the break-out
                Life + 0.12f, Life + 0.5f,                  // the roots letting go; the tree going under
                Life + 1.6f, Life + 3.0f,                   // what it leaves in the air
            };
            public override (Vector3 eye, Vector3 look, float fov)[] Views => new[]
            {
                (new Vector3(4.0f, 4.0f, 11.8f), new Vector3(0.2f, 2.9f, 0.4f), 46f),
                (new Vector3(2.0f, 3.9f, 6.0f), new Vector3(0f, 4.6f, 0f), 44f),
                // Low, at the prisoners and the foot of the tree: the limbs, the roots on their legs, the court.
                (new Vector3(0.5f, 1.35f, 5.4f), new Vector3(0f, 0.85f, 1.0f), 46f),
            };

            private static Vector3 HeldAt(int i) => CaughtAt[i].normalized * PaeteRules.SentryHoldDistance;

            public override void Begin(FxStage stage)
            {
                var host = stage.New("~SentryHost");
                _body = PaeteSentryBody.Build(host.transform);
                _body.SetFacing(Vector3.forward);
                _body.CatchLead = Lead;
                _body.LifeSeconds = Life;
                var art = RosterBook.Load()?.FindPersonArt("sean");
                if (art != null && art.Model != null)
                {
                    for (int i = 0; i < CaughtAt.Length; i++)
                    {
                        try
                        {
                            var holder = stage.New("~SentryPrisoner");
                            holder.transform.SetPositionAndRotation(CaughtAt[i], Quaternion.LookRotation(Vector3.forward));
                            var model = (GameObject)Object.Instantiate(art.Model, holder.transform, false);
                            model.transform.localPosition = Vector3.zero;
                            model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                            model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                            foreach (var animator in holder.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                            _prisoners.Add(holder.AddComponent<CharacterMotor>());
                            _coils.Add(null);
                            _bound.Add(false);
                            _arrive.Add(Mathf.Max(0.05f, PaeteRules.SentryPullArriveSeconds(CaughtAt[i].magnitude)));
                        }
                        catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] prisoner: " + e.Message); }
                    }
                }
                _body.SetTargets(_prisoners);
            }

            private bool Crossed(float t, float at) => _last < at && t >= at;

            public override void Step(FxStage stage, float t, float dt)
            {
                // `PaeteSentry.Update`'s growth cues, by hand and in its order: the cues, then the pose.
                if (Crossed(t, 0f)) PaeteEmbraceFx.Breach(Vector3.zero);
                for (int k = 0; k < PaeteSentryBody.Heaves.Length; k++)
                    if (Crossed(t, PaeteSentryBody.Heaves[k].x)) PaeteEmbraceFx.Haul(Vector3.zero, k);
                if (Crossed(t, _body.WakeAt)) PaeteEmbraceFx.Wake(_body.CrownNode, _body.EyesNode, Vector3.zero, PaeteSentryBody.Scale);

                // The drag (in a match a carry on the body: held speed, then a slide to a stop), and at its end the roots.
                for (int i = 0; i < _prisoners.Count; i++)
                {
                    var p = _prisoners[i];
                    if (p == null || _bound[i]) continue;
                    float pulled = Mathf.Clamp01((t - CatchAt) / _arrive[i]);
                    p.transform.position = Vector3.Lerp(CaughtAt[i], HeldAt(i), 1f - (1f - pulled) * (1f - pulled));
                    if (pulled < 1f) continue;
                    _bound[i] = true;
                    try { p.ApplyRooted(Life - t); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] rooted: " + e.Message); }
                    // As `PaeteSentry` asks `StatusBodyMarks` to: the roots, and the turn to face out from the trunk.
                    try { _coils[i] = PaeteRootCoil.Attach(p, Vector3.zero); }
                    catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] coil: " + e.Message); _coils[i] = p.GetComponentInChildren<PaeteRootCoil>(); }
                }

                _body.Pose(t, Vector3.zero);
                // `PaeteSentry` is destroyed 0.6 s after its life.
                if (t >= Life + 0.6f && _body.gameObject.activeSelf) _body.gameObject.SetActive(false);

                // One of them holds Interact against the roots (the film cannot hold a key: `Strain` stands in for it) and
                // breaks out; the other is held until the tree sleeps, and is let go.
                if (_coils.Count > 0 && _coils[0] != null) _coils[0].Strain = t >= BreakAt - StrainFor && t < BreakAt;
                if (!_broke && t >= BreakAt && _prisoners.Count > 0)
                {
                    _broke = true;
                    try { _prisoners[0].EndRooted(); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] break: " + e.Message); }
                }
                if (!_slept && t >= Life)
                {
                    _slept = true;
                    foreach (var p in _prisoners)
                        try { if (p != null) p.EndRooted(); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] sleep: " + e.Message); }
                }
                foreach (var coil in _coils) if (coil != null) coil.Step(dt);
                _last = t;
            }
        }
    }
}
