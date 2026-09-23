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
        // A warm band where lit turns to shade; the "soft toy" warmth in PEAK's cast.
        [Range(0,.4f)] public float Terminator=.14f;
        [Range(0,.25f)] public float UpperRim=.13f;
        [Range(0,.2f)] public float FeetShade=.10f;
        [Range(0,.2f)] public float EnvironmentContact=.08f;
        [Range(0,.3f)] public float MetalHighlight=.22f;

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

        [Header("Grade")]
        // Colour-protecting saturation lift after the tonemap.
        [Range(0,.6f)] public float Vibrance=.24f;
        // HDR glow on true highlights: the sun, VFX cores and glints.
        // ⚠️ 1.7 AND 0.12, DOWN FROM 1.2 AND 0.22, BECAUSE THE FIRST RENDER HALOED THE CAST.
        // A lit toon body is albedo times a 1.3 sun plus the bright ambient, so a yellow shirt
        // sits near 1.5 to 1.8 in HDR and cleared a 1.2 threshold on every frame: Sean and the
        // Mini Bruja wore a glowing rim in all five maps. The sky is under 1.0 and never bloomed
        // at all. Above 1.7 only real highlights reach the chain, and the up-walk sums five
        // levels, so 0.12 already lands a visible glow on them.
        [Range(0,1)] public float Bloom=.12f;
        [Range(.5f,3)] public float BloomThreshold=1.7f;

        public MapLook[] Maps={
            new MapLook("BayanPlaza",new Color(.56f,.63f,.80f),new Color(.66f,.62f,.56f),new Color(.56f,.46f,.36f),new Color(.86f,.88f,1.08f),38,210,0,true)
                .Air(new Color(.87f,.91f,.95f),new Color(.33f,.62f,.93f),new Color(.90f,.93f,.94f),new Color(1,.99f,.96f),new Color(.70f,.76f,.90f))
                .Key(new Color(1,.95f,.84f),1.32f,52,.76f,new Color(.012f,.012f,.022f)),
            new MapLook("Eskinita",new Color(.58f,.60f,.80f),new Color(.70f,.60f,.54f),new Color(.60f,.45f,.33f),new Color(.92f,.86f,1.06f),34,190,1,false)
                .Air(new Color(.96f,.86f,.76f),new Color(.40f,.64f,.92f),new Color(.98f,.88f,.76f),new Color(1,.97f,.90f),new Color(.74f,.72f,.88f))
                .Key(new Color(1,.91f,.76f),1.34f,50,.74f,new Color(.018f,.011f,.020f)),
            new MapLook("IlalimNgTulay",new Color(.52f,.66f,.76f),new Color(.60f,.64f,.62f),new Color(.52f,.47f,.40f),new Color(.84f,.94f,1.06f),36,180,2,false)
                .Air(new Color(.82f,.91f,.92f),new Color(.36f,.66f,.88f),new Color(.88f,.93f,.90f),new Color(1,.99f,.95f),new Color(.66f,.76f,.84f))
                .Key(new Color(1,.94f,.82f),1.30f,52,.72f,new Color(.010f,.016f,.020f)),
            new MapLook("SaBubong",new Color(.62f,.58f,.82f),new Color(.74f,.60f,.56f),new Color(.62f,.46f,.38f),new Color(.96f,.84f,1.08f),40,210,3,true)
                .Air(new Color(.98f,.84f,.76f),new Color(.44f,.58f,.90f),new Color(1,.84f,.70f),new Color(1,.93f,.82f),new Color(.76f,.68f,.88f))
                .Key(new Color(1,.87f,.70f),1.32f,42,.74f,new Color(.020f,.012f,.024f)),
            new MapLook("Lagoon",new Color(.52f,.72f,.86f),new Color(.62f,.74f,.72f),new Color(.60f,.56f,.44f),new Color(.84f,.96f,1.06f),55,290,4,false)
                .Air(new Color(.80f,.92f,.95f),new Color(.26f,.62f,.92f),new Color(.86f,.95f,.96f),new Color(1,1,.97f),new Color(.66f,.78f,.90f))
                .Key(new Color(1,.96f,.86f),1.36f,52,.72f,new Color(.008f,.016f,.022f))
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
