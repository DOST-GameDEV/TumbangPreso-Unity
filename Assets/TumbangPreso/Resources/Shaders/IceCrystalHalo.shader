// The soft halo round the bright parts of an ice crystal (`Shaders/IceCrystal`): one additive card, turned to the lens
// by whoever places it.
//
// ⚠️ WHY IT IS A CARD. Nothing blooms in this game's cameras, and a solid surface cannot draw outside its own outline,
// so a glow that is meant to spill past the crystal has to be drawn as a thing. It is brightest in the middle and gone
// before the card's edge, with no hard rim anywhere, so it never reads as a disc.
//
// Additive, no depth write, tested against depth so the floor and nearer things still hide it. No texture, no time.
Shader "TumbangPreso/IceCrystalHalo"
{
    Properties
    {
        _Color ("Halo colour", Color) = (0.10, 0.62, 1.00, 1)
        _Strength ("Strength (set per frame)", Range(0,2)) = 0.5
        _Core ("How much of it is a hot middle", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Strength, _Core;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv*2.0-1.0; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float fall=saturate(1.0-length(i.uv));
                // A long soft skirt and, inside it, a small hotter middle.
                float soft=fall*fall*(3.0-2.0*fall);
                float a=soft*soft*(1.0-_Core)+pow(fall,6.0)*_Core*2.0;
                return fixed4(_Color.rgb*a*_Strength,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
