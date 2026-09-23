using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// TASKS: how TANSAN is earned. The door is HOME's TASK sticker and the + beside the balance.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   today's three tasks and the week's three, with how far along each is
    ///   first press     CLAIM on a finished task; nothing else here needs pressing
    ///   not needed now  how currency is spent (that is the SHOP)
    ///   out             BACK or Escape
    ///
    /// ⚠️⚠️ CLAIM IS A REQUEST. The server counts only the matches it recorded and decides; the row
    /// redraws from its answer. Offline, the progress shown is this machine's own career read by the
    /// same rules, labelled as such, and CLAIM says why it cannot run.
    /// </summary>
    public sealed class HubTasks : HubScreen
    {
        public override float CourtShade => 1.0f;
        private RectTransform _daily, _weekly;
        private Text _note;

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.ArmyDeep, 41);
            HubChrome.Back(Root, Hub);
            HubChrome.Title(Root, "TASKS", "EARN " + EconomyRules.CurrencyName);
            HubChrome.TopRight(Root, Hub);

            _note = HubKit.Text(Root, "Note", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(_note.rectTransform, HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(1500, 44));

            var columns = HubKit.Span(HubKit.Rect(Root, "Columns"), Vector2.zero, Vector2.one,
                                      new Vector2(HubKit.Margin, HubKit.Margin + 70), new Vector2(HubKit.Margin, HubKit.Margin + 200));
            var row = HubKit.Place(HubKit.Rect(columns, "Row"), HubKit.Top, Vector2.zero, new Vector2(1720, 720));
            _daily = Column(row, "Daily", "TODAY", new Vector2(0, 0));
            _weekly = Column(row, "Weekly", "THIS WEEK", new Vector2(880, 0));

            Draw();
            if (GameServices.Wallet != null)
            {
                GameServices.Wallet.Changed += Draw;
                _ = GameServices.Wallet.RefreshAsync();
            }
        }

        private void OnDestroy()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= Draw;
        }

        private static RectTransform Column(RectTransform parent, string name, string heading, Vector2 at)
        {
            var column = HubKit.Place(HubKit.Rect(parent, name), HubKit.TopLeft, at, new Vector2(840, 720));
            var title = HubKit.Text(column, "Heading", heading, HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, Vector2.zero, new Vector2(800, 64));
            return column;
        }

        private void Draw()
        {
            if (_daily == null) return;
            foreach (var column in new[] { _daily, _weekly })
                for (int i = column.childCount - 1; i >= 1; i--) Destroy(column.GetChild(i).gameObject);

            var wallet = GameServices.Wallet;
            if (wallet == null) return;
            int d = 0, w = 0;
            foreach (var task in wallet.Tasks())
            {
                bool daily = task.Def.Period == TaskPeriod.Daily;
                var column = daily ? _daily : _weekly;
                int index = daily ? d++ : w++;
                Row(column, task, index);
            }
            _note.text = !WalletStore.CanTransact
                ? "Offline: showing progress from matches on this machine. Sign in to claim."
                : !string.IsNullOrEmpty(wallet.Status) ? wallet.Status
                : "Finished matches pay " + EconomyRules.CurrencyName + " too, up to " + EconomyRules.DailyMatchCap + " a day.";
            Hub.RefreshFocus();
        }

        private void Row(RectTransform column, WalletStore.TaskState task, int index)
        {
            var card = HubKit.Place(HubKit.Rect(column, "Task_" + task.Def.Id), HubKit.TopLeft,
                                    new Vector2(0, -(84 + index * 206)), new Vector2(840, 186));
            // Dark cards with honey lettering: the brief's "no pale default" applied to the one screen
            // that is a list of six cards. A finished card lights up golden, which is the thing the
            // eye should find first; a claimed one drops back to night.
            Color cardFill = task.Claimed ? HubStyle.Night : task.Done ? HubStyle.Golden : HubStyle.ArmyDeep;
            var plate = HubKit.Shape(card, "Plate", cardFill, false, 600 + index, 5, 22);
            HubKit.Stretch(plate.rectTransform);
            Color ink = task.Claimed ? HubStyle.HoneySoft : HubStyle.TextOn(cardFill);

            var words = HubKit.Text(card, "Sentence", task.Def.Sentence, HubStyle.Label, true, ink, TextAnchor.MiddleLeft);
            HubKit.Place(words.rectTransform, HubKit.TopLeft, new Vector2(30, -18), new Vector2(520, 60));
            HubKit.Fit(words, 520);

            var track = HubKit.Shape(card, "Track", HubStyle.Night, false, 610 + index, 3, 14);
            HubKit.Place(track.rectTransform, HubKit.BottomLeft, new Vector2(30, 40), new Vector2(420, 30));
            var fill = HubKit.Rect(track.transform, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(task.Progress / (float)task.Def.Target), 1);
            fill.offsetMin = new Vector2(4, 4); fill.offsetMax = new Vector2(0, -4);
            var image = fill.gameObject.AddComponent<Image>();
            image.color = task.Done ? HubStyle.Chartreuse : HubStyle.Persimmon;
            image.raycastTarget = false;
            var count = HubKit.Text(card, "Count", task.Progress + " / " + task.Def.Target, HubStyle.Floor, false, ink, TextAnchor.MiddleLeft);
            HubKit.Place(count.rectTransform, HubKit.BottomLeft, new Vector2(466, 36), new Vector2(120, 40));

            var reward = HubKit.Place(HubKit.Rect(card, "Reward"), HubKit.TopRight, new Vector2(-30, -20), new Vector2(230, 56));
            var cap = HubKit.Glyph(reward, "Cap", HubGlyph.Mark.Cap, task.Claimed ? HubStyle.HoneySoft : task.Done ? HubStyle.DeepRed : HubStyle.Golden, 0.11f);
            HubKit.Place(cap.rectTransform, HubKit.Left, Vector2.zero, new Vector2(54, 54));
            var amount = HubKit.Text(reward, "Amount", "+" + task.Def.Reward, HubStyle.Title, true, ink, TextAnchor.MiddleLeft);
            HubKit.Place(amount.rectTransform, HubKit.Left, new Vector2(62, 0), new Vector2(170, 56));

            if (task.Claimed)
            {
                var done = HubKit.Text(card, "Claimed", "CLAIMED", HubStyle.Label, true, HubStyle.Chartreuse, TextAnchor.MiddleRight);
                HubKit.Place(done.rectTransform, HubKit.BottomRight, new Vector2(-30, 26), new Vector2(230, 56));
            }
            else if (task.Done)
            {
                var claim = HubKit.Button(card, "Claim_" + task.Def.Id, "CLAIM", HubStyle.Chartreuse, () => Claim(task.Def.Id), HubStyle.Label, 620 + index);
                HubKit.Place((RectTransform)claim.transform, HubKit.BottomRight, new Vector2(-24, 22), new Vector2(220, 82));
                claim.interactable = !GameServices.Wallet.Busy;
            }
            HubSlap.On(card, 0.03f * index, index % 2 == 0 ? -1 : 1);
        }

        private async void Claim(string id)
        {
            var wallet = GameServices.Wallet;
            if (wallet == null) return;
            string result = await wallet.ClaimAsync(id);
            if (this == null) return;
            Hub.Toast(result == "offline" ? wallet.Status : WalletStore.Sentence(result));
            if (result == "claimed") MenuSfx.Valid();
        }
    }
}
