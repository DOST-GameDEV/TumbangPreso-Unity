using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT, 7.0 s (was TITAN FISSURE, 3.8 s; plan.md § 5 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-08. The owner, of the nine cutscenes after Paete's: a bolder staging idea chosen from
        // options. Of three for Dante he liked two ("dante i like 2 and 3") and agreed to them joined, at about 7 s:
        //   1. THE PUNCH (0 to 1.15): he plants and drives a fist into the court.
        //   2. THE CONTINENTS (1.15 to 3.0), from high above: the court is seven plates of stone on a molten sea, and
        //      they drift apart. This is the ultimate's own name made literal.
        //   3. THE RISE (3.4 to 5.3): out of the widest seam, under him, the horned basalt back shoulders up and lifts
        //      his plate; the plates round it are shoved aside.
        //   4. THE STOMP (5.3 to 7.0, one shot): it raises a fist and he loads both of his, held; they come down as one
        //      at 6.88 and the cutscene ENDS THERE, him still up on its back in the stamp pose.
        // ⚠️ NO BLAST IS SHOWN AND NOTHING GOES BACK UNDER HERE. The first cut (film d2) ran all five blasts and play then
        // fired the real five; asked, the owner: "end on the fist landing", then, of a cut that set him down inside the
        // cutscene: "the cutscene should end exactly when the fist lands before switching back to live first person
        // gameplay, by then dante should still be on the longhorn and only then will it disappeaer into the ground".
        // So the stage does not fade at its end as the others do: it is whole on the last frame, and the going under is
        // LIVE play's to show.
        //
        // What is kept of his earlier asks: basalt and molten seams with horn and claw silhouettes (AGENTS.md, "more
        // demonic skills"), stone that is SOLID and plain, no grain (the noisy basalt texture was refused), an
        // earthquake the lens feels. The two parted slabs and the ridge of the first cutscene are gone: that was
        // Bernardo Carpio's pose, the idea he did not pick this time.
        //
        // ⚠️ THE LOOK WAS DONE 2026-10-10, ONCE THE STAGING STOOD (the owner: "continue polishing all the ultimates. i dont
        // want to see anything bare in textures, anything looking unpolished, no vfx etc."). Until then everything here
        // was a stock prism in one grey on a flat orange disc. What each thing is now:
        //   * THE LONGHORN is a painted model (`Models/UltimateProps/longhorn`, `tools/build_ultimate_props_dante.py`,
        //     `UltimateProp.EarthPalette`), dressed as the cast is. Its right arm, its head and its jaw are joints; the
        //     arm swings from the shoulder the blocks' arm swung from, between the same two angles at the same times.
        //   * THE PLATES have broken edges that still fit their neighbours, a worn pale lip, a dark wall and a molten rim
        //     where they meet the sea. Their tops stay plain stone with one hairline each: no grain.
        //   * THE SEA is hot in the middle and dark at its rim, with two layers of bright veins turning slowly under the
        //     plates, crust adrift in the seams, and light coming up through every seam.
        //   * THE HORIZON is crags that come up out of the ground as he plants, and columns of smoke.
        //   * EVERY BLOW HAS ITS STONE: the punch throws shards and heaves a ring of court up round his fist; the rise
        //     shoulders rubble aside, sheds rock off its back and spits lava out of the seams; the raised fist gathers
        //     heat; the stomp lands in a flash, a ring and thrown rock.
        //   * IT IS LIT FROM INSIDE (second pass, the owner on film p4: "the longhorn lacks texture", "do more with
        //     emmissives"): its paint was redone in beds, pits, chips, ash and scorch, every seam carries a light that
        //     beats faster and hotter until the stomp, embers vent from the seams and grit falls off it whenever it moves,
        //     and the air over its back wavers.
        // ⚠️ NEARLY ALL OF IT IS OPAQUE, AND GROWS OR SHRINKS INSTEAD OF FADING. The stage is ringed by two see-through dust
        // walls, and a see-through thing in front of a see-through wall is drawn in whichever order its middle happens
        // to sort: a crag that faded in would have been painted over by the wall behind it. Opaque rock and opaque,
        // faceted smoke have no such order, and they are also what this world's things look like.
        //
        // ⚠️ EVERY TIME BELOW IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `_dante`): the punch, his lift
        // and the stamp are typed there, and the plate under him is carried by `LiftAt`, so it cannot leave his feet.
        // =========================================================================================
        private const float DnPunch = 1.10f, DnBreak = 1.12f, DnRiseFrom = 3.45f, DnRisen = 5.20f, DnStomp = 6.88f, DnTitanTop = 3.4f, DnFloat = .15f;
        // The six outer plates ride a little higher than his, so that each shows a wall and a molten rim over the sea.
        private const float DnOuterFloat = .21f;
        // ⚠️ HIS PLATE DOES NOT RIDE UP WITH HIM (owner, 2026-10-10, of a close look from below at the plate floating over
        // its back: "underside of dante's platform is invisible.. i feel like this should dissolve into rocks and have
        // dante actually stand on the longhorn"). As the back comes up under it the plate breaks up and falls off, and from
        // then on his feet are on the crown of its back.
        // ⚠️ IT BREAKS IN THE RISE SHOT, NEVER IN THE ONE FROM ABOVE, AND IT FALLS BY SIMULATED PHYSICS (owner, of the first
        // break-up: "i was actually thinking of it dissolving in the shot of the longhorn rising instead of this top down
        // view, that way it can be switched for a breakapart model for the dissolve (preferrably dropping with simulated
        // physics)"). There is no switch to see, because the centre plate IS the break-apart model from the moment the
        // court breaks: eight pieces lying shut, one flat top between them. At `DnCrumble`, 0.25 s into the rise shot,
        // seven of them are let go on a rigid-body fall that was simulated once, outside the game, against the longhorn's
        // back, shoulders, head and horns moving exactly as the lift moves them (`tools/bake_dante_plate_break.py`,
        // which writes `HeroIntroductionScene.DanteBreak.cs`). It is played back on the cutscene's own clock: the game's
        // physics is paused during a cutscene, the film scrubs it, and every peer must draw the same fall.
        private const float DnCrumble = DnRiseFrom + .2f;
        // ⚠️ A PIECE IS DRAWN THICKER THAN IT WAS SIMULATED. It fell as a 10 cm slab; from the low lens that was a dark line.
        // It is drawn 24 cm deep, the extra hanging under it, so its molten faces are wide enough to read. Lying shut it
        // is in the crown of the longhorn's back, where the extra is not seen.
        private const float DnBreakLooks = .24f;

        // The centre plate's corners (x, z), then one kink and one far corner per crack running out from each of them.
        private static readonly Vector2[] DnCore =
        { new Vector2(1.55f, .45f), new Vector2(.70f, 1.75f), new Vector2(-.95f, 1.50f), new Vector2(-1.70f, .05f), new Vector2(-.80f, -1.55f), new Vector2(.95f, -1.40f) };
        private static readonly Vector2[] DnKink =
        { new Vector2(5.3f, .6f), new Vector2(1.2f, 5.6f), new Vector2(-3.9f, 4.1f), new Vector2(-5.6f, -.9f), new Vector2(-2.0f, -5.2f), new Vector2(3.7f, -4.3f) };
        private static readonly Vector2[] DnFar =
        { new Vector2(10.6f, 2.6f), new Vector2(3.9f, 10.1f), new Vector2(-7.0f, 8.2f), new Vector2(-10.7f, -.4f), new Vector2(-5.4f, -9.3f), new Vector2(6.6f, -8.5f) };
        // The far edge of each outer plate bulges through one point between its two cracks.
        private static readonly Vector2[] DnRim =
        { new Vector2(8.2f, 7.4f), new Vector2(-1.6f, 11.0f), new Vector2(-10.2f, 4.4f), new Vector2(-9.0f, -6.2f), new Vector2(.8f, -10.9f), new Vector2(10.4f, -3.6f) };
        // Each outer plate's own share of the drift, its tilt once adrift (degrees about x and z) and its turn.
        private static readonly float[] DnShare = { 1.00f, .82f, 1.18f, .90f, 1.10f, .95f };
        private static readonly Vector3[] DnTilt =
        { new Vector3(3.5f, 5f, -2.5f), new Vector3(-4f, -3f, 1.5f), new Vector3(2f, 6f, 4f), new Vector3(-2.5f, -4f, -3.5f), new Vector3(4.5f, 2f, 2f), new Vector3(-3f, -6f, 3f) };

        // The horizon's crags: compass (degrees, 0 is ahead of him), how far out, how wide at the foot, how high, its lean,
        // and 1 for the nearer, darker row. None stands between compass 320 and 15: the last shot's lens is out there.
        private static readonly float[,] DnCrag =
        {
            { 20, 10.7f, .85f, 4.6f, 6, 0 }, { 58, 10.7f, .90f, 6.2f, 4, 0 }, { 80, 10.2f, .62f, 3.2f, 10, 1 }, { 104, 10.7f, .85f, 5.0f, -5, 0 },
            { 128, 10.2f, .60f, 2.8f, 7, 1 }, { 150, 10.7f, .95f, 6.8f, 3, 0 }, { 172, 10.2f, .66f, 3.7f, -9, 1 }, { 190, 10.7f, .88f, 5.6f, 6, 0 },
            { 214, 10.2f, .62f, 3.0f, -6, 1 }, { 236, 10.7f, .92f, 6.0f, -4, 0 }, { 258, 10.2f, .56f, 2.5f, 9, 1 }, { 282, 10.7f, .84f, 4.8f, 5, 0 },
            { 306, 10.2f, .60f, 3.2f, -7, 1 },
        };
        // The smoke columns behind them: compass, how far out, how big.
        private static readonly float[,] DnSmokeAt = { { 48, 10.6f, 1.0f }, { 118, 10.4f, 1.25f }, { 166, 10.7f, .9f }, { 208, 10.3f, 1.3f }, { 292, 10.6f, 1.05f } };
        // Where the dust rolls out from under it as it rises: compass only, kept off both lenses that watch it.
        private static readonly float[] DnRiseDustAt = { 75, 110, 150, 195, 235, 270, 300 };
        // ⚠️ ITS SEAMS ARE LIGHT, NOT ORANGE PAINT (owner, of film p4: "do more with emmissives"). The toon dress has no
        // glow of its own, so each seam carries a light: which joint it rides (0 its body, 1 its head, 2 its right arm),
        // where on that joint, and how big. The same places vent embers when it moves.
        private static readonly float[,] DnSeamLight =
        {
            { 0, -1.50f, 2.30f, 1.80f, 1.9f }, { 0, 1.50f, 2.30f, 1.80f, 1.9f }, { 0, -2.26f, 1.80f, 1.80f, 1.6f }, { 0, 2.26f, 1.80f, 1.80f, 1.6f },
            { 0, -2.80f, 1.30f, -.20f, 1.7f }, { 0, 2.80f, 1.30f, -.20f, 1.7f }, { 0, -2.55f, 1.30f, -1.80f, 1.5f }, { 0, 2.55f, 1.30f, -1.80f, 1.5f },
            { 0, -3.05f, 1.08f, 2.50f, 1.5f }, { 1, -1.30f, .05f, .92f, .9f }, { 1, 1.30f, .05f, .92f, .9f }, { 2, 0, -2.72f, .55f, 1.6f }, { 2, 0, -3.30f, .80f, 1.5f },
        };
        // The four pieces of court the stomp throws, each its own outline: they turn over in the air, so each is a whole slab.
        private static readonly Vector2[][] DnSlab =
        {
            new[] { new Vector2(-.9f, -.5f), new Vector2(.2f, -.9f), new Vector2(1f, -.2f), new Vector2(.6f, .8f), new Vector2(-.7f, .7f) },
            new[] { new Vector2(-1f, -.2f), new Vector2(-.1f, -.8f), new Vector2(.9f, -.6f), new Vector2(.8f, .5f), new Vector2(-.4f, .9f) },
            new[] { new Vector2(-.7f, -.8f), new Vector2(.8f, -.7f), new Vector2(1f, .3f), new Vector2(-.2f, .9f) },
            new[] { new Vector2(-.9f, -.4f), new Vector2(.5f, -.9f), new Vector2(.9f, .4f), new Vector2(.1f, .9f), new Vector2(-.8f, .5f) },
        };
        // Where its fist lands, in the stage's space.
        private static readonly Vector3 DnStompAt = new Vector3(2.75f, 0, 3.0f);

        private int _dnDustLow, _dnDustHigh, _dnDustWarm, _dnDustWarmHigh, _dnSea, _dnSeaVeinA, _dnSeaVeinB, _dnSeaHeat, _dnSeam, _dnSeamGlow, _dnEyeLeft, _dnEyeRight;
        private readonly List<Transform> _dnPlates = new List<Transform>(7);
        private readonly List<Vector2> _dnPlateHome = new List<Vector2>(7);
        private readonly List<int> _dnCrags = new List<int>(26), _dnSmoke = new List<int>(25), _dnCrackGlow = new List<int>(6), _dnChips = new List<int>(24),
                                   _dnEmbers = new List<int>(24), _dnLicks = new List<int>(16), _dnPuffs = new List<int>(6), _dnShards = new List<int>(9),
                                   _dnRocks = new List<int>(10), _dnRiseDust = new List<int>(7), _dnGather = new List<int>(8), _dnStompShards = new List<int>(8);
        private int _dnCoreGlow, _dnFistCharge, _dnPunchFlash, _dnPunchRubble, _dnPunchDust, _dnRubble, _dnUnderGlow, _dnBackGlow, _dnTitanFistGlow,
                    _dnMouthGlow, _dnPalmGlow, _dnStompFlash, _dnStompRing, _dnStompRubble, _dnBlastGlow;
        private Transform _dnTitan, _dnTitanArm, _dnTitanHead, _dnTitanJaw;
        private readonly List<int> _dnSeamLights = new List<int>(13), _dnVents = new List<int>(13), _dnGrit = new List<int>(14),
                                   _dnSlabs = new List<int>(4);
        private int _dnStompCore, _dnCrumbleLight;
        private Transform[] _dnBreak;
        private Vector3[] _dnBreakHome;
        private readonly List<int> _dnCrumbleDust = new List<int>(3), _dnSplashes = new List<int>(7);
        private bool _dnTitanModelled;

        private void BuildDante()
        {
            _dnDustLow = Wall("DustGround", 0, 1.4f, new Color(.2f, .14f, .09f, .8f), radius: 11.6f);
            _dnDustHigh = Wall("DustSky", 1.4f, 30, new Color(.42f, .32f, .22f, .72f), radius: 11.6f, emission: .08f, cap: true);
            // The sea lights the dust from below once the court has broken: a warm band low on the wall.
            _dnDustWarm = Wall("DustEmberLight", 0, 2.0f, new Color(.95f, .36f, .08f, .2f), radius: 11.5f, emission: .5f);
            _dnDustWarmHigh = Wall("DustEmberLightHigh", 2.0f, 4.6f, new Color(.95f, .36f, .08f, .08f), radius: 11.5f, emission: .5f);
            BuildDanteHorizon();
            BuildDanteSea();

            // The plates: the centre one he stands on, then six round it, each between two cracks.
            var stone = new Color(.34f, .30f, .27f, 1);
            // The centre plate is the break-apart pieces (`BuildDanteBreak`); this is only its place in the list.
            var home = Vector2.zero;
            foreach (var corner in DnCore) home += corner;
            home /= DnCore.Length;
            var centre = new GameObject("PlateCentre").transform; centre.SetParent(_root.transform, false);
            _dnPlates.Add(centre); _dnPlateHome.Add(home);
            BuildDanteBreak(stone);
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                var ring = new[] { DnCore[i], DnKink[i], DnFar[i], DnRim[i], DnFar[j], DnKink[j], DnCore[j] };
                var tone = stone * (i % 2 == 0 ? 1f : .9f); tone.a = 1;
                _dnPlates.Add(DantePlate("Plate" + i, ring, tone, .13f, DnOuterFloat, .15f, i, out home)); _dnPlateHome.Add(home);
            }

            _dnCrumbleLight = AddGlow("PlateBreakLight", new Color(1f, .5f, .1f, 1), billboard: false, falloff: 1.5f, core: .5f);
            for (int i = 0; i < 3; i++)
                _dnCrumbleDust.Add(DantePuff("PlateBreakDust" + i, VfxShapes.NovaShell(5, 8, .12f, 220 + i), new Color(.6f, .51f, .42f, 1)));
            // Where each piece meets the sea: a splash of light (and a crown of the sea, `HeroIntroductionScene.DanteFx.cs`).
            for (int i = 0; i < DnBreakSimulated; i++)
                _dnSplashes.Add(AddGlow("PlateSplash" + i, new Color(1f, .55f, .12f, 1), falloff: 1.6f, core: .5f, lift: .3f));

            BuildDanteSeams();
            BuildDantePunch();
            BuildDanteTitan();
            BuildDanteRise();
            BuildDanteStomp();
            BuildDanteFx();
        }

        // ------------------------------------------------------------------ makers

        /// <summary>A fraction from a number, the same every time: where a chip sits, how fast an ember climbs.</summary>
        private static float DanteHash(float n) { float s = Mathf.Sin(n * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }
        private static float DanteOut(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u); }
        /// <summary>A thing that swells fast and then dwindles, over an age of 0 to 1: opaque dust has no fade, so it has this.</summary>
        private static float DantePop(float age) => age <= 0 || age >= 1 ? 0 : age < .25f ? age / .25f : 1 - (age - .25f) / .75f;

        /// <summary>
        /// Molten rock: opaque, and lit by itself. Its own colour is kept dark and the light is all in the emission, because
        /// the sun on a bright orange surface plus its own glow clips to yellow.
        /// </summary>
        private int DanteLava(string name, Mesh mesh, Color colour)
        {
            var dim = colour * .3f; dim.a = 1;
            int index = AddSolid(name, mesh, dim);
            var renderer = _pieces[index].Renderer;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var material = renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b, 1) * .95f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return index;
        }

        /// <summary>
        /// Smoke and dust: opaque and round, and nearly all its own tone. Lit like rock, a pale puff has a bright side and a
        /// dark side and reads as a boulder hanging in the air (film p3); most of its colour is given as its own light instead.
        /// </summary>
        private int DantePuff(string name, Mesh mesh, Color colour)
        {
            var dim = colour * .4f; dim.a = 1;
            int index = AddSolid(name, mesh, dim);
            var renderer = _pieces[index].Renderer;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var material = renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b, 1) * .7f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return index;
        }

        /// <summary>Unlit, in its own vertex colours: the sea is a gradient, and the sun must not make a lit floor of it.</summary>
        private void DanteUnlit(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var piece = _pieces[index];
            var material = new Material(shader) { name = "Basilio molten sea", color = piece.Color };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }

        private void DanteUnder(int index, Transform parent)
        {
            _pieces[index].Transform.SetParent(parent, false);
            Place(index, Vector3.zero, Vector3.one, Quaternion.identity, 1);
        }

        // ------------------------------------------------------------------ the horizon

        private void BuildDanteHorizon()
        {
            // Two rows of crags, the far row paler with the dust that is between it and the lens.
            for (int i = 0; i < DnCrag.GetLength(0); i++)
            {
                var tone = DnCrag[i, 5] > .5f ? new Color(.19f, .15f, .13f, 1) : new Color(.33f, .26f, .2f, 1);
                _dnCrags.Add(BasilioStone("HorizonCrag" + i, VfxShapes.Spire(6, .34f, .42f, 100 + i), tone));
                // Its shorter neighbour, leaning the other way: one spire alone is a cone, two are broken rock.
                _dnCrags.Add(BasilioStone("HorizonCragLow" + i, VfxShapes.Spire(5, .42f, .42f, 200 + i), tone * .9f + new Color(0, 0, 0, 1)));
            }
            // Smoke as this world draws it: fat round puffs, opaque, climbing and swelling, close enough to run into one column.
            // Pale, the colour of the dust behind them: dark ones read as boulders hanging in the sky (film p1).
            for (int c = 0; c < DnSmokeAt.GetLength(0); c++)
                for (int k = 0; k < 5; k++)
                    _dnSmoke.Add(DantePuff("HorizonSmoke" + c + "_" + k, VfxShapes.NovaShell(6, 10, .05f, 120 + c * 5 + k), new Color(.5f, .42f, .35f, 1)));
        }

        private void SampleDanteHorizon(float t, float haze)
        {
            for (int i = 0; i < DnCrag.GetLength(0); i++)
            {
                // They come up out of the ground as he plants, one after another round the ring, each a little past its height and back.
                float up = Ease(.2f + i * .035f, .7f + i * .035f, t);
                float high = DnCrag[i, 3] * up * (1 + .14f * Mathf.Sin(up * Mathf.PI));
                float compass = DnCrag[i, 0] * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(compass), 0, Mathf.Cos(compass)) * DnCrag[i, 1];
                Place(_dnCrags[i * 2], at, new Vector3(DnCrag[i, 2], high, DnCrag[i, 2]), Quaternion.Euler(0, DnCrag[i, 0] + i * 31, DnCrag[i, 4]), up > .01f ? 1 : 0);
                float beside = (DnCrag[i, 0] + (i % 2 == 0 ? 6.5f : -6.5f)) * Mathf.Deg2Rad;
                var next = new Vector3(Mathf.Sin(beside), 0, Mathf.Cos(beside)) * (DnCrag[i, 1] - .25f);
                Place(_dnCrags[i * 2 + 1], next, new Vector3(DnCrag[i, 2] * 1.15f, high * (.42f + .12f * (i % 3)), DnCrag[i, 2] * 1.15f),
                      Quaternion.Euler(0, DnCrag[i, 0] + i * 57, -DnCrag[i, 4] * 1.4f), up > .01f ? 1 : 0);
            }
            for (int c = 0; c < DnSmokeAt.GetLength(0); c++)
            {
                float compass = DnSmokeAt[c, 0] * Mathf.Deg2Rad, size = DnSmokeAt[c, 2];
                var foot = new Vector3(Mathf.Sin(compass), 0, Mathf.Cos(compass)) * DnSmokeAt[c, 1];
                for (int k = 0; k < 5; k++)
                {
                    float age = Mathf.Repeat(t * .14f + k * .2f + c * .37f, 1f);
                    float swell = Mathf.Pow(Mathf.Sin(age * Mathf.PI), .6f) * haze;
                    var at = foot + new Vector3(age * age * 1.6f, .8f + age * 6.2f, age * age * -.7f);
                    Place(_dnSmoke[c * 5 + k], at, Vector3.one * (size * (.9f + age * 1.1f) * swell), Quaternion.Euler(age * 40, c * 50 + age * 90, k * 25), swell > .02f ? 1 : 0);
                }
            }
        }

        // ------------------------------------------------------------------ the sea

        private void BuildDanteSea()
        {
            _dnSea = Add("MoltenSea", DanteSeaMesh(), Color.white, 1f, plain: true);
            DanteUnlit(_dnSea);
            _pieces[_dnSea].Renderer.sortingOrder = -6;
            // Brighter veins in it, two layers turning against each other: through a seam they are seen sliding past.
            _dnSeaVeinA = DanteLava("MoltenVeinWide", VfxShapes.Fracture(7, 4, .03f, 5), new Color(1f, .68f, .18f, 1));
            _dnSeaVeinB = DanteLava("MoltenVeinFine", VfxShapes.Fracture(5, 4, .018f, 9), new Color(1f, .86f, .4f, 1));
            _dnSeaHeat = AddGlow("MoltenHeat", new Color(1f, .42f, .08f, 1), billboard: false, falloff: 1.3f, core: .15f);
        }

        /// <summary>The sea as rings of colour: hot at the middle, dark crimson where it meets the dust, each ring a little out of round.</summary>
        private static Mesh DanteSeaMesh()
        {
            const int sectors = 36;
            float[] radii = { 0, .2f, .46f, .74f, 1f };
            Color[] bands = { new Color(1f, .6f, .14f, 1), new Color(1f, .46f, .08f, 1), new Color(.95f, .31f, .05f, 1), new Color(.74f, .19f, .04f, 1), new Color(.4f, .09f, .04f, 1) };
            var vertices = new List<Vector3>(); var colours = new List<Color>(); var triangles = new List<int>();
            for (int ring = 0; ring < radii.Length; ring++)
                for (int s = 0; s <= sectors; s++)
                {
                    float a = s * Mathf.PI * 2 / sectors;
                    float r = radii[ring] * (ring == 0 || ring == radii.Length - 1 ? 1 : 1 + .09f * Mathf.Sin(a * 3 + ring * 1.7f) + .05f * Mathf.Sin(a * 7 + ring));
                    vertices.Add(new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r)); colours.Add(bands[ring]);
                    if (ring == radii.Length - 1 || s == sectors) continue;
                    int n = ring * (sectors + 1) + s, w = n + sectors + 1;
                    triangles.AddRange(new[] { n, n + 1, w, n + 1, w + 1, w });
                }
            var mesh = new Mesh { name = "Basilio molten sea" };
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return VfxShapes.TwoSided(mesh);
        }

        // ------------------------------------------------------------------ the plates

        /// <summary>
        /// One plate of the court, under its own holder, which sits on the plate's middle so it tilts about itself: a top,
        /// a paler worn lip round it, a dark wall down to <paramref name="wall"/>, and a molten rim flaring out from there
        /// to <paramref name="thick"/>. An outer plate (<paramref name="scar"/> 0 or more) also carries one hairline.
        /// </summary>
        private Transform DantePlate(string name, Vector2[] ring, Color colour, float wall, float thick, float flare, int scar, out Vector2 home)
        {
            home = Vector2.zero;
            foreach (var p in ring) home += p;
            home /= ring.Length;
            var holder = new GameObject(name).transform; holder.SetParent(_root.transform, false);

            var edge = DanteBroken(ring);
            var inset = new List<Vector2>(edge.Count); var flared = new List<Vector2>(edge.Count);
            for (int i = 0; i < edge.Count; i++)
            {
                edge[i] -= home;
                var outward = edge[i].normalized;
                inset.Add(edge[i] - outward * Mathf.Min(.13f, edge[i].magnitude * .3f));
                flared.Add(edge[i] + outward * flare);
            }
            var pale = colour * 1.45f; pale.a = 1; var dark = colour * .55f; dark.a = 1;
            DanteUnder(BasilioStone(name + "Top", DanteShell(inset, 0, null, 0), colour), holder);
            DanteUnder(BasilioStone(name + "Lip", DanteShell(inset, 0, edge, -.05f), pale), holder);
            DanteUnder(BasilioStone(name + "Wall", DanteShell(edge, -.05f, edge, -wall), dark), holder);
            DanteUnder(DanteLava(name + "MoltenRim", DanteShell(edge, -wall, flared, -thick), new Color(1f, .5f, .1f, 1)), holder);
            // An underside. A plate is seen from below the moment it heels or is looked up at, and without one it was a
            // skin with nothing in it.
            var under = colour * .4f; under.a = 1;
            DanteUnder(BasilioStone(name + "Under", DanteShell(flared, -thick, null, 0, true), under), holder);
            if (scar >= 0)
            {
                // One clean hairline across its top, a shade under the stone. Plain rock: this is the whole of its marking.
                var line = colour * .84f; line.a = 1;
                int index = BasilioStone(name + "Hairline", VfxShapes.Fracture(3, 2, .014f, 140 + scar), line);
                _pieces[index].Renderer.shadowCastingMode = ShadowCastingMode.Off;
                _pieces[index].Transform.SetParent(holder, false);
                Place(index, new Vector3(0, .008f, 0), new Vector3(1.5f, 1, 1.5f), Quaternion.Euler(0, scar * 77, 0), 1);
            }
            return holder;
        }

        /// <summary>
        /// ⚠️ A PLATE'S EDGE IS BROKEN, AND ITS NEIGHBOUR'S IS BROKEN THE SAME WAY. Each straight edge of the outline is cut
        /// into pieces about a metre long and each cut is pushed in or out a hand. The push is worked out from the edge's
        /// two ends taken in a fixed order, so the two plates that share a crack get the same jagged line and still close
        /// on each other while the court is whole.
        /// </summary>
        private static List<Vector2> DanteBroken(Vector2[] ring)
        {
            var edge = new List<Vector2>(ring.Length * 5);
            var between = new List<Vector2>(8);
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                bool flip = a.x > b.x || (a.x == b.x && a.y > b.y);
                Vector2 from = flip ? b : a, to = flip ? a : b, along = to - from;
                float length = along.magnitude;
                int cuts = Mathf.Clamp(Mathf.RoundToInt(length / 1.2f), 1, 7);
                var normal = new Vector2(-along.y, along.x) / length;
                float reach = Mathf.Min(.2f, length * .07f);
                between.Clear();
                for (int k = 1; k < cuts; k++)
                {
                    float seed = from.x * 7.31f + from.y * 3.77f + to.x * 5.13f + to.y * 9.91f + k * 1.37f;
                    float u = (k + (DanteHash(seed + 4.1f) - .5f) * .5f) / cuts;
                    between.Add(from + along * u + normal * ((DanteHash(seed) - .5f) * 2f * reach));
                }
                if (flip) between.Reverse();
                edge.Add(a); edge.AddRange(between);
            }
            return edge;
        }

        /// <summary>
        /// A band between two outlines round the origin, each at its own height, every face its own (flat, cut rock); with
        /// no second outline, the flat top over the first (or, <paramref name="faceDown"/>, the flat underside).
        /// </summary>
        private static Mesh DanteShell(List<Vector2> a, float ya, List<Vector2> b, float yb, bool faceDown = false)
        {
            var vertices = new List<Vector3>(a.Count * 6); var triangles = new List<int>(a.Count * 6);
            void Face(Vector3 p, Vector3 q, Vector3 r, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(q - p, r - p), outward) < 0) { var keep = q; q = r; r = keep; }
                int n = vertices.Count; vertices.Add(p); vertices.Add(q); vertices.Add(r);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            for (int i = 0; i < a.Count; i++)
            {
                int k = (i + 1) % a.Count;
                Vector3 a0 = new Vector3(a[i].x, ya, a[i].y), a1 = new Vector3(a[k].x, ya, a[k].y);
                if (b == null) { Face(new Vector3(0, ya, 0), a0, a1, faceDown ? Vector3.down : Vector3.up); continue; }
                Vector3 b0 = new Vector3(b[i].x, yb, b[i].y), b1 = new Vector3(b[k].x, yb, b[k].y);
                var outward = (a0 + a1) * .5f; outward.y = 0; outward = outward.normalized + Vector3.up * .3f;
                Face(a0, a1, b1, outward); Face(a0, b1, b0, outward);
            }
            var mesh = new Mesh { name = "Basilio court plate" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A slab from an outline (x, z) round its middle: a top, one wall per edge and an underside, every face its own.</summary>
        private static Mesh DantePlateMesh(Vector2[] ring, Vector2 middle, float thick)
        {
            var vertices = new List<Vector3>(ring.Length * 9); var triangles = new List<int>(ring.Length * 9);
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var keep = b; b = c; c = keep; }
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 p = ring[i] - middle, q = ring[(i + 1) % ring.Length] - middle;
                Vector3 a = new Vector3(p.x, 0, p.y), b = new Vector3(q.x, 0, q.y), down = Vector3.down * thick;
                Face(Vector3.zero, a, b, Vector3.up);
                var outward = (a + b) * .5f;
                Face(a, b, b + down, outward); Face(a, b + down, a + down, outward);
                Face(down, a + down, b + down, Vector3.down);
            }
            var mesh = new Mesh { name = "Basilio court plate" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private int BasilioStone(string name, Mesh mesh, Color colour)
        {
            int index = AddSolid(name, mesh, colour);
            // Keep opaque lit facets, with a small local fill so the dusk stage
            // cannot turn the moving slabs into indistinguishable black cutouts.
            var material = _pieces[index].Renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * .45f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return index;
        }

        // ------------------------------------------------------------------ his plate, and its break-up

        /// <summary>
        /// The centre plate as its pieces (`DnBreakPiece`): each a whole slab with a top, an underside, dark walls where it
        /// is the plate's own edge and MOLTEN walls where it is a break, under a holder at its own middle. Shut, their tops
        /// are one flat face in one colour, which is why nothing is seen to change when they are let go.
        /// ⚠️ ITS TOP IS 7 cm OVER THE COURT, CLEAR OF EVERYTHING ELSE. The plate before this sat 3 cm up, in the very plane
        /// the sea's bright veins turn in, and from above the two fought for every pixel (the owner: "z fighting on the
        /// platform").
        /// </summary>
        private void BuildDanteBreak(Color stone)
        {
            _dnBreak = new Transform[DnBreakPiece.Length]; _dnBreakHome = new Vector3[DnBreakPiece.Length];
            var dark = stone * .5f; dark.a = 1;
            for (int i = 0; i < DnBreakPiece.Length; i++)
            {
                var outline = DnBreakPiece[i];
                var middle = Vector2.zero;
                foreach (var point in outline) middle += point;
                middle /= outline.Length;
                var holder = new GameObject("PlatePiece" + i).transform; holder.SetParent(_root.transform, false);
                _dnBreak[i] = holder; _dnBreakHome[i] = new Vector3(middle.x, 0, middle.y);
                DanteUnder(BasilioStone("PlatePieceTop" + i, DantePieceMesh(outline, middle, 0), stone), holder);
                DanteUnder(BasilioStone("PlatePieceRock" + i, DantePieceMesh(outline, middle, 1), dark), holder);
                var breaks = DantePieceMesh(outline, middle, 2);
                if (breaks.vertexCount > 0) DanteUnder(DanteLava("PlatePieceBreak" + i, breaks, new Color(1f, .5f, .1f, 1)), holder);
                holder.gameObject.SetActive(false);
            }
        }

        /// <summary>Is the edge from <paramref name="a"/> to <paramref name="b"/> part of the plate's own outline (and not a break through it)?</summary>
        private static bool DanteOnRim(Vector2 a, Vector2 b)
        {
            for (int i = 0; i < DnCore.Length; i++)
            {
                Vector2 from = DnCore[i], along = DnCore[(i + 1) % DnCore.Length] - from;
                float cross = Mathf.Abs(along.x * (a.y - from.y) - along.y * (a.x - from.x)) + Mathf.Abs(along.x * (b.y - from.y) - along.y * (b.x - from.x));
                if (cross / along.magnitude < .02f) return true;
            }
            return false;
        }

        /// <summary>One part of a piece round its middle: 0 its top, 1 its underside and the walls on the plate's own edge, 2 the walls that are breaks.</summary>
        private static Mesh DantePieceMesh(Vector2[] outline, Vector2 middle, int part)
        {
            var vertices = new List<Vector3>(outline.Length * 9); var triangles = new List<int>(outline.Length * 9);
            void Face(Vector3 p, Vector3 q, Vector3 r, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(q - p, r - p), outward) < 0) { var keep = q; q = r; r = keep; }
                int n = vertices.Count; vertices.Add(p); vertices.Add(q); vertices.Add(r);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            float half = DnBreakThick * .5f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                Vector3 top0 = new Vector3(a.x - middle.x, half, a.y - middle.y), top1 = new Vector3(b.x - middle.x, half, b.y - middle.y);
                Vector3 low0 = top0 + Vector3.down * DnBreakLooks, low1 = top1 + Vector3.down * DnBreakLooks;
                bool rim = DanteOnRim(a, b);
                if (part == 0) Face(new Vector3(0, half, 0), top0, top1, Vector3.up);
                if (part == 1) Face(new Vector3(0, half - DnBreakLooks, 0), low0, low1, Vector3.down);
                if ((part == 1 && rim) || (part == 2 && !rim))
                {
                    var outward = (top0 + top1) * .5f; outward.y = 0;
                    Face(top0, top1, low1, outward); Face(top0, low1, low0, outward);
                }
            }
            var mesh = new Mesh { name = "Basilio plate piece" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private void SampleDanteBreak(float t, float lift, bool broken, float light)
        {
            float since = t - DnCrumble;
            float frame = Mathf.Clamp(since * DnBreakRate, 0, DnBreakFrames - 1);
            int row = Mathf.Min((int)frame, DnBreakFrames - 2); float part = frame - row;
            for (int i = 0; i < _dnBreak.Length; i++)
            {
                var piece = _dnBreak[i];
                var at = _dnBreakHome[i] + Vector3.up * (lift + DnBreakTop - DnBreakThick * .5f);
                var turn = Quaternion.identity; var scale = Vector3.one; bool there = broken;
                if (since >= 0 && i < DnBreakSimulated)
                {
                    // The simulated fall, between the two sixtieths this moment lies in.
                    int from = (i * DnBreakFrames + row) * 9, to = from + 9;
                    Vector3 Read(int k) => new Vector3(DnBreakPath[k], DnBreakPath[k + 1], DnBreakPath[k + 2]);
                    at = Vector3.Lerp(Read(from), Read(to), part);
                    turn = Quaternion.Slerp(Quaternion.LookRotation(Read(from + 3), Read(from + 6)), Quaternion.LookRotation(Read(to + 3), Read(to + 6)), part);
                    there = at.y > -.15f && since * DnBreakRate < DnBreakFrames - 1;
                }
                else if (since >= 0)
                {
                    // The middle piece, under his feet: it is ground to nothing between his soles and the crown of its back.
                    float ground = Mathf.Clamp01(since / .3f);
                    scale = new Vector3(1 - ground, 1, 1 - ground); there = ground < .98f;
                }
                if (piece.gameObject.activeSelf != there) piece.gameObject.SetActive(there);
                piece.localPosition = at; piece.localRotation = turn; piece.localScale = scale;
            }
            for (int i = 0; i < _dnSplashes.Count; i++)
            {
                // Where it went in: light for a quarter of a second.
                int landed = DnBreakLanded[i];
                float age = landed < 0 ? -1 : since - landed / DnBreakRate;
                int k = (i * DnBreakFrames + Mathf.Max(0, landed)) * 9;
                var spot = new Vector3(DnBreakPath[k], .1f, DnBreakPath[k + 2]);
                PlaceGlow(_dnSplashes[i], spot + Vector3.up * .3f, Vector3.one * Mathf.Lerp(1.4f, 2.8f, Mathf.Clamp01(age / .25f)), Quaternion.identity,
                          age >= 0 ? Mathf.Clamp01(1 - age / .3f) * .9f * light : 0);
            }
        }

        // ------------------------------------------------------------------ the seams

        /// <summary>A point along crack <paramref name="crack"/> while the court is whole: 0 at the centre plate's corner, .5 at its kink, 1 at its far end.</summary>
        private static Vector2 DanteSeam(int crack, float u)
            => u < .5f ? Vector2.Lerp(DnCore[crack], DnKink[crack], u * 2) : Vector2.Lerp(DnKink[crack], DnFar[crack], (u - .5f) * 2);

        /// <summary>How far the middle of that crack has moved once the plates on either side of it stand <paramref name="open"/> off.</summary>
        private Vector2 DanteSeamDrift(int crack, float open)
        {
            int left = (crack + 5) % 6;
            return (_dnPlateHome[left + 1].normalized * DnShare[left] + _dnPlateHome[crack + 1].normalized * DnShare[crack]) * (open * .5f);
        }

        /// <summary>A ribbon along a line on the ground, u along it and v across it: what a seam's light is drawn on.</summary>
        private static Mesh DanteStrip(Vector2[] line, float half, bool closed)
        {
            int n = line.Length, count = closed ? n + 1 : n;
            var vertices = new Vector3[count * 2]; var uv = new Vector2[count * 2]; var triangles = new int[(count - 1) * 6];
            for (int i = 0; i < count; i++)
            {
                Vector2 p = line[i % n];
                Vector2 before = closed ? line[(i - 1 + n) % n] : line[Mathf.Max(0, i - 1)];
                Vector2 after = closed ? line[(i + 1) % n] : line[Mathf.Min(n - 1, i + 1)];
                Vector2 along = (after - before).normalized, across = new Vector2(-along.y, along.x) * half;
                vertices[i * 2] = new Vector3(p.x - across.x, 0, p.y - across.y); vertices[i * 2 + 1] = new Vector3(p.x + across.x, 0, p.y + across.y);
                float u = i / (float)(count - 1);
                uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
                if (i == count - 1) continue;
                int v = i * 2, k = i * 6;
                triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1; triangles[k + 3] = v + 2; triangles[k + 4] = v + 3; triangles[k + 5] = v + 1;
            }
            var mesh = new Mesh { name = "Basilio seam light" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildDanteSeams()
        {
            var light = new Color(1f, .45f, .08f, 1);
            for (int i = 0; i < 6; i++)
            {
                int index = AddGlow("SeamLight" + i, light, DanteStrip(new[] { DnCore[i], DnKink[i], DnFar[i] }, .55f, false), billboard: false, band: true, falloff: 1.5f, core: .5f);
                // No hard end where a crack meets the centre plate or runs out under the dust.
                if (_pieces[index].Renderer.sharedMaterial.HasProperty("_Tips")) _pieces[index].Renderer.sharedMaterial.SetFloat("_Tips", .5f);
                _dnCrackGlow.Add(index);
            }
            _dnCoreGlow = AddGlow("SeamLightCentre", light, DanteStrip(DnCore, .5f, true), billboard: false, band: true, falloff: 1.5f, core: .5f);

            // Crust adrift in the seams: each a dark chip with the sea showing bright round its foot.
            for (int i = 0; i < 12; i++)
            {
                var chip = VfxShapes.Prism(5, .5f, .72f, .3f, 0, 60 + i);
                _dnChips.Add(DanteLava("CrustGlow" + i, chip, new Color(1f, .8f, .3f, 1)));
                _dnChips.Add(BasilioStone("Crust" + i, chip, new Color(.17f, .14f, .13f, 1)));
            }
            // Embers: small tapered sparks of rock climbing off the seams. A shape, so it reads at any distance.
            for (int i = 0; i < 24; i++)
                _dnEmbers.Add(DanteLava("Ember" + i, VfxShapes.Prism(4, 1f, .12f), i % 3 == 0 ? new Color(1f, .82f, .32f, 1) : new Color(1f, .55f, .12f, 1)));
            // Tongues of the sea licking up through the cracks; the six at the centre plate's corners are the ones that spit when it rises.
            for (int i = 0; i < 8; i++)
            {
                // Two tones, as a flame is drawn: the tongue, and a paler hot heart low inside it.
                var tongue = VfxShapes.Tongue(5, .34f, .28f, .55f, .18f, 70 + i);
                _dnLicks.Add(DanteLava("LavaTongue" + i, tongue, new Color(1f, .42f, .06f, 1)));
                _dnLicks.Add(DanteLava("LavaTongueHeart" + i, tongue, new Color(1f, .82f, .3f, 1)));
            }

            _dnSeamGlow = Add("MoltenSeamGlow", VfxShapes.Fracture(6, 3, .09f, 21), new Color(1, .42f, .08f, .55f), .8f);
            _dnSeam = Add("MoltenSeam", VfxShapes.Fracture(6, 3, .045f, 21), new Color(1, .62f, .18f, .95f), 1f);
        }

        private void SampleDanteSeams(float t, float haze, float open, bool broken, float light)
        {
            // The sea under everything, breathing: its veins swell and turn, and its heat lies over the whole court.
            float breath = _reducedEffects ? .9f : .86f + .14f * Mathf.Sin(t * 5.2f);
            float surge = Ease(DnRiseFrom, DnRiseFrom + .35f, t) * (1 - Ease(4.6f, DnRisen + .1f, t));
            Place(_dnSea, new Vector3(0, .012f, 0), new Vector3(11.3f, 1, 11.3f), Quaternion.identity, broken ? 1 : 0);
            float swell = 1 + .03f * Mathf.Sin(t * 1.7f);
            Place(_dnSeaVeinA, new Vector3(0, .02f, 0), new Vector3(11f * swell, 1, 11f * swell), Quaternion.Euler(0, t * 5f, 0), broken ? 1 : 0);
            Place(_dnSeaVeinB, new Vector3(0, .026f, 0), new Vector3(9.5f / swell, 1, 9.5f / swell), Quaternion.Euler(0, 40 - t * 7f, 0), broken ? 1 : 0);
            PlaceGlow(_dnSeaHeat, new Vector3(0, .5f, 0), new Vector3(23, 23, 1), Quaternion.Euler(90, 0, 0), broken ? (.07f + .12f * surge) * breath * light : 0);
            Tint(_dnDustWarm, haze * Ease(DnBreak, 2.2f, t) * (.75f + .25f * breath)); Tint(_dnDustWarmHigh, haze * Ease(DnBreak, 2.2f, t) * (.75f + .25f * breath));

            // Light up through every seam: it runs out along each crack as the court breaks, flares, and then stays.
            float flare = 1 + 1.2f * (1 - Ease(DnBreak, DnBreak + .5f, t));
            for (int i = 0; i < 6; i++)
            {
                var drift = DanteSeamDrift(i, open);
                float lit = Ease(DnBreak + i * .03f, DnBreak + .22f + i * .03f, t);
                PlaceGlow(_dnCrackGlow[i], new Vector3(drift.x, .27f, drift.y), Vector3.one, Quaternion.identity, lit * (.42f + .2f * surge) * flare * breath * light);
            }
            float ringOut = 1 + open * .5f / 1.5f;
            PlaceGlow(_dnCoreGlow, new Vector3(0, .27f, 0), new Vector3(ringOut, 1, ringOut), Quaternion.identity,
                      (broken ? (.32f + .4f * surge) * flare * breath * light : 0));

            for (int i = 0; i < 12; i++)
            {
                int crack = i % 6;
                float u = .26f + .42f * (i / 6) + .12f * DanteHash(i * 2.3f) + .03f * Mathf.Sin(t * .4f + i);
                var at = DanteSeam(crack, u) + DanteSeamDrift(crack, open);
                float size = Mathf.Min(1f, open * .38f) * (.6f + .5f * DanteHash(i * 7.7f));
                float bob = Mathf.Sin(t * 1.6f + i * 2.1f) * .02f;
                var turn = Quaternion.Euler(0, i * 53 + t * (i % 2 == 0 ? 9 : -7), 0);
                bool on = broken && size > .05f;
                Place(_dnChips[i * 2], new Vector3(at.x, .034f, at.y), new Vector3(size * 1.3f, .04f, size * 1.3f), turn, on ? 1 : 0);
                Place(_dnChips[i * 2 + 1], new Vector3(at.x, .05f + bob, at.y), new Vector3(size, .24f, size), turn, on ? 1 : 0);
            }

            for (int i = 0; i < _dnEmbers.Count; i++)
            {
                int crack = i % 6;
                float h = DanteHash(i * 3.1f), age = Mathf.Repeat(t * (.32f + .25f * DanteHash(i * 5.7f)) + DanteHash(i * 9.3f), 1f);
                var from = DanteSeam(crack, .06f + .8f * h) + DanteSeamDrift(crack, open);
                var at = new Vector3(from.x + Mathf.Sin(t * 1.3f + i) * .14f * age, .25f + age * (1.3f + 1.5f * h), from.y + Mathf.Cos(t * 1.1f + i * 2) * .14f * age);
                // Before the court breaks only a few are in the air, and only as his stage arrives.
                float there = broken ? 1 : i % 3 == 0 ? haze : 0;
                float size = Mathf.Sqrt(Mathf.Sin(age * Mathf.PI)) * (1.25f - age * .5f) * there;
                Place(_dnEmbers[i], at, new Vector3(.075f, .26f, .075f) * size, Quaternion.Euler(Mathf.Sin(t * 2 + i) * 14, i * 37 + t * 90, 0), size > .05f ? 1 : 0);
            }

            for (int i = 0; i < _dnLicks.Count / 2; i++)
            {
                int crack = i < 6 ? i : i == 6 ? 1 : 4;
                float h = DanteHash(i * 4.9f), u = i < 6 ? .05f + .08f * h : .55f + .1f * h;
                var from = DanteSeam(crack, u) + DanteSeamDrift(crack, open);
                // Each on its own clock: up quickly, hang, sink, and stay under a while.
                float cycle = Mathf.Repeat((t - DnBreak) / (1.5f + h * .9f) + DanteHash(i * 1.7f), 1f);
                float up = cycle < .18f ? DanteOut(cycle / .18f) : cycle < .5f ? 1 - .15f * (cycle - .18f) / .32f : cycle < .75f ? .85f * (1 - (cycle - .5f) / .25f) : 0;
                float size = Mathf.Clamp(open * 1.3f, .22f, .8f);
                // ⚠️ AS ITS BACK COMES UP THE SEA IS FORCED OUT ROUND IT: the six at the centre spit together, far higher, once.
                float spit = i < 6 ? Mathf.Sin(Mathf.Clamp01((t - (DnRiseFrom + .05f + i * .05f)) / .8f) * Mathf.PI) : 0;
                float high = Mathf.Max(up * size * 1.5f, spit * 2.2f), wide = size * 1.3f + spit * .7f;
                var turn = Quaternion.Euler(0, i * 67 + Mathf.Floor((t - DnBreak) / (1.5f + h * .9f)) * 40, 0);
                Place(_dnLicks[i * 2], new Vector3(from.x, .06f, from.y), new Vector3(wide, high, wide), turn, broken && high > .04f ? 1 : 0);
                Place(_dnLicks[i * 2 + 1], new Vector3(from.x, .07f, from.y) + turn * new Vector3(.02f * wide, 0, 0), new Vector3(wide * .55f, high * .58f, wide * .55f), turn * Quaternion.Euler(0, 60, 0),
                      broken && high > .04f ? 1 : 0);
            }
        }

        // ------------------------------------------------------------------ the punch

        private void BuildDantePunch()
        {
            var ember = new Color(1f, .5f, .1f, 1);
            _dnFistCharge = AddGlow("FistHeat", ember, falloff: 2f, core: .5f, lift: .25f);
            _dnPunchFlash = AddGlow("PunchFlash", ember, falloff: 1.7f, core: .4f);
            // The court heaved up round his fist: a ring of slabs, thick ones, that stand a moment and sink back.
            _dnPunchRubble = BasilioStone("PunchRubble", VfxShapes.Upheaval(8, .06f, .5f, 31, .09f), new Color(.24f, .21f, .19f, 1));
            _dnPunchDust = Add("PunchDustRing", VfxShapes.Collar(22, .3f, .8f, .22f, 0, 33), new Color(.62f, .52f, .4f, .6f), .05f, plain: true);
            for (int i = 0; i < 6; i++)
                _dnPuffs.Add(DantePuff("PunchDust" + i, VfxShapes.NovaShell(5, 8, .12f, 40 + i), new Color(.6f, .51f, .42f, 1)));
            for (int i = 0; i < 9; i++)
                _dnShards.Add(BasilioStone("PunchShard" + i, VfxShapes.Spire(4, .3f, .4f, 50 + i), new Color(.27f, .23f, .21f, 1)));
        }

        private void SampleDantePunch(float t, float light)
        {
            var fist = new Vector3(.15f, .05f, .85f);
            float since = t - DnPunch;

            // Heat gathers in the fist as it goes up, and is spent in the court.
            var hand = _heldItem != null ? FreePalm : RightPalm;
            float charge = Ease(.45f, 1.0f, t) * (1 - Ease(DnPunch - .08f, DnPunch + .02f, t));
            PlaceGlow(_dnFistCharge, hand, Vector3.one * Mathf.Lerp(.3f, .65f, charge), Quaternion.identity, charge * .7f * light);
            PlaceGlow(_dnPunchFlash, fist + Vector3.up * .1f, Vector3.one * 1.9f, Quaternion.identity, since >= 0 ? Mathf.Clamp01(1 - since / .24f) * .7f * light : 0);

            // Seams split out from his fist before the plates take over.
            float crack = Ease(DnPunch - .02f, DnPunch + .28f, t), crackOff = 1 - Ease(1.45f, 1.7f, t);
            var crackScale = Vector3.one * Mathf.Lerp(.3f, 3.4f, crack);
            Place(_dnSeamGlow, fist + Vector3.down * .005f + Vector3.up * .12f, crackScale * 1.12f, Quaternion.identity, crack * crackOff * .8f);
            Place(_dnSeam, fist + Vector3.up * .125f, crackScale, Quaternion.identity, crack * crackOff);

            // The ring of court round his fist: up fast and a little too far, held, then back down as the plates settle.
            float heave = Ease(DnPunch, DnPunch + .1f, t), back = Ease(1.5f, 1.95f, t);
            Place(_dnPunchRubble, new Vector3(fist.x, t >= DnBreak ? .12f : -.02f, fist.z), new Vector3(Mathf.Lerp(.4f, 1.05f, heave), heave * (1 + .3f * Mathf.Sin(heave * Mathf.PI)) * (1 - back) * .5f, Mathf.Lerp(.4f, 1.05f, heave)),
                  Quaternion.identity, heave > .05f && back < .98f ? 1 : 0);
            {
                float age = Mathf.Clamp01(since / .75f), wide = Mathf.Lerp(.8f, 6.5f, DanteOut(age));
                Place(_dnPunchDust, new Vector3(fist.x, .16f, fist.z), new Vector3(wide, Mathf.Lerp(1f, .35f, age), wide), Quaternion.identity, since >= 0 ? (1 - age) * .85f : 0);
            }
            for (int i = 0; i < _dnPuffs.Count; i++)
            {
                float age = since / .95f, angle = (i * 60 + 25) * Mathf.Deg2Rad, out_ = .5f + DanteOut(age) * 1.9f;
                var at = fist + new Vector3(Mathf.Cos(angle) * out_, .25f + age * .7f, Mathf.Sin(angle) * out_);
                float size = (.3f + .35f * Mathf.Clamp01(age)) * DantePop(age);
                Place(_dnPuffs[i], at, Vector3.one * size, Quaternion.Euler(age * 120, i * 40, age * 60), size > .02f ? 1 : 0);
            }
            for (int i = 0; i < _dnShards.Count; i++)
            {
                // Thrown: out and up from the fist, turning over, and gone where each comes back down.
                float h = DanteHash(i * 6.1f), g = DanteHash(i * 2.9f), angle = (i * 40 + 13) * Mathf.Deg2Rad;
                float out_ = .4f + (1.4f + 2.2f * h) * since, high = .15f + (3.6f + 2.4f * g) * since - 6f * since * since;
                var at = fist + new Vector3(Mathf.Cos(angle) * out_, high, Mathf.Sin(angle) * out_);
                float size = (.05f + .05f * g) * Mathf.Clamp01(since / .07f);
                Place(_dnShards[i], at, new Vector3(size, size * 2.6f, size), Quaternion.Euler(i * 70 + since * 420 * (h - .5f) * 2, i * 50, i * 110 + since * 380),
                      since >= 0 && high > .12f ? 1 : 0);
            }
        }

        // ------------------------------------------------------------------ the longhorn

        /// <summary>
        /// ⚠️ THE LONGHORN IS MODELLED (2026-10-10; it was about fifteen stock prisms while the staging was judged):
        /// `Models/UltimateProps/longhorn`, built in `tools/build_ultimate_props_dante.py` the way his hand companion and
        /// Paete's plants are (round forms, a painted atlas, a drawn face) and dressed as the cast is, with a heavier ink
        /// line than a person wears because it is seven metres across. Its right arm (`arm-r`), its
        /// `head` and its `jaw` are joints; the arm hangs from the same shoulder the blocks' arm hung from, so the swing
        /// below is the swing that was agreed. Its eyes, its mouth and its raised fist carry a light each.
        /// A missing model is a warning in the log and empty joints here: the cutscene still runs, without it.
        /// </summary>
        private void BuildDanteTitan()
        {
            var model = UltimateProp.Spawn("longhorn", _root.transform, UltimateProp.EarthPalette, ToonSkin.PersonOutlineWidth * 3.5f);
            _dnTitanModelled = model != null;
            if (model != null)
            {
                _dnTitan = model.transform; model.name = "BasaltTitan";
                _dnTitanArm = PaeteProp.Find(model, "arm-r"); _dnTitanHead = PaeteProp.Find(model, "head"); _dnTitanJaw = PaeteProp.Find(model, "jaw");
            }
            else { _dnTitan = new GameObject("BasaltTitan").transform; _dnTitan.SetParent(_root.transform, false); }
            if (_dnTitanArm == null)
            {
                _dnTitanArm = new GameObject("TitanArmRight").transform; _dnTitanArm.SetParent(_dnTitan, false);
                _dnTitanArm.localPosition = new Vector3(2.75f, 2.5f, .6f);
            }
            if (_dnTitanHead == null)
            {
                _dnTitanHead = new GameObject("TitanHead").transform; _dnTitanHead.SetParent(_dnTitan, false);
                _dnTitanHead.localPosition = new Vector3(0, 1.0f, 1.9f);
            }
            var ember = new Color(1, .5f, .1f, .95f);
            _dnEyeLeft = AddGlow("TitanEyeLeft", ember, falloff: 2.2f, core: .5f, lift: .12f);
            _dnEyeRight = AddGlow("TitanEyeRight", ember, falloff: 2.2f, core: .5f, lift: .12f);
            _dnMouthGlow = AddGlow("TitanMouthHeat", new Color(1f, .4f, .08f, 1), falloff: 1.8f, core: .4f, lift: .5f);
            _pieces[_dnEyeLeft].Transform.SetParent(_dnTitanHead, false); _pieces[_dnEyeRight].Transform.SetParent(_dnTitanHead, false);
            _pieces[_dnMouthGlow].Transform.SetParent(_dnTitanHead, false);
            _dnTitanFistGlow = AddGlow("TitanFistHeat", new Color(1f, .45f, .08f, 1), falloff: 1.6f, core: .35f, lift: 1.1f);
            _pieces[_dnTitanFistGlow].Transform.SetParent(_dnTitanArm, false);
            for (int i = 0; i < DnSeamLight.GetLength(0); i++)
            {
                var joint = DnSeamLight[i, 0] < .5f ? _dnTitan : DnSeamLight[i, 0] < 1.5f ? _dnTitanHead : _dnTitanArm;
                // A hot heart and a wide soft skirt: the heart is the seam, the skirt is its light on the rock beside it.
                int light = AddGlow("TitanSeamLight" + i, new Color(1f, .46f, .09f, 1), falloff: 1.5f, core: .9f, lift: .45f);
                _pieces[light].Transform.SetParent(joint, false);
                _dnSeamLights.Add(light);
                _dnVents.Add(DanteLava("TitanVent" + i, VfxShapes.Prism(4, 1f, .12f), new Color(1f, .74f, .26f, 1)));
            }
            // Ash and grit shaken off it: small dark chips that fall, and nothing more than that.
            for (int i = 0; i < 14; i++)
                _dnGrit.Add(BasilioStone("TitanGrit" + i, VfxShapes.Prism(5, .6f, .7f, .35f, 0, 170 + i), i % 3 == 0 ? new Color(.5f, .45f, .4f, 1) : new Color(.2f, .17f, .16f, 1)));

        }

        private void BuildDanteRise()
        {
            // The court it shoulders aside stays round it as a collar of leaning slabs.
            _dnRubble = BasilioStone("RiseRubble", VfxShapes.Upheaval(11, .1f, .5f, 47, .1f), new Color(.25f, .22f, .2f, 1));
            for (int i = 0; i < 10; i++)
                _dnRocks.Add(BasilioStone("ShedRock" + i, VfxShapes.Spire(5, .5f, .45f, 80 + i), new Color(.28f, .24f, .22f, 1)));
            for (int i = 0; i < DnRiseDustAt.Length; i++)
                _dnRiseDust.Add(DantePuff("RiseDust" + i, VfxShapes.NovaShell(5, 8, .12f, 90 + i), new Color(.56f, .47f, .39f, 1)));
            _dnUnderGlow = AddGlow("RiseUnderLight", new Color(1f, .4f, .06f, 1), billboard: false, falloff: 1.5f, core: .3f);
            // A glow behind it, so the dark of it is cut out of the dark of the dust.
            _dnBackGlow = AddGlow("RiseBackLight", new Color(1f, .38f, .06f, 1), falloff: 1.4f, core: .1f);
        }

        private void BuildDanteStomp()
        {
            var ember = new Color(1f, .5f, .1f, 1);
            for (int i = 0; i < 8; i++)
                _dnGather.Add(DanteLava("GatheredEmber" + i, VfxShapes.Prism(4, 1f, .12f), new Color(1f, .7f, .22f, 1)));
            _dnPalmGlow = AddGlow("LoadedFists", ember, falloff: 2f, core: .5f, lift: .3f);
            _dnStompFlash = AddGlow("StompFlash", ember, falloff: 1.6f, core: .4f, lift: .6f);
            _dnStompRing = Add("StompDustRing", VfxShapes.Collar(24, .35f, .8f, .22f, 0, 35), new Color(.62f, .52f, .4f, .65f), .05f, plain: true);
            _dnStompRubble = BasilioStone("StompRubble", VfxShapes.Upheaval(9, .06f, .6f, 53, .1f), new Color(.24f, .21f, .19f, 1));
            _dnStompCore = AddGlow("StompFlashCore", new Color(1f, .78f, .3f, 1), falloff: 2.4f, core: .8f, lift: .8f);
            for (int i = 0; i < 4; i++)
                _dnSlabs.Add(BasilioStone("StompSlab" + i, DantePlateMesh(DnSlab[i], Vector2.zero, .32f), new Color(.32f, .28f, .25f, 1)));
            for (int i = 0; i < 8; i++)
                _dnStompShards.Add(BasilioStone("StompShard" + i, VfxShapes.Spire(4, .3f, .4f, 150 + i), new Color(.27f, .23f, .21f, 1)));
            var strip = new[] { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) };
            _dnBlastGlow = Add("BlastSeam", DantePlateMesh(strip, Vector2.zero, .02f), new Color(1, .5f, .1f, .9f), 1f);
        }

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the punch, the long grind of the rise, and the stomp it ends on.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnPunch, .05f, .3f) + DanteKick(t, DnStomp, .12f, .45f)
                       + .016f * Ease(DnRiseFrom, DnRiseFrom + .3f, t) * (1 - Ease(DnRisen - .1f, DnRisen + .2f, t));
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        private void SampleDante(float t)
        {
            // Whole to the last frame: nothing of his stage leaves before the hand-back (the header says why).
            float haze = Ease(.3f, 1.0f, t);
            Tint(_dnDustLow, haze); Tint(_dnDustHigh, haze);
            // Reduced effects keeps every shape and halves the light.
            float light = _reducedEffects ? .5f : 1f;

            float lift = LiftAt(t);
            bool broken = t >= DnBreak;
            // How far the plates stand off: a hairline as the cracks run, adrift on the sea, then shouldered aside by what rises.
            float adrift = Ease(DnBreak, 1.5f, t) * .16f + Ease(1.5f, 3.3f, t) * .72f;
            float shoved = Ease(DnRiseFrom, DnRiseFrom + .95f, t) * 2.3f;
            float open = adrift + shoved;
            float landed = t - DnStomp;

            SampleDanteHorizon(t, haze);
            SampleDanteSeams(t, haze, open, broken, light);
            SampleDantePunch(t, light);

            for (int i = 0; i < _dnPlates.Count; i++)
            {
                var plate = _dnPlates[i]; var home = _dnPlateHome[i];
                // His own plate is the break-apart pieces, posed by `SampleDanteBreak`.
                if (i == 0) continue;
                plate.gameObject.SetActive(broken);
                int k = i - 1;
                Vector2 away = home.normalized * open * DnShare[k];
                float loose = Mathf.Clamp01(open / .9f), heel = Mathf.Clamp01(shoved / 2.3f);
                float bob = Mathf.Sin(t * 1.4f + k * 1.7f) * .05f * loose;
                // The stomp is felt through the court: every plate jumps, the near ones most.
                float near = 1 - Mathf.Clamp01((new Vector3(home.x + away.x, 0, home.y + away.y) - DnStompAt).magnitude / 14f);
                float jolt = landed >= 0 ? Mathf.Sin(Mathf.Clamp01(landed / .12f) * Mathf.PI * .5f) * .32f * near : 0;
                // ⚠️ The real court is solid at 0 and the sea lies on it, so a plate may never dip: it floats a hand above the
                // sea and its rock is kept small enough that its lowest corner (5 m out) stays over it (film d1 tilted them
                // 5 degrees and half of every plate went under the sea).
                plate.localPosition = new Vector3(home.x + away.x, DnOuterFloat + bob + heel * .42f + jolt, home.y + away.y);
                // Adrift it rocks a little; shoved aside, its near edge is lifted by the shoulders.
                var rock = DnTilt[k] * .24f * loose * (.6f + .4f * Mathf.Sin(t * 1.1f + k));
                var heeled = Quaternion.AngleAxis(-4f * heel, new Vector3(-home.y, 0, home.x).normalized);
                plate.localRotation = heeled * Quaternion.Euler(rock.x, rock.y * .4f, rock.z);
            }

            // ------------------------------------------------------------- his plate breaks up
            // Light comes up through it as the back takes its weight, it flashes, and its pieces are let go (`SampleDanteBreak`).
            {
                bool crumbled = t >= DnCrumble;
                float strain = Ease(DnRiseFrom, DnCrumble, t), spent = Mathf.Clamp01((t - DnCrumble) / .3f);
                PlaceGlow(_dnCrumbleLight, new Vector3(0, lift + .2f, .1f), new Vector3(4.6f, 4.6f, 1), Quaternion.Euler(90, 0, 0),
                          t < DnRiseFrom ? 0 : (crumbled ? (1 - spent) * 1.0f : strain * .5f) * light);
SampleDanteBreak(t, lift, broken, light);
                                for (int i = 0; i < _dnCrumbleDust.Count; i++)
                {
                    float age = (t - DnCrumble - i * .05f) / .8f, angle = (i * 120 + 40) * Mathf.Deg2Rad;
                    float size = (.4f + .4f * Mathf.Clamp01(age)) * DantePop(age);
                    Place(_dnCrumbleDust[i], new Vector3(Mathf.Sin(angle) * (1.2f + 1.3f * DanteOut(age)), lift + .2f + Mathf.Clamp01(age) * .6f, Mathf.Cos(angle) * (1.2f + 1.3f * DanteOut(age))),
                          Vector3.one * size, Quaternion.Euler(age * 90, i * 50, 0), size > .02f ? 1 : 0);
                }
            }

            // ------------------------------------------------------------- the rise
            // The thing under the court: it comes up exactly as far as he is lifted, shuddering.
            bool risen = lift > .01f;
            float grinding = 1 - Ease(DnRisen - .1f, DnRisen + .2f, t);
            float raised = Ease(5.5f, 6.3f, t) * (1 - Ease(DnStomp - .1f, DnStomp, t));
            _dnTitan.gameObject.SetActive(risen);
            if (risen)
            {
                float grind = _reducedEffects ? 0 : Mathf.Sin(t * 41f) * .03f * grinding;
                _dnTitan.localPosition = new Vector3(grind, lift - DnTitanTop, 0);
                // Its right arm: planted, raised overhead through his load, held trembling, brought down on the stomp.
                // ⚠️ A FIST THAT HEAVY DOES NOT GLIDE. It leans on the fist first, heaves it up past where it will hold and
                // lets it sink back; before the blow it draws back further still; and it comes down faster all the way,
                // with nothing easing it at the bottom, because the court is what stops it. The times are the agreed ones:
                // up from 5.5, held by 6.3, landing at the stomp.
                float tremble = _reducedEffects ? 0 : Mathf.Sin(t * 33f) * 1.6f * raised;
                float lean = Mathf.Sin(Mathf.Clamp01((t - 5.5f) / .14f) * Mathf.PI) * 7f;
                float heave = Ease(5.58f, 6.18f, t), past = Mathf.Sin(Mathf.Clamp01((t - 6.1f) / .32f) * Mathf.PI) * 9f;
                float drawn = Ease(DnStomp - .24f, DnStomp - .1f, t) * 12f;
                float fall = Mathf.Clamp01((t - (DnStomp - .1f)) / .1f); fall *= fall;
                float swing = Mathf.Lerp(Mathf.Lerp(-40f, -152f, heave) + lean - past - drawn, -40f, fall);
                _dnTitanArm.localRotation = Quaternion.Euler(swing + tremble * (1 - fall), 0, Mathf.Lerp(0, -10f, heave) * (1 - fall));
                // Its head sways as it forces its way up, lifts to roar while the fist is high, and drops with the fist.
                float roar = Ease(5.55f, 6.05f, t) * (1 - Ease(DnStomp - .08f, DnStomp, t));
                float thrown = Mathf.Sin(Mathf.Clamp01((t - 5.95f) / .35f) * Mathf.PI) * 4f;
                float nod = Mathf.Clamp01((t - (DnStomp - .08f)) / .08f); nod *= nod;
                _dnTitanHead.localRotation = Quaternion.Euler(-9f * roar - thrown + 10f * nod + Mathf.Sin(t * 5.3f) * 1.5f * grinding, Mathf.Sin(t * 3.1f) * 4f * grinding, Mathf.Sin(t * 4.3f) * 3f * grinding);
                if (_dnTitanJaw != null) _dnTitanJaw.localRotation = Quaternion.Euler(6f * grinding + 26f * roar + tremble * .6f, 0, 0);

                bool shown = _dnTitanModelled;
                float eyes = Ease(4.3f, 4.8f, t) * (.8f + .5f * Flash(t, DnStomp, .2f)) * (1 + .5f * roar) * light;
                PlaceGlow(_dnEyeLeft, new Vector3(-.5f, .3f, 1.47f), new Vector3(.75f, .32f, 1), Quaternion.identity, shown ? eyes : 0);
                PlaceGlow(_dnEyeRight, new Vector3(.5f, .3f, 1.47f), new Vector3(.75f, .32f, 1), Quaternion.identity, shown ? eyes : 0);
                PlaceGlow(_dnMouthGlow, new Vector3(0, -.5f, 1.55f), new Vector3(1.7f, .8f, 1), Quaternion.identity, shown ? roar * .8f * light : 0);
                // The fist it is about to bring down runs hot, hotter the longer it is held.
                float held = raised * (.55f + .35f * Ease(6.1f, DnStomp, t)) * (_reducedEffects ? 1 : 1 + .12f * Mathf.Sin(t * 17f));
                PlaceGlow(_dnTitanFistGlow, new Vector3(0, -3.3f, 0), Vector3.one * Mathf.Lerp(2.4f, 3.6f, raised), Quaternion.identity, shown ? held * light : 0);

                // ⚠️ THE SEAMS BEAT, AND THE BEAT BUILDS TO THE STOMP. Dull as it forces its way up, brighter once it is out,
                // then quicker and hotter the whole time the fist is held, and all of them at once on the blow.
                float build = Ease(5.5f, DnStomp, t), awake = Ease(DnRiseFrom + .3f, DnRisen, t);
                float beat = _reducedEffects ? .85f : .75f + .25f * Mathf.Sin(t * (5f + 13f * build));
                float burn = (.22f + .2f * awake + .5f * build) * beat + (landed >= 0 ? .7f : 0);
                // How much it is moving: the grind of the rise, the swing of the arm, the blow.
                float moving = Mathf.Max(grinding * Ease(DnRiseFrom, DnRiseFrom + .3f, t), Mathf.Max(Mathf.Sin(heave * Mathf.PI), fall > 0 ? 1 : 0));
                for (int i = 0; i < _dnSeamLights.Count; i++)
                {
                    var at = new Vector3(DnSeamLight[i, 1], DnSeamLight[i, 2], DnSeamLight[i, 3]);
                    bool onArm = DnSeamLight[i, 0] > 1.5f;
                    float size = DnSeamLight[i, 4] * (1 + .12f * Mathf.Sin(t * 7f + i * 1.9f));
                    PlaceGlow(_dnSeamLights[i], at, Vector3.one * size, Quaternion.identity, shown ? burn * (onArm ? .6f + .6f * raised : 1) * light : 0);
                    // Embers vent from the same seam while it moves: out along the seam's own side of the beast, and up.
                    var joint = DnSeamLight[i, 0] < .5f ? _dnTitan : DnSeamLight[i, 0] < 1.5f ? _dnTitanHead : _dnTitanArm;
                    var from = _root.transform.InverseTransformPoint(joint.TransformPoint(at));
                    float age = Mathf.Repeat(t * (1.1f + .5f * DanteHash(i * 2.7f)) + DanteHash(i * 6.1f), 1f);
                    var away = new Vector3(at.x < -.01f ? -.7f : at.x > .01f ? .7f : 0, 0, .5f) * age;
                    float spark = Mathf.Sqrt(Mathf.Sin(age * Mathf.PI)) * moving;
                    Place(_dnVents[i], from + away + Vector3.up * (age * (1.2f + DanteHash(i * 4.3f))), new Vector3(.07f, .28f, .07f) * spark, Quaternion.Euler(0, i * 47 + t * 120, away.x * 25),
                          shown && spark > .08f ? 1 : 0);
                }
                // Grit off its shoulders as it climbs, and off the arm as that goes up and comes down. Each falls from where
                // the joint is NOW: it is shaken loose, it is not thrown.
                for (int i = 0; i < _dnGrit.Count; i++)
                {
                    bool offArm = i % 2 == 1;
                    float h = DanteHash(i * 3.3f), age = Mathf.Repeat(t * (1.3f + .6f * h) + DanteHash(i * 7.9f), 1f);
                    var seat = offArm ? _dnTitanArm.TransformPoint(new Vector3((h - .5f) * .9f, -1.2f - 2.2f * DanteHash(i * 5.1f), (DanteHash(i * 1.3f) - .5f) * .9f))
                                      : _dnTitan.TransformPoint(new Vector3((i % 4 < 2 ? -1 : 1) * (2.0f + h * 1.3f), 2.9f, .2f + DanteHash(i * 1.3f) * 1.2f));
                    var at = _root.transform.InverseTransformPoint(seat) + new Vector3((h - .5f) * .8f * age, -4.5f * age * age, .3f * age);
                    float loose = offArm ? Mathf.Max(Mathf.Sin(heave * Mathf.PI), fall > 0 && fall < 1 ? 1 : 0) : grinding * Ease(DnRiseFrom + .2f, DnRiseFrom + .6f, t);
                    float size = (.05f + .06f * h) * loose * (1 - age * .5f);
                    Place(_dnGrit[i], at, new Vector3(size, size * .6f, size), Quaternion.Euler(age * 300 + i * 40, i * 70, age * 220), shown && size > .02f && at.y > .1f ? 1 : 0);
                }
            }
            else
            {
                PlaceGlow(_dnEyeLeft, Vector3.zero, Vector3.one, Quaternion.identity, 0); PlaceGlow(_dnEyeRight, Vector3.zero, Vector3.one, Quaternion.identity, 0);
                PlaceGlow(_dnMouthGlow, Vector3.zero, Vector3.one, Quaternion.identity, 0); PlaceGlow(_dnTitanFistGlow, Vector3.zero, Vector3.one, Quaternion.identity, 0);
                foreach (int index in _dnSeamLights) PlaceGlow(index, Vector3.zero, Vector3.one, Quaternion.identity, 0);
                foreach (int index in _dnVents) Tint(index, 0);
                foreach (int index in _dnGrit) Tint(index, 0);
            }

            // What comes up with it. The collar of court it shoulders aside: up as its back breaks through, and it stays.
            {
                float up = Ease(DnRiseFrom + .05f, DnRiseFrom + .75f, t), wide = Mathf.Lerp(3.2f, 6.0f, up);
                Place(_dnRubble, new Vector3(0, .06f, .35f), new Vector3(wide, .9f * up * (1 + .22f * Mathf.Sin(up * Mathf.PI)), wide), Quaternion.Euler(0, 20, 0), up > .01f ? 1 : 0);
                // Lit from under and from behind while the sea is open round it.
                float under = Ease(DnRiseFrom, DnRiseFrom + .4f, t) * (.5f - .22f * Ease(DnRisen - .3f, DnRisen + .3f, t));
                PlaceGlow(_dnUnderGlow, new Vector3(0, .7f, .3f), new Vector3(13, 13, 1), Quaternion.Euler(90, 0, 0), under * light);
                PlaceGlow(_dnBackGlow, new Vector3(0, Mathf.Max(.5f, lift * .6f), -3.4f), Vector3.one * Mathf.Lerp(5f, 10.5f, Mathf.Clamp01(lift / DnTitanTop)), Quaternion.identity,
                          Ease(DnRiseFrom + .2f, DnRiseFrom + 1f, t) * .34f * light);
            }
            for (int i = 0; i < _dnRocks.Count; i++)
            {
                // Rock shed off its back as it climbs: each lets go at its own moment from where the back then is, and falls clear.
                float start = DnRiseFrom + .15f + i * .14f, since = t - start, h = DanteHash(i * 3.7f), g = DanteHash(i * 8.3f);
                float angle = (i * 36 + 10) * Mathf.Deg2Rad, out_ = 2.3f + (2.2f + 1.5f * h) * since;
                float high = LiftAt(start) - .25f + (2.2f + 2f * g) * since - 5f * since * since;
                float size = .09f + .1f * g;
                Place(_dnRocks[i], new Vector3(Mathf.Cos(angle) * out_, high, Mathf.Sin(angle) * out_), new Vector3(size, size * 1.7f, size),
                      Quaternion.Euler(since * 260 * (h - .5f) * 2, i * 70, since * 210), since >= 0 && high > .15f ? 1 : 0);
            }
            for (int i = 0; i < _dnRiseDust.Count; i++)
            {
                float age = (t - (DnRiseFrom + .1f + i * .09f)) / 1.5f, compass = DnRiseDustAt[i] * Mathf.Deg2Rad, out_ = 3.3f + 1.3f * DanteOut(age);
                float size = (.55f + .6f * Mathf.Clamp01(age)) * DantePop(age);
                Place(_dnRiseDust[i], new Vector3(Mathf.Sin(compass) * out_, .4f + Mathf.Clamp01(age) * 1.4f, Mathf.Cos(compass) * out_), Vector3.one * size,
                      Quaternion.Euler(age * 90, i * 50, age * 40), size > .02f ? 1 : 0);
            }

            // ------------------------------------------------------------- the held fist, and the stomp
            // Embers are drawn in to its fist while it is raised, and his own two fists run hot as he loads them.
            var knuckles = _root.transform.InverseTransformPoint(_dnTitanArm.TransformPoint(new Vector3(0, -3.3f, 0)));
            for (int i = 0; i < _dnGather.Count; i++)
            {
                float age = Mathf.Repeat(t * 1.5f + i / 8f, 1f);
                var off = Quaternion.Euler(i * 47, i * 83, 0) * Vector3.forward * ((1 - age) * 2.6f);
                float size = raised * (.5f + age) * Mathf.Sin(age * Mathf.PI);
                Place(_dnGather[i], knuckles + off, new Vector3(.07f, .3f, .07f) * size, Quaternion.LookRotation(off.sqrMagnitude > .0001f ? off.normalized : Vector3.up) * Quaternion.Euler(-90, 0, 0),
                      _dnTitanModelled && risen && size > .05f ? 1 : 0);
            }
            float loaded = Ease(6.0f, 6.6f, t) * (1 - Ease(DnStomp, DnStomp + .1f, t));
            PlaceGlow(_dnPalmGlow, BothPalms, Vector3.one * Mathf.Lerp(.5f, 1.1f, loaded), Quaternion.identity, loaded * .75f * light);

            // Where its fist lands: a flash, the court heaved up round it, rock thrown, a ring of dust, and a lit seam across
            // the court in front of it, all on the last frames.
            {
                float hit = Mathf.Clamp01(landed / .1f);
                PlaceGlow(_dnStompFlash, DnStompAt + Vector3.up * .5f, Vector3.one * Mathf.Lerp(3.5f, 6.5f, hit), Quaternion.identity, landed >= 0 ? .9f * light : 0);
                Place(_dnStompRubble, DnStompAt + Vector3.up * .1f, new Vector3(Mathf.Lerp(1.2f, 2.9f, hit), 1.7f * DanteOut(hit), Mathf.Lerp(1.2f, 2.9f, hit)), Quaternion.Euler(0, 65, 0), landed >= 0 ? 1 : 0);
                PlaceGlow(_dnStompCore, DnStompAt + Vector3.up * .3f, Vector3.one * Mathf.Lerp(1.6f, 2.8f, hit), Quaternion.identity, landed >= 0 ? 1.1f * light : 0);
                for (int i = 0; i < _dnSlabs.Count; i++)
                {
                    // Four pieces of court big enough to see, flung flat side up and turning over.
                    float h = DanteHash(i * 4.7f), angle = (i * 85 + 50) * Mathf.Deg2Rad, out_ = 1.3f + (2.5f + 2f * h) * landed;
                    float high = .4f + (9f + 3f * h) * landed - 10f * landed * landed, size = .34f + .16f * h;
                    Place(_dnSlabs[i], DnStompAt + new Vector3(Mathf.Cos(angle) * out_, high, Mathf.Sin(angle) * out_), new Vector3(size, size, size),
                          Quaternion.Euler(landed * 600 * (h - .3f) + i * 60, i * 80, landed * 380), landed >= 0 ? 1 : 0);
                }
                float wide = Mathf.Lerp(1.5f, 6.5f, DanteOut(landed / .2f));
                Place(_dnStompRing, DnStompAt + Vector3.up * .25f, new Vector3(wide, 1.2f, wide), Quaternion.identity, landed >= 0 ? .85f : 0);
                for (int i = 0; i < _dnStompShards.Count; i++)
                {
                    float h = DanteHash(i * 5.3f), g = DanteHash(i * 1.9f), angle = (i * 45 + 20) * Mathf.Deg2Rad;
                    float out_ = 1.1f + (3f + 4f * h) * landed, high = .3f + (7f + 5f * g) * landed - 10f * landed * landed;
                    float size = .13f + .12f * g;
                    Place(_dnStompShards[i], DnStompAt + new Vector3(Mathf.Cos(angle) * out_, high, Mathf.Sin(angle) * out_), new Vector3(size, size * 2.6f, size),
                          Quaternion.Euler(landed * 900 * (h - .5f) * 2 + i * 40, i * 50, landed * 700), landed >= 0 ? 1 : 0);
                }
                Place(_dnBlastGlow, new Vector3(2.75f, DnOuterFloat + .12f, 3.4f), new Vector3(3.2f * (1 + 4f * Mathf.Clamp01(landed / .1f)), 1, .5f), Quaternion.identity,
                      landed >= 0 ? 1 : 0);
            }
            SampleDanteFx(t, lift, open, broken, light);
        }
    }
}
