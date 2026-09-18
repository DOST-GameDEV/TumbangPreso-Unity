using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A few leaves off the tree she painted in the top right corner, tumbling
    /// down across the street and settling out before they reach the caption.
    ///
    /// ⚠️ THE LEAF IS ONE SHE ALREADY DREW. `tools/author_owner_menu_v3.py` lifts
    /// it out of the litter lying on the road in 48.png, so a falling leaf and a
    /// fallen leaf are the same object and the screen gains no new vocabulary.
    ///
    /// ⚠️⚠️ FIVE LEAVES, NOT A PARTICLE SYSTEM, AND THAT IS THE WHOLE DESIGN.
    /// 🧑: *"make sure all main menu effects are subtle"*. One leaf is on screen
    /// roughly half the time and the fall takes eleven seconds, so it reads as a
    /// quiet street rather than as weather. `OwnerRoadDust` is the precedent and
    /// its header carries the same argument: no full-screen veil.
    ///
    /// ⚠️ POSITIONS ARE IN THE PAINTING'S OWN 1920x1080 PIXELS AND ARE MAPPED
    /// THROUGH THE BACKGROUND'S `uvRect`, exactly as `OwnerRoadDust` does. A leaf
    /// placed in screen fractions would drift onto the wall or off the road the
    /// moment the aspect crop moved.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerMenuLeaves : MaskableGraphic
    {
        public RawImage Background;
        private Texture2D _leaf;
        public override Texture mainTexture => _leaf != null ? (Texture)_leaf : Texture2D.whiteTexture;

        // One fall, in source pixels: out of the canopy, across the street, gone
        // before it reaches the road markings the eye is resting on.
        private const int Count = 5;
        private const float Fall = 11.4f;

        private bool _reduced;

        protected override void Awake()
        {
            base.Awake();
            _leaf = OwnerMenuArt.Texture("main2-leaf");
            raycastTarget = false;
        }

        private void LateUpdate()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            if (reduced != _reduced) { _reduced = reduced; SetVerticesDirty(); }
            if (reduced) return;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Background == null || _leaf == null || Settings.SettingsStore.Current.ReducedUiMotion) return;
            float time = Time.unscaledTime;
            for (int i = 0; i < Count; i++)
            {
                // Staggered by an irrational fraction so no two leaves ever pair up.
                float phase = Mathf.Repeat(time / Fall + i * .61803399f, 1);
                float seed = i * 2.39996f;

                // The canopy hangs over the top right. Each leaf leaves it from a
                // different branch and is carried left by the same wind that moves
                // the dust, so the two effects read as one afternoon.
                float startX = Mathf.Lerp(1425, 1880, Mathf.Repeat(seed * .37f, 1));
                float x = startX - phase * Mathf.Lerp(360, 620, Mathf.Repeat(seed * .53f, 1))
                          + Mathf.Sin(phase * 7.1f + seed) * 34;
                float y = Mathf.Lerp(-40, 760, phase * phase * .55f + phase * .45f);

                // Tumble: the quad's width is squashed through zero, which is a leaf
                // turning edge on. Cheaper and truer than spinning a sprite.
                float turn = Mathf.Sin(time * 1.7f + seed * 3.1f);
                float roll = Mathf.Sin(phase * 5.2f + seed) * .55f;
                float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(phase / .12f))
                             * Mathf.SmoothStep(0, 1, Mathf.Clamp01((1 - phase) / .3f));
                float size = Mathf.Lerp(.42f, .62f, Mathf.Repeat(seed * .71f, 1));
                Leaf(mesh, new Vector2(x, y), new Vector2(_leaf.width * size * turn, _leaf.height * size),
                    roll, fade * .85f);
            }
        }

        // ⚠️ STATIC, BECAUSE THIS RUNS EVERY FRAME. `CLAUDE.md` § 7.1 records what a per-frame
        // allocation costs here: a HUD string rebuilt every frame took an eighth of the probe's
        // frames and most of its physics steps.
        private static readonly Vector2[] Corners =
            { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) };
        private static readonly Vector2[] Uvs =
            { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        private void Leaf(VertexHelper mesh, Vector2 centre, Vector2 half, float roll, float opacity)
        {
            if (opacity <= .004f) return;
            float cos = Mathf.Cos(roll), sin = Mathf.Sin(roll);
            var tint = (Color)color; tint.a = opacity;
            int first = mesh.currentVertCount;
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector2(Corners[i].x * half.x, Corners[i].y * half.y);
                var turned = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
                mesh.AddVert(Local(centre + turned), tint, Uvs[i]);
            }
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private Vector3 Local(Vector2 source)
        {
            var uv = Background.uvRect;
            var rect = rectTransform.rect;
            return new Vector3(rect.xMin + (source.x / 1920f - uv.x) / uv.width * rect.width,
                rect.yMin + (1 - source.y / 1080f - uv.y) / uv.height * rect.height);
        }
    }
}
