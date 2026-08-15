using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `tutorial.gd`, driving the converted `Tutorial.tscn`.
    ///
    /// ⚠️ IT TEACHES THE THESIS, NOT JUST THE KEYS. The tension is the retrieval, not the
    /// throw: throwing is safe and free, and getting your slipper back is what costs you. A
    /// player who learns only the controls plays the game backwards for their first match.
    /// </summary>
    public sealed class ConvertedTutorialPanel : ConvertedOverlay
    {
        private int _page;

        protected override void Wire()
        {
            SetText("BannerLabel", "HOW TO PLAY");

            OnClick("PrevButton", () => Turn(-1));
            OnClick("NextButton", () => Turn(1));
            OnClick("BackButton", Close);

            ApplyPage();
        }

        public void ResetToFirstPage()
        {
            _page = 0;
            ApplyPage();
        }

        /// <summary>⚠️ IT CLAMPS RATHER THAN WRAPPING. Wrapping from the last page back to the
        /// first reads as the panel having closed and reopened.</summary>
        private void Turn(int delta)
        {
            _page = Mathf.Clamp(_page + delta, 0, TutorialContent.Pages.Length - 1);
            ApplyPage();
        }

        private void ApplyPage()
        {
            var page = TutorialContent.Pages[_page];

            SetText("PageTitle", page.Title);
            SetText("PageLede", page.Lede);
            SetText("PageLabel", $"{_page + 1} / {TutorialContent.Pages.Length}");

            var rows = Node("Rows");
            if (rows == null) return;

            Clear(rows);

            foreach (var row in page.Rows) Row(rows, row.Chip, row.Body);
        }
    }
}
