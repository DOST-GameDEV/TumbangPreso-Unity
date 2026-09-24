using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAFI, BREAKWATER, 3.4 s. plan.md § 7.
        //
        // "Draws you into the wrong current. Leaves with his slipper." He grew up in a Sama Dilaut
        // community in Tawi-Tawi where the playing deck was also the way home (BADJAO_EXPANSION.md),
        // and he likes making a rival commit too early. So the stage is his sea: a turquoise horizon
        // with stilt houses standing over the water, a ripple at his feet on each feint (the false
        // step), currents gathering at his palms when he drops into the boat-deck crouch, and a
        // wave that climbs up behind him and pitches forward over his shoulders on the send.
        //
        // ⚠️ THE STILT HOUSES DO NOT BOB. Fixed houses stand on piles; only boats float (owner's
        // lagoon correction, AGENTS.md). They are silhouettes on the far wall, nothing more.
        // =========================================================================================
        private int _seaLow, _foamLine, _seaSky, _wave, _waveCrest;
        private readonly List<int> _stilts = new List<int>(12), _ripples = new List<int>(3), _currents = new List<int>(4), _spray = new List<int>(6);
        private static readonly float[] RafiSteps = { .16f, .46f, 1.12f };

        private void BuildRafi()
        {
            _seaLow = Wall("SeaGround", 0, 1.0f, new Color(.04f, .22f, .25f, .88f), emission: .1f);
            _foamLine = Wall("SeaHorizonFoam", 1.0f, 1.07f, new Color(.9f, .97f, .95f, .85f), emission: .5f);
            _seaSky = Wall("SeaSky", 1.07f, 11, new Color(.55f, .78f, .76f, .6f), emission: .3f);
            for (int i = 0; i < 4; i++)
            {
                _stilts.Add(AddSolid("StiltHouse" + i, VfxShapes.Prism(4, 1, .8f), new Color(.1f, .12f, .12f, 1)));
                _stilts.Add(AddSolid("StiltRoof" + i, VfxShapes.Prism(4, 1, .05f), new Color(.08f, .09f, .09f, 1)));
                _stilts.Add(AddSolid("StiltPile" + i, VfxShapes.Prism(4, 1, 1), new Color(.08f, .09f, .09f, 1)));
            }
            for (int i = 0; i < 3; i++) _ripples.Add(Add("FalseStepRipple" + i, VfxShapes.Collar(24, .02f, .82f), new Color(.6f, .9f, .92f, .7f)));
            for (int i = 0; i < 4; i++) _currents.Add(Add("GatheredCurrent" + i, WaterRibbon(), new Color(.25f, .67f, .78f, .5f)));
            _wave = Add("BreakwaterWave", ArcWallMesh(18, 120), new Color(.16f, .55f, .62f, .78f));
            _waveCrest = Add("BreakwaterCrest", ArcWallMesh(18, 120), new Color(.88f, .97f, .96f, .9f));
            for (int i = 0; i < 6; i++) _spray.Add(Add("SendSpray" + i, VfxShapes.TwoSided(VfxShapes.Splat(8, .3f, 60 + i)), new Color(.85f, .96f, .95f, .85f), plain: true));
        }

        /// <summary>An arc of wall behind the hero, open toward the camera side: the wave's face.</summary>
        private static Mesh ArcWallMesh(int sides, float arcDegrees)
        {
            var mesh = new Mesh { name = "Rafi breakwater arc" };
            var vertices = new Vector3[(sides + 1) * 2];
            var triangles = new int[sides * 12];
            for (int i = 0; i <= sides; i++)
            {
                float a = Mathf.Deg2Rad * (180 - arcDegrees * .5f + arcDegrees * i / sides);
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                // The top leans inward: a wave's face curls toward what it is about to hit.
                vertices[i * 2] = rim; vertices[i * 2 + 1] = rim * .82f + Vector3.up;
                if (i == sides) continue;
                int n = i * 2, t = i * 12;
                int[] faces = { n, n + 1, n + 2, n + 2, n + 1, n + 3, n, n + 2, n + 1, n + 2, n + 3, n + 1 };
                for (int k = 0; k < 12; k++) triangles[t + k] = faces[k];
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private void SampleRafi(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The sea comes in with the beckon: he is inviting you onto his water.
            float sea = Ease(.85f, 1.5f, t) * leave;
            Tint(_seaLow, sea); Tint(_foamLine, sea); Tint(_seaSky, sea);
            for (int i = 0; i < 4; i++)
            {
                float angle = (180 - 55 + i * 34) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.3f, 0, Mathf.Cos(angle) * 7.3f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                float stand = sea > .01f ? 1 : 0;
                Place(_stilts[i * 3 + 2], at + Vector3.up * .6f, new Vector3(.08f, .7f, .08f), face, stand);
                Place(_stilts[i * 3], at + Vector3.up * 1.3f, new Vector3(.55f, .45f, .4f), face * Quaternion.Euler(0, 45, 0), stand);
                Place(_stilts[i * 3 + 1], at + Vector3.up * 1.75f, new Vector3(.62f, .35f, .46f), face * Quaternion.Euler(0, 45, 0), stand);
            }

            // Every false step leaves a ripple at his feet.
            for (int i = 0; i < _ripples.Count; i++)
            {
                float age = t - RafiSteps[i], u = Mathf.Clamp01(age / .5f);
                Place(_ripples[i], Vector3.up * .02f, Vector3.one * Mathf.Lerp(.3f, 1.3f, u), Quaternion.identity, age >= 0 ? (1 - u) * .8f * leave : 0);
            }

            // Currents gather at his palms in the deck crouch.
            float current = Ease(1.55f, 1.9f, t) * (1 - Ease(2.3f, 2.5f, t));
            for (int i = 0; i < _currents.Count; i++)
            {
                float angle = i * Mathf.PI * .5f + t * 2.2f;
                var palm = i % 2 == 0 ? FreePalm : RightPalm;
                var origin = palm + new Vector3(Mathf.Cos(angle) * .12f, .02f + i * .02f, Mathf.Sin(angle) * .12f);
                Place(_currents[i], origin, Vector3.one * Mathf.Lerp(.25f, .7f, current), Quaternion.Euler(8, i * 90 + t * 60, 18), current * leave);
            }

            // The wave climbs behind him as he lifts his arms, then pitches over on the send.
            float climb = Ease(2.0f, 2.75f, t);
            float pitch = Ease(2.88f, 3.2f, t);
            float height = Mathf.Lerp(.05f, 2.7f, climb);
            var waveTilt = Quaternion.Euler(-pitch * 28, 0, 0);
            var wavePos = new Vector3(0, 0, -.2f + pitch * .8f);
            Place(_wave, wavePos, new Vector3(2.4f, height, 2.4f), waveTilt, climb * leave * (1 - pitch * .35f));
            Place(_waveCrest, wavePos + waveTilt * Vector3.up * height * .97f, new Vector3(2.4f * .84f, .16f + pitch * .1f, 2.4f * .84f), waveTilt,
                climb * leave);
            for (int i = 0; i < _spray.Count; i++)
            {
                float age = t - 2.92f, u = Mathf.Clamp01(age / .4f);
                float x = (i - 2.5f) * .45f;
                var at = new Vector3(x, 2.2f + u * .4f - u * u * .9f, .2f + u * 2.4f);
                Place(_spray[i], at, Vector3.one * (.18f + u * .12f), Quaternion.Euler(-70, i * 30, 0), age >= 0 ? (1 - u) * leave : 0);
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
