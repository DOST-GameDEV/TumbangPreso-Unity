using System.IO;
using TumbangPreso.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Builds the Phase 3 vertical slice scene from code.
    ///
    /// ⚠️⚠️ THE SCENE IS GENERATED, NOT AUTHORED, AND THAT IS A DELIBERATE CHOICE FOR THIS
    /// PHASE. A hand-authored scene is a binary blob that cannot be reviewed in a diff, cannot
    /// be rebuilt after a mistake, and cannot be regenerated when a constant moves. The arena
    /// here is derived entirely from `Balance`: the chalk square, the spawn ring and the
    /// throwing line are all computed, so changing `CONFINEMENT_RADIUS` and re-running this
    /// produces a correct arena rather than a stale one that silently disagrees with the rules.
    ///
    /// That is the same property the Godot build got from its Python map builders, which
    /// derived the chalk from the constant rather than drawing it by hand.
    ///
    /// ⚠️ THIS IS THE SLICE, NOT THE GAME. No art, no materials, no map dressing: primitives
    /// only. The point of Phase 3 is to measure MOVEMENT FEEL against the Godot build with
    /// nothing else in the scene, and every extra object is something that can be blamed for a
    /// difference that is really the controller.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.SceneBuilder.Build
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/VerticalSlice.unity";

        [MenuItem("Tumbang Preso/Build Vertical Slice Scene")]
        public static void BuildFromMenu() => BuildInternal();

        public static void Build()
        {
            bool ok = BuildInternal();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool BuildInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildGround();
            BuildChalk();

            var lata = BuildLata();
            var slippers = new Slipper[Balance.PlayerCount];
            var seats = new CharacterMotor[Balance.PlayerCount];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                seats[slot] = BuildSeat(slot);
                slippers[slot] = BuildSlipper(slot);
            }

            BuildCamera(seats[0]);
            BuildDirector(lata, seats, slippers);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(saved
                ? $"[SceneBuilder] wrote {ScenePath}"
                : $"[SceneBuilder] FAILED to write {ScenePath}");

            if (saved) AddSceneToBuildSettings();
            return saved;
        }

        // -------------------------------------------------------------------

        private static void BuildLighting()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            go.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
        }

        /// <summary>
        /// Ground big enough to hold the spawn ring and the AI standoff ring with room over.
        /// Sized from the constants rather than a round number, so it cannot become too small
        /// when the box grows.
        /// </summary>
        private static void BuildGround()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Ground";

            // A Unity plane is 10 units across, so scale is half-extent / 5.
            float halfExtent = AIController.PlayableHalfZ + 4.0f;
            go.transform.localScale = Vector3.one * (halfExtent / 5.0f);
            go.transform.position = Vector3.zero;
        }

        /// <summary>
        /// ⚠️⚠️ FOUR STRAIGHT LINES, BECAUSE THE BOX IS A SQUARE. This is the visible half of
        /// the rule that `Confinement.ClampToBox` enforces, and the two must agree: a square
        /// and a circle of the same radius differ by 2.9 m on the diagonal at the shipping
        /// value, which is exactly where a taya stands to cover a corner. Drawing this as a
        /// circle while the clamp stays square is the bug that cost a session in the original,
        /// and it is invisible until somebody walks into a corner.
        /// </summary>
        private static void BuildChalk()
        {
            var root = new GameObject("Chalk");
            float r = Balance.ConfinementRadius;
            const float w = 0.12f;

            AddLine(root.transform, "North", new Vector3(0, 0.01f, r), new Vector3(r * 2 + w, 0.02f, w));
            AddLine(root.transform, "South", new Vector3(0, 0.01f, -r), new Vector3(r * 2 + w, 0.02f, w));
            AddLine(root.transform, "East", new Vector3(r, 0.01f, 0), new Vector3(w, 0.02f, r * 2 + w));
            AddLine(root.transform, "West", new Vector3(-r, 0.01f, 0), new Vector3(w, 0.02f, r * 2 + w));

            // The throwing line, derived. Attackers must stand at or past this to throw.
            float t = Confinement.ThrowingLine();
            AddLine(root.transform, "ThrowLineN", new Vector3(0, 0.01f, t), new Vector3(t * 2, 0.02f, 0.05f));
            AddLine(root.transform, "ThrowLineS", new Vector3(0, 0.01f, -t), new Vector3(t * 2, 0.02f, 0.05f));
        }

        private static void AddLine(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;

            // Chalk is a marking, not geometry: nothing may collide with it.
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // -------------------------------------------------------------------

        private static Lata BuildLata()
        {
            var go = new GameObject("Lata");

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);

            // ⚠️ SIZED TO THE REAL CANS, WHICH SPAN 0.108 TO 0.143 IN RADIUS. A placeholder
            // at a round 0.5 would make the hit window feel wrong in exactly the way Phase 3
            // exists to detect, and the window is 0.53 m at neutral so the ratio matters.
            visual.transform.localScale = new Vector3(0.26f, 0.15f, 0.26f);
            visual.transform.localPosition = new Vector3(0, 0.15f, 0);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            return go.AddComponent<Lata>();
        }

        private static Slipper BuildSlipper(int slot)
        {
            var go = new GameObject($"Slipper{slot}");

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localScale = new Vector3(0.12f, 0.045f, 0.28f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var s = go.AddComponent<Slipper>();
            s.OwnerSlot = slot;
            s.SkinIndex = slot;
            return s;
        }

        /// <summary>
        /// ⚠️ THE CAPSULE NUMBERS BELONG TO THE PERSON ROLE, NOT TO A MODEL. 1.6 tall with a
        /// 1.25 eye height, carried straight over. Everything ever tuned against a Person,
        /// including the camera and the tag reach, assumes them, so a placeholder at Unity's
        /// default 2.0 capsule would make every distance measurement in Phase 3 wrong.
        /// </summary>
        private static CharacterMotor BuildSeat(int slot)
        {
            var go = new GameObject($"Seat{slot}");

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.8f, 0);
            cc.slopeLimit = 45.0f;
            cc.stepOffset = 0.3f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            visual.transform.localPosition = new Vector3(0, 0.8f, 0);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);
            hand.transform.localPosition = new Vector3(0.3f, 1.1f, 0.35f);

            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;
            motor.CharacterIndex = slot;

            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();

            // ⚠️ EVERY SEAT IS A BOT IN THE SLICE, INCLUDING SEAT 0. A headless run has no
            // keyboard, so a human seat would simply stand still and the probe would measure
            // a match that never happened.
            go.AddComponent<AIController>();

            return motor;
        }

        private static void BuildCamera(CharacterMotor follow)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60.0f;

            // A fixed overhead angle for the slice. The real rig is Phase 6.
            go.transform.position = new Vector3(0, 14.0f, -14.0f);
            go.transform.rotation = Quaternion.Euler(40.0f, 0, 0);
        }

        private static void BuildDirector(Lata lata, CharacterMotor[] seats, Slipper[] slippers)
        {
            var go = new GameObject("MatchDirector");
            var runner = go.AddComponent<SliceRunner>();
            runner.Lata = lata;
            runner.Seats = seats;
            runner.Slippers = slippers;
        }

        private static void AddSceneToBuildSettings()
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
                if (s.path == ScenePath) return;

            var next = new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(next, 0);
            next[existing.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = next;
        }
    }
}
