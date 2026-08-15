using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>The title screen.</summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "MainMenu");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform, "TUMBANG PRESO", 96, UiTheme.Amber,
                          new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200, 140));

            MenuKit.Label(canvas.transform, "Four players. Four rounds. One taya.",
                          28, UiTheme.CreamMuted,
                          new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(1200, 60));

            var size = new Vector2(460, 84);
            float y = 60.0f;
            const float step = 104.0f;

            MenuKit.WoodButton(canvas.transform, "START", new Vector2(0.5f, 0.5f),
                               new Vector2(0, y), size, () => SceneFlow.Go(SceneFlow.ModeSelect));

            MenuKit.WoodButton(canvas.transform, "SETTINGS", new Vector2(0.5f, 0.5f),
                               new Vector2(0, y - step), size, () => Panel.Open<SettingsPanel>(this));

            MenuKit.WoodButton(canvas.transform, "TUTORIAL", new Vector2(0.5f, 0.5f),
                               new Vector2(0, y - step * 2), size, () => Panel.Open<TutorialPanel>(this));

            MenuKit.WoodButton(canvas.transform, "CREDITS", new Vector2(0.5f, 0.5f),
                               new Vector2(0, y - step * 3), size, () => Panel.Open<CreditsPanel>(this));

            MenuKit.WoodButton(canvas.transform, "QUIT", new Vector2(0.5f, 0.5f),
                               new Vector2(0, y - step * 4), size, SceneFlow.Quit);

            GameServices.Music?.Play("menu", GameServices.MenuTrack);
        }
    }

    /// <summary>
    /// Single player against bots, or multiplayer.
    ///
    /// ⚠️ SINGLE PLAYER IS A HOST WITH NO PEERS, not a separate mode with its own rules. That
    /// is why every host-side path runs unchanged offline, and it is why this screen chooses a
    /// flow rather than a ruleset.
    /// </summary>
    public sealed class ModeSelectScreen : MonoBehaviour
    {
        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "ModeSelect");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform, "CHOOSE A MODE", 64, UiTheme.Amber,
                          new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(1000, 100));

            var size = new Vector2(520, 96);

            MenuKit.WoodButton(canvas.transform, "SINGLE PLAYER", new Vector2(0.5f, 0.5f),
                               new Vector2(0, 60), size, () =>
                               {
                                   SceneFlow.Networked = false;
                                   SceneFlow.Go(SceneFlow.MatchSetup);
                               });

            MenuKit.WoodButton(canvas.transform, "MULTIPLAYER", new Vector2(0.5f, 0.5f),
                               new Vector2(0, -50), size, () =>
                               {
                                   SceneFlow.Networked = true;
                                   SceneFlow.Go(SceneFlow.MultiplayerSetup);
                               });

            MenuKit.WoodButton(canvas.transform, "BACK", new Vector2(0.5f, 0.15f),
                               Vector2.zero, new Vector2(300, 72),
                               () => SceneFlow.Go(SceneFlow.MainMenu));
        }
    }

    /// <summary>Pick the arena, then the characters.</summary>
    public sealed class MatchSetupScreen : MonoBehaviour
    {
        private Text _mapLabel;
        private int _index;

        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "MatchSetup");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform, "MATCH SETUP", 64, UiTheme.Amber,
                          new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(1000, 100));

            MenuKit.Label(canvas.transform, "MAP", 30, UiTheme.CreamMuted,
                          new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(400, 50));

            _mapLabel = MenuKit.Label(canvas.transform, "", 44, UiTheme.Cream,
                                      new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(700, 70));

            MenuKit.WoodButton(canvas.transform, "<", new Vector2(0.5f, 0.58f),
                               new Vector2(-330, 0), new Vector2(90, 70), () => Cycle(-1));

            MenuKit.WoodButton(canvas.transform, ">", new Vector2(0.5f, 0.58f),
                               new Vector2(330, 0), new Vector2(90, 70), () => Cycle(1));

            // ⚠️ THE RULES ARE PRINTED, NOT ASSUMED KNOWN. Four rounds, ninety seconds, one
            // taya, rotating. A player who has not read a GDD should be able to learn the
            // format from the setup screen.
            MenuKit.Label(canvas.transform,
                          $"{Balance.Rounds} rounds  ·  {Balance.RoundTime:0}s each  ·  " +
                          $"{Balance.PlayerCount} players  ·  the taya rotates every round",
                          26, UiTheme.CreamMuted,
                          new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(1400, 60));

            MenuKit.WoodButton(canvas.transform, "CHOOSE CHARACTERS", new Vector2(0.5f, 0.28f),
                               Vector2.zero, new Vector2(560, 90),
                               () => SceneFlow.Go(SceneFlow.CharacterSelect));

            MenuKit.WoodButton(canvas.transform, "BACK", new Vector2(0.5f, 0.13f),
                               Vector2.zero, new Vector2(300, 72),
                               () => SceneFlow.Go(SceneFlow.ModeSelect));

            Refresh();
        }

        private void Cycle(int delta)
        {
            int n = SceneFlow.Maps.Length;
            _index = ((_index + delta) % n + n) % n;
            Refresh();
        }

        private void Refresh()
        {
            SceneFlow.SelectedMap = SceneFlow.Maps[_index];
            _mapLabel.text = SceneFlow.SelectedMap.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Host, join by code, or browse the LAN.
    ///
    /// ⚠️ THE TRANSPORT IS NOT WIRED YET (Port_Plan phase 5), so this screen presents the flow
    /// and the LAN browser and says plainly that connecting is not live. A button that looks
    /// live and silently does nothing is worse than one that says what it is waiting for.
    /// </summary>
    public sealed class MultiplayerSetupScreen : MonoBehaviour
    {
        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "MultiplayerSetup");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform, "MULTIPLAYER", 64, UiTheme.Amber,
                          new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(1000, 100));

            var size = new Vector2(520, 84);

            MenuKit.WoodButton(canvas.transform, "HOST A GAME", new Vector2(0.5f, 0.62f),
                               Vector2.zero, size, NotWiredYet);

            MenuKit.WoodButton(canvas.transform, "JOIN BY CODE", new Vector2(0.5f, 0.50f),
                               Vector2.zero, size, NotWiredYet);

            MenuKit.WoodButton(canvas.transform, "LAN GAMES", new Vector2(0.5f, 0.38f),
                               Vector2.zero, size, NotWiredYet);

            MenuKit.Label(canvas.transform,
                          "Transport not wired yet. Lobby seating, reconnection, seat reclaim,\n" +
                          "join codes and LAN discovery are all ported and unit tested.",
                          24, UiTheme.CreamMuted,
                          new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(1400, 80));

            MenuKit.WoodButton(canvas.transform, "BACK", new Vector2(0.5f, 0.13f),
                               Vector2.zero, new Vector2(300, 72),
                               () => SceneFlow.Go(SceneFlow.ModeSelect));
        }

        private static void NotWiredYet() =>
            Debug.Log("[Net] transport is Phase 5. The lobby logic behind this is ported and tested.");
    }

    /// <summary>
    /// The three tabs.
    ///
    /// ⚠️⚠️ EACH TAB RENAMES THE SAME THREE KEYS AND THE KEYS NEVER CHANGE. bilis, lakas and
    /// tatag are internal identifiers; only the LABELS differ per tab. Renaming a key to match
    /// its label is a silent flat-3 fallback on every entry, because a missing key resolves to
    /// neutral without erroring.
    ///
    /// ⚠️ AND RECOVERY IS ON tatag WHILE RESET IS ON bilis. They read alike and sit on different
    /// keys. That is the one trap in the table: check the key, never the word.
    /// </summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        private static readonly string[] TabNames = { "PERSON", "LATA", "TSINELAS" };

        private static readonly string[][] MeterLabels =
        {
            new[] { "SPEED", "POWER", "GRIT" },
            new[] { "RESET", "REBOUND", "STANCE" },
            new[] { "FLIGHT", "IMPACT", "RECOVERY" },
        };

        private int _tab;
        private readonly int[] _pick = { 0, 0, 0 };

        private Text _name;
        private Text _meters;
        private Text _tabLabel;

        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "CharacterSelect");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            _tabLabel = MenuKit.Label(canvas.transform, "", 40, UiTheme.Amber,
                                      new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(800, 70));

            MenuKit.WoodButton(canvas.transform, "TAB", new Vector2(0.5f, 0.86f),
                               new Vector2(430, 0), new Vector2(140, 60), () => CycleTab(1));

            _name = MenuKit.Label(canvas.transform, "", 56, UiTheme.Cream,
                                  new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(900, 90));

            MenuKit.WoodButton(canvas.transform, "<", new Vector2(0.5f, 0.66f),
                               new Vector2(-400, 0), new Vector2(90, 80), () => CycleEntry(-1));

            MenuKit.WoodButton(canvas.transform, ">", new Vector2(0.5f, 0.66f),
                               new Vector2(400, 0), new Vector2(90, 80), () => CycleEntry(1));

            _meters = MenuKit.Label(canvas.transform, "", 30, UiTheme.Cream,
                                    new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(900, 200));

            MenuKit.WoodButton(canvas.transform, "START MATCH", new Vector2(0.5f, 0.24f),
                               Vector2.zero, new Vector2(520, 92), Begin);

            MenuKit.WoodButton(canvas.transform, "BACK", new Vector2(0.5f, 0.11f),
                               Vector2.zero, new Vector2(300, 72),
                               () => SceneFlow.Go(SceneFlow.MatchSetup));

            Refresh();
        }

        private System.Collections.Generic.IReadOnlyList<RosterEntry> Entries =>
            _tab == 0 ? Roster.People : (_tab == 1 ? Roster.Cans : Roster.Slippers);

        private void CycleTab(int d)
        {
            _tab = ((_tab + d) % 3 + 3) % 3;
            Refresh();
        }

        private void CycleEntry(int d)
        {
            int n = Entries.Count;
            _pick[_tab] = ((_pick[_tab] + d) % n + n) % n;
            Refresh();
        }

        private void Refresh()
        {
            _tabLabel.text = TabNames[_tab];

            var entry = Entries[_pick[_tab]];
            _name.text = entry.Name;

            var labels = MeterLabels[_tab];
            _meters.text =
                $"{labels[0],-10}{Bar(entry.Bilis)}\n" +
                $"{labels[1],-10}{Bar(entry.Lakas)}\n" +
                $"{labels[2],-10}{Bar(entry.Tatag)}";
        }

        private static string Bar(int points)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= Roster.TraitMax; i++) sb.Append(i <= points ? "#" : ".");
            return sb.ToString();
        }

        private void Begin()
        {
            var s = Settings.SettingsStore.Current;
            s.CharacterPick = _pick[0];
            s.CanPick = _pick[1];
            s.SlipperPick = _pick[2];
            Settings.SettingsStore.Save();

            SceneFlow.StartMatch();
        }
    }

    /// <summary>Final scores. ⚠️ A tie at the top is reported as an honest draw.</summary>
    public sealed class MatchResultScreen : MonoBehaviour
    {
        public static int[] FinalScores = new int[Balance.PlayerCount];
        public static int WinningSlot = -1;

        private void Start()
        {
            var canvas = MenuKit.BuildCanvas(transform, "MatchResult");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform,
                          WinningSlot < 0 ? "A DRAW" : $"P{WinningSlot + 1} WINS",
                          80, UiTheme.Amber,
                          new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200, 120));

            var sb = new System.Text.StringBuilder();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                sb.AppendLine($"P{slot + 1}    {FinalScores[slot]}");

            MenuKit.Label(canvas.transform, sb.ToString(), 40, UiTheme.Cream,
                          new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(700, 260));

            MenuKit.WoodButton(canvas.transform, "MAIN MENU", new Vector2(0.5f, 0.2f),
                               Vector2.zero, new Vector2(460, 86),
                               () => SceneFlow.Go(SceneFlow.MainMenu));
        }
    }
}
