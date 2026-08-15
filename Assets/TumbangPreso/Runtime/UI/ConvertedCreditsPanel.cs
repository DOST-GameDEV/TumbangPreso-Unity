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
    /// Ported from `credits_panel.gd`, driving the converted `CreditsPanel.tscn`.
    ///
    /// ⚠️⚠️ THIS SCREEN IS LICENCE COMPLIANCE, NOT POLISH. Three CC-BY-4.0 models ship and
    /// their one requirement is that the author be reachable from somewhere the game actually
    /// ships. An earlier Unity pass rebuilt this as a studio blurb with NO asset credits at all.
    /// The strings are each model's own LICENSE.txt, verbatim. **Do not reword or trim them.**
    /// </summary>
    public sealed class ConvertedCreditsPanel : ConvertedOverlay
    {
        protected override void Wire()
        {
            SetText("Title", "CREDITS");
            OnClick("BackButton", Close);

            var rows = Node("Rows");
            if (rows == null) return;

            Clear(rows);

            Heading(rows, "TUMBANG PRESO  ·  BH STUDIOS");

            var made = MenuKit.Styled(rows, "MenuBody",
                "1st place, Gear Up NCR Esports Game Dev Challenge  ·  " +
                "NCR's entry at the nationals in General Santos City", TextAnchor.UpperLeft);
            made.horizontalOverflow = HorizontalWrapMode.Wrap;
            made.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            foreach (var person in CreditsContent.TeamCredits) Row(rows, person.Name, person.Role);

            Heading(rows, "THIRD-PARTY MODELS  ·  CC-BY-4.0");
            foreach (var credit in CreditsContent.CcByCredits) Row(rows, credit.Chip, credit.Body);

            Heading(rows, "EVERYTHING ELSE");
            foreach (var credit in CreditsContent.CourtesyCredits) Row(rows, credit.Chip, credit.Body);
        }
    }
}
