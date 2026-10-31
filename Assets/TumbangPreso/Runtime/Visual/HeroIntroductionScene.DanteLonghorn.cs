using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // DANTE, CONTINENTAL DRIFT: THE LONGHORN AND THE SUIT (2026-10-29). The owner: *"we're keeping the shields and body
        // armor but we're applying the old long horn cutscene we had. maybe improve the design on that tho"*, then *"yes, so
        // basically we're merging the armor thing that astra brought + the longhorn cutscene"*.
        //
        // One sentence: the mountain wakes behind him, its fire becomes his armour, and the two of them bring their fists down
        // together; the ground they struck rises at the players.
        //
        // WHAT IS WHOSE. The longhorn is ours (p11, kept as text in tools/cutscene_rework/old/pre-astra-merge-2026-10-18 and in
        // git at d971877ec), redrawn as a bull's bust standing up out of the court (`Models/UltimateProps/longhorn-titan`,
        // `tools/build_ultimate_props_dante_titan.py`, which says what changed and why). The SUIT is ASTRA's MOUNTAIN FORM,
        // built and stepped by ASTRA's own `DanteMountainArmour` exactly as play wears it; nothing here restyles a piece of it.
        // The end is ASTRA's v8 end: the first band heaves at his fists and the cut comes `GeoRules.DriftCutsceneTail` later,
        // with play's own cascade (`DanteDriftLive`) on the live lane, so play's `DanteDriftWave` carries on from that frame.
        //
        // ⚠️ THE SECOND STAGING (film l1, 2026-10-29). The first one stood the longhorn 2 m behind him, so its chest was against his
        // back: from the front he was a dark shape inside its molten V for three seconds and the suit forming, the whole point of
        // the merge, could not be seen; its fists filled two shots. It now stands 3.6 m back at 0.8 size, so it TOWERS behind him with clear
        // court between them, and every shot of the suit is framed on HIM with its face over his head. Its fists come down
        // beside him, never between him and the lens.
        //
        // AUTHORED ON 4.86 s (the beats below). The cutscene's real length is the table's (`tools/author_ultimate_intros.py`,
        // `LONGHORN_SECONDS`): everything up to the slam is stretched or squeezed to fit (`DlAuthored`), and the slam to the cut
        // stays as it is, because that tail is play's (`GeoRules.DriftCutsceneTail`). Change that ONE constant to change the length.
        // ⚠️ WHAT 3.61 s (ASTRA's length) WOULD COST: every beat before the slam runs at 0.72 speed. The rise goes from 0.95 s to
        // 0.69, the gift from 1.58 s to 1.14 (its six pieces land 0.16 s apart again, the l1 fault of a suit that arrives as one
        // shudder), the load and hang from 1.5 s to 1.08. A hand-cut 3.61 would rather drop most of the hang and keep the gift.
        //
        //   0.30 THE CALL          both fists into the court: a crack star under each, cracks racing back from them behind him
        //   0.35 THE MOUNTAIN WAKES the court 3.6 m behind him heaves three times and the longhorn shoulders up out of it, horns
        //                          first; its eyes light at 1.05
        //   1.32 THE GIFT          it roars and plants its fists behind him either side; he stands up into it, braced, and its
        //                          own stone tears off it a piece at a time and is flung onto him on a trail of fire, each from
        //                          the part of it that matches the piece of ASTRA's suit that locks as it lands: fists 1.62,
        //                          bracers 1.90, chest 2.12, shoulders 2.34, legs 2.56, the stone horn 2.72, whole at 2.90
        //   3.00 THE LOAD          both of them wind both fists overhead; the hang trembles from 3.45, the world dims under it
        //   4.50 THE SLAM          four fists into the court at once, its two landing either side of his: impact frames, rings
        //   4.58 THE GROUND RISES  the first band heaves; the longhorn sinks back into the court; cut at 4.86
        //
        // SIX SHOTS, all on his right: the call; the rise (wide, him at the bottom); the gift (on him, its face over him); the load
        // (CONTINUES from the gift's last frame and pulls back, never a cut: owner on l1, "the jump from getting armor to getting
        // on the longhorn is also weird"; he never leaves the court in front of it); the slam (in front, both of them); a cut on
        // the band's heave to play's hand-back frame. The longhorn never touches him: the film logs its stone's distance from his
        // body every frame (`PaeteAbilityFilm.Intro`, `clear_dante_<tag>.txt`; maplh6: 0.115 m at the closest, 2.27 s).
        //
        // RULES (the method's traps): nothing here runs on `Update`; everything is posed from the scene clock; flat effects sit on
        // the court (`_dnCourt`); the hottest colour is (1, .62, .18), never white; stone never fades, it grows, shrinks or sinks.
        // =========================================================================================

        /// <summary>
        /// ⚠️ THE SWITCH. On: the longhorn cutscene above with ASTRA's armour in it. Off: ASTRA's v8 (THE CHARGE-UP and THE LEAP,
        /// `HeroIntroductionScene.Dante.cs`), untouched. It also picks the table (`UltimatePerformance.For`): the longhorn's length
        /// is shared phase timing (`SkillContractFingerprint` carries it), so every peer must be built with the same value.
        /// </summary>
        public static bool DanteLonghornCut = true;

        // The beats, on the AUTHORED 4.86 s clock (see `DlAuthored`).
        private const float DlPlant = .30f, DlRiseFrom = .35f, DlRisen = 1.30f, DlEyes = 1.05f, DlStone = 1.32f, DlFormed = 2.90f,
                            DlLoad = 3.00f, DlLoaded = 3.45f, DlSlam = 4.50f, DlBand0 = 4.58f, DlEnd = DlBand0 + Core.GeoRules.DriftCutsceneTail;
        /// <summary>Its three shoves up out of the court: (start, end, share of the climb).</summary>
        private static readonly (float from, float to, float share)[] DlShoves = { (.35f, .62f, .36f), (.68f, .95f, .34f), (1.00f, 1.30f, .30f) };
        /// <summary>
        /// ASTRA's suit's own beats (`DanteMountainArmour`: the first flight starts .58, fists .84, bracers 1.14, chest 1.28,
        /// shoulders 1.44, legs 1.62, horn 1.70, whole at `FormedAt` 2.0) and when each lands HERE. ASTRA's beats come 0.14 to 0.3 s
        /// apart, which at the size he is on screen read as one shudder (film l1); here each gets at least 0.16 s of its own, with a
        /// stream of the longhorn's fire arriving with it.
        /// </summary>
        private static readonly float[] DlSuitAt = { 0f, .56f, .84f, 1.14f, 1.28f, 1.44f, 1.62f, 1.70f, DanteMountainArmour.FormedAt };
        private static readonly float[] DlSuitHere = { 0f, DlStone, 1.62f, 1.90f, 2.12f, 2.34f, 2.56f, 2.72f, DlFormed };
        /// <summary>Where it stands: this far behind him, and this far under the court before it wakes (its horn tips stand at 5.0),
        /// at this size of the model (film l2: at full size, 6.3 m to its horn tips, it was seven of him tall and every shot that
        /// held both of them was either all longhorn or a speck of him; at 0.8 it is five of him, and still towers).</summary>
        private const float DlBack = -3.6f, DlBuried = -5.4f, DlSize = .8f;
        // Its arms (`arm-l`, `arm-r`) hinge here in its own space and hang 3.4 m to the middle of the fist. At 57 degrees forward,
        // with the body leant 6 degrees into the blow, the fist's foot is in the court 0.7 m behind him and 2.05 m to his side.
        private static readonly Vector3 DlShoulder = new Vector3(2.05f, 2.55f, .55f);
        private const float DlArmLength = 3.4f, DlLanded = -57f, DlLean = 6f;
        // The light in its seams, in its body's space: the molten V on its chest and the seam down each flank.
        private static readonly Vector3[] DlSeams =
        {
            new Vector3(-.66f, 2.05f, 1.66f), new Vector3(.66f, 2.05f, 1.66f), new Vector3(0f, 1.52f, 1.66f),
            new Vector3(-1.94f, 1.05f, .34f), new Vector3(1.94f, 1.05f, .34f),
        };
        // Its eyes and mouth, in its HEAD's space (the builder's grown numbers less the neck hinge, turnaround t4).
        private static readonly Vector3 DlEye = new Vector3(.82f, 1.19f, 1.18f), DlMouth = new Vector3(0f, -.09f, 1.06f);
        // The cracks back from his fists to where it rises: (degrees off straight back, how far behind him they end).
        private static readonly float[] DlCrackAngles = { -48f, -22f, -6f, 6f, 22f, 48f };
        // THE GIFT (concept B in ROUNDS.md): for each piece of his suit, a chunk of ITS stone tears off the matching part of it (its
        // knuckles for his fists, its forearms for his bracers, its chest for his, its shoulder boulders for his, the court at its
        // fists for his legs, its brow for his horn) and flies onto him on a fire trail, landing as the piece locks. Two a piece.
        private const int DlGifts = 12, DlTrail = 3;
        private const float DlGiftFlight = .4f;
        private static readonly Vector2[] DlGiftProfile = { new Vector2(.02f, -.5f), new Vector2(.42f, -.42f), new Vector2(.5f, -.05f), new Vector2(.44f, .3f), new Vector2(.24f, .48f), new Vector2(.02f, .5f) };

        // ⚠️ ITS PARTS (owner on film l1: *"the longhorn honestly looks so rigid and stiff, did you take the time to animate it
        // properly?"*): the builder splits it into a body, a breathing chest, two shoulder boulders, three joints an arm, a head with
        // a jaw and two horns. Index 0 is its left (-x), 1 its right.
        private readonly Transform[] _dlFore = new Transform[2], _dlFist = new Transform[2], _dlRock = new Transform[2], _dlHorn = new Transform[2];
        private Transform _dlChest;
        private Vector3[] _dlSuitRest;
        private bool[] _dlSuitHid;
        private const float DlFistDrop = .55f;
        private Transform _dlBeast, _dlArmL, _dlArmR, _dlHead, _dlJaw;
        private bool _dlModelled;
        private float _dlStretch = 1f;
        private int _dlEyeL, _dlEyeR, _dlMouthGlow, _dlFistGlowL, _dlFistGlowR, _dlUnderGlow, _dlRubble, _dlSlamStar, _dlSlamRing, _dlSlamRingOuter,
                    _dlCrater, _dlCraterGlow, _dlBeastStarL, _dlBeastStarR, _dlBeastRingL, _dlBeastRingR, _dlTakeGlow, _dlLockFlash;
        private readonly List<int> _dlPlantStars = new List<int>(2), _dlCracks = new List<int>(6), _dlSeamLights = new List<int>(5),
                                   _dlRiseDust = new List<int>(7), _dlShed = new List<int>(8), _dlFlow = new List<int>(DlGifts * DlTrail),
                                   _dlGift = new List<int>(12), _dlTear = new List<int>(12), _dlGiftHeat = new List<int>(12), _dlHeat = new List<int>(2), _dlSlamDust = new List<int>(9), _dlShards = new List<int>(8), _dlSpray = new List<int>(12);
        private readonly Vector3[] _dlPlantAt = new Vector3[2];
        private bool _dlPlanted;
        private Vector3? _dlSlamAt;
        private Material _dlImpact;
        private FxStudyDanteFireWave _dlWave;
        private readonly List<(FxCloud cloud, Transform holder, float born, int side, int kind)> _dlPuffs = new List<(FxCloud, Transform, float, int, int)>();
        private int _dlSlamPuffFirst = -1;
        private const int DlRiseCloud = 0, DlSlamCloud = 1;
        // The emitters, made from the cloud's own `Puff` (its column's look, on for a moment), only the timing and reach set here.
        private static readonly FxCloud.Emitter DlRisePuff = DlEmitter(.3f, .9f, 1.3f, .26f), DlSlamPuff = DlEmitter(.3f, .6f, .7f, .28f);
        private static FxCloud.Emitter DlEmitter(float on, float life, float rise, float spread)
        {
            var e = FxCloud.Emitter.Puff(Vector3.zero, new Vector3(0f, rise, 0f), 0f, on, life);
            e.Widen = .45f; e.Weave = .14f; e.Spread = spread;
            return e;
        }

        /// <summary>One puff of the bench's cloud under a holder of its own (placed, turned to `way`, scaled to `size`), off until
        /// `born`. `side` (0 its left, 1 its right) puts it behind that fist as it hauls. Eskinita's court dust: its lit tone sampled
        /// off the court in film maplh13 (about 0.72, 0.58, 0.47), with a shade and a line a step darker and warmer each.</summary>
        private void DlPuff(Vector3 at, Vector3 way, float born, float size, int seed, int kind, int side = -1)
        {
            var holder = new GameObject("LonghornDust" + seed).transform;
            holder.SetParent(_root.transform, false);
            holder.localPosition = at; holder.localRotation = Quaternion.FromToRotation(Vector3.up, way.normalized); holder.localScale = Vector3.one * size;
            var cloud = FxCloud.Build(holder, "cloud", seed, 14, new Color(.74f, .58f, .45f), new Color(.55f, .41f, .31f), new Color(.37f, .26f, .19f), FxStudyCloud.Line);
            if (cloud.Piece.Material != null) cloud.Piece.Material.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
            cloud.Piece.Go.SetActive(false);
            if (kind == DlSlamCloud && _dlSlamPuffFirst < 0) _dlSlamPuffFirst = _dlPuffs.Count;
            _dlPuffs.Add((cloud, holder, born, side, kind));
        }

        private void DlPosePuffs(float t)
        {
            foreach (var (cloud, holder, born, side, kind) in _dlPuffs)
            {
                var e = kind == DlSlamCloud ? DlSlamPuff : DlRisePuff;
                float a = t - born;
                // Kicked up where its fist really is on the frame it hauls, a little behind it (so the fist is in front of it).
                if (side >= 0 && a >= 0f && a < .05f)
                {
                    // Where its fist is hauling, but never inside the collar of broken court round its base (maplh14: placed at the
                    // fist itself, every puff was hidden in the collar for its whole life).
                    var fist = DlBeastFist(side == 0 ? -1 : 1);
                    float x = Mathf.Sign(fist.x == 0f ? (side == 0 ? -1f : 1f) : fist.x) * Mathf.Max(Mathf.Abs(fist.x) * 1.1f, 2.3f);
                    holder.localPosition = new Vector3(x, DnFloat + .05f, Mathf.Max(fist.z, DlBack + 1.7f));
                }
                bool on = a >= 0f && a <= e.To + e.Life && !_reducedEffects;
                if (cloud.Piece.Go.activeSelf != on) cloud.Piece.Go.SetActive(on);
                if (on) cloud.Pose(a, e);
            }
        }
        /// <summary>Frees the wave's own meshes and materials (`HeroIntroductionScene.Dispose` calls it for Dante).</summary>
        private void LonghornDispose()
        {
            _dlWave?.Dispose(); _dlWave = null;
            foreach (var puff in _dlPuffs) puff.cloud.Dispose();
            _dlPuffs.Clear();
        }

        // ------------------------------------------------------------------ the clocks

        /// <summary>
        /// The scene's clock on the authored 4.86 s one. The table's length (`Seconds`) decides the real one: the call, the rise, the
        /// suit and the load are stretched together by one factor; the slam to the cut is play's tail and is never stretched.
        /// </summary>
        private float DlAuthored(float t)
        {
            float slam = DlSlam * _dlStretch;
            return t < slam ? t / _dlStretch : t - slam + DlSlam;
        }

        /// <summary>ASTRA's suit on this cutscene's clock: each of its beats lands on ours (`DlSuitHere`), then the clock runs on so
        /// the last frame is exactly `DanteMountainArmour.CutEndAt`, the second play's suit starts from.</summary>
        private static float DlArmourClock(float t)
        {
            if (t >= DlFormed) return Mathf.Lerp(DanteMountainArmour.FormedAt, DanteMountainArmour.CutEndAt, Mathf.Clamp01((t - DlFormed) / (DlEnd - DlFormed)));
            for (int i = 1; i < DlSuitHere.Length; i++)
                if (t <= DlSuitHere[i]) return Mathf.Lerp(DlSuitAt[i - 1], DlSuitAt[i], Mathf.InverseLerp(DlSuitHere[i - 1], DlSuitHere[i], t));
            return DanteMountainArmour.FormedAt;
        }

        // ------------------------------------------------------------------ build

        private void BuildLonghorn()
        {
            float court = Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y;
            _dnCourt = float.IsInfinity(court) || float.IsNaN(court) ? 0f : Mathf.Clamp(court, 0f, .6f);
            // The table's length against the authored one (see `DlAuthored`).
            _dlStretch = Mathf.Max(.3f, (Seconds - (DlEnd - DlSlam)) / DlSlam);
            var molten = new Color(1f, .45f, .09f);
            var dust = new Color(.56f, .47f, .39f, 1f);
            var basalt = new Color(.27f, .23f, .21f, 1f);

            // THE CALL.
            for (int f = 0; f < 2; f++) _dlPlantStars.Add(AddGlow("LonghornPlantStar" + f, molten, billboard: false, falloff: 1.6f, core: .6f));
            for (int i = 0; i < DlCrackAngles.Length; i++)
                _dlCracks.Add(AddGlow("LonghornCrack" + i, new Color(1f, .5f, .1f), billboard: false, band: true, falloff: 1.7f, core: .7f));

            // THE LONGHORN, dressed as the cast is, with a heavier ink line than a person wears because it is six metres tall.
            var model = UltimateProp.Spawn("longhorn-titan", _root.transform, UltimateProp.EarthPalette, ToonSkin.PersonOutlineWidth * 3f);
            _dlModelled = model != null;
            _dlBeast = model != null ? model.transform : new GameObject("Longhorn").transform;
            if (model == null) _dlBeast.SetParent(_root.transform, false);
            else
            {
                model.name = "Longhorn";
                _dlArmL = PaeteProp.Find(model, "arm-l"); _dlArmR = PaeteProp.Find(model, "arm-r");
                _dlHead = PaeteProp.Find(model, "head"); _dlJaw = PaeteProp.Find(model, "jaw");
                _dlChest = PaeteProp.Find(model, "chest");
                for (int s = 0; s < 2; s++)
                {
                    string side = s == 0 ? "-l" : "-r";
                    _dlFore[s] = PaeteProp.Find(model, "forearm" + side); _dlFist[s] = PaeteProp.Find(model, "fist" + side);
                    _dlRock[s] = PaeteProp.Find(model, "shoulder" + side); _dlHorn[s] = PaeteProp.Find(model, "horn" + side);
                }
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            Transform Joint(Transform found, string name, Vector3 at)
            {
                if (found != null) return found;
                var go = new GameObject(name).transform; go.SetParent(_dlBeast, false); go.localPosition = at; return go;
            }
            _dlArmL = Joint(_dlArmL, "LonghornArmLeft", new Vector3(-DlShoulder.x, DlShoulder.y, DlShoulder.z));
            _dlArmR = Joint(_dlArmR, "LonghornArmRight", DlShoulder);
            _dlHead = Joint(_dlHead, "LonghornHead", new Vector3(0f, 3.10f, .90f));

            var ember = new Color(1f, .5f, .1f, .95f);
            _dlEyeL = AddGlow("LonghornEyeLeft", ember, falloff: 2.2f, core: .5f, lift: .15f);
            _dlEyeR = AddGlow("LonghornEyeRight", ember, falloff: 2.2f, core: .5f, lift: .15f);
            _dlMouthGlow = AddGlow("LonghornMouthHeat", new Color(1f, .4f, .08f, 1f), falloff: 1.8f, core: .4f, lift: .5f);
            foreach (int g in new[] { _dlEyeL, _dlEyeR, _dlMouthGlow }) _pieces[g].Transform.SetParent(_dlHead, false);
            _dlFistGlowL = AddGlow("LonghornFistHeatLeft", new Color(1f, .45f, .08f, 1f), falloff: 1.6f, core: .35f, lift: 1.1f);
            _dlFistGlowR = AddGlow("LonghornFistHeatRight", new Color(1f, .45f, .08f, 1f), falloff: 1.6f, core: .35f, lift: 1.1f);
            _pieces[_dlFistGlowL].Transform.SetParent(_dlFist[0] ?? _dlArmL, false); _pieces[_dlFistGlowR].Transform.SetParent(_dlFist[1] ?? _dlArmR, false);
            for (int i = 0; i < DlSeams.Length; i++)
            {
                int light = AddGlow("LonghornSeamLight" + i, new Color(1f, .46f, .09f, 1f), falloff: 1.5f, core: .9f, lift: .45f);
                _pieces[light].Transform.SetParent(_dlBeast, false);
                _dlSeamLights.Add(light);
            }

            // What comes up with it: the collar of court it shoulders aside, dust rolling out, rock shed off its shoulders, the
            // light under it.
            _dlRubble = DanteStone("LonghornRubble", DlTwoSided(VfxShapes.Upheaval(11, .1f, .5f, 47, .1f)), new Color(.15f, .13f, .12f, 1f));
            // ⚠️ THE DUST IS THE BENCH'S CLOUD (`FxCloud`, the owner's "looks good" of 2026-10-11, the smoke on F12), called as it is
            // with this cutscene's own inputs (`DlPuff`). The owner on maplh8, circling the faceted grey balls that rolled out round
            // its base: "why is it using this smoke? we already have cloud smoke to use". One puff on each heave round its base.
            // maplh13 (coordinator): seven big pale balls sitting on the court round its base were the loudest thing on screen. Now
            // four, small, kicked up off the court where its FISTS haul it out (its left on the first and third heave, its right
            // on the second and third), just behind them, and drifting up and out; Eskinita's own court dust in tone.
            for (int i = 0; i < 4; i++)
            {
                int heave = i < 2 ? i : 2, side = i == 0 || i == 2 ? 0 : 1;
                DlPuff(Vector3.zero, new Vector3(side == 0 ? -.8f : .8f, 1f, -.3f), DlShoves[heave].from + .1f, .7f, 90 + i, DlRiseCloud, side);
            }

            _dlUnderGlow = AddGlow("LonghornUnderLight", new Color(1f, .4f, .06f, 1f), billboard: false, falloff: 1.5f, core: .3f);

            // THE FIRE: its streams into him, a glow on him where they land, and a flare as each piece of the suit locks.
            for (int i = 0; i < DlGifts * DlTrail; i++) _dlFlow.Add(AddGlow("LonghornFlow" + i, new Color(1f, .58f, .16f), falloff: 2.4f, core: .7f));
            for (int i = 0; i < DlGifts; i++)
            {
                // A rough lump, never a spire (maplh8: a five-sided spire with its heat behind it showed one clean glowing facet to the
                // lens, an orange triangle with a dark edge that read as a video play button).
                _dlGift.Add(DanteStone("LonghornGift" + i, DlTwoSided(FxStudyKit.Lump(DlGiftProfile, 300 + i * 1.7f, .45f, 14, .3f)), basalt));
                _dlTear.Add(AddGlow("LonghornTear" + i, new Color(1f, .5f, .12f), falloff: 1.8f, core: .5f, lift: .4f));
                _dlGiftHeat.Add(AddGlow("LonghornGiftHeat" + i, new Color(1f, .55f, .14f), falloff: 1.6f, core: .55f, lift: .2f));
            }
            _dlTakeGlow = AddGlow("LonghornTakeGlow", new Color(1f, .5f, .12f), falloff: 1.8f, core: .4f, lift: .3f);
            _dlLockFlash = AddGlow("LonghornLockFlash", new Color(1f, .6f, .18f), falloff: 2.0f, core: .6f, lift: .3f);
            // THE LOAD: his two fists run hot as they are held.
            for (int f = 0; f < 2; f++) _dlHeat.Add(AddGlow("LonghornHisFistHeat" + f, new Color(1f, .5f, .12f), falloff: 2.2f, core: .8f));

            // THE SLAM.
            _dlSlamStar = AddGlow("LonghornSlamStar", molten, billboard: false, falloff: 1.4f, core: .6f);
            _dlSlamRing = AddGlow("LonghornSlamRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            _dlSlamRingOuter = AddGlow("LonghornSlamRingOuter", new Color(1f, .42f, .08f), DnRingMesh, billboard: false, band: true, falloff: 1.4f, core: .3f);
            _dlCrater = Add("LonghornCrater", VfxShapes.Splat(13, .34f, 611), new Color(.13f, .09f, .07f, .92f), .05f, plain: true);
            _dlCraterGlow = AddGlow("LonghornCraterGlow", new Color(1f, .45f, .09f), billboard: false, falloff: 1.4f, core: .55f);
            _dlBeastStarL = AddGlow("LonghornFistStarLeft", molten, billboard: false, falloff: 1.4f, core: .6f);
            _dlBeastStarR = AddGlow("LonghornFistStarRight", molten, billboard: false, falloff: 1.4f, core: .6f);
            _dlBeastRingL = AddGlow("LonghornFistRingLeft", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            _dlBeastRingR = AddGlow("LonghornFistRingRight", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            // The slam's wave of fire (see THE SLAM in `SampleLonghorn`), off until the blow.
            _dlWave = new FxStudyDanteFireWave(_root.transform);
            _dlWave.Root.gameObject.SetActive(false);
            // The slam's dust, the same cloud: one at his fists and one at each of its fists, thrown out low.
            for (int i = 0; i < 3; i++) DlPuff(Vector3.zero, new Vector3(0f, .5f, 0f), DlSlam + .01f * i, .55f, 220 + i, DlSlamCloud);
            for (int i = 0; i < 8; i++) _dlShards.Add(DanteStone("LonghornShard" + i, DlTwoSided(VfxShapes.Spire(4, .3f, .4f, 150 + i)), basalt));
            for (int i = 0; i < 12; i++) _dlSpray.Add(AddGlow("LonghornSpray" + i, new Color(1f, .58f, .16f), falloff: 2.4f, core: .7f));

            // Play's own cascade, on the live lane, measured exactly as the wave will measure it (ASTRA's, as v8 builds it).
            var origin = _source != null ? _source.transform.position : _root.transform.position;
            Abilities.DanteDriftWave.Lane(origin, _root.transform.forward, out float halfWidth, out float reach);
            _dnLane = DanteDriftLive.BuildStage(_root.transform, _dnCourt, halfWidth, reach);
            _dnLane.gameObject.SetActive(false);
            // ASTRA's suit, on his copy (`HeroIntroductionScene.cs` adds its renderers to the capture through `_dnArmour`).
            if (_bodyRoot != null) _dnArmour = DanteMountainArmour.Build(_bodyRoot, _root.transform);
            // The real players, where they stand: the last shot looks down the lane at them.
            StageOthers(0f, 1000f);
        }

        // ------------------------------------------------------------------ the clock helpers

        /// <summary>
        /// ⚠️ EVERY ROCK HERE IS SOLID FROM EVERY SIDE (owner on maplh13, of the slabs round its base: "the rock effects at the
        /// bottom of the longhorn have culled faces"). The shared shapes (`VfxShapes.Upheaval`, `Spire`) are open shells meant
        /// to be seen from outside, and the lit shader culls back faces, so from a low lens the inside of a slab drew as nothing
        /// and the slab as a dark sliver. This copy carries every triangle twice, the second wound the other way with its
        /// normal turned round, so the inside is lit stone too. The shared builders are not touched.
        /// </summary>
        private static Mesh DlTwoSided(Mesh m)
        {
            var v = m.vertices; var n = m.normals; var uv = m.uv; var c = m.colors; var tris = m.triangles;
            int count = v.Length;
            var v2 = new Vector3[count * 2]; var n2 = new Vector3[count * 2];
            v.CopyTo(v2, 0); v.CopyTo(v2, count);
            if (n == null || n.Length != count) { m.RecalculateNormals(); n = m.normals; }
            for (int i = 0; i < count; i++) { n2[i] = n[i]; n2[count + i] = -n[i]; }
            var t2 = new int[tris.Length * 2];
            tris.CopyTo(t2, 0);
            for (int i = 0; i < tris.Length; i += 3)
            { t2[tris.Length + i] = tris[i] + count; t2[tris.Length + i + 1] = tris[i + 2] + count; t2[tris.Length + i + 2] = tris[i + 1] + count; }
            var both = new Mesh { name = m.name + "-TwoSided" };
            both.vertices = v2; both.normals = n2;
            if (uv != null && uv.Length == count) { var uv2 = new Vector2[count * 2]; uv.CopyTo(uv2, 0); uv.CopyTo(uv2, count); both.uv = uv2; }
            if (c != null && c.Length == count) { var c2 = new Color[count * 2]; c.CopyTo(c2, 0); c.CopyTo(c2, count); both.colors = c2; }
            both.triangles = t2;
            both.RecalculateBounds();
            if (Application.isPlaying) Object.Destroy(m); else Object.DestroyImmediate(m);
            return both;
        }

        private static float DlOut(float u) { u = Mathf.Clamp01(u); return 1f - (1f - u) * (1f - u); }
        /// <summary>A thing that swells fast and dwindles over an age of 0 to 1: opaque dust has no fade, so it has this.</summary>
        private static float DlPop(float age) => age <= 0f || age >= 1f ? 0f : age < .25f ? age / .25f : 1f - (age - .25f) / .75f;
        private static float DlHash(float n) { float s = Mathf.Sin(n * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }

        /// <summary>How far up out of the court it has come, 0 to 1, in three shoves.</summary>
        private static float DlClimb(float t)
        {
            float u = 0f;
            foreach (var s in DlShoves) u += s.share * Ease(s.from, s.to, t);
            return u;
        }

        /// <summary>A blow's ring: zero before <paramref name="at"/>, then a decaying wobble (follow-through and settle).</summary>
        private static float DlRing(float t, float at, float hz, float decay) { float a = t - at; return a < 0f ? 0f : Mathf.Exp(-decay * a) * Mathf.Sin(a * hz * 6.2832f); }
        /// <summary>0, up to 1 and back to 0 over [from, to]: an anticipation, a toss, a grunt.</summary>
        private static float DlBump(float t, float from, float to) => t <= from || t >= to ? 0f : Mathf.Sin((t - from) / (to - from) * Mathf.PI);

        /// <summary>How far up out of the court it has come, with weight: it sinks a little before each heave (anticipation),
        /// overshoots the heave and settles back (follow-through).</summary>
        private static float DlClimbWeighted(float t)
        {
            float u = 0f;
            foreach (var s in DlShoves)
                u += s.share * Ease(s.from, s.to, t) - .035f * DlBump(t, s.from - .14f, s.from + .04f) + .03f * s.share * 3f * DlRing(t, s.to, 2.4f, 7f);
            return u;
        }

        /// <summary>
        /// One arm's swing at the shoulder about x (negative is forward and up). It hauls itself out of the court one arm per shove
        /// (its left on the first and third, its right on the second), thumps its fists down either side of him, presses on each
        /// piece of his suit as it locks, pushes down before it flings both up (anticipation), heaves past the hold, draws back
        /// further still, and the blow, which gets faster all the way down because the court is what stops it.
        /// </summary>
        private float DlArmSwing(int side, float t, out float tremble)
        {
            // Planted at -42: the middle of each fist at the court 1.4 m behind him and 2 m to his side, so its fists frame him from
            // BEHIND (film l2: at -50 they came forward level with him and the near one filled the left of the frame).
            // ⚠️ maplh6 (owner): its forearms lay on the court and read as rubble. Planted is now the upper arm hanging nearly
            // straight down (-20) with the elbow bent hard (`DlElbow`, -50): the forearm stands up off the court at 70 degrees
            // and only the knuckles touch it, 1.3 m behind him and 1.6 m to his side.
            float clawing = -26f, planted = -20f, raised = -165f;
            float swing = Mathf.Lerp(clawing, planted, Ease(DlStone - .1f, DlStone + .22f, t)) + 4f * DlRing(t, DlStone + .22f, 2.2f, 6f);
            for (int i = 0; i < DlShoves.Length; i++)
                if (i % 2 == side) swing -= 14f * DlBump(t, DlShoves[i].from - .08f, DlShoves[i].to + .05f);
            for (int i = 2; i < DlSuitHere.Length - 1; i++) swing += 2.5f * DlBump(t, DlSuitHere[i] - .04f, DlSuitHere[i] + .12f);
            swing += 9f * DlBump(t, DlLoad - .14f, DlLoad + .1f);
            swing = Mathf.Lerp(swing, raised, Ease(DlLoad + .02f, DlLoaded, t));
            swing -= Mathf.Sin(Mathf.Clamp01((t - DlLoaded + .05f) / .3f) * Mathf.PI) * 8f + Ease(DlSlam - .3f, DlSlam - .1f, t) * 10f;
            // The hang is a STRAIN, never a freeze (film l3: 0.9 s of one still pose read as a paused film): a tremble that grows
            // the longer it is held, and a slow heave of both arms back a little further on every breath.
            float holding = Ease(DlLoaded, DlLoaded + .2f, t) * (1f - Ease(DlSlam - .1f, DlSlam - .09f, t));
            tremble = _reducedEffects ? 0f : (Mathf.Sin(t * 33f + side) * (1.6f + 2.4f * Ease(DlLoaded, DlSlam - .1f, t)) + 3f * Mathf.Sin(t * 4.2f)) * holding;
            float fall = Mathf.Clamp01((t - (DlSlam - .1f)) / .1f); fall *= fall;
            // The court stops the fist and the arm rebounds off it.
            return Mathf.Lerp(swing + tremble, DlLanded, fall) - 3f * DlRing(t, DlSlam, 2.6f, 7f);
        }

        /// <summary>The elbow (negative folds the fist forward of the upper arm, which overhead puts it behind the head). Its own pose
        /// plus DRAG: the forearm trails the shoulder's motion a beat, so a fast swing whips through the elbow.</summary>
        private float DlElbow(int side, float t)
        {
            float bend = Mathf.Lerp(-40f, -50f, Ease(DlStone - .1f, DlStone + .22f, t));
            for (int i = 0; i < DlShoves.Length; i++)
                if (i % 2 == side) bend += 22f * DlBump(t, DlShoves[i].from - .08f, DlShoves[i].from + .12f) - 26f * DlBump(t, DlShoves[i].from + .05f, DlShoves[i].to + .1f);
            bend = Mathf.Lerp(bend, -38f, Ease(DlLoad, DlLoaded, t));
            bend = Mathf.Lerp(bend, -72f, Ease(DlSlam - .32f, DlSlam - .1f, t));
            // The whip: the forearm straightens a beat after the shoulder starts down, and over-straightens on the hit.
            float snap = Mathf.Clamp01((t - (DlSlam - .07f)) / .07f); snap *= snap;
            bend = Mathf.Lerp(bend, 0f, snap) + 7f * DlRing(t, DlSlam, 3f, 8f);
            float drag = -.3f * (DlArmSwing(side, t, out _) - DlArmSwing(side, t - .07f, out _));
            return bend + Mathf.Clamp(drag, -25f, 25f) * (1f - snap);
        }

        /// <summary>Its fist's wrist and squeeze: cocked back to lead with the knuckles on the wind-up, flicked through on the hit;
        /// open on the court, clenched and trembling overhead, squashed by the court on the blow.</summary>
        private void DlFistPose(int side, float t, out Quaternion turn, out Vector3 scale)
        {
            float wrist = 18f * Ease(DlSlam - .3f, DlSlam - .1f, t) * (1f - Ease(DlSlam - .06f, DlSlam, t)) - 10f * DlBump(t, DlSlam - .02f, DlSlam + .14f);
            wrist += .25f * (DlElbow(side, t) - DlElbow(side, t - .06f));
            float open = Ease(DlStone, DlStone + .2f, t) * (1f - Ease(DlLoad, DlLoad + .2f, t));
            float clench = Ease(DlLoad + .05f, DlLoaded, t) * (1f - Ease(DlSlam, DlSlam + .3f, t)) * (1f + (_reducedEffects ? 0f : .4f * Mathf.Sin(t * 41f + side * 2f)));
            float squash = DlBump(t, DlSlam - .01f, DlSlam + .16f);
            turn = Quaternion.Euler(wrist, 0f, 0f);
            scale = new Vector3(1f + .05f * open - .05f * clench + .12f * squash, 1f - .05f * open + .04f * clench - .16f * squash, 1f + .05f * open - .05f * clench + .12f * squash);
        }

        /// <summary>Where the middle of one of its fists is, in the stage's space.</summary>
        private Vector3 DlBeastFist(int side)
        {
            var fist = _dlFist[side < 0 ? 0 : 1];
            var at = fist != null ? fist.TransformPoint(new Vector3(0f, -DlFistDrop, 0f)) : (side < 0 ? _dlArmL : _dlArmR).TransformPoint(new Vector3(0f, -DlArmLength, 0f));
            return _root.transform.InverseTransformPoint(at);
        }

        /// <summary>
        /// The piece of the suit that is arriving at <paramref name="t"/>, as a point on HIM: the fists, the bracers (the forearms),
        /// the chest, a shoulder, a leg, the horn on his head. The fire streams to it. <paramref name="lane"/> picks a side.
        /// </summary>
        private Vector3 DlSuitTarget(float t, int lane)
        {
            var head = HeadPoint;
            float side = lane % 2 == 0 ? -1f : 1f;
            var fist = DnFistPoint(lane % 2);
            if (t < DlSuitHere[2] + .06f) return fist;
            if (t < DlSuitHere[3] + .06f) return Vector3.Lerp(fist, head + new Vector3(side * .22f, -.55f, 0f), .35f);
            if (t < DlSuitHere[4] + .06f) return head + new Vector3(side * .06f, -.62f, .1f);
            if (t < DlSuitHere[5] + .06f) return head + new Vector3(side * .24f, -.48f, 0f);
            if (t < DlSuitHere[6] + .06f) return new Vector3(side * .13f, DnFloat + .2f, .05f);
            return head + new Vector3(0f, -.2f, 0f);
        }

        /// <summary>Where a piece of the suit goes on HIM: 0 fists, 1 bracers, 2 chest, 3 shoulders, 4 legs, 5 horn; lane 0 his left.</summary>
        private Vector3 DlGiftTarget(int part, int lane)
        {
            var head = HeadPoint;
            float side = lane == 0 ? -1f : 1f;
            var fist = DnFistPoint(lane);
            switch (part)
            {
                case 0: return fist;
                case 1: return Vector3.Lerp(fist, head + new Vector3(side * .22f, -.55f, 0f), .35f);
                case 2: return head + new Vector3(side * .08f, -.62f, .12f);
                case 3: return head + new Vector3(side * .24f, -.48f, 0f);
                case 4: return new Vector3(side * .13f, DnFloat + .2f, .05f);
                default: return head + new Vector3(side * .05f, -.2f, 0f);
            }
        }

        /// <summary>Where on IT that piece's stone tears off: the matching part of it (its knuckles for his fists, and so on).</summary>
        private Vector3 DlGiftSource(int part, int lane)
        {
            Vector3 world;
            switch (part)
            {
                case 0: return DlBeastFist(lane == 0 ? -1 : 1) + Vector3.up * .45f;
                case 1: world = _dlFore[lane] != null ? _dlFore[lane].TransformPoint(new Vector3(0f, -.7f, .2f)) : (lane == 0 ? _dlArmL : _dlArmR).TransformPoint(new Vector3(0f, -2.2f, .2f)); break;
                case 2: world = (_dlChest != null ? _dlChest : _dlBeast).TransformPoint(_dlChest != null ? DlSeams[lane] - new Vector3(0f, 1.2f, 1.0f) : DlSeams[lane]); break;
                case 3: world = _dlRock[lane] != null ? _dlRock[lane].TransformPoint(new Vector3(0f, .6f, 0f)) : _dlBeast.TransformPoint(new Vector3(lane == 0 ? -1.95f : 1.95f, 3.1f, .4f)); break;
                case 4: { var f = DlBeastFist(lane == 0 ? -1 : 1); return new Vector3(f.x * .8f, DnFloat + .05f, f.z + .3f); }
                default: world = _dlHead.TransformPoint(new Vector3(lane == 0 ? -.6f : .6f, 1.4f, .9f)); break;
            }
            return _root.transform.InverseTransformPoint(world);
        }

        /// <summary>
        /// ASTRA's suit on this cutscene's clock, with ONE difference made from here and nothing changed in ASTRA's file: while a
        /// piece is still in flight it is not drawn. ASTRA's pieces fly in from a ring on the court round him, and in this cutscene
        /// every piece is the longhorn's stone flung onto him (THE GIFT), so two sources at once read as noise (owner on maplh6:
        /// the pieces "flying in from the ground at the same time"). A piece is in flight when it is not where it sits once the
        /// suit is whole: the suit is stepped to `FormedAt` first and every renderer's place kept, then to this frame's clock, and
        /// any renderer more than 3 cm from its kept place is switched off for this frame (a piece's lock jolt is 2.2 cm, so a
        /// locked piece is never hidden). Only renderers switched off here are switched back on.
        /// </summary>
        private void DlStepSuit(float t)
        {
            if (_dnArmour == null) return;
            var all = _dnArmour.Renderers;
            bool forming = t < DlFormed;
            if (_dlSuitRest == null || _dlSuitRest.Length != all.Count) { _dlSuitRest = new Vector3[all.Count]; _dlSuitHid = new bool[all.Count]; }
            if (forming)
            {
                _dnArmour.StepTo(DanteMountainArmour.FormedAt);
                for (int i = 0; i < all.Count; i++) if (all[i] != null) _dlSuitRest[i] = all[i].transform.position;
            }
            _dnArmour.StepTo(DlArmourClock(t));
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                if (r == null) continue;
                bool flying = forming && (r.transform.position - _dlSuitRest[i]).sqrMagnitude > .03f * .03f;
                if (flying && r.enabled) { r.enabled = false; _dlSuitHid[i] = true; }
                else if (!flying && _dlSuitHid[i]) { r.enabled = true; _dlSuitHid[i] = false; }
            }
        }

        // ------------------------------------------------------------------ sample

        private void SampleLonghorn(float realT)
        {
            float t = DlAuthored(realT);
            float light = _reducedEffects ? .5f : 1f;
            if (!_dlPlanted && t >= DlPlant && t < DlPlant + .4f)
            {
                for (int f = 0; f < 2; f++) { var p = DnFistPoint(f); _dlPlantAt[f] = new Vector3(p.x, DnFloat, Mathf.Max(.35f, p.z)); }
                _dlPlanted = true;
            }
            if (_dlSlamAt == null && t >= DlSlam && t < DlSlam + .4f)
            {
                var mid = DnFistMid;
                _dlSlamAt = new Vector3(mid.x, DnFloat + .02f, Mathf.Max(.4f, mid.z));
            }
            var slamAt = _dlSlamAt ?? new Vector3(0f, DnFloat + .02f, .7f);

            // ------------------------------------------------------------- THE CALL
            for (int f = 0; f < 2; f++)
            {
                float age = t - DlPlant;
                var at = _dlPlanted ? _dlPlantAt[f] : new Vector3(f == 0 ? -.3f : .3f, DnFloat, .55f);
                PlaceGlow(_dlPlantStars[f], at + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.4f, 1.1f, Mathf.Clamp01(age / .08f)), Quaternion.Euler(90f, 0f, 0f),
                          age < 0f ? 0f : 1.3f * (1f - .6f * Ease(DlRisen, DlLoad, t)) * light);
            }
            // The cracks race back from his fists to where it is coming up, and beat with every shove.
            for (int i = 0; i < _dlCracks.Count; i++)
            {
                var from = _dlPlanted ? _dlPlantAt[i < 3 ? 0 : 1] : new Vector3(i < 3 ? -.3f : .3f, DnFloat, .55f);
                float a = (180f + DlCrackAngles[i]) * Mathf.Deg2Rad;
                var end = new Vector3(Mathf.Sin(a) * 2.2f, 0f, DlBack + 1.2f + Mathf.Cos(a) * 1.0f);
                var dir = new Vector3(end.x - from.x, 0f, end.z - from.z);
                float grow = Ease(DlPlant + .03f * i, DlPlant + .22f + .03f * i, t), len = Mathf.Max(.01f, dir.magnitude * grow);
                float beat = 0f;
                foreach (var s in DlShoves) beat = Mathf.Max(beat, Mathf.Clamp01(1f - Mathf.Abs(t - s.from - .05f) / .18f));
                float heat = grow <= 0f ? 0f : (1.0f + .9f * beat) * (1f - Ease(DlRisen, DlFormed, t));
                float yaw = 90f - Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                PlaceGlow(_dlCracks[i], new Vector3(from.x, DnFloat + .012f, from.z) + dir.normalized * (len * .5f), new Vector3(len + .2f, .22f, 1f),
                          Quaternion.Euler(90f, yaw, 0f), heat * light);
            }

            // ------------------------------------------------------------- THE LONGHORN
            float climb = DlClimbWeighted(t);
            // After the slam it goes back into the court it came out of, while the first band rises in front of him.
            // Fast and deep (film l4: sinking 1.6 m over the tail, its arm still lay across the bottom of play's hand-back frame).
            // maplh6: still half up when the hand-back shot opened, so it overlapped play's first band for a tenth of a second;
            // now it is under the court by the cut to that shot.
            float sinking = Mathf.Clamp01((t - (DlSlam + .02f)) / .2f), sink = sinking * sinking * 6.5f;
            float grind = _reducedEffects ? 0f : Mathf.Sin(t * 41f) * .03f * (t > DlRiseFrom && t < DlRisen ? 1f : 0f);
            float awake = Ease(DlRiseFrom + .3f, DlRisen, t);
            float breath = _reducedEffects ? 0f : Mathf.Sin(t * 2.3f) * awake;
            // ⚠️ WEIGHT, NOT TWEENS (owner on film l1: "so rigid and stiff"). Each move has an anticipation, the move, and an
            // overshoot that settles: it crouches before it flings its fists up, rises onto them as they go up, and the blow drives
            // it down 0.3 m into the court and it rebounds.
            float crouch = .28f * DlBump(t, DlLoad - .16f, DlLoad + .14f), stretch = .22f * Ease(DlLoad + .06f, DlLoaded, t) * (1f - Ease(DlSlam - .12f, DlSlam - .02f, t));
            float drop = .3f * Ease(DlSlam - .06f, DlSlam + .02f, t) * (1f - .5f * Ease(DlSlam + .08f, DlSlam + .3f, t)) + .06f * DlRing(t, DlSlam + .02f, 2.4f, 6f);
            float strain = _reducedEffects ? 0f : Mathf.Sin(t * 47f) * .025f * Ease(DlLoaded, DlSlam - .1f, t) * (1f - Ease(DlSlam - .1f, DlSlam - .08f, t));
            _dlBeast.localPosition = new Vector3(grind + strain, DnFloat + Mathf.Lerp(DlBuried, 0f, climb) - sink + .04f * breath - crouch + stretch - drop, DlBack);
            _dlBeast.localScale = Vector3.one * DlSize;
            // Head-first out of the court (pitched back as it climbs, so the horns break the surface before the body); it rears back
            // to roar and throws its weight forward on the roar; leans over him as it gives him its stone; rocks back as its fists
            // go up and is flung forward into the blow.
            float rollIn = 0f, yawIn = 0f;
            for (int i = 0; i < DlShoves.Length; i++)
            {
                float heave = DlBump(t, DlShoves[i].from - .06f, DlShoves[i].to + .16f) * (i % 2 == 0 ? 1f : -1f);
                rollIn += 6f * heave; yawIn += 5f * heave;
            }
            float rear = DlBump(t, DlStone - .38f, DlStone + .04f), thrust = DlBump(t, DlStone - .02f, DlStone + .42f);
            float giving = Ease(DlStone + .2f, DlStone + .45f, t) * (1f - Ease(DlFormed, DlLoad + .1f, t));
            float lean = Mathf.Lerp(-10f, 0f, climb) - 7f * rear + 6f * thrust + 4f * giving
                       - 6f * Ease(DlLoad + .05f, DlLoaded, t) * (1f - Ease(DlSlam - .16f, DlSlam - .08f, t))
                       + DlLean * Ease(DlSlam - .12f, DlSlam, t) + 2.5f * DlRing(t, DlSlam + .02f, 2.2f, 5f);
            _dlBeast.localRotation = Quaternion.Euler(lean, yawIn, rollIn + .8f * Mathf.Sin(t * 1.7f) * awake * (1f - Ease(DlLoad, DlLoaded, t)));
            _dlBeast.gameObject.SetActive(t > DlRiseFrom - .01f);

            // Its arms: shoulder, elbow (with drag) and wrist, each side on its own clock.
            float tremble = 0f;
            for (int side = 0; side < 2; side++)
            {
                float swing = DlArmSwing(side, t, out float shake);
                tremble = shake;
                // Wide either side of him while it gives him its stone (so its fists frame him and never cover him), wider overhead.
                float spread = Mathf.Lerp(4f, 8f, Ease(DlStone - .1f, DlStone + .22f, t));
                spread = Mathf.Lerp(spread, 14f, Ease(DlLoad, DlLoaded, t)) * (1f - Ease(DlSlam - .1f, DlSlam, t));
                (side == 0 ? _dlArmL : _dlArmR).localRotation = Quaternion.Euler(swing, 0f, side == 0 ? -spread : spread);
                if (_dlFore[side] != null) _dlFore[side].localRotation = Quaternion.Euler(DlElbow(side, t), 0f, 0f);
                if (_dlFist[side] != null) { DlFistPose(side, t, out var turn, out var size); _dlFist[side].localRotation = turn; _dlFist[side].localScale = size; }
            }

            // Its head LEADS the body by a beat (`th`): it tosses up into each heave, tucks its chin before the roar and throws it
            // back, bows over him and nods each piece of the suit on, looks up with its fists, and drives down into the blow.
            float th = t + .05f;
            float grinding = Ease(DlRiseFrom, DlRiseFrom + .2f, t) * (1f - Ease(DlRisen - .1f, DlRisen + .2f, t));
            float toss = 0f;
            foreach (var s in DlShoves) toss += -16f * DlBump(th, s.from - .05f, s.to) + 5f * DlRing(th, s.to, 2f, 6f);
            float roar = Ease(DlStone - .15f, DlStone + .05f, th) * (1f - Ease(DlStone + .3f, DlStone + .55f, th));
            float roarLoad = Ease(DlLoad + .05f, DlLoaded, th) * (1f - Ease(DlSlam - .1f, DlSlam, th));
            float bow = Ease(DlStone + .4f, DlStone + .7f, t) * (1f - Ease(DlFormed, DlLoad + .2f, t));
            float press = 0f;
            for (int i = 2; i < DlSuitHere.Length - 1; i++) press += 4f * DlRing(t, DlSuitHere[i] - .02f, 3f, 8f);
            float nod = Mathf.Clamp01((th - (DlSlam - .08f)) / .08f); nod *= nod;
            float pitch = toss + 8f * DlBump(th, DlStone - .4f, DlStone - .14f) - 24f * roar + 12f * bow + press - 18f * roarLoad
                        + 20f * nod - 6f * DlRing(t, DlSlam + .03f, 2.2f, 5f) - 12f * Ease(DlSlam + .1f, DlEnd, t);
            float look = 7f * Mathf.Sin(t * 2.1f) * giving + Mathf.Sin(t * 3.1f) * 5f * grinding - .6f * yawIn;
            _dlHead.localRotation = Quaternion.Euler(pitch + Mathf.Sin(t * 5.3f) * 2f * grinding, look, -.6f * rollIn + Mathf.Sin(t * 4.3f) * 3f * grinding);
            // The horns: stone, so stiff; their tips shiver a beat after a toss or a blow.
            float hornRing = 0f;
            foreach (var s in DlShoves) hornRing += DlRing(t, s.to + .03f, 4f, 9f);
            hornRing += 1.5f * DlRing(t, DlSlam + .03f, 4f, 7f) + DlRing(t, DlStone + .05f, 4f, 8f);
            for (int h = 0; h < 2; h++) if (_dlHorn[h] != null) _dlHorn[h].localRotation = Quaternion.Euler(0f, 0f, (h == 0 ? 1f : -1f) * 2.2f * hornRing);
            // Its jaw: a grunt on every heave, the roars, chewing breath while it bows over him, snapped shut on the blow and opened
            // again as it sinks.
            float grunt = 0f;
            foreach (var s in DlShoves) grunt += DlBump(t, s.from - .02f, s.to + .05f);
            float jaw = 4f * grinding + 10f * grunt + 30f * roar + 20f * roarLoad + 6f * bow * (.6f + .4f * Mathf.Sin(t * 6f))
                      + 16f * Ease(DlSlam + .06f, DlSlam + .25f, t) + (_reducedEffects ? 0f : Mathf.Sin(t * 43f) * 2.5f * (roar + roarLoad));
            if (_dlJaw != null) _dlJaw.localRotation = Quaternion.Euler(jaw * (1f - Ease(DlSlam - .06f, DlSlam - .02f, t) * (1f - Ease(DlSlam + .06f, DlSlam + .25f, t))), 0f, 0f);
            // Its chest breathes, swells before each roar and holds the breath through the hang, and empties on the blow.
            if (_dlChest != null)
            {
                float swell = .028f * breath + .1f * DlBump(t, DlStone - .45f, DlStone + .08f) + .08f * Ease(DlLoad, DlLoaded, t) * (1f - Ease(DlSlam - .05f, DlSlam, t))
                            - .06f * DlBump(t, DlSlam, DlSlam + .3f);
                _dlChest.localScale = new Vector3(1f + .4f * swell, 1f + swell, 1f + 1.2f * swell);
            }
            // The shoulder boulders: loose stone on it, so they shudder a beat behind every heave, the roar and the blow.
            for (int r = 0; r < 2; r++)
            {
                if (_dlRock[r] == null) continue;
                float hz = r == 0 ? 3.1f : 3.7f, j = 0f;
                foreach (var s in DlShoves) j += DlRing(t, s.to + .04f, hz, 6f);
                j += .7f * DlRing(t, DlStone + .08f, hz, 6f) + 1.6f * DlRing(t, DlSlam + .04f, hz, 5f) + .8f * DlRing(t, DlSuitHere[5] - DlGiftFlight, hz, 6f);
                _dlRock[r].localRotation = Quaternion.Euler(5f * j, 0f, (r == 0 ? 1f : -1f) * 4f * j);
                _dlRock[r].localPosition = new Vector3(r == 0 ? -1.95f : 1.95f, 2.55f + .06f * Mathf.Abs(j), .40f);
            }

            bool shown = _dlModelled && _dlBeast.gameObject.activeSelf;
            float eyes = Ease(DlEyes, DlEyes + .25f, t) * (.8f + .6f * Flash(t, DlSlam, .2f)) * (1f + .5f * roar) * (1f - .7f * Ease(DlSlam + .1f, DlEnd, t)) * light;
            PlaceGlow(_dlEyeL, new Vector3(-DlEye.x, DlEye.y, DlEye.z), new Vector3(1.1f, .45f, 1f), Quaternion.identity, shown ? eyes : 0f);
            PlaceGlow(_dlEyeR, DlEye, new Vector3(1.1f, .45f, 1f), Quaternion.identity, shown ? eyes : 0f);
            float breathe = Mathf.Max(roar, .8f * bow);
            PlaceGlow(_dlMouthGlow, DlMouth, new Vector3(2.2f, 1.0f, 1f), Quaternion.identity, shown ? breathe * .9f * light : 0f);
            // Its seams beat, slow as it climbs, hottest while its fire runs into him, quicker and hotter the longer the fists are
            // held, all at once on the blow.
            float build = Ease(DlLoad, DlSlam, t);
            float pulse = _reducedEffects ? .85f : .75f + .25f * Mathf.Sin(t * (5f + 13f * build));
            float burn = ((.25f + .25f * awake + .35f * giving + .5f * build) * pulse + (t >= DlSlam ? .7f : 0f)) * (1f - .8f * Ease(DlSlam + .1f, DlEnd, t));
            for (int i = 0; i < _dlSeamLights.Count; i++)
                PlaceGlow(_dlSeamLights[i], DlSeams[i], Vector3.one * (1.5f + .15f * Mathf.Sin(t * 7f + i * 1.9f)), Quaternion.identity, shown ? burn * light : 0f);
            // Its fists run hot while they are raised.
            float held = Ease(DlLoad, DlLoaded, t) * (.55f + .45f * Ease(DlLoaded, DlSlam, t)) * (1f - .6f * Ease(DlSlam, DlEnd, t));
            PlaceGlow(_dlFistGlowL, new Vector3(0f, _dlFist[0] != null ? -DlFistDrop : -DlArmLength, 0f), Vector3.one * 2.6f, Quaternion.identity, shown ? held * light : 0f);
            PlaceGlow(_dlFistGlowR, new Vector3(0f, _dlFist[1] != null ? -DlFistDrop : -DlArmLength, 0f), Vector3.one * 2.6f, Quaternion.identity, shown ? held * light : 0f);

            // The court it shoulders aside, round its base: up with the first shove, and it stays.
            var baseAt = new Vector3(0f, DnFloat, DlBack);
            {
                // Film l3: at 5.4 m across and 0.9 m high its slabs lay out flat either side of him like blades; narrower and taller it is
                // a collar of broken court round its base.
                float up = Ease(DlRiseFrom, DlRiseFrom + .6f, t), wide = Mathf.Lerp(2.2f, 4.0f, up);
                Place(_dlRubble, baseAt + new Vector3(0f, .04f, 0f), new Vector3(wide, 1.3f * up * (1f + .22f * Mathf.Sin(up * Mathf.PI)), wide * .8f), Quaternion.Euler(0f, 20f, 0f), up > .01f ? 1f : 0f);
                float under = Ease(DlRiseFrom - .1f, DlRiseFrom + .3f, t) * (1f - .6f * Ease(DlRisen, DlFormed, t));
                PlaceGlow(_dlUnderGlow, baseAt + Vector3.up * .02f, new Vector3(7f, 6f, 1f), Quaternion.Euler(90f, 0f, 0f), under * .8f * light);
            }
            DlPosePuffs(t);
            // (No rock shed off its shoulders any more: maplh10's pale lumps tumbling either side of it read as balls of smoke.)

            // ------------------------------------------------------------- THE FIRE: ASTRA's suit, and the longhorn's fire into him
            DlStepSuit(t);
            // A slipper he holds goes into the boulder fist as the fists close (as in v8; play brings it back out coated).
            if (_heldItem != null) _heldItem.transform.localScale = DlArmourClock(t) >= .84f ? Vector3.zero : Vector3.one;
            {
                // THE GIFT: each piece of his suit is a chunk of ITS stone, torn off the matching part of it, flown onto him on a fire
                // trail and landing as ASTRA's piece locks on (`DlSuitHere`). The tear flares on it where the stone left.
                for (int g = 0; g < DlGifts; g++)
                {
                    int part = g / 2, lane = g % 2;
                    float lockAt = DlSuitHere[part + 2], launch = lockAt - DlGiftFlight - .03f * lane;
                    float c = (t - launch) / (lockAt - launch);
                    var from = DlGiftSource(part, lane);
                    var to = DlGiftTarget(part, lane);
                    // Up off it and over, then falling onto him faster and faster (eased in), with a twist out to its side.
                    float arc = part == 4 ? .5f : 1.3f + .4f * DlHash(g * 3.3f);
                    Vector3 Path(float u) => Vector3.Lerp(from, to, u * u) + Vector3.up * (Mathf.Sin(Mathf.Min(1f, u * 1.15f) * Mathf.PI) * arc)
                                            + new Vector3((lane == 0 ? -1f : 1f) * Mathf.Sin(u * Mathf.PI) * .45f, 0f, 0f);
                    bool flying = c >= 0f && c < 1f;
                    // maplh6: 0.34 m stones at 3 m were specks; now they leave it the size of his head and shrink into the piece.
                    float size = Mathf.Lerp(part == 2 || part == 5 ? .62f : .52f, .2f, c * c);
                    Place(_dlGift[g], flying ? Path(c) : to, new Vector3(size, size * 1.3f, size), Quaternion.Euler(c * 420f + g * 37f, g * 61f, c * 300f), flying ? 1f : 0f);
                    // Molten where it broke: the stone carries its own heat in flight (film l2: dark stone alone read as loose debris
                    // on the warm street, not as a piece of him being given).
                    PlaceGlow(_dlGiftHeat[g], flying ? Path(c) : to, Vector3.one * (size * 2.2f), Quaternion.identity, flying ? (2.1f + .5f * Mathf.Sin(c * 30f)) * light : 0f);
                    for (int k = 0; k < DlTrail; k++)
                    {
                        float back = c - .07f * (k + 1);
                        PlaceGlow(_dlFlow[g * DlTrail + k], Path(Mathf.Clamp01(back)), Vector3.one * (.85f - .18f * k) * (1f - .4f * Mathf.Clamp01(back)),
                                  Quaternion.identity, flying && back > 0f ? (1.8f - .45f * k) * light : 0f);
                    }
                    // Where it tore off: a molten flare on its body for a beat.
                    PlaceGlow(_dlTear[g], from, Vector3.one * (1.2f + .6f * Mathf.Clamp01(c * 3f)), Quaternion.identity, c >= 0f ? 1.6f * DnBeat(t, launch, .3f) * light : 0f);
                }
                // Where its stone lands on him: a warm glow while it pours in, and a flare on each lock of a piece of the suit.
                float flow = Ease(DlSuitHere[2] - .2f, DlSuitHere[2], t) * (1f - Ease(DlFormed - .05f, DlFormed + .1f, t));
                // Small and low: film l3's 0.9 m glow at 0.5 blew his chest out to a yellow ball and hid the very pieces it was lighting.
                PlaceGlow(_dlTakeGlow, DlSuitTarget(t, 1) * .5f + DlSuitTarget(t, 0) * .5f, Vector3.one * .45f, Quaternion.identity, flow * (.25f + .1f * Mathf.Sin(t * 19f)) * light);
                float lockFlash = 0f; int locked = 2;
                for (int i = 2; i < DlSuitHere.Length - 1; i++) { float f = DnBeat(t, DlSuitHere[i], .14f); if (f > lockFlash) { lockFlash = f; locked = i; } }
                PlaceGlow(_dlLockFlash, (DlGiftTarget(locked - 2, 0) + DlGiftTarget(locked - 2, 1)) * .5f, Vector3.one * .7f, Quaternion.identity, lockFlash * .9f * light);
            }

            // ------------------------------------------------------------- THE LOAD
            for (int f = 0; f < 2; f++)
            {
                float hot = Ease(DlLoad + .1f, DlLoaded, t) * (1f + .6f * Ease(DlLoaded, DlSlam, t)) * (1f - .6f * Ease(DlSlam + .1f, DlEnd, t));
                PlaceGlow(_dlHeat[f], DnFistPoint(f), Vector3.one * 1.4f, Quaternion.identity, hot * (1f + .2f * Mathf.Sin(t * 24f + f)) * light);
            }

            // ------------------------------------------------------------- THE SLAM
            {
                float age = t - DlSlam;
                // ⚠️ THE WAVE IS FIRE, NOT RINGS (owner on maplh6: "at the end theres like an orange/fiery shockwave effect- but it
                // looks like a sticker circle outline. i need it to look fiery"). The five flat orange rings and stars this block used
                // to draw (at the blow and under its two fists) are gone; the bench's `FxStudyDanteFireWave` stands in their place:
                // a ragged wall of flame tongues rolling out from the blow, the court burnt and cracked glowing behind it, embers and
                // smoke. Only the dark crater stays.
                foreach (int ring in new[] { _dlSlamStar, _dlSlamRing, _dlSlamRingOuter, _dlCraterGlow, _dlBeastStarL, _dlBeastStarR, _dlBeastRingL, _dlBeastRingR })
                    PlaceGlow(ring, slamAt, Vector3.one, Quaternion.identity, 0f);
                float crater = age < 0f ? 0f : Mathf.Lerp(.45f, 1f, Ease(0f, .06f, age));
                Place(_dlCrater, slamAt + Vector3.up * .006f, new Vector3(1.0f, 1f, .9f) * crater, Quaternion.Euler(0f, 17f, 0f), age < 0f ? 0f : .92f);
                if (_dlWave != null)
                {
                    bool on = age >= 0f && !_reducedEffects;
                    if (_dlWave.Root.gameObject.activeSelf != on) _dlWave.Root.gameObject.SetActive(on);
                    if (on)
                    {
                        // Round HIM, not round his fists in front of him (maplh8: born at his fists, its first flames stood up between
                        // him and the lens and hid him on the blow; it now opens 1 m out, `FxStudyDanteFireWave.Clear`).
                        _dlWave.Root.localPosition = new Vector3(0f, DnFloat, .1f);
                        ShotAt(ShotIndexAt(realT), realT, out var lens, out _, out _);
                        _dlWave.Sample(age, lens);
                    }
                }
                // Its dust, from all three impacts (the cloud; placed where the fists really are on the frame of the blow).
                for (int i = 0; i < 3 && _dlSlamPuffFirst >= 0; i++)
                {
                    var centre = i == 0 ? slamAt : DlBeastFist(i == 1 ? -1 : 1);
                    if (age < .02f) _dlPuffs[_dlSlamPuffFirst + i].holder.localPosition = new Vector3(centre.x, DnFloat + .05f, centre.z);
                }
                for (int i = 0; i < _dlShards.Count; i++)
                {
                    float h = DlHash(i * 5.3f), g = DlHash(i * 1.9f), ang = (i * 45f + 20f) * Mathf.Deg2Rad;
                    float out_ = .6f + (2.4f + 3f * h) * Mathf.Max(0f, age), high = DnFloat + .2f + (6f + 4f * g) * age - 9.8f * age * age;
                    float size = .12f + .1f * g;
                    Place(_dlShards[i], slamAt + new Vector3(Mathf.Cos(ang) * out_, high - slamAt.y, Mathf.Sin(ang) * out_), new Vector3(size, size * 2.4f, size),
                          Quaternion.Euler(age * 900f * (h - .5f) * 2f + i * 40f, i * 50f, age * 700f), age >= 0f && high > DnFloat ? 1f : 0f);
                }
                for (int i = 0; i < _dlSpray.Count; i++)
                {
                    float a = age - .01f * i, way = (-60f + 120f * DlHash(i * 3.1f)) * Mathf.Deg2Rad, speed = 3.4f + 2.8f * DlHash(i * 9.7f), up = 3.8f + 2f * DlHash(i * 4.4f);
                    var at = slamAt + new Vector3(Mathf.Sin(way) * speed, 0f, Mathf.Cos(way) * speed) * Mathf.Max(0f, a) + Vector3.up * (.1f + up * Mathf.Max(0f, a) - 4.9f * a * a);
                    at.y = Mathf.Max(DnFloat + .04f, at.y);
                    PlaceGlow(_dlSpray[i], at, Vector3.one * .18f, Quaternion.identity, a < 0f ? 0f : 2.3f * (1f - Mathf.Clamp01(a / .9f)) * light);
                }
            }

            // ------------------------------------------------------------- THE GROUND RISES: play's cascade, on play's clock
            if (_dnLane != null)
            {
                bool on = t >= DlBand0;
                if (_dnLane.gameObject.activeSelf != on) _dnLane.gameObject.SetActive(on);
                if (on) _dnLane.StepTo(t - DlBand0);
            }
        }

        // ------------------------------------------------------------------ lens, grade, impact

        private Vector3 LonghornShake(float realT)
        {
            float t = DlAuthored(realT);
            float size = DanteKick(t, DlPlant, .08f, .2f) + DanteKick(t, DlSlam, .36f, .45f) + DanteKick(t, DlBand0, .1f, .22f);
            foreach (var s in DlShoves) size += DanteKick(t, s.from + .04f, .07f, .3f);
            // The grind of it climbing, the roar, and a fine tremble through the hang.
            size += .016f * Ease(DlRiseFrom, DlRiseFrom + .2f, t) * (1f - Ease(DlRisen - .1f, DlRisen + .2f, t));
            size += .02f * Ease(DlStone - .1f, DlStone + .1f, t) * (1f - Ease(DlStone + .4f, DlStone + .6f, t));
            size += .018f * Ease(DlLoaded, DlLoaded + .2f, t) * (1f - Ease(DlSlam - .08f, DlSlam, t));
            // Each piece of the suit locking on him is a small blow.
            for (int i = 2; i < DlSuitHere.Length - 1; i++) size += DanteKick(t, DlSuitHere[i], .03f, .14f);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0f) * size;
        }

        private void LonghornGrade(float realT, out float brightness, out float saturation)
        {
            float t = DlAuthored(realT);
            float back = Ease(DlRiseFrom, DlRisen, t) * (1f - Ease(DlSlam + .1f, DlEnd - .05f, t));
            float beat = Mathf.Max(DnBeat(t, DlSlam, .3f), .4f * DnBeat(t, DlPlant, .25f));
            foreach (var s in DlShoves) beat = Mathf.Max(beat, .25f * DnBeat(t, s.from + .04f, .2f));
            float hold = Ease(DlLoaded + .2f, DlSlam - .1f, t) * (1f - Ease(DlSlam, DlSlam + .02f, t));
            brightness = Mathf.Lerp(1f, .85f, back) - .14f * hold + .34f * beat;
            saturation = Mathf.Lerp(1f, .9f, back) - .1f * hold + .2f * beat;
        }

        /// <summary>Three impact frames on the slam, ink on molten orange, lines in to his fists (v8's shader, `Shaders/DanteImpact`).</summary>
        private void LonghornPostProcess(RenderTexture frame, Camera camera, float realT)
        {
            float t = DlAuthored(realT);
            if (_reducedEffects || camera == null || t < DlSlam || t >= DlSlam + .1f) return;
            int beat = Mathf.Clamp(Mathf.FloorToInt((t - DlSlam) / .0334f), 0, 2);
            if (_dlImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/DanteImpact");
                if (shader == null) return;
                _dlImpact = new Material(shader) { name = "LonghornImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(_dlSlamAt ?? new Vector3(0f, DnFloat, .7f)));
            _dlImpact.SetFloat("_Impact", beat == 2 ? .55f : 1f);
            _dlImpact.SetVector("_Focus", new Vector4(Mathf.Clamp(vp.x, .1f, .9f), Mathf.Clamp(vp.y, .1f, .9f), 0f, 0f));
            _dlImpact.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            _dlImpact.SetFloat("_Seed", 1f + beat);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _dlImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>
        /// The shots. Every one stays on his right (+x) and answers one question in order: WHAT HE DOES (calls), WHAT ANSWERS (it
        /// rises behind him, towering), WHAT IT GIVES (its fire, becoming his suit: framed on HIM, its face over his head), WHAT
        /// THEY DO (both load), WHAT IT LAUNCHES (the slam, the ground rising at the players). ⚠️ None is framed on its fists: in
        /// film l1 two shots were filled by them and he was lost behind them.
        /// </summary>
        private void LonghornFrame(int index, float realT, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            float t = DlAuthored(realT);
            var head = HeadPoint;
            switch (index)
            {
                case 0:
                {
                    var fists = DnFistMid;
                    eye = Vector3.Lerp(new Vector3(2.1f, DnFloat + .6f, 2.1f), new Vector3(1.75f, DnFloat + .5f, 1.75f), Ease(0f, DlRiseFrom, t));
                    look = new Vector3(fists.x * .5f, Mathf.Lerp(head.y - .2f, DnFloat + .35f, Ease(0f, DlPlant, t)), .45f);
                    fov = 46f;
                    break;
                }
                case 1:
                {
                    // Wide in front and a little above him, so all of him reads at the bottom of the frame while the lens tilts up
                    // after its horns as it climbs out of the court 3.6 m behind him.
                    float u = Ease(DlRiseFrom, DlRisen, t), up = DlClimb(t);
                    eye = Vector3.Lerp(new Vector3(2.8f, DnFloat + 1.3f, 4.8f), new Vector3(2.3f, DnFloat + 1.05f, 4.1f), u);
                    look = new Vector3(0f, DnFloat + Mathf.Lerp(1.0f, 2.9f, up), Mathf.Lerp(-2.0f, -3.2f, up));
                    fov = 58f;
                    break;
                }
                case 2:
                {
                    // THE FIRE, on HIM: low at his front right, close enough that each piece of the suit reads as it lands, its face
                    // over his head and its fire streaming down at him. A slow push in as he stands into the suit.
                    float u = Ease(DlStone, DlFormed, t);
                    // Further to his right than film l3 (his dark hair sat on its dark muzzle): he stands right of middle, its face
                    // over his left shoulder, its horns whole in the frame.
                    // maplh6: its face was behind his head and its horns ran out of the top. Higher and further right, looking up past
                    // him: his head sits 24 degrees under its eyes and 15 degrees to their left, and both horn tips are in.
                    eye = Vector3.Lerp(new Vector3(2.6f, DnFloat + .7f, 2.6f), new Vector3(2.35f, DnFloat + .75f, 2.25f), u);
                    look = new Vector3(.5f, DnFloat + Mathf.Lerp(2.0f, 2.1f, u), Mathf.Lerp(-1.8f, -1.9f, u));
                    fov = 62f;
                    break;
                }
                case 3:
                {
                    // ⚠️ NOT A CUT (owner on film l1: "the jump from getting armor to getting on the longhorn is also weird"): this
                    // shot STARTS where THE FIRE's lens stopped, on him in his finished suit, and pulls back and down as both of them
                    // raise their fists, until it is a low hero angle wide enough for him in front, it behind and its horns over
                    // all of it. He never leaves the court in front of it; the lens leans in through the hang.
                    float back = Ease(DlFormed, DlLoaded + .1f, t), u = Ease(DlLoaded, DlSlam, t);
                    // Film l3: from 4.8 m its horns and both raised fists were above the frame for the whole hang.
                    // maplh6: from 5.1 to 5.8 m he was an eighth of the frame. Pulled back only to 3.6 to 4.6 m (about a sixth), the
                    // field narrowing as it pushes in through the hang; their raised fists and its horns still in.
                    var wide = Vector3.Lerp(new Vector3(2.6f, DnFloat + .45f, 4.6f), new Vector3(2.2f, DnFloat + .4f, 3.6f), u);
                    eye = Vector3.Lerp(new Vector3(2.35f, DnFloat + .75f, 2.25f), wide, back);
                    look = Vector3.Lerp(new Vector3(.5f, DnFloat + 2.1f, -1.9f), new Vector3(0f, DnFloat + Mathf.Lerp(2.8f, 2.9f, u), Mathf.Lerp(-2.6f, -2.8f, u)), back);
                    fov = 62f - 6f * Ease(DlLoaded, DlSlam - .05f, t);
                    break;
                }
                case 4:
                {
                    // THE SLAM, in FRONT of him at his right and a little above: him, its two fists landing either side of his, its
                    // head driven down over him, one picture of the two of them hitting together. (Films l2 to l5 shot this from high
                    // behind him, and from every such spot its nodding head and right forearm filled half the frame; from 5 m up the
                    // lens went through Eskinita's bunting.) The field snaps in on the hit.
                    float u = Ease(DlSlam - .05f, DlBand0 + .05f, t), hit = DanteKick(t, DlSlam, 1f, .22f);
                    float rebound = Mathf.Sin(Mathf.Clamp01((t - DlSlam - .04f) / .3f) * Mathf.PI) * (t > DlSlam + .04f ? 1f : 0f);
                    eye = Vector3.Lerp(new Vector3(3.1f, DnFloat + 1.7f, 3.9f), new Vector3(2.8f, DnFloat + 1.5f, 3.5f), u) + Vector3.down * (.35f * hit);
                    look = new Vector3(0f, DnFloat + 1.1f - .3f * hit, -.9f);
                    fov = 62f - 12f * hit + 5f * rebound;
                    break;
                }
                case 5:
                {
                    // THE GROUND RISES: cut on the band's heave to high at his right, down the lane at the real players, ending exactly
                    // on play's hand-back frame (eye (4.0, 3.6, 0.5), down the lane to 6 m). The longhorn is sinking back into the court
                    // behind the lens. The field punches in on the band.
                    float u = Ease(DlBand0, DlEnd, t), band = DanteKick(t, DlBand0, 1f, .2f);
                    eye = Vector3.Lerp(new Vector3(4.3f, DnFloat + 3.4f, .3f), new Vector3(4.0f, DnFloat + 3.6f, .5f), u);
                    look = new Vector3(0f, DnFloat + .4f, Mathf.Lerp(4.6f, 6.0f, u));
                    fov = 58f - 5f * band;
                    break;
                }
            }
        }
    }
}
