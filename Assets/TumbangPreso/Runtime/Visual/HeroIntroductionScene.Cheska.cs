using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // Cheska: retained 2.8 s baseline until her own REFINE-2.11 performance replaces it.
        private void BuildCheska()
        {
            for (int i = 0; i < 3; i++)
                Add("GatheredIce" + i, VfxShapes.Spire(6, .12f, 0, i * 3), new Color(.18f, .65f, .92f, .88f));
        }
        private void SampleCheska(float t)
        {
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(Seconds - .42f, Seconds, t);
            float form = Ease(.4f, 1.85f, t);
            Vector3 gatherAt = _heldItem != null ? FreePalm : BothPalms;
            for (int i = 0; i < 3; i++)
                Place(i, gatherAt + new Vector3((i - 1) * Mathf.Lerp(.22f, .13f, form) - (_heldItem != null ? .15f : 0), .14f + Mathf.Abs(i - 1) * .04f, .17f),
                    new Vector3(.10f, .36f, .10f) * form, Quaternion.Euler(18, i * 65, (i - 1) * 28), enter * leave);
        }
    }
}
