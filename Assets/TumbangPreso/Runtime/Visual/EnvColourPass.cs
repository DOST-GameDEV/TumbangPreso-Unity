using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Per-instance COLOUR for the map's dressing: seeded facade tints, foliage variation,
    /// and the road's warm-neutral correction. Converted from
    /// `scripts/systems/env_toon_pass.gd`.
    ///
    /// ⚠️⚠️ IT DOES NOT APPLY TOON SHADING OR OUTLINES, AND THE GODOT NAME IS A LEFTOVER —
    /// which is why this file is called EnvColourPass instead. Phase 8 put the whole map on a
    /// toon shader with an inverted-hull outline so world and characters shared one shading
    /// model. That shipped, was played, and was reverted on 2026-07-29: 🧑 *"the current
    /// shaders look terrible and are causing severe lag on other PCs. The toon shader is
    /// creating ugly, horizontal banded shadows."*
    ///
    /// **Do not re-add a world toon pass.** Characters keep theirs; the world is plainly lit
    /// and cheap. Both halves of that complaint were structural rather than tuning — the
    /// banding is what a stepped ramp does across a large flat surface, and the cost is what
    /// an inverted hull costs on every mesh in a dressed street.
    ///
    /// What survives is the part that was actually working: colour variety. Kenney's kits
    /// ship one material per prop, so an unretinted street is fifty identical cream houses.
    /// </summary>
    public sealed class EnvColourPass : MonoBehaviour
    {
        /// <summary>
        /// The facade palette. These are the real Manila colours the art direction names, not
        /// a generated ramp: cream is the default, and every street has some unpainted concrete.
        /// </summary>
        public static readonly Color[] FacadeTints =
        {
            Hex("e2d2ac"),   // ENV_PAINT_CREAM — the default Manila facade
            Hex("b5664c"),   // ENV_PAINT_TERRA — oxide red / terracotta
            Hex("86b4a6"),   // ENV_PAINT_MINT  — the pale mint that is everywhere
            Hex("c9994a"),   // ENV_PAINT_OCHRE — mustard
            Hex("b7b2a6"),   // ENV_CONCRETE    — unpainted, and every street has some
            Hex("cbb9b4"),   // a washed-out rose; the same family, one step cooler
        };

        public static readonly Color[] FoliageTints =
        {
            new Color(0.86f, 0.94f, 0.78f),
            new Color(0.72f, 0.82f, 0.60f),
            new Color(0.95f, 0.90f, 0.66f),
            new Color(0.62f, 0.76f, 0.58f),
            new Color(0.80f, 0.86f, 0.70f),
        };

        public static readonly string[] RoadGroups = { "Kalsada", "Road", "Slab", "Apron" };

        /// <summary>Asphalt reads blue-grey out of the kit; this warms it to the neutral the
        /// rest of the palette sits against.</summary>
        public static readonly Color RoadTint = new Color(0.66f, 0.62f, 0.55f);

        public static readonly string[] SlabGroups = { "Slab" };
        public static readonly Color SlabTint = new Color(0.88f, 0.85f, 0.78f);

        /// <summary>The far belt fades toward the sky so the horizon does not read as a wall.</summary>
        public static readonly Color BeltFade = new Color(0.878f, 0.812f, 0.694f);
        public const float BeltFadeAmount = 0.68f;

        public static readonly string[] FacadeGroups =
        {
            "Bahay", "Likod", "Malayo", "Kanto", "Puno", "TreesNear", "TreesFar",
            "Layer1", "Layer2", "Belt", "CrossRow",
        };

        /// <summary>Hanging laundry sways. The anchor is the line it hangs from, so the drop
        /// scales the motion down toward the pegged edge rather than swinging the whole sheet.</summary>
        public const string WindPrefix = "Sampay";
        public const float WindStrength = 0.075f;
        public const float WindSpeed = 1.15f;
        public const float WindAnchorY = 2.62f;
        public const float WindDrop = 0.70f;

        [Tooltip("Same seed, same street. Change it and every house repaints.")]
        [SerializeField] private int _seed = 20260729;

        private readonly List<Transform> _wind = new List<Transform>();
        private readonly List<Vector3> _windHome = new List<Vector3>();

        private void Start() => Apply();

        /// <summary>
        /// ⚠️ SEEDED, NOT RANDOM. The same map must repaint identically every run, or a
        /// screenshot cannot be compared against the last one and two peers see different
        /// streets. `Random.InitState` here rather than a shared generator, so nothing else
        /// in the frame perturbs the sequence.
        /// </summary>
        public void Apply()
        {
            Random.InitState(_seed);

            foreach (var renderer in GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                string name = renderer.transform.name;
                Transform parent = renderer.transform.parent;
                string group = parent != null ? parent.name : "";

                Color? tint = TintFor(name, group);
                if (tint == null) continue;

                // ⚠️ A PROPERTY BLOCK, NOT A MATERIAL INSTANCE. Writing `renderer.material`
                // clones the material per renderer, which on a dressed street is hundreds of
                // materials and the draw-call cost the toon pass was reverted for.
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", tint.Value);
                block.SetColor("_BaseColor", tint.Value);
                renderer.SetPropertyBlock(block);
            }

            CollectWind();
        }

        private Color? TintFor(string name, string group)
        {
            if (Contains(RoadGroups, group) || Contains(RoadGroups, name))
                return Contains(SlabGroups, group) ? SlabTint : RoadTint;

            if (name.Contains("Puno") || name.Contains("Tree") || group.Contains("Trees"))
                return FoliageTints[Random.Range(0, FoliageTints.Length)];

            if (Contains(FacadeGroups, group))
            {
                Color tint = FacadeTints[Random.Range(0, FacadeTints.Length)];

                // The far belt washes toward the sky colour so distance reads as distance.
                if (group == "Belt") tint = Color.Lerp(tint, BeltFade, BeltFadeAmount);

                return tint;
            }

            return null;
        }

        private void CollectWind()
        {
            _wind.Clear();
            _windHome.Clear();

            foreach (var t in GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (!t.name.StartsWith(WindPrefix)) continue;

                _wind.Add(t);
                _windHome.Add(t.localPosition);
            }
        }

        /// <summary>
        /// Laundry sway. Cheap on purpose: a sine per hanging sheet, no simulation, and the
        /// motion scales down toward the line it is pegged to.
        /// </summary>
        private void Update()
        {
            if (_wind.Count == 0) return;

            float t = Time.time * WindSpeed;

            for (int i = 0; i < _wind.Count; i++)
            {
                var tr = _wind[i];
                if (tr == null) continue;

                Vector3 home = _windHome[i];

                // Distance below the line, so a sheet swings at its hem and not at its pegs.
                float drop = Mathf.Clamp01((WindAnchorY - home.y) / WindDrop);
                float sway = Mathf.Sin(t + i * 0.7f) * WindStrength * drop;

                tr.localPosition = home + new Vector3(sway, 0.0f, sway * 0.4f);
            }
        }

        private static bool Contains(string[] set, string value)
        {
            foreach (string s in set) if (s == value) return true;
            return false;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
