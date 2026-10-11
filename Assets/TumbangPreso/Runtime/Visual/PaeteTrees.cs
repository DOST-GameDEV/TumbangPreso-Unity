using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S TREES ARE MODELS NOW, NOT UNITY CUBES (owner, 2026-09-26: *"the current models of all
    /// his skills look ugly still its js blocks"*, *"thoroughly work on the detail of each part
    /// manually"*, then *"really caerfullly and delicately work on the animations + model of his
    /// trees"*). `tools/build_paete_props.py` types the sentry, the seedling and the thorn construct
    /// part by part in his own palette cells and writes them to `Resources/Models/PaeteProps`; this
    /// loads one, dresses it exactly as he is dressed (`ToonSkin.Apply` with his roster palette: the
    /// two-band toon and the ink outline), and hands back its named nodes for the bodies to pose.
    /// Direction: `docs/reports/paete-kit-2026-09-25/direction.md` section 5.
    /// </summary>
    public static class PaeteProp
    {
        public const string ResourceFolder = "Models/PaeteProps";
        private static Color[] _palette;

        /// <summary>His palette, from the roster (the same sixteen slots his body wears).</summary>
        public static Color[] Palette
        {
            get
            {
                if (_palette == null || _palette.Length != 16) _palette = RosterBook.Load()?.FindPersonArt("paete")?.Palette;
                return _palette;
            }
        }

        /// <summary>The prop named <paramref name="name"/> under <paramref name="parent"/>, dressed; null if it is missing.</summary>
        public static GameObject Spawn(string name, Transform parent) => Spawn(name, parent, null, ToonSkin.PersonOutlineWidth);

        /// <summary>As above, in <paramref name="palette"/> (his when null) with an outline <paramref name="width"/> wide.</summary>
        public static GameObject Spawn(string name, Transform parent, Color[] palette, float width)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null)
            {
                Debug.LogWarning("[PaeteProp] Models/PaeteProps/" + name + " is missing; run tools/build_paete_props.py.");
                return null;
            }
            var go = Object.Instantiate(source, parent, false);
            go.name = "PaeteProp-" + name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            // Disabled first: `Destroy` in play is deferred to the frame's end, and a caller may check now.
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) VfxRenderTag.Attach(r.gameObject);
            ToonSkin.Apply(go, width, palette ?? Palette);
            // Remember the scale each part's ink was sized at, so a later re-dress keeps it (`Redress`).
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is SkinnedMeshRenderer) continue;
                var rest = r.gameObject.AddComponent<PaeteOutlineRest>();
                rest.Scale = MaxAxis(r.transform.lossyScale); rest.Width = width;
            }
            return go;
        }

        private static float MaxAxis(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));

        /// <summary>The prop undressed: for a surface that brings its own material (Makiling's spirit).</summary>
        public static GameObject SpawnRaw(string name, Transform parent)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null) { Debug.LogWarning("[PaeteProp] Models/PaeteProps/" + name + " is missing."); return null; }
            var go = Object.Instantiate(source, parent, false);
            go.name = "PaeteProp-" + name;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) VfxRenderTag.Attach(r.gameObject);
            return go;
        }

        /// <summary>
        /// Re-dress with a different palette (the seedling drying), cached per palette by `ToonSkin`.
        /// ⚠️⚠️ FIXED 2026-09-27: THE INK WAS RE-SIZED AT WHATEVER SCALE EACH PART HAD AT THAT MOMENT. `ToonSkin.Apply` sizes a part's
        /// outline as `worldWidth / its current scale`, and BAKYA BLOOM re-dresses on its first pose, while the pitcher is still
        /// popping up out of the court at nearly zero scale (and later while its clog regrows from nothing): the outline came out up to
        /// ten thousand times too wide, and when the part grew to size its inverted hull was a building-sized dark shell over the court
        /// (the skills film, `Logs/paete-evidence-s2` and `-s3`, from the plant's landing on). A re-dress now keeps each part's outline
        /// at the width it was given at spawn, when every part stood at its rest scale (`PaeteOutlineRest`).
        /// </summary>
        public static void Redress(GameObject model, Color[] palette)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                // A piece built in code and hung on the prop (the carved clog in the pitcher: `PaeteInk.Part`) wears its own
                // colour, not a palette cell: re-dressing it with the prop's palette painted it black (2026-10-07 film).
                if (r.GetComponent<GrowthMeshOwner>() != null) continue;
                var rest = r.GetComponent<PaeteOutlineRest>();
                if (rest == null || rest.Scale <= 0.0001f) { ToonSkin.Apply(r, ToonSkin.PersonOutlineWidth, palette); continue; }
                // Scaled by now / rest, so `Apply`'s `width / now` lands on the spawn's `width / rest`.
                ToonSkin.Apply(r, rest.Width * Mathf.Max(0.0001f, MaxAxis(r.transform.lossyScale)) / rest.Scale, palette);
            }
        }

        public static Transform Find(GameObject model, string name)
        {
            if (model == null) return null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }

    // ⚠️ `PaeteForestTree` IS DELETED (2026-09-26 night, direction.md 5.13). It stood the sentry's model up as the
    // four small "forest" trees behind him in the introduction and as its payoff tree. The owner: *"i dont get what
    // the 3 plants showing up and the big plant showing up means"*, *"i js really dont liek taht u fucking show 3 small
    // plants that does not make snese"*. The forest is cut, and the payoff is now the live guardian itself
    // (`PaeteSentryBody` with `Staged` set), so what rises in the cutscene is exactly what rises in play.

    /// <summary>
    /// ⚠️⚠️ MARIANG MAKILING, WATCHING OVER HIM (owner, 2026-09-26: *"i want the lore for this character
    /// to be that its the guardian of mount makiling"*, *"make it seem like the spirit of maria makiling or
    /// smth is watching over him"*, Aphelios and Alune as the model, *"one place i want this maria makiling
    /// or smht to shhow up is his ult cutscene"*, *"its fine if u dont make maria makiling like other
    /// characters"*). direction.md sections 5.10 and 5.13. `Resources/Models/PaeteProps/makiling.glb`
    /// (`tools/build_paete_props.py` `makiling`), a tall calm woman in a baro't saya, in HER palette.
    ///
    /// ⚠️⚠️ A GHOST, NOT A FIGURE (owner, same day: *"make her look see thru so that it seems like a ghost"*, and
    /// on the v4 redirect *"makiling needs to be see thru tho okay? like a spirit thats js watching over"*). So,
    /// as Alune is drawn: ONE luminous hue (his jade light), the forms kept only by value, brighter at the edges,
    /// see-through in the middle, her hem dissolving into the mist, looming over his shoulder.
    /// `Resources/Shaders/SpiritGhost.shader` draws her (a depth pass so only her front shows, then a premultiplied
    /// blend so she veils and glows at once); the only solid thing on her is the light she carries.
    ///
    /// ⚠️ v3 (2026-09-26 night): she is posed through <see cref="Look"/>, one struct the cutscene fills per frame,
    /// because she now does more than rise and bow: she leans over him, reaches her arms over his and parts her
    /// hands to let the light fall, bends low with him at the connect, straightens to watch the guardian come up,
    /// and lets the mist take her back from the feet up. Her arms are their own nodes (`arm-left`, `arm-right`).
    /// The deer is gone (owner: *"why is there a deer even did i ask for that"*), and so is the v1 `hands` node.
    /// </summary>
    public sealed class MakilingSpirit
    {
        /// <summary>
        /// Her sixteen colours, the same slots as `tools/build_paete_props.py`'s MK_* table: 0 hair, 1 hair lit,
        /// 2 skin, 3 skin shade, 4 camisa, 5 camisa shade, 6 petal, 7 flower heart, 8 ink, 9 leaf, 10 light,
        /// 11 pañuelo, 12 saya, 13 saya fold, 14 tapis, 15 tapis trim. Through the ghost only their VALUE counts,
        /// so they are also her light and shade: the camisa a step darker than the pañuelo so the kerchief's
        /// shape reads over it, the tapis the one dark band that gives her a waist.
        /// </summary>
        public static readonly Color[] Palette =
        {
            // 2026-10-08, the diwata (`tools/build_paete_makiling.py`): 0 is her hair's own black, 5 her blush and 13 gold now.
            Hex(0x1B1A16), Hex(0x4A382E), Hex(0xC98E68), Hex(0xA8704F), Hex(0xF1ECE0), Hex(0xE7968A), Hex(0xFBF8EF), Hex(0xE8D8A0),
            Hex(0x1E140C), Hex(0x6A962E), Hex(0xD8FF6A), Hex(0xFFFDF6), Hex(0xEDE6D8), Hex(0xE2B84A), Hex(0x3F5F2C), Hex(0x8FB06A),
        };

        /// <summary>
        /// ⚠️ IN THE SKY (2026-10-08, owner: "she could fill up the sky or something like a god visible in the sky"). She was a
        /// figure of a person's size beside him; the cutscene now stands her far off and vast, sunk to her hips under the
        /// horizon. These say, in HER OWN metres above her feet, where the mist that hides her ends while she is there
        /// (`MistFrom` nothing, `MistTo` all of her), and the height below which she never takes her full form
        /// (`FormFloor`: "She turns solid only around her face and hands"). The defaults are the figure beside him.
        /// </summary>
        public float MistFrom = 0.12f, MistTo = 1.0f, FormFloor = -0.2f;

        /// <summary>One frame of her, filled by the cutscene. Every field has a neutral default of 0.</summary>
        public struct Look
        {
            /// <summary>0 to 1: risen out of the mist (0 is under it, unseen).</summary>
            public float Presence;
            /// <summary>Scene-space metres added to where she stands.</summary>
            public Vector3 Drift;
            /// <summary>Degrees she bends forward from her feet.</summary>
            public float Lean;
            /// <summary>Degrees her head bows (negative lifts it).</summary>
            public float Bow;
            /// <summary>Degrees her head turns (positive toward her own right).</summary>
            public float Turn;
            /// <summary>0 to 1: her arms raised forward from her breast, out over him.</summary>
            public float Reach;
            /// <summary>0 to 1: her hands parted (the light falls between them).</summary>
            public float Open;
            /// <summary>How much her hair and sleeves stir.</summary>
            public float Wind;
            /// <summary>0 to 1: the mist taking her back, from the feet up.</summary>
            public float Fade;
            /// <summary>0 to 1: the light held in her hands (and lighting her from within).</summary>
            public float Light;
            /// <summary>
            /// ⚠️ v4 (owner: *"i dont mind if u show hher briefly full form and she vanishes back (she sstarts translucent to full forma
            /// nd translucent again)"*): 0 to 1, how far her FULL FORM has swept up her from the feet (her own colours, lit, inked).
            /// </summary>
            public float Form;
            /// <summary>0 to 1: how far the spirit has come back up her after it, from the feet (1 is all ghost again).</summary>
            public float Unform;
            /// <summary>
            /// Her size as a share of the size she was made at; 0 means 1. ⚠️ In the sky nothing says how far away she is, so
            /// a figure growing IS a figure coming nearer: this is how she arrives (`HeroIntroductionScene.Paete.cs`).
            /// </summary>
            public float Size;
        }

        public readonly GameObject Model;
        private readonly Transform _head, _hair, _armLeft, _armRight, _seed;
        private readonly Quaternion _headRest, _hairRest, _armLeftRest, _armRightRest;
        private readonly Vector3 _seedScale, _at;
        private readonly Material _ghost;
        private readonly float _scale, _yaw;
        private static readonly int BaseYId = Shader.PropertyToID("_BaseY"), PresenceId = Shader.PropertyToID("_Presence"),
            FadeLowId = Shader.PropertyToID("_FadeLow"), FadeHighId = Shader.PropertyToID("_FadeHigh"),
            LightPosId = Shader.PropertyToID("_LightPos"), LightStrengthId = Shader.PropertyToID("_LightStrength"),
            LightRadiusId = Shader.PropertyToID("_LightRadius"), SolidFromId = Shader.PropertyToID("_SolidFrom"),
            SolidToId = Shader.PropertyToID("_SolidTo"), InkWidthId = Shader.PropertyToID("_InkWidth");
        /// <summary>Her height above her feet, crown included (the glb stands 3.0 m; the lines sweep a little past it).</summary>
        private const float FormHeight = 3.25f;

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>World point at the middle of her face (her halo stands behind it).</summary>
        public Vector3 HeadWorld => _head != null ? _head.TransformPoint(new Vector3(0f, 0.24f, 0f)) : HandsWorld;
        /// <summary>Her size on the last frame posed (made size times `Look.Size`).</summary>
        public float SizeNow { get; private set; }
        /// <summary>How brightly her edges shine as a ghost (the shader's own are 0.45 and 0.55), and the rim of light on her full form.</summary>
        public void Shine(float rimAlpha, float rimGlow, float formRim)
        {
            if (_ghost == null) return;
            _ghost.SetFloat("_RimAlpha", rimAlpha); _ghost.SetFloat("_Glow", rimGlow); _ghost.SetFloat("_FormRim", formRim);
        }

        /// <summary>World point between her hands: where the light is held, and where it falls from.</summary>
        public Vector3 HandsWorld => _seed != null ? _seed.position : (Model != null ? Model.transform.position + Vector3.up * 2f * _scale : Vector3.zero);

        public MakilingSpirit(Transform parent, Vector3 at, float yaw, float scale)
        {
            _at = at; _scale = scale; _yaw = yaw; SizeNow = scale;
            Model = PaeteProp.SpawnRaw("makiling", parent);
            if (Model == null) return;
            Model.transform.localPosition = at;
            Model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Model.transform.localScale = Vector3.one * scale;
            var shader = Resources.Load<Shader>("Shaders/SpiritGhost");
            if (shader != null)
            {
                _ghost = new Material(shader) { name = "MakilingSpirit" };
                var slots = new Vector4[16];
                for (int i = 0; i < 16; i++) slots[i] = Palette[i].linear;
                _ghost.SetVectorArray("_Palette", slots);
                _ghost.SetFloat(LightRadiusId, 0.95f * scale);
                // Her face's part of the atlas (`R_FACE` in `tools/build_paete_makiling.py`, with Unity's v): its dark pixels are her eyes and mouth.
                _ghost.SetVector("_FaceRect", new Vector4(0.25f, 0.75f, 0.5f, 0.875f));
                VfxRenderTag.Own(Model, _ghost);
            }
            else Debug.LogWarning("[MakilingSpirit] Shaders/SpiritGhost is missing.");
            // Her painted atlas (`makiling-atlas.png`, beside the model) rides on the importer's material: hand it to the ghost
            // before that material is replaced (`SpiritGhost.shader` samples it for a UV in the atlas's upper half).
            if (_ghost != null)
                foreach (var r in Model.GetComponentsInChildren<Renderer>(true))
                {
                    var from = r.sharedMaterial;
                    Texture paint = null;
                    if (from != null)
                        foreach (string name in new[] { "_BaseMap", "_MainTex", "baseColorTexture" })
                            if (paint == null && from.HasProperty(name)) paint = from.GetTexture(name);
                    if (paint == null) continue;
                    _ghost.SetTexture("_MainTex", paint);
                    break;
                }
            foreach (var r in Model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (_ghost != null)
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int k = 0; k < mats.Length; k++) mats[k] = _ghost;
                    r.sharedMaterials = mats;
                }
            }
            _head = PaeteProp.Find(Model, "head");
            _hair = PaeteProp.Find(Model, "hair");
            _armLeft = PaeteProp.Find(Model, "arm-left");
            _armRight = PaeteProp.Find(Model, "arm-right");
            _seed = PaeteProp.Find(Model, "seed");
            if (_head != null) _headRest = _head.localRotation;
            if (_hair != null) _hairRest = _hair.localRotation;
            if (_armLeft != null) _armLeftRest = _armLeft.localRotation;
            if (_armRight != null) _armRightRest = _armRight.localRotation;
            if (_seed != null) _seedScale = _seed.localScale;
        }

        /// <summary>Pose her at scene time <paramref name="t"/> (for her own sway) as <paramref name="look"/> says.</summary>
        public void Pose(float t, Look look)
        {
            if (Model == null) return;
            float rise = Mathf.SmoothStep(0f, 1f, look.Presence);
            bool seen = rise > 0.002f && look.Fade < 0.999f;
            if (Model.activeSelf != seen) Model.SetActive(seen);
            if (!seen) return;
            // Up out of the mist, floating: a slow bob, and she comes up from under the court as she arrives.
            float scale = _scale * (look.Size > 0f ? look.Size : 1f);
            SizeNow = scale;
            Model.transform.localScale = Vector3.one * scale;
            Model.transform.localPosition = _at + look.Drift + Vector3.up * (-1.3f * scale * (1f - rise) + 0.041f * scale * Mathf.Sin(t * 1.6f));
            Model.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(look.Lean, 0f, 0f);

            // The head: bowed toward him, turned, and a slow tilt as she watches.
            if (_head != null)
                _head.localRotation = _headRest * Quaternion.Euler(look.Bow, look.Turn, 4f * Mathf.Sin(t * 0.8f));
            // The arms: raised forward by Reach (negative x swings a hanging arm forward), parted by Open (her
            // left, on -x after the import's mirror, turns out with negative yaw; her right with positive). The
            // sleeves are on the arm nodes, so they swing with them; the wind lifts them a little.
            float lift = -58f * look.Reach + 2.5f * look.Wind * Mathf.Sin(t * 1.4f);
            float part = 30f * look.Open;
            if (_armLeft != null) _armLeft.localRotation = _armLeftRest * Quaternion.Euler(lift, -part, -6f * look.Open);
            if (_armRight != null) _armRight.localRotation = _armRightRest * Quaternion.Euler(lift + 1.5f * look.Wind * Mathf.Sin(t * 1.7f + 1f), part, 6f * look.Open);
            // Her long hair stirs, more when the ground is being called.
            float wind = 1f + 2.2f * look.Wind;
            if (_hair != null) _hair.localRotation = _hairRest * Quaternion.Euler(3f * wind * Mathf.Sin(t * 1.3f) - 4f * look.Wind, 0f, 2f * wind * Mathf.Sin(t * 1.1f + 1f));
            // The light she holds: it glows up in her cupped hands and is gone from them when she lets it fall.
            if (_seed != null)
            {
                float glow = Mathf.Clamp01(look.Light);
                _seed.gameObject.SetActive(glow > 0.01f);
                _seed.localScale = _seedScale * Mathf.Max(0.001f, glow * (1f + 0.14f * Mathf.Sin(t * 9f)));
            }
            if (_ghost != null)
            {
                _ghost.SetFloat(BaseYId, Model.transform.position.y);
                _ghost.SetFloat(PresenceId, rise);
                // Her hem is always mist; the Fade raises the mist line up her until it has her whole.
                float fade = Mathf.SmoothStep(0f, 1f, look.Fade);
                _ghost.SetFloat(FadeLowId, Mathf.Lerp(MistFrom, 3.2f, fade) * scale);
                _ghost.SetFloat(FadeHighId, Mathf.Lerp(MistTo, 3.45f, fade) * scale);
                _ghost.SetVector(LightPosId, HandsWorld);
                _ghost.SetFloat(LightStrengthId, 1.1f * Mathf.Clamp01(look.Light));
                _ghost.SetFloat(LightRadiusId, 0.95f * scale);
                // HER FULL FORM (v4): the solid band runs from `_SolidFrom` to `_SolidTo`, metres above her feet. Forming sweeps the top
                // line up her from below her feet; turning back sweeps the bottom line up after it, so she rises into her form and out.
                float top = FormHeight * scale;
                float floor = FormFloor > 0f ? FormFloor * scale : FormFloor;
                _ghost.SetFloat(SolidToId, Mathf.Lerp(floor, top, Mathf.SmoothStep(0f, 1f, look.Form)));
                _ghost.SetFloat(SolidFromId, Mathf.Lerp(floor, top, Mathf.SmoothStep(0f, 1f, look.Unform)));
                _ghost.SetFloat(InkWidthId, 0.013f * scale);
            }
        }
    }

    /// <summary>
    /// Inked parts for the pieces that are built every frame (the embrace limbs, the shin branches, the
    /// thorn lashes, the ground branches). They wear the same toon shader and ink as the modelled props
    /// so a limb growing out of the tree does not change material where it leaves the wood.
    ///
    /// ⚠️ ONE SOURCE MATERIAL PER COLOUR, SHARED. `ToonSkin` caches a variant per source material for
    /// the life of the process, so a fresh material per part (`VfxMaterial.Solid`'s way) would leak a
    /// cached variant on every cast. ⚠️ THE OUTLINE NEEDS ITS NORMAL IN THE TANGENT CHANNEL
    /// (`OutlineNormals`), and a mesh rebuilt every frame loses it, so `Finish` writes it after each build.
    /// </summary>
    public static class PaeteInk
    {
        private static readonly Dictionary<Color, Material> Sources = new Dictionary<Color, Material>();
        private static readonly List<Vector3> Normals = new List<Vector3>();
        private static readonly List<Vector4> Tangents = new List<Vector4>();
        private static readonly List<int> Tris = new List<int>();

        public static MeshFilter Part(Transform parent, string name, Mesh mesh, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!Sources.TryGetValue(colour, out var source) || source == null)
            {
                var template = MaterialKit.Lit;
                var shader = template != null ? template.shader : Shader.Find("Standard");
                source = new Material(shader) { name = "PaeteInkSource", hideFlags = HideFlags.DontSave };
                source.color = colour;
                if (source.HasProperty("_BaseColor")) source.SetColor("_BaseColor", colour);
                Sources[colour] = source;
            }
            renderer.sharedMaterial = source;
            VfxRenderTag.Attach(go);
            go.AddComponent<GrowthMeshOwner>().Mesh = mesh;
            ToonSkin.Apply(renderer, ToonSkin.PersonOutlineWidth, null);
            return filter;
        }

        /// <summary>After a rebuild: the smooth normal into the tangent channel, which is what the ink pushes along.</summary>
        public static void Finish(Mesh mesh)
        {
            mesh.GetNormals(Normals);
            Tangents.Clear();
            foreach (var n in Normals) Tangents.Add(new Vector4(n.x, n.y, n.z, 1f));
            mesh.SetTangents(Tangents);
        }

        /// <summary>
        /// ⚠️⚠️ `GrowthVfx.Tube` WINDS ITS TRIANGLES INSIDE OUT for Unity (its faces look inward), which
        /// never showed because `VfxMaterial.Solid` draws both sides. The toon shader culls back faces
        /// and its ink hull culls front faces, so on the first native film (PaeteReviewProbe v14) every
        /// limb, shin branch and ground branch drew as solid ink with a thin brown rim. The winding is
        /// flipped here, for these inked parts only, and the normals recomputed from it.
        /// </summary>
        public static void Tube(Mesh mesh, IList<Vector3> points, IList<float> radii, int sides)
        {
            if (points.Count < 2) { mesh.Clear(); return; }
            GrowthVfx.Tube(mesh, points, radii, sides);
            Flip(mesh);
        }

        /// <summary>`GrowthVfx.Leaf`, turned the right way out for the inked parts (v15 drew its leaves as ink).</summary>
        public static Mesh Leaf(float length, float width, float thickness)
        {
            var mesh = GrowthVfx.Leaf(length, width, thickness);
            Flip(mesh);
            return mesh;
        }

        private static void Flip(Mesh mesh)
        {
            mesh.GetTriangles(Tris, 0);
            for (int i = 0; i + 2 < Tris.Count; i += 3) { int k = Tris[i + 1]; Tris[i + 1] = Tris[i + 2]; Tris[i + 2] = k; }
            mesh.SetTriangles(Tris, 0);
            mesh.RecalculateNormals();
            Finish(mesh);
        }
    }

    /// <summary>
    /// A WOVEN BRANCH: two or three bark cords laid round a centreline, the sentry's own weave in
    /// miniature (owner: *"dont use vines use woven tree branches"*). The cords twist round each other
    /// at a fixed rate per metre, so a limb that grows keeps its weave still and only gets longer.
    /// </summary>
    /// <summary>The scale and ink width a prop part was dressed at when it spawned, read by `PaeteProp.Redress`.</summary>
    public sealed class PaeteOutlineRest : MonoBehaviour
    {
        public float Scale, Width;
    }

    public sealed class PaeteRope
    {
        private readonly Mesh[] _strands;
        private readonly float[] _girth;
        private readonly float _spread, _twist, _phase;
        private readonly List<Vector3> _pts = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();

        public PaeteRope(Transform parent, string name, Color[] colours, float[] girth, float spread, float twistPerMetre, float phase)
        {
            _strands = new Mesh[colours.Length];
            _girth = girth; _spread = spread; _twist = twistPerMetre; _phase = phase;
            for (int i = 0; i < colours.Length; i++)
            {
                _strands[i] = new Mesh { name = name };
                _strands[i].MarkDynamic();
                PaeteInk.Part(parent, name + "-" + i, _strands[i], colours[i]);
            }
        }

        public void Clear() { foreach (var m in _strands) m.Clear(); }

        /// <summary>Draw along <paramref name="centre"/> (local space); thick at the root, <paramref name="tipScale"/> of it at the tip.</summary>
        public void Draw(List<Vector3> centre, float tipScale)
        {
            int n = centre.Count;
            if (n < 2) { Clear(); return; }
            for (int s = 0; s < _strands.Length; s++)
            {
                _pts.Clear(); _radii.Clear();
                Vector3 side = Vector3.zero;
                float along = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector3 t = (centre[Mathf.Min(n - 1, i + 1)] - centre[Mathf.Max(0, i - 1)]);
                    if (t.sqrMagnitude < 1e-8f) t = Vector3.up;
                    t.Normalize();
                    side = i == 0 ? Vector3.Cross(t, Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right) : side - Vector3.Dot(side, t) * t;
                    if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(t, Vector3.right);
                    side.Normalize();
                    Vector3 up = Vector3.Cross(t, side);
                    if (i > 0) along += Vector3.Distance(centre[i], centre[i - 1]);
                    float u = i / (float)(n - 1);
                    float a = _phase + s * Mathf.PI * 2f / _strands.Length + along * _twist;
                    float taper = Mathf.Lerp(1f, tipScale, u);
                    _pts.Add(centre[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * _spread * taper);
                    _radii.Add(_girth[s] * taper);
                }
                PaeteInk.Tube(_strands[s], _pts, _radii, 5);
            }
        }
    }

    // ⚠️ `PaeteRootVein` IS DELETED (2026-09-26 night, direction.md 5.14). It raced three bark cords along the court with a
    // bright lime block at their head, and from the court and from his own screen that block was a seed rolling to the spot:
    // the owner, *"i also dont like that paete just throws seeds in his ult"*. `PaeteRootRidge` (`PaeteGroundCall.cs`) replaces
    // it: the roots go UNDER the court, which heaves and splits over them with the light inside the split, and nothing lit
    // leads them.

    /// <summary>
    /// Bark breaking where his own roots snap out of the court (`PaeteGroundCall`, when he walks out of the call). It was
    /// eight bark cubes on its own `Update`; it is `PaeteEmbraceFx.BarkShatter` now: inked chips of his bark and splinters of
    /// the pale wood under it, thrown as one `PaeteBits` handful. The prisoners' break-out no longer comes here: it has a
    /// burst of its own (`PaeteEmbraceFx.BreakOut`).
    /// </summary>
    public static class PaeteBarkShatter
    {
        public static void Spawn(Vector3 at, int count) => PaeteEmbraceFx.BarkShatter(at, count);
    }

    /// <summary>
    /// ⚠️⚠️ ROOTED'S BODY TELL: THE ROOTS ROUND A HELD PLAYER'S LEGS (direction.md section 5.8). Owner: *"characters
    /// should look tied to the tree"*, *"dont use vines use woven tree branches"*, then (2026-09-26) *"make it seem more
    /// apparent that the people tied to the tree are actually TIED bcz they look like theyre js standing"*. Attached to any
    /// body that gains Rooted, on every peer.
    ///
    /// ⚠️ 2026-10-07, THE LOOK IS `PaeteRootBands` (`PaeteEmbraceFx.cs`); THIS IS ONLY ITS LIFE ON A BODY. v2's four dark
    /// bands on circles ran inside the redesigned bodies' legs and stood out front and back as a brown mass; why, and what
    /// replaced them, is written on `PaeteRootBands` and `PaeteEmbraceFx.LegHalfWidth`. This reads the body (rooted,
    /// straining), plays the two sounds it always played, and says which way the hold ended:
    ///  * TORN FREE (the prisoner was straining within the last third of a second, which is the 7 s hold finishing; every
    ///    peer knows it, because the strain is on the wire): the bands SNAP (`PaeteEmbraceFx.BreakOut`);
    ///  * LET GO (the tree sleeping, a tag): they slacken and draw back into the court (`PaeteEmbraceFx.LetGo`).
    /// </summary>
    public sealed class PaeteRootCoil : MonoBehaviour
    {
        private CharacterMotor _body;
        private PaeteRootBands _bands;
        private float _age, _strainedAt = -9f;
        private bool _facingEstablished, _retiring;

        /// <summary>The film's stand-in for a held Interact (`PaeteAbilityFilm.Sentry`): nothing in play sets it.</summary>
        public bool Strain { get; set; }

        /// <summary>
        /// ⚠️ CAUGHT FACING OUT, BACK TO THE TRUNK (owner, 2026-09-27: *"make everyone get caught in opposite direction (they should
        /// face against the tree not towards) this is bcz i want them to be able to throw shit still"*). A Rooted player can still
        /// throw and cast, and a throw leaves along the body's facing; dragged in, a body was left facing the trunk, so its first view
        /// in third person was bark. On the first frame this peer sees them held, the body is turned to face straight away from
        /// <paramref name="treeCentre"/> and the held view reopens behind it, looking out at the court. Once: they may turn freely
        /// after. On each peer, like the coil; the player's own peer is the one whose turn sticks (their yaw is theirs).
        /// </summary>
        public static PaeteRootCoil Attach(CharacterMotor body, Vector3 treeCentre)
        {
            var coil = Attach(body);
            if (coil == null || coil._facingEstablished) return coil;
            var away = body.transform.position - treeCentre; away.y = 0f;
            if (away.sqrMagnitude > 1e-4f)
            {
                body.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
                CameraSystem.CameraRig.FaceHeldView(body);
            }
            coil._facingEstablished = true;
            return coil;
        }

        public static PaeteRootCoil Attach(CharacterMotor body)
        {
            if (body == null || !body.gameObject.activeInHierarchy) return null;
            var existing = body.GetComponentInChildren<PaeteRootCoil>();
            if (existing != null) return existing;
            var go = new GameObject("PaeteRootCoil");
            go.transform.SetParent(body.transform, false);
            var fx = go.AddComponent<PaeteRootCoil>();
            fx._body = body;
            fx._bands = new PaeteRootBands(go.transform);
            return fx;
        }

        public void Retire()
        {
            if (_retiring) return;
            _retiring = true;
            gameObject.SetActive(false);
            PaeteProp.Kill(gameObject);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && (_body == null || !_body.gameObject.activeInHierarchy)) Retire();
        }

        /// <summary>The break-out (direction.md section 5.6): the bands snapping and the snap's sound, on every peer.</summary>
        public static void Break(Vector3 feet) => Break(feet, Quaternion.identity, true);

        /// <summary>The hold ending at <paramref name="feet"/>: torn free (the burst), or let go (the roots drawing back).</summary>
        public static void Break(Vector3 feet, Quaternion facing, bool tornFree)
        {
            if (tornFree) PaeteEmbraceFx.BreakOut(feet, facing);
            else PaeteEmbraceFx.LetGo(feet, facing);
            GameServices.Audio?.PlayAtVaried("sfx_paete_root_break", feet, 0.95f, 1.05f, 0.8f);
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>One step of the roots (the film drives this outside Play).</summary>
        public void Step(float dt)
        {
            if (_retiring) return;
            if (_body == null || !_body.IsRooted)
            {
                // The roots letting go: the hold finished, a tag landed or the sentry slept. Local on
                // every peer off the replicated state, like the gain below.
                if (_body != null) Break(_body.transform.position, _body.transform.rotation, _age - _strainedAt < 0.35f);
                Retire(); return;
            }
            if (_age <= 0f)
            {
                GameServices.Audio?.PlayAtVaried("sfx_status_rooted", _body.transform.position, 0.95f, 1.05f, 0.8f);
                PaeteEmbraceFx.Bind(_body.transform.position, _body.transform.rotation);
            }
            _age += dt;
            bool fighting = _body.IsStruggling || Strain;
            if (fighting) _strainedAt = _age;
            _bands.Draw(_age, 0f, fighting);
        }
    }
}
