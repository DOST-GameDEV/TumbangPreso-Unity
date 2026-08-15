using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Generic slow-zone hazard, converted from `scripts/systems/hazard_zone.gd`.
    ///
    /// Used by Palayok's Shatter Trap; reusable as-is for map hazards later (GDD §5 — Bayan
    /// Plaza mud patches, Palengke wet floor).
    ///
    /// ⚠️ IT WATCHES BODIES, NOT A COMPANION HURTBOX. The original masked layer 2, the
    /// `Hurtbox` layer, which was deleted with the prop-as-player rewrite. It watches the
    /// character bodies directly instead — there is no hurtbox to find any more.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HazardZone : MonoBehaviour
    {
        public float SpeedMultiplier = 0.5f;

        /// <summary>&lt;= 0 means permanent — for map hazards, not specials.</summary>
        public float Lifetime = 4.0f;

        /// <summary>
        /// The test arena's floor top sits at world y = 0.5. Every other placement in this
        /// arena is similarly hardcoded pending real per-map geometry; this is the same kind
        /// of placeholder, not a design decision, and becomes map-relative when real maps land.
        /// </summary>
        public const float FloorTopY = 0.5f;

        /// <summary>Timed hazards are cleaned up between rounds — an ability-spawned trap
        /// must not outlive the round it was cast in. A PERMANENT hazard (duration &lt;= 0) is
        /// part of the map rather than the round and survives a world reset.</summary>
        public const string RoundScopedTag = "hazard_zone";

        public bool IsRoundScoped { get; private set; }

        private float _life;

        public static HazardZone Spawn(Transform parent, Vector3 at, float radius,
            float duration, float multiplier = 0.5f)
        {
            var go = new GameObject("HazardZone");
            go.transform.SetParent(parent, false);

            // ⚠️ POSITION BEFORE THE VISUAL IS BUILT. The Godot original documents this at
            // length: its `_ready` fired synchronously inside `add_child`, so a position set
            // afterwards left the zone at the origin. Here Awake runs on AddComponent, so the
            // transform is placed first for the same reason.
            go.transform.position = at;

            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;

            var zone = go.AddComponent<HazardZone>();
            zone.SpeedMultiplier = multiplier;
            zone.Lifetime = duration;
            zone.IsRoundScoped = duration > 0.0f;

            zone.BuildVisual(radius);
            return zone;
        }

        private void Awake() => _life = Lifetime;

        /// <summary>
        /// Q-7: it gained a visual because it was previously invisible in game. Built from the
        /// COLLIDER's own radius rather than a second exported number that could drift from
        /// it, so a scene-placed map hazard gets the visual for free.
        /// </summary>
        private void BuildVisual(float radius)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "HazardDisc";
            Destroy(disc.GetComponent<Collider>());

            disc.transform.SetParent(transform, false);
            disc.transform.localScale = new Vector3(radius * 2.0f, 0.025f, radius * 2.0f);
            disc.transform.localPosition =
                new Vector3(0.0f, FloorTopY - transform.position.y + 0.01f, 0.0f);

            var block = new MaterialPropertyBlock();
            var tint = new Color(UiTheme.Impact.r, UiTheme.Impact.g, UiTheme.Impact.b, 0.35f);
            block.SetColor("_Color", tint);
            block.SetColor("_BaseColor", tint);
            disc.GetComponent<Renderer>().SetPropertyBlock(block);
        }

        private void Update()
        {
            if (Lifetime <= 0.0f) return;   // permanent map hazard

            _life -= Time.deltaTime;
            if (_life <= 0.0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var m = other.GetComponentInParent<CharacterMotor>();
            if (m != null) m.EnterSpeedZone(SpeedMultiplier);
        }

        private void OnTriggerExit(Collider other)
        {
            var m = other.GetComponentInParent<CharacterMotor>();
            if (m != null) m.ExitSpeedZone(SpeedMultiplier);
        }
    }
}
