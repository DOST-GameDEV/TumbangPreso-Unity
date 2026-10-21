using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY J, THE CROSSCURRENT (a first rough sample). Ilyas aims a narrow current along the ground: after 0.18 s
    /// (`RafiRules.CurrentGather`) it runs straight out at 8 m/s for 6 m (`CurrentRange`), and the first flying slipper
    /// it meets is bent 40 degrees (`CurrentTurnDegrees`), which spends it; solid cover spends it too. What meets a
    /// slipper is a column round the current's head, 0.65 m in radius (`CurrentRadius`) and 1.4 m tall
    /// (`CurrentHalfHeight` either side of 0.85 m). It moves no player. So the picture is ONE ROLLING HEAD OF WATER that
    /// size, at that height, travelling at that speed, with a thin wake behind it that is plainly not the part that
    /// hits, and it bursts where it is spent or where its 6 m end.
    ///
    /// It speaks the wall's language (`FxStudyRafiWall`, whose body the owner chose): flowing streaks (`water_flow`) on
    /// two see-through sheets running at different speeds, thin fast white lines (`water_lines`), and foam
    /// (`water_foam`) as the bright lip at its front. All of it on the bench's own ribbon shader.
    ///
    /// ⚠️ BUILT TO BE CHEAP, BY CONSTRUCTION, because a game effect has to be (the owner, of a cloud that rebuilt its mesh
    /// every frame: "remember this needs to be heavily optimized"). Every mesh here is built ONCE and shared by every
    /// cast; every material is built once per light setting and shared by every cast; nothing is allocated or rebuilt
    /// while it plays. Per frame a cast moves a few transforms and hands each renderer its clock, its opacity and its
    /// erosion in a property block, and the shader does the rest (the streaks run, the lines run, the cells are eaten).
    /// There are no drops: a drop would be a mesh rebuilt each frame. In a match the spray of its burst is meant to be
    /// the pooled cloud puffs (`CloudPuffs.Spawn`) in water colours, which cost nothing here.
    ///
    /// The motion is our own design; there is no reference for it beyond the wall.
    /// </summary>
    public sealed class FxStudyRafiCurrent : FxStudyEffect
    {
        public const float Radius = Core.RafiRules.CurrentRadius, Height = 0.85f, Gather = Core.RafiRules.CurrentGather,
                           Range = Core.RafiRules.CurrentRange, Speed = 8f;
        /// <summary>How long it lives untouched (the field's own duration), and how long its burst lasts after it is spent.</summary>
        public const float Life = Gather + Range / Speed, Burst = 0.24f;

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"),
                                    ErodeId = Shader.PropertyToID("_Erode"), TileId = Shader.PropertyToID("_Tile"), ErodeTileId = Shader.PropertyToID("_ErodeTile");

        // ---- built once, shared by every cast.
        private static Mesh _headMesh, _coreMesh, _bowMesh, _wakeMesh;
        private sealed class Kit { public Material Head, Core, Lines, Bow, Wake; }
        private static readonly Dictionary<int, Kit> Kits = new Dictionary<int, Kit>();

        // One drawn part of one cast: its renderer, where it hangs, its own block, and its own slide on the picture.
        private sealed class Part
        {
            public MeshRenderer Renderer; public Transform At; public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock(); public Vector4 Tile;
        }
        private readonly Part _head, _core, _lines, _bow, _wake;
        private readonly Transform _front, _spin, _trail;
        private readonly float _benchSpentAt;

        public override float Seconds => 0.3f + (_benchSpentAt > 0f ? _benchSpentAt : Life) + Burst + 0.25f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            focus = new Vector3(0f, 0.85f, 3.0f);
            eye = focus + new Vector3(6.2f, 2.6f, -3.8f);
            fov = 40f;
        }

        public FxStudyRafiCurrent(Transform parent) : this(parent, FxStudyRafiWall.Look.Bench, -1f, 0f) { }

        /// <summary>`benchSpentAt` above 0 makes the bench's current spent at that age (as a bent slipper does); `seed`
        /// slides each cast's pictures so two currents are not the same current.</summary>
        public FxStudyRafiCurrent(Transform parent, FxStudyRafiWall.Look look, float benchSpentAt, float seed) : base("rafi-current", parent)
        {
            _benchSpentAt = benchSpentAt;
            BuildMeshes();
            var kit = KitFor(look);
            _front = Group("front");
            _spin = new GameObject("spin") { hideFlags = HideFlags.DontSave }.transform;
            _spin.SetParent(_front, false);
            _trail = Group("trail");
            // The shells are turned on the lathe about the upright and laid down along the way it travels.
            _head = Make("head", _headMesh, kit.Head, _spin, new Vector4(1.0f, 1f, seed, seed * 0.7f));
            _core = Make("core", _coreMesh, kit.Core, _spin, new Vector4(0.8f, 0.8f, seed + 0.37f, seed * 0.3f));
            _lines = Make("lines", _headMesh, kit.Lines, _spin, new Vector4(1.0f, 1f, seed + 0.21f, seed * 0.5f));
            _lines.At.localScale = Vector3.one * 1.02f;
            _bow = Make("bow", _bowMesh, kit.Bow, _spin, new Vector4(6f, -0.94f, seed, 0.97f));
            _wake = Make("wake", _wakeMesh, kit.Wake, _trail, new Vector4(2.0f, 2f, seed + 0.5f, 0f));
        }

        private Part Make(string name, Mesh mesh, Material material, Transform parent, Vector4 tile)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = material != null;
            return new Part { Renderer = renderer, At = go.transform, Tile = tile };
        }

        private static void BuildMeshes()
        {
            if (_headMesh != null) return;
            // The head, from its tail (u 0) to its nose (u 1), as (radius, metres along the way it travels from its front).
            // 1.24 m across at its widest: the column that meets a slipper is 1.3 m.
            _headMesh = FxStudyKit.Shell(new[] { new Vector2(0.16f, -1.00f), new Vector2(0.38f, -0.55f), new Vector2(0.57f, -0.10f), new Vector2(0.62f, 0.20f), new Vector2(0.48f, 0.47f), new Vector2(0.08f, 0.62f) }, 0f, 360f, 220f, 26, 28);
            _coreMesh = FxStudyKit.Shell(new[] { new Vector2(0.10f, -0.80f), new Vector2(0.26f, -0.40f), new Vector2(0.40f, 0.00f), new Vector2(0.40f, 0.25f), new Vector2(0.06f, 0.48f) }, 40f, 360f, -160f, 20, 20);
            // The bright lip round its front, a collar a little behind its nose: at the very front it was a ring round a hole.
            _bowMesh = FxStudyKit.Shell(new[] { new Vector2(0.58f, -0.12f), new Vector2(0.64f, 0.06f), new Vector2(0.60f, 0.24f) }, 0f, 360f, 30f, 6, 36);
            // The wake: thin, long, and plainly smaller than what hits. Its tip is at the head's tail.
            _wakeMesh = FxStudyKit.Shell(new[] { new Vector2(0.02f, -3.00f), new Vector2(0.12f, -1.80f), new Vector2(0.24f, -0.70f), new Vector2(0.30f, 0.00f) }, 0f, 360f, 300f, 20, 12);
            foreach (var mesh in new[] { _headMesh, _coreMesh, _bowMesh, _wakeMesh }) { mesh.hideFlags = HideFlags.HideAndDontSave; mesh.UploadMeshData(true); }
        }

        private static Material Set(Material m, string name, float x, float y = 0f, float z = 0f, float w = 0f)
        {
            if (m == null) return null;
            if (m.HasProperty(name))
            {
                if (name == "_Opacity" || name == "_Erode" || name == "_Age" || name == "_Add" || name == "_Halo" || name == "_Emit" || name == "_Soft" || name == "_Sweep" || name == "_Swap" || name == "_Curve") m.SetFloat(name, x);
                else m.SetVector(name, new Vector4(x, y, z, w));
            }
            return m;
        }
        private static Material Colours(Material m, Color deep, Color body, Color hot, Color halo)
        {
            if (m == null) return null;
            m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo);
            return m;
        }

        private static Kit KitFor(FxStudyRafiWall.Look look)
        {
            int key = Mathf.RoundToInt(look.Light * 100f);
            if (Kits.TryGetValue(key, out var kit) && kit.Head != null) return kit;
            float light = look.Light;
            kit = new Kit();
            // The body: the wall's streaks, running back along it from the nose and round it as it rolls.
            kit.Head = Colours(FxStudyKit.Ribbon("water_flow", "water_flow"), look.Deep, look.Body, look.Pale, look.Body);
            Set(kit.Head, "_Scroll", -1.6f, 0.6f); Set(kit.Head, "_ErodeScroll", -1.6f, 0.6f); Set(kit.Head, "_Cut", 0.05f, 0.70f); Set(kit.Head, "_Stops", 0.30f, 0.80f, 1.0f);
            Set(kit.Head, "_Ends", 0.30f, 0f, 0f); Set(kit.Head, "_Soft", 0.20f); Set(kit.Head, "_Halo", 0f); Set(kit.Head, "_Emit", 0.2f); Set(kit.Head, "_Add", light); Set(kit.Head, "_Hue", look.HueDrift, 5f, 0.2f, 1f);
            // The second sheet inside, deeper and slower, turning the other way.
            kit.Core = Colours(FxStudyKit.Ribbon("water_flow", "water_flow"), look.Deep, Color.Lerp(look.Deep, look.Body, 0.6f), look.Body, look.Deep);
            Set(kit.Core, "_Scroll", -0.9f, -0.4f); Set(kit.Core, "_ErodeScroll", -0.9f, -0.4f); Set(kit.Core, "_Cut", 0.05f, 0.75f); Set(kit.Core, "_Stops", 0.30f, 0.85f, 1.0f);
            Set(kit.Core, "_Ends", 0.30f, 0f, 0f); Set(kit.Core, "_Soft", 0.20f); Set(kit.Core, "_Halo", 0f); Set(kit.Core, "_Emit", 0f); Set(kit.Core, "_Add", light * 0.5f); Set(kit.Core, "_Hue", look.HueDrift, 4f, 0.9f, 1f);
            // The thin fast white lines.
            kit.Lines = Colours(FxStudyKit.Ribbon("water_lines", "water_flow"), look.Body, look.Pale, look.Foam, look.Pale);
            Set(kit.Lines, "_Scroll", -2.4f, 0.9f); Set(kit.Lines, "_ErodeScroll", -0.5f, 0f); Set(kit.Lines, "_Cut", 0.22f, 0.70f); Set(kit.Lines, "_Stops", 0.30f, 0.50f, 0.90f);
            Set(kit.Lines, "_Ends", 0.35f, 0.10f, 0f); Set(kit.Lines, "_Soft", 0.20f); Set(kit.Lines, "_Halo", 0f); Set(kit.Lines, "_Emit", 0.5f); Set(kit.Lines, "_Add", Mathf.Lerp(0.5f, 0.9f, light));
            // The foam lip, laid crosswise so its lobes hang back from the nose all the way round.
            kit.Bow = Colours(FxStudyKit.Ribbon("water_foam", "erode_cloud"), look.Body, look.Pale, look.Foam, look.Pale);
            Set(kit.Bow, "_Swap", 1f); Set(kit.Bow, "_Scroll", 0f, 0.15f); Set(kit.Bow, "_ErodeScroll", 0f, 0f); Set(kit.Bow, "_Cut", 0.10f, 0.50f); Set(kit.Bow, "_Stops", 0.30f, 0.66f, 0.92f);
            Set(kit.Bow, "_Ends", 0f, 0f, 0f); Set(kit.Bow, "_Soft", 0.15f); Set(kit.Bow, "_Halo", 0f); Set(kit.Bow, "_Emit", 0.3f); Set(kit.Bow, "_Add", Mathf.Lerp(0.2f, 0.6f, light));
            // The wake: the same streaks, faint, thinning to nothing behind.
            kit.Wake = Colours(FxStudyKit.Ribbon("water_flow", "water_flow"), look.Deep, look.Body, look.Pale, look.Body);
            Set(kit.Wake, "_Scroll", -0.6f, 0.5f); Set(kit.Wake, "_ErodeScroll", -0.6f, 0.5f); Set(kit.Wake, "_Cut", 0.08f, 0.80f); Set(kit.Wake, "_Stops", 0.30f, 0.85f, 1.0f);
            Set(kit.Wake, "_Ends", 0.40f, 0.05f, 0f); Set(kit.Wake, "_Sweep", 1.2f); Set(kit.Wake, "_Soft", 0.25f); Set(kit.Wake, "_Halo", 0f); Set(kit.Wake, "_Emit", 0f); Set(kit.Wake, "_Add", light * 0.7f); Set(kit.Wake, "_Hue", look.HueDrift, 4f, 0.5f, 1f);
            foreach (var m in new[] { kit.Head, kit.Core, kit.Lines, kit.Bow, kit.Wake }) if (m != null) m.hideFlags = HideFlags.HideAndDontSave;
            if (kit.Head != null) { kit.Head.renderQueue = 3002; kit.Core.renderQueue = 3001; kit.Lines.renderQueue = 3003; kit.Bow.renderQueue = 3004; kit.Wake.renderQueue = 3000; }
            Kits[key] = kit;
            return kit;
        }

        public override void Sample(float age, Vector3 eye) => Pose(age - 0.3f, _benchSpentAt);

        /// <summary>
        /// Poses it `t` seconds after its cast, in its own space (its start at the origin, travelling along +z).
        /// `spentAt` above 0 is when it was spent; otherwise it runs its whole 6 m and ends there. ⚠️ ITS HEAD IS WHERE
        /// THE RULE'S COLUMN IS: `Speed` metres a second from the end of the gather, as `RafiWaterField.Travel` sums it.
        /// </summary>
        public void Pose(float t, float spentAt)
        {
            float age = Mathf.Max(0f, t), end = spentAt > 0f ? Mathf.Min(spentAt, Life) : Life;
            bool live = t >= 0f;
            float travel = Mathf.Min(Range, Mathf.Max(0f, Mathf.Min(t, end) - Gather) * Speed);
            // Gathered out of nothing at the hand for 0.18 s, then thrown, arriving whole and settling.
            float gathered = FxStudyKit.Pop(FxStudyKit.Run(t, 0f, Gather + 0.06f), 0.15f);
            float burst = FxStudyKit.Run(t, end, end + Burst);
            _front.localPosition = new Vector3(0f, Height, travel);
            // At its burst it opens out and is eaten by its own cells.
            float grow = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(gathered)) * (1f + 0.45f * FxStudyKit.EaseOut(burst));
            _front.localScale = Vector3.one * Mathf.Max(0.01f, grow);
            // It rolls about the way it travels: one and a half turns a second, on top of its pictures running round.
            _spin.localRotation = Quaternion.Euler(0f, 0f, -age * 540f);
            float eaten = FxStudyKit.EaseIn(burst);
            Frame(_head, age, live ? 0.85f : 0f, eaten);
            Frame(_core, age, live ? 0.60f : 0f, eaten);
            Frame(_lines, age, live ? 0.80f : 0f, Mathf.Lerp(0.40f, 1f, burst));
            Frame(_bow, age, live ? 0.95f * FxStudyKit.Run(t, 0.06f, Gather) : 0f, Mathf.Lerp(0.05f, 1f, burst));
            // The wake is only ever as long as the head has gone, and is let go from its far end.
            _trail.localPosition = new Vector3(0f, Height, Mathf.Max(0f, travel - 0.9f));
            _trail.localScale = new Vector3(1f, 1f, Mathf.Clamp01((travel - 0.4f) / 3f) + 0.001f);
            float letGo = FxStudyKit.Run(t, end - 0.15f, end + Burst + 0.1f);
            Frame(_wake, age, live && travel > 0.4f ? 0.55f * (1f - letGo * 0.5f) : 0f, Mathf.Lerp(0.15f, 1f, letGo));
        }

        // Its clock, how much of it shows and how far it is eaten, in its own block: the shared material is not touched.
        private static void Frame(Part part, float age, float opacity, float erode)
        {
            bool on = opacity > 0.002f && erode < 0.999f && part.Renderer.sharedMaterial != null;
            if (part.Renderer.enabled != on) part.Renderer.enabled = on;
            if (!on) return;
            part.Block.SetFloat(AgeId, age); part.Block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); part.Block.SetFloat(ErodeId, Mathf.Clamp01(erode));
            part.Block.SetVector(TileId, part.Tile); part.Block.SetVector(ErodeTileId, part.Tile);
            part.Renderer.SetPropertyBlock(part.Block);
        }
    }
}
