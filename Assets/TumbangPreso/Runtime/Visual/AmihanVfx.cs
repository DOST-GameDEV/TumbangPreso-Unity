using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // AMIHAN'S EFFECTS, ONE CLASS PER BEAT OF ONE ABILITY.
    //
    // Every class here follows `docs/reports/amihan-kit-2026-09-25/direction.md`: the six beats
    // (tell, release, travel, contact, linger, dissipate), bright thin edges around a darker middle
    // (`WindVfx` ribbons), depth layering, her cotton-and-thread motif, and fading by THINNING.
    // Each transient is an `IVfxTimeline`, so its whole look is a function of its age: the live
    // game, a rejoin, a replay and a probe capture all draw the same frame for the same moment.
    // Nothing here touches gameplay; the abilities (`AmihanHeroKit`) decide, these draw.
    // =============================================================================================

    /// <summary>
    /// QUICK DASH, the slipstream. Tell: a ring of air kicks off her heels. Release/travel: three
    /// strands wind along her line from start to end, the head racing ahead of the tail, with a
    /// flat streak on the road and cotton pulled into her wake. Linger: two strands curl at the
    /// far end. Dissipate: everything thins to threads.
    /// </summary>
    public sealed class AmihanDashWake : MonoBehaviour, IVfxTimeline
    {
        public const float Life = 1.05f;
        private readonly List<WindVfx.Ribbon> _strands = new List<WindVfx.Ribbon>();
        private WindVfx.Ribbon _road, _heel, _curlA, _curlB, _land;
        private readonly List<WindVfx.Ribbon> _threads = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _motif;
        private Vector3 _from, _to;
        private float _age;

        public float LifeSeconds => Life;

        public static AmihanDashWake Build(Vector3 from, Vector3 to, Transform parent = null)
        {
            var go = new GameObject("AmihanDashWake");
            if (parent != null) go.transform.SetParent(parent, true);
            from = VfxShapes.GroundPoint(from); to = VfxShapes.GroundPoint(to);
            go.transform.position = from;
            var wake = go.AddComponent<AmihanDashWake>();
            wake._from = Vector3.zero; wake._to = to - from;
            Vector3 lift = Vector3.up * 0.85f;
            Vector3 axis = wake._to;
            for (int i = 0; i < 3; i++)
            {
                // Three strands, a third of a turn apart, at three radii: near, middle and far
                // layers of one gust (direction § 2 "depth layering").
                var spine = WindVfx.Helix(lift * (0.8f + i * 0.12f), wake._to + lift * (0.75f + i * 0.1f),
                                          0.32f + i * 0.08f, 1.15f + i * 0.2f, 28, i * 120.0f, 0.55f);
                var strand = WindVfx.Build(go.transform, "SlipstreamStrand" + i, spine, 0.2f - i * 0.035f,
                                           WindVfx.AroundAxis(spine, axis), 5.0f, 0.18f, i * 3.1f);
                // v3 language (2026-10-03): the slipstream is SHEETS of air, bright rims and clear middles, like her Airburst.
                wake._strands.Add(WindVfx.InkSheet(strand, 0.55f + i * 0.04f));
            }
            var road = new List<Vector3>();
            for (int i = 0; i < 16; i++) road.Add(Vector3.Lerp(Vector3.zero, wake._to, i / 15.0f) + Vector3.up * 0.04f);
            wake._road = WindVfx.Floor(WindVfx.Build(go.transform, "SlipstreamRoad", road, 0.42f, WindVfx.Flat(road), 7.0f, 0.12f, 9.0f));
            // The kick off her heels is her kasikus, not a circle; and the landing on the reaching foot sets a small one down.
            var heel = WindVfx.Kasikus(0.32f, 0.05f);
            wake._heel = WindVfx.Floor(WindVfx.Build(go.transform, "HeelKick", heel, 0.12f, WindVfx.Flat(heel), 4.0f, 0.25f, 2.0f));
            var land = WindVfx.Kasikus(0.28f, 0.04f);
            for (int k = 0; k < land.Count; k++) land[k] += wake._to;
            wake._land = WindVfx.Floor(WindVfx.Build(go.transform, "LandingKasikus", land, 0.09f, WindVfx.Flat(land), 3.0f, 0.3f, 6.0f));
            // Two abel threads drawn along her line behind her, the thread that becomes her Airburst's warp.
            for (int i = 0; i < 2; i++)
            {
                var thread = WindVfx.Helix(lift * (0.55f + i * 0.5f), wake._to + lift * (0.6f + i * 0.45f), 0.18f + i * 0.06f, 0.6f + i * 0.25f, 24, 60 + i * 150, 0.8f);
                wake._threads.Add(WindVfx.Thread(WindVfx.Build(go.transform, "SlipThread" + i, thread, 0.035f, WindVfx.AroundAxis(thread, axis), 8.0f, 0.35f, 7.0f + i), 1 + i));
            }
            Vector3 dir = wake._to.sqrMagnitude > 0.01f ? wake._to.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            var curlA = WindVfx.Helix(wake._to + lift * 0.9f, wake._to + dir * 1.1f + lift * 1.3f + right * 0.4f, 0.35f, 0.8f, 18, 0, 0.3f);
            var curlB = WindVfx.Helix(wake._to + lift * 0.6f, wake._to + dir * 0.9f + lift * 0.5f - right * 0.45f, 0.3f, 0.9f, 18, 180, 0.3f);
            wake._curlA = WindVfx.Build(go.transform, "WakeCurlA", curlA, 0.1f, WindVfx.AroundAxis(curlA, dir), 3.0f, 0.3f, 4.0f);
            wake._curlB = WindVfx.Build(go.transform, "WakeCurlB", curlB, 0.08f, WindVfx.AroundAxis(curlB, dir), 3.0f, 0.3f, 5.0f);
            wake._motif = new WindVfx.Motif(go.transform, 14, from.x * 3.1f + from.z * 1.7f);
            wake.StepTo(0.0f);
            return wake;
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= Life) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float t = _age;
            bool calm = WindVfx.Reduced;
            // The head races the body: 0 to 1 in 0.28 s (she covers 5 m in about 0.45 s, and the
            // air arrives first). The tail follows from 0.2 s and has caught up by 0.85 s.
            float head = WindVfx.Ease(0.0f, 0.28f, t);
            float tail = WindVfx.Ease(0.2f, 0.85f, t);
            float thin = WindVfx.Ease(0.45f, 0.95f, t);
            float phase = calm ? 0.0f : t * 5.5f;
            for (int i = 0; i < _strands.Count; i++)
                _strands[i].Set(0.95f - i * 0.18f, phase + i * 0.4f, head, tail * 0.95f, thin);
            _road.Set(0.75f, phase * 1.3f, WindVfx.Ease(0.0f, 0.22f, t), WindVfx.Ease(0.15f, 0.7f, t), thin);
            float kick = WindVfx.Ease(0.0f, 0.18f, t);
            _heel.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.9f, kick);
            _heel.GameObject.transform.localRotation = Quaternion.Euler(0, kick * 25.0f, 0);
            _heel.Set(1.0f - WindVfx.Ease(0.1f, 0.4f, t), phase, 1.0f, 0.0f, WindVfx.Ease(0.05f, 0.35f, t));
            for (int i = 0; i < _threads.Count; i++)
                _threads[i].Set(0.85f - i * 0.15f, phase * 1.4f + i, WindVfx.Ease(0.03f + i * 0.04f, 0.3f + i * 0.04f, t),
                                WindVfx.Ease(0.3f, 0.95f, t), 0.2f + 0.6f * thin);
            // The landing (her reaching foot, about 0.45 s in): a small kasikus set down and opening.
            float landing = t - 0.42f;
            float landOpen = WindVfx.Ease(0.0f, 0.2f, landing);
            _land.GameObject.transform.localScale = Vector3.one;
            _land.Set(landing < 0 ? 0.0f : 0.8f * (1.0f - WindVfx.Ease(0.25f, 0.6f, landing)), phase, landOpen, 0.0f, WindVfx.Ease(0.15f, 0.55f, landing));
            float curl = WindVfx.Envelope(t, 0.28f, 0.15f, 1.0f, 0.4f);
            _curlA.Set(curl * 0.8f, phase, WindVfx.Ease(0.28f, 0.55f, t), WindVfx.Ease(0.55f, 1.0f, t), thin);
            _curlB.Set(curl * 0.7f, phase + 1, WindVfx.Ease(0.32f, 0.6f, t), WindVfx.Ease(0.6f, 1.0f, t), thin);
            Vector3 to = _to;
            _motif.Step(WindVfx.Ease(0.02f, 1.0f, t), calm ? 0.5f : 0.9f, (start, drift, u) =>
            {
                // Pulled from along her line into the space behind her, lifting and slowing.
                float along = Mathf.Lerp(0.1f, 0.95f, start.y);
                Vector3 p = to * along;
                Vector3 side = Vector3.Cross(Vector3.up, to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward);
                return p + side * start.x * 0.9f + Vector3.up * (0.3f + u * 0.9f * drift.y) + to.normalized * (u * 0.8f);
            });
        }
    }

    /// <summary>
    /// A wind CONTACT on a body: a crescent of air that wraps once round the victim in the push
    /// direction and a flat ring at their feet. It is the "what it did to someone" beat, so it is
    /// on the victim, never at the caster, and it is the same for every wind hit of hers.
    /// </summary>
    public sealed class AmihanWindHit : MonoBehaviour, IVfxTimeline
    {
        public const float Life = 0.6f;
        private WindVfx.Ribbon _crescent, _inner, _ring;
        private WindVfx.Motif _motif;
        private float _age;
        public float LifeSeconds => Life;

        public static AmihanWindHit Build(Vector3 at, Vector3 push)
        {
            var go = new GameObject("AmihanWindHit");
            go.transform.position = VfxShapes.GroundPoint(at);
            push.y = 0.0f;
            go.transform.rotation = push.sqrMagnitude > 0.001f ? Quaternion.LookRotation(push) : Quaternion.identity;
            var hit = go.AddComponent<AmihanWindHit>();
            var arc = WindVfx.Arc(0.55f, 250.0f, 26, 1.0f, -200.0f);
            for (int i = 0; i < arc.Length; i++) arc[i].y = 0.7f + i / (float)arc.Length * 0.6f;
            hit._crescent = WindVfx.InkSheet(WindVfx.Build(go.transform, "WrapCrescent", arc, 0.26f, WindVfx.Standing, 4.0f, 0.2f, 1.0f));
            var inner = WindVfx.Arc(0.42f, 200.0f, 22, 0.5f, -150.0f);
            hit._inner = WindVfx.Thread(WindVfx.Build(go.transform, "WrapInner", inner, 0.06f, WindVfx.Standing, 3.0f, 0.3f, 2.0f), 1);
            var ring = WindVfx.Kasikus(0.42f, 0.04f);
            hit._ring = WindVfx.Floor(WindVfx.Build(go.transform, "HitRing", ring, 0.11f, WindVfx.Flat(ring), 5.0f, 0.2f, 3.0f));
            hit._motif = new WindVfx.Motif(go.transform, 6, at.x * 5.3f + at.z);
            hit.StepTo(0.0f);
            return hit;
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= Life) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float t = _age / Life;
            float phase = WindVfx.Reduced ? 0.0f : _age * 6.0f;
            _crescent.GameObject.transform.localRotation = Quaternion.Euler(0, WindVfx.Ease(0.0f, 0.7f, t) * 160.0f, 0);
            _crescent.Set(1.0f - WindVfx.Ease(0.6f, 1.0f, t), phase, WindVfx.Ease(0.0f, 0.3f, t), WindVfx.Ease(0.3f, 0.95f, t), WindVfx.Ease(0.4f, 1.0f, t));
            _inner.GameObject.transform.localRotation = Quaternion.Euler(0, -WindVfx.Ease(0.0f, 0.8f, t) * 120.0f, 0);
            _inner.Set(0.8f * (1.0f - WindVfx.Ease(0.5f, 1.0f, t)), phase + 1, WindVfx.Ease(0.05f, 0.35f, t), WindVfx.Ease(0.35f, 1.0f, t), WindVfx.Ease(0.3f, 1.0f, t));
            _ring.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 2.2f, WindVfx.Ease(0.0f, 0.6f, t));
            _ring.Set(0.9f * (1.0f - WindVfx.Ease(0.3f, 0.9f, t)), phase, 1.0f, 0.0f, WindVfx.Ease(0.1f, 0.8f, t));
            _motif.Step(t, 0.9f, (start, drift, u) => new Vector3(start.x * 0.5f, 0.6f + start.y * 0.8f + u * 0.6f, u * (0.6f + drift.z)));
        }
    }

    /// <summary>
    /// FEATHERFALL's launch: unequal rising streaks carry her out of the court's pressure rim.
    /// </summary>
    public sealed class AmihanUpdraftLaunch : MonoBehaviour, IVfxTimeline
    {
        public const float Life = 1.0f;
        private readonly List<WindVfx.Ribbon> _column = new List<WindVfx.Ribbon>();
        private readonly List<float> _delays = new List<float>();
        private WindVfx.Ribbon _burst, _burstOuter, _spiral;
        private WindVfx.Motif _motif;
        private float _age, _height;
        public float LifeSeconds => Life;

        public static AmihanUpdraftLaunch Build(Vector3 at, float height)
        {
            var go = new GameObject("AmihanUpdraftLaunch");
            go.transform.position = VfxShapes.GroundPoint(at);
            var fx = go.AddComponent<AmihanUpdraftLaunch>(); fx._height = height;
            fx.Rise("NearLeft", new Vector3(-.56f,.03f,.38f), new Vector3(-.70f,height*.43f,.24f), new Vector3(-.43f,height+.44f,.09f), .055f, .01f, 1);
            fx.Rise("FarRight", new Vector3(.46f,.02f,-.47f), new Vector3(.59f,height*.58f,-.38f), new Vector3(.34f,height+.65f,-.18f), .044f, .09f, 3);
            fx.Rise("NearRight", new Vector3(.62f,.04f,.25f), new Vector3(.78f,height*.39f,.16f), new Vector3(.54f,height+.22f,-.02f), .038f, .05f, 6);
            fx.Rise("BackLeft", new Vector3(-.28f,.02f,-.64f), new Vector3(-.40f,height*.50f,-.72f), new Vector3(-.13f,height+.38f,-.43f), .061f, .13f, 9);
            fx.Rise("FrontThread", new Vector3(.04f,.03f,.65f), new Vector3(-.09f,height*.48f,.71f), new Vector3(.12f,height+.31f,.46f), .031f, .16f, 12);
            fx.Rise("LeftThread", new Vector3(-.82f,.02f,-.12f), new Vector3(-.91f,height*.33f,.03f), new Vector3(-.66f,height+.12f,.23f), .034f, .19f, 16);
            fx.Rise("BackThread", new Vector3(.18f,.04f,-.83f), new Vector3(.30f,height*.61f,-.89f), new Vector3(.04f,height+.72f,-.60f), .029f, .07f, 20);
            fx.Rise("RightThread", new Vector3(.81f,.02f,-.13f), new Vector3(.88f,height*.52f,-.25f), new Vector3(.62f,height+.39f,-.38f), .041f, .11f, 25);
            fx.Rise("InnerLift", new Vector3(-.07f,.03f,-.35f), new Vector3(.14f,height*.46f,-.49f), new Vector3(-.02f,height+.51f,-.31f), .046f, .03f, 31);
            var spiral = WindVfx.Helix(Vector3.up * .04f, Vector3.up * (height + .42f), .69f, .83f, 28, 24, .57f);
            fx._spiral = WindVfx.InkSheet(WindVfx.Build(go.transform, "ClimbingGust", spiral, .11f, WindVfx.AroundAxis(spiral, Vector3.up), 4, .16f, 35), .55f);
            // Her kasikus pressed into the court as she leaves it (v3 language: diamonds, ink-weighted to read on light tiles).
            var ring = WindVfx.Kasikus(0.58f, 0.05f, 8);
            fx._burst = WindVfx.Floor(WindVfx.Build(go.transform, "LiftRing", ring, 0.15f, WindVfx.Flat(ring), 6.0f, 0.2f, 3.0f));
            var outer = WindVfx.Kasikus(0.9f, 0.03f, 8);
            fx._burstOuter = WindVfx.Floor(WindVfx.Build(go.transform, "LiftRingOuter", outer, 0.09f, WindVfx.Flat(outer), 8.0f, 0.15f, 4.0f));
            fx._motif = new WindVfx.Motif(go.transform, 12, at.x * 2.9f + at.z * 0.3f, 0.3f);
            fx.StepTo(0.0f);
            return fx;
        }

        private void Rise(string name, Vector3 start, Vector3 bend, Vector3 top, float width, float delay, float seed)
        {
            // Film r2: at 1.3 times they still barely showed from the court; twice the authored width.
            var streak = WindVfx.Build(transform, name, new[] { start, bend, top }, width * 2.0f, _ => Vector3.right, 3, .18f, seed);
            _column.Add(name.EndsWith("Thread") ? WindVfx.Thread(streak, (int)seed) : WindVfx.InkSheet(streak, .5f));
            _delays.Add(delay);
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= Life) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float t = _age;
            float phase = WindVfx.Reduced ? 0.0f : t * 4.0f;
            for (int i = 0; i < _column.Count; i++)
            {
                float age = t - _delays[i];
                _column[i].Set(WindVfx.Reduced ? .45f : .88f, -phase, WindVfx.Ease(0, .34f, age), WindVfx.Ease(.23f, .74f, age), WindVfx.Ease(.42f, .82f, age));
            }
            _spiral.Set(WindVfx.Reduced ? .32f : .6f, phase, WindVfx.Ease(.04f, .45f, t), WindVfx.Ease(.28f, .93f, t), WindVfx.Ease(.55f, 1, t));
            _burst.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.6f, WindVfx.Ease(0.0f, 0.45f, t));
            _burst.GameObject.transform.localRotation = Quaternion.Euler(0, 45.0f * WindVfx.Ease(0.0f, 0.6f, t), 0);
            _burstOuter.GameObject.transform.localRotation = Quaternion.Euler(0, -30.0f * WindVfx.Ease(0.0f, 0.6f, t), 0);
            _burst.Set(1.0f - WindVfx.Ease(0.3f, 0.75f, t), phase, 1, 0, WindVfx.Ease(0.1f, 0.6f, t));
            _burstOuter.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.4f, WindVfx.Ease(0.05f, 0.6f, t));
            _burstOuter.Set(0.7f * (1.0f - WindVfx.Ease(0.35f, 0.85f, t)), phase * 1.4f, 1, 0, WindVfx.Ease(0.15f, 0.7f, t));
            float h = _height;
            _motif.Step(t, 0.9f, (start, drift, u) =>
            {
                float a = start.x * 12.0f + u * 5.0f;
                float r = 0.35f + start.z * 0.5f + u * 0.4f;
                return new Vector3(Mathf.Cos(a) * r, u * (h + 0.6f) * (0.5f + drift.y * 0.5f), Mathf.Sin(a) * r);
            });
        }
    }

    /// <summary>
    /// FEATHERFALL support: open curls at the limbs and air rising beside the body.
    /// The legacy component name is retained, but no flat ring travels with her feet.
    /// </summary>
    public sealed class AmihanHoverRing : MonoBehaviour
    {
        private CharacterMotor _body;
        private Abilities.HeroKit _kit;
        private AmihanFlightPose _pose;
        private WindVfx.Ribbon _legA, _legB, _handA, _handB, _liftA, _liftB, _liftC;
        private WindVfx.Ribbon _dustA, _dustB, _dustC;
        private Transform _groundAnchor;
        private float _age, _fade, _landing = -1;
        private bool _descending, _cancelled;
        private int _movementEpoch;

        public static AmihanHoverRing Attach(CharacterMotor body, AmihanFlightPose pose = null)
        {
            if (body == null) return null;
            foreach (var existing in body.GetComponentsInChildren<AmihanHoverRing>())
                if (!existing._cancelled && existing._kit == body.AbilitySystem?.Kit && existing._movementEpoch == body.MovementEpoch)
                {
                    if (pose != null) existing._pose = pose;
                    if (body.IsAloft) existing._landing = -1;
                    return existing;
                }
                else Destroy(existing.gameObject);
            var go = new GameObject("AmihanHoverRing");
            go.transform.SetParent(body.transform, false);
            var fx = go.AddComponent<AmihanHoverRing>(); fx._body = body; fx._kit = body.AbilitySystem?.Kit;
            fx._movementEpoch = body.MovementEpoch;
            fx._pose = pose != null ? pose : body.GetComponent<AmihanFlightPose>();
            fx._legA = Curl(go.transform, "LeftShinCurl", new Vector3(-.16f, -.06f, .08f), new Vector3(-.16f, .58f, .08f), .20f, .72f, .055f, 3);
            fx._legB = Curl(go.transform, "RightShinCurl", new Vector3(.16f, .02f, -.06f), new Vector3(.16f, .76f, -.06f), .17f, .91f, .045f, 4);
            fx._handA = Curl(go.transform, "LeftWristCurl", Vector3.down * .10f, Vector3.up * .13f, .12f, .64f, .035f, 8);
            fx._handB = Curl(go.transform, "RightWristCurl", Vector3.down * .08f, Vector3.up * .17f, .10f, .79f, .030f, 12);
            fx._liftA = WindVfx.Build(go.transform, "RisingAirLeft", new[] { new Vector3(-.58f,-.75f,-.08f), new Vector3(-.64f,.18f,-.02f), new Vector3(-.48f,1.35f,.16f) }, .045f, _ => Vector3.right, 3, .22f, 15);
            fx._liftB = WindVfx.Build(go.transform, "RisingAirBack", new[] { new Vector3(.24f,-.55f,-.52f), new Vector3(.34f,.42f,-.58f), new Vector3(.18f,1.65f,-.36f) }, .032f, _ => Vector3.right, 4, .18f, 21);
            fx._liftC = WindVfx.Build(go.transform, "RisingAirRight", new[] { new Vector3(.51f,-.92f,.18f), new Vector3(.65f,.08f,.23f), new Vector3(.46f,1.02f,.35f) }, .038f, _ => Vector3.forward, 3, .20f, 28);
            var anchor = new GameObject("GroundMark").transform; anchor.SetParent(go.transform, false);
            fx._groundAnchor = anchor;
            var dustA = WindVfx.Arc(.62f, 115, 14, .02f, 15);
            var dustB = WindVfx.Arc(.87f, 82, 11, .035f, 176);
            var dustC = WindVfx.Arc(.48f, 68, 10, .045f, 282);
            fx._dustA = WindVfx.Build(anchor, "CourtDustLeft", dustA, .065f, WindVfx.Flat(dustA), 4, .12f, 5);
            fx._dustB = WindVfx.Build(anchor, "CourtDustBack", dustB, .04f, WindVfx.Flat(dustB), 3, .10f, 7);
            fx._dustC = WindVfx.Build(anchor, "CourtDustNear", dustC, .05f, WindVfx.Flat(dustC), 3, .10f, 9);
            var dust = new Color(.50f, .52f, .39f, 1);
            fx._dustA.Recolour(WindVfx.Cotton, dust, WindVfx.Ink);
            fx._dustB.Recolour(WindVfx.Cotton, dust, WindVfx.Ink);
            fx._dustC.Recolour(WindVfx.Cotton, dust, WindVfx.Ink);
            return fx;
        }

        private static WindVfx.Ribbon Curl(Transform parent, string name, Vector3 from, Vector3 to, float radius, float turns, float width, float seed)
        {
            var spine = WindVfx.Helix(from, to, radius, turns, 18, seed * 31, .65f);
            return WindVfx.Build(parent, name, spine, width, WindVfx.AroundAxis(spine, Vector3.up), 4, .20f, seed);
        }

        public void Cancel() { _cancelled = true; _landing = -1; }

        private void LateUpdate()
        {
            if (_body == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt;
            if (_body.AbilitySystem?.Kit != _kit || _body.MovementEpoch != _movementEpoch
                || _body.IsTagged || _body.IsRooted || _body.IsTripped || _body.IsSwimming || _body.IsEdgeRecovering) Cancel();
            bool flying = !_cancelled && _body.IsFlying;
            if (!_cancelled && _descending && _body.IsGrounded && !_body.IsFlying)
            {
                _landing = 0;
                // Every peer observes contact locally; never rebroadcast this presentation cue.
                using (NetCue.SuppressRelay()) NetCue.PlayVaried("sfx_amihan_updraft_settle", _body.transform.position, .96f, 1.04f, .8f);
            }
            _descending = !_cancelled && _body.IsFlying && !_body.IsAloft;
            if (_landing >= 0) _landing += dt;
            _fade = Mathf.Clamp01(_fade + (flying ? dt * 4 : -dt * 4));
            if (!flying && _fade <= 0 && (_landing < 0 || _landing > .45f)) { Destroy(gameObject); return; }
            float phase = WindVfx.Reduced ? 0.0f : _age * 3.0f;
            float a = _fade * (WindVfx.Reduced ? .55f : 1);
            float breathe = 0.5f + 0.5f * Mathf.Sin(_age * 2.2f);
            _legA.Set(.55f * a, phase, Mathf.Lerp(.66f, 1, breathe), 0, .3f);
            _legB.Set(.43f * a, phase + 1, 1, Mathf.Lerp(0, .22f, breathe), .35f);
            Wrist(_handA, _pose != null ? _pose.LeftPalm : null, .48f * a, phase);
            Wrist(_handB, _pose != null ? _pose.RightPalm : null, .37f * a, phase + 1.1f);
            _liftA.Set(.38f * a, -phase * 1.3f, 1, 0, .35f);
            _liftB.Set(.26f * a, -phase * 1.1f + 2, 1, 0, .45f);
            _liftC.Set(.31f * a, -phase * 1.5f + 4, 1, 0, .4f);

            // The road mark: straight down from her, on whatever surface is under her.
            Vector3 feet = _body.transform.position;
            float ground = VfxShapes.GroundAt(feet, feet.y - 3.0f, 5.0f);
            float gap = Mathf.Max(0.0f, feet.y - ground);
            _groundAnchor.position = new Vector3(feet.x, ground + 0.03f, feet.z);
            _groundAnchor.rotation = Quaternion.identity;
            float landing = _landing >= 0 ? 1 - Mathf.Clamp01(_landing / .45f) : 0;
            float dust = Mathf.Clamp01(gap / 1.2f) * .23f * a + landing * .5f;
            _groundAnchor.localScale = Vector3.one * (1 + (_landing >= 0 ? Mathf.Clamp01(_landing / .45f) * .65f : gap * .08f));
            _dustA.Set(dust, phase * .6f, 1, 0, .4f + .5f * (1 - _fade));
            _dustB.Set(dust * .65f, phase * .45f + 1, 1, 0, .5f);
            _dustC.Set(dust * .8f, phase * .7f + 2, 1, 0, .45f);
        }

        private static void Wrist(WindVfx.Ribbon ribbon, Transform palm, float alpha, float phase)
        {
            if (palm == null) { ribbon.Set(0, 0); return; }
            ribbon.GameObject.transform.SetPositionAndRotation(palm.position, palm.rotation);
            ribbon.Set(alpha, phase, 1, 0, .3f);
        }
    }

    /// <summary>
    /// WHIRLWIND, the gale: a curved front of layered ribbons rolling along the road, bright leading
    /// edge, lower and dimmer layers behind it, a dust skirt on the ground and cotton torn up into
    /// it. It moves on its own clock from its accepted origin, so every peer draws it in the same
    /// place (`AmihanGale` owns the contact).
    /// </summary>
    public sealed class AmihanGaleFront : MonoBehaviour, IVfxTimeline
    {
        private readonly List<WindVfx.Ribbon> _layers = new List<WindVfx.Ribbon>();
        private WindVfx.Ribbon _skirt, _spray;
        private readonly List<WindVfx.Ribbon> _crest = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _motif;
        private Vector3 _origin, _forward;
        private float _age, _life, _speed, _start;
        public float LifeSeconds => _life;

        public static AmihanGaleFront Build(Transform parent, Vector3 origin, Vector3 forward, float life, float speed, float start, float width)
        {
            var go = new GameObject("AmihanGaleFront");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<AmihanGaleFront>();
            forward.y = 0.0f; fx._forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            fx._origin = origin; fx._life = life; fx._speed = speed; fx._start = start;
            // The front is an arc bowed forward: radius from the chord and the bow.
            float bow = AmihanRules.WhirlwindBow, half = width * 0.5f;
            float radius = (half * half + bow * bow) / (2.0f * bow);
            float degrees = 2.0f * Mathf.Asin(Mathf.Clamp01(half / radius)) * Mathf.Rad2Deg;
            for (int i = 0; i < 5; i++)
            {
                // Five layers: the leading edge tallest and brightest, the later ones lower, a step
                // behind, a little wider, dimmer. Near, middle and far in one travelling front.
                var arc = WindVfx.Arc(radius + i * 0.05f, degrees * (1.0f + i * 0.06f), 26, 0.0f);
                for (int k = 0; k < arc.Length; k++)
                {
                    // Film r2: inked it read, but as a low sickle under 1.1 m; a wall, the leading sheet to 1.6 m.
                    arc[k] += new Vector3(0, 0.3f + (4 - i) * 0.33f, -radius + bow - i * 0.28f);
                    float edge = Mathf.Abs(k / (float)(arc.Length - 1) * 2.0f - 1.0f);
                    arc[k].y *= 1.0f - edge * edge * 0.55f; // the ends of the front sit lower
                }
                // v3 language: each layer a SHEET, its rim bright and its middle clear, the leading edge brightest.
                fx._layers.Add(WindVfx.InkSheet(WindVfx.Build(go.transform, "GaleLayer" + i, arc, 0.55f - i * 0.06f, WindVfx.Standing, 5.0f + i,
                    i == 0 ? 0.22f : 0.14f, i * 2.3f), 0.5f + i * 0.05f));
            }
            var skirt = WindVfx.Arc(radius, degrees * 1.08f, 26, 0.04f);
            for (int k = 0; k < skirt.Length; k++) skirt[k] += new Vector3(0, 0, -radius + bow - 0.35f);
            fx._skirt = WindVfx.Floor(WindVfx.Build(go.transform, "GaleSkirt", skirt, 0.7f, WindVfx.Flat(skirt), 8.0f, 0.1f, 11.0f));
            // Three abel threads riding the crest: the gale carries her loom's thread like everything she makes.
            for (int i = 0; i < 3; i++)
            {
                var crest = WindVfx.Arc(radius - 0.05f * i, degrees * (0.9f - i * 0.12f), 22, 0.0f);
                for (int k = 0; k < crest.Length; k++)
                {
                    float edge = Mathf.Abs(k / (float)(crest.Length - 1) * 2.0f - 1.0f);
                    crest[k] += new Vector3(0, (1.25f - i * 0.28f) * (1.0f - edge * edge * 0.55f) + 0.06f * Mathf.Sin(k * 0.9f + i), -radius + bow + 0.04f - i * 0.12f);
                }
                fx._crest.Add(WindVfx.Thread(WindVfx.Build(go.transform, "GaleThread" + i, crest, 0.04f, WindVfx.Standing, 8.0f, 0.35f, 15.0f + i), i + 1));
            }
            var spray = WindVfx.Arc(radius + 0.15f, degrees * 0.8f, 20, 1.6f);
            for (int k = 0; k < spray.Length; k++) spray[k] += new Vector3(0, 0, -radius + bow + 0.1f);
            fx._spray = WindVfx.Build(go.transform, "GaleCrest", spray, 0.1f, WindVfx.Standing, 3.0f, 0.4f, 13.0f);
            fx._motif = new WindVfx.Motif(go.transform, 16, origin.x * 1.3f + origin.z * 2.1f);
            fx.StepTo(0.0f);
            return fx;
        }

        /// <summary>Where the front's middle is at this age (world). The same sum `AmihanGale` hits with.</summary>
        public static Vector3 FrontAt(Vector3 origin, Vector3 forward, float speed, float start, float age)
            => origin + forward * (start + speed * Mathf.Max(0.0f, age));

        private void Update() { StepTo(_age + Time.deltaTime); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Clamp(seconds, 0.0f, _life);
            float t = _age;
            var at = FrontAt(_origin, _forward, _speed, _start, t);
            transform.position = VfxShapes.GroundPoint(at);
            transform.rotation = Quaternion.LookRotation(_forward);
            float phase = WindVfx.Reduced ? 0.0f : t * 7.0f;
            // Tell: the front unrolls from the middle out in the first 0.18 s. Dissipate: from
            // 2.0 s it frays (thins) and the upper layers drop away first.
            float open = WindVfx.Ease(0.0f, 0.18f, t);
            float fray = WindVfx.Ease(_life - 0.55f, _life, t);
            for (int i = 0; i < _layers.Count; i++)
            {
                float alpha = (1.0f - i * 0.15f) * (1.0f - WindVfx.Ease(_life - 0.45f + i * 0.05f, _life, t));
                float headroom = 0.5f + open * 0.5f;
                _layers[i].Set(alpha, phase * (1.0f + i * 0.15f), headroom, 0.5f - open * 0.5f, fray * (0.6f + i * 0.08f));
            }
            _skirt.Set(0.6f * (1.0f - fray), phase * 1.5f, 1, 0, fray);
            for (int i = 0; i < _crest.Count; i++)
                _crest[i].Set((0.85f - i * 0.15f) * (1.0f - fray), phase * 1.8f + i, 0.5f + open * 0.5f, 0.5f - open * 0.5f, 0.2f + fray * 0.7f);
            _spray.Set(0.7f * open * (1.0f - fray), phase * 2.0f, 1, 0, 0.3f + fray * 0.7f);
            _motif.Step(Mathf.Repeat(t / 0.9f, 1.0f), 0.85f * (1.0f - fray), (start, drift, u) =>
                new Vector3((start.x) * AmihanRules.WhirlwindWidth, 0.15f + u * (0.8f + drift.y), -0.2f - u * 1.4f));
        }
    }

    /// <summary>
    /// AIRBURST, live (`docs/reports/amihan-presentation-2026-10-02/direction.md`). The windup is the other players'
    /// dodge window, so it reads as wind being HELD, never already blowing (the Miks reference: an outline at once,
    /// then a floor that fills like a meter).
    ///
    /// GATHER: the two edges are the exact 30 degree contact limit (`AmihanStorm.InsideFan`) and stay the brightest
    /// lines. Inside them six kasikus chevrons, the front corners of her graduated whirlwind diamonds (|x| + z = r),
    /// contract TOWARD her in three steps that land on the body's two pack beats and its draw back, brighter on each,
    /// so the floor says where and how soon. Their arms stop short of the edges, so nothing decorative touches or
    /// crosses the real limit. Two small diamonds tighten at her feet; cotton and thread are drawn in to her hands.
    /// RELEASE (the instant the host throws everyone): every chevron bursts outward at once, a standing chevron front
    /// crosses the 14 m court within about three frames so nobody is seen flying before the wind reaches them, the
    /// sigil flashes under her and cotton flies out. Everything thins to threads and is gone by gather + 1.0 s.
    ///
    /// Negative ages draw the fan ON (edges racing out from her feet, chevrons appearing far to near) for the cutscene's
    /// last shot (`HeroIntroductionScene.Amihan.cs`), whose final frame is this fan at age 0.
    ///
    /// FEEL PASS (2026-10-02, the F3 films): on Bayan Plaza's light tiles the mint strokes nearly vanished, and the release
    /// sigil grew to 6 m diamonds BEHIND her, outside the fan. The floor strokes are now ink-weighted in her darker greens
    /// (a dark rim round a mint line, like the cast's ink outlines), the edges are thicker and set INSIDE the true limit so
    /// their outer ink is exactly the 30 degree contact line, the meter starts brighter, and the sigil stays under her feet.
    ///
    /// AIRBURST v3 (2026-10-03, `docs/reports/amihan-presentation-2026-10-02/airburst-v3.md`; the owner: *"theres legit no vfx
    /// and shit"*). The fan is now her LOOM. WARP: nine threads strung from her hands to the far end of the fan, drawn on along
    /// the cutscene's flick over 1.8 s, pulled taut and brighter on each pack beat, straining hardest on the draw. GATHER: three
    /// wind sheets orbit close round her (inside the sigil's reach) and dive into her hands on the draw; a small kasikus emblem
    /// glows between her palms. RELEASE, the beater: the warp snaps forward and races out, THREE layered wind fronts (broad
    /// sheets, bright rims, clear middles) cross the court within about four frames, ten streaks comb the floor outward, and
    /// cotton, thread and scraps fly down the lane. Every decorative piece away from her body stays inside the 60 degrees;
    /// reduced effects keep every shape and drop the scraps.
    /// </summary>
    public sealed class AmihanStormFan : MonoBehaviour, IVfxTimeline
    {
        public const int Chevrons = 6;
        /// <summary>How long the release takes to clear; replay derives the field's life from it.</summary>
        public const float WallSeconds = 0.6f;
        /// <summary>The cutscene draws the fan on over this long before age 0: from the flick (3.78 of 5.6 s) to the hand-back.</summary>
        public const float DrawOnSeconds = 1.8f;
        public const int WarpThreads = 9, CombStreaks = 10, Fronts = 3;
        private const float FirstSlot = 2.6f, SlotSpacing = 2.4f, ChevronWidth = 0.24f, EdgeWidth = 0.26f;
        /// <summary>Share of the way from a chevron's apex to the fan edge that its arms reach (short of the thicker edge).</summary>
        private const float ArmReach = 0.74f;
        /// <summary>Her darker greens for strokes lying on the court: a mint line inside a wind-green body inside deep ink.</summary>
        private static readonly Color FloorBody = new Color(0.40f, 0.66f, 0.29f, 1.0f), FloorInk = new Color(0.11f, 0.27f, 0.10f, 1.0f);
        private readonly List<WindVfx.Ribbon> _chevrons = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _front = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _sigil = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _feet = new List<WindVfx.Ribbon>();
        // v3.2 THE RING: the blast round her (`AmihanRules.StormSurgeAroundRadius`): her kasikus on the court at its edge, and two
        // standing rings of wind racing out to it on the release, so the players behind her see what took them.
        private WindVfx.Ribbon _aroundEdge;
        // THE ABEL (`abel-cloth-direction.md`): the blast is cloth off her loom. A standing woven curtain racing out across the
        // half map, a cloth skirt snapping out to the ring, and six long sashes flung off her palms down the blast.
        private AbelCloth _clothFront, _clothSkirt;
        private readonly System.Collections.Generic.List<AbelCloth> _sashes = new System.Collections.Generic.List<AbelCloth>();
        private Vector3[] _clothCentre, _clothAcross;
        private const int ClothFrontSamples = 72, ClothSkirtSamples = 64, SashSamples = 26;
        // THE SASHES: direction (share of the half angle), length, width, launch delay, whip frequency, lift.
        private static readonly float[,] SashRows =
        {
            { -0.08f, 13.0f, 0.55f, 0.00f, 2.2f, 1.1f }, { 0.22f, 10.5f, 0.45f, 0.03f, 2.7f, 1.6f }, { -0.45f, 9.0f, 0.42f, 0.05f, 2.4f, 0.8f },
            { 0.55f, 11.5f, 0.50f, 0.02f, 2.0f, 1.3f }, { -0.78f, 8.0f, 0.38f, 0.07f, 3.0f, 1.0f }, { 0.82f, 8.5f, 0.40f, 0.06f, 2.6f, 1.4f },
        };
        private readonly List<WindVfx.Ribbon> _ringFront = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _warp = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _sheets = new List<WindVfx.Ribbon>();
        private readonly List<Transform> _sheetHosts = new List<Transform>();
        private readonly List<WindVfx.Ribbon> _emblem = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _comb = new List<WindVfx.Ribbon>();
        private Transform _emblemHost;
        private WindVfx.Ribbon _edgeLeft, _edgeRight;
        private WindVfx.Motif _drawn, _flung, _scraps;
        /// <summary>Her cupped hands at the hip, in the fan's frame (the windup body's coil).</summary>
        private static readonly Vector3 Hands = new Vector3(0.38f, 0.62f, 0.1f);
        // GATHERING SHEETS: radius, low and high height, start angle, sweep (degrees), width, tilt (degrees), orbit (degrees/s).
        private static readonly float[,] SheetRows =
        {
            // Under her eyes (film r1: at 1.25 to 1.55 m they swept across her own first-person view and her aim).
            { 1.05f, 0.30f, 0.95f,   0.0f, 150.0f, 0.36f,  22.0f,  130.0f },
            { 1.25f, 0.95f, 0.40f, 120.0f, 135.0f, 0.30f, -18.0f, -110.0f },
            { 1.15f, 0.45f, 1.05f, 240.0f, 160.0f, 0.28f,  14.0f,  150.0f },
        };
        private float _age, _gather, _half, _range;
        public float LifeSeconds => _gather + WallSeconds + 0.4f;
        public float Gather => _gather;

        public static AmihanStormFan Build(Transform parent, Vector3 origin, Vector3 forward, float gather)
        {
            var go = new GameObject("AmihanStormFan");
            go.transform.SetParent(parent, false);
            forward.y = 0.0f;
            go.transform.SetPositionAndRotation(CourtUnder(origin) + Vector3.up * 0.03f,
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward));
            var fx = go.AddComponent<AmihanStormFan>();
            fx._gather = Mathf.Max(0.1f, gather); fx._half = AmihanRules.StormSurgeHalfAngle; fx._range = Abilities.AmihanStorm.FanRange;
            var left = new List<Vector3>(); var right = new List<Vector3>();
            float half = fx._half * Mathf.Deg2Rad;
            // Each edge's centre line sits half its width INSIDE the true limit, so the outer ink is the contact line itself.
            var inLeft = new Vector3(Mathf.Cos(half), 0, Mathf.Sin(half)) * (EdgeWidth * 0.5f);
            var inRight = new Vector3(-Mathf.Cos(half), 0, Mathf.Sin(half)) * (EdgeWidth * 0.5f);
            for (int i = 0; i < 24; i++)
            {
                float r = Mathf.Lerp(0.7f, fx._range, i / 23.0f);
                left.Add(new Vector3(-Mathf.Sin(half) * r, 0.02f, Mathf.Cos(half) * r) + inLeft);
                right.Add(new Vector3(Mathf.Sin(half) * r, 0.02f, Mathf.Cos(half) * r) + inRight);
            }
            fx._edgeLeft = Floor(WindVfx.Build(go.transform, "FanEdgeLeft", left, EdgeWidth, WindVfx.Flat(left), 12.0f, 0.3f, 20.0f));
            fx._edgeRight = Floor(WindVfx.Build(go.transform, "FanEdgeRight", right, EdgeWidth, WindVfx.Flat(right), 12.0f, 0.3f, 21.0f));
            for (int i = 0; i < Chevrons; i++)
            {
                var spine = Chevron(fx._half, FirstSlot + SlotSpacing * i, 0.025f);
                fx._chevrons.Add(Floor(WindVfx.Build(go.transform, "KasikusChevron" + i, spine, ChevronWidth * (1.0f + i * 0.08f),
                    WindVfx.Flat(spine), 3.0f, 0.3f, 60.0f + i)));
            }
            // THE BEATER: three layered release fronts, standing chevrons at unit radius scaled outward, lower at their arms;
            // broad sheets with bright rims and clear middles, so the can, slippers and chalk read through them.
            for (int i = 0; i < Fronts; i++)
            {
                var spine = Chevron(fx._half, 1.0f, 0.0f);
                for (int k = 0; k < spine.Count; k++)
                {
                    float edge = Mathf.Abs(k / (float)(spine.Count - 1) * 2.0f - 1.0f);
                    spine[k] = new Vector3(spine[k].x, (1.05f - i * 0.28f) * (1.0f - edge * edge * 0.45f), spine[k].z);
                }
                var front = WindVfx.Build(go.transform, "AirburstFront" + i, spine, 1.5f - i * 0.4f, WindVfx.Standing, 3.0f + i * 2.0f, 0.16f, 30.0f + i);
                // Film r1: a bright rim over Bayan Plaza's light tiles was invisible from the court view. The front's rim is her ink,
                // its middle mint you can see through: it reads on a light court and on a dark one.
                front.Recolour(WindVfx.Core, WindVfx.SheetBody, WindVfx.FloorInk);
                if (front.Material != null) { front.Material.SetFloat("_InkFrom", 0.55f); front.Material.SetFloat("_InkAlpha", 0.95f); }
                fx._front.Add(front);
            }
            // THE WARP: nine threads from her hands to the far end, sagging to the court, each in an abel colour; their far
            // ends sit at 92 percent of the half angle, so nothing touches the real limit.
            for (int i = 0; i < WarpThreads; i++)
            {
                float a = Mathf.Lerp(-0.92f, 0.92f, i / (float)(WarpThreads - 1)) * half;
                float reach = fx._range * (0.97f - 0.03f * (i % 2));
                var far = new Vector3(Mathf.Sin(a) * reach, 0.1f + 0.04f * (i % 3), Mathf.Cos(a) * reach);
                var control = Vector3.Lerp(Hands, far, 0.3f) + Vector3.up * (0.25f + 0.05f * (i % 3));
                var spine = new List<Vector3>(28);
                for (int k = 0; k < 28; k++)
                {
                    float u = k / 27.0f;
                    spine.Add(Vector3.Lerp(Vector3.Lerp(Hands, control, u), Vector3.Lerp(control, far, u), u));
                }
                // 9 to 11 cm: film r1's 5 cm read as hairlines at the far end of the lane.
                var thread = WindVfx.Build(go.transform, "WarpThread" + i, spine, 0.11f - 0.02f * (i % 2), WarpSide(spine), 8.0f, 0.35f, 90.0f + i);
                // Film r1 (the court view): the cream thread vanished on Bayan Plaza's cream tiles. Teal, rust and gold, inked.
                var colour = WindVfx.Threads[1 + i % 3];
                thread.Recolour(Color.Lerp(colour, WindVfx.Core, 0.45f), colour, WindVfx.FloorInk);
                if (thread.Material != null) { thread.Material.SetFloat("_InkFrom", 0.55f); thread.Material.SetFloat("_InkAlpha", 0.9f); }
                fx._warp.Add(thread);
            }
            // THE COMB: ten streaks lying on the court, racing outward on the release.
            for (int i = 0; i < CombStreaks; i++)
            {
                float a = Mathf.Lerp(-0.86f, 0.86f, (i * 7 % CombStreaks) / (float)(CombStreaks - 1)) * half;
                var spine = new List<Vector3>(16);
                for (int k = 0; k < 16; k++)
                {
                    float r = Mathf.Lerp(1.2f, fx._range * 0.95f, k / 15.0f);
                    spine.Add(new Vector3(Mathf.Sin(a) * r, 0.03f, Mathf.Cos(a) * r));
                }
                fx._comb.Add(Floor(WindVfx.Build(go.transform, "CombStreak" + i, spine, 0.16f + 0.03f * (i % 3), WindVfx.Flat(spine), 2.0f, 0.3f, 70.0f + i)));
            }
            // THE GATHERING SHEETS, each on its own host so it can orbit and dive into her hands. Close to her (1.05 to 1.25 m,
            // inside the sigil's reach): they are her, not an area.
            for (int i = 0; i < SheetRows.GetLength(0); i++)
            {
                var host = Host(go.transform, "GatherSheetHost" + i);
                float r = SheetRows[i, 0], h0 = SheetRows[i, 1], h1 = SheetRows[i, 2], a0 = SheetRows[i, 3], sweep = SheetRows[i, 4];
                float tilt = SheetRows[i, 6] * Mathf.Deg2Rad;
                var spine = new List<Vector3>(22); var radial = new List<Vector3>(22);
                for (int k = 0; k < 22; k++)
                {
                    float u = k / 21.0f, aa = (a0 + sweep * u) * Mathf.Deg2Rad;
                    var outward = new Vector3(Mathf.Sin(aa), 0, Mathf.Cos(aa));
                    spine.Add(outward * r * (1.0f - 0.18f * u) + Vector3.up * Mathf.Lerp(h0, h1, u)); radial.Add(outward);
                }
                float cos = Mathf.Cos(tilt), sin = Mathf.Sin(tilt);
                fx._sheets.Add(Sheet(WindVfx.Build(host, "GatherSheet" + i, spine, SheetRows[i, 5], k => Vector3.up * cos - radial[k] * sin,
                    3.0f, 0.16f, 95.0f + i), 0.55f));
                fx._sheetHosts.Add(host);
            }
            // THE EMBLEM between her palms: three small graduated kasikus diamonds, upright, gold inside cream inside mint.
            fx._emblemHost = Host(go.transform, "HandKasikus");
            fx._emblemHost.localPosition = Hands + new Vector3(0, 0.04f, 0.08f);
            Color[] ec = { WindVfx.Gold, WindVfx.Cotton, WindVfx.Body };
            for (int i = 0; i < ec.Length; i++)
            {
                var spine = UprightDiamond(0.06f + 0.045f * i);
                var ring = WindVfx.Build(fx._emblemHost, "HandKasikus" + i, spine, i == 0 ? 0.03f : 0.024f, UprightSide(spine), 4.0f, 0.35f, 105.0f + i);
                ring.Recolour(Color.Lerp(ec[i], WindVfx.Core, 0.5f), ec[i], WindVfx.Ink);
                fx._emblem.Add(ring);
            }
            // The kasikus sigil under her feet, flashing on the release. Kept small: it is her mark, not an area.
            for (int i = 0; i < 4; i++) fx._sigil.Add(Floor(Diamond(go.transform, "Kasikus" + i, 0.42f + i * 0.2f, 0.1f, 40.0f + i)));
            // Two small diamonds at her feet that tighten on the beats.
            for (int i = 0; i < 2; i++) fx._feet.Add(Floor(Diamond(go.transform, "HeldDiamond" + i, 0.85f + i * 0.4f, 0.12f, 50.0f + i)));
            // The ring's limit on the court: a circle at its exact radius (the contact line), her ink outside, as the fan's edges are.
            var aroundEdge = WindVfx.Arc(AmihanRules.StormSurgeAroundRadius - 0.13f, 360.0f, 72, 0.025f);
            fx._aroundEdge = Floor(WindVfx.Build(go.transform, "AroundEdge", aroundEdge, 0.26f, WindVfx.Flat(aroundEdge), 24.0f, 0.3f, 22.0f));
            for (int i = 0; i < 2; i++)
            {
                var ring = WindVfx.Arc(1.0f, 360.0f, 64, 0.0f);
                fx._ringFront.Add(WindVfx.Build(go.transform, "RingFront" + i, ring, 1.3f - i * 0.45f, WindVfx.Standing, 6.0f + i * 3.0f, 0.16f, 34.0f + i));
                fx._ringFront[i].Recolour(WindVfx.Core, WindVfx.SheetBody, WindVfx.FloorInk);
                if (fx._ringFront[i].Material != null) { fx._ringFront[i].Material.SetFloat("_InkFrom", 0.55f); fx._ringFront[i].Material.SetFloat("_InkAlpha", 0.95f); }
            }
            // Each Motif gets its own host: a Motif hands its shared tuft mesh to its parent's single
            // GeneratedMeshOwner, so two on one object throws (the shipped cutscene did, on every cast).
            fx._drawn = new WindVfx.Motif(Host(go.transform, "DrawnCotton"), 16, origin.x * 1.7f + origin.z * 0.9f, 0.5f);
            fx._clothFront = new AbelCloth(go.transform, "AbelFront", ClothFrontSamples, 0.9f);
            fx._clothSkirt = new AbelCloth(go.transform, "AbelSkirt", ClothSkirtSamples, 1.2f);
            for (int i = 0; i < SashRows.GetLength(0); i++) fx._sashes.Add(new AbelCloth(go.transform, "AbelSash" + i, SashSamples, 1.8f));
            fx._clothCentre = new Vector3[ClothFrontSamples]; fx._clothAcross = new Vector3[ClothFrontSamples];
            fx._flung = new WindVfx.Motif(Host(go.transform, "FlungCotton"), 40, origin.x * 0.7f + origin.z * 3.3f);
            fx._scraps = new WindVfx.Motif(Host(go.transform, "FlungScraps"), 24, origin.x * 2.3f + origin.z * 0.4f, 0.7f);
            fx.StepTo(0.0f);
            return fx;
        }

        /// <summary>
        /// The surface she and the slippers actually stand on. `VfxShapes.GroundPoint` prefers a collider classed as
        /// court even when the visible floor is above it, and on Bayan Plaza the shipped fan's floor pieces drew under
        /// the tiles: a native film showed no wedge at all through the windup. `Slipper.GroundY` is what Paete's and
        /// Phaister's floor work use; a hit more than half a metre above her is a roof, not her floor.
        /// </summary>
        public static Vector3 CourtUnder(Vector3 origin)
        {
            var at = VfxShapes.GroundPoint(origin);
            float court = Slipper.GroundY(origin + Vector3.up * 0.3f);
            if (court > at.y && court < origin.y + 0.5f) at.y = court;
            return at;
        }

        /// <summary>A stroke lying on the court: her darker greens and a wide, solid ink rim so it reads on a light floor.</summary>
        private static WindVfx.Ribbon Floor(WindVfx.Ribbon ribbon)
        {
            ribbon.Recolour(WindVfx.Body, FloorBody, FloorInk);
            if (ribbon.Material != null) { ribbon.Material.SetFloat("_InkFrom", 0.45f); ribbon.Material.SetFloat("_InkAlpha", 0.95f); }
            return ribbon;
        }

        private static Transform Host(Transform parent, string name)
        {
            var host = new GameObject(name).transform;
            host.SetParent(parent, false);
            return host;
        }

        private static WindVfx.Ribbon Sheet(WindVfx.Ribbon ribbon, float rimFrom) => WindVfx.InkSheet(ribbon, rimFrom);

        /// <summary>A thread's width half flat, half standing, so it reads from her own camera and from over her shoulder.</summary>
        private static System.Func<int, Vector3> WarpSide(IList<Vector3> spine)
            => i =>
            {
                Vector3 along = spine[Mathf.Min(i + 1, spine.Count - 1)] - spine[Mathf.Max(i - 1, 0)];
                Vector3 flat = Vector3.Cross(Vector3.up, along);
                return flat.sqrMagnitude > 1e-6f ? (flat.normalized + Vector3.up).normalized : Vector3.up;
            };

        /// <summary>A side vector that lays a ribbon in the XY plane, across its own direction (an upright diamond's stroke).</summary>
        private static System.Func<int, Vector3> UprightSide(IList<Vector3> spine)
            => i =>
            {
                Vector3 along = spine[Mathf.Min(i + 1, spine.Count - 1)] - spine[Mathf.Max(i - 1, 0)];
                along.z = 0.0f;
                return along.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.forward, along) : Vector3.up;
            };

        /// <summary>An upright kasikus diamond in XY, corners on the axes.</summary>
        private static List<Vector3> UprightDiamond(float r)
        {
            var spine = new List<Vector3>(25);
            for (int k = 0; k < 4; k++)
                for (int j = 0; j < 6; j++)
                {
                    float a0 = k * 90.0f * Mathf.Deg2Rad, a1 = (k + 1) * 90.0f * Mathf.Deg2Rad;
                    spine.Add(Vector3.Lerp(new Vector3(Mathf.Sin(a0), Mathf.Cos(a0), 0), new Vector3(Mathf.Sin(a1), Mathf.Cos(a1), 0), j / 6.0f) * r);
                }
            spine.Add(spine[0]);
            return spine;
        }

        /// <summary>The front corner of a kasikus diamond of radius <paramref name="r"/>, inside the fan.</summary>
        private static List<Vector3> Chevron(float halfDegrees, float r, float y)
        {
            float a = halfDegrees * Mathf.Deg2Rad;
            // Where |x| + z = r meets the fan edge, and the arm stopping short of it.
            float d = r / (Mathf.Sin(a) + Mathf.Cos(a));
            var apex = new Vector3(0, y, r);
            var edge = new Vector3(Mathf.Sin(a) * d, y, Mathf.Cos(a) * d);
            var tip = Vector3.Lerp(apex, edge, ArmReach);
            var spine = new List<Vector3>(17);
            for (int k = 0; k <= 8; k++) spine.Add(Vector3.Lerp(new Vector3(-tip.x, y, tip.z), apex, k / 8.0f));
            for (int k = 1; k <= 8; k++) spine.Add(Vector3.Lerp(apex, tip, k / 8.0f));
            return spine;
        }

        private static WindVfx.Ribbon Diamond(Transform parent, string name, float r, float width, float seed)
        {
            var dense = new List<Vector3>();
            for (int k = 0; k < 4; k++)
                for (int j = 0; j < 6; j++)
                {
                    float a0 = k * 90.0f * Mathf.Deg2Rad, a1 = (k + 1) * 90.0f * Mathf.Deg2Rad;
                    dense.Add(Vector3.Lerp(new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)), new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)), j / 6.0f) * r
                              + Vector3.up * 0.03f);
                }
            dense.Add(dense[0]);
            return WindVfx.Build(parent, name, dense, width, WindVfx.Flat(dense), 3.0f, 0.4f, seed);
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= LifeSeconds) Destroy(gameObject); }

        /// <summary>
        /// THE ABEL, posed from the age after the release. The front: a standing woven curtain across the whole half angle,
        /// racing out to the fan's reach, billowing (a travelling ripple along it) with its top edge whipping forward, fraying
        /// as it goes. The skirt: a cloth band snapping out round her to `StormSurgeAroundRadius`. The sashes: unfurling off
        /// her palms down the blast, whipping, fraying away.
        /// </summary>
        private void StepCloth(float after, bool calm)
        {
            if (after < 0) { _clothFront.Hide(); _clothSkirt.Hide(); foreach (var sash in _sashes) sash.Hide(); return; }
            float half = _half * Mathf.Deg2Rad;
            float ripple = calm ? 0.0f : 1.0f;
            // THE FRONT.
            {
                float u = Mathf.Clamp01(after / 0.7f);
                float r = Mathf.Lerp(1.2f, _range, 1.0f - Mathf.Pow(1.0f - u, 3.2f));
                float height = Mathf.Lerp(1.9f, 1.2f, u);
                for (int i = 0; i < ClothFrontSamples; i++)
                {
                    float s = i / (ClothFrontSamples - 1.0f);
                    float a = Mathf.Lerp(-half, half, s) * 0.985f;
                    var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    // Billow: the cloth bellies out between its ends and ripples along its length.
                    float belly = 0.9f * Mathf.Sin(s * Mathf.PI) + ripple * 0.35f * Mathf.Sin(s * 22.0f - after * 26.0f);
                    float rr = r + belly * (0.4f + u);
                    var bottom = outward * rr + Vector3.up * 0.12f;
                    // The top edge whips forward and back as it races.
                    var top = outward * (rr + (0.6f + ripple * 0.35f * Mathf.Sin(s * 13.0f - after * 31.0f)) * (1.0f - u * 0.5f)) + Vector3.up * height;
                    _clothCentre[i] = (bottom + top) * 0.5f; _clothAcross[i] = (top - bottom) * 0.5f;
                }
                _clothFront.Pose(_clothCentre, _clothAcross, WindVfx.Ease(0.35f, 0.75f, after));
            }
            // THE SKIRT round her.
            {
                float u = Mathf.Clamp01(after / 0.38f);
                float r = Mathf.Lerp(0.6f, AmihanRules.StormSurgeAroundRadius, 1.0f - Mathf.Pow(1.0f - u, 3.0f));
                for (int i = 0; i < ClothSkirtSamples; i++)
                {
                    float s = i / (ClothSkirtSamples - 1.0f), a = s * Mathf.PI * 2.0f;
                    var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    float wave = ripple * 0.3f * Mathf.Sin(a * 9.0f + after * 24.0f);
                    var bottom = outward * r + Vector3.up * 0.08f;
                    var top = outward * (r + 0.7f + wave) + Vector3.up * (1.0f + wave);
                    _clothCentre[i] = (bottom + top) * 0.5f; _clothAcross[i] = (top - bottom) * 0.5f;
                }
                _clothSkirt.Pose(Trim(_clothCentre, ClothSkirtSamples), Trim(_clothAcross, ClothSkirtSamples), WindVfx.Ease(0.25f, 0.6f, after));
            }
            // THE SASHES off her palms.
            for (int k = 0; k < _sashes.Count; k++)
            {
                float t = after - SashRows[k, 3];
                if (t <= 0) { _sashes[k].Hide(); continue; }
                float a = SashRows[k, 0] * half;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var side = Vector3.Cross(Vector3.up, dir);
                float length = SashRows[k, 1] * WindVfx.Ease(0.0f, 0.35f, t);
                float travel = 14.0f * Mathf.Max(0, t - 0.25f);
                float width = SashRows[k, 2], freq = SashRows[k, 4], lift = SashRows[k, 5];
                for (int i = 0; i < SashSamples; i++)
                {
                    float s = i / (SashSamples - 1.0f);
                    // From her palms (s 0) out to its free end (s 1), lifting off the court and whipping side to side.
                    float whip = ripple * Mathf.Sin(s * freq * 6.0f - t * 22.0f) * (0.15f + 0.85f * s) * 0.9f;
                    var c = Hands + dir * (travel + length * s) + side * whip + Vector3.up * (lift * Mathf.Sin(s * Mathf.PI * 0.8f) * (0.6f + 0.4f * s));
                    _clothCentre[i] = c;
                    // Twisting as it flies, so its weave turns to the light and back.
                    float twist = ripple * (s * 4.0f - t * 9.0f + k);
                    _clothAcross[i] = (side * Mathf.Cos(twist) + Vector3.up * Mathf.Sin(twist)) * width * 0.5f * (1.0f - 0.4f * s);
                }
                _sashes[k].Pose(Trim(_clothCentre, SashSamples), Trim(_clothAcross, SashSamples), WindVfx.Ease(0.45f, 0.95f, t));
            }
        }

        private readonly System.Collections.Generic.Dictionary<int, Vector3[]> _trims = new System.Collections.Generic.Dictionary<int, Vector3[]>();
        private int _trimFlip;
        /// <summary>The first <paramref name="n"/> of a pose buffer as an array of exactly that length (two kept per length).</summary>
        private Vector3[] Trim(Vector3[] source, int n)
        {
            int key = n * 2 + (_trimFlip++ & 1);
            if (!_trims.TryGetValue(key, out var buffer)) { buffer = new Vector3[n]; _trims[key] = buffer; }
            System.Array.Copy(source, buffer, n);
            return buffer;
        }

        /// <summary>A beat's step, eased in over 0.12 s from <paramref name="at"/>.</summary>
        private static float Step(float t, float at) => WindVfx.Ease(at, at + 0.12f, t);

        /// <summary>A beat's flash, up at once and decaying over 0.18 s.</summary>
        private static float Pulse(float t, float at) => t < at ? 0.0f : 1.0f - WindVfx.Ease(at, at + 0.18f, t);

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(-DrawOnSeconds, seconds);
            float t = _age, g = _gather, after = t - g;
            bool calm = WindVfx.Reduced;
            // The beats are shares of the windup so the body (`HeroAbilityClips.Amihan.cs`) and the floor agree at 1.5 s.
            float beat1 = g / 3.0f, beat2 = g * 2.0f / 3.0f, draw = g * 0.88f;
            float pull = 0.07f * Step(t, beat1) + 0.07f * Step(t, beat2) + 0.11f * Step(t, draw);
            float pulse = Pulse(t, beat1) + Pulse(t, beat2) + 0.8f * Pulse(t, draw);
            float level = 0.5f + 0.12f * Step(t, beat1) + 0.12f * Step(t, beat2) + 0.14f * Step(t, draw);
            // A slow drift only: an outward streak rush would say the wind had already left.
            float phase = calm ? 0.0f : t * 0.8f;
            float released = after < 0 ? 0.0f : 1.0f;

            // The edges: drawn out from her feet in the cutscene, full through the windup, flashing then thinning away.
            float edgeHead = t < 0 ? WindVfx.Ease(-DrawOnSeconds, -0.25f, t) : 1.0f;
            float edgeAlpha = t < 0 ? WindVfx.Ease(-DrawOnSeconds, -0.6f, t)
                : after < 0 ? 0.85f + 0.15f * Mathf.Clamp01(pulse) : 1.0f - WindVfx.Ease(0.05f, 0.45f, after);
            float edgeThin = after < 0 ? 0.15f : WindVfx.Ease(0.0f, 0.4f, after);
            _edgeLeft.Set(edgeAlpha, phase * 0.5f, edgeHead, 0, edgeThin);
            _edgeRight.Set(edgeAlpha, phase * 0.5f, edgeHead, 0, edgeThin);

            // The chevrons: contract in steps through the windup, then all burst outward together.
            float burst = 1.0f - Mathf.Pow(1.0f - Mathf.Clamp01(after / 0.5f), 3.0f);
            for (int i = 0; i < _chevrons.Count; i++)
            {
                float appear = t < 0 ? WindVfx.Ease(-0.75f + (Chevrons - 1 - i) * 0.07f, -0.35f + (Chevrons - 1 - i) * 0.05f, t) : 1.0f;
                float scale = after < 0 ? 1.0f - pull : Mathf.Lerp(1.0f - pull, 2.2f + i * 0.1f, burst);
                _chevrons[i].GameObject.transform.localScale = new Vector3(scale, 1.0f, scale);
                float alpha = after < 0 ? appear * Mathf.Clamp01(level + 0.25f * pulse) : 0.95f * (1.0f - WindVfx.Ease(0.1f, 0.45f, after));
                _chevrons[i].Set(alpha, phase + i * 0.3f, 1.0f, 0.0f, after < 0 ? 0.1f : WindVfx.Ease(0.0f, 0.4f, after));
            }

            // The release front: past the 14 m court in about 0.1 s, out to the fan's end by WallSeconds.
            for (int i = 0; i < _front.Count; i++)
            {
                float lagged = after - i * 0.035f;
                float u = lagged < 0 ? 0.0f : Mathf.Clamp01(lagged / WallSeconds);
                float radius = Mathf.Lerp(1.0f, _range, 1.0f - Mathf.Pow(1.0f - u, 4.0f));
                _front[i].GameObject.transform.localScale = new Vector3(radius, 1.0f + u * 0.4f, radius);
                // The cloth carries the blast's mass now; the fronts are the wind round it, lighter.
                float alive = lagged < 0 ? 0.0f : (0.6f - i * 0.15f) * (1.0f - WindVfx.Ease(0.25f, WallSeconds, lagged));
                _front[i].Set(alive, phase * 2.0f, 1.0f, 0.0f, WindVfx.Ease(0.15f, WallSeconds - 0.05f, Mathf.Max(0, lagged)));
            }

            // THE RING: its edge drawn on with the fan's, held through the windup; on the release two rings of wind race out to it.
            {
                float on = t < 0 ? WindVfx.Ease(-DrawOnSeconds * 0.6f, -0.2f, t) : 1.0f;
                _aroundEdge.Set(on * (after < 0 ? 0.75f + 0.2f * Mathf.Clamp01(pulse) : 1.0f - WindVfx.Ease(0.05f, 0.45f, after)), phase * 0.5f,
                                t < 0 ? WindVfx.Ease(-DrawOnSeconds * 0.6f, -0.1f, t) : 1.0f, 0.0f, after < 0 ? 0.15f : WindVfx.Ease(0.0f, 0.4f, after));
                float around = AmihanRules.StormSurgeAroundRadius;
                for (int i = 0; i < _ringFront.Count; i++)
                {
                    float lagged = after - i * 0.04f;
                    float u = lagged < 0 ? 0.0f : Mathf.Clamp01(lagged / 0.35f);
                    float radius = Mathf.Lerp(0.6f, around, 1.0f - Mathf.Pow(1.0f - u, 3.0f));
                    _ringFront[i].GameObject.transform.localScale = new Vector3(radius, 1.0f + u * 0.3f, radius);
                    _ringFront[i].Set(lagged < 0 ? 0.0f : (0.95f - i * 0.3f) * (1.0f - WindVfx.Ease(0.2f, 0.45f, lagged)), phase * 2.0f, 1.0f, 0.0f, WindVfx.Ease(0.1f, 0.4f, Mathf.Max(0, lagged)));
                }
            }

            StepCloth(after, calm);

            // At her feet: two diamonds held tight through the windup; the sigil bursts out on the release.
            for (int i = 0; i < _feet.Count; i++)
            {
                float s = 1.0f - pull * 1.2f;
                _feet[i].GameObject.transform.localScale = new Vector3(s, 1, s);
                _feet[i].GameObject.transform.localRotation = Quaternion.Euler(0, 45.0f + (i == 0 ? 1 : -1) * (t * 30.0f + pull * 90.0f), 0);
                float on = t < 0 ? WindVfx.Ease(-0.5f, 0.0f, t) : 1.0f - WindVfx.Ease(0.0f, 0.15f, after);
                _feet[i].Set(on * (0.45f + 0.3f * Mathf.Clamp01(pulse)), phase, 1, 0, 0.15f);
            }
            for (int i = 0; i < _sigil.Count; i++)
            {
                float flash = after < 0 ? 0.0f : WindVfx.Envelope(after, i * 0.05f, 0.06f, 0.6f + i * 0.05f, 0.35f);
                // At most about 1.3 m from her feet: the previous growth drew 6 m diamonds behind her, outside the fan.
                float grow = 1.0f + Mathf.Clamp01(Mathf.Max(0, after) / 0.5f) * (0.12f + i * 0.04f);
                _sigil[i].GameObject.transform.localScale = new Vector3(grow, 1, grow);
                _sigil[i].GameObject.transform.localRotation = Quaternion.Euler(0, 45.0f + (i % 2 == 0 ? 1 : -1) * t * 20.0f, 0);
                _sigil[i].Set(flash, phase, 1, 0, WindVfx.Ease(0.2f, 0.6f, after));
            }

            // THE WARP: strung from her hands along the cutscene's flick (the first 0.6 s of the draw-on), taut and brighter on
            // each pack beat, straining on the draw; on the release it snaps forward off her hands and races out down the lane.
            for (int i = 0; i < _warp.Count; i++)
            {
                float lag = 0.03f * Mathf.Abs(i - (WarpThreads - 1) * 0.5f);
                float head = t < 0 ? WindVfx.Ease(-DrawOnSeconds + lag, -DrawOnSeconds + 0.6f + lag, t) : 1.0f;
                float tail = after < 0 ? 0.0f : WindVfx.Ease(0.0f, 0.32f + lag, after);
                float on = t < 0 ? WindVfx.Ease(-DrawOnSeconds, -DrawOnSeconds + 0.15f, t) : 1.0f;
                // Film r4: in the cutscene's REVEAL the warp read as faint lines. Strung (the draw-on) it is at full strength and
                // full width, settling to the windup's held level by age 0.
                float strung = t < 0 ? 1.0f - WindVfx.Ease(-0.4f, 0.0f, t) : 0.0f;
                float alpha = on * (after < 0 ? Mathf.Lerp(0.6f + 0.25f * Mathf.Clamp01(pulse) + 0.15f * Step(t, draw), 1.0f, strung) : 1.0f - WindVfx.Ease(0.15f, 0.5f, after));
                // Taut on each beat: drawn finer and brighter (the ends never move, so the threads stay in her hands).
                _warp[i].Set(alpha, (calm ? 0.0f : t * 1.4f) + i * 0.4f, head, tail, after < 0 ? (0.25f - 0.6f * pull) * (1.0f - strung) : WindVfx.Ease(0.0f, 0.4f, after));
            }

            // THE GATHERING SHEETS: orbiting close round her through the windup, quickening on the beats, diving into her hands
            // on the draw.
            float dive = WindVfx.Ease(draw - 0.08f, g, t);
            for (int i = 0; i < _sheets.Count; i++)
            {
                var host = _sheetHosts[i];
                float spin = SheetRows[i, 7] * (Mathf.Max(0, t) + 0.35f * (Step(t, beat1) + Step(t, beat2)));
                host.localRotation = Quaternion.Euler(0, calm ? 0.0f : spin, 0);
                host.localPosition = Vector3.Lerp(Vector3.zero, Hands - Vector3.up * 0.5f, dive);
                host.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.15f, dive);
                float on = t < 0 ? WindVfx.Ease(-0.6f + i * 0.1f, -0.1f + i * 0.1f, t) : 1.0f;
                float alpha = on * (0.55f + 0.2f * Mathf.Clamp01(pulse)) * (1.0f - WindVfx.Ease(g - 0.06f, g, t));
                _sheets[i].Set(alpha, phase * 3.0f + i, 1.0f, 0.0f, 0.1f + 0.6f * dive);
            }

            // THE EMBLEM between her palms: swelling on each beat, brightest on the draw, bursting and gone on the release.
            {
                float on = t < 0 ? WindVfx.Ease(-0.5f, 0.0f, t) : 1.0f;
                float burstOut = after < 0 ? 0.0f : WindVfx.Ease(0.0f, 0.15f, after);
                _emblemHost.localScale = Vector3.one * (1.0f + 0.3f * Mathf.Clamp01(pulse) + 0.25f * Step(t, draw) + 2.5f * burstOut);
                _emblemHost.localRotation = Quaternion.Euler(0, 0, calm ? 0.0f : t * 30.0f);
                for (int i = 0; i < _emblem.Count; i++)
                    _emblem[i].Set(on * (0.75f + 0.25f * Mathf.Clamp01(pulse)) * (1.0f - burstOut), phase * (i % 2 == 0 ? 1 : -1), 1, 0, burstOut * 0.8f);
            }

            // THE COMB: ten streaks racing outward over the court on the release, each at its own pace.
            for (int i = 0; i < _comb.Count; i++)
            {
                if (after < 0) { _comb[i].Set(0, 0); continue; }
                float pace = 0.28f + 0.03f * (i % 4);
                float head = WindVfx.Ease(0.0f, pace, after), tail = WindVfx.Ease(0.06f, pace + 0.2f, after);
                _comb[i].Set((calm ? 0.55f : 0.9f) * (1.0f - WindVfx.Ease(pace, pace + 0.25f, after)), phase * 2.0f + i, head, tail, WindVfx.Ease(0.1f, 0.5f, after));
            }

            // Cotton and thread: drawn in to her cupped hands through the windup, flung out down the fan on the release.
            float half = _half * Mathf.Deg2Rad, range = _range;
            var hands = Hands;
            _drawn.Step(t <= 0 ? 0 : Mathf.Clamp01(t / g), (calm ? 0.45f : 0.85f) * (1.0f - released), (start, drift, u) =>
            {
                float a = start.x * 1.6f * half;
                float d = 6.0f + start.z * 8.0f + start.y * 3.0f;
                var from = new Vector3(Mathf.Sin(a) * d, 0.3f + start.y * 1.1f, Mathf.Cos(a) * d);
                float e = u * u * (3.0f - 2.0f * u);
                var side = Vector3.Cross(Vector3.up, hands - from);
                return Vector3.Lerp(from, hands, e) + (side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right) * Mathf.Sin(u * Mathf.PI) * (drift.x * 0.8f);
            }, 0.08f);
            _flung.Step(after < 0 ? 0 : Mathf.Clamp01(after / (WallSeconds + 0.2f)), calm ? 0.5f : 1.0f, (start, drift, u) =>
            {
                float a = (start.x * 1.7f) * half;
                float r = 0.8f + u * range * (0.3f + drift.z * 0.5f);
                return new Vector3(Mathf.Sin(a) * r, 0.3f + start.y * 1.4f + drift.y * u, Mathf.Cos(a) * r);
            }, 0.11f);
            // Scraps and dust skimming the court down the lane: an extra, dropped by reduced effects.
            _scraps.Step(after < 0 ? 0 : Mathf.Clamp01(after / (WallSeconds + 0.3f)), calm ? 0.0f : 0.9f, (start, drift, u) =>
            {
                float a = (start.x * 1.6f) * half;
                float r = 1.0f + u * range * (0.4f + drift.z * 0.5f);
                return new Vector3(Mathf.Sin(a) * r, 0.06f + start.y * 0.35f + 0.4f * Mathf.Sin(u * Mathf.PI) * drift.y, Mathf.Cos(a) * r);
            }, 0.07f);
        }
    }

    /// <summary>
    /// The WHIRLED body tell (owner's status table, 2026-09-25): a kasikus diamond turning at the
    /// waist and a small spiral strand round the slipper hand's side, for as long as the status
    /// runs, thinning as it runs out. It reads without the icon, which is the direction's rule for
    /// every status (§ 6). Attached by `StatusBodyMarks` on every peer.
    /// </summary>
    public sealed class WhirledMark : MonoBehaviour
    {
        private CharacterMotor _body;
        private WindVfx.Ribbon _diamond, _spiral, _ring;
        private float _age;

        public static WhirledMark Attach(CharacterMotor body)
        {
            var go = new GameObject("WhirledMark");
            go.transform.SetParent(body.transform, false);
            var mark = go.AddComponent<WhirledMark>(); mark._body = body;
            var diamond = new List<Vector3>();
            for (int k = 0; k < 4; k++)
                for (int j = 0; j < 5; j++)
                {
                    float a0 = k * 90.0f * Mathf.Deg2Rad, a1 = (k + 1) * 90.0f * Mathf.Deg2Rad;
                    Vector3 p0 = new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * 0.5f, p1 = new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * 0.5f;
                    diamond.Add(Vector3.Lerp(p0, p1, j / 5.0f) + Vector3.up * 0.95f);
                }
            diamond.Add(diamond[0]);
            mark._diamond = WindVfx.Build(go.transform, "WhirledDiamond", diamond, 0.09f, WindVfx.Standing, 4.0f, 0.35f, 1.0f);
            var spiral = WindVfx.Helix(Vector3.up * 0.4f, Vector3.up * 1.4f, 0.42f, 1.6f, 26, 0, 0.6f);
            mark._spiral = WindVfx.Build(go.transform, "WhirledSpiral", spiral, 0.07f, WindVfx.AroundAxis(spiral, Vector3.up), 5.0f, 0.3f, 2.0f);
            var ring = WindVfx.Arc(0.4f, 300.0f, 22, 0.03f);
            mark._ring = WindVfx.Build(go.transform, "WhirledFeet", ring, 0.08f, WindVfx.Flat(ring), 6.0f, 0.3f, 3.0f);
            return mark;
        }

        private void LateUpdate()
        {
            if (_body == null || !_body.IsWhirled) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            float left = Mathf.Clamp01(_body.WhirledLeft / StatusRules.WhirledSeconds);
            float arrive = WindVfx.Ease(0.0f, 0.12f, _age);
            float phase = WindVfx.Reduced ? 0.0f : _age * 5.0f;
            _diamond.GameObject.transform.localRotation = Quaternion.Euler(0, _age * 300.0f, 0);
            _diamond.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1.0f, arrive);
            _diamond.Set(0.9f * arrive, phase, 1, 0, 1 - left);
            _spiral.GameObject.transform.localRotation = Quaternion.Euler(0, -_age * 420.0f, 0);
            _spiral.Set(0.7f * arrive * left, phase, 1, 0, 0.2f + (1 - left) * 0.6f);
            _ring.GameObject.transform.localRotation = Quaternion.Euler(0, _age * 200.0f, 0);
            _ring.Set(0.6f * arrive * left, phase, 1, 0, 0.3f);
        }
    }

    /// <summary>
    /// The CHILLED body tell: a ring of small frost crystals round the feet and a cold ground ring,
    /// in Cheska's ice colours (it is her status), for as long as it runs.
    /// </summary>
    public sealed class ChilledMark : MonoBehaviour
    {
        private CharacterMotor _body;
        private readonly List<Transform> _crystals = new List<Transform>();
        private readonly List<Material> _materials = new List<Material>();
        private float _age;

        public static ChilledMark Attach(CharacterMotor body)
        {
            var go = new GameObject("ChilledMark");
            go.transform.SetParent(body.transform, false);
            var mark = go.AddComponent<ChilledMark>(); mark._body = body;
            var ice = UI.UiTheme.HeroIceBright;
            for (int i = 0; i < 7; i++)
            {
                var piece = VfxShapes.Stand(go.transform, "FrostCrystal" + i, VfxShapes.Crystal(5, 0.2f), 0.07f, 0.2f);
                var renderer = piece.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                VfxMaterial.Ghost(renderer, new Color(ice.r, ice.g, ice.b, 0.8f), 0.5f);
                mark._crystals.Add(piece.transform); mark._materials.Add(renderer.sharedMaterial);
            }
            return mark;
        }

        private void LateUpdate()
        {
            if (_body == null || !_body.IsChilled) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            float left = Mathf.Clamp01(_body.ChilledLeft / StatusRules.ChilledSeconds);
            float grow = WindVfx.Ease(0.0f, 0.2f, _age);
            for (int i = 0; i < _crystals.Count; i++)
            {
                float a = i / (float)_crystals.Count * Mathf.PI * 2.0f + _age * 0.3f;
                _crystals[i].localPosition = new Vector3(Mathf.Cos(a) * 0.38f, 0.02f, Mathf.Sin(a) * 0.38f);
                _crystals[i].localRotation = Quaternion.Euler(12.0f * Mathf.Sin(a * 2), i * 51.0f, 10.0f * Mathf.Cos(a));
                _crystals[i].localScale = new Vector3(0.07f, 0.2f * grow * Mathf.Lerp(0.35f, 1.0f, left) * (0.7f + (i % 3) * 0.2f), 0.07f);
                var c = _materials[i].color; c.a = 0.8f * grow; _materials[i].color = c; _materials[i].SetColor("_BaseColor", c);
            }
        }
    }

    /// <summary>
    /// Puts the right body tell on a body when it gains a status, on every peer. Added to every
    /// `CharacterMotor` so received timers and joining snapshots need no original cast.
    /// Frozen's existing restraint is body-owned too; Tagged keeps its caught mark.
    /// </summary>
    public sealed class StatusBodyMarks : MonoBehaviour
    {
        private CharacterMotor _body;
        private WhirledMark _whirled;
        private ChilledMark _chilled;
        private Abilities.HeroHazards.IceCubePrisonComponent _frozen;
        private bool _frozenPresentationFailed;
        private PaeteRootCoil _rooted;
        private bool _rootedPresentationFailed;

        private void Awake() => _body = GetComponent<CharacterMotor>();

        public PaeteRootCoil EnsureRootedRestraint(Vector3? centre = null)
        {
            if (!isActiveAndEnabled || _body == null || !_body.IsRooted || _rootedPresentationFailed) return null;
            try
            {
                _rooted = centre.HasValue ? PaeteRootCoil.Attach(_body, centre.Value) : PaeteRootCoil.Attach(_body);
                return _rooted;
            }
            catch (System.Exception error)
            {
                _rootedPresentationFailed = true;
                var partial = _body.GetComponentInChildren<PaeteRootCoil>();
                if (partial != null) partial.Retire();
                Debug.LogException(error);
                return null;
            }
        }

        public GameObject EnsureFrozenRestraint()
        {
            if (!isActiveAndEnabled || _body == null || !_body.IsFrozen) return null;
            if (_frozen != null && !_frozen.Shattered) return _frozen.gameObject;
            if (_frozenPresentationFailed) return null;
            try
            {
                var visual = Abilities.HeroHazards.CreateIceCubePrison(_body.transform, _body.StunLeft);
                _frozen = visual.GetComponent<Abilities.HeroHazards.IceCubePrisonComponent>();
                return visual;
            }
            catch (System.Exception error)
            {
                // Missing/reworked presentation must not retry and allocate every frame.
                _frozenPresentationFailed = true;
                Debug.LogException(error);
                return null;
            }
        }

        private void LateUpdate()
        {
            if (_body == null) return;
            if (_body.IsRooted)
            {
                if (_rooted == null || !_rooted.gameObject.activeInHierarchy) EnsureRootedRestraint();
            }
            else
            {
                if (_rooted != null) _rooted.Retire();
                _rooted = null; _rootedPresentationFailed = false;
            }
            if (_body.IsFrozen) EnsureFrozenRestraint();
            else
            {
                _frozenPresentationFailed = false;
                if (_frozen != null) { _frozen.Shatter(); _frozen = null; }
            }
            if (_body.IsWhirled && _whirled == null)
            {
                _whirled = WhirledMark.Attach(_body);
                // v3.2: thrown by the wind, the body loses itself to it (`WindTumble`), on every peer.
                WindTumble.Attach(_body);
                // ⚠️ LOCAL, NOT `NetCue`: this runs on EVERY peer off the replicated timer, so each
                // plays it once; relaying as well would be a flam of four (`audit_cue_relay.py`).
                GameServices.Audio?.PlayAtVaried("sfx_status_whirled", _body.transform.position, 0.95f, 1.05f, 0.8f);
            }
            if (_body.IsChilled && _chilled == null)
            {
                _chilled = ChilledMark.Attach(_body);
                GameServices.Audio?.PlayAtVaried("sfx_status_chilled", _body.transform.position, 0.95f, 1.05f, 0.7f);
            }
        }

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
        private void Clear()
        {
            if (_rooted != null) _rooted.Retire();
            if (_frozen != null) Destroy(_frozen.gameObject);
            if (_whirled != null) Destroy(_whirled.gameObject);
            if (_chilled != null) Destroy(_chilled.gameObject);
            _frozen = null; _whirled = null; _chilled = null;
            _frozenPresentationFailed = false;
            _rooted = null; _rootedPresentationFailed = false;
        }
    }
}
