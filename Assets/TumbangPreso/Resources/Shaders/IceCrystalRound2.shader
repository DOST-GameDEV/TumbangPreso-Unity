// THE SECOND SET'S SHADER, KEPT ASIDE. Nothing in the game uses it. The owner looked at this set beside the first and
// said of the first: "this old one was better", so `Shaders/IceCrystal` is the game's and this one is kept whole in
// case any of it is wanted (`tools/build_ice_crystals_round2.py`, `Resources/Models/IceCrystalsRound2`). Against the
// first it has a third glow colour (`_GlowMid`), a frost that is clouded by the paint, a soft sheen where the first
// has a hard glint, and different numbers throughout. What follows is its own description, as it was written.
//
// Ice that knows where it is thin: the shader of the sculpted and baked crystals
// (`tools/build_ice_crystals_round2.py`, `Resources/Models/IceCrystalsRound2`).
//
// ⚠️ WHY IT EXISTS. The owner read an article on a stylised crystal that is sculpted, baked and painted from its bakes,
// asked why our ice was not made that way, and said: "try making an ice crystals". The ice we had
// (`Shaders/UltimateIceLotus`) lays a core, a rim and a glint over a painted atlas, all of them worked out from the
// face's direction alone, so every crystal is lit the same all over and reads as pale frosted plastic. What it cannot
// know is the one thing that makes ice look like ice: WHERE THE CRYSTAL IS THIN. That is not a thing a shader can find
// out on a phone, so it is BAKED: for every point of the surface, how much crystal lies behind it. With that:
//   * VALUE STEPS: each cut face takes one of four values from a fixed key light, so a light face sits against a mid
//     face against a dark one. Stepped, not graded: it is still this game's world.
//   * DEPTH: a face turned from the light goes to navy, and so does a THICK part looked straight into, with a second,
//     shifted read of the paint under it so the inside seems to sit below the surface.
//   * GLOW-THROUGH: the foot, the thin end and every thin facet are FLOODED with light, the mask says where; more when
//     the lens looks toward the light and on faces the light is behind. Thick middles get none. That contrast is the
//     whole effect.
//   * EDGE LIGHT: every edge between two faces, from a mask at two widths: a soft wide one under a thin sharp one.
//     The cracks are in it too. This net of lines is most of what reads as "cut ice".
//   * A SHEEN, not a glint: a soft broken frost on the faces the key lights.
//
// ⚠️ THREE HUES, KEPT APART. The body is navy to deep blue (the paint and `_Deep`). The light coming through is aqua
// to cyan, clearly GREENER than the body, and near white only where it floods (`_GlowDeep`, `_GlowMid`, `_GlowHot`).
// The faces the key lights are a pale frosty grey-white (`_LitColor`). A first pass was one royal blue for all three,
// glow included, and it read as blue plastic with brighter blue edges.
//
// ⚠️ THE KEY LIGHT IS A PROPERTY, NOT THE SCENE'S. An effect is filmed outside Play and drawn in a cutscene with the
// match paused; it must look the same wherever it stands. `_KeyDir` points TOWARD the light, in world space.
//
// ⚠️ TIME IS `_Age`, set by whoever owns the crystal. Nothing here reads `_Time` (`UltimateIceLotus` has the reason).
//
// ⚠️ THE NORMAL MAP IS READ AS PLAIN RGB (x2, -1), so it must be imported as a default texture with sRGB OFF, and so
// must the mask. `IceCrystalFilm.EnsureImports` sets both. Imported as a Unity normal map it would be repacked and
// read wrong here.
//
// ⚠️ WRITTEN FOR PHONES AS WELL (GLES3): four texture reads, no loops, no screen-space derivatives, no depth texture,
// no grab pass. SOLID: it writes depth and needs no sorting, so a cluster standing through itself draws correctly.
// The soft halo round the bright parts is not in here (a solid surface cannot draw outside itself): it is a separate
// additive card, `Shaders/IceCrystalHalo`.
Shader "TumbangPreso/IceCrystalRound2"
{
    Properties
    {
        _MainTex ("Paint (RGB), sheen break-up (A)", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Tangent normal, plain RGB, linear", 2D) = "bump" {}
        [NoScaleOffset] _MaskTex ("R glow-through, G soft edge, B sharp edge and cracks, A thickness, linear", 2D) = "black" {}
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _Bump ("Normal map strength", Range(0,2)) = 0.8
        _Shade ("Value of the darkest step", Range(0,1)) = 0.40
        _Deep ("Depth colour (navy)", Color) = (0.010, 0.040, 0.165, 1)
        _DeepAmount ("Faces turned away go this far to it", Range(0,1)) = 0.50
        _LitColor ("Frost the bright steps are pushed toward", Color) = (0.72, 0.86, 0.95, 1)
        _LitAmount ("How far they are pushed", Range(0,1)) = 0.52
        _Depth ("Thick parts looked into go this far to navy", Range(0,1)) = 0.70
        _Parallax ("Inner layer shift", Range(0,0.08)) = 0.03
        _Inner ("Inner layer strength", Range(0,1)) = 0.16
        _GlowDeep ("Light coming through, a little of it", Color) = (0.00, 0.50, 0.85, 1)
        _GlowMid ("A lot of it (cyan)", Color) = (0.00, 0.95, 0.95, 1)
        _GlowHot ("Where it floods", Color) = (0.80, 1.00, 0.98, 1)
        _Glow ("Glow strength", Range(0,4)) = 1.30
        _GlowBase ("Glow with no help", Range(0,2)) = 0.62
        _GlowBack ("More when the lens looks toward the light", Range(0,2)) = 0.45
        _GlowThrough ("More on faces the light is behind", Range(0,2)) = 0.55
        _RimAmount ("Light in the outline", Range(0,2)) = 0.55
        _EdgeSoft ("Soft wide edge", Color) = (0.05, 0.75, 0.95, 1)
        _EdgeSharp ("Thin sharp edge", Color) = (0.80, 1.00, 1.00, 1)
        _EdgeSoftAmount ("Soft edge strength", Range(0,2)) = 0.40
        _EdgeSharpAmount ("Sharp edge strength", Range(0,2)) = 1.05
        _GlintColor ("Sheen", Color) = (0.85, 0.93, 0.97, 1)
        _GlintPower ("Sheen tightness", Range(2,128)) = 12
        _GlintAmount ("Sheen strength", Range(0,2)) = 0.22
        _Age ("Owner's clock (s, set per frame)", Float) = 0
        _Shimmer ("Shimmer of the sheen (0 is still)", Range(0,1)) = 0.5
        _ShimmerPace ("Shimmer pace", Float) = 2.2
        _Lit ("Lit from inside (set per frame, 1 is normal)", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _BumpMap, _MaskTex;
            float4 _MainTex_ST, _KeyDir;
            fixed4 _Deep, _LitColor, _GlowDeep, _GlowMid, _GlowHot, _EdgeSoft, _EdgeSharp, _GlintColor;
            float _Bump, _Shade, _DeepAmount, _LitAmount, _Depth, _Parallax, _Inner, _Glow, _GlowBase, _GlowBack, _GlowThrough;
            float _RimAmount, _EdgeSoftAmount, _EdgeSharpAmount, _GlintPower, _GlintAmount, _Age, _Shimmer, _ShimmerPace, _Lit;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 tangent:TEXCOORD2;
                float3 bitangent:TEXCOORD3; float3 toLens:TEXCOORD4;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent=cross(o.normal,o.tangent)*v.tangent.w*unity_WorldTransformParams.w;
                o.toLens=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 paint=tex2D(_MainTex,i.uv);
                fixed4 mask=tex2D(_MaskTex,i.uv);
                float glow=mask.r, wide=mask.g, thin=mask.b, thick=mask.a;
                float3 nt=tex2D(_BumpMap,i.uv).xyz*2.0-1.0;
                nt.xy*=_Bump;
                float3 ng=normalize(i.normal), t=normalize(i.tangent), b=normalize(i.bitangent);
                float3 n=normalize(t*nt.x+b*nt.y+ng*nt.z);
                float3 v=normalize(i.toLens), l=normalize(_KeyDir.xyz);
                float ndl=dot(n,l);

                // FOUR VALUES, three steps. A face is flat, so it lands on one of them whole.
                float mid=smoothstep(.18,.28,ndl), top=smoothstep(.58,.68,ndl);
                float lit=smoothstep(-.25,-.15,ndl)*.35+mid*.35+top*.30;
                float3 col=paint.rgb*lerp(_Shade,1.0,lit);
                col=lerp(col,_Deep.rgb,(1.0-lit)*_DeepAmount);
                // The two bright steps are pushed toward frost, the brightest most: paint alone never gets pale. The
                // frost is not even: the painted break-up clouds it.
                col=lerp(col,_LitColor.rgb*(.82+.30*paint.a),(mid*.12+top*.7)*_LitAmount);

                // DEPTH. Looked straight into, a thick part is dark; and the paint is read again, shifted against the
                // view, and laid in faintly, so there seems to be something under the surface that moves less than it.
                float facing=saturate(dot(n,v));
                float into=thick*facing*facing;
                col=lerp(col,_Deep.rgb*.6,into*_Depth);
                float3 vt=float3(dot(v,t),dot(v,b),dot(v,ng));
                float3 under=tex2D(_MainTex,i.uv-vt.xy/(abs(vt.z)+.5)*_Parallax).rgb;
                col+=under*into*_Inner;

                // GLOW-THROUGH. The mask says where the crystal is flooded; the lens and the light say how much. Its
                // colour climbs with it: aqua, then cyan, then near white where it floods.
                float back=saturate(dot(v,-l)*.5+.5);
                float through=saturate(-ndl*.5+.5);
                float fres=1.0-facing; fres=fres*fres*fres;
                float g=glow*(_GlowBase+_GlowBack*back*back+_GlowThrough*through);
                g+=fres*_RimAmount*(1.0-thick*.6);
                g*=_Lit;
                float3 gc=lerp(_GlowDeep.rgb,_GlowMid.rgb,saturate(g*1.5));
                gc=lerp(gc,_GlowHot.rgb,smoothstep(.85,1.50,g));
                col+=gc*g*_Glow;

                // EDGE LIGHT: the soft wide pass, then the thin sharp one on top. Brighter on the lit side.
                col+=_EdgeSoft.rgb*wide*_EdgeSoftAmount*(.6+.4*lit);
                col+=_EdgeSharp.rgb*thin*_EdgeSharpAmount*(.55+.45*lit);

                // THE SHEEN: wide and soft, only as strong as the face is lit, clouded by the painted break-up.
                // `_Age` slides the break-up a little so it glitters. (A first pass was a tight hard glint cut by a
                // hard mask: white flakes on a face, like peeling paint.)
                float spec=pow(saturate(dot(n,normalize(l+v))),_GlintPower);
                float broken=paint.a+_Shimmer*.12*sin(_Age*_ShimmerPace+paint.a*17.0+i.uv.x*31.0);
                col+=_GlintColor.rgb*smoothstep(.20,.90,spec)*saturate(.25+broken*.9)*(.3+.7*lit)*_GlintAmount;
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
