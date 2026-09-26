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
        private WindVfx.Ribbon _road, _heel, _curlA, _curlB;
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
                var strand = WindVfx.Build(go.transform, "SlipstreamStrand" + i, spine, 0.16f - i * 0.03f,
                                           WindVfx.AroundAxis(spine, axis), 5.0f, 0.22f, i * 3.1f);
                wake._strands.Add(strand);
            }
            var road = new List<Vector3>();
            for (int i = 0; i < 16; i++) road.Add(Vector3.Lerp(Vector3.zero, wake._to, i / 15.0f) + Vector3.up * 0.04f);
            wake._road = WindVfx.Build(go.transform, "SlipstreamRoad", road, 0.5f, WindVfx.Flat(road), 7.0f, 0.12f, 9.0f);
            var heel = WindVfx.Arc(0.35f, 330.0f, 24, 0.05f);
            wake._heel = WindVfx.Build(go.transform, "HeelKick", heel, 0.14f, WindVfx.Flat(heel), 4.0f, 0.25f, 2.0f);
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
            _heel.Set(1.0f - WindVfx.Ease(0.1f, 0.4f, t), phase, 1.0f, 0.0f, WindVfx.Ease(0.05f, 0.35f, t));
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
            hit._crescent = WindVfx.Build(go.transform, "WrapCrescent", arc, 0.22f, WindVfx.Standing, 4.0f, 0.24f, 1.0f);
            var inner = WindVfx.Arc(0.42f, 200.0f, 22, 0.5f, -150.0f);
            hit._inner = WindVfx.Build(go.transform, "WrapInner", inner, 0.12f, WindVfx.Standing, 3.0f, 0.3f, 2.0f);
            var ring = WindVfx.Arc(0.45f, 340.0f, 26, 0.04f);
            hit._ring = WindVfx.Build(go.transform, "HitRing", ring, 0.12f, WindVfx.Flat(ring), 5.0f, 0.2f, 3.0f);
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
    /// UPDRAFT's launch: a column of three strands spiralling up from her feet to where she will
    /// hover, a ring of air bursting out across the road, and cotton lifted with her.
    /// </summary>
    public sealed class AmihanUpdraftLaunch : MonoBehaviour, IVfxTimeline
    {
        public const float Life = 1.0f;
        private readonly List<WindVfx.Ribbon> _column = new List<WindVfx.Ribbon>();
        private WindVfx.Ribbon _burst, _burstOuter;
        private WindVfx.Motif _motif;
        private float _age, _height;
        public float LifeSeconds => Life;

        public static AmihanUpdraftLaunch Build(Vector3 at, float height)
        {
            var go = new GameObject("AmihanUpdraftLaunch");
            go.transform.position = VfxShapes.GroundPoint(at);
            var fx = go.AddComponent<AmihanUpdraftLaunch>(); fx._height = height;
            for (int i = 0; i < 3; i++)
            {
                var spine = WindVfx.Helix(Vector3.up * 0.05f, Vector3.up * (height + 0.9f), 0.55f - i * 0.1f, 1.6f + i * 0.35f, 30, i * 120.0f, 0.55f);
                fx._column.Add(WindVfx.Build(go.transform, "UpdraftStrand" + i, spine, 0.2f - i * 0.04f,
                                             WindVfx.AroundAxis(spine, Vector3.up), 5.0f, 0.2f, i * 1.7f));
            }
            var ring = WindVfx.Arc(0.6f, 355.0f, 30, 0.05f);
            fx._burst = WindVfx.Build(go.transform, "LiftRing", ring, 0.18f, WindVfx.Flat(ring), 6.0f, 0.2f, 3.0f);
            var outer = WindVfx.Arc(0.9f, 355.0f, 30, 0.03f);
            fx._burstOuter = WindVfx.Build(go.transform, "LiftRingOuter", outer, 0.1f, WindVfx.Flat(outer), 8.0f, 0.15f, 4.0f);
            fx._motif = new WindVfx.Motif(go.transform, 12, at.x * 2.9f + at.z * 0.3f, 0.3f);
            fx.StepTo(0.0f);
            return fx;
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= Life) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float t = _age;
            float phase = WindVfx.Reduced ? 0.0f : t * 4.0f;
            for (int i = 0; i < _column.Count; i++)
            {
                _column[i].GameObject.transform.localRotation = Quaternion.Euler(0, t * (220.0f + i * 60.0f), 0);
                _column[i].Set(0.95f - i * 0.2f, phase + i, WindVfx.Ease(0.0f, 0.42f, t), WindVfx.Ease(0.3f, 0.95f, t), WindVfx.Ease(0.5f, 1.0f, t));
            }
            _burst.GameObject.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.6f, WindVfx.Ease(0.0f, 0.45f, t));
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
    /// UPDRAFT while aloft: a ring of air turning under her feet, two strands swirling round her
    /// legs, a thin downdraft line to the road and a ring ON the road, so every player can read
    /// where she is above them (and the taya can read where she will come down). It follows the
    /// body and removes itself when the flight is over.
    /// </summary>
    public sealed class AmihanHoverRing : MonoBehaviour
    {
        private CharacterMotor _body;
        private WindVfx.Ribbon _feet, _feetOuter, _legA, _legB, _shadowRing, _downdraft;
        private Transform _groundAnchor;
        private float _age, _fade;

        public static AmihanHoverRing Attach(CharacterMotor body)
        {
            if (body == null) return null;
            var existing = body.GetComponentInChildren<AmihanHoverRing>();
            if (existing != null) { existing._fade = 0.0f; return existing; }
            var go = new GameObject("AmihanHoverRing");
            go.transform.SetParent(body.transform, false);
            var fx = go.AddComponent<AmihanHoverRing>(); fx._body = body;
            var feet = WindVfx.Arc(0.42f, 340.0f, 26, 0.02f);
            fx._feet = WindVfx.Build(go.transform, "FeetRing", feet, 0.13f, WindVfx.Flat(feet), 5.0f, 0.25f, 1.0f);
            var outer = WindVfx.Arc(0.62f, 300.0f, 26, -0.08f);
            fx._feetOuter = WindVfx.Build(go.transform, "FeetRingOuter", outer, 0.08f, WindVfx.Flat(outer), 7.0f, 0.2f, 2.0f);
            var legA = WindVfx.Helix(Vector3.up * -0.1f, Vector3.up * 0.9f, 0.34f, 1.2f, 20, 0, 0.8f);
            var legB = WindVfx.Helix(Vector3.up * -0.2f, Vector3.up * 0.7f, 0.4f, 1.0f, 20, 180, 0.7f);
            fx._legA = WindVfx.Build(go.transform, "LegStrandA", legA, 0.08f, WindVfx.AroundAxis(legA, Vector3.up), 4.0f, 0.25f, 3.0f);
            fx._legB = WindVfx.Build(go.transform, "LegStrandB", legB, 0.07f, WindVfx.AroundAxis(legB, Vector3.up), 4.0f, 0.25f, 4.0f);
            var anchor = new GameObject("GroundMark").transform; anchor.SetParent(go.transform, false);
            fx._groundAnchor = anchor;
            var shadow = WindVfx.Arc(0.5f, 350.0f, 28, 0.0f);
            fx._shadowRing = WindVfx.Build(anchor, "GroundRing", shadow, 0.1f, WindVfx.Flat(shadow), 6.0f, 0.3f, 5.0f);
            var draft = new List<Vector3>();
            for (int i = 0; i < 10; i++) draft.Add(Vector3.up * (i / 9.0f));
            fx._downdraft = WindVfx.Build(anchor, "Downdraft", draft, 0.06f, _ => Vector3.right, 6.0f, 0.4f, 6.0f);
            return fx;
        }

        private void LateUpdate()
        {
            if (_body == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt;
            bool flying = _body.IsFlying;
            _fade = Mathf.Clamp01(_fade + (flying ? dt * 4.0f : -dt * 3.0f));
            if (!flying && _fade <= 0.0f) { Destroy(gameObject); return; }
            float phase = WindVfx.Reduced ? 0.0f : _age * 3.0f;
            float a = _fade;
            _feet.GameObject.transform.localRotation = Quaternion.Euler(0, _age * 140.0f, 0);
            _feet.Set(0.9f * a, phase, 1, 0, 1 - a);
            _feetOuter.GameObject.transform.localRotation = Quaternion.Euler(0, -_age * 90.0f, 0);
            _feetOuter.Set(0.55f * a, phase * 1.3f, 1, 0, 1 - a);
            _legA.GameObject.transform.localRotation = Quaternion.Euler(0, _age * 260.0f, 0);
            _legB.GameObject.transform.localRotation = Quaternion.Euler(0, _age * 200.0f + 90.0f, 0);
            float breathe = 0.5f + 0.5f * Mathf.Sin(_age * 2.2f);
            _legA.Set(0.6f * a, phase, Mathf.Lerp(0.6f, 1.0f, breathe), 0.0f, 0.2f);
            _legB.Set(0.5f * a, phase + 1, 1.0f, Mathf.Lerp(0.0f, 0.3f, breathe), 0.25f);

            // The road mark: straight down from her, on whatever surface is under her.
            Vector3 feet = _body.transform.position;
            float ground = VfxShapes.GroundAt(feet, feet.y - 3.0f, 5.0f);
            float gap = Mathf.Max(0.0f, feet.y - ground);
            _groundAnchor.position = new Vector3(feet.x, ground + 0.03f, feet.z);
            _groundAnchor.rotation = Quaternion.identity;
            _shadowRing.GameObject.transform.localRotation = Quaternion.Euler(0, -_age * 60.0f, 0);
            _shadowRing.Set(Mathf.Clamp01(gap / 1.2f) * 0.8f * a, phase, 1, 0, 0.3f);
            _downdraft.GameObject.transform.localScale = new Vector3(1, Mathf.Max(0.01f, gap), 1);
            _downdraft.GameObject.transform.rotation = Quaternion.LookRotation(Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up) + Vector3.forward * 0.001f : Vector3.forward);
            _downdraft.Set(Mathf.Clamp01(gap / 1.5f) * 0.45f * a, -phase * 2.0f, 1, 0, 0.5f);
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
                    arc[k] += new Vector3(0, 0.25f + (4 - i) * 0.22f, -radius + bow - i * 0.28f);
                    float edge = Mathf.Abs(k / (float)(arc.Length - 1) * 2.0f - 1.0f);
                    arc[k].y *= 1.0f - edge * edge * 0.55f; // the ends of the front sit lower
                }
                fx._layers.Add(WindVfx.Build(go.transform, "GaleLayer" + i, arc, 0.34f - i * 0.04f, WindVfx.Standing, 5.0f + i, i == 0 ? 0.26f : 0.16f, i * 2.3f));
            }
            var skirt = WindVfx.Arc(radius, degrees * 1.08f, 26, 0.04f);
            for (int k = 0; k < skirt.Length; k++) skirt[k] += new Vector3(0, 0, -radius + bow - 0.35f);
            fx._skirt = WindVfx.Build(go.transform, "GaleSkirt", skirt, 0.9f, WindVfx.Flat(skirt), 8.0f, 0.1f, 11.0f);
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
            _spray.Set(0.7f * open * (1.0f - fray), phase * 2.0f, 1, 0, 0.3f + fray * 0.7f);
            _motif.Step(Mathf.Repeat(t / 0.9f, 1.0f), 0.85f * (1.0f - fray), (start, drift, u) =>
                new Vector3((start.x) * AmihanRules.WhirlwindWidth, 0.15f + u * (0.8f + drift.y), -0.2f - u * 1.4f));
        }
    }

    /// <summary>
    /// STORM SURGE, live. GATHER (2.5 s): the fan the wind will blow down lights up on the road,
    /// its streaks rushing outward faster and faster, its two edges drawn as bright lines so every
    /// player can see exactly where not to stand; pressure rings tighten round her. RELEASE: a wall
    /// of three ribbon layers sweeps out along the fan at speed, a kasikus sigil (the binakol
    /// whirlwind her family weaves) flashes out under her, and cotton and threads fly with it.
    /// </summary>
    public sealed class AmihanStormFan : MonoBehaviour, IVfxTimeline
    {
        public const int Lanes = 7;
        public const float WallSeconds = 1.3f;
        private readonly List<WindVfx.Ribbon> _lanes = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _walls = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _sigil = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _pressure = new List<WindVfx.Ribbon>();
        private WindVfx.Ribbon _edgeLeft, _edgeRight;
        private WindVfx.Motif _motif;
        private float _age, _gather, _half, _range;
        public float LifeSeconds => _gather + WallSeconds + 0.4f;
        public float Gather => _gather;

        public static AmihanStormFan Build(Transform parent, Vector3 origin, Vector3 forward, float gather)
        {
            var go = new GameObject("AmihanStormFan");
            go.transform.SetParent(parent, false);
            forward.y = 0.0f;
            go.transform.SetPositionAndRotation(VfxShapes.GroundPoint(origin) + Vector3.up * 0.03f,
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward));
            var fx = go.AddComponent<AmihanStormFan>();
            fx._gather = gather; fx._half = AmihanRules.StormSurgeHalfAngle; fx._range = 26.0f;
            for (int i = 0; i < Lanes; i++)
            {
                float a0 = -fx._half + 2 * fx._half * i / Lanes + 1.2f;
                float a1 = -fx._half + 2 * fx._half * (i + 1) / Lanes - 1.2f;
                fx._lanes.Add(WindVfx.BuildMesh(go.transform, "FanLane" + i, WindVfx.FanLane(fx._range, a0, a1, 30, 0.8f), 10.0f, 0.3f, i * 1.9f));
            }
            var left = new List<Vector3>(); var right = new List<Vector3>();
            for (int i = 0; i < 24; i++)
            {
                float r = Mathf.Lerp(0.7f, fx._range, i / 23.0f);
                float la = -fx._half * Mathf.Deg2Rad, ra = fx._half * Mathf.Deg2Rad;
                left.Add(new Vector3(Mathf.Sin(la) * r, 0.02f, Mathf.Cos(la) * r));
                right.Add(new Vector3(Mathf.Sin(ra) * r, 0.02f, Mathf.Cos(ra) * r));
            }
            fx._edgeLeft = WindVfx.Build(go.transform, "FanEdgeLeft", left, 0.16f, WindVfx.Flat(left), 12.0f, 0.5f, 20.0f);
            fx._edgeRight = WindVfx.Build(go.transform, "FanEdgeRight", right, 0.16f, WindVfx.Flat(right), 12.0f, 0.5f, 21.0f);
            for (int i = 0; i < 3; i++)
            {
                var arc = WindVfx.Arc(1.0f, fx._half * 2.0f + 6.0f, 32, 0.0f);
                for (int k = 0; k < arc.Length; k++)
                {
                    float edge = Mathf.Abs(k / (float)(arc.Length - 1) * 2.0f - 1.0f);
                    arc[k].y = (1.9f - i * 0.5f) * (1.0f - edge * edge * 0.4f);
                    arc[k] = new Vector3(arc[k].x, arc[k].y + 0.2f, arc[k].z);
                }
                fx._walls.Add(WindVfx.Build(go.transform, "StormWall" + i, arc, 0.55f - i * 0.12f, WindVfx.Standing, 4.0f + i * 2.0f, 0.2f, 30.0f + i));
            }
            // The kasikus: graduated squares turned to diamonds, radiating from her.
            for (int i = 0; i < 4; i++)
            {
                float r = 0.6f + i * 0.45f;
                var diamond = new List<Vector3>();
                for (int k = 0; k <= 4; k++)
                {
                    float a = k * 90.0f * Mathf.Deg2Rad;
                    diamond.Add(new Vector3(Mathf.Sin(a) * r, 0.03f, Mathf.Cos(a) * r));
                }
                var dense = new List<Vector3>();
                for (int k = 0; k < 4; k++) for (int j = 0; j < 6; j++) dense.Add(Vector3.Lerp(diamond[k], diamond[k + 1], j / 6.0f));
                dense.Add(diamond[4]);
                fx._sigil.Add(WindVfx.Build(go.transform, "Kasikus" + i, dense, 0.1f, WindVfx.Flat(dense), 3.0f, 0.4f, 40.0f + i));
            }
            for (int i = 0; i < 2; i++)
            {
                var ring = WindVfx.Arc(1.0f, 330.0f, 30, 1.2f + i * 0.9f);
                fx._pressure.Add(WindVfx.Build(go.transform, "PressureRing" + i, ring, 0.12f, WindVfx.Flat(ring), 6.0f, 0.25f, 50.0f + i));
            }
            fx._motif = new WindVfx.Motif(go.transform, 30, origin.x * 0.7f + origin.z * 3.3f);
            fx.StepTo(0.0f);
            return fx;
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= LifeSeconds) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float t = _age;
            bool calm = WindVfx.Reduced;
            float g = Mathf.Clamp01(t / _gather);
            float after = t - _gather;
            // GATHER: streaks rush outward, accelerating as the pressure builds (phase ~ t^2).
            float phase = calm ? 0.0f : t * t * 1.6f + t * 2.0f;
            float gatherAlpha = Mathf.Lerp(0.25f, 0.75f, g) * (1.0f - WindVfx.Ease(0.0f, 0.35f, after));
            for (int i = 0; i < _lanes.Count; i++)
                _lanes[i].Set(gatherAlpha * (0.8f + 0.2f * Mathf.Sin(i * 1.3f)), phase + i * 0.3f,
                              Mathf.Lerp(0.15f, 1.0f, WindVfx.Ease(0.0f, 0.8f, g)), 0.0f, WindVfx.Ease(0.1f, 0.4f, after));
            float edges = WindVfx.Ease(0.0f, 0.3f, t) * (1.0f - WindVfx.Ease(0.2f, 0.9f, after));
            _edgeLeft.Set(edges, phase * 0.5f, Mathf.Lerp(0.2f, 1.0f, g), 0, 0.2f);
            _edgeRight.Set(edges, phase * 0.5f, Mathf.Lerp(0.2f, 1.0f, g), 0, 0.2f);
            for (int i = 0; i < _pressure.Count; i++)
            {
                float squeeze = Mathf.Lerp(3.2f - i, 0.9f + i * 0.2f, g);
                _pressure[i].GameObject.transform.localScale = new Vector3(squeeze, 1, squeeze);
                _pressure[i].GameObject.transform.localRotation = Quaternion.Euler(0, (i == 0 ? 1 : -1) * t * (90 + 140 * g), 0);
                _pressure[i].Set(0.7f * WindVfx.Ease(0.1f, 0.8f, t) * (after > 0 ? 0 : 1), phase, 1, 0, 0.1f);
            }
            // RELEASE: the wall sweeps out to the whole fan in WallSeconds, easing out so the
            // first metres are violent and the far end arrives as a blast front.
            float sweep = after < 0 ? 0.0f : 1.0f - Mathf.Pow(1.0f - Mathf.Clamp01(after / WallSeconds), 2.2f);
            for (int i = 0; i < _walls.Count; i++)
            {
                float lag = Mathf.Clamp01(sweep - i * 0.06f);
                float radius = Mathf.Lerp(0.8f, _range, lag);
                _walls[i].GameObject.transform.localScale = new Vector3(radius, 1.0f + lag * 0.6f, radius);
                float alive = after < 0 ? 0 : (1.0f - WindVfx.Ease(WallSeconds - 0.35f, WallSeconds + 0.3f, after));
                _walls[i].Set(alive * (1.0f - i * 0.22f), phase * (1.2f + i * 0.3f), 1, 0, WindVfx.Ease(0.5f, 1.4f, after) * 0.9f);
            }
            for (int i = 0; i < _sigil.Count; i++)
            {
                float flash = after < 0 ? WindVfx.Ease(_gather - 0.6f, _gather, t) * 0.4f
                    : WindVfx.Envelope(after, i * 0.06f, 0.08f, 0.9f + i * 0.05f, 0.5f);
                float grow = 1.0f + Mathf.Max(0, after) * (1.5f + i * 0.8f);
                _sigil[i].GameObject.transform.localScale = new Vector3(grow, 1, grow);
                _sigil[i].GameObject.transform.localRotation = Quaternion.Euler(0, 45.0f + (i % 2 == 0 ? 1 : -1) * t * 20.0f, 0);
                _sigil[i].Set(flash, phase, 1, 0, WindVfx.Ease(0.3f, 0.9f, after));
            }
            float half = _half * Mathf.Deg2Rad, range = _range;
            _motif.Step(after < 0 ? 0 : Mathf.Clamp01(after / (WallSeconds + 0.2f)), 1.0f, (start, drift, u) =>
            {
                float a = (start.x * 2.0f) * half;
                float r = 0.8f + u * range * (0.4f + drift.z * 0.6f);
                return new Vector3(Mathf.Sin(a) * r, 0.3f + start.y * 1.6f + drift.y * u * 1.2f, Mathf.Cos(a) * r);
            }, 0.12f);
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
    /// `CharacterMotor` so a status needs no per-map wiring. Frozen and Tagged already have their
    /// own tells (the ice coat, the caught mark), so only the two new statuses are drawn here.
    /// </summary>
    public sealed class StatusBodyMarks : MonoBehaviour
    {
        private CharacterMotor _body;
        private WhirledMark _whirled;
        private ChilledMark _chilled;

        private void Awake() => _body = GetComponent<CharacterMotor>();

        private void LateUpdate()
        {
            if (_body == null) return;
            if (_body.IsWhirled && _whirled == null)
            {
                _whirled = WhirledMark.Attach(_body);
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
    }
}
