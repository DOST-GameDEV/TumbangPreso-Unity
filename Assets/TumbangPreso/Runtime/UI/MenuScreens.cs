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
    /// Host, join by address, or browse the LAN.
    ///
    /// ⚠️ THE LAN LIST REFRESHES ITSELF RATHER THAN NEEDING A BUTTON. Hosts broadcast once a
    /// second and entries expire after four missed beacons, so a list that only updated on
    /// demand would show hosts that had already quit and miss ones that had just opened.
    /// </summary>
    public sealed class MultiplayerSetupScreen : MonoBehaviour
    {
        private Text _status;
        private Text _lanList;
        private InputField _address;
        private Net.NetSession _net;

        private void Start()
        {
            _net = Net.NetSession.Ensure();
            _net.StatusChanged += OnStatus;

            var canvas = MenuKit.BuildCanvas(transform, "MultiplayerSetup");
            MenuKit.Backdrop(canvas.transform, UiTheme.WoodDark);

            MenuKit.Label(canvas.transform, "MULTIPLAYER", 60, UiTheme.Amber,
                          new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1000, 90));

            var size = new Vector2(420, 76);

            MenuKit.WoodButton(canvas.transform, "HOST A GAME", new Vector2(0.28f, 0.72f),
                               Vector2.zero, size, Host);

            MenuKit.WoodButton(canvas.transform, "JOIN", new Vector2(0.28f, 0.60f),
                               Vector2.zero, size, Join);

            _address = BuildAddressField(canvas.transform);

            MenuKit.WoodButton(canvas.transform, "REFRESH LAN", new Vector2(0.28f, 0.40f),
                               Vector2.zero, size, () => _net.BrowseLan());

            MenuKit.WoodButton(canvas.transform, "DISCONNECT", new Vector2(0.28f, 0.28f),
                               Vector2.zero, size, () => _net.Stop());

            MenuKit.Label(canvas.transform, "GAMES ON THIS NETWORK", 26, UiTheme.CreamMuted,
                          new Vector2(0.72f, 0.78f), Vector2.zero, new Vector2(600, 50));

            _lanList = MenuKit.Label(canvas.transform, "searching...", 24, UiTheme.Cream,
                                     new Vector2(0.72f, 0.55f), Vector2.zero,
                                     new Vector2(640, 420), TextAnchor.UpperLeft);

            _status = MenuKit.Label(canvas.transform, "offline", 26, UiTheme.Highlight,
                                    new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(1400, 60));

            MenuKit.WoodButton(canvas.transform, "BACK", new Vector2(0.5f, 0.06f),
                               Vector2.zero, new Vector2(280, 64),
                               () => SceneFlow.Go(SceneFlow.ModeSelect));

            _net.BrowseLan();
        }

        private InputField BuildAddressField(Transform parent)
        {
            var go = new GameObject("Address");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = UiTheme.WoodDark;
            MenuKit.Place(img.rectTransform, new Vector2(0.28f, 0.50f), Vector2.zero,
                          new Vector2(420, 62));

            var text = MenuKit.Label(go.transform, "127.0.0.1", 28, UiTheme.Cream,
                                     new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 56));

            var field = go.AddComponent<InputField>();
            field.textComponent = text;
            field.text = "127.0.0.1";
            return field;
        }

        private void Host()
        {
            if (_net.StartHost()) SceneFlow.Go(SceneFlow.MatchSetup);
        }

        private void Join()
        {
            string addr = string.IsNullOrWhiteSpace(_address.text) ? "127.0.0.1" : _address.text.Trim();
            _net.StartClient(addr);
        }

        private void OnStatus(string s)
        {
            if (_status != null) _status.text = s;
        }

        private void Update()
        {
            if (_lanList == null || _net == null) return;

            var sb = new System.Text.StringBuilder();
            int n = 0;

            foreach (var e in _net.LanEntries)
            {
                n++;
                string who = string.IsNullOrEmpty(e.HostName) ? "(unnamed)" : e.HostName;
                sb.AppendLine($"{who}   {e.Players}/{e.MaxPlayers}   {e.JoinCode}");
                sb.AppendLine($"   {e.Address}:{e.Port}{(e.InProgress ? "   in progress" : "")}");
                sb.AppendLine();
            }

            _lanList.text = n == 0
                ? "no games found on this network.\n\nhosts broadcast once a second;\nentries expire after four missed beacons."
                : sb.ToString();
        }

        private void OnDestroy()
        {
            if (_net != null) _net.StatusChanged -= OnStatus;
        }
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
        private Text _tabLabel;

        /// <summary>
        /// ⚠️⚠️ CHALK MARKS, NOT A TEXT BAR. The whole game is played inside a chalk court and
        /// the moodboard treats that as its signature; five tally marks scratched on the
        /// ground is what a kid keeping score in the street actually does. The hash-and-dot
        /// string this replaced was a placeholder, not the design.
        /// </summary>
        public const int TraitSlots = 5;
        public static readonly Vector2 TraitPipSize = new Vector2(42, 12);
        public const int TraitPipGap = 6;

        /// <summary>The same yellow as the base-circle decal and the timer's urgency state,
        /// so a full meter reads as the same game system rather than a new colour.</summary>
        public static readonly Color TraitPipFilled = new Color(0.973f, 0.816f, 0.157f);
        public static readonly Color TraitPipEmpty = new Color(0.961f, 0.902f, 0.784f, 0.20f);

        private readonly System.Collections.Generic.List<(Text label, Image[] pips)> _traitRows =
            new System.Collections.Generic.List<(Text, Image[])>();

        private ModelPreview _preview;
        private RosterBook _book;

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

            // The live 3D subject. It turns on its own and can be dragged; the preview owns
            // both rules.
            var previewPanel = new GameObject("PreviewPanel", typeof(RectTransform));
            previewPanel.transform.SetParent(canvas.transform, false);
            MenuKit.Place(previewPanel.GetComponent<RectTransform>(),
                new Vector2(0.26f, 0.52f), Vector2.zero, new Vector2(460, 560));

            _book = RosterBook.Load();
            _preview = gameObject.AddComponent<ModelPreview>();
            _preview.Attach(previewPanel.GetComponent<RectTransform>());

            for (int r = 0; r < 3; r++)
            {
                float y = 0.50f - r * 0.09f;

                var label = MenuKit.Label(canvas.transform, "", 24, UiTheme.Cream,
                    new Vector2(0.58f, y), Vector2.zero, new Vector2(220, 36),
                    TextAnchor.MiddleLeft);

                var pips = new Image[TraitSlots];
                for (int p = 0; p < TraitSlots; p++)
                {
                    var go = new GameObject($"Pip{r}_{p}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(canvas.transform, false);

                    var img = go.GetComponent<Image>();
                    img.color = TraitPipEmpty;
                    img.raycastTarget = false;

                    MenuKit.Place(img.rectTransform, new Vector2(0.72f, y),
                        new Vector2(p * (TraitPipSize.x + TraitPipGap), 0), TraitPipSize);

                    pips[p] = img;
                }

                _traitRows.Add((label, pips));
            }

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
            int[] points = { entry.Bilis, entry.Lakas, entry.Tatag };

            for (int r = 0; r < _traitRows.Count; r++)
            {
                _traitRows[r].label.text = labels[r];

                for (int p = 0; p < TraitSlots; p++)
                    _traitRows[r].pips[p].color = p < points[r] ? TraitPipFilled : TraitPipEmpty;
            }

            RefreshPreview();
        }

        /// <summary>A lata and a tsinelas lie on the ground and need the steeper look-down
        /// angle; a Person stands.</summary>
        private void RefreshPreview()
        {
            if (_preview == null || _book == null) return;

            int index = _pick[_tab];
            RosterEntryAsset art = _tab == 0 ? _book.PersonArt(index)
                                 : _tab == 1 ? _book.CanArt(index)
                                 : _book.SlipperArt(index);

            _preview.Show(art != null ? art.Model : null, flat: _tab != 0);
        }

        private void Update()
        {
            if (_preview == null) return;

            // Drag to turn the subject. The first drag ends the idle sweep for good.
            if (Input.GetMouseButton(0))
                _preview.Orbit(new Vector2(Input.GetAxisRaw("Mouse X") * 10.0f,
                                           Input.GetAxisRaw("Mouse Y") * 10.0f));

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) _preview.Zoom(wheel);
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
