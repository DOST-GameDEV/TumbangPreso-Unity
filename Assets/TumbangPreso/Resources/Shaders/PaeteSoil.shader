// ⚠️⚠️ THE SOIL UNDER THE COURT IN PAETE'S ULTIMATE (v9, 2026-10-08). The owner, choosing the shot: *"The camera goes
// underground. When he slams the court, the camera drops through it and rides his root through the dark soil, then bursts
// back up with the tree's first claw."* (`HeroIntroductionScene.PaeteUnder.cs`). Everything the lens sees down there wears
// this: the tunnel's walls, the stones, the dead roots, the slipper, the can and his own living root.
//
// Why a shader of its own: under the court there is NO LIGHT. The scene has one directional sun and it is above a court that
// shadows everything below it, so `VfxMaterial.Solid` (lit Standard) comes out black where the map casts shadows and patchy
// sunlit where it does not (the film's bare plane), and neither is his root lighting the earth as it passes. So this one
// ignores the scene's lights entirely and is lit by three of its own, all his lime:
//
//  * `_PuRootA` to `_PuRootB`: the lit length of his root, from under his hands to its head, as ONE LINE of light (the nearest
//    point on the segment lights the surface), so the soil glows along the whole root and goes dark beyond its head;
//  * `_PuRootB` again, hotter: the head itself, the light that races ahead of the lens;
//  * `_PuSpot`: the light through the cracks in the ceiling under the spot as the court breaks.
// The light is STEPPED (three soft-edged bands), the game's toon shading, so the walls show pools of light, never a gradient.
//
//  * `_Strata` 1 paints the BEDS of the soil by height in the tunnel's own space (`_PuToRig`): `_PuStrata[k]` is a bed's
//    colour and the height of its top, `_PuStrataWave[k]` how far and how fast its top wanders, all typed by hand in the C#
//    (`PuStrataRows`). A dark seam lies where one bed sits on the next. Grit and pebbles are two thresholded noises, so the
//    walls have something to streak past the lens. 0 is a THING in the soil, in its own `_Color`.
//  * `_Self` is the share of a surface that shines by itself (the lime cord in his root is all of it).
//  * `_Ink` > 0 draws the game's ink round a thing (a second, inverted hull); the tunnel's inward shell has none.
// Distance sinks into `_PuDeep`, so the far end of the tunnel is dark until his light gets there.
// Loaded through Resources (`Shaders/PaeteSoil`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "TumbangPreso/PaeteSoil"
{
    Properties
    {
        _Color ("Colour", Color) = (0.4, 0.28, 0.18, 1)
        _Strata ("Paint the beds (the tunnel's walls)", Float) = 0
        _Self ("Shines by itself", Float) = 0
        _Grain ("Grit and pebbles", Float) = 1
        _Ink ("Ink width (m)", Float) = 0
        _InkColor ("Ink", Color) = (0.06, 0.035, 0.02, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Strata, _Self, _Grain;
            float4x4 _PuToRig;
            float4 _PuStrata[8];
            float4 _PuStrataWave[8];
            float4 _PuRootA, _PuRootB, _PuSpot;
            float4 _PuLight;   // rgb his light, a the share of its own colour a surface keeps with no light on it
            float4 _PuDeep;    // rgb the dark the distance sinks into, a how far away it is complete (m)

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; float3 rig : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.wp = world.xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.rig = mul(_PuToRig, float4(world.xyz, 1.0)).xyz;
                return o;
            }

            float puHash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float puNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(puHash(i), puHash(i + float3(1, 0, 0)), f.x),
                                 lerp(puHash(i + float3(0, 1, 0)), puHash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(puHash(i + float3(0, 0, 1)), puHash(i + float3(1, 0, 1)), f.x),
                                 lerp(puHash(i + float3(0, 1, 1)), puHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            // One of his lights on this surface: wrapped, so a wall turned a little away from it is not black.
            float puLit(float3 toLight, float3 n, float strength, float fall)
            {
                float d2 = dot(toLight, toLight);
                float wrap = saturate(dot(n, toLight * rsqrt(d2 + 1e-5)) * 0.6 + 0.4);
                return strength * wrap / (1.0 + d2 * fall);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 base = _Color.rgb;
                if (_Strata > 0.5)
                {
                    float3 rp = i.rig;
                    base = _PuStrata[0].rgb;
                    for (int k = 1; k < 8; k++)
                    {
                        float4 w = _PuStrataWave[k];
                        float edge = _PuStrata[k].w + w.x * sin(rp.z * w.y + w.z) + 0.5 * w.x * sin(rp.z * w.y * 2.3 + rp.x * 2.1 + w.z * 1.7);
                        float under = smoothstep(edge + 0.012, edge - 0.012, rp.y);
                        base = lerp(base, _PuStrata[k].rgb, under);
                        // The seam where this bed lies on the one above it.
                        base *= 1.0 - 0.32 * smoothstep(0.035, 0.0, abs(rp.y - edge));
                    }
                    float pebbles = smoothstep(0.64, 0.68, puNoise(rp * 9.0 + 3.7));
                    float pits = smoothstep(0.60, 0.70, puNoise(rp * 3.6 + 11.3));
                    float grit = puNoise(rp * 31.0);
                    base *= 1.0 + _Grain * (0.34 * pebbles - 0.22 * pits + 0.10 * (grit - 0.5));
                }

                float3 n = normalize(i.wn);
                float3 ab = _PuRootB.xyz - _PuRootA.xyz;
                float h = saturate(dot(i.wp - _PuRootA.xyz, ab) / max(dot(ab, ab), 1e-4));
                float light = puLit(_PuRootA.xyz + ab * h - i.wp, n, _PuRootA.w, 3.0)
                            + puLit(_PuRootB.xyz - i.wp, n, _PuRootB.w, 1.6)
                            + puLit(_PuSpot.xyz - i.wp, n, _PuSpot.w, 0.9);
                // Stepped: three bands with soft edges.
                float lq = min(light, 2.0) * 3.0;
                float fl = floor(lq);
                light = (fl + smoothstep(0.40, 0.60, lq - fl)) / 3.0;

                float sink = saturate(distance(i.wp, _WorldSpaceCameraPos) / max(_PuDeep.a, 0.01));
                float3 unlit = lerp(base * _PuLight.a, _PuDeep.rgb, sink * 0.85);
                float3 col = unlit + base * _PuLight.rgb * light;
                col = lerp(col, base, saturate(_Self));
                return fixed4(col, 1.0);
            }
            ENDCG
        }

        // The ink: the same thing a little larger, seen from inside out.
        Pass
        {
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Ink;
            fixed4 _InkColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.xyz += normalize(UnityObjectToWorldNormal(v.normal)) * _Ink;
                o.pos = mul(UNITY_MATRIX_VP, world);
                // No ink asked for: put the hull outside the view so nothing of it is drawn.
                if (_Ink <= 0.0) o.pos = float4(2.0, 2.0, 2.0, 1.0);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_InkColor.rgb, 1.0);
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}
