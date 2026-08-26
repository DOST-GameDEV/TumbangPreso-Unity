using System.Collections;
using System.Collections.Generic;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// One all-bot Hero Strike match, in the BUILT PLAYER, at a frame rate this machine is told
    /// to hold.
    ///
    /// ⚠️⚠️ IT EXISTS FOR ONE OPEN QUESTION AND IT IS THE MOST VALUABLE ONE IN `docs/TODO.md`.
    /// § 17: the bots are steeply sensitive to the frame step, and the shipped physics rate is
    /// 0.02 s (50 Hz), so **a machine rendering at 50 fps has `Time.deltaTime` equal to the
    /// physics step**. That is the configuration the probe measured at **18 throws and ZERO
    /// skill uses**, against 40 to 90 throws and 27 to 38 skill uses at 1/60. A 50 Hz panel,
    /// vsync on a heavy scene, or a laptop under load all land there.
    ///
    /// ⚠️⚠️ AND § 17's STEP 1 IS EXPLICITLY *"REPRODUCE IT IN THE PLAYER, NOT IN THE PROBE ...
    /// Do this first: everything below is only worth doing if a player can meet it."* The whole
    /// entry turns on whether the effect is real or is an artefact of batch mode, and no
    /// PlayMode test can answer that, because a PlayMode test IS the probe. So this is the same
    /// measurement taken by the shipped executable.
    ///
    /// ⚠️ IT IS OFF UNLESS ASKED FOR ON THE COMMAND LINE, so a normal launch cannot reach any of
    /// it. `NetBootstrap` already parses `Environment.GetCommandLineArgs` for the dedicated
    /// server path; this is the same idea and the same cost when the flag is absent, which is one
    /// string comparison at startup.
    ///
    ///     TumbangPreso.exe -tp-framecap 50 -tp-botmatch -tp-report frames50.txt
    ///
    /// ⚠️⚠️ `targetFrameRate` NEEDS `vSyncCount = 0` OR IT IS IGNORED OUTRIGHT. With vsync on,
    /// Unity paces to the display and the field does nothing at all, which would produce a run
    /// that looks like a 50 fps machine, reports healthy numbers, and closes the entry on a
    /// measurement that never happened. `NetSession` sets the same pair together for the same
    /// reason.
    ///
    /// ⚠️ THE REPORT IS A FILE, NOT `Debug.Log`. A built player's log is
    /// `%LOCALAPPDATA%Low/<company>/<product>/Player.log`, it is overwritten per launch, and it
    /// is interleaved with everything else the game says. A named file beside the executable is
    /// what a sweep across several frame rates can actually be read out of.
    /// </summary>
    public sealed class FrameCapProbe : MonoBehaviour
    {
        private const string CapArg = "-tp-framecap";
        private const string MatchArg = "-tp-botmatch";
        private const string ReportArg = "-tp-report";

        /// <summary>
        /// How long to let the match run before giving up, in real seconds.
        ///
        /// ⚠️ IT IS A CEILING ON A HUNG RUN, NOT THE LENGTH OF THE MATCH. Hero Strike is eight
        /// 90 s rounds, so a real match is about 12 minutes of wall clock at 1x plus the buffers
        /// between rounds; the probe stops when `MatchDirector` says the match is over. This
        /// number only stops a run that never gets there from holding a sweep open forever.
        /// </summary>
        private const float HardTimeoutSeconds = 1100.0f;

        private int _cap;
        private string _reportPath = "framecap-report.txt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args;
            try { args = System.Environment.GetCommandLineArgs(); }
            catch { return; }

            int cap = 0;
            bool run = false;
            string report = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == CapArg && i + 1 < args.Length)
                    int.TryParse(args[i + 1], out cap);
                else if (args[i] == MatchArg) run = true;
                else if (args[i] == ReportArg && i + 1 < args.Length) report = args[i + 1];
            }

            if (cap <= 0 && !run) return;

            if (cap > 0)
            {
                // ⚠️ BOTH, ALWAYS, AND IN THIS ORDER. See the class note: the cap is ignored
                // while vsync is on, and a silently ignored cap is the one outcome that would
                // make this whole probe lie.
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = cap;
            }

            if (!run) return;

            var go = new GameObject("~FrameCapProbe");
            DontDestroyOnLoad(go);

            var probe = go.AddComponent<FrameCapProbe>();
            probe._cap = cap;
            if (!string.IsNullOrEmpty(report)) probe._reportPath = report;

            probe.StartCoroutine(probe.RunMatch());
        }

        private IEnumerator RunMatch()
        {
            // ⚠️ ALL FOUR SEATS ARE BOTS. `docs/TODO.md` § 11: `GameLaunch.SoloSeat` defaults to
            // 1, and until `AllBots` landed that seat was a PARKED HUMAN in every headless run,
            // so every probe number ever printed was an average over three seats and one statue.
            // A frame-rate sweep compared against those old numbers would be comparing four
            // players with three.
            GameLaunch.AllBots = true;
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            SceneFlow.Networked = false;

            yield return null;

            SceneFlow.Go(SceneFlow.SelectedMap);

            // Wait for the arena to install its seats and its lata.
            float waited = 0.0f;
            while (waited < 30.0f)
            {
                var round = GameServices.Round;
                if (round != null && round.Lata != null
                    && round.Players.Count >= Balance.PlayerCount) break;

                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            var tally = new Tally();
            var match = GameServices.Match;
            float elapsed = 0.0f;
            int frames = 0;

            while (elapsed < HardTimeoutSeconds)
            {
                var round = GameServices.Round;
                if (round != null)
                {
                    tally.Sample(round.Players);

                    foreach (var slipper in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                        tally.NoteFlight(slipper);
                }

                frames++;
                elapsed += Time.unscaledDeltaTime;

                // ⚠️ `TotalRounds`, NOT `Balance.HeroStrikeRounds`. `docs/VISION.md` § 1.1:
                // Classic plays four rounds and Hero Strike eight, and the director already owns
                // that decision. Reading the Hero Strike constant here would be a second opinion
                // about when a match is over, which is exactly the shape of drift `Design.md`
                // opens by warning about.
                if (match != null && match.RoundNumber > match.TotalRounds) break;

                yield return null;
            }

            Write(tally, frames, elapsed);
            Application.Quit();
        }

        private void Write(Tally tally, int frames, float elapsed)
        {
            var text = new StringBuilder();
            text.AppendLine("frame cap probe, built player");
            text.AppendLine($"requested cap        {_cap} fps");
            text.AppendLine($"vSyncCount           {QualitySettings.vSyncCount}");
            text.AppendLine($"fixedDeltaTime       {Time.fixedDeltaTime:F5} s");

            // ⚠️⚠️ THE MEASURED RATE IS REPORTED BESIDE THE REQUESTED ONE, AND IT IS THE NUMBER
            // THAT MATTERS. `targetFrameRate` is a request: a machine that cannot hold 50 will
            // report a cap of 50 and run at 31, and the whole point of § 17 is that the two bands
            // behave differently. Reading only the request would close the entry against a run
            // that was never in the band being tested.
            text.AppendLine($"measured frame rate  {(elapsed > 0.0f ? frames / elapsed : 0.0f):F1} fps"
                            + $"  ({frames} frames in {elapsed:F1} s)");
            text.AppendLine($"map                  {SceneFlow.SelectedMap}");
            text.AppendLine();
            text.AppendLine(tally.Describe());

            string path = System.IO.Path.Combine(Application.dataPath, "..", _reportPath);

            try
            {
                System.IO.File.WriteAllText(System.IO.Path.GetFullPath(path), text.ToString());
                Debug.Log($"[FrameCapProbe] wrote {System.IO.Path.GetFullPath(path)}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FrameCapProbe] could not write {path}: {e.Message}");
            }

            Debug.Log("[FrameCapProbe]\n" + text);
        }

        /// <summary>
        /// The counters, kept deliberately close to `BotBehaviourProbe`'s.
        ///
        /// ⚠️⚠️ IT IS A SECOND COPY AND THAT IS NOT AN OVERSIGHT. The probe's tally lives in the
        /// TESTS assembly, which is not compiled into a player at all, so a shared one would have
        /// to move into the runtime and ship inside the game. What is duplicated is thirty lines
        /// of counting with no rules in it; what must NOT drift is the DEFINITION of each count,
        /// and each of the three that has a subtlety carries the probe's own note for it.
        /// </summary>
        private sealed class Tally
        {
            public int Throws, SkillUses, UltimateUses, LataKnocks, Tags, IdlePenalties;
            public bool SawAnyKit;

            private readonly HashSet<Slipper> _inFlight = new HashSet<Slipper>();
            private readonly Dictionary<HeroAbility, float> _lastCooldown =
                new Dictionary<HeroAbility, float>();
            private readonly Dictionary<HeroKit, float> _lastUltimate =
                new Dictionary<HeroKit, float>();
            private bool _subscribed;

            /// <summary>⚠️ ONE COUNT PER FLIGHT. The set has to be EMPTIED again: a slipper only
            /// ever added counts once for its whole life, so a match with four tsinelas could not
            /// report more than four throws. `BotBehaviourProbe.NoteFlight` has the account.
            /// </summary>
            public void NoteFlight(Slipper slipper)
            {
                if (slipper == null) return;

                if (slipper.State == SlipperState.InFlight)
                {
                    if (_inFlight.Add(slipper)) Throws++;
                }
                else _inFlight.Remove(slipper);
            }

            public void Sample(IReadOnlyList<CharacterMotor> seats)
            {
                Subscribe();

                foreach (var seat in seats)
                {
                    var system = seat != null ? seat.AbilitySystem : null;
                    if (system?.Kit == null) continue;

                    SawAnyKit = true;

                    Count(system.Kit.Skill1);
                    Count(system.Kit.Skill2);
                    CountUltimate(system.Kit);
                }
            }

            private void Subscribe()
            {
                if (_subscribed) return;

                var match = GameServices.Match;
                if (match == null) return;

                match.Scored += (slot, e) =>
                {
                    if (e == ScoreEvent.LataKnocked) LataKnocks++;
                    else if (e == ScoreEvent.Tag) Tags++;
                    else if (e == ScoreEvent.UnretrievedSlipperPenalty) IdlePenalties++;
                };

                _subscribed = true;
            }

            /// <summary>⚠️ A SKILL USE IS A COOLDOWN THAT WENT UP. There is no activation event,
            /// and counting "is on cooldown" would count one press once per frame.</summary>
            private void Count(HeroAbility ability)
            {
                if (ability == null) return;

                _lastCooldown.TryGetValue(ability, out float previous);
                if (ability.CooldownRemaining > previous + 0.01f) SkillUses++;
                _lastCooldown[ability] = ability.CooldownRemaining;
            }

            /// <summary>⚠️ AN ULTIMATE IS COUNTED BY ITS CHARGE EMPTYING, NOT BY A COOLDOWN, and
            /// the threshold is the KIT'S OWN COST: each hero pays 90 to 150 since 2026-08-25, so
            /// half of the shared `UltimateMax` is above Nemu's whole meter.</summary>
            private void CountUltimate(HeroKit kit)
            {
                _lastUltimate.TryGetValue(kit, out float previous);
                if (previous > kit.UltimateCost * 0.5f && kit.UltimateCharge <= 0.01f)
                    UltimateUses++;
                _lastUltimate[kit] = kit.UltimateCharge;
            }

            public string Describe()
                => $"throws          {Throws}\n"
                 + $"skill uses      {SkillUses}\n"
                 + $"ultimate uses   {UltimateUses}\n"
                 + $"lata knocks     {LataKnocks}\n"
                 + $"tags            {Tags}\n"
                 + $"idle penalties  {IdlePenalties}\n"
                 + $"kits seen       {SawAnyKit}";
        }
    }
}
