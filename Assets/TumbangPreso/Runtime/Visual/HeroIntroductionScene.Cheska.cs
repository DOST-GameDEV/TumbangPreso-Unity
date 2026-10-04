using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // YASMIN, ABSOLUTE ZERO, 3.2 s. plan.md § 6.
        //
        // "Reads the space. Leaves you the harder route." She is from La Trinidad, Benguet: cool
        // highland air and pine ridges (CHARACTER_ORIGINS.md; her ice is her own magic, not local
        // snow). So the stage is quiet and precise: a cold breath, a pale highland mist with a line
        // of pines on the ridge, ONE exact line of frost drawn by her fingertip, a crystal closed
        // between her palms, angular frost plates spreading from her feet, and far behind her a
        // ring of ice spires rising like a route being shut. On the snap the crystal shatters.
        //
        // ⚠️ LOW CONTRAST ON PURPOSE. Every other stage darkens the world; hers pales it, so she
        // is the one dark, warm shape in a cold frame. The glacial white is kept off her face.
        // =========================================================================================
        private int _mistLow, _mistHigh, _breath, _crystal, _frostPlates, _frostLine;
        private readonly List<int> _pines = new List<int>(10), _spires = new List<int>(9), _shards = new List<int>(6), _lineBuds = new List<int>(5);
        private static readonly Color CheskaIce = new Color(.62f, .9f, 1, .9f);

        private void BuildCheska()
        {
            _mistLow = Wall("HighlandMistGround", 0, 1.4f, new Color(.6f, .66f, .64f, .55f), emission: .2f);
            _mistHigh = Wall("HighlandMistSky", 1.4f, 11, new Color(.8f, .86f, .88f, .5f), emission: .3f, cap: true);
            for (int i = 0; i < 10; i++)
                _pines.Add(AddSolid("RidgePine" + i, VfxShapes.Spire(6, .08f, .25f, 120 + i), new Color(.26f, .34f, .31f, 1)));
            _breath = Add("ColdBreath", VfxShapes.TwoSided(VfxShapes.Splat(10, .3f, 5)), new Color(.95f, .98f, 1, .5f), .4f);
            _frostLine = Add("FrostLine", VfxShapes.Prism(4, 1, 1), CheskaIce, .7f);
            for (int i = 0; i < 5; i++) _lineBuds.Add(Add("FrostBud" + i, YasminGatheredCrystal(), CheskaIce, .7f));
            _crystal = Add("GatheredCrystal", YasminGatheredCrystal(), new Color(.7f, .94f, 1, .95f), .8f);
            _frostPlates = Add("FrostPlates", VfxShapes.Wedges(8, .14f, 9, .05f, .25f, 33), new Color(.78f, .95f, 1, .8f), .5f);
            for (int i = 0; i < 9; i++)
                _spires.Add(Add("ClosingSpire" + i, VfxShapes.Spire(6, .12f, .3f, 140 + i), new Color(.55f, .85f, 1, .85f), .45f));
            for (int i = 0; i < 6; i++) _shards.Add(Add("SnapShard" + i, YasminGatheredCrystal(), CheskaIce, .9f));
        }

        // A held crystal needs volume. VfxShapes.Crystal is a flat ground fan,
        // which disappeared edge-on in the low gather shot.
        private static Mesh YasminGatheredCrystal()
        {
            var top = Vector3.up;
            var bottom = Vector3.down * .65f;
            var front = Vector3.forward;
            var right = Vector3.right;
            var back = Vector3.back;
            var left = Vector3.left;
            var mesh = new Mesh { name = "Yasmin gathered ice crystal" };
            mesh.vertices = new[] {
                top, front, right, top, right, back,
                top, back, left, top, left, front,
                bottom, right, front, bottom, back, right,
                bottom, left, back, bottom, front, left
            };
            mesh.triangles = new[] { 0,1,2,3,4,5,6,7,8,9,10,11,
                12,13,14,15,16,17,18,19,20,21,22,23 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private void SampleCheska(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float side = _heldItem != null ? -1 : 1; // the drawing hand: right when free, left when holding a shoe

            // A cold breath while she reads the court.
            float puff = Ease(.1f, .3f, t) * (1 - Ease(.35f, .8f, t));
            Place(_breath, HeadPoint + new Vector3(0, -.2f, .35f + Ease(.1f, .8f, t) * .3f), Vector3.one * Mathf.Lerp(.08f, .3f, Ease(.1f, .8f, t)),
                Quaternion.Euler(-90, 0, 0), puff * leave);

            // The mist and the pine ridge come in as she draws.
            float mist = Ease(.7f, 1.4f, t) * leave;
            Tint(_mistLow, mist); Tint(_mistHigh, mist);
            for (int i = 0; i < _pines.Count; i++)
            {
                float angle = (180 - 95 + i * 21) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.2f, 0, Mathf.Cos(angle) * 7.2f);
                float height = 2.1f + (i * 41 % 5) * .3f;
                Place(_pines[i], at, new Vector3(.55f, height * Ease(.8f + i * .03f, 1.3f + i * .03f, t) + .01f, .55f), Quaternion.identity, mist > .01f ? 1 : 0);
            }

            // One exact line of frost, drawn left to right by her fingertip, budding as it goes.
            float drawn = Ease(.9f, 1.3f, t);
            var to = (_heldItem != null ? FreePalm : RightPalm) + new Vector3(0, .04f, .12f);
            var from = to - new Vector3(side * .4f, .06f, -.1f) * drawn;
            var end = to;
            var along = end - from;
            float lineOn = Ease(.9f, .95f, t) * (1 - Ease(1.45f, 1.7f, t)) * leave;
            if (along.sqrMagnitude > 1e-5f)
            {
                var rotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
                Place(_frostLine, from, new Vector3(.012f, along.magnitude, .012f), rotation, lineOn);
            }
            else Tint(_frostLine, 0);
            for (int i = 0; i < _lineBuds.Count; i++)
            {
                float u = (i + .5f) / _lineBuds.Count;
                float grown = Ease(.9f + u * .4f, 1.0f + u * .4f, t);
                Place(_lineBuds[i], Vector3.Lerp(from, to, u), Vector3.one * .035f * grown, Quaternion.Euler(0, i * 50, 20), grown * lineOn);
            }

            // The crystal closes between her palms, rises with them, and shatters on the snap.
            Vector3 gatherAt = (_heldItem != null ? FreePalm : BothPalms) + new Vector3(0, .1f, .12f);
            float form = Ease(1.45f, 1.95f, t), snap = Ease(2.88f, 2.92f, t);
            Place(_crystal, gatherAt, new Vector3(.11f, .18f, .11f) * form, Quaternion.Euler(0, t * 40, 0), form * (1 - snap) * leave);
            for (int i = 0; i < _shards.Count; i++)
            {
                float age = t - 2.9f, u = Mathf.Clamp01(age / .3f);
                float angle = i * Mathf.PI * 2 / _shards.Count;
                var at = gatherAt + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .7f, .3f) * u * .9f;
                Place(_shards[i], at, Vector3.one * .06f * (1 - u * .5f), Quaternion.Euler(angle * 57, 0, 90), age >= 0 ? (1 - u) * leave : 0);
            }

            // Frost plates spread from her feet as she lifts the crystal.
            float spread = Ease(2.1f, 2.9f, t);
            Place(_frostPlates, Vector3.up * .02f, Vector3.one * Mathf.Lerp(.3f, 2.3f, spread), Quaternion.Euler(0, 15, 0), spread * leave);

            // Far behind her the route closes: a ring of spires rises, left to right.
            for (int i = 0; i < _spires.Count; i++)
            {
                float angle = (180 - 90 + i * 22.5f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 5.6f, 0, Mathf.Cos(angle) * 5.6f);
                float rise = Ease(2.15f + i * .05f, 2.5f + i * .05f, t);
                Place(_spires[i], at, new Vector3(.45f, (1.3f + (i % 3) * .35f) * rise + .01f, .45f), Quaternion.Euler(0, i * 31, (i % 2 == 0 ? 4 : -4)), rise * leave);
            }
        }
    }
}
