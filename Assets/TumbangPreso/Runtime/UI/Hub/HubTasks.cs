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
            // ⚠️ THE LISTAHAN (`HubScenery`, 2026-09-24). Six dark cards on maroon read as a
            // spreadsheet; a sari-sari store keeps its list in marker on a piece of cardboard taped
            // to the wall, and a finished line gets highlighted, then ticked. Same data, same CLAIM,
            // now a thing on a wall.
            HubScenery.PaintedGround(Root, 41);
            HubChrome.Back(Root, Hub);
            HubScenery.SprayTitle(HubChrome.Title(Root, "TASKS", "EARN " + EconomyRules.CurrencyName));
            HubChrome.TopRight(Root, Hub);

            _note = HubKit.Text(Root, "Note", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(_note.rectTransform, HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(1500, 44));

            var columns = HubKit.Span(HubKit.Rect(Root, "Columns"), Vector2.zero, Vector2.one,
                                      new Vector2(HubKit.Margin, HubKit.Margin + 70), new Vector2(HubKit.Margin, HubKit.Margin + 200));
            var board = HubScenery.Cardboard(columns, "Listahan", 41);
            // ⚠️ 720 TALL, NOT 790: at 790 the board covered the offline note on the wall under it.
            HubKit.Place(board, HubKit.Top, new Vector2(0, 44), new Vector2(1800, 720));
            board.localRotation = Quaternion.Euler(0, 0, -0.8f);
            var row = HubKit.Place(HubKit.Rect(columns, "Row"), HubKit.Top, Vector2.zero, new Vector2(1720, 720));
            row.localRotation = Quaternion.Euler(0, 0, -0.8f);
            HubSlap.On(board, 0, -2);
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
            var title = HubKit.Text(column, "Heading", heading, HubStyle.Title, true, HubStyle.DeepRed, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, Vector2.zero, new Vector2(800, 64));
            // Underlined twice in marker, the way a heading on a handwritten list is.
            var under = HubKit.Rect(title.transform, "Underline").gameObject.AddComponent<HubChalk>();
            under.Shape = HubChalk.Stroke.Underline; under.Source = title; under.Width = 5;
            under.color = HubStyle.DeepRed; under.raycastTarget = false;
            HubKit.Stretch(under.rectTransform);
            under.DrawIn(0.1f);
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
                                    new Vector2(0, -(84 + index * 196)), new Vector2(840, 176));
            // ⚠️ A LINE ON THE LISTAHAN, NOT A CARD. Ink on cardboard measures about 7 : 1. The
            // thing the eye must find first, a finished task, is swiped with a golden highlighter;
            // a claimed one is ticked in red and fades back into the list.
            var ink = task.Claimed ? new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.5f) : HubStyle.Ink;
            if (task.Done && !task.Claimed)
            {
                var swipe = HubKit.Shape(card, "Highlight", new Color(HubStyle.Golden.r, HubStyle.Golden.g, HubStyle.Golden.b, 0.72f),
                                         false, 600 + index, 0, 10);
                HubKit.Stretch(swipe.rectTransform, 4);
                swipe.rectTransform.localRotation = Quaternion.Euler(0, 0, index % 2 == 0 ? 0.7f : -0.6f);
            }
            var rule = HubKit.Rect(card, "Rule").gameObject.AddComponent<HubChalk>();
            rule.Shape = HubChalk.Stroke.Path; rule.Width = 3.5f; rule.raycastTarget = false;
            rule.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.45f);
            HubKit.Stretch(rule.rectTransform);
            rule.Points = new[] { new Vector2(-400, -92), new Vector2(0, -94), new Vector2(410, -91) };
            rule.DrawIn(0.08f + 0.04f * index);

            var words = HubKit.Text(card, "Sentence", task.Def.Sentence, HubStyle.Label, true, ink, TextAnchor.MiddleLeft);
            HubKit.Place(words.rectTransform, HubKit.TopLeft, new Vector2(30, -18), new Vector2(520, 60));
            HubKit.Fit(words, 520);

            // Progress in marker: an inked box, filled in red, or in the action's chartreuse once done.
            var track = HubKit.Shape(card, "Track", new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.25f), false, 610 + index, 3, 14);
            HubKit.Place(track.rectTransform, HubKit.BottomLeft, new Vector2(30, 40), new Vector2(420, 30));
            var fill = HubKit.Rect(track.transform, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(task.Progress / (float)task.Def.Target), 1);
            fill.offsetMin = new Vector2(4, 4); fill.offsetMax = new Vector2(0, -4);
            var image = fill.gameObject.AddComponent<Image>();
            image.color = task.Done ? HubStyle.Chartreuse : HubStyle.RimRed;
            image.raycastTarget = false;
            var count = HubKit.Text(card, "Count", task.Progress + " / " + task.Def.Target, HubStyle.Floor, false, ink, TextAnchor.MiddleLeft);
            HubKit.Place(count.rectTransform, HubKit.BottomLeft, new Vector2(466, 36), new Vector2(120, 48));

            var reward = HubKit.Place(HubKit.Rect(card, "Reward"), HubKit.TopRight, new Vector2(-30, -20), new Vector2(230, 56));
            var cap = HubKit.Glyph(reward, "Cap", HubGlyph.Mark.Cap, task.Claimed ? ink : HubStyle.DeepRed, 0.11f);
            HubKit.Place(cap.rectTransform, HubKit.Left, Vector2.zero, new Vector2(54, 54));
            var amount = HubKit.Text(reward, "Amount", "+" + task.Def.Reward, HubStyle.Title, true, ink, TextAnchor.MiddleLeft);
            HubKit.Place(amount.rectTransform, HubKit.Left, new Vector2(62, 0), new Vector2(170, 56));

            if (task.Claimed)
            {
                var tick = HubKit.Glyph(card, "Tick", HubGlyph.Mark.Check, HubStyle.RimRed, 0.16f);
                HubKit.Place(tick.rectTransform, HubKit.BottomRight, new Vector2(-150, 10), new Vector2(110, 110));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
                var done = HubKit.Text(card, "Claimed", "CLAIMED", HubStyle.Label, true, HubStyle.RimRed, TextAnchor.MiddleRight);
                HubKit.Place(done.rectTransform, HubKit.BottomRight, new Vector2(-30, 26), new Vector2(230, 56));
            }
            else if (task.Done)
            {
                var claim = HubKit.Button(card, "Claim_" + task.Def.Id, "CLAIM", HubStyle.Chartreuse, () => Claim(task.Def.Id), HubStyle.Label, 620 + index);
                HubKit.Place((RectTransform)claim.transform, HubKit.BottomRight, new Vector2(-24, 22), new Vector2(220, 82));
                claim.interactable = !GameServices.Wallet.Busy;
            }
            HubSlap.On(card, 0.06f + 0.03f * index, index % 2 == 0 ? -1 : 1);
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
