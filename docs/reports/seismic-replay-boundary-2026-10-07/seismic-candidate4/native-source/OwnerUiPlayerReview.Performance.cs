using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        [Serializable] private sealed class PerformanceAction
        {
            public string window,hero,ability,answer,role,coverage;
            public int repetition,throws,hits,ultimateStarts,statusGains;
            public bool accepted,impactRequired,impactObserved,frozeTarget,knockedTarget;
            public int actorSeat,targetSeat;
            public long presentationMatchId;
            public string recipe,firstAnswer;
            public bool preconditionsPassed,anchorObserved,recastDelivered,matchedTargetStatus,effectStateObserved;
            public bool loadedSourceObserved,ownedPowerFlightObserved,sourceImpactObserved;
            public float targetDisplacement;
            public List<string> statuses = new List<string>();
        }
        [Serializable] private sealed class PerformanceSetup
        {
            public string stage,hero,ability,role,failure,recordId,rules;
            public int actorSeat,targetSeat,registeredSeats,runnerSeats,rounds;
            public long presentationMatchId;
            public float roundSeconds,clockBefore,clockAfter;
            public bool training,canUpright,holding,ready,passed;
        }
        [Serializable] private sealed class PerformanceActions
        {
            public string scope = "Controlled first-use measurement in normal scored offline matches: real UI callbacks and InputIntent casts; actual role owners with staged targets, charge and world resets. Kit-specific consequences are named per recipe; other mechanics remain unasserted. Final result checks use separate fresh natural matches. No human-play, network-services or default-match claim.";
            public List<PerformanceAction> actions = new List<PerformanceAction>();
            public List<PerformanceSetup> preparations = new List<PerformanceSetup>();
        }
        private readonly PerformanceActions _performanceActions = new PerformanceActions();
        private CharacterMotor _performanceTarget;
        private PerformanceSetup _performanceSetup;
        private string _performanceLastNaturalRecord;
        private DriveInfo _performanceDrive;
        private float _performanceDiskCheckAt;
        private bool _performanceBinary;
        private const long PerformanceTraceBudget = 512L * 1024 * 1024;
        private const int AllocationCalibrationSize = 4096;
        private bool _allocationCounterChecked, _allocationCounterAvailable;
        private long _allocationCalibrationBytes = -1;
        private string _allocationCounterStatus = "not-calibrated";
        private byte[] _allocationCalibrationRetained;

        private static void ValidatePerformanceCaptureMode(bool binary, bool development)
        {
            if (binary && !development)
                throw new InvalidOperationException("Binary profiler capture requires a Development player. Ordinary wall-clock timing supports release players.");
        }

        private static bool AllocationCalibrationPassed(long before, long after)
            => before >= 0 && after >= before && after - before >= AllocationCalibrationSize;

        private void CalibrateAllocationCounter()
        {
            if (_allocationCounterChecked) return;
            _allocationCounterChecked = true;
            try
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                _allocationCalibrationRetained = new byte[AllocationCalibrationSize];
                _allocationCalibrationRetained[0] = 1;
                long after = GC.GetAllocatedBytesForCurrentThread();
                GC.KeepAlive(_allocationCalibrationRetained);
                _allocationCalibrationBytes = after >= before ? after - before : -1;
                _allocationCounterAvailable = AllocationCalibrationPassed(before, after);
                _allocationCounterStatus = _allocationCounterAvailable ? "calibrated-main-thread-managed-bytes" : "unavailable-no-valid-allocation-signal";
            }
            catch (Exception error) when (error is NotSupportedException || error is NotImplementedException)
            {
                _allocationCounterStatus = "unavailable-" + error.GetType().Name;
            }
            ReportAllocationCounter();
        }

        private void ReportAllocationCounter()
        {
            _report.allocationCounterAvailable = _allocationCounterAvailable;
            _report.allocationCounterStatus = _allocationCounterStatus;
            _report.allocationCalibrationBytes = _allocationCalibrationBytes;
        }

        private long ReadAllocationCounter()
        {
            if (!_allocationCounterAvailable) return -1;
            try
            {
                long current = GC.GetAllocatedBytesForCurrentThread();
                if (current >= 0) return current;
                _allocationCounterAvailable = false; _allocationCounterStatus = "unavailable-negative-counter";
                ReportAllocationCounter(); return -1;
            }
            catch (Exception error) when (error is NotSupportedException || error is NotImplementedException)
            {
                _allocationCounterAvailable = false; _allocationCounterStatus = "unavailable-" + error.GetType().Name;
                ReportAllocationCounter(); return -1;
            }
        }

        private long AllocationBytesSinceStart()
        {
            long current = ReadAllocationCounter();
            if (current < 0 || _allocatedStart < 0) return -1;
            if (current >= _allocatedStart) return current - _allocatedStart;
            _allocationCounterAvailable = false; _allocationCounterStatus = "unavailable-counter-regressed";
            ReportAllocationCounter(); return -1;
        }

        private void ConfigureMeasurementReport()
        {
            _report.measurementMode = _performanceReview ? (_performanceBinary ? "wall-clock-with-binary-profiler" : "wall-clock-no-binary-profiler") : "unity-unscaled-delta";
            _report.developmentBuild = Debug.isDebugBuild; _report.editor = Application.isEditor;
            _report.binaryProfilerRequested = _performanceReview && _performanceBinary;
            _report.profilerSupported = Profiler.supported;
            _report.buildFlagsEvidence = "Runtime development/editor/profiler flags; complete BuildOptions including ConnectWithProfiler require the matching build receipt.";
            _report.allocationScope = "Current-thread managed bytes including probe overhead; excludes worker/native/GPU allocations. -1 means unavailable or not sampled. Collection counts are raw runtime deltas; no forced collection.";
#if ENABLE_IL2CPP
            _report.scriptingBackend = "IL2CPP";
#else
            _report.scriptingBackend = "Mono";
#endif
            ReportAllocationCounter();
        }

        private bool PerformanceHasHeadroom()
        {
            if (_finished) return false;
            if (Time.realtimeSinceStartup < _performanceDiskCheckAt) return true;
            _performanceDiskCheckAt = Time.realtimeSinceStartup + 1;
            if (_performanceBinary && Directory.EnumerateFiles(_folder, "*.raw")
                    .Sum(path => new FileInfo(path).Length) >= PerformanceTraceBudget)
            {
                Finish(false, "Binary profiler capture reached its 512 MiB budget. Completed timing windows are retained; this is not a complete performance pass.");
                return false;
            }
            _performanceDrive ??= new DriveInfo(Path.GetPathRoot(_folder));
            // Leave the 4 GiB reserve plus room for the profiler's final buffered write.
            if (_performanceDrive.AvailableFreeSpace >= 5L * 1024 * 1024 * 1024) return true;
            Finish(false, "Performance capture stopped with less than 5 GiB free to preserve the 4 GiB disk reserve. Completed window evidence is retained.");
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallPerformanceReview()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-uireview");
            if (Application.isEditor || at < 0 || at + 1 >= args.Length || !args.Contains("-tp-performance-only") || args.Contains("-tp-tournament")) return;
            var owner = new GameObject("~OwnerUiPlayerReview"); DontDestroyOnLoad(owner);
            var probe = owner.AddComponent<OwnerUiPlayerReview>();
            probe._folder = Path.GetFullPath(args[at + 1]);
            Directory.CreateDirectory(probe._folder);
            probe._performanceReview = probe._measureMenus = true;
            probe._performanceBinary = args.Contains("-tp-performance-binary");
            probe._deadline = Time.realtimeSinceStartup + 1500;
            try
            {
                probe.StartFrameWindow("00-boot-to-title");
                probe.StartCoroutine(probe.Guard(probe.Walk()));
            }
            catch (Exception error) { probe.Finish(false, error.ToString()); }
        }

        private void BeginPerformanceProfile(string name)
        {
            ValidatePerformanceCaptureMode(_performanceBinary, Debug.isDebugBuild);
            if (_performanceBinary && !Profiler.supported)
                throw new InvalidOperationException("Binary profiler capture is not supported by this player.");
            // Routine timing keeps CSV evidence without the profiler's collection cost.
            // Large binary traces require an explicit, separately budgeted diagnostic.
            if (Profiler.supported)
            {
                Profiler.enabled = false;
                Profiler.enableBinaryLog = false;
            }
            if (!_performanceBinary) return;
            Profiler.logFile = Path.Combine(_folder, name + ".raw");
            Profiler.enableBinaryLog = true;
            Profiler.SetAreaEnabled(ProfilerArea.CPU, true);
            Profiler.SetAreaEnabled(ProfilerArea.Rendering, true);
            Profiler.SetAreaEnabled(ProfilerArea.Memory, true);
            Profiler.SetAreaEnabled(ProfilerArea.GPU, true);
            Profiler.enabled = true;
        }

        private IEnumerator MeasurePerformance(string name, IEnumerator work)
        {
            Stage(name);
            StartFrameWindow(name);
            try { yield return work; }
            finally { StopFrameWindow(); }
        }

        private IEnumerator PerformanceOnly()
        {
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 150);
            StopFrameWindow();
            yield return MeasurePerformance("01-title-to-home", PerformanceHomeEntry());
            yield return MeasurePerformance("02-home-idle", PerformanceIdle(3));

            foreach (string door in new[] { "AvatarButton", "HeroButton", "LoadoutButton", "ShopButton", "TaskButton", "MenuButton", "ModeCard" })
            {
                yield return MeasurePerformance("menu-" + door, PerformanceDoor(door));
                TumpHub.Current.Home(); yield return null;
                if (door == "HeroButton")
                {
                    yield return MeasurePerformance("menu-HeroButton-repeat", PerformanceDoor(door));
                    TumpHub.Current.Home(); yield return null;
                }
            }
            yield return MeasurePerformance("menu-profile", PerformanceOverlay("NamePlate", "ClosePlayerHub"));
            yield return MeasurePerformance("menu-settings", PerformanceSettings());
            yield return MeasurePerformance("menu-hero-selection", PerformanceSelection());
            if (Environment.GetCommandLineArgs().Contains("-tp-performance-menus-only"))
            {
                Stage("menu-only performance capture complete; gameplay and results not exercised");
                yield break;
            }
            yield return MeasurePerformance("lobby-local-host", PerformanceLobby());

            HubHome.Choice = 1;
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
            SceneFlow.PinSelectedRules(rules);
            yield return MeasurePerformance("classic-match-entry-and-round-start", PerformanceScoredEntry());
            yield return MeasurePerformance("classic-first-throw-and-hit", PerformanceThrow(PerformanceActor(), "classic-first-throw-and-hit"));
            yield return MeasurePerformance("classic-return-to-home", PerformanceReturnHome());

            HubHome.Choice = 2;
            rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
            SceneFlow.PinSelectedRules(rules);
            yield return MeasurePerformance("hero-match-entry-and-round-start", PerformanceScoredEntry());
            yield return MeasurePerformance("first-throw-and-hit", PerformanceThrow(PerformanceActor()));

            foreach (string hero in ReviewHeroes())
            {
                yield return MeasurePerformance(hero + "-first-model-and-kit", PerformanceHero(PerformanceActor(), hero));
                // A role kit has a shared signature plus two different second skills.
                foreach (int slot in new[] { 0, 1, 2, 3 })
                {
                    var local = PerformanceActor(slot == 2);
                    if (local.AbilitySystem.HeroId != hero)
                        yield return MeasurePerformance(hero + "-" + slot + "-role-model-and-kit", PerformanceHero(local, hero));
                    var ability = PerformanceAbility(local, slot);
                    if (ability is PlaceholderRoleAbility) continue;
                    for (int repetition = 1; repetition <= 2; repetition++)
                    {
                        // Overclock and any other match-persistent ultimate cannot
                        // legally cast twice in one match. Keep assets warm but
                        // establish a new actual match for the second activation.
                        if (repetition > 1 && slot == 3 && ability.IsPersistentActive)
                        {
                            yield return MeasurePerformance(hero + "-persistent-return", PerformanceReturnHome());
                            HubHome.Choice = 2;
                            rules = CustomGameRules.Defaults(GameMode.HeroStrike);
                            rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
                            rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
                            SceneFlow.PinSelectedRules(rules);
                            yield return MeasurePerformance(hero + "-persistent-fresh-match", PerformanceScoredEntry());
                            local = PerformanceActor();
                            yield return MeasurePerformance(hero + "-persistent-rebind", PerformanceHero(local, hero));
                            ability = PerformanceAbility(local, slot);
                        }
                        yield return MeasurePerformance(hero + "-" + slot + "-prepare-" + repetition,
                            PerformancePrepare(local, hero, slot));
                        var liveAbility = PerformanceAbility(local, slot);
                        if (local.IsDefender != (slot == 2) || liveAbility.Id != ability.Id)
                            throw new InvalidOperationException(hero + "/" + slot + " changed role or ability during preparation.");
                        if (slot == 1 && hero == "amihan" && local.HoldingSlipper && local.IsInsideBox())
                            throw new InvalidOperationException("Updraft preparation left the held slipper inside the box.");
                        string name = hero + "-" + ability.Id + "-cast-" + repetition;
                        yield return MeasurePerformance(name, PerformanceCast(local, hero, slot, repetition, name));
                    }
                }
            }

            yield return MeasurePerformance("return-to-home", PerformanceReturnHome());
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                HubHome.Choice = mode == GameMode.Classic ? 1 : 2;
                rules = CustomGameRules.Defaults(mode);
                rules.Rounds = 1; rules.RoundSeconds = CustomGameRules.MinRoundSeconds;
                rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
                SceneFlow.PinSelectedRules(rules);
                yield return MeasurePerformance(mode + "-result-match-entry", PerformanceScoredEntry());
                foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
                foreach (var actor in GameServices.Round.Players)
                {
                    actor.IsBot = true; actor.Intent.Parked = false;
                    (actor.GetComponent<AIController>() ?? actor.gameObject.AddComponent<AIController>()).enabled = true;
                }
                yield return MeasurePerformance(mode + "-natural-custom-round-and-match-end", PerformanceResult());
                if (mode == GameMode.Classic) yield return MeasurePerformance("classic-result-return", PerformanceReturnHome());
            }
            File.WriteAllText(Path.Combine(_folder, "performance-actions.json"), JsonUtility.ToJson(_performanceActions, true));
            if (_performanceActions.actions.Any(a => !a.accepted || a.impactRequired && !a.impactObserved))
                throw new InvalidOperationException("Some first-use actions were refused or missed their required consequence; see performance-actions.json. Their timings do not qualify those effects.");
            Stage("general boot and first-use performance route complete");
        }

        private static IEnumerator PerformanceIdle(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        private IEnumerator PerformanceHomeEntry()
        {
            Settings.SettingsStore.Current.Fullscreen = false;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1920 && Screen.height == 1080, 8);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            // Login already opens the existing preparation view and then Home.
            // Observe that arrival rather than click its retired title action.
            yield return WaitFor(() => TumpHub.Current != null && TumpHub.Current.ShowingHome, 45);
            yield return new WaitForSecondsRealtime(1);
        }

        private IEnumerator PerformanceDoor(string name)
        {
            yield return Click(name);
            yield return new WaitForSecondsRealtime(1);
        }

        private IEnumerator PerformanceOverlay(string open, string close)
        {
            yield return Click(open); yield return new WaitForSecondsRealtime(1);
            yield return Click(close); yield return new WaitForSecondsRealtime(.3f);
        }

        private IEnumerator PerformanceSettings()
        {
            TumpHub.Current.Host.OpenSettings();
            yield return WaitFor(() => Find("TumpSettingsBack") != null);
            yield return new WaitForSecondsRealtime(1);
            yield return Click("TumpSettingsBack");
        }

        private IEnumerator PerformanceSelection()
        {
            HubHome.Choice = 2;
            TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = false);
            yield return new WaitForSecondsRealtime(1);
            TumpHub.Current.Home(); yield return null;
        }

        private IEnumerator PerformanceLobby()
        {
            var hub = TumpHub.Current;
            var task = hub.Host.HostRoom("Performance review", SceneFlow.SelectedMap, GameMode.HeroStrike, RoomVisibility.Private, false);
            yield return WaitFor(() => task.IsCompleted, 10);
            if (task.IsFaulted) throw task.Exception;
            if (!string.IsNullOrEmpty(task.Result)) throw new InvalidOperationException(task.Result);
            hub.ShowLobby(); yield return new WaitForSecondsRealtime(2);
            hub.Host.LeaveRoom(); hub.Home(); yield return new WaitForSecondsRealtime(.4f);
        }

        private void PerformanceRequire(bool condition, string failure)
        {
            if (condition) return;
            if (_performanceSetup != null) _performanceSetup.failure = failure;
            throw new InvalidOperationException("Performance fixture: " + failure);
        }

        private CharacterMotor[] PerformanceWorld()
        {
            var round = GameServices.Round; var match = GameServices.Match;
            var runner = Object.FindAnyObjectByType<SliceRunner>();
            var players = round != null ? round.Players.ToArray() : Array.Empty<CharacterMotor>();
            if (_performanceSetup != null)
            {
                _performanceSetup.training = GameLaunch.TrainingRange || PracticeRange.Requested || PracticeRange.Active;
                _performanceSetup.registeredSeats = players.Length;
                _performanceSetup.runnerSeats = runner?.Seats?.Count(p => p != null && p.gameObject.activeInHierarchy) ?? 0;
                _performanceSetup.presentationMatchId = match?.PresentationMatchId ?? 0;
                _performanceSetup.roundSeconds = SceneFlow.SelectedRoundSeconds;
                _performanceSetup.rounds = SceneFlow.SelectedRules.Rounds;
            }
            PerformanceRequire(!GameLaunch.TrainingRange && !PracticeRange.Requested && !PracticeRange.Active && !GameLaunch.GuidedTutorial,
                "The measured world must be scored local play, not training.");
            PerformanceRequire(!SceneFlow.Networked && !NetAuthority.IsNetworked, "The controlled scenario must remain offline.");
            PerformanceRequire(match != null && match.MatchInProgress && round != null && round.RoundActive,
                "The actual match and round must be live.");
            PerformanceRequire(GameServices.Stats != null, "The real match statistics owner is missing.");
            PerformanceRequire(players.Length == Balance.PlayerCount && players.All(p => p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy)
                && players.Select(p => p.PlayerSlot).Distinct().Count() == Balance.PlayerCount,
                "Expected four unique active registered players.");
            PerformanceRequire(runner != null && runner.Seats != null && runner.Seats.Length == Balance.PlayerCount
                && players.All(p => p.PlayerSlot >= 0 && p.PlayerSlot < runner.Seats.Length && runner.Seats[p.PlayerSlot] == p),
                "Runner seats must match all four registered players.");
            PerformanceRequire(players.Count(p => p.IsDefender) == 1 && round.PlayerAt(match.DefenderSlot)?.IsDefender == true,
                "Roles must agree with the match's derived defender.");
            PerformanceRequire(players.All(p => p.Mode == SceneFlow.SelectedMode), "Player mode differs from the requested mode.");
            return players;
        }

        private IEnumerator PerformanceScoredEntry()
        {
            long previous = GameServices.Match?.PresentationMatchId ?? 0;
            string requestedRules = CustomGameRules.ToWire(SceneFlow.SelectedRules), requestedMap = SceneFlow.SelectedMap;
            GameMode requestedMode = SceneFlow.SelectedMode;
            _performanceSetup = new PerformanceSetup { stage = "normal-scored-entry", rules = requestedRules };
            _performanceActions.preparations.Add(_performanceSetup);
            var host = TumpHub.Current.Host;
            host.LeaveRoom(); GameLaunch.Reset(); GameLaunch.AllBots = false; GameLaunch.SoloSeat = 1;
            host.StartGame();
            yield return WaitFor(() => Hud.Instance != null && !HubLoading.Visible, 90);
            // ReadyGate owns the initial match start; never Begin a second match.
            yield return StartReadyRound();
            PerformanceWorld();
            PerformanceRequire(CustomGameRules.ToWire(SceneFlow.SelectedRules) == requestedRules && SceneFlow.SelectedMode == requestedMode
                && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == requestedMap
                && GameServices.Match.TotalRounds == SceneFlow.SelectedRules.Rounds,
                "Normal entry changed the requested map, mode or pinned rules.");
            PerformanceRequire(GameServices.Match.PresentationMatchId != previous && GameServices.Match.RoundNumber == 1
                && GameServices.Stats?.Last == null, "Entry reused a prior match or completed record.");
            _performanceSetup.clockBefore = GameServices.Round.TimeLeft;
            yield return new WaitForSeconds(.25f);
            _performanceSetup.clockAfter = GameServices.Round.TimeLeft;
            PerformanceRequire(_performanceSetup.clockAfter < _performanceSetup.clockBefore,
                "A scored round's clock must decrease naturally.");
            _performanceSetup.passed = true;
        }

        private CharacterMotor PerformanceActor(bool defending = false)
        {
            var players = PerformanceWorld();
            var actor = defending ? GameServices.Round.PlayerAt(GameServices.Match.DefenderSlot)
                : players.First(p => !p.IsDefender);
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
            foreach (var person in players) person.IsBot = true;
            var rig = Camera.main.GetComponent<CameraRig>();
            rig.Follow(actor, true); rig.SetAimSource(AimSource.Movement);
            return actor;
        }

        private static HeroAbility PerformanceAbility(CharacterMotor actor, int slot)
            => slot == 0 ? actor.AbilitySystem.Kit.Skill1 : slot == 3 ? actor.AbilitySystem.Kit.Ultimate : actor.AbilitySystem.Kit.Skill2;

        private static Vector3 PerformanceBodyCentre(CharacterMotor actor)
            => actor.transform.position + (actor.GetComponent<CharacterController>()?.center ?? Vector3.up * .8f);

        private IEnumerator PerformanceHero(CharacterMotor actor, string hero)
        {
            PerformanceWorld();
            actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            var art = RosterBook.Load().FindPersonArt(hero);
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            actor.AbilitySystem.BindHero(hero);
            Hud.Instance.Bind(actor); Hud.Instance.ShowReadyPrompt(false);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private IEnumerator PerformancePrepare(CharacterMotor actor, string hero, int slot)
        {
            _performanceSetup = new PerformanceSetup { stage = "action-prepare", hero = hero, actorSeat = actor.PlayerSlot,
                role = actor.IsDefender ? "defending" : "attacking" };
            _performanceActions.preparations.Add(_performanceSetup);
            var players = PerformanceWorld(); var round = GameServices.Round;
            long identity = GameServices.Match.PresentationMatchId;
            int roundNumber = GameServices.Match.RoundNumber;
            var scores = players.Select(p => GameServices.Match.ScoreFor(p.PlayerSlot)).ToArray();
            PerformanceRequire(actor.IsDefender == (slot == 2), "Choose the actual role owner instead of overriding a single actor's role.");
            foreach (var person in players)
            {
                person.GetComponent<Carrier>()?.CancelPendingInput();
                person.GetComponent<CombatVerbs>()?.RetireActions();
                person.GetComponent<AIController>()?.RetirePendingInput();
            }
            round.EndRound();
            Object.FindAnyObjectByType<SliceRunner>().ResetWorld(GameServices.Match.DefenderSlot);
            round.BeginRound();
            _performanceTarget = players.First(p => p != actor && p.IsDefender != actor.IsDefender);
            _performanceSetup.targetSeat = _performanceTarget.PlayerSlot;
            var shoes = Object.FindAnyObjectByType<SliceRunner>().Slippers;
            foreach (var person in players)
            {
                person.ClearStatuses(); person.Stamina.RefillAndClearFatigue();
                person.Intent.Clear(); person.Intent.Parked = person != actor;
                var shoe = shoes.Single(s => s != null && s.SeatOfOrigin == person.PlayerSlot);
                if (person.IsDefender)
                {
                    person.GetComponent<Carrier>()?.Held?.HostDisarm();
                    shoe.HostDisarm(); shoe.OwnerSlot = -1; shoe.gameObject.SetActive(false);
                }
                else { shoe.gameObject.SetActive(true); shoe.HostForceEquip(person); }
                if (person != actor) person.Teleport(person == _performanceTarget
                    ? new Vector3(0, .12f, -2) : new Vector3(person.PlayerSlot % 2 == 0 ? 4 : -4, .12f, -2));
                MatchHost.SeatOnFloor(person);
            }
            actor.Teleport(new Vector3(0, .12f, slot == 2 ? -4 : -9)); actor.transform.rotation = Quaternion.identity;
            MatchHost.SeatOnFloor(actor);
            bool bodyAim = slot == 1 && (hero == "dante" || hero == "sean" || hero == "cheska" || hero == "zack")
                || hero == "zack" && slot == 2 || hero == "phaister" && (slot == 1 || slot == 2);
            actor.Intent.AimPoint = bodyAim ? PerformanceBodyCentre(_performanceTarget) : _performanceTarget.transform.position;
            actor.Intent.FaceAimPoint = true;
            if (hero == "nemu" && slot == 1)
            {
                var owned = actor.GetComponent<Carrier>().Held;
                owned.HostDisarm(); owned.transform.position = new Vector3(1, owned.RestHeight, -3);
            }
            if (slot == 3) actor.AbilitySystem.Kit.AddUltimateCharge(100);
            PerformanceRequire(GameServices.Match.PresentationMatchId == identity && GameServices.Match.RoundNumber == roundNumber
                && scores.SequenceEqual(players.Select(p => GameServices.Match.ScoreFor(p.PlayerSlot))) && GameServices.Stats.Last == null,
                "Staged world reset changed match identity, round, score or final record.");
            yield return new WaitForSecondsRealtime(.5f);
            PerformanceWorld();
            _performanceSetup.canUpright = round.Lata != null && round.Lata.IsUpright;
            _performanceSetup.holding = actor.GetComponent<Carrier>()?.Held != null;
            _performanceSetup.ready = actor.CanAct();
            PerformanceRequire(_performanceSetup.canUpright && actor.CanAct(), "The clean case needs an upright can and an actionable actor.");
            if (!string.IsNullOrEmpty(hero))
            {
                var ability = PerformanceAbility(actor, slot);
                var context = new AbilityContext(actor, actor.GetComponent<Carrier>(), actor.GetComponent<CombatVerbs>());
                _performanceSetup.ability = ability.Id;
                _performanceSetup.ready = ability.IsReady && ability.CanActivate(context);
                PerformanceRequire(actor.AbilitySystem.HeroId == hero && actor.IsDefender == (slot == 2) && _performanceSetup.ready,
                    hero + "/" + ability.Id + " does not satisfy its actual role/resource/target preconditions.");
            }
            _performanceSetup.passed = true;
        }

        private IEnumerator PerformancePress(CharacterMotor actor, Verb verb, float seconds, Action observe = null)
        {
            actor.Intent.Set(verb, false);
            yield return null;
            actor.Intent.Set(verb, true); actor.Intent.BufferPress(verb);
            float began = Time.time;
            try
            {
                // At least one full consumer frame sees this press even when a
                // first-use stall jumps over a wall-time input interval.
                do { observe?.Invoke(); yield return null; }
                while (!PresentationClock.BlocksInput && Time.time - began < seconds);
            }
            finally { actor.Intent.Set(verb, false); }
            yield return null; observe?.Invoke();
        }

        private IEnumerator PerformanceObserveUntil(Func<bool> complete, float seconds, Action observe)
        {
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                observe();
                if (complete()) yield break;
                yield return null;
            } while (Time.realtimeSinceStartup < until);
            observe();
        }

        private static string PerformanceRecipe(string hero, int slot)
        {
            if (slot == 1 && hero == "dante") return "imbued-throw-selected-target-concussed";
            if (slot == 1 && hero == "cheska") return "imbued-throw-selected-target-frozen";
            if (slot == 1 && hero == "sean") return "empowered-throw-impact-and-selected-target-nudge";
            if (slot == 1 && hero == "zack") return "held-shoe-bank-load";
            if (slot == 2 && hero == "zack") return "visible-capsule-lock-selected-target-zapped";
            if (slot == 0 && hero == "nemu") return "actual-kuro-anchor-and-recast-teleport";
            if (slot == 1 && hero == "nemu") return "own-loose-shoe-delivered";
            if (slot == 2 && hero == "nemu") return "upright-can-kuro-protection";
            if (slot == 1 && hero == "phaister") return "reach-mark-delay-selected-target-drained";
            if (slot == 2 && hero == "phaister") return "reach-mark-arm-recast-selected-target-hexed";
            if (slot == 2 && hero == "amihan") return "gale-selected-target-whirled";
            if (slot == 3 && hero == "paete") return "owned-sentry-selected-target-rooted";
            return "accepted-cast";
        }

        private IEnumerator PerformanceCast(CharacterMotor actor, string hero, int slot, int repetition, string name)
        {
            var system = actor.AbilitySystem;
            var ability = PerformanceAbility(actor, slot);
            var verb = slot == 0 ? Verb.Skill1 : slot == 3 ? Verb.Ultimate : Verb.Skill2;
            var answerSlot = slot == 0 ? HeroAbilitySystem.Slot.Skill1 : slot == 3 ? HeroAbilitySystem.Slot.Ultimate : HeroAbilitySystem.Slot.Skill2;
            var target = _performanceTarget;
            string recipe = PerformanceRecipe(hero, slot);
            var receipt = new PerformanceAction { window = name, hero = hero, ability = ability.Id, repetition = repetition,
                role = actor.IsDefender ? "defending" : "attacking",
                actorSeat = actor.PlayerSlot, targetSeat = target.PlayerSlot, recipe = recipe,
                presentationMatchId = GameServices.Match.PresentationMatchId, preconditionsPassed = _performanceSetup?.passed == true,
                impactRequired = recipe != "accepted-cast" };
            int roundNumber = GameServices.Match.RoundNumber;
            float previousAnswerAt = Time.time - system.SecondsSinceAnswer(answerSlot);
            bool anchorReady = false, recalled = false;
            Vector3 anchor = Vector3.zero, beforeRecall = Vector3.zero;
            Vector3 targetBefore = target.transform.position;
            PaeteSentry ownedSentry = null; float nextSentryScan = 0;
            var ownedShoe = Object.FindAnyObjectByType<SliceRunner>().Slippers.Single(s => s != null && s.SeatOfOrigin == actor.PlayerSlot);
            void Observe()
            {
                if (Time.time - system.SecondsSinceAnswer(answerSlot) > previousAnswerAt
                    && system.LastAnswer(answerSlot) == HeroKit.CastOutcome.Cast) receipt.accepted = true;
                receipt.frozeTarget |= target.IsFrozen; receipt.knockedTarget |= target.IsTripped;
                if (hero == "dante" && slot == 1) receipt.matchedTargetStatus |= target.IsConcussed;
                else if (hero == "cheska" && slot == 1) receipt.matchedTargetStatus |= target.IsFrozen;
                else if (hero == "sean" && slot == 1)
                {
                    receipt.loadedSourceObserved |= ownedShoe.Holder == actor && ownedShoe.Affinity == SlipperAffinity.FireExplosive;
                    receipt.ownedPowerFlightObserved |= ownedShoe.State == SlipperState.InFlight
                        && ownedShoe.ThrowerSlot == actor.PlayerSlot && ownedShoe.Affinity == SlipperAffinity.FireExplosive;
                    // A blocked contact clears thrower credit in the same physics
                    // step that spends the power. Retain the observed owned flight,
                    // then require its matched contact and consumed affinity.
                    receipt.sourceImpactObserved |= receipt.loadedSourceObserved && receipt.ownedPowerFlightObserved
                        && receipt.throws > 0 && receipt.hits > 0 && ownedShoe.Affinity == SlipperAffinity.Normal;
                    Vector3 moved = target.transform.position - targetBefore; moved.y = 0;
                    receipt.targetDisplacement = Mathf.Max(receipt.targetDisplacement, moved.magnitude);
                    // Empowered Throw deliberately nudges rather than applying a
                    // burn/stun. Preserve its actual owned impact and movement.
                    receipt.matchedTargetStatus |= receipt.sourceImpactObserved && receipt.targetDisplacement > .02f;
                }
                else if (hero == "zack" && slot == 2) receipt.matchedTargetStatus |= target.IsZapped;
                else if (hero == "phaister" && slot == 1) receipt.matchedTargetStatus |= target.IsDrained;
                else if (hero == "phaister" && slot == 2) receipt.matchedTargetStatus |= target.IsHexed;
                else if (hero == "amihan" && slot == 2) receipt.matchedTargetStatus |= target.IsWhirled;
                else if (hero == "paete" && slot == 3 && target.IsRooted)
                {
                    if (ownedSentry == null && Time.unscaledTime >= nextSentryScan)
                    {
                        nextSentryScan = Time.unscaledTime + .1f;
                        ownedSentry = Object.FindObjectsByType<PaeteSentry>().FirstOrDefault(s => s.OwnerSlot == actor.PlayerSlot);
                    }
                    receipt.matchedTargetStatus |= ownedSentry != null;
                }
                if (hero == "zack" && slot == 1)
                    receipt.effectStateObserved |= (system.Kit as ZackHeroKit)?.IsOverchargeThrowActive == true;
                if (hero == "nemu" && slot == 0 && ability is IPreparedWorldReplication prepared
                    && prepared.CapturePreparedWorld(out var point, out _, out _))
                {
                    anchor = point;
                    var pet = actor.GetComponent<CharacterVisual>()?.Companion;
                    anchorReady |= pet != null && Vector3.Distance(pet.transform.position, anchor) < 2;
                    receipt.anchorObserved |= anchorReady;
                }
                if (recalled) receipt.effectStateObserved |= Vector3.Distance(actor.transform.position, beforeRecall) > 1
                    && Vector3.Distance(actor.transform.position, anchor) < .5f;
                if (hero == "nemu" && slot == 1)
                    receipt.effectStateObserved |= ownedShoe.State == SlipperState.Loose
                        && Vector3.Distance(ownedShoe.transform.position, actor.transform.position) < Balance.InteractionRadius * 1.5f;
                if (hero == "nemu" && slot == 2 && ability is IPreparedWorldReplication guard
                    && guard.CapturePreparedWorld(out _, out _, out _))
                    receipt.effectStateObserved |= GameServices.Round.Lata.IsUpright && GameServices.Round.Lata.IsProtected;
            }
            void Outcome(MatchFlair.Kind kind, int source, int subject, Vector3 at, float strength)
            {
                if (source != actor.PlayerSlot) return;
                if (kind == MatchFlair.Kind.Throw) receipt.throws++;
                else if (subject == target.PlayerSlot && (kind == MatchFlair.Kind.Tag || kind == MatchFlair.Kind.Block)) receipt.hits++;
            }
            void Ultimate(CharacterMotor who, HeroKit kit, HeroAbility power) { if (who == actor) receipt.ultimateStarts++; }
            void Status(CharacterMotor who, StatusKind kind)
            {
                if (who != target) return;
                receipt.statusGains++;
                string statusName = kind.ToString();
                if (!receipt.statuses.Contains(statusName)) receipt.statuses.Add(statusName);
                Observe();
            }
            MatchFlair.Presented += Outcome; HeroAbilitySystem.UltimateStarted += Ultimate;
            target.StatusGained += Status;
            try
            {
                PerformanceRequire(receipt.preconditionsPassed && actor.CanAct(), "Cast began without a valid recorded preparation.");
                yield return PerformancePress(actor, verb, ability.HoldToAim ? ability.AimRampSeconds + .1f : .12f, Observe);
                receipt.firstAnswer = system.LastAnswer(answerSlot).ToString();
                yield return PerformanceObserveUntil(() => slot == 3 ? receipt.ultimateStarts == 1 && !PresentationClock.Held
                    : receipt.accepted && !ability.IsWindingUp, Mathf.Max(15, ability.Windup + 10), Observe);
                if (slot == 3) receipt.accepted = receipt.ultimateStarts == 1;
                if (receipt.accepted && slot == 1 && (hero == "dante" || hero == "sean" || hero == "cheska"))
                {
                    PerformanceRequire(GameServices.Round.CanThrow(actor), "An imbue case must perform a legal normal throw.");
                    actor.Intent.AimPoint = PerformanceBodyCentre(target);
                    yield return PerformancePress(actor, Verb.SpecialAbility, Balance.ChargeFullTime + .1f, Observe);
                }
                if (receipt.accepted && hero == "phaister" && slot == 2)
                {
                    yield return PerformanceObserveUntil(() => ability.ReactivateReady, VoodooRules.ReachSeconds + VoodooRules.HexArmSeconds + 8, Observe);
                    if (ability.ReactivateReady)
                    { yield return PerformancePress(actor, verb, .12f, Observe); receipt.recastDelivered = true; }
                }
                if (receipt.accepted && hero == "nemu" && slot == 0)
                {
                    yield return PerformanceObserveUntil(() => anchorReady && ability.ReactivateReady, 6, Observe);
                    if (anchorReady && ability.ReactivateReady)
                    {
                        beforeRecall = actor.transform.position; recalled = true;
                        yield return PerformancePress(actor, verb, .12f, Observe); receipt.recastDelivered = true;
                    }
                }
                // Do not end a successful first-use window before its authored
                // consequence. Non-impact casts still retain a presentation tail.
                float tail = Mathf.Clamp(ability.Duration + ability.Windup + 2, 4, 12);
                float tailAt = Time.time + tail;
                yield return PerformanceObserveUntil(() => Time.time >= tailAt, tail + 15, Observe);
                receipt.impactObserved = hero == "nemu" && slot == 0
                    ? receipt.anchorObserved && receipt.recastDelivered && receipt.effectStateObserved
                    : receipt.matchedTargetStatus || receipt.effectStateObserved;
                receipt.coverage = receipt.impactRequired ? (receipt.impactObserved ? "cast and matched recipe consequence" : "cast only; required consequence absent")
                    : "accepted cast; additional mechanics not asserted";
                receipt.answer = system.LastAnswer(answerSlot).ToString();
            }
            finally
            {
                MatchFlair.Presented -= Outcome; HeroAbilitySystem.UltimateStarted -= Ultimate;
                if (target != null) target.StatusGained -= Status;
                actor.Intent.Clear(); _performanceActions.actions.Add(receipt);
            }
            PerformanceRequire(GameServices.Match.PresentationMatchId == receipt.presentationMatchId && GameServices.Match.RoundNumber == roundNumber
                && GameServices.Match.MatchInProgress && GameServices.Stats.Last == null, "The controlled cast crossed a match/round/result boundary.");
        }

        private IEnumerator PerformanceThrow(CharacterMotor actor, string label = "first-throw-and-hit")
        {
            yield return PerformancePrepare(actor, "", 0);
            var round = GameServices.Round; var carrier = actor.GetComponent<Carrier>();
            var receipt = new PerformanceAction { window = label, hero = actor.AbilitySystem?.HeroId ?? "classic", ability = "throw", repetition = 1,
                actorSeat = actor.PlayerSlot, targetSeat = -1, presentationMatchId = GameServices.Match.PresentationMatchId,
                recipe = "ordinary-owned-shoe-throw-can-contact", preconditionsPassed = _performanceSetup?.passed == true };
            actor.Teleport(new Vector3(0, .12f, -10));
            actor.Intent.AimPoint = round.Lata.transform.position + Vector3.up * .18f;
            // The first throw has a clear can lane; later power cases put a body on it.
            foreach (var other in round.Players) if (other != actor) other.Teleport(new Vector3(4, .12f, -2 + other.PlayerSlot));
            yield return WaitFor(() => round.CanThrow(actor), 5);
            var shoe = carrier.Held;
            int serial = round.Lata.HostKnockdownSerial;
            yield return PerformancePress(actor, Verb.SpecialAbility, Balance.ChargeFullTime + .1f);
            yield return new WaitForSecondsRealtime(3);
            receipt.throws = carrier.Held != shoe ? 1 : 0;
            receipt.hits = round.Lata.HostKnockdownSerial - serial;
            receipt.accepted = receipt.throws == 1 && receipt.hits > 0;
            receipt.impactRequired = true; receipt.impactObserved = receipt.hits > 0;
            receipt.coverage = receipt.impactObserved ? "throw and can contact" : "throw only";
            receipt.answer = receipt.accepted ? "can hit" : "throw or can contact absent";
            _performanceActions.actions.Add(receipt);
        }

        private IEnumerator PerformanceReturnHome()
        {
            SceneFlow.LeaveMatchToMainMenu();
            yield return WaitFor(() => TumpHub.Current != null && TumpHub.Current.ShowingHome, 45);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private IEnumerator PerformanceResult()
        {
            _performanceSetup = new PerformanceSetup { stage = "fresh-natural-result" };
            _performanceActions.preparations.Add(_performanceSetup);
            PerformanceWorld();
            long identity = GameServices.Match.PresentationMatchId;
            var stats = GameServices.Stats;
            PerformanceRequire(SceneFlow.SelectedRules.Rounds == 1 && Mathf.Approximately(SceneFlow.SelectedRoundSeconds, CustomGameRules.MinRoundSeconds)
                && stats != null && stats.Last == null, "The natural result requires a fresh scored one-round/30-second match.");
            MatchRecord finished = null; int records = 0;
            void Record(MatchRecord record) { finished = record; records++; }
            stats.RecordReady += Record;
            try
            {
                yield return WaitForResult();
                yield return WaitFor(() => finished != null, 5);
                PerformanceRequire(GameServices.Match.PresentationMatchId == identity && GameServices.Match.HasCompleted
                    && records == 1 && stats.Last == finished && !string.IsNullOrEmpty(finished.MatchId)
                    && finished.MatchId != _performanceLastNaturalRecord && finished.Mode == SceneFlow.SelectedMode.ToString()
                    && finished.Rounds == 1 && finished.Players != null && finished.Players.Length == Balance.PlayerCount && finished.Players.All(p => p != null)
                    && finished.DurationSeconds > 0 && !GameServices.Round.RoundActive,
                    "Natural completion, current completion record and actual result UI did not agree.");
                _performanceLastNaturalRecord = finished.MatchId;
                _performanceSetup.recordId = finished.MatchId; _performanceSetup.passed = true;
                yield return new WaitForSecondsRealtime(2);
            }
            finally { stats.RecordReady -= Record; }
        }
    }
}
