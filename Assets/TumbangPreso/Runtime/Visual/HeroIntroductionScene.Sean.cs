using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAGO, MAYON AWAKENS v4: THE SKY TURNS AND THE MOUNTAIN IS BORN, 7.0 s.
        //
        // Owner 2026-11-23, of v3: "i'd like for a more dramatic ultimate cutscene for rago. the current shots really dont
        // sell the fact that he creates a whole volcano. the sky should turn red, with dark clouds etc", and "more unique
        // camera views/shots.. from a drone/sky view etc". v3's payoff was a 3 m cone seen from 10 m in a static wide shot
        // under the street's afternoon. v4 keeps v3's beats and its hand-back and changes two things:
        //   THE SKY (`HeroIntroductionScene.SeanSky.cs`): as he calls the mountain the sky turns blood red with dark clouds
        //      rolling in over him, the street's light goes red and low, ash falls, and the mountain's spirit wakes on the
        //      horizon; the column of ash climbs into that sky at the rise.
        //   VANTAGE AS SCALE: six shots, each one idea, on the right of the line from his fists to the spot so the lava always
        //      travels left to right: A1 THE BLOW (court level), A2 THE SKY TURNS (worm's-eye looking up past him into the
        //      sky), B1 THE DRONE (high over the court, riding the river), B2 THE FOOT (worm's-eye at the spot, the cone
        //      punching up past the lens), C1 FROM THE CLOUDS (22 m up, the column climbing to the lens), C2 THE HERO (low
        //      beside him, him and the mountain towering). The extra second is the drone and the clouds.
        //
        // v3, which v4 is built on:
        //
        // Owner 2026-11-23: "volcano look is fine", "start rago's cutscene with the new volcano"; of v2, the execution was
        // "pretty poor" and the concept stays (the fists, the run, the rise). Earlier, 2026-10-24: *"his ult sucks as shit
        // especially cutscene i cant even tell wtf his ult does"*, and he chose "The mountain rises". Play is `SeanVolcano`.
        // The direction (one sentence, the travelling thing, the beat table, the shots) is in the rework's DIRECTION.md;
        // in short:
        //
        // ONE SENTENCE: he drives the lava in his fists into the court, it races to the spot he chose, and the court there
        // gives birth to the mountain, which drinks the river and throws its wave at the players.
        // ONE TRAVELLING THING: the lava, ONE substance the whole way: the live volcano's own shader (`SeanVolcanoLava`,
        // dark crust plates on a molten core, a torn hot front that grows by `_Grow`) in the cracks round his fists, up his
        // forearms (the Molten fists, the locked look, called), along the court as a river, pooling on the court where it
        // bulges, drunk back into the spot as the mountain bursts up, and out again down the mountain's own flows.
        //   A. THE FISTS (0 to 1.6): A1 low at court level, the wind-up and the SLAM (0.45); A2 close on his forearms, the
        //      lava climbing them, craning up to his eyes as they ignite (1.3).
        //   B. THE RUN (1.6 to 3.4): B1 at shin height racing beside the river's head; B2 low at the spot as it arrives,
        //      pools, and the court bulges, cracks and vents.
        //   C. THE RISE (3.4 to 6.0): the mountain punches up past B2's lens (3.4); C, wide and low from his side, him near,
        //      the mountain behind, the wave riding out to the players; then the lens comes home to his own eyes.
        // ⚠️ THE STAGED MOUNTAIN IS PLAY'S. `SeanVolcanoLook.Build` at play's spot (`SeanVolcano.SpotFor`), turned with the
        // world, so it turns to the can by the same code; its mountain rises on play's curve; its court and rocks are posed
        // by play's own `Live.Pose`; the eruption round it is `SeanEruption`, which play builds too and poses from the same
        // number. Play is spawned `HandBackLead` (= this length minus `SeanRiseAt`) seconds into all of it, so its first frame
        // is this one's last. The river, the cracks round his fists and the bulge are all drunk back by 3.9: nothing of the
        // cutscene's own is left to vanish at the hand-back.
        // ⚠️ IT ENDS AS THE WAVE REACHES THE PLAYERS, NOT AFTER IT THROWS THEM: play throws them at the hand-back.
        // ⚠️ NO STAGE WALL: the court and the real players are the stage; only the grade steps back.
        // ⚠️ NOTHING HERE RUNS ON UPDATE. Every piece is posed from the scene clock in `SampleSean`.
        // =========================================================================================

        /// <summary>When the mountain begins to rise in the cutscene. Play is handed the rest (`SeanVolcano.HandBackLead`).</summary>
        public const float SeanRiseAt = 3.60f;

        private const float SnSlam = .45f, SnFistsFrom = .62f, SnFistsTo = 1.30f, SnIgnite = 1.45f, SnRunFrom = 1.75f, SnRunTo = 3.05f,
                            SnBulge = 3.02f, SnRise = SeanRiseAt, SnRiseSeconds = .45f, SnDrain = .5f, SnHome = 6.35f,
                            SnWebFrom = 1.70f, SnWebTo = 2.60f;
        private static readonly Color SnCore = new Color(1f, .74f, .20f, 1), SnEmber = new Color(1f, .45f, .12f, 1);
        private static readonly Color SnSteamLit = new Color(.88f, .84f, .80f), SnSteamShade = new Color(.66f, .61f, .58f), SnSteamLine = new Color(.45f, .40f, .38f);
        private static readonly Color SnDustLit = new Color(.90f, .83f, .74f), SnDustShade = new Color(.70f, .61f, .53f), SnDustLine = new Color(.50f, .42f, .36f);

        private Vector3 _snSpot;
        /// <summary>Where the staged mountain stands (world), for a film that hands it over to the live one. Null for others.</summary>
        public Vector3? SeanSpotWorld => _snVolRoot != null ? _snVolRoot.position : (Vector3?)null;
        private Transform _snVolRoot, _snVolBody;
        private SeanVolcanoLook.Live _snLook;
        private SeanEruption _snErupt;
        // The lava on the court: what wells up round his fists, the river (grown from his fists, then drunk back from the
        // spot), and the court splitting at the spot.
        private Renderer _snFistCracks, _snFistPool, _snRiverOut, _snRiverBack, _snSpotCracks, _snSpotPool, _snDome, _snWeb;
        private Transform _snDomeAt;
        private float _snFistCrackLength, _snRiverLength, _snSpotCrackLength, _snWebLength;
        private MaterialPropertyBlock _snBlock;
        private FxCloud _snSlamDust;
        private readonly List<FxCloud> _snVents = new List<FxCloud>(2);
        private readonly List<Vector3> _snVentAt = new List<Vector3>(2);
        private readonly List<int> _snFistGlows = new List<int>(2), _snDrips = new List<int>(6), _snEyes = new List<int>(2);
        private readonly List<Quaternion> _snOtherRest = new List<Quaternion>(4);
        private readonly List<Mesh> _snMeshes = new List<Mesh>();
        private readonly List<Renderer> _snBombs = new List<Renderer>(5);

        /// <summary>A Molten fist on one of the copy's hands: the model, the bone it rides, the palm in that bone's space.</summary>
        private sealed class SnFist { public Transform Model, Knuckles, Bone; public Vector3 Palm; public float Scale; public bool Carrying; }
        private readonly List<SnFist> _snFists = new List<SnFist>(2);

        private static readonly int SnGrowId = Shader.PropertyToID("_Grow"), SnClockId = Shader.PropertyToID("_Clock");

        private void BuildSean()
        {
            // Play's spot, in the scene's space: where the live mountain will stand.
            var lata = GameServices.Round?.Lata;
            var spot = Abilities.SeanVolcano.SpotFor(_source.transform.position, _source.transform.forward, _aim, lata);
            if (lata == null && FilmCan.HasValue)
            {
                var can = FilmCan.Value;
                SeanRules.VolcanoSpotClearOfCan(spot.x, spot.z, can.x, can.z, _source.transform.position.x, _source.transform.position.z,
                    out float x, out float z);
                spot.x = x; spot.z = z;
            }
            _snSpot = _root.transform.InverseTransformPoint(spot);
            _snSpot.y = Mathf.Clamp(_snSpot.y, -1.5f, 1.5f);
            spot = _root.transform.TransformPoint(_snSpot);
            _snBlock = new MaterialPropertyBlock();

            // THE MOUNTAIN, PLAY'S OWN, built first: the court under its foot is looked for among the renderers about, and this
            // stage's own court-coloured bulge (below) must not be there yet to be found instead.
            _snVolRoot = new GameObject("SeanVolcanoStaged").transform;
            _snVolRoot.SetParent(_root.transform, false);
            _snVolRoot.SetPositionAndRotation(spot, Quaternion.identity);
            _snVolBody = new GameObject("Body").transform;
            _snVolBody.SetParent(_snVolRoot, false);
            _snLook = SeanVolcanoLook.Build(_snVolRoot, _snVolBody, spot);
            if (_snLook != null)
            {
                _snVolBody.localScale = Vector3.one * _snLook.Fit;
                _snLook.Mountain.gameObject.SetActive(false);
                if (_snLook.Surround != null) _snLook.Surround.gameObject.SetActive(false);
                if (_snLook.Rocks != null) _snLook.Rocks.gameObject.SetActive(false);
            }
            _snErupt = SeanEruption.Build(_snVolRoot);
            foreach (var r in _snVolRoot.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("SeanEruption-bomb")) _snBombs.Add(r);
            // Under his sky the eruption's pale court dust is graded red with the street, and still read as pale balls near the
            // lens (film v4r3, B2 and C2): the cutscene's copies are a darker warm dust. Play's own are untouched; at the
            // hand-back the eruption has finished on both sides (`HandBackLead` is `SeanEruption.EndSeconds`).
            foreach (var r in _snVolRoot.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty("_Lit")) continue;
                // The crater's own ash burst: lit from the fire below like the column (`SnLitFromBelow`), so it is not a
                // hollow ring of lines against the dark sky.
                if (r.name.StartsWith("SeanEruption-ash"))
                {
                    m.SetColor("_Lit", new Color(1f, .62f, .40f)); m.SetColor("_Shade", new Color(.36f, .23f, .21f)); m.SetColor("_LineColor", new Color(.20f, .12f, .11f));
                    SnLitFromBelow(r);
                    continue;
                }
                if (!r.name.StartsWith("SeanEruption-dust") && !r.name.StartsWith("SeanEruption-wave")) continue;
                m.SetColor("_Lit", new Color(.70f, .52f, .44f)); m.SetColor("_Shade", new Color(.47f, .33f, .29f)); m.SetColor("_LineColor", new Color(.30f, .20f, .17f));
            }

            // A: the court splits round his fists and a pool wells up; the slam's dust.
            BuildSeanFists();
            _snFistCracks = SnLavaPiece("SeanFistCracks", SnCracks(SnFistsOnCourt, 6, .85f, 1.55f, .34f, 11, null, out _snFistCrackLength), false);
            _snFistPool = SnLavaPiece("SeanFistPool", SnPool(SnFistsOnCourt + Vector3.up * .004f, .55f, 12, null), true);
            // The web: nine long cracks spidering out over the whole court from his fists as the river leaves (B1, the drone,
            // sees all of it). The same lava, the same growing front; drunk back with the river. Hotter than the cracks at his
            // fists (film v4r1: crusted dark, from 10 m up they read as the legs of a black spider).
            _snWeb = SnLavaPiece("SeanCourtWeb", SnCracks(SnFistsOnCourt, 9, 2.8f, 5.6f, .30f, 37, null, out _snWebLength, .02f, .35f), false);
            _snSlamDust = FxCloud.Build(_root.transform, "SeanSlamDust", 53, 40, SnDustLit, SnDustShade, SnDustLine, FxStudyCloud.Line * .3f / .62f, .07f, .30f);
            for (int i = 0; i < 2; i++) _snFistGlows.Add(AddGlow("SeanFistGlow" + i, SnEmber, falloff: 2.2f, core: .45f));
            for (int i = 0; i < 6; i++) _snDrips.Add(AddGlow("SeanDrip" + i, i % 2 == 0 ? SnCore : SnEmber, falloff: 2.4f, core: .8f));
            var glint = VfxShapes.Star(4, .22f);
            for (int i = 0; i < 2; i++) _snEyes.Add(AddGlow("SeanEyeGlint" + i, SnCore, glint, falloff: 1.4f, core: .8f));

            // B: the river, twice over the same path: grown out from his fists, and the same strip measured from the spot,
            // which is what is shown once it has arrived, so that running `_Grow` back drinks it into the spot.
            var path = new List<Vector3>(24);
            for (int i = 0; i <= 23; i++) path.Add(SnRiverAt(i / 23f));
            _snRiverOut = SnLavaPiece("SeanRiver", SnStrip(path, false, out _snRiverLength), false);
            _snRiverBack = SnLavaPiece("SeanRiverBack", SnStrip(path, true, out _), false);

            // B: the court at the spot, domed up in the court's own material and mapping, and the lava splitting it.
            var court = SeanVolcanoLook.FindCourt(spot, _snVolRoot, out var uvOf, out _);
            if (court == null) { court = Resources.Load<Material>(SeanVolcanoLook.Folder + "volcano_paving"); uvOf = w => new Vector2(w.x, w.z) / 2f; }
            _snDomeAt = new GameObject("SeanBulge").transform;
            _snDomeAt.SetParent(_root.transform, false);
            _snDomeAt.localPosition = _snSpot;
            var domeGo = new GameObject("SeanBulgeCourt");
            domeGo.transform.SetParent(_snDomeAt, false);
            domeGo.AddComponent<MeshFilter>().sharedMesh = SnDomeMesh(uvOf);
            var domeR = domeGo.AddComponent<MeshRenderer>();
            domeR.sharedMaterial = court;
            domeR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxRenderTag.Attach(domeGo);
            _snDome = domeR;
            _snSpotCracks = SnLavaPiece("SeanSpotCracks", SnCracks(Vector3.zero, 5, 1.1f, 1.75f, .30f, 23, SnDomeHeight, out _snSpotCrackLength), false, _snDomeAt);
            _snSpotPool = SnLavaPiece("SeanSpotPool", SnPool(Vector3.zero, .75f, 29, SnDomeHeight), true, _snDomeAt);
            for (int i = 0; i < 2; i++)
            {
                float a = (i * 160f + 40f) * Mathf.Deg2Rad;
                _snVentAt.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 1.25f);
                _snVents.Add(FxCloud.Build(_snDomeAt, "SeanVent" + i, 61 + i, 30, SnSteamLit, SnSteamShade, SnSteamLine, FxStudyCloud.Line * .45f / .62f, .10f, .45f));
            }

            // The real players, where they stand (never pulled in: who is thrown is play's rule, not the stage's).
            StageOthers(1.0f, 40f);
            foreach (var other in _others) _snOtherRest.Add(other.Holder.transform.localRotation);

            // v4: the sky he turns, the street's red light, the ash, the column and its lightning (`SeanSky.cs`).
            BuildSeanSky();
            _roll = SnRoll;
        }

        /// <summary>The lens's roll (degrees): a kick against the rise in B2, a slow lean from the clouds in C1, level elsewhere.</summary>
        private static float SnRoll(float t)
        {
            if (t >= SnRise && t < 4.05f) return -3.5f * Ease(SnRise, SnRise + .2f, t);
            if (t >= 4.05f && t < 5.05f) return 6f - 2.5f * Ease(4.05f, 5.05f, t);
            return 0f;
        }

        /// <summary>
        /// One Molten fist per hand of the copy, on the hand bones the game itself uses (`CharacterVisual.HandBone`): on this rig
        /// that is the FOREARM, so the fist sits on the hand and not the elbow, and the sleeve lies along the real forearm.
        /// ⚠️ THE LOCKED LOOK (`SeanProps/molten_fist`, what `SeanMoltenLook` puts on him in play): placed and grown, never restyled.
        /// </summary>
        private void BuildSeanFists()
        {
            if (_bodyRoot == null) return;
            SkinnedMeshRenderer skinned = null;
            foreach (var candidate in _bodyRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (CharacterVisual.HandBone(candidate, "right", out _, out _)) { skinned = candidate; break; }
            if (skinned == null) return;
            foreach (string side in new[] { "left", "right" })
            {
                if (!CharacterVisual.HandBone(skinned, side, out int index, out var palm)) continue;
                var model = SeanProp.Spawn("molten_fist", _root.transform);
                if (model == null) return;
                model.SetActive(false);
                var bone = skinned.bones[index];
                float forearm = (bone.TransformPoint(palm) - bone.position).magnitude;
                // The slipper rides the right hand (`CharacterVisual.HandAnchor`): that hand stays a hand.
                _snFists.Add(new SnFist
                {
                    Model = model.transform, Knuckles = SeanProp.Find(model, "knuckles"), Bone = bone, Palm = palm,
                    Scale = Mathf.Clamp(forearm * .95f / .36f, .6f, 1.6f), Carrying = side == "right" && _heldItem != null,
                });
            }
        }

        // ------------------------------------------------------------------ the lava's meshes (built once)

        private Renderer SnLavaPiece(string name, Mesh mesh, bool pool, Transform parent = null)
        {
            _snMeshes.Add(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            // The stage's own copy, drawn after the street's red grade (`SeanSkyGrade`, AlphaTest+52) so the lava on the court
            // keeps its own heat under the red light (film v4r2: graded with the court, the river and the web went dull).
            r.sharedMaterial = SnLavaAfterGrade(pool);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            VfxRenderTag.Attach(go);
            return r;
        }

        private Material _snLavaFlow, _snLavaPool;
        private Material SnLavaAfterGrade(bool pool)
        {
            ref Material m = ref (pool ? ref _snLavaPool : ref _snLavaFlow);
            if (m != null) return m;
            var shared = SeanEruption.Lava(pool);
            if (shared == null) return null;
            m = new Material(shared) { name = shared.name + "-stage", renderQueue = 2504 };
            return m;
        }

        /// <summary>The dome the court bulges into at the spot, unit high at its middle, 0 at its rim.</summary>
        private const float SnDomeRadius = 1.9f, SnDomeTall = .5f;
        private static float SnDomeHeight(Vector3 local)
        {
            float r = Mathf.Clamp01(new Vector2(local.x, local.z).magnitude / SnDomeRadius);
            float k = 1f - r * r;
            return k * k;
        }

        /// <summary>
        /// A lava strip along <paramref name="path"/> in the format `SeanVolcanoLava` reads for a flow: uv0 across and down
        /// (in repeats of the crust field), uv1 (which flow, how far it has cooled), uv2 (metres from where it grows from).
        /// ⚠️ THE SHADER TURNS EVERY UV SET'S v BACK OVER (the importer flips the model's), so every v here is written as 1
        /// minus itself. `back` measures the metres from the far end, so `_Grow` run down drinks it into that end.
        /// </summary>
        private static Mesh SnStrip(List<Vector3> path, bool back, out float length)
        {
            var p = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var flow = new List<Vector2>(); var grow = new List<Vector2>();
            var tris = new List<int>();
            length = 0f;
            var along = new float[path.Count];
            for (int i = 1; i < path.Count; i++) { length += Vector3.Distance(path[i - 1], path[i]); along[i] = length; }
            SnStripInto(path, along, length, back, 0, .95f, .60f, .05f, .55f, null, p, n, uv, flow, grow, tris);
            return SnMesh("SeanRiver" + (back ? "Back" : ""), p, n, uv, flow, grow, tris);
        }

        private static void SnStripInto(List<Vector3> path, float[] along, float length, bool back, int flowIndex, float wideAt, float wideEnd,
                                        float coolFrom, float coolTo, Func<Vector3, float> lift,
                                        List<Vector3> p, List<Vector3> n, List<Vector2> uv, List<Vector2> flow, List<Vector2> grow, List<int> tris)
        {
            const int across = 6;
            int first = p.Count;
            for (int i = 0; i < path.Count; i++)
            {
                float s = along[i] / Mathf.Max(.01f, length);
                var a = path[Mathf.Max(0, i - 1)]; var b = path[Mathf.Min(path.Count - 1, i + 1)];
                var way = b - a; way.y = 0f; if (way.sqrMagnitude < 1e-6f) way = Vector3.forward; way.Normalize();
                var side = new Vector3(way.z, 0f, -way.x);
                // Wide where it leaves, narrowing as it runs; the very ends pinched so a strip never ends in a square.
                float w = Mathf.Lerp(wideAt, wideEnd, s) * Mathf.Lerp(.55f, 1f, Mathf.Clamp01(Mathf.Min(s, 1f - s) * 12f));
                float rep = Mathf.Max(.2f, wideAt / .65f);
                for (int k = 0; k <= across; k++)
                {
                    float u = k / (float)across, off = (u - .5f) * w;
                    var at = path[i] + side * off;
                    // Domed a little across: a flow has body, it is not a decal.
                    at.y += .012f + .028f * (1f - (2f * u - 1f) * (2f * u - 1f));
                    if (lift != null) at.y += lift(at) * SnDomeTall;
                    p.Add(at); n.Add(Vector3.up);
                    uv.Add(new Vector2(u, 1f - along[i] / rep));
                    flow.Add(new Vector2(0f, 1f - Mathf.Lerp(coolFrom, coolTo, s)));
                    grow.Add(new Vector2(back ? length - along[i] : along[i], 1f));
                    if (i == 0 || k == across) continue;
                    int c = first + i * (across + 1) + k, pr = c - (across + 1);
                    tris.Add(pr); tris.Add(c); tris.Add(pr + 1);
                    tris.Add(pr + 1); tris.Add(c); tris.Add(c + 1);
                }
            }
        }

        /// <summary>Cracks that branch out from <paramref name="centre"/>: short flows, each its own wandering path, all grown
        /// from the centre (uv2 is the metres from it), in one mesh.</summary>
        private static Mesh SnCracks(Vector3 centre, int count, float shortest, float longest, float width, int salt, Func<Vector3, float> lift, out float longestOut,
                                      float coolFrom = .25f, float coolTo = .80f)
        {
            var p = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var flow = new List<Vector2>(); var grow = new List<Vector2>();
            var tris = new List<int>();
            longestOut = 0f;
            for (int c = 0; c < count; c++)
            {
                float a = (c + .5f * SnScatter(c, salt)) / count * Mathf.PI * 2f;
                float len = Mathf.Lerp(shortest, longest, SnScatter(c, salt + 1));
                var path = new List<Vector3>(9); var along = new float[9];
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var at = centre + dir * .12f;
                for (int i = 0; i < 9; i++)
                {
                    if (i > 0)
                    {
                        float turn = (SnScatter(c * 11 + i, salt + 2) - .5f) * .9f;
                        dir = Quaternion.Euler(0f, turn * Mathf.Rad2Deg, 0f) * dir;
                        var next = at + dir * (len / 8f);
                        along[i] = along[i - 1] + (next - at).magnitude;
                        at = next;
                    }
                    path.Add(at);
                }
                float total = along[8] + .12f;
                for (int i = 0; i < 9; i++) along[i] += .12f;
                longestOut = Mathf.Max(longestOut, total);
                SnStripInto(path, along, total, false, 0, width, width * .35f, coolFrom, coolTo, lift, p, n, uv, flow, grow, tris);
            }
            return SnMesh("SeanCracks", p, n, uv, flow, grow, tris);
        }

        /// <summary>A pool, in the format `SeanVolcanoLava` reads for one: uv1.y how far out (0.25 in the middle, 1 at the rim),
        /// uv2 the metres from the middle (so it grows outward); its rim lobed.</summary>
        private static Mesh SnPool(Vector3 centre, float radius, int salt, Func<Vector3, float> lift)
        {
            const int rings = 6, sides = 28;
            var p = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var flow = new List<Vector2>(); var grow = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f, k = r / (float)rings;
                    float lobe = 1f + .18f * Mathf.Sin(a * 3f + salt) + .10f * Mathf.Sin(a * 7f + salt * 1.7f);
                    var at = centre + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius * k * lobe;
                    at.y += .02f + .03f * (1f - k * k);
                    if (lift != null) at.y += lift(at) * SnDomeTall;
                    p.Add(at); n.Add(Vector3.up);
                    uv.Add(new Vector2(.5f + .5f * Mathf.Sin(a) * k, 1f - (.5f + .5f * Mathf.Cos(a) * k)));
                    flow.Add(new Vector2(0f, 1f - (.25f + .75f * k)));
                    grow.Add(new Vector2(radius * k * lobe, 1f));
                    if (r == 0 || s == sides) continue;
                    int c = r * (sides + 1) + s, pr = c - (sides + 1);
                    tris.Add(pr); tris.Add(c); tris.Add(pr + 1);
                    tris.Add(pr + 1); tris.Add(c); tris.Add(c + 1);
                }
            return SnMesh("SeanPool", p, n, uv, flow, grow, tris);
        }

        /// <summary>The bulge: a disc of the court, its uv the court's own at every point (so its joints carry on from the
        /// ground round it), domed unit high in its middle. The stage scales it up from flat.</summary>
        private Mesh SnDomeMesh(Func<Vector3, Vector2> uvOf)
        {
            const int rings = 12, sides = 40;
            var p = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f, k = r / (float)rings;
                    var flat = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * SnDomeRadius * k;
                    var at = flat + Vector3.up * (SnDomeHeight(flat) * SnDomeTall + .004f);
                    p.Add(at);
                    uv.Add(uvOf(_root.transform.TransformPoint(_snSpot + flat)));
                    if (r == 0 || s == sides) continue;
                    int c = r * (sides + 1) + s, pr = c - (sides + 1);
                    tris.Add(pr); tris.Add(c); tris.Add(pr + 1);
                    tris.Add(pr + 1); tris.Add(c); tris.Add(c + 1);
                }
            var mesh = new Mesh { name = "SeanBulge", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(p); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            _snMeshes.Add(mesh);
            return mesh;
        }

        private static Mesh SnMesh(string name, List<Vector3> p, List<Vector3> n, List<Vector2> uv, List<Vector2> flow, List<Vector2> grow, List<int> tris)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.indexFormat = p.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(p); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetUVs(1, flow); mesh.SetUVs(2, grow);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A fixed scatter in 0 to 1 for row <paramref name="i"/>: the same every frame and on every peer.</summary>
        private static float SnScatter(int i, int salt)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(salt * 19349663);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }

        /// <summary>Where his fists meet the court (the river's source).</summary>
        private static readonly Vector3 SnFistsOnCourt = new Vector3(0, .03f, .55f);

        /// <summary>The river's path from his fists to the spot, <paramref name="u"/> 0 to 1, a gentle S so it reads as poured.</summary>
        private Vector3 SnRiverAt(float u)
        {
            var from = SnFistsOnCourt; var to = new Vector3(_snSpot.x, _snSpot.y + .01f, _snSpot.z);
            var way = to - from; way.y = 0;
            var side = way.sqrMagnitude > .01f ? new Vector3(way.z, 0, -way.x).normalized : Vector3.right;
            float bend = Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Min(.5f, way.magnitude * .05f) * (1f - u * .5f);
            var p = Vector3.Lerp(from, to, u) + side * bend;
            p.y = Mathf.Lerp(from.y - .02f, to.y, u);
            return p;
        }

        /// <summary>The river's head along its path, 0 to 1: off his fists at 1.65, at the spot by 2.75, fast at first.</summary>
        private static float SnRiverHead(float t)
        {
            float u = Mathf.Clamp01(Mathf.InverseLerp(SnRunFrom, SnRunTo, t));
            return 1 - (1 - u) * (1 - u) * (1 - .3f * u);
        }

        /// <summary>Play's rise curve (`SeanVolcano.Pose` on a fresh cast): up out of the court with one overshoot.</summary>
        private static float SnRiseUp(float t)
        {
            float u = Mathf.Clamp01((t - SnRise) / SnRiseSeconds);
            return t <= SnRise ? 0f : 1f - Mathf.Pow(1f - u, 3f) + .12f * Mathf.Sin(u * Mathf.PI);
        }

        // ------------------------------------------------------------------ the clock

        private void SampleSean(float t)
        {
            float present = StagePresence(t, .2f);
            SampleSeanFists(t, present);
            SampleSeanLava(t);
            SampleSeanMountain(t);
            SampleSeanOthers(t);
            SampleSeanSky(t);
        }

        private void SnLava(Renderer r, float grow, float clock)
        {
            if (r == null) return;
            bool on = grow > .005f;
            r.enabled = on;
            if (!on) return;
            r.GetPropertyBlock(_snBlock);
            _snBlock.SetFloat(SnGrowId, grow);
            _snBlock.SetFloat(SnClockId, clock);
            r.SetPropertyBlock(_snBlock);
        }

        private void SampleSeanFists(float t, float present)
        {
            // The Molten fists: lava climbs out of the court up both forearms (left, then right) and stays; he is Molten from here.
            for (int i = 0; i < _snFists.Count; i++)
            {
                var f = _snFists[i];
                float from = SnFistsFrom + .12f * i;
                float grow = Mathf.Clamp01(Mathf.InverseLerp(from, from + (SnFistsTo - SnFistsFrom), t));
                float swell = grow <= 0 ? 0 : 1f - Mathf.Pow(1f - grow, 3f) + .2f * Mathf.Sin(grow * Mathf.PI);
                swell *= 1 + .12f * Flash(t, SnRise, .12f);
                if (f.Carrying) swell = 0;
                bool shown = swell > .01f;
                if (f.Model.gameObject.activeSelf != shown) f.Model.gameObject.SetActive(shown);
                Vector3 palmWorld = f.Bone.TransformPoint(f.Palm);
                Vector3 palm = _root.transform.InverseTransformPoint(palmWorld);
                Vector3 dir = _root.transform.InverseTransformDirection(palmWorld - f.Bone.position);
                if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
                dir.Normalize();
                // The world's up, so its drips hang down (film s15: the bone's up stood them out sideways like rods).
                var up = Mathf.Abs(dir.y) < .95f ? Vector3.up : Vector3.forward;
                float s = f.Scale * Mathf.Max(.0001f, swell);
                f.Model.localPosition = palm - dir * .1f * s;
                f.Model.localRotation = Quaternion.LookRotation(dir, up);
                f.Model.localScale = Vector3.one * s;
                if (f.Knuckles != null) f.Knuckles.localScale = Vector3.one * (1f + .08f * Mathf.Sin(t * 9f + i * 1.7f));
                if (i < _snFistGlows.Count)
                    PlaceGlow(_snFistGlows[i], palm, Vector3.one * .45f * Mathf.Min(1, swell), Quaternion.identity, shown ? .8f + .6f * Flash(t, SnRise, .2f) : 0);
                for (int d = i * 3; d < i * 3 + 3 && d < _snDrips.Count; d++)
                {
                    float cycle = Mathf.Repeat(t * 1.4f + d * .29f, 1f);
                    var at = palm + new Vector3(.06f * (d % 3 - 1), -cycle * Mathf.Max(.1f, palm.y), .02f);
                    PlaceGlow(_snDrips[d], at, Vector3.one * .05f, Quaternion.identity, shown && cycle < .95f ? 1.1f : 0);
                }
            }

            // His eyes ignite as the lava reaches his elbows: a four-pointed glint on each.
            var head = HeadPoint + new Vector3(0, -.32f, .17f);
            float eyes = Ease(SnIgnite - .05f, SnIgnite + .08f, t) * (1 - Ease(SnRunFrom + .3f, SnRunFrom + .7f, t)) * (1 + 1.5f * Flash(t, SnIgnite + .05f, .12f));
            for (int i = 0; i < _snEyes.Count; i++)
                PlaceGlow(_snEyes[i], head + new Vector3(i == 0 ? -.075f : .075f, 0, 0), Vector3.one * (.16f + .2f * Flash(t, SnIgnite + .05f, .15f)),
                    Quaternion.Euler(0, 0, 45f * t), eyes * 1.3f * present);

            // The slam's dust: thrown out low round his fists, settling.
            if (_snSlamDust != null)
            {
                // Short and low: it is gone before A2's lens comes in close over his fists (film r3: balls filled that lens).
                var e = FxCloud.Emitter.Puff(SnFistsOnCourt + Vector3.up * .04f, new Vector3(0f, .28f, -.1f), SnSlam, .10f, .38f);
                e.Spread = 1.0f; e.Widen = .7f; e.Weave = .12f; e.Small = .06f; e.Large = .24f; e.Gone = .04f;
                bool on = t >= SnSlam && t <= SnSlam + .5f;
                _snSlamDust.Piece.Renderer.enabled = on;
                if (on) _snSlamDust.Pose(t, e);
            }
        }

        private void SampleSeanLava(float t)
        {
            // One clock for the crust on the court, the same pace as the mountain's.
            float clock = 5f + t;
            // Drunk back: everything on the court goes back toward where it came from as the mountain rises.
            float drain = Ease(SnRise, SnRise + SnDrain, t);
            // A: the court splits round his fists as they land, and a pool wells up between them.
            float cracks = Ease(SnSlam, SnSlam + .38f, t);
            SnLava(_snFistCracks, (cracks * (1f - drain)) * _snFistCrackLength, clock);
            SnLava(_snFistPool, Ease(SnSlam + .02f, SnSlam + .45f, t) * (1f - drain) * .75f, clock);
            // The web spiders out fast at first and slows, as a crack runs.
            float web = Ease(SnWebFrom, SnWebTo, t);
            SnLava(_snWeb, (1f - (1f - web) * (1f - web)) * (1f - drain) * _snWebLength, clock);

            // B: the river grows from his fists to the spot; once there it is the strip measured from the spot, and the
            // mountain drinks it.
            float head = SnRiverHead(t);
            bool arrived = t >= SnRunTo;
            SnLava(_snRiverOut, arrived ? 0f : t > SnRunFrom ? head * _snRiverLength : 0f, clock);
            SnLava(_snRiverBack, arrived ? (drain > 0f ? (1f - drain) * _snRiverLength : 100f) : 0f, clock);

            // B: the court at the spot bulges up, trembling, cracks open and vents; the mountain breaks it.
            float bulge = Ease(SnBulge, SnRise - .05f, t);
            bool domeOn = t >= SnBulge - .05f && t < SnRise + .03f;
            if (_snDome != null) _snDome.enabled = domeOn;
            float tremble = .06f * Mathf.Sin(t * 47f) * bulge;
            _snDomeAt.localScale = new Vector3(1f, Mathf.Max(.001f, bulge * (1f + tremble)), 1f);
            _snDomeAt.localPosition = _snSpot;
            SnLava(_snSpotCracks, domeOn ? Ease(SnBulge + .12f, SnRise - .05f, t) * _snSpotCrackLength : 0f, clock);
            SnLava(_snSpotPool, domeOn ? Ease(SnRunTo - .05f, SnRunTo + .4f, t) * 1.05f : 0f, clock);
            for (int i = 0; i < _snVents.Count; i++)
            {
                var cloud = _snVents[i];
                float from = SnBulge + .22f + .12f * i;
                var e = FxCloud.Emitter.Column(_snVentAt[i] + Vector3.up * .1f, new Vector3(.15f, 1.6f, .1f));
                e.Spread = .25f; e.Life = .7f; e.Widen = .35f; e.Weave = .12f; e.Small = .10f; e.Large = .45f; e.Gone = .05f;
                e.From = from; e.To = SnRise;
                bool on = t >= from && t <= SnRise + .7f;
                cloud.Piece.Renderer.enabled = on;
                if (on) cloud.Pose(t, e);
            }
        }

        private void SampleSeanMountain(float t)
        {
            // The mountain punches up out of the court on play's curve, and from there it is play's: its court and rocks burst
            // up by play's `Live.Pose`, its lava runs down from the crater over a second, the eruption goes off round it.
            float since = t - SnRise;
            bool up = since >= 0f;
            if (_snLook != null)
            {
                if (_snLook.Mountain.gameObject.activeSelf != up) _snLook.Mountain.gameObject.SetActive(up);
                if (_snLook.Surround != null && _snLook.Surround.gameObject.activeSelf != up) _snLook.Surround.gameObject.SetActive(up);
                if (_snLook.Rocks != null && _snLook.Rocks.gameObject.activeSelf != up) _snLook.Rocks.gameObject.SetActive(up);
                float shake = _reducedEffects ? 0f : .05f * Mathf.Clamp01(1f - since / .4f) * Mathf.Sin(t * 70f) * (up ? 1f : 0f);
                _snVolBody.localPosition = new Vector3(shake, -SeanRules.VolcanoHeight * (1f - SnRiseUp(t)), 0f);
                _snLook.Pose(since, since, 0f, 0f);
            }
            _snErupt?.Pose(since);
            // A bomb that would pass within 2.4 m of B2's lens is not drawn there: that close it was a flat dark ball over a
            // quarter of the frame (film v4r7). The rest still arc over the lens.
            if (t >= 2.95f && t < 4.05f && _snBombs.Count > 0)
            {
                Vector3 eye = default, look = default; float fov = 0f;
                SeanFrame(3, t, ref eye, ref look, ref fov);
                var eyeWorld = _root.transform.TransformPoint(eye);
                foreach (var b in _snBombs)
                    if (b != null && b.enabled && (b.transform.position - eyeWorld).sqrMagnitude < 2.4f * 2.4f) b.enabled = false;
            }
        }

        private void SampleSeanOthers(float t)
        {
            // Each staged player braces and leans away from the mountain as its wave reaches them (play's own reach).
            float reach = SeanEruption.WaveReach(t - SnRise);
            for (int i = 0; i < _others.Count; i++)
            {
                var other = _others[i];
                if (other.Holder == null) continue;
                if (!other.Holder.activeSelf) other.Holder.SetActive(true);
                var away = other.Home - _snSpot; away.y = 0;
                float d = away.magnitude;
                float lean = d <= SeanRules.VolcanoBlastRadius ? Mathf.Clamp01((reach - d + .9f) / 1.1f) : 0f;
                lean = lean * lean * (3f - 2f * lean);
                var axis = Vector3.Cross(Vector3.up, away.sqrMagnitude > .001f ? away.normalized : Vector3.forward);
                other.Holder.transform.localRotation = Quaternion.AngleAxis(16f * lean, axis) * _snOtherRest[i];
            }
        }

        // ------------------------------------------------------------------ the lens, the grade, the hand-back

        /// <summary>
        /// Every shot is placed from his body and the real spot (the table's are fallbacks for a stage with no spot). Scene space.
        /// ⚠️ EVERY LENS STANDS ON THE RIGHT OF THE LINE FROM HIS FISTS TO THE SPOT (`side`; the left with the slipper hand), so
        /// the lava travels left to right on screen in every cut and each cut continues the one before.
        ///   0 A1 THE BLOW: at court level in front of his fists, looking up at him; the fists come down into frame.
        ///   1 A2 THE SKY TURNS: worm's-eye under his fists, looking up past his face into the sky as it turns red over him.
        ///   2 B1 THE DRONE: high over the court, looking steeply down, riding the river's head across it; the crack web
        ///     spidering out from his fists is the scale.
        ///   3 B2 THE FOOT: knee height, 4.4 m from the spot; the river runs in from the left, the court
        ///     bulges with a growing shake, the cone punches up past the lens and the lens tilts and widens with the summit.
        ///   4 C1 FROM THE CLOUDS: 22 m up beyond the spot, looking down; the column climbs toward the lens, lightning in it,
        ///     the dust surge rings out over the little court below.
        ///   5 C2 THE HERO: low beside and behind him, looking up past him at the summit; the wave reaches the players.
        /// </summary>
        private void SeanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var line = _snSpot - SnFistsOnCourt; line.y = 0;
            float length = line.magnitude;
            var dir = length > .01f ? line / length : Vector3.forward;
            var side = new Vector3(dir.z, 0, -dir.x);
            // The lens goes to the side away from the slipper hand.
            if (_heldItem != null) side = -side;
            float u;
            switch (index)
            {
                case 0:
                    u = Ease(0f, .70f, t);
                    // Far enough that his fists come down INTO the frame onto the court, low enough to look up at him.
                    eye = SnFistsOnCourt + dir * (2.1f - .2f * u) - side * (1.15f - .1f * u) + Vector3.up * .28f;
                    look = SnFistsOnCourt + Vector3.up * (.62f - .2f * Ease(.3f, .5f, t)) - dir * .15f;
                    fov = 56f - 3f * u;
                    break;
                case 1:
                {
                    // Under his fists, looking up past his face at the sky behind him; the aim climbs as the sky turns, so
                    // by the end he is the lower third and the red sky with its clouds is the rest.
                    u = Ease(.70f, 1.70f, t);
                    float turn = Ease(.75f, 1.30f, t);
                    // (film v4r1: from 1.9 m his face filled the middle and the sky was a sliver over the roofs.)
                    eye = SnFistsOnCourt + dir * (2.6f + .2f * u) + side * (.50f + .1f * u) + Vector3.up * (.20f + .08f * u);
                    look = HeadPoint + Vector3.up * (1.2f + 2.6f * turn) - dir * (1.0f + 2.0f * turn);
                    fov = 66f + 4f * u;
                    break;
                }
                case 2:
                {
                    // The drone rides the river's head from high on the right of the line, looking steeply down and a little
                    // ahead to the spot, so the court streams under it and the web is seen whole.
                    u = Ease(1.70f, 2.95f, t);
                    var at = SnRiverAt(SnRiverHead(Mathf.Clamp(t, SnRunFrom, SnRunTo)));
                    eye = at + side * 5.2f - dir * 2.4f + Vector3.up * (10.2f - 1.4f * u);
                    look = Vector3.Lerp(at, _snSpot, .28f);
                    fov = 50f;
                    break;
                }
                case 3:
                {
                    // The summit's height on screen: the lens tilts up and widens with it as it punches up.
                    float rise = SnRiseUp(t);
                    look = _snSpot - dir * .2f + Vector3.up * (.35f + 2.6f * Mathf.Min(1.1f, rise));
                    // Knee height, 4.4 m out: the 3 m cone towers (film v4r1: at 0.22 m the bulge was a dune filling the frame;
                    // v4r8: at 3.6 m the dust thrown round its foot rolled up to the lens as big balls).
                    // Of two places on the right of the line, the one whose sight line is clearest of the players.
                    eye = SnClearOf(_snSpot + Vector3.up * .3f, _snSpot - dir * 2.4f + side * 3.7f + Vector3.up * .55f,
                                    _snSpot - dir * 1.0f + side * 4.3f + Vector3.up * .55f);
                    fov = 58f + 14f * Ease(SnRise, SnRise + .35f, t);
                    // The lens punches up with the cone as it bursts past, and the dust thrown round its foot rolls out under it
                    // instead of into it (film v4r9: held at knee height, the last frames were big balls of dust at the lens).
                    eye += Vector3.up * 1.5f * Ease(SnRise + .05f, SnRise + .45f, t);
                    eye = KeepLensOffOthers(eye, 1.3f);
                    break;
                }
                case 4:
                {
                    // Up in the clouds beyond the spot, looking down past the column at the court; it sinks as the column
                    // climbs toward it. Still the right of the line, so he is the left of the frame and the mountain the right.
                    u = Ease(4.05f, 5.05f, t);
                    // (film v4r1: straight down, a column is foreshortened into one grey blob over the frame; from 35 degrees
                    // its climb reads, and the court is still a little square under it.)
                    // (v4r2: from 15 m up the cone hid under the column; from 11 m and further round, both read.)
                    eye = _snSpot + dir * 5f + side * 14f + Vector3.up * (11f - 1.5f * u);
                    look = _snSpot - dir * .5f + Vector3.up * (4.0f + 2.5f * u);
                    fov = 50f;
                    break;
                }
                default:
                {
                    // Low beside and behind him, looking up past him at the summit, him a towering shoulder at the left of the
                    // frame and the mountain over the court; the lens is cut to the mountain's distance so it stands as tall in
                    // the frame wherever he put it.
                    u = Ease(5.05f, SnHome, t);
                    // (film v4r1: 1.6 m behind him his shoulder was a third of the frame and soft, and the mountain sat low.)
                    // (v4r2: 2.6 m behind him, his head was off the top of the frame and he read as a block. v4r4: drifting back
                    // to 4 m at knee height it went into the steps behind him on Eskinita; it drifts sideways instead.)
                    eye = SnFistsOnCourt - dir * 3.2f + side * (2.3f + .6f * u) + Vector3.up * (.35f + .15f * u);
                    look = _snSpot - side * .5f + Vector3.up * (2.2f + .2f * u);
                    float far = (new Vector3(_snSpot.x, 0f, _snSpot.z) - new Vector3(eye.x, 0f, eye.z)).magnitude;
                    fov = Mathf.Clamp(2f * Mathf.Atan(4.8f / Mathf.Max(1f, far)) * Mathf.Rad2Deg, 38f, 58f) + 2f * u;
                    eye = KeepLensOffOthers(eye, 1.3f);
                    break;
                }
            }
            // The slam and the rise knock the lens; the court's rumble grows under B2 as the bulge swells.
            float knock = Flash(t, SnSlam, .12f) * .035f + Flash(t, SnRise + .05f, .2f) * .09f;
            if (index == 3 && t < SnRise) knock += .028f * Ease(SnBulge, SnRise, t);
            if (knock > 0) eye += new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 71f), 0) * knock;
        }

        /// <summary>
        /// Of two places for the lens, the one whose line of sight to <paramref name="target"/> passes furthest from every staged
        /// player (film r2: a player stood between B2's lens and the spot and hid the whole birth). Chosen on the players'
        /// standing places, so it never flips during a shot.
        /// </summary>
        private Vector3 SnClearOf(Vector3 target, Vector3 a, Vector3 b)
        {
            float Clear(Vector3 eye)
            {
                float worst = float.MaxValue;
                foreach (var o in _others)
                {
                    var p = o.Home + Vector3.up * .9f; var seg = target - eye;
                    float k = Mathf.Clamp01(Vector3.Dot(p - eye, seg) / Mathf.Max(.01f, seg.sqrMagnitude));
                    worst = Mathf.Min(worst, (eye + seg * k - p).magnitude);
                }
                return worst;
            }
            return Clear(b) > Clear(a) + .3f ? b : a;
        }

        /// <summary>The street steps back a little under his sky (`SeanSkyGrade` does the red), flares on the slam and the rise, and
        /// is the street exactly on the last frame.</summary>
        private void SeanGrade(float t, out float brightness, out float saturation)
        {
            float on = Ease(0, .25f, t) * (1 - Ease(SnHome, Seconds - .03f, t));
            brightness = Mathf.Lerp(1f, .93f, on) * (1 + .15f * Flash(t, SnSlam, .1f) + .22f * Flash(t, SnRise + .05f, .14f));
            saturation = Mathf.Lerp(1f, .96f, on);
        }

        /// <summary>
        /// ⚠️ HIS OWN SCREEN COMES HOME TO HIS OWN EYES (kept from the parol's cutscene, owner 2026-10-09). From `SnHome` the lens
        /// leaves the last shot for where his first-person camera will be the moment play resumes: the live camera's own place
        /// and aim (the world is paused). His copy is not drawn once the lens is nearly in it. World positions.
        /// </summary>
        private void SeanHandBack(float t, Camera live, ref Vector3 eye, ref Vector3 target, ref float fov)
        {
            float home = Ease(SnHome, Seconds - .06f, t);
            // ⚠️ SET BOTH WAYS, EVERY CALL. The phase samples each shot at its end to judge it (`UltimatePhaseView.ChooseShot`):
            // the last one's end is past `SnHome`, and a one-way "off" hid his copy for the whole cutscene in a match (film v5).
            bool drawn = home <= .7f;
            foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = drawn;
            if (!drawn) foreach (var f in _snFists) if (f.Model != null) f.Model.gameObject.SetActive(false);
            if (home <= 0) return;
            var eyes = live.transform.position;
            eye = Vector3.Lerp(eye, eyes, home);
            target = Vector3.Lerp(target, eyes + live.transform.forward * 8f, home);
            fov = Mathf.Lerp(fov, live.fieldOfView, home);
        }

        /// <summary>Frees what this stage made that the root does not own: the staged volcano's paved and copied meshes, the
        /// eruption's clouds, this stage's own meshes.</summary>
        private void SeanDispose()
        {
            _snLook?.Dispose(); _snLook = null;
            _snErupt?.Dispose(); _snErupt = null;
            _snSlamDust?.Dispose(); _snSlamDust = null;
            SeanSkyDispose();
            if (_snLavaFlow != null) ObjectDestroy(_snLavaFlow);
            if (_snLavaPool != null) ObjectDestroy(_snLavaPool);
            _snLavaFlow = _snLavaPool = null;
            foreach (var c in _snVents) c?.Dispose();
            _snVents.Clear();
            foreach (var m in _snMeshes) if (m != null) ObjectDestroy(m);
            _snMeshes.Clear();
        }

        /// <summary>An unlit backdrop piece, so the court's light never greys it. Cheska's stage uses it.</summary>
        private void RagoUnlitBackdrop(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var piece = _pieces[index];
            var material = new Material(shader) { name = "Rago dusk backdrop", color = piece.Color };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }
    }
}
