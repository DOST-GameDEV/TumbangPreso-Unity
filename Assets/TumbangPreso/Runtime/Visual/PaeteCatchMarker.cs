using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// WHERE PAETE'S VINES WILL CATCH, SHOWN BEFORE HE CASTS: a small ring of four leaves on the exact spot.
    ///
    /// ⚠️ WHY (owner, 2026-10-10: "when the vines latch it snaps into a diferent position close to it"; told that the
    /// catch moving to an edge is the leap's own rule: "the catch point jumping to an edge is what happens, but i feel
    /// like this snap shouldnt be seen"; asked how to hide it, he chose the catch marker). The leap takes the nearest
    /// thing along the aim, and an aim at open sky is brought down and a little sideways to the first edge in range
    /// (`PaeteVine.FindCatch`, `SnapUp`: up to 44 degrees under the aim, 16 to a side). The vines then fly to THAT
    /// point, not to the crosshair, and until now the player first learnt which point when they arrived. This draws
    /// the same answer the cast will get, every frame the leap is ready, so the choice of edge is seen before the
    /// press and nothing jumps.
    ///
    /// ⚠️ IT ASKS THE SAME QUESTION THE CAST ASKS (`FindCatch` with his feet, his facing and his aim point, as
    /// `PaeteHeroKit` does), so it cannot show one edge and catch another. It is drawn only for the body this camera
    /// is looking through, only while he is Paete with the leap ready and not already swinging. Presentation only:
    /// nothing is sent, nothing gameplay reads it, and it makes no collider.
    ///
    /// ⚠️ THE LOOK: leaves, not a particle and not a UI dot. Four pointed leaves round the spot, tips inward, turning
    /// slowly; a SWING catch (a beam, a lip, a face) is bright spring green and open, a plain pull to the ground is
    /// smaller and paler. It keeps about the same size on screen at any range.
    /// </summary>
    public sealed class PaeteCatchMarker : MonoBehaviour
    {
        /// <summary>Off, the marker is never drawn (a switch for a capture or a comparison).</summary>
        public static bool Enabled = true;

        private static readonly Color SwingLeaf = new Color(0.62f, 0.96f, 0.30f, 0.95f);
        private static readonly Color PullLeaf = new Color(0.80f, 0.92f, 0.62f, 0.60f);

        private Transform _ring;
        private Material _material;
        private Vector3 _at;
        private float _shown, _open;
        private bool _placed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("PaeteCatchMarker") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<PaeteCatchMarker>();
        }

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { enabled = false; return; }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 3100 };
            var ring = new GameObject("Leaves") { hideFlags = HideFlags.HideAndDontSave };
            ring.transform.SetParent(transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = Leaves();
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _ring = ring.transform;
            ring.SetActive(false);
        }

        /// <summary>Four leaves in the XY plane, tips toward the middle, each a pointed oval with a darker midrib side.</summary>
        private static Mesh Leaves()
        {
            const int segments = 6;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var colours = new System.Collections.Generic.List<Color>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int leaf = 0; leaf < 4; leaf++)
            {
                var turn = Quaternion.AngleAxis(leaf * 90f + 45f, Vector3.forward);
                int first = vertices.Count;
                // The midrib from the inner tip (0.34 out) to the stem end (1.0 out), and an edge either side of it.
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float along = Mathf.Lerp(0.34f, 1.0f, t);
                    float half = 0.21f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f));
                    vertices.Add(turn * new Vector3(-half, along, 0f)); colours.Add(new Color(1f, 1f, 1f, 1f));
                    vertices.Add(turn * new Vector3(0f, along, 0f)); colours.Add(new Color(0.78f, 0.86f, 0.70f, 1f));
                    vertices.Add(turn * new Vector3(half, along, 0f)); colours.Add(new Color(0.88f, 0.94f, 0.82f, 1f));
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = first + i * 3, b = a + 3;
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1, a + 1, b + 1, a + 2, a + 2, b + 1, b + 2 });
                }
            }
            var mesh = new Mesh { name = "Paete catch marker", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private void LateUpdate()
        {
            if (_ring == null) return;
            bool show = false, swing = false;
            Vector3 target = _at;
            var camera = Camera.main;
            if (Enabled && camera != null && !MatchInstaller.PreviewOnly && !PresentationClock.BlocksInput)
            {
                var viewer = CourtBoundaryPresentation.Viewer(camera);
                // ⚠️ A hero kit is not a component (play probe 3: `GetComponent<PaeteHeroKit>` threw every frame of a match, 93,812
                // times in five minutes); it is reached through the body's ability system.
                var kit = viewer != null && viewer.AbilitySystem != null ? viewer.AbilitySystem.Kit as PaeteHeroKit : null;
                var leap = kit != null ? kit.Skill1 : null;
                if (leap != null && leap.IsReady && !viewer.IsSwinging && !viewer.IsBot && viewer.IsLocallySimulated()
                    && viewer.Intent != null && viewer.Intent.HasAimPoint)
                {
                    Vector3 forward = viewer.transform.forward;
                    target = PaeteVine.FindCatch(viewer.transform.position, forward, viewer.Intent.AimPoint, out VineSwing kind);
                    swing = kind != VineSwing.Reel;
                    show = true;
                }
            }

            _shown = Mathf.MoveTowards(_shown, show ? 1f : 0f, Time.unscaledDeltaTime * (show ? 9f : 14f));
            if (_shown <= 0.001f) { if (_ring.gameObject.activeSelf) _ring.gameObject.SetActive(false); _placed = false; return; }
            if (!_ring.gameObject.activeSelf) _ring.gameObject.SetActive(true);
            if (camera == null) return;

            // It GLIDES to a new catch (quickly), so the choice moving from one edge to the next is a motion, not a blink.
            if (show)
            {
                if (!_placed || (target - _at).sqrMagnitude > 36f) _at = target;
                else _at = Vector3.Lerp(_at, target, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
                _placed = true;
            }
            _open = Mathf.MoveTowards(_open, swing ? 1f : 0f, Time.unscaledDeltaTime * 7f);

            Vector3 toEye = camera.transform.position - _at;
            float range = Mathf.Max(0.5f, toEye.magnitude);
            // A little toward the eye, so it is not half buried in the face it sits on.
            _ring.position = _at + toEye / range * Mathf.Min(0.18f, range * 0.3f);
            _ring.rotation = camera.transform.rotation * Quaternion.AngleAxis(Time.unscaledTime * 38f, Vector3.forward);
            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 5.2f);
            float size = range * Mathf.Lerp(0.030f, 0.043f, _open) * pulse * Mathf.Lerp(0.6f, 1f, _shown);
            _ring.localScale = new Vector3(size, size, size);
            Color colour = Color.Lerp(PullLeaf, SwingLeaf, _open);
            colour.a *= _shown;
            _material.color = colour;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
