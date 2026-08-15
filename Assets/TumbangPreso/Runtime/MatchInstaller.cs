using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Drops a playable match into a map scene at runtime.
    ///
    /// ⚠️⚠️ THE MAPS ARE GEOMETRY AND NOTHING ELSE, AND THAT IS THE RIGHT SPLIT. They are
    /// converted node-for-node from the Godot builders' output, so anything added to those
    /// scenes by hand is lost the moment a map is re-imported. Keeping the gameplay in a
    /// component that BUILDS itself means a map can be regenerated from source at any time
    /// without losing the players, and it means both arenas are wired identically by
    /// construction rather than by somebody remembering to do the second one.
    ///
    /// ⚠️ SPAWNS COME FROM THE BOX, NOT FROM MARKERS IN THE MAP. "Outside the box" is the rule,
    /// and a marker that drifted half a metre inside the radius would spawn an attacker
    /// VULNERABLE on frame one. That reads as a rules bug and gets debugged as one.
    /// </summary>
    public sealed class MatchInstaller : MonoBehaviour
    {
        [Tooltip("Seat 0 is the human unless this is left on for a headless probe run.")]
        [SerializeField] private bool _allBots;

        private void Start()
        {
            var lata = BuildLata();
            var seats = new CharacterMotor[Balance.PlayerCount];
            var slippers = new Slipper[Balance.PlayerCount];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                seats[slot] = BuildSeat(slot);
                slippers[slot] = BuildSlipper(slot);
            }

            BuildCameraAndHud(seats[0]);

            var runner = gameObject.AddComponent<SliceRunner>();
            runner.Lata = lata;
            runner.Seats = seats;
            runner.Slippers = slippers;
            runner.AutoStart = true;

            GameServices.Music?.Play("match", GameServices.MatchTrack);
        }

        private Lata BuildLata()
        {
            var go = new GameObject("Lata");

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);

            // Sized to the real cans, which span 0.108 to 0.143 in radius. A round placeholder
            // would make the 0.53 m hit window feel wrong in exactly the way it is meant to be
            // measured against.
            visual.transform.localScale = new Vector3(0.26f, 0.15f, 0.26f);
            visual.transform.localPosition = new Vector3(0, 0.15f, 0);
            Destroy(visual.GetComponent<Collider>());

            return go.AddComponent<Lata>();
        }

        private Slipper BuildSlipper(int slot)
        {
            var go = new GameObject($"Slipper{slot}");

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(go.transform);
            visual.transform.localScale = new Vector3(0.12f, 0.045f, 0.28f);
            Destroy(visual.GetComponent<Collider>());

            var s = go.AddComponent<Slipper>();
            s.OwnerSlot = slot;
            s.SkinIndex = slot;
            return s;
        }

        /// <summary>
        /// ⚠️ 1.6 CAPSULE, 1.25 EYE. Those belong to the Person ROLE, not to any model, and
        /// everything ever tuned against a Person assumes them. A default 2.0 capsule would
        /// quietly invalidate every distance in the game.
        /// </summary>
        private CharacterMotor BuildSeat(int slot)
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
            Destroy(visual.GetComponent<Collider>());

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);
            hand.transform.localPosition = new Vector3(0.3f, 1.1f, 0.35f);

            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;

            // ⚠️ SEAT 0 WEARS THE PLAYER'S OWN PICK. -1 is legal and resolves to neutral, which
            // is exactly what an AI seat and a peer on an older build both get.
            motor.CharacterIndex = slot == 0
                ? Settings.SettingsStore.Current.CharacterPick
                : slot;

            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();
            go.AddComponent<Social.EmotePlayer>();
            go.AddComponent<Visual.CharacterVisual>();

            bool human = slot == 0 && !_allBots;
            if (human) go.AddComponent<PlayerInputReader>();
            else go.AddComponent<AIController>();

            return motor;
        }

        private void BuildCameraAndHud(CharacterMotor local)
        {
            // ⚠️ THE MAP MAY ALREADY CARRY A CAMERA from its Godot original. Reuse it rather
            // than adding a second, because two enabled cameras render over each other and it
            // reads as a graphics bug rather than a scene one.
            var existing = UnityEngine.Camera.main;
            GameObject camGo;

            if (existing != null)
            {
                camGo = existing.gameObject;
            }
            else
            {
                camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<UnityEngine.Camera>();
            }

            var rig = camGo.GetComponent<CameraSystem.CameraRig>();
            if (rig == null) rig = camGo.AddComponent<CameraSystem.CameraRig>();
            rig.Follow(local);

            var hudGo = new GameObject("HUD");
            hudGo.AddComponent<UI.Hud>().Bind(local);

            var pauseGo = new GameObject("PauseHost");
            pauseGo.AddComponent<PauseWatcher>().Local = local;
        }
    }

    /// <summary>
    /// Opens the pause overlay on Escape.
    ///
    /// ⚠️ IT PARKS INPUT AS WELL AS STOPPING TIME. A verb held across the pause boundary stays
    /// held in the intent table, and the player walks out of the menu already sprinting or
    /// mid-throw-charge.
    /// </summary>
    public sealed class PauseWatcher : MonoBehaviour
    {
        public CharacterMotor Local;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var panel = UI.Panel.Open<UI.PausePanel>(this);
            panel.Local = Local;
        }
    }
}
