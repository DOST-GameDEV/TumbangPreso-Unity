using System.Collections;
using System.Linq;
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
        private float _hudScale;
        private bool _largerText;
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore()
        {
            _hudScale = Settings.SettingsStore.Current.HudScale;
            _largerText = Settings.SettingsStore.Current.LargerText;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator ResetWorldAfter()
        {
            Settings.SettingsStore.Current.HudScale = _hudScale;
            Settings.SettingsStore.Current.LargerText = _largerText;
            yield return PlayModeWorld.Reset();
        }

        [UnityTest]
        public IEnumerator FirstMatchLineUsesItsLaidOutWidthAtLargeHudSize()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, Core.GameMode.HeroStrike);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            Settings.SettingsStore.Current.HudScale = 1.2f;
            Settings.SettingsStore.Current.LargerText = true;
            var chat = LobbyChat.Attach(canvas.transform, inMatch: true);
            chat.PlaceBottomRight(38, 232, 540);
            // Deliver before the first layout frame, as a real queued message can arrive.
            typeof(LobbyChat).GetMethod("AddLocal", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(chat, new object[] { "LOCAL UI REVIEW: a readable chat line." });
            yield return null;
            var row = chat.transform.Find("ChatLine5").GetComponent<Text>();
            foreach (var size in new[] { new Vector2Int(1680, 720), new Vector2Int(960, 540) })
                yield return TumpUiCapture.Capture("MatchChat-first-line-large-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, checkPalette: false, checkActionBounds: false);
            StringAssert.StartsWith("LOCAL UI REVIEW", row.text,
                "The first message was shortened against its pre-layout width and never recovered.");
            Assert.LessOrEqual(row.preferredHeight, row.rectTransform.rect.height + .5f);
            Assert.GreaterOrEqual(row.cachedTextGenerator.characterCountVisible, row.text.TrimEnd().Length);
            Assert.Less(((RectTransform)chat.transform).anchoredPosition.x, 0,
                "Large HUD text must preserve the final right-hand anchor.");
        }

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
        public IEnumerator TheRoomChatColumnDrawsEveryReceivedLine()
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

            // ---- what the room's chat drawer is actually showing ----------------------------
            // ⚠️ SINCE THE KNIT REBUILD (owner 2026-10-28) THE DRAWER IS ONE BUBBLE PER LINE
            // (`LobbyChat.Lobby.cs`), not one transcript. The question this probe exists for is
            // unchanged: are the newest lines DRAWN, whole, not merely assigned.
            chat.SetPresented(true);
            for (int i = 0; i < 3; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            var words = chat.GetComponentsInChildren<Text>(includeInactive: true)
                .Where(t => t.name == "Words" && t.transform.parent != null && t.transform.parent.name == "Shape").ToList();
            Assert.AreEqual(Lines.Length, words.Count, "Every received line needs its own bubble.");
            foreach (string line in Lines)
            {
                string what = line.Substring(line.IndexOf(":  ", System.StringComparison.Ordinal) + 3);
                var bubble = words.FirstOrDefault(t => t.text == what);
                Assert.IsNotNull(bubble, "A received line has no bubble: " + what);
                float have = bubble.rectTransform.rect.height, need = bubble.preferredHeight;
                Debug.Log($"[ChatStrip] bubble rect={have:F1} needs={need:F1} visible={bubble.cachedTextGenerator.characterCountVisible}");
                Assert.LessOrEqual(need, have + 0.5f, "A bubble's text is taller than its own rect, so legacy Text clips it.");
                // Generated against the bubble's own rect, as the canvas draws it: every character must fit.
                var generator = new TextGenerator();
                generator.Populate(bubble.text, bubble.GetGenerationSettings(bubble.rectTransform.rect.size));
                Assert.GreaterOrEqual(generator.characterCountVisible, what.Length - 1,
                    "Part of a bubble is generated but not drawn.");
            }

            Object.Destroy(canvasGo);
            yield return null;
        }
    }
}
