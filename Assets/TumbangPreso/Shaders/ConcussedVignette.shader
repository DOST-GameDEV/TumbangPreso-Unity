// CONCUSSED, THE VICTIM'S OWN SCREEN (Basilio's Boulder and Continental Drift; `docs/reports/dante-presentation-2026-10-16/plan.md`
// section 3). A dull dust-ochre rim, the colour of the court's dust after a stone hits, thick in the corners and clear through the
// middle (a rim, never a filter: the victim still has to read the court and run). Its inner edge is chipped, a few broad angular
// steps round the frame like a broken stone's outline, never grain (the owner refused texture noise on his stone). It throbs
// slowly, a headache's pulse, and thins as the status runs out. The double image and the ringing live elsewhere
// (`Visual.ConcussedPerception`). Reduce visual effects stills the throb and keeps the rim.
Shader "TumbangPreso/ConcussedVignette"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Dust ("Dust ochre", Color) = (0.42, 0.32, 0.22, 1)
        _Basalt ("Basalt", Color) = (0.20, 0.17, 0.15, 1)
        _Presence ("Presence", Range(0, 1)) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Clock ("Clock", Float) = 0
        _Calm ("Reduced effects", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            fixed4 _Dust, _Basalt;
            float _Presence, _Aspect, _Clock, _Calm;

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            // Eleven typed bites round the frame (inward, as a share of the rim): a chipped outline, uneven on purpose.
            float bite(int k)
            {
                k = k % 11;
                return k == 0 ? 0.06 : k == 1 ? 0.00 : k == 2 ? 0.09 : k == 3 ? 0.03 : k == 4 ? 0.11 : k == 5 ? 0.01
                     : k == 6 ? 0.07 : k == 7 ? 0.10 : k == 8 ? 0.02 : k == 9 ? 0.08 : 0.04;
            }
            float chip(float turn)
            {
                float f = frac(turn) * 11.0;
                int a = (int)floor(f);
                // Hard steps with a short ramp: broken stone, not a wave.
                return lerp(bite(a), bite(a + 1), smoothstep(0.82, 1.0, frac(f)));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                // A rounded-rectangle distance, so the rim hugs the frame on any aspect and is heaviest in the corners.
                float2 q4 = p * p; q4 *= q4;
                float r = pow(q4.x + q4.y, 0.25);
                float turn = atan2(p.y, p.x * _Aspect) / 6.2831853;
                float throb = _Calm > 0.5 ? 0.5 : 0.5 + 0.5 * sin(_Clock * 5.6);
                float inner = 0.70 - 0.05 * throb + chip(turn) * 0.6 + (1.0 - _Presence) * 0.22;
                float body = smoothstep(inner, inner + 0.28, r);
                // A thin darker band along the chipped edge, the stone's broken lip, so it reads as an edge and not as haze.
                float lip = smoothstep(inner - 0.015, inner + 0.02, r) * (1.0 - smoothstep(inner + 0.02, inner + 0.07, r));
                fixed3 colour = lerp(_Dust.rgb, _Basalt.rgb, saturate(body * 0.55 + lip * 0.8));
                float a = saturate(body * 0.30 + lip * 0.12) * _Presence;
                return fixed4(colour, a);
            }
            ENDCG
        }
    }
}
