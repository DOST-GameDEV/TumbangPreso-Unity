using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A short branching electric arc standing off a shock trail anchor, snapping between a few
    /// pre-built shapes on a timer.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A FLOOR HAZARD SEEN FROM EYE HEIGHT IS A LINE. Zack's shock trail
    /// was one flat translucent disc, and the player it most needs to warn is running at it in
    /// first person, where a disc on the ground is edge-on and about two pixels tall. The arc is
    /// the part that has a SILHOUETTE from the angle the danger is approached from.
    ///
    /// ⚠️ THE FLICKER IS THE READ, NOT DECORATION. A straight bar reads as a post or a bollard,
    /// which is a thing you walk around; a line that changes shape several times a second reads
    /// as current, which is a thing you do not touch.
    ///
    /// ⚠️⚠️ IT IS ONE MESH PER SHAPE NOW, NOT FOUR STRETCHED CUBES PER FRAME, AND THAT IS THE
    /// SAME COMPLAINT 🧑 MADE ABOUT EVERY OTHER EFFECT IN THE GAME: *"the same logic and code was
    /// used to generate all of them"*. What stood here built four `PrimitiveType.Cube`s and
    /// re-posed them, which is the same primitive the fire trail's embers, the void's shards, the
    /// magma seams and the frost spikes were all made of. Five fictions, one lump of geometry.
    /// `VfxShapes.Bolt` is a real branching tube, so this is four renderers and four colliders
    /// down to ONE renderer and none, and the arc finally FORKS.
    ///
    /// ⚠️⚠️ THE SHAPES ARE PRE-BUILT AND CYCLED, NEVER REBUILT PER TICK. Six of these are live
    /// during a sprint and the interval is 0.09 s, so rebuilding geometry on the timer would be
    /// about seventy mesh allocations a second, all of them garbage a frame later. Three shapes
    /// built once at spawn and swapped is indistinguishable at this speed and costs nothing after
    /// the first frame. `VfxShapes.Own` cannot hold three, so the teardown is done here.
    ///
    /// ⚠️⚠️ AND IT IS SEEDED AND STEPPABLE, SO IT CAN FINALLY BE PHOTOGRAPHED. The old version
    /// reshaped from unseeded `Random.Range` in `Update`, which meant the shock trail was the one
    /// effect whose renders could not be compared between two versions, and in edit mode it froze
    /// on whatever `Build` happened to roll. `IVfxTimeline` is the same contract every blast uses:
    /// the frame a capture shows is produced by the code that produces the frame a player sees.
    ///
    /// ⚠️⚠️ KNEE HEIGHT, NEVER HIGHER. `docs/VISION.md` § 2 rule 5 requires a mid-fight frame to
    /// still show the lata, the chalk and every player. Six of these are live during a sprint,
    /// and six head-height arcs would be a fence across the arena. At **0.72 m** they mark the
    /// ground without hiding anybody standing behind them, which is a little over knee height on
    /// a 1.8 m body and is the ceiling for this: do not raise it again without re-taking
    /// `AbilityShowcaseProbe`'s worst-frame shots, which are what that rule is judged on.
    ///
    /// `docs/Hero_Strike_Balance.md` § 3.2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcFlicker : MonoBehaviour, IVfxTimeline
    {
        private const int Shapes = 3;
        private const float Height = 0.72f;
        private const float RebuildInterval = 0.09f;

        private readonly Mesh[] _shapes = new Mesh[Shapes];
        private MeshFilter _filter;
        private float _elapsed;
        private int _showing = -1;

        /// <summary>
        /// One full pass through the shapes, so a capture asked for a fraction of this lands on a
        /// well-defined one rather than on whatever the clock happened to be doing.
        /// </summary>
        public float LifeSeconds => RebuildInterval * Shapes;

        public void Build(float radius)
        {
            float r = Mathf.Max(0.2f, radius);

            var go = new GameObject("ArcBolt");
            go.transform.SetParent(transform, false);

            _filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();

            // ⚠️ EMISSION 1.6 WROTE PAST WHITE AND THE ARC DISAPPEARED INTO ITS OWN RING.
            // `Logs/shots-abilities/ability_shock_trail_v1.png` shows the whole effect as one
            // flat yellow coin with a two-pixel white squiggle standing on it, which is the arc.
            // Solid rather than ghosted so the bolt has an edge against the mark underneath it,
            // and 0.35 emission keeps it hot without clipping. Same fault as every other rim in
            // `HeroHazards`.
            VfxMaterial.Solid(renderer, new Color(1.0f, 0.97f, 0.62f), 0.35f);

            // ⚠️ BUILT IN WORLD UNITS, AND THE OBJECT IS LEFT AT SCALE 1. `VfxShapes.Bolt` takes
            // its height, its jag and its thickness as real metres precisely so this does not
            // have to reason about what a parent scale does to a tube's diameter, which is the
            // arithmetic that put a 2 m ball on the street in `docs/TODO.md` § 15.5.
            //
            // ⚠️ THE SIDEWAYS KICK IS SCALED TO THE HAZARD RADIUS. At the trail's 1.0 m a fixed
            // offset would throw the arc outside the circle it is supposed to be marking, which
            // is the same class of fault as an effect reaching past its own telegraph.
            int seed = Mathf.RoundToInt(transform.position.x * 131.0f + transform.position.z * 17.0f);

            for (int i = 0; i < Shapes; i++)
            {
                _shapes[i] = VfxShapes.Bolt(Height, 5, r * 0.22f, 0.045f, 2, seed + i * 37);
            }

            Show(0);
        }

        private void Update() => StepTo(_elapsed + Time.deltaTime);

        public void StepTo(float seconds)
        {
            _elapsed = seconds;

            if (RebuildInterval <= 0.0f) return;

            // Wraps, because the arc has no end state: it is live for as long as the mark is.
            int frame = Mathf.FloorToInt(_elapsed / RebuildInterval) % Shapes;
            if (frame < 0) frame += Shapes;

            Show(frame);
        }

        private void Show(int index)
        {
            if (_filter == null || index == _showing) return;

            _showing = index;
            _filter.sharedMesh = _shapes[index];
        }

        /// <summary>
        /// ⚠️ THREE MESHES, ONE OWNER, AND `VfxShapes.Own` CANNOT DO THIS. It is
        /// `[DisallowMultipleComponent]` and holds a single mesh, which is right for the one-shape
        /// case and leaves the other two to leak here. `VfxShapes.Own`'s note has the numbers on
        /// why that matters: a dashing hero drops a trail disc every 0.10 s, so a leak on this
        /// object is a leak measured in thousands over a round.
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < Shapes; i++)
            {
                if (_shapes[i] == null) continue;

                if (Application.isPlaying) Destroy(_shapes[i]);
                else DestroyImmediate(_shapes[i]);

                _shapes[i] = null;
            }
        }
    }
}
