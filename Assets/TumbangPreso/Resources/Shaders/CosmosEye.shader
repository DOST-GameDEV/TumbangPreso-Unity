// ⚠️⚠️ OMEN'S EYE: A WINDOW INTO THE COSMOS (HERO-10, owner, 2026-09-27, of the first eye, a black sphere in a spiky ring:
// *"needs serious refinement"*, then *"make it look bigger and make the actual blackwhole prettier like ur peeking into the
// cosmos or smth"*).
//
// A camera-facing disc (billboarded in the vertex shader, as `SpiritGlow` does) that is a HOLE, not a ball: through it you see
// deep space, procedural and unlit:
//  * a far NEBULA of her colours (black-violet ground, violet and magenta clouds, lilac wisps), fbm value noise;
//  * two STAR layers, a far fine one and a near brighter one;
//  * everything turning CLOCKWISE and spiralling IN toward the middle (her direction rule: all her turning is clockwise), the
//    inner part turning faster, as a vortex does;
//  * PARALLAX: each layer shifts by the camera's direction to the eye at its own rate (the far nebula least, the near stars
//    most), so walking round it you see depth behind the disc instead of a picture on it;
//  * the middle falls away into a deeper dark with one faint far light at its heart;
//  * the EVENT HORIZON: a thin hot magenta rim at the disc's edge and a soft violet glow outside it that fades to nothing.
//  * `_Glitch` 0..1, WHILE IT FORMS (owner: *"i envision it as the blackhole looking glitchy and unstable as fuck when forming
//    (make it pulsate?) it starts out small and gradually gets bigger"*): horizontal slices jump sideways on a fast stepped
//    clock, the rim goes ragged in uneven bands, the colours split (magenta one way, violet the other) and it drops out for a
//    frame now and then. The C# drives it from 1 while forming down to 0 once it is open; the pulsing and the growth are the
//    disc's scale.
// Never white (her rule): the brightest star is pale lilac.
// Loaded through Resources (`Shaders/CosmosEye`), which keeps it in the player.
Shader "TumbangPreso/CosmosEye"
{
    Properties
    {
        _Open ("Open (0 shut, 1 open)", Float) = 1
        _Spin ("Spin, turns a second at the rim", Float) = 0.08
        _Glow ("Outer glow width (of radius)", Float) = 0.28
        _Time0 ("Clock (s)", Float) = 0
        _Glitch ("Glitch while forming", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Open, _Spin, _Glow, _Time0, _Glitch;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 look : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 centre = mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1)).xyz;
                float sx = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                float sy = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));
                float3 view = centre + float3(v.vertex.x * sx, v.vertex.y * sy, 0.0);
                view.z += 0.05;
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
                o.uv = v.uv;
                // The camera's direction to the eye, as yaw and height: the parallax input.
                float3 at = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 toCam = normalize(_WorldSpaceCameraPos - at);
                o.look = float2(atan2(toCam.x, toCam.z), toCam.y);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float a = 0.5, s = 0.0;
                for (int k = 0; k < 5; k++) { s += a * noise(p); p = p * 2.03 + float2(1.7, 9.2); a *= 0.5; }
                return s;
            }

            float2 rotate(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }

            // One star layer: a star in some cells of a grid, a soft point with a hot centre, twinkling.
            float stars(float2 p, float density, float t)
            {
                float2 cell = floor(p), f = frac(p) - 0.5;
                float h = hash(cell);
                if (h > density) return 0.0;
                float2 offset = (float2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5) * 0.7;
                float d = length(f - offset);
                float twinkle = 0.65 + 0.35 * sin(t * (2.0 + h * 5.0) + h * 40.0);
                return (smoothstep(0.12, 0.0, d) * 0.6 + smoothstep(0.035, 0.0, d)) * twinkle;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;           // -1..1 across the disc
                float t = _Time0;
                float g = saturate(_Glitch);
                // FORMING: slices jump sideways on a stepped clock (18 steps a second), the colours split by the jump.
                float tick = floor(t * 18.0);
                float band = floor(i.uv.y * 16.0);
                float jump = (hash(float2(band, tick)) - 0.5) * 0.22 * g * step(0.55, hash(float2(tick, band * 1.7)));
                p.x += jump;
                float r = length(p);
                float open = saturate(_Open);
                // The rim goes ragged in uneven angular bands while it forms.
                float sector = floor((atan2(p.y, p.x) + 3.14159) * 5.0);
                float ragged = (hash(float2(sector, floor(t * 12.0))) - 0.5) * 0.30 * g;
                float edge = max(0.001, open * (1.0 + ragged));   // the hole's radius in the disc

                // Outside the hole: only the soft violet glow and nothing past it.
                float outside = r - edge;
                if (outside > _Glow) discard;

                // The swirl: clockwise (negative angle), faster toward the middle, drawn inward over time.
                float rr = r / edge;
                float swirl = -(_Spin * 6.2831853 * t) * (1.0 + 2.5 * (1.0 - rr)) - 2.2 * (1.0 - rr) * (1.0 - rr);
                float2 q = rotate(p / edge, swirl);
                float inward = frac(t * 0.05);

                // Parallax: the far nebula barely shifts with the camera, the near stars shift most.
                float2 far = q * 1.6 + i.look * float2(0.25, 0.35);
                float2 mid = q * 2.6 + i.look * float2(0.55, 0.8);
                float2 near = q * 4.0 + i.look * float2(1.1, 1.5);

                // The nebula, in her colours.
                float n = fbm(far * 1.4 + float2(0.0, inward * 2.0));
                float m = fbm(mid * 1.1 + float2(5.2, 1.3));
                float3 ground = float3(0.035, 0.008, 0.065);
                float3 violet = float3(0.36, 0.12, 0.62);
                float3 magenta = float3(0.80, 0.10, 0.46);
                float3 lilac = float3(0.72, 0.56, 0.95);
                float3 col = ground;
                col = lerp(col, violet, smoothstep(0.42, 0.78, n) * 0.85);
                col = lerp(col, magenta, smoothstep(0.55, 0.85, m) * 0.6 * (0.4 + 0.6 * rr));
                col += lilac * pow(saturate(n * m * 1.6 - 0.35), 3.0) * 0.8;

                // Stars: far and fine, near and brighter.
                col += lilac * stars(far * 9.0, 0.30, t) * 0.7;
                col += lilac * stars(near * 5.0, 0.18, t * 1.3) * 1.1;

                // The middle falls away into a deeper dark, with one faint far light at its heart.
                float depth = smoothstep(0.0, 0.55, rr);
                col *= 0.25 + 0.75 * depth;
                col += magenta * smoothstep(0.12, 0.0, rr) * 0.35 + lilac * smoothstep(0.04, 0.0, rr) * 0.5;

                // The event horizon: a thin hot rim and a soft glow outside it.
                float rim = smoothstep(0.10, 0.0, abs(outside)) ;
                col = lerp(col, magenta * 1.4 + lilac * 0.25, rim * 0.9);
                float alpha = 1.0;
                if (outside > 0.0)
                {
                    float halo = 1.0 - saturate(outside / _Glow);
                    col = lerp(violet, magenta, halo) * (0.6 + 0.8 * halo);
                    alpha = halo * halo * 0.85;
                }
                // The colour split while forming: a jump to the right runs magenta, to the left violet.
                col += (jump > 0.0 ? magenta : violet) * abs(jump) * 4.0 * g;
                // And now and then it drops out for a frame.
                float dropout = 1.0 - g * step(0.86, hash(float2(floor(t * 24.0), 3.3)));
                return fixed4(col, alpha * saturate(open * 4.0) * dropout);
            }
            ENDCG
        }
    }
    FallBack Off
}
