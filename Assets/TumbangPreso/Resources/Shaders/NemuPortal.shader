// NEMU'S GATE (LET THEM IN v10, 2026-10-14, `Runtime/Visual/NemuPortal.cs`). The owner on v9: *"siguro wag na floor cracks"* and
// *"js show some sort of otherwordly portal somewhere where the ghosts come from"*; on the first film of it: *"texturize portal
// make it look amazing and not flat"*. A tear in the air over the court, built to read as a HOLE, not a disc:
//  * THE TWIST: the world behind it (a grab of the frame) is wrung round it, so the sky and the buildings swirl into its lip.
//  * THE THROAT: a tunnel going back for ever: smoke walls mapped by angle and 1/radius, three layers turning at their own
//    speeds, shaded darker the deeper they go, faint violet seams lighting the ridges, black at the bottom.
//  * THE LIP: a torn bevel lit from above (its top edge catches the light, its underside falls into shadow), crawling, with
//    filaments of violet crackling along it and tongues of black smoke dragged inward over it.
//  * THE PULL: specks of light and smoke sucked in round it on spirals.
// It always faces the lens (turned in the vertex stage). _Open 0 is a hairline slit, 1 the full oval; _Surge flares it as the
// souls pour out. Black and one violet only. Loaded through Resources (`Shaders/NemuPortal`), which keeps it in the player.
Shader "TumbangPreso/NemuPortal"
{
    Properties
    {
        _Open ("Open", Range(0, 1)) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _Surge ("Surge", Range(0, 1)) = 0
        _Size ("Size (m, quad)", Float) = 3
        _Violet ("Violet", Color) = (0.62, 0.26, 1.0, 1)
        _Glow ("Glow", Float) = 2.4
        _Twist ("Twist of the world behind", Float) = 0.06
        _Clock ("Clock (s)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-5" "RenderType" = "Transparent" "IgnoreProjector" = "True" "DisableBatching" = "True" }
        GrabPass { "_NemuGateGrab" }
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

            float _Open, _Fade, _Surge, _Size, _Glow, _Twist, _Clock;
            fixed4 _Violet;
            sampler2D _NemuGateGrab;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 grab : TEXCOORD1; float4 centre : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 centre = UnityObjectToViewPos(float3(0, 0, 0));
                float2 corner = v.uv * 2.0 - 1.0;
                float3 at = centre + float3(corner.x * _Size * 0.5, corner.y * _Size * 0.5, 0);
                o.pos = mul(UNITY_MATRIX_P, float4(at, 1));
                o.uv = corner;
                o.grab = ComputeGrabScreenPos(o.pos);
                o.centre = ComputeGrabScreenPos(mul(UNITY_MATRIX_P, float4(centre, 1)));
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
                float s = 0.0, a = 0.5;
                for (int k = 0; k < 4; k++) { s += noise(p) * a; p = p * 2.03 + 7.1; a *= 0.5; }
                return s;
            }
            // A noise that wraps round the circle (angle in 0..1 tiles at `n` cells), so the tunnel has no seam.
            float ringNoise(float u, float v, float n)
            {
                float a = u * n, ia = floor(a), fa = frac(a);
                float sa = fa * fa * (3.0 - 2.0 * fa);
                float a0 = fmod(ia, n), a1 = fmod(ia + 1.0, n);
                float iv = floor(v), fv = frac(v), sv = fv * fv * (3.0 - 2.0 * fv);
                float n00 = hash(float2(a0, iv)), n10 = hash(float2(a1, iv)), n01 = hash(float2(a0, iv + 1.0)), n11 = hash(float2(a1, iv + 1.0));
                return lerp(lerp(n00, n10, sa), lerp(n01, n11, sa), sv);
            }
            float ringFbm(float u, float v, float n)
            {
                return ringNoise(u, v, n) * 0.55 + ringNoise(u + 0.13, v * 2.1 + 3.3, n * 2.0) * 0.3 + ringNoise(u + 0.37, v * 4.3 + 8.1, n * 4.0) * 0.15;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float TAU = 6.28318;
                float wide = lerp(0.02, 0.46, _Open);
                float tall = lerp(0.3, 0.66, sqrt(_Open));
                float2 q = float2(i.uv.x / wide, i.uv.y / tall);
                float r = length(q);
                float a = atan2(q.y, q.x);
                float au = a / TAU + 0.5;
                // The lip is torn and crawls.
                float torn = (ringFbm(au, _Clock * 0.6, 7.0) - 0.5) * 0.3 + (ringNoise(au, _Clock * 2.5, 31.0) - 0.5) * 0.07;
                float edge = 1.0 + torn;
                float d = r - edge;                       // < 0 inside the hole
                float rn = r / edge;                      // 1 at the lip

                // THE TWIST: the world behind wrung round its centre, hardest just outside the lip, nothing far out.
                float2 grabUV = i.grab.xy / i.grab.w;
                float2 mid = i.centre.xy / i.centre.w;
                float pull = saturate(1.0 - max(d, 0.0) / 0.9) * step(0.0, d) * (1.0 - smoothstep(0.75, 1.0, max(abs(i.uv.x), abs(i.uv.y))));
                float spin = _Twist * (1.0 + _Surge) * pull * pull;
                float2 off = grabUV - mid;
                float s = sin(spin * 6.0), c = cos(spin * 6.0);
                float2 wrung = mid + float2(off.x * c - off.y * s, off.x * s + off.y * c) * (1.0 - 0.25 * pull * pull);
                float3 behind = tex2D(_NemuGateGrab, wrung).rgb;
                // ...darkened and bruised violet as it nears the lip.
                behind = lerp(behind, behind * float3(0.35, 0.2, 0.5), pull * 0.85);

                // THE THROAT: angle and 1/radius, three layers of smoke walls turning at their own speeds, falling away.
                float depth = 1.0 / max(rn, 0.04);        // 1 at the lip, large deep in
                float3 throat = 0;
                float wallsLit = 0;
                for (int k = 0; k < 3; k++)
                {
                    float speed = 0.35 + 0.25 * k;
                    float turn = (k == 1 ? -1.0 : 1.0) * (0.05 + 0.04 * k);
                    float v = depth * (0.9 + 0.35 * k) + _Clock * speed * (1.0 + 1.5 * _Surge);
                    float u = au + turn * _Clock + 0.15 * depth;
                    float wall = ringFbm(frac(u), v, 9.0 + 4.0 * k);
                    float ridge = 1.0 - abs(wall * 2.0 - 1.0);
                    float seam = pow(saturate(ridge), 10.0);
                    float fall = exp(-(depth - 1.0) * (0.55 + 0.15 * k));
                    throat += float3(0.035, 0.012, 0.06) * wall * fall;
                    wallsLit += seam * fall * (0.5 - 0.12 * k);
                }
                throat += _Violet.rgb * wallsLit * (0.55 + 0.8 * _Surge);
                // The throat's walls shaded: the near rim of the tunnel catches the lip's light, the depths go black.
                throat *= lerp(0.15, 1.0, saturate(rn * rn));
                // Something deep in it, far off, breathing.
                throat += _Violet.rgb * pow(saturate(1.0 - rn), 10.0) * (0.12 + 0.6 * _Surge) * (0.8 + 0.2 * sin(_Clock * 3.0));

                // THE LIP: a bevel lit from above.
                float bevel = 0.11 + 0.04 * _Surge;
                float onLip = 1.0 - smoothstep(0.0, bevel, abs(d + bevel * 0.35));
                float2 n2 = normalize(q + 1e-4);
                float lit = saturate(0.55 + 0.45 * dot(n2, normalize(float2(-0.35, 1.0))));
                float filament = pow(saturate(1.0 - abs(ringFbm(au, _Clock * 4.0, 24.0) * 2.0 - 1.0)), 14.0);
                float flick = 0.75 + 0.25 * noise(float2(au * 40.0, _Clock * 12.0));
                float3 lipCol = lerp(float3(0.02, 0.0, 0.035), _Violet.rgb * _Glow * flick, lit * lit)
                              + _Violet.rgb * _Glow * 1.4 * filament * (1.0 - smoothstep(0.0, bevel * 1.3, abs(d)));
                // A thin hot inner edge where the lip turns down into the throat.
                float inner = 1.0 - smoothstep(0.0, 0.035, abs(d + bevel * 0.75));
                lipCol += _Violet.rgb * _Glow * 0.6 * inner * (0.6 + 0.4 * lit);

                // TONGUES of black smoke dragged in over the lip from outside.
                float tongueField = ringFbm(frac(au + _Clock * 0.03), d * 3.0 + _Clock * 1.2, 12.0);
                float tongue = smoothstep(0.55, 0.85, tongueField) * saturate(1.0 - d / 0.35) * step(-0.05, d);

                // THE PULL: specks spiralling in.
                float specks = 0;
                for (int m = 0; m < 3; m++)
                {
                    float lane = floor(au * (24.0 + 8.0 * m));
                    float ph = frac(_Clock * (0.35 + 0.1 * m) + hash(float2(lane, m)));
                    float sr = lerp(1.9, 1.0, ph * ph);
                    float sa = (lane + 0.5) / (24.0 + 8.0 * m) - ph * 0.08;
                    float2 sp = float2(cos((sa - 0.5) * TAU), sin((sa - 0.5) * TAU)) * sr * edge;
                    float2 dq = (q - sp) * float2(wide, tall) * 9.0;
                    specks += smoothstep(0.08, 0.0, length(dq)) * step(0.5, hash(float2(lane, m + 9.0))) * sin(ph * 3.14159);
                }

                // Composite.
                float inside = 1.0 - smoothstep(-0.01, 0.01, d);
                float3 col = lerp(behind, throat, inside);
                col = lerp(col, float3(0.01, 0.0, 0.02), tongue * 0.85);
                col = lerp(col, lipCol, onLip);
                col += _Violet.rgb * _Glow * 0.7 * specks * (1.0 - inside);
                // The glow it throws on the air round it.
                float halo = exp(-max(d, 0.0) * 4.0) * step(0.0, d) * (0.18 + 0.25 * _Surge);
                col += _Violet.rgb * halo;

                // Outside the hole only the wrung world, the tongues, specks and halo show, fading out to the quad's edge.
                float outer = (1.0 - smoothstep(0.75, 1.0, max(abs(i.uv.x), abs(i.uv.y))));
                float alpha = max(inside, max(onLip, max(tongue * 0.85, max(pull, saturate(halo * 3.0 + specks))))) ;
                alpha = inside > 0.5 ? 1.0 : alpha * outer;
                // A hairline slit still shows whole along its length.
                alpha *= saturate(_Open * 4.0 + 0.5);
                return fixed4(col, alpha * _Fade);
            }
            ENDCG
        }
    }
}
