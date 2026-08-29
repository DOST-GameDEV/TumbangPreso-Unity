using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The career page: who you are, what you have done, and the last twenty matches.
    ///
    /// ⚠️⚠️ EVERY RATE ON THIS SCREEN IS GATED ON ITS OWN SAMPLE SIZE, WHICH IS THE ONE RULE
    /// `FUTURE.md` § 2.2 STATES AS A COMMANDMENT: *"DO NOT SHOW A STAT YOU WILL NOT DEFEND."*
    /// Every number here becomes an argument in a lobby, and a 100 per cent shove rate over two
    /// attempts is not a fact about a player. `MatchRecordRules.IsReportable` decides, the row
    /// says what it is waiting for, and the raw counts are shown meanwhile because a count is
    /// always true.
    ///
    /// ⚠️ IT IS CODE-BUILT RATHER THAN CONVERTED, LIKE `AccountOverlay`. `ConvertedScreen` finds
    /// its nodes by the names a Godot `.tscn` gave them, and there is no Godot original for this
    /// screen: there was no career in that build. Following the converted conventions here would
    /// mean authoring a scene whose only purpose is to be found by name.
    ///
    /// ⚠️ THE PARTS THAT ARE NOT HERE ARE NOT FORGOTTEN. `FUTURE.md` § 2.1 also lists an avatar,
    /// a rank badge, an achievement shelf and a friend comparison. Each is owned by a later phase
    /// and each would be an empty box today; `docs/TODO.md` § 89.4 lists them with the phase that
    /// fills them. Drawing an empty rank badge would teach a player that the game has a rank.
    /// </summary>
    public sealed class ProfileOverlay : MonoBehaviour
    {
        private const int HistoryPageSize = 20;

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _detail;
        private GameObject _masteryPanel;

        private Text _handle, _identity, _status, _career;
        private Text _modeTitle;
        private readonly List<Text> _statRows = new List<Text>();
        private readonly List<Button> _historyRows = new List<Button>();
        private readonly List<Text> _historyLabels = new List<Text>();
        private GameObject _mastery;
        private Text _pager, _detailBody, _masteryBody;

        private GameMode _mode = GameMode.Classic;
        private int _page;
        private List<MatchRecord> _shown = new List<MatchRecord>();

        public void Install()
        {
            if (_canvas != null) return;

            _canvas = MenuKit.BuildCanvas(transform, "ProfileCanvas");

            // ⚠️ BELOW `AccountOverlay`'S 80. The account panel is the one that has to be able to
            // cover this: signing in from the career page redraws everything under it.
            _canvas.sortingOrder = 70;

            MenuKit.WoodButton(_canvas.transform, "CAREER", new Vector2(1, 1),
                new Vector2(-320, -42), new Vector2(190, 54), Open);

            BuildPanel();
            BuildDetail();
            BuildMastery();

            _panel.SetActive(false);
            _detail.SetActive(false);
            _masteryPanel.SetActive(false);

            var career = GameServices.Career;
            if (career != null) career.Changed += Refresh;
        }

        private void OnDestroy()
        {
            if (GameServices.Career != null) GameServices.Career.Changed -= Refresh;
        }

        // -------------------------------------------------------------------
        // § THE PANEL
        // -------------------------------------------------------------------

        private void BuildPanel()
        {
            _panel = new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            MenuKit.Place((RectTransform)_panel.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1480, 940));
            _panel.GetComponent<Image>().color = UiTheme.WoodDeep;

            // Header card. One screenshot is meant to be the whole flex (§ 2.1 item 1).
            _handle = MenuKit.Label(_panel.transform, "", 40, UiTheme.Amber,
                new Vector2(0, 1), new Vector2(360, -54), new Vector2(680, 56), TextAnchor.MiddleLeft);
            _identity = MenuKit.Label(_panel.transform, "", 20, UiTheme.Cream,
                new Vector2(0, 1), new Vector2(360, -100), new Vector2(680, 36), TextAnchor.MiddleLeft);
            _status = MenuKit.Label(_panel.transform, "", 18, UiTheme.CreamMuted,
                new Vector2(0, 1), new Vector2(360, -132), new Vector2(680, 34), TextAnchor.MiddleLeft);

            // Career strip (§ 2.1 item 2). Mode-independent totals, so it sits above the tabs.
            _career = MenuKit.Label(_panel.transform, "", 20, UiTheme.Cream,
                new Vector2(0.5f, 1), new Vector2(0, -186), new Vector2(1400, 34));

            // Mode tabs (§ 2.1 item 3). ⚠️ Classic and Hero Strike are never merged: they are
            // separate games and a combined knockdown count is a number about neither.
            MenuKit.WoodButton(_panel.transform, "CLASSIC", new Vector2(0, 1),
                new Vector2(150, -232), new Vector2(200, 46), () => SetMode(GameMode.Classic));
            MenuKit.WoodButton(_panel.transform, "HERO STRIKE", new Vector2(0, 1),
                new Vector2(366, -232), new Vector2(230, 46), () => SetMode(GameMode.HeroStrike));
            _modeTitle = MenuKit.Label(_panel.transform, "", 22, UiTheme.Amber,
                new Vector2(0, 1), new Vector2(620, -232), new Vector2(420, 40), TextAnchor.MiddleLeft);

            MenuKit.Label(_panel.transform, "STATS", 24, UiTheme.Amber,
                new Vector2(0, 1), new Vector2(150, -282), new Vector2(300, 34), TextAnchor.MiddleLeft);

            for (int i = 0; i < 16; i++)
                _statRows.Add(MenuKit.Label(_panel.transform, "", 19, UiTheme.Cream,
                    new Vector2(0, 1), new Vector2(360, -320 - i * 30), new Vector2(660, 28),
                    TextAnchor.MiddleLeft));

            MenuKit.Label(_panel.transform, "MATCH HISTORY", 24, UiTheme.Amber,
                new Vector2(1, 1), new Vector2(-580, -282), new Vector2(320, 34), TextAnchor.MiddleLeft);

            for (int i = 0; i < HistoryPageSize; i++)
            {
                int row = i;
                var button = MenuKit.WoodButton(_panel.transform, "", new Vector2(1, 1),
                    new Vector2(-390, -318 - i * 27), new Vector2(690, 25), () => OpenDetail(row));
                _historyRows.Add(button);

                var label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.fontSize = 17;
                _historyLabels.Add(label);
            }

            _pager = MenuKit.Label(_panel.transform, "", 18, UiTheme.CreamMuted,
                new Vector2(1, 0), new Vector2(-560, 118), new Vector2(400, 30), TextAnchor.MiddleLeft);
            MenuKit.WoodButton(_panel.transform, "NEWER", new Vector2(1, 0),
                new Vector2(-250, 118), new Vector2(150, 42), () => Page(-1));
            MenuKit.WoodButton(_panel.transform, "OLDER", new Vector2(1, 0),
                new Vector2(-90, 118), new Vector2(150, 42), () => Page(1));

            // ⚠️⚠️ THE MASTERY LIST (§ 2.1 item 7) IS BEHIND A BUTTON RATHER THAN ON THIS
            // PANEL, AND THAT IS A LAYOUT DECISION WITH A MEASUREMENT UNDER IT. Laid out where
            // it was first written, at 660 by 120 in the bottom-left, its top edge sat at 188 px
            // from the panel floor and the last stat row's box reached down to 156, so the two
            // overlapped by about 30 px, and its own bottom edge ran into the REFRESH and CLOSE
            // row. Eighteen characters plus six heroes is a grid, not a footnote: it gets its
            // own panel, the same way the match detail does.
            MenuKit.WoodButton(_panel.transform, "CHARACTERS", new Vector2(0, 0),
                new Vector2(150, 62), new Vector2(220, 46), OpenMastery);
            MenuKit.WoodButton(_panel.transform, "REFRESH", new Vector2(0, 0),
                new Vector2(380, 62), new Vector2(180, 46), RefreshFromServer);
            MenuKit.WoodButton(_panel.transform, "CLOSE", new Vector2(0, 0),
                new Vector2(570, 62), new Vector2(180, 46), Close);
        }

        private void BuildMastery()
        {
            _masteryPanel = new GameObject("MasteryPanel", typeof(RectTransform), typeof(Image));
            _masteryPanel.transform.SetParent(_canvas.transform, false);
            MenuKit.Place((RectTransform)_masteryPanel.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900, 760));
            _masteryPanel.GetComponent<Image>().color = UiTheme.WoodDark;

            MenuKit.Label(_masteryPanel.transform, "CHARACTERS", 30, UiTheme.Amber,
                new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(800, 44));

            _masteryBody = MenuKit.Label(_masteryPanel.transform, "", 19, UiTheme.Cream,
                new Vector2(0.5f, 1), new Vector2(0, -410), new Vector2(820, 660), TextAnchor.UpperLeft);

            MenuKit.WoodButton(_masteryPanel.transform, "BACK", new Vector2(0.5f, 0),
                new Vector2(0, 48), new Vector2(200, 48), () => _masteryPanel.SetActive(false));
        }

        private void OpenMastery()
        {
            WriteMastery(GameServices.Career?.Profile);
            _masteryPanel.SetActive(true);
        }

        private void BuildDetail()
        {
            _detail = new GameObject("MatchDetail", typeof(RectTransform), typeof(Image));
            _detail.transform.SetParent(_canvas.transform, false);
            MenuKit.Place((RectTransform)_detail.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1080, 760));
            _detail.GetComponent<Image>().color = UiTheme.WoodDark;

            MenuKit.Label(_detail.transform, "MATCH DETAIL", 30, UiTheme.Amber,
                new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(900, 44));

            _detailBody = MenuKit.Label(_detail.transform, "", 19, UiTheme.Cream,
                new Vector2(0.5f, 1), new Vector2(0, -410), new Vector2(980, 660), TextAnchor.UpperLeft);

            MenuKit.WoodButton(_detail.transform, "BACK", new Vector2(0.5f, 0),
                new Vector2(0, 48), new Vector2(200, 48), () => _detail.SetActive(false));
        }

        // -------------------------------------------------------------------
        // § WHAT IT SHOWS
        // -------------------------------------------------------------------

        private void Open()
        {
            _panel.SetActive(true);
            _page = 0;
            Refresh();
            RefreshFromServer();
        }

        private void Close()
        {
            _detail.SetActive(false);
            _masteryPanel.SetActive(false);
            _panel.SetActive(false);
        }

        private void SetMode(GameMode mode)
        {
            _mode = mode;
            Refresh();
        }

        private async void RefreshFromServer()
        {
            var career = GameServices.Career;
            if (career == null) return;

            await career.SyncAsync();
            await LoadPageAsync();
            Refresh();
        }

        private void Page(int delta)
        {
            _page = Mathf.Max(0, _page + delta);
            _ = LoadPageAsync();
        }

        private async System.Threading.Tasks.Task LoadPageAsync()
        {
            var career = GameServices.Career;
            if (career == null) return;

            _shown = await career.HistoryPageAsync(_page * HistoryPageSize, HistoryPageSize);

            // ⚠️ A PAGE PAST THE END STEPS BACK RATHER THAN DRAWING NOTHING. Pressing OLDER once
            // too often on a twenty-match career otherwise empties the list and looks like the
            // history was lost.
            if (_shown.Count == 0 && _page > 0)
            {
                _page--;
                _shown = await career.HistoryPageAsync(_page * HistoryPageSize, HistoryPageSize);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_panel == null || !_panel.activeSelf) return;

            var account = GameServices.Account;
            var career = GameServices.Career;
            if (career == null) return;

            var profile = career.Profile;
            var totals = ProfileRules.ModeFor(profile, _mode.ToString()).Totals;

            _handle.text = account != null ? account.LobbyName : "PLAYER";
            _identity.text = IdentityLine(account, profile);
            _status.text = career.Status +
                           (career.QueuedCount > 0 ? $"   ·   {career.QueuedCount} waiting to upload" : "");

            _modeTitle.text = _mode == GameMode.HeroStrike ? "HERO STRIKE" : "CLASSIC";
            _career.text = CareerStrip(profile);

            WriteStats(totals);
            WriteHistory();

            // ⚠️ ONLY WHILE IT IS OPEN. It is its own panel now, and rebuilding a string for
            // eighteen characters on every `Changed` from a screen nobody is looking at is the
            // shape `Hud`'s per-frame string rebuild took an eighth of the probe's frames with.
            if (_masteryPanel != null && _masteryPanel.activeSelf) WriteMastery(profile);
        }

        /// <summary>
        /// ⚠️ THE LEVEL AND THE RANK ARE DELIBERATELY ABSENT UNTIL SOMETHING AWARDS THEM. The
        /// document carries both fields from day one so no profile has to be migrated, and Phase 4
        /// and Phase 9 fill them. A header that draws "LEVEL 1" and an empty badge on every
        /// account in the game is telling every player about two systems that do not exist.
        /// </summary>
        private static string IdentityLine(Net.PlayerAccount account, PlayerProfile profile)
        {
            var parts = new List<string>();

            if (account != null && !string.IsNullOrEmpty(account.Country)) parts.Add(account.Country);
            if (account != null && !string.IsNullOrEmpty(account.Pronouns)) parts.Add(account.Pronouns);

            string age = AccountAge(profile.CreatedUtc);
            if (!string.IsNullOrEmpty(age)) parts.Add(age);

            if (account != null && !string.IsNullOrEmpty(account.Bio)) parts.Add(account.Bio);

            return parts.Count == 0 ? "No matches recorded yet" : string.Join("   ·   ", parts);
        }

        private static string AccountAge(string createdUtc)
        {
            if (!DateTime.TryParse(createdUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime created))
                return "";

            int days = Mathf.Max(0, (int)(DateTime.UtcNow - created).TotalDays);
            return days < 1 ? "First day" : $"{days} day{(days == 1 ? "" : "s")} old";
        }

        /// <summary>The career strip: totals across both modes, because "how much have you played"
        /// is not a per-mode question. Every per-mode number is in the stat block below.</summary>
        private static string CareerStrip(PlayerProfile profile)
        {
            int matches = 0, wins = 0, streak = 0;
            float seconds = 0.0f;

            foreach (var mode in profile.Modes)
            {
                if (mode?.Totals == null) continue;
                matches += mode.Totals.Matches;
                wins += mode.Totals.Wins;
                seconds += mode.Totals.SecondsPlayed;
                streak = Mathf.Max(streak, mode.Totals.LongestWinStreak);
            }

            if (matches == 0) return "No matches played yet. Finish one and it lands here.";

            var character = ProfileRules.Favourite(profile.Characters);
            var slipper = ProfileRules.Favourite(profile.Slippers);

            string rate = MatchRecordRules.IsReportable(matches)
                ? $"{MatchRecordRules.Rate(wins, matches) * 100.0f:0}% WIN RATE"
                : $"WIN RATE AFTER {MatchRecordRules.MinimumSampleForARate} MATCHES";

            return $"{matches} PLAYED   ·   {wins} WON   ·   {rate}   ·   " +
                   $"{seconds / 3600.0f:0.0} H   ·   BEST STREAK {streak}   ·   " +
                   $"{Name(character, Roster.AllPeople)}   ·   {Name(slipper, Roster.Slippers)}";
        }

        private static string Name(PickRecord pick, IReadOnlyList<RosterEntry> from)
        {
            if (pick == null) return "NO FAVOURITE YET";
            foreach (var entry in from)
                if (entry.Id == pick.Id) return $"{entry.Name} x{pick.Games}";
            return $"{pick.Id} x{pick.Games}";
        }

        private void WriteStats(CareerTotals t)
        {
            var rows = new List<string>
            {
                $"MATCHES              {t.Matches}",
                $"PLACEMENTS           1st {t.Placements[0]}   2nd {t.Placements[1]}   " +
                $"3rd {t.Placements[2]}   4th {t.Placements[3]}",
                $"KNOCKDOWNS           {t.Knockdowns}",
                Rate("KNOCKDOWNS / THROW", t.Knockdowns, t.Throws, "throws", "0.00"),
                $"RETRIEVALS           {t.Retrievals}   ({t.RetrievalsUnderPressure} under pressure)",
                Percent("UNDER PRESSURE", t.RetrievalsUnderPressure, t.Retrievals, "retrievals"),
                $"TAGS AS TAYA         {t.Tags}",
                Rate("TAGS / ROUND TAYA", t.Tags, t.RoundsDefended, "rounds defended", "0.00"),
                $"PASSIVE DEFENCE      {ProfileRules.PassiveDefenceSeconds(t):0} s",
                $"SABOTAGES            {t.Sabotages}",
                Percent("SHOVE HIT RATE", t.ShoveHits, t.ShoveAttempts, "shoves"),
                Percent("LUNGE HIT RATE", t.LungeHits, t.LungeAttempts, "lunges"),
                $"LONGEST LAST STAND   {t.LongestLastAttacker:0.0} s",
                Percent("CLUTCH RATE", t.Clutches, t.ComebackChances, "comeback chances"),
                $"FIRST THROW          {(t.MatchesWithAThrow > 0 ? $"{ProfileRules.AverageTimeToFirstThrow(t):0.0} s avg" : "no throws yet")}",
                $"DISTANCE / ROUND     {ProfileRules.DistancePerRound(t, MatchRules.RoundCountFor(_mode)):0} m",
            };

            // ⚠️ THE BLOCK HAS TO HOLD EVERY ROW IT WRITES. It was built with 14 slots against
            // a 16-row list once, and the two that fell off the end were the ones appended
            // after the fact, which is exactly the pair nobody would notice missing.
            Debug.Assert(_statRows.Count >= rows.Count,
                $"the stat block has {_statRows.Count} rows and {rows.Count} to write");

            for (int i = 0; i < _statRows.Count; i++)
                _statRows[i].text = i < rows.Count ? rows[i] : "";
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS `FUTURE.md` § 2.2'S RULE ON SCREEN, AND THE MESSAGE IS THE POINT OF IT. A
        /// hidden row with no explanation reads as a missing feature; a row that says what it is
        /// waiting for reads as a game that is being careful with its numbers. The raw counts are
        /// still shown, because a count of 3 is true at any sample size and only the RATE lies.
        /// </summary>
        private static string Percent(string caption, int hits, int attempts, string noun)
        {
            string pad = caption.PadRight(20);
            if (!MatchRecordRules.IsReportable(attempts))
                return $"{pad}{hits}/{attempts}   (needs {MatchRecordRules.MinimumSampleForARate} {noun})";
            return $"{pad}{MatchRecordRules.Rate(hits, attempts) * 100.0f:0}%   ({hits}/{attempts})";
        }

        private static string Rate(string caption, int numerator, int denominator, string noun, string format)
        {
            string pad = caption.PadRight(20);
            if (!MatchRecordRules.IsReportable(denominator))
                return $"{pad}{numerator}/{denominator}   (needs {MatchRecordRules.MinimumSampleForARate} {noun})";
            return $"{pad}{MatchRecordRules.Rate(numerator, denominator).ToString(format)}   " +
                   $"({numerator}/{denominator})";
        }

        private void WriteHistory()
        {
            string me = GameServices.Account?.ConnectionToken ?? Net.NetIdentity.Token;

            for (int i = 0; i < _historyRows.Count; i++)
            {
                bool live = i < _shown.Count;
                _historyRows[i].gameObject.SetActive(live);
                if (!live) continue;

                var record = _shown[i];
                var line = MatchRecordRules.LineFor(record, me);
                int place = line?.Placement ?? 0;

                _historyLabels[i].text =
                    $"{Ordinal(place)}  {record.Mode.ToUpperInvariant(),-11} {record.MapId,-14} " +
                    $"{(line?.Score ?? 0),5} PTS   {Ago(record.PlayedUtc)}";

                // ⚠️ COLOUR TRACKS PLACEMENT, NEVER TEAM IDENTITY, which is `MatchResult`'s rule
                // and § 4.2's hard one: there are no teams in this game.
                _historyLabels[i].color = place == 1 ? UiTheme.Amber
                    : place == Balance.PlayerCount ? UiTheme.CreamMuted
                    : UiTheme.Cream;
            }

            _pager.text = _shown.Count == 0
                ? "NO MATCHES ON THIS PAGE"
                : $"MATCHES {_page * HistoryPageSize + 1} TO {_page * HistoryPageSize + _shown.Count}";
        }

        private void WriteMastery(PlayerProfile profile)
        {
            if (_masteryBody == null || profile == null) return;

            var lines = new List<string>
            {
                $"{"CHARACTER",-14}{"GAMES",7}{"WON",7}",
                "",
            };

            foreach (var pick in profile.Characters)
            {
                if (pick == null || pick.Games <= 0) continue;
                string name = pick.Id;
                foreach (var entry in Roster.AllPeople) if (entry.Id == pick.Id) name = entry.Name;

                // ⚠️ THE SAME SAMPLE GATE AS EVERY OTHER RATE ON THIS SCREEN. A 100 per cent
                // win rate on one game with a character is the single most quotable wrong
                // number a profile can produce.
                lines.Add(MatchRecordRules.IsReportable(pick.Games)
                    ? $"{name,-14}{pick.Games,7}{MatchRecordRules.Rate(pick.Wins, pick.Games) * 100.0f,6:0}%"
                    : $"{name,-14}{pick.Games,7}{pick.Wins,7}");
            }

            if (lines.Count == 2) lines.Add("Nothing played yet.");
            _masteryBody.text = string.Join("\n", lines);
        }

        private void OpenDetail(int row)
        {
            if (row < 0 || row >= _shown.Count) return;

            var record = _shown[row];
            var lines = new List<string>
            {
                $"{record.Mode.ToUpperInvariant()}   ·   {record.MapId}   ·   {record.Rounds} ROUNDS   " +
                $"·   {record.DurationSeconds / 60.0f:0} MIN   ·   {(record.Online ? "ONLINE" : "OFFLINE")}",
                "",
                $"{"PLACE",-6}{"PLAYER",-18}{"PTS",6}{"KD",5}{"THR",5}{"RET",5}{"TAG",5}{"SAB",5}{"DEF s",7}",
            };

            foreach (var p in SortedByPlacement(record))
            {
                lines.Add($"{Ordinal(p.Placement),-6}{Short(p.Handle),-18}{p.Score,6}{p.Knockdowns,5}" +
                          $"{p.Throws,5}{p.Retrievals,5}{p.Tags,5}{p.Sabotages,5}" +
                          $"{MatchRecordRules.PassiveDefenceSeconds(p),7:0}");
            }

            lines.Add("");
            lines.Add("TAYA BY ROUND");
            for (int i = 0; i < record.DefenderByRound.Length; i++)
            {
                int slot = record.DefenderByRound[i];
                string who = slot >= 0 && slot < record.Players.Length
                    ? Short(record.Players[slot].Handle)
                    : $"P{slot + 1}";
                lines.Add($"   ROUND {i + 1}   {who}");
            }

            string me = GameServices.Account?.ConnectionToken ?? Net.NetIdentity.Token;
            if (MatchRecordRules.IsClutch(record, MatchRecordRules.LineFor(record, me)?.Slot ?? -1))
            {
                lines.Add("");
                lines.Add("CLUTCH: won from last place going into the final round.");
            }

            _detailBody.text = string.Join("\n", lines);
            _detail.SetActive(true);
        }

        private static List<PlayerMatchStats> SortedByPlacement(MatchRecord record)
        {
            var list = new List<PlayerMatchStats>(record.Players ?? Array.Empty<PlayerMatchStats>());

            // ⚠️ THE TIE-BREAK IS SLOT, LIKE `MatchDirector.Ranking`'S, AND FOR THE SAME REASON:
            // a board has to draw the rows in SOME order and must not reshuffle two tied players
            // between openings. The PLACEMENT they are labelled with is still shared.
            list.Sort((a, b) => a.Placement == b.Placement
                ? a.Slot.CompareTo(b.Slot)
                : a.Placement.CompareTo(b.Placement));
            return list;
        }

        private static string Short(string handle)
        {
            if (string.IsNullOrEmpty(handle)) return "PLAYER";
            return handle.Length <= 17 ? handle : handle.Substring(0, 17);
        }

        private static string Ordinal(int place)
        {
            switch (place)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                case 4: return "4th";
                default: return "-";
            }
        }

        private static string Ago(string playedUtc)
        {
            if (!DateTime.TryParse(playedUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime when))
                return "";

            TimeSpan since = DateTime.UtcNow - when;
            if (since.TotalMinutes < 60) return $"{Mathf.Max(1, (int)since.TotalMinutes)} min ago";
            if (since.TotalHours < 24) return $"{(int)since.TotalHours} h ago";
            return $"{(int)since.TotalDays} d ago";
        }
    }
}
