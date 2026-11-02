using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ABSOLUTE ZERO, REWORKED LIVE LOOK (c3, 2026-10-29). Its cutscene is not this and is unchanged.
    ///
    /// The owner, of c1: "abszero reads as noise on the floor and not like a wave of ice suddenly expanding to cover the
    /// whole floor and freeze it". The reference for all of her effects is Greg Tapper's ice sketch #27 (a glossy floor
    /// swirling out of a centre under a crown of jagged crystals); the Fallen Angel ice boss attack (crystals bursting up
    /// along a travelling line) and a crystal burst out of a bright core are secondary, for the front and the centre.
    ///
    /// The rule (`CheskaHeroKit.AbsoluteZero`): no area. As play resumes from her cutscene every other player is Frozen
    /// 2.5 s, then Chilled 5 s. She is not touched. So the wave is cosmetic and is gone before play needs the floor.
    ///
    /// The picture, as a WAVE:
    ///   * 0 s, HER CENTRE BURSTS: seven DepthB crystals burst up round her feet and sink again within the first second.
    ///   * THE FRONT RACES OUT across the whole playable box in 0.5 s (fast, then easing as it reaches the far corner):
    ///     DepthB crystals burst up out of the road along it, taller in clusters, each rising as the front reaches its foot
    ///     and cracking back into the road 0.3 s later, so a crown of crystals runs across the court.
    ///   * BEHIND IT the court is one continuous slab of #27's ice (`Shaders/CheskaIceSlab`): darker than the road,
    ///     glossy, swirled cyan seams and white spiral cracks out of her. No patches, no noise.
    ///   * Every frozen player is cased (`CheskaFrozenLook`) AS THE FRONT PASSES THEM: their casing waits for it.
    ///   * It holds, then from 2.0 s it thaws by cracking up, the edge cells first, gone by 2.9 s.
    ///
    /// Cost: 1 slab draw (a grid over the playable box, built and draped once a cast), 2 to 4 crystal draws for the front
    /// (baked ONCE A CAST, because its crystals stand where her box is; the report gives the time) and 2 for the burst
    /// (baked once a session); shared materials.
    ///
    /// ⚠️ THE FLOOR IS THE ICE LOTUS'S NOW (`LotusFloor`, 2026-10-30). The owner: "for cheska, her ult can use the floor vfx
    /// used in lotus", then "the ice on the floor should have the same material as the ice wall". The slab above is kept
    /// behind the switch. With it on (`FxStudyLotusFrost`, `CheskaIceFloorMap`): the bench lotus's ice spikes stand at her feet,
    /// and every walkable surface is broken floes in the Glacial Wall's own DepthB material, rising
    /// as the front reaches them and sinking from the edge of the court in at the thaw. The front, the crystals, the casings
    /// and every time above are unchanged. Cost with it on: 2 draws for floes and frost, the strike's light and burst and
    /// the spikes, made once a cast and freed with the look; its rays and milliseconds are in `FrostReport`.
    /// </summary>
    public sealed class CheskaAbsoluteZeroLook : MonoBehaviour, IVfxTimeline
    {
        /// <summary>On: the c3 live look. Off: the old nova (plus q2's pieces if `UltimateLiveLook.CheskaAbsoluteZero`).</summary>
        public static bool Enabled = true;

        /// <summary>On: the court is the ice lotus's floor (`FxStudyLotusFrost`). Off: c3's slab. Read once a cast, at `Play`.</summary>
        public static bool LotusFloor = true;

        public const float RunSeconds = 0.5f, ThawStart = 2.0f, ThawSeconds = 0.85f, BurstSinkAt = 0.55f;
        /// <summary>How long a front crystal stands after the front reaches it, before it sinks.</summary>
        public const float FrontHold = 0.30f;
        public float LifeSeconds => ThawStart + ThawSeconds + 0.1f;

        // Her centre: seven crystals burst up round her feet as play resumes, lean out, and sink within the first second.
        //          kind                    heading wide  tall  deep  leanOut side turn  birth  death
        private static readonly float[,] Burst =
        {
            { CheskaFrostKit.Shard,         20f, 0.56f, 1.00f, 0.40f, 30f,  -6f,  40f, 0.00f, 0.20f },
            { CheskaFrostKit.Spike,         72f, 0.34f, 1.20f, 0.30f, 34f,   8f, 150f, 0.03f, 0.12f },
            { CheskaFrostKit.Shard,        124f, 0.52f, 0.86f, 0.38f, 28f,  -4f, 260f, 0.01f, 0.06f },
            { CheskaFrostKit.Spike,        170f, 0.32f, 1.06f, 0.28f, 36f,   5f,  70f, 0.04f, 0.00f },
            { CheskaFrostKit.Shard,        218f, 0.54f, 0.92f, 0.40f, 30f,   6f, 190f, 0.02f, 0.04f },
            { CheskaFrostKit.Spike,        268f, 0.34f, 1.12f, 0.30f, 32f,  -8f, 310f, 0.00f, 0.10f },
            { CheskaFrostKit.Shard,        318f, 0.50f, 0.82f, 0.38f, 26f,   2f, 100f, 0.05f, 0.18f },
        };
        private static readonly float[] BurstRadii = { 0.85f, 0.95f, 0.80f, 0.90f, 0.85f, 1.00f, 0.75f };

        private static Mesh[] _burst;
        public static string BakeReport { get; private set; } = "";
        /// <summary>What the last cast's floor came to, for a film's report.</summary>
        public static string FrostReport { get; private set; } = "";
        /// <summary>A film in a preview scene hands the map's standing points here: `MapRoutes.Current` is only set in a match.</summary>
        public static Vector3[] StandingOverride;
        private static CheskaIceFloorMap _map;

        // The wave in flight, so a casing raised by a body's Frozen status waits for the front to reach that body.
        private static Vector3 _waveAt;
        private static float _waveTime = -100f, _waveFar = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() { _burst = null; _waveTime = -100f; }

        private Renderer[] _burstParts, _frontParts;
        private Renderer _slab;
        private FxStudyLotusFrost _frost;
        private Mesh[] _frontMeshes;
        private float _age, _far, _edge;
        private Vector4 _clip;
        public float Far => _far;
        /// <summary>The lotus's objects, for a film that shows the caster's view.</summary>
        public Renderer[] LotusParts => _frost != null ? _frost.Lotus : new Renderer[0];
        public Renderer[] Parts
        {
            get
            {
                var all = new List<Renderer>(_burstParts); all.AddRange(_frontParts);
                if (_slab != null) all.Add(_slab);
                if (_frost != null) all.AddRange(_frost.Root.GetComponentsInChildren<Renderer>(true));
                return all.ToArray();
            }
        }

        /// <summary>
        /// Seconds after the hand-back at which the front reaches <paramref name="distance"/> metres from her: the front's
        /// radius is far * (1 - (1 - t / RunSeconds)^2), solved for t.
        /// </summary>
        public static float Arrival(float distance, float far)
        {
            float share = Mathf.Clamp01(distance / Mathf.Max(0.01f, far));
            return RunSeconds * (1f - Mathf.Sqrt(1f - share));
        }

        /// <summary>How much longer a casing at <paramref name="at"/> should wait for a live wave's front (0 if none).</summary>
        public static float PendingDelay(Vector3 at)
        {
            float since = Time.time - _waveTime;
            if (since < 0f || since > RunSeconds + 0.1f) return 0f;
            var flat = new Vector2(at.x - _waveAt.x, at.z - _waveAt.z);
            return Mathf.Max(0f, Arrival(flat.magnitude, _waveFar) - since);
        }

        /// <summary>The live look at the hand-back, round her at <paramref name="at"/>.</summary>
        /// <param name="ownEyes">This client is the caster: her camera stands among the petals, which from there were big
        /// grey facets over the bottom of the picture ("the worst thing in the film"), so she alone sees clean ice.</param>
        public static CheskaAbsoluteZeroLook Play(Vector3 at, bool renderOnly = false, bool ownEyes = false)
        {
            // Her own surface, a platform included: the shared rule prefers the court under a platform to the platform.
            at = LotusFloor && VfxShapes.SurfaceUnderFeet(at, out float feet) ? new Vector3(at.x, feet, at.z) : VfxShapes.GroundPoint(at);
            var root = new GameObject("AbsoluteZeroLive");
            root.transform.position = at;
            var look = root.AddComponent<CheskaAbsoluteZeroLook>();
            look.enabled = !renderOnly;
            Warm();
            look._burstParts = CheskaFrostKit.Draw(root.transform, _burst);

            float x0 = AIController.PlayableMinX, x1 = AIController.PlayableMaxX, z0 = AIController.PlayableMinZ, z1 = AIController.PlayableMaxZ;
            float far = 0f;
            for (int k = 0; k < 4; k++)
            {
                float cx = (k & 1) == 0 ? x0 : x1, cz = (k & 2) == 0 ? z0 : z1;
                far = Mathf.Max(far, new Vector2(cx - at.x, cz - at.z).magnitude);
            }
            look._far = far;
            look._edge = Mathf.Max(1f, 0.5f * Mathf.Min(x1 - x0, z1 - z0));
            look._clip = new Vector4(x0 - 0.3f, x1 + 0.3f, z0 - 0.3f, z1 + 0.3f);
            var min = new Vector2(x0 - at.x, z0 - at.z); var max = new Vector2(x1 - at.x, z1 - at.z);
            if (LotusFloor)
            {
                // Every surface a body can stand on, from above the box's walls down, from the map's own standing points.
                float top = Mathf.Max(AIController.PlayableCeilingY, at.y + 6f);
                var standing = StandingOverride ?? (MapRoutes.Current != null ? MapRoutes.Current.Nodes : null);
                string scene = FloorKey();
                _map = CheskaIceFloorMap.For(new Vector2(x0, z0), new Vector2(x1, z1),
                    (Vector2 xz, List<VfxShapes.Surface> into) => VfxShapes.WalkableSurfaces(new Vector3(xz.x, top, xz.y), top + 60f, into), standing, at, scene);
                look._frost = new FxStudyLotusFrost(root.transform, min, max, RunSeconds, ThawStart, ThawSeconds, _map, 0f, at);
                FrostReport = look._frost.Report;
                if (ownEyes) look._frost.HideSpikes();
            }
            else
            {
                look._slab = CheskaFrostKit.DrawSlab(root.transform, CheskaFrostKit.SlabBox(min, max, far), "AbsoluteZeroSlab");
                if (look._slab != null) VfxShapes.DrapeToGround(look._slab.gameObject, 0f, 0.4f);
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            look._frontMeshes = CheskaFrostKit.Bake("AbsoluteZeroFront", FrontCrystals(min, max, far, at, LotusFloor ? _map : null), out string report);
            look._frontParts = CheskaFrostKit.Draw(root.transform, look._frontMeshes);
            BakeReport = "front: " + report + "baked in " + watch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms this cast";

            if (!renderOnly) { _waveAt = at; _waveTime = Time.time; _waveFar = far; }
            look.StepTo(0f);
            return look;
        }

        // A stage rearranges between rounds (`ArenaStage`), so its active colliders are part of what was baked.
        private static string FloorKey()
        {
            string key = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var stage = Map.ArenaStage.Instance;
            if (stage == null) return key;
            float sum = 0f;
            foreach (var c in stage.GetComponentsInChildren<Collider>(false)) sum += c.bounds.max.y + c.bounds.center.x * 0.01f;
            return key + sum.ToString("0.00");
        }

        /// <summary>Takes the rays of the floor's map behind the loading screen (`MatchInstaller.Start`), so a cast only lays floes.
        /// A match without her pays nothing (130 to 160 ms on Eskinita, every match). If she arrives later, her first cast bakes
        /// it (`Play`).</summary>
        public static void PrepareFloor()
        {
            if (!Enabled || !LotusFloor || !SheIsInThisMatch()) return;
            float x0 = AIController.PlayableMinX, x1 = AIController.PlayableMaxX, z0 = AIController.PlayableMinZ, z1 = AIController.PlayableMaxZ;
            float top = Mathf.Max(AIController.PlayableCeilingY, 6f);
            var standing = StandingOverride ?? (MapRoutes.Current != null ? MapRoutes.Current.Nodes : null);
            var middle = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            _map = CheskaIceFloorMap.For(new Vector2(x0, z0), new Vector2(x1, z1),
                (Vector2 xz, List<VfxShapes.Surface> into) => VfxShapes.WalkableSurfaces(new Vector3(xz.x, top, xz.y), top + 60f, into), standing, middle, FloorKey());
            FxStudyLotusFrost.PrepareGeometry(_map);
        }

        private static bool SheIsInThisMatch()
        {
            foreach (var seat in Object.FindObjectsByType<Abilities.HeroAbilitySystem>(FindObjectsSortMode.None))
                if (seat.Kit is Abilities.CheskaHeroKit) return true;
            return false;
        }

        /// <summary>Bakes her burst ahead of the first cast (`CheskaIceVisuals.Warmup`, behind the loading screen).</summary>
        public static void Warm()
        {
            CheskaFrostKit.Slab();
            if (_burst != null) return;
            var crystals = new CheskaFrostKit.Crystal[Burst.GetLength(0)];
            for (int i = 0; i < crystals.Length; i++)
                crystals[i] = new CheskaFrostKit.Crystal((int)Burst[i, 0], Burst[i, 1], BurstRadii[i], Burst[i, 2], Burst[i, 3], Burst[i, 4],
                                                         Burst[i, 5], Burst[i, 6], Burst[i, 7], Burst[i, 8], Burst[i, 9]);
            _burst = CheskaFrostKit.Bake("AbsoluteZeroBurst", crystals, out string report);
            BakeReport = report;
        }

        // The front's crystals: rings out from her every 1.9 m, a crystal about every 1.6 m along each ring, only inside the
        // box. Heights swell and fall round each ring, so they come up taller in clusters. Each is born when the front
        // reaches its foot and dies `FrontHold` later (`_DieAt` is 0; its death is in its own life).
        private static CheskaFrostKit.Crystal[] FrontCrystals(Vector2 min, Vector2 max, float far, Vector3 at, CheskaIceFloorMap map)
        {
            var list = new List<CheskaFrostKit.Crystal>();
            int n = 0;
            for (float r = 1.9f; r < far; r += 1.9f)
            {
                int count = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * r / 1.6f));
                float offset = Hash(r * 7.1f) * 360f;
                for (int k = 0; k < count; k++, n++)
                {
                    float heading = offset + k * 360f / count + (Hash(n * 1.37f) - 0.5f) * 140f / count;
                    float rr = r + (Hash(n * 2.91f) - 0.5f) * 0.9f;
                    float h = heading * Mathf.Deg2Rad;
                    float x = Mathf.Sin(h) * rr, z = Mathf.Cos(h) * rr;
                    if (x < min.x + 0.35f || x > max.x - 0.35f || z < min.y + 0.35f || z > max.y - 0.35f) continue;
                    // On whatever surface is there, and none over a place with no ice (they stood in the air off a platform).
                    float y = 0f;
                    if (map != null) { float? surface = map.TopAt(at.x + x, at.z + z); if (!surface.HasValue) continue; y = surface.Value - at.y; }
                    float swell = 0.5f + 0.5f * Mathf.Sin(heading * Mathf.Deg2Rad * 5f + r);
                    float tall = Mathf.Lerp(0.40f, 0.95f, swell * swell) * (0.85f + 0.3f * Hash(n * 4.3f));
                    int kind = swell > 0.6f ? (n % 2 == 0 ? CheskaFrostKit.Spike : CheskaFrostKit.Shard) : (n % 3 == 0 ? CheskaFrostKit.Stub : CheskaFrostKit.Shard);
                    float birth = Arrival(rr, far) - 0.04f;
                    list.Add(new CheskaFrostKit.Crystal(kind, heading, rr, tall * (kind == CheskaFrostKit.Spike ? 0.45f : 0.62f), tall, tall * 0.42f,
                        28f + 14f * Hash(n * 5.7f), (Hash(n * 6.1f) - 0.5f) * 20f, Hash(n * 8.9f) * 360f, birth, birth + FrontHold, y));
                }
            }
            return list.ToArray();
        }

        private static float Hash(float x) => Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f);

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (_age > LifeSeconds) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // The front was baked for this cast alone.
            if (_frontMeshes != null) foreach (var m in _frontMeshes) CheskaFrostKit.Kill(m);
            // The frost's quads and materials were made for this cast too.
            _frost?.Dispose(); _frost = null;
        }

        public void StepTo(float seconds)
        {
            _age = seconds;
            CheskaFrostKit.Pose(_burstParts, _age, BurstSinkAt, 1f, -1f, 0.45f);
            CheskaFrostKit.Pose(_frontParts, _age, 0f, 1f, 0.16f, 0.34f);
            float x = Mathf.Clamp01(_age / RunSeconds);
            float reach = 1f - (1f - x) * (1f - x);
            float lag = 0.03f;
            // The slab's front is the crystals' front: `_Front` solved so the run shown is the radius reached.
            float front = x >= 1f ? 1.06f : (reach + lag * 0.5f) / (1.02f + lag);
            float thaw = Mathf.Clamp01((_age - ThawStart) / ThawSeconds) * 1.08f;
            CheskaFrostKit.PoseSlab(_slab, front, thaw, _edge, _clip, 1.15f, lag);
            // Its pieces all lie on the floor, so where it is seen from does not move any of them.
            _frost?.Sample(_age, Vector3.zero);
        }
    }
}
