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
            public List<string> statuses = new List<string>();
        }
        [Serializable] private sealed class PerformanceActions
        {
            public string scope = "Controlled first-use measurement: real UI callbacks and InputIntent casts; staged actors, roles, targets, charge and round resets. No human-play, network-services or default-match claim.";
            public List<PerformanceAction> actions = new List<PerformanceAction>();
        }
        private readonly PerformanceActions _performanceActions = new PerformanceActions();
        private DriveInfo _performanceDrive;
        private float _performanceDiskCheckAt;

        private bool PerformanceHasHeadroom()
        {
            if (_finished) return false;
            if (Time.realtimeSinceStartup < _performanceDiskCheckAt) return true;
            _performanceDiskCheckAt = Time.realtimeSinceStartup + 1;
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
            probe._deadline = Time.realtimeSinceStartup + 1500;
            probe.StartFrameWindow("00-boot-to-title");
            probe.StartCoroutine(probe.Guard(probe.Walk()));
        }

        private void BeginPerformanceProfile(string name)
        {
            // Development/autoconnect also permits live inspection. The binary files are
            // the saved evidence; enabling autoconnect alone does not record a capture.
            Profiler.enabled = false;
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
            if (!Debug.isDebugBuild) throw new InvalidOperationException("Performance review requires the Development player.");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 150);
            StopFrameWindow();
            yield return MeasurePerformance("01-title-to-home", PerformanceHomeEntry());
            yield return MeasurePerformance("02-home-idle", PerformanceIdle(3));

            foreach (string door in new[] { "AvatarButton", "HeroButton", "LoadoutButton", "ShopButton", "TaskButton", "MenuButton", "ModeCard" })
            {
                yield return MeasurePerformance("menu-" + door, PerformanceDoor(door));
                TumpHub.Current.Home(); yield return null;
            }
            yield return MeasurePerformance("menu-profile", PerformanceOverlay("NamePlate", "ClosePlayerHub"));
            yield return MeasurePerformance("menu-settings", PerformanceSettings());
            yield return MeasurePerformance("menu-hero-selection", PerformanceSelection());
            yield return MeasurePerformance("lobby-local-host", PerformanceLobby());

            HubHome.Choice = 1;
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            yield return MeasurePerformance("classic-match-entry-and-round-start", PerformancePracticeEntry());
            yield return MeasurePerformance("classic-first-throw-and-hit", PerformanceThrow(PerformanceActor(), "classic-first-throw-and-hit"));
            yield return MeasurePerformance("classic-return-to-home", PerformanceReturnHome());

            HubHome.Choice = 2;
            rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            yield return MeasurePerformance("hero-match-entry-and-round-start", PerformancePracticeEntry());
            var local = PerformanceActor();
            yield return MeasurePerformance("first-throw-and-hit", PerformanceThrow(local));

            foreach (string hero in ReviewHeroes())
            {
                yield return MeasurePerformance(hero + "-first-model-and-kit", PerformanceHero(local, hero));
                // A role kit has a shared signature plus two different second skills.
                foreach (int slot in new[] { 0, 1, 2, 3 })
                {
                    local.IsDefender = slot == 2;
                    local.AbilitySystem.Kit.SetRole(local.IsDefender,
                        new AbilityContext(local, local.GetComponent<Carrier>(), local.GetComponent<CombatVerbs>()));
                    var ability = slot == 0 ? local.AbilitySystem.Kit.Skill1 : slot == 3 ? local.AbilitySystem.Kit.Ultimate : local.AbilitySystem.Kit.Skill2;
                    if (ability is PlaceholderRoleAbility) continue;
                    for (int repetition = 1; repetition <= 2; repetition++)
                    {
                        yield return MeasurePerformance(hero + "-" + slot + "-prepare-" + repetition,
                            PerformancePrepare(local, hero, slot));
                        var liveAbility = slot == 0 ? local.AbilitySystem.Kit.Skill1 : slot == 3 ? local.AbilitySystem.Kit.Ultimate : local.AbilitySystem.Kit.Skill2;
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
                SceneFlow.PinSelectedRules(rules);
                yield return MeasurePerformance(mode + "-result-match-entry", PerformancePracticeEntry());
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
            Settings.SettingsStore.Current.GraphicsQuality = 2;
            Settings.GraphicsProfiles.Apply(2);
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1280 && Screen.height == 720, 8);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            if (TumpHub.Current == null) yield return Click("StartButton");
            yield return WaitFor(() => TumpHub.Current != null && TumpHub.Current.Top is HubHome, 45);
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

        private IEnumerator PerformancePracticeEntry()
        {
            TumpHub.Current.Host.StartPractice();
            yield return WaitFor(() => Hud.Instance != null && !HubLoading.Visible, 60);
            yield return StartReadyRound();
            yield return new WaitForSecondsRealtime(1);
        }

        private static CharacterMotor PerformanceActor()
        {
            var actor = Object.FindFirstObjectByType<PauseWatcher>().Local;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
            foreach (var person in GameServices.Round.Players) person.IsBot = true;
            var rig = Camera.main.GetComponent<CameraRig>();
            rig.Follow(actor, true); rig.SetAimSource(AimSource.Movement);
            return actor;
        }

        private IEnumerator PerformanceHero(CharacterMotor actor, string hero)
        {
            GameServices.Round.EndRound(); GameServices.Round.BeginRound();
            actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            var art = RosterBook.Load().FindPersonArt(hero);
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            actor.AbilitySystem.BindHero(hero);
            Hud.Instance.Bind(actor); Hud.Instance.ShowReadyPrompt(false);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private IEnumerator PerformancePrepare(CharacterMotor actor, string hero, int slot)
        {
            GameServices.Round.EndRound(); GameServices.Round.BeginRound();
            int targetSlot = (actor.PlayerSlot + 1) % Balance.PlayerCount;
            foreach (var person in GameServices.Round.Players)
            {
                person.AbilitySystem?.ResetKit(); person.ClearStun(); person.ClearTrip(); person.ClearStatuses();
                person.Intent.Clear(); person.Intent.Parked = person != actor;
                if (person != actor) person.Teleport(person.PlayerSlot == targetSlot
                    ? new Vector3(0, .12f, -2) : new Vector3(person.PlayerSlot % 2 == 0 ? 4 : -4, .12f, -2));
            }
            actor.Teleport(new Vector3(0, .12f, slot == 2 ? -4 : -10)); actor.transform.rotation = Quaternion.identity;
            actor.Intent.AimPoint = new Vector3(0, slot == 1 && (hero == "sean" || hero == "cheska") ? .8f : .1f, -2);
            actor.Intent.FaceAimPoint = true;
            var owned = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(s => s.SeatOfOrigin == actor.PlayerSlot);
            owned.gameObject.SetActive(true); owned.HostForceEquip(actor);
            if ((hero == "zack" || hero == "nemu") && slot == 1)
            {
                owned.HostDisarm(); owned.transform.position = new Vector3(1, owned.RestHeight, -3);
            }
            if (slot == 3) actor.AbilitySystem.Kit.AddUltimateCharge(100);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private IEnumerator PerformanceCast(CharacterMotor actor, string hero, int slot, int repetition, string name)
        {
            var system = actor.AbilitySystem;
            var ability = slot == 0 ? system.Kit.Skill1 : slot == 3 ? system.Kit.Ultimate : system.Kit.Skill2;
            var verb = slot == 0 ? Verb.Skill1 : slot == 3 ? Verb.Ultimate : Verb.Skill2;
            var answerSlot = slot == 0 ? HeroAbilitySystem.Slot.Skill1 : slot == 3 ? HeroAbilitySystem.Slot.Ultimate : HeroAbilitySystem.Slot.Skill2;
            var receipt = new PerformanceAction { window = name, hero = hero, ability = ability.Id, repetition = repetition,
                role = actor.IsDefender ? "defending" : "attacking",
                impactRequired = slot == 1 && (hero == "cheska" || hero == "sean" || hero == "dante" || hero == "phaister")
                    || slot == 2 && (hero == "amihan" || hero == "phaister") || hero == "nemu" && slot == 0 || hero == "paete" && slot == 3 };
            float began = Time.realtimeSinceStartup;
            float previousAnswerAt = Time.time - system.SecondsSinceAnswer(answerSlot);
            float seconds = slot == 3 ? 13 : Mathf.Clamp(ability.Duration + ability.Windup + 2, 4, 9);
            var targets = GameServices.Round.Players.Where(p => p != actor).ToArray();
            void Outcome(MatchFlair.Kind kind, int source, int subject, Vector3 at, float strength)
            {
                if (source != actor.PlayerSlot) return;
                if (kind == MatchFlair.Kind.Throw) receipt.throws++;
                else if (kind == MatchFlair.Kind.LataDown || kind == MatchFlair.Kind.Tag || kind == MatchFlair.Kind.Block) receipt.hits++;
            }
            void Ultimate(CharacterMotor who, HeroKit kit, HeroAbility power) { if (who == actor) receipt.ultimateStarts++; }
            void Status(CharacterMotor who, StatusKind kind)
            {
                receipt.statusGains++;
                string statusName = kind.ToString();
                if (!receipt.statuses.Contains(statusName)) receipt.statuses.Add(statusName);
            }
            MatchFlair.Presented += Outcome; HeroAbilitySystem.UltimateStarted += Ultimate;
            foreach (var target in targets) target.StatusGained += Status;
            try
            {
                while (Time.realtimeSinceStartup - began < seconds)
                {
                    float age = Time.realtimeSinceStartup - began;
                    actor.Intent.Set(verb, age >= .15f && age < .65f);
                    if (slot == 0 && (hero == "sean" || hero == "zack")) actor.Intent.Move = age > .7f && age < 2 ? Vector2.up * .6f : Vector2.zero;
                    if (slot == 1 && (hero == "sean" || hero == "cheska")) actor.Intent.Set(Verb.SpecialAbility, age > 1.2f && age < 3.2f);
                    if (Time.time - system.SecondsSinceAnswer(answerSlot) > previousAnswerAt && system.LastAnswer(answerSlot) == HeroKit.CastOutcome.Cast) receipt.accepted = true;
                    foreach (var target in targets)
                    {
                        receipt.frozeTarget |= target.IsFrozen;
                        receipt.knockedTarget |= target.IsTripped;
                    }
                    yield return null;
                }
                if (slot == 3) receipt.accepted = receipt.ultimateStarts == 1;
                receipt.impactObserved = hero == "cheska" && slot == 1 ? receipt.frozeTarget
                    : receipt.hits > 0 || receipt.statusGains > 0 || receipt.frozeTarget || receipt.knockedTarget;
                receipt.coverage = receipt.impactObserved ? "cast and observed contact/status" : "cast only";
                receipt.answer = system.LastAnswer(answerSlot).ToString();
            }
            finally
            {
                MatchFlair.Presented -= Outcome; HeroAbilitySystem.UltimateStarted -= Ultimate;
                foreach (var target in targets) if (target != null) target.StatusGained -= Status;
                actor.Intent.Clear(); _performanceActions.actions.Add(receipt);
            }
        }

        private IEnumerator PerformanceThrow(CharacterMotor actor, string label = "first-throw-and-hit")
        {
            yield return PerformancePrepare(actor, "", 0);
            actor.IsDefender = false;
            var round = GameServices.Round; var carrier = actor.GetComponent<Carrier>();
            var receipt = new PerformanceAction { window = label, hero = actor.AbilitySystem?.HeroId ?? "classic", ability = "throw", repetition = 1 };
            actor.Teleport(new Vector3(0, .12f, -10));
            actor.Intent.AimPoint = round.Lata.transform.position + Vector3.up * .18f;
            // The first throw has a clear can lane; later power cases put a body on it.
            foreach (var other in round.Players) if (other != actor) other.Teleport(new Vector3(4, .12f, -2 + other.PlayerSlot));
            yield return WaitFor(() => round.CanThrow(actor), 5);
            var shoe = carrier.Held;
            int serial = round.Lata.HostKnockdownSerial;
            actor.Intent.Set(Verb.SpecialAbility, true); yield return new WaitForSeconds(2);
            actor.Intent.Set(Verb.SpecialAbility, false);
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
            yield return WaitFor(() => TumpHub.Current != null && TumpHub.Current.Top is HubHome, 45);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private IEnumerator PerformanceResult()
        {
            yield return WaitForResult();
            yield return new WaitForSecondsRealtime(2);
        }
    }
}
