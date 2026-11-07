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
            // The search as a raffle ticket (`boot_menus.py`): the elapsed time and the mode on its body,
            // the cancel torn off on the stub. The refusal line shares the mode's line, so it is still read.
            var root = HubKit.Rect(holder, "QueuePlate");
            HubKnit.At(root, 660, 20, 600, 128);
            plate._hub = hub;
            plate._root = root;
            var ticket = HubKit.Rect(root, "Ticket").gameObject.AddComponent<RawImage>();
            ticket.texture = Resources.Load<Texture2D>("UI/boot-kit/queue-ticket");
            ticket.raycastTarget = false;
            HubKit.Stretch(ticket.rectTransform);
            var inkTime = HubBoard.Colour("#4A1018");
            plate._clock = HubKnit.Display(root, "Elapsed", "00:00", 70, inkTime, TextAnchor.MiddleCenter);
            HubKnit.In(plate._clock.rectTransform, 20, 9, 430, 70);

            plate._line = HubKnit.Words(root, "QueueLine", "SEARCHING", larger ? 30 : 28, HubBoard.Colour("#8A3A0C"), TextAnchor.MiddleCenter, true);
            HubKnit.In(plate._line.rectTransform, 20, 76, 430, 36);

            plate._cancel = HubKnit.Button(root, "CancelQueue", null, Knit.Danger, () => hub.Host.CancelQueue(), HubStyle.Label, HubGlyph.Mark.Close);
            // Centred up and down on the ticket's yellow face (owner, 2026-11-07), slab included: measured in a capture, the
            // face runs 6 to 118 of the plate and the drawn button sits 4 below its rect's top, so the rect starts 24 down.
            // Across, centred between the tear line (450 of the ticket's 600) and the bite in its end, as the board places it.
            HubKnit.In((RectTransform)plate._cancel.transform, 464, 24, 100, 72);

            plate._bots = HubKnit.Button(holder, "PlayWithBots", "PLAY WITH BOTS", Knit.Yellow, () =>
            {
                HubQueueWatch.AcceptBots();
                hub.Host.AcceptBots();
            }, HubStyle.Body, HubGlyph.Mark.Bots);
            HubKit.Place((RectTransform)plate._bots.transform, HubKit.Top, new Vector2(0, larger ? -206 : -178), new Vector2(360, 76));

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
            _clock.text = $"{seconds / 60:00}:{seconds % 60:00}";

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
        public override bool WearsBoard => true;
        public override bool ShowsQueuePlate => false;
        public override float CourtShade => 0;
        private float _until;

        public override void Build()
        {
            // ⚠️ THE ONE BIG BEAT IN THE FRONT END (`HubScenery`, 2026-09-24): the panel slams down too
            // big and jolts the screen once. 1.6 seconds, no decision on it, and reduced motion keeps it still.
            HubKnit.Ground(Root);
            var plate = HubKnit.At(HubKnit.Plate(Root, "Band", Knit.Panel), 260, 330, 1400, 394);
            var words = HubKnit.Display(HubKnit.FaceOf(plate), "Heading", "MATCH FOUND", 150, HubKnit.KitMaroon, TextAnchor.MiddleCenter);
            HubKnit.In(words.rectTransform, 0, 40, 1400, 190);

            string mode = HubQueueWatch.Stake == QueueStake.Ranked ? "RANKED  ·  HERO STRIKE"
                        : HubQueueWatch.Mode == GameMode.Classic ? "CLASSIC" : "HERO STRIKE";
            var line = HubKnit.Tag(Root, "ModeLine", mode, Knit.PillGo, 92);
            HubKit.Place(line, HubKit.Centre, new Vector2(0, -86), line.sizeDelta);

            HubImpact.On(plate, Root, 0);
            HubSlap.On(line, 0.24f, 2);
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
            var root = HubKit.Place(HubKit.Rect(parent, "Toast"), HubKit.Bottom, new Vector2(0, 150), new Vector2(980, 100));
            var toast = root.gameObject.AddComponent<HubToast>();
            toast._root = root;
            // A dark card with cream words (`boot_menus.py`).
            HubKit.Stretch(HubKnit.Plate(root, "Plate", Knit.DarkCard));
            toast._words = HubKnit.Words(root, "Words", "", 40, HubKnit.KitCream, TextAnchor.MiddleCenter, true);
            HubKit.Stretch(toast._words.rectTransform, 18);
            toast._words.rectTransform.offsetMax = new Vector2(-18, -4);
            toast._words.rectTransform.offsetMin = new Vector2(18, 18);
            root.gameObject.SetActive(false);
            return toast;
        }

        public void Show(string words)
        {
            if (string.IsNullOrWhiteSpace(words)) return;
            _words.text = words;
            // Refusals often need two lines, especially with Larger text. Size this one temporary
            // message from its actual content instead of letting it overflow an88-unit plate.
            _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(100, _words.preferredHeight + 50));
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
