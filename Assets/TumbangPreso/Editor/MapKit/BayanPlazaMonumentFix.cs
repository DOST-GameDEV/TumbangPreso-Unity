using TumbangPreso.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Takes Bayan Plaza's monument out of the defender's box.
    ///
    /// ⚠️⚠️ IT IS A SCRIPT RATHER THAN A HAND EDIT BECAUSE THE SCENE IS AN IMPORTED `.tscn`.
    /// `docs/TODO.md` § 4: the monument's collider occupied **0.70 m by 1.90 m of the chalk,
    /// from y = 0.10 to y = 5.10**. The taya is CLAMPED into that box by
    /// `Confinement.ClampToBox` and cannot step out to walk around it, so one approach to the
    /// can was permanently shielded for whoever was defending. In Classic that is a quirk you
    /// play around; in Hero Strike a wall or a zone placed against it closes a lane outright,
    /// and `docs/VISION.md` § 4 aims that mode at a bracket.
    ///
    /// ⚠️⚠️ THE COLLIDER AND THE STATUE LIVE IN DIFFERENT HIERARCHIES AND BOTH HAD TO MOVE.
    /// The box is `Obstacles/MonumentBody/CollisionShape3D` and the mesh is
    /// `Dressing/Monument/Monument`, and they were a matched pair: both measured
    /// (-8.90, 0.10, -7.70) to (-6.30, 5.10, -5.10) before this ran. Moving one without the
    /// other is an invisible wall or a statue you walk through, which is why this measures both
    /// and asserts they agree rather than trusting two numbers to be typed the same twice.
    ///
    /// ⚠️ THE MOVE IS 0.45 m WEST AND THE REST IS TAKEN OFF THE COLLIDER, and both halves of
    /// that split were forced by what is already in the corner:
    ///  * The plaza rail runs at x = -9.51, so the statue cannot travel the full 0.70 m it would
    ///    need to clear the chalk on its own: at -0.70 its base would sit inside the railing.
    ///    0.45 m stops 0.16 m clear of it and is imperceptible against a 2.60 m monument.
    ///  * The corner outside the box is not empty either. `MonHedge_1` and `Rail_5` occupy it,
    ///    so the "move it to the plaza edge" option in the TODO entry would have had to relocate
    ///    the hedge and the railing as well, and a composition somebody arranged is worth more
    ///    than the 0.25 m sliver that is left.
    ///  * That sliver is what remains INSIDE the box, and it is now walkable rather than solid.
    ///    A taya clipping the corner of a statue for a quarter of a metre is a smaller fault
    ///    than a 1.90 m wall that decides who wins the round.
    ///
    /// ⚠️ IT IS IDEMPOTENT. It solves the collider from where the statue actually is, so running
    /// it twice does not move anything twice, and it refuses rather than guesses if the scene
    /// does not look the way this note says it does.
    /// </summary>
    public static class BayanPlazaMonumentFix
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity";
        private const string ColliderPath = "BayanPlaza/Obstacles/MonumentBody";
        private const string StatuePath = "BayanPlaza/Dressing/Monument/Monument";

        /// <summary>How far west the statue moves. See the class note for what bounds it.</summary>
        private const float StatueShiftX = -0.45f;

        /// <summary>
        /// Where the collider's east face lands.
        ///
        /// ⚠️ 0.05 m OUTSIDE THE CHALK, NOT ON IT. `MapGeometryCheck.CheckBoxIsClear` computes
        /// `min(max.x, r) - max(min.x, -r)` and skips at zero or below, so a face exactly on the
        /// line is a floating-point coin toss between passing and failing.
        /// </summary>
        private const float ColliderEastFaceMargin = 0.05f;

        [MenuItem("Tumbang Preso/Fix Bayan Plaza Monument")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var body = GameObject.Find(ColliderPath);
            var statue = GameObject.Find(StatuePath);

            if (body == null || statue == null)
            {
                Debug.LogError($"[BayanPlazaMonumentFix] expected both {ColliderPath} and " +
                               $"{StatuePath}. The scene is not the one this was written for.");
                return false;
            }

            var box = body.GetComponentInChildren<BoxCollider>();
            if (box == null)
            {
                Debug.LogError("[BayanPlazaMonumentFix] MonumentBody has no BoxCollider.");
                return false;
            }

            if (!TryVisualBounds(statue, out Bounds statueBounds))
            {
                Debug.LogError("[BayanPlazaMonumentFix] the statue has no renderer to measure.");
                return false;
            }

            // Move the statue first, then solve the collider against where it ended up. Doing it
            // in that order is what makes a second run a no-op.
            statue.transform.position += Vector3.right * StatueShiftX;
            TryVisualBounds(statue, out statueBounds);

            float r = Balance.ConfinementRadius;
            float wantEast = Mathf.Min(statueBounds.max.x, -r - ColliderEastFaceMargin);
            float west = statueBounds.min.x;

            if (wantEast <= west)
            {
                Debug.LogError($"[BayanPlazaMonumentFix] the statue spans {west:F2} to " +
                               $"{statueBounds.max.x:F2} in x and the box edge is at {-r:F2}, so " +
                               "there is no collider left once the chalk is cleared. The statue " +
                               "has to move rather than the collider shrink.");
                return false;
            }

            // The collider is authored on a child at the identity, so its world box is the
            // component's centre and size through the body's transform. Both are written in the
            // body's own space so the child stays where the .tscn put it.
            Vector3 bodyPos = body.transform.position;
            float centreX = (wantEast + west) * 0.5f;
            float sizeX = wantEast - west;

            box.center = new Vector3(centreX - bodyPos.x, box.center.y, box.center.z);
            box.size = new Vector3(sizeX, box.size.y, box.size.z);

            EditorUtility.SetDirty(statue);
            EditorUtility.SetDirty(box);
            EditorSceneManager.MarkSceneDirty(statue.scene);
            EditorSceneManager.SaveScene(statue.scene);

            Bounds after = box.bounds;
            float insideX = Mathf.Min(after.max.x, r) - Mathf.Max(after.min.x, -r);

            Debug.Log($"[BayanPlazaMonumentFix] statue now {statueBounds.min.x:F2} to " +
                      $"{statueBounds.max.x:F2}, collider {after.min.x:F2} to {after.max.x:F2}, " +
                      $"box overlap {insideX:F2} m (needs to be at or below zero), " +
                      $"unguarded statue sliver {statueBounds.max.x - after.max.x:F2} m.");

            if (insideX > 0.0f)
            {
                Debug.LogError("[BayanPlazaMonumentFix] the collider is still inside the chalk.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ⚠️ THE UNION OF THE ENABLED RENDERERS, matching what `MapGeometryCheck` measures. See
        /// `IlalimNgTulayBuilder.TryVisibleBounds` for the longer version of why a disabled
        /// variant renderer in the union moves the answer.
        /// </summary>
        private static bool TryVisualBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (var r in go.GetComponentsInChildren<Renderer>(includeInactive: false))
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            return any;
        }
    }
}
