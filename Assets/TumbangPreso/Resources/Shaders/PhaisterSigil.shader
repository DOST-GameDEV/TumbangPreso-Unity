// PHAISTER'S HEX SIGIL (`PhaisterHexSigil`, CURSE: HEX rebuilt on the owner's reference, 2026-10-30).
//
// Owner, of the button-and-moth mark: "no this genuinely needs some rework. the execution of the effects are horrible",
// then of the first sigil (sig4): "it looks so dull and un-magic like". His references: ref5 (a sigil burning over the
// cursed player's head, inky smoke, smoky ring sweeps round the body), ref1 and ref3 (circles that read as EMITTED
// LIGHT: a near-white core line inside a saturated violet glow inside a wide soft halo). The sigil is HER OWN, painted by
// `tools/paint_phaister_sigil.py` (a crescent cupping an eye on a stitched spine, moth antennae; a fictional lunar
// grammar). Its picture carries R the strokes, G the order they are drawn in (so it WRITES ITSELF on the GPU), B the
// eye glyph.
//
// ⚠️ LIGHT WITHOUT BLOOM IS DRAWN IN THREE LAYERS, as the references are built: the stroke itself near-white, the
// stroke read from a small blurred mip in saturated magenta-violet, and from a much smaller mip as a wide lilac halo. All
// three add light; a dark smoky purple behind them gives that light something to be bright against on a sunlit court.
// The lines shimmer, embers glitter along them, motes orbit and rise (mode 4), and every big moment has a flash.
//
// ⚠️ THE LAYOUT IS THE OWNER'S (2026-10-31): "maybe put the magic circle on the ground and the sigil on the torso". The
// circle lies under their feet and carries the fuse; the sigil sits on the chest with NO disc behind it, only an inky
// shadow tracing its own strokes; the smoke rising from the circle is the approved cloud (`FxCloud`), called by `PhaisterHexSigil`.
//
//   _Mode 0  the sigil on the chest, facing whoever looks, with its embers
//   _Mode 1  the rune circle on the ground: a dark pool, spinning runes, the fuse round its rim, the arming shockwave
//   _Mode 2  the stab's burst on the ground: rays, a shockwave ring and an eye that bursts open with light
//   _Mode 3  the small eye over a HEXED player, facing whoever looks
//   _Mode 4  motes: a static mesh of small cards, each orbiting and rising on its own seed (`_Burst` flings them out)
//
// Premultiplied: smoke covers, light adds. Every mesh is static and placed by the vertex program. No `_Time`.
Shader "TumbangPreso/PhaisterSigil"
{
    Properties
    {
        _SigilTex ("Sigil (R strokes, G draw order, B eye glyph)", 2D) = "black" {}
        _RuneTex ("Rune ring", 2D) = "black" {}
        _CloudTex ("Erosion (erode_cloud)", 2D) = "gray" {}
        _SmokeTex ("Smoke tongues (trail_smoke)", 2D) = "black" {}
        _RayTex ("Rays (erode_radial)", 2D) = "black" {}
        _BurnTex ("The sigil as approved (d4, premultiplied)", 2D) = "black" {}
        _OrderTex ("Which stroke each pixel of it belongs to, in writing order", 2D) = "black" {}
        _EyeTex ("The eye as approved (d4, premultiplied)", 2D) = "black" {}
        _RingTex ("The blast as approved (d4, premultiplied)", 2D) = "black" {}
        _Pull ("How far toward the eye its depth is written (m)", Float) = 0
        _Mode ("0 sigil, 1 rune circle, 2 ground burst, 3 hexed eye, 4 motes", Float) = 0
        _Lift ("Sigil: how far out from the body's axis toward the eye (m)", Float) = 0.3
        _Centre ("Centre (world)", Vector) = (0,2,0,0)
        _Size ("Half size (m)", Float) = 0.32
        _Age ("Clock (s)", Float) = 0
        _Draw ("How much of the sigil is drawn, in stroke order", Range(0,1)) = 1
        _Bright ("0 dim and unfinished, 1 armed", Range(0,2)) = 0
        _Fuse ("Fuse ring, 0 to 1", Range(0,1)) = 0
        _Smoke ("Smoke round it", Range(0,1)) = 1
        _Crack ("Cracking apart, 0 whole to 1 gone", Range(0,1)) = 0
        _Fade ("Fade (by erosion), 0 whole to 1 gone", Range(0,1)) = 0
        _Burst ("Burst, 0 to 1", Range(0,1)) = 0
        _Since ("Seconds since it armed (below 0 before)", Float) = -1
        _EyeOn ("Burst: the eye opens in it", Range(0,1)) = 1
        _FogOn ("Circle: soft ground fog round its rim", Range(0,1)) = 0
        _Core ("White-hot", Color) = (1.00,0.95,1.00,1)
        _Magenta ("Inner glow", Color) = (0.92,0.22,0.95,1)
        _Violet ("Violet", Color) = (0.55,0.14,0.98,1)
        _Lilac ("Halo", Color) = (0.80,0.50,1.00,1)
        _Dark ("Smoke", Color) = (0.24,0.05,0.38,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _SigilTex, _RuneTex, _CloudTex, _SmokeTex, _RayTex, _BurnTex, _OrderTex, _EyeTex, _RingTex;
            float _Mode, _Size, _Age, _Draw, _Bright, _Fuse, _Smoke, _Crack, _Fade, _Burst, _Since, _Lift, _EyeOn, _FogOn, _Pull;
            float4 _Centre;
            fixed4 _Core, _Magenta, _Violet, _Lilac, _Dark;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 seed:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 q:TEXCOORD0; float4 mote:TEXCOORD1; };

            float G(float x) { return pow(saturate(x), 0.4545); }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Turn(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }

            v2f vert(appdata v)
            {
                v2f o;
                o.mote = 0;
                float3 world;
                if (_Mode > 3.5 && _Mode < 4.5)
                {
                    // A MOTE: orbits the sigil on its own seed and rises, wrapping; `_Burst` flings it outward.
                    float4 s = v.seed;
                    float rise = frac(s.y + _Age * (0.18 + 0.22 * s.z));
                    float a = s.x * 6.2832 + _Age * (0.6 + 1.2 * s.w) * (s.z > 0.5 ? 1.0 : -1.0);
                    float rad = _Size * (0.55 + 0.9 * s.z) * (1.0 + 0.25 * sin(_Age * 2.0 + s.x * 9.0));
                    float3 orbit = float3(cos(a) * rad, (rise - 0.35) * _Size * 2.6, sin(a) * rad);
                    float3 fling = normalize(orbit + float3(0, 0.2, 0)) * _Burst * _Size * (2.5 + 2.0 * s.w);
                    float life = sin(rise * 3.14159);
                    float size = _Size * (0.04 + 0.06 * s.w) * life * (1.0 + 2.0 * _Burst * (1.0 - _Burst));
                    world = _Centre.xyz + orbit + fling + (UNITY_MATRIX_V[0].xyz * v.vertex.x + UNITY_MATRIX_V[1].xyz * v.vertex.z) * size;
                    o.q = v.vertex.xz;
                    o.mote = float4(life, s.w, s.x, 0);
                }
                else
                {
                    float2 q = v.vertex.xz * (_Mode > 0.5 && _Mode < 1.5 ? 1.5 : 1.0);
                    if (_Mode > 0.5 && _Mode < 2.5)
                    {
                        world = _Centre.xyz + float3(q.x, 0, q.y) * _Size;
                        if (_Mode > 1.5)
                        {
                            // The eye on the ground lies across the view and opens up against the foreshortening, so from a low
                            // court camera it is an eye and not a leaf pointing at the lens. The ring does not care.
                            float3 to = _WorldSpaceCameraPos - _Centre.xyz;
                            float flat_ = length(to.xz) + 1e-3;
                            float2 across = float2(UNITY_MATRIX_V[0].x, UNITY_MATRIX_V[0].z);
                            across /= max(length(across), 1e-3);
                            float2 along = float2(-across.y, across.x);
                            float sinE = to.y / length(to);
                            o.mote = float4(dot(q, across), dot(q, along) * max(lerp(1.0, saturate(sinE) + 0.05, 0.9), 0.45), 0, 0);
                        }
                    }
                    else world = _Centre.xyz + (UNITY_MATRIX_V[0].xyz * q.x + UNITY_MATRIX_V[1].xyz * q.y) * _Size;
                    o.q = q;
                }
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                if (_Pull > 0.0)
                {
                    // ⚠️ IN FRONT OF THE BODY WITHOUT LEAVING IT, AND WITHOUT SHOWING THROUGH WALLS. The picture stays where it is on
                    // screen; only its DEPTH is written as if it stood `_Pull` metres nearer the eye, enough to clear the body it
                    // sits on. Depth testing stays on, so a wall between the eye and the victim still hides it. Never past the lens.
                    float3 toEye = _WorldSpaceCameraPos - world;
                    float dist = length(toEye);
                    float4 nearer = mul(UNITY_MATRIX_VP, float4(world + toEye / max(dist, 1e-3) * min(_Pull, max(dist - 0.2, 0.0)), 1.0));
                    o.pos.z = nearer.z / nearer.w * o.pos.w;
                }
                return o;
            }

            float Inside(float2 uv) { return step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0); }
            // The strokes at uv, cut to what has been drawn; `blur` reads a smaller mip for the light round them. At a
            // blurred mip the mean order of nearby strokes is G over R, so the glow follows the pen.
            float Strokes(float2 uv, float blur)
            {
                float4 s = tex2Dlod(_SigilTex, float4(uv, 0, blur));
                float order = blur > 0.5 ? s.g / max(s.r, 0.02) : s.g;
                return s.r * step(order, _Draw + (blur > 0.5 ? 0.03 : 0.004)) * Inside(uv);
            }
            float Clockwise(float2 q) { return frac(0.25 - atan2(q.y, q.x) / 6.2832); }

            // Three layers of emitted light from a mask's line, its near glow and its halo.
            fixed3 Burn(float line_, float near, float far, float hot)
            {
                // (sig7, day floor: a pale lilac halo over the dark smoke turned the whole disc grey. The halo is saturated
                // violet now and weaker, so the dark stays dark and the light stays coloured.)
                return _Core.rgb * line_ * (0.8 + 0.35 * hot) + _Magenta.rgb * near * (0.6 + 0.7 * hot) + _Violet.rgb * far * (0.18 + 0.30 * hot);
            }

            // A grid bent by the cloud, so anything cracked along it breaks along irregular lines, never a straight grid.
            float2 Warp(float2 p, float scale)
            {
                float2 n = float2(tex2Dlod(_CloudTex, float4(p * scale, 0, 0)).r, tex2Dlod(_CloudTex, float4(p * scale + 0.37, 0, 0)).r);
                return p + (n - 0.25) * 1.3;
            }

            float2 Drift(float2 cell)
            {
                float2 home = (cell + 0.5 - 7.0) / 2.6;
                return (normalize(home + 1e-3) * 0.45 + (float2(Hash(cell), Hash(cell + 3.1)) - 0.5) * 0.3) * _Crack;
            }

            // THE SIGIL IS THE APPROVED PICTURE (d4, `tools/paint_phaister_sigil_burn.py`), not redrawn: the owner accepted its
            // weight and its broken burn. It is revealed in writing order, dimmer while unfinished, and heat runs along it.
            void SigilArt(float2 uv, out float4 art, out float order)
            {
                float inside = Inside(uv);
                art = tex2Dlod(_BurnTex, float4(uv, 0, 0)) * inside;
                order = tex2Dlod(_OrderTex, float4(uv, 0, 0)).r;
            }

            fixed4 Sigil(float2 q)
            {
                float2 uv = q * 0.5 + 0.5;
                float4 art = 0; float order = 0, seam = 0;
                if (_Crack > 0.001)
                {
                    float2 here = floor(Warp(q, 1.1) * 2.6 + 7.0);
                    for (int y = -3; y <= 3; y++)
                        for (int x = -3; x <= 3; x++)
                        {
                            float2 cell = here + float2(x, y);
                            float2 home = (cell + 0.5 - 7.0) / 2.6;
                            float2 src = home + Turn(q - Drift(cell) - home, -(Hash(cell + 9.2) - 0.5) * _Crack);
                            float2 w = Warp(src, 1.1) * 2.6 + 7.0;
                            if (any(floor(w) != cell)) continue;
                            float4 piece; float o; SigilArt(src * 0.5 + 0.5, piece, o);
                            if (piece.a > art.a) art = piece;
                            float2 f = frac(w);
                            seam = max(seam, smoothstep(0.09, 0.0, min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y))) * piece.a);
                        }
                }
                else SigilArt(uv, art, order);
                float hot = saturate(_Bright);
                float written = _Crack > 0.001 ? 1.0 : smoothstep(_Draw + 0.012, _Draw - 0.002, order);
                float cloud = G(tex2D(_CloudTex, q * 0.4 + float2(0.2, _Age * 0.07)).r);
                float keep = smoothstep(0.0, 0.15, cloud * 0.8 + 0.25 - max(_Fade, _Crack * 0.6) * 1.1) * written;
                // The pen, while it writes, and a pulse of heat along the strokes once it burns.
                float pen = art.a * smoothstep(_Draw - 0.05, _Draw, order) * step(_Draw, 0.995) * step(_Crack, 0.001);
                float run = pow(0.5 + 0.5 * sin(order * 30.0 - _Age * 8.0), 3.0) * step(_Crack, 0.001);
                float shimmer = 0.92 + 0.08 * sin(_Age * 21.0 + uv.y * 17.0) * sin(_Age * 13.0 + uv.x * 11.0);
                float heat = saturate(dot(art.rgb, float3(0.3, 0.6, 0.1)) * 1.6);
                fixed3 c = art.rgb * lerp(0.42, 1.08, hot) * shimmer + _Core.rgb * (pen * 0.9 + run * heat * 0.25 * hot) + _Magenta.rgb * seam * (1.0 - _Crack);
                float a = art.a * lerp(0.72, 1.0, hot);
                return fixed4(c * keep, a * keep);
            }

            // Three bands of the rune picture turn at their own rates, opposite ways, sliding at its empty gaps.
            void Runes(float2 q, out float line_, out float near, out float far)
            {
                float r = length(q);
                float spin = r > 0.77 ? _Age * 0.55 : (r > 0.45 ? -_Age * 0.95 : _Age * 1.5);
                float2 uv = Turn(q, spin) * 0.5 + 0.5;
                float inside = Inside(uv);
                line_ = tex2Dlod(_RuneTex, float4(uv, 0, 0.8)).r * inside;
                near = tex2Dlod(_RuneTex, float4(uv, 0, 3.0)).r * inside;
                far = tex2Dlod(_RuneTex, float4(uv, 0, 5.0)).r * inside;
            }

            fixed4 RuneCircle(float2 q)
            {
                float r = length(q), seam = 0, line_ = 0, near = 0, far = 0;
                float hot = saturate(_Bright);
                if (_Crack > 0.001)
                {
                    float2 here = floor(Warp(q, 0.6) * 1.6 + 7.0);
                    for (int y = -2; y <= 2; y++)
                        for (int x = -2; x <= 2; x++)
                        {
                            float2 cell = here + float2(x, y);
                            float2 home = (cell + 0.5 - 7.0) / 1.6;
                            float2 src = q - normalize(home + 1e-3) * 0.32 * _Crack;
                            float2 w = Warp(src, 0.6) * 1.6 + 7.0;
                            if (any(floor(w) != cell)) continue;
                            float l, n, f; Runes(src, l, n, f);
                            line_ = max(line_, l); near = max(near, n); far = max(far, f);
                            float2 g = frac(w);
                            seam = max(seam, smoothstep(0.05, 0.0, min(min(g.x, 1.0 - g.x), min(g.y, 1.0 - g.y))) * step(length(src), 1.0));
                        }
                }
                else Runes(q, line_, near, far);
                float at = Clockwise(q);
                float written = smoothstep(_Draw + 0.01, _Draw - 0.03, at);
                line_ *= written; near *= written; far *= written;
                float cloud = G(tex2D(_CloudTex, q * 0.45 + float2(_Age * 0.03, 0.4)).r);
                // Light that wanders round the circle: one number along the rim, and a slower wave across the bands.
                float wander = 0.5 + 0.7 * G(tex2D(_CloudTex, float2(at * 5.0 + _Age * 0.14, r * 0.5 - _Age * 0.2)).r);
                float wave = 0.72 + 0.28 * sin(at * 18.85 - _Age * 3.0 + r * 7.0);
                float keep = smoothstep(0.0, 0.14, cloud * 0.7 + (1.25 - r) * 0.3 + 0.1 - max(_Fade, _Crack * 0.45) * 1.05);
                float pool = smoothstep(1.34, 1.08, r + (cloud - 0.5) * 0.35) * 0.97 * smoothstep(0.0, 0.25, _Draw);
                fixed3 lineC = lerp(_Magenta.rgb * lerp(0.7, 1.3, hot), _Core.rgb, 0.34 * hot * wave);
                fixed3 light = lineC * line_ * lerp(0.75, 1.0, wave) + _Magenta.rgb * near * (0.35 + 0.8 * hot) * wander + _Violet.rgb * far * (0.3 + 0.5 * hot) * wander
                             + _Core.rgb * seam * (1.0 - _Crack) * 0.8;
                // Light seeps inward from the rim over the dark of the pool, broken by drifting cloud: depth, not a flat fill.
                float seep = exp(-max(1.05 - r, 0.0) / 0.20) * step(r, 1.05) * smoothstep(0.25, 0.8, cloud) * wander;
                light += _Violet.rgb * seep * pool * (0.35 + 0.55 * hot);
                // THE FUSE IS THE CIRCLE'S OUTERMOST BAND: two fine rails with a ladder of narrow rungs between them. Rungs and
                // rails light clockwise, a hot head leading; whole and beating once armed.
                float rails = max(smoothstep(0.016, 0.006, abs(r - 1.05)), smoothstep(0.016, 0.006, abs(r - 1.18)));
                float rung = smoothstep(0.30, 0.22, abs(frac(at * 72.0) - 0.5)) * step(1.05, r) * step(r, 1.18);
                float fill = max(smoothstep(_Fuse + 0.002, _Fuse - 0.006, at) * step(0.001, _Fuse), step(0.999, _Fuse));
                float headAt = smoothstep(0.045, 0.0, abs(at - _Fuse)) * step(_Fuse, 0.999) * step(0.001, _Fuse);
                float inBand = smoothstep(0.11, 0.05, abs(r - 1.115));
                float ladder = saturate(rung + rails) * step(0.001, _Draw);
                fixed3 ladderC = lerp(_Violet.rgb * 0.45, lerp(_Magenta.rgb * 1.25, _Core.rgb, 0.25 * hot), fill);
                light += ladderC * ladder * lerp(1.0, wander, fill) + _Core.rgb * headAt * inBand;
                float lightA = saturate(line_ + ladder * 0.8);
                // THE HALO HAS A FALLOFF AND IT WANDERS (g4: a flat pink blob). Beyond the rim it dies away fast and unevenly,
                // and it TINTS as well as adds, so on pale paving it stays magenta and never goes pastel.
                float beyond = exp(-max(r - 1.18, 0.0) / 0.10) * step(1.16, r);
                float haloA = beyond * wander * fill * (0.20 + 0.42 * hot);
                light *= keep; lightA *= keep; pool *= keep; haloA *= keep;
                float ko = saturate(_Since / 0.55);
                float shock = (_Since >= 0.0 && _Since < 0.55) ? smoothstep(0.14 * (1.0 - ko) + 0.02, 0.0, abs(r - lerp(1.18, 1.78, 1.0 - (1.0 - ko) * (1.0 - ko)))) * (1.0 - ko) : 0.0;
                light += (_Core.rgb * 0.7 + _Magenta.rgb) * shock + _Magenta.rgb * haloA;
                // SOFT GROUND FOG off the rim, drifting outward and thinning: the owner's "no visible balls" is what `FxCloud` cannot
                // do (it is solid balls by design), so this is painted with the same cloud picture the rest of the circle uses.
                float drift = G(tex2D(_CloudTex, float2(at * 6.0 + _Age * 0.03, r * 0.8 - _Age * 0.20)).r);
                float drift2 = G(tex2D(_CloudTex, float2(-at * 4.0, r * 1.1 - _Age * 0.13 + 0.4)).r);
                float fogA = smoothstep(1.14, 1.22, r) * exp(-max(r - 1.2, 0.0) / 0.22) * smoothstep(1.8, 1.55, r) * smoothstep(0.16, 0.60, drift * (0.55 + 0.8 * drift2)) * 0.95 * _FogOn;
                fixed3 fogC = lerp(_Dark.rgb, _Violet.rgb * 0.55, 0.45) * fogA;
                float coverA = saturate(pool + lightA * 0.5 + shock * 0.4 + haloA * 0.8);
                return fixed4((_Dark.rgb * pool * (1.0 - lightA) + light) + fogC * (1.0 - coverA), saturate(coverA + fogA * (1.0 - coverA)));
            }

            float S(float a, float b, float x) { return smoothstep(a, b, x); }
            float Cl(float u, float v) { return tex2Dlod(_CloudTex, float4(u, v, 0, 0)).r; }

            fixed4 Over(fixed4 top, fixed4 bot) { return fixed4(top.rgb + bot.rgb * (1.0 - top.a), top.a + bot.a * (1.0 - top.a)); }

            // THE BLAST IS THE APPROVED PICTURE TOO (d4, tears clumped so they stop reading as a comb), not a re-derivation of it: a
            // port of its formula came out as a ring of even dashes, which the owner has already rejected once. One tap, no seam.
            // It BLASTS out fast (the picture scaled from the centre, so its strands stretch as it goes), TEARS as it slows, its
            // cloud-shaped gaps opening from the outside in, and ERODES until only scraps of it are left.
            fixed4 Ring(float2 q, float k)
            {
                float grow = 1.0 - pow(1.0 - saturate(k * 2.4), 3.0);
                float scale = lerp(0.16, 1.0, grow);
                float2 p = Turn(q / scale, k * 0.12);
                float2 uv = p * 0.5 + 0.5;
                float4 art = tex2Dlod(_RingTex, float4(uv, 0, 0)) * Inside(uv);
                float die = S(0.32, 1.0, k);
                float cloud = G(Cl(p.x * 0.7 + 0.31, p.y * 0.7 + 0.17)) * 0.7 + G(Cl(p.x * 2.1 + 0.6, p.y * 2.1)) * 0.3;
                float keep = S(0.0, 0.12, cloud + 0.18 - die * 1.15);
                float brim = S(0.0, 0.1, cloud - die * 1.15 + 0.08) * (1.0 - keep);
                art *= keep;
                art.rgb += _Magenta.rgb * brim * art.a;
                return art;
            }

            fixed4 Burst(v2f i)
            {
                fixed4 c = Ring(i.q / 1.0, _Burst);
                float k = _Burst;
                if (_EyeOn > 0.5)
                {
                    // The eye opens within the first of the blast and then BURNS AWAY through the cloud map with the ring. It never
                    // squashes shut: a closing lens on the floor is the leaf this was asked to stop being.
                    float open = S(0.02, 0.16, k);
                    if (open > 0.02)
                    {
                        // Big enough to span most of the ring, and shifted toward the viewer so the body stands on its upper lid and
                        // not on its iris (owner: a big clear eye inside the ring).
                        float2 g = float2(i.mote.x, (i.mote.y + 0.1) / open) / 0.84;
                        float2 uv = g * 0.5 + 0.5;
                        float4 eye = tex2Dlod(_EyeTex, float4(uv, 0, 0)) * Inside(uv);
                        float burn = S(0.0, 0.2, G(Cl(g.x * 1.3 + 0.3, g.y * 1.3 + 0.6)) + 0.25 - S(0.55, 0.98, k) * 1.25);
                        c = Over(eye * burn, c);
                    }
                }
                return c;
            }

            fixed4 Mote(v2f i)
            {
                float d = length(i.q);
                float core = smoothstep(0.35, 0.0, d), glow = smoothstep(1.2, 0.0, d);
                float tw = 0.6 + 0.4 * sin(_Age * 11.0 + i.mote.z * 50.0);
                fixed3 c = lerp(_Magenta.rgb, _Core.rgb, step(0.6, i.mote.y)) * core * 1.2 + _Magenta.rgb * glow * glow * 0.6;
                return fixed4(c * tw * saturate(_Bright + 0.3) * (1.0 - _Fade), 0.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Mode < 0.5) return Sigil(i.q);
                if (_Mode < 1.5) return RuneCircle(i.q);
                if (_Mode < 2.5) return Burst(i);
                return Mote(i);
            }
            ENDCG
        }
    }
}
