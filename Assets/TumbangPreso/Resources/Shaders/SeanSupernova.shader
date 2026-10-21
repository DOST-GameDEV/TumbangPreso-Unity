// SEAN'S SUPERNOVA, the live landing (`SeanSupernovaLook`). One mesh, one draw, driven by `_Age` alone.
// Painted like `SeanFireThrow` at `_Stylise` 2: three crisp bands (white-yellow, orange, deep red), see-through,
// a procedural picture panned at two speeds and multiplied by itself so the inside churns. Pieces die by eroding
// in place: the threshold rises, nothing rotates, swings or slides.
Shader "TumbangPreso/SeanSupernova"
{
    Properties
    {
        _Age ("Clock (s, set per frame)", Float) = 0
        _Life ("Crater life (s)", Float) = 5
        _Gain ("Brightness", Range(0,4)) = 1.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Age, _Life, _Gain;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x), lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
            }
            // Rounder blobs than plain value noise: two octaves, softly squared.
            float blobs(float2 p) { float n = noise(p) * .65 + noise(p * 2.1 + 7.3) * .35; return smoothstep(.15, .85, n); }

            v2f vert(appdata v)
            {
                float part = v.color.r, seed = v.color.g;
                float3 p = v.vertex.xyz;
                if (part < .25)
                {
                    // Dome: swells out fast, eased, then keeps creeping as it is eaten.
                    float s = 1 - pow(1 - saturate(_Age / .32), 3);
                    p *= .15 + .90 * s + .08 * saturate((_Age - .32) / .5);
                }
                else if (part < .75)
                {
                    // Ring: runs out to the radius in a quarter second and stays there.
                    float s = 1 - pow(1 - saturate(_Age / .26), 2);
                    p.xz *= lerp(.2, 1, s);
                }
                else
                {
                    // Tongue: rises after the blast, each breathing at its own rate. Only the top moves.
                    float born = .18 + seed * .25;
                    float grow = saturate((_Age - born) / .3);
                    float breathe = 1 + .22 * sin(_Age * (5 + seed * 4) + seed * 40) + .12 * sin(_Age * (11 + seed * 7));
                    p.y *= grow * lerp(1, breathe, v.uv.y);
                }
                v2f o;
                o.pos = UnityObjectToClipPos(float4(p, 1));
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float part = i.color.r, seed = i.color.g;
                float2 uv = i.uv;
                float heat, eat;

                if (part < .25)
                {
                    // Dome: bright at the base, eaten from the top down over .7 s.
                    float2 q = float2(uv.x * 10, uv.y * 3);
                    float a = blobs(q + float2(0, -_Age * 2.2));
                    float b = blobs(q * 1.7 + float2(_Age * .7, -_Age * 3.6) + 3);
                    heat = a * b * 2.2 * (1.15 - uv.y);
                    eat = saturate((_Age - .12) / .6) * 1.1 + uv.y * .35 * saturate(_Age / .3);
                }
                else if (part < .75)
                {
                    // Ring: a scalloped band, brightest mid-band, eroding over a second.
                    float a = blobs(float2(uv.x * 4, uv.y * 2 - _Age * 1.5));
                    float b = blobs(float2(uv.x * 7 + _Age * .9, uv.y * 3 + 5));
                    float edge = smoothstep(0, .6, uv.y) * smoothstep(1, .7, uv.y);
                    heat = a * b * 2.4 * edge;
                    eat = saturate((_Age - .35) / .7);
                }
                else
                {
                    // Tongue: a rounded flame silhouette with the picture rising through it.
                    float x = uv.x * 2 - 1;
                    float width = (1 - uv.y) * .9 + .1;
                    float shape = saturate(1 - abs(x) / width);
                    shape = smoothstep(0, .5, shape) * smoothstep(1, .55, uv.y);
                    float2 q = float2(uv.x * 1.6 + seed * 9, uv.y * 2.2);
                    float a = blobs(q + float2(0, -_Age * 2.4));
                    float b = blobs(q * 1.9 + float2(seed * 3, -_Age * 4.1));
                    heat = shape * (a * b * 2.0 + .25) * (1.2 - uv.y * .6);
                    // Dies in place, each tongue a little apart from the next.
                    float endAt = _Life - .9 + seed * .6;
                    eat = saturate((_Age - endAt) / .7);
                }

                heat -= eat;
                // Three crisp bands. Below the lowest the piece is not there.
                if (heat < .12) discard;
                float3 deep = float3(.62, .08, .04), mid = float3(1, .45, .08), hot = float3(1, .92, .55);
                float3 col = heat > .55 ? hot : (heat > .30 ? mid : deep);
                float alpha = heat > .30 ? .92 : .62;
                // Half light, half cover, so it stays see-through and still reads in daylight.
                return fixed4(col * _Gain * alpha, alpha * .5);
            }
            ENDCG
        }
    }
}
