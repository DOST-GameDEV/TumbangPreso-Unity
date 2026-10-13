// THE POSSESSED EYES, ON THE PAINTED EYES (LET THEM IN, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
// section 16). The owner liked glowing eyes but not glows floating in front of the face (*"ur current impleemtnation sucsk"*), so
// the head is drawn a second time with this: inside the hero's two eye rectangles (`Resources/Visual/eye-rects.txt`, from
// `tools/author_eye_rects.py`) every texel that is not skin burns violet, hot in the middle of the eye, deep at its edge.
// Black and one violet: the hot core is a pale violet, never white.
// Loaded through Resources (`Shaders/NemuEyes`), which keeps it in the player.
Shader "TumbangPreso/NemuEyes"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _EyeA ("Left eye (u0 v0 u1 v1)", Vector) = (0, 0, 0, 0)
        _EyeB ("Right eye (u0 v0 u1 v1)", Vector) = (0, 0, 0, 0)
        _Skin ("Skin", Color) = (0.87, 0.68, 0.54, 1)
        _Deep ("Edge", Color) = (0.42, 0.10, 0.95, 1)
        _Hot ("Core", Color) = (0.86, 0.62, 1.0, 1)
        _Amount ("Amount", Range(0, 1)) = 0
        _Clock ("Clock (s)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _EyeA, _EyeB;
            fixed4 _Skin, _Deep, _Hot;
            float _Amount, _Clock;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // How deep inside a rectangle a UV is: 0 at its edge, 1 at its middle, below 0 outside.
            float inside(float2 uv, float4 r)
            {
                float2 c = (r.xy + r.zw) * 0.5, h = max(1e-5, (r.zw - r.xy) * 0.5);
                float2 d = 1.0 - abs(uv - c) / h;
                return min(d.x, d.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float a = inside(i.uv, _EyeA), b = inside(i.uv, _EyeB);
                float k = max(a, b);
                clip(k);
                fixed3 tex = tex2D(_MainTex, i.uv).rgb;
                // Only the eye itself, not the skin round it inside the rectangle.
                float eye = smoothstep(0.18, 0.3, distance(tex, _Skin.rgb));
                // Hot toward the middle of the eye, with a slow pulse.
                float pulse = 0.85 + 0.15 * sin(_Clock * 7.0);
                float core = saturate(k * 1.6) * pulse;
                fixed3 rgb = lerp(_Deep.rgb, _Hot.rgb, core);
                return fixed4(rgb, eye * _Amount);
            }
            ENDCG
        }
    }
}
