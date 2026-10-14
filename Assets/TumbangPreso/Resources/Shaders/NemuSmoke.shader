// NEMU'S DEAD AND THEIR AURA (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 11.3).
// The owner: *"use black and purple or low opacity black"*, smoky wraiths, *"hell i hate how the ghosts look"* (v1's lathed
// translucent bodies). Fade's NIGHTFALL is the reference: black smoke with the faces and claws drawn in light inside it.
//
// One shader, two modes, its particles laid out by hand (`Visual.NemuSmokeBatch`). (Modes 2 and 3, the cutscene's dead and their
// hands drawn as cards, were retired after preview v4: they are 3D now, `Visual.NemuDeadFigure` and `Shaders/NemuDead`.)
//  * `_Mode` 0, SMOKE: a soft round puff with a frayed, flowing rim. PREMULTIPLIED: its body darkens what is behind it (near
//    black, alpha = density) and its rim adds violet light, so one puff is both the black and the edge.
//    Vertex colour: r = how much the rim glows, g = fray (0 whole, 1 gone: holes open from the rim in), b = seed (each puff its
//    own rim), a = density.
//  * `_Mode` 1, LIGHT: eyes, mouths, claws, embers. Adds only (alpha 0), a soft core inside a falloff, never a hard dot.
//    Vertex colour is the light; `_Intensity` lets it go past 1.
//  * `_Clock` is set by whoever lays the particles out (the scene clock in the cutscene, the status clock in play), so the
//    smoke still flows with the world paused under the phase, and a film at 30 frames per game second matches.
//  * A LIGHT's vertex alpha carries a pull toward the lens: (1 - a) * 2 metres, along its own line of sight, so a glow lying
//    on the court is drawn whole instead of cut by it (Nemu's cracks, film r30).
//  * `_Crisp` 1 (the marks' material): a clean edge, so a soul's eyes and mouth keep their drawn shape.
//  * `_Near` (metres) thins anything to nothing as the lens comes within that distance, so no puff ever sits on a lens.
// Loaded through Resources (`Shaders/NemuSmoke`), which keeps it in the player.
Shader "TumbangPreso/NemuSmoke"
{
    Properties
    {
        _Mode ("Mode (0 smoke, 1 light)", Float) = 0
        _Core ("Smoke body", Color) = (0.016, 0.004, 0.03, 1)
        _RimColor ("Smoke rim", Color) = (0.50, 0.17, 0.95, 1)
        _RimGain ("Rim gain", Float) = 0.75
        _Intensity ("Light intensity", Float) = 5.0
        _Clock ("Clock (s)", Float) = 0
        _Near ("Thin within (m)", Float) = 0.6
        _Crisp ("Crisp edge (marks)", Float) = 0
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

            float _Mode, _RimGain, _Intensity, _Clock, _Near, _Crisp;
            fixed4 _Core, _RimColor;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float near : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 view = UnityObjectToViewPos(v.vertex);
                o.near = saturate((-view.z - 0.15) / max(0.01, _Near));
                if (_Mode > 0.5)
                {
                    float pull = (1.0 - v.color.a) * 2.0;
                    float d = length(view);
                    view *= max(0.05, (d - pull) / max(1e-3, d));
                }
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = (i.uv - 0.5) * 2.0;
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
                // `_Crisp` (a soul's hollow eyes and mouth, owner on v7: *"their faces are so weird"*): the edge barely boils, so
                // an eye stays the shape it was drawn instead of wobbling into a blob.
                float d = length(c) + (n - 0.5) * 0.8 * (1.0 - _Crisp * 0.85);
                float body = 1.0 - smoothstep(lerp(0.42, 0.74, _Crisp), 0.98, d);
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
