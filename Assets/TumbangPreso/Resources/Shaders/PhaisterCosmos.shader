// PHAISTER'S POOLS, BUBBLES, BUTTON AND SWIRL (`PhaisterCosmosPortal` and `PhaisterHexLook`, the kit rework's
// directions A and B).
//
// The construction is Shannon McSheehan's breakdown of Zoe's Sleepy Trouble Bubble (Riot, realtimevfx "LoL FX knowledge
// share"): a DARK disc that covers what is behind it, under a LIGHTER swirl that adds light, the swirl being two copies of
// one picture running opposite ways and multiplied. The dark layer is what reads on a sunlit court with no bloom.
//
// ⚠️ THE LOOK IS IN THE BENCH'S AUTHORED MASKS, NOT IN SINE WAVES. The first round painted the swirl from `sin()` and it
// read as a three-blade pinwheel and a flat sticker (round 1, and the owner's "cartoony sticker vfx" note). Every pattern
// here is a bench picture (`Resources/Textures/UltimateFx`): `noise_simplex_cells` laid on a SPIRAL (u round the disc plus
// the log of the radius, so the cells wind into the middle like a galaxy's arms), `trail_smoke` run up a shell,
// `erode_cloud` eating every edge, `flare_star` for the few stars. Colour is a ramp through the picture's grey.
//
//   _Mode 0  a pool on the ground (B's departure and arrival)
//   _Mode 1  a cosmic bubble facing whoever looks (B's hex mark)
//   _Mode 2  a cloth button over a smoky dark pool, facing whoever looks (A's hex mark; its moth is `PhaisterMoths`)
//   _Mode 3  a swirl of smoke tongues wound round her (B: it closes over her as she sinks and blooms where she lands)
//
// Premultiplied: the dark parts cover, the light parts add. No `_Time`: `_Age` and `_Clock` are the effect's clocks.
// ⚠️ HER PALETTE, AND NO BLUE (`docs/Art_Direction.md` section 1: 0080e8 means DEFENCE). Every violet leans to magenta.
Shader "TumbangPreso/PhaisterCosmos"
{
    Properties
    {
        _NoiseTex ("Cells (noise_simplex_cells)", 2D) = "gray" {}
        _SmokeTex ("Smoke tongues (trail_smoke)", 2D) = "gray" {}
        _CloudTex ("Erosion (erode_cloud)", 2D) = "gray" {}
        _StarTex ("Star (flare_star)", 2D) = "black" {}
        _Mode ("0 ground pool, 1 bubble, 2 button, 3 swirl shell", Float) = 0
        _Centre ("Centre (world)", Vector) = (0,0,0,0)
        _Size ("Radius (m)", Float) = 1
        _Open ("Open, a share of the radius", Float) = 1
        _Age ("The effect's clock (s)", Float) = 0
        _Clock ("The hex's idle clock (s)", Float) = 0
        _Erode ("Eaten away, 0 whole to 1 gone", Range(0,1)) = 0
        _Fuse ("The hex's fuse, 0 to 1", Range(0,1)) = 0
        _Armed ("The hex is armed", Range(0,1)) = 0
        _Since ("Seconds since the hex armed (below 0 before)", Float) = -1
        _Pop ("The hex expiring, 0 to 1", Range(0,1)) = 0
        _Spin ("Swirl speed", Float) = 1
        _Light ("How much light the swirl adds", Range(0,2)) = 1
        _Shell ("Shell: x height (m), y radius at its foot, z radius at its top, w twist (radians)", Vector) = (2,0.6,0.3,3)
        _Dark ("Dark", Color) = (0.060,0.020,0.090,1)
        _Deep ("Deep violet", Color) = (0.235,0.070,0.360,1)
        _Violet ("Violet", Color) = (0.560,0.200,0.780,1)
        _Lilac ("Lilac", Color) = (0.840,0.620,1.000,1)
        _Moon ("Moon pale", Color) = (1.000,0.930,0.960,1)
        _Thread ("Thread", Color) = (0.86,0.16,0.42,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            sampler2D _NoiseTex, _SmokeTex, _CloudTex, _StarTex;
            float _Mode, _Size, _Open, _Age, _Clock, _Erode, _Fuse, _Armed, _Spin, _Since, _Pop, _Light;
            float4 _Centre, _Shell;
            fixed4 _Dark, _Deep, _Violet, _Lilac, _Moon, _Thread;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 q:TEXCOORD0; float3 world:TEXCOORD1; };

            // The masks are painted in plain grey and the project is linear: undo the darkening a sample comes back with.
            float G(float x) { return pow(saturate(x), 0.4545); }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Turn(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }

            v2f vert(appdata v)
            {
                v2f o;
                float3 world;
                if (_Mode > 2.5)
                {
                    float h = v.uv.y;
                    float a = v.uv.x * 6.2832 + _Shell.w * h - _Age * _Spin * 1.6;
                    float r = lerp(_Shell.y, _Shell.z, h * h);
                    world = _Centre.xyz + float3(cos(a) * r, h * _Shell.x, sin(a) * r);
                    o.q = v.uv;
                }
                else
                {
                    float throb = 1.0 + _Armed * (1.0 - _Pop) * 0.06 * sin(_Clock * 7.0);
                    float k = _Size * _Open * throb;
                    if (_Mode < 0.5) world = _Centre.xyz + float3(v.vertex.x, 0, v.vertex.z) * k + float3(0, 0.02, 0);
                    else world = _Centre.xyz + (UNITY_MATRIX_V[0].xyz * v.vertex.x + UNITY_MATRIX_V[1].xyz * v.vertex.z) * k;
                    o.q = v.vertex.xz;
                }
                o.world = world;
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                return o;
            }

            // The cells laid on a spiral: u round the disc (two whole tiles, so the seam meets itself), plus the log of the
            // radius, so they wind tighter toward the middle. Two copies, opposite ways, multiplied (Zoe's construction).
            float Swirl(float2 q, float speed)
            {
                float r = length(q), a = atan2(q.y, q.x) / 6.2832;
                float2 one = float2(a * 2.0 + log(r + 0.04) * 0.55 - _Clock * 0.20 * speed, r * 0.9 + _Clock * 0.03);
                float2 two = float2(-a * 1.0 + log(r + 0.04) * -0.30 + _Clock * 0.11 * speed, r * 0.7 + 0.37);
                return G(tex2D(_NoiseTex, one).r) * (0.35 + 0.85 * G(tex2D(_NoiseTex, two).r));
            }

            float Cloud(float2 p) { return G(tex2D(_CloudTex, p).r); }

            // A few stars: one cell in three holds a star, the bench's own `flare_star` picture, twinkling.
            float Stars(float2 q, float cells, float share)
            {
                float2 p = Turn(q, -_Clock * 0.35 * _Spin) * cells;
                float2 cell = floor(p), f = frac(p);
                float pick = Hash(cell);
                float tw = 0.55 + 0.45 * sin(_Clock * 4.0 + pick * 40.0);
                float2 at = (f - 0.5 - (float2(Hash(cell + 7.1), Hash(cell + 3.3)) - 0.5) * 0.4) / (0.9 * tw) + 0.5;
                float star = G(tex2D(_StarTex, saturate(at)).r) * step(abs(at.x - 0.5), 0.5) * step(abs(at.y - 0.5), 0.5);
                return star * step(1.0 - share, pick) * tw;
            }

            // Grey to her ramp, in soft-edged bands: deep, violet, lilac, and moon pale only in the brightest.
            fixed3 Ramp(float m)
            {
                fixed3 c = _Deep.rgb;
                c = lerp(c, _Violet.rgb, smoothstep(0.38, 0.48, m));
                c = lerp(c, _Lilac.rgb, smoothstep(0.62, 0.72, m));
                c = lerp(c, _Moon.rgb, smoothstep(0.84, 0.92, m));
                return c;
            }

            float Clockwise(float2 q) { return frac(0.25 - atan2(q.y, q.x) / 6.2832); }

            // The arming pop: one ring of light thrown past the rim, thinning, in the 0.45 s after arming.
            float PopRing(float r)
            {
                if (_Since < 0.0 || _Since > 0.45) return 0.0;
                float k = _Since / 0.45;
                float at = lerp(0.90, 1.16, 1.0 - (1.0 - k) * (1.0 - k));
                float w = 0.10 * (1.0 - k) + 0.015;
                return saturate(1.0 - abs(r - at) / w) * (1.0 - k * 0.6);
            }

            // A dark body that covers, light that adds. Returned premultiplied.
            fixed4 Mix(float darkA, fixed3 darkC, float lightA, fixed3 lightC)
            {
                return fixed4(darkC * darkA * (1.0 - lightA) + lightC * lightA, saturate(darkA + lightA * 0.5));
            }

            fixed4 Pool(float2 q)
            {
                float r = length(q);
                float cloud = Cloud(q * 0.55 + float2(_Age * 0.05, 0.3));
                // The dark covering disc: a smoky edge, never a drawn circle.
                float edge = r + (cloud - 0.5) * 0.35;
                float keep = smoothstep(0.0, 0.12, cloud * 0.7 + (1.0 - r) * 0.6 + 0.05 - _Erode * 1.3);
                float darkA = smoothstep(1.02, 0.70, edge) * 0.92 * keep;
                float m = Swirl(q, _Spin) * smoothstep(0.98, 0.55, edge) + smoothstep(0.22, 0.0, r) * 0.6;
                float lightA = smoothstep(0.34, 0.62, m) * keep * _Light;
                // The rim: a broken ring of lilac light where the dark meets the ground.
                float rim = exp(-pow((edge - 0.82) / 0.07, 2.0)) * G(tex2D(_NoiseTex, float2(atan2(q.y, q.x) / 6.2832 * 3.0 - _Age * 0.5, 0.2)).r);
                lightA = saturate(lightA + rim * 0.8 * keep);
                fixed3 lightC = Ramp(max(m, rim * 0.75));
                float star = Stars(q, 4.5, 0.32) * step(r, 0.75) * keep;
                fixed4 o = Mix(darkA, _Dark.rgb, lightA * 0.9, lightC);
                o.rgb += _Moon.rgb * star;
                return o;
            }

            fixed4 Bubble(float2 q)
            {
                float r = length(q);
                // Expiry POPS: a hole opens from the middle and races out; the rim goes last, torn by the cloud.
                float cloud = Cloud(q * 0.7 + 0.2);
                float hole = smoothstep(_Pop * 1.2 - 0.06, _Pop * 1.2, r + (cloud - 0.5) * 0.25 * _Pop);
                float inside = smoothstep(1.0, 0.97, r) * hole;
                float speed = _Spin * (1.0 + 1.5 * _Armed);
                float light = 0.5 + 0.5 * saturate(_Fuse * 1.1);
                float m = Swirl(q * 0.9, speed) * smoothstep(0.98, 0.6, r) + smoothstep(0.28, 0.0, r) * 0.55;
                float lightA = smoothstep(0.30, 0.62, m) * light * inside;
                fixed3 lightC = Ramp(m * (0.85 + 0.25 * _Armed));
                // The rim: the fuse runs round it clockwise as a band of moon pale; once armed, all of it, beating.
                float band = smoothstep(0.80, 0.86, r) * inside;
                float done = step(Clockwise(q), _Fuse * (1.0 - _Pop));
                float beat = _Armed * (0.5 + 0.5 * sin(_Clock * 7.0));
                fixed3 rimC = lerp(lerp(_Deep.rgb, _Violet.rgb, 0.6), lerp(_Lilac.rgb, _Moon.rgb, 0.4 + 0.6 * beat), done);
                float rimA = band * lerp(0.55, 1.0, done);
                // Gloss: a fat crescent, upper left, what makes it a bubble.
                float gloss = smoothstep(0.31, 0.29, length(q - float2(-0.36, 0.40))) * smoothstep(0.25, 0.28, length(q - float2(-0.29, 0.32)));
                float star = Stars(q, 3.4, 0.36 + 0.3 * _Armed) * step(r, 0.78) * inside;
                fixed4 o = Mix(0.94 * inside, _Dark.rgb, saturate(lightA + rimA), lerp(lightC, rimC, rimA));
                o.rgb += _Moon.rgb * (star + gloss * 0.85 * inside);
                o.rgb += _Moon.rgb * PopRing(r);
                o.a = max(o.a, PopRing(r) * 0.6);
                return o;
            }

            // ⚠️ HEX, DIRECTION A, ROUND h2. Round h1 was a bright flat lilac button the size of the whole mark with a thin dark
            // ring: the "cartoony sticker" the owner turned down. The mark is now built in the approved teleport's language:
            // a see-through pool of her smoke (`trail_smoke` wound round it, eroded by the cloud) with a lit lilac edge, and a
            // SMALL dark cloth button sunk in its middle. The moth asleep on it (`PhaisterMoths`) is the one bright thing.
            fixed4 Button(float2 q)
            {
                float r = length(q), ang = atan2(q.y, q.x) / 6.2832;
                float cloud = Cloud(q * 0.6 + float2(_Clock * 0.03, 0.1));
                // Expiry: the whole mark draws in on itself from the edge, torn by the cloud, never holed through the middle.
                float keep = smoothstep(0.0, 0.12, (1.0 - r) * 1.3 + cloud * 0.45 - _Pop * 1.7);
                float edge = r + (cloud - 0.5) * 0.16;
                // The pool: dark smoke, its tongues wound slowly round, see-through toward its edge.
                float smoke = G(tex2D(_SmokeTex, float2(ang * 4.0 + r * 0.6 - _Clock * 0.08, 1.05 - r * 0.85)).r);
                float body = smoothstep(1.0, 0.55, edge);
                // (h2: the smoke cut the pool's edge into a rounded square. It shades the pool now; it no longer shapes it.)
                float darkA = saturate(body * (0.78 + 0.22 * smoke)) * 0.88;
                // (h6: fine smoke streaks alone read as a flat dark disc at range.) Two broad spiral arms of deep violet wind
                // slowly into the middle under the smoke, so the pool has a shape a player sees at six metres.
                float arms = smoothstep(0.42, 0.70, Swirl(q * 0.8, 0.5));
                fixed3 darkC = lerp(_Dark.rgb, _Deep.rgb * 1.35, max(arms * 0.85, smoothstep(0.35, 0.8, smoke) * smoothstep(0.2, 0.8, r) * 0.6));
                // Its lit edge is the fuse: lilac light along the rim, running clockwise from twelve over the 10 s; beating
                // once armed. Where the fuse has not reached, the edge is only a dim violet.
                float rim = exp(-pow((edge - 0.80) / 0.065, 2.0)) * (0.7 + 0.3 * smoke);
                // The fill runs clockwise with a soft glowing head; once armed the ring is whole, no seam at twelve.
                float fill = _Fuse * (1.0 - _Pop), at = Clockwise(q);
                float done = max(smoothstep(fill + 0.002, fill - 0.035, at) * step(0.001, fill), _Armed * (1.0 - _Pop));
                done = max(done, _Armed * step(at, fill));
                float beat = _Armed * (1.0 - _Pop) * (0.5 + 0.5 * sin(_Clock * 7.0));
                float lightA = rim * lerp(0.25, 0.95, done);
                fixed3 lightC = lerp(_Violet.rgb * 0.8, lerp(_Lilac.rgb, _Moon.rgb, 0.25 + 0.6 * beat), done);
                fixed4 o = Mix(darkA, darkC, lightA, lightC);
                // The button: small, dark cloth, its rim lit upper left, four holes and her crimson cross-stitch.
                float br = 0.42;
                float face = smoothstep(br + 0.012, br - 0.012, r);
                if (face > 0.0)
                {
                    float2 n = q / max(r, 1e-4);
                    float upper = dot(n, float2(-0.707, 0.707));
                    fixed3 b = lerp(_Deep.rgb, _Violet.rgb, 0.45 + 0.25 * G(tex2D(_NoiseTex, q * 0.9).r));
                    b = lerp(b, lerp(_Deep.rgb * 0.8, _Lilac.rgb, smoothstep(-0.2, 0.7, upper)), smoothstep(br * 0.74, br * 0.88, r));
                    b = lerp(b, _Dark.rgb, smoothstep(br * 0.94, br, r));
                    float2 hq = (q - float2(0.0, -0.17)) / (br * 0.78);  // holes and stitch below the sleeping moth, where they show
                    float hole = smoothstep(0.17, 0.13, length(abs(hq) - 0.30));
                    float d1 = abs(hq.x - hq.y) * 0.7071, d2 = abs(hq.x + hq.y) * 0.7071;
                    float within = step(max(abs(hq.x), abs(hq.y)), 0.34);
                    float stitch = within * max(smoothstep(0.09, 0.06, d1), smoothstep(0.09, 0.06, d2));
                    b = lerp(b, _Dark.rgb, hole);
                    b = lerp(b, _Thread.rgb, stitch);
                    o = lerp(o, fixed4(b, 1.0), face);
                }
                o *= keep;
                // The arming snap: a broad ring of light thrown out past the rim.
                float pop = PopRing(r);
                o.rgb += lerp(_Lilac.rgb, _Moon.rgb, 0.5) * pop;
                o.a = max(o.a, pop * 0.5);
                return o;
            }

            fixed4 Shell(v2f i, float facing)
            {
                float h = i.q.y, u = i.q.x;
                // Smoke tongues run round it, two copies at two speeds; open between the tongues so what is behind shows.
                // (Round 3: `trail_smoke` holds its tongues in the top of the picture only, so on the shell just its top band
                // showed and floated over her head. The long diagonal streaks of the cells run the whole height.)
                float one = G(tex2D(_NoiseTex, float2(u * 2.0 + h * 0.35, h * 0.9 - _Age * 1.3 * _Spin)).r);
                float two = G(tex2D(_NoiseTex, float2(-u * 1.0 - _Age * 0.5, h * 0.5 + _Age * 0.35) + 0.37).r);
                float m = one * (0.45 + 0.75 * two);
                float cloud = Cloud(float2(u * 2.0, h * 1.2 + 0.3));
                float keep = smoothstep(0.0, 0.10, cloud * 0.55 + (1.0 - h) * 1.0 - 0.05 - _Erode * 1.6);
                float ends = smoothstep(0.0, 0.10, h) * smoothstep(1.0, 0.75, h);
                float alpha = smoothstep(0.22, 0.55, m) * keep * ends;
                fixed3 c = Ramp(m);
                c *= facing > 0 ? 1.0 : 0.55;
                // Mostly light that adds; its darker body covers a little, so it reads over a pale court.
                return fixed4(c * alpha, alpha * 0.55);
            }

            fixed4 frag(v2f i, fixed facing:VFACE) : SV_Target
            {
                if (_Mode < 0.5) return Pool(i.q);
                if (_Mode < 1.5) return Bubble(i.q);
                if (_Mode < 2.5) return Button(i.q);
                return Shell(i, facing);
            }
            ENDCG
        }
    }
}
