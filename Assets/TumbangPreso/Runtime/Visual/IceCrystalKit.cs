using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The sculpted and baked ice crystals (`tools/build_ice_crystals.py`, `Resources/Models/IceCrystals`,
    /// `Shaders/IceCrystal`) put together for the RUNNING game: a model, its three maps and the shader, by name.
    ///
    /// ⚠️ WHY IT EXISTS. Until this file the only thing that assembled a crystal was the editor's film
    /// (`Editor/MapKit/IceCrystalFilm.cs`), so a crystal could be looked at turning on a dark floor and nowhere else.
    /// This is that film's assembly, line for line, without the editor: the same `Resources` paths, the same three
    /// texture slots (`_MainTex`, `_BumpMap`, `_MaskTex`), the same key light handed to the shader as `_KeyDir`, and
    /// the crystal stood on its own foot whatever its origin is. If the film changes how it builds the material,
    /// change it here too, or the game and the film show two different crystals.
    ///
    /// ⚠️ EVERYTHING IS LOADED BY NAME AND NOTHING IS COPIED. The models and maps are still being reworked and are
    /// written again under the same file names, so this reads them fresh each play session and keeps no file of its
    /// own. A name that does not load (a model, a map or the shader) is reported ONCE and gives null: the caller
    /// draws without it.
    ///
    /// ⚠️ ONE MATERIAL A MODEL, SHARED. What differs between two crystals of one model (the clock the glint shimmers
    /// on, how strongly it is lit from inside, the frost its bright faces are pushed toward) is set per renderer
    /// through a property block (<see cref="Dress(Renderer[], float, float, Color?)"/>), so forty spikes are one
    /// material and one set of textures, and one crystal flashing does not flash the rest.
    ///
    /// ⚠️ LOOK ONLY. No collider survives <see cref="Spawn(string, Transform)"/>, nothing here reads or writes a
    /// rule, and every renderer wears <see cref="VfxRenderTag"/> so the ground queries and the prop checks treat it
    /// as an effect.
    /// </summary>
    public static class IceCrystalKit
    {
        public const string Spike = "ice_spike", Shard = "ice_shard", Stub = "ice_stub", Cluster = "ice_cluster";

        private const string Models = "Models/IceCrystals/";

        /// <summary>
        /// Toward the key light, in world space. The film's own value: high, to one side and a little in front. The
        /// crystals do not read the scene's lights (the shader has the reason), so this is what decides which faces
        /// go pale and which go deep, wherever the crystal stands.
        /// </summary>
        public static readonly Vector3 TowardKey = new Vector3(-0.62f, 0.60f, -0.22f).normalized;

        private static readonly int AgeId = Shader.PropertyToID("_Age"), LitId = Shader.PropertyToID("_Lit"),
            FrostId = Shader.PropertyToID("_LitColor"), ColourId = Shader.PropertyToID("_Color"),
            StrengthId = Shader.PropertyToID("_Strength");

        private sealed class Kind
        {
            public GameObject Prefab;
            public Material Look;
        }

        private static readonly Dictionary<string, Kind> Kinds = new Dictionary<string, Kind>();
        private static readonly Dictionary<Mesh, Mesh> WithTangents = new Dictionary<Mesh, Mesh>();
        private static readonly HashSet<string> Reported = new HashSet<string>();
        private static Material _halo;
        private static Mesh _card;
        private static MaterialPropertyBlock _block;

        /// <summary>
        /// Every play session starts with nothing remembered. The editor can enter Play without reloading scripts,
        /// and a material kept from the last session would still hold the maps and the shader as they were then,
        /// which is exactly what must not happen while they are being reworked.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget()
        {
            Kinds.Clear();
            WithTangents.Clear();
            Reported.Clear();
            _halo = null;
            _card = null;
            _block = null;
        }

        private static void Report(string key, string what)
        {
            if (Reported.Add(key)) Debug.LogWarning("[IceCrystalKit] " + what + " Nothing is drawn for it.");
        }

        private static Kind Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Kinds.TryGetValue(name, out var kind) && kind.Prefab != null && kind.Look != null) return kind;

            var shader = Resources.Load<Shader>("Shaders/IceCrystal");
            if (shader == null || !shader.isSupported)
            {
                Report("shader", "Shaders/IceCrystal is missing or did not compile.");
                return null;
            }
            var prefab = Resources.Load<GameObject>(Models + name);
            if (prefab == null)
            {
                Report(name, "No model at Resources/" + Models + name + ".");
                return null;
            }
            var paint = Resources.Load<Texture2D>(Models + name + "_albedo");
            var normal = Resources.Load<Texture2D>(Models + name + "_normal");
            var mask = Resources.Load<Texture2D>(Models + name + "_mask");
            if (paint == null || normal == null || mask == null)
            {
                Report(name, "A map of " + name + " is missing (it needs _albedo, _normal and _mask beside the model).");
                return null;
            }

            var look = new Material(shader) { name = "IceCrystal_" + name };
            look.SetTexture("_MainTex", paint);
            look.SetTexture("_BumpMap", normal);
            look.SetTexture("_MaskTex", mask);
            look.SetVector("_KeyDir", TowardKey);

            kind = new Kind { Prefab = prefab, Look = look };
            Kinds[name] = kind;
            return kind;
        }

        /// <summary>
        /// One crystal under <paramref name="parent"/>, or null if any part of it is missing.
        ///
        /// What comes back is a HOLDER whose origin is the crystal's foot and whose +Y runs up its length, so the
        /// caller can lean it, scale it along its length and stand it on the ground without knowing where the model's
        /// own origin is. The model is a child of it.
        /// </summary>
        public static GameObject Spawn(string name, Transform parent) => Spawn(name, parent, out _, out _);

        /// <summary>
        /// The same, and how big the model is as it was built: <paramref name="tall"/> along its length and
        /// <paramref name="wide"/> across, in metres at a scale of one. Measured from this very instance, so a
        /// model that is rebuilt at another size is followed without a number being typed anywhere.
        /// </summary>
        public static GameObject Spawn(string name, Transform parent, out float tall, out float wide)
        {
            tall = 0f;
            wide = 0f;
            var kind = Load(name);
            if (kind == null) return null;

            var holder = new GameObject("IceCrystal-" + name);
            holder.transform.SetParent(parent, false);
            var model = Object.Instantiate(kind.Prefab, holder.transform, false);
            model.name = name;
            model.transform.localPosition = Vector3.zero;

            // Look only. An importer that one day adds a collider must not put a solid spike in the road.
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }

            bool any = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var mesh = filter.sharedMesh;
                if (renderer == null || mesh == null) continue;

                // The normal map is laid in the mesh's tangents. A model that came in without them gets a copy that
                // has them (one copy a mesh, kept), as the film gives it; where the mesh cannot be read it is drawn
                // as it is and said so, because its bake will sit wrong.
                if (!mesh.HasVertexAttribute(VertexAttribute.Tangent))
                {
                    if (!WithTangents.TryGetValue(mesh, out var fixedMesh) || fixedMesh == null)
                    {
                        fixedMesh = null;
                        if (mesh.isReadable)
                        {
                            fixedMesh = Object.Instantiate(mesh);
                            fixedMesh.name = mesh.name + " (tangents)";
                            fixedMesh.RecalculateTangents();
                            WithTangents[mesh] = fixedMesh;
                        }
                        Report(name + "/tangents", name + " has no tangents" + (fixedMesh != null
                            ? "; they were worked out here, not by the exporter the bake was made against."
                            : " and its mesh cannot be read, so its normal map will sit wrong."));
                    }
                    if (fixedMesh != null)
                    {
                        mesh = fixedMesh;
                        filter.sharedMesh = fixedMesh;
                    }
                }

                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length <= 1) renderer.sharedMaterial = kind.Look;
                else
                {
                    for (int i = 0; i < slots.Length; i++) slots[i] = kind.Look;
                    renderer.sharedMaterials = slots;
                }
                // It is lit by its own key light and nothing else, so the street's shadows have no business on it.
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                VfxRenderTag.Attach(renderer.gameObject);

                // Measured in the holder's own space, so it does not matter where the parent stands or how it is
                // turned and scaled.
                Matrix4x4 toHolder = holder.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Vector3 c = mesh.bounds.center, e = mesh.bounds.extents;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = toHolder.MultiplyPoint3x4(c + new Vector3(
                        (corner & 1) == 0 ? -e.x : e.x, (corner & 2) == 0 ? -e.y : e.y, (corner & 4) == 0 ? -e.z : e.z));
                    if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
                    else bounds.Encapsulate(point);
                }
            }

            if (!any)
            {
                Report(name + "/mesh", name + " has no mesh to draw.");
                Object.Destroy(holder);
                return null;
            }

            // Stood on its foot, whatever its own origin is.
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            tall = bounds.size.y;
            wide = Mathf.Max(bounds.size.x, bounds.size.z);
            return holder;
        }

        /// <summary>The renderers of a crystal from <see cref="Spawn(string, Transform)"/>, to keep and hand to
        /// <see cref="Dress(Renderer[], float, float, Color?)"/> each frame instead of searching for them.</summary>
        public static Renderer[] Parts(GameObject crystal) =>
            crystal != null ? crystal.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();

        /// <summary>
        /// What is this crystal's own, written each frame by whoever owns it: its clock (the shader reads no time of
        /// its own, so the glint only shimmers if this moves), how strongly it is lit from inside (1 is normal; a
        /// crystal arriving or cracking is brighter for a moment), and, if given, the frost its bright faces are
        /// pushed toward, which is how one crystal is tinted without a second material.
        /// </summary>
        public static void Dress(Renderer[] parts, float age, float lit = 1f, Color? frost = null)
        {
            if (parts == null) return;
            _block ??= new MaterialPropertyBlock();
            foreach (var part in parts)
            {
                if (part == null) continue;
                part.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, age);
                _block.SetFloat(LitId, lit);
                if (frost.HasValue) _block.SetColor(FrostId, frost.Value);
                part.SetPropertyBlock(_block);
            }
        }

        /// <summary>
        /// One soft additive card (`Shaders/IceCrystalHalo`), a metre across at a scale of one, dark until
        /// <see cref="Glow"/> lights it. Nothing blooms in this game's cameras, so light that is meant to spill past
        /// a crystal is drawn as a card, the way the film places one at the tip and one lying at the foot.
        /// <paramref name="flat"/> lays it on the ground; otherwise the owner turns it to the lens each frame.
        /// Null if the shader is missing.
        /// </summary>
        public static Renderer Halo(Transform parent, string label, bool flat = false)
        {
            if (_halo == null)
            {
                var shader = Resources.Load<Shader>("Shaders/IceCrystalHalo");
                if (shader == null || !shader.isSupported)
                {
                    Report("halo", "Shaders/IceCrystalHalo is missing or did not compile.");
                    return null;
                }
                _halo = new Material(shader) { name = "IceCrystalHalo" };
            }
            if (_card == null)
            {
                _card = new Mesh { name = "IceHaloCard" };
                _card.vertices = new[]
                {
                    new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0),
                };
                _card.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                _card.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _card.RecalculateBounds();
            }

            var go = new GameObject("IceHalo-" + label);
            go.transform.SetParent(parent, false);
            if (flat) go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = _card;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _halo;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            VfxRenderTag.Attach(go);
            Glow(renderer, Color.black, 0f);
            return renderer;
        }

        /// <summary>A halo card's colour and strength. A card at no strength is not drawn at all.</summary>
        public static void Glow(Renderer halo, Color colour, float strength)
        {
            if (halo == null) return;
            bool lit = strength > 0.004f;
            if (halo.enabled != lit) halo.enabled = lit;
            if (!lit) return;
            _block ??= new MaterialPropertyBlock();
            halo.GetPropertyBlock(_block);
            _block.SetColor(ColourId, colour);
            _block.SetFloat(StrengthId, strength);
            halo.SetPropertyBlock(_block);
        }
    }
}
