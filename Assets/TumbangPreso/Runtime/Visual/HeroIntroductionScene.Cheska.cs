using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // YASMIN, ABSOLUTE ZERO, 6.5 s (was 3.2 s; plan.md § 6 is the first one, staged as GLACIAL NOVA).
        //
        // ⚠️ THE SIXTH STAGING, 2026-10-08. The tone is the owner's: "cheeky but surprisingly strong". How it got here, each
        // one filmed: a crystal with the court in miniature in it ("cheska's is just boring overall"); she taps the lens and
        // the court is a gallery of ice statues ("the whole animation just feels dead. there's no pzazz or character to
        // it"); she stands and each snap is a hard cut to a player in a flat colour panel ("maybe do something where its
        // like her pov and shes pointing to and snapping at each character's live ingame positions. and then do that star
        // burst effect. i like the idea of her skating around too"); she skates a whole lap seen over her shoulder ("i was
        // thinking she only skates at the start, then its fpv one by one looking at each character, it zooms in to focus
        // on them, and then does that graphical starburst effect").
        // ⚠️ AND THE SEVENTH, the same day, which is this file, 11.5 s (9.0 at first; see the last note of this list). Of the sixth (6.5 s, a quick loop, three looks of 0.8 s
        // each with a hand built here that had fingers) he said: "its a bit too sped up and linear for my liking. first of all
        // the first skating part is just not graceful enough, next the fingers are weird, i was thinking just use the original
        // fpv hand, instead make her whole elbow bent and then extend to point towards the person. after do a top view of her
        // skating circling around, then do the glacier stuff".
        //   1. SKATE (0 to 2.3): one long slow loop on a ribbon of ice, a leg held out behind her, her arms wide, and a
        //      turn on the spot to stop where she began.
        //   2. ONE, TWO, THREE (2.3, 3.6, 4.5), THROUGH HER EYES, and not three alike: the first is slow (1.3 s), the next
        //      two quicker (0.9, 0.8). She turns to another player where they really stand, her arm comes up with the
        //      elbow bent, the view zooms in on them, and the arm straightens out at them. That point is the trigger: the frame goes to one flat colour, a two-colour
        //      star bursts behind them and a block of ice comes down over them. There is no snap and no fingers.
        //   3. THE HEART (5.3 to 7.95), from straight above: a one-line heart on a long waving stroke (`CkRoute`): she skates a heart on the ice, inside everyone, from its point
        //      where she stands round both lobes and home again.
        //   4. THE STOMP (7.95 to 8.55), wide and low: one foot down and the whole court goes up in ice out of that ring.
        //   5. (THE BLOW is cut: the heart needed its time, 2.65 s for a line 30 m long. `CkBlow` is past the end.) It was: she blows the frost off her fingertip.
        //   6. THE SNAP (8.55 to 9.0), high and wide: her hands snap apart, the glacier bursts, and everyone is left
        //      standing in their block of ice. Play resumes with them frozen (`HeroAbility.IntroductionIsTheWindup`).
        // ⚠️ THE TIMES IN THE LIST ABOVE ARE THE 9.0 s CUT'S. It is 11.5 s now, on his word ("ykw we can extend the ult cutscene
        // until everything looks smooth"), and then 12.6 (of the blow, "its ending too fast"): the skate to 2.8, the looks at
        // 2.8, 4.1 and 5.0, the heart 5.8 to 9.9, the stomp at 10.05, the blow 10.55 to 11.9, the last snap at 12.28. And then
        // 11.9: "i think the heart is a tad bit too slow for someone skating", so the heart is 3.4 s (was 4.1) and everything
        // after it is 0.7 s sooner. The constants below are the truth.
        //
        // ⚠️ THE ARM IN HER VIEW IS HER MODEL'S OWN, ELBOW AND ALL. The first cut of this used the first-person arm play draws
        // (`Models/RosterArms/cheska_*`), which is her arm cut out as ONE rigid mesh, and I told him it had no joint; he:
        // "doesnt cheska's model have elbows?". It does (`forearm-left`, `forearm-right`, her authoring script's rule 8).
        // Posing the arm on her body where it stands did not work either (film c14): her arm is short and hangs a forearm's
        // length off the line of sight, so the zoom looks straight past it. So what is in her view is a SECOND COPY OF HER
        // WHOLE BODY, held to the frame the way a first-person arm is in play: its shoulder at the bottom corner whatever the
        // zoom, its head shrunk to nothing, everything else of it behind or below the lens. Its upper arm and forearm are
        // posed here: up with the elbow closed, the fist by her cheek, then out straight at them.
        // ⚠️ HER BODY TRAVELS in the skate and the circle (the clip writes only the bones under the copy's root, and the game
        // samples the clip before this stage, so the root is this file's to move). It is home, turned as it began, after each.
        // ⚠️ BLOCKING: every shape here is a plain one. ⚠️ The colour panels are stood for the AUTHORED lens; a shot the
        // game mirrors or pushes in on a real map (`UltimatePhaseView.ChooseShot`) would see past their edge.
        // ⚠️ LUNA SNOW (the reference he named) WAS NOT LOOKED AT: the footage would not draw in the browser pane.
        // ⚠️ EVERY TIME IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `_cheska`). The last pose is `snap`.
        // =========================================================================================
        private const float CkSpinFrom = 2.24f, CkSpinTo = 2.72f, CkCircleFrom = 5.80f, CkCircleTo = 9.16f, CkHeartMost = 4.4f, CkHeartLeast = 4.4f,
                            CkStomp = 9.35f, CkBlow = 10.02f, CkSnap = 11.58f;
        private const int CkFirstLook = 1, CkWideShot = 5;
        // The skate: one loop in front of her, and when she is at each point of it.
        private static readonly float[] CkWayAt = { .26f, .74f, 1.20f, 1.63f, 2.00f, 2.33f };
        // ⚠️ Kept inside 2.6 m of where she stands: the others are staged no nearer than 3 m, so she never skates through one.
        private static readonly Vector3[] CkWay =
        { Vector3.zero, new Vector3(1.9f, 0, .9f), new Vector3(1.3f, 0, 2.3f), new Vector3(-1.3f, 0, 2.2f), new Vector3(-1.9f, 0, .8f), Vector3.zero };
        // The three looks: when each starts, how long it is given, and when in it her arm points (the first is the slow one).
        private static readonly float[] CkLooks = { 2.80f, 4.10f, 5.00f }, CkLookFor = { 1.30f, .90f, .80f }, CkPointIn = { .86f, .54f, .46f };
        private static readonly Color CheskaIce = new Color(.62f, .9f, 1, .9f);
        // Each panel's flat colour, and the star behind the white one.
        private static readonly Color[] CkPanelColour = { new Color(.30f, .78f, .95f, 1), new Color(.98f, .96f, .86f, 1), new Color(.16f, .40f, .62f, 1) };
        private static readonly Color[] CkPanelStar = { new Color(.16f, .40f, .62f, 1), new Color(.30f, .78f, .95f, 1), new Color(.30f, .78f, .95f, 1) };
        // The crown behind her: (x, z, height, lean), typed, the middle one the tallest.
        private static readonly Vector4[] CkCrown =
        {
            new Vector4(-3.6f, -2.6f, 4.6f, 12f), new Vector4(-1.7f, -3.5f, 7.2f, 5f), new Vector4(.2f, -3.9f, 9.0f, -2f),
            new Vector4(2.1f, -3.4f, 6.6f, -7f), new Vector4(3.9f, -2.4f, 4.1f, -13f),
        };
        // The rings that race out from her foot: (distance, how many, height).
        private static readonly Vector3[] CkRings = { new Vector3(3.3f, 7, 1.5f), new Vector3(5.8f, 10, 2.3f), new Vector3(8.5f, 13, 3.2f) };
        private static readonly Vector3[] CkMotes =
        {
            new Vector3(-1.6f, 1.9f, 2.2f), new Vector3(1.1f, 2.6f, 3.4f), new Vector3(2.8f, 1.2f, 1.6f), new Vector3(-3.4f, 2.3f, 4.6f),
            new Vector3(.4f, 3.1f, 5.8f), new Vector3(3.9f, 1.7f, 5.1f), new Vector3(-2.2f, 1.1f, 6.9f), new Vector3(1.9f, 2.2f, 7.8f),
            new Vector3(-4.8f, 1.6f, 2.1f), new Vector3(5.2f, 2.8f, 3.0f), new Vector3(-.8f, 1.5f, 9.2f), new Vector3(3.1f, 3.3f, 9.9f),
            new Vector3(-3.9f, 2.9f, 8.4f), new Vector3(.9f, 1.0f, 4.4f), new Vector3(-5.6f, 2.0f, 6.0f), new Vector3(4.6f, 1.3f, 7.2f),
            new Vector3(-1.3f, 2.7f, -1.8f), new Vector3(2.2f, 1.8f, -2.6f), new Vector3(-3.0f, 1.4f, -3.4f), new Vector3(.3f, 3.4f, 1.0f),
        };

        private sealed class CkStatue
        {
            public Other Body;          // null for the can
            public int Cube;
            public Vector3 Home; public float Height, FrozenAt = float.MaxValue;
        }

        private sealed class CkSpire
        {
            public int Piece;
            public Vector3 At; public float Height, Width, Lean, Turn, From;
        }

        private int _ckMistLow, _ckMistHigh, _ckBreath, _ckPlates, _ckFloor, _ckBackdrop, _ckBackFloor, _ckBurst, _ckBurstBack;
        private readonly List<int> _ckPines = new List<int>(10), _ckShards = new List<int>(6), _ckMoteGlows = new List<int>(20);
        private readonly List<CkStatue> _ckStatues = new List<CkStatue>(5);
        // Who she turns to, in order.
        private readonly List<CkStatue> _ckPanels = new List<CkStatue>(3);
        private readonly List<CkSpire> _ckSpires = new List<CkSpire>(40);
        // Which side of her the wide shot is taken from: the side with nobody standing in the lens (`CheskaClearSide`).
        private float _ckWideSide = 1f, _ckSkateSide = 1f;
        // How wide the heart is skated, in metres: as near `CkHeartMost` as leaves her clear of everyone (`CheskaHeartSize`).
        private float _ckHeartSize = CkHeartLeast;
        private LineRenderer _ckTrail, _ckRing;
        // Her copy's root as it was handed over.
        private Vector3 _ckBodyAt; private Quaternion _ckBodyTurn;
        // The copy of her that is held to the frame through her eyes, and on it the arm she points with (the free one when
        // she holds a slipper): its upper arm and forearm, which way along their own X they run, and how they rest.
        private GameObject _ckSelf;
        private Transform _ckUpper, _ckFore;
        private float _ckArmAlong = 1f;
        private Quaternion _ckSelfTurn, _ckUpperRest;
        private Vector3 _ckSelfScale;

        private void BuildCheska()
        {
            _ckMistLow = Wall("HighlandMistGround", 0, 1.4f, new Color(.6f, .66f, .64f, .55f), radius: 14.5f, emission: .2f);
            _ckMistHigh = Wall("HighlandMistSky", 1.4f, 30, new Color(.8f, .86f, .88f, .5f), radius: 14.5f, emission: .3f, cap: true);
            for (int i = 0; i < 10; i++)
                _ckPines.Add(AddSolid("RidgePine" + i, VfxShapes.Spire(6, .08f, .25f, 120 + i), new Color(.26f, .34f, .31f, 1)));
            _ckBreath = Add("ColdBreath", VfxShapes.TwoSided(VfxShapes.Splat(10, .3f, 5)), new Color(.95f, .98f, 1, .5f), .4f);
            _ckFloor = Add("FrozenCourt", VfxShapes.Prism(36, .012f, 1f), new Color(.82f, .95f, 1, .4f), .35f);
            _ckPlates = Add("FrostPlates", VfxShapes.Wedges(8, .14f, 9, .05f, .25f, 33), new Color(.78f, .95f, 1, .8f), .5f);
            for (int i = 0; i < 6; i++) _ckShards.Add(Add("SnapShard" + i, YasminGatheredCrystal(), CheskaIce, .9f));

            // Everyone else, where they stand, and the can.
            StageOthers(3.0f, 9.5f);
            foreach (var other in _others) _ckStatues.Add(CheskaStatue(other, other.Home, 1.75f));
            var seen = new List<CkStatue>(_ckStatues);
            seen.Sort((a, b) => Mathf.Atan2(a.Home.x, a.Home.z).CompareTo(Mathf.Atan2(b.Home.x, b.Home.z)));
            var can = CheskaStatue(null, _hasCan ? _canAt : new Vector3(0, 0, 4.5f), .62f);
            _ckStatues.Add(can);
            // Three looks. With fewer than three other players the can takes one, then they go round again.
            if (seen.Count < CkLooks.Length) seen.Add(can);
            for (int look = 0; look < CkLooks.Length; look++)
            {
                var caught = seen[look % seen.Count];
                _ckPanels.Add(caught);
                caught.FrozenAt = Mathf.Min(caught.FrozenAt, CkLooks[look] + CkPointIn[look] + .04f);
            }
            // Anyone no snap reached is taken by the stomp.
            foreach (var statue in _ckStatues) if (statue.FrozenAt > CkStomp) statue.FrozenAt = CkStomp + .08f;

            // A panel is ONE flat colour, so its pieces are unlit (the ghost material glows them to white): a sheet behind
            // the player, the ground under them, and two stars turning against each other between.
            _ckBackdrop = Add("PanelBackdrop", VfxShapes.TwoSided(GlowQuad()), Color.white, 1f, plain: true);
            _ckBackFloor = Add("PanelGround", VfxShapes.Prism(4, .01f, 1f), Color.white, 1f, plain: true);
            _ckBurstBack = Add("SnapBurstBack", VfxShapes.TwoSided(VfxShapes.Star(9, .42f, 8)), Color.white, 1f, plain: true);
            _ckBurst = Add("SnapBurst", VfxShapes.TwoSided(VfxShapes.Star(14, .34f, 3)), Color.white, 1f, plain: true);
            RagoUnlitBackdrop(_ckBackdrop); RagoUnlitBackdrop(_ckBackFloor); RagoUnlitBackdrop(_ckBurstBack); RagoUnlitBackdrop(_ckBurst);
            _ckWideSide = CheskaClearSide(CkWideShot); _ckSkateSide = CheskaClearSide(0);
            _ckHeartSize = CheskaHeartSize();

            _ckTrail = Line("SkateRibbon", 56, .26f, new Color(.82f, .96f, 1, .75f));
            _ckRing = Line("SkateHeart", 360, .22f, new Color(.82f, .96f, 1, .95f));
            if (_bodyRoot != null) { _ckBodyAt = _bodyRoot.transform.position; _ckBodyTurn = _bodyRoot.transform.rotation; }
            BuildCheskaHand();

            // The glacier: the crown behind her, then the rings, each spire off any body or can it would stand on.
            foreach (var row in CkCrown)
                _ckSpires.Add(new CkSpire { At = new Vector3(row.x, 0, row.y), Height = row.z, Width = .5f + row.z * .11f, Lean = row.w, Turn = row.x * 31f, From = CkStomp + .04f + Mathf.Abs(row.x) * .012f });
            for (int ring = 0; ring < CkRings.Length; ring++)
            {
                int count = (int)CkRings[ring].y;
                for (int i = 0; i < count; i++)
                {
                    // Uneven round the ring: every spire a little off its place, its own height and lean.
                    float angle = (i + .5f * (ring % 2) + Mathf.Sin(i * 2.7f + ring) * .22f) * Mathf.PI * 2 / count;
                    float reach = CkRings[ring].x + Mathf.Sin(i * 1.9f + ring * 2) * .55f;
                    var at = new Vector3(Mathf.Sin(angle) * reach, 0, Mathf.Cos(angle) * reach);
                    // Not in front of her face, and not through anyone.
                    if (at.z > 0 && Mathf.Abs(at.x) < 1.6f && ring == 0) continue;
                    bool clear = true;
                    foreach (var statue in _ckStatues) if ((statue.Home - at).magnitude < 1.35f) clear = false;
                    if (!clear) continue;
                    float tall = CkRings[ring].z * (.7f + .6f * Mathf.Abs(Mathf.Sin(i * 3.3f + ring)));
                    _ckSpires.Add(new CkSpire { At = at, Height = tall, Width = .3f + tall * .1f, Lean = Mathf.Sin(i * 4.1f) * 11f, Turn = i * 47f, From = CkStomp + .10f + ring * .13f + (i % 3) * .02f });
                }
            }
            for (int i = 0; i < _ckSpires.Count; i++)
                _ckSpires[i].Piece = Add("GlacierSpire" + i, VfxShapes.Spire(6, .1f, .2f, 140 + i), new Color(.66f, .92f, 1, .86f), .5f);

            for (int i = 0; i < CkMotes.Length; i++)
                _ckMoteGlows.Add(AddGlow("HangingMote" + i, new Color(.9f, .97f, 1, 1), falloff: 2.4f, core: .7f));
        }

        private CkStatue CheskaStatue(Other body, Vector3 home, float height)
        {
            var statue = new CkStatue { Body = body, Home = home, Height = height };
            statue.Cube = Add("IceBlock", VfxShapes.Prism(4, 1f, .94f, 0, 0, 40 + _ckStatues.Count), new Color(.7f, .93f, 1, .5f), .55f);
            return statue;
        }

        /// <summary>
        /// <summary>
        /// <summary>
        /// The copy of her that her view holds, and the arm on it. A rig with no elbow under the arm has nothing to bend, and
        /// she then raises no arm.
        /// </summary>
        private void BuildCheskaHand()
        {
            if (_bodyRoot == null) return;
            _ckSelf = MiniatureOf(_bodyRoot, _root.transform, out var surfaces);
            _ckSelf.name = "FirstPersonSelf";
            // How her body is turned in the scene as it is handed over (it faces the scene's own forward), and its size.
            _ckSelfTurn = Quaternion.Inverse(_root.transform.rotation) * _bodyRoot.transform.rotation;
            _ckSelfScale = _bodyRoot.transform.lossyScale;
            foreach (var surface in surfaces)
                if (surface is SkinnedMeshRenderer skin) { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
            string side = _heldItem != null ? "left" : "right";
            foreach (var bone in _ckSelf.GetComponentsInChildren<Transform>(true))
            {
                // Through her own eyes she has no head to look past.
                if (bone.name == "head") bone.localScale = Vector3.zero;
                if (bone.name == "arm-" + side) _ckUpper = bone;
                if (bone.name == "forearm-" + side) _ckFore = bone;
            }
            if (_ckUpper == null || _ckFore == null || _ckFore.parent != _ckUpper) { _ckUpper = null; _ckFore = null; _ckSelf.SetActive(false); return; }
            // Shoulder to elbow, in the upper arm's own frame, is along +X or -X (the importer mirrors it; `HeroAbilityClips`, THE ELBOWS).
            _ckArmAlong = _ckFore.localPosition.x < 0 ? -1f : 1f;
            _ckUpperRest = _ckUpper.localRotation;
            // ⚠️ ONLY THE ARM (2026-10-09, measured): see `SampleCheskaArm`. Her model is not touched; this is the copy.
            var trunk = _ckUpper.parent;
            foreach (var bone in _ckSelf.GetComponentsInChildren<Transform>(true))
            {
                if (bone == _ckSelf.transform || bone == trunk || bone.IsChildOf(_ckUpper) || trunk.IsChildOf(bone)) continue;
                if (bone.GetComponent<Renderer>() != null) continue;
                bone.localScale = Vector3.zero;
            }
            trunk.localScale *= .001f; _ckUpper.localScale *= 1000f;
            _ckSelf.SetActive(false);
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

        private Vector3 CheskaGather => (_heldItem != null ? FreePalm : BothPalms) + new Vector3(0, .1f, .12f);

        /// <summary>
        /// The wide shot stands low in front of her, to one side. The others stand wherever they stood, so the side is chosen:
        /// whichever keeps every body furthest from the lens and from its line to her (film c5 had one a metre from the lens
        /// and the glacier went up behind a block of ice). +1 is the authored side.
        /// </summary>
        private float CheskaClearSide(int index)
        {
            if (_performance == null || _performance.Shots.Count <= index) return 1f;
            var shot = _performance.Shots[index];
            float best = -1f, side = 1f;
            foreach (float tried in new[] { 1f, -1f })
            {
                float clear = float.MaxValue;
                foreach (var eye in new[] { shot.EyeFrom, shot.EyeTo })
                {
                    var lens = new Vector3(eye.x * tried, 0, eye.z);
                    foreach (var statue in _ckStatues)
                    {
                        // Distance from this body to the line from the lens to her.
                        float along = Mathf.Clamp01(Vector3.Dot(statue.Home - lens, -lens) / Mathf.Max(.01f, lens.sqrMagnitude));
                        clear = Mathf.Min(clear, (statue.Home - (lens - lens * along)).magnitude);
                    }
                }
                if (clear > best) { best = clear; side = tried; }
            }
            return side;
        }

        /// <summary>
        /// <summary>
        /// ⚠️ THE HEART IS A ONE-LINE HEART, the kind drawn without lifting the pen. How it got here, all 2026-10-08: he drew a
        /// route in red over the film twice ("the heart is wayy too small, should be larger like 2-3x large"; "follow the route
        /// i gave, and make the heart more natural - the loop looks like an exact circle and breaks the smooth curve"); his
        /// points traced gave every wobble of a mouse line ("now it looks like a child drew it"); a clean mirrored one was
        /// wrong too ("no not symmetric at all"); and then he sent two pictures, "something like these": a single-line heart
        /// on a long waving stroke, and a brush heart with one lobe larger than the other.
        /// So, after the first of those: the line comes IN from the left over a low wave, dips, and rises through the heart's
        /// point; up the right side; over the right lobe, which is the taller and narrower; down into the cleft and round a
        /// TINY teardrop there; over the left lobe, which is the rounder and reaches further out; down the left side; back
        /// through the point, nearly level, crossing itself; on to the right, up over a crest, and OUT rising (the small curl the picture has in its
        /// tail was in and is out again, on his word). The heart leans a little to the left, as theirs do.
        /// Each row is a place on the line and the way and speed it is going through it, as (across, up the frame) in HEART
        /// WIDTHS from the point where the heart's two sides cross; a smooth curve is laid between each pair, so there is no
        /// corner anywhere and no arc that is a formula's. ⚠️ The tails are shorter than the picture's (it is nearly four
        /// hearts wide): the whole line here is 2.7 heart widths across, and that is already most of a court.
        /// </summary>
        private static readonly Vector2[,] CkRoute =
        {
            { new Vector2(-1.25f, .20f), new Vector2(.55f, -.10f) },    // in, off the back of a low wave
            { new Vector2(-.42f, -.10f), new Vector2(.50f, .00f) },     // the dip
            { new Vector2(0f, 0f), new Vector2(.42f, .30f) },           // through the point, rising
            { new Vector2(.40f, .56f), new Vector2(.00f, .50f) },       // the right side, at its widest
            { new Vector2(.18f, .92f), new Vector2(-.38f, .02f) },      // the top of the right lobe
            // ⚠️ The teardrop was 0.08 of a heart across and she whipped round it ("turn on the small heart loop is still too
            // sharp/fast in velocity"). It is twice that now and a quarter of a heart deep, and its bottom is a wide sweep.
            { new Vector2(-.10f, .68f), new Vector2(-.12f, -.34f) },    // down into the cleft
            { new Vector2(-.105f, .40f), new Vector2(.24f, .00f) },     // the bottom of the teardrop
            { new Vector2(-.02f, .58f), new Vector2(-.10f, .30f) },     // and back up across itself
            { new Vector2(-.32f, .84f), new Vector2(-.34f, .06f) },     // the top of the left lobe
            { new Vector2(-.60f, .56f), new Vector2(.00f, -.42f) },     // the left side, at its widest
            { new Vector2(-.34f, .14f), new Vector2(.36f, -.24f) },     // sweeping in to the point
            { new Vector2(0f, 0f), new Vector2(.40f, -.02f) },          // through the point again, nearly level
            { new Vector2(.62f, .20f), new Vector2(.38f, .30f) },       // rising
            // ⚠️ The tail had a small curl in it here, as the picture's does. He: "you can remove the tail curl at the end".
            { new Vector2(1.00f, .44f), new Vector2(.36f, .16f) },      // easing off the rise
            { new Vector2(1.36f, .58f), new Vector2(.30f, .10f) },      // out, where she stands
        };
        private const int CkRouteSteps = 20;
        // WHEN she is at each step of each curve (0 to 1 of the heart's time). ⚠️ NOT by length: at one even speed the line was
        // "too linear.. its not flowwy at all.. speed should slow down and vary, especially on short loops or turns" (owner,
        // 2026-10-08). A skater carries speed down a straight and gives it up to turn, so her speed at each step comes from
        // how tightly the line bends there (`CheskaHeart` builds this once).
        private static float[] _ckRouteClock;

        /// <summary>One curve of the line: from row <paramref name="curve"/> to the next, leaving and arriving the way each row says.</summary>
        private static Vector2 CheskaRoutePoint(int curve, float u)
        {
            Vector2 from = CkRoute[curve, 0], to = CkRoute[curve + 1, 0], leave = from + CkRoute[curve, 1] / 3f, arrive = to - CkRoute[curve + 1, 1] / 3f;
            float v = 1 - u;
            return v * v * v * from + 3 * v * v * u * leave + 3 * v * u * u * arrive + u * u * u * to;
        }

        /// <summary>
        /// Where she is on the route, <paramref name="along"/> 0 to 1 of its TIME (she is slow in its turns), for a heart <paramref name="width"/> metres
        /// wide (scene space; up his frame is in front of her). ⚠️ THE ROUTE ENDS WHERE SHE STANDS, not at the heart: play
        /// resumes from her own place, so the end of the last curve is laid on it and the heart falls to her left. She is
        /// first seen on it at its far left end; the shot before is through her own eyes, so nobody sees her go.
        /// </summary>
        private static Vector3 CheskaHeart(float along, float width)
        {
            int curves = CkRoute.GetLength(0) - 1, count = curves * CkRouteSteps + 1;
            if (_ckRouteClock == null)
            {
                // Every step's place, then how sharply the line turns at it: the angle it turns through over the distance it
                // takes to do it (in heart widths, so 1 is a gentle bend, 4 is round a lobe, and the teardrop is over 15).
                var places = new Vector2[count];
                for (int i = 0; i < count; i++)
                    places[i] = i == 0 ? CheskaRoutePoint(0, 0) : CheskaRoutePoint(Mathf.Min(curves - 1, (i - 1) / CkRouteSteps), ((i - 1) % CkRouteSteps + 1) / (float)CkRouteSteps);
                var pace = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Vector2 back = places[i] - places[Mathf.Max(0, i - 1)], next = places[Mathf.Min(count - 1, i + 1)] - places[i];
                    float span = (back.magnitude + next.magnitude) * .5f;
                    float bend = i == 0 || i == count - 1 || span < 1e-5f ? 0 : Vector2.Angle(back, next) * Mathf.Deg2Rad / span;
                    // Her speed there: all of it down a straight, about a third round a lobe, a tenth in the teardrop and the curl.
                    pace[i] = Mathf.Max(.09f, 1f / (1f + .5f * Mathf.Pow(bend, 1.05f)));
                }
                // She does not brake in one step or leave a turn in one: her speed is eased over the steps either side. Twice.
                for (int pass = 0; pass < 2; pass++)
                {
                    var eased = new float[count];
                    for (int i = 0; i < count; i++)
                    {
                        float sum = 0; int taken = 0;
                        for (int k = -7; k <= 7; k++) if (i + k >= 0 && i + k < count) { sum += pace[i + k]; taken++; }
                        eased[i] = sum / taken;
                    }
                    pace = eased;
                }
                // And she glides to her stop over the last of it.
                for (int i = 0; i < count; i++) pace[i] *= Mathf.Lerp(.3f, 1f, Mathf.Clamp01((count - 1 - i) / (count * .07f)));
                var clock = new float[count];
                for (int i = 1; i < count; i++) clock[i] = clock[i - 1] + (places[i] - places[i - 1]).magnitude / ((pace[i] + pace[i - 1]) * .5f);
                for (int i = 1; i < count; i++) clock[i] /= clock[count - 1];
                _ckRouteClock = clock;
            }
            along = Mathf.Clamp01(along);
            int step = 1;
            while (step < count - 1 && _ckRouteClock[step] < along) step++;
            float within = Mathf.InverseLerp(_ckRouteClock[step - 1], _ckRouteClock[step], along);
            int curve = Mathf.Min(curves - 1, (step - 1) / CkRouteSteps);
            var on = CheskaRoutePoint(curve, ((step - 1) % CkRouteSteps + within) / CkRouteSteps) - CkRoute[curves, 0];
            return new Vector3(on.x, 0, on.y) * width;
        }

        /// <summary>
        /// How wide the heart is. He asked for two to three times the first (2.6 m), twice ("wayy too small"). ⚠️ IT IS NOW
        /// ALWAYS 4.4 m (1.7 times; the whole line is then 12 m across), WHOEVER IS IN THE WAY: `CkHeartLeast` equals `CkHeartMost`, so the search below never runs. With
        /// a smaller least it shrank the heart to keep her clear of the others, and with three people and a can on a court
        /// that was nearly always, back to the size he had refused (film c19). So she may pass through somebody's block of
        /// ice. Lower `CkHeartLeast` to have the clearance back.
        /// </summary>
        private float CheskaHeartSize()
        {
            for (float width = CkHeartMost; width > CkHeartLeast + .01f; width -= .2f)
            {
                bool clear = true;
                for (int i = 0; i <= 120 && clear; i++)
                {
                    var at = CheskaHeart(i / 120f, width);
                    foreach (var statue in _ckStatues)
                        if ((statue.Home - at).magnitude < (statue.Body != null ? .95f : .5f)) { clear = false; break; }
                }
                if (clear) return width;
            }
            return CkHeartLeast;
        }

        /// <summary>Where she is at this moment (scene space, her feet): on the loop she opens with, on the heart, or where she stands.</summary>
        private Vector3 CheskaSkate(float t)
        {
            if (t > CkCircleFrom && t < CkCircleTo)
            {
                float u = Mathf.InverseLerp(CkCircleFrom, CkCircleTo, t);
                // She comes in already moving and bleeds her speed only at the very end, where she stands.
                // Her pace along it is the line's own (`_ckRouteClock`): quick down its sweeps, slow round its turns.
                return CheskaHeart(u, _ckHeartSize);
            }
            int last = CkWayAt.Length - 1;
            if (t <= CkWayAt[0] || t >= CkWayAt[last]) return Vector3.zero;
            int i = 0;
            while (i < last - 1 && t >= CkWayAt[i + 1]) i++;
            float along = Mathf.InverseLerp(CkWayAt[i], CkWayAt[i + 1], t);
            // She gathers speed out of the push and bleeds it coming home.
            if (i == 0) along *= along; else if (i == last - 1) along = 1 - (1 - along) * (1 - along);
            Vector3 p0 = CkWay[Mathf.Max(0, i - 1)], p1 = CkWay[i], p2 = CkWay[i + 1], p3 = CkWay[Mathf.Min(last, i + 2)];
            // Catmull-Rom through the points: a loop, never a corner.
            return .5f * (2f * p1 + (p2 - p0) * along + (2f * p0 - 5f * p1 + 4f * p2 - p3) * along * along + (3f * p1 - p0 - 3f * p2 + p3) * along * along * along);
        }

        private Vector3 CheskaHeading(float t)
        {
            var way = CheskaSkate(t + .04f) - CheskaSkate(t - .04f); way.y = 0;
            return way.sqrMagnitude > 1e-5f ? way.normalized : Vector3.forward;
        }

        /// <summary>Which of the three she is looking at now, or -1.</summary>
        private static int CheskaLookAt(float t)
        {
            for (int look = CkLooks.Length - 1; look >= 0; look--) if (t >= CkLooks[look]) return t < CkLooks[look] + CkLookFor[look] ? look : -1;
            return -1;
        }

        private Vector3 CheskaHead => _head != null ? _root.transform.InverseTransformPoint(_head.position) : new Vector3(0, 1.3f, 0);

        /// <summary>The middle of whoever the look is on.</summary>
        private Vector3 CheskaTarget(int which) => _ckPanels[which].Home + Vector3.up * _ckPanels[which].Height * .56f;

        /// <summary>How far the turn to this look has gone (0 to 1, and a hair past for the whips), by its own clock.</summary>
        private static float CheskaTurned(int which, float since)
        {
            float turn = which == 0 ? .34f : .17f;
            float turned = Mathf.Clamp01(since / turn);
            turned = which == 0 ? turned * turned * (3 - 2 * turned) : 1 - (1 - turned) * (1 - turned) * (1 - turned);
            float past = which == 0 ? 0 : .05f * Mathf.Sin(Mathf.Clamp01(since / (turn * 1.7f)) * Mathf.PI) * (since > turn * .9f ? 1 : 0);
            return turned + past;
        }

        /// <summary>
        /// Shots 1 to 3 are THROUGH HER EYES: the view turns from the last one she looked at to the next, then zooms in until
        /// they fill the frame, and kicks as she points. The first is unhurried; the others are quick. The wide shot is taken
        /// from the side nobody stands on.
        /// </summary>
        private void CheskaFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == CkWideShot) { eye.x *= _ckWideSide; look.x *= _ckWideSide; return; }
            // The skate is watched from in front and to one side too, and the same choice is made for it (film c11 watched
            // the whole loop from behind somebody's hat).
            if (index == 0) { eye.x *= _ckSkateSide; look.x *= _ckSkateSide; return; }
            // His route is shot from straight above the middle of all of it (the heart and both tails), from as high as holds
            // it, the frame square to her so the heart stands upright in it.
            if (index == CkWideShot - 1)
            {
                float width = _ckHeartSize, drift = Ease(CkCircleFrom, CkCircleTo, t);
                var middle = new Vector3(-1.305f * width, 0, -.17f * width);
                look = middle;
                // ⚠️ Nearly straight down, leaning a fixed tenth toward her side of the court and NEVER sideways. Looking exactly
                // down, the frame has no up of its own and whichever way the lens drifted became "up": the heart came out
                // lying on its side and turning (film c20). The lean keeps the lobes up the frame, as he drew them.
                float high = width * Mathf.Lerp(2.3f, 2.2f, drift) + 1f;
                eye = middle + new Vector3(0, high, -.1f * high);
                fov = 46f;
                return;
            }
            int which = index - CkFirstLook;
            if (which < 0 || which >= _ckPanels.Count) return;
            var caught = _ckPanels[which];
            float since = t - CkLooks[which];
            var at = CheskaTarget(which);
            var from = which == 0 ? CheskaHead + Vector3.forward * 6f : CheskaTarget(which - 1);
            // The turn: a slow, easy one to the first of them, a whip to the others that lands a hair past and comes back.
            float turn = which == 0 ? .34f : .17f, pointed = CkPointIn[which];
            look = Vector3.LerpUnclamped(from, at, CheskaTurned(which, since));
            // The lens is her eyes. Her body is not drawn while it is (her own goggles and hat would stand across the view).
            eye = CheskaHead + new Vector3(0, .1f, .12f);
            // The zoom: until they are about 3.4 m of frame tall, wherever they are; done just before she points.
            float tight = Mathf.Clamp(2f * Mathf.Atan(1.7f / Mathf.Max(1f, (at - eye).magnitude)) * Mathf.Rad2Deg, 12f, 42f);
            fov = Mathf.Lerp(64f, tight, Ease(turn, pointed - .14f, since)) * (1 - .1f * Flash(since, pointed + .05f, .09f));
        }

        /// <summary>Each block of ice landing, the stomp, and the glacier bursting.</summary>
        private Vector3 CheskaShake(float t)
        {
            float size = DanteKick(t, CkStomp, .15f, .55f) + DanteKick(t, CkSnap, .06f, .25f);
            // Small ones: the lens is zoomed in when these land, and a shake is that much bigger through it.
            for (int look = 0; look < CkLooks.Length; look++) size += DanteKick(t, CkLooks[look] + CkPointIn[look] + .1f, .02f, .18f);
            return new Vector3(Mathf.Sin(t * 67f), Mathf.Sin(t * 89f + 1.1f), 0) * size;
        }

        private void SampleCheska(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            SampleCheskaSkate(t, leave);
            int looking = CheskaLookAt(t);
            // Through her own eyes she does not see her own head. `enabled`, not `forceRenderingOff`: the phase sets that one
            // itself on every capture.
            foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = looking < 0;
            if (_heldRenderers != null) foreach (var surface in _heldRenderers) if (surface != null) surface.enabled = looking < 0;
            if (looking < 0 && _ckSelf != null) _ckSelf.SetActive(false);

            // The frost blown off her fingertip.
            float blown = Ease(CkBlow, CkBlow + .12f, t) * (1 - Ease(CkBlow + .45f, CkBlow + 1.25f, t)), carried = Ease(CkBlow, CkBlow + 1.25f, t);
            Place(_ckBreath, HeadPoint + new Vector3(0, -.3f, .35f + carried * .9f), Vector3.one * Mathf.Lerp(.08f, .34f, carried), Quaternion.Euler(-90, 0, 0), blown * leave);

            // Her world arrives with the glacier: the pale sky, the pines, the frozen ground.
            float world = Ease(CkStomp, CkStomp + .22f, t) * leave;
            Tint(_ckMistLow, world); Tint(_ckMistHigh, world);
            for (int i = 0; i < _ckPines.Count; i++)
            {
                float angle = (180 - 95 + i * 21) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 13.4f, 0, Mathf.Cos(angle) * 13.4f);
                Place(_ckPines[i], at, new Vector3(.9f, 3.4f + (i * 41 % 5) * .5f, .9f), Quaternion.identity, world > .01f ? 1 : 0);
            }
            Place(_ckFloor, Vector3.up * .035f, Vector3.one * 13.5f * Ease(CkStomp, CkStomp + .4f, t) + Vector3.up, Quaternion.identity, world);
            Place(_ckPlates, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.4f, 2.6f, Ease(CkStomp, CkStomp + .18f, t)), Quaternion.Euler(0, 15, 0), world);

            // Everyone else: loose until their snap, then a block of ice comes down over them and they are done.
            foreach (var statue in _ckStatues)
            {
                float age = t - statue.FrozenAt; bool caught = age >= 0;
                if (statue.Body != null)
                {
                    float sway = caught || _reducedEffects ? 0 : Mathf.Sin(t * 3.1f + statue.Body.Reach) * 3.5f;
                    statue.Body.Holder.transform.localRotation = Quaternion.Euler(0, statue.Body.Yaw + sway, sway * .5f);
                    // ⚠️ NOT FADED AT THE END: play resumes with them frozen, so they are frozen on the last frame.
                    FrostOther(statue.Body.Renderers, caught ? 1 : 0);
                }
                // Down in two frames, squashed where it lands, then standing.
                float fallen = Mathf.Clamp01(age / .07f), settle = Mathf.Clamp01((age - .07f) / .16f);
                float tall = caught ? Mathf.Lerp(.72f, 1f, settle * settle * (3 - 2 * settle)) : 1, wide = 1 + (1 - tall) * .7f;
                Place(statue.Cube, statue.Home + Vector3.up * (1 - fallen) * 3.2f, new Vector3(.74f * wide, statue.Height * 1.14f * tall, .74f * wide),
                    Quaternion.Euler(0, 45 + statue.Home.x * 17, 0), caught ? 1 : 0);
            }

            // Through her eyes: the hand, and on the snap the frame goes graphic behind whoever she is looking at.
            bool graphic = false;
            if (looking >= 0 && _performance != null && ShotCount > CkFirstLook + looking)
            {
                var caught = _ckPanels[looking];
                _performance.Shot(CkFirstLook + looking, t, out var eye, out var look, out float fov);
                CheskaFrame(CkFirstLook + looking, t, ref eye, ref look, ref fov);
                float since = t - CkLooks[looking], snapped = since - CkPointIn[looking];
                SampleCheskaArm(eye, look, fov, since, looking);
                if (snapped >= 0)
                {
                    graphic = true;
                    var middle = caught.Home + Vector3.up * caught.Height * .56f;
                    var forward = (middle - eye).normalized; var facing = Quaternion.LookRotation(forward, Vector3.up);
                    float pop = Mathf.Clamp01(snapped / .07f) * (1 + .35f * Mathf.Clamp01(1 - Mathf.Abs(snapped - .09f) / .06f));
                    Place(_ckBackdrop, middle + forward * 2.8f, new Vector3(40f, 24f, 1), facing, 1); Tint(_ckBackdrop, 1, CkPanelColour[looking]);
                    Place(_ckBackFloor, caught.Home + Vector3.up * .07f, new Vector3(30f, 1, 30f), Quaternion.identity, 1); Tint(_ckBackFloor, 1, CkPanelColour[looking]);
                    Place(_ckBurstBack, middle + forward * 1.3f, Vector3.one * 3.5f * pop, facing * Quaternion.Euler(-90, -snapped * 80f, 0), 1); Tint(_ckBurstBack, 1, CkPanelStar[looking]);
                    Place(_ckBurst, middle + forward * 1.0f, Vector3.one * 2.4f * pop, facing * Quaternion.Euler(-90, snapped * 100f, 0), 1);
                    Tint(_ckBurst, 1, looking == 1 ? new Color(.16f, .40f, .62f, 1) : Color.white);
                }
            }
            if (!graphic) { Tint(_ckBackdrop, 0); Tint(_ckBackFloor, 0); Tint(_ckBurstBack, 0); Tint(_ckBurst, 0); }

            // The glacier: every spire up in three frames with a kick past its height, standing, and gone on her snap.
            float burst = Ease(CkSnap, CkSnap + .14f, t);
            foreach (var spire in _ckSpires)
            {
                float age = t - spire.From;
                float up = age < 0 ? 0 : Mathf.Clamp01(age / .1f) * (1 + .25f * Mathf.Clamp01(1 - Mathf.Abs(age - .12f) / .08f));
                Place(spire.Piece, spire.At, new Vector3(spire.Width, Mathf.Max(.01f, spire.Height * up * (1 - burst)), spire.Width),
                    Quaternion.Euler(spire.Lean * .4f, spire.Turn, spire.Lean), age >= 0 && burst < .99f ? leave : 0);
            }
            for (int i = 0; i < _ckMoteGlows.Count; i++)
                PlaceGlow(_ckMoteGlows[i], CkMotes[i] + Vector3.up * Flash(t, CkSnap + .2f, .5f) * .5f, Vector3.one * (.09f + (i % 3) * .03f) * (1 + 2.5f * Flash(t, CkSnap + .12f, .3f)),
                    Quaternion.identity, world * .9f);

            // Shards from her hands as they snap apart.
            Vector3 gather = CheskaGather;
            for (int i = 0; i < _ckShards.Count; i++)
            {
                float age = t - CkSnap, u = Mathf.Clamp01(age / .3f);
                float angle = i * Mathf.PI * 2 / _ckShards.Count;
                var at = gather + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .7f, .3f) * u * .9f;
                Place(_ckShards[i], at, Vector3.one * .06f * (1 - u * .5f), Quaternion.Euler(angle * 57, 0, 90), age >= 0 ? (1 - u) * leave : 0);
            }
        }

        /// <summary>Her body on the loop and on the circle, and the ice each leaves.</summary>
        private void SampleCheskaSkate(float t, float leave)
        {
            var feet = CheskaSkate(t); var way = CheskaHeading(t);
            if (_bodyRoot != null)
            {
                // Turned the way she goes while she glides, one slow turn on the spot as the loop ends, and exactly as she began
                // whenever she is standing.
                float gliding = Ease(CkWayAt[0], CkWayAt[0] + .2f, t) * (1 - Ease(CkSpinFrom - .2f, CkSpinFrom, t))
                              + Ease(CkCircleFrom, CkCircleFrom + .15f, t) * (1 - Ease(CkCircleTo - .18f, CkCircleTo, t));
                float yaw = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg * gliding + 360f * Ease(CkSpinFrom, CkSpinTo, t);
                var ahead = CheskaHeading(t + .12f);
                float bank = Mathf.Clamp(Vector3.SignedAngle(way, ahead, Vector3.up) * .75f, -28f, 28f) * gliding;
                _bodyRoot.transform.SetPositionAndRotation(_ckBodyAt + _facing * feet,
                    _facing * Quaternion.Euler(0, yaw, -bank) * Quaternion.Inverse(_facing) * _ckBodyTurn);
            }
            // The loop's ribbon melts as she turns to the first of them; the ring stays for the glacier to come up through.
            CheskaRibbon(_ckTrail, CkWayAt[0], CkWayAt[CkWayAt.Length - 1], t, t < CkLooks[0] + .2f && leave > .02f);
            CheskaRibbon(_ckRing, CkCircleFrom, CkCircleTo, t, t < CkStomp + .35f && leave > .02f);
        }

        private void CheskaRibbon(LineRenderer ribbon, float from, float to, float t, bool shown)
        {
            if (ribbon == null) return;
            float upTo = Mathf.Clamp(t, from, to - .001f);
            for (int i = 0; i < ribbon.positionCount; i++)
                ribbon.SetPosition(i, CheskaSkate(Mathf.Lerp(from + .001f, upTo, i / (float)(ribbon.positionCount - 1))) + Vector3.up * .05f);
            ribbon.enabled = shown && t > from + .05f;
        }

        /// <summary>
        /// Her arm through her eyes: the copy of her is held to the FRAME (its shoulder at the bottom corner, the same size on
        /// screen however far the lens has zoomed), and on it the arm comes up with the elbow closed, the fist by her cheek,
        /// then swings to their line and opens until it is straight at them, a little past and back. The elbow is a hinge
        /// about the forearm's own Y, as every clip folds it.
        /// </summary>
        /// <summary>
        /// ⚠️ THE ARM, MEASURED (2026-10-09). Three placements of this arm were tried without seeing them and none showed.
        /// The editor's cutscene film sets this, and every frame the arm is up writes where its shoulder, elbow and every
        /// surface of the copy really are IN THE LENS'S OWN SPACE (right, up, forward in metres, then where that falls on
        /// the frame, -1 to 1 each way), to `Logs/paete-ability-film/arm_<tag>.txt`. Null in a match, and unread.
        /// </summary>
        public static System.Text.StringBuilder CheskaArmLog;

        private void SampleCheskaArm(Vector3 eye, Vector3 look, float fov, float since, int which)
        {
            if (_ckSelf == null || _ckUpper == null) return;
            float pointed = CkPointIn[which], length = CkLookFor[which];
            float raised = Ease(pointed - .54f, pointed - .26f, since) * (1 - Ease(length - .14f, length, since));
            _ckSelf.SetActive(raised > .01f);
            if (raised <= .01f) return;
            var forward = (look - eye).normalized; var right = Vector3.Cross(Vector3.up, forward).normalized; var above = Vector3.Cross(forward, right);
            float half = Mathf.Tan(fov * .5f * Mathf.Deg2Rad), zoom = half / Mathf.Tan(32f * Mathf.Deg2Rad);
            float side = _heldItem != null ? -1 : 1;
            // The thrust: fast, a little too far, and back.
            float thrust = Mathf.Clamp01((since - (pointed - .1f)) / .1f); thrust = 1 - (1 - thrust) * (1 - thrust);
            float over = .1f * Mathf.Sin(Mathf.Clamp01((since - pointed) / .2f) * Mathf.PI);

            // The copy faces the way she looks, the size the zoom leaves it.
            _ckSelf.transform.localRotation = Quaternion.LookRotation(forward, above) * _ckSelfTurn;
            // ⚠️ Her true size. Half as big again, with the shoulder at the lens, her arm and chest were the whole picture (film c16).
            _ckSelf.transform.localScale = _ckSelfScale * zoom;

            // Her arm, in the scene's space. Bent: the upper arm forward and down from the shoulder, a little out, so the
            // elbow leads. Straight: along her line of sight, turned a little in to the middle of the frame, where they are.
            var to = (forward * .9f + above * .35f - right * .25f * side).normalized;
            var upper = Vector3.Slerp((forward * .8f - above * .3f + right * .12f * side).normalized, to, thrust + over * .3f);
            // The elbow closes toward her own face (up and in), which is where the fist is before it goes.
            var close = (above - right * .25f * side + forward * .2f).normalized;
            var front = (close - upper * Vector3.Dot(close, upper)).normalized;
            var aimed = _root.transform.rotation * Quaternion.LookRotation(front, Vector3.Cross(front, upper * _ckArmAlong));
            _ckUpper.localRotation = _ckUpperRest;
            _ckUpper.rotation = Quaternion.Slerp(_ckUpper.rotation, aimed, raised);
            _ckFore.localRotation = Quaternion.Euler(0, -_ckArmAlong * Mathf.Lerp(96f, 0f, thrust) * raised, 0);

            // Then the whole copy is slid so that shoulder sits on the frame where a first-person arm's would: low and to
            // her side, just behind the lens, coming up from under the edge and in as she points.
            // ⚠️ MEASURED 2026-10-09: 0.2 m IN FRONT of the lens at no zoom (it was 0.25 behind, and nothing of her showed).
            // A fixed 0.75 m, and not a distance that shrinks with the zoom: her copy already shrinks with it, so a seat that came
            // nearer as well made the arm grow on every tighter look until it was the whole picture (films c31, c32).
            float away = .75f;
            // ⚠️ A quarter of a metre BEHIND the lens: her chest is then behind it too, and only the arm comes forward into view.
            float across = Mathf.Lerp(.7f, .6f, thrust) * side, high = Mathf.Lerp(-2.4f, Mathf.Lerp(-1.2f, -1.1f, thrust), raised);
            var seat = eye + forward * away + right * across * half * away * (16f / 9f) + above * high * half * away;
            _ckSelf.transform.position += _root.transform.TransformPoint(seat) - _ckUpper.position;
            if (CheskaArmLog == null) return;
            var lensAt = _root.transform.TransformPoint(eye);
            var lensTurn = Quaternion.Inverse(_root.transform.rotation * Quaternion.LookRotation(forward, above));
            string Seen(Vector3 world)
            {
                var v = lensTurn * (world - lensAt);
                string frame = v.z > .01f ? (v.x / (v.z * half * 16f / 9f)).ToString("0.00") + ", " + (v.y / (v.z * half)).ToString("0.00") : "BEHIND THE LENS";
                return "(" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ", " + v.z.ToString("0.00") + ") frame " + frame;
            }
            CheskaArmLog.AppendLine("look " + which + " since " + since.ToString("0.00") + " raised " + raised.ToString("0.00") + " thrust " + thrust.ToString("0.00")
                + " fov " + fov.ToString("0") + " zoom " + zoom.ToString("0.00") + " scale " + _ckSelf.transform.lossyScale.x.ToString("0.00"));
            CheskaArmLog.AppendLine("  shoulder " + Seen(_ckUpper.position));
            CheskaArmLog.AppendLine("  elbow    " + Seen(_ckFore.position));
            foreach (Transform child in _ckFore) CheskaArmLog.AppendLine("  under the forearm, " + child.name + " " + Seen(child.position));
            foreach (var surface in _ckSelf.GetComponentsInChildren<Renderer>(true))
                CheskaArmLog.AppendLine("  surface " + surface.name + " enabled " + surface.enabled + " active " + surface.gameObject.activeInHierarchy
                    + " forcedOff " + surface.forceRenderingOff + " middle " + Seen(surface.bounds.center) + " size " + surface.bounds.size.ToString("0.00"));
        }
    }
}
