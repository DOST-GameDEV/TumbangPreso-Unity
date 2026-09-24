using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // Dante: retained 2.8 s baseline until his own REFINE-2.11 performance replaces it.
        private void BuildDante()
        {
            for (int i = 0; i < 3; i++)
                Add("LoadedGround" + i, VfxShapes.Prism(5, .10f, .86f), new Color(.26f, .22f, .15f, .92f));
        }
        private void SampleDante(float t)
        {
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(Seconds - .42f, Seconds, t);
            float weight = Ease(.3f, 1.9f, t);
            for (int i = 0; i < 3; i++)
                Place(i, new Vector3((i - 1) * .55f, .015f + weight * .035f, .25f + i * .1f),
                    new Vector3(.30f, .3f, .40f), Quaternion.Euler(weight * (i - 1) * 8, 20 + i * 38, 0), enter * leave);
        }
    }
}
