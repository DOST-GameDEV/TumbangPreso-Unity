using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's ambient and reactive effects (owner, 2026-10-05: "there should be more vfx
    /// overall in the map"; and his first brief's "rocket leaegue goal style effects"). Each is
    /// small, each is drawn through `ArenaFx` (one draw call for all of them), and each is read
    /// off something every peer already has, so nothing is sent and every peer sees it:
    ///
    ///   REACTIVE
    ///   * THE CAN GOES DOWN (`MatchFlair.Kind.LataDown`, the event `ArenaCrowd` and
    ///     `SidewalkLife` already hear): a burst and a column of light from the can's spot, a
    ///     shockwave across the stage, confetti, three fireworks over the stands, the crowd's
    ///     wave and roar, the screens' stinger, and a shake on this screen.
    ///   * THE MATCH ENDS: a long salvo of fireworks.
    ///   * A JUMP PAD THROWS A BODY (an upward speed no jump gives, beside a pad): a ring and
    ///     a column at the pad, sparks, and a trail under the body while it climbs.
    ///   * A SPEED PAD: chevrons of light run along it; a boosted body trails streaks.
    ///   * A STAMINA PICKUP: glints while it waits, a burst when taken, a ring closing in as
    ///     it comes back.
    ///   * A BODY FALLING DOWN THE SHAFT: wind streaks rushing up past it.
    ///
    ///   AMBIENT
    ///   * THE HOVER EMITTERS under every platform drip light down into the shaft, and glow
    ///     when seen from below (falling, or the break's low shots).
    ///   * THE SHAFT'S RINGS: thin rings of light down the shaft, a pulse chasing down them,
    ///     all of them bright on a platform's lock (`ArenaShow.Surge`).
    ///   * MOTES drifting in the floodlit air over the stage and the field.
    ///   * SEARCHLIGHTS sweeping the sky over the stadium (owner, 2026-10-05: "moving
    ///     spotlights"): six on the hull's rim, one on each of the four landing pads, five on
    ///     the city's rooftops, each with its own period, all on the clock every peer shares.
    ///   * SHOW SPOTS from under the four canopies: eight beams that sweep the field and the
    ///     crowd slowly, each with a soft pool where it lands; they chase round the bowl
    ///     through a break's alarm and at the match's end, and snap onto the stage when the
    ///     platforms move and when the can goes down. A spot that points at the eye GLARES
    ///     in the camera (`ArenaGlare`), the bodies on the stage take a rim of its light, and
    ///     the picture a brief veil. Beams and quads: no Light is added.
    ///   * THE 24 FLOODLIGHT BANKS and the sky's searchlights glare the same way, quietly.
    ///   * THE CITY'S HAZE (owner, 2026-10-05: "a distance haze effect for outside the arena"):
    ///     two shader globals that TumbangPreso/ArenaPainted reads (a layer of lit air low
    ///     among the towers, none inside the stadium), and one faint ring of light standing
    ///     between the hull and the nearest towers.
    ///   * THE SCREENS: the four corner screens and the scoreboard flash and run a stinger
    ///     with the game's logo on a knocked can and on the stage's reveal.
    ///
    /// ⚠️ PRESENTATION ONLY. Nothing here moves a body, reads an input or is on the wire, and
    /// reduced effects halves what each throws and how bright it is (`ArenaFx.Light`,
    /// `ArenaFx.Count`) and drops the largest see-through quads.
    /// </summary>
    [DefaultExecutionOrder(890)]
    public sealed class ArenaAmbience : MonoBehaviour
    {
        // The stadium's own numbers (docs/ARENA_ART_BRIEF.md, "The blockout and its numbers").
        private const float ShaftRadius = 39.2f, RimRadius = 237.0f;
        private const float ScreenRadius = 152.0f, ScreenHeight = 44.0f, ScreenWide = 40.0f, ScreenTall = 18.0f;
        private const float BoardRadius = 9.4f, BoardHeight = 47.1f;
        private const int ShaftRings = 5, Searchlights = 6, MaxBodies = 8, MaxPads = 8, MaxPickups = 12, MaxEmitters = 96, MaxShells = 12;
        private const string LogoResource = "UI/brand/tump_logo";

        // The city's haze (see TumbangPreso/ArenaPainted, THE CITY'S HAZE). It is gone above
        // y 340 (the nearest towers' crowns are 330 to 390 m up, the further ones higher) and
        // whole 480 m under that; it begins 270 m level from the eye (past the hull's rim from
        // anywhere on the stage) and is whole 350 m further out. At its strongest it leaves 40
        // per cent of a tower's foot; where a tower meets the canopies' line seen from the
        // stage (about y 180 at 500 m) it is 13 per cent, and the crowns take none.
        private static readonly Color HazeColour = new Color(0.22f, 0.18f, 0.44f, 0.6f);
        private static readonly Vector4 HazeShape = new Vector4(340.0f, 480.0f, 270.0f, 350.0f);
        private static readonly int HazeColourId = Shader.PropertyToID("_ArenaHazeColor"), HazeShapeId = Shader.PropertyToID("_ArenaHaze");
        /// <summary>The ring of lit air: between the hull's rim (239 m) and the nearest tower's face (about 320 m).</summary>
        private const float HazeRingRadius = 300.0f;

        // The show spots: two under each canopy (the four upper stands are centred on 0, 90,
        // 180 and 270 degrees; the canopies' undersides are 64 m up from 150 m out).
        private const int Spots = 8;
        private const float SpotRadius = 160.0f, SpotHeight = 62.0f;
        private static readonly float[] SpotBearing = { -14.0f, 14.0f, 76.0f, 104.0f, 166.0f, 194.0f, 256.0f, 284.0f };
        // The landing pads (bearing; they stand 254 m out) and five rooftops (bearing, metres
        // out, the roof's height: the city kit's table, tools/author_arena_city.py TOWERS, T01,
        // T08, T12, T17 and T04, each roof at its distance times the tangent of its elevation).
        private const float PadRadius = 254.0f;
        private static readonly Vector3[] Rooftops =
        {
            new Vector3(45.0f, 365.0f, 332.0f), new Vector3(135.0f, 375.0f, 392.0f), new Vector3(225.0f, 395.0f, 260.0f),
            new Vector3(315.0f, 368.0f, 358.0f), new Vector3(8.0f, 620.0f, 488.0f),
        };
        private const float StingerSeconds = 2.0f;

        // The floodlight banks (tools/arena_lights.json, `roof.floodlight_banks`): six on the
        // front edge of each canopy, 151.3 m out and 66.75 m up, each aimed at the stage 14 m
        // out from the can on its own bearing.
        private const int Floods = 24;
        private const float FloodRadius = 151.3f, FloodHeight = 66.75f, FloodAimOut = 14.0f;
        private static readonly float[] FloodBearing = { -27.0f, -17.0f, -7.125f, 7.125f, 17.0f, 27.0f };
        private static readonly Color SpotColour = new Color(0.80f, 0.93f, 1.0f), FloodColour = new Color(0.93f, 0.96f, 1.0f);
        /// <summary>A show spot glares whole inside this many degrees of its aim and not at all
        /// past the second: from 170 m that is a body within 7 m of where it points, fading out
        /// by 27 m, so the glare comes and goes as a player moves through the light.</summary>
        private const float SpotFull = 2.5f, SpotGone = 9.0f;
        /// <summary>How far the spots' pools reach on a deck, and what a body on the stage
        /// takes from them: added to the look's own upper rim (`_WorldLookShape.y`, which
        /// TumbangPreso/Toon reads) while they are on the stage.</summary>
        private const float PoolRadius = 2.6f, SpotRim = 0.6f;
        private static readonly int LookShapeId = Shader.PropertyToID("_WorldLookShape");

        private static ArenaAmbience _instance;
        private readonly float[] _spotSeen = new float[Spots];
        private float _lit, _focusAge;

        private readonly Vector3[] _spotAim = new Vector3[Spots];
        private bool _spotsAimed;
        private float _focus, _chase;
        private Mesh _hazeMesh;
        private Material _hazeMaterial;

        /// <summary>Run the screens' stinger: the logo pops on the corner screens and the
        /// scoreboard and they flash. `ArenaShow` calls it on the reveal.</summary>
        public static void Stinger()
        {
            if (_instance != null) _instance._stinger = StingerSeconds;
        }

        private float _clock, _stinger;
        private int _layout = -2;
        private readonly Vector3[] _scratch = new Vector3[8];

        // Per body.
        private readonly float[] _lastRise = new float[MaxBodies], _launched = new float[MaxBodies], _trailAt = new float[MaxBodies], _windAt = new float[MaxBodies];

        // The applied layout's features and emitters.
        private readonly JumpPad[] _jumpPads = new JumpPad[MaxPads];
        private readonly ArenaSpeedPad[] _speedPads = new ArenaSpeedPad[MaxPads];
        private readonly ArenaStaminaPickup[] _pickups = new ArenaStaminaPickup[MaxPickups];
        private readonly bool[] _pickupWas = new bool[MaxPickups];
        private readonly float[] _pickupGlint = new float[MaxPickups];
        private int _jumpCount, _speedCount, _pickupCount;
        private readonly Vector3[] _emitters = new Vector3[MaxEmitters];
        private int _emitterCount;
        private float _drip, _mote;

        // Fireworks waiting their turn.
        private readonly Vector3[] _shellAt = new Vector3[MaxShells];
        private readonly float[] _shellIn = new float[MaxShells];
        private readonly Color[] _shellColour = new Color[MaxShells];
        private int _shells;

        // The logo's quads.
        private Material _logoMaterial;
        private Mesh _logoMesh;
        private Transform _logoRoot;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MatchDirector _match;

        private void OnEnable()
        {
            _instance = this;
            MatchFlair.Presented += OnFlair;
            Camera.onPreRender += LightBodies;
            Shader.SetGlobalColor(HazeColourId, HazeColour);
            Shader.SetGlobalVector(HazeShapeId, HazeShape);
        }

        private void Start()
        {
            BuildHazeRing();
            // The crowd's sound and the stadium's PA, for a scene built before they existed.
            if (GetComponent<ArenaCrowdAudio>() == null) gameObject.AddComponent<ArenaCrowdAudio>();
        }

        /// <summary>
        /// The bodies on the stage, lit by the spots (no Light is added: the scene keeps its
        /// few). TumbangPreso/Toon draws a rim on a body's upper half from the look's global
        /// `_WorldLookShape.y`; `WorldLookPresentation` sets that for each camera as it is
        /// culled and puts it back after it has drawn, so raising it here, between the two,
        /// lasts for this camera's picture only and leaves nothing set. Nothing without the
        /// look (Classic), where the rim is not drawn at all.
        /// </summary>
        private void LightBodies(Camera camera)
        {
            if (_lit <= 0.01f || WorldLookPresentation.Current == null || !WorldLookPresentation.HandlesCamera(camera)) return;
            Vector4 shape = Shader.GetGlobalVector(LookShapeId);
            shape.y += SpotRim * _lit * ArenaFx.Light;
            Shader.SetGlobalVector(LookShapeId, shape);
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
            MatchFlair.Presented -= OnFlair;
            Camera.onPreRender -= LightBodies;
            _lit = 0.0f; _focusAge = 0.0f;
            Unhook();
            // The haze is this map's: no other map's surfaces read it, and none is left set.
            Shader.SetGlobalColor(HazeColourId, Color.clear);
            Shader.SetGlobalVector(HazeShapeId, Vector4.zero);
            _shells = 0; _stinger = 0.0f; _focus = 0.0f; _chase = 0.0f;
            if (_logoRoot != null) _logoRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_logoMaterial != null) Destroy(_logoMaterial);
            if (_logoMesh != null) Destroy(_logoMesh);
            if (_hazeMaterial != null) Destroy(_hazeMaterial);
            if (_hazeMesh != null) Destroy(_hazeMesh);
        }

        /// <summary>
        /// One open ring of lit air round the stadium, 300 m out: 48 sides, strongest 40 m under
        /// the deck and gone by 210 m over it and 260 m under it, so it has no edge anywhere and
        /// the sky over the towers takes none of it. Added light at 7 per cent, with no fog of
        /// its own, faded out near the eye (a camera flown through it is not washed).
        /// 288 triangles, one draw call.
        /// </summary>
        private void BuildHazeRing()
        {
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            var stage = ArenaStage.Instance;
            if (shader == null || stage == null || _hazeMesh != null) return;

            const int sides = 48;
            float[] heights = { -260.0f, -40.0f, 60.0f, 210.0f };
            float[] alphas = { 0.0f, 1.0f, 0.6f, 0.0f };
            int rows = heights.Length;
            var vertices = new Vector3[(sides + 1) * rows];
            var normals = new Vector3[vertices.Length];
            var colours = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[sides * (rows - 1) * 6];
            int t = 0;
            for (int s = 0; s <= sides; s++)
            {
                Vector3 outward = ArenaStageMesh.Direction(360.0f * s / sides);
                for (int r = 0; r < rows; r++)
                {
                    int v = s * rows + r;
                    vertices[v] = outward * HazeRingRadius + Vector3.up * heights[r];
                    normals[v] = -outward;
                    colours[v] = new Color(1.0f, 1.0f, 1.0f, alphas[r]);
                    uvs[v] = new Vector2((float)s / sides, (float)r / (rows - 1));
                    if (s == sides || r == rows - 1) continue;
                    triangles[t++] = v; triangles[t++] = v + 1; triangles[t++] = v + rows;
                    triangles[t++] = v + 1; triangles[t++] = v + rows + 1; triangles[t++] = v + rows;
                }
            }

            _hazeMesh = new Mesh { name = "Arena haze ring" };
            _hazeMesh.SetVertices(vertices);
            _hazeMesh.SetNormals(normals);
            _hazeMesh.SetColors(colours);
            _hazeMesh.SetUVs(0, uvs);
            _hazeMesh.SetTriangles(triangles, 0);

            _hazeMaterial = new Material(shader) { name = "Arena haze ring", hideFlags = HideFlags.DontSave };
            _hazeMaterial.SetColor("_Color", new Color(0.26f, 0.21f, 0.52f, 0.07f));
            _hazeMaterial.SetFloat("_Rim", 0.0f);
            _hazeMaterial.SetVector("_Near", new Vector4(40.0f, 160.0f, 0.0f, 0.0f));
            _hazeMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _hazeMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
            _hazeMaterial.SetFloat("_Cull", (float)CullMode.Off);
            _hazeMaterial.SetFloat("_Fog", 0.0f);
            _hazeMaterial.renderQueue = (int)RenderQueue.Transparent + 10;

            var ring = new GameObject("Haze ring");
            ring.transform.SetParent(transform, false);
            ring.transform.SetPositionAndRotation(stage.transform.position, Quaternion.identity);
            ring.AddComponent<MeshFilter>().sharedMesh = _hazeMesh;
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _hazeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Unhook()
        {
            if (_match != null) _match.MatchEnded -= OnMatchEnded;
            _match = null;
        }

        // ------------------------------------------------------------------ what the match does

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != MatchFlair.Kind.LataDown) return;
            var fx = ArenaFx.Instance;
            var stage = ArenaStage.Instance;
            if (fx == null || stage == null) return;

            // "Rocket League goal style": from the can's spot, outward.
            Vector3 can = new Vector3(at.x, stage.transform.position.y + stage.CanHeight + 0.15f, at.z);
            fx.Flash(can + Vector3.up * 1.5f, 3.0f, 44.0f, ArenaFx.White, 0.9f, 0.45f);
            fx.Ring(can, 0.5f, stage.Radius + 8.0f, ArenaFx.Gold, 0.9f, 0.7f);
            fx.Ring(can + Vector3.up * 0.05f, 0.5f, 46.0f, ArenaFx.Cyan, 0.6f, 1.2f);
            fx.Pillar(can, 24.0f, 2.6f, ArenaFx.Gold, 0.85f, 0.6f);
            fx.Sparks(can + Vector3.up * 0.3f, Vector3.up, 70.0f, 48, 6.0f, 19.0f, ArenaFx.Gold, 0.95f, 0.5f, 1.1f, 0.2f, 13.0f);
            fx.Dots(can + Vector3.up * 1.0f, Vector3.up, 50.0f, 14, 7.0f, 15.0f, ArenaFx.Magenta, 0.9f, 1.6f, 2.8f, 0.4f, 4.0f, 1.2f);
            fx.Dots(can + Vector3.up * 1.0f, Vector3.up, 50.0f, 14, 7.0f, 15.0f, ArenaFx.Cyan, 0.9f, 1.6f, 2.8f, 0.4f, 4.0f, 1.2f);
            for (int i = 0; i < ArenaFx.Count(8); i++)
                fx.Glint(can + fx.RandDirection(0.6f) * fx.Rand(2.0f, 7.0f), fx.Rand(1.2f, 2.8f), ArenaFx.White, 0.9f, fx.Rand(0.3f, 0.7f));

            Vector3 centre = stage.transform.position;
            for (int k = 0; k < 3; k++)
                Shell(centre + ArenaStageMesh.Direction(fx.Rand(0.0f, 360.0f)) * fx.Rand(96.0f, 124.0f) + Vector3.up * fx.Rand(46.0f, 60.0f),
                      0.2f + 0.3f * k, k == 1 ? ArenaFx.Magenta : ArenaFx.Gold);

            ArenaCrowd.Wave();
            // The roar is the crowd's own now (`ArenaCrowdAudio` hears the same event).
            ArenaFx.Cue("sfx_arena_pyro", can, 0.95f, 1.05f);
            _stinger = StingerSeconds;
            _focus = 2.5f;

            // Felt on this screen (the game camera: a knocked can is in play, never in a break).
            var view = Camera.main;
            var rig = view != null ? view.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig != null) rig.Shake(0.8f, 0.7f);
        }

        private void OnMatchEnded(int winner)
        {
            var fx = ArenaFx.Instance;
            var stage = ArenaStage.Instance;
            if (fx == null || stage == null) return;

            Vector3 centre = stage.transform.position;
            for (int k = 0; k < MaxShells; k++)
                Shell(centre + ArenaStageMesh.Direction(30.0f * k + fx.Rand(-10.0f, 10.0f)) * fx.Rand(92.0f, 126.0f) + Vector3.up * fx.Rand(44.0f, 62.0f),
                      0.15f + 0.32f * k, k % 3 == 0 ? ArenaFx.Gold : k % 3 == 1 ? ArenaFx.Magenta : ArenaFx.Cyan);
            _stinger = StingerSeconds;
            _chase = 8.0f;
        }

        private void Shell(Vector3 at, float delay, Color colour)
        {
            if (_shells >= MaxShells) return;
            _shellAt[_shells] = at; _shellIn[_shells] = delay; _shellColour[_shells] = colour;
            _shells++;
        }

        // ------------------------------------------------------------------ the frame

        private void LateUpdate()
        {
            var stage = ArenaStage.Instance;
            var fx = ArenaFx.Instance;
            if (stage == null || fx == null) return;

            // The match is made after the scene: take its event when it appears, once.
            var match = GameServices.Match;
            if (match != _match) { Unhook(); _match = match; if (match != null) match.MatchEnded += OnMatchEnded; }

            float dt = ArenaFx.Step;
            _clock += dt;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            Vector3 centre = stage.transform.position;
            var view = ArenaFx.View;
            Vector3 eye = view != null ? view.transform.position : centre + Vector3.up * 3.0f;

            Fireworks(fx, dt);
            if (stage.Applied != _layout && !stage.Travelling) Adopt(stage);
            bool still = !stage.Travelling && stage.Applied == _layout;

            Bodies(stage, fx, dt);
            if (still) { Pads(fx); Pickups(fx, dt); Emitters(fx, dt, eye, centre, reduced); }
            Shaft(fx, centre, reduced);
            Motes(fx, dt, centre);
            ArenaGlare.Begin(view);
            Sky(fx, centre, reduced);
            Floodlights(fx, centre, reduced);
            ShowSpots(stage, fx, dt, centre, reduced);
            Screens(fx, dt, centre);
        }

        // ------------------------------------------------------------------ the show spots

        /// <summary>The bowl's surface under a point `r` metres from the can (the art brief's
        /// numbers): the field, the lower rows, the cross walkway, the upper rows, the concourse.</summary>
        private static float BowlHeight(float r)
        {
            if (r < 85.3f) return 0.0f;
            if (r < 106.0f) return Mathf.Lerp(3.0f, 12.6f, (r - 85.3f) / 20.7f);
            if (r < 110.6f) return Mathf.Lerp(12.6f, 15.4f, (r - 106.0f) / 4.6f);
            if (r < 130.0f) return Mathf.Lerp(15.4f, 26.0f, (r - 110.6f) / 19.4f);
            return 26.0f;
        }

        /// <summary>
        /// Eight spots under the canopies. IDLE: each sweeps its own side slowly (the even ones
        /// the field, the odd ones the crowd), never across the stage, so no shaft stands
        /// between a player and the play. CHASE (a break's alarm, the match's end): the eight
        /// run round the lower bowl. FOCUS (the platforms moving, a knocked can): all eight on
        /// a point over the can.
        ///
        /// THE BEAMS ARE THE ORIGINAL ONES (owner, 2026-10-05, of a three-quad shaft that
        /// replaced them: "the old spotlight beam was good, i mean i wanted a camera glare
        /// when iit was pointed at you"): two `DrawBeam`s of the streak, white and cyan. What
        /// went is the soft disc that hung in mid-air at each beam's end in focus (his
        /// "feathered circles"); in its place a POOL on the deck under the aim only, 5 cm proud
        /// of that deck's own top, shrunk to fit inside its edges, and not drawn over a gap or
        /// while the platforms are moving. What was added is the CAMERA'S GLARE when a lamp
        /// points at the eye (`ArenaGlare.Lamp`), unless a body or a deck stands in the way.
        /// </summary>
        private void ShowSpots(ArenaStage stage, ArenaFx fx, float dt, Vector3 centre, bool reduced)
        {
            _focus = Mathf.Max(0.0f, _focus - dt);
            _chase = Mathf.Max(0.0f, _chase - dt);

            bool focus = _focus > 0.0f, chase = _chase > 0.0f && !focus;
            if (stage.TryBreak(out var beats) && beats.Showing && beats.From != beats.To)
            {
                focus = beats.Age >= beats.Undock && beats.Age < beats.Reveal + 0.8f;
                chase = beats.Age < beats.Undock;
            }
            // Reduced effects: no chase (it is the one fast sweep here).
            if (reduced) chase = false;

            _lit = Mathf.MoveTowards(_lit, focus ? 1.0f : 0.0f, dt * (focus ? 6.0f : 1.5f));
            _focusAge = focus ? _focusAge + dt : 0.0f;
            // The veil: up in an eighth of a second, then down to a third of itself over the next.
            float veil = focus ? Mathf.Clamp01(_focusAge / 0.12f) * Mathf.Lerp(1.0f, 0.3f, Mathf.Clamp01((_focusAge - 0.25f) / 1.0f)) : 0.0f;

            float shared = (float)(ArenaFx.SharedClock % 3600.0);
            for (int i = 0; i < Spots; i += reduced ? 2 : 1)
            {
                Vector3 foot = centre + ArenaStageMesh.Direction(SpotBearing[i]) * SpotRadius + Vector3.up * SpotHeight;
                bool crowd = (i & 1) == 1;
                Vector3 want;
                float out_;
                if (focus)
                {
                    out_ = 0.0f;
                    want = centre + ArenaStageMesh.Direction(45.0f * i) * 2.5f + Vector3.up * (stage.CanHeight + 1.6f);
                }
                else if (chase)
                {
                    out_ = 97.0f;
                    want = centre + ArenaStageMesh.Direction(_clock * 150.0f + 45.0f * i) * out_ + Vector3.up * (BowlHeight(out_) + 2.9f);
                }
                else
                {
                    float swing = 46.0f * Mathf.Sin(shared * 0.21f * (1.0f + 0.13f * i) + i * 1.7f);
                    out_ = crowd ? 104.0f + 15.0f * Mathf.Sin(shared * 0.17f + i) : 60.0f + 12.0f * Mathf.Sin(shared * 0.15f + i * 2.3f);
                    // Over the crowd's heads on a stand, 5 cm proud of the turf on the field.
                    want = centre + ArenaStageMesh.Direction(SpotBearing[i] + swing) * out_ + Vector3.up * (BowlHeight(out_) + (crowd ? 2.9f : 0.05f));
                }

                // The snap onto the stage is quick; everything else eases.
                _spotAim[i] = _spotsAimed ? Vector3.Lerp(_spotAim[i], want, 1.0f - Mathf.Exp(-(focus ? 10.0f : chase ? 16.0f : 3.0f) * dt)) : want;
                Vector3 aim = _spotAim[i];
                Vector3 along = aim - foot;
                float reach = along.magnitude;
                if (reach < 1.0f) continue;
                Vector3 direction = along / reach;

                fx.DrawBeam(foot, aim, 1.1f, focus ? 4.0f : 8.5f, ArenaFx.White, 0.10f);
                fx.DrawBeam(foot, aim, 0.4f, focus ? 1.5f : 3.0f, ArenaFx.Cyan, 0.07f);

                // The lamp. Only a spot on the stage can have a body or a deck between it and the eye.
                float seen = focus ? ArenaGlare.Seen(foot, stage) : 1.0f;
                _spotSeen[i] = Mathf.MoveTowards(_spotSeen[i], seen, dt * 12.0f);
                ArenaGlare.Lamp(fx, foot, direction, SpotColour, 1.0f, SpotFull, SpotGone, 1.0f, true, focus && !reduced, _spotSeen[i], 0.22f);

                if (focus)
                {
                    // No disc in the air at the beam's end. A pool on the deck under it, if there
                    // is one there, once the aim has arrived.
                    if ((aim - want).sqrMagnitude > 1.0f) continue;
                    Vector3 local = stage.transform.InverseTransformPoint(want);
                    float radius = PoolRadius;
                    if (!Surface(stage, local.x, local.z, ref radius, out float top)) continue;
                    local.y = top + 0.05f;
                    float across = radius * 2.0f / 0.92f;
                    fx.DrawFlat(ArenaFx.Cell.Disc, stage.transform.TransformPoint(local), across, across, 0.0f, SpotColour, 0.20f * _lit);
                    continue;
                }

                // The pool: a soft disc lying along the surface it lands on (flat on the field,
                // tilted up the stand), where the aim has it.
                Vector3 flat = aim - centre; flat.y = 0.0f;
                float r = flat.magnitude;
                if (r < 1.0f) continue;
                Vector3 outward = flat / r, sideways = Vector3.Cross(Vector3.up, outward);
                Vector3 slope = (outward * 2.0f + Vector3.up * (BowlHeight(r + 1.0f) - BowlHeight(r - 1.0f))).normalized;
                fx.DrawQuad(ArenaFx.Cell.Disc, aim, sideways * 6.0f, slope * 6.0f, ArenaFx.White, 0.16f);
            }
            _spotsAimed = true;

            ArenaGlare.Veil(fx, veil);
        }

        /// <summary>
        /// The deck under a plan point of the stage (its own frame), if a pool `radius` across
        /// fits on one: the highest flat top that covers the point that far in from every one
        /// of its edges, tried again at half the radius. Never a ramp (a pool lies flat), never
        /// while the platforms are moving, and false over a gap.
        /// </summary>
        private static bool Surface(ArenaStage stage, float x, float z, ref float radius, out float top)
        {
            top = float.NegativeInfinity;
            int layout = stage.Applied;
            if (layout < 0 || stage.Travelling) return false;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                for (int p = 0; p < stage.Pieces.Length; p++)
                {
                    var piece = stage.Pieces[p];
                    if (piece == null || piece.Shapes == null || layout >= piece.Shapes.Length) continue;
                    var shape = piece.Shapes[layout];
                    if (!shape.Exists || shape.IsRamp || !shape.Contains(x, z, radius)) continue;
                    top = Mathf.Max(top, shape.Top);
                }

                if (!float.IsNegativeInfinity(top)) return true;
                radius *= 0.5f;
            }

            return false;
        }

        /// <summary>The 24 floodlight banks: lamps that are always on and always aimed at the
        /// stage, so from the stage each is a small steady glare with a short streak.</summary>
        private void Floodlights(ArenaFx fx, Vector3 centre, bool reduced)
        {
            for (int i = 0; i < Floods; i += reduced ? 2 : 1)
            {
                Vector3 outward = ArenaStageMesh.Direction(90.0f * (i / 6) + FloodBearing[i % 6]);
                Vector3 lamp = centre + outward * FloodRadius + Vector3.up * FloodHeight;
                Vector3 aim = (centre + outward * FloodAimOut + Vector3.up * 0.5f - lamp).normalized;
                float breathe = 0.93f + 0.07f * Mathf.Sin(_clock * (1.9f + 0.13f * i) + i * 1.9f);
                ArenaGlare.Lamp(fx, lamp, aim, FloodColour, 0.5f * breathe, 7.0f, 24.0f, 0.55f);
            }
        }

        private void Fireworks(ArenaFx fx, float dt)
        {
            for (int i = _shells - 1; i >= 0; i--)
            {
                _shellIn[i] -= dt;
                if (_shellIn[i] > 0.0f) continue;

                fx.Firework(_shellAt[i], _shellColour[i], 1.6f);
                if (i % 2 == 0) ArenaFx.Cue("sfx_arena_pyro", _shellAt[i], 0.9f, 1.15f, 0.7f);
                _shells--;
                _shellAt[i] = _shellAt[_shells]; _shellIn[i] = _shellIn[_shells]; _shellColour[i] = _shellColour[_shells];
            }
        }

        /// <summary>The layout that has just come to rest: its pads and pickups, and a point
        /// under every platform for the hover emitters.</summary>
        private void Adopt(ArenaStage stage)
        {
            _layout = stage.Applied;
            _jumpCount = _speedCount = _pickupCount = _emitterCount = 0;
            if (_layout < 0 || _layout >= stage.LayoutCount) return;

            var features = stage.Layouts[_layout] != null ? stage.Layouts[_layout].Features : null;
            if (features != null)
            {
                foreach (var pad in features.GetComponentsInChildren<JumpPad>(true)) if (_jumpCount < MaxPads) _jumpPads[_jumpCount++] = pad;
                foreach (var pad in features.GetComponentsInChildren<ArenaSpeedPad>(true)) if (_speedCount < MaxPads) _speedPads[_speedCount++] = pad;
                foreach (var pickup in features.GetComponentsInChildren<ArenaStaminaPickup>(true))
                    if (_pickupCount < MaxPickups) { _pickupWas[_pickupCount] = true; _pickupGlint[_pickupCount] = 0.2f * _pickupCount; _pickups[_pickupCount++] = pickup; }
            }

            for (int i = 0; i < stage.Pieces.Length; i++)
            {
                var piece = stage.Pieces[i];
                if (piece == null || piece.Shapes == null || _layout >= piece.Shapes.Length || !piece.Shapes[_layout].Exists) continue;
                int n = stage.PiecePoints(i, _layout, _scratch);
                for (int k = 0; k < n && _emitterCount < MaxEmitters; k++)
                    _emitters[_emitterCount++] = _scratch[k] + Vector3.down * (piece.Shapes[_layout].Thick + 0.05f);
            }
        }

        // ------------------------------------------------------------------ bodies: the pads' throw, the boost, the fall

        private void Bodies(ArenaStage stage, ArenaFx fx, float dt)
        {
            var round = GameServices.Round;
            if (round == null) return;

            var players = round.Players;
            for (int i = 0; i < players.Count && i < MaxBodies; i++)
            {
                var who = players[i];
                if (who == null || !who.gameObject.activeInHierarchy) { _lastRise[i] = 0.0f; _launched[i] = 0.0f; continue; }

                Vector3 feet = who.transform.position, velocity = who.Velocity;

                // Thrown by a jump pad: an upward speed no jump gives, beside a pad.
                float thrown = Balance.JumpVelocity + 2.0f;
                if (_lastRise[i] < thrown && velocity.y >= thrown)
                    for (int p = 0; p < _jumpCount; p++)
                    {
                        var pad = _jumpPads[p];
                        if (pad == null || !pad.isActiveAndEnabled) continue;
                        Vector3 flat = pad.transform.position - feet; flat.y = 0.0f;
                        if (flat.sqrMagnitude > (pad.Radius + 1.5f) * (pad.Radius + 1.5f)) continue;

                        Vector3 at = pad.transform.position + Vector3.up * 0.1f;
                        fx.Ring(at, 0.4f, 3.6f, ArenaFx.Teal, 0.9f, 0.45f);
                        fx.Ring(at, 0.2f, 2.0f, ArenaFx.White, 0.7f, 0.3f, ArenaFx.Cell.ThinRing);
                        fx.Pillar(at, 7.0f, 1.5f, ArenaFx.Teal, 0.8f, 0.4f);
                        fx.Sparks(at, Vector3.up, 30.0f, 14, 6.0f, 15.0f, ArenaFx.Teal, 0.9f, 0.3f, 0.6f, 0.16f, 8.0f);
                        _launched[i] = 1.1f;
                        break;
                    }
                _lastRise[i] = velocity.y;

                // The climb's trail.
                if (_launched[i] > 0.0f)
                {
                    _launched[i] -= dt;
                    if (who.IsGrounded || velocity.y < -1.0f) _launched[i] = 0.0f;
                    else if ((_trailAt[i] -= dt) <= 0.0f)
                    {
                        _trailAt[i] = 0.03f;
                        fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, feet + Vector3.up * 0.2f, Vector3.zero, ArenaFx.Teal, 0.6f * _launched[i], 0.4f, 0.8f, 0.15f);
                        fx.Emit(ArenaFx.Cell.Streak, ArenaFx.Mode.Stretch, feet + new Vector3(fx.Rand(-0.3f, 0.3f), 0.1f, fx.Rand(-0.3f, 0.3f)), Vector3.down * 3.0f,
                                ArenaFx.White, 0.5f, 0.3f, 0.12f, 0.04f, 1.4f, 0.5f);
                    }
                }
                // Boosted by a speed pad: streaks left behind the run.
                else if (who.IsSpeedBoosted && who.IsGrounded && velocity.sqrMagnitude > 9.0f && (_trailAt[i] -= dt) <= 0.0f)
                {
                    _trailAt[i] = 0.04f;
                    Vector3 back = -velocity.normalized;
                    fx.Emit(ArenaFx.Cell.Streak, ArenaFx.Mode.Stretch, feet + new Vector3(fx.Rand(-0.35f, 0.35f), fx.Rand(0.2f, 1.3f), fx.Rand(-0.35f, 0.35f)), back * 2.5f,
                            ArenaFx.Lime, 0.55f, 0.32f, 0.14f, 0.04f, 1.8f, 0.5f);
                }

                // Down the shaft: the air rushing up past the body.
                if (ArenaStage.IsShaftFall(who) && (_windAt[i] -= dt) <= 0.0f)
                {
                    _windAt[i] = 0.025f;
                    for (int k = 0; k < 2; k++)
                    {
                        float a = fx.Rand(0.0f, Mathf.PI * 2.0f), r = fx.Rand(0.7f, 2.2f);
                        Vector3 at = feet + new Vector3(Mathf.Cos(a) * r, fx.Rand(-5.0f, 0.5f), Mathf.Sin(a) * r);
                        fx.Emit(ArenaFx.Cell.Streak, ArenaFx.Mode.Stretch, at, Vector3.up * 5.0f, ArenaFx.White, 0.42f, 0.3f, 0.09f, 0.03f, 2.6f, 1.2f);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ pads and pickups

        private void Pads(ArenaFx fx)
        {
            for (int p = 0; p < _speedCount; p++)
            {
                var pad = _speedPads[p];
                if (pad == null || !pad.isActiveAndEnabled) continue;

                // Three chevrons running the pad's length, fading in and out at its ends, 9 cm proud of it.
                Vector3 forward = pad.transform.forward;
                float yaw = pad.transform.eulerAngles.y, run = pad.HalfSize.y * 1.7f;
                for (int k = 0; k < 3; k++)
                {
                    float t = Mathf.Repeat(_clock * 1.15f + k / 3.0f, 1.0f);
                    Vector3 at = pad.transform.position + forward * ((t - 0.5f) * run) + Vector3.up * 0.09f;
                    fx.DrawFlat(ArenaFx.Cell.Chevron, at, pad.HalfSize.x * 1.7f, 1.3f, yaw, ArenaFx.Lime, 0.75f * Mathf.Sin(t * Mathf.PI));
                }
            }
        }

        private void Pickups(ArenaFx fx, float dt)
        {
            for (int p = 0; p < _pickupCount; p++)
            {
                var pickup = _pickups[p];
                if (pickup == null || !pickup.isActiveAndEnabled) continue;

                Vector3 orb = pickup.transform.position + Vector3.up * 0.95f;
                bool there = pickup.Available;
                if (there != _pickupWas[p])
                {
                    _pickupWas[p] = there;
                    if (!there)
                    {
                        // Taken.
                        fx.Flash(orb, 0.6f, 3.4f, ArenaFx.Violet, 0.85f, 0.3f);
                        fx.Ring(pickup.transform.position + Vector3.up * 0.08f, 0.3f, 2.8f, ArenaFx.Violet, 0.85f, 0.4f);
                        fx.Sparks(orb, Vector3.up, 180.0f, 16, 2.5f, 7.0f, ArenaFx.Violet, 0.95f, 0.3f, 0.6f, 0.13f, 4.0f);
                    }
                    else
                    {
                        // Back: a ring closing in on it, and a pop.
                        fx.Ring(pickup.transform.position + Vector3.up * 0.08f, 2.6f, 0.3f, ArenaFx.Violet, 0.8f, 0.4f, ArenaFx.Cell.ThinRing);
                        fx.Flash(orb, 1.8f, 0.5f, ArenaFx.White, 0.8f, 0.35f);
                        for (int k = 0; k < ArenaFx.Count(4); k++) fx.Glint(orb + fx.RandDirection() * 0.5f, fx.Rand(0.8f, 1.5f), ArenaFx.White, 0.9f, fx.Rand(0.25f, 0.5f));
                    }
                }

                // Waiting: a glint now and then, round the orb.
                if (there && (_pickupGlint[p] -= dt) <= 0.0f)
                {
                    _pickupGlint[p] = fx.Rand(0.25f, 0.55f);
                    fx.Glint(orb + fx.RandDirection() * fx.Rand(0.25f, 0.6f), fx.Rand(0.35f, 0.75f), fx.Rand() < 0.5f ? ArenaFx.Violet : ArenaFx.White, 0.85f, fx.Rand(0.3f, 0.55f));
                }
            }
        }

        // ------------------------------------------------------------------ under the stage

        private void Emitters(ArenaFx fx, float dt, Vector3 eye, Vector3 centre, bool reduced)
        {
            if (_emitterCount == 0) return;

            // Light dripping down into the shaft, from a few emitters at a time.
            _drip += dt * (reduced ? 7.0f : 14.0f);
            while (_drip >= 1.0f)
            {
                _drip -= 1.0f;
                Vector3 at = _emitters[Mathf.Min(_emitterCount - 1, (int)(fx.Rand() * _emitterCount))];
                fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, at, Vector3.down * fx.Rand(2.0f, 4.5f), ArenaFx.Cyan, 0.75f, fx.Rand(1.2f, 1.9f), 0.34f, 0.07f, -1.0f, -1.0f, 1.5f);
            }

            // Their glow is only ever seen from below the decks: a falling body's view, the break's low shots.
            if (eye.y > centre.y + 1.2f) return;
            float pulse = 0.45f + 0.2f * Mathf.Sin(_clock * 3.1f) + 0.5f * ArenaShow.Surge;
            for (int i = 0; i < _emitterCount; i += reduced ? 2 : 1)
                fx.DrawBillboard(ArenaFx.Cell.Dot, _emitters[i], 1.5f, ArenaFx.Cyan, pulse);
        }

        private void Shaft(ArenaFx fx, Vector3 centre, bool reduced)
        {
            // Thin rings of light down the shaft; a pulse chases down them, and a lock lights them all.
            float across = ShaftRadius * ArenaFx.RingScale(ArenaFx.Cell.ThinRing);
            for (int k = 0; k < ShaftRings; k += reduced ? 2 : 1)
            {
                float chase = Mathf.Pow(Mathf.Max(0.0f, Mathf.Sin((_clock * 0.45f - k * 0.17f) * Mathf.PI * 2.0f)), 8.0f);
                float lit = 0.10f + 0.45f * chase + 0.55f * ArenaShow.Surge;
                fx.DrawFlat(ArenaFx.Cell.ThinRing, centre + Vector3.up * (-8.0f - 10.0f * k), across, across, 0.0f, k % 2 == 0 ? ArenaFx.Cyan : ArenaFx.Indigo, lit);
            }
        }

        private void Motes(ArenaFx fx, float dt, Vector3 centre)
        {
            // Dust in the floodlit air: a few dozen alive at once, each drifting for a few seconds.
            _mote += dt * 5.0f;
            while (_mote >= 1.0f)
            {
                _mote -= 1.0f;
                Vector3 at = centre + ArenaStageMesh.Direction(fx.Rand(0.0f, 360.0f)) * fx.Rand(5.0f, 48.0f) + Vector3.up * fx.Rand(3.0f, 24.0f);
                Vector3 drift = fx.RandDirection() * fx.Rand(0.15f, 0.5f) + Vector3.up * 0.08f;
                fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, at, drift, ArenaFx.White, 0.3f, fx.Rand(5.0f, 8.0f), 0.2f, 0.16f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.3f);
            }
        }

        // ------------------------------------------------------------------ the sky and the screens

        private void Sky(ArenaFx fx, Vector3 centre, bool reduced)
        {
            // Searchlights on the hull's rim, leaning in over the stadium and swinging slowly,
            // on the clock every peer shares, so every peer sees the same sky.
            float clock = (float)(ArenaFx.SharedClock % 3600.0);
            for (int i = 0; i < Searchlights; i += reduced ? 2 : 1)
            {
                float bearing = 30.0f + 360.0f * i / Searchlights;
                Vector3 outward = ArenaStageMesh.Direction(bearing);
                Vector3 foot = centre + outward * RimRadius + Vector3.up * 1.0f;
                float tilt = (30.0f + 13.0f * Mathf.Sin(clock * 0.09f * (1.0f + i * 0.11f) + i)) * Mathf.Deg2Rad;
                float swing = 38.0f * Mathf.Sin(clock * 0.13f * (1.0f + i * 0.17f) + i * 1.9f);
                Vector3 aim = Quaternion.AngleAxis(swing, Vector3.up) * (-outward * Mathf.Sin(tilt)) + Vector3.up * Mathf.Cos(tilt);
                Vector3 end = foot + aim * 430.0f;
                fx.DrawBeam(foot, end, 5.0f, 44.0f, ArenaFx.White, 0.075f);
                fx.DrawBeam(foot, end, 2.0f, 14.0f, ArenaFx.Cyan, 0.06f);
                Searchlight(fx, foot, aim, centre, ArenaFx.White);
            }

            // The four landing pads: steeper, and slower, crossing over the bowl. Seen from the
            // stage a beam clears the canopies' line (about 23 degrees up, 150 m out) whenever it
            // leans in less than 60 degrees from the vertical; these lean 16 to 38.
            for (int i = 0; i < 4; i += reduced ? 2 : 1)
            {
                Vector3 outward = ArenaStageMesh.Direction(45.0f + 90.0f * i);
                Vector3 foot = centre + outward * PadRadius + Vector3.up * 2.0f;
                float tilt = (27.0f + 11.0f * Mathf.Sin(clock * 0.07f * (1.0f + i * 0.19f) + i * 2.3f)) * Mathf.Deg2Rad;
                float swing = 52.0f * Mathf.Sin(clock * 0.10f * (1.0f + i * 0.23f) + i * 0.9f);
                Vector3 aim = Quaternion.AngleAxis(swing, Vector3.up) * (-outward * Mathf.Sin(tilt)) + Vector3.up * Mathf.Cos(tilt);
                Vector3 end = foot + aim * 460.0f;
                fx.DrawBeam(foot, end, 4.0f, 36.0f, ArenaFx.Cyan, 0.07f);
                fx.DrawBeam(foot, end, 1.6f, 11.0f, ArenaFx.White, 0.055f);
                Searchlight(fx, foot, aim, centre, ArenaFx.Cyan);
            }

            // Five rooftops in the city: each turns a slow cone about the vertical, leaning
            // toward the stadium, every one above 30 degrees from the stage.
            for (int i = 0; i < Rooftops.Length; i += reduced ? 2 : 1)
            {
                Vector3 outward = ArenaStageMesh.Direction(Rooftops[i].x);
                Vector3 foot = centre + outward * Rooftops[i].y + Vector3.up * Rooftops[i].z;
                float lean = (26.0f + 12.0f * Mathf.Sin(clock * 0.06f * (1.0f + i * 0.21f) + i)) * Mathf.Deg2Rad;
                float round = clock * (9.0f + 2.5f * i) * (i % 2 == 0 ? 1.0f : -1.0f) + 70.0f * i;
                Vector3 aim = Quaternion.AngleAxis(round, Vector3.up) * (outward * Mathf.Sin(lean)) + Vector3.up * Mathf.Cos(lean) - outward * 0.35f;
                Vector3 end = foot + aim.normalized * 520.0f;
                fx.DrawBeam(foot, end, 6.0f, 50.0f, i % 2 == 0 ? ArenaFx.Magenta : ArenaFx.White, 0.06f);
                fx.DrawBeam(foot, end, 2.2f, 16.0f, ArenaFx.White, 0.045f);
                Searchlight(fx, foot, aim, centre, i % 2 == 0 ? ArenaFx.Magenta : ArenaFx.White);
            }
        }

        /// <summary>A searchlight's lamp: it glares for the moment its beam sweeps across the
        /// eye (whole inside 1.5 degrees, gone by 6), if the stadium is not between them.</summary>
        private static void Searchlight(ArenaFx fx, Vector3 foot, Vector3 aim, Vector3 centre, Color colour)
        {
            if (!ArenaGlare.OverTheBowl(foot, centre)) return;
            ArenaGlare.Lamp(fx, foot, aim.normalized, colour, 0.85f, 1.5f, 6.0f, 1.2f, true, !Settings.SettingsStore.Current.ReducedEffects, 1.0f, 0.12f);
        }

        private void Screens(ArenaFx fx, float dt, Vector3 centre)
        {
            if (_stinger <= 0.0f)
            {
                if (_logoRoot != null && _logoRoot.gameObject.activeSelf) _logoRoot.gameObject.SetActive(false);
                return;
            }

            float age = StingerSeconds - _stinger;
            _stinger -= dt;

            // The flash: every screen white for a blink, then falling away.
            float flash = Mathf.Exp(-age * 4.5f);
            for (int k = 0; k < 4; k++)
            {
                Vector3 outward = ArenaStageMesh.Direction(45.0f + 90.0f * k);
                Vector3 right = Vector3.Cross(Vector3.up, outward);
                fx.DrawQuad(ArenaFx.Cell.Disc, centre + outward * (ScreenRadius - 1.2f) + Vector3.up * ScreenHeight,
                            right * (ScreenWide * 0.62f), Vector3.up * (ScreenTall * 0.75f), ArenaFx.White, 0.7f * flash);
            }
            fx.DrawBillboard(ArenaFx.Cell.Dot, centre + Vector3.up * BoardHeight, 30.0f, ArenaFx.White, 0.5f * flash);

            // The logo: it pops on, holds, and fades.
            if (_logoRoot == null && !_logoFailed) BuildLogos(centre);
            if (_logoRoot == null) return;

            if (!_logoRoot.gameObject.activeSelf) _logoRoot.gameObject.SetActive(true);
            float pop = age < 0.22f ? 1.25f * Mathf.Sin(age / 0.22f * Mathf.PI * 0.5f) : Mathf.Lerp(1.25f, 1.0f, Mathf.Clamp01((age - 0.22f) / 0.15f));
            float fade = Mathf.Clamp01(_stinger / 0.5f) * ArenaFx.Light;
            _logoRoot.localScale = Vector3.one;
            for (int i = 0; i < _logoRoot.childCount; i++) _logoRoot.GetChild(i).localScale = _logoSizes[i] * pop;
            _logoMaterial.SetColor(ColorId, new Color(1.0f, 1.0f, 1.0f, fade));
        }

        private bool _logoFailed;
        private readonly Vector3[] _logoSizes = new Vector3[8];

        /// <summary>The game's logo on a quad in front of each corner screen (1.5 m proud of
        /// it, facing the can) and on four sides of the scoreboard. Built the first time a
        /// stinger runs; with no logo or no shader the screens only flash.</summary>
        private void BuildLogos(Vector3 centre)
        {
            var logo = Resources.Load<Texture2D>(LogoResource);
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            if (logo == null || shader == null) { _logoFailed = true; return; }

            _logoMaterial = new Material(shader) { name = "Arena screen stinger", mainTexture = logo, hideFlags = HideFlags.DontSave };
            _logoMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _logoMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _logoMaterial.SetFloat("_Cull", (float)CullMode.Off);
            _logoMaterial.SetFloat("_Fog", 0.0f);
            _logoMaterial.renderQueue = (int)RenderQueue.Transparent + 45;

            _logoMesh = new Mesh { name = "Arena screen stinger" };
            _logoMesh.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) });
            _logoMesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            _logoMesh.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            _logoMesh.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            _logoMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);

            _logoRoot = new GameObject("Screen stinger").transform;
            _logoRoot.SetParent(transform, false);
            _logoRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            float aspect = logo.height > 0 ? (float)logo.width / logo.height : 1.0f;

            for (int k = 0; k < 8; k++)
            {
                bool board = k >= 4;
                Vector3 outward = ArenaStageMesh.Direction(board ? 90.0f * k : 45.0f + 90.0f * k);
                float tall = board ? 5.5f : ScreenTall * 0.8f;
                var quad = new GameObject(board ? "Scoreboard logo" : "Corner screen logo");
                quad.transform.SetParent(_logoRoot, false);
                // A corner screen is seen from the can, so its logo faces inward; the scoreboard's face outward.
                quad.transform.SetPositionAndRotation(
                    centre + outward * (board ? BoardRadius + 0.35f : ScreenRadius - 1.5f) + Vector3.up * (board ? BoardHeight : ScreenHeight),
                    Quaternion.LookRotation(board ? -outward : outward, Vector3.up));
                // As tall as its place allows, and no wider: the logo keeps its own shape.
                float wide = tall * aspect, widest = board ? 11.0f : ScreenWide * 0.9f;
                if (wide > widest) { wide = widest; tall = widest / aspect; }
                _logoSizes[k] = new Vector3(wide, tall, 1.0f);
                quad.transform.localScale = _logoSizes[k];
                quad.AddComponent<MeshFilter>().sharedMesh = _logoMesh;
                var renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _logoMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
}
