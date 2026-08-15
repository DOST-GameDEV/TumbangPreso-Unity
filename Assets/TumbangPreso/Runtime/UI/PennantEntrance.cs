using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Unfurls a screen's pennants on entry, in the order they sit in the scene.
    ///
    /// ⚠️ RE-RUN ON RETURN, NOT ONLY ON LOAD. `main_menu.gd` calls `_unfurl()` again every time
    /// a panel closes over it, so coming back from SETTINGS replays the entrance rather than
    /// revealing a static column. That is the screen's whole sense of life.
    /// </summary>
    public sealed class PennantEntrance : MonoBehaviour
    {
        private ArrowButtonView[] _pennants;

        private void OnEnable() => Play();

        public void Play()
        {
            if (_pennants == null || _pennants.Length == 0)
                _pennants = GetComponentsInChildren<ArrowButtonView>(true);

            for (int i = 0; i < _pennants.Length; i++)
                _pennants[i].AnimateIn(i * ArrowButtonView.Stagger);
        }
    }
}
