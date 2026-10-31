// A STRING OF PHAISTER'S PUPPET (`PhaisterPuppetStrings`). A plain cord: lit lilac down its middle, her deep violet at its edges so it
// holds against a pale sky and a pale wall alike. No black and no white edge, no stitches and no glow to depend on. The vertex colour
// is how taut it is (the caller's) and its alpha fades it. Premultiplied, one pixel soft across.
Shader "TumbangPreso/PhaisterPuppetString"
{
    Properties
    {
        _Edge ("Edge", Color) = (0.20,0.06,0.36,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+14" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Edge;
            struct appdata { float4 vertex:POSITION; float4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 colour:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.colour = v.colour; o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float across = abs(i.uv.y * 2.0 - 1.0);
                float cover = saturate((1.0 - across) / max(fwidth(across), 1e-3));
                fixed3 c = lerp(i.colour.rgb, _Edge.rgb, smoothstep(0.30, 0.62, across));
                float a = cover * i.colour.a;
                return fixed4(c * a, a);
            }
            ENDCG
        }
    }
}
