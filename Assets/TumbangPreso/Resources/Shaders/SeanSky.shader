// ⚠️⚠️ THE SKY RAGO TURNS (MAYON AWAKENS v4, 2026-11-23). The owner, of v3: *"the current shots really dont sell the fact that
// he creates a whole volcano. the sky should turn red, with dark clouds etc"*. A volcano is a sky event, and v3's sky was the
// street's afternoon. This is the far sky of the cutscene while he calls the mountain:
//
//  * THE SKY    blood red overhead, burning orange at the horizon. Saturated, never white: the hottest value is an orange.
//  * THE CLOUDS heavy and dark, painted in three tones (dark body, warm mid, ember glow on the undersides), rolling in from
//               behind him toward the spot as `_Turn` rises, and churning. Shaded from the glow below (`_Glow`, the eruption),
//               so their undersides flare when the mountain bursts up. Lightning lights them from inside (`_Flash`).
//  * MAYON      the mountain's spirit: a vast perfect cone in silhouette on the horizon toward the spot (`_MayonDir`), its
//               crater kindling as he calls it (`_Kindle`) and its plume climbing into the clouds from the rise (`_Erupt`).
//               Only painted here, in the sky: the rule's mountain is the 3 m one in the court, this is its echo.
//
// ⚠️ IT ONLY DRAWS WHERE THE MAP SHOWS SKY. The dome is centred on the lens and pushed onto the far plane, drawn after the
// map's skybox with the depth test on, so every wall, roof, wire and tree of the map stays in front of it and nothing of the
// map is hidden (Zack's street needed `StageOnlyView` because his stage replaces the world; Rago's stays in the real street).
// ⚠️ It ignores its transform except for being drawn: the mesh is a unit sphere whose points ARE directions.
// ⚠️ Nothing here is a texture: the cloud field is value noise in the shader, so it costs one draw and no memory.
// Loaded through Resources (`Shaders/SeanSky`), which is what keeps it in the player.
Shader "TumbangPreso/SeanSky"
{
    Properties
    {
        _Turn ("How far the sky has turned (0 the street's, 1 his)", Range(0, 1)) = 0
        _Clock ("Scene clock (s)", Float) = 0
        _MayonDir ("Toward the spot (world, xz)", Vector) = (0, 0, 1, 0)
        _Kindle ("Mayon's crater kindled", Range(0, 1)) = 0
        _Erupt ("Mayon's plume, 0 to 1", Range(0, 1)) = 0
        _Glow ("The eruption's light on the cloud undersides", Range(0, 2)) = 0
        _Flash ("Lightning inside the clouds", Range(0, 1)) = 0
        _Zenith ("Overhead", Color) = (0.36, 0.05, 0.04, 1)
        _Mid ("Mid sky", Color) = (0.74, 0.13, 0.05, 1)
        _Horizon ("Horizon", Color) = (1.0, 0.47, 0.12, 1)
        _CloudDark ("Cloud body", Color) = (0.15, 0.045, 0.045, 1)
        _CloudMid ("Cloud mid", Color) = (0.36, 0.095, 0.06, 1)
        _CloudEmber ("Cloud undersides", Color) = (0.96, 0.38, 0.10, 1)
        _MountainTone ("Mayon", Color) = (0.11, 0.035, 0.035, 1)
    }
    SubShader
    {
        // After the skybox (2500), the clouds (+51) and the street's grade (+52), so it is never graded; the map's depth is
        // already laid, so only sky pixels pass.
        Tags { "Queue" = "AlphaTest+53" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Skybox" }
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

            float _Turn, _Clock, _Kindle, _Erupt, _Glow, _Flash;
            float4 _MayonDir;
            fixed4 _Zenith, _Mid, _Horizon, _CloudDark, _CloudMid, _CloudEmber, _MountainTone;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 d = normalize(v.vertex.xyz);
                o.pos = UnityWorldToClipPos(_WorldSpaceCameraPos + d * 50.0);
                // Onto the far plane (just inside it), so the map's depth wins everywhere it was drawn.
                #if defined(UNITY_REVERSED_Z)
                o.pos.z = o.pos.w * 0.000001;
                #else
                o.pos.z = o.pos.w * 0.999999;
                #endif
                o.dir = d;
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1, 0)), c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            // Billowy: the lobes are the turned-over ridges of the noise, so a cloud's edge is a run of round bulges.
            float billow(float2 p)
            {
                float s = 0, w = 0.55;
                for (int k = 0; k < 4; k++) { s += w * (1.0 - abs(vnoise(p) * 2.0 - 1.0)); p = p * 2.03 + float2(3.1, 1.7); w *= 0.5; }
                return s;
            }

            // One deck of cloud, seen from below in perspective. Its lobes are the billow's bulges; each is lit on its outer
            // edge, the side toward the horizon, which is the underside a cloud shows from below and the side the fire lights,
            // and shaded round into a dark crown. Three painted bands: dark body, warm mid, ember underside. `fire` pushes the
            // ember band wider for the lower deck, which is nearer the fire.
            fixed3 CloudLayer(float3 d, float up, float2 toward, float ahead, float scale, float speed, float2 seed, float coverFull, float fire, out float dens)
            {
                float2 layer = d.xz / (max(up, 0.0) + 0.10) * scale + seed;
                float2 drift = toward * _Clock * speed;
                float2 warp = float2(vnoise(layer * 0.35 + _Clock * 0.25), vnoise(layer * 0.35 + 5.2 - _Clock * 0.2)) - 0.5;
                float2 q = layer - drift + warp * 0.5;
                float n = billow(q * 0.5);
                // They roll in from behind him toward the spot: a front that travels along the line as the sky turns.
                float front = lerp(-1.6, 1.4, _Turn);
                float gate = smoothstep(front + 0.35, front - 0.25, ahead);
                // Heaviest overhead, broken toward the horizon, where the red sky and the fire's glow show through.
                float cover = lerp(1.15, coverFull, _Turn) + 0.30 * (1.0 - gate) - 0.10 * saturate(up * 1.4) + 0.10 * (1.0 - saturate(up * 4.0));
                float e = n - cover;
                dens = smoothstep(0.0, 0.02, e) * smoothstep(-0.02, 0.08, up);
                float2 outward = normalize(d.xz + 1e-4);
                float facing = n - billow((q + outward * 0.28) * 0.5);
                // The body: darkest overhead, a warmer dark toward the horizon, where the fire lights the air under it.
                fixed3 c = lerp(_CloudDark.rgb * 1.7, _CloudDark.rgb, saturate(up * 2.2));
                // Warm mid round every edge and on the lower bulges inside a mass.
                float mid = max(1.0 - smoothstep(0.06, 0.09, e), smoothstep(0.05, 0.08, facing));
                c = lerp(c, _CloudMid.rgb, mid);
                // The ember rim: only at the outer edge of a lobe that faces the horizon (its underside), a band, never a fill
                // (v4r5: banded on the whole of small lobes, the lower deck read as floating orange flames).
                float rim = smoothstep(0.0, 0.03, facing) * (1.0 - smoothstep(0.025 + 0.02 * (_Glow + fire * 0.5), 0.045 + 0.02 * (_Glow + fire * 0.5), e));
                c = lerp(c, _CloudEmber.rgb * (0.80 + 0.20 * saturate(1.0 - up * 1.5)), rim);
                // Lightning inside them: the cloud lights through, warm, in patches.
                float bolt = _Flash * smoothstep(0.55, 0.9, vnoise(q * 0.9 + floor(_Clock * 12.0)));
                // Orange, not cream, and only a quarter of the way: these are LINEAR values, and a mid orange in linear is a pale
                // tan on screen (film v4r8 and v4r10: at a half toward a pale gold the flash washed a band of sky toward white).
                return lerp(c, fixed3(1.0, 0.40, 0.10), bolt * 0.28);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;
                float2 flat = d.xz / max(1e-4, length(d.xz));
                float2 toward = normalize(_MayonDir.xz + 1e-5);
                float2 across = float2(toward.y, -toward.x);
                float ahead = dot(flat, toward);               // 1 toward the spot, -1 behind him
                float az = atan2(dot(flat, across), ahead);   // radians off the line to the spot

                // The sky: a hot band at the horizon under a blood-red vault.
                float h = saturate(up);
                fixed3 sky = lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0.0, 0.22, h));
                sky = lerp(sky, _Zenith.rgb, smoothstep(0.25, 0.85, h));
                // The horizon burns hotter toward the mountain.
                sky = lerp(sky, _Horizon.rgb * 1.05, (1.0 - smoothstep(0.0, 0.16, h)) * saturate(ahead * 0.5 + 0.5) * 0.6);

                // MAYON: a concave-sided cone (Mayon's own profile), peaking 24 degrees up, 33 degrees either side: tall enough to stand over
                // the street's roofs and over the little mountain in the court (film v4r1: at 15 degrees the roofs hid it; v4r3: at
                // 29 its crater and plume were off the top of the hero shot).
                float peak = 0.42, halfW = 0.58;
                float side01 = saturate(1.0 - abs(az) / halfW);
                float profile = peak * pow(side01, 1.65);
                profile = min(profile, peak * 0.965);          // the crater's flat lip
                float elev = asin(clamp(up, -1, 1));
                float inMountain = (1.0 - smoothstep(profile - 0.004, profile + 0.004, elev)) * step(abs(az), halfW) * step(-0.2, elev);
                // Lava running down its flanks as it kindles: straight from the crater's lip down the slope, each its own
                // length, thinning and cooling as it runs (film v4r2: wandering ones read as cartoon steam lines).
                float streak = 0;
                float fromTop = (peak * 0.965 - elev) / peak;   // 0 at the lip, 1 at the foot
                // Three broad flows that taper as they run (v4r3: five hairlines read as rays, and thin lines are banned).
                for (int s = 0; s < 3; s++)
                {
                    float lean = (s - 1.0) * 0.55;              // how far it slants off straight down, in az per unit of drop
                    float x0 = (s - 1.0) * 0.02 + lean * fromTop * peak * 1.2 + 0.006 * sin(fromTop * 9.0 + s * 2.0);
                    float w = 0.016 * (1.0 - fromTop * 0.75);
                    float reach = s == 1 ? 0.62 : 0.42;
                    streak = max(streak, (1.0 - smoothstep(w * 0.55, w, abs(az - x0))) * (1.0 - smoothstep(reach * 0.6, reach, fromTop)) * step(0.0, fromTop));
                }
                fixed3 mountain = _MountainTone.rgb + _CloudEmber.rgb * streak * _Kindle * 0.9;
                // Its rim lit by its own crater from above and the street's fire from below.
                mountain += _CloudEmber.rgb * 0.18 * _Kindle * smoothstep(peak * 0.75, peak * 0.965, elev);

                // The crater's glow and the plume out of it.
                float2 crater = float2(az, elev - peak * 0.965);
                float glow = exp(-dot(crater, crater) / (0.0016 + 0.004 * _Erupt)) * _Kindle;
                float plumeH = _Erupt * 0.9;                  // how high up the sky it has climbed (radians)
                float above = elev - peak * 0.96;
                float plumeW = 0.02 + above * 0.55 + 0.03 * billow(float2(az * 9.0, above * 7.0 - _Clock * 0.6));
                float inPlume = step(0.0, above) * (1.0 - smoothstep(plumeH * 0.85, plumeH, above)) * (1.0 - smoothstep(plumeW * 0.8, plumeW, abs(az + above * 0.25)));
                float plumeLit = smoothstep(0.25, 0.0, above) * 0.8 + 0.25 * billow(float2(az * 14.0, above * 12.0 - _Clock));

                // THE CLOUDS: two layers, a high heavy ceiling and a lower, faster, broken deck under it, each rolling toward the
                // spot. The lower one slides faster across the high one, which is what makes the sky read as DEEP (v4r4: one
                // layer, however shaded, was a flat camouflage pattern).
                float densHigh, densLow;
                fixed3 high = CloudLayer(d, up, toward, ahead, 0.75, 0.5, float2(0.0, 0.0), 0.64, 0.0, densHigh);
                fixed3 low = CloudLayer(d, up, toward, ahead, 1.5, 1.5, float2(7.3, 2.1), 0.86, 1.0, densLow);

                fixed3 colour = sky;
                colour = lerp(colour, lerp(_CloudDark.rgb * 1.3, _CloudEmber.rgb, saturate(plumeLit * _Kindle)), inPlume);
                colour = lerp(colour, mountain, inMountain);
                colour += fixed3(1.0, 0.55, 0.15) * glow * 0.85;
                float overMountain = 1.0 - inMountain * step(elev, profile);
                colour = lerp(colour, high, densHigh * overMountain);
                colour = lerp(colour, low, densLow * overMountain);
                // The plume climbs INTO the clouds, so it is drawn over them (v4r3: under them it never showed).
                colour = lerp(colour, lerp(_CloudDark.rgb * 1.3, _CloudEmber.rgb, saturate(plumeLit * _Kindle)), inPlume * 0.9);
                // Below the horizon the far sky is the street's haze, dark red.
                colour = lerp(colour, _CloudDark.rgb * 1.6, saturate(-up * 8.0) * (1.0 - inMountain));
                return fixed4(min(colour, fixed3(1.0, 0.86, 0.6)), _Turn);
            }
            ENDCG
        }
    }
    FallBack Off
}
