using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // OVERCLOCK, THE BOLT WAITS FOR HIM, 6.4 s (was 2.8 s: a finger spark, a storm, a bolt on his head).
        //
        // ⚠️ RESTAGED 2026-10-09, UNPITCHED. The owner was away and asked for "something for the rest of the heroes ...
        // surprise me with something really good", so this was not chosen from options as batch 1 was. BLOCKING until he
        // says the idea stays. The first cutscene is whole in `tools/cutscene_rework/old/`.
        //
        // The ultimate makes him faster than everybody for the rest of the match, so the cutscene shows exactly that,
        // once, as a joke only he is in on:
        //   1. THE CALL (0 to 1.2): the cocky flick, the finger up, the storm comes in and answers far off (kept).
        //   2. THE STOP (1.2 to 1.94), from low, looking up past him: the real bolt comes down the sky at him, and
        //      the world STOPS with its tip an arm's length over his hand. The rain hangs. Everyone else is a statue.
        //   3. THE LOOK (1.94 to 2.7), close: he is the only thing moving. He looks at the lens.
        //   4. THE POKE (2.7 to 3.6): he reaches up and pokes the tip of the lightning with one finger. It wobbles.
        //   5. THE SHRUG (3.6 to 4.5), wide and travelling round the stopped court: hung rain, statues, the bolt held
        //      from the cloud to just over him, and him shrugging at it.
        //   6. THE LET GO (4.5 to 6.4): he sets his feet and lets the world go. The rain falls, the bolt finishes its
        //      trip into him, the ring goes out to the ultimate's own 4.5 m and whoever is inside it jumps, and he is
        //      left gold and crackling.
        //
        // ⚠️ THE STOP IS ONE CLOCK (`ZackWorld`): the rain, the clouds and the storm's flicker all read it, so they stop
        // and start together. His body and the lens read the real one.
        //
        // ⚠️ WHO IS ZAPPED IS THE ABILITY'S RULE (`ZackHeroKit`, 4.5 m). This shows the ring at that size and jolts the
        // staged players who really stand inside it; it decides nothing.
        //
        // The free hand authors the gesture; the carrying hand keeps its actual shoe. Reduced effects keep the shapes
        // and drop the two flashes, as before.
        // =========================================================================================
        private const float ZkSlow = 1.80f, ZkStopped = 1.94f, ZkPoke = 3.15f, ZkGo = 5.20f, ZkHit = 5.27f, ZkReach = 4.5f;
        private static readonly Vector3 ZkTop = new Vector3(-.25f, 13f, .1f), ZkHung = new Vector3(-.62f, 1.82f, .3f);

        private int _stormLow, _skyline, _stormHigh, _fingerSpark, _answerBolt, _snapBolt, _snapGlow, _zkTip, _zkTick, _zkRing, _zkPop;
        private readonly List<int> _clouds = new List<int>(5), _rooftops = new List<int>(7), _zkRain = new List<int>(40), _zkJolts = new List<int>(4);
        private readonly List<LineRenderer> _crackle = new List<LineRenderer>(3);
        private static readonly Color ZackGold = new Color(1, .82f, .2f, .95f);

        private void BuildZack()
        {
            _stormLow = Wall("StormGround", 0, 1.3f, new Color(.09f, .09f, .1f, 1f), radius: 12f);
            _stormHigh = Wall("StormSky", 1.3f, 16, new Color(.13f, .14f, .17f, 1f), radius: 12f, emission: .1f, cap: true);
            // The storm is a backdrop, not a sun-lit surface: keep dark hair legible.
            ZackUnlitBackdrop(_stormLow); ZackUnlitBackdrop(_stormHigh);
            // A roofline skyline cut into the low band: condo blocks at different heights, shoulder to shoulder, so it
            // reads as a city and not as monoliths (`previews/zack_v3_stage.png`).
            for (int i = 0; i < 16; i++)
                _rooftops.Add(Add("Skyline" + i, VfxShapes.Prism(4, 1, 1), new Color(.07f, .07f, .08f, 1f), .02f));
            _skyline = Add("SkylineWindows", VfxShapes.TwoSided(VfxShapes.Wedges(9, .9f, 22, 0, .1f, 17)), new Color(1, .84f, .45f, .5f), .6f);
            for (int i = 0; i < 5; i++)
                _clouds.Add(Add("StormCloud" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .3f, 70 + i)), new Color(.13f, .13f, .15f, .9f), .02f));
            _fingerSpark = Add("FingerSpark", VfxShapes.TwoSided(VfxShapes.Star(4, .35f, 9)), ZackGold, .9f);
            _answerBolt = Add("AnswerBolt", VfxShapes.Bolt(1, 7, .16f, .045f, 2, 91), ZackGold, .9f);
            _snapBolt = Add("SnapBolt", VfxShapes.Bolt(1, 11, .10f, .05f, 4, 93), ZackGold, 1f);
            _snapGlow = AddGlow("SnapGlow", new Color(1, .85f, .3f, .5f), billboard: false, falloff: 2.5f, core: .35f);
            _zkTip = AddGlow("HungBoltTip", new Color(1, .9f, .5f, 1f), falloff: 2.2f, core: .5f);
            _zkPop = Add("PokeSpark", VfxShapes.TwoSided(VfxShapes.Star(6, .3f, 14)), new Color(1, .95f, .7f, 1f), 1f);
            _zkTick = Add("WorldStops", VfxShapes.Collar(40, .02f, .96f), new Color(.8f, .86f, 1f, .7f), .5f);
            _zkRing = Add("OverclockRing", VfxShapes.Collar(40, .04f, .9f), ZackGold, 1f);
            for (int i = 0; i < 3; i++) _crackle.Add(Line("FingerCrackle" + i, 8, .012f, ZackGold));
            for (int i = 0; i < 40; i++)
                _zkRain.Add(Add("Rain" + i, VfxShapes.Prism(4, 1, 1), new Color(.74f, .8f, .92f, .6f), .35f));

            // Everyone else where they stand, for the stop to turn into statues and the ring to reach or not reach.
            StageOthers(2.6f, 9f);
            for (int i = 0; i < _others.Count && i < 4; i++)
                _zkJolts.Add(Add("ZapJolt" + i, VfxShapes.TwoSided(VfxShapes.Star(5, .3f, 30 + i)), ZackGold, 1f));
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

        /// <summary>
        /// ⚠️ THE WORLD'S CLOCK. It runs with the real one, brakes to nothing between `ZkSlow` and `ZkStopped`, stands still
        /// until `ZkGo` and then runs again. Everything that is "the world" reads this, so it all stops as one thing.
        /// </summary>
        private static float ZackWorld(float t)
        {
            if (t <= ZkSlow) return t;
            float brake = ZkStopped - ZkSlow;
            float u = Mathf.Clamp01((t - ZkSlow) / brake);
            float stopped = ZkSlow + brake * (u - u * u * .5f);
            return t <= ZkGo ? stopped : stopped + (t - ZkGo);
        }

        private void SampleZack(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float world = ZackWorld(t);
            float hung = Ease(ZkSlow, ZkStopped, t) * (1 - Ease(ZkGo, ZkGo + .1f, t));
            // The storm rolls in as the finger goes up; before that he is just on the court.
            float storm = Ease(.45f, .95f, t) * leave;
            float answer = Flash(t, .82f, .07f), snap = Flash(t, ZkHit + .02f, .1f);
            var flicker = new Color(.13f, .14f, .17f, 1f) * (1 + .8f * Mathf.Max(answer, snap));
            flicker.a = 1f;
            Tint(_stormLow, storm); Tint(_stormHigh, storm, flicker);

            for (int i = 0; i < _rooftops.Count; i++)
            {
                float angle = (-160 + i * 21.3f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 11.4f, 0, Mathf.Cos(angle) * 11.4f);
                float height = 1.3f + (i * 53 % 7) * .16f + (i % 5 == 2 ? .5f : 0);
                Place(_rooftops[i], at, new Vector3(2.9f, height, 1.2f), Quaternion.LookRotation(-at.normalized, Vector3.up) * Quaternion.Euler(0, 45, 0), storm);
            }
            Place(_skyline, new Vector3(0, 1.7f, -.3f), new Vector3(11f, 1, 11f), Quaternion.Euler(0, 180, 0), storm * .6f);

            // The clouds slide overhead on the world's clock, so they stop with it.
            for (int i = 0; i < _clouds.Count; i++)
            {
                float angle = (-100 + i * 50 + world * 9) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 9.5f, 8.2f + (i % 2) * 1.1f - storm * .6f, Mathf.Cos(angle) * 9.5f);
                var face = Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                Place(_clouds[i], at, new Vector3(3.6f, 1, 1.7f), face, storm);
            }

            // The rain: falling on the world's clock. Hung, each streak is a short still dash.
            const float ceiling = 8f;
            for (int i = 0; i < _zkRain.Count; i++)
            {
                float a = i * 2.39996f, r = 1.1f + (i * 37 % 23) / 23f * 7.4f;
                float y = ceiling - Mathf.Repeat(i * 1.37f + world * 10.5f, ceiling);
                float length = Mathf.Lerp(.42f, .2f, hung);
                Place(_zkRain[i], new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r), new Vector3(.024f, length, .024f), Quaternion.Euler(4, 0, 3),
                    Ease(.8f, 1.2f, t) * storm * .85f);
            }

            // The flick: one spark off the fingertip.
            float spark = Ease(.36f, .42f, t) * (1 - Ease(.42f, .62f, t));
            Place(_fingerSpark, FreePalm + new Vector3(0, .1f, .12f) + Vector3.up * Ease(.42f, .62f, t) * .25f,
                Vector3.one * .09f, Quaternion.Euler(-90, t * 400, 0), spark * leave);
            Place(_answerBolt, new Vector3(-.3f, 6.5f, -6f), new Vector3(.8f, 1.8f, .8f), Quaternion.Euler(180, 0, 0) * Quaternion.Euler(0, 30, 0),
                (_reducedEffects ? .6f : 1) * Ease(.78f, .82f, t) * (1 - Ease(.9f, 1.2f, t)) * storm);

            // The crackle: up off his finger before the bolt comes, and all round him once it has landed.
            float up = Ease(.62f, .72f, t) * (1 - Ease(1.45f, 1.7f, t));
            float charged = Ease(ZkHit, ZkHit + .12f, t);
            for (int i = 0; i < _crackle.Count; i++)
            {
                var line = _crackle[i];
                int points = line.positionCount;
                if (charged <= 0)
                {
                    line.widthMultiplier = .012f * up * leave;
                    for (int k = 0; k < points; k++)
                    {
                        float u = k / (points - 1f);
                        float jag = k == 0 ? 0 : Mathf.Sin(k * 7.3f + i * 2.1f + Mathf.Floor(t * 14) * 1.7f) * .06f;
                        line.SetPosition(k, FreePalm + new Vector3((i - 1) * .05f + jag, .1f + u * .9f * up, jag * .5f));
                    }
                    continue;
                }
                // Three arcs that run round his body at different heights and leans, redrawn fourteen times a second.
                line.widthMultiplier = .02f * charged * leave;
                float step = Mathf.Floor(t * 14);
                for (int k = 0; k < points; k++)
                {
                    float around = (k / (points - 1f) * 200 + step * 67 + i * 120) * Mathf.Deg2Rad;
                    float jag = Mathf.Sin(k * 5.1f + i * 2.7f + step * 1.9f) * .07f;
                    line.SetPosition(k, new Vector3(Mathf.Sin(around) * (.5f + jag), .55f + i * .45f + Mathf.Sin(around * 2 + i) * .16f + jag, Mathf.Cos(around) * (.5f + jag)));
                }
            }

            // THE BOLT. It comes down the sky, is stopped an arm's length over his hand, and is let go into him.
            float down = Ease(1.48f, ZkStopped, t);
            float land = Mathf.Clamp01((t - ZkGo) / (ZkHit - ZkGo));
            Vector3 tip = Vector3.Lerp(Vector3.Lerp(ZkTop, ZkHung, down), HeadPoint - Vector3.up * .25f, land);
            // Poked, it rings like a struck wire.
            float since = t - ZkPoke;
            if (since > 0 && t < ZkGo) tip.x += Mathf.Sin(since * 34f) * .07f * Mathf.Exp(-since * 4.5f);
            Vector3 along = tip - ZkTop;
            float bolt = Ease(1.48f, 1.54f, t) * (1 - Ease(ZkHit + .1f, ZkHit + .45f, t)) * storm;
            // 3.2 across: at 1.1 it was a thread from the wide shot (film z1).
            Place(_snapBolt, ZkTop, new Vector3(3.2f, Mathf.Max(.05f, along.magnitude), 3.2f),
                Quaternion.FromToRotation(Vector3.up, along.normalized), bolt);
            // Its tip burns while it waits, slow and even: the one light in a stopped world.
            float waiting = Ease(ZkStopped - .05f, ZkStopped + .1f, t) * (1 - Ease(ZkGo, ZkHit, t));
            PlaceGlow(_zkTip, tip, Vector3.one * (.85f + .1f * Mathf.Sin(t * 5f)), Quaternion.identity, waiting * leave * 1.2f);

            // The poke: a small bright star where his finger meets it.
            float pop = Mathf.Clamp01(since / .05f) * (1 - Mathf.Clamp01((since - .05f) / .28f));
            Place(_zkPop, Vector3.Lerp(FreePalm + Vector3.up * .12f, ZkHung, .5f), Vector3.one * (.14f + .2f * Mathf.Clamp01(since / .3f)),
                Quaternion.Euler(-90, since * 300, 0), since > 0 ? pop : 0);

            // The stop goes out from him across the court as one thin ring.
            float tickAge = t - (ZkStopped - .06f), tickU = Mathf.Clamp01(tickAge / .55f);
            Place(_zkTick, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.4f, 11f, tickU), Quaternion.identity, tickAge >= 0 ? (1 - tickU) * .9f * storm : 0);

            // The landing: light on the ground under him, and the ring out to the ultimate's own reach.
            float hit = t - ZkHit, ringU = Mathf.Clamp01(hit / .3f);
            Place(_snapGlow, new Vector3(0, .03f, 0), Vector3.one * (.8f + Ease(ZkHit, ZkHit + .35f, t) * 2.2f), Quaternion.Euler(90, 0, 0),
                hit >= 0 ? (1 - Ease(ZkHit + .2f, ZkHit + .9f, t)) * storm : 0);
            Place(_zkRing, Vector3.up * .06f, Vector3.one * Mathf.Lerp(.3f, ZkReach, 1 - (1 - ringU) * (1 - ringU)), Quaternion.identity,
                hit >= 0 ? (1 - Ease(ZkHit + .3f, ZkHit + .8f, t)) * storm : 0);

            // Whoever really stands inside it jumps as the ring passes them, with a spark over their head.
            for (int i = 0; i < _others.Count && i < _zkJolts.Count; i++)
            {
                var other = _others[i];
                float reached = hit - other.Reach / ZkReach * .3f;
                float jolt = other.Reach <= ZkReach && reached >= 0 ? Mathf.Exp(-reached * 5f) * Mathf.Clamp01(reached / .03f) : 0;
                other.Holder.transform.localPosition = other.Home + new Vector3(Mathf.Sin(t * 91f + i), 0, Mathf.Cos(t * 77f + i * 2)) * (.07f * jolt)
                    + Vector3.up * (.18f * jolt);
                Place(_zkJolts[i], other.Home + Vector3.up * 2.35f, Vector3.one * (.2f + .25f * jolt), Quaternion.Euler(-90, t * 500 + i * 60, 0), jolt * leave);
            }
        }

        /// <summary>The colour goes out of the world while it is stopped, and comes back with the bolt.</summary>
        private void ZackGrade(float t, out float brightness, out float saturation)
        {
            float stopped = Ease(ZkSlow, ZkStopped, t) * (1 - Ease(ZkGo, ZkHit, t));
            saturation = Mathf.Lerp(1f, .35f, stopped);
            brightness = Mathf.Lerp(1f, .9f, stopped) * (1 + .15f * Flash(t, ZkHit + .03f, .12f));
        }
    }
}
