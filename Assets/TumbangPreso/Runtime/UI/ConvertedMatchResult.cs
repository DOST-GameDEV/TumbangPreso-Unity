using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `match_result.gd`.
    ///
    /// ⚠️ IT RANKS, IT DOES NOT JUST LIST. Four places with a position, a name and points, and
    /// ⚠️ A TIE AT THE TOP IS AN HONEST DRAW rather than being broken by seat order, because
    /// breaking it that way would hand round 1's taya a structural advantage in a game whose
    /// whole fairness argument is that the seats are symmetric.
    /// </summary>
    public sealed class ConvertedMatchResult : ConvertedScreen
    {
        public static int[] Scores = new int[Balance.PlayerCount];
        public static string[] Names = { "P1", "P2", "P3", "P4" };
        public static int WinningSlot = -1;

        protected override void Wire()
        {
            SetText("MessageLabel", WinningSlot < 0
                ? "A DRAW"
                : $"{Names[Mathf.Clamp(WinningSlot, 0, Names.Length - 1)]} WINS");

            var order = new List<int>();
            for (int i = 0; i < Balance.PlayerCount; i++) order.Add(i);
            order.Sort((a, b) => Scores[b].CompareTo(Scores[a]));

            for (int place = 0; place < order.Count; place++)
            {
                var root = Node($"Place{place}");
                if (root == null) continue;

                int slot = order[place];
                SetChildText(root, "Place", $"{place + 1}");
                SetChildText(root, "Name", Names[slot]);
                SetChildText(root, "Points", Scores[slot].ToString());
            }

            OnClick("RematchButton", SceneFlow.StartMatch);
            OnClick("MenuButton", () => SceneFlow.Go(SceneFlow.MainMenu));
        }

        private static void SetChildText(Transform root, string childName, string value)
        {
            var child = root.Find(childName);
            if (child == null) return;

            var text = child.GetComponent<Text>();
            if (text != null) text.text = value;
        }
    }
}
