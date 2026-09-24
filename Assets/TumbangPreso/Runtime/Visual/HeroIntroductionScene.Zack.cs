using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // ZACK, THUNDERSTRIKE, 2.8 s. plan.md § 3.
        //
        // "Finds the angle before you see the opening." He grew up on a condo roofdeck in Pasig
        // (CHARACTER_ORIGINS.md), so his stage is a storm rolling in over a skyline, and nothing
        // in it strains: a spark flicked off a fingertip, one lazy finger that the storm ANSWERS
        // with a far bolt, a thin gold line running out along his arm while he sights the shot,
        // and on the snap a bolt dropping straight down behind him. Then he shrugs.
        //
        // ⚠️ ONE BRIGHT MOMENT, NOT A STROBE. The storm flickers exactly twice (the answer and
        // the snap), each a single short flash, and reduced effects keeps the bolt shapes without
        // either flash (research.md § 1, and VISION's whiteout rule).
        // =========================================================================================
        private int _stormLow, _skyline, _stormHigh, _fingerSpark, _answerBolt, _snapBolt, _snapGlow;
        private readonly List<int> _clouds = new List<int>(5), _rooftops = new List<int>(7);
        private LineRenderer _sightLine;
        private readonly List<LineRenderer> _crackle = new List<LineRenderer>(3);
        private static readonly Color ZackGold = new Color(1, .82f, .2f, .95f);

        private void BuildZack()
        {
            _stormLow = Wall("StormGround", 0, 1.3f, new Color(.09f, .09f, .1f, .86f));
            _stormHigh = Wall("StormSky", 1.3f, 11, new Color(.24f, .25f, .26f, .84f), emission: .1f);
            // A roofline skyline cut into the low band: condo blocks at different heights.
            for (int i = 0; i < 7; i++)
                _rooftops.Add(Add("Skyline" + i, VfxShapes.Prism(4, 1, 1), new Color(.07f, .07f, .08f, .92f), .02f));
            _skyline = Add("SkylineWindows", VfxShapes.TwoSided(VfxShapes.Wedges(9, .9f, 22, 0, .1f, 17)), new Color(1, .84f, .45f, .5f), .6f);
            for (int i = 0; i < 5; i++)
                _clouds.Add(Add("StormCloud" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .3f, 70 + i)), new Color(.13f, .13f, .15f, .9f), .02f));
            _fingerSpark = Add("FingerSpark", VfxShapes.TwoSided(VfxShapes.Star(4, .35f, 9)), ZackGold, .9f);
            _answerBolt = Add("AnswerBolt", VfxShapes.Bolt(1, 7, .16f, .045f, 2, 91), ZackGold, .9f);
            _snapBolt = Add("SnapBolt", VfxShapes.Bolt(1, 8, .14f, .06f, 3, 93), ZackGold, 1f);
            _snapGlow = Add("SnapGlow", VfxShapes.TwoSided(VfxShapes.Splat(14, .2f, 94)), new Color(1, .9f, .5f, .5f), .8f);
            _sightLine = Line("SightLine", 2, .018f, ZackGold);
            for (int i = 0; i < 3; i++) _crackle.Add(Line("FingerCrackle" + i, 6, .012f, ZackGold));
        }

        private void SampleZack(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The storm rolls in as the finger goes up; before that he is just on the court.
            float storm = Ease(.45f, .95f, t) * leave;
            float answer = Flash(t, .82f, .07f), snap = Flash(t, 1.9f, .08f);
            var flicker = new Color(.24f, .25f, .26f, .84f) * (1 + .8f * Mathf.Max(answer, snap));
            flicker.a = .84f;
            Tint(_stormLow, storm); Tint(_stormHigh, storm, flicker);

            for (int i = 0; i < _rooftops.Count; i++)
            {
                float angle = (-120 + i * 36) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.4f, 0, Mathf.Cos(angle) * 7.4f - .3f);
                float height = 1.6f + (i * 53 % 7) * .32f;
                Place(_rooftops[i], at, new Vector3(.9f + (i % 3) * .3f, height, .5f), Quaternion.LookRotation(-at.normalized, Vector3.up) * Quaternion.Euler(0, 45, 0), storm);
            }
            Place(_skyline, new Vector3(0, 1.2f, -.3f), new Vector3(7.2f, 1, 7.2f), Quaternion.Euler(0, 180, 0), storm * .6f);

            // The clouds slide in overhead from his left, lowering the sky.
            for (int i = 0; i < _clouds.Count; i++)
            {
                float angle = (-100 + i * 50 + t * 9) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.2f, 5.2f + (i % 2) * .8f - storm * .6f, Mathf.Cos(angle) * 7.2f);
                var face = Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                Place(_clouds[i], at, new Vector3(2.4f, 1, 1.1f), face, storm);
            }

            // The flick: one spark off the fingertip.
            float spark = Ease(.36f, .42f, t) * (1 - Ease(.42f, .62f, t));
            Place(_fingerSpark, RightPalm + new Vector3(0, .1f, .12f) + Vector3.up * Ease(.42f, .62f, t) * .25f,
                Vector3.one * .09f, Quaternion.Euler(-90, t * 400, 0), spark * leave);

            // The finger up: a crackle climbs from it, and the storm answers with a far bolt.
            float up = Ease(.62f, .72f, t) * (1 - Ease(1.12f, 1.3f, t));
            for (int i = 0; i < _crackle.Count; i++)
            {
                var line = _crackle[i]; line.widthMultiplier = .012f * up * leave;
                for (int k = 0; k < 6; k++)
                {
                    float u = k / 5f;
                    float jag = k == 0 ? 0 : Mathf.Sin(k * 7.3f + i * 2.1f + Mathf.Floor(t * 14) * 1.7f) * .06f;
                    line.SetPosition(k, RightPalm + new Vector3((i - 1) * .05f + jag, .1f + u * .9f * up, jag * .5f));
                }
            }
            float answered = Ease(.78f, .82f, t) * (1 - Ease(.9f, 1.2f, t));
            Place(_answerBolt, new Vector3(-3.6f, 1.2f, -5.8f), new Vector3(.9f, 5.2f, .9f), Quaternion.Euler(180, 0, 0) * Quaternion.Euler(0, 30, 0),
                (_reducedEffects ? .6f : 1) * answered * storm);

            // The sight line runs out along his arm, the angle he found.
            float sighting = Ease(1.3f, 1.45f, t) * (1 - Ease(1.8f, 1.9f, t));
            _sightLine.widthMultiplier = .018f * sighting * leave;
            var hand = RightPalm;
            _sightLine.SetPosition(0, hand);
            _sightLine.SetPosition(1, hand + new Vector3(-.05f, -.12f, 5.5f * Ease(1.3f, 1.6f, t)));

            // The snap: a bolt drops straight down behind him and lights the ground there.
            float drop = Ease(1.86f, 1.9f, t) * (1 - Ease(2.05f, 2.5f, t));
            Place(_snapBolt, new Vector3(.8f, 6.5f, -2.8f), new Vector3(1.2f, 6.5f, 1.2f), Quaternion.Euler(180, 0, 0), drop * storm);
            Place(_snapGlow, new Vector3(.8f, .03f, -2.8f), Vector3.one * (.6f + Ease(1.88f, 2.2f, t) * 1.2f), Quaternion.identity,
                (drop * .7f + snap * .3f) * storm);
        }
    }
}
