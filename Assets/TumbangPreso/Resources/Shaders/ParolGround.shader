// ⚠️ WHAT RAGO'S LANTERN PUTS ON THE COURT (SUPERNOVA's cutscene, `HeroIntroductionScene.Sean.cs`), 2026-10-10.
// The owner, of a cut whose ground marks were flat tinted meshes: "sean's parol looks good, but the ground effects/magic
// circles are lacking proper shaders and effects", and of every hero: "do more with emmissives".
//
// One flat quad lying on the court, drawn here as LIGHT rather than as a painted shape. `_Mode` picks which mark:
//   0  THE LANDING MARK: where he comes down. A bright outer ring with light running round it and a soft bloom past its
//      edge, a ring of ticks that steps like a clock, an inner ring running the other way, and inside it the underside of
//      his parol as the lantern throws it down: a five-point star in two warm halves, capiz bands breathing out from its
//      heart, a brighter heart star, shell fans between the points, embers drifting across it. Each point of the star
//      ignites with a flare as `_Lit` passes it (0 to 5), and the whole mark burns hotter with `_Heat` as the leap nears.
//   1  A CAST: the lantern's light lying on the court. A soft warm pool with the star's shape thrown into it, turning with
//      the lantern (`_Turn`), and rings of bulb light that step with the chase. Small, it is the pool at the foot of a
//      shaft of light; court-wide, it is the lantern lighting the ground under it.
//   2  THE MARK UNDER A PLAYER INSIDE THE RING: a ring with light running round it and a small star beating in it.
//   3  A RING THROWN ALONG THE COURT (the leap): `_Lit` is how far it has gone, 0 to 1. A ragged front of fire with a
//      hot lip, a fading wake and sparks torn off behind it.
//   4  THE COIL'S FIRE ON THE GROUND: a pool of heat with arms of flame winding in toward his feet.
//
// ⚠️ NOTHING HERE READS `_Time`. The cutscene runs with the match paused, and it must also sample at any moment out of
// order (the film, the shot judge). Every movement is driven by `_Age`, which the scene sets from its own clock.
//
// Premultiplied: rgb is added as light, alpha only dims what is under it a little, so a mark tints the court and glows on
// it at once. Unlit, no depth write, pulled a hair toward the lens so it never fights the court it lies on.
Shader "TumbangPreso/ParolGround"
{
    Properties
    {
        _Gold ("Gold", Color) = (1, 0.80, 0.34, 1)
        _Orange ("Orange", Color) = (1, 0.48, 0.10, 1)
        _Red ("Red", Color) = (0.95, 0.20, 0.08, 1)
        _Cream ("Capiz cream", Color) = (1, 0.95, 0.80, 1)
        _Mode ("Which mark", Float) = 0
        _Age ("Scene clock (s)", Float) = 0
        _Presence ("Presence", Float) = 1
        _Lit ("Points lit / ring progress", Float) = 0
        _Heat ("Heat (the leap nearing)", Float) = 0
        _Turn ("Turn (radians)", Float) = 0
        _Glow ("Glow", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+12" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Gold, _Orange, _Red, _Cream;
            float _Mode, _Age, _Presence, _Lit, _Heat, _Turn, _Glow;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            #define TAU 6.2831853
            #define FIFTH 1.2566371

            float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash2(i), hash2(i + float2(1, 0)), f.x), lerp(hash2(i + float2(0, 1)), hash2(i + float2(1, 1)), f.x), f.y);
            }
            float band(float x, float at, float w) { return saturate(1.0 - abs(x - at) / w); }
            float soft(float x, float at, float w) { float d = (x - at) / w; return exp(-d * d); }

            // How far out a five-point star reaches along angle `a` (its first point along a = 0), as a share of its point.
            float starReach(float a)
            {
                float t = fmod(fmod(a, FIFTH) + FIFTH, FIFTH);
                float th = min(t, FIFTH - t);
                return 0.2245 / (0.2245 * cos(th) + 0.691 * sin(th));
            }
            // Which half of its point this angle is in: the two halves of a parol's point are a shade apart.
            float starHalf(float a)
            {
                float t = fmod(fmod(a, FIFTH) + FIFTH, FIFTH);
                return step(t, FIFTH * 0.5);
            }

            // Embers: small hot specks in cells that drift outward and wink. `p` in the mark's own space.
            float embers(float2 p, float age, float cells, float speed)
            {
                float r = length(p), a = atan2(p.y, p.x);
                float2 q = float2(a / TAU * cells, r * cells * 0.5 - age * speed);
                float2 id = floor(q), f = frac(q) - 0.5;
                float h = hash2(id);
                float2 off = float2(hash2(id + 3.1), hash2(id + 7.7)) - 0.5;
                float d = length(f - off * 0.6);
                float wink = saturate(sin(age * (3.0 + h * 5.0) + h * 40.0) * 0.5 + 0.5);
                return step(0.72, h) * saturate(1.0 - d / 0.13) * wink;
            }

            float4 landing(float2 p, float r, float a)
            {
                float heat = saturate(_Heat);
                float3 col = 0; float alpha = 0;
                float run = _Age * (1.6 + 2.4 * heat);

                // The outer ring: a hot line, light running round it in three long strokes, bloom past its edge.
                float stroke = pow(saturate(cos(a * 3.0 - run * 2.0) * 0.5 + 0.5), 3.0);
                float rim = band(r, 0.93, 0.035);
                col += _Gold.rgb * rim * (1.4 + 2.2 * stroke + 1.2 * heat);
                col += _Cream.rgb * band(r, 0.93, 0.012) * (0.6 + 1.6 * stroke);
                col += _Orange.rgb * soft(r, 0.95, 0.07) * (0.35 + 0.35 * heat);
                alpha += rim * 0.8;

                // The ticks: thirty-six, stepping half a tick six times a second, every other step dimmer. A clock.
                float beat = floor(_Age * 6.0);
                float ta = a / TAU * 36.0 + beat * 0.5;
                float tick = step(abs(frac(ta) - 0.5), 0.26) * step(0.80, r) * step(r, 0.865);
                float sweep = pow(saturate(cos(a - beat * TAU / 36.0 * 3.0) * 0.5 + 0.5), 6.0);
                col += _Gold.rgb * tick * (0.7 + 0.3 * fmod(beat, 2.0) + 1.6 * sweep + 0.6 * heat);
                alpha += tick * 0.6;

                // The inner ring, running the other way, thin.
                float stroke2 = pow(saturate(cos(a * 2.0 + run * 1.4) * 0.5 + 0.5), 4.0);
                col += lerp(_Orange.rgb, _Cream.rgb, stroke2) * band(r, 0.765, 0.014) * (0.9 + 1.4 * stroke2);

                // Inside: the lantern's underside thrown down. The star, turned so one point is toward the lens side (+y).
                float sa = a - TAU * 0.25 - _Turn;
                float reach = r / (0.72 * starReach(sa));
                float inStar = smoothstep(1.02, 0.97, reach);
                float halfTone = starHalf(sa);
                float3 paper = lerp(_Orange.rgb, _Gold.rgb, halfTone * 0.75);
                // Capiz bands breathing out from the heart: the lantern's light moving through its panels.
                float breath = sin(reach * 15.0 - _Age * (2.2 + 2.0 * heat)) * 0.5 + 0.5;
                float chevron = soft(frac(reach * 3.0 - _Age * 0.25), 0.5, 0.16);
                col += paper * inStar * (0.22 + 0.20 * breath + 0.18 * heat) + _Cream.rgb * inStar * chevron * 0.12;
                col += _Cream.rgb * band(reach, 1.0, 0.035) * step(r, 0.76) * (0.9 + 0.9 * heat);      // the star's edge
                col += _Cream.rgb * soft(fmod(fmod(sa, FIFTH) + FIFTH, FIFTH), FIFTH * 0.5, 0.035) * inStar * 0.35 * smoothstep(0.1, 0.5, reach); // its ridges
                alpha += inStar * 0.62;
                // The heart star, turned into the gaps, brighter, beating.
                float reachH = r / (0.30 * starReach(sa + FIFTH * 0.5));
                float inHeart = smoothstep(1.04, 0.94, reachH);
                float pulse = 0.75 + 0.25 * sin(_Age * (5.0 + 6.0 * heat));
                col += lerp(_Gold.rgb, _Cream.rgb, 0.3) * inHeart * (0.5 + 0.5 * heat) * pulse;
                col += _Cream.rgb * soft(r, 0.0, 0.08) * (0.3 + 0.4 * heat);
                // Between the points: shell fans, faint, and a second ring of small scallops just inside the inner ring.
                float gap = (1.0 - inStar) * step(r, 0.75);
                float fan = soft(frac(sa / FIFTH * 6.0), 0.5, 0.22) * smoothstep(0.30, 0.72, r);
                col += _Red.rgb * gap * (0.10 + 0.14 * fan);
                float scallop = abs(sin(a * 15.0)) * 0.035;
                col += _Gold.rgb * band(r, 0.70 - scallop, 0.012) * gap * 0.8;
                alpha += gap * 0.45;

                // The five points igniting: each a flare with four rays when `_Lit` passes it, then a steady bead of light.
                for (int k = 0; k < 5; k++)
                {
                    float on = saturate(_Lit - k);
                    float kick = on * exp(-3.2 * max(0.0, _Lit - k - 0.25));
                    float pa = TAU * 0.25 + _Turn + k * FIFTH;
                    float2 d = p - float2(cos(pa), sin(pa)) * 0.72;
                    float dist = length(d);
                    float2 e = float2(d.x * cos(pa) + d.y * sin(pa), -d.x * sin(pa) + d.y * cos(pa));
                    float rays = max(saturate(1.0 - abs(e.y) / 0.012) * saturate(1.0 - abs(e.x) / (0.10 + 0.22 * kick)),
                                     saturate(1.0 - abs(e.x) / 0.012) * saturate(1.0 - abs(e.y) / (0.07 + 0.16 * kick)));
                    col += _Cream.rgb * on * (soft(dist, 0.0, 0.035 + 0.05 * kick) * (1.5 + 2.5 * kick) + rays * (0.5 + 1.8 * kick));
                    col += _Orange.rgb * on * soft(dist, 0.0, 0.11 + 0.10 * kick) * (0.5 + kick);
                    col += _Orange.rgb * (1.0 - on) * band(dist, 0.035, 0.010) * 0.7;                 // unlit: an empty socket
                }

                // Heat on it: embers drifting out across it, and a shimmer that brightens toward the leap.
                col += lerp(_Gold.rgb, _Cream.rgb, 0.5) * embers(p, _Age, 22.0, 0.7 + heat) * step(r, 0.9) * (1.2 + 1.5 * heat);
                float shimmer = vnoise(p * 7.0 + float2(0.0, -_Age * 1.3)) * vnoise(p * 13.0 + float2(_Age * 0.7, 0.0));
                col += _Orange.rgb * shimmer * step(r, 0.9) * (0.06 + 0.25 * heat);
                // Everything a little hotter, and whiter, as he coils.
                col *= 1.0 + 0.2 * heat;
                return float4(col, saturate(alpha));
            }

            float4 castLight(float2 p, float r, float a)
            {
                float3 col = 0;
                float pool = pow(saturate(1.0 - r), 1.6);
                // The star's shape thrown down, soft, turning with the lantern.
                float sa = a - TAU * 0.25 - _Turn;
                float reach = r / (0.78 * starReach(sa));
                float inStar = smoothstep(1.15, 0.75, reach);
                col += _Orange.rgb * pool * 0.4 + lerp(_Orange.rgb, _Gold.rgb, 0.6) * inStar * pool * 0.7;
                // Its rings of bulbs as beads of light on the ground, every third one bright and the bright one stepping.
                float beat = floor(_Age * 10.0);
                for (int k = 0; k < 3; k++)
                {
                    float at = 0.30 + 0.24 * k, count = 10.0 + 6.0 * k, way = (k == 1) ? -1.0 : 1.0;
                    float ca = (a - _Turn) / TAU * count;
                    float bead = saturate(1.0 - length(float2((frac(ca) - 0.5) * (TAU * at / count), r - at)) / 0.035);
                    float bright = step(fmod(floor(ca) + way * beat + 300.0, 3.0), 0.5);
                    col += lerp(_Orange.rgb, _Cream.rgb, bright) * bead * (0.35 + 1.1 * bright);
                }
                // A thin lip where the light ends, and the flicker of a flame behind paper.
                col += _Gold.rgb * band(r, 0.90, 0.02) * 0.35;
                float flicker = 0.86 + 0.14 * sin(_Age * 9.0 + sin(_Age * 23.0)) * (0.5 + 0.5 * vnoise(p * 3.0 + _Age));
                col *= flicker * (1.0 + 0.8 * saturate(_Heat));
                return float4(col, pool * 0.22);
            }

            float4 under(float2 p, float r, float a)
            {
                float3 col = 0;
                float stroke = pow(saturate(cos(a * 2.0 + _Age * 5.0) * 0.5 + 0.5), 3.0);
                col += _Red.rgb * band(r, 0.90, 0.07) * (1.6 + 2.4 * stroke) + _Cream.rgb * band(r, 0.90, 0.02) * (0.4 + 1.6 * stroke);
                col += _Orange.rgb * soft(r, 0.92, 0.10) * 0.5;
                float sa = a - TAU * 0.25 - _Age * 1.2;
                float beat = 0.82 + 0.18 * sin(_Age * 9.0);
                float inStar = smoothstep(1.05, 0.93, r / (0.60 * beat * starReach(sa)));
                col += lerp(_Orange.rgb, _Gold.rgb, starHalf(sa)) * inStar * 1.3 + _Cream.rgb * soft(r, 0.0, 0.14) * 0.9;
                return float4(col, saturate(band(r, 0.90, 0.07) * 0.7 + inStar * 0.5));
            }

            float4 thrown(float2 p, float r, float a)
            {
                float go = saturate(_Lit);
                float front = lerp(0.12, 0.92, 1.0 - (1.0 - go) * (1.0 - go));
                float ragged = (vnoise(float2(a * 5.0, _Age * 5.0)) - 0.5) * 0.07;
                float lip = band(r, front + ragged, 0.028);
                float wake = smoothstep(front - 0.34, front, r) * step(r, front + ragged);
                float tongues = vnoise(float2(a * 9.0, r * 9.0 - _Age * 7.0));
                float fade = 1.0 - go;
                float3 col = _Cream.rgb * lip * 2.4 + _Gold.rgb * soft(r, front + ragged, 0.06) * 1.2 + lerp(_Red.rgb, _Orange.rgb, tongues) * wake * (0.35 + 0.9 * tongues);
                col += _Cream.rgb * embers(p, _Age, 16.0, 1.6) * step(r, front) * 1.6;
                return float4(col * fade, saturate(wake * 0.3 + lip) * fade);
            }

            float4 coil(float2 p, float r, float a)
            {
                float pool = pow(saturate(1.0 - r), 1.4);
                // Three arms of flame winding in toward his feet, each with a hot spine.
                float arm = sin(a * 3.0 + r * 9.0 - _Age * 7.0) * 0.5 + 0.5;
                float lick = vnoise(float2(a * 4.0 + _Age * 2.0, r * 7.0 - _Age * 3.0));
                float3 col = _Red.rgb * pool * 0.5 + _Orange.rgb * pool * pow(arm, 2.0) * (0.6 + 0.8 * lick) + _Cream.rgb * pool * pow(arm, 9.0) * 0.9;
                col += _Gold.rgb * band(r, 0.62 + 0.05 * sin(_Age * 11.0), 0.03) * 0.8;
                col += _Cream.rgb * embers(p, _Age, 14.0, -1.3) * 1.5 * step(r, 0.9);
                col *= 0.85 + 0.15 * sin(_Age * 13.0);
                return float4(col, pool * 0.4);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                if (r > 1.0) discard;
                float a = atan2(p.y, p.x);
                float4 c = _Mode < 0.5 ? landing(p, r, a) : _Mode < 1.5 ? castLight(p, r, a) : _Mode < 2.5 ? under(p, r, a) : _Mode < 3.5 ? thrown(p, r, a) : coil(p, r, a);
                float there = saturate(_Presence);
                // Never a hard edge at the quad's own rim.
                float edge = smoothstep(1.0, 0.96, r);
                return fixed4(c.rgb * _Glow * there * edge, c.a * there * edge);
            }
            ENDCG
        }
    }
    FallBack Off
}
