using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️ PHAISTER'S VOODOO WORLD OBJECTS (ABILITY-2): the cursed doll, the pin's cone and HIGOP's black
    /// hole. Gameplay is resolved on the host by distance; the bodies here are first-pass shapes in her
    /// magenta and stitched cloth, placeholders for the presentation pass (plan § 5, `ultimates.md` § 3.3),
    /// recorded as such in TODO ABILITY-2.
    /// </summary>
    public sealed class VoodooDoll : MonoBehaviour
    {
        private static readonly Color Cloth = new Color(0.62f, 0.45f, 0.36f), Stitch = new Color(0.16f, 0.08f, 0.12f),
                                      Pin = new Color(0.85f, 0.2f, 0.62f);
        private Vector3 _velocity;
        private int _owner;
        private bool _done;

        public static VoodooDoll Spawn(Vector3 origin, Vector3 target, int ownerSlot)
        {
            var go = new GameObject("VoodooDoll");
            go.transform.position = origin;
            var d = go.AddComponent<VoodooDoll>();
            d._owner = ownerSlot;
            d._velocity = Slipper.SolveArc(origin, target, VoodooRules.DollSpeed);
            // The modelled doll (`tools/build_rework_props.py`); the blocks below are the fallback only.
            if (Visual.ReworkProp.Spawn("doll", go.transform, Visual.ReworkProp.VoodooPalette) != null) return d;
            var body = Visual.GrowthVfx.Block(go.transform, "doll-body", new Vector3(0.16f, 0.22f, 0.10f), Cloth).transform;
            var head = Visual.GrowthVfx.Block(go.transform, "doll-head", new Vector3(0.14f, 0.13f, 0.12f), Cloth).transform;
            head.localPosition = new Vector3(0f, 0.18f, 0f);
            var seam = Visual.GrowthVfx.Block(go.transform, "doll-seam", new Vector3(0.02f, 0.22f, 0.11f), Stitch).transform;
            seam.localPosition = new Vector3(0f, 0.02f, 0f);
            var pin = Visual.GrowthVfx.Block(go.transform, "doll-pin", new Vector3(0.02f, 0.02f, 0.26f), Pin, 0.4f).transform;
            pin.localPosition = new Vector3(0.02f, 0.18f, 0f); pin.localRotation = Quaternion.Euler(0f, 30f, 0f);
            body.localRotation = Quaternion.identity;
            return d;
        }

        private void FixedUpdate()
        {
            if (_done) return;
            float dt = Time.fixedDeltaTime;
            _velocity.y -= Balance.Gravity * dt;
            transform.position += _velocity * dt;
            transform.Rotate(Vector3.right, 540f * dt, Space.Self);
            bool landed = transform.position.y <= Slipper.GroundY(transform.position) + 0.1f && _velocity.y < 0f;
            if (NetAuthority.ShouldResolve())
            {
                var round = GameServices.Round;
                if (round != null)
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == _owner) continue;
                        Vector3 d = p.transform.position + Vector3.up * 0.8f - transform.position;
                        float reach = landed ? VoodooRules.DollHitRadius + 0.8f : VoodooRules.DollHitRadius;
                        if (d.magnitude > reach) continue;
                        p.ApplyDisoriented();
                        Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroCursed, _owner, p.PlayerSlot, p.transform.position, StatusRules.DisorientedSeconds);
                        landed = true;
                        break;
                    }
            }
            if (landed || transform.position.y < -20f)
            {
                _done = true;
                NetCue.Play("sfx_blink_arrive", transform.position);
                Destroy(gameObject, 0.25f);
            }
        }
    }

    /// <summary>The pin's cone, drawn as a short-lived fan of stitched thread on the ground.</summary>
    public sealed class VoodooConeFlash : MonoBehaviour
    {
        private float _age;
        public static void Spawn(Vector3 at, Vector3 forward)
        {
            var go = new GameObject("VoodooCone");
            go.transform.position = at + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.LookRotation(forward);
            float half = VoodooRules.VulnerableConeDegrees * 0.5f, range = VoodooRules.VulnerableConeRange;
            var colour = new Color(0.85f, 0.2f, 0.62f);
            foreach (float a in new[] { -half, -half * 0.35f, half * 0.35f, half })
            {
                var t = Visual.GrowthVfx.Block(go.transform, "thread", new Vector3(0.04f, 0.02f, range), colour, 0.5f).transform;
                t.localRotation = Quaternion.Euler(0f, a, 0f);
                t.localPosition = t.localRotation * new Vector3(0f, 0f, range * 0.5f);
            }
            go.AddComponent<VoodooConeFlash>();
        }
        private void Update() { _age += Time.deltaTime; transform.localScale = new Vector3(1f, 1f, Mathf.Min(1f, _age / 0.15f)); if (_age > 0.6f) Destroy(gameObject); }
    }

    /// <summary>
    /// HIGOP, the black hole. A mark grows through her slow cast; then for the pull a dark core with a
    /// violet rim and a ring of pins turns while `HeroHazards.SeanceVoidComponent` (the same pull Kuro's
    /// maw uses, on every peer for its own body and the host for all) drags every other body and every
    /// loose slipper that is not hers to the heart. The pull outruns a run, so pushing away gains a few
    /// steps and loses them (owner: *"they can try to move away but they js get sucked back in"*).
    /// </summary>
    public sealed class VoodooBlackHole : MonoBehaviour
    {
        private static readonly Color Core = new Color(0.04f, 0.0f, 0.05f), Rim = new Color(0.78f, 0.22f, 0.86f),
                                      PinCol = new Color(0.95f, 0.85f, 0.9f);
        private float _age, _castLeft, _life;
        private Transform _core, _rim, _pins;
        private bool _pulling;
        private int _owner;

        public static GameObject Spawn(Vector3 at, int ownerSlot, float castSeconds, float pullSeconds)
        {
            var go = new GameObject("Higop");
            go.transform.position = Visual.VfxShapes.GroundPoint(at);
            var h = go.AddComponent<VoodooBlackHole>();
            h._owner = ownerSlot; h._castLeft = castSeconds; h._life = pullSeconds;
            h._core = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Visual.VfxMaterial.StripCollider(h._core.gameObject);
            h._core.SetParent(go.transform, false); h._core.localPosition = Vector3.up * 1.4f;
            Visual.VfxMaterial.Solid(h._core.GetComponent<MeshRenderer>(), Core, 0.0f);
            h._rim = new GameObject("rim").transform; h._rim.SetParent(go.transform, false); h._rim.localPosition = Vector3.up * 1.4f;
            // The modelled accretion ring of stitched thread and pins; the segments below are the fallback only.
            if (Visual.ReworkProp.Spawn("higop", h._rim, Visual.ReworkProp.VoodooPalette) != null)
            {
                h._rim.localRotation = Quaternion.Euler(18f, 0f, 8f);
                go.transform.localScale = Vector3.one * 0.01f;
                return go;
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + (i % 2) * 7f;
                var seg = Visual.GrowthVfx.Block(h._rim, "rim-" + i, new Vector3(0.10f, 0.10f, 0.62f), Rim, 0.8f).transform;
                seg.localRotation = Quaternion.Euler(0f, a, 0f);
                seg.localPosition = seg.localRotation * new Vector3(1.05f + 0.04f * (i % 3), 0f, 0f);
            }
            h._pins = new GameObject("pins").transform; h._pins.SetParent(go.transform, false); h._pins.localPosition = Vector3.up * 1.4f;
            for (int i = 0; i < 6; i++)
            {
                var pin = Visual.GrowthVfx.Block(h._pins, "pin-" + i, new Vector3(0.03f, 0.03f, 0.42f), PinCol, 0.3f).transform;
                pin.localRotation = Quaternion.Euler(20f * (i % 2 == 0 ? 1 : -1), i * 60f + 11f * i, 0f);
                pin.localPosition = pin.localRotation * new Vector3(0f, 0f, 1.8f + 0.15f * (i % 3));
            }
            go.transform.localScale = Vector3.one * 0.01f;
            return go;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (!_pulling)
            {
                _castLeft -= Time.deltaTime;
                // Through the cast it gathers: a small dark bead that grows, the rim spinning up.
                float t = Mathf.Clamp01(_age / Mathf.Max(0.1f, _age + _castLeft));
                transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.45f, t);
                if (_castLeft <= 0.0f) BeginPull();
            }
            else
            {
                _life -= Time.deltaTime;
                float open = Mathf.Clamp01((_age) / 0.35f);
                float close = Mathf.Clamp01(_life / 0.3f);
                transform.localScale = Vector3.one * Mathf.Min(open, close);
                if (_life <= 0.0f) { GameServices.Audio?.PlayAt("sfx_phaister_higop_close", transform.position); Destroy(gameObject); return; }
            }
            if (_rim != null) _rim.Rotate(0f, 220f * Time.deltaTime, 0f, Space.Self);
            if (_pins != null) _pins.Rotate(0f, -90f * Time.deltaTime, 0f, Space.Self);
        }

        private void BeginPull()
        {
            _pulling = true; _age = 0.0f;
            GameServices.Audio?.PlayAt("sfx_phaister_higop_open", transform.position);
            var pull = gameObject.AddComponent<HeroHazards.SeanceVoidComponent>();
            pull.Radius = VoodooRules.HigopRadius; pull.Duration = _life; pull.OwnerSlot = _owner;
            pull.PullStrength = 50.0f; pull.LiftHeight = 0.0f; pull.SlipperPull = 10.0f;
            pull.SparedSlipperOwner = _owner; pull.DrowseEvery = 0.0f;
            HazardVolume.Attach(gameObject, VoodooRules.HigopRadius, _owner);
        }
    }
}
