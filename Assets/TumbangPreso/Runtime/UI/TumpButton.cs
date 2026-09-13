using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class TumpButton : Button
    {
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            var face = GetComponent<TumpSurface>();
            if (face != null) face.SetVerticesDirty();
        }
    }
}
