using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § THE PLAYERS' SCORES, flush with the left screen edge (live HUD, 2026-11-01;
    /// `boot_match.others_pop`, `tab_board` and `score_plate`).
    ///
    /// Every seat in the match, yours included and ranked with the rest, so you can read your score
    /// against the others (owner, 2026-11-03). Each is a grey woven plate with its seat colour as a strip
    /// on the screen edge, a nearly solid section holding the hero (every hero the same height, centred by
    /// its own ink), the score, a crown on the unique leader and, for a moment, the points the seat just
    /// won on a green tag. Your own plate is underlined in lime. The plates slide in from the
    /// left when any seat scores or the lead changes and slide out three seconds later. When places
    /// change the plates trade rows in the open: the overtaker swings out and over the one it passed.
    ///
    /// ⚠️ HOLD TAB FOR THE TABLE. The same plates with rank numbers, staying up as long as it is held;
    /// no panel and no dim behind them, so the court stays in view. `Scoreboard` is
    /// its own rebindable key; ability details moved to F1 (owner, 2026-11-02). A pad and a touch screen
    /// have no spare control for it, so there the board still opens on the info hold.
    ///
    /// ⚠️ PRESENTATION ONLY. It reads `MatchDirector.ScoreFor` and the seat's character, and never
    /// writes anything back. Motion uses unscaled time, Reduced UI Motion keeps the fades and drops
    /// the sliding, and nothing here allocates per frame.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private const float BoardTop = 150, BoardWidth = 276, BoardHeight = 58, BoardStep = 70;
        private const float BoardHold = 3f, BoardIn = .3f, BoardOut = .22f, BoardStagger = .04f;
        private const float BoardSwap = .7f, BoardSwapClimb = 44, BoardSwapSink = 16, BoardSwapSwell = .07f, BoardCrownPop = .4f;

        private sealed class BoardPlate
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Body, Strip, Hero, You;
            public Text Score, Rank;
            public HudBadge Crown;
            public RectTransform Tag;
            public Text TagLabel;
            public CharacterMotor Actor;
            public float Amount, Delay, Y = float.NaN, Width;
            public bool Want;
            public int ShownScore = int.MinValue, ShownRank = -1, ShownGain;
            public bool ShownCrown, ShownLocal;
            public int LastRank = -1, SwapDirection;
            public float SwapAt = -10, SwapFrom, CrownAt = -10;
        }

        private RectTransform _boardRoot;
        private readonly BoardPlate[] _plates = new BoardPlate[4];
        private readonly int[] _boardOrder = new int[4], _boardScore = new int[4];
        private readonly int[] _boardGain = new int[4];
        private readonly float[] _boardGainAt = { -10, -10, -10, -10 };
        private float _boardPopUntil = -10;
        private int _boardLeader = -2, _boardLeaderSeen = -2;
        private InputAction _boardKey, _boardPadKey;
        private bool _playing;
        private CharacterMotor _playingAs;
        private static readonly Color BoardCream = new Color32(0xF3, 0xEA, 0xE2, 255), BoardAmber = new Color32(0xFF, 0xC4, 0x40, 255);

        /// <summary>True while the table is held: the scoreboard key, or the info hold on a pad or touch.</summary>
        public bool BoardHeld => _playing && (_boardCapture || _boardKey != null && _boardKey.IsPressed()
                                              || _boardPadKey != null && _boardPadKey.IsPressed() && _boardPadKey.activeControl?.device is Gamepad
                                              || InputLayer.TouchInput.ScreenHeld("AbilityInfo"));

        private bool _boardCapture;

        /// <summary>Review captures only: shows the table as if the info key were held.</summary>
        public void HoldBoardForCapture(bool held) => _boardCapture = held;

        /// <summary>For tests: how many plates are on screen, however far they have slid.</summary>
        public int BoardPlatesShown
        {
            get { int n = 0; foreach (var p in _plates) if (p != null && p.Amount > 0) n++; return n; }
        }

        private void BuildBoard()
        {
            _boardRoot = OwnerUiLayout.Rect(_root, "OtherScores");
            _boardRoot.anchorMin = _boardRoot.anchorMax = new Vector2(0, 1); _boardRoot.pivot = new Vector2(0, 1);
            _boardRoot.anchoredPosition = Vector2.zero; _boardRoot.sizeDelta = new Vector2(1, 1);
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            _boardKey = asset?.FindActionMap("Player", false)?.FindAction("Scoreboard", false); _boardKey?.Enable();
            _boardPadKey = asset?.FindActionMap("Player", false)?.FindAction("AbilityInfo", false); _boardPadKey?.Enable();
            for (int seat = 0; seat < 4; seat++) _plates[seat] = BuildPlate(seat);
        }

        private BoardPlate BuildPlate(int seat)
        {
            var p = new BoardPlate();
            p.Root = OwnerUiLayout.Rect(_boardRoot, "ScorePlate" + seat);
            p.Root.anchorMin = p.Root.anchorMax = new Vector2(0, 1); p.Root.pivot = new Vector2(0, 1);
            p.Root.sizeDelta = new Vector2(BoardWidth, BoardHeight + 5);
            p.Group = p.Root.gameObject.AddComponent<CanvasGroup>();
            p.Group.blocksRaycasts = false; p.Group.interactable = false; p.Group.alpha = 0;

            p.Body = OwnerUiLayout.Rect(p.Root, "Plate").gameObject.AddComponent<Image>();
            p.Body.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + "plate-grey", new Vector4(100, 0, 24, 0));
            p.Body.type = Image.Type.Sliced; p.Body.raycastTarget = false;
            OwnerUiLayout.Place(p.Body.rectTransform, 0, 0, BoardWidth, BoardHeight + 5);

            // The seat colour along the screen edge, over the plate's first nine pixels.
            p.Strip = OwnerUiLayout.Rect(p.Root, "SeatStrip").gameObject.AddComponent<Image>();
            p.Strip.raycastTarget = false; p.Strip.color = PlayerIdentity.Colour(seat);
            OwnerUiLayout.Place(p.Strip.rectTransform, 0, 0, 9, BoardHeight);

            p.Hero = OwnerUiLayout.Rect(p.Root, "Hero").gameObject.AddComponent<Image>();
            p.Hero.raycastTarget = false; p.Hero.preserveAspect = true;

            p.Score = OwnerUiLayout.Text(p.Root, "Score", "", 34, OwnerUiLayout.TypeRole.Display);
            if (InGameTypography.Clock != null) p.Score.font = InGameTypography.Clock;
            p.Score.color = BoardCream; p.Score.alignment = TextAnchor.MiddleLeft;
            p.Score.horizontalOverflow = HorizontalWrapMode.Overflow; p.Score.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Place(p.Score.rectTransform, 104, 0, 130, BoardHeight - 4);

            p.Rank = OwnerUiLayout.Text(p.Root, "Rank", "", 30, OwnerUiLayout.TypeRole.Display);
            if (InGameTypography.Clock != null) p.Rank.font = InGameTypography.Clock;
            p.Rank.color = BoardCream; p.Rank.alignment = TextAnchor.MiddleCenter;
            p.Rank.horizontalOverflow = HorizontalWrapMode.Overflow; p.Rank.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Place(p.Rank.rectTransform, BoardWidth - 34 - 30, 0, 60, BoardHeight - 4);

            // Your own plate is underlined in lime, so you find yourself among the others.
            p.You = OwnerUiLayout.Rect(p.Root, "You").gameObject.AddComponent<Image>();
            p.You.raycastTarget = false; p.You.color = LiveHud.Lime;
            OwnerUiLayout.Place(p.You.rectTransform, 0, BoardHeight - 2, BoardWidth - 12, 4);
            p.You.enabled = false;

            p.Crown = OwnerUiLayout.Rect(p.Root, "LeaderCrown").gameObject.AddComponent<HudBadge>();
            p.Crown.raycastTarget = false;
            OwnerUiLayout.Place(p.Crown.rectTransform, 48 - 28, -36, 56, 46);

            p.Tag = LiveHud.Tag(p.Root, "Points", "+100", Hub.Knit.ThinGo, LiveHud.GoInk, 28);
            p.Tag.anchorMin = p.Tag.anchorMax = new Vector2(0, 1); p.Tag.pivot = new Vector2(0, .5f);
            p.TagLabel = p.Tag.GetComponentInChildren<Text>();
            p.Tag.gameObject.SetActive(false);
            return p;
        }

        /// <summary>
        /// Names the changes that make the plates appear: any seat's score moving, or the lead changing hands.
        /// Called from <see cref="Scores"/> with the numbers the match already published.
        /// </summary>
        private void NoteBoardScore(int slot, int score, int previous, bool known)
        {
            if (!known || score == previous) return;
            _boardGain[slot] = score - previous; _boardGainAt[slot] = LiveHud.Now;
            _boardPopUntil = LiveHud.Now + BoardHold;
        }

        private void NoteBoardLeader(int leader)
        {
            if (_boardLeaderSeen != -2 && leader != _boardLeaderSeen) _boardPopUntil = LiveHud.Now + BoardHold;
            _boardLeaderSeen = _boardLeader = leader;
        }

        private static Sprite PlateHero(CharacterMotor actor, out bool baked)
        {
            var people = Roster.GetPeople(actor.Mode);
            Sprite art = null;
            if (actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count)
                art = LiveHud.Picture("hero-plate/" + people[actor.CharacterIndex].Id);
            baked = art != null;
            return baked ? art : PortraitFor(actor);
        }

        /// <summary>Slides the plates in and out, ranks them and keeps their words current. Once per frame, from LateUpdate.</summary>
        private void PaintBoard()
        {
            if (_boardRoot == null) return;
            var match = GameServices.Match; var round = GameServices.Round;
            bool play = _playing && match != null && round != null;
            float now = LiveHud.Now, dt = LiveHud.Delta;
            bool still = LiveHud.Still;
            bool tab = play && BoardHeld;
            bool pop = play && !Hud.OnTouch && now < _boardPopUntil;
            int mine = _playingAs != null ? _playingAs.PlayerSlot : -1;

            // Rank the seats that are in the match, highest score first, ties by seat.
            int n = 0;
            for (int seat = 0; seat < 4; seat++)
            {
                if (!play || round.PlayerAt(seat) == null) continue;
                _boardScore[seat] = match.ScoreFor(seat);
                int at = n++;
                while (at > 0 && (_boardScore[_boardOrder[at - 1]] < _boardScore[seat])) { _boardOrder[at] = _boardOrder[at - 1]; at--; }
                _boardOrder[at] = seat;
            }
            for (int seat = 0; seat < 4; seat++)
            {
                var p = _plates[seat];
                int rank = -1, slot = -1;
                for (int r = 0; r < n; r++) if (_boardOrder[r] == seat) { rank = r; break; }
                if (rank >= 0)
                {
                    slot = rank;
                    // A change of place: the plates arrive in the old order first, then trade rows.
                    if (p.LastRank >= 0 && rank != p.LastRank && !still)
                    {
                        p.SwapFrom = p.Amount > 0 && !float.IsNaN(p.Y) ? p.Y : BoardTop + p.LastRank * BoardStep;
                        p.SwapAt = now + (p.Amount < 1 ? BoardIn : 0);
                        p.SwapDirection = rank < p.LastRank ? 1 : -1;
                        if (p.SwapDirection > 0) p.Root.SetAsLastSibling();
                    }
                    p.LastRank = rank;
                }
                bool want = rank >= 0 && (tab || pop);
                if (want && !p.Want) { p.Want = true; p.Delay = tab ? slot * BoardStagger : 0; if (p.Amount <= 0) p.Y = float.NaN; }
                else if (!want && p.Want) { p.Want = false; p.Delay = 0; }
                if (p.Delay > 0) p.Delay -= dt;
                else p.Amount = Mathf.MoveTowards(p.Amount, p.Want ? 1 : 0, dt / (p.Want ? BoardIn : BoardOut));
                if (p.Amount <= 0 && !p.Want)
                {
                    if (p.Group.alpha != 0) p.Group.alpha = 0;
                    if (p.Root.gameObject.activeSelf) p.Root.gameObject.SetActive(false);
                    continue;
                }
                if (!p.Root.gameObject.activeSelf) p.Root.gameObject.SetActive(true);

                // Content, rewritten only when it changes.
                var actor = round.PlayerAt(seat);
                if (actor != null && actor != p.Actor)
                {
                    p.Actor = actor;
                    var hero = PlateHero(actor, out bool baked);
                    p.Hero.sprite = hero; p.Hero.enabled = hero != null;
                    var size = hero == null ? Vector2.zero : baked ? hero.rect.size * .5f : new Vector2(48, 48);
                    OwnerUiLayout.Place(p.Hero.rectTransform, 48 - size.x * .5f, 5 + 24 - size.y * .5f, size.x, size.y);
                }
                if (rank >= 0)
                {
                    int score = _boardScore[seat];
                    if (p.ShownScore != score) { p.ShownScore = score; p.Score.text = ScoreTextForChip(score); }
                    bool local = seat == mine;
                    float width = BoardWidth;
                    p.Width = width;
                    if (p.You.enabled != local) p.You.enabled = local;
                    bool rankShown = tab;
                    if (p.Rank.enabled != rankShown) p.Rank.enabled = rankShown;
                    if (rankShown && p.ShownRank != rank)
                    {
                        p.ShownRank = rank; p.Rank.text = (rank + 1).ToString();
                        p.Rank.color = rank == 0 ? BoardAmber : BoardCream;
                    }
                    bool crown = _boardLeader == seat;
                    if (p.ShownCrown != crown && crown) p.CrownAt = now;
                    if (p.ShownCrown != crown) { p.ShownCrown = crown; p.Crown.Show(crown ? HudBadge.Glyph.Crown : HudBadge.Glyph.None, HudDraw.Honey, Color.clear); }
                    int gain = !tab && now - _boardGainAt[seat] < BoardHold ? _boardGain[seat] : 0;
                    bool tagged = gain > 0;
                    if (p.Tag.gameObject.activeSelf != tagged) p.Tag.gameObject.SetActive(tagged);
                    if (tagged && p.ShownGain != gain)
                    {
                        p.ShownGain = gain; p.TagLabel.text = "+" + gain;
                        LiveHud.SizeTag(p.Tag, p.TagLabel);
                        p.Tag.anchoredPosition = new Vector2(width + 14, -(BoardHeight * .5f));
                    }
                }

                // Pose: the row eases to its place, the plate slides from the screen's edge (a fade under Reduced UI Motion).
                float target = BoardTop + slot * BoardStep;
                // A plate changing places swings out on an arc: the climber to the right and over the top, the one passed back against the edge.
                float swap = still ? 1 : (now - p.SwapAt) / BoardSwap;
                float swing = swap > 0 && swap < 1 ? Mathf.Sin(swap * Mathf.PI) : 0;
                if (slot >= 0 && swap < 1) p.Y = Mathf.Lerp(p.SwapFrom, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(swap)));
                else if (slot >= 0) p.Y = float.IsNaN(p.Y) || still ? target : Mathf.Lerp(p.Y, target, 1 - Mathf.Exp(-14 * dt));
                float e = LiveHud.EaseOut(p.Amount);
                float x = still ? 0 : -(p.Width + 24) * (1 - e);
                x += swing * (p.SwapDirection > 0 ? BoardSwapClimb : -BoardSwapSink);
                p.Root.anchoredPosition = new Vector2(x, -(float.IsNaN(p.Y) ? target : p.Y));
                float swell = p.SwapDirection > 0 ? 1 + swing * BoardSwapSwell : 1;
                if (p.Root.localScale.x != swell) p.Root.localScale = new Vector3(swell, swell, 1);
                float crownPop = still ? 1 : LiveHud.Pop((now - p.CrownAt) / BoardCrownPop);
                if (p.Crown.rectTransform.localScale.x != crownPop) p.Crown.rectTransform.localScale = new Vector3(crownPop, crownPop, 1);
                p.Group.alpha = still ? p.Amount : Mathf.Clamp01(p.Amount * 3);
            }
        }
    }
}
