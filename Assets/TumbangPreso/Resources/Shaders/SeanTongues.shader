// RAGO'S FLAME TONGUES: the wake of his STOKE STEP as MANY tongues, each with a life of its own (`FxStudySeanStoke`).
//
// The owner, of a wake drawn as one textured horn: "all of the flame moves as one. it doesnt look like flame at all". A
// picture scrolled over one or two shells always moves as one. So the wake is a dozen tongues, and each is its own:
//   * BORN at his back, at its own moment (births are staggered, so at any instant the tongues are at different ages);
//   * it STRETCHES back off him, flickers in length at its own rate, sways at its own rate, narrows;
//   * it PINCHES OFF its tip, which goes on as a separate darker blob, rising and drifting back as it shrinks and dies;
//   * the body that is left shrinks back into nothing.
// Inside each tongue a noise streams back faster than the tongue itself, and its edge and tip are EATEN by that noise
// (cut away, never faded). The colour is in hard painted steps: a white-yellow core near its base, an orange body, a red
// edge; the detached blobs are darker red.
//
// ⚠️ ALL OF IT IS HERE, FROM ONE CLOCK. The mesh is every tongue and every blob, built once for the process; each vertex
// carries its tongue's constants (where it is born, how often, how long and wide it gets, how it sways). The vertex
// program works out from `_Age` and the rule's own numbers where his body is, which life each tongue is in, how long it
// is, and lays it as a strip turned to the eye. One material, one draw. A tongue that is not alive is a single point.
//
// ⚠️ ITS BODY IS WHERE THE RULE HAS HIM: launched at `_Launch`, stopped by `_Friction`, never past `_Reach` (where he
// really is). Tongues are born only between his launch and his stop, so the wake is exactly as long-lived as the dash.
//
// Tagged `Transparent` and drawn at 2503 though it is cut out and writes depth, for the cloud puffs' reason: the game's
// occlusion and ink pass would not run this vertex program. No `_Time`, no depth texture, no loops over tongues.
Shader "TumbangPreso/SeanTongues"
{
    Properties
    {
        _ErodeTex ("Noise that eats the edges (the bench's erode_cloud)", 2D) = "grey" {}
        _Age ("Clock: seconds since the press (set per frame)", Float) = 0
        _Opacity ("Shown", Range(0,1)) = 1
        _Erode ("Unused (the bench's per-frame setter writes it)", Range(0,1)) = 0
        _Reach ("How far he has gone so far (m)", Float) = 2
        _Stopped ("When he stopped, from the press (s; far ahead while he is still going)", Float) = 10000
        _Launch ("Launch speed (m/s)", Float) = 10.95
        _Friction ("Friction (m/s^2)", Float) = 30
        _Brace ("Brace before the launch (s)", Float) = 0.18
        _Back ("How far behind the middle of his body the tongues are born (m)", Float) = 0.18
        _Core ("Core: white-yellow", Color) = (1.0, 0.94, 0.62, 1)
        _Body ("Body: orange", Color) = (1.0, 0.50, 0.06, 1)
        _Edge ("Edge: red", Color) = (0.86, 0.16, 0.03, 1)
        _Blob ("Detached blob: dark red", Color) = (0.66, 0.10, 0.03, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _ErodeTex;
            float _Age, _Opacity, _Reach, _Stopped, _Launch, _Friction, _Brace, _Back;
            fixed4 _Core, _Body, _Edge, _Blob;
            // a: x along the strip (0 foot, 1 tip), y across (-1 to 1), z 1 for a detached blob, w a seed
            // b: x, y where it is born off his back (m: across, up), z how often one is born (s), w its offset in that cycle (s)
            // c: x its full length (m), y its full width (m), z how far it sways (m), w how fast (radians a second)
            // d: x how much it rises toward its tip (m), y how fast its length flickers, z when its tip pinches off (share of its life)
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; float4 d:TEXCOORD3; };
            struct v2f { float4 pos:SV_POSITION; float4 info:TEXCOORD0; float2 life:TEXCOORD1; };

            float Gone(float t)
            {
                float s = clamp(t - _Brace, 0.0, _Launch / max(0.01, _Friction));
                return min(_Launch * s - 0.5 * _Friction * s * s, _Reach);
            }

            // A point along tongue `k` at share `s` of its length, now.
            float3 Spine(float s, float3 foot, float len, float4 b, float4 c, float4 d, float curl)
            {
                float3 back = normalize(lerp(float3(0, 0, -1), float3(0, 1, -0.35), curl));
                float sway = sin(s * 3.2 + _Age * c.w + b.w * 9.0) * c.z * s;
                return foot + back * (s * len) + float3(sway, d.x * s * s * len, 0.0);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float period = v.b.z, phase = v.b.w;
                float a = frac((_Age + phase) / period);
                float born = _Age - a * period;
                // The life that was already running at the launch is cut to start AT the launch (squeezed into what is
                // left of it), so every tongue is out from the first frame of the burst, not a tenth of a second later.
                float wasOn = frac((_Brace + phase) / period);
                float firstEnd = _Brace + (1.0 - wasOn) * period;
                if (born < _Brace && _Age >= _Brace && _Age < firstEnd)
                {
                    a = (_Age - _Brace) / max(0.05, firstEnd - _Brace);
                    born = _Brace;
                    period = firstEnd - _Brace;
                }
                float stop = min(_Stopped, _Brace + _Launch / max(0.01, _Friction));
                float alive = step(_Brace - 0.01, born) * step(born, stop - 0.02) * step(0.001, _Opacity);
                float pinch = v.d.z;
                float curl = smoothstep(0.0, 0.25, _Age - _Stopped);
                float3 foot = float3(v.b.x, v.b.y, Gone(_Age) - _Back);
                // It stretches out over the first third of its life, flickering in length at its own rate; after its tip
                // has gone it shrinks back into nothing.
                float flicker = 1.0 + 0.22 * sin(_Age * v.d.y + phase * 7.0);
                float grow = smoothstep(0.0, 0.18, a);
                float after = saturate((a - pinch) / max(0.05, 1.0 - pinch));
                float len = v.c.x * grow * flicker * lerp(1.0, 0.25, after) * (a < pinch ? 1.0 : 0.7);
                float s = v.a.x;
                float3 at, along;
                float half_;
                if (v.a.z > 0.5)
                {
                    // The blob: where the tip was when it pinched off, going on back and up, shrinking.
                    float since = (a - pinch) * period;
                    float pinchedAt = born + pinch * period;
                    float3 footThen = float3(v.b.x, v.b.y, Gone(pinchedAt) - _Back);
                    float3 tip = Spine(1.0, footThen, v.c.x * flicker, v.b, v.c, v.d, curl);
                    float3 drift = float3(sin(phase * 5.0) * 0.3, 0.9, -1.1) * since;
                    float size = v.c.y * 0.55 * (1.0 - after) * step(pinch, a);
                    along = normalize(float3(0.1, 0.6, -1.0));
                    at = tip + drift + along * (s - 0.5) * size * 1.6;
                    half_ = size * 0.5 * sin(3.14159 * saturate(s));
                    alive *= step(pinch, a);
                }
                else
                {
                    at = Spine(s, foot, len, v.b, v.c, v.d, curl);
                    float3 ahead = Spine(min(1.0, s + 0.05), foot, len, v.b, v.c, v.d, curl);
                    float3 behind = Spine(max(0.0, s - 0.05), foot, len, v.b, v.c, v.d, curl);
                    along = ahead - behind;
                    // A teardrop: full a little way from its foot, a neck where the tip is about to pinch, a point at the tip.
                    float neck = 1.0 - 0.75 * smoothstep(pinch - 0.12, pinch, a) * exp(-pow((s - 0.72) / 0.1, 2.0));
                    float shape = pow(sin(3.14159 * saturate(s * 0.92 + 0.08)), 0.7) * (1.0 - 0.55 * s);
                    half_ = 0.5 * v.c.y * shape * neck * lerp(1.0, 0.5, after) * grow;
                }
                // Laid as a strip turned to the eye.
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 side = cross(normalize(along + float3(0, 1e-4, 0)), normalize(eye - at));
                side = normalize(side + float3(1e-5, 0, 0));
                at += side * v.a.y * half_ * alive;
                if (alive < 0.5) at = foot;
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.info = float4(s, v.a.y, v.a.z, v.a.w);
                o.life = float2(a, alive);
                return o;
            }

            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }

            fixed4 frag(v2f i):SV_Target
            {
                float s = i.info.x, across = abs(i.info.y), blob = i.info.z, seed = i.info.w, a = i.life.x;
                // The noise streams back down the tongue faster than the tongue goes, from each tongue's own place in it.
                float n = grey(tex2D(_ErodeTex, float2(s * 1.3 - _Age * 3.4 + seed * 7.0, i.info.y * 0.35 + seed * 3.1)).r);
                // Eaten at its edges always, and more toward its tip and as it ages: cut away, not faded.
                float limit = 1.0 - 0.22 * n - (blob > 0.5 ? 0.15 * n : smoothstep(0.60, 1.0, s) * (0.30 + 0.45 * a) * (0.6 + 0.8 * n));
                clip(limit - across);
                fixed3 colour;
                if (blob > 0.5) colour = across < 0.35 * limit ? _Edge.rgb : _Blob.rgb;
                else
                {
                    // Hard painted steps: core near the foot and up its middle, body, a red rim.
                    float core = (0.42 - 0.5 * s) * (1.15 - 0.6 * a) + 0.14 * (n - 0.5);
                    float body = 0.80 * limit - 0.10;
                    colour = across < core ? _Core.rgb : (across < body ? _Body.rgb : _Edge.rgb);
                }
                return fixed4(colour, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
