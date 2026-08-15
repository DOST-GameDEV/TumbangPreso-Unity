using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Ported from `mode_select.gd`.</summary>
    public sealed class ConvertedModeSelect : ConvertedScreen
    {
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
