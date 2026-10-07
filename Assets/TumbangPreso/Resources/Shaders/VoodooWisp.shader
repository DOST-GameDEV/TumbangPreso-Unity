// ⚠️⚠️ PHAISTER'S SOUL DRAW (HERO-10 v3, 2026-09-29). The owner on the soul thread (films v12 to v16): *"dont make the pulling thing
// look like a physical line i want it to look like sucking aura or smth"*, *"it sucks rn ur implementation"*. So the reach is no
// cord at all: it is wisps of the victim's aura peeled off them and sucked into the doll she holds out (`VoodooSoulDraw`). This is
// one wisp, a particle of a `ParticleSystem` that `VoodooSoulDraw` lays out itself (stretched along its flight, so a fast one is a
// streak and a slow one a puff).
//
//  * The vertex colour is the curse's hue (DRAIN crimson, HEX violet); its alpha is the wisp's life.
//  * A HOT CORE over a DARK SMOKY RIM, alpha-blended rather than additive: light alone washes out on the sunny court, smoke alone is
//    dull; a dark edge with a bright middle reads on sand, on a wall and against the sky (the thread's rule, kept).
//  * The rim frays with a noise that flows backward along the wisp, so it looks torn off something rather than stamped.
//  * `_Near` thins a wisp to nothing within that distance of the lens, so none ever sits on a victim's or her own eye.
// Loaded through Resources (`Shaders/VoodooWisp`), which keeps it in the player.
Shader "TumbangPreso/VoodooWisp"
{
    Properties
    {
        _Near ("Thin within (m)", Float) = 0.55
        _Core ("Core brightness", Float) = 2.4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+12" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Near, _Core;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float near : TEXCOORD1; float seed : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 view = UnityObjectToViewPos(v.vertex);
                o.near = saturate((-view.z - 0.12) / max(0.01, _Near));
                o.color = v.color;
                o.uv = v.uv;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.seed = frac(sin(dot(floor(world * 3.0), float3(12.9898, 78.233, 37.719))) * 43758.5453);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = (i.uv - 0.5) * 2.0;
                // Flowing torn edge: the noise runs along the wisp's length, so the rim streams like smoke pulled away.
                float t = TumpShaderTime();
                float n = noise(float2(c.x * 2.3 - t * 3.1 + i.seed * 17.0, c.y * 2.3 + i.seed * 9.0)) * 0.6
                        + noise(float2(c.x * 5.0 - t * 5.3, c.y * 5.0 - i.seed * 4.0)) * 0.4;
                float d = length(c) + (n - 0.5) * 0.38;
                float body = saturate(1.0 - smoothstep(0.15, 1.0, d));
                float core = exp(-dot(c, c) * 7.0);
                // Holes open as the wisp dies (alpha low): it thins apart rather than fading as a flat shape.
                float life = i.color.a;
                float holes = smoothstep(1.0 - life * 1.15, 1.05 - life * 1.15 + 0.25, n);
                float a = body * holes * saturate(life * 1.6) * i.near;
                float3 rim = i.color.rgb * 0.22;
                float3 hot = i.color.rgb * _Core + core * 0.35;
                float3 rgb = lerp(rim, hot, saturate(core * 1.4 + body * 0.25));
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
