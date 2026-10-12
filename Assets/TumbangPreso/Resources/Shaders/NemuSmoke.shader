// NEMU'S DEAD AND THEIR AURA (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 11.3).
// The owner: *"use black and purple or low opacity black"*, smoky wraiths, *"hell i hate how the ghosts look"* (v1's lathed
// translucent bodies). Fade's NIGHTFALL is the reference: black smoke with the faces and claws drawn in light inside it.
//
// One shader, two modes, both particles laid out by hand (`Visual.NemuSmokeBatch`):
//  * `_Mode` 0, SMOKE: a soft round puff with a frayed, flowing rim. PREMULTIPLIED: its body darkens what is behind it (near
//    black, alpha = density) and its rim adds violet light, so one puff is both the black and the edge.
//    Vertex colour: r = how much the rim glows, g = fray (0 whole, 1 gone: holes open from the rim in), b = seed (each puff its
//    own rim), a = density.
//  * `_Mode` 1, LIGHT: eyes, mouths, claws, embers. Adds only (alpha 0), a soft core inside a falloff, never a hard dot.
//    Vertex colour is the light; `_Intensity` lets it go past 1.
//  * `_Clock` is set by whoever lays the particles out (the scene clock in the cutscene, the status clock in play), so the
//    smoke still flows with the world paused under the phase, and a film at 30 frames per game second matches.
//  * `_Near` (metres) thins anything to nothing as the lens comes within that distance, so no puff ever sits on a lens.
// Loaded through Resources (`Shaders/NemuSmoke`), which keeps it in the player.
Shader "TumbangPreso/NemuSmoke"
{
    Properties
    {
        _Mode ("Mode (0 smoke, 1 light, 2 ghost)", Float) = 0
        _Core ("Smoke body", Color) = (0.016, 0.004, 0.03, 1)
        _RimColor ("Smoke rim", Color) = (0.50, 0.17, 0.95, 1)
        _RimGain ("Rim gain", Float) = 0.75
        _Intensity ("Light intensity", Float) = 5.0
        _Clock ("Clock (s)", Float) = 0
        _Near ("Thin within (m)", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Mode, _RimGain, _Intensity, _Clock, _Near;
            fixed4 _Core, _RimColor;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float near : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 view = UnityObjectToViewPos(v.vertex);
                o.near = saturate((-view.z - 0.15) / max(0.01, _Near));
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 p) { return noise(p) * 0.55 + noise(p * 2.1 + 7.3) * 0.3 + noise(p * 4.3 - 3.1) * 0.15; }
            float capsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h) - r;
            }
            float smin(float a, float b, float k) { float h = saturate(0.5 + 0.5 * (b - a) / k); return lerp(b, a, h) - k * h * (1 - h); }

            // GHOST (`_Mode` 2, film r3 and the owner: *"these doesnnt read as ghosts anymroe"*): ONE ghost per quad, so the
            // silhouette is a ghost's before it is smoke. A hooded head, a body that flows down into a swaying, tattered tail,
            // two arms raised with hooked claws, the edge frayed by flowing smoke and lit in broken violet, and the face of the
            // dead: two slanted hollow eyes and a long screaming mouth, glowing. The quad is 0.8 as wide as it is tall (stretched
            // longer in flight, which stretches the ghost with it).
            // Vertex colour: r = the face (0 seen from behind), g = arms (0 low and out, 1 raised clawing), b = seed, a = density.
            fixed4 ghost(float2 c, fixed4 col, float near)
            {
                float seed = col.b * 31.0;
                float raise = col.g;
                float2 q = float2(c.x * 0.8, c.y);
                float below = saturate(0.35 - q.y);
                float sway = sin(q.y * 5.0 + _Clock * 3.2 + seed) * 0.11 * below;
                // Head and hood.
                float d = length(q - float2(0.0, 0.55)) - 0.27;
                // The body: shoulders, a wider middle, then narrowing into a tail that tatters.
                float t = saturate((0.5 - q.y) / 1.5);
                float tatter = 0.7 + 0.6 * noise(float2(q.x * 7.0 + seed, q.y * 3.0 - _Clock * 1.5));
                float w = lerp(0.30, 0.37, saturate(t * 3.0)) * (1.0 - smoothstep(0.32, 1.0, t)) * lerp(1.0, tatter, smoothstep(0.45, 0.8, t)) + 0.015;
                float body = max(abs(q.x - sway) - w, q.y - 0.46);
                d = smin(d, body, 0.12);
                // Arms and claws.
                for (int s = -1; s <= 1; s += 2)
                {
                    float2 sh = float2(s * 0.24, 0.30);
                    float2 hand = float2(s * lerp(0.62, 0.46, raise), lerp(0.02, 0.86, raise)) + float2(0.0, 0.03 * sin(_Clock * 4.0 + seed + s));
                    d = smin(d, capsule(q, sh, hand, 0.06), 0.05);
                    float2 dir = normalize(hand - sh);
                    float2 side = float2(dir.y, -dir.x);
                    for (int k = -1; k <= 1; k++)
                    {
                        float2 knuckle = hand + normalize(dir + side * k * 0.55) * 0.08;
                        float2 tip = knuckle + normalize(dir + side * k * 0.25 - side * s * 0.5) * 0.09;
                        d = min(d, capsule(q, hand, knuckle, 0.024));
                        d = min(d, capsule(q, knuckle, tip, 0.012));
                    }
                }
                // The edge frays like smoke torn off it.
                float n = fbm(q * 3.4 + float2(seed, -_Clock * 1.3));
                d += (n - 0.5) * 0.13;
                float density = col.a;
                float fill = smoothstep(0.03, -0.03, d);
                float a = fill * density * near;
                float band = smoothstep(-0.075, -0.015, d) * (1.0 - smoothstep(-0.015, 0.02, d)) * smoothstep(0.42, 0.68, n);
                float rim = band * density * near;
                // The face: hollow slanted eyes (outer corners up) and a long open mouth, burning.
                float face = 0.0;
                for (int e = -1; e <= 1; e += 2)
                {
                    float2 p = q - float2(e * 0.11, 0.585);
                    float ang = e * 0.38;
                    float2 r = float2(p.x * cos(ang) + p.y * sin(ang), -p.x * sin(ang) + p.y * cos(ang));
                    face += smoothstep(1.0, 0.25, length(r / float2(0.085, 0.032)));
                }
                float mouthOpen = 0.85 + 0.15 * sin(_Clock * 7.0 + seed);
                face += 0.85 * smoothstep(1.0, 0.25, length((q - float2(0.0, 0.395)) / float2(0.045, 0.095 * mouthOpen)));
                face *= col.r * near * fill;
                float3 rgb = _Core.rgb * a + _RimColor.rgb * rim * _RimGain * 1.4 + _RimColor.rgb * face * 2.6 + float3(0.35, 0.2, 0.45) * face * face;
                return fixed4(rgb, a);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = (i.uv - 0.5) * 2.0;
                if (_Mode > 1.5) return ghost(c, i.color, i.near);
                if (_Mode > 0.5)
                {
                    // LIGHT: a hot core in a soft falloff. Additive through the premultiplied blend (alpha 0).
                    float r2 = dot(c, c);
                    // A hot core in a wide soft falloff (film r3: a flatter profile read as dull lilac discs).
                    // (film r7: a tight core was a few pixels at play distance; the glow fills the quad now.)
                    float glow = exp(-r2 * 4.0) * 1.2 + exp(-r2 * 1.4) * 0.35;
                    glow *= saturate(1.0 - r2);
                    return fixed4(i.color.rgb * glow * _Intensity * i.near, 0);
                }
                // SMOKE.
                float seed = i.color.b * 31.0;
                float rimAmount = i.color.r;
                float fray = i.color.g;
                float density = i.color.a;
                // The noise flows up the puff and turns slowly, so it reads as smoke boiling rather than a stamped disc.
                float2 q = c * 1.6 + float2(seed * 1.7, seed * 2.3 - _Clock * 0.9);
                float n = fbm(q);
                float n2 = fbm(q * 1.8 + float2(-_Clock * 0.4, 3.0));
                float d = length(c) + (n - 0.5) * 0.8;
                float body = 1.0 - smoothstep(0.42, 0.98, d);
                // Holes open from the rim inward as it dies.
                float holes = 1.0 - smoothstep(1.05 - fray * 1.2, 1.2 - fray * 1.2, n2 + d * 0.3);
                float a = saturate(body * density * holes) * i.near;
                // The violet edge: a THIN line just inside the frayed edge, and a few threads (film r1: a wide band and strong
                // threads summed over many puffs into a lavender cloud, and each puff read as a violet ring).
                float band = smoothstep(0.66, 0.84, d) * (1.0 - smoothstep(0.84, 0.97, d));
                float threads = smoothstep(0.78, 0.95, n2) * body;
                // Broken, never a closed ring (film r2: a whole violet outline round every puff read as bubbles): the edge only
                // lights where the noise runs high, so it reads as embers along torn smoke.
                band *= smoothstep(0.48, 0.72, n);
                float rim = (band + threads * 0.2) * rimAmount * holes * i.near * saturate(density * 1.4);
                float3 rgb = _Core.rgb * a + _RimColor.rgb * rim * _RimGain;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
