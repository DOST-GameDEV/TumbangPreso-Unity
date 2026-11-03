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
        public override bool WearsBoard => true;
        public override float CourtShade => 1.0f;
        private RectTransform _daily, _weekly;
        private Text _note;
        private bool _rowsDrawn;
        private System.Collections.Generic.List<WalletStore.TaskState> _shownTasks;
        private readonly System.Collections.Generic.List<Button> _claimButtons = new System.Collections.Generic.List<Button>();

        public override void Build()
        {
            // The boot kit (`boot_heroes.py`): the berry ground, a cream panel holding the two lists, one
            // sunk track per task. The list keeps its data and CLAIM exactly as before.
            HubKnit.Ground(Root);
            HubKnit.Frame(Root, Hub, "TASKS", "EARN " + EconomyRules.CurrencyName);

            _note = HubKnit.Words(Root, "Note", "", 32, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            HubKnit.At(_note.rectTransform, 60, 962, 1700, 44);

            var panel = HubKnit.Plate(Root, "Listahan", Knit.Panel);
            HubKnit.At(panel, 60, 230, 1800, 720 + HubKnit.SlabHeight);
            var face = HubKnit.FaceOf(panel);
            HubSlap.On(panel, 0, -2);
            _daily = Column(face, "Daily", "TODAY", 60);
            _weekly = Column(face, "Weekly", "THIS WEEK", 940);

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

        private static RectTransform Column(RectTransform parent, string name, string heading, float x)
        {
            var column = HubKit.Rect(parent, name);
            HubKnit.In(column, x, 30, 790, 640);
            var title = HubKnit.Display(column, "Heading", heading, 52, HubKnit.KitMaroon, TextAnchor.MiddleLeft);
            HubKnit.In(title.rectTransform, 0, 5, 790, 70);
            return column;
        }

        private void Draw()
        {
            if (_daily == null) return;
            var wallet = GameServices.Wallet;
            if (wallet == null) return;
            _note.text = !WalletStore.CanTransact
                ? "Offline: showing progress from matches on this machine. Sign in to claim."
                : !string.IsNullOrEmpty(wallet.Status) ? wallet.Status
                : "Finished matches pay " + EconomyRules.CurrencyName + " too, up to " + EconomyRules.DailyMatchCap + " a day.";
            var tasks = wallet.Tasks();
            if (SameRows(tasks))
            {
                foreach (var button in _claimButtons) if (button != null) button.interactable = !wallet.Busy;
                return;
            }
            foreach (var column in new[] { _daily, _weekly })
                for (int i = column.childCount - 1; i >= 1; i--)
                {
                    var old = column.GetChild(i).gameObject;
                    old.SetActive(false);
                    Destroy(old);
                }
            _claimButtons.Clear();
            int d = 0, w = 0;
            foreach (var task in tasks)
            {
                bool daily = task.Def.Period == TaskPeriod.Daily;
                var column = daily ? _daily : _weekly;
                int index = daily ? d++ : w++;
                Row(column, task, index);
            }
            _shownTasks = tasks;
            _rowsDrawn = true;
            Hub.RefreshFocus();
        }

        private bool SameRows(System.Collections.Generic.List<WalletStore.TaskState> tasks)
        {
            if (_shownTasks == null || tasks.Count != _shownTasks.Count) return false;
            for (int i = 0; i < tasks.Count; i++)
            {
                var next = tasks[i]; var previous = _shownTasks[i];
                if (next.Def.Id != previous.Def.Id || next.Def.Target != previous.Def.Target ||
                    next.Def.Reward != previous.Def.Reward || next.Progress != previous.Progress ||
                    next.Claimed != previous.Claimed) return false;
            }
            return true;
        }

        private void Row(RectTransform column, WalletStore.TaskState task, int index)
        {
            var card = HubKit.Rect(column, "Task_" + task.Def.Id);
            HubKnit.In(card, 0, 100 + index * 190, 790, 170);
            // A claimed task fades back into the list; a finished one carries its CLAIM.
            float fade = task.Claimed ? 0.5f : 1f;
            var ink = HubKnit.KitInk; ink.a = fade;

            var words = HubKnit.Words(card, "Sentence", task.Def.Sentence, 36, ink, TextAnchor.MiddleLeft, true);
            HubKnit.In(words.rectTransform, 0, 4, 560, 60);
            HubKit.Fit(words, 560);

            var coin = HubKit.Rect(card, "Cap").gameObject.AddComponent<Image>();
            coin.sprite = HubBoard.Art("icon_coin"); coin.preserveAspect = true; coin.raycastTarget = false;
            coin.color = new Color(1, 1, 1, fade);
            HubKnit.In(coin.rectTransform, 572, 4, 60, 60);
            var maroon = HubKnit.KitMaroon; maroon.a = fade;
            var amount = HubKnit.Display(card, "Amount", "+" + task.Def.Reward, 50, maroon, TextAnchor.MiddleLeft);
            HubKnit.In(amount.rectTransform, 640, 4, 150, 60);

            // Progress on a sunk track: a lime pill once done, a yellow one on the way.
            var track = HubKnit.Plate(card, "Track", Knit.Track);
            HubKnit.In(track, 0, 76, 440, 56);
            float ratio = Mathf.Clamp01(task.Progress / (float)task.Def.Target);
            if (ratio > 0)
            {
                var fill = HubKit.Rect(track, "Fill").gameObject.AddComponent<Image>();
                HubKnit.KitFace(fill, task.Done ? Knit.PillGo : Knit.PillYellow);
                float width = Mathf.Max(70, 418 * ratio);
                HubKnit.In(fill.rectTransform, 11, 8, width, 38);
            }
            var count = HubKnit.Words(card, "Count", task.Progress + " / " + task.Def.Target, 32, ink, TextAnchor.MiddleLeft, true);
            HubKnit.In(count.rectTransform, 470, 74, 160, 56);

            if (index < 2)
            {
                var rule = HubKit.Rect(card, "Rule").gameObject.AddComponent<Image>();
                rule.color = new Color(HubKnit.KitTan.r, HubKnit.KitTan.g, HubKnit.KitTan.b, .8f);
                rule.raycastTarget = false;
                HubKnit.In(rule.rectTransform, -10, 160, 790, 3);
            }

            if (task.Claimed)
            {
                var tick = HubKit.Glyph(card, "Tick", HubGlyph.Mark.Check, HubKnit.KitMaroon, 0.16f);
                HubKnit.In(tick.rectTransform, 560, 80, 50, 50);
                var done = HubKnit.Display(card, "Claimed", "CLAIMED", 36, HubKnit.KitSoftRed, TextAnchor.MiddleRight);
                HubKnit.In(done.rectTransform, 610, 80, 180, 50);
            }
            else if (task.Done)
            {
                var claim = HubKnit.Button(card, "Claim_" + task.Def.Id, "CLAIM", Knit.Go, () => Claim(task.Def.Id), 40);
                HubKnit.In((RectTransform)claim.transform, 570, 70, 220, 82);
                claim.interactable = !GameServices.Wallet.Busy;
                _claimButtons.Add(claim);
            }
            // RefreshAsync can report the same rows immediately after Build. Rebuilding
            // their live data must not replay the entrance motion over the first one.
            if (!_rowsDrawn)
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
