using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Ported from `mode_select.gd`.</summary>
    public sealed class ConvertedModeSelect : ConvertedScreen
    {
        /// <summary>`mode_select.gd` backs out to the title on Escape.</summary>
        protected override string CancelTarget => SceneFlow.MainMenu;

        protected override void Wire()
        {
            OnClick("SoloButton", () =>
            {
                SceneFlow.Networked = false;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("MultiButton", () =>
            {
                SceneFlow.Networked = true;
                SceneFlow.Go(SceneFlow.MultiplayerSetup);
            });

            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.MainMenu));
        }
    }
}
