using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// ONE PAINTED FLAME FOR EVERY FIRE ON PAYO (2026-10-19). Owner, of the Payo night: "could also use some torch lights
    /// or those stone pedestals with fire in them"; the map session placed the pieces and left a `flame_anchor` on each,
    /// and the flame is this. One design covers the torches (slim), the stone pedestals (a wide basin, with its coals)
    /// and the fire bowls, at any size.
    ///
    /// `PayoFire.Attach(anchor, size, kind)` puts one on an anchor (in the editor's scene build or at run time). The
    /// scene keeps only this component and its two numbers: the drawn piece under it is made when it is enabled and is
    /// never saved, so nothing built here can go stale in a scene file.
    ///
    /// ⚠️ THERE WILL BE DOZENS ON SCREEN AND THE OWNER IS FIGHTING FRAME DROPS, SO:
    ///   * every fire draws ONE shared mesh (about a hundred triangles, made once) with ONE shared material, GPU
    ///     instanced: dozens of fires are one or a few instanced draws, never one draw each;
    ///   * all of its motion is in the shader (`TumbangPreso/PayoFire`); nothing is written per fire per frame, there
    ///     is no property block (it would break the instancing). What differs between fires (size, shape, flicker,
    ///     its turn at dusk) the shader reads off each instance's own transform and where it stands;
    ///   * once a frame, ONE call (`Tick`, from whichever fire updates first) writes two globals: the night
    ///     (`PayoNight.Factor`) and a clock that runs through the half-time break (the dusk happens in one, at
    ///     `Time.timeScale` 0). By day every fire's renderer is switched off, once, when the day starts: no draw at all.
    ///
    /// ⚠️ ITS TURN AT DUSK IS THE OLD BOWLS' RULE, IN THE SHADER: lit from `0.42 + 0.3 * order` over 0.12 of the night,
    /// with the same pop, `order` its bearing round the pit (`PayoNightLife.Flames`). So a fire's flame catches on the
    /// very frame its `PayoFlame` lamp does, and with the bowls beside it. The lamps stay the night life's.
    ///
    /// ⚠️ PRESENTATION ONLY: no collider, nothing sent, nothing gameplay reads. Every peer computes the same night from
    /// the match it already has, so every peer, a late joiner included, sees the same fires lit. Hidden in the menus'
    /// map preview.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PayoFire : MonoBehaviour
    {
        /// <summary>The shape of fire. Each is a width to height: a torch's bound head is slim, a basin is wide and shows its coals.</summary>
        public enum Kind { Torch, Pedestal, Bowl }

        /// <summary>The flame's height in metres. 0 or less takes it from the anchor's own scale (the Payo fire builder
        /// scales each `flame_anchor` to its flame's size).</summary>
        public float Size;
        public Kind Shape = Kind.Torch;

        /// <summary>A still may hold the night at a value (0 to 1) and the clock at a time; under 0 they follow the match.
        /// Set through `Pose`.</summary>
        public static float HoldNight = -1.0f, HoldClock = -1.0f;
        /// <summary>The valley's wind the flames lean down (the geyser's steam leans the same way) and how far the tip of a
        /// flame leans at full gust, as a share of its height.</summary>
        public static readonly Vector3 WindHeading = new Vector3(-0.7f, 0.0f, -0.55f);
        public const float WindLean = 0.16f;

        /// <summary>Width over height for each kind: a torch's flame is half as wide as it is tall, a basin about as wide.</summary>
        public static float WidthFor(Kind kind) => kind == Kind.Pedestal ? 0.95f : kind == Kind.Bowl ? 0.72f : 0.5f;
        /// <summary>The earliest night any fire is lit at (the shader's 0.42 for the first in turn).</summary>
        private const float FirstLight = 0.42f;

        private static readonly List<PayoFire> Live = new List<PayoFire>();
        private static Mesh _mesh;
        private static Material _material;
        private static int _tickFrame = -1;
        private static float _clock;
        private static bool _shown;
        private static readonly int NightId = Shader.PropertyToID("_PayoFireNight"), ClockId = Shader.PropertyToID("_PayoFireClock"), WindId = Shader.PropertyToID("_PayoFireWind");

        private MeshRenderer _renderer;
        private Transform _drawn;

        /// <summary>How many fires there are, and how many triangles one fire's mesh is (for a cost line).</summary>
        public static int Count => Live.Count;
        public static int TrianglesEach => _mesh != null ? (int)(_mesh.GetIndexCount(0) / 3) : 0;
        /// <summary>Whether the fires are being drawn now (night has come far enough for the first to catch).</summary>
        public static bool Shown => _shown;

        /// <summary>
        /// ⚠️ THE ONE ENTRY POINT. Puts a flame on `anchor` (its foot on the anchor, rising in the world's up whatever way
        /// the anchor is turned), `size` metres tall (0 or less: the anchor's own scale), shaped as `kind`. Calling it again
        /// on the same anchor changes that flame rather than adding a second. Works in the editor's scene build and at run
        /// time; returns the component, which is all the scene keeps.
        /// </summary>
        public static PayoFire Attach(Transform anchor, float size, Kind kind)
        {
            if (anchor == null) return null;
            var fire = anchor.GetComponent<PayoFire>();
            if (fire == null) fire = anchor.gameObject.AddComponent<PayoFire>();
            fire.Size = size;
            fire.Shape = kind;
            if (fire.isActiveAndEnabled) fire.Rebuild();
            return fire;
        }

        /// <summary>For a still or a film in the editor: hold every fire at `night` (0 to 1) and `clock` seconds, applied at
        /// once. `Pose(-1, -1)` lets them follow the match again.</summary>
        public static void Pose(float night, float clock)
        {
            HoldNight = night; HoldClock = clock;
            _tickFrame = -1;
            Tick();
        }

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
            Rebuild();
        }

        private void OnDisable()
        {
            Live.Remove(this);
            if (_drawn != null)
            {
                if (Application.isPlaying) Destroy(_drawn.gameObject); else DestroyImmediate(_drawn.gameObject);
            }
            _drawn = null; _renderer = null;
        }

        private void LateUpdate() => Tick();

        /// <summary>The drawn piece: a hidden child carrying the shared mesh and material, scaled so its world scale is the
        /// flame's width and height (the shader reads them back off the instance's matrix).</summary>
        private void Rebuild()
        {
            Ensure();
            if (_drawn == null)
            {
                var go = new GameObject("fire (made at run time)") { hideFlags = HideFlags.HideAndDontSave, layer = gameObject.layer };
                _drawn = go.transform;
                _drawn.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _mesh;
                _renderer = go.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = _material;
                _renderer.shadowCastingMode = ShadowCastingMode.Off; _renderer.receiveShadows = false;
                _renderer.lightProbeUsage = LightProbeUsage.Off; _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _renderer.allowOcclusionWhenDynamic = false;
            }
            float height = Size > 0.0f ? Size : Mathf.Max(0.05f, transform.lossyScale.y);
            float wide = height * WidthFor(Shape);
            Vector3 parent = transform.lossyScale;
            _drawn.localPosition = Vector3.zero;
            // Upright in the world whatever the anchor's turn, so the matrix's columns are exactly width and height.
            _drawn.rotation = Quaternion.identity;
            _drawn.localScale = new Vector3(wide / Safe(parent.x), height / Safe(parent.y), wide / Safe(parent.z));
            _renderer.enabled = _shown && !IsPreview();
        }

        private static float Safe(float v) => Mathf.Abs(v) < 1e-4f ? 1e-4f : v;

        /// <summary>Once a frame, for every fire: the night and the clock into the shader, and every renderer switched on
        /// or off when the night crosses the first fire's moment.</summary>
        public static void Tick()
        {
            if (_tickFrame == Time.frameCount) return;
            _tickFrame = Time.frameCount;
            float dt = PresentationClock.Held ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime;
            _clock = HoldClock >= 0.0f ? HoldClock : _clock + dt;
            // The clock is wrapped: the shader's sines want small numbers, and an hour of fire never shows the seam.
            if (_clock > 3600.0f) _clock -= 3600.0f;
            float night = HoldNight >= 0.0f ? Mathf.Clamp01(HoldNight) : (Application.isPlaying ? PayoNight.Factor : 0.0f);
            Shader.SetGlobalFloat(NightId, night);
            Shader.SetGlobalFloat(ClockId, _clock);
            Vector3 wind = WindHeading.normalized;
            Shader.SetGlobalVector(WindId, new Vector4(wind.x, wind.y, wind.z, WindLean));

            bool shown = night > FirstLight;
            if (shown == _shown) return;
            _shown = shown;
            foreach (var fire in Live)
                if (fire != null && fire._renderer != null) fire._renderer.enabled = shown && !fire.IsPreview();
        }

        // ------------------------------------------------------------------ made once for the process

        private static void Ensure()
        {
            if (_mesh == null) _mesh = BuildMesh();
            if (_material != null) return;
            var shader = Resources.Load<Shader>("Shaders/PayoFire");
            if (shader == null) shader = Shader.Find("TumbangPreso/PayoFire");
            if (shader == null) { Debug.LogWarning("[Payo] the shader TumbangPreso/PayoFire is missing: the night fires are not drawn"); shader = Shader.Find("Sprites/Default"); }
            _material = new Material(shader) { name = "Payo fire (shared)", hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
            _material.SetTexture("_NoiseA", Resources.Load<Texture2D>("Textures/UltimateFx/erode_streak"));
            _material.SetTexture("_NoiseB", Resources.Load<Texture2D>("Textures/UltimateFx/erode_cloud"));
            _material.SetTexture("_Cells", Resources.Load<Texture2D>("Textures/UltimateFx/noise_simplex_cells"));
            _material.renderQueue = 3050;
        }

        /// <summary>
        /// Every piece of one fire in one mesh, in drawing order (back to front inside one fire): the veil, the coal bed,
        /// three head cards, three short tongues, the body, three pinched blobs, five embers. Each corner carries its
        /// piece's constants; the shader does the rest. The positions only give the bounds a shape (the flame is a unit
        /// tall and a unit wide in this mesh's space), and the bounds are set generously for the lean, the embers and
        /// the far growth.
        /// </summary>
        private static Mesh BuildMesh()
        {
            var pos = new List<Vector3>(); var a = new List<Vector4>(); var b = new List<Vector4>(); var tris = new List<int>();

            void Strip(int piece, int index, int rings, Vector4 constants)
            {
                int n = pos.Count;
                for (int r = 0; r < rings; r++)
                {
                    float s = r / (float)(rings - 1);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        pos.Add(new Vector3(side * 0.3f, s, 0.0f));
                        a.Add(new Vector4(s, side, piece, index));
                        b.Add(constants);
                    }
                }
                for (int r = 0; r + 1 < rings; r++)
                {
                    int q = n + r * 2;
                    tris.Add(q); tris.Add(q + 1); tris.Add(q + 3);
                    tris.Add(q); tris.Add(q + 3); tris.Add(q + 2);
                }
            }

            void Card(int piece, int index, Vector4 constants)
            {
                int n = pos.Count;
                for (int c = 0; c < 4; c++)
                {
                    float u = c == 0 || c == 1 ? -1.0f : 1.0f, v = c == 0 || c == 3 ? -1.0f : 1.0f;
                    pos.Add(new Vector3(u * 0.5f, 0.5f + v * 0.5f, 0.0f));
                    a.Add(new Vector4(u, v, piece, index));
                    b.Add(constants);
                }
                tris.Add(n); tris.Add(n + 1); tris.Add(n + 2);
                tris.Add(n); tris.Add(n + 2); tris.Add(n + 3);
            }

            // ⚠️ The cycles are measured off nothing and are this design's own: a short tongue lives 0.6 to 0.9 s (a
            // flicker you can follow), a head card about 0.4 s (a churn), an ember 1.4 to 2.2 s (long enough to see it
            // curl). They are all different, so no two pieces ever line up.
            Strip(0, 0, 10, Vector4.zero);                                          // the veil
            Card(6, 0, Vector4.zero);                                               // the coal bed
            Card(4, 0, new Vector4(0.42f, 0.00f, 0, 0));                            // head cards
            Card(4, 1, new Vector4(0.37f, 0.33f, 0, 0));
            Card(4, 2, new Vector4(0.47f, 0.66f, 0, 0));
            Vector4[] shortOnes = { new Vector4(0.71f, 0.0f, 0.62f, 0), new Vector4(0.83f, 0.4f, 0.5f, 0), new Vector4(0.62f, 0.75f, 0.56f, 0) };
            for (int k = 0; k < 3; k++) Strip(2, k, 8, shortOnes[k]);                // short tongues
            Strip(1, 0, 10, Vector4.zero);                                          // the body
            for (int k = 0; k < 3; k++) Card(3, k, shortOnes[k]);                    // their pinched blobs (the same lives)
            float[] emberCycle = { 1.6f, 1.9f, 1.45f, 2.2f, 1.75f };
            for (int k = 0; k < 5; k++) Card(5, k, new Vector4(emberCycle[k], k * 0.19f, 0, 0));

            var mesh = new Mesh { name = "Payo fire (shared)", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(pos);
            mesh.SetUVs(0, a);
            mesh.SetUVs(1, b);
            mesh.SetTriangles(tris, 0, false);
            mesh.bounds = new Bounds(new Vector3(0.0f, 1.1f, 0.0f), new Vector3(5.0f, 4.0f, 5.0f));
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
