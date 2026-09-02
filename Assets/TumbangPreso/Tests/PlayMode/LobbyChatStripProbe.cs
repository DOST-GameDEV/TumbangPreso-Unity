using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The two-line chat strip above the lobby's input field: that it actually shows the last two
    /// lines, and that they are DRAWN rather than merely assigned.
    ///
    /// ⚠️⚠️ `docs/TODO.md` § 79.3 IS "NOT REPRODUCED BY READING", AND THIS IS THE DRIVING IT ASKS
    /// FOR. 🧑 2026-08-29 with `docs/reports/2026-08-29/reported/18.png`, where the scrollable log
    /// holds four messages and the box above the field is empty: *"u dont see most recent chats in
    /// say something"*.
    ///
    /// The entry's own reading of the source is correct as far as it goes: `_lines` is built
    /// `MaxLines` = 6 long in both modes, `SetLines` computes `target = 6 - min(2, count)` = 4 and
    /// fills `_lines[4]` and `_lines[5]`, and the panel is built two rows tall. Every one of those
    /// is true and the strip was still empty, which is why this is a probe and not another read.
    ///
    /// ⚠️ THE ASSERTION IS ON THE RENDERED HEIGHT, NOT ON `Text.text`. That distinction is the
    /// whole value of this file. `SetLines` ends by setting `verticalOverflow = Truncate` on every
    /// active row, after `MenuKit.FitBlock(line, LineHeight * 2.0f)` has been allowed to size the
    /// type against a TWO-line cap while the row's own `LayoutElement` is pinned to ONE line at
    /// `LineHeight`. Legacy `Text` on `Truncate` draws nothing at all when a single line does not
    /// fit its rect, so a row can hold the right string, be active, have a sensible height, and
    /// paint no pixels. Checking `.text` would pass against exactly the screenshot he sent.
    /// </summary>
    public class LobbyChatStripProbe
    {
        /// <summary>
        /// ⚠️ REAL SENTENCES AT A REAL LENGTH. `MatchRpc.MaxChatLength` is 120 and § 79.3's
        /// sibling note records 120 characters at the minimum readable size wrapping to three
        /// lines in this panel, so a probe that pushes "hi" would miss the fault entirely.
        /// </summary>
        private static readonly string[] Lines =
        {
            "bayan:  sino taya ngayon ha",
            "cheska:  ako na naman, ang unfair nito grabe",
            "dante:  wag mo iwan yung lata diyan sa gitna please lang",

            // ⚠️⚠️ THE LAST TWO ARE AT `MatchRpc.MaxChatLength`, WHICH IS THE LENGTH THE FAULT
            // NEEDS. The strip shows the last two lines, so these are the two that get drawn.
            // 120 characters at `MenuKit.MinReadableUnits` in this panel wraps past the one-line
            // rect the row's `LayoutElement` pins it to, which is the condition legacy `Text` on
            // `Truncate` paints nothing under. A probe pushing short lines exercises the happy
            // path and reports green against the exact screenshot it was written for.
            "zack:  isa pang round tapos kain na tayo ha sabi ni mama may ulam na daw sa bahay kaya bilisan niyo na please",
            "nemu:  wag kayong mag alala malapit na ako makabalik jan sandali lang talaga ang bilis ng lag dito sa amin eh",
        };

        [UnityTest]
        public IEnumerator TheLobbyStripShowsTheLastTwoLinesAndDrawsThem()
        {
            var canvasGo = new GameObject("~ChatCanvas", typeof(Canvas), typeof(CanvasScaler),
                                          typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // ⚠️ `inMatch: false` IS THE WHOLE SUBJECT. The match column is six rows and a
            // different code path in both `Push` and `SetLines`; the lobby's two-row strip is the
            // one that was empty.
            var chat = LobbyChat.Attach(canvasGo.transform, inMatch: false);
            Assert.IsNotNull(chat, "LobbyChat.Attach built nothing.");

            yield return null;

            // ⚠️ THROUGH THE COMPONENT'S OWN RECEIVE PATH. `Add` is what `MatchRpc.OnChatLine`
            // calls, and it is private, so this reaches it by reflection rather than by pushing
            // strings into a field: the point is to exercise the same route a real message takes,
            // including the `who:  what` formatting that decides how long the line is.
            var add = typeof(LobbyChat).GetMethod("Add",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(add, "LobbyChat.Add(string, string) is gone, so this probe is stale.");

            foreach (string line in Lines)
            {
                int split = line.IndexOf(":  ", System.StringComparison.Ordinal);
                add.Invoke(chat, new object[] { line.Substring(0, split), line.Substring(split + 3) });
            }

            for (int i = 0; i < 4; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            // ---- what the strip is actually showing ----------------------------------------
            var rows = chat.GetComponentsInChildren<Text>(includeInactive: true);

            int active = 0;
            int drawn = 0;
            var detail = new System.Text.StringBuilder();

            foreach (var row in rows)
            {
                // The input field's own label and placeholder live under the same object.
                if (row.transform.parent != chat.transform) continue;

                bool on = row.gameObject.activeInHierarchy;
                float have = ((RectTransform)row.transform).rect.height;
                float need = row.preferredHeight;

                detail.AppendLine($"  row '{row.name}' active={on} text='{row.text}' " +
                                  $"rect={have:F1} needs={need:F1} size={row.fontSize}");

                if (!on || string.IsNullOrEmpty(row.text)) continue;

                active++;

                // ⚠️ THIS IS THE CONDITION LEGACY `Text` DRAWS NOTHING UNDER. On
                // `VerticalWrapMode.Truncate` a label whose content is taller than its rect is
                // clipped by WHOLE LINES, so when even one line does not fit, nothing is
                // rasterised and the row is invisible while remaining active and non-empty.
                if (need <= have + 0.5f) drawn++;
            }

            Debug.Log($"[ChatStrip] active={active} drawn={drawn}\n{detail}");

            Assert.AreEqual(LobbyChat.LobbyVisibleLines, active,
                $"the lobby strip has {active} active rows against " +
                $"{LobbyChat.LobbyVisibleLines} it is built for, after {Lines.Length} messages.\n"
                + detail);

            Assert.AreEqual(active, drawn,
                $"{active - drawn} of the strip's {active} rows hold text and are active but are " +
                "taller than the rect they are clipped to, so legacy Text on Truncate paints " +
                "nothing and the box above the field reads as empty. That is " +
                "docs/reports/2026-08-29/reported/18.png.\n" + detail);

            Object.Destroy(canvasGo);
            yield return null;
        }
    }
}
