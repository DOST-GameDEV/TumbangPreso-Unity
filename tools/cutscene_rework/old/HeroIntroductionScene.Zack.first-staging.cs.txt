using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // OVERCLOCK: the storm is drawn into Zack, not fired at a distant target.
        // The free hand authors the gesture; the carrying hand keeps its actual shoe.
        // Existing2.8s phase, two separated flashes and reduced-effects behavior remain.
        private int _stormLow, _skyline, _stormHigh, _fingerSpark, _answerBolt, _snapBolt, _snapGlow;
        private readonly List<int> _clouds = new List<int>(5), _rooftops = new List<int>(7);
        private LineRenderer _sightLine;
        private readonly List<LineRenderer> _crackle = new List<LineRenderer>(3);
        private static readonly Color ZackGold = new Color(1, .82f, .2f, .95f);

        private void BuildZack()
        {
            _stormLow = Wall("StormGround", 0, 1.3f, new Color(.09f, .09f, .1f, 1f));
            _stormHigh = Wall("StormSky", 1.3f, 11, new Color(.13f, .14f, .17f, 1f), emission: .1f, cap: true);
            // The storm is a backdrop, not a sun-lit surface: keep dark hair legible.
            ZackUnlitBackdrop(_stormLow); ZackUnlitBackdrop(_stormHigh);
            // A roofline skyline cut into the low band: condo blocks at different heights. Sixteen
            // shoulder to shoulder: seven spaced ones read as monoliths on the stage sketch
            // (`previews/zack_v3_stage.png`), a continuous row reads as a city.
            for (int i = 0; i < 16; i++)
                _rooftops.Add(Add("Skyline" + i, VfxShapes.Prism(4, 1, 1), new Color(.07f, .07f, .08f, 1f), .02f));
            _skyline = Add("SkylineWindows", VfxShapes.TwoSided(VfxShapes.Wedges(9, .9f, 22, 0, .1f, 17)), new Color(1, .84f, .45f, .5f), .6f);
            for (int i = 0; i < 5; i++)
                _clouds.Add(Add("StormCloud" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .3f, 70 + i)), new Color(.13f, .13f, .15f, .9f), .02f));
            _fingerSpark = Add("FingerSpark", VfxShapes.TwoSided(VfxShapes.Star(4, .35f, 9)), ZackGold, .9f);
            _answerBolt = Add("AnswerBolt", VfxShapes.Bolt(1, 7, .16f, .045f, 2, 91), ZackGold, .9f);
            _snapBolt = Add("SnapBolt", VfxShapes.Bolt(1, 8, .14f, .06f, 3, 93), ZackGold, 1f);
            _snapGlow = AddGlow("SnapGlow", new Color(1, .85f, .3f, .5f), billboard: false, falloff: 2.5f, core: .35f);
            _sightLine = Line("SightLine", 2, .018f, ZackGold);
            for (int i = 0; i < 3; i++) _crackle.Add(Line("FingerCrackle" + i, 6, .012f, ZackGold));
        }

        private void ZackUnlitBackdrop(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var piece = _pieces[index];
            var material = new Material(shader) { name = "Isagani storm backdrop", color = piece.Color };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }

        private void SampleZack(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The storm rolls in as the finger goes up; before that he is just on the court.
            float storm = Ease(.45f, .95f, t) * leave;
            float answer = Flash(t, .82f, .07f), snap = Flash(t, 1.9f, .08f);
            var flicker = new Color(.13f, .14f, .17f, 1f) * (1 + .8f * Mathf.Max(answer, snap));
            flicker.a = 1f;
            Tint(_stormLow, storm); Tint(_stormHigh, storm, flicker);

            for (int i = 0; i < _rooftops.Count; i++)
            {
                float angle = (-160 + i * 21.3f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.5f, 0, Mathf.Cos(angle) * 7.5f);
                float height = .9f + (i * 53 % 7) * .11f + (i % 5 == 2 ? .35f : 0);
                Place(_rooftops[i], at, new Vector3(1.95f, height, .9f), Quaternion.LookRotation(-at.normalized, Vector3.up) * Quaternion.Euler(0, 45, 0), storm);
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
            Place(_fingerSpark, FreePalm + new Vector3(0, .1f, .12f) + Vector3.up * Ease(.42f, .62f, t) * .25f,
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
                    line.SetPosition(k, FreePalm + new Vector3((i - 1) * .05f + jag, .1f + u * .9f * up, jag * .5f));
                }
            }
            float answered = Ease(.78f, .82f, t) * (1 - Ease(.9f, 1.2f, t));
            Place(_answerBolt, new Vector3(-.3f, 4.2f, .25f), new Vector3(.5f, 1.1f, .5f), Quaternion.Euler(180, 0, 0) * Quaternion.Euler(0, 30, 0),
                (_reducedEffects ? .6f : 1) * answered * storm);

            // Current draws from the free hand into his chest rather than aiming away.
            float sighting = Ease(1.3f, 1.45f, t) * (1 - Ease(1.8f, 1.9f, t));
            _sightLine.widthMultiplier = .018f * sighting * leave;
            var hand = FreePalm;
            _sightLine.SetPosition(0, hand);
            _sightLine.SetPosition(1, Vector3.Lerp(hand, HeadPoint - Vector3.up * .6f, Ease(1.3f, 1.6f, t)));

            // The bolt terminates at the moving crown. The ground mark is centred on Zack.
            float drop = Ease(1.86f, 1.9f, t) * (1 - Ease(2.05f, 2.5f, t));
            Place(_snapBolt, HeadPoint + Vector3.up * 4.8f, new Vector3(.7f, 4.8f, .7f), Quaternion.Euler(180, 0, 0), drop * storm);
            Place(_snapGlow, new Vector3(0, .03f, 0), Vector3.one * (.6f + Ease(1.88f, 2.2f, t) * 1.2f), Quaternion.Euler(90, 0, 0),
                (drop * .7f + snap * .3f) * storm);
        }
    }
}
