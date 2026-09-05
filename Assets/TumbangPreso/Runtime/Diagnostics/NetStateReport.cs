using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// What one process believes about the match, written to a file so two processes can be
    /// compared line by line.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE "BOTH PROCESSES STAYED OPEN" IS NOT A NETWORK TEST, AND THAT IS
    /// THE ONLY TEST THIS PROJECT HAD. Every networking fault in `docs/TODO.md` §§ 32, 35, 36 and
    /// 38 shares one property: **the host cannot see it.** A joiner saw three statues (§ 36.1),
    /// saw the wrong roster (§ 32.1), heard no abilities (§ 25.1) and earned no Street Hype
    /// (§ 38.15), and in every case the person running the lobby had a perfectly normal match in
    /// front of them. Two logs that both say "connected" prove none of it.
    ///
    /// ⚠️⚠️ SO IT REPORTS WHAT EACH PEER BELIEVES, NOT WHETHER IT SURVIVED. Per seat: which
    /// character, bot or human, how far the body actually travelled, how many casts were seen.
    /// Per prop: where it is and who holds it. Plus the scores and the round. **Two files that
    /// disagree name the fault directly**: equal characters and unequal distances is a transform
    /// fault, equal distances and unequal casts is a replication fault, and a client reporting
    /// zero movement on three seats is § 36.1 exactly.
    ///
    /// Run it with the player's own switches, one process each:
    ///
    ///   TumbangPreso.exe -tp-host 8910 -tp-profile host -tp-allbots \
    ///                    -tp-netreport host.txt -tp-netseconds 45 -logFile host.log
    ///   TumbangPreso.exe -tp-join 127.0.0.1 8910 -tp-profile client -tp-allbots \
    ///                    -tp-netreport client.txt -tp-netseconds 45 -logFile client.log
    ///
    /// ⚠️ `-tp-allbots` IS WHAT MAKES THE COMPARISON MEAN ANYTHING. Without it nobody presses a
    /// key, every seat stands still, and two processes agreeing that nothing happened is not
    /// evidence. With it all four seats play, so the distances, the casts and the props are real
    /// numbers on both sides.
    ///
    /// ⚠️ THE HASH IS FOR THE EYE, NOT FOR AN ASSERTION. Two peers cannot agree bit for bit on a
    /// continuous quantity, so it covers only the DISCRETE state a divergence would show up in:
    /// character indices, bot flags, the defender, slipper states and holders, and the scores.
    /// </summary>
    public sealed class NetStateReport : MonoBehaviour
    {
        public const string ReportSwitch = "-tp-netreport";
        public const string SecondsSwitch = "-tp-netseconds";

        private const float DefaultSeconds = 45.0f;

        private string _path;
        private float _left;
        private float _elapsed;

        private readonly float[] _travelled = new float[Balance.PlayerCount];
        private readonly Vector3[] _lastPosition = new Vector3[Balance.PlayerCount];
        private readonly bool[] _seen = new bool[Balance.PlayerCount];

        private readonly int[] _skillCasts = new int[Balance.PlayerCount];
        private readonly int[] _ultimateCasts = new int[Balance.PlayerCount];
        private readonly Dictionary<HeroAbility, float> _lastCooldown =
            new Dictionary<HeroAbility, float>();
        private readonly Dictionary<HeroKit, float> _lastCharge = new Dictionary<HeroKit, float>();

        private readonly int[] _slipperTransitions = new int[Balance.PlayerCount];
        private readonly int[] _lastSlipperState = new int[Balance.PlayerCount];
        private int _lataFlips;
        private bool _lastLataUpright = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path = Argument(ReportSwitch);
            if (string.IsNullOrEmpty(path)) return;

            var go = new GameObject("~NetStateReport") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);

            var report = go.AddComponent<NetStateReport>();
            report._path = path;
            report._left = Seconds();

            Debug.Log($"[NetReport] writing {path} after {report._left:F0} s.");
        }

        private static float Seconds()
        {
            string raw = Argument(SecondsSwitch);
            return !string.IsNullOrEmpty(raw) && float.TryParse(raw, out float value) && value > 1.0f
                ? value
                : DefaultSeconds;
        }

        private static string Argument(string name)
        {
            string[] args;
            try { args = Environment.GetCommandLineArgs(); }
            catch { return null; }

            if (args == null) return null;

            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];

            return null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;

            Sample();

            _left -= dt;
            if (_left > 0.0f) return;

            enabled = false;
            Write();

            // ⚠️ QUIT, SO THE HARNESS DOES NOT HAVE TO KILL A WINDOW. The file is flushed above.
            Application.Quit();
        }

        /// <summary>
        /// Everything that has to be integrated rather than read once: distance, casts, and the
        /// number of times a prop changed state.
        ///
        /// ⚠️ CASTS ARE COUNTED THE WAY `BotBehaviourProbe` COUNTS THEM, off a cooldown RISING
        /// and off an ultimate meter EMPTYING, because there is no cast event to subscribe to and
        /// an ultimate is authored with `Cooldown` 0 so the cooldown test can never fire for it.
        /// </summary>
        private void Sample()
        {
            var round = GameServices.Round;
            if (round == null) return;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round.PlayerAt(slot);
                if (unit == null) continue;

                Vector3 at = unit.transform.position;
                if (_seen[slot]) _travelled[slot] += Vector3.Distance(_lastPosition[slot], at);

                _lastPosition[slot] = at;
                _seen[slot] = true;

                var kit = unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
                if (kit == null) continue;

                CountSkill(slot, kit.Skill1);
                CountSkill(slot, kit.Skill2);
                CountUltimate(slot, kit);
            }

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var slipper = FindSlipper(slot);
                if (slipper == null) continue;

                int state = (int)slipper.State;
                if (state != _lastSlipperState[slot]) _slipperTransitions[slot]++;
                _lastSlipperState[slot] = state;
            }

            var lata = round.Lata;
            if (lata != null)
            {
                if (lata.IsUpright != _lastLataUpright) _lataFlips++;
                _lastLataUpright = lata.IsUpright;
            }
        }

        private void CountSkill(int slot, HeroAbility ability)
        {
            if (ability == null) return;

            _lastCooldown.TryGetValue(ability, out float previous);
            if (ability.CooldownRemaining > previous + 0.01f) _skillCasts[slot]++;
            _lastCooldown[ability] = ability.CooldownRemaining;
        }

        private void CountUltimate(int slot, HeroKit kit)
        {
            _lastCharge.TryGetValue(kit, out float previous);
            if (previous > kit.UltimateCost * 0.5f && kit.UltimateCharge <= 0.01f) _ultimateCasts[slot]++;
            _lastCharge[kit] = kit.UltimateCharge;
        }

        /// <summary>
        /// ⚠️⚠️ BY `SeatOfOrigin`, NOT BY `OwnerSlot`, AND THE INSTRUMENT HAD THE SAME BUG AS THE
        /// THING IT MEASURES. `docs/TODO.md` § 78.1: `OwnerSlot` goes to -1 the round its seat
        /// becomes taya, so this returned null for the defender's tsinelas and the report printed
        /// `-1` — which reads as "no such object" and is how the bug was found, but it also meant
        /// the row could never show what that shoe was actually doing. Keyed on the seat, all four
        /// rows are always populated and the OWNER is printed as its own column instead.
        /// </summary>
        /// ⚠️ INACTIVE INCLUDED, because "switched off" is exactly the state this report has to be
        /// able to show: the taya's tsinelas is parked with `SetActive(false)` and excluding it
        /// would print `-1` on a peer that has it right and `-1` on a peer that has never heard
        /// about it. Those are different facts and the `on` column separates them.
        private static Slipper FindSlipper(int seatOfOrigin)
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Include,
                                                         FindObjectsSortMode.None))
                if (s != null && s.SeatOfOrigin == seatOfOrigin) return s;

            return null;
        }

        private void Write()
        {
            var round = GameServices.Round;
            var match = GameServices.Match;
            var sb = new StringBuilder();

            sb.AppendLine("TUMBANG PRESO NETWORK STATE REPORT");
            sb.AppendLine();
            sb.AppendLine($"role            : {(NetAuthority.IsHost ? "HOST" : "CLIENT")}");
            sb.AppendLine($"networked       : {NetAuthority.IsNetworked}");
            sb.AppendLine($"local slot      : {NetAuthority.LocalSlot}");
            sb.AppendLine($"protocol        : {Net.NetSession.ProtocolVersion}");
            sb.AppendLine($"mode            : {UI.SceneFlow.SelectedMode}");
            sb.AppendLine($"map             : {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            sb.AppendLine($"sampled         : {_elapsed:F1} s");
            sb.AppendLine($"round           : {(match != null ? match.RoundNumber : -1)}");
            sb.AppendLine($"defender        : {(match != null ? match.DefenderSlot : -1)}");
            sb.AppendLine($"round active    : {(round != null && round.RoundActive)}");
            sb.AppendLine($"lata upright    : {_lastLataUpright}   flips: {_lataFlips}");

            // ⚠️⚠️ THE PRESET, NOT MERELY "A MATCH HAPPENED". `docs/TODO.md` § 145.8: the recorded
            // green cold start at `87346b8` proved a shipped player reached round 1, moved four
            // seats and scored, and it is a **macOS player playing HERO STRIKE**, while
            // `docs/VISION.md` § 1.1 says CLASSIC is the tournament ruleset. A harness cannot
            // assert what it cannot read, so the two sentences a bracket match rests on are
            // printed here: is this the tournament RULE SET, and is any practice or debug
            // modifier still set.
            //
            // ⚠️ ONE LINE EACH, AND `OK` / `none` ARE THE PASSING VALUES. `TournamentGuard
            // .Refusal` answers a paragraph on purpose (an operator has to act on it), and a
            // harness parsing a paragraph out of a report is a parser nobody can trust, so it is
            // flattened onto one line here and printed in full in the player's own log.
            string refusal = TournamentGuard.Refusal().Replace("\r", "").Replace("\n", " / ");
            sb.AppendLine($"tournament ruleset : {(string.IsNullOrEmpty(refusal) ? "OK" : refusal)}");
            sb.AppendLine($"tournament modifiers : {ModifiersLeftSet()}");
            sb.AppendLine($"build identity  : {BuildIdentity.OneLine()}");
            sb.AppendLine();

            // ⚠️⚠️ `origin` IS A NEW COLUMN AND IT IS THE ONE THAT CAN BE COMPARED BETWEEN
            // PEERS. `bot` says who is driving the chair RIGHT NOW and moves the moment somebody
            // disconnects; `origin` says what the chair has been for the match, which is
            // `Core.SeatOrigin` and only ever moves forward. `docs/TODO.md` § 145.4b.
            //
            // ⚠️ THE COLUMN GOES AFTER `bot` AND NOT INSTEAD OF IT. The verifier needs both: the
            // live flag is what a departure changes and the origin is what explains it, and a
            // table with only one of them cannot tell a handover from a disagreement.
            sb.AppendLine($"{"seat",-5} {"char",5} {"bot",5} {"origin",11} {"taya",5} {"score",7} " +
                          $"{"travelled",10} {"skills",7} {"ults",5}");
            sb.AppendLine(new string('-', 74));

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round != null ? round.PlayerAt(slot) : null;
                int score = match != null ? match.ScoreFor(slot) : 0;
                string origin = unit != null ? unit.SeatOrigin.ToString() : "Unknown";

                sb.AppendLine($"{slot,-5} {(unit != null ? unit.CharacterIndex : -1),5} " +
                              $"{(unit != null && unit.IsBot),5} {origin,11} " +
                              $"{(unit != null && unit.IsDefender),5} " +
                              $"{score,7} {_travelled[slot],10:F1} " +
                              $"{_skillCasts[slot],7} {_ultimateCasts[slot],5}");
            }

            sb.AppendLine();
            // ⚠️ `owner` IS ITS OWN COLUMN NOW. It is the field `docs/TODO.md` § 78.1 stopped using
            // as an address, and it is what says whether the taya's tsinelas has been correctly
            // disowned on this peer: the defender's row should read owner -1 on BOTH reports.
            sb.AppendLine($"{"slipper",-8} {"on",4} {"state",6} {"owner",6} {"holder",7} {"changes",8}");
            sb.AppendLine(new string('-', 45));

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var s = FindSlipper(seat);
                int holder = s != null && s.Holder != null ? s.Holder.PlayerSlot : -1;
                int owner = s != null ? s.OwnerSlot : -1;
                string on = s == null ? "-" : (s.gameObject.activeSelf ? "y" : "n");

                sb.AppendLine($"{seat,-8} {on,4} {(s != null ? (int)s.State : -1),6} {owner,6} " +
                              $"{holder,7} {_slipperTransitions[seat],8}");
            }

            sb.AppendLine();
            sb.AppendLine($"structural state hash : {StructuralHash(round)}");
            sb.AppendLine($"discrete state hash : {DiscreteHash(round, match)}");
            sb.AppendLine();
            sb.AppendLine("⚠️ THE DISCRETE HASH COVERS ONLY DISCRETE STATE. Two peers cannot agree bit");
            sb.AppendLine("for bit on a position or a clock; they must agree on who is who, who is");
            sb.AppendLine("taya, what each tsinelas is doing and what the score is.");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️ AND IT IS NOT AN EQUALITY GATE ACROSS PEERS, WHICH THE STRUCTURAL HASH");
            sb.AppendLine("IS. Discrete is not the same as CONSTANT: the score, the slipper states and");
            sb.AppendLine("the defender all move during a match, and two reports stop at two different");
            sb.AppendLine("instants by construction (a referee has to outlive its clients). Comparing");
            sb.AppendLine("the discrete hash between peers therefore fails on a working link roughly");
            sb.AppendLine("whenever anything happens. The structural hash covers only what CANNOT");
            sb.AppendLine("change while a match runs: which character sits in which seat, which seats");
            sb.AppendLine("are bots, and the protocol. Two peers disagreeing on that are playing two");
            sb.AppendLine("different matches, whenever you look.");

            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(_path));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(_path, sb.ToString());
                Debug.Log($"[NetReport] wrote {_path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetReport] could not write {_path}: {e.Message}");
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// The part of the state that cannot change while a match is running.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `DiscreteHash` WAS BEING READ AS AN EQUALITY GATE AND CANNOT BE
        /// ONE. `docs/TODO.md` § 145.4: a verifier that hard-compares a field which legitimately
        /// differs because two reports stopped at different timestamps is a verifier that goes red
        /// on a working link. Every peer's discrete hash folds in the SCORE and the slipper
        /// states, and `referee_run.py` gives the referee its clients' head start back plus a
        /// margin on purpose, so the two are sampled seconds apart. **Discrete is not constant.**
        ///
        /// ⚠️⚠️ AND IT USED TO FOLD IN `IsBot`, WHICH IS NOT CONSTANT EITHER, SO THIS HASH MADE
        /// EXACTLY THE CLAIM ITS OWN COMMENT WARNED ABOUT. The note under it already said
        /// *"`MatchRpc.HostPeerLeft` can flip a bot flag mid-match"* and then hashed the flag
        /// anyway. `docs/TODO.md` § 145.4b is the measurement: a seatless referee and two idle
        /// clients, no `-tp-allbots`, and the two clients agreed with each other perfectly while
        /// the referee, which outlives them by design, called every chair a bot **because by the
        /// time it sampled both players had quit and their chairs had been handed over**. Three
        /// findings, and the game was right in all three.
        ///
        /// ⚠️ SO IT FOLDS IN THE PERSISTENT FACT INSTEAD: `SeatHandover.APersonSatHere`, which is
        /// whether a person EVER sat in this chair. A chair somebody sat in never becomes a chair
        /// nobody sat in, whoever is driving it at the instant a report is written, so this is a
        /// hard equality that holds across a departure. The live `bot` flag is still printed in
        /// the seat table beside the origin, where a reader can see both.
        ///
        /// ⚠️ WHAT IS IN IT: the character index and whether a person ever sat there, per seat,
        /// plus the protocol. All three are decided at seating and none of them moves again.
        ///
        /// ⚠️ THE DEFENDER IS DELIBERATELY NOT IN IT. It is derived from the round number
        /// (`docs/VISION.md` § 4), so two peers on different rounds hold different defenders
        /// CORRECTLY, and a hash that folded it in would be a clock in disguise.
        /// </summary>
        private string StructuralHash(RoundDirector round)
        {
            var sb = new StringBuilder();
            sb.Append(Net.NetSession.ProtocolVersion).Append('|');

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round != null ? round.PlayerAt(slot) : null;
                sb.Append(unit != null ? unit.CharacterIndex : -1).Append(':');
                sb.Append(unit != null && Core.SeatHandover.APersonSatHere(unit.SeatOrigin) ? 1 : 0)
                  .Append('/');
            }

            return Fnv(sb.ToString());
        }

        /// <summary>
        /// The tournament modifiers that are NOT at their safe value, comma separated, or "none".
        ///
        /// ⚠️ NAMES RATHER THAN A COUNT. A harness asserting "no modifiers" against a number
        /// cannot say WHICH one is set, and the whole value of `TournamentPreset.Modifiers` is
        /// that each row names a switch and the reason it matters.
        ///
        /// ⚠️ AN UNREADABLE MODIFIER COUNTS AS SET. `TournamentGuard.Read` answers
        /// `known: false` for a name this build cannot resolve, and reading that as "off" is the
        /// fail-open half of exactly the hole `docs/TODO.md` § 145.3 closed.
        /// </summary>
        private static string ModifiersLeftSet()
        {
            var names = new List<string>();

            foreach (var reading in TournamentGuard.LiveModifiers())
            {
                if (reading.IsSafe) continue;
                names.Add(reading.Known ? reading.Name : reading.Name + "(UNREADABLE)");
            }

            return names.Count == 0 ? "none" : string.Join(", ", names);
        }

        private string DiscreteHash(RoundDirector round, MatchDirector match)
        {
            var sb = new StringBuilder();

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round != null ? round.PlayerAt(slot) : null;
                sb.Append(unit != null ? unit.CharacterIndex : -1).Append(':');
                sb.Append(unit != null && unit.IsBot ? 1 : 0).Append(':');
                sb.Append(unit != null && unit.IsDefender ? 1 : 0).Append(':');
                sb.Append(match != null ? match.ScoreFor(slot) : 0).Append('|');

                var s = FindSlipper(slot);
                sb.Append(s != null ? (int)s.State : -1).Append(':');
                sb.Append(s != null && s.Holder != null ? s.Holder.PlayerSlot : -1).Append('/');
            }

            sb.Append(_lastLataUpright ? 1 : 0);

            return Fnv(sb.ToString());
        }

        /// <summary>⚠️ ONE HASH FUNCTION FOR BOTH, so two hashes of the same string are the same
        /// number and a reader comparing them across reports is comparing like with like.</summary>
        private static string Fnv(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }

                return hash.ToString("X8");
            }
        }
    }
}
