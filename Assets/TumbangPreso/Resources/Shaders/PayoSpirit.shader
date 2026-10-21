// THE BUBBLE SPIRIT'S BODY (`PayoBubbleSpirit`, Payo's fall catcher, owner 2026-10-18).
//
// A chubby water spirit with a carved stone mask, turquoise inlay eyes, a woven red-black-white headband and two droopy
// taro-leaf ears. Its mesh is built once (`PayoSpiritMeshes.Spirit`) and every moving part is moved HERE, from the clock
// and from what is baked into each vertex: nothing on the CPU touches a vertex after the build.
//   COLOR      rgb the part's painted colour
//   TEXCOORD0  x how far along its part (a leaf's tip is 1), y its side (-1 left, +1 right), z the part, w spare
//   TEXCOORD1  xyz the part's pivot (object space), w spare
// Parts: 0 water body, 1 stone, 2 turquoise inlay, 3 leaf ear, 4 arm, 5 woven band, 6 mouth, 7 eye glint.
// `_Pose` is the only thing the CPU writes, once a frame, through a property block: x arms raised (0 to 1), y ears
// (-1 swept down, +1 perked up), z mouth open (0 to 1), w how much of it is mist (0 all of it there, 1 none).
//
// THE LOOK: three flat value bands with crisp soft steps (the calm painted style, `CALM_STYLE.md`), a lit water band at
// the silhouette, and the tail eaten into mist by one octave of big-celled noise with a pale painted edge. The ink
// line is an inverted hull of the same animated vertices.
// ⚠️ TAGGED `Transparent` AND QUEUED AT 2501 THOUGH OPAQUE (the reason `FxCloudPuffs.shader` gives): the game's ink and
// occlusion passes redraw `Opaque` things through a replacement shader that would not run this vertex program.
Shader "TumbangPreso/PayoSpirit"
{
    Properties
    {
        _Pose ("Arms, ears, mouth, mist", Vector) = (0, 0, 0.2, 0)
        _Glow ("Night glow (0 day, 1 night)", Range(0,1)) = 0
        _Line ("Ink line width (m)", Range(0,0.05)) = 0.016
        _Rim ("Water's lit edge", Color) = (0.82, 0.98, 1.0, 1)
        _Mist ("Mist edge", Color) = (0.92, 0.97, 0.98, 1)
        _Ink ("Ink line", Color) = (0.13, 0.29, 0.34, 1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float4 _Pose;
    float _Glow, _Line;
    fixed4 _Rim, _Mist, _Ink;

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; float4 uv0:TEXCOORD0; float4 uv1:TEXCOORD1; };

    float3 RotZ(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x - s * p.y, s * p.x + c * p.y, p.z); }

    float Hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }
    // One octave of value noise, big cells.
    float Noise(float3 p)
    {
        float3 i = floor(p), f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        float a = lerp(lerp(Hash(i), Hash(i + float3(1,0,0)), f.x), lerp(Hash(i + float3(0,1,0)), Hash(i + float3(1,1,0)), f.x), f.y);
        float b = lerp(lerp(Hash(i + float3(0,0,1)), Hash(i + float3(1,0,1)), f.x), lerp(Hash(i + float3(0,1,1)), Hash(i + float3(1,1,1)), f.x), f.y);
        return lerp(a, b, f.z);
    }

    // Every moving part, from the clock and the vertex's own numbers.
    void Animate(inout float3 p, inout float3 n, float4 uv0, float4 uv1)
    {
        float part = uv0.z, t = _Time.y;
        if (part < 0.5)
        {
            // The water body: a slow ripple round it, and the tail swishing (more toward its tip).
            float ripple = 0.016 * sin(p.y * 7.0 + t * 4.2) + 0.010 * sin(atan2(p.z, p.x) * 3.0 - t * 2.6);
            p += n * ripple;
            float tail = saturate((-p.y - 1.25) / 0.7);
            float a = tail * tail * (0.9 * sin(t * 3.1) + 0.3);
            float s = sin(a), c = cos(a);
            p.xz = float2(c * p.x - s * p.z, s * p.x + c * p.z);
            n.xz = float2(c * n.x - s * n.z, s * n.x + c * n.z);
            p.x += tail * tail * 0.16 * sin(t * 2.3 + 1.0);
        }
        else if (part > 2.5 && part < 3.5)
        {
            // A leaf ear, turned about where it meets the band: perked or swept, and always a little sway.
            float side = uv0.y;
            float sway = 0.12 * sin(t * 2.7 + side * 1.3) + 0.05 * sin(t * 5.3 + uv0.x * 3.0);
            float ang = side * (_Pose.y * 0.75 + sway) * (0.35 + 0.65 * uv0.x);
            float3 q = RotZ(p - uv1.xyz, ang);
            n = RotZ(n, ang);
            p = uv1.xyz + q;
            // The tip flutters on its own.
            p.y += 0.035 * uv0.x * uv0.x * sin(t * 7.0 + side * 2.0);
        }
        else if (part > 3.5 && part < 4.5)
        {
            // A little arm, raised about its shoulder.
            float ang = uv0.y * (_Pose.x * 2.0 + 0.08 * sin(t * 4.0 + uv0.y));
            float3 q = RotZ(p - uv1.xyz, ang);
            n = RotZ(n, ang);
            p = uv1.xyz + q;
        }
        else if (part > 5.5 && part < 6.5)
        {
            // The mouth, opened about its middle: a small "o" to a big gulp.
            float3 q = p - uv1.xyz;
            q.y *= lerp(0.45, 1.8, _Pose.z);
            q.x *= lerp(0.85, 1.25, _Pose.z);
            p = uv1.xyz + q;
        }
    }

    // How far a point is inside the visible spirit (under 0: eaten into mist). From the rest position, so the cut does not swim.
    float Mist(float3 rest, float wobble)
    {
        float cut = lerp(-1.72, 0.30, _Pose.w);
        float n = Noise(rest * 3.4 + float3(0, -_Time.y * 0.7, 0)) - 0.5;
        return rest.y - cut + n * 0.42 + wobble;
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="AlphaTest+51" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityLightingCommon.cginc"
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 wp:TEXCOORD1; fixed3 col:TEXCOORD2; float4 info:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                Animate(p, n, v.uv0, v.uv1);
                o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                o.pos = UnityWorldToClipPos(o.wp);
                o.wn = UnityObjectToWorldNormal(n);
                o.col = v.color.rgb;
                o.info = float4(v.vertex.xyz, v.uv0.z);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float part = i.info.w;
                float m = Mist(i.info.xyz, 0);
                clip(m);
                float3 N = normalize(i.wn), V = normalize(_WorldSpaceCameraPos - i.wp), L = normalize(_WorldSpaceLightPos0.xyz);
                float d = dot(N, L);
                float3 base = i.col;
                // Three bands: lit, a cooler middle, a cool shade. Soft steps, crisp at a distance.
                float3 mid = base * float3(0.86, 0.90, 0.97), shade = base * float3(0.68, 0.75, 0.90);
                float3 c = lerp(shade, mid, smoothstep(-0.30, -0.24, d));
                c = lerp(c, base, smoothstep(0.22, 0.28, d));
                float facing = saturate(dot(N, V));
                bool water = part < 0.5 || (part > 3.5 && part < 4.5);
                if (water)
                {
                    // The water's lit edge: one painted band, not a gradient.
                    float rim = 1.0 - facing;
                    c = lerp(c, _Rim.rgb, smoothstep(0.52, 0.58, rim) * 0.6);
                    c += _Rim.rgb * _Glow * (0.18 + 0.60 * smoothstep(0.45, 0.85, rim));
                }
                if (part > 6.5) c = float3(1, 1, 1);                         // the eye's glint
                if (part > 5.5 && part < 6.5) c = base;                      // the mouth: flat
                // The scene's light, gently: day leaves the paint alone, night cools and dims it.
                float3 amb = ShadeSH9(float4(N, 1));
                float3 lightTint = lerp(float3(1, 1, 1), saturate(_LightColor0.rgb + amb), 0.5);
                c *= lightTint;
                if (part > 1.5 && part < 2.5) c += base * (0.30 + 0.85 * _Glow);   // the inlay glows, more at night
                // The edge where it becomes mist: pale and crisp.
                c = lerp(_Mist.rgb * lerp(1.0, 0.75, _Glow), c, smoothstep(0.03, 0.06, m));
                return fixed4(c, 1);
            }
            ENDCG
        }
        // The ink line: the same animated vertices pushed out along their normals, drawn inside out.
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            struct v2f { float4 pos:SV_POSITION; float4 info:TEXCOORD0; fixed3 col:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                Animate(p, n, v.uv0, v.uv1);
                float3 wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                float3 wn = UnityObjectToWorldNormal(n);
                // Thinner on the small parts (eyes, glints, mouth), none on the glint.
                float part = v.uv0.z;
                float w = _Line * (part > 6.5 ? 0.0 : (part > 1.5 && part < 2.5) || (part > 5.5) ? 0.45 : 1.0);
                // A little wider far away, so it still reads.
                w *= clamp(distance(wp, _WorldSpaceCameraPos) / 9.0, 0.8, 2.2);
                wp += wn * w;
                o.pos = UnityWorldToClipPos(wp);
                o.info = float4(v.vertex.xyz, part);
                o.col = v.color.rgb;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(Mist(i.info.xyz, -0.05));
                // A darker tone of the part's own colour, leaning to the spirit's teal ink.
                return fixed4(lerp(_Ink.rgb, i.col * 0.35, 0.35), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
