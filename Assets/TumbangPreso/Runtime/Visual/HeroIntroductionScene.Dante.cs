using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // DANTE, TITAN FISSURE, 3.8 s. plan.md § 5.
        //
        // "Holds the difficult space. Refuses to be rushed." In Montalban he grew up with the
        // mountain stories, and Bernardo Carpio's divided stone is the image his own power borrows
        // (CHARACTER_ORIGINS.md; his fictional expression, not the folk hero himself). So his stage
        // is WEIGHT: a dust haze settles, a ridge line lifts on the horizon, and two stone slabs
        // shoulder up out of the road behind him, grinding, and part as he pushes his arms apart.
        // On the stamp, molten seams split forward along the ground toward where the live fissure
        // opens, and dust kicks up at his feet.
        //
        // ⚠️ THE SLABS ARE OPAQUE STONE, NOT GLOWING GHOSTS. They are objects he is moving, and an
        // object has to read as mass (`Art_Direction.md` § 0: cute blocky world; stone is a block).
        // Molten orange is only in the seams, which is Dante's retained seam language.
        // =========================================================================================
        private int _dustLow, _dustHigh, _slabLeft, _slabRight, _seam, _seamGlow;
        private readonly List<int> _ridge = new List<int>(9), _puffs = new List<int>(5);

        private void BuildDante()
        {
            _dustLow = Wall("DustGround", 0, 1.2f, new Color(.2f, .14f, .09f, .8f));
            _dustHigh = Wall("DustSky", 1.2f, 11, new Color(.42f, .32f, .22f, .72f), emission: .08f);
            for (int i = 0; i < 9; i++)
                _ridge.Add(AddSolid("Ridge" + i, VfxShapes.Prism(4, 1, .25f, .2f, 0, 60 + i), new Color(.24f, .19f, .15f, 1)));
            var slab = VfxShapes.Prism(5, 1, .72f, .18f, 0, 7);
            _slabLeft = AddSolid("MountainSlabLeft", slab, new Color(.36f, .3f, .25f, 1));
            _slabRight = AddSolid("MountainSlabRight", slab, new Color(.33f, .28f, .23f, 1));
            _seamGlow = Add("MoltenSeamGlow", VfxShapes.Fracture(5, 3, .09f, 21), new Color(1, .42f, .08f, .55f), .8f);
            _seam = Add("MoltenSeam", VfxShapes.Fracture(5, 3, .045f, 21), new Color(1, .62f, .18f, .95f), 1f);
            for (int i = 0; i < 5; i++)
                _puffs.Add(Add("StampDust" + i, VfxShapes.Splat(10, .25f, 30 + i), new Color(.62f, .52f, .4f, .7f), .05f));
        }

        private void SampleDante(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The haze settles while he plants: the whole world gets heavier first.
            float haze = Ease(.3f, 1.1f, t) * leave;
            Tint(_dustLow, haze); Tint(_dustHigh, haze);

            // The ridge lifts on the horizon behind him as he gets under the weight.
            for (int i = 0; i < _ridge.Count; i++)
            {
                float angle = (180 - 80 + i * 20) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.1f, 0, Mathf.Cos(angle) * 7.1f);
                float height = (1.6f + (i * 37 % 5) * .45f) * Ease(.9f + i * .04f, 1.6f + i * .04f, t);
                Place(_ridge[i], at, new Vector3(1.3f, Mathf.Max(.01f, height), 1.1f), Quaternion.Euler(0, i * 23, 0), haze);
            }

            // The two slabs: up out of the road while he braces, apart while he pushes, and a
            // tremor in them matching his.
            float surface = Ease(1.0f, 1.7f, t);
            float part = Ease(1.8f, 2.4f, t);
            float shake = _reducedEffects ? 0 : Mathf.Sin(t * 38) * .02f * (Ease(1.1f, 1.3f, t) - Ease(2.3f, 2.5f, t));
            for (int side = -1; side <= 1; side += 2)
            {
                int index = side < 0 ? _slabLeft : _slabRight;
                var at = new Vector3(side * Mathf.Lerp(1.15f, 2.5f, part) + shake, Mathf.Lerp(-3.2f, 0, surface), -3.1f);
                var tilt = Quaternion.Euler(0, side * 12, side * -Mathf.Lerp(2, 13, part));
                Place(index, at, new Vector3(1.25f, 3.1f, .85f), tilt, haze > .01f ? 1 : 0);
            }

            // The stamp: seams split forward along the road, dust kicks up round his feet.
            float crack = Ease(3.16f, 3.4f, t);
            var seamAt = new Vector3(0, .025f, 1.9f);
            var seamScale = new Vector3(.9f, 1, Mathf.Lerp(.2f, 2.6f, crack));
            Place(_seamGlow, seamAt + Vector3.down * .005f, Vector3.Scale(seamScale, new Vector3(1.15f, 1, 1.05f)), Quaternion.identity,
                crack * leave * (_reducedEffects ? .6f : .8f + .2f * Mathf.Sin(t * 9)));
            Place(_seam, seamAt, seamScale, Quaternion.identity, crack * leave);
            for (int i = 0; i < _puffs.Count; i++)
            {
                float age = t - 3.18f, u = Mathf.Clamp01(age / .5f);
                float angle = (i * 72 + 20) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * (.4f + u * .7f), .05f + u * .25f, Mathf.Sin(angle) * (.4f + u * .7f) + .3f);
                Place(_puffs[i], at, Vector3.one * (.25f + u * .35f), Quaternion.Euler(0, i * 40, 0), age >= 0 ? (1 - u) * leave * .8f : 0);
            }
        }
    }
}
