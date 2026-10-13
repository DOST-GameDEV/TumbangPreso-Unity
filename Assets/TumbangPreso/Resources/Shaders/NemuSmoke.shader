// NEMU'S DEAD AND THEIR AURA (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 11.3).
// The owner: *"use black and purple or low opacity black"*, smoky wraiths, *"hell i hate how the ghosts look"* (v1's lathed
// translucent bodies). Fade's NIGHTFALL is the reference: black smoke with the faces and claws drawn in light inside it.
//
// One shader, three modes (the third, the whole ghost, is described above `ghost`), both particles laid out by hand (`Visual.NemuSmokeBatch`):
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

            // GHOST (`_Mode` 2), ONE ghost per quad. Film r3, the owner: *"these doesnnt read as ghosts anymroe"*; preview v2, the
            // owner: *"the ghosts dont look that scary"*. v2's hood with claws raised over it read as a cat's ears, and its filled
            // glowing slits as an emoji. So the dead are now GAUNT AND TALL: a long skull tipped on its neck, long black hair of
            // smoke hanging past the shoulders in strings, a starved body tearing into ragged strips, long jointed arms hanging or
            // reaching at the lens with spindly fingers (never above the head), and a DEATH MASK: a dim violet face with two
            // hollow black sockets, a burning pinpoint in each, and a jaw that drops far past the chin when it screams.
            // The quad is 0.62 as wide as it is tall (`NemuSmokeBatch.GhostAspect`).
            // Vertex colour: r = the face (0 seen from behind); g = arms (high nibble: 0 hanging, 15 reaching) and scream (low
            // nibble: 0 shut, 15 wide); b = seed; a = density.
            float2 rot2(float2 p, float a) { float c = cos(a), s = sin(a); return float2(p.x * c - p.y * s, p.x * s + p.y * c); }
            float ellipse(float2 p, float2 r) { return (length(p / r) - 1.0) * min(r.x, r.y); }

            fixed4 ghost(float2 c, fixed4 col, float near)
            {
                float seed = col.b * 31.0;
                float packed = floor(col.g * 255.0 + 0.5);
                float reach = floor(packed / 16.0) / 15.0;
                float scream = fmod(packed, 16.0) / 15.0;
                float2 q = float2(c.x * 0.62, c.y);
                // STOP-MOTION: the head twitches to a new tilt now and then and HOLDS it (stepped from the clock, never smooth).
                float beat = floor(_Clock * 7.0 + seed * 3.7);
                float twitch = hash(float2(beat, seed)) > 0.55 ? (hash(float2(seed, beat)) - 0.5) * 0.9 : 0.0;
                float tilt = (frac(seed * 0.37) - 0.5) * 0.5 + twitch;
                float2 neck = float2(0.0, 0.47);
                float2 hq = rot2(q - neck, -tilt) + neck;
                // Skull: long and narrow.
                float d = ellipse(hq - float2(0.0, 0.67), float2(0.145, 0.205));
                // Hair: from the crown down past the shoulders, widening, its lower edge torn into strings that sway.
                float hy = hq.y;
                float hx = hq.x + sin(hy * 6.0 + _Clock * 2.2 + seed) * 0.035 * saturate(0.85 - hy);
                float hw = 0.16 + 0.11 * saturate((0.68 - hy) / 0.6);
                float strand = 0.36 - 0.42 * noise(float2(hx * 17.0 + seed, _Clock * 0.25 + seed));
                float hair = max(max(abs(hx) - hw, hy - 0.86), strand - hy);
                d = min(d, hair);
                // A starved body: narrow shoulders, a caved chest, then a tail torn into strips.
                float below = saturate(0.4 - q.y);
                float sway = sin(q.y * 4.0 + _Clock * 2.6 + seed) * 0.09 * below;
                float t = saturate((0.45 - q.y) / 1.45);
                float w = lerp(0.20, 0.15, saturate(t * 2.5)) * (1.0 - smoothstep(0.35, 1.0, t)) + 0.012;
                float body = max(abs(q.x - sway) - w, q.y - 0.45);
                float strips = smoothstep(0.5, 0.68, noise(float2((q.x - sway) * 11.0 + seed, q.y * 1.3 - _Clock * 1.1)));
                body += strips * 0.14 * smoothstep(0.4, 0.75, t);
                d = smin(d, body, 0.06);
                // Arms: long and jointed; hanging to the thighs, or reaching at the lens with the hands either side of the face.
                for (int s = -1; s <= 1; s += 2)
                {
                    float2 sh = float2(s * 0.18, 0.40);
                    float jerk = 0.025 * sin(_Clock * 11.0 + seed * 2.0 + s * 1.7) * step(0.7, hash(float2(beat, s + seed)));
                    float2 elbow = lerp(float2(s * 0.27, 0.02), float2(s * 0.40, 0.22), reach) + float2(jerk, 0.0);
                    float2 hand = lerp(float2(s * 0.25, -0.38), float2(s * 0.43, 0.56), reach) + float2(0.0, jerk);
                    d = smin(d, capsule(q, sh, elbow, 0.03), 0.03);
                    d = min(d, capsule(q, elbow, hand, 0.022));
                    d = min(d, length(q - hand) - 0.035);
                    float2 dir = normalize(hand - elbow);
                    for (int k = 0; k < 4; k++)
                    {
                        float off = (k - 1.5) * 0.34 + 0.07 * sin(_Clock * 9.0 + k * 1.3 + seed);
                        float2 mid = hand + rot2(dir, off) * 0.10;
                        float2 tip = mid + rot2(dir, off * 1.3 - s * 0.45) * 0.09;
                        d = min(d, capsule(q, hand, mid, 0.011));
                        d = min(d, capsule(q, mid, tip, 0.006));
                    }
                }
                // The edge frays like smoke torn off it.
                float n = fbm(q * 3.4 + float2(seed, -_Clock * 1.3));
                d += (n - 0.5) * 0.09;
                float density = col.a;
                float fill = smoothstep(0.02, -0.02, d);
                float a = fill * density * near;
                float band = smoothstep(-0.06, -0.012, d) * (1.0 - smoothstep(-0.012, 0.015, d)) * smoothstep(0.42, 0.68, n);
                float rim = band * density * near;
                // THE DEATH MASK. A dim violet face so the holes in it read as holes; two hollow sockets slanting down to the
                // middle, each with a burning pinpoint; the jaw drops past the chin as it screams.
                float2 f = hq - float2(0.0, 0.66);
                float mask = smoothstep(0.0, -0.06, ellipse(f, float2(0.12, 0.17)));
                float holes = 0.0, pupils = 0.0, ring = 0.0;
                for (int e = -1; e <= 1; e += 2)
                {
                    float2 p = rot2(f - float2(e * 0.058, 0.03), e * 0.42);
                    float sd = ellipse(p, float2(0.05, 0.034));
                    holes = max(holes, smoothstep(0.006, -0.006, sd));
                    ring += smoothstep(0.008, 0.0, abs(sd)) * 0.3;
                    float2 pp = f - float2(e * 0.056, 0.022);
                    pupils += exp(-dot(pp, pp) / 0.00007) + 0.25 * exp(-dot(pp, pp) / 0.0006);
                }
                float open = 0.35 + 0.65 * scream * (0.9 + 0.1 * sin(_Clock * 23.0 + seed));
                float jawLow = -0.09 - 0.2 * open;
                float2 m = f - float2(0.0, (jawLow - 0.04) * 0.5);
                float mouthD = ellipse(m, float2(0.026 + 0.022 * open, abs(jawLow + 0.04) * 0.5 + 0.01));
                float mouth = smoothstep(0.006, -0.006, mouthD);
                float tz = (f.y - jawLow * 0.6) / max(0.02, abs(jawLow) * 0.35);
                float throat = mouth * exp(-tz * tz * 4.0);
                ring += smoothstep(0.008, 0.0, abs(mouthD)) * 0.3;
                float faceOn = col.r * near * fill;
                // Film r10: a mask lit at 0.16 read as a flat lavender face with a glowing oval mouth (silly, not dead). The
                // mask is now barely there and shaded like a skull (brow and cheekbones catch it), the sockets and the jaw are
                // BLACK, and the only hot things are the two pinpoints and an ember deep in the throat.
                float bone = saturate(1.0 - length((f - float2(0.0, 0.05)) / float2(0.13, 0.15)));
                float lit = (mask * (0.03 + 0.07 * bone) * (1.0 - holes) * (1.0 - mouth) + ring * 0.35) * faceOn;
                float hot = (pupils * 6.0 + throat * 0.5) * faceOn;
                float3 rgb = _Core.rgb * a + _RimColor.rgb * rim * _RimGain * 1.4 + _RimColor.rgb * lit * 2.0
                           + (_RimColor.rgb * 1.3 + float3(0.45, 0.35, 0.5) * saturate(pupils)) * hot;
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
