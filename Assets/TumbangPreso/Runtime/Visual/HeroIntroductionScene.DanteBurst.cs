using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, THE DENSITY PASS (plan section 5.4; the method's section 6: in every Genshin burst the AIR ITSELF is full). Every
        // kind is a typed table, his own rows (the kinds are Paete's, never his rows): hanging grit, shockwave rings, rising
        // embers, a rock curtain, rays, the travelling light, and a near layer at the lens in every shot. His colours only:
        // basalt, dust ochre, molten core and molten hot. Never white. Posed from the scene clock (and the story clock in THE HANG).
        // Reduced effects keep every shape and halve the light.
        // =========================================================================================

        // HANGING GRIT, 14: (x, y, z round him, size, hop height at each knock, seed, dust or basalt). Angular, each its own.
        private static readonly (float x, float y, float z, float size, float hop, int seed, bool dust)[] DnGritRows =
        {
            ( .62f, .42f,  .40f, .055f, .05f,  3, false), ( .38f, .78f,  .82f, .040f, .04f,  7, true ), (-.44f, .55f,  .70f, .048f, .06f, 11, false),
            ( .90f, .95f, -.10f, .036f, .03f, 13, true ), (-.70f, .30f, -.20f, .062f, .05f, 17, false), ( .20f, 1.15f, .55f, .032f, .04f, 19, false),
            (-.15f, .22f, 1.10f, .058f, .07f, 23, true ), ( .75f, .20f,  .95f, .045f, .06f, 29, false), (-.95f, .85f,  .30f, .038f, .03f, 31, false),
            ( .48f, .62f, -.55f, .050f, .05f, 37, true ), (-.30f, 1.02f, -.40f, .034f, .04f, 41, false), ( .05f, .48f,  .30f, .044f, .05f, 43, false),
            ( 1.10f, .50f,  .45f, .052f, .06f, 47, true ), (-.62f, .68f,  1.05f, .042f, .04f, 53, false),
        };
        // SHOCKWAVE RINGS, 6: (time, x, z, final radius, thickness of the molten inner edge share). The punch, the second split, the three
        // heaves (where its hands slam down), the stomp.
        private static readonly (float at, float x, float z, float radius, float inner)[] DnRingRows =
        {
            (DnPunch, .15f, .85f, 3.0f, .9f), (DnSplit2, 0f, 1.2f, 6.5f, .7f),
            (4.00f, -2.62f, 2.9f, 3.2f, .8f), (4.50f, 2.70f, 2.7f, 3.6f, .8f), (4.95f, -2.62f, 2.9f, 4.0f, .8f),
            (DnStomp, 2.75f, 3.4f, 7.5f, 1f),
        };
        // RISING EMBERS, 30: (seam 0 to 5, share along it, birth time, rise speed, sway, size). Uneven on purpose.
        private static readonly (int seam, float along, float born, float rise, float sway, float size)[] DnMoteRows =
        {
            (0,.22f,2.30f,.70f,.08f,.045f),(0,.48f,2.36f,.95f,.05f,.035f),(0,.71f,2.52f,.62f,.10f,.05f),(1,.18f,2.33f,.85f,.06f,.04f),(1,.40f,2.44f,1.10f,.04f,.03f),
            (1,.66f,2.61f,.78f,.09f,.045f),(1,.88f,2.75f,.90f,.07f,.035f),(2,.25f,2.31f,.68f,.05f,.05f),(2,.52f,2.48f,.98f,.08f,.04f),(2,.80f,2.66f,.74f,.06f,.03f),
            (3,.30f,2.38f,.88f,.07f,.04f),(3,.58f,2.55f,.64f,.11f,.05f),(3,.84f,2.80f,1.05f,.05f,.035f),(4,.20f,2.42f,.76f,.09f,.045f),(4,.45f,2.57f,.92f,.04f,.03f),
            (4,.73f,2.70f,.69f,.08f,.05f),(5,.28f,2.35f,1.00f,.06f,.04f),(5,.55f,2.50f,.81f,.10f,.045f),(5,.79f,2.68f,.73f,.05f,.035f),(0,.35f,3.05f,.88f,.07f,.04f),
            (1,.30f,3.35f,.80f,.06f,.045f),(2,.62f,3.60f,.95f,.09f,.035f),(3,.42f,3.90f,.70f,.05f,.05f),(4,.60f,4.20f,1.02f,.08f,.04f),(5,.40f,4.55f,.77f,.07f,.045f),
            (0,.62f,4.90f,.90f,.06f,.035f),(1,.52f,5.25f,.84f,.09f,.05f),(2,.36f,5.60f,.72f,.05f,.04f),(3,.70f,5.95f,.97f,.08f,.035f),(4,.30f,6.30f,.86f,.06f,.045f),
        };
        // THE ROCK CURTAIN, 24: chunks sliding off its back as it rises: (x, y, z on the beast, heave it goes on 0 to 2, delay, size, seed).
        private static readonly (float x, float y, float z, int heave, float delay, float size, int seed)[] DnCurtainRows =
        {
            ( 1.2f,2.9f,-.6f,0,.00f,.30f, 2),(-1.3f,2.8f,-.2f,0,.04f,.24f, 5),( .4f,3.2f,-1.6f,0,.08f,.20f, 8),(-.6f,3.3f, .6f,0,.12f,.18f,12),
            ( 1.9f,2.6f, .5f,0,.06f,.26f,15),(-2.0f,2.5f, .7f,0,.10f,.22f,18),( .9f,2.3f,-1.9f,0,.15f,.17f,21),(-.2f,3.4f,-.9f,0,.02f,.16f,24),
            ( 1.5f,3.0f,-.1f,1,.00f,.28f,27),(-1.1f,3.1f,-.9f,1,.05f,.22f,30),( .2f,3.3f, .7f,1,.09f,.19f,33),(-1.8f,2.7f,-.4f,1,.03f,.25f,36),
            ( 2.1f,2.8f, .9f,1,.11f,.21f,39),( -.7f,2.6f,-1.8f,1,.07f,.18f,42),( 1.0f,3.4f,-1.2f,1,.13f,.16f,45),(-1.5f,3.2f, .3f,1,.01f,.20f,48),
            ( -.9f,3.0f,-.3f,2,.00f,.27f,51),( 1.4f,3.1f,-.7f,2,.04f,.23f,54),( .1f,2.9f,-1.7f,2,.08f,.20f,57),(-2.1f,2.6f, .6f,2,.12f,.24f,60),
            ( 2.0f,2.7f, .2f,2,.02f,.22f,63),( .7f,3.3f, .4f,2,.06f,.17f,66),(-.4f,3.3f,-1.3f,2,.10f,.19f,69),( 1.7f,2.4f,-1.5f,2,.14f,.18f,72),
        };
        // RAYS, 9: warm molten rays behind the pair at the stomp, slowly turning: (angle, length, width, turn speed deg/s).
        private static readonly (float angle, float length, float width, float turn)[] DnRayRows =
        {
            (  8f, 9.5f, .9f, 4f), ( 46f, 7.8f, .7f, 4f), ( 83f, 10.2f, 1.1f, 4f), (122f, 8.4f, .8f, 4f), (160f, 9.0f, .75f, 4f),
            (201f, 7.4f, .65f, 4f), (238f, 10.6f, 1.0f, 4f), (279f, 8.1f, .7f, 4f), (318f, 9.3f, .85f, 4f),
        };
        // THE NEAR LAYER, per shot: (shot index, share from the lens toward the focus, screen x, screen y, size, ember or grit, drift).
        private static readonly (int shot, float share, float sx, float sy, float size, bool ember, float drift)[] DnNearRows =
        {
            (0,.18f,-.55f,.30f,.030f,false,.05f),(0,.22f,.62f,-.35f,.026f,false,.04f),(0,.16f,.40f,.52f,.020f,false,.06f),(0,.24f,-.70f,-.48f,.034f,false,.03f),
            (1,.30f,-.62f,.40f,.010f,false,.03f),(1,.34f,.58f,-.42f,.012f,true,.04f),(1,.28f,.70f,.45f,.009f,false,.05f),(1,.32f,-.50f,-.55f,.011f,false,.04f),
            (2,.20f,-.60f,.35f,.028f,false,.06f),(2,.18f,.55f,-.30f,.024f,true,.05f),(2,.22f,.68f,.50f,.022f,false,.07f),(2,.16f,-.72f,-.45f,.030f,true,.04f),(2,.25f,.15f,.70f,.020f,false,.05f),
            (3,.12f,-.58f,.28f,.050f,true,.08f),(3,.10f,.62f,-.36f,.045f,true,.07f),(3,.14f,.48f,.56f,.040f,false,.06f),(3,.11f,-.66f,-.52f,.055f,true,.09f),(3,.13f,.05f,-.66f,.038f,true,.07f),
            (4,.16f,-.62f,.42f,.040f,true,.06f),(4,.15f,.66f,-.30f,.036f,false,.05f),(4,.18f,.52f,.58f,.034f,true,.07f),(4,.14f,-.70f,-.50f,.045f,false,.05f),
            (5,.14f,-.58f,.36f,.045f,true,.07f),(5,.12f,.64f,-.40f,.040f,true,.06f),(5,.16f,.46f,.60f,.036f,false,.08f),(5,.13f,-.68f,-.56f,.050f,true,.06f),
            (6,.12f,-.60f,.40f,.050f,true,.08f),(6,.11f,.66f,-.34f,.046f,true,.07f),(6,.14f,.50f,.62f,.040f,false,.06f),(6,.10f,-.72f,-.50f,.055f,true,.09f),(6,.12f,.10f,-.70f,.042f,true,.07f),
        };

        private static readonly Color DnBasalt = new Color(.31f, .27f, .24f, 1f), DnDust = new Color(.42f, .32f, .22f, 1f);
        private static readonly Color DnMolten = new Color(1f, .42f, .08f), DnHot = new Color(1f, .62f, .18f);

        private readonly List<int> _dnGrit = new List<int>(14), _dnRings = new List<int>(6), _dnRingCores = new List<int>(6),
                                   _dnMotes = new List<int>(30), _dnCurtain = new List<int>(24), _dnRays = new List<int>(9), _dnNear = new List<int>(32);
        private int _dnDiveLight, _dnArmLight, _dnChestLightL, _dnChestLightR;

        private void BuildDanteBurst()
        {
            foreach (var r in DnGritRows)
                _dnGrit.Add(AddSolid("DanteGrit" + r.seed, VfxShapes.Prism(4 + r.seed % 3, 1f, .6f, .35f, 20f, r.seed), r.dust ? DnDust : DnBasalt));
            for (int i = 0; i < DnRingRows.Length; i++)
            {
                _dnRings.Add(AddGlow("DanteShockRing" + i, new Color(.62f, .48f, .32f), DnRingMesh, billboard: false, band: true, falloff: 1.1f, core: .45f));
                _dnRingCores.Add(AddGlow("DanteShockCore" + i, DnMolten, DnRingMesh, billboard: false, band: true, falloff: 1.4f, core: .5f));
            }
            for (int i = 0; i < DnMoteRows.Length; i++)
                _dnMotes.Add(AddGlow("DanteEmber" + i, i % 3 == 0 ? DnHot : DnMolten, falloff: 2.4f, core: .7f));
            foreach (var r in DnCurtainRows)
                _dnCurtain.Add(AddSolid("DanteCurtain" + r.seed, VfxShapes.Prism(5, 1f, .7f, .3f, 15f, r.seed), r.seed % 3 == 0 ? DnDust : DnBasalt));
            for (int i = 0; i < DnRayRows.Length; i++)
                _dnRays.Add(AddGlow("DanteRay" + i, i % 2 == 0 ? DnMolten : DnHot, falloff: 1.6f, core: .4f, billboard: false, band: true));
            for (int i = 0; i < DnNearRows.Length; i++)
            {
                var r = DnNearRows[i];
                _dnNear.Add(r.ember ? AddGlow("DanteNear" + i, DnHot, falloff: 2.2f, core: .8f)
                                    : AddSolid("DanteNear" + i, VfxShapes.Prism(4, 1f, .6f, .35f, 22f, 90 + i), i % 2 == 0 ? DnBasalt : DnDust));
            }
            _dnDiveLight = AddGlow("DanteDiveLight", DnHot, falloff: 2f, core: 1f);
            _dnArmLight = AddGlow("DanteArmLight", DnHot, falloff: 2f, core: 1f);
            _dnChestLightL = AddGlow("DanteChestLightL", DnHot, falloff: 2f, core: .9f);
            _dnChestLightR = AddGlow("DanteChestLightR", DnHot, falloff: 2f, core: .9f);
        }

        private void SampleDanteBurst(float t, float story)
        {
            float light = _reducedEffects ? .5f : 1f;
            // HANGING GRIT: still from the first frame, a hop on each knock, pulled into his cocked fist, gone into it at the punch.
            var fist = RightPalm;
            for (int i = 0; i < _dnGrit.Count; i++)
            {
                var r = DnGritRows[i];
                var home = new Vector3(r.x, DnFloat + r.y, r.z);
                float hop = 0f;
                foreach (var k in new[] { DnKnock1, DnKnock2 }) { float a = t - k - i * .006f; if (a > 0f && a < .22f) hop += Mathf.Sin(a / .22f * Mathf.PI) * r.hop; }
                float drift = _reducedEffects ? 0f : Mathf.Sin(story * .9f + i) * .012f;
                float pull = Ease(DnWind + i * .008f, DnWind + .15f + i * .008f, t);
                var at = Vector3.Lerp(home + Vector3.up * (hop + drift), fist + new Vector3(Mathf.Sin(i * 2.3f) * .08f, Mathf.Cos(i * 1.7f) * .08f, .02f), pull * pull);
                bool gone = t >= DnPunch;
                Place(_dnGrit[i], at, Vector3.one * r.size * (gone ? 0f : 1f), Quaternion.Euler(r.seed * 13f + story * 20f * (1f - pull), r.seed * 29f, r.seed * 7f), gone ? 0f : 1f);
            }
            // SHOCKWAVE RINGS: a thick dust-ochre ring flat on the court, racing out, with a molten inner edge.
            for (int i = 0; i < DnRingRows.Length; i++)
            {
                var r = DnRingRows[i];
                float age = t - r.at;
                float u = Mathf.Clamp01(age / .45f);
                float radius = Mathf.Lerp(.2f, r.radius, 1f - (1f - u) * (1f - u));
                float on = age < 0f ? 0f : (1f - u);
                float y = DnFloat + .03f + (i >= 2 && i <= 4 ? 0f : 0f);
                PlaceGlow(_dnRings[i], new Vector3(r.x, y, r.z), new Vector3(radius, 1f, radius), Quaternion.identity, on * 1.1f * light);
                PlaceGlow(_dnRingCores[i], new Vector3(r.x, y + .004f, r.z), new Vector3(radius * .9f, 1f, radius * .9f), Quaternion.identity, on * r.inner * 1.2f * light);
            }
            // RISING EMBERS from the seams, on the story clock, each born on its own seam and rising, swaying, shrinking out.
            for (int i = 0; i < DnMoteRows.Length; i++)
            {
                var r = DnMoteRows[i];
                float age = story - r.born;
                if (age < 0f || age > 1.6f) { PlaceGlow(_dnMotes[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                var p0 = Vector2.Lerp(DnCore[r.seam], DnKink[r.seam], Mathf.Clamp01(r.along * 2f));
                var p1 = Vector2.Lerp(DnKink[r.seam], DnFar[r.seam], Mathf.Clamp01(r.along * 2f - 1f));
                var on = r.along < .5f ? p0 : p1;
                var at = new Vector3(on.x + Mathf.Sin(age * 3f + i) * r.sway, DnFloat + .05f + age * r.rise, on.y);
                PlaceGlow(_dnMotes[i], at, Vector3.one * r.size * 2.2f * (1f - age / 1.6f), Quaternion.identity, (1f - age / 1.6f) * 1.3f * light);
            }
            // THE ROCK CURTAIN: chunks slide off its back on each heave, fall to the court and break smaller there.
            for (int i = 0; i < DnCurtainRows.Length; i++)
            {
                var r = DnCurtainRows[i];
                float start = DnHeaves[r.heave] + r.delay;
                float age = t - start;
                if (_dnTitan == null || age < -.6f || age > 1.4f) { Place(_dnCurtain[i], Vector3.zero, Vector3.zero, Quaternion.identity, 0f); continue; }
                // Riding on its back until it goes, then free fall from where it left.
                float ride = Mathf.Min(age, 0f);
                var onBack = _root.transform.InverseTransformPoint(_dnTitan.TransformPoint(new Vector3(r.x, r.y, r.z)));
                float fall = Mathf.Max(0f, age);
                var at = onBack + new Vector3(Mathf.Sign(r.x) * fall * .9f, -4.9f * fall * fall, -fall * .3f);
                bool landed = at.y < DnFloat;
                if (landed) at.y = DnFloat;
                float s = landed ? (age < 1.0f ? .7f : .4f) : 1f;
                Place(_dnCurtain[i], at, Vector3.one * r.size * s, Quaternion.Euler(r.seed * 11f + fall * 300f, r.seed * 23f, r.seed * 5f + fall * 120f), 1f);
            }
            // RAYS behind the pair at the stomp, slowly turning on the story clock (they all but stop in the hang).
            {
                float on = Ease(DnLoad + .2f, DnLoad + .8f, t) * light;
                var centre = new Vector3(-.4f, 6.0f, -5.8f);
                for (int i = 0; i < DnRayRows.Length; i++)
                {
                    var r = DnRayRows[i];
                    float a = (r.angle + r.turn * (story - DnLoad)) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    PlaceGlow(_dnRays[i], centre + dir * r.length * .5f, new Vector3(r.width, r.length, 1f),
                              Quaternion.Euler(0f, 0f, -r.angle - r.turn * (story - DnLoad)), on * (.55f + .5f * Flash(t, DnStomp, .1f)));
                }
            }
            // THE TRAVELLING LIGHT. The dive: from the fist along the widest seam toward them, then down into it (3.10).
            {
                float u = Ease(2.85f, DnDive, t), down = Ease(DnDive, DnDive + .18f, t);
                var from = DnFist; var to = new Vector3(.4f, DnFloat, 2.6f);
                var at = Vector3.Lerp(from, to, u) + Vector3.down * down * .6f;
                PlaceGlow(_dnDiveLight, at, Vector3.one * .55f, Quaternion.identity, (u > 0f ? 1f : 0f) * (1f - down) * 1.6f * light);
            }
            // Up its arm into its fist, and from his chest down his arms into his fists, through the load (5.30 to 6.60).
            {
                float u = Ease(DnLoad + .1f, DnHangFrom, t);
                float on = (u > 0f ? 1f : 0f) * (1f - Ease(DnStomp, DnStomp + .05f, t)) * 1.4f * light;
                if (_dnTitanArm != null)
                {
                    var along = _root.transform.InverseTransformPoint(_dnTitanArm.TransformPoint(new Vector3(0f, -2.42f * u, .2f)));
                    PlaceGlow(_dnArmLight, along, Vector3.one * .9f, Quaternion.identity, on);
                }
                var chest = HeadPoint + new Vector3(0f, -.75f, .1f);
                PlaceGlow(_dnChestLightL, Vector3.Lerp(chest, FreePalm, u), Vector3.one * .3f, Quaternion.identity, on * .8f);
                PlaceGlow(_dnChestLightR, Vector3.Lerp(chest, RightPalm, u), Vector3.one * .3f, Quaternion.identity, on * .8f);
            }
            // THE NEAR LAYER: grit and embers on the lens line of the shot being seen, drifting, on the story clock.
            int shot = ShotIndexAt(t);
            for (int i = 0; i < DnNearRows.Length; i++)
            {
                var r = DnNearRows[i];
                bool here = r.shot == shot && _performance != null;
                if (!here) { if (DnNearIsGlow(i)) PlaceGlow(_dnNear[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); else Place(_dnNear[i], Vector3.zero, Vector3.zero, Quaternion.identity, 0f); continue; }
                _performance.Shot(shot, t, out var eye, out var look, out var fov);
                var fwd = (look - eye).normalized;
                var right = Vector3.Cross(Vector3.up, fwd).normalized; var up = Vector3.Cross(fwd, right);
                float dist = Vector3.Distance(eye, look) * r.share;
                float half = Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * dist;
                float sway = _reducedEffects ? 0f : Mathf.Sin(story * 1.3f + i) * r.drift;
                var at = eye + fwd * dist + right * (r.sx * half * 1.6f + sway) + up * (r.sy * half + (r.ember ? story * .05f % .2f : -story * .03f % .15f));
                if (r.ember) PlaceGlow(_dnNear[i], at, Vector3.one * r.size * 4f, Quaternion.identity, 1.1f * light);
                else Place(_dnNear[i], at, Vector3.one * r.size, Quaternion.Euler(i * 31f + story * 40f, i * 17f, i * 11f), 1f);
            }
        }

        private bool DnNearIsGlow(int i) => DnNearRows[i].ember;
    }
}
