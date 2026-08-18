using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Per-instance COLOUR for the map's dressing: seeded facade tints, recoloured roof
    /// atlases, foliage variation, and the road's warm-neutral correction. Converted from
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
    /// <remarks>
    /// ⚠️⚠️ THREE THINGS WERE MISSING FROM THE FIRST CONVERSION AND ALL THREE WERE VISIBLE.
    /// Reported from a side-by-side screenshot of the two builds:
    ///
    ///  1. **THE ROOF ATLASES.** `FACADE_TINTS` multiplies the whole atlas, which varies the
    ///     near-white WALL and does nothing at all to the roof: the roof swatch is a saturated
    ///     green (#61CB8B) and green multiplied by any tint is still green. A City Kit
    ///     building is ONE mesh with ONE material, so there is no surface index to tint
    ///     separately either. Godot swaps the whole ATLAS per instance, from the six
    ///     pre-generated `colormap_roof_*.png` files, and those files are already in this
    ///     repo. Without the swap the Unity street had terracotta walls under mint roofs,
    ///     which is exactly what the report showed.
    ///
    ///  2. **THE TINT WAS RANDOM RATHER THAN NAME-HASHED.** `Random.Range` walks a sequence
    ///     in renderer order, so a house's colour depends on how many renderers happened to
    ///     be visited before it. Godot hashes the instance's own stable NAME. The visible
    ///     difference is that consecutive houses walked the palette in order here and read as
    ///     a repeating rainbow, which is the exact thing the multiplier in `_facade_tint` was
    ///     chosen to avoid.
    ///
    ///  3. **CARS AND TREES WERE PAINTED AS FACADES.** `Layer1` holds houses AND parked cars;
    ///     `Layer2` holds houses AND street trees. Godot classifies by the map generator's own
    ///     instance name (`_is_building`) and leaves a car alone. Tinting by group handed
    ///     every van a house colour.
    ///
    /// ⚠️ AND THE ALBEDO IS MULTIPLIED, NOT REPLACED. `mat.albedo_color = base * tint` in the
    /// .gd. Assigning the tint outright throws away whatever the kit material already carried
    /// and flattens two different props to one colour.
    /// </remarks>
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

        /// <summary>
        /// ⚠️ THE ROOF VARIANTS, IN THE .gd's ORDER. `tools/models/make_roof_atlases.py` emits
        /// one atlas per roof colour from the shipped one; the six PNGs are committed in both
        /// repos. Order is load-bearing because the atlas is chosen by an index into this list
        /// and a reordering repaints every roof in the game.
        /// </summary>
        public static readonly string[] RoofAtlases =
        {
            "colormap_roof_terra",
            "colormap_roof_rust",
            "colormap_roof_slate",
            "colormap_roof_ochre",
            "colormap_roof_galv",
            "colormap_roof_teal",
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

        private readonly List<Transform> _wind = new List<Transform>();
        private readonly List<Vector3> _windHome = new List<Vector3>();

        /// <summary>
        /// ⚠️ THE LAYERS ARE THE CHILDREN OF `Dressing`, exactly as in the .gd, where the
        /// script IS that node. Walking from the map root instead would treat `Bounds`,
        /// `Floor` and `SpawnPoints` as dressing groups and would find no group name at all
        /// for the meshes inside them.
        /// </summary>
        private Transform DressingRoot()
        {
            var dressing = transform.Find("Dressing");
            return dressing != null ? dressing : transform;
        }

        private void Start() => Apply();

        public void Apply()
        {
            var root = DressingRoot();
            int painted = 0, seen = 0, roofed = 0;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                seen++;

                // ⚠️⚠️ THE GROUP IS THE LAYER NODE, AND THE INSTANCE IS ITS DIRECT CHILD. In
                // Godot a layer's children ARE the generator's named instances ("L1_7_E",
                // "BeltX_31") and the Kenney mesh sits somewhere inside. The Unity conversion
                // keeps that shape, so both facts come from one walk up the chain.
                if (!Resolve(renderer.transform, root, out string group, out string instance))
                    continue;

                Color tint = Color.white;
                string roof = null;

                if (Contains(SlabGroups, group))
                {
                    tint = SlabTint;
                }
                else if (Contains(RoadGroups, group))
                {
                    tint = RoadTint;
                }
                else if (Contains(FacadeGroups, group))
                {
                    // ⚠️ CLASSIFY BY THE GENERATOR'S OWN NAME. "Is this a building?" cannot be
                    // answered from the mesh: Layer1 holds houses AND parked cars, Layer2 holds
                    // houses AND street trees, and the Belt holds both again. Keying off the
                    // LAYER handed a City Kit roof atlas to every van in the street.
                    if (IsBuilding(instance))
                    {
                        tint = FacadeTints[Pick(instance, 7, 0, FacadeTints.Length)];
                        roof = RoofAtlases[Pick(instance, 13, 5, RoofAtlases.Length)];
                    }
                    else if (instance.Contains("Tree") || instance.Contains("Puno"))
                    {
                        tint = FoliageTints[Pick(instance, 11, 3, FoliageTints.Length)];
                    }

                    // Everything else in these layers (parked cars) keeps its kit colours. A car
                    // is small, already varied by model, and is the one thing on the street that
                    // is SUPPOSED to be a bright manufactured colour.
                    if (group == "Belt" || group == "Malayo")
                        tint = Color.Lerp(tint, BeltFade, BeltFadeAmount);
                }
                else
                {
                    continue;
                }

                if (Paint(renderer, tint, roof)) painted++;
                if (roof != null) roofed++;
            }

            // ⚠️ IT REPORTS THE COUNT, because "the pass ran" and "the pass did anything" are
            // different claims and the first one is what a silent version of this told us for
            // the whole port. A zero here means the group names moved, not that the street is
            // already the right colour.
            Debug.Log($"[Env] repainted {painted} of {seen} renderers, {roofed} with a roof variant.");

            CollectWind();
        }

        /// <summary>
        /// The layer (group) this renderer belongs to, and the generator's name for the
        /// instance directly under it. Both are needed and both come from the same walk.
        /// </summary>
        private static bool Resolve(Transform t, Transform root, out string group, out string instance)
        {
            group = null;
            instance = null;

            for (var step = t; step != null && step != root; step = step.parent)
            {
                if (step.parent != root) continue;

                group = step.name;

                // The instance is the child of the layer on this same chain. Walk back down by
                // remembering the node whose parent is the layer.
                for (var down = t; down != null && down != step; down = down.parent)
                {
                    if (down.parent != step) continue;

                    instance = down.name;
                    break;
                }

                // A mesh parented straight onto the layer is its own instance.
                if (instance == null) instance = t.name;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The map generator's building names, and nothing else. Layer1/Layer2 rows are
        /// `L1_&lt;i&gt;_&lt;side&gt;` / `L2_&lt;i&gt;`, the silhouette belt is `BeltX_&lt;i&gt;` /
        /// `BeltZ_&lt;i&gt;`, and its trees are `BeltTreeX_&lt;i&gt;` — which is why this checks the
        /// belt prefixes BEFORE falling through, or every belt tree would match.
        ///
        /// ⚠️ EVERY `Puno*` NAME IS REJECTED BEFORE THE BUILDING TEST. A tree handed a City Kit
        /// roof atlas is the bug this guard exists for.
        /// </summary>
        private static bool IsBuilding(string name)
        {
            if (name.StartsWith("BeltTree") || name.Contains("Puno")) return false;

            return name.StartsWith("Bahay_")
                || name.StartsWith("Likod_")
                || name.StartsWith("Kanto_")
                || name.StartsWith("MalayoX_")
                || name.StartsWith("MalayoZ_")
                || name.StartsWith("L1_")
                || name.StartsWith("L2_")
                || name.StartsWith("Cross_")
                || name.StartsWith("BeltX_")
                || name.StartsWith("BeltZ_");
        }

        /// <summary>
        /// ⚠️ SEEDED OFF THE NAME, NEVER RANDOM — the same rule the map generator works under.
        /// A street that repaints itself every load cannot be reviewed in a screenshot and
        /// cannot be reproduced from a bug report, and two peers would see different streets.
        ///
        /// The exact GDScript hash, including the 31-multiply and the 0x7fffffff mask, so a
        /// given house is the same colour in both builds rather than merely stable in each.
        /// </summary>
        private static int NameHash(string text)
        {
            int value = 0;
            for (int i = 0; i < text.Length; i++)
                value = (value * 31 + text[i]) & 0x7fffffff;

            return value;
        }

        /// <summary>
        /// Index into a palette from an instance name, stirred by a per-palette multiplier.
        ///
        /// ⚠️⚠️ THE ARITHMETIC IS 64-BIT, AND DOING IT IN `int` THREW ON THE FIRST LOAD. The
        /// .gd is `FACADE_TINTS[(_name_hash(name) * 7) % FACADE_TINTS.size()]` and GDScript ints
        /// are 64-bit, so that product simply cannot overflow. A C# `int` holds the hash's full
        /// 0x7fffffff range with nothing to spare, so multiplying by 7 wraps NEGATIVE, `%` in
        /// C# keeps the sign of the dividend, and the lookup goes off the front of the array.
        /// Transcribing the expression faithfully is exactly what broke it.
        ///
        /// ⚠️ THE MULTIPLIERS ARE DIFFERENT PER PALETTE, ON PURPOSE. Sharing one would lock each
        /// wall colour to one roof and give six house types instead of the thirty the two
        /// palettes make between them, and consecutive houses (L1_0, L1_1, L1_2) would walk the
        /// palette in order and read as a repeating rainbow.
        /// </summary>
        private static int Pick(string instance, long multiplier, long offset, int count)
        {
            long value = (long)NameHash(instance) * multiplier + offset;
            return (int)(value % count);
        }

        /// <summary>
        /// The colour properties a material might actually carry, in the order they are tried.
        ///
        /// ⚠️ THE NAME DEPENDS ON THE SHADER AND THERE IS NO SAFE GUESS. Standard uses `_Color`,
        /// URP's Lit uses `_BaseColor`, glTF imports use `baseColorFactor`, and older kit
        /// materials use `_TintColor`. Setting one blindly is a silent no-op on the other three.
        /// </summary>
        private static readonly string[] ColourProperties =
        {
            "_BaseColor", "_Color", "baseColorFactor", "_TintColor",
        };

        /// <summary>
        /// ⚠️⚠️ THE glTF NAMES ARE IN HERE AND LEAVING THEM OUT MADE EVERY ROOF IN THE GAME
        /// GREEN. The pass reported `99 with a roof variant` on Eskinita — the exact number of
        /// buildings on the map — and not one of them changed colour, because the kit materials
        /// arrive through glTFast and carry `baseColorTexture`, which was in neither of the two
        /// names this list held. The TINT landed (`baseColorFactor` IS in
        /// <see cref="ColourProperties"/>, so the walls varied correctly), which is what made
        /// the fault so hard to see: the street was visibly being repainted, and only the roofs
        /// — the one thing a tint cannot vary, because green multiplied by anything is still
        /// green — were untouched. Measured against `Logs/shots-godot/g04-ready.png`, where the
        /// same street is red, rust and slate and this build had no red roof anywhere in frame.
        ///
        /// ⚠️ AND A MISS IS REPORTED NOW. `Paint` warned when no COLOUR property matched and
        /// said nothing when no TEXTURE property did, so the roof half failed in exactly the
        /// silence rule 5 exists to stop: *"a property block writes a NAMED property, silently
        /// doing nothing when the shader has none"*. Same trap, one list along.
        /// </summary>
        private static readonly string[] TextureProperties =
        {
            "_BaseMap", "_MainTex", "baseColorTexture", "_BaseColorMap",
        };

        /// <summary>
        /// One material per (source, tint, roof) triple, so a street of six colours and six
        /// roofs costs at most thirty-six materials rather than one per house.
        /// </summary>
        private static readonly Dictionary<(Material, Color, string), Material> Painted =
            new Dictionary<(Material, Color, string), Material>();

        private static readonly Dictionary<string, Texture2D> RoofCache =
            new Dictionary<string, Texture2D>();

        /// <summary>
        /// ⚠️⚠️ A MATERIAL VARIANT, NOT A PROPERTY BLOCK, AND THAT IS HALF THE FIX. The block
        /// version repainted 418 of 434 renderers on every load and changed nothing on screen:
        /// a block writes a NAMED property, and if the shader has no property by that name the
        /// write is discarded without an error.
        ///
        /// ⚠️ AND THE ALBEDO IS MULTIPLIED INTO WHATEVER THE KIT ALREADY HAD, matching
        /// `mat.albedo_color = base_mat.albedo_color * tint`. Overwriting it flattens two
        /// differently-authored props to the same colour.
        /// </summary>
        /// <summary>
        /// § THE TINT MULTIPLY, IN THE SPACE THE MULTIPLY IS DEFINED IN.
        ///
        /// ⚠️⚠️ `GetColor(p) * tint` PASSED THE PRODUCT THROUGH THE sRGB CURVE A SECOND TIME AND
        /// MADE THE ROAD 2.6× TOO DARK. Measured against the Godot reference frame this repo
        /// ships, `Logs/shots-godot/g04-ready.png`, over the same band of the same street from
        /// the same camera:
        ///
        ///     Godot road   R 79.2  G 68.8  B 70.8
        ///     this, was    R 43.7  G 29.3  B 26.5
        ///
        /// Too dark AND too red, and the second symptom is what identifies the fault rather than
        /// merely measuring it. `Material.GetColor` hands back the LINEAR value in a linear
        /// project and `Material.SetColor` converts gamma to linear on the way in, so this read
        /// linear, scaled it, and then had it converted AGAIN. The curve is steeper the darker
        /// the channel, so `RoadTint`'s (0.66, 0.62, 0.55) came out as ratios of (0.60, 0.55,
        /// 0.48): blue is knocked down hardest, and a neutral grey asphalt turns into a warm
        /// brown-black. The red cast was the tell.
        ///
        /// ⚠️ A TINT IS A RATIO, NOT A COLOUR, WHICH IS WHY IT DOES NOT GET CONVERTED. Godot
        /// computes `mat.albedo_color = base * tint` on two sRGB Colors and converts the PRODUCT
        /// once, at upload, because `albedo_color` is a `source_color` uniform. So the multiply
        /// happens in sRGB and exactly one conversion follows it. This does the same thing:
        /// undo `GetColor`'s linearity, multiply, and let `SetColor` perform the one conversion
        /// Godot performs.
        ///
        /// ⚠️ AND IT WAS INVISIBLE IN GAMMA SPACE, like the palette in `ToonSkin.ToShading` and
        /// the ambient in `TscnImporter.Energised`. All three are the same fault: a number that
        /// only has one meaning while nothing converts anywhere. They had to be found together
        /// and they had to be fixed together.
        /// </summary>
        private static Color Tinted(Color stored, Color tint)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return stored * tint;

            Color srgb = stored.gamma;
            return new Color(srgb.r * tint.r, srgb.g * tint.g, srgb.b * tint.b, stored.a);
        }

        private static bool Paint(Renderer renderer, Color tint, string roof)
        {
            var source = renderer.sharedMaterial;
            if (source == null) return false;

            var key = (source, tint, roof ?? "");

            if (!Painted.TryGetValue(key, out var material) || material == null)
            {
                material = new Material(source)
                {
                    name = $"{source.name}_{ColorUtility.ToHtmlStringRGB(tint)}{(roof == null ? "" : "_" + roof)}",
                };

                bool set = false;

                foreach (var property in ColourProperties)
                {
                    if (!material.HasProperty(property)) continue;

                    material.SetColor(property, Tinted(material.GetColor(property), tint));
                    set = true;
                }

                if (roof != null)
                {
                    var atlas = LoadRoof(roof);

                    if (atlas != null)
                    {
                        bool swapped = false;

                        foreach (var property in TextureProperties)
                        {
                            if (!material.HasProperty(property)) continue;

                            material.SetTexture(property, atlas);
                            swapped = true;
                        }

                        // ⚠️ IT SAYS SO WHEN IT MISSES. See TextureProperties: this half failed
                        // silently for the whole port while the pass reported a roof count that
                        // was correct and meant nothing. Rule 5 in this ledger is about exactly
                        // this — a named property that does not exist is a no-op, not an error.
                        if (!swapped)
                        {
                            Debug.LogWarning(
                                $"[Env] '{source.shader.name}' has no texture property this pass " +
                                "knows, so its roof atlas was not applied and it keeps the kit's " +
                                "own roof. Add the name to EnvColourPass.TextureProperties.");
                        }
                    }
                }

                if (!set)
                {
                    Debug.LogWarning($"[Env] '{source.shader.name}' has no colour property this " +
                                     "pass knows; that surface keeps the kit's own colour.");
                }

                Painted[key] = material;
            }

            renderer.sharedMaterial = material;
            return true;
        }

        /// <summary>
        /// ⚠️ FROM `Resources`, NOT FROM THE ART FOLDER. A texture referenced only by a string
        /// is stripped from a player build; `RoofAtlasSync` copies the six PNGs under
        /// `Resources/Models/roofs` so the reference survives. A missing atlas leaves the kit's
        /// own roof rather than drawing magenta.
        /// </summary>
        private static Texture2D LoadRoof(string atlas)
        {
            if (RoofCache.TryGetValue(atlas, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>($"Models/roofs/{atlas}");

            if (texture == null)
            {
                Debug.LogWarning($"[Env] roof atlas '{atlas}' is not in Resources/Models/roofs; " +
                                 "that building keeps the kit's mint roof.");
            }

            RoofCache[atlas] = texture;
            return texture;
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
