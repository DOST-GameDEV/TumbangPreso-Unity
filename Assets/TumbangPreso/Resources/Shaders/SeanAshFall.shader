// ⚠️ ASH FALLING UNDER RAGO'S SKY (MAYON AWAKENS v4, 2026-11-23): the near layer of every shot from the sky's turn on
// (`docs/HERO_KIT_METHOD.md` section 6, "a near layer"). Grey-brown flakes tumbling down, about one in seven an ember.
//
// ⚠️ MOVED IN THE SHADER, ON A MESH BUILT ONCE. Every flake is four corners sharing one seed; the vertex program places it from
// the scene clock in a box that wraps round the lens (`_WorldSpaceCameraPos`), so the flakes are world-still as the lens moves
// (they do not ride with it) and there are always some near it whichever shot it is. Nothing on the CPU per flake.
// ⚠️ Flakes nearer than half a metre are dropped (one across the lens would be a grey blot) and the far ones thin out.
// ⚠️ Painted, not glowing: a flat tone with a darker rim; the embers are a saturated orange, never white.
Shader "TumbangPreso/SeanAshFall"
{
    Properties
    {
        _Amount ("How much ash (0 none)", Range(0, 1)) = 0
        _Clock ("Scene clock (s)", Float) = 0
        _Box ("Wrap box (m)", Float) = 16
        _Ash ("Ash", Color) = (0.30, 0.22, 0.20, 1)
        _Ember ("Ember", Color) = (1.0, 0.48, 0.12, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Amount, _Clock, _Box;
            fixed4 _Ash, _Ember;

            // uv0: the corner (-1 to 1); uv1: the seed (x, y, z in 0 to 1); uv2: (size 0 to 1, ember 0 or 1).
            struct appdata { float4 vertex : POSITION; float2 corner : TEXCOORD0; float3 seed : TEXCOORD1; float2 kind : TEXCOORD2; };
            struct v2f { float4 pos : SV_POSITION; float2 corner : TEXCOORD0; float2 kind : TEXCOORD1; float fade : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                float t = _Clock;
                // Falling, with a slow sideways tumble; embers fall slower and wander more (they ride the heat).
                float fall = lerp(1.1, 0.55, v.kind.y) * (0.8 + 0.4 * v.seed.x);
                float3 p = v.seed * _Box + float3(sin(t * 0.9 + v.seed.y * 17.0) * 0.35 + t * 0.4, -t * fall, cos(t * 0.7 + v.seed.z * 13.0) * 0.35);
                float3 cam = _WorldSpaceCameraPos;
                p = cam + (frac((p - cam) / _Box) - 0.5) * _Box;
                float dist = distance(p, cam);
                o.fade = smoothstep(0.5, 1.1, dist) * (1.0 - smoothstep(_Box * 0.32, _Box * 0.5, dist));
                // A flake turns as it falls: its quad is squashed on one axis by its spin.
                float spin = t * (2.0 + 3.0 * v.seed.z) + v.seed.x * 6.28;
                float size = lerp(0.025, 0.065, v.kind.x) * lerp(1.0, 0.6, v.kind.y) * step(0.001, _Amount * o.fade);
                float3 right = UNITY_MATRIX_V[0].xyz, up = UNITY_MATRIX_V[1].xyz;
                float2 c = v.corner;
                float2 r = float2(c.x * cos(spin) - c.y * sin(spin), c.x * sin(spin) + c.y * cos(spin));
                r.y *= lerp(0.35, 1.0, abs(sin(spin * 0.7)));
                p += (right * r.x + up * r.y) * size;
                o.pos = UnityWorldToClipPos(p);
                o.corner = v.corner;
                o.kind = v.kind;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // A flake: a lumpy blob with a darker rim; an ember: a hot round core.
                float r = length(i.corner);
                float lump = 0.82 + 0.12 * sin(atan2(i.corner.y, i.corner.x) * 3.0 + i.kind.x * 9.0);
                float inside = 1.0 - smoothstep(lump - 0.12, lump, r);
                fixed3 c = lerp(_Ash.rgb, _Ash.rgb * 0.62, smoothstep(lump * 0.45, lump, r));
                c = lerp(c, lerp(_Ember.rgb, fixed3(1.0, 0.72, 0.30), 1.0 - smoothstep(0.0, 0.5, r)), i.kind.y);
                return fixed4(c, inside * i.fade * _Amount);
            }
            ENDCG
        }
    }
    FallBack Off
}
