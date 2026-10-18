// Ice that knows where it is thin: the shader of the sculpted and baked crystals (`tools/build_ice_crystals.py`,
// `Resources/Models/IceCrystals`).
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
//   * GLOW-THROUGH: the thin parts, the tips and the outline carry saturated aqua light, more when the lens looks
//     toward the light and on faces the light is behind. Thick middles get none. That contrast is the whole effect.
//   * EDGE LIGHT: every edge between two faces, from a baked edge mask at two widths: a soft wide one under a thin
//     sharp one. The cracks are in it too.
//   * A BROKEN GLINT: the key light's highlight, cut up by a painted mask, so it is never a clean bar.
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
// ⚠️ TWO OPT-IN INPUTS, BOTH OFF BY DEFAULT, for Yasmin's GLACIAL WALL (`CheskaGlacialWallLook`), which draws many
// crystals as one baked mesh a kind. `_Grow` (0 off) raises each crystal out of the ground and sinks it again, from what
// the mesh carries per vertex (TEXCOORD1 the crystal's foot, TEXCOORD2 its birth, height, death and seed), on `_Age`.
// `_Flow` (0 off) lets cold light drift through the inside at two speeds. At 0 the approved crystals draw exactly as
// they were filmed: the first changes nothing when it is off, the second adds nothing.
//
// ⚠️ WRITTEN FOR PHONES AS WELL (GLES3): four texture reads, no loops, no screen-space derivatives, no depth texture,
// no grab pass. SOLID: it writes depth and needs no sorting, so a cluster standing through itself draws correctly.
// The soft halo round the bright parts is not in here (a solid surface cannot draw outside itself): it is a separate
// additive card, `Shaders/IceCrystalHalo`.
Shader "TumbangPreso/IceCrystal"
{
    Properties
    {
        _MainTex ("Paint (RGB), glint break-up (A)", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Tangent normal, plain RGB, linear", 2D) = "bump" {}
        [NoScaleOffset] _MaskTex ("R glow-through, G soft edge, B sharp edge, A thickness, linear", 2D) = "black" {}
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _Bump ("Normal map strength", Range(0,2)) = 0.8
        _Shade ("Value of the darkest step", Range(0,1)) = 0.40
        _Deep ("Depth colour", Color) = (0.012, 0.050, 0.200, 1)
        _DeepAmount ("Faces turned away go this far to it", Range(0,1)) = 0.50
        _LitColor ("Frost the brightest step is pushed toward", Color) = (0.42, 0.80, 1.00, 1)
        _LitAmount ("How far it is pushed", Range(0,1)) = 0.42
        _Depth ("Thick parts looked into go this far to it", Range(0,1)) = 0.70
        _Parallax ("Inner layer shift", Range(0,0.08)) = 0.03
        _Inner ("Inner layer strength", Range(0,1)) = 0.16
        _GlowDeep ("Light coming through", Color) = (0.00, 0.56, 1.00, 1)
        _GlowHot ("Where it is strongest", Color) = (0.62, 0.98, 1.00, 1)
        _Glow ("Glow strength", Range(0,4)) = 1.35
        _GlowBase ("Glow with no help", Range(0,2)) = 0.40
        _GlowBack ("More when the lens looks toward the light", Range(0,2)) = 0.80
        _GlowThrough ("More on faces the light is behind", Range(0,2)) = 0.80
        _EdgeSoft ("Soft wide edge", Color) = (0.10, 0.62, 0.95, 1)
        _EdgeSharp ("Thin sharp edge", Color) = (0.72, 0.97, 1.00, 1)
        _EdgeSoftAmount ("Soft edge strength", Range(0,2)) = 0.42
        _EdgeSharpAmount ("Sharp edge strength", Range(0,2)) = 0.95
        _RimColor ("Outline light", Color) = (0.10, 0.70, 1.00, 1)
        _RimAmount ("Outline light strength", Range(0,2)) = 0.55
        _GlintColor ("Glint", Color) = (0.90, 1.00, 1.00, 1)
        _GlintPower ("Glint tightness", Range(4,128)) = 56
        _GlintAmount ("Glint strength", Range(0,2)) = 0.6
        _Age ("Owner's clock (s, set per frame)", Float) = 0
        _Shimmer ("Shimmer of the glint (0 is still)", Range(0,1)) = 0.5
        _ShimmerPace ("Shimmer pace", Float) = 2.2
        _Lit ("Lit from inside (set per frame, 1 is normal)", Range(0,3)) = 1
        _Grow ("Rise from the ground per vertex (0 off)", Float) = 0
        _GrowTime ("Seconds a crystal takes to rise", Float) = 0.22
        _DieAt ("Owner's clock at which the sinking starts", Float) = 100000
        _DieTime ("Seconds a crystal takes to sink", Float) = 0.35
        _Flow ("Cold light drifting inside (0 off)", Range(0,2)) = 0
        [NoScaleOffset] _FlowTex ("Drifting light map", 2D) = "black" {}
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
            sampler2D _MainTex, _BumpMap, _MaskTex, _FlowTex;
            float _Grow, _GrowTime, _DieAt, _DieTime, _Flow;
            float4 _MainTex_ST, _KeyDir;
            fixed4 _Deep, _LitColor, _GlowDeep, _GlowHot, _EdgeSoft, _EdgeSharp, _RimColor, _GlintColor;
            float _Bump, _Shade, _DeepAmount, _LitAmount, _GlintAmount, _Depth, _Parallax, _Inner, _Glow, _GlowBase, _GlowBack, _GlowThrough;
            float _EdgeSoftAmount, _EdgeSharpAmount, _RimAmount, _GlintPower, _Age, _Shimmer, _ShimmerPace, _Lit;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; float4 foot:TEXCOORD1; float4 life:TEXCOORD2; };
            struct v2f
            {
                float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 tangent:TEXCOORD2;
                float3 bitangent:TEXCOORD3; float3 toLens:TEXCOORD4; float3 at:TEXCOORD5;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.at=v.vertex.xyz;
                if (_Grow>0.5)
                {
                    // Out of the ground: it rises from under the road on an easing that slows into place, stretches
                    // a little as it arrives and settles; at its death it sinks back in and thins, where it stands.
                    float g=saturate((_Age-v.life.x)/_GrowTime);
                    float rise=1.0-(1.0-g)*(1.0-g)*(1.0-g);
                    float bump=sin(saturate((g-0.45)/0.55)*3.14159);
                    float d=saturate((_Age-_DieAt-v.life.z)/_DieTime);
                    d=d*d*(3.0-2.0*d);
                    float3 rel=v.vertex.xyz-v.foot.xyz;
                    rel.y*=(1.0+0.10*bump)*(1.0-0.20*d);
                    rel.xz*=(lerp(0.62,1.0,rise)-0.05*bump)*(1.0-0.40*d);
                    rel.y-=(1.0-rise)*v.life.y*1.08+d*v.life.y*1.05;
                    v.vertex.xyz=v.foot.xyz+rel;
                }
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
                // The two bright steps are pushed toward frost, the brightest most: paint alone never gets pale.
                col=lerp(col,_LitColor.rgb,(mid*.3+top*.7)*_LitAmount);

                // DEPTH. Looked straight into, a thick part is dark; and the paint is read again, shifted against the
                // view, and laid in faintly, so there seems to be something under the surface that moves less than it.
                float facing=saturate(dot(n,v));
                float into=thick*facing*facing;
                col=lerp(col,_Deep.rgb*.6,into*_Depth);
                float3 vt=float3(dot(v,t),dot(v,b),dot(v,ng));
                float3 under=tex2D(_MainTex,i.uv-vt.xy/(abs(vt.z)+.5)*_Parallax).rgb;
                col+=under*into*_Inner;

                // GLOW-THROUGH. The baked mask says where the crystal is thin; the lens and the light say how much.
                float back=saturate(dot(v,-l)*.5+.5);
                float through=saturate(-ndl*.5+.5);
                float fres=1.0-facing; fres=fres*fres*fres;
                float g=glow*(_GlowBase+_GlowBack*back*back+_GlowThrough*through);
                g+=fres*_RimAmount*(1.0-thick*.6);
                g*=_Lit;
                col+=lerp(_GlowDeep.rgb,_GlowHot.rgb,smoothstep(.80,1.50,g))*g*_Glow;

                // EDGE LIGHT: the soft wide pass, then the thin sharp one on top. Brighter on the lit side.
                col+=_EdgeSoft.rgb*wide*_EdgeSoftAmount*(.6+.4*lit);
                col+=_EdgeSharp.rgb*thin*_EdgeSharpAmount*(.55+.45*lit);

                // THE GLINT, broken by the painted mask; `_Age` slides the break-up a little so it glitters.
                float spec=pow(saturate(dot(n,normalize(l+v))),_GlintPower);
                float broken=paint.a+_Shimmer*.22*sin(_Age*_ShimmerPace+paint.a*17.0+i.uv.x*31.0);
                col+=_GlintColor.rgb*smoothstep(.42,.50,spec*(.35+broken))*_GlintAmount;
                if (_Flow>0.0)
                {
                    // Cold light drifting through: one map read twice, crossing at two speeds and multiplied, so what
                    // lights up inside changes every frame; strongest where the crystal is thin.
                    float f=tex2D(_FlowTex,i.at.xy*0.55+float2(0.0,_Age*0.11)).r*tex2D(_FlowTex,i.at.zy*0.42+float2(_Age*0.07,-_Age*0.05)).r;
                    f=smoothstep(0.18,0.62,f);
                    col+=lerp(_GlowDeep.rgb,_GlowHot.rgb,f)*f*_Flow*(0.35+0.65*glow);
                }
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
