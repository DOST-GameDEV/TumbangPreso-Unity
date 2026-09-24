using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The queue as the hub sees it: whether this machine's room came from PLAY, and when the match
    /// counts as FOUND.
    ///
    /// ⚠️⚠️ FOUND IS READ OFF THE REPLICATED ROSTER, SO EVERY PEER REACHES IT WITHOUT A NEW MESSAGE.
    /// The queue is `Matchmaker`'s lobby-based search (it joins a room or hosts one), so a match is
    /// found when:
    ///
    ///   the host's room is full           every seat occupied by a human
    ///   the host accepted bots            Phase 11's offer, pressed on the plate
    ///   a joiner is seated in a room      its own ticket found a match; it picks while the host fills
    ///
    /// No wire change: `NetSession.ProtocolVersion` does not move (`CLAUDE.md` § 4a).
    /// </summary>
    public static class HubQueueWatch
    {
        public static bool QueueRoom { get; private set; }
        public static bool BotsAccepted { get; private set; }
        public static bool Found { get; private set; }
        public static float StartedAt { get; private set; }
        public static GameMode Mode { get; private set; }
        public static QueueStake Stake { get; private set; }

        public static void Begin(GameMode mode, QueueStake stake)
        {
            QueueRoom = true;
            BotsAccepted = false;
            Found = false;
            Mode = mode;
            Stake = stake;
            StartedAt = Time.unscaledTime;
        }

        public static void AcceptBots() => BotsAccepted = true;

        public static void End()
        {
            QueueRoom = false;
            BotsAccepted = false;
            Found = false;
        }

        public static int Occupied(IHubHost host)
        {
            int n = 0;
            foreach (var seat in host.Seats()) if (seat.Occupied) n++;
            return n;
        }

        public static void Tick(TumpHub hub)
        {
            if (!QueueRoom) return;
            var host = hub.Host;
            var queue = Matchmaker.Current;
            bool searching = queue != null && queue.IsQueueing;

            if (!searching && !host.InRoom)
            {
                // The room went away under a found match, or the search died with a refusal.
                bool wasFound = Found;
                End();
                if (wasFound) { hub.Home(); hub.Toast("Match unavailable."); }
                return;
            }

            if (Found) return;

            bool full = host.InRoom && Occupied(host) >= Balance.PlayerCount;
            bool joiner = host.InRoom && !host.IsHost && queue != null && queue.State == QueueState.Found;
            if (!full && !BotsAccepted && !joiner) return;

            Found = true;

            // ⚠️ THE ROOM LEAVES THE POOL THE MOMENT IT IS FOUND, or a stranger joins a match that is
            // already picking. `Matchmaker.Cancel` clears the advert and keeps the room.
            if (host.IsHost && searching) queue.Cancel();

            hub.Home();
            hub.Push<HubMatchFound>();
        }
    }

    /// <summary>
    /// The top-centre plate while queued: elapsed time and an X. The owner's final HOME sketch.
    ///
    /// ⚠️ IT SITS ON THE HUB'S OVERLAY RATHER THAN ON HOME, so it stays visible while the player
    /// checks their hero or loadout during a search, which is when a queue timer matters most.
    /// The bot offer appears under it rather than growing it, so X never moves under a cursor.
    /// </summary>
    public sealed class HubQueuePlate : MonoBehaviour
    {
        public bool Allowed = true;
        private TumpHub _hub;
        private RectTransform _root;
        private Text _clock, _line;
        private HubButton _cancel, _bots;
        private int _shownSecond = -1;

        public static HubQueuePlate Build(RectTransform parent, TumpHub hub)
        {
            // ⚠️ THE SCRIPT LIVES ON A HOLDER THAT IS NEVER SWITCHED OFF. The first hub run put it on
            // the plate itself, which is hidden until a queue starts, so its `Update` could never
            // run to show it: a plate that only ever appeared if something else woke it.
            var holder = HubKit.Stretch(HubKit.Rect(parent, "QueuePlateHolder"));
            var plate = holder.gameObject.AddComponent<HubQueuePlate>();
            bool larger = Settings.SettingsStore.Current.LargerText;
            var root = HubKit.Place(HubKit.Rect(holder, "QueuePlate"), HubKit.Top, new Vector2(0, -24), new Vector2(540, larger ? 148 : 118));
            plate._hub = hub;
            plate._root = root;

            // A hanging ticket: the dark night sticker, cut at the corners, no shadow (it is hung,
            // not stuck down), with a persimmon keel along its bottom.
            var shape = HubKit.Shape(root, "Ticket", HubStyle.Night, true, 44, 5, 26);
            HubKit.Stretch(shape.rectTransform);
            shape.ShadowOffset = new Vector2(0, -8);
            shape.BandFraction = 0.12f;
            shape.Outline = HubStyle.Ink;

            var clockIcon = HubKit.Glyph(root, "ClockIcon", HubGlyph.Mark.Clock, HubStyle.Golden);
            HubKit.Place(clockIcon.rectTransform, HubKit.Left, new Vector2(26, 8), new Vector2(46, 46));

            plate._clock = HubKit.Text(root, "Elapsed", "0:00", HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(plate._clock.rectTransform, HubKit.Left, new Vector2(84, larger ? 24 : 12), new Vector2(210, larger ? 92 : 84));

            plate._line = HubKit.Text(root, "QueueLine", "SEARCHING", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(plate._line.rectTransform, HubKit.BottomLeft, new Vector2(30, larger ? 6 : 10), new Vector2(390, larger ? 44 : 36));

            plate._cancel = HubKit.IconButton(root, "CancelQueue", HubGlyph.Mark.Close, HubStyle.DeepRed, () => hub.Host.CancelQueue(), 71);
            HubKit.Place((RectTransform)plate._cancel.transform, HubKit.Right, new Vector2(-24, 4), new Vector2(84, 84));

            plate._bots = HubKit.Button(holder, "PlayWithBots", "PLAY WITH BOTS", HubStyle.Golden, () =>
            {
                HubQueueWatch.AcceptBots();
                hub.Host.AcceptBots();
            }, HubStyle.Body, 72, HubGlyph.Mark.Bots);
            HubKit.Place((RectTransform)plate._bots.transform, HubKit.Top, new Vector2(0, larger ? -196 : -156), new Vector2(360, 76));

            root.gameObject.SetActive(false);
            plate._bots.gameObject.SetActive(false);
            return plate;
        }

        private void Update()
        {
            bool show = Allowed && HubQueueWatch.QueueRoom && !HubQueueWatch.Found;
            if (_root.gameObject.activeSelf != show)
            {
                _root.gameObject.SetActive(show);
                if (show) HubSlap.On(_root, 0, 0);
                _hub.RefreshFocus();
            }

            var queue = Matchmaker.Current;
            bool offer = show && queue != null && queue.OffersBotFill;
            if (_bots.gameObject.activeSelf != offer)
            {
                _bots.gameObject.SetActive(offer);
                if (offer) HubSlap.On(_bots.transform, 0, -2);
            }
            if (!show) { _shownSecond = -1; return; }

            int seconds = Mathf.FloorToInt(Time.unscaledTime - HubQueueWatch.StartedAt);
            if (seconds == _shownSecond) return;
            _shownSecond = seconds;
            _clock.text = $"{seconds / 60}:{seconds % 60:00}";

            var host = _hub.Host;
            string mode = HubQueueWatch.Stake == QueueStake.Ranked ? "RANKED"
                        : HubQueueWatch.Mode == GameMode.Classic ? "CLASSIC" : "HERO STRIKE";
            if (queue != null && queue.State == QueueState.Refused)
                _line.text = queue.Refusal;
            else if (host.InRoom && HubQueueWatch.Occupied(host) > 1)
                _line.text = mode + "  ·  " + HubQueueWatch.Occupied(host) + " OF " + Balance.PlayerCount;
            else
                _line.text = mode + "  ·  SEARCHING";
            HubKit.Fit(_line);
        }
    }

    /// <summary>
    /// MATCH FOUND: a beat, not a screen with a decision on it. It advances by itself to
    /// CHARACTER SELECT, the way every queue pop in every game the player already knows does.
    /// </summary>
    public sealed class HubMatchFound : HubScreen
    {
        public override bool ShowsQueuePlate => false;
        public override float CourtShade => 0.55f;
        private float _until;

        public override void Build()
        {
            // ⚠️ THE ONE BIG BEAT IN THE FRONT END (`HubScenery`, 2026-09-24). It was a flat band
            // with a word on it. Now the fiesta arrives: rays burst out over the court, the band
            // slams down too big and jolts the screen once, and the bunting drops in on its
            // strings. 1.6 seconds, no decision on it, and reduced motion keeps it all still.
            var rays = HubScenery.Burst(Root, "Rays", new Color(HubStyle.Golden.r, HubStyle.Golden.g, HubStyle.Golden.b, 0.22f), 9, 24);
            HubKit.Place(rays.rectTransform, HubKit.Centre, new Vector2(0, 40), new Vector2(40, 40));
            HubScenery.Bunting(Root, 3, 0, 0.12f);
            var band = HubKit.Span(HubKit.Rect(Root, "Band"), new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                                   new Vector2(-40, -150), new Vector2(-40, -150));
            var tape = HubKit.Shape(band, "Tape", HubStyle.Chartreuse, true, 12, 6, 10);
            HubKit.Stretch(tape.rectTransform);
            tape.BandFraction = 0.14f;

            var words = HubKit.Text(band, "Heading", "MATCH FOUND", HubStyle.Hero, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Stretch(words.rectTransform);

            string mode = HubQueueWatch.Stake == QueueStake.Ranked ? "RANKED  ·  HERO STRIKE"
                        : HubQueueWatch.Mode == GameMode.Classic ? "CLASSIC" : "HERO STRIKE";
            var line = HubKit.Text(Root, "ModeLine", mode, HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(line.rectTransform, HubKit.Centre, new Vector2(0, -200), new Vector2(1200, 70));

            band.localRotation = Quaternion.Euler(0, 0, -3);
            HubImpact.On(band, Root, 0);
            HubSlap.On(line.rectTransform, 0.24f, 2);
            MenuSfx.Start();
            _until = Time.unscaledTime + 1.6f;
        }

        public override bool Back() => true;

        public override void Tick()
        {
            if (Time.unscaledTime < _until) return;
            Hub.Pop(this);
            Hub.Push<HubCharacterSelect>(s => s.Timed = true);
        }
    }

    /// <summary>A short line at the bottom of the screen that says what just happened.</summary>
    public sealed class HubToast : MonoBehaviour
    {
        private RectTransform _root;
        private Text _words;
        private float _until;

        public static HubToast Build(RectTransform parent)
        {
            var root = HubKit.Place(HubKit.Rect(parent, "Toast"), HubKit.Bottom, new Vector2(0, 150), new Vector2(980, 88));
            var toast = root.gameObject.AddComponent<HubToast>();
            toast._root = root;
            var shape = HubKit.Shape(root, "Plate", HubStyle.Night, false, 3, 4, 22);
            HubKit.Stretch(shape.rectTransform);
            toast._words = HubKit.Text(root, "Words", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Stretch(toast._words.rectTransform, 18);
            root.gameObject.SetActive(false);
            return toast;
        }

        public void Show(string words)
        {
            if (string.IsNullOrWhiteSpace(words)) return;
            _words.text = words;
            // Refusals often need two lines, especially with Larger text. Size this one temporary
            // message from its actual content instead of letting it overflow an88-unit plate.
            _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(88, _words.preferredHeight + 36));
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            HubSlap.On(_root, 0, 0);
            _until = Time.unscaledTime + 3.2f;
        }

        private void Update()
        {
            if (_root.gameObject.activeSelf && Time.unscaledTime > _until) _root.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// A warm darkening at the edges of the court, so the corner stickers sit on a quieter ground.
    /// ⚠️ It is the only thing drawn between the court and the stickers, and it stops a third of the
    /// way in: the middle of the court, which is the picture, is untouched (§ 6.2c row 3).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubVignette : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Color32 edge = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0.62f);
            Color32 clear = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0);
            float x = r.width * 0.30f, y = r.height * 0.34f;
            Band(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + x, r.yMax), new Vector2(r.xMin + x, r.yMin), edge, clear);
            Band(vh, new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax - x, r.yMin), new Vector2(r.xMax - x, r.yMax), edge, clear);
            Band(vh, new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + y), new Vector2(r.xMax, r.yMin + y), edge, clear);
            Band(vh, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - y * 0.6f), new Vector2(r.xMin, r.yMax - y * 0.6f), edge, clear);
        }

        private static void Band(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 outer, Color32 inner)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, outer, Vector2.zero);
            vh.AddVert(b, outer, Vector2.zero);
            vh.AddVert(c, inner, Vector2.zero);
            vh.AddVert(d, inner, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
