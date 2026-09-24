using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // Rafi: retained 2.8 s baseline until his own REFINE-2.11 performance replaces it.
        private void BuildRafi()
        {
            for (int i = 0; i < 4; i++) Add("GatheredCurrent" + i, WaterRibbon(), new Color(.25f, .67f, .78f, .46f));
        }
        private void SampleRafi(float t)
        {
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(Seconds - .42f, Seconds, t);
            float current = Ease(.3f, 1.9f, t);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f + t * .65f;
                var origin = FreePalm + new Vector3(Mathf.Cos(angle) * .12f, .04f + i * .025f, Mathf.Sin(angle) * .12f);
                Place(i, origin, Vector3.one * Mathf.Lerp(.25f, .75f, current), Quaternion.Euler(8, i * 90 + t * 32, 18), enter * leave);
            }
        }
        private static Mesh WaterRibbon()
        {
            var mesh = new Mesh { name = "Rafi cupped current ribbon" }; var vertices = new Vector3[26]; var triangles = new int[72];
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f, a = t * 2.1f;
                var point = new Vector3(Mathf.Sin(a) * .28f, t * .4f, Mathf.Cos(a) * .28f);
                vertices[i * 2] = point - Vector3.up * .035f; vertices[i * 2 + 1] = point + Vector3.up * .035f;
                if (i == 12) continue; int n = i * 2, j = i * 6;
                int[] faces = { n, n + 2, n + 1, n + 2, n + 3, n + 1 };
                for (int k = 0; k < 6; k++) triangles[j + k] = faces[k];
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
