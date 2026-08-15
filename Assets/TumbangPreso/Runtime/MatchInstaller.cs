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
        [Tooltip("Every seat is a bot. For headless probes, which have nobody to drive one.")]
        [SerializeField] private bool _allBots;

        /// <summary>
        /// ⚠️⚠️ THE PLAYER DOES NOT ALWAYS SIT IN SEAT 0, AND ASSUMING SO BROKE THE ONE CHOICE
        /// THE SETUP SCREEN IS ABOUT. Four seat rows on that screen exist to be pressed:
        /// `GameLaunch.SoloSeat` defaults to P2 and is the whole point of the taya rotation
        /// being visible before the match starts. Hard-coding seat 0 meant picking a chair moved
        /// a label and nothing else, and the camera then followed a bot.
        ///
        /// ⚠️ AND -1 IS SPECTATING, which is a real fifth option rather than an error case. No
        /// seat is human, and the camera is the free one.
        /// </summary>
        public int HumanSeat =>
            _allBots || GameLaunch.Spectator
                ? -1
                : Mathf.Clamp(GameLaunch.SoloSeat, 0, Balance.PlayerCount - 1);

        private RosterBook _book;

        /// <summary>
        /// ⚠️ SET BEFORE LOADING AN ARENA FOR A PREVIEW. The setup screen renders the chosen map
        /// live behind its panels, and a map scene brings its whole match with it: four
        /// characters, a can, the slippers and the directors, all spawned by this component the
        /// instant the scene loads. Without this the menu had a game running behind it, with
        /// bots moving and the round timer already counting.
        /// </summary>
        public static bool PreviewOnly;

        private void Start()
        {
            // The arena was loaded to be looked at, not played. See PreviewOnly.
            if (PreviewOnly)
            {
                enabled = false;
                return;
            }

            _book = RosterBook.Load();

            // ⚠️ THE SAVED DIFFICULTY WAS BEING IGNORED. It is written by the settings panel
            // and was never read back, so every bot played at Normal no matter what the
            // player chose. Applied once here, before any seat is built.
            AIController.ApplyDifficultyFromSettings();

            var lata = BuildLata();
            var seats = new CharacterMotor[Balance.PlayerCount];
            var slippers = new Slipper[Balance.PlayerCount];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                seats[slot] = BuildSeat(slot);
                slippers[slot] = BuildSlipper(slot);
            }

            // ⚠️ THE CAMERA FOLLOWS THE SEAT THE PLAYER CHOSE, not seat 0. A spectator has no
            // seat at all, so it falls back to the first one and the spectator rig takes over.
            int human = Mathf.Max(0, HumanSeat);
            BuildCameraAndHud(seats[human]);

            var runner = gameObject.AddComponent<SliceRunner>();
            runner.Lata = lata;
            runner.Seats = seats;
            runner.Slippers = slippers;

            // ⚠️ THE READY GATE OWNS THE ROUND START, so AutoStart must be off or the round
            // begins under the countdown and the free-roam window never happens. A headless
            // probe has nobody to press R, which is what UseReadyGate is for — leave it true
            // for anything a human will look at.
            runner.AutoStart = !UseReadyGate;

            if (UseReadyGate) BuildReadyGate(seats[human], runner);

            GameServices.Music?.Play("match", GameServices.MatchTrack);
        }

        /// <summary>
        /// ⚠️ THE REAL MESH IF THERE IS ONE, A PRIMITIVE IF THERE IS NOT. The fallback is not
        /// laziness: a missing model must still produce a visible, correctly-sized object,
        /// because an invisible lata is an unplayable game while a cylinder-shaped one is
        /// merely an ugly one that still tells you exactly what is wrong.
        /// </summary>
        private Lata BuildLata()
        {
            var go = new GameObject("Lata");

            int pick = Settings.SettingsStore.Current.CanPick;
            var art = _book != null ? _book.CanArt(pick) : null;

            if (art != null && art.Model != null)
            {
                var model = Instantiate(art.Model, go.transform);
                model.name = "Visual";
                StripColliders(model);
            }
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual";
                visual.transform.SetParent(go.transform);

                // Sized to the real cans, which span 0.108 to 0.143 in radius. A round
                // placeholder would make the 0.53 m hit window feel wrong in exactly the way
                // it is meant to be measured against.
                visual.transform.localScale = new Vector3(0.26f, 0.15f, 0.26f);
                visual.transform.localPosition = new Vector3(0, 0.15f, 0);
                Destroy(visual.GetComponent<Collider>());
            }

            var lata = go.AddComponent<Lata>();
            lata.SkinIndex = pick;
            return lata;
        }

        private Slipper BuildSlipper(int slot)
        {
            var go = new GameObject($"Slipper{slot}");

            // Seat 0 wears the player's own pick; the bots get one each so the four are
            // visibly different, which is what the owner arrow and glow read against.
            int pick = slot == 0 ? Settings.SettingsStore.Current.SlipperPick : slot;
            var art = _book != null ? _book.SlipperArt(pick) : null;

            if (art != null && art.Model != null)
            {
                var model = Instantiate(art.Model, go.transform);
                model.name = "Visual";
                StripColliders(model);
            }
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(go.transform);
                visual.transform.localScale = new Vector3(0.12f, 0.045f, 0.28f);
                Destroy(visual.GetComponent<Collider>());
            }

            var s = go.AddComponent<Slipper>();
            s.OwnerSlot = slot;
            s.SkinIndex = pick;

            // Seat 0 is the local player, so seat 0's slipper is the one that glows. Per-peer
            // by construction: nothing about this crosses the wire.
            s.SetOwnerGlow(slot == 0);

            return s;
        }

        /// <summary>
        /// ⚠️ IMPORTED MESHES ARRIVE WITH COLLIDERS AND THEY MUST COME OFF. Every contact in
        /// this game is a host-side distance check, never an overlap, so a stray MeshCollider
        /// on a slipper does nothing useful and plenty harmful: it catches the CharacterController
        /// and a thrown slipper starts shoving players around the arena.
        /// </summary>
        private static void StripColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                // ⚠️⚠️ A CharacterController IS A Collider IN UNITY. Stripping colliders off a
                // seat without this check destroys the thing that moves the player, and the
                // symptom is a unit that renders perfectly and cannot walk. Never widen this
                // to a blanket destroy.
                if (c is CharacterController) continue;

                Destroy(c);
            }
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

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(go.transform);
            visualRoot.transform.localPosition = Vector3.zero;

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

            // The role ring and floating tag. Parented under the seat so it inherits position
            // but sizes itself off the capsule.
            var plateGo = new GameObject("Nameplate");
            plateGo.transform.SetParent(go.transform, false);
            plateGo.AddComponent<Visual.CharacterNameplate>();

            // ⚠️ THE VISUAL MEASURES THE MODEL AND ALIGNS IT TO THE CAPSULE FLOOR rather than
            // assuming a height, which is what lets twelve differently-authored rigs all stand
            // correctly without per-character setup. Give it the model and it does the rest.
            var visual = go.AddComponent<Visual.CharacterVisual>();
            var art = _book != null ? _book.PersonArt(motor.CharacterIndex) : null;

            if (art != null && art.Model != null)
            {
                visual.ApplyModel(art.Model, art.Tint, art.Clips);

                // Strip from the whole seat, because the visual parents the model under the
                // seat root rather than under visualRoot. The CharacterController survives by
                // the explicit check inside.
                StripColliders(go);
            }
            else
            {
                var caps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                caps.name = "Fallback";
                caps.transform.SetParent(visualRoot.transform);
                caps.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
                caps.transform.localPosition = new Vector3(0, 0.8f, 0);
                Destroy(caps.GetComponent<Collider>());
            }

            bool human = slot == HumanSeat;
            if (human) go.AddComponent<PlayerInputReader>();
            else go.AddComponent<AIController>();

            return motor;
        }

        /// <summary>Set false for headless probes: there is nobody to press READY, and the
        /// gate would hold the round open forever.</summary>
        public bool UseReadyGate = true;

        private void BuildReadyGate(CharacterMotor local, SliceRunner runner)
        {
            var gate = gameObject.AddComponent<ReadyGate>();

            gate.RoundShouldBegin += runner.Begin;

            // The ready press is readable in the world, not only on the HUD — the other
            // players can see somebody signal that they are set.
            gate.ReadyGestureRequested += who =>
                who.GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("ready");

            var hud = UnityEngine.Object.FindFirstObjectByType<UI.Hud>();
            if (hud != null)
            {
                gate.ReadyPromptChanged += hud.ShowReadyPrompt;
                gate.CountdownTick += hud.ShowCountdownTick;
                gate.CountdownHidden += hud.HideCountdown;
            }

            // ⚠️ THE ANNOUNCER SPEAKS THE SAME TICKS THE HUD DRAWS, off the one event, so the
            // spoken "GO!" cannot drift from the drawn one.
            gate.CountdownTick += tick => GameServices.Voice?.PlayCountdown(tick);

            gate.Open(local);
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

            // The match-end board. Present from the start and listening; it shows itself when
            // MatchEnded fires.
            var resultGo = new GameObject("MatchResult");
            resultGo.AddComponent<UI.MatchResult>();

            // ⚠️ THE ANNOUNCER'S OWN CUE POINTS, wired once here rather than scattered through
            // the systems that fire them. Godot connected these inside AudioManager; the same
            // rule applies either way — every line has exactly one call site.
            if (GameServices.Match != null)
            {
                GameServices.Match.RoundStarted += (round, _) =>
                    GameServices.Voice?.OnRoundStarted(round);

                GameServices.Match.MatchEnded += slot =>
                    GameServices.Voice?.OnMatchWon(slot);
            }

            // The intermission card, on the same terms: it listens for the round boundary.
            var swapGo = new GameObject("RoleSwapCard");
            swapGo.AddComponent<UI.RoleSwapCard>();

            // Which unit you are driving, and what it can do right now.
            var youGo = new GameObject("YouCard");
            youGo.AddComponent<UI.YouCard>().Bind(local);

            // The emote wheel, driven straight to the local unit's emote player.
            var wheelGo = new GameObject("EmoteWheel");
            var wheel = wheelGo.AddComponent<UI.EmoteWheel>();
            wheel.EmoteChosen += id => local.GetComponent<Social.EmotePlayer>()?.Request(id);

            var pauseGo = new GameObject("PauseHost");
            pauseGo.AddComponent<PauseWatcher>().Local = local;
        }
    }
}
