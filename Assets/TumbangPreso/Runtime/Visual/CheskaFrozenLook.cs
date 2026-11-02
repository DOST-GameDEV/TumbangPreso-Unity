using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// FROZEN, REWORKED LOOK (c1, 2026-10-29): the casing on a player Yasmin has frozen. It is the hit of FROSTBITE and
    /// the freeze of ABSOLUTE ZERO, so both read in her approved ice (GLACIAL WALL DepthB, through `CheskaFrostKit`).
    ///
    /// The rule: Frozen (`StatusRules.FrozenSeconds` 2.5 s, an Ice stun on the stun stack): no moving, no grabbing, until
    /// it runs out or is mashed off. The casing lives exactly as long as the body is Frozen, because
    /// `HeroHazards.IceCubePrisonComponent` (which this hangs under) already ends on the body's own state, on every peer.
    ///
    /// The picture: seven DepthB crystals snap up out of the road round the legs in 0.18 s, on a small frost patch that runs
    /// out under the feet. Tallest at the back and sides (to the hip), lowest in front (to the shin), so the face, the
    /// torso and the victim's own eye line stay open, which the old shards' note asked for and the camera needs. When the
    /// freeze ends it CRACKS: the crystals light from inside for a beat (the wall's slipper-hit flash) and sink back into
    /// the road front first in 0.36 s, and the patch thaws by erosion. Nothing pops and nothing alpha-fades.
    ///
    /// Cost: 3 crystal draws and 1 frost draw a frozen player, shared materials, meshes baked once a session. Old: 5 glassy
    /// shard draws with 5 materials a player, then 5 fragment draws with 5 more materials on the break.
    /// </summary>
    public sealed class CheskaFrozenLook : MonoBehaviour, IVfxTimeline
    {
        /// <summary>On: the c1 casing. Off: the old five glassy shards and their thaw fragments.</summary>
        public static bool Enabled = true;

        public const float GrowSeconds = 0.18f, CrackSeconds = 0.10f, SinkSeconds = 0.36f, PatchRadius = 0.72f;

        // In the victim's space at the freeze: +z is where they faced, heading 0 is in front, 90 their right. Feet sit
        // 0.38 to 0.48 m out, just outside the cast's blocky body, leaning out so the tops clear the arms.
        //          kind                    heading wide  tall  deep  leanOut side turn  birth  death
        private static readonly float[,] Casing =
        {
            { CheskaFrostKit.Shard,        112f, 0.44f, 0.72f, 0.32f, 14f,   4f,  30f, 0.02f, 0.10f },
            { CheskaFrostKit.Shard,        168f, 0.48f, 0.86f, 0.34f,  9f,  -5f, 120f, 0.04f, 0.12f },
            { CheskaFrostKit.Shard,        222f, 0.46f, 0.80f, 0.32f, 12f,   6f, 210f, 0.00f, 0.11f },
            { CheskaFrostKit.Shard,        272f, 0.40f, 0.66f, 0.28f, 16f,  -3f, 300f, 0.05f, 0.08f },
            { CheskaFrostKit.Cluster,       58f, 0.40f, 0.40f, 0.36f, 18f,   2f,  60f, 0.03f, 0.04f },
            { CheskaFrostKit.Stub,         318f, 0.36f, 0.34f, 0.30f, 20f,  -4f, 150f, 0.06f, 0.02f },
            { CheskaFrostKit.Stub,           8f, 0.34f, 0.26f, 0.28f, 24f,   0f, 240f, 0.07f, 0.00f },
        };
        private static readonly float[] Radii = { 0.44f, 0.40f, 0.43f, 0.44f, 0.46f, 0.45f, 0.48f };

        private static Mesh[] _meshes;
        private static Mesh _patch;
        public static string BakeReport { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() { _meshes = null; _patch = null; }

        private Renderer[] _parts;
        private Renderer _frost;
        private float _age, _releasedAt = -1f;
        public float LifeSeconds => FrozenLife + CrackSeconds + SinkSeconds + 0.1f;
        private const float FrozenLife = Core.StatusRules.FrozenSeconds;
        public bool Released => _releasedAt >= 0f;
        public Renderer[] Parts { get { var all = new System.Collections.Generic.List<Renderer>(_parts); if (_frost != null) all.Add(_frost); return all.ToArray(); } }

        /// <summary>The casing under <paramref name="prison"/>, whose origin is the victim's feet and +z their facing.</summary>
        public static CheskaFrozenLook Build(Transform prison)
        {
            Warm();
            var go = new GameObject("FrozenCasing");
            go.transform.SetParent(prison, false);
            var look = go.AddComponent<CheskaFrozenLook>();
            look._parts = CheskaFrostKit.Draw(go.transform, _meshes);
            look._frost = CheskaFrostKit.DrawFrost(go.transform, _patch, "FrozenPatch", 0.010f);
            // Under an Absolute Zero wave the casing waits for the front to reach this body (c3: "frozen players are
            // encased as the front passes them"); its clock starts that far below zero. Anywhere else the wait is 0.
            look.StepTo(-CheskaAbsoluteZeroLook.PendingDelay(prison.position));
            return look;
        }

        /// <summary>Bakes the casing ahead of the first freeze (`CheskaIceVisuals.Warmup`, behind the loading screen).</summary>
        public static void Warm()
        {
            if (_meshes == null)
            {
                var crystals = new CheskaFrostKit.Crystal[Casing.GetLength(0)];
                for (int i = 0; i < crystals.Length; i++)
                    crystals[i] = new CheskaFrostKit.Crystal((int)Casing[i, 0], Casing[i, 1], Radii[i], Casing[i, 2], Casing[i, 3], Casing[i, 4],
                                                             Casing[i, 5], Casing[i, 6], Casing[i, 7], Casing[i, 8], Casing[i, 9]);
                _meshes = CheskaFrostKit.Bake("FrozenCasing", crystals, out string report);
                BakeReport = report;
            }
            if (_patch == null) { _patch = CheskaFrostKit.Disc(PatchRadius, 6, 40); _patch.UploadMeshData(true); }
            for (int k = 0; k < 4; k++) CheskaFrostKit.Look(k);
            CheskaFrostKit.Frost();
        }

        /// <summary>
        /// The freeze ended: the casing leaves the prison (which is destroyed this frame) where it stands, cracks and
        /// sinks, then removes itself. True if there was a casing to release.
        /// </summary>
        public static bool Release(Transform prison)
        {
            var look = prison != null ? prison.GetComponentInChildren<CheskaFrozenLook>(true) : null;
            if (look == null || look.Released) return false;
            look.transform.SetParent(null, true);
            look.BeginRelease();
            return true;
        }

        /// <summary>Starts the crack and the sink at the casing's current age (a film calls it at its own moment).</summary>
        public void BeginRelease() { if (_releasedAt < 0f) _releasedAt = _age; }

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (Released && _age > _releasedAt + CrackSeconds + SinkSeconds + 0.12f) Destroy(gameObject);
        }

        public void StepTo(float seconds)
        {
            _age = seconds;
            float dieAt = Released ? _releasedAt + CrackSeconds : 1e5f;
            // The crack: lit from inside for a beat, as a slipper's hit lights the wall, then the sink.
            float flash = Released ? Mathf.Clamp01(1f - Mathf.Abs(_age - _releasedAt - CrackSeconds * 0.5f) / (CrackSeconds * 0.5f + 0.04f)) : 0f;
            CheskaFrostKit.Pose(_parts, _age, dieAt, 1f + 1.4f * flash, GrowSeconds, SinkSeconds * 0.8f);
            float front = Mathf.Clamp01(_age / 0.16f) * 1.06f;
            float thaw = Released ? Mathf.Clamp01((_age - _releasedAt) / (CrackSeconds + SinkSeconds)) * 1.08f : 0f;
            CheskaFrostKit.PoseFrost(_frost, _age, front, thaw);
        }
    }
}
