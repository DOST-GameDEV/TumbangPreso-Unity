using TumbangPreso.Core;
using TumbangPreso.UI;
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
        public int HumanSeat
        {
            get
            {
                if (_allBots || GameLaunch.Spectator) return -1;
                var net = Net.NetSession.Instance;
                if (net != null && net.IsNetworked)
                {
                    if (net.IsSeatlessReferee) return -1;
                    return net.LocalSlot;
                }
                return Mathf.Clamp(GameLaunch.SoloSeat, 0, Balance.PlayerCount - 1);
            }
        }

        private RosterBook _book;
        private bool _spectating;

        /// <summary>
        /// § THE HANDLERS THIS INSTALLER PUTS ON THE `DontDestroyOnLoad` DIRECTORS.
        ///
        /// ⚠️⚠️ THEY HAVE TO BE HELD SO THEY CAN BE TAKEN OFF AGAIN, AND THE FAULT THAT MAKES
        /// THIS NOT OPTIONAL IS IN `SliceRunner.Subscribe`'s note WITH A LOG EXCERPT. The
        /// directors outlive every scene change; these three lambdas captured this scene's HUD
        /// and this scene's local seat, and a lambda anonymous enough to subscribe is anonymous
        /// enough that nothing could ever unsubscribe it. On the second match of a session the
        /// first match's copy fires FIRST, touches a destroyed MonoBehaviour and throws, and a
        /// C# event stops dead at the first exception — so every subscriber added after it,
        /// including the live one, is simply never called.
        ///
        /// That is the whole of *"WHY TF is it just stuck here when round ends"*: one dead
        /// delegate at the head of a list. Tagging would have been the next thing to go, for the
        /// same reason on the same event, because `Tagged` carries a handler of exactly this
        /// shape.
        /// </summary>
        private System.Action<int, int> _tagged;
        private System.Action<int, int> _roundVoice;
        private System.Action<int> _wonVoice;

        private void OnDestroy()
        {
            if (GameServices.Round != null && _tagged != null)
                GameServices.Round.Tagged -= _tagged;

            if (GameServices.Match != null)
            {
                if (_roundVoice != null) GameServices.Match.RoundStarted -= _roundVoice;
                if (_wonVoice != null) GameServices.Match.MatchEnded -= _wonVoice;
            }

            _tagged = null;
            _roundVoice = null;
            _wonVoice = null;
        }

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

            var bounds = GameObject.Find("Bounds");
            if (bounds != null)
            {
                float maxX = 8.6f;
                float maxZ = 13.0f;
                foreach (var col in bounds.GetComponentsInChildren<BoxCollider>())
                {
                    Vector3 c = col.transform.position + col.center;
                    if (Mathf.Abs(c.x) > 1.0f) maxX = Mathf.Max(maxX, Mathf.Abs(c.x));
                    if (Mathf.Abs(c.z) > 1.0f) maxZ = Mathf.Max(maxZ, Mathf.Abs(c.z));
                }
                AIController.PlayableHalfX = maxX;
                AIController.PlayableHalfZ = maxZ;
            }
            else
            {
                AIController.PlayableHalfX = 8.6f;
                AIController.PlayableHalfZ = 13.0f;
            }

            var lata = BuildLata();
            var seats = new CharacterMotor[Balance.PlayerCount];
            var slippers = new Slipper[Balance.PlayerCount];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                seats[slot] = BuildSeat(slot);
                slippers[slot] = BuildSlipper(slot);
            }

            // ⚠️⚠️ THE SEATS ARE REGISTERED AND GIVEN THEIR ROLES BEFORE THE HUD IS BUILT, AND
            // NOT ONLY WHEN THE ROUND STARTS. `main.gd` does both at spawn. Deferring them to
            // `SliceRunner.Begin` meant the whole ready-up window — the first thing a player
            // ever sees of a match — had no registered players and no assigned taya, so the
            // scoreboard printed "P1..P4" instead of the cast's names, the round line said
            // "TAYA: P1" whoever was actually defending, and the role objective told the taya to
            // knock the lata down. All three were visible in the report's screenshot.
            int firstDefender = MatchRules.DefenderSlotFor(1);

            GameServices.Round.Clear();
            GameServices.Round.Lata = lata;

            for (int slot = 0; slot < seats.Length; slot++)
            {
                if (seats[slot] == null) continue;

                seats[slot].IsDefender = slot == firstDefender;
                GameServices.Round.Register(seats[slot]);
            }

            // ⚠️ THE CAMERA FOLLOWS THE SEAT THE PLAYER CHOSE, not seat 0. A spectator has no
            // seat at all, so it falls back to the first one and the spectator rig takes over.
            int human = Mathf.Max(0, HumanSeat);
            BuildCameraAndHud(seats[human], lata);

            var runner = gameObject.AddComponent<SliceRunner>();
            runner.Lata = lata;
            runner.Seats = seats;
            runner.Slippers = slippers;

            // ⚠️ THE READY GATE OWNS THE ROUND START, so AutoStart must be off or the round
            // begins under the countdown and the free-roam window never happens. A headless
            // probe has nobody to press R, which is what UseReadyGate is for — leave it true
            // for anything a human will look at.
            runner.AutoStart = !UseReadyGate;

            // ⚠️⚠️ THE BOARD IS WIPED HERE, NOT AT `StartMatch`, BECAUSE THE PLAYER LOOKS AT IT
            // FIRST. `GameServices` is `DontDestroyOnLoad`, so the match and round directors
            // outlive the scene change out of a finished match — and `SliceRunner.Begin`, which
            // is what calls `StartMatch`, does not run until the countdown ends. A second match
            // therefore opened its whole free-roam window showing the FIRST match's final scores
            // and round number, with the LATA card up because the old round was still flagged
            // active, and snapped it all to zero on "GO!". Caught in the arena capture, where a
            // freshly-loaded Eskinita opens on four seats holding 900 points each.
            //
            // ⚠️ THE ROUND IS ENDED AS WELL AS THE SCORES CLEARED, and it is the half that is
            // easy to miss: `RoundActive` gates the lata card, the passive defence tick and
            // `IsTaggable`, so a stale true is a rule left running over a match that has not
            // begun.
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();

            // ⚠️⚠️ THE SEATS AND THE CAN GO IN IMMEDIATELY, NOT AT `SliceRunner.Begin`, AND THE
            // FREE-ROAM WINDOW IS WHY. `Begin` does not run until the countdown ends, so with the
            // ready gate on, `RoundDirector` knew about nobody and nothing for the whole time the
            // player was walking around: no screen-edge arrow to the can, no nameplate distance
            // fade, and every `Players`-driven query answering as though the arena were empty.
            // `main.gd` has the four on their marks and registered before it waits for R.
            //
            // ⚠️ AND IT IS NOT COSMETIC. `AiLaneTests` was passing only because a PREVIOUS test in
            // the same batch had left its seats registered on this `DontDestroyOnLoad` director —
            // it failed the moment the stale state was cleared, which is the leak doing the work
            // of the feature. `Begin` still calls `Clear` and re-registers, so this is idempotent.
            if (GameServices.Round != null)
            {
                GameServices.Round.Lata = lata;

                foreach (var seat in seats)
                    if (seat != null) GameServices.Round.Register(seat);
            }

            // ⚠️⚠️ EVERYBODY IS PLACED BEFORE THE READY WINDOW OPENS, NOT WHEN THE ROUND STARTS.
            // `main.gd` spawns the four on their role marks and THEN waits for R; the free-roam
            // window is meant to be spent walking around the arena you are about to play in.
            // Placing them only in `OnRoundStarted` left all four stacked on the world origin
            // underneath the countdown, so the opening shot was an empty street — which is
            // exactly what the report's screenshot of the Unity build shows.
            if (UseReadyGate) runner.ResetWorld(MatchRules.DefenderSlotFor(1));

            if (UseReadyGate) BuildReadyGate(seats[human], runner);

            // ⚠️⚠️ THE MATCH BED IS NOT STARTED HERE WHEN A READY GATE OWNS THE OPENING. It
            // starts on the first countdown tick instead — see `Hud.ShowCountdownTick`, which
            // carries `audio_manager.gd`'s reasoning. Starting it at load put the match music
            // under the free-roam window and under the countdown, which is a different opening
            // from the one the game was cut to. A headless probe has no gate and no countdown,
            // so it still needs the bed started here or the run is silent.
            // ⚠️⚠️ AND THE MENU BED IS CUT DEAD THE MOMENT THE ARENA EXISTS. Without this it
            // played on under the free-roam window and the countdown and was only replaced on
            // the first tick — so the title-screen track scored the opening of every match.
            // Reported here as *"the OST is supposed to change as soon as i leave the screen
            // and it enters the actual game"*, and `audio_manager.gd::_poll_scene_state()`
            // already answers it on the "match" branch with `stop_music_now()` rather than a
            // fade, under 🧑's *"pls js abruptly cut it"*.
            if (UseReadyGate) GameServices.Music?.StopNow();
            else GameServices.Music?.Play("match", GameServices.MatchTrack);

            // Scene management is intentionally game-owned rather than Netcode-owned. Tell
            // the host only after every local seat, prop, camera and HUD target exists; this
            // closes the cold-rejoin race where the connection-time snapshot arrived while
            // the client was still loading the arena and therefore had nothing to apply to.
            var liveNet = Net.NetSession.Instance;
            if (liveNet != null && liveNet.IsNetworked && !liveNet.IsHost)
                Net.MatchRpc.Instance?.RequestWorldSnapshotServerRpc();
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

                // ⚠️ THE LATA IS A HERO PROP AND WEARS THE INK OUTLINE. `_apply_toon_pass`
                // applies it to every Prop in the Godot build, and this is the object the whole
                // sport is about hitting: without a border it reads as untextured placeholder
                // geometry from throwing distance.
                Visual.ToonSkin.Apply(model, Visual.ToonSkin.PropOutlineWidth);
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

            var net = Net.NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;
            int humanSeat = HumanSeat;
            bool isLocalHuman = slot == humanSeat;
            var peerRecord = isNetworked ? net.Lobby.PeerInSeat(slot) : null;

            int pick = isLocalHuman
                ? Settings.SettingsStore.Current.SlipperPick
                : (peerRecord != null && peerRecord.SlipperPick >= 0 ? peerRecord.SlipperPick : slot);

            var art = _book != null ? _book.SlipperArt(pick) : null;

            if (art != null && art.Model != null)
            {
                var model = Instantiate(art.Model, go.transform);
                model.name = "Visual";
                StripColliders(model);

                // The other hero prop. Same reasoning as the lata, and the owner glow below
                // drives _RimStrength, which this shader is the one that actually carries.
                Visual.ToonSkin.Apply(model, Visual.ToonSkin.PropOutlineWidth);
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

            // The local player's slipper is the one that glows, and the local player is whichever
            // seat they chose. Per-peer by construction: nothing about this crosses the wire.
            s.SetOwnerGlow(isLocalHuman);

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

            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;
            motor.Mode = SceneFlow.SelectedMode;

            var net = Net.NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;
            int humanSeat = HumanSeat;
            bool isLocalHuman = slot == humanSeat;

            var peerRecord = isNetworked ? net.Lobby.PeerInSeat(slot) : null;
            bool isHumanPlayer = isLocalHuman || (peerRecord != null && !peerRecord.Spectator);

            // ⚠️ ONLY HUMAN PEERS ARE HUMANS; UNOCCUPIED SLOTS ARE BOTS.
            motor.IsBot = !isHumanPlayer;

            if (isLocalHuman)
            {
                motor.PlayerName = Settings.SettingsStore.Current.PlayerName;
                motor.CharacterIndex = Settings.SettingsStore.Current.CharacterPick >= 0
                    ? Settings.SettingsStore.Current.CharacterPick
                    : AiCharacterIndex(slot);
            }
            else if (peerRecord != null)
            {
                motor.PlayerName = peerRecord.Name;
                motor.CharacterIndex = peerRecord.CharacterPick >= 0
                    ? peerRecord.CharacterPick
                    : AiCharacterIndex(slot);
            }
            else
            {
                motor.PlayerName = "";
                motor.CharacterIndex = AiCharacterIndex(slot);
            }

            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();
            go.AddComponent<Social.EmotePlayer>();

            if (SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                var abilities = go.AddComponent<Abilities.HeroAbilitySystem>();
                var heroPeople = Roster.GetPeople(GameMode.HeroStrike);
                string heroId = (motor.CharacterIndex >= 0 && motor.CharacterIndex < heroPeople.Count)
                    ? heroPeople[motor.CharacterIndex].Id
                    : "dante";
                abilities.BindHero(heroId);
            }

            // The role ring and floating tag. Parented under the seat so it inherits position
            // but sizes itself off the capsule.
            var plateGo = new GameObject("Nameplate");
            plateGo.transform.SetParent(go.transform, false);
            plateGo.AddComponent<Visual.CharacterNameplate>();

            // ⚠️ THE VISUAL MEASURES THE MODEL AND ALIGNS IT TO THE CAPSULE FLOOR rather than
            // assuming a height, which is what lets twelve differently-authored rigs all stand
            // correctly without per-character setup. Give it the model and it does the rest.
            var visual = go.AddComponent<Visual.CharacterVisual>();

            // ⚠️ THE MODEL HANGS UNDER `Visual`, NOT UNDER THE SEAT. Nothing pointed at this
            // child, so CharacterVisual fell back to the seat itself and its floor alignment
            // moved the CharacterController along with the mesh. See SetModelRoot.
            visual.SetModelRoot(visualRoot.transform);

            var art = _book != null ? _book.PersonArt(motor.CharacterIndex, SceneFlow.SelectedMode) : null;

            if (art != null && art.Model != null)
            {
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);

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

            if (isLocalHuman)
            {
                go.AddComponent<PlayerInputReader>();
            }
            else if (!isHumanPlayer && (!isNetworked || NetAuthority.IsHost))
            {
                // Unoccupied seats run AI on the host only
                go.AddComponent<AIController>();
            }

            // ⚠️ THE AIM ARC IS ATTACHED TO THE MOTOR
            TrajectoryPreview.AttachTo(motor);

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

        private void BuildCameraAndHud(CharacterMotor local, Lata lata)
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

            // ⚠️⚠️ NOTHING IN THE PROJECT EVER SELECTED MOUSE AIM AND THE GAME WAS UNPLAYABLE
            // BECAUSE OF IT. `AimSource` defaults to MOVEMENT, `CameraRig.StepLook` returns on
            // the first line unless it is MOUSE, and `SetAimSource` had no call site at all. So
            // the mouse turned nothing: the view was frozen on whatever heading the seat spawned
            // at, the body never yawed, and W therefore walked the player along a fixed world
            // axis for the entire match. `main.gd:751` sets this on the local rig the moment a
            // single-player match starts, which is the line this restores.
            rig.SetAimSource(CameraSystem.AimSource.Mouse);

            // ⚠️⚠️ SPECTATING IS A REAL FIFTH OPTION AND NOTHING EVER SELECTED IT. The setup
            // screen's SPECTATE button writes `GameLaunch.Spectator`, `HumanSeat` correctly
            // answers -1, and then the installer fell back to seat 0 and gave the player a
            // first-person camera bolted to a bot. `SpectatorCamera` has been converted and
            // audited line by line since 2026-08-15 and had no call site at all: the camera was
            // done, nothing selected it.
            //
            // ⚠️ THE GAMEPLAY RIG IS TURNED OFF RATHER THAN LEFT RUNNING BESIDE IT. Two enabled
            // cameras render over each other, and the FPP rig would keep writing the followed
            // bot's yaw from the mouse while the spectator flew.
            _spectating = GameLaunch.Spectator;

            if (_spectating)
            {
                rig.SetActive(false);

                var specGo = new GameObject("SpectatorCamera");
                specGo.tag = "MainCamera";
                specGo.AddComponent<CameraSystem.SpectatorCamera>();
            }

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<UI.Hud>();
            hud.Bind(local);

            // The HUD STRIPS for a watcher rather than being replaced: the clock and the
            // scoreboard are facts about the match and are what somebody watching wants.
            if (_spectating) hud.EnterSpectatorMode();

            // ⚠️⚠️ THE TOASTS AND THE KNOCKDOWN FLASH WERE BUILT AND NEVER SUBSCRIBED. `hud.gd`
            // connects five signals in `_ready`; the converted HUD had the methods and nothing
            // ever called them, so the can going over, the can coming back up and a tag landing
            // all happened in complete silence on screen. These are the moments the whole round
            // is made of.
            //
            // ⚠️ AND EACH ONE IS WRITTEN FROM THE LOCAL PLAYER'S POINT OF VIEW. A defender is
            // told to reset it; an attacker is told who knocked it down; a tagged player and the
            // taya who tagged them read two different lines about the same event. A toast that
            // says the same thing to everybody is a scoreboard, not feedback.
            if (lata != null)
            {
                lata.UprightChanged += up =>
                {
                    hud.SetDownedFlash(!up);

                    if (up) { hud.ShowToast("LATA IS BACK UP", 1.2f); return; }

                    hud.ShowToast(local.IsDefender ? "LATA DOWN  ·  RESET IT"
                                                   : "LATA DOWN", 1.6f);
                };
            }

            // ⚠️ THE RESPAWN NEEDS A LINE ON SCREEN. `main.gd::_on_character_respawned` toasts
            // "OUT OF BOUNDS": falling off the world teleports you back with no animation and no
            // sound of its own, so without this the player is simply somewhere else with no
            // explanation and reads it as the game glitching.
            foreach (var plane in FindObjectsByType<KillPlane>(FindObjectsSortMode.None))
            {
                plane.CharacterRespawned.AddListener(who =>
                {
                    if (who == local) hud.ShowToast("OUT OF BOUNDS", 1.5f);
                });
            }

            if (GameServices.Round != null)
            {
                _tagged = (taya, victim) =>
                {
                    // ⚠️ THE HUD IS RE-CHECKED, NOT TRUSTED. See OnDestroy: this handler is on a
                    // `DontDestroyOnLoad` director and the object it captured belongs to a scene.
                    if (hud == null || local == null) return;

                    if (local.PlayerSlot == victim)
                        hud.ShowToast("TAGGED  ·  BACK TO THE SAFE ZONE", 2.0f);
                    else if (local.PlayerSlot == taya)
                        hud.ShowToast($"TAG  ·  {NameForSlot(victim)}", 1.4f);
                };

                GameServices.Round.Tagged += _tagged;
            }

            // The match-end board. Present from the start and listening; it shows itself when
            // MatchEnded fires.
            var resultGo = new GameObject("MatchResult");
            resultGo.AddComponent<UI.MatchResult>();

            // ⚠️ THE ANNOUNCER'S OWN CUE POINTS, wired once here rather than scattered through
            // the systems that fire them. Godot connected these inside AudioManager; the same
            // rule applies either way — every line has exactly one call site.
            if (GameServices.Match != null)
            {
                // ⚠️ THE MATCH TAKES THE MOUSE. Godot sets `MOUSE_MODE_CAPTURED` when the arena
            // loads; without it the pointer stays free over a first-person game, drifts onto a
            // second monitor mid-round, and every click goes to whatever is behind the window.
            // A spectator flies with the mouse too, so this covers both.
            UI.CursorMode.Capture();

            _roundVoice = (round, _) => GameServices.Voice?.OnRoundStarted(round);
                _wonVoice = slot => GameServices.Voice?.OnMatchWon(slot);

                GameServices.Match.RoundStarted += _roundVoice;
                GameServices.Match.MatchEnded += _wonVoice;
            }

            // The intermission card, on the same terms: it listens for the round boundary.
            var swapGo = new GameObject("RoleSwapCard");
            swapGo.AddComponent<UI.RoleSwapCard>();

            // Which unit you are driving, and what it can do right now.
            var youGo = new GameObject("YouCard");
            youGo.AddComponent<UI.YouCard>().Bind(local);

            // ⚠️⚠️ THE EMOTE WHEEL FOLLOWS WHOEVER IS BEING DRIVEN, NOT THE SEAT THIS MATCH
            // OPENED ON. It captured `local` in a lambda, so after Tab handed the player a
            // different body the wheel still emoted on the seat they had LEFT: the emote played
            // correctly, on a character somewhere else on the street, and read as *"emotes dont
            // work at all"*. `Driven()` answers the same question the switcher does, off the
            // scene, so the two cannot disagree.
            //
            // ⚠️ AND NOT AT ALL FOR A SPECTATOR. A watcher has no body (§ SpectatorCamera), so
            // wiring the wheel to a seat would let them puppet a bot's emotes from a camera
            // whose whole contract is that it writes no gameplay state.
            if (!_spectating)
            {
                var wheelGo = new GameObject("EmoteWheel");
                var wheel = wheelGo.AddComponent<UI.EmoteWheel>();
                wheel.EmoteChosen += id => Driven(local)?.GetComponent<Social.EmotePlayer>()?.Request(id);
            }

            var pauseGo = new GameObject("PauseHost");
            pauseGo.AddComponent<PauseWatcher>().Local = local;

            // ⚠️⚠️ TAB, AND NOTHING IN THE PROJECT EVER CREATED THE COMPONENT THAT READS IT.
            // 🧑 *"tab doesnt let me switch characters in single player"*, and *"some controls
            // didnt get ported"* before that. `DebugPlayerSwitcher` is a complete conversion of
            // `debug_player_switcher.gd`, its self-destruct in release builds was already fixed
            // on human instruction, and it had NO call site anywhere: no scene held one, no
            // installer added one. The key was dead because the reader did not exist, which is
            // why fixing the gate alone did not bring it back.
            //
            // ⚠️ SINGLE PLAYER ONLY, AND NOT FOR A SPECTATOR. Seizing a seat locally would
            // desync every peer's idea of who drives what, which is the switcher's own rule; and
            // a spectator's Tab already means "cycle the follow target", so installing both
            // would put two readers on one key and hand a watcher a body at the same time.
            if (!NetAuthority.IsNetworked && !_spectating)
                new GameObject("DebugPlayerSwitcher").AddComponent<DebugPlayerSwitcher>();
        }

        /// <summary>
        /// The unit the human is actually driving right now.
        ///
        /// ⚠️ DISCOVERED, NOT REMEMBERED. Tab moves the player between bodies mid-match, so any
        /// reference captured at install time is stale the moment they use it. The human's unit
        /// is exactly the one with no active AI on it, which is the same fact
        /// <see cref="DebugPlayerSwitcher.DefaultSlot"/> reads and needs no cooperation from
        /// gameplay to stay true.
        /// </summary>
        private static CharacterMotor Driven(CharacterMotor fallback)
        {
            foreach (var unit in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude))
            {
                var ai = unit.GetComponent<AIController>();
                if (ai == null || !ai.enabled) return unit;
            }

            return fallback;
        }

        /// <summary>
        /// ⚠️⚠️ THE BOTS ARE SPREAD ACROSS THE ROSTER, NOT NUMBERED OFF THE SEAT.
        /// `main.gd::AI_PERSON_SPREAD` is `[0, 3, 6, 9]`, so the three bots come from four
        /// corners of a twelve-entry cast and the street reads as four different kids. Using
        /// the seat index gave seats 0-3 the FIRST FOUR roster entries every single match, which
        /// is why the Unity build's scoreboard always read BERTO / MARING / TOTOY / INDAY while
        /// the Godot one varied.
        ///
        /// ⚠️ AND IT WALKS FORWARD ON A COLLISION rather than adding a random offset, which
        /// keeps the whole thing a pure function of the seat and the set already taken.
        ///
        /// ⚠️⚠️ THE SPREAD IS ROTATED, AND WITHOUT THAT EIGHT OF THE TWELVE PEOPLE COULD NEVER
        /// APPEAR AS A BOT AT ALL. 🧑 *"does zack even render for other characters? can bots get
        /// zach?"* — and the honest answer was no. `[0, 3, 6, 9]` against a twelve-entry cast is
        /// a fixed set of four rows, so BERTO, INDAY, TIKBOY and LOLA PACING were the entire bot
        /// cast of every match ever played, and ZACK at index 5 was unreachable unless the
        /// player picked him. That is also exactly the scoreboard in every screenshot of this
        /// build.
        ///
        /// The stride is what the spread is FOR — four corners of the cast rather than four
        /// consecutive rows — so it is kept and the offset moves instead.
        ///
        /// ⚠️⚠️ ROTATED BY THE PLAYER'S OWN PICK, NOT BY A RANDOM AND NOT BY THE ROUND. It has to
        /// stay a pure function of things every peer already agrees on, or two machines render
        /// different people into the same seat and a screenshot stops being reproducible from a
        /// bug report — the same rule `EnvColourPass` follows for the street, and the reason
        /// `Random.Range` is wrong here. The round number is not usable: seats are built ONCE,
        /// before round 1 starts, so it reads 0 at every install and rotates nothing.
        ///
        /// The pick is known to everybody, is stable for the whole match, and changes when the
        /// player changes theirs, which is what makes the rest of the cast reachable in play.
        /// </summary>
        private int AiCharacterIndex(int slot)
        {
            var people = Roster.GetPeople(SceneFlow.SelectedMode);
            int size = people.Count;
            if (size <= 0) return 0;

            // The human's own pick is taken; a bot must not wear the player's face.
            int human = HumanSeat >= 0 ? Settings.SettingsStore.Current.CharacterPick : -1;

            int rotation = human >= 0 ? human % AiPersonSpread.Length : 0;
            int start = (AiPersonSpread[slot % AiPersonSpread.Length] + rotation) % size;

            for (int step = 0; step < size; step++)
            {
                int candidate = (start + step) % size;
                if (candidate != human) return candidate;
            }

            return start;
        }

        private static readonly int[] AiPersonSpread = { 0, 3, 6, 9 };

        /// <summary>The seat's name for a toast. Falls back to the seat label when the slot is
        /// genuinely nameless, which is what a disconnect looks like before the AI takes over.</summary>
        private static string NameForSlot(int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            return who != null ? who.DisplayName() : $"P{slot + 1}";
        }
    }
}
