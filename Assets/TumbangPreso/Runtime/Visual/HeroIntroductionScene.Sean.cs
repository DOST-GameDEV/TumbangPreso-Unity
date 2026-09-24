using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // Sean: retained 2.8 s baseline until his own REFINE-2.11 performance replaces it.
        private void BuildSean()
        {
            for (int i = 0; i < 6; i++)
                Add("InwardHeat" + i, VfxShapes.Tongue(5, .24f, .15f, .35f, .08f, 240 + i),
                    new Color(1, i % 2 == 0 ? .32f : .58f, .04f, .68f));
        }
        private void SampleSean(float t)
        {
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(Seconds - .42f, Seconds, t);
            float gather = Ease(.65f, 2.15f, t);
            for (int i = 0; i < _pieces.Count; i++)
            {
                float a = i * Mathf.PI / 3 + t * .65f;
                float radius = Mathf.Lerp(1.05f, .60f, gather);
                Place(i, new Vector3(Mathf.Cos(a) * radius, .06f + gather * .32f, Mathf.Sin(a) * radius),
                    new Vector3(.8f, Mathf.Lerp(.9f, .6f, gather), .8f) * enter,
                    Quaternion.Euler(-18, -a * Mathf.Rad2Deg, 0), enter * leave);
            }
        }
    }
}
