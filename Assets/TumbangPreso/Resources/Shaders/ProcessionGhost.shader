// NEMU'S PROCESSION GHOSTS (PRUSISYON, 2026-10-11, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 1).
// The owner: *"i dont want ghosts to be too blocky kahit translucent sila"*. So they are smooth lathed sheets drawn as
// light, not tinted boxes:
//  1. DEPTH ONLY first, so the colour pass draws only each ghost's FRONT (no inner surface showing through another).
//  2. PREMULTIPLIED colour: the body veils what is behind it by `_Alpha` (a soft pale lilac, shaded a little so its
//     form reads), and the rim, brighter where the surface turns away from the lens, is ADDED on top, so a ghost's
//     edges glow against a lit court the way a thing made of light does.
//  3. The hem dissolves into mist: below `_FadeHigh` metres (object space, the mesh's own y) it thins to nothing at
//     `_FadeLow`, wavering so it reads as cloth turning to smoke rather than a cut.
// `_Presence` fades the whole ghost in and out (forming, leaving, grip). Never pure white: the tint is lilac grey.
Shader "TumbangPreso/ProcessionGhost"
{
    Properties
    {
        _Tint ("Body", Color) = (0.84, 0.80, 0.92, 1)
        _RimColor ("Rim", Color) = (0.78, 0.66, 1.0, 1)
        _Alpha ("Body alpha", Range(0, 1)) = 0.32
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.5
        _Glow ("Rim glow (added)", Range(0, 2)) = 0.7
        _Presence ("Presence", Range(0, 1)) = 1
        _FadeLow ("Mist: gone below (object y)", Float) = -1
        _FadeHigh ("Mist: whole above (object y)", Float) = -0.9
        _Waver ("Mist waver", Float) = 0.03
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _FadeLow, _FadeHigh, _Waver;
            struct v2f { float4 pos : SV_POSITION; float3 obj : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.obj = v.vertex.xyz; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                // The dissolved mist must not block what is behind it either.
                float waver = sin(i.obj.x * 37.0 + _Time.y * 2.1) * _Waver + sin(i.obj.z * 23.0 - _Time.y * 1.3) * _Waver * 0.6;
                clip(i.obj.y + waver - lerp(_FadeLow, _FadeHigh, 0.5));
                return 0;
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Tint, _RimColor;
            float _Alpha, _RimAlpha, _Glow, _Presence, _FadeLow, _FadeHigh, _Waver;
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view : TEXCOORD1;
                float3 obj : TEXCOORD2;
            };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                o.obj = v.vertex.xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal), v = normalize(i.view);
                float facing = saturate(dot(n, v));
                float rim = pow(1.0 - facing, 2.2);
                // A little form: lighter on top, darker underneath, so a hood reads as a hood.
                float shade = 0.78 + 0.22 * saturate(n.y * 0.5 + 0.5);
                float waver = sin(i.obj.x * 37.0 + _Time.y * 2.1) * _Waver + sin(i.obj.z * 23.0 - _Time.y * 1.3) * _Waver * 0.6;
                float mist = smoothstep(_FadeLow, _FadeHigh, i.obj.y + waver);
                float body = _Alpha * mist * _Presence;
                float edge = rim * _RimAlpha * mist * _Presence;
                float a = saturate(body + edge);
                float3 colour = _Tint.rgb * shade * body + _RimColor.rgb * rim * _Glow * mist * _Presence;
                return fixed4(colour, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
