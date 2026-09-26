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
            var source = Resources.Load<GameObject>("Models/PaeteProps/" + name);
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
            return go;
        }

        /// <summary>The prop undressed: for a surface that brings its own material (Makiling's spirit).</summary>
        public static GameObject SpawnRaw(string name, Transform parent)
        {
            var source = Resources.Load<GameObject>("Models/PaeteProps/" + name);
            if (source == null) { Debug.LogWarning("[PaeteProp] Models/PaeteProps/" + name + " is missing."); return null; }
            var go = Object.Instantiate(source, parent, false);
            go.name = "PaeteProp-" + name;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) VfxRenderTag.Attach(r.gameObject);
            return go;
        }

        /// <summary>Re-dress with a different palette (the seedling drying), cached per palette by `ToonSkin`.</summary>
        public static void Redress(GameObject model, Color[] palette) => ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

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
            Hex(0x231A17), Hex(0x4A382E), Hex(0xC98E68), Hex(0xA8704F), Hex(0xE6DDCC), Hex(0xC9BEA9), Hex(0xFBF8EF), Hex(0xE8D8A0),
            Hex(0x1E140C), Hex(0x6A962E), Hex(0xD8FF6A), Hex(0xFFFDF6), Hex(0xEDE6D8), Hex(0xCFC4AE), Hex(0x3F5F2C), Hex(0x8FB06A),
        };

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

        /// <summary>World point between her hands: where the light is held, and where it falls from.</summary>
        public Vector3 HandsWorld => _seed != null ? _seed.position : (Model != null ? Model.transform.position + Vector3.up * 2f * _scale : Vector3.zero);

        public MakilingSpirit(Transform parent, Vector3 at, float yaw, float scale)
        {
            _at = at; _scale = scale; _yaw = yaw;
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
                VfxRenderTag.Own(Model, _ghost);
            }
            else Debug.LogWarning("[MakilingSpirit] Shaders/SpiritGhost is missing.");
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
            Model.transform.localPosition = _at + look.Drift + Vector3.up * (-1.3f * _scale * (1f - rise) + 0.045f * Mathf.Sin(t * 1.6f));
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
                _ghost.SetFloat(FadeLowId, Mathf.Lerp(0.12f, 3.2f, fade) * _scale);
                _ghost.SetFloat(FadeHighId, Mathf.Lerp(1.0f, 3.45f, fade) * _scale);
                _ghost.SetVector(LightPosId, HandsWorld);
                _ghost.SetFloat(LightStrengthId, 1.1f * Mathf.Clamp01(look.Light));
                // HER FULL FORM (v4): the solid band runs from `_SolidFrom` to `_SolidTo`, metres above her feet. Forming sweeps the top
                // line up her from below her feet; turning back sweeps the bottom line up after it, so she rises into her form and out.
                float top = FormHeight * _scale;
                _ghost.SetFloat(SolidToId, Mathf.Lerp(-0.2f, top, Mathf.SmoothStep(0f, 1f, look.Form)));
                _ghost.SetFloat(SolidFromId, Mathf.Lerp(-0.2f, top, Mathf.SmoothStep(0f, 1f, look.Unform)));
                _ghost.SetFloat(InkWidthId, 0.013f * _scale);
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
    /// Bark breaking: chunks thrown out from a point that fall, bounce once and shrink away. The
    /// break-out (direction.md section 5.6): the shin branches and the waist band cracking apart. Each
    /// chunk's throw is typed.
    /// </summary>
    public sealed class PaeteBarkShatter : MonoBehaviour
    {
        private static readonly float[] Yaw = { 12f, 64f, 118f, 161f, 205f, 250f, 296f, 338f };
        private static readonly float[] Out = { 2.1f, 1.6f, 2.5f, 1.8f, 2.3f, 1.5f, 2.0f, 2.6f };
        private static readonly float[] Up = { 2.6f, 3.2f, 2.2f, 3.0f, 2.4f, 3.4f, 2.8f, 2.0f };
        private static readonly float[] Size = { 0.09f, 0.07f, 0.11f, 0.06f, 0.10f, 0.08f, 0.07f, 0.09f };
        private readonly List<Transform> _bits = new List<Transform>();
        private readonly List<Vector3> _v = new List<Vector3>();
        private float _age;
        private const float Life = 0.9f;

        public static void Spawn(Vector3 at, int count)
        {
            if (GrowthVfx.Reduced) count = Mathf.Max(3, count / 2);
            var go = new GameObject("PaeteBarkShatter");
            go.transform.position = at;
            var fx = go.AddComponent<PaeteBarkShatter>();
            for (int i = 0; i < Mathf.Min(count, Yaw.Length); i++)
            {
                Color c = i % 3 == 0 ? GrowthVfx.BarkDark : i % 3 == 1 ? GrowthVfx.Bark : GrowthVfx.BarkLit;
                var bit = GrowthVfx.Block(go.transform, "bark-chunk", new Vector3(Size[i], Size[i] * 0.55f, Size[i] * 1.6f), c).transform;
                bit.localRotation = Quaternion.Euler(20f * i, Yaw[i], 0f);
                float y = Yaw[i] * Mathf.Deg2Rad;
                fx._bits.Add(bit);
                fx._v.Add(new Vector3(Mathf.Sin(y) * Out[i], Up[i], Mathf.Cos(y) * Out[i]));
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_age >= Life) { Destroy(gameObject); return; }
            for (int i = 0; i < _bits.Count; i++)
            {
                var v = _v[i];
                v.y -= 14f * dt;
                var p = _bits[i].localPosition + v * dt;
                // One bounce off the road, then it skids.
                if (p.y < -transform.position.y + Slipper.GroundY(transform.position) + 0.02f && v.y < 0f) { v.y *= -0.3f; v.x *= 0.5f; v.z *= 0.5f; }
                _v[i] = v;
                _bits[i].localPosition = p;
                _bits[i].Rotate(new Vector3(500f + 40f * i, 200f - 30f * i, 0f) * dt, Space.Self);
                _bits[i].localScale = new Vector3(Size[i], Size[i] * 0.55f, Size[i] * 1.6f) * Mathf.Clamp01((Life - _age) / 0.35f);
            }
        }
    }

    /// <summary>
    /// ⚠️⚠️ ROOTED'S BODY TELL, AS WOVEN ROOT-BRANCHES (direction.md section 5.8). Owner: *"characters
    /// should look tied to the tree"*, *"dont use vines use woven tree branches"*. Three bark branches
    /// climb the shins out of the road, each on its own typed path round the legs (not one shape turned
    /// three times), a knot where they meet at the knee and a leaf at one tip. They grow up over 0.6 s,
    /// creak and shake when the player struggles, and on release they CRACK: bark chunks, a few leaves
    /// and `sfx_paete_root_break`. Attached to any body that gains Rooted, on every peer.
    /// </summary>
    public sealed class PaeteRootCoil : MonoBehaviour
    {
        // Each branch: keys of (angle round the legs in degrees, height, distance out, girth).
        // ⚠️⚠️ v2, TIED, NOT DECORATED (owner, 2026-09-26: *"make it seem more apparent that the people tied to the
        // tree are actually TIED bcz they look like theyre js standing"*). v1 was three branches 2 to 7 cm thick
        // spiralling loosely 30 to 46 cm out round the shins only, up to 0.58 m: from any distance they vanished.
        // Now four thick bands (6 to 11 cm) come up out of the road and wind TIGHT round the legs (24 to 29 cm out,
        // on the trousers) all the way to the hips (0.98 m), crossing each other, so the legs read as lashed
        // together; the embrace limb from the trunk wraps the waist above them. Each band typed on its own path.
        private static readonly Vector4[][] Branches =
        {
            new[] { new Vector4(-20f, -0.06f, 0.40f, 0.110f), new Vector4(30f, 0.08f, 0.29f, 0.100f), new Vector4(110f, 0.20f, 0.26f, 0.094f),
                    new Vector4(200f, 0.33f, 0.25f, 0.088f), new Vector4(290f, 0.48f, 0.26f, 0.080f), new Vector4(372f, 0.62f, 0.27f, 0.072f),
                    new Vector4(450f, 0.78f, 0.28f, 0.062f), new Vector4(505f, 0.90f, 0.29f, 0.040f) },
            new[] { new Vector4(110f, -0.06f, 0.42f, 0.104f), new Vector4(160f, 0.06f, 0.30f, 0.096f), new Vector4(230f, 0.16f, 0.26f, 0.090f),
                    new Vector4(310f, 0.28f, 0.25f, 0.084f), new Vector4(395f, 0.42f, 0.26f, 0.078f), new Vector4(470f, 0.56f, 0.27f, 0.070f),
                    new Vector4(540f, 0.72f, 0.28f, 0.058f), new Vector4(590f, 0.84f, 0.29f, 0.036f) },
            new[] { new Vector4(232f, -0.06f, 0.41f, 0.106f), new Vector4(275f, 0.10f, 0.29f, 0.098f), new Vector4(200f, 0.24f, 0.26f, 0.090f),
                    new Vector4(120f, 0.38f, 0.25f, 0.084f), new Vector4(40f, 0.52f, 0.26f, 0.076f), new Vector4(-40f, 0.68f, 0.27f, 0.066f),
                    new Vector4(-115f, 0.84f, 0.28f, 0.054f), new Vector4(-160f, 0.96f, 0.29f, 0.034f) },
            new[] { new Vector4(350f, -0.06f, 0.43f, 0.096f), new Vector4(300f, 0.12f, 0.29f, 0.090f), new Vector4(225f, 0.30f, 0.25f, 0.084f),
                    new Vector4(150f, 0.46f, 0.25f, 0.078f), new Vector4(75f, 0.62f, 0.26f, 0.070f), new Vector4(0f, 0.78f, 0.27f, 0.060f),
                    new Vector4(-70f, 0.92f, 0.28f, 0.048f), new Vector4(-110f, 0.98f, 0.29f, 0.030f) },
        };
        private static readonly Color[] Shade = { PaeteSentryBody.BarkDark, PaeteSentryBody.Bark, PaeteSentryBody.BarkLit, PaeteSentryBody.Bark };

        private CharacterMotor _body;
        private readonly Mesh[] _meshes = new Mesh[4];
        private Transform _knot, _leaf;
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private float _age;

        public static void Attach(CharacterMotor body)
        {
            if (body == null || body.GetComponentInChildren<PaeteRootCoil>() != null) return;
            var go = new GameObject("PaeteRootCoil");
            go.transform.SetParent(body.transform, false);
            var fx = go.AddComponent<PaeteRootCoil>();
            fx._body = body;
            for (int i = 0; i < fx._meshes.Length; i++)
            {
                fx._meshes[i] = new Mesh { name = "PaeteShinBranch" };
                fx._meshes[i].MarkDynamic();
                PaeteInk.Part(go.transform, "shin-branch-" + i, fx._meshes[i], Shade[i]);
            }
            var knotMesh = new Mesh { name = "PaeteShinKnot" };
            PaeteInk.Tube(knotMesh, new List<Vector3> { new Vector3(-0.05f, 0f, 0f), new Vector3(0.05f, 0.01f, 0f) }, new List<float> { 0.10f, 0.09f }, 5);
            fx._knot = PaeteInk.Part(go.transform, "shin-knot", knotMesh, PaeteSentryBody.BarkDark).transform;
            fx._leaf = PaeteInk.Part(go.transform, "shin-leaf", PaeteInk.Leaf(0.20f, 0.11f, 0.016f), PaeteSentryBody.Leaf).transform;
        }

        /// <summary>The break-out (direction.md section 5.6): chunks, leaves and the snap, on every peer.</summary>
        public static void Break(Vector3 feet)
        {
            PaeteBarkShatter.Spawn(feet + Vector3.up * 0.35f, 8);
            PaeteLeafBurst.Spawn(feet + Vector3.up * 0.45f, 4, 1.3f);
            GameServices.Audio?.PlayAtVaried("sfx_paete_root_break", feet, 0.95f, 1.05f, 0.8f);
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>One step of the growth (the review probe drives this in edit mode).</summary>
        public void Step(float dt)
        {
            if (_body == null || !_body.IsRooted)
            {
                // The roots letting go: the hold finished, a tag landed or the sentry slept. Local on
                // every peer off the replicated state, like the gain below.
                if (_body != null) Break(_body.transform.position);
                PaeteProp.Kill(gameObject); return;
            }
            if (_age <= 0f) GameServices.Audio?.PlayAtVaried("sfx_status_rooted", _body.transform.position, 0.95f, 1.05f, 0.8f);
            _age += dt;
            float grow = GrowthVfx.Pop(_age / 0.6f);
            bool fighting = _body.IsStruggling;
            // ⚠️ ALIVE ALL THE TIME (owner: *"animate taht shit"*): the bands SQUEEZE in a slow breath, tightening
            // a few centimetres on the legs, and when the player fights they judder and strain against them.
            float squeeze = 1f - 0.05f * (0.5f + 0.5f * Mathf.Sin(_age * 2.4f));
            for (int b = 0; b < Branches.Length; b++)
            {
                var keys = Branches[b];
                _points.Clear(); _radii.Clear();
                // The typed keys, eased between: four samples a span, growing up from the road.
                int shown = Mathf.Clamp(Mathf.CeilToInt(grow * (keys.Length - 1) * 4f), 1, (keys.Length - 1) * 4);
                for (int s = 0; s <= shown; s++)
                {
                    float f = s / 4f;
                    int k = Mathf.Min(keys.Length - 2, Mathf.FloorToInt(f));
                    float t = f - k;
                    Vector4 a = Vector4.Lerp(keys[k], keys[k + 1], t);
                    float ang = a.x * Mathf.Deg2Rad;
                    float r = a.y > 0.05f ? a.z * squeeze : a.z;
                    float shake = fighting ? Mathf.Sin(_age * 34f + b * 2f + s) * 0.035f * Mathf.Clamp01(a.y + 0.2f) : 0f;
                    _points.Add(new Vector3(Mathf.Sin(ang) * r + shake, a.y + (fighting ? Mathf.Sin(_age * 27f + b) * 0.01f : 0f), Mathf.Cos(ang) * r));
                    _radii.Add(a.w * (fighting ? 1.08f : 1f));
                }
                PaeteInk.Tube(_meshes[b], _points, _radii, 6);
            }
            // The knot at the knee where two meet, and a leaf on the tallest tip.
            _knot.localPosition = new Vector3(0.02f, 0.62f, 0.27f * squeeze);
            _knot.localScale = Vector3.one * Mathf.Clamp01((grow - 0.7f) * 3.3f);
            var tip = Branches[2][7];
            float ta = tip.x * Mathf.Deg2Rad;
            _leaf.localPosition = new Vector3(Mathf.Sin(ta) * tip.z, tip.y + 0.04f, Mathf.Cos(ta) * tip.z);
            _leaf.localRotation = Quaternion.Euler(-35f, tip.x, 0f);
            _leaf.localScale = Vector3.one * Mathf.Clamp01((grow - 0.85f) * 6f);
        }
    }
}
