// PHAISTER'S SKY CIRCLE AND HER DOLL'S SEALS (`PhaisterSkySeal`, `PhaisterDollLook`).
//
// Owner, of magic circles: fine linework, a star polygon, nested rings turning against each other, dense small glyphs
// (`Logs/coordinator/refs/hex`). The lines are one painted picture (`tools/paint_phaister_seal.py`), three layers in its
// three channels, so each layer is one quad at its own height, turned by its own angle: seen from under it the layers
// slide past each other.
//
// LIGHT WITHOUT BLOOM (as `PhaisterSigil`): the stroke read sharp in a pale pink, the same stroke read from a small mip
// in magenta, and from a smaller mip as a wide violet halo. Pale pink, never white: "no white outline lines". The halo also
// COVERS a little, which is what lets violet stay violet over a bright sky: adding it alone goes to white.
//
//   _Mode 0  a layer of lines, a quad lying flat
//   _Mode 1  the dark behind it: rolling smoke across the disc and the opening in its middle
//   _Mode 2  the fall of light from the opening down to the doll (`PhaisterLook.Shell`)
//
// Every mesh is static and placed by the vertex program. No `_Time`, so a paused game holds it and a film can step it.
Shader "TumbangPreso/PhaisterSeal"
{
    Properties
    {
        _SealTex ("Lines (R outer, G star, B inner)", 2D) = "black" {}
        _CloudTex ("Erosion", 2D) = "gray" {}
        _NoiseTex ("Wound noise", 2D) = "gray" {}
        _SmokeTex ("Streaks", 2D) = "gray" {}
        _Mode ("Mode", Float) = 0
        _Chan ("Which lines", Vector) = (1,0,0,0)
        _Centre ("Centre (world)", Vector) = (0,7,0,0)
        _Bottom ("Shaft: where it ends (world), w its radius there", Vector) = (0,0,0,0.5)
        _Size ("Radius (m)", Float) = 5.6
        _Spin ("This layer's turn (radians)", Float) = 0
        _Age ("Clock (s)", Float) = 0
        _Write ("How much is written, round the circle", Range(0,1)) = 1
        _Erode ("Eaten away, 0 whole to 1 gone", Range(0,1)) = 0
        _Bright ("Brightness", Float) = 1
        _Alpha ("Alpha", Range(0,1)) = 1
        _Open ("The opening, 0 shut to 1 wide", Range(0,1)) = 1
        _Hole ("The opening's radius", Float) = 0.335
        _Pale ("Line", Color) = (1.00,0.70,0.94,1)
        _Magenta ("Inner glow", Color) = (0.95,0.16,0.80,1)
        _Violet ("Halo", Color) = (0.46,0.12,0.92,1)
        _Dark ("Dark", Color) = (0.20,0.035,0.40,1)
        _Smoke ("Smoke", Color) = (0.42,0.09,0.72,1)
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

            sampler2D _SealTex, _CloudTex, _NoiseTex, _SmokeTex;
            float _Mode, _Size, _Spin, _Age, _Write, _Erode, _Bright, _Alpha, _Open, _Hole;
            float4 _Chan, _Centre, _Bottom;
            fixed4 _Pale, _Magenta, _Violet, _Dark, _Smoke;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 p:TEXCOORD0; float face:TEXCOORD1; };

            static const float Extent = 1.05, TAU = 6.2831853;

            v2f vert(appdata v)
            {
                v2f o;
                float3 world;
                if (_Mode > 1.5 && _Mode < 2.5)
                {
                    // The fall of light: an open funnel, wide at the opening, narrow at the doll.
                    float a = v.uv.x * TAU, u = v.uv.y;
                    float r = lerp(_Size, _Bottom.w, pow(u, 0.7));
                    world = lerp(_Centre.xyz, _Bottom.xyz, u) + float3(cos(a), 0, sin(a)) * r;
                    o.p = v.uv;
                    // How square-on this side of the funnel is: its silhouette fades, so it has no drawn edge.
                    o.face = abs(dot(float3(cos(a), 0, sin(a)), normalize(_WorldSpaceCameraPos - world)));
                }
                else
                {
                    float2 q = (v.uv - 0.5) * 2.4;
                    o.p = q;
                    o.face = 1;
                    world = _Centre.xyz + float3(q.x, 0, q.y) * _Size;
                }
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1));
                return o;
            }

            float2 Turn(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }
            float LineAt(float2 uv, float mip) { return dot(tex2Dlod(_SealTex, float4(uv, 0, mip)).rgb, _Chan.rgb); }

            fixed4 Lines(float2 p)
            {
                float r = length(p);
                if (r > Extent) return 0;
                float2 q = Turn(p, _Spin);
                float2 uv = q / Extent * 0.5 + 0.5;
                float dx = length(fwidth(uv));
                float mip = max(0.0, log2(max(dx * 2048.0 * 0.6, 1e-4)));
                float sharp = LineAt(uv, mip);
                float near = LineAt(uv, max(mip, 3.2)), far = LineAt(uv, max(mip, 5.2));

                // It writes itself round the circle from the top, with a hot front.
                float a = frac(atan2(q.y, q.x) / TAU + 0.25);
                float written = smoothstep(0.0, 0.015, _Write * 1.03 - a);
                float front = smoothstep(0.09, 0.0, abs(_Write * 1.03 - a)) * (_Write < 0.995 ? 1.0 : 0.0);

                // Pulses run round the lines, so they are alive and not printed.
                float n = tex2Dlod(_NoiseTex, float4(a * 2.0 - _Age * 0.11, r * 0.8 + _Age * 0.03, 0, 0)).r;
                float pulse = 0.72 + 0.75 * smoothstep(0.45, 0.8, n);

                // It goes by being eaten, with a burning edge.
                float e = tex2Dlod(_CloudTex, float4(p * 1.15 + 0.13, 0, 0)).r;
                float eat = smoothstep(_Erode * 1.15 - 0.08, _Erode * 1.15, e + 0.02);
                if (_Erode <= 0.001) eat = 1.0;
                float ember = _Erode > 0.001 ? smoothstep(0.16, 0.0, e - _Erode * 1.15) * eat : 0.0;

                float heat = (pulse + front * 2.5 + ember * 2.0) * _Bright;
                fixed3 c = _Pale.rgb * saturate(sharp * 1.25) * min(heat, 1.6)
                         + _Magenta.rgb * near * 2.1 * heat
                         + _Violet.rgb * far * 4.2 * heat;
                float cover = saturate(sharp * 1.1) * 0.95 + saturate(near * 3.0) * 0.8 + saturate(far * 4.0) * 0.42;
                float k = written * eat * _Alpha;
                return fixed4(c * k, saturate(cover) * k);
            }

            fixed4 Back(float2 p)
            {
                float r = length(p);
                float ang = atan2(p.y, p.x);
                // Smoke wound round the middle: read in polar so it swirls, and turned with the clock.
                float2 pol = float2(ang / TAU + r * 0.22 - _Age * 0.03, r * 0.42 - _Age * 0.03) * 2.0;
                float n1 = tex2Dlod(_CloudTex, float4(pol, 0, 0)).r;
                float2 pol2 = float2(p.x * 0.55 + _Age * 0.02, p.y * 0.55 - _Age * 0.015) + r * 0.12;
                float n2 = tex2Dlod(_NoiseTex, float4(pol2, 0, 0)).r;
                float body = smoothstep(1.08, 0.72, r + (n1 - 0.5) * 0.34);
                float fine = tex2Dlod(_CloudTex, float4(p * 1.15 + 0.13, 0, 0)).r;
                float eat = _Erode <= 0.001 ? 1.0 : smoothstep(_Erode * 1.2 - 0.1, _Erode * 1.2, fine);
                fixed3 smoke = lerp(_Dark.rgb, _Smoke.rgb, smoothstep(0.42, 0.78, n1 * 0.6 + n2 * 0.5));
                float alpha = body * lerp(0.92, 0.70, saturate(r));

                // THE OPENING is a hole: a lens that widens to a round well, darkest at its heart, its own light down inside
                // round the wall (ref1's crescent), and the wall's lip the brightest thing.
                float lensW = _Hole * lerp(0.92, 1.0, _Open), lensH = max(0.004, _Hole * _Open);
                float2 s = p / float2(lensW, lensH);
                float sd = (length(s) - 1.0) * lensH;
                float inside = smoothstep(0.012, -0.012, sd);
                float2 wind = float2(ang / TAU * 2.0 + r * 3.2 - _Age * 0.22, r * 1.6 - _Age * 0.09);
                float w1 = tex2Dlod(_NoiseTex, float4(wind, 0, 0)).r, w2 = tex2Dlod(_CloudTex, float4(wind * 0.7 + 0.31, 0, 0)).r;
                // The crescent: one side of the wall lit, slowly coming round.
                float side = 0.5 + 0.5 * cos(ang - _Age * 0.55);
                float wall = smoothstep(0.30, 0.95, r / max(_Hole, 1e-3)) * lerp(0.28, 1.0, side * side);
                float flame = saturate(wall * (w1 * 0.9 + w2 * 0.65));
                fixed3 well = lerp(_Dark.rgb * 0.22, _Violet.rgb * 1.1, smoothstep(0.08, 0.42, flame));
                well = lerp(well, _Magenta.rgb * 1.25, smoothstep(0.38, 0.72, flame));
                well = lerp(well, _Pale.rgb * 1.15, smoothstep(0.72, 1.0, flame));
                fixed3 c = lerp(smoke, well, inside);
                alpha = lerp(alpha, 1.0, inside * step(0.001, _Open));
                float lip = smoothstep(0.035, 0.0, abs(sd)) * step(0.001, _Open);
                c += _Magenta.rgb * lip * 1.1 * _Bright;
                // A spill of its light on the smoke round the opening.
                c += _Magenta.rgb * smoothstep(0.25, 0.0, sd) * (1.0 - inside) * 0.45 * saturate(_Open * 2.0) * _Bright;

                float k = eat * _Alpha;
                return fixed4(c * alpha * k, alpha * k);
            }

            fixed4 Shaft(float2 uv, float face)
            {
                // Soft wide veils of light turning as they fall, never stripes: low frequency round the funnel, eroded with the
                // cloud so there is no straight edge, and strongest down its middle height.
                float n1 = tex2Dlod(_CloudTex, float4(uv.x * 2.0 + uv.y * 0.35 - _Age * 0.05, uv.y * 0.55 - _Age * 0.33, 0, 0)).r;
                float n2 = tex2Dlod(_CloudTex, float4(uv.x * 3.0 - uv.y * 0.5 + _Age * 0.07, uv.y * 0.9 - _Age * 0.52, 0, 0)).r;
                float s = smoothstep(0.34, 0.86, n1 * 0.62 + n2 * 0.5);
                float ends = smoothstep(0.0, 0.16, uv.y) * smoothstep(1.0, 0.8, uv.y);
                float a = s * ends * _Alpha * lerp(1.0, 0.7, uv.y) * smoothstep(0.08, 0.75, face);
                fixed3 c = lerp(_Violet.rgb, _Magenta.rgb, s * 0.8) * _Bright;
                return fixed4(c * a * 0.95, a * 0.55);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Mode < 0.5) return Lines(i.p);
                if (_Mode < 1.5) return Back(i.p);
                return Shaft(i.p, i.face);
            }
            ENDCG
        }
    }
}
