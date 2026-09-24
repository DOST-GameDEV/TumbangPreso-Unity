using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // Zack: retained 2.8 s baseline until his own REFINE-2.11 performance replaces it.
        private void BuildZack()
        {
            for (int i = 0; i < 3; i++) Line("FineCharge" + i, 7, .018f, new Color(1, .78f, .17f, .85f));
        }
        private void SampleZack(float t)
        {
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(Seconds - .42f, Seconds, t);
            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i]; line.widthMultiplier = .018f * enter * leave;
                for (int k = 0; k < 7; k++)
                {
                    float u = k / 6f;
                    float jag = k == 0 || k == 6 ? 0 : Mathf.Sin(k * 8.2f + i * 2.7f + t * 13) * .055f;
                    line.SetPosition(k, RightPalm + new Vector3((i - 1) * .10f + jag, u * .85f * enter, .08f + jag));
                }
            }
        }
    }
}
