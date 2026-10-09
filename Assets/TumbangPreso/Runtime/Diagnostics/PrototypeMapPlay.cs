#if UNITY_EDITOR
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// What the temporary prototype map does in play (`Editor/CharacterPrototypeMap.cs` puts one of these on the row of
    /// heroes). EDITOR ONLY: the whole file is compiled out of a player build.
    ///
    /// Owner, 2026-10-06: *"can you remove the gameplay in the prototype map? and also do the thing where i can look at
    /// the characters and switch to them"*.
    ///
    ///  * NO GAME. The map plays as the Training Range, which still stands a can on its base. Here the can is frozen and
    ///    hidden, and the range's own cheats are switched on (skills always ready, the ultimate full, stamina endless),
    ///    so the map is a place to move and cast in, with nothing to win.
    ///  * LOOK AND PRESS H. The hero nearest the middle of the view, within `PickCone` degrees, is named at the bottom
    ///    of the screen; H makes the player that hero through the range's own switch (`PracticeRange.ChangeCharacter`),
    ///    which is what its pause menu calls: body, kit and first-person arms.
    ///  * F3 shows what the body's walk layer is doing, for chasing a hero whose hands or arms do not move.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class PrototypeMapPlay : MonoBehaviour
    {
        public Transform[] Figures = System.Array.Empty<Transform>();
        public string[] HeroIds = System.Array.Empty<string>();
        public float PickCone = 12f;

        /// <summary>A figure in the Classic row, with the model and clips H puts on the player.</summary>
        [System.Serializable]
        public struct Body
        {
            public string Id;
            public Transform Figure;
            public GameObject Model;
            public AnimationClip[] Clips;
        }

        public Body[] Classics = System.Array.Empty<Body>();

        // The walk layer's readout is always on while a hero's hands are being chased. (It was on F3 for one build; F3 is
        // the game's own key for taking over another seat.)
        private bool _quiet, _details = true;
        private string _looking, _notice;
        private float _noticeLeft;
        private GUIStyle _style;

        /// <summary>
        /// ⚠️ THE LAUNCH IS SET HERE, BEFORE THE MAP'S LAUNCHER WAKES (execution order -10000). The editor tool set it from
        /// a hook that runs when scripts reload, and the map came up as an ordinary match with bots (owner, 2026-10-06:
        /// "gameplay still happening", and F3 "switches me to play as another person/bot"): that hook does not run when
        /// the editor enters play without reloading scripts. This is in the scene, so it runs either way. The same
        /// launch the menu's practice button sets (`ConvertedMatchSetup.StartPractice`).
        /// </summary>
        private void Awake()
        {
            GameLaunch.Reset();
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            GameLaunch.TrainingRange = true;
            GameLaunch.SelectedMap = "eskinita";
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SelectedMode = GameMode.HeroStrike;
            // The movement rework is a debug switch that is on by default only here (owner, 2026-10-07). F9 flips it.
            MovementRework.Enabled = true;
            Debug.Log("[PrototypeMap] Training Range requested from the scene. Requested = " + PracticeRange.Requested);
        }

        // (This scene used to turn the rework off again as it unloaded. It is on for the whole game now, 2026-10-09.)

        private void Update()
        {
            var range = PracticeRange.Instance;
            if (!_quiet && PracticeRange.Active && range != null && range.CanEdit) Quiet(range);
            // Kept on: a reset of the range or a change of character must not bring the cooldowns back.
            else if (_quiet && range != null && range.CanEdit
                && !(range.Value(PracticeRange.Cheat.NoCooldowns) && range.Value(PracticeRange.Cheat.InfiniteSkills) && range.Value(PracticeRange.Cheat.FullUltimate)))
            {
                range.Set(PracticeRange.Cheat.NoCooldowns, true);
                range.Set(PracticeRange.Cheat.InfiniteSkills, true);
                range.Set(PracticeRange.Cheat.FullUltimate, true);
            }

            // ⚠️ THE MOUSE COMES BACK WHEN THE MENU CLOSES (owner, 2026-10-07: "pressing escape and then trying to click back
            // in doesnt work. but u can still look around"). Escape opens the pause card, which frees the mouse, and on
            // closing the card takes it back only on a map of the game's pool (`SceneFlow.InMatch`, by scene name). This
            // scene is outside the pool on purpose, so nothing took it back and the game no longer even wanted it.
            // Here: playing, the body's input live (no menu holds it), and the mouse not wanted, means take it.
            // (Not in the third person's free camera, which wants the pointer free on purpose.)
            if (_rig == null) _rig = FindAnyObjectByType<CameraSystem.CameraRig>();
            if (range != null && range.Local != null && !range.Local.Intent.Parked && !UI.CursorMode.WantsCapture
                && !PresentationClock.BlocksInput && !(_rig != null && _rig.DebugFreeCamera))
                UI.CursorMode.Capture();
            // And the other way: a menu that closed while the free camera was on took the pointer; give it back.
            else if (_rig != null && _rig.DebugFreeCamera && UI.CursorMode.WantsCapture
                     && range != null && range.Local != null && !range.Local.Intent.Parked)
                UI.CursorMode.Release();

            var keys = UnityEngine.InputSystem.Keyboard.current;
            var eye = Camera.main;
            _looking = null;
            float best = PickCone;
            if (eye != null)
            {
                for (int i = 0; i < Figures.Length && i < HeroIds.Length; i++)
                {
                    if (Figures[i] == null) continue;
                    // About chest height on these bodies, whatever their feet are standing on.
                    Vector3 chest = Figures[i].position + Vector3.up * 0.75f;
                    float angle = Vector3.Angle(eye.transform.forward, chest - eye.transform.position);
                    if (angle < best) { best = angle; _looking = HeroIds[i]; }
                }
            }
            int classic = -1;
            if (eye != null)
            {
                // The two rows are staggered by half a space, so the figure nearest the middle of the view is the one
                // meant, whichever row it stands in.
                for (int i = 0; i < Classics.Length; i++)
                {
                    if (Classics[i].Figure == null) continue;
                    Vector3 chest = Classics[i].Figure.position + Vector3.up * 0.75f;
                    float angle = Vector3.Angle(eye.transform.forward, chest - eye.transform.position);
                    if (angle < best) { best = angle; classic = i; }
                }
                if (classic >= 0) _looking = Classics[classic].Id;
            }
            if (keys == null) return;
            // B: a chaser that never tires, and B again to send him away (owner, 2026-10-09).
            if (keys.bKey.wasPressedThisFrame) ToggleChaser(range);
            // (N switched his stamina between endless and a real taya's bar for one cut. The owner: "give the chaser
            // infinite stamina". He always has it now; `PrototypeChaser.RealStamina` is still there for a later test.)
            if (_chaser != null && _chaser.RealStamina) _chaser.RealStamina = false;
            // V: third person and back, to watch the body's own animation (owner, 2026-10-07).
            if (keys.vKey.wasPressedThisFrame)
            {
                var rig = FindAnyObjectByType<CameraSystem.CameraRig>();
                if (rig == null) Say("No camera yet.");
                else if (rig.SetDebugThirdPerson(!rig.DebugThirdPerson)) Say(rig.DebugThirdPerson
                    ? "Third person, free camera: hold RIGHT mouse to look round. G locks it over the shoulder. V for first person."
                    : "First person. V for third person.");
                else Say("Not during an emote.");
            }
            // G: the third person's lock. Locked, the camera is over the shoulder and the mouse turns the body; free,
            // the pointer is loose and the right button swings the camera (owner, 2026-10-09: "like roblox").
            if (keys.gKey.wasPressedThisFrame)
            {
                var rig = FindAnyObjectByType<CameraSystem.CameraRig>();
                if (rig == null || !rig.DebugThirdPerson) Say("G locks the third-person camera. Press V for third person first.");
                else if (rig.SetDebugCameraLock(!rig.DebugCameraLocked)) Say(rig.DebugCameraLocked
                    ? "Camera LOCKED over the shoulder: the mouse turns you. G to free it."
                    : "Camera FREE: hold RIGHT mouse to look round, the keys walk relative to the camera. G to lock it.");
            }
            if (keys.f9Key.wasPressedThisFrame)
            {
                MovementRework.Enabled = !MovementRework.Enabled;
                Say("Movement rework " + (MovementRework.Enabled ? "ON: Ctrl or C crouches, crouch at a sprint slides, hold Space to keep hopping." : "OFF: the game's own movement."));
            }
            if (!keys.hKey.wasPressedThisFrame) return;
            if (_looking == null) { Say("Look at a character in the rows, then press H."); return; }
            if (range == null || range.Local == null) { Say("The Training Range is not running yet."); return; }
            if (classic >= 0)
            {
                // ⚠️ THE BODY ONLY. The Classic twelve have no kit and are not in the roster this range plays, so the
                // player keeps the hero's skills and wears this person's model and clips. The first-person arms follow
                // the body by themselves (`ViewmodelArms` names the character from the model it wears).
                var body = Classics[classic];
                var visual = range.Local.GetComponent<Visual.CharacterVisual>();
                if (visual == null || body.Model == null) { Say("Could not wear " + body.Id + "."); return; }
                visual.ApplyModel(body.Model, Color.white, body.Clips, null, null);
                Say("Now wearing " + body.Id + " (their body, animations and first-person arms; the skills stay the hero's).");
                return;
            }
            var roster = Roster.GetPeople(range.Local.Mode);
            int index = -1;
            for (int i = 0; i < roster.Count; i++) if (roster[i].Id == _looking) { index = i; break; }
            Say(index >= 0 && range.ChangeCharacter(index) ? "Now playing " + _looking + "." : "Could not switch to " + _looking + ".");
        }

        /// <summary>The can out of the way, and the range's cheats on.</summary>
        private void Quiet(PracticeRange range)
        {
            _quiet = true;
            range.Set(PracticeRange.Cheat.FreezeCan, true);
            // Owner, 2026-10-07: "unli and insta ability recharge". No cooldowns is the instant half; it was left off.
            range.Set(PracticeRange.Cheat.NoCooldowns, true);
            range.Set(PracticeRange.Cheat.InfiniteSkills, true);
            range.Set(PracticeRange.Cheat.FullUltimate, true);
            // ⚠️ STAMINA IS REAL HERE NOW (it was endless): the movement rework prices a hop chain in the stamina bar, and
            // with the bar refilled every step that price could not be felt. The range's pause menu can turn it back on.
            range.Set(PracticeRange.Cheat.InfiniteStamina, false);
            foreach (var can in FindObjectsByType<Lata>())
            {
                foreach (var r in can.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (var c in can.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            }
            Say("Prototype map: no can, skills always ready. H plays as the character you look at. V is third person. F9 is the movement rework.");
        }

        private CameraSystem.CameraRig _rig;
        private PrototypeChaser _chaser;
        private int _chaserCatches;

        /// <summary>
        /// The chaser wears the first body of the Classic row (Bayan, the game's own "immovable taya") and runs at the
        /// taya's run speed without ever tiring. He starts eight metres behind the player.
        /// </summary>
        private void ToggleChaser(PracticeRange range)
        {
            if (_chaser != null) { Destroy(_chaser.gameObject); _chaser = null; Say("Chaser sent away."); return; }
            if (range == null || range.Local == null) { Say("The Training Range is not running yet."); return; }
            var player = range.Local.transform;
            var go = new GameObject("Prototype Chaser");
            go.transform.SetPositionAndRotation(player.position - player.forward * 8f + Vector3.up * 0.2f, player.rotation);
            _chaser = go.AddComponent<PrototypeChaser>();
            _chaser.Target = player;
            _chaserCatches = 0;
            if (Classics.Length > 0 && Classics[0].Model != null)
            {
                var model = Instantiate(Classics[0].Model, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, Visual.CharacterVisual.PersonModelYaw, 0f);
                model.transform.localScale = Vector3.one * Visual.CharacterVisual.PersonScale;
                Visual.ToonSkin.Apply(model, Visual.ToonSkin.PersonOutlineWidth, null);
                _chaser.Model = model;
                foreach (var clip in Classics[0].Clips)
                {
                    if (clip == null) continue;
                    if (clip.name == "sprint") _chaser.Run = clip;
                    else if (clip.name == "idle") _chaser.Idle = clip;
                }
            }
            Say("Chaser: he runs at the taya's speed (" + _chaser.Speed.ToString("F1") + " m/s) and never tires. B sends him away.");
        }

        private void Say(string text) { _notice = text; _noticeLeft = 4f; Debug.Log("[PrototypeMap] " + text); }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _noticeLeft -= Time.unscaledDeltaTime * (Event.current.type == EventType.Repaint ? 1f : 0f);
            string line = _noticeLeft > 0f ? _notice : _looking != null ? "H: play as " + _looking : null;
            if (line != null) Shadowed(new Rect(0, Screen.height - 150, Screen.width, 30), line);
            if (!_details) return;
            var range = PracticeRange.Instance;
            var animator = range != null && range.Local != null ? range.Local.GetComponentInChildren<Visual.CharacterAnimator>() : null;
            var arms = FindAnyObjectByType<CameraSystem.ViewmodelArms>();
            string detail = animator == null ? "no animator" : "walk layer (clip | arm L | arm R | amount | style): " + animator.ArmSwingDiagnostics
                + "   run " + animator.GaitRunWeight.ToString("F2");
            if (arms != null) detail += "   hands: natural " + arms.NaturalArms + ", air lift " + arms.AirLift.ToString("F3");
            Shadowed(new Rect(0, 60, Screen.width, 30), detail);
            if (_chaser != null)
            {
                if (_chaser.Catches != _chaserCatches) { _chaserCatches = _chaser.Catches; Say("CAUGHT after " + _chaser.LastSurvived.ToString("F1") + " s. Average " + _chaser.AverageSurvived.ToString("F1") + " s over " + _chaserCatches + "."); }
                Shadowed(new Rect(0, 120, Screen.width, 30), "chaser (B): " + _chaser.Distance.ToString("F1") + " m behind   caught you " + _chaser.Catches + " times   stamina: endless");
                Shadowed(new Rect(0, 150, Screen.width, 30), "survived: now " + _chaser.Survived.ToString("F1") + " s   last " + _chaser.LastSurvived.ToString("F1")
                    + " s   average " + _chaser.AverageSurvived.ToString("F1") + " s   best " + _chaser.BestSurvived.ToString("F1") + " s");
            }
            var body = range != null ? range.Local : null;
            if (body != null)
                Shadowed(new Rect(0, 90, Screen.width, 30), "movement rework (F9) " + (MovementRework.Active ? "ON" : "off")
                    + "   speed " + body.ReworkFlatSpeed.ToString("F1") + " m/s"
                    + "   jump fatigue " + Mathf.RoundToInt(body.ReworkJumpFatigue * 100f) + "%"
                    + (body.ReworkSliding ? "   SLIDE" : body.ReworkCrouched ? "   crouch" : "") + (body.IsGrounded ? "" : "   air"));
        }

        private void Shadowed(Rect at, string text)
        {
            var colour = GUI.color;
            GUI.color = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(at.x + 1.5f, at.y + 1.5f, at.width, at.height), text, _style);
            GUI.color = Color.white;
            GUI.Label(at, text, _style);
            GUI.color = colour;
        }
    }
}
#endif
