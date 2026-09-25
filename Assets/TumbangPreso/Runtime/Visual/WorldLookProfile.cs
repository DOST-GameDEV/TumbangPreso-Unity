using System;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The per-map world look: lighting rig, sky, air, character shading and edges.
    ///
    /// ⚠️⚠️ § THE BRIGHT LOOK, 2026-09-23, AND WHY IT REVERSES VISUAL-1.8. The owner asked for
    /// the lighting to be bright "instead of our current lighting's gloomy and dark lighting",
    /// studied against PEAK (Aggro Crab and Landfall, 2025), and explicitly "not the route
    /// towards realism". VISUAL-1.8 had pushed the other way: ambient cut to roughly a third
    /// (sky 0.25, ground 0.08), fog pulled in to 20 m in a grey-beige, a 0.44 shadow band, so
    /// Eskinita and Ilalim read as black asphalt under black shadows. It measured a contrast
    /// gain and it bought it with gloom.
    ///
    /// What PEAK's store frames actually show, and what each field below transcribes:
    ///  * HIGH KEY. Nothing on screen is black. The darkest values are coloured (plum, brown,
    ///    teal), so <see cref="MapLook.Lift"/> puts a coloured floor under every map.
    ///  * SHADOWS CARRY HUE, NOT JUST LESS LIGHT. Warm sun, sky-coloured shade. In the built-in
    ///    pipeline a shadowed pixel keeps only the ambient term, so the ambient trilight IS the
    ///    shadow colour: <see cref="MapLook.Sky"/> is a saturated sky tint, and
    ///    <see cref="MapLook.Ground"/> is the warm bounce off the court.
    ///  * AIR. Distance dissolves into a bright, coloured haze matching the horizon, which is
    ///    what layers PEAK's silhouettes. <see cref="MapLook.Fog"/> is that haze and it starts
    ///    past the court, so it never softens a player you need to read.
    ///  * SOFT, PLUSH BODIES with no ink. Bodies use a wide, wrapped terminator
    ///    (<see cref="Softness"/>, <see cref="Wrap"/>) with a faint warm band where light turns
    ///    to shade (<see cref="Terminator"/>), in place of a hard two-band step.
    ///  * EDGES ARE LIGHT AND COLOUR, NOT BLACK LINES. Sun-catching convex edges lift
    ///    (<see cref="EdgeHighlight"/>), inside corners take a soft coloured shade
    ///    (<see cref="CreaseShade"/>), silhouettes darken the local colour instead of drawing
    ///    ink (<see cref="SilhouetteShade"/>), and the cast's hull becomes a thin line in a deeper
    ///    shade of whatever it outlines (<see cref="CastInkSelf"/>).
    ///
    /// ⚠️ THE OFF VALUE STILL EXISTS. <see cref="WorldCueProfile.WorldLighting"/> at 0 restores
    /// each scene's own lighting exactly as authored, the VISUAL-1 rule for every look lever.
    /// </summary>
    [CreateAssetMenu(menuName="Tumbang Preso/World look profile")]
    public sealed class WorldLookProfile : ScriptableObject
    {
        [Serializable] public sealed class MapLook
        {
            public string Map;
            // Ambient trilight. In this pipeline it is the colour of every shadow.
            public Color Sky,Equator,Ground;
            // The cast's shade colour, multiplied by ShadowLevel.
            public Color ShadowTint,Chalk,ChalkEdge;
            public float FogStart,FogEnd;
            public int WearKind;
            // Key light. Elevation 0 keeps the scene's own angle.
            public Color Sun;
            public float SunIntensity,SunElevation,ShadowStrength;
            // Air and sky.
            public Color Fog,Zenith,Horizon,CloudLight,CloudShade;
            // The coloured black floor added after the tonemap, in linear.
            public Color Lift;
            // ⚠️⚠️ THE COURT'S OWN GROUND, BRIGHTENED, AND ONLY WHERE IT IS DARK ASPHALT. The rig
            // lifts every shadow, but a sunlit surface can only be as bright as its albedo, and
            // Eskinita's and Ilalim's road texture is near black: after the whole bright look it
            // still measured (90,85,71) and (74,81,73) in full sun, the largest area in every
            // court view. PEAK has no dark ground anywhere. This multiplies the court floor's
            // colour through a property block, the same code-chosen road colour
            // `EnvColourPass.RoadTint` already sets, so no material asset or texture is touched.
            // 1 (or an unset 0) leaves the floor alone; the plaza, deck and rooftop keep theirs.
            public float GroundLift=1;
            public MapLook(string map,Color sky,Color equator,Color ground,Color tint,float fogStart,float fogEnd,int wear,bool dark)
            {
                Map=map;Sky=sky;Equator=equator;Ground=ground;ShadowTint=tint;FogStart=fogStart;FogEnd=fogEnd;WearKind=wear;
                Chalk=dark?new Color(.18f,.16f,.14f):new Color(.96f,.92f,.81f);
                ChalkEdge=dark?new Color(.79f,.75f,.65f):new Color(.14f,.11f,.075f);
                Sun=new Color(1,.93f,.8f);SunIntensity=1.3f;ShadowStrength=.78f;
                Fog=new Color(.9f,.88f,.84f);Zenith=new Color(.38f,.63f,.9f);Horizon=new Color(.93f,.9f,.84f);
                CloudLight=new Color(1,.98f,.94f);CloudShade=new Color(.7f,.74f,.88f);Lift=new Color(.014f,.011f,.022f);
            }
            public MapLook Air(Color fog,Color zenith,Color horizon,Color cloudLight,Color cloudShade)
            {Fog=fog;Zenith=zenith;Horizon=horizon;CloudLight=cloudLight;CloudShade=cloudShade;return this;}
            public MapLook Key(Color sun,float intensity,float elevation,float shadowStrength,Color lift)
            {Sun=sun;SunIntensity=intensity;SunElevation=elevation;ShadowStrength=shadowStrength;Lift=lift;return this;}
            public MapLook Floor(float lift){GroundLift=lift;return this;}
        }

        [Header("Cast and hero props")]
        // ⚠️ 0.58, UP FROM 0.44. The shade side of a body keeps 58 per cent of the key light
        // before the (now bright) ambient is added, which lands a lit/shade ratio near 1.6:1.
        [Range(.2f,.8f)] public float ShadowLevel=.58f;
        [Range(.03f,.15f)] public float BandEdge=.065f;
        // Width of the terminator in N.L. 0.5 reads as a plush, clay-soft turn.
        [Range(.05f,1f)] public float Softness=.52f;
        // How far the light wraps past the geometric terminator, in N.L.
        [Range(0,.6f)] public float Wrap=.22f;
        // A warm band where lit turns to shade: the "fuzzy orange" the owner named (2026-09-25),
        // the colour a painter puts on a turning form under a warm key.
        // ⚠️⚠️ IT SHIFTS HUE AND KEEPS LUMINANCE. The first band multiplied red by 1.25 and so
        // brightened as it warmed, which pushed orange skin and yellow cloth into the clip; it
        // was cut to 0.06 for that. `WorldLookPresentation.Build` now rotates the ramp toward
        // orange at constant luminance, so the band can be strong (0.22) and add no light.
        [Range(0,.4f)] public float Terminator=.22f;
        // ⚠️⚠️ UPPER RIM AND METAL HIGHLIGHT ARE 0, AND THAT IS THE OWNER'S CALL, NOT A TUNE.
        // 2026-09-25: "the character glows", "remove the bright finish on all characters". The
        // rim painted a cream edge round every head and shoulder turned away from the sun, and
        // the metal term a stepped glint on caps and buckles: both a finish laid over the body,
        // neither in PEAK, whose cast is matte felt and plastic with no rim at all. The shader
        // terms stay so a later look can use them; this look does not.
        [Range(0,.25f)] public float UpperRim=0;
        [Range(0,.2f)] public float FeetShade=.10f;
        [Range(0,.2f)] public float EnvironmentContact=.08f;
        [Range(0,.3f)] public float MetalHighlight=0;

        [Header("Edges")]
        // How much a silhouette darkens its own colour (0 no line, 1 the old black ink).
        [Range(0,1)] public float SilhouetteShade=.34f;
        // Sun-side convex edges brighten toward the key light.
        [Range(0,1)] public float EdgeHighlight=.42f;
        // Inside corners take a coloured shade.
        [Range(0,1)] public float CreaseShade=.30f;
        // 1 draws the cast's hull in a deeper shade of its own colour, 0 in black.
        [Range(0,1)] public float CastInkSelf=.88f;
        // The cast's hull width against its authored width.
        [Range(.3f,1)] public float CastInkWidth=.72f;
        // ⚠️ 0 IS TODAY'S HULL AND THE SHIPPED VALUE, BECAUSE THIS IS AN OWNER TASTE CALL
        // (docs/TODO.md LIGHT-1.6). Above 0 the hull keeps at least that share of the luminance
        // of the colour it frames, so dark skin gets a deep brown line instead of near ink.
        // Every hull darker than the floor moves, not only skin: the first render lifted tan
        // skin, greys and greens at 0.25 and orange at 0.35, and a blue strap takes a deep blue
        // line. True black keeps its ink at any floor, because a share of nothing is nothing.
        // `WorldCourtCueTests.BrightLookDarkHullChoiceCaptures` renders the choice.
        [Range(0,.6f)] public float CastInkFloor=0;

        [Header("Grade")]
        // Colour-protecting saturation lift after the tonemap.
        // ⚠️ 0.14, DOWN FROM 0.24 (2026-09-25; 0.08 read dull once the shade turned violet). The cast palette is already near full chroma,
        // so on top of the old exposure it drove channels to their floor: the before frame read
        // Eskinita's yellow jacket at (255,225,5) and orange skin at (244,121,5), neon rather
        // than painted. PEAK's lit khaki shirt is (239,194,97): saturated, with the third
        // channel still alive.
        [Range(0,.6f)] public float Vibrance=.14f;
        // ⚠️⚠️ § SPLIT TONE, THE COLOUR-THEORY HALF OF THE GRADE (owner 2026-09-25: "the lighting
        // itself leans towards adjusting the shadows and brightness instead of adjusting the
        // ambient hues"). After the tonemap, dark values lean toward `ShadowHue` and light ones
        // toward `HighlightHue`, each at constant luminance, so it changes hue and never value.
        // This is what reaches the world's imported materials, which keep Unity's Standard
        // lighting and so cannot take the cast's ramp.
        [Range(0,.5f)] public float SplitTone=.18f;
        public Color ShadowHue=new Color(.66f,.56f,.92f);
        public Color HighlightHue=new Color(1,.88f,.70f);
        // HDR glow on true highlights: the sun, VFX cores and glints.
        // ⚠️ 1.7 AND 0.12, DOWN FROM 1.2 AND 0.22, BECAUSE THE FIRST RENDER HALOED THE CAST.
        // A lit toon body is albedo times a 1.3 sun plus the bright ambient, so a yellow shirt
        // sits near 1.5 to 1.8 in HDR and cleared a 1.2 threshold on every frame: Sean and the
        // Mini Bruja wore a glowing rim in all five maps. The sky is under 1.0 and never bloomed
        // at all. Above 1.7 only real highlights reach the chain, and the up-walk sums five
        // levels, so 0.12 already lands a visible glow on them.
        // ⚠️⚠️ AND IT WAS STILL HALOING THE CAST AT 1.7, BECAUSE OF THE KNEE (2026-09-25). The
        // prefilter's soft knee was a hard-coded 0.6 of the threshold, 1.02, so the knee opened
        // at 1.7 - 1.02 = 0.68: every sunlit body fed the chain, which is the glow the owner
        // named. The knee is now 0.2 of a 2.2 threshold, so nothing under 1.76 contributes,
        // and with the lower key a lit body tops out near 1.4. Only the sun disc, VFX cores and
        // glints bloom, at 0.05.
        [Range(0,1)] public float Bloom=.05f;
        [Range(.5f,3)] public float BloomThreshold=2.2f;
        // The soft knee as a share of the threshold. See above for why it is not 0.6.
        [Range(0,1)] public float BloomKnee=.2f;

        [Header("Sky")]
        // ⚠️ PAINTED CLOUDS (owner 2026-09-25: "do not make the cloud realistic"). 1 reads the
        // cloud panorama three mips down and re-edges it into two flat tones, PEAK's brushed
        // shapes; 0 keeps the photographic silhouettes. See `NeighbourhoodSky.shader`.
        [Range(0,1)] public float CloudPaint=1;

        // ⚠️⚠️ A WARM HORIZON NEEDS A CYAN ZENITH OR THE SKY BETWEEN THEM TURNS LAVENDER. The
        // sky shader blends horizon to zenith in linear light, and the first render paired a
        // peach horizon (red well above green) with a violet-leaning blue: every sky pixel in
        // between kept the red and the blue and lost the green, and Eskinita and SaBubong
        // measured (195,196,239) where PEAK is a clean blue. The warm maps now pair a cream or
        // gold horizon (green close to red) with a zenith whose green sits near 0.62, so the
        // blend passes through pale neutral. Warmth comes from the sun, the haze and the clouds'
        // lit faces instead. The shade tints (Sky ambient, CloudShade) moved the same way,
        // because a lavender shadow is the same fault on the ground.
        // ⚠️ SABUBONG'S HAZE STARTS AT 60 M, NOT 40. The rooftop is the one court ringed by
        // tall buildings at mid distance, and a haze starting at 40 m washed their faces to
        // the horizon gold, so the whole frame read milky rather than bright. Pushed out, the
        // towers keep their colour and only the skyline beyond them dissolves.
        // ⚠️⚠️ § THE TONE-DOWN, 2026-09-25. The owner: "tone down the brightness", "the
        // character glows", against a PEAK frame of three climbers on sand. Measured on that
        // frame: the lit green body is (48,160,77), luma 130, and the khaki shirt (239,194,97);
        // the darkest 1 per cent sits at luma 64; shadow on sand is a deeper, MORE saturated
        // sand (165,92,51), not a grey or blue one; the sky is a pale mint (210,230,222) and the
        // far mountain dissolves into teal air. Nothing on a character is near white. Our
        // bright frame had the cast at albedo x (1.34 sun + ~0.65 ambient), about 1.9 before
        // the curve, so every saturated colour clipped. So, per map:
        //  * SUN ABOUT 1.08, DOWN FROM ~1.33, and the AMBIENT TRILIGHT about 0.8 of its old
        //    strength. A lit body lands near albedo x 1.5, under Classic's authored ~1.7, which is
        //    what "toned down" means; the look stays brighter than Classic in its SHADOWS (lifted
        //    sun, 0.74 shadow strength), which is where PEAK is bright.
        //  * ⚠️⚠️ AND THE SHADE IS A HUE, NOT A DARKNESS (owner, same day: "peak's lighting
        //    typically uses artistic color theory for shadow and light colors (leaning more
        //    towards a slight purple instead of a plain dark shadow for cooler areas, and a more
        //    fuzzy orange for warmer settings)"). The first tone-down took the blue OUT of the
        //    shade and left it a plain dimmer version of the lit colour, which is exactly the
        //    flatness he named. The painter's rule instead, per map:
        //      - the KEY is warm (peach on the plaza, orange on the alley and the rooftop, cream
        //        under the bridge and on the lagoon);
        //      - the SKY term of the ambient, which in this pipeline is the colour of every shadow
        //        facing up or out, is a soft VIOLET, shifted away from the key's hue;
        //      - the EQUATOR is a mauve between the two, and the GROUND term a warm orange bounce
        //        off the court;
        //      - the cast's `ShadowTint` leans violet the same way, and the terminator band turns
        //        the form through orange at constant luminance (`Terminator`);
        //      - the coloured black floor (`Lift`) is violet, so even the darkest value is a hue.
        //    The warm maps (Eskinita, SaBubong) push both ends furthest; the cool ones (Ilalim,
        //    the Lagoon) keep a cream key and a bluer violet.
        //  * A LESS SATURATED, SLIGHTLY GREENER ZENITH (green about 0.70) so the sky reads as
        //    PEAK's soft teal-blue rather than a poster blue. The lavender rule above is about the
        //    SKY GRADIENT and still holds: violet belongs in the shade, never between horizon and
        //    zenith.
        //  * A DEEPER COLOURED BLACK FLOOR. PEAK's darkest 1 per cent sits at luma 64 and is
        //    coloured. ⚠️ `Lift` IS NOW WRITTEN AS A VECTOR, SO IT REALLY IS LINEAR: it went
        //    through `Material.SetColor`, which converts from sRGB in a linear project, so the
        //    0.012 to 0.02 authored here was reaching the shader as about 0.001, a floor nobody
        //    could see. 0.03 to 0.06 linear lands about luma 48 to 68.
        public MapLook[] Maps={
            new MapLook("BayanPlaza",new Color(.5f,.47f,.64f),new Color(.6f,.5f,.5f),new Color(.58f,.42f,.3f),new Color(.86f,.8f,1.1f),38,210,0,true)
                .Air(new Color(.74f,.85f,.94f),new Color(.44f,.7f,.88f),new Color(.76f,.87f,.95f),new Color(1,.96f,.9f),new Color(.72f,.7f,.86f))
                .Key(new Color(1,.9f,.76f),1.08f,52,.76f,new Color(.045f,.03f,.065f)),
            new MapLook("Eskinita",new Color(.52f,.46f,.62f),new Color(.64f,.5f,.46f),new Color(.62f,.42f,.28f),new Color(.9f,.78f,1.06f),34,190,1,false)
                .Air(new Color(.95f,.9f,.8f),new Color(.42f,.69f,.88f),new Color(.97f,.93f,.82f),new Color(1,.93f,.84f),new Color(.76f,.7f,.84f))
                .Key(new Color(1,.86f,.68f),1.1f,50,.74f,new Color(.055f,.03f,.06f)).Floor(1.6f),
            new MapLook("IlalimNgTulay",new Color(.46f,.47f,.64f),new Color(.5f,.5f,.58f),new Color(.5f,.42f,.36f),new Color(.82f,.82f,1.12f),36,180,2,false)
                .Air(new Color(.82f,.91f,.92f),new Color(.44f,.72f,.84f),new Color(.88f,.93f,.9f),new Color(.98f,.97f,.94f),new Color(.68f,.7f,.86f))
                .Key(new Color(1,.93f,.82f),1.06f,52,.72f,new Color(.035f,.03f,.065f)).Floor(1.6f),
            new MapLook("SaBubong",new Color(.54f,.46f,.64f),new Color(.66f,.5f,.48f),new Color(.62f,.42f,.3f),new Color(.92f,.78f,1.06f),60,300,3,true)
                .Air(new Color(.98f,.88f,.74f),new Color(.42f,.68f,.88f),new Color(1,.91f,.74f),new Color(1,.9f,.78f),new Color(.8f,.68f,.82f))
                .Key(new Color(1,.84f,.64f),1.08f,42,.74f,new Color(.06f,.03f,.065f)),
            new MapLook("Lagoon",new Color(.46f,.52f,.68f),new Color(.52f,.56f,.62f),new Color(.58f,.5f,.38f),new Color(.82f,.86f,1.1f),55,290,4,false)
                .Air(new Color(.7f,.87f,.94f),new Color(.38f,.7f,.86f),new Color(.74f,.89f,.95f),new Color(.98f,.98f,.95f),new Color(.68f,.74f,.88f))
                .Key(new Color(1,.94f,.84f),1.1f,52,.72f,new Color(.03f,.035f,.065f))
        };
        public MapLook Find(string map)
        {foreach(var entry in Maps)if(entry.Map==map)return entry;return null;}
        private static WorldLookProfile _current;
        public static WorldLookProfile Current
        {
            get
            {
                if(_current!=null && _current.hideFlags!=HideFlags.HideAndDontSave)return _current;
                var authored=Resources.Load<WorldLookProfile>("WorldLookProfile");
                if(authored!=null)
                {
                    if(_current!=null){if(Application.isPlaying)Destroy(_current);else DestroyImmediate(_current);}
                    return _current=authored;
                }
                if(_current==null){_current=CreateInstance<WorldLookProfile>();_current.hideFlags=HideFlags.HideAndDontSave;}
                return _current;
            }
        }
    }
}
