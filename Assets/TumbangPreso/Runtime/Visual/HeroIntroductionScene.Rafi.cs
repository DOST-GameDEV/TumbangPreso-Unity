using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // ILYAS, BAHA: THE TIDE GOES OUT, AND WHAT COMES BACK, 9.2 s (was 8.6; was BREAKWATER, 3.4 s; plan.md § 7 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-09. A first restaging the same day (the tide goes out, the sea stacks up behind him with a boat on
        // top, he shrugs and sends it; films r1, r2) was made unpitched while the owner was away. Shown it, he gave his own idea:
        // "something like paete's where he goes underwater, can be seen riding a shark, jumps out the water, lands on the ground
        // but the shark remains in the air but at a large scale as it dives/crashes down causing a big wave. but i also like this
        // current scene u made". Offered the two joined, with the tide going out as the way under: "rafi's idea sounds good".
        //   1. THE TEASE (0 to 1.46): the two feints, the shrug, the beckon (kept). The sea comes in ankle deep.
        //   2. THE TIDE GOES OUT (1.46 to 2.75), from high: on his pull the water runs off the whole court toward him and past
        //      him and stacks up behind him. Everyone is left on wet sand with fish flopping about.
        //   3. IN (2.75 to 3.4): he turns, leaps and goes into the wall head first, and the lens goes in after him.
        //   4. UNDER (3.4 to 5.5): he arrives through the surface in his own bubbles, the shark comes up under him out of the
        //      dark, and he rides it through the blue, banking, then turning up.
        //   5. OUT (5.5 to 6.4): they burst out of the top of the wall. He lets go and lands on the court.
        //   6. IT STAYS UP (6.4 to 7.75): the shark does not come down with him. It hangs over the court, enormous now, tips
        //      over, and dives.
        //   7. BAHA (7.75 to 9.2), from high: it hits, and the hit is the wave. A ring of water goes out across the court low,
        //      as the ultimate's front really is, everyone it passes is carried a step, the sea is back, and a loose slipper
        //      rides it to his feet ("Leaves with his slipper").
        //
        // ⚠️ THE WATER IS `Shaders/RafiSea` (2026-10-10), on this scene's own clock. The owner, of the flat tinted sheets that
        // stood here: "water doesnt have a good water shader, nor does it have any flowing water and wave effects, maybe take a
        // look at lagooncove again." Every body of water below wears it: its header says why it is not the lagoon's own.
        //
        // ⚠️ THE WAY IN WAS REBUILT THE SAME DAY. The owner: "the part with rafi diving into the big wave doesnt really flow
        // well. the wave is so thin so it looks like he's diving into basically not much water, then it cuts mid-dive to him
        // being underwater". So the wall is a MASS now (three metres through at its foot, a face that leans back and curls
        // over), he is seen to go in (a crown of water off the face, a ring closing where he went, his feet last), the lens
        // follows him to the face and the water closes over IT, and the shot under water opens on him coming down through the
        // surface in the bubbles he brought. That passage took 0.6 s more than it had, which is where the length went.
        //
        // ⚠️ THE UNDERWATER SET IS NOT UNDER THE COURT. It is a closed shell 60 m straight up (`RfDeep`), and his body
        // copy is carried there for those 2.1 s and back. Under a real court there is a real map; up there there is, on the
        // maps so far, nothing. NOT YET SEEN ON A REAL MAP: one with something built that high would show it inside the shell.
        //
        // ⚠️ THE HIT IS DRAWN WITH THE EFFECTS KIT (`UltimateFx`, `Shaders/UltimateErode`; 2026-10-10). Of the crash as it was
        // (the third lathe crown, a wide glow, and the ring passing through the lens): "The crash at 7.8 to 8.0 s looks like
        // icy shards, and the frame goes pale as the wave passes the camera." A lathe with a torn lip is one stiff cup, and
        // five metres of it stood over a lens six metres away. `RafiCrash` has what is there now. The ring and the sea coming
        // back behind it are as they were, but the ring ducks under the ground lens as it passes it (`ShapeRfRing`).
        //
        // ⚠️ THE STILT HOUSES DO NOT BOB. Fixed houses stand on piles; only boats float (owner's lagoon correction, AGENTS.md).
        //
        // Every time below is on the table's clock (`tools/author_ultimate_intros.py`, `rafi`).
        // =========================================================================================
        private const float RfPull = 1.5f, RfDry = 2.5f, RfStacked = 2.9f, RfIn = 2.75f, RfSplash = 3.18f, RfUnder = 3.4f, RfMount = 4.05f, RfOut = 5.5f,
                            RfLetGo = 5.85f, RfLand = 6.4f, RfHung = 6.8f, RfDive = 7.2f, RfCrash = 7.75f, RfWall = 7.2f, RfWide = 6.5f, RfGiant = 5.2f,
                            RfRidden = 1.8f;
        private static readonly Vector3 RfDeep = new Vector3(0, 60f, 0), RfHang = new Vector3(-1.0f, 10.5f, -3.0f), RfHit = new Vector3(0, 0, 4.6f);
        // Where he sits on it (behind its head, ahead of its back fin, which stands far back on a whale shark), where he comes
        // through the surface under water, and where on the wall's face he goes in.
        private static readonly Vector3 RfSeat = new Vector3(0, .31f, .12f), RfArrive = new Vector3(1.7f, 5.4f, -.9f), RfEntry = new Vector3(0, 1.75f, -3.5f);
        private static readonly int RfAgeId = Shader.PropertyToID("_Age"), RfFlowId = Shader.PropertyToID("_Flow"), RfLineId = Shader.PropertyToID("_Line"),
                                    RfRadialId = Shader.PropertyToID("_Radial"), RfCentreId = Shader.PropertyToID("_Centre");
        private Shader _rfSeaShader;
        private int _rfShallows, _rfBed, _rfSheen, _rfShell, _rfSurface, _rfRing, _rfWaveBack, _rfEntryRing, _rfWipe, _rfHitFlash, _rfCrestLight,
                    _rfSlipperGlow, _rfMountGlow;
        private readonly int[] _rfCrowns = new int[3];
        // The hit, when the effects kit is there (`RafiCrash`): the column and the dome of water punched up at the place, the
        // foam that blooms under them, the wash on the court, two crowns of curled water, four single wisps thrown wider,
        // and the heavy water thrown out on arcs.
        private bool _rfKit;
        private UltimateFx.Piece _rfHitWash, _rfHitBloom, _rfHitColumn, _rfHitDome;
        private readonly int[] _rfHitBlobs = new int[RfHeavy.Length];
        private readonly UltimateFx.Piece[] _rfHitCrowns = new UltimateFx.Piece[2], _rfHitWisps = new UltimateFx.Piece[RfThrown.Length];
        private readonly List<int> _rfShafts = new List<int>(5), _rfBubbles = new List<int>(28), _rfMotes = new List<int>(14), _rfGlints = new List<int>(8),
                                   _rfFeet = new List<int>(8), _rfTrail = new List<int>(4);
        private Mesh _rfRingMesh;
        private Vector3[] _rfRingAt;
        private Color[] _rfRingPaint;
        private Vector2[] _rfRingUv;
        private Transform _rfShark, _rfSharkTail, _rfSharkJaw, _rfGhost, _rfGhostTail, _rfGhostJaw, _rfBoat, _rfBoatRig;
        private RafiSharkSpine _rfSpine, _rfGhostSpine;
        private Material _rfSpirit;
        // The small modelled things (`Models/UltimateProps`): the fish on the sand, the ones in the wall and under water, the
        // houses on the far water, and the slipper. A missing model is an empty joint, as the shark's is.
        private readonly List<Transform> _rfStranded = new List<Transform>(5), _rfInWall = new List<Transform>(4), _rfSchool = new List<Transform>(6),
                                         _rfHouses = new List<Transform>(4);
        private Transform _rfTsinelas;
        private int _rfGhostGlow;
        private Vector3 _rfBodyAt, _rfGround, _rfChase;
        private Quaternion _rfBodyTurn;
        private int _seaLow, _foamLine, _seaSky, _wave, _rfSurfaceGlow;
        private readonly List<int> _rfDrops = new List<int>(24);
        private readonly List<int> _ripples = new List<int>(3), _currents = new List<int>(4);
        private static readonly float[] RafiSteps = { .16f, .46f, 1.12f };

        /// <summary>
        /// A piece of his sea: <paramref name="mesh"/> (uv in metres; vertex red is foam, green is depth, alpha its own
        /// opacity) in `Shaders/RafiSea`. Without the shader it is left a plain tinted piece and the cutscene still runs.
        /// </summary>
        private int AddSea(string name, Mesh mesh, int order, float shallow, float deep, float caustic, float cells, float streak, float foam,
                           float glow = .15f, float rim = .3f, int queue = 3000)
        {
            int index = Add(name, mesh, new Color(.2f, .62f, .68f, .6f), 0f, plain: true);
            var piece = _pieces[index];
            piece.Renderer.sortingOrder = order;
            if (_rfSeaShader == null) _rfSeaShader = Resources.Load<Shader>("Shaders/RafiSea");
            if (_rfSeaShader == null) return index;
            var m = new Material(_rfSeaShader) { name = name, renderQueue = queue };
            m.SetFloat("_ShallowAlpha", shallow); m.SetFloat("_DeepAlpha", deep); m.SetFloat("_Caustic", caustic); m.SetFloat("_Scale", cells);
            m.SetFloat("_Streak", streak); m.SetFloat("_FoamAmount", foam); m.SetFloat("_Glow", glow); m.SetFloat("_Rim", rim);
            piece.Renderer.sharedMaterial = m; piece.Color = Color.white;
            VfxRenderTag.Own(piece.Renderer.gameObject, m);
            return index;
        }

        /// <summary>This piece's own sea material, or null where the shader was missing.</summary>
        private Material SeaOf(int index)
        {
            var m = _pieces[index].Renderer.sharedMaterial;
            return m != null && m.shader == _rfSeaShader && _rfSeaShader != null ? m : null;
        }

        /// <summary>The scene's clock, how far the water has run (uv metres) and where it ends, for a piece of sea. Then `Place` it.</summary>
        private void Sea(int index, float age, Vector2 run, float line = 9999f, float radial = 0f, Vector2 centre = default)
        {
            var block = _pieces[index].Block;
            block.SetFloat(RfAgeId, age); block.SetVector(RfFlowId, run); block.SetFloat(RfLineId, line);
            block.SetFloat(RfRadialId, radial); block.SetVector(RfCentreId, centre);
        }

        private void BuildRafi()
        {
            // 15 m, not 8: the high shots stand 12 m out and must be inside their own sea.
            _seaLow = Wall("SeaGround", 0, 1.0f, new Color(.04f, .22f, .25f, .88f), radius: 15f, emission: .1f);
            _foamLine = Wall("SeaHorizonFoam", 1.0f, 1.09f, new Color(.9f, .97f, .95f, .85f), radius: 15f, emission: .5f);
            _seaSky = Wall("SeaSky", 1.09f, 18, new Color(.55f, .78f, .76f, .6f), radius: 15f, emission: .3f, cap: true);
            _rfBed = Add("WetSand", VfxShapes.Prism(32, .02f, 1f), new Color(.38f, .31f, .22f, .9f), emission: .02f, plain: true);
            _pieces[_rfBed].Renderer.sortingOrder = -4;
            // What the tide leaves on the sand: nothing but the last runnels draining after it, and the light on them.
            _rfSheen = AddSea("WetSandRunnels", RfSheet(13f, RfSheetFoamNone, RfSheetDepthFlat), -3, 0f, 0f, .18f, .8f, .55f, 0f, glow: .1f, rim: 0f);
            SeaOf(_rfSheen)?.SetVector("_StreakScale", new Vector4(3.2f, .07f, 0, 0));
            // THE SEA OVER THE COURT. Clear in the shallows over the court itself, caustics on the ground under it, a swell,
            // and foam where it ends: at its far shore, and at the waterline when it runs out and when it comes back.
            _rfShallows = AddSea("Shallows", RfSheet(13f, RfSheetFoamShore, RfSheetDepthShelf), 2, .5f, .84f, .68f, .5f, .0f, 1f, glow: .1f, rim: .15f);
            // Everyone else where they stand: left on the sand when the water goes, carried a step when it comes back.
            StageOthers(3f, 10f);
            if (_bodyRoot != null) { _rfBodyAt = _bodyRoot.transform.position; _rfBodyTurn = _bodyRoot.transform.rotation; }
            // The one shot from the ground: kept off whoever stands near it ONCE, here, so that it does not move afterwards.
            _rfGround = KeepLensOffOthers(new Vector3(2.4f, .35f, 10.5f), 3.5f);
            _rfChase = KeepLensOffOthers(new Vector3(2.3f, 1.6f, 3.7f), 2.0f);
            // Foam round every pair of ankles standing in it, his first.
            for (int i = 0; i <= _others.Count; i++)
                _rfFeet.Add(AddSea("AnkleFoam" + i, RfLathe("Ilyas ankle foam", RfFootOut, RfFootUp, RfFootPaint, 20, 2.4f), 3, 0f, .0f, 0f, 1f, 0f, 1.1f, glow: .2f, rim: 0f));
            for (int i = 0; i < 8; i++) _rfGlints.Add(AddGlow("SeaGlint" + i, new Color(.8f, 1f, .95f, 1f), falloff: 3.2f, core: 1.2f));

            // UNDER: a closed shell far over the court, lit from above: pale turquoise under the surface going down to a deep
            // blue-green, with curtains of light hanging in it; the living underside of the surface; soft shafts; motes; bubbles.
            _rfShell = AddSea("DeepShell", RfLathe("Ilyas deep shell", RfShellOut, RfShellUp, RfShellPaint, 28, 20f), -8, 1f, 1f, 0f, .09f, .22f, 0f, glow: 0f,
                              rim: 0f, queue: 2900);
            var shell = SeaOf(_rfShell);
            if (shell != null)
            {
                shell.SetColor("_Shallow", new Color(.22f, .70f, .74f, 1f)); shell.SetColor("_Deep", new Color(.01f, .11f, .19f, 1f));
                shell.SetColor("_FoamColor", new Color(.42f, .86f, .86f, 1f)); shell.SetVector("_StreakScale", new Vector4(.22f, .012f, 0, 0));
                shell.SetFloat("_Wave", 0f); shell.SetFloat("_CausticWidth", .16f);
            }
            _rfSurface = AddSea("DeepSurface", RfSheet(15.5f, RfSheetFoamNone, RfSheetDepthRim), -7, 1f, 1f, 1.0f, .2f, 0f, 0f, glow: .12f, rim: 0f, queue: 2901);
            var surface = SeaOf(_rfSurface);
            if (surface != null)
            {
                surface.SetColor("_Shallow", new Color(.42f, .84f, .84f, 1f)); surface.SetColor("_Deep", new Color(.05f, .34f, .44f, 1f));
                surface.SetFloat("_Wave", 1f);
            }
            for (int i = 0; i < 5; i++)
            {
                int shaft = AddGlow("DeepShaft" + i, new Color(.5f, .95f, .9f, 1f), RfShaftMesh(), billboard: false, band: true, falloff: 1.5f, core: .12f);
                _pieces[shaft].Renderer.sharedMaterial?.SetFloat("_Tips", .7f);
                _rfShafts.Add(shaft);
            }
            for (int i = 0; i < 14; i++) _rfMotes.Add(AddGlow("DeepMote" + i, new Color(.7f, 1f, .92f, 1f), falloff: 2.6f, core: .9f));
            var bubble = RfLathe("Ilyas bubble", RfBubbleOut, RfBubbleUp, RfBubblePaint, 10, 1f);
            for (int i = 0; i < 28; i++) _rfBubbles.Add(AddSea("DeepBubble" + i, bubble, 4, .08f, .2f, 0f, 1f, 0f, 0f, glow: .6f, rim: 2f));
            _rfSurfaceGlow = AddGlow("DeepSurfaceGlow", new Color(.6f, .95f, .95f, 1f), falloff: 1.6f, core: .2f);
            _rfMountGlow = AddGlow("SharkArrivesGlow", new Color(.3f, .9f, .85f, 1f), falloff: 2.0f, core: .3f);

            BuildRafiShark();
            Transform Prop(string name)
            {
                var model = UltimateProp.Spawn(name, _root.transform, UltimateProp.SeaPalette);
                if (model == null) { model = new GameObject("MissingProp"); model.transform.SetParent(_root.transform, false); }
                model.SetActive(false);
                return model.transform;
            }
            for (int i = 0; i < 4; i++) _rfHouses.Add(Prop("stilthouse"));
            for (int i = 0; i < 5; i++) _rfStranded.Add(Prop("fish"));
            for (int i = 0; i < 4; i++) _rfInWall.Add(Prop("fish"));
            for (int i = 0; i < 6; i++) _rfSchool.Add(Prop("fish"));
            _rfTsinelas = Prop("tsinelas");
            _rfSlipperGlow = AddGlow("FoundSlipperGlow", new Color(1f, .82f, .4f, 1f), falloff: 2.2f, core: .3f);
            for (int i = 0; i < 3; i++) _ripples.Add(Add("FalseStepRipple" + i, VfxShapes.Collar(24, .02f, .82f), new Color(.6f, .9f, .92f, .7f)));
            for (int i = 0; i < 4; i++) _currents.Add(Add("GatheredCurrent" + i, WaterRibbon(), new Color(.25f, .67f, .78f, .5f)));

            // THE WALL. It comes round behind him from one side of the court to the other, and it is a mass of water, not a sheet: its back and top first (darker, nearly solid), then its face in front of
            // them (the court side: it leans back from a foaming foot, stands, and curls over into a breaking crest), so the
            // face is always seen against its own depth. Water climbs the face; the crest is foam; light comes through the top.
            _rfWaveBack = AddSea("BreakwaterBody", RfArcSheet(RfBackOut, RfBackUp, RfBackPaint, 32, 174), 4, 1f, 1f, .3f, .4f, .06f, 1.0f, glow: .08f, rim: .2f);
            _wave = AddSea("BreakwaterFace", RfArcSheet(RfFaceOut, RfFaceUp, RfFacePaint, 32, 170), 6, .74f, .96f, .26f, .4f, .07f, 1.0f, glow: .14f, rim: .45f);
            SeaOf(_wave)?.SetVector("_StreakScale", new Vector4(3f, .3f, 0, 0));
            _rfCrestLight = AddGlow("BreakwaterCrestLight", new Color(.45f, .95f, .85f, 1f), falloff: 1.7f, core: .15f);
            // Thrown water: three crowns (where he goes in, where they come out, where it lands), a ring closing on the face
            // behind him, and drops that are drops.
            var crown = RfLathe("Ilyas splash crown", RfCrownOut, RfCrownUp, RfCrownPaint, 28, 9f, RfCrownRagged);
            // The third (where it lands) is only built where the effects kit is not: a piece nobody places stays drawn.
            _rfKit = UltimateFx.Ready;
            for (int i = 0; i < (_rfKit ? 2 : 3); i++)
            {
                _rfCrowns[i] = AddSea("SplashCrown" + i, crown, 8, .42f, .7f, .15f, .7f, .5f, .8f, glow: .2f, rim: .6f);
                SeaOf(_rfCrowns[i])?.SetVector("_StreakScale", new Vector4(2.2f, .2f, 0, 0));
            }
            _rfEntryRing = AddSea("EntryRing", RfLathe("Ilyas entry ring", RfEntryOut, RfFootUp, RfFootPaint, 24, 5f), 9, 0f, 0f, 0f, 1f, 0f, 1.2f, glow: .2f, rim: 0f);
            var drop = RfLathe("Ilyas thrown drop", RfDropOut, RfDropUp, RfDropPaint, 8, 1f);
            for (int i = 0; i < 24; i++) _rfDrops.Add(AddSea("ThrownWater" + i, drop, 10, .82f, .9f, 0f, 1f, 0f, 0f, glow: .55f, rim: 1.4f));
            // THE WAVE THE HIT MAKES: a breaking ring, rebuilt every frame so that it keeps its own width as it widens.
            _rfRingMesh = new Mesh { name = "Ilyas baha ring" };
            _rfRingMesh.MarkDynamic();
            BuildRfRing();
            _rfRing = AddSea("BahaRing", _rfRingMesh, 7, .66f, .92f, .3f, .6f, .3f, 1.1f, glow: .18f, rim: .4f);
            // Its foam a step under white and a little more broken: from the ground lens the whole ring is seen side on, and
            // at full white its crest was two snow banks either side of the hit (film fx2).
            var ringSea = SeaOf(_rfRing);
            if (ringSea != null) { ringSea.SetColor("_FoamColor", new Color(.78f, .9f, .88f, 1f)); ringSea.SetFloat("_FoamAmount", .92f); }
            if (_rfKit) BuildRafiCrash();
            _rfHitFlash = AddGlow("BahaHitLight", new Color(.4f, .95f, .88f, 1f), falloff: 1.8f, core: .35f);
            _rfWipe = AddGlow("WaterOverTheLens", new Color(.2f, .62f, .66f, 1f), falloff: .35f, core: .25f);
            for (int i = 0; i < 4; i++)
            {
                int trail = AddGlow("GhostDiveTrail" + i, new Color(.45f, .95f, .9f, 1f), RfShaftMesh(), billboard: false, band: true, falloff: 2.2f, core: .3f);
                _pieces[trail].Renderer.sharedMaterial?.SetFloat("_Tips", 1.2f);
                _rfTrail.Add(trail);
            }
        }

        // ---------------------------------------------------------------- the water's shapes, each typed
        // A paint is (foam, depth, 0, opacity): see `Shaders/RafiSea`.

        // The sheet over the court, by rings from its middle to its edge.
        private static readonly float[] RfSheetRings = { 0f, .18f, .42f, .68f, .88f, 1f };
        private static readonly float[] RfSheetFoamShore = { 0f, 0f, 0f, 0f, .3f, 1f }, RfSheetFoamNone = { 0f, 0f, 0f, 0f, 0f, 0f };
        private static readonly float[] RfSheetDepthShelf = { .08f, .1f, .16f, .3f, .55f, .8f }, RfSheetDepthFlat = { .3f, .3f, .3f, .3f, .3f, .3f },
                                        RfSheetDepthRim = { 0f, .06f, .2f, .45f, .75f, 1f };

        private static Mesh RfSheet(float radius, float[] foam, float[] depth)
        {
            const int sides = 40; int rows = RfSheetRings.Length;
            var mesh = new Mesh { name = "Ilyas sea sheet" };
            var at = new Vector3[(sides + 1) * rows]; var uv = new Vector2[at.Length]; var paint = new Color[at.Length];
            var triangles = new int[sides * (rows - 1) * 6]; int n = 0;
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                for (int row = 0; row < rows; row++)
                {
                    int v = i * rows + row;
                    at[v] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (radius * RfSheetRings[row]);
                    uv[v] = new Vector2(at[v].x, at[v].z);
                    paint[v] = new Color(foam[row], depth[row], 0, 1);
                    if (i == sides || row == rows - 1) continue;
                    triangles[n++] = v; triangles[n++] = v + rows + 1; triangles[n++] = v + rows;
                    triangles[n++] = v; triangles[n++] = v + 1; triangles[n++] = v + rows + 1;
                }
            }
            mesh.vertices = at; mesh.uv = uv; mesh.colors = paint; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        /// <summary>
        /// A shape turned about y from a typed profile (how far out, how high, and its paint, row by row). uv is how far round
        /// (<paramref name="around"/> metres for the whole turn) by how far along the profile. <paramref name="ragged"/>
        /// tears the last row's height unevenly round the turn, for the lip of thrown water.
        /// </summary>
        private static Mesh RfLathe(string name, float[] outward, float[] up, Color[] paint, int sides, float around, float[] ragged = null)
        {
            int rows = outward.Length;
            var mesh = new Mesh { name = name };
            var at = new Vector3[(sides + 1) * rows]; var uv = new Vector2[at.Length]; var colours = new Color[at.Length];
            var triangles = new int[sides * (rows - 1) * 6]; int n = 0;
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, along = 0;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                for (int row = 0; row < rows; row++)
                {
                    int v = i * rows + row;
                    float tear = ragged == null ? 1f : Mathf.Lerp(1f, ragged[i % ragged.Length], row == rows - 1 ? 1f : row == rows - 2 ? .45f : 0f);
                    at[v] = rim * outward[row] + Vector3.up * (up[row] * tear);
                    if (row > 0) along += Vector2.Distance(new Vector2(outward[row], up[row]), new Vector2(outward[row - 1], up[row - 1]));
                    uv[v] = new Vector2(around * i / sides, along * around * .16f);
                    colours[v] = paint[row];
                    if (i == sides || row == rows - 1) continue;
                    triangles[n++] = v; triangles[n++] = v + rows; triangles[n++] = v + 1;
                    triangles[n++] = v + rows; triangles[n++] = v + rows + 1; triangles[n++] = v + 1;
                }
            }
            mesh.vertices = at; mesh.uv = uv; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        // A ring of foam lying on the water (round an ankle; on the wall's face where he went in).
        private static readonly float[] RfFootOut = { .5f, .72f, .9f, 1f }, RfFootUp = { 0f, .012f, .012f, 0f }, RfEntryOut = { .76f, .86f, .95f, 1f };
        private static readonly Color[] RfFootPaint = { new Color(0f, .2f, 0, 0f), new Color(1f, .2f, 0, 1f), new Color(.9f, .2f, 0, 1f), new Color(0f, .2f, 0, 0f) };
        // A crown of thrown water: a narrow foot, flaring, its lip torn.
        private static readonly float[] RfCrownOut = { .30f, .40f, .58f, .86f, 1.2f, 1.5f }, RfCrownUp = { 0f, .30f, .58f, .80f, .94f, 1f };
        private static readonly Color[] RfCrownPaint = { new Color(.5f, .7f, 0, .0f), new Color(.1f, .6f, 0, .9f), new Color(.15f, .4f, 0, .9f),
                                                         new Color(.45f, .22f, 0, .85f), new Color(.9f, .1f, 0, .85f), new Color(1f, 0f, 0, .0f) };
        private static readonly float[] RfCrownRagged = { 1f, .78f, .93f, .62f, 1.06f, .84f, .70f, .98f, .74f, 1.04f, .66f, .9f, .8f, 1f };
        // A drop: fat at the front, drawn out behind.
        private static readonly float[] RfDropOut = { 0f, .3f, .42f, .4f, .26f, .1f, 0f }, RfDropUp = { -.5f, -.42f, -.2f, .05f, .4f, .8f, 1.1f };
        private static readonly Color[] RfDropPaint = { new Color(0, .5f, 0, 1), new Color(0, .4f, 0, 1), new Color(0, .25f, 0, 1), new Color(0, .15f, 0, 1),
                                                        new Color(0, .1f, 0, 1), new Color(0, .05f, 0, .9f), new Color(0, 0, 0, .6f) };
        // A bubble: a ball that is nearly all rim.
        private static readonly float[] RfBubbleOut = { 0f, .5f, .87f, 1f, .87f, .5f, 0f }, RfBubbleUp = { -1f, -.87f, -.5f, 0f, .5f, .87f, 1f };
        private static readonly Color[] RfBubblePaint = { new Color(0, 1f, 0, 1), new Color(0, .8f, 0, 1), new Color(0, .5f, 0, 1), new Color(0, .3f, 0, 1),
                                                          new Color(0, .15f, 0, 1), new Color(0, .05f, 0, 1), new Color(0, 0, 0, 1) };
        // The deep: a closed shell seen from inside, light at the top and dark at the bottom.
        private static readonly float[] RfShellOut = { 0f, 8f, 13f, 16f, 16f, 13.5f, 8.5f, 0f }, RfShellUp = { -12f, -10f, -6f, -1f, 3f, 6.5f, 8.5f, 9.2f };
        private static readonly Color[] RfShellPaint = { new Color(0, 1f, 0, 1), new Color(0, 1f, 0, 1), new Color(0, .96f, 0, 1), new Color(0, .82f, 0, 1),
                                                         new Color(0, .56f, 0, 1), new Color(0, .3f, 0, 1), new Color(0, .12f, 0, 1), new Color(0, .04f, 0, 1) };

        // THE WALL'S FACE, from its foot on the sand to the tip of its lip, as shares of its width and its height: it leans
        // back as it rises, stands, then comes forward over him and hangs.
        private static readonly float[] RfFaceOut = { .50f, .535f, .548f, .525f, .465f, .388f, .328f, .312f }, RfFaceUp = { 0f, .12f, .30f, .52f, .72f, .88f, .955f, .905f };
        private static readonly Color[] RfFacePaint = { new Color(.75f, .8f, 0, .9f), new Color(.08f, .98f, 0, .95f), new Color(0f, .94f, 0, .95f), new Color(0f, .74f, 0, .92f),
                                                        new Color(0f, .46f, 0, .88f), new Color(.15f, .2f, 0, .84f), new Color(.9f, .05f, 0, .9f), new Color(1f, 0f, 0, .95f) };
        // ITS TOP AND BACK, from the lip over the crest and down the long back to the sand three metres behind the foot.
        private static readonly float[] RfBackOut = { .312f, .36f, .47f, .62f, .80f, .93f, 1.0f, 1.02f }, RfBackUp = { .905f, .985f, 1f, .97f, .86f, .66f, .36f, 0f };
        private static readonly Color[] RfBackPaint = { new Color(1f, 0f, 0, .95f), new Color(.85f, .1f, 0, .95f), new Color(.3f, .32f, 0, .95f), new Color(0f, .56f, 0, .95f),
                                                        new Color(0f, .76f, 0, .95f), new Color(0f, .9f, 0, .95f), new Color(0f, 1f, 0, .95f), new Color(.6f, 1f, 0, .9f) };

        /// <summary>An arc of the wall behind him, open toward the court, from a typed profile; highest in its middle.</summary>
        private static Mesh RfArcSheet(float[] outward, float[] up, Color[] paint, int sides, float arcDegrees)
        {
            var mesh = new Mesh { name = "Ilyas breakwater" };
            int rows = outward.Length;
            var vertices = new Vector3[(sides + 1) * rows]; var uv = new Vector2[vertices.Length]; var colours = new Color[vertices.Length];
            var triangles = new int[sides * (rows - 1) * 6]; int at = 0;
            for (int i = 0; i <= sides; i++)
            {
                float u = i / (float)sides, a = Mathf.Deg2Rad * (180 - arcDegrees * .5f + arcDegrees * u), along = 0;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                for (int row = 0; row < rows; row++)
                {
                    int v = i * rows + row;
                    vertices[v] = rim * outward[row] + Vector3.up * (up[row] * WaveLipHeight(u));
                    if (row > 0) along += Vector2.Distance(new Vector2(outward[row] * RfWide, up[row] * RfWall), new Vector2(outward[row - 1] * RfWide, up[row - 1] * RfWall));
                    // In metres at the size it stands at, so the foam's grain and the caustics are the court's.
                    uv[v] = new Vector2(u * arcDegrees * Mathf.Deg2Rad * RfWide * .6f, along);
                    colours[v] = paint[row];
                    // Its two ends sink into the far water instead of being cut off.
                    colours[v].a *= Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), .22f);
                    if (i == sides || row == rows - 1) continue;
                    // The sea's shader culls nothing: one winding, not two coplanar draws.
                    triangles[at++] = v; triangles[at++] = v + rows; triangles[at++] = v + 1;
                    triangles[at++] = v + rows; triangles[at++] = v + rows + 1; triangles[at++] = v + 1;
                }
            }
            mesh.vertices = vertices; mesh.colors = colours; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static float WaveLipHeight(float u)
            => 1 - .3f * Mathf.Pow(Mathf.Abs(2 * u - 1), 3.2f) + .035f * Mathf.Sin(u * Mathf.PI * 7);

        // THE RING the hit sends out, across it from just ahead of its front to the wash behind: a steep front that leans
        // forward into a lip, and a long back. How far out from the ring's own line (in its widths), how high, and its paint.
        private static readonly float[] RfRingOut = { .46f, .40f, .54f, .32f, 0f, -.6f, -1.25f, -2.0f }, RfRingUp = { 0f, .42f, .86f, 1f, .9f, .5f, .16f, 0f };
        private static readonly Color[] RfRingPaint = { new Color(.8f, .5f, 0, .0f), new Color(.2f, .62f, 0, .9f), new Color(1f, .2f, 0, .95f), new Color(1f, .1f, 0, .95f),
                                                        new Color(.4f, .3f, 0, .92f), new Color(.3f, .5f, 0, .88f), new Color(.38f, .56f, 0, .8f), new Color(.6f, .4f, 0, 0f) };
        private const int RfRingSides = 56;

        private void BuildRfRing()
        {
            int rows = RfRingOut.Length;
            _rfRingAt = new Vector3[(RfRingSides + 1) * rows]; _rfRingUv = new Vector2[_rfRingAt.Length];
            var paint = _rfRingPaint = new Color[_rfRingAt.Length]; var triangles = new int[RfRingSides * (rows - 1) * 6]; int n = 0;
            for (int i = 0; i <= RfRingSides; i++)
                for (int row = 0; row < rows; row++)
                {
                    int v = i * rows + row; paint[v] = RfRingPaint[row];
                    if (i == RfRingSides || row == rows - 1) continue;
                    triangles[n++] = v; triangles[n++] = v + rows; triangles[n++] = v + 1;
                    triangles[n++] = v + rows; triangles[n++] = v + rows + 1; triangles[n++] = v + 1;
                }
            ShapeRfRing(1f, 1f, 1f);
            _rfRingMesh.triangles = triangles;
        }

        // ⚠️ `under` AND `duck`: THE RING GOES UNDER THE GROUND LENS. That lens stands on the court six metres from the hit,
        // a hand above the ground, and the ring is two metres of foam when it gets there: the frame went white for three
        // frames before the cut (36 per cent of it above 245, film q6). So within a few metres of a place (`under`, in
        // the ring's own space) its crest is pressed down to a swell, by `duck` (0 not at all, 1 fully), and its foam goes
        // with its height: pressed flat and still painted as a crest, it was a white floor across the bottom of the frame
        // instead (19 per cent, film fx2).
        private void ShapeRfRing(float radius, float high, float wide, Vector3 under = default, float duck = 0f)
        {
            int rows = RfRingOut.Length;
            for (int i = 0; i <= RfRingSides; i++)
            {
                float a = i * Mathf.PI * 2 / RfRingSides;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                // The front is not one height all the way round: it surges.
                float surge = 1f + .16f * Mathf.Sin(a * 5f + radius * .9f) + .08f * Mathf.Sin(a * 11f - radius * 1.7f), crest = 1f;
                if (duck > 0f)
                {
                    var front = rim * radius;
                    float near = new Vector2(front.x - under.x, front.z - under.z).magnitude;
                    crest = Mathf.Lerp(1f, Smooth01(1.2f, 4.6f, near), duck);
                    surge *= .07f + .93f * crest;
                }
                for (int row = 0; row < rows; row++)
                {
                    int v = i * rows + row;
                    _rfRingAt[v] = rim * Mathf.Max(.05f, radius + RfRingOut[row] * wide) + Vector3.up * (RfRingUp[row] * high * surge);
                    _rfRingUv[v] = new Vector2(a * 5.5f, RfRingOut[row] * wide - radius * .6f);
                    _rfRingPaint[v].r = RfRingPaint[row].r * crest;
                }
            }
            _rfRingMesh.vertices = _rfRingAt; _rfRingMesh.uv = _rfRingUv; _rfRingMesh.colors = _rfRingPaint;
            _rfRingMesh.RecalculateNormals(); _rfRingMesh.RecalculateBounds();
        }

        /// <summary>A strip for a shaft of light: one long along x, one wide along y, its glow fading across it.</summary>
        private static Mesh RfShaftMesh()
        {
            var mesh = new Mesh { name = "Ilyas light shaft" };
            mesh.vertices = new[] { new Vector3(0, -.5f, 0), new Vector3(1, -.5f, 0), new Vector3(1, .5f, 0), new Vector3(0, .5f, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        /// <summary>
        /// ⚠️ THE SHARK IS A WHALE SHARK, SCULPTED AS ONE SKIN AND PAINTED (`Models/UltimateProps/shark`,
        /// `tools/build_ultimate_shark.py`, whose header has the owner's four notes on the sharks before it and the
        /// measurements this one is built to). It is fierce by ATTITUDE, which was his choice: its true face (tiny eyes, a wide
        /// straight mouth held open and dark), its size, and the way it is flown here: head down hard in the dive. It has no
        /// jaw. It is here TWICE on the same joints: the one he rides, and its spirit, drawn by the shader Makiling is drawn
        /// by in Paete's sky (owner: "the shark should end up becoming ghostlike as it enlargens to the sky, like paete's ult
        /// makiling"). Its nose is along +z, its back is +y, and it is 3.5 m long at a scale of one.
        /// </summary>
        private void BuildRafiShark()
        {
            Transform Joint(GameObject model, string name, Transform parent)
            {
                if (model != null) return name == null ? model.transform : PaeteProp.Find(model, name);
                // A missing model is a warning in the log and an empty joint here: the cutscene still runs, without it.
                var empty = new GameObject("MissingProp").transform; empty.SetParent(parent, false); return name == null ? empty : null;
            }
            var solid = UltimateProp.Spawn("shark", _root.transform, UltimateProp.SharkPalette);
            _rfShark = Joint(solid, null, _root.transform); _rfSharkTail = Joint(solid, "tail", null); _rfSharkJaw = Joint(solid, "mouth", null);
            var spirit = UltimateProp.SpawnSpirit("shark", _root.transform, UltimateProp.SharkPalette, new Color(.42f, .95f, .9f, 1f), new Color(.86f, 1f, .97f, 1f), out _rfSpirit);
            _rfGhost = Joint(spirit, null, _root.transform); _rfGhostTail = Joint(spirit, "tail", null); _rfGhostJaw = Joint(spirit, "mouth", null);
            // Both of them swim with their whole length (`RafiSharkSpine`), on the one beat.
            _rfSpine = RafiSharkSpine.On(solid); _rfGhostSpine = RafiSharkSpine.On(spirit);
            _rfGhostGlow = AddGlow("BahaSharkGhostGlow", new Color(.45f, .95f, .9f, 1f), falloff: 1.8f, core: .25f);
            var boat = UltimateProp.Spawn("lepa", _root.transform, UltimateProp.SeaPalette);
            _rfBoat = Joint(boat, null, _root.transform); _rfBoatRig = Joint(boat, "rig", null);
        }

        private static float Smooth01(float from, float to, float value)
        { float u = Mathf.Clamp01((value - from) / (to - from)); return u * u * (3 - 2 * u); }

        /// <summary>Where the shark swims under water, by how far through that passage it is: up out of the dark behind him,
        /// across in front of the lens in one long weave, and steeply up toward the light at the end.</summary>
        private static Vector3 RafiSwim(float u)
            => RfDeep + new Vector3(Mathf.Sin(u * 3.1f) * 2.5f - 1f, -2.2f + u * u * 1.6f + Smooth01(.70f, 1.05f, u) * 5.2f, -6.5f + u * 10.5f);

        // (Cut back hard after q5, which the owner called "too squiggly": in the air it barely wags at all, as a breaching
        // whale shark barely does, and under water the wave is moderate. `RafiSharkSpine` keeps it in the tail third.)
        // ⚠️ ITS BEAT, typed as moments: when, how fast it is beating there (radians a second) and how far its tail tip goes
        // each way (metres at its own size). SLOW AND WIDE while it glides under him, faster and harder as it turns up for
        // the surface and throws itself out of the wall, almost still while it hangs, and a hard drive down. How far
        // through its beat it is at a time is the SUM of how fast it has been beating until then, so its speed can change
        // without the wave ever jumping (a sine of `t` times a changing speed does).
        private static readonly float[] RfBeatAt = { RfUnder, RfOut - .5f, RfOut - .15f, RfOut + .35f, RfHung, RfDive - .1f, RfDive + .3f, RfCrash + .3f };
        private static readonly float[] RfBeatFast = { 5.2f, 5.6f, 9f, 6f, 2.4f, 2.4f, 4.5f, 4.5f };
        private static readonly float[] RfBeatWide = { .22f, .24f, .28f, .12f, .05f, .05f, .09f, .09f };

        private static void RafiBeat(float t, out float phase, out float sweep)
        {
            int last = RfBeatAt.Length - 1;
            float now = Mathf.Clamp(t, RfBeatAt[0], RfBeatAt[last]);
            phase = 0; sweep = RfBeatWide[last];
            for (int i = 0; i < last; i++)
            {
                float from = RfBeatAt[i], to = RfBeatAt[i + 1];
                if (now >= to) { phase += (RfBeatFast[i] + RfBeatFast[i + 1]) * .5f * (to - from); continue; }
                float s = (now - from) / (to - from);
                phase += (RfBeatFast[i] + Mathf.Lerp(RfBeatFast[i], RfBeatFast[i + 1], s)) * .5f * (now - from);
                sweep = Mathf.Lerp(RfBeatWide[i], RfBeatWide[i + 1], s * s * (3 - 2 * s));
                break;
            }
        }

        // ⚠️ ITS LENGTH LIES ALONG ITS ROAD (owner 2026-10-10, of the shark flown on one rotation: "too rigid as it goes up in
        // the underwater scene before the jump up shot. and even in the jump-dive shot the shark is rigid. the shark looks
        // like this as it goes up -> / then it pivots around the center of its body, still straight -> - then it goes down,
        // still straight -> \"). One rotation is a straight stick at every instant. So each section of it is given ITS OWN
        // way to point: the way the path was going a moment AGO for a section behind the middle (the tail the longest ago,
        // and later still toward its tip, so it arrives last), and the way the path WILL be going a moment from now for a
        // section ahead of it (the head leads the turn). `RafiSharkSpine` lays the body through those, end to end.
        //   * It is by TIME and not by distance along the path on purpose: at the top of the leap it is six times life size
        //     and barely moving, and its own length is far more road than it travels there, so a body laid on the road
        //     itself would fold back on itself. By time, the arch it made coming up eases out as it hangs and forms again
        //     the other way as it tips over, with nothing to snap.
        //   * It is EXAGGERATED (`RfBendMore`), because this is a drawn world and the curve has to read in a still frame;
        //     and it is LIMITED (`RfBendMost` degrees off its middle's own way at either end) so the skin never folds.
        //   * Its tail WHIPS over the top after the rest of it: one flick past where it is going, and back.
        //   * And it is drawn out long as it drives up, pressed a little short as it coils before that and as it hangs.
        private const float RfLead = .42f, RfLag = .52f, RfBendMore = 1.12f, RfBendMost = 46f;
        private static readonly Vector3[] RfWays = new Vector3[RafiSharkSpine.Sections + 1];
        private static readonly float[] RfStretchAt = { RfOut - .55f, RfOut - .2f, RfOut + .12f, RfLetGo + .25f, RfHung + .1f, RfDive, RfDive + .3f, RfCrash };
        private static readonly float[] RfStretchBy = { 1f, .96f, 1.07f, 1f, .96f, .97f, 1.05f, 1.05f };

        /// <summary>Which way the shark's road is going at a moment, in the scene's space, on the road it is on at <paramref name="now"/>.</summary>
        private static Vector3 RafiWay(float when, float now)
        {
            if (now < RfOut)
            {
                // Not past the end of the water: the road stops rising there, and its head would look down before the cut.
                float u = Mathf.Min((when - RfUnder) / (RfOut - RfUnder), 1.03f);
                return (RafiSwim(u + .01f) - RafiSwim(u - .01f)).normalized;
            }
            var going = RafiSharkPath(when + .03f) - RafiSharkPath(when - .03f);
            return going.sqrMagnitude > 1e-8f ? going.normalized : Vector3.up;
        }

        /// <summary>Fill <paramref name="ways"/> with each section's own way, in the shark's frame (<paramref name="turn"/>); returns how far it is drawn out.</summary>
        private static float RafiSpine(float t, Quaternion turn, Vector3[] ways)
        {
            const float middle = RafiSharkSpine.Nose / RafiSharkSpine.Long;
            var back = Quaternion.Inverse(turn);
            float since = t - (RfDive + .12f);
            // One settle, damped out inside a single swing.
            float flick = since > 0 && t >= RfOut ? 11f * Mathf.Exp(-7f * since) * Mathf.Sin(8f * since) : 0f;
            int last = ways.Length - 1;
            for (int k = 0; k <= last; k++)
            {
                float share = k / (float)last, ahead = middle - share;
                float when = ahead >= 0 ? t + ahead * RfLead : t - Mathf.Pow(-ahead / (1 - middle), 1.3f) * (1 - middle) * RfLag;
                var way = back * RafiWay(when, t);
                float off = Vector3.Angle(Vector3.forward, way);
                way = Vector3.SlerpUnclamped(Vector3.forward, way, Mathf.Min(RfBendMore, RfBendMost / Mathf.Max(off, .01f)));
                if (flick != 0f && share > .68f) { float tip = (share - .68f) / .32f; way = Quaternion.Euler(flick * tip * tip, 0, 0) * way; }
                ways[k] = way;
            }
            float stretch = RfStretchBy[RfStretchBy.Length - 1];
            if (t <= RfStretchAt[0]) return RfStretchBy[0];
            for (int i = 0; i < RfStretchAt.Length - 1; i++)
                if (t < RfStretchAt[i + 1]) { stretch = Mathf.Lerp(RfStretchBy[i], RfStretchBy[i + 1], Smooth01(RfStretchAt[i], RfStretchAt[i + 1], t)); break; }
            return stretch;
        }

        /// <summary>Where his seat is on its bending back at a moment, and how the back is turned there, in the scene's space.</summary>
        private static void RafiSeat(float t, Vector3 sharkAt, Quaternion sharkTurn, float sharkScale, out Vector3 seat, out Quaternion turn)
        {
            RafiBeat(t, out float beat, out float wide);
            float stretch = RafiSpine(t, sharkTurn, RfWays);
            RafiSharkSpine.Lay(RfWays, stretch, beat, wide);
            RafiSharkSpine.Seat(RfSeat, stretch, out var at, out var on);
            seat = sharkAt + sharkTurn * (at * sharkScale); turn = sharkTurn * on;
        }

        /// <summary>
        /// Where the shark is, which way it points and how big it is, in the scene's space. Under water it weaves toward the
        /// lens and turns up; it comes out of the top of the wall, climbs to `RfHang` growing as it goes, hangs, tips over and
        /// dives at `RfHit`, faster all the way.
        /// </summary>
        private static void RafiShark(float t, out Vector3 at, out Quaternion turn, out float scale)
        {
            // 1.8 while he rides it: at 1 he was bigger than his shark (film r3).
            scale = RfRidden;
            if (t < RfOut)
            {
                float u0 = (t - RfUnder) / (RfOut - RfUnder);
                at = RafiSwim(u0);
                var way = (RafiSwim(u0 + .02f) - RafiSwim(u0 - .02f)).normalized;
                // It swims with its whole length (`RafiSharkSpine` bends it): its head swings a little against the wave that
                // runs down it, and it leans into each turn of the weave.
                RafiBeat(t, out float beat, out float wide);
                turn = Quaternion.LookRotation(way, Vector3.up) * Quaternion.Euler(0, -Mathf.Sin(beat - .6f) * 6f * wide, -Mathf.Cos(u0 * 3.1f) * 22f);
                return;
            }
            // ⚠️ ONE CURVE FROM THE WALL TO THE COURT (owner 2026-10-10, of straight lines eased end to end: "it's also tweened so
            // poorly that it doesnt look like its smoothly jumping out the water and diving down"). It is thrown out of the wall
            // already going fast, slows as a thrown thing slows, is nearly still at the top, tips over and falls faster and
            // faster: four typed moments (where it is and how fast it is going), joined so that neither its place nor its speed
            // ever jumps. And its NOSE FOLLOWS ITS PATH, so it turns over the top because its path does, not on a dial.
            scale = Mathf.Lerp(RfRidden, RfGiant, Ease(RfLetGo, RfHung, t));
            at = RafiSharkPath(t);
            var going = RafiSharkPath(t + .03f) - RafiSharkPath(t - .03f);
            // ⚠️ ITS BACK IS CARRIED ALONG ITS ROAD, NOT HELD TO THE WORLD (owner, playing q5: "and then flips around? so now
            // its facing backwars as it dives"). Its road is a loop in one upright plane: up, over toward the court, down.
            // Held to a fixed "up" (it was held with its back to the wall, and rolled as it climbed besides), its back was
            // right going up and WRONG coming down: at the top it had to turn over about its own length to keep it. So its
            // back is always the OUTSIDE of the loop (its road's way crossed with the one line that never changes, the
            // loop's own axis across the court): away from the lens as it climbs, to the sky over the top, toward the lens
            // as it comes down nose first, and never turned over on the way. No roll is added.
            var nose = going.sqrMagnitude > 1e-6f ? going.normalized : Vector3.up;
            var spine = Vector3.Cross(nose, Vector3.right);
            turn = Quaternion.LookRotation(nose, spine.sqrMagnitude > 1e-6f ? spine.normalized : Vector3.back);
        }

        private static readonly Vector3 RfBreach = new Vector3(0f, 4.4f, -RfWide * .5f - .2f);
        private static readonly float[] RfPathAt = { RfOut, RfHung, RfDive, RfCrash };
        private static readonly Vector3[] RfPathPlace =
            { RfBreach, RfHang, RfHang + new Vector3(.2f, .25f, .9f), RfHit + Vector3.up * 1.2f };
        // Metres a second at each: out of the wall, drifting at the top, tipping, and at the court.
        // ⚠️ IT COMES OUT OF THE MIDDLE OF THE WAVE, THROUGH ITS CREST, AND COMES DOWN ON THE SAME LINE. For one film (q5) it
        // came out of the wall well to one side so that its arch would be side-on to the fixed lens; the owner, playing it:
        // "rafi's whale exits off-center from the middle of the wave". Its road bows a metre to one side over the top, no
        // more, and starts and ends on the wave's own centre line.
        private static readonly Vector3[] RfPathSpeed =
            { new Vector3(-1.1f, 11f, 1.4f), new Vector3(-.1f, 1.0f, 1.9f), new Vector3(.5f, -2.6f, 2.6f), new Vector3(1.0f, -38f, 9f) };

        private static Vector3 RafiSharkPath(float t)
        {
            if (t <= RfPathAt[0]) return RfPathPlace[0] + RfPathSpeed[0] * (t - RfPathAt[0]);
            // Through the court and on down: it does not stop at the ground, the ground stops showing it.
            if (t >= RfPathAt[3]) return RfPathPlace[3] + RfPathSpeed[3] * (t - RfPathAt[3]);
            int i = t < RfPathAt[1] ? 0 : t < RfPathAt[2] ? 1 : 2;
            float span = RfPathAt[i + 1] - RfPathAt[i], s = (t - RfPathAt[i]) / span, s2 = s * s, s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * RfPathPlace[i] + (s3 - 2 * s2 + s) * span * RfPathSpeed[i]
                 + (-2 * s3 + 3 * s2) * RfPathPlace[i + 1] + (s3 - s2) * span * RfPathSpeed[i + 1];
        }

        /// <summary>
        /// HIM, wherever the stage carries him (the scene's space; zero and no turn while he stands where he stood): into the
        /// wall, down through the water onto the shark, on its back, and off it onto the court.
        /// </summary>
        private static void RafiBody(float t, out Vector3 feet, out Quaternion lean)
        {
            feet = Vector3.zero; lean = Quaternion.identity;
            if (t >= RfIn && t < RfUnder)
            {
                // He turns his back on the court, takes it at a run and goes in head first: slow off the sand, fast at the
                // face, level by the time his hands reach it (`RfSplash`), his feet the last of him to go.
                float u = (t - RfIn) / (RfUnder - RfIn), go = Mathf.Clamp01((u - .26f) / .74f);
                feet = new Vector3(0, 1.55f * Mathf.Sin(go * Mathf.PI * .5f), -.25f * Smooth01(0, .3f, u) - 4.5f * Mathf.Pow(go, 1.25f));
                lean = Quaternion.Euler(0, 180f * Smooth01(0, .34f, u), 0) * Quaternion.Euler(86f * Smooth01(0, .62f, go), 0, 0);
                return;
            }
            RafiShark(Mathf.Max(t, RfUnder), out var sharkAt, out var sharkTurn, out float sharkScale);
            // He sits ON its back, which is bending under him.
            RafiSeat(Mathf.Max(t, RfUnder), sharkAt, sharkTurn, sharkScale, out var seat, out var seated);
            if (t >= RfUnder && t < RfMount)
            {
                // Down through the surface, fast and slowing as water slows a body, head first, onto the back that is coming
                // up to meet him.
                float s = (t - RfUnder) / (RfMount - RfUnder), sunk = 1 - Mathf.Pow(1 - s, 2.3f);
                var from = RfDeep + RfArrive;
                feet = Vector3.Lerp(from, seat, sunk);
                var falling = Quaternion.FromToRotation(Vector3.up, (seat - from).normalized + Vector3.forward * .25f);
                lean = Quaternion.Slerp(falling, seated, Smooth01(.45f, 1f, s));
            }
            else if (t >= RfMount && t < RfLetGo) { feet = seat; lean = seated; }
            else if (t >= RfLetGo && t < RfLand)
            {
                // He lets go at the top of the wall and comes down to where he stood, facing the court again.
                RafiShark(RfLetGo, out var gone, out var goneTurn, out float goneScale);
                RafiSeat(RfLetGo, gone, goneTurn, goneScale, out var from, out var fromTurn);
                float u = (t - RfLetGo) / (RfLand - RfLetGo);
                feet = Vector3.Lerp(from, Vector3.zero, u) + Vector3.up * (u * (1 - u) * 5.2f);
                lean = Quaternion.Slerp(fromTurn, Quaternion.identity, Mathf.Clamp01(u * 1.4f)) * Quaternion.Euler(-360f * Ease(0, .8f, u), 0, 0);
            }
        }

        /// <summary>Where the lens that follows him into the wall is: from beside him on the sand to the face itself.</summary>
        private Vector3 RafiChase(float t)
            => Vector3.Lerp(_rfChase, new Vector3(1.15f, 1.9f, -.9f), Smooth01(RfIn + .1f, RfUnder, t));

        /// <summary>Where the lens under water is: beside the shark's road, drifting up the way it is going.</summary>
        private static Vector3 RafiDeepLens(float t)
        {
            float u = Mathf.Clamp01((t - RfUnder) / (RfOut - RfUnder));
            return RfDeep + new Vector3(6.2f - u * 2.2f, 1.3f + u * .9f, -1.4f + u * 6.6f);
        }

        private Vector3 RafiShake(float t)
        {
            float size = DanteKick(t, RfLand, .03f, .2f) + DanteKick(t, RfCrash, .15f, .55f) + DanteKick(t, RfOut + .12f, .05f, .3f)
                       + .012f * Ease(RfDive, RfCrash, t) * (t < RfCrash ? 1 : 0);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        private void SampleRafi(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The sea comes in with the beckon: he is inviting you onto his water.
            float sea = Ease(.85f, 1.5f, t) * leave;
            Tint(_seaLow, sea); Tint(_foamLine, sea); Tint(_seaSky, sea);
            for (int i = 0; i < 4; i++)
            {
                float angle = (180 - 55 + i * 34) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 14f, 0, Mathf.Cos(angle) * 14f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                // A built thing does not fade in: each house comes up out of the far water as the sea arrives, one after another,
                // and goes back under as the stage leaves. Turned a little off the lens each, so they are not four of one picture.
                float up = Ease(.9f + i * .12f, 1.5f + i * .12f, t) * leave;
                var house = _rfHouses[i];
                house.gameObject.SetActive(up > .01f);
                house.localPosition = at + Vector3.down * (1 - up) * 5.5f;
                house.localRotation = face * Quaternion.Euler(0, -24f + i * 17f, 0);
                house.localScale = Vector3.one * (1.45f + (i % 2) * .2f);
            }

            // Every false step leaves a ripple at his feet.
            for (int i = 0; i < _ripples.Count; i++)
            {
                float age = t - RafiSteps[i], u = Mathf.Clamp01(age / .5f);
                Place(_ripples[i], Vector3.up * .02f, Vector3.one * Mathf.Lerp(.3f, 1.3f, u), Quaternion.identity, age >= 0 ? (1 - u) * .8f * leave : 0);
            }

            // ------------------------------------------------------------- the tide
            // Ankle deep over the whole court. On his pull its far edge comes across the court toward him, quicker as it goes,
            // foam at the waterline and everything on it streaming his way; it goes past him into the wall. After the shark
            // hits it comes back from where it hit, behind the ring.
            float goes = Mathf.Clamp01((t - RfPull) / (RfDry - RfPull)); goes = goes * goes * (3 - 2 * goes); goes *= goes;
            float hit = t - RfCrash, ring = Mathf.Clamp01(hit / 1.45f); ring = 1 - Mathf.Pow(1 - ring, 1.6f);
            float ringAt = Mathf.Lerp(1.2f, 17f, ring);
            float deep = Ease(.85f, 1.5f, t);
            bool returned = hit >= 0, away = t > RfDry + .05f && !returned;
            if (returned) Sea(_rfShallows, t, new Vector2(0, t * .12f), ringAt - .2f, 1f, new Vector2(RfHit.x, RfHit.z));
            else Sea(_rfShallows, t, new Vector2(0, t * .12f + 11f * goes), t < RfPull ? 9999f : Mathf.Lerp(15f, -8.5f, goes));
            Place(_rfShallows, new Vector3(0, .13f, 0), Vector3.one, Quaternion.identity, away ? 0 : deep * leave);
            float bare = Ease(RfPull, RfPull + .5f, t) * (1 - Ease(RfCrash + .3f, RfCrash + .95f, t)) * leave;
            Place(_rfBed, new Vector3(0, .03f, 0), new Vector3(12.5f, 1, 12.5f), Quaternion.identity, bare);
            Sea(_rfSheen, t, new Vector2(0, 3f * goes + t * .5f));
            Place(_rfSheen, new Vector3(0, .05f, 0), Vector3.one, Quaternion.identity, bare * Mathf.Lerp(1f, .45f, Ease(RfDry, RfDry + 1.2f, t)));
            // Foam round every ankle the water reaches: there while it is in, gone as its edge passes them.
            for (int i = 0; i < _rfFeet.Count; i++)
            {
                Vector3 stood = i == 0 ? Vector3.zero : _others[i - 1].Home;
                float wet = returned ? Ease(0, .25f, ringAt - (stood - RfHit).magnitude) : away ? 0 : deep * (t < RfPull ? 1 : 1 - Ease(-.6f, .4f, stood.z - Mathf.Lerp(15f, -8.5f, goes)));
                Sea(_rfFeet[i], t + i, new Vector2(t * .3f, 0));
                float lap = .4f + .05f * Mathf.Sin(t * 3.1f + i * 1.9f);
                Place(_rfFeet[i], new Vector3(stood.x, .15f, stood.z), new Vector3(lap, 1, lap), Quaternion.identity, wet * leave * (i == 0 && t >= RfIn && t < RfLand ? 0 : 1));
            }
            // And the light on it: a few glints that come and go.
            for (int i = 0; i < _rfGlints.Count; i++)
            {
                float a = (25 + i * 47) * Mathf.Deg2Rad, r = 2.5f + (i * 5 % 7) * 1.3f;
                float wink = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * (2.3f + i * .4f) + i * 2.4f)), 6f);
                var at = new Vector3(Mathf.Sin(a) * r, .2f, Mathf.Cos(a) * r);
                float lit = returned ? Ease(0, 1f, ringAt - (at - RfHit).magnitude) : away ? 0 : deep * (1 - Ease(RfPull, RfPull + .4f, t));
                PlaceGlow(_rfGlints[i], at, Vector3.one * (.5f + .3f * wink), Quaternion.identity, lit * wink * .8f * leave);
            }

            // What the water left behind: a few fish, flopping.
            float stranded = Ease(RfPull + .6f, RfDry, t) * (1 - Ease(RfCrash, RfCrash + .4f, t)) * leave;
            for (int i = 0; i < _rfStranded.Count; i++)
            {
                float a = (35 + i * 71) * Mathf.Deg2Rad, r = 3.2f + (i * 3 % 5) * 1.25f;
                float flop = Mathf.Sin(t * (11f + i) + i * 2.1f);
                var at = new Vector3(Mathf.Sin(a) * r, .08f + Mathf.Abs(flop) * .16f, Mathf.Cos(a) * r * .8f + 1.5f);
                // On its side on the sand, bucking: the tail slaps one way as the head lifts the other.
                var fish = _rfStranded[i];
                fish.gameObject.SetActive(stranded > .02f);
                fish.localPosition = at + Vector3.up * .06f; fish.localRotation = Quaternion.Euler(flop * 16f, i * 67 + flop * 12f, 90f);
                fish.localScale = Vector3.one * (1.5f * Mathf.Min(1f, stranded * 2f));
                var flap = PaeteProp.Find(fish.gameObject, "tail");
                if (flap != null) flap.localRotation = Quaternion.Euler(0, -flop * 38f, 0);
            }

            // Currents gather at his palms while he hauls it.
            float current = Ease(RfPull, RfPull + .4f, t) * (1 - Ease(RfIn - .2f, RfIn, t));
            for (int i = 0; i < _currents.Count; i++)
            {
                float angle = i * Mathf.PI * .5f + t * 2.2f;
                var palm = i % 2 == 0 ? FreePalm : RightPalm;
                var origin = palm + new Vector3(Mathf.Cos(angle) * .12f, .02f + i * .02f, Mathf.Sin(angle) * .12f);
                Place(_currents[i], origin, Vector3.one * Mathf.Lerp(.25f, .7f, current), Quaternion.Euler(8, i * 90 + t * 60, 18), current * leave);
            }

            // ------------------------------------------------------------- the wall
            // WHERE IT WENT. The wall stacks up behind him as the court empties and stands there breathing, water climbing its
            // face all the while. It heaves as they come out of the top of it, and it is knocked flat when the shark hits.
            float stack = Ease(RfPull + .3f, RfStacked, t);
            float heave = Mathf.Sin(Mathf.Clamp01((t - RfOut) / .7f) * Mathf.PI) * .9f;
            float flat = Ease(RfCrash, RfCrash + .35f, t);
            float height = Mathf.Lerp(Mathf.Lerp(.05f, RfWall, stack) + Mathf.Sin(t * 2.1f) * .12f * stack + heave, .2f, flat);
            // (Gone inside the hit's first quarter second, under the pop and the thrown water. Left longer, pressed flat, it was
            // all crest: two white mounds either side of the hit for a third of a second, film fx1.)
            float waveOn = stack * leave * (1 - Ease(RfCrash + .03f, RfCrash + .24f, t));
            var standing = new Vector3(RfWide, height, RfWide);
            // It climbs fastest while it is being fed, and never stops.
            float climbed = t * 1.5f + 5f * stack;
            Sea(_rfWaveBack, t, new Vector2(t * .2f, climbed * .6f));
            Place(_rfWaveBack, Vector3.zero, standing, Quaternion.identity, waveOn);
            Sea(_wave, t, new Vector2(Mathf.Sin(t * .7f) * .3f, -climbed));
            Place(_wave, Vector3.zero, standing, Quaternion.identity, waveOn);
            // The sun is behind it: the thin water at its crest is lit through.
            PlaceGlow(_rfCrestLight, new Vector3(0, height * .86f, -RfWide * .42f), new Vector3(13f, 5.5f, 1f), Quaternion.identity,
                waveOn * Ease(1f, 4f, height) * (.34f + .05f * Mathf.Sin(t * 2.6f)) * (t >= RfUnder && t < RfOut ? 0 : 1));
            // The sea he stacked up still has its fish in it: four of them, seen through the face of the wall, swimming up it
            // as if nothing had happened. They are what says it is water standing there and not a hill.
            float walled = Ease(RfStacked - .5f, RfStacked, t) * (1 - Ease(RfCrash - .1f, RfCrash + .1f, t)) * leave;
            for (int i = 0; i < _rfInWall.Count; i++)
            {
                var fish = _rfInWall[i];
                float lane = (i - 1.5f) * 1.9f, swim = Mathf.Repeat(t * (.16f + i * .04f) + i * .31f, 1f);
                fish.gameObject.SetActive(walled > .02f && height > 2f);
                // Inside the mass, between its face and its back, climbing and drifting across.
                float high = Mathf.Lerp(.1f, .62f, swim);
                fish.localPosition = new Vector3(lane + Mathf.Sin(t * 1.3f + i * 2f) * .5f, height * high, -RfWide * (.66f + .08f * Mathf.Sin(i * 1.7f)));
                fish.localRotation = Quaternion.Euler(-50f, (i % 2 == 0 ? 90f : -90f) + Mathf.Sin(t * 2f + i) * 18f, 0);
                fish.localScale = Vector3.one * (2.2f * walled);
                var flap = PaeteProp.Find(fish.gameObject, "tail");
                if (flap != null) flap.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 12f + i * 1.7f) * 30f, 0);
            }

            // The boat on top of it. It rocks; it is thrown clear when they burst out beside it.
            float rock = Mathf.Sin(t * 2.6f);
            float thrown = Mathf.Clamp01((t - RfOut) / .8f);
            var deck = new Vector3(2.1f, height * 1.0f + rock * .06f, -RfWide * .47f) + new Vector3(thrown * 3.2f, thrown * 5f - thrown * thrown * 7f, -thrown * 1.2f);
            var keel = Quaternion.Euler(thrown * 240, 25, rock * 9 + thrown * 150);
            float afloat = Ease(RfDry - .3f, RfStacked, t) * (1 - Ease(RfOut + .55f, RfOut + .8f, t)) * leave;
            // A solid thing does not fade: it grows up out of the crest as the wall forms and is gone when it is thrown clear.
            _rfBoat.gameObject.SetActive(afloat > .02f);
            _rfBoat.localPosition = deck; _rfBoat.localRotation = keel; _rfBoat.localScale = Vector3.one * (1.2f * Mathf.Min(1f, afloat * 1.5f));
            if (_rfBoatRig != null) _rfBoatRig.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 1.9f) * 12f, 0);

            // ------------------------------------------------------------- the shark, and him
            RafiShark(t, out var sharkAt, out var sharkTurn, out float sharkScale);
            bool shark = t >= RfUnder - .02f && t < RfCrash + .16f;
            _rfShark.localPosition = sharkAt; _rfShark.localRotation = sharkTurn; _rfShark.localScale = Vector3.one * sharkScale;
            _rfGhost.localPosition = sharkAt; _rfGhost.localRotation = sharkTurn; _rfGhost.localScale = Vector3.one * sharkScale;
            // It turns to light as it outgrows him, and the light is what goes up into the sky and comes down.
            float ghostly = Ease(RfLetGo + .05f, RfLetGo + .5f, t);
            // IT SWIMS AS ONE LENGTH, LAID ALONG ITS ROAD (`RafiSpine`; owner 2026-10-10: "the shark is too stiff as its swimming"). A wave runs down it from
            // its head, which barely swings, to its tail, which sweeps widest and last (`RafiBeat` is its beat, `RafiSharkSpine`
            // bends the skin). Its side fins work against the bank of each turn and trail while it glides, and are held hard
            // in the dive. The one he rides, the one that leaps and the ghost are all the same animal on the same beat.
            RafiBeat(t, out float beat, out float sweep);
            float bank = t < RfOut ? -Mathf.Cos(Mathf.Clamp01((t - RfUnder) / (RfOut - RfUnder)) * 3.1f) : 0f;
            float easy = 1 - .7f * Ease(RfDive, RfDive + .3f, t);
            float drawn = RafiSpine(t, sharkTurn, RfWays);
            if (shark && ghostly < .6f) SwimRafiShark(_rfSpine, _rfSharkTail, RfWays, drawn, beat, sweep, bank, easy);
            if (shark && ghostly > .01f) SwimRafiShark(_rfGhostSpine, _rfGhostTail, RfWays, drawn, beat, sweep, bank, easy);
            // Its mouth is held open, and eases a little with each beat as a thing breathing does: nothing works it as a jaw.
            var breath = new Vector3(1f, 1f + .12f * Mathf.Sin(beat * .5f), 1f);
            if (_rfSharkJaw != null) _rfSharkJaw.localScale = breath;
            if (_rfGhostJaw != null) _rfGhostJaw.localScale = breath;
            _rfShark.gameObject.SetActive(shark && ghostly < .6f);
            _rfGhost.gameObject.SetActive(shark && ghostly > .01f);
            UltimateProp.Presence(_rfSpirit, ghostly * leave * (1 + .08f * Mathf.Sin(t * 6f)));
            PlaceGlow(_rfGhostGlow, sharkAt, Vector3.one * (sharkScale * 3.4f), Quaternion.identity, shark ? ghostly * .45f * leave : 0);
            // The light it leaves behind it in the dive: four long streaks up the way it came, longer the faster it falls.
            float falling = Ease(RfDive + .1f, RfCrash - .05f, t) * (t < RfCrash + .1f ? 1 : 0);
            for (int i = 0; i < _rfTrail.Count; i++)
            {
                var beside = sharkAt + new Vector3((i - 1.5f) * 1.9f, 1.5f + (i % 2) * 1.2f, (i % 2 == 0 ? -.8f : .9f));
                PlaceGlow(_rfTrail[i], beside, new Vector3(4f + 9f * falling, .7f + (i % 2) * .4f, 1f), Quaternion.Euler(0, 0, 90f), falling * .55f * leave);
            }

            if (_bodyRoot != null)
            {
                // His copy's place and turn in the scene's space; put on the court's own from the place it was handed over at.
                RafiBody(t, out var feet, out var lean);
                _bodyRoot.transform.SetPositionAndRotation(_rfBodyAt + _facing * feet, _facing * lean * Quaternion.Inverse(_facing) * _rfBodyTurn);
            }

            // ------------------------------------------------------------- in
            // Where his hands meet the face a crown of water comes off it toward the court, a ring of foam opens round the
            // place and closes again behind his feet, and drops fly. Then the lens reaches the face and the water takes it.
            float entered = t - RfSplash;
            var outOfFace = Quaternion.FromToRotation(Vector3.up, new Vector3(0, .3f, 1f).normalized);
            Crown(0, t, entered, .42f, RfEntry, outOfFace, .7f, 1.6f, .9f, leave);
            float closing = Mathf.Clamp01(entered / .34f);
            Sea(_rfEntryRing, t, new Vector2(t * .8f, 0));
            Place(_rfEntryRing, RfEntry + new Vector3(0, 0, .12f), Vector3.one * Mathf.Lerp(.5f, 1.25f, Mathf.Sin(closing * Mathf.PI * .8f)),
                outOfFace, entered >= 0 && t < RfUnder ? (1 - closing * closing) * leave : 0);
            float wipe = t < RfUnder ? Ease(RfUnder - .1f, RfUnder - .01f, t) : 1 - Ease(RfUnder, RfUnder + .3f, t);
            var lens = t < RfUnder ? RafiChase(t) : RafiDeepLens(t);
            var ahead = t < RfUnder ? (RfEntry - lens).normalized : (RafiSwim(.2f) - lens).normalized;
            PlaceGlow(_rfWipe, lens + ahead * 1.2f, Vector3.one * 9f, Quaternion.identity, t > RfSplash && t < RfUnder + .3f ? wipe * (t < RfUnder ? .85f : .4f) : 0);

            // ------------------------------------------------------------- under
            float under = t >= RfUnder - .02f && t < RfOut ? 1 : 0;
            Sea(_rfShell, t, new Vector2(t * .35f, 0));
            Place(_rfShell, RfDeep, Vector3.one, Quaternion.identity, under);
            Sea(_rfSurface, t, new Vector2(t * .4f, t * .25f));
            Place(_rfSurface, RfDeep + Vector3.up * 8.3f, Vector3.one, Quaternion.identity, under);
            PlaceGlow(_rfSurfaceGlow, RfDeep + new Vector3(-1f, 8f, 1f), Vector3.one * 24f, Quaternion.identity, under * .3f);
            for (int i = 0; i < _rfShafts.Count; i++)
            {
                // Shafts from the surface down and across, each swaying on its own, turned edge-on to nothing.
                var head = RfDeep + new Vector3(-6.5f + i * 2.9f, 8.4f, -4f + (i * 5 % 7) * 1.3f);
                float sway = -62f + Mathf.Sin(t * .7f + i * 1.3f) * 3f - i * 2.5f;
                PlaceGlow(_rfShafts[i], head, new Vector3(17f + (i % 2) * 3f, 1.5f + (i % 3) * .7f, 1f), Quaternion.Euler(0, -72f, sway),
                    under * (.2f + .07f * Mathf.Sin(t * 1.9f + i * 2.2f)));
            }
            // Motes hanging in the water, lit: they are what the lens moves past.
            for (int i = 0; i < _rfMotes.Count; i++)
            {
                var at = RfDeep + new Vector3(-5f + (i * 7 % 11) * 1.1f, -3f + (i * 5 % 9) * .9f + Mathf.Sin(t * .8f + i) * .15f, -4f + (i * 3 % 10) * 1.05f);
                PlaceGlow(_rfMotes[i], at, Vector3.one * (.1f + (i % 3) * .05f), Quaternion.identity, under * (.35f + .3f * Mathf.Sin(t * 2.2f + i * 1.7f)));
            }
            // The shark is seen coming before it is seen: a light rising under him.
            PlaceGlow(_rfMountGlow, sharkAt, Vector3.one * 7f, Quaternion.identity, under * .3f * (1 - Ease(RfMount - .1f, RfMount + .5f, t)));
            // A school crossing behind them the other way, so the blue has a far and a near and the shark has something to pass.
            for (int i = 0; i < _rfSchool.Count; i++)
            {
                var fish = _rfSchool[i];
                float across = (t - RfUnder) / (RfOut - RfUnder);
                fish.gameObject.SetActive(under > 0);
                fish.localPosition = RfDeep + new Vector3(-6f + across * 9f + (i % 3) * .9f, -2.4f + (i % 2) * .8f + Mathf.Sin(t * 2.4f + i) * .18f + (i / 2) * .5f, 4.5f - across * 5f - (i / 3) * 1.1f);
                fish.localRotation = Quaternion.Euler(0, 118f + Mathf.Sin(t * 1.6f + i) * 8f, 0);
                fish.localScale = Vector3.one * (1.25f + (i % 3) * .2f);
                var flap = PaeteProp.Find(fish.gameObject, "tail");
                if (flap != null) flap.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 14f + i * 1.3f) * 32f, 0);
            }
            for (int i = 0; i < _rfBubbles.Count; i++)
            {
                Vector3 from; float age, rise, size;
                if (i < 14)
                {
                    // The air he brought down with him: let go along his way in, one after another, and going back up.
                    float born = RfUnder + i * .04f;
                    age = (t - born) / 1.25f; rise = 2.6f + (i % 3) * .9f; size = .09f + (i % 4) * .04f;
                    RafiBody(Mathf.Min(born, RfMount - .01f), out var him, out _);
                    from = him + new Vector3(Mathf.Sin(i * 2.4f) * .35f, .5f + (i % 3) * .2f, Mathf.Cos(i * 1.7f) * .35f);
                }
                else if (i < 22)
                {
                    // Off the shark's back and tail as it drives.
                    age = Mathf.Repeat(t * (1.1f + (i % 3) * .2f) + i * .37f, 1f); rise = 1.3f; size = .06f + (i % 3) * .03f;
                    from = sharkAt + sharkTurn * new Vector3((i % 4 - 1.5f) * .3f, .35f, -1.6f - age * 3.2f);
                }
                else
                {
                    // And a few that only rise through the frame.
                    age = Mathf.Repeat(t * (.5f + (i % 3) * .12f) + i * .41f, 1f); rise = 8f; size = .05f + (i % 3) * .025f;
                    from = RfDeep + new Vector3(-3f + (i * 7 % 9), -5f, -2f + (i * 3 % 8));
                }
                bool seen = age >= 0 && age < 1;
                var wobble = new Vector3(Mathf.Sin(t * 5f + i * 1.9f), 0, Mathf.Cos(t * 4.3f + i)) * (.08f * age);
                Place(_rfBubbles[i], from + Vector3.up * (age * rise) + wobble, Vector3.one * (size * (1 + age * .5f)), Quaternion.identity,
                    seen ? under * Mathf.Min(1f, (1 - age) * 3f) : 0);
            }

            // ------------------------------------------------------------- out
            // They take the top of the wall with them: a crown of it climbs after the shark and falls back.
            var top = new Vector3(RfBreach.x, RfWall * .93f, RfBreach.z + .2f);
            Crown(1, t, t - (RfOut + .1f), .95f, top, Quaternion.identity, 1.8f, 6.5f, 2.2f, leave);

            // ------------------------------------------------------------- the hit, and the wave it makes
            float ringOn = hit >= 0 ? (1 - Ease(RfCrash + 1.0f, RfCrash + 1.4f, t)) * leave : 0;
            // ⚠️ THE RING RISES OUT OF THE HIT, IT IS NOT THERE AT ITS FULL HEIGHT ON THE FIRST FRAME. It was: two and a half
            // metres of foam-topped wall a metre across, standing still round the place for the first frames, which is the
            // thing that read as ice (film fx1, with everything else at the hit already replaced). It comes up as it goes out.
            // (The ground shot ends .35 s after the hit; from the high one after it the ring stands at its full height.)
            if (hit >= 0 && ringOn > 0)
                ShapeRfRing(ringAt, Mathf.Lerp(2.5f, .75f, ring) * Mathf.Lerp(.12f, 1f, Smooth01(.02f, .34f, hit)), Mathf.Lerp(.8f, 1.7f, ring),
                    _rfGround - RfHit, 1 - Ease(RfCrash + .33f, RfCrash + .4f, t));
            Sea(_rfRing, t, new Vector2(0, 0));
            Place(_rfRing, RfHit, Vector3.one, Quaternion.identity, ringOn);
            if (_rfKit) RafiCrash(t, hit, leave);
            else Crown(2, t, hit, .8f, RfHit, Quaternion.identity, 2.4f, 5.6f, 2.4f, leave);
            // Its light comes from INSIDE the water it throws up: aqua, not white, a metre over the place, and gone in a
            // few frames. (It was eighteen metres of glow for half a second, which is part of what turned the frame pale.)
            float flashed = hit >= 0 ? Mathf.Pow(Mathf.Clamp01(1 - hit / .24f), 2f) : 0;
            PlaceGlow(_rfHitFlash, RfHit + Vector3.up * 1.1f, Vector3.one * (3.2f + 3f * Mathf.Clamp01(hit / .24f)), Quaternion.identity, _reducedEffects ? flashed * .15f : flashed * .5f * leave);
            // Ten drops go up off the hit and come down round it; six leave the top of the wall with the shark; eight come off
            // the face where he went in. Each points the way it is going, fat when it leaves and thinning as it falls.
            for (int i = 0; i < _rfDrops.Count; i++)
            {
                int kind = i < 10 ? 0 : i < 16 ? 1 : 2;
                float age = kind == 0 ? hit - (i % 3) * .03f : kind == 1 ? (t - RfOut - .1f) - (i % 3) * .04f : entered - (i % 4) * .02f;
                float life = kind == 0 ? 1.0f : kind == 1 ? .9f : .5f;
                float u = Mathf.Clamp01(age / life), a = (i * (kind == 0 ? 36f : kind == 1 ? 60f : 45f) + 20f) * Mathf.Deg2Rad;
                Vector3 from = kind == 0 ? RfHit : kind == 1 ? top : RfEntry;
                float reach = kind == 0 ? 2.4f + (i % 3) * 1.5f : kind == 1 ? 1.4f + (i % 2) * 1.1f : .5f + (i % 3) * .3f;
                float peak = kind == 0 ? 6.5f + (i % 4) * 1.6f : kind == 1 ? 3.4f + (i % 3) * .9f : .9f + (i % 2) * .5f;
                // Off the face they go OUT toward the court more than up.
                var across = kind == 2 ? new Vector3(Mathf.Sin(a) * reach, Mathf.Cos(a) * reach * .5f, 1.6f + (i % 3) * .5f) : new Vector3(Mathf.Sin(a) * reach, 0, Mathf.Cos(a) * reach);
                var at = from + across * u + Vector3.up * (peak * (u - u * u) * 2f);
                var way = (across + Vector3.up * (peak * (1 - 2 * u) * 2f)).normalized;
                float size = (kind == 0 ? .5f : kind == 1 ? .36f : .2f) * (1 - u * .5f);
                Sea(_rfDrops[i], t + i, Vector2.zero);
                Place(_rfDrops[i], at, new Vector3(size * .6f, size * 1.25f, size * .6f), Quaternion.FromToRotation(Vector3.up, -way), age >= 0 && u < 1 ? leave : 0);
            }

            // Everyone the ring passes is carried one step outward with it, once, and set down.
            for (int i = 0; i < _others.Count; i++)
            {
                var other = _others[i];
                var out_ = other.Home - RfHit; out_.y = 0;
                float reached = RfCrash + Mathf.Clamp01(out_.magnitude / 15.8f) * .75f;
                float carried = Ease(reached, reached + .3f, t);
                float lifted = Mathf.Sin(Mathf.Clamp01((t - reached) / .3f) * Mathf.PI);
                other.Holder.transform.localPosition = other.Home + (out_.sqrMagnitude > .01f ? out_.normalized : Vector3.forward) * (carried * .6f) + Vector3.up * (lifted * .18f);
            }

            // And a loose slipper rides it from where the shark went in to his feet, and is lit when it gets there.
            float ride = Mathf.Clamp01((t - (RfCrash + .2f)) / .8f);
            float slide = 1 - (1 - ride) * (1 - ride);
            var slipperAt = Vector3.Lerp(RfHit + new Vector3(.5f, 1.4f, -.6f), new Vector3(.14f, .16f, .55f), slide) + Vector3.up * (Mathf.Sin(ride * Mathf.PI) * .35f);
            slipperAt.y += ride >= 1 ? Mathf.Sin(t * 3.4f) * .025f : 0;
            var slipperTurn = Quaternion.Euler(0, Mathf.Lerp(250, 12, slide), Mathf.Sin(ride * 9f) * 14 * (1 - ride));
            float slipperOn = ride > 0 ? leave : 0;
            // Twice life size on purpose: at its real size it was not seen from the high shot (film r1).
            _rfTsinelas.gameObject.SetActive(slipperOn > .02f);
            _rfTsinelas.localPosition = slipperAt; _rfTsinelas.localRotation = slipperTurn; _rfTsinelas.localScale = Vector3.one * (2.1f * Mathf.Min(1f, slipperOn * 3f));
            PlaceGlow(_rfSlipperGlow, slipperAt + Vector3.up * .1f, Vector3.one * (1.6f + .2f * Mathf.Sin(t * 5f)), Quaternion.identity, Ease(.75f, 1f, ride) * .45f * leave);
        }

        private struct RfWisp { public string Shape; public int Look; public float Turn, Lean, Long, Late, Life; }
        // THE FOUR SINGLE WISPS the hit throws wider than its crowns, each typed: which ribbon, which mask it is torn
        // by, which way round the place it goes (degrees; the ground lens is at about 22, and none is thrown straight at
        // it), how far it leans out, how long it gets (metres), how late it leaves and how long it lasts (seconds).
        private static readonly RfWisp[] RfThrown =
        {
            new RfWisp { Shape = "whip", Look = UltimateFx.Streak, Turn = 78f, Lean = 14f, Long = 4.6f, Late = .06f, Life = .56f },
            new RfWisp { Shape = "hook", Look = UltimateFx.Smoke, Turn = 148f, Lean = -8f, Long = 5.6f, Late = .09f, Life = .60f },
            new RfWisp { Shape = "whip", Look = UltimateFx.Streak, Turn = 236f, Lean = 20f, Long = 4.0f, Late = .05f, Life = .52f },
            new RfWisp { Shape = "arc", Look = UltimateFx.Smoke, Turn = 304f, Lean = 6f, Long = 5.0f, Late = .10f, Life = .58f },
        };

        private struct RfBlob { public float Turn, Reach, Peak, Size, Late, Life; }
        // THE HEAVY WATER, each piece typed: which way round the place it goes (degrees), how far out it lands and how high
        // it goes (metres), how big it is, how late it leaves and how long it is in the air (seconds). First a ring of
        // eight thrown low and wide, which is the weight of the thing that hit; then five big ones thrown high, which
        // are what is still coming down when everything else has gone.
        private static readonly RfBlob[] RfHeavy =
        {
            new RfBlob { Turn = 52f, Reach = 3.6f, Peak = 1.9f, Size = .62f, Late = .00f, Life = .56f },
            new RfBlob { Turn = 96f, Reach = 4.3f, Peak = 2.5f, Size = .54f, Late = .02f, Life = .64f },
            new RfBlob { Turn = 139f, Reach = 3.2f, Peak = 1.6f, Size = .70f, Late = .00f, Life = .52f },
            new RfBlob { Turn = 181f, Reach = 4.0f, Peak = 2.2f, Size = .58f, Late = .03f, Life = .60f },
            new RfBlob { Turn = 226f, Reach = 3.4f, Peak = 2.7f, Size = .66f, Late = .01f, Life = .66f },
            new RfBlob { Turn = 268f, Reach = 4.4f, Peak = 1.8f, Size = .52f, Late = .00f, Life = .58f },
            new RfBlob { Turn = 311f, Reach = 3.7f, Peak = 2.4f, Size = .64f, Late = .04f, Life = .62f },
            new RfBlob { Turn = 352f, Reach = 3.0f, Peak = 2.0f, Size = .56f, Late = .01f, Life = .54f },
            new RfBlob { Turn = 70f, Reach = 2.6f, Peak = 6.6f, Size = 1.25f, Late = .08f, Life = .92f },
            new RfBlob { Turn = 128f, Reach = 3.5f, Peak = 5.2f, Size = 1.05f, Late = .11f, Life = .84f },
            new RfBlob { Turn = 203f, Reach = 2.2f, Peak = 7.4f, Size = 1.40f, Late = .07f, Life = .98f },
            new RfBlob { Turn = 259f, Reach = 3.3f, Peak = 5.8f, Size = 1.10f, Late = .12f, Life = .88f },
            new RfBlob { Turn = 322f, Reach = 2.8f, Peak = 6.2f, Size = 1.20f, Late = .09f, Life = .90f },
        };

        // THE COLUMN the hit punches up, from its flared foot to its closed top: how far out, how high, and how whole it is
        // (the erode shader reads a vertex's alpha as how much of it is already torn away).
        private static readonly float[] RfColumnOut = { .78f, .56f, .46f, .42f, .38f, .30f, .16f, 0f }, RfColumnUp = { 0f, .10f, .30f, .54f, .76f, .90f, .98f, 1f };
        private static readonly Color[] RfColumnPaint = { new Color(1, 1, 1, .55f), new Color(1, 1, 1, 1f), new Color(1, 1, 1, 1f), new Color(1, 1, 1, 1f),
                                                          new Color(1, 1, 1, 1f), new Color(1, 1, 1, .9f), new Color(1, 1, 1, .7f), new Color(1, 1, 1, .3f) };
        // THE DOME that swells round its foot: a low bell.
        private static readonly float[] RfDomeOut = { 1f, .97f, .86f, .66f, .40f, .16f, 0f }, RfDomeUp = { 0f, .22f, .50f, .74f, .90f, .98f, 1f };
        private static readonly Color[] RfDomePaint = { new Color(1, 1, 1, .5f), new Color(1, 1, 1, 1f), new Color(1, 1, 1, 1f), new Color(1, 1, 1, 1f),
                                                        new Color(1, 1, 1, .9f), new Color(1, 1, 1, .7f), new Color(1, 1, 1, .4f) };
        // A heavy drop: rounder and fatter than the thrown drops, with a short tail.
        private static readonly float[] RfBlobOut = { 0f, .34f, .50f, .52f, .40f, .20f, 0f }, RfBlobUp = { -.5f, -.42f, -.2f, .08f, .4f, .7f, .95f };

        /// <summary>
        /// A solid form of water in the erode shader: one of this stage's own turned shapes worn by a piece of the effects
        /// kit. Its uv is how far round by how far up, so its mask is run UP it, and nothing is read across it.
        /// </summary>
        private UltimateFx.Piece RfForm(Mesh mesh, Color core, Color edge, int look, int order)
        {
            var piece = UltimateFx.Wisp(_root.transform, "arc", core, edge, look, order);
            var filter = piece.Transform != null ? piece.Transform.GetComponent<MeshFilter>() : null;
            if (filter != null) filter.sharedMesh = mesh;
            return piece.With("_Shape", 0f).With("_Sweep", 0f).With("_Lead", 2f);
        }

        /// <summary>Out fast, past its size, and back to it.</summary>
        private static float RfBurst(float u, float past = 1.9f)
        { u = Mathf.Clamp01(u) - 1f; return 1f + (past + 1f) * u * u * u + past * u * u; }

        /// <summary>The hit's own pieces, from the effects kit. Only called when it is there.</summary>
        private void BuildRafiCrash()
        {
            // ⚠️ ALL OF IT IS WATER. A comic pop stood at the instant of contact for one film (fx4: flat black and teal
            // shards, a halftone patch, streak lines); the owner: "oh no, the 2d comic pop doesnt fit rafi's". So the
            // instant is a huge animal hitting water and nothing else: a beat where the water is pressed down and swells,
            // then a thick column thrown up out of a dome, lit from inside, with the foam blooming under it and the
            // heavy water going out on arcs. Every piece is a form with thickness, in his painted teals.
            // His water in three steps and its foam. The foam is a little under white, so that a frame full of it is not a
            // white frame.
            Color deep = new Color(.03f, .33f, .40f, 1f), body = new Color(.06f, .50f, .55f, 1f), aqua = new Color(.20f, .80f, .78f, 1f),
                  foam = new Color(.84f, .94f, .91f, 1f);
            // THE WASH: a disc of foam on the court that races out ahead of the ring and is eaten from the middle along its
            // own streaks, so what reaches the players is streaks and what is left behind is nothing.
            _rfHitWash = UltimateFx.Disc(_root.transform, aqua, foam, UltimateFx.Streak, 5).With("_Glow", .35f).With("_Band", .34f)
                .With("_Scale", new Vector4(1.0f, 9f, 0, 0)).With("_Spread", 1.5f);
            // THE BLOOM: foam on the water under the hit, spreading as lace (cells opening in it) and lying on the sea as it
            // comes back. It is also what the ground lens has in front of it for its last frames, where the returned sea
            // was a blank pale band (films fx2 to fx4, 8.03 to 8.07 s).
            _rfHitBloom = UltimateFx.Disc(_root.transform, foam, new Color(.62f, .9f, .87f, 1f), UltimateFx.Cells, 6).With("_Radial", 0f)
                .With("_Scale", new Vector4(19f, 19f, 0, 0)).With("_Scroll", new Vector4(.25f, .15f, 0, 0)).With("_Warp", .7f)
                .With("_Band", .16f).With("_Glow", .2f).With("_Soft", .05f);
            // THE COLUMN AND THE DOME: turned forms, thick, the water in them running upward.
            _rfHitColumn = RfForm(RfLathe("Ilyas hit column", RfColumnOut, RfColumnUp, RfColumnPaint, 24, 6f), body, foam, UltimateFx.Smoke, 10)
                .With("_Scale", new Vector4(1.4f, .8f, 0, 0)).With("_Scroll", new Vector4(.3f, 3.4f, 0, 0)).With("_Band", .34f)
                .With("_Hot", aqua).With("_Glow", .6f);
            // (The dome is torn by the smoke mask, not the cells: with cells it was a blob with round holes punched in it, film fx5.)
            _rfHitDome = RfForm(RfLathe("Ilyas hit dome", RfDomeOut, RfDomeUp, RfDomePaint, 28, 8f), aqua, foam, UltimateFx.Smoke, 9)
                .With("_Scale", new Vector4(1.1f, 1.3f, 0, 0)).With("_Scroll", new Vector4(.2f, 2.2f, 0, 0)).With("_Band", .36f)
                .With("_Hot", aqua).With("_Glow", .5f);
            // THE CROWNS: curled lobes of water thrown up and out, foam on the leading edge and wherever it is tearing. The
            // mask runs up each lobe to its tip, and each is eaten from its foot, so the water leaves the court and what
            // hangs at the top is streaks. An inner one, narrow and tall, first; an outer one, wide and low, after it.
            for (int i = 0; i < _rfHitCrowns.Length; i++)
            {
                _rfHitCrowns[i] = UltimateFx.Wisp(_root.transform, "crown", i == 0 ? body : deep, foam, i == 0 ? UltimateFx.Streak : UltimateFx.Smoke, 11 + i)
                    .With("_Scale", i == 0 ? new Vector4(1.5f, 1.7f, 0, 0) : new Vector4(2.2f, 1.4f, 0, 0)).With("_Scroll", new Vector4(3.0f, .2f, 0, 0))
                    .With("_Sweep", 1.1f).With("_Shape", .8f).With("_Band", .34f).With("_Lead", .7f).With("_Hot", aqua).With("_Glow", .45f);
                _rfHitCrowns[i].Seed = 1 + i * 2;
            }
            for (int i = 0; i < _rfHitWisps.Length; i++)
            {
                _rfHitWisps[i] = UltimateFx.Wisp(_root.transform, RfThrown[i].Shape, i % 2 == 0 ? aqua : body, foam, RfThrown[i].Look, 12)
                    .With("_Scale", new Vector4(2.0f, 1.6f, 0, 0)).With("_Scroll", new Vector4(3.2f, 0, 0, 0)).With("_Sweep", 1.2f)
                    .With("_Shape", .7f).With("_Band", .32f).With("_Lead", .7f).With("_Hot", aqua).With("_Glow", .5f);
                _rfHitWisps[i].Seed = 5 + i * 1.7f;
            }
            var blob = RfLathe("Ilyas heavy drop", RfBlobOut, RfBlobUp, RfDropPaint, 10, 1f);
            for (int i = 0; i < _rfHitBlobs.Length; i++)
                _rfHitBlobs[i] = AddSea("HeavyWater" + i, blob, 13, .86f, .94f, 0f, 1f, 0f, 0f, glow: .5f, rim: 1.5f);
        }

        /// <summary>
        /// THE HIT, <paramref name="hit"/> seconds after the shark meets the court: a huge animal going into water.
        ///   1. PRESSED (the first frame and a half): the water under it is pushed down and out, a low wide swell.
        ///   2. THROWN: a thick column goes up out of the swell, past its height and back, lit from inside, and the inner
        ///      crown bursts out round it; the dome swells out and sinks; foam blooms on the court; heavy water goes out
        ///      low all round, and five big pieces go high.
        ///   3. FALLING: the outer crown and the single wisps go wide and come apart from the foot up, the column drops
        ///      back into its own foam, and the big pieces come down last.
        /// </summary>
        private void RafiCrash(float t, float hit, float leave)
        {
            const float pressed = .03f;
            float thrown = hit - pressed;

            float washed = Mathf.Clamp01(hit / .62f), reach = Mathf.Lerp(1.4f, 10.5f, 1 - Mathf.Pow(1 - washed, 2.6f));
            UltimateFx.Place(_rfHitWash, RfHit + Vector3.up * .21f, Quaternion.identity, new Vector3(reach, 1f, reach));
            UltimateFx.Set(_rfHitWash, hit, Mathf.Lerp(.3f, 1f, Smooth01(.15f, 1f, washed)), leave);

            // The bloom: out to nine metres in four tenths of a second, thinning to lace, and gone a second after the hit.
            float bloomed = Mathf.Clamp01(hit / .42f), bloom = Mathf.Lerp(.8f, 9.5f, 1 - Mathf.Pow(1 - bloomed, 2.2f));
            UltimateFx.Place(_rfHitBloom, RfHit + Vector3.up * .24f, Quaternion.Euler(0, 33f, 0), new Vector3(bloom, 1f, bloom));
            UltimateFx.Set(_rfHitBloom, hit, Mathf.Lerp(.16f, 1f, Smooth01(.12f, 1.0f, hit)), leave * .9f);

            // The dome: the swell itself. Low and wide while the water is pressed, up and out with the throw, then it
            // sinks as it widens and opens into foam.
            float domed = Mathf.Clamp01(hit / .46f), swell = RfBurst(thrown / .12f, 1.4f);
            // (Wider than the shark from its first frame: the shark is three metres through and still passing through the
            // place for five frames, and a swell inside it is a swell nobody sees. Film fx5 had nothing at all on 7.77.)
            float domeWide = hit < pressed ? Mathf.Lerp(2.4f, 3.0f, hit / pressed) : 3.0f + 1.5f * (1 - Mathf.Pow(1 - Mathf.Clamp01(thrown / .4f), 2f));
            float domeHigh = hit < pressed ? Mathf.Lerp(.9f, .55f, hit / pressed) : Mathf.Lerp(.55f, 2.4f, swell) * (1 - .85f * Smooth01(.14f, .42f, thrown));
            UltimateFx.Place(_rfHitDome, RfHit + Vector3.up * .08f, Quaternion.Euler(0, 200f, 0), new Vector3(domeWide, Mathf.Max(.05f, domeHigh), domeWide));
            UltimateFx.Set(_rfHitDome, hit, Mathf.Lerp(.1f, 1f, Smooth01(.3f, 1f, domed)), leave);

            // The column: nothing while the water is pressed, then up five and a half metres in a tenth of a second, past
            // that and back, narrowing as it stretches; then it drops and thickens into its own foot.
            float columned = Mathf.Clamp01(thrown / .5f), shot = RfBurst(thrown / .13f, 2.2f);
            float columnHigh = 5.6f * shot * (1 - .7f * Smooth01(.3f, 1f, columned));
            float columnWide = Mathf.Lerp(2.1f, 1.45f, Mathf.Clamp01(thrown / .1f)) + .9f * Smooth01(.3f, 1f, columned);
            UltimateFx.Place(_rfHitColumn, RfHit + Vector3.up * .1f, Quaternion.Euler(0, 180f, 0), new Vector3(columnWide, Mathf.Max(.05f, columnHigh), columnWide));
            UltimateFx.Set(_rfHitColumn, thrown, Mathf.Lerp(.08f, 1f, Smooth01(.34f, 1f, columned)), leave);
            // Lit from inside for the first few frames of the throw.
            float lit = _reducedEffects ? 0f : Mathf.Pow(Mathf.Clamp01(1 - thrown / .2f), 2f);
            _rfHitColumn.With("_Emit", .5f * lit); _rfHitDome.With("_Emit", .45f * lit);

            for (int i = 0; i < _rfHitCrowns.Length; i++)
            {
                // The inner one (1) goes with the column, at once, tall and narrow, past its height and back. The outer
                // one (0) is two frames behind it, lower and wider, and leans, so the two are never one ring of spikes.
                // Each is already tearing when it arrives and is gone a little over half a second after the hit.
                bool inner = i == 1;
                float age = thrown - (inner ? 0f : .025f), life = inner ? .56f : .6f, u = Mathf.Clamp01(age / life);
                float up = RfBurst(age / (inner ? .12f : .16f), inner ? 2.1f : 1.4f) * (1 - .4f * Smooth01(.34f, 1f, u));
                float outward = 1 - (1 - u) * (1 - u);
                float across = inner ? .9f + .9f * outward : 1.6f + 1.9f * outward, high = inner ? 6.6f : 3.9f;
                var lean = inner ? Quaternion.Euler(-5f, 66f, 3f) : Quaternion.Euler(7f, 40f, -6f);
                UltimateFx.Place(_rfHitCrowns[i], RfHit + Vector3.up * .1f, lean, new Vector3(across, Mathf.Max(.05f, high * up), across * (inner ? 1f : .86f)));
                UltimateFx.Set(_rfHitCrowns[i], age, Mathf.Lerp(.36f, 1f, Smooth01(.06f, .92f, u)), leave);
            }
            for (int i = 0; i < _rfHitWisps.Length; i++)
            {
                // Each grows out along its own way, quickest at first, and is eaten from its root: a length of water
                // leaving the place, not a shape standing on it.
                var typed = RfThrown[i];
                float age = hit - typed.Late, u = Mathf.Clamp01(age / typed.Life);
                float grown = typed.Long * (.3f + .7f * (1 - Mathf.Pow(1 - Mathf.Clamp01(u / .5f), 2.6f)));
                var turn = Quaternion.Euler(0, typed.Turn, 0) * Quaternion.Euler(typed.Lean, 0, 0);
                UltimateFx.Place(_rfHitWisps[i], RfHit + Vector3.up * .15f, turn, new Vector3(grown * .8f, grown * (1 - .25f * Smooth01(.5f, 1f, u)), grown * (.7f + .3f * u)));
                UltimateFx.Set(_rfHitWisps[i], age, Mathf.Lerp(.3f, 1f, Smooth01(.08f, .92f, u)), leave);
            }
            for (int i = 0; i < _rfHitBlobs.Length; i++)
            {
                // The heavy water: each on its own arc, nose first, fat when it leaves and drawn out as it falls.
                var typed = RfHeavy[i];
                float age = hit - typed.Late, u = Mathf.Clamp01(age / typed.Life), a = typed.Turn * Mathf.Deg2Rad;
                var across = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * typed.Reach;
                var at = RfHit + across * (.2f + .8f * u) + Vector3.up * (.3f + typed.Peak * 4f * (u - u * u));
                var way = (across * .8f + Vector3.up * (typed.Peak * 4f * (1 - 2 * u))).normalized;
                float size = typed.Size * Mathf.Min(1f, .4f + age * 9f) * (1 - u * .35f), drawn = 1.1f + .5f * Mathf.Abs(1 - 2 * u);
                Sea(_rfHitBlobs[i], t + i * 1.3f, Vector2.zero);
                Place(_rfHitBlobs[i], at, new Vector3(size * .62f, size * drawn, size * .62f), Quaternion.FromToRotation(Vector3.up, -way), age >= 0 && u < 1 ? leave : 0);
            }
        }

        /// <summary>Bend one of the two sharks for this moment; where its skin could not be bent, wag its tail joint instead.</summary>
        private static void SwimRafiShark(RafiSharkSpine spine, Transform tail, Vector3[] ways, float drawn, float beat, float sweep, float bank, float easy)
        {
            if (spine != null && spine.Bends) { spine.Swim(ways, drawn, beat, sweep, bank, easy); return; }
            if (tail != null) tail.localRotation = Quaternion.Euler(0, Mathf.Sin(beat) * sweep * 70f, 0);
        }

        /// <summary>
        /// One crown of thrown water, <paramref name="age"/> seconds old of <paramref name="life"/>: it jumps up and out fast,
        /// hangs, and falls back wider than it rose, the water in it running up it all the while.
        /// </summary>
        private void Crown(int which, float t, float age, float life, Vector3 at, Quaternion turn, float wide, float high, float spread, float leave)
        {
            int index = _rfCrowns[which];
            float u = Mathf.Clamp01(age / life);
            float up = Mathf.Sin(Mathf.Pow(u, .55f) * Mathf.PI), outward = 1 - (1 - u) * (1 - u);
            Sea(index, t, new Vector2(which * 3.1f, -age * 5f));
            Place(index, at, new Vector3(wide + spread * outward, Mathf.Max(.05f, high * up), wide + spread * outward), turn,
                age >= 0 && u < 1 ? Mathf.Min(1f, (1 - u) * 2.2f) * leave : 0);
        }

        /// <summary>The lens follows him into the wall; under water it swims beside the shark; the shot after watches it climb and hang.</summary>
        private void RafiFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            RafiShark(t, out var sharkAt, out _, out _);
            if (index == 2)
            {
                // Behind his shoulder as he turns, then after him, to the face of the wall. It never loses him: he is what it
                // looks at until the place he went in is all there is to look at.
                RafiBody(t, out var feet, out _);
                eye = RafiChase(t);
                look = Vector3.Lerp(feet + new Vector3(0, .9f, -.6f), RfEntry, Smooth01(RfSplash - .05f, RfUnder, t));
                fov = Mathf.Lerp(fov, fov + 8f, Smooth01(RfSplash, RfUnder, t));
                return;
            }
            if (index == 3)
            {
                // On him as he comes down, then on the two of them once he is on its back.
                RafiBody(t, out var feet, out _);
                eye = RafiDeepLens(t);
                look = Vector3.Lerp(feet + Vector3.up * .3f, sharkAt + Vector3.up * .4f, Smooth01(RfMount - .35f, RfMount + .25f, t));
                return;
            }
            if (index != 4) return;
            // ⚠️ ONE SHOT FROM THE GROUND, AND THE LENS DOES NOT MOVE (owner 2026-10-09: "camera position stays there, meaning its
            // all in one shot"). It only turns: on the shark as it comes out, then held between him on the court and the shark
            // over it so both stay in the frame, and down with it as it falls in front of him.
            eye = _rfGround;
            var both = new Vector3(0, Mathf.Max(.9f, Mathf.Lerp(1.2f, sharkAt.y, .5f)), Mathf.Lerp(0, sharkAt.z, .5f));
            look = Vector3.Lerp(sharkAt, both, Ease(RfLetGo, RfLand, t));
            // ⚠️ ITS HEAD MUST BE SEEN, AND IN A MATCH THE POWER'S NAME LIES ACROSS THE TOP FIFTH OF THE SCREEN (owner's
            // screenshot from play, q5: the giant's head was off the top of the frame under the banner). So the lens tilts
            // up as far as it takes to keep the nose a fifth of the frame's height below where that banner ends, and no
            // further than still shows him on the court. It still does not move.
            RafiShark(t, out _, out var sharkTurn, out float sharkScale);
            var nose = sharkAt + sharkTurn * Vector3.forward * (RafiSharkSpine.Nose * sharkScale);
            float away = new Vector2(nose.x - eye.x, nose.z - eye.z).magnitude, flat = new Vector2(look.x - eye.x, look.z - eye.z).magnitude;
            float lift = Mathf.Atan2(nose.y - eye.y, Mathf.Max(away, .5f)) * Mathf.Rad2Deg - fov * .5f * (1f - 2f * .18f - .2f);
            // ⚠️ NEVER BELOW -80: once the shark is through the court its nose is far under the lens, the angle passes straight
            // down, and a tangent past straight down comes back POSITIVE. The lens then looked at the empty sky for the last
            // five frames of this shot, which were the crash (films q6 and fx1, 7.93 to 8.07 s).
            look.y = Mathf.Max(look.y, eye.y + Mathf.Tan(Mathf.Clamp(lift, -80f, 44f) * Mathf.Deg2Rad) * flat);
        }

        private static Mesh WaterRibbon()
        {
            var mesh = new Mesh { name = "Ilyas cupped current ribbon" }; var vertices = new Vector3[26]; var triangles = new int[72];
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f, a = t * 2.1f;
                var point = new Vector3(Mathf.Sin(a) * .28f, t * .4f, Mathf.Cos(a) * .28f);
                vertices[i * 2] = point - Vector3.up * .035f; vertices[i * 2 + 1] = point + Vector3.up * .035f;
                if (i == 12) continue; int n = i * 2, j = i * 6;
                int[] faces = { n, n + 2, n + 1, n + 2, n + 3, n + 1 };
                for (int k = 0; k < 6; k++) triangles[j + k] = faces[k];
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
