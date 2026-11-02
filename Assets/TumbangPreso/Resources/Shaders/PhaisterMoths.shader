// PHAISTER'S MOTHS, MANY IN ONE DRAW (`PhaisterMothRibbon`, direction A of the kit rework, and the hex moth in
// `PhaisterHexLook`).
//
// ⚠️⚠️ ONE STATIC MESH, MOVED IN THE VERTEX PROGRAM. The old vanishing act (`PhaisterSwarm`) was 28 toon props each moved
// from C#, and its own film note says it "read as confetti" at 6 m. Here every moth is three cards built once (two wings
// and a body), and each card carries its moth's TYPED ROW in its texture coordinates: when it leaves, which lane of the
// ribbon it flies, how high it starts on her body and where it packs back in. The program works out where each moth is
// from one clock, `_Age`. No `_Time`, so a paused game holds it and the film can step it.
//
//   TEXCOORD0.xy  the card's own corner (0 to 1): across the wing from the hinge, and from tail to head
//   TEXCOORD1     x part (-1 left wing, 0 body, +1 right wing), y row (an index, for the wing's own erosion), z size, w beats a second
//   TEXCOORD2     x when it leaves (s), y lane sideways (m), z lane up (m), w its phase (radians)
//   TEXCOORD3     x height it leaves her body at, y height it packs back in at, z side of her body it leaves from, w 1 = a straggler
//
// The wing is painted in the fragment program from her own prop palette (`PhaisterProp`: moth wing 6A3AA8, vein 3E1F6E,
// edge C9A2F0) in three crisp value bands, a pale moon spot on each forewing, and a body of moon pale. Cut out, not
// blended, so it is a solid painted shape that holds its value on the sunlit court with no bloom.
//
// ⚠️ TAGGED `Transparent` THOUGH IT WRITES DEPTH, for `FxCloudPuffs.shader`'s reason: the game's occlusion and ink pass
// redraws the scene through a replacement shader that would not run this vertex program.
Shader "TumbangPreso/PhaisterMoths"
{
    Properties
    {
        _Age ("The effect's clock (s)", Float) = 0
        _Mode ("0 the ribbon, 1 one moth perched on a mark", Float) = 0
        _From ("Where she left (ground, world)", Vector) = (0,0,0,0)
        _To ("Where she arrives (ground, world)", Vector) = (0,0,5,0)
        _Travel ("How long one moth takes from her body to the far end (s)", Float) = 0.34
        _Perch ("Perched: where (world), w its size", Vector) = (0,2,0,1)
        _Open ("Perched: 0 asleep with its wings folded, 1 awake and beating", Range(0,1)) = 0
        _Fade ("Perched: leaving, 0 on its perch to 1 flown off and gone", Range(0,1)) = 0
        _PerchLift ("Perched: how far up the screen from the perch it sits (m)", Float) = 0
        _Wing ("Wing", Color) = (0.416,0.227,0.659,1)
        _WingDark ("Wing near the body, and its veins", Color) = (0.243,0.122,0.431,1)
        _Edge ("Wing edge", Color) = (0.788,0.635,0.941,1)
        _Moon ("Moon spot and body", Color) = (0.949,0.902,0.855,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            float _Age, _Mode, _Travel, _Open, _Fade, _PerchLift;
            float4 _From, _To, _Perch;
            fixed4 _Wing, _WingDark, _Edge, _Moon;

            struct appdata { float4 vertex:POSITION; float4 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; float4 c:TEXCOORD3; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 info:TEXCOORD1; float2 light:TEXCOORD2; };

            // Where a row's moth is at time t (world), and how big it is (0 hidden). Called twice to get its heading.
            float3 RibbonAt(float t, float4 b, float4 c, out float scale)
            {
                float3 up = float3(0,1,0);
                float3 along = _To.xyz - _From.xyz; along.y = 0;
                float len = max(0.01, length(along));
                along = len > 0.02 ? along / len : float3(0,0,1);
                float3 side = normalize(cross(up, along));
                float3 leave = _From.xyz + up * c.x + side * c.z;
                float3 land = _To.xyz + up * c.y;
                float start = 0.03 + b.x;
                float u = saturate((t - start) / _Travel);
                // Before it leaves, it is part of her: it grows out of her silhouette where it sits.
                scale = saturate((t + 0.02) / 0.07);
                float e = u * u * (3.0 - 2.0 * u);
                // One broad arc over the court, bowed to the lane's side: a ribbon, not thirty separate paths.
                float3 mid = lerp(leave, land, 0.5) + up * (0.55 + b.z + 0.10 * len) + side * b.y;
                float3 p = (1.0 - e) * (1.0 - e) * leave + 2.0 * (1.0 - e) * e * mid + e * e * land;
                // The ribbon twists clockwise about its own line, widest in the middle of the flight.
                float turn = b.w - 9.42 * u;
                p += (side * cos(turn) + up * sin(turn)) * 0.20 * sin(3.14159 * u);
                if (c.w < 0.5)
                {
                    // It packs back into her at its own height, shrinking into her body.
                    scale *= 1.0 - smoothstep(0.80, 1.0, u);
                }
                else
                {
                    // A straggler flies on, up and off her shoulder, and is eaten away (the fragment program).
                    float k = saturate((t - start - _Travel) / 0.75);
                    float o = 1.0 - (1.0 - k) * (1.0 - k);
                    p += (side * sign(b.y + 0.001) * 0.75 + up * 1.15 + along * 0.25) * o + side * sin(k * 9.0 + b.w) * 0.08 * k;
                }
                return p;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float part = v.a.x, size = v.a.z;
                float3 centre, F, U, R;
                float flap, sweep = 0.0, lift = 0.0, scale = 1.0, erode = 0.0;
                float beat = sin(_Age * v.a.w * 6.2832 + v.b.w);

                if (_Mode < 0.5)
                {
                    float s0, s1;
                    centre = RibbonAt(_Age, v.b, v.c, s0);
                    float3 ahead = RibbonAt(_Age + 0.012, v.b, v.c, s1);
                    float3 d = ahead - centre;
                    F = dot(d, d) > 1e-6 ? normalize(d) : normalize(float3(_To.x - _From.x, 0.0001, _To.z - _From.z));
                    U = float3(0,1,0);
                    U = normalize(U - F * dot(U, F) + float3(0, 1e-4, 0));
                    R = normalize(cross(U, F));
                    // Bank with the twist, so the wings show their faces through the turn rather than an edge.
                    float bank = 0.55 * sin(v.b.w - 9.42 * saturate((_Age - 0.03 - v.b.x) / _Travel));
                    float3 R2 = R * cos(bank) + U * sin(bank);
                    U = normalize(cross(F, R2)); R = R2;
                    scale = s0;
                    flap = radians(12.0 + 62.0 * (0.5 + 0.5 * beat));
                    // A straggler's wings are eaten as it flies off.
                    float start = 0.03 + v.b.x;
                    erode = v.c.w > 0.5 ? saturate((_Age - start - _Travel - 0.25) / 0.5) : 0.0;
                }
                else
                {
                    // Perched on her mark, lying on it with its back to whoever looks: head up the screen.
                    // When the mark expires it does not shred: the moth lifts off and flutters away up the screen.
                    float leave = _Fade * sqrt(_Fade);
                    centre = _Perch.xyz + UNITY_MATRIX_V[2].xyz * 0.03 + UNITY_MATRIX_V[1].xyz * _PerchLift
                           + (UNITY_MATRIX_V[1].xyz * 1.1 + UNITY_MATRIX_V[0].xyz * 0.45) * leave;
                    F = normalize(UNITY_MATRIX_V[1].xyz);
                    U = normalize(UNITY_MATRIX_V[2].xyz);
                    R = normalize(cross(U, F));
                    scale = _Perch.w;
                    // Asleep: wings folded back along the body like a tent, rising and falling slowly as it breathes.
                    // Awake: spread and beating.
                    float breathe = 0.5 + 0.5 * sin(_Age * 1.7 + v.b.w);
                    sweep = lerp(radians(74.0), radians(-8.0), _Open);
                    lift = lerp(radians(18.0 + 8.0 * breathe), 0.0, _Open);
                    flap = lerp(0.0, radians(8.0 + 60.0 * (0.5 + 0.5 * beat)), _Open);
                    erode = saturate((_Fade - 0.55) / 0.45);
                }

                // ⚠️ Nothing big against a lens: a moth within a metre of the camera shrinks away (her own eyes as she leaves).
                float near = distance(centre, _WorldSpaceCameraPos);
                scale *= smoothstep(0.55, 1.35, near) * size;

                float3 world;
                if (abs(part) < 0.5)
                {
                    // The body: a short fat card along the moth, a hair above the wings.
                    world = centre + (R * (v.uv.x - 0.5) * 0.26 + F * (v.uv.y - 0.5) * 0.40 + U * 0.012) * scale;
                }
                else
                {
                    float sd = sign(part);
                    float3 span = R * sd * cos(sweep) - F * sin(sweep);
                    float3 fore = F * cos(sweep) + R * sd * sin(sweep);
                    float a = flap + lift;
                    float3 out_ = span * cos(a) + U * sin(a);
                    world = centre + (out_ * v.uv.x * 0.44 + fore * (v.uv.y - 0.5) * 0.54) * scale;
                }
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                if (scale < 0.002) o.pos = float4(0, 0, -2, 1);
                o.uv = v.uv.xy;
                o.info = float3(part, v.a.y, erode);
                // The wing's top catches the sun when it lies flat and darkens as it stands on edge: two tones, no gradient.
                // Which side the camera sees comes from the moth's own up, not the triangle's winding, which the mirrored
                // wings flip.
                o.light = float2(saturate(dot(U, normalize(float3(-0.35, 0.85, -0.25)))), step(0.0, dot(U, _WorldSpaceCameraPos - centre)));
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x), lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
            }
            float Ellipse(float2 p, float2 c, float2 r, float turn)
            {
                float2 d = p - c;
                float cs = cos(turn), sn = sin(turn);
                d = float2(cs * d.x - sn * d.y, sn * d.x + cs * d.y);
                return (length(d / r) - 1.0) * min(r.x, r.y);
            }

            // ⚠️ SEE-THROUGH, WITH A LIT EDGE (round 3: cut-out wings in flat bands read as paper clouds, the "cartoony sticker"
            // the owner turned down). Castorice's wing (research.md section 3): a dark membrane you can see the world through,
            // darker veins, an edge that carries light, a glowing body. Premultiplied: the membrane covers a little, the edge
            // and body add light. Every edge is one pixel soft (`fwidth`), never stair-stepped.
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv;
                float part = i.info.x;
                float lit = i.light.y > 0.5 ? lerp(0.85, 1.0, step(0.45, i.light.x)) : 0.75;
                if (abs(part) < 0.5)
                {
                    // Body: a fat moon-pale grub and two feathered antennae, which make it read as a MOTH.
                    float d = Ellipse(p, float2(0.5, 0.40), float2(0.19, 0.38), 0.0);
                    float al = Ellipse(p, float2(0.31, 0.86), float2(0.08, 0.15), -0.55);
                    float ar = Ellipse(p, float2(0.69, 0.86), float2(0.08, 0.15), 0.55);
                    float ant = min(al, ar);
                    float dd = min(d, ant);
                    float cover = saturate(-dd / max(fwidth(dd), 1e-4));
                    if (cover <= 0.0) discard;
                    fixed3 c = d < ant ? lerp(_Moon.rgb * 1.1, _Edge.rgb, smoothstep(-0.07, -0.02, d)) : _Edge.rgb * 0.9;
                    return fixed4(c * cover, cover);
                }
                // A forewing swept forward and out, a rounder hindwing behind it, joined at the hinge.
                float fore = Ellipse(p, float2(0.50, 0.66), float2(0.52, 0.30), 0.42);
                float hind = Ellipse(p, float2(0.36, 0.30), float2(0.38, 0.27), -0.30);
                float k = 0.06;
                float h = saturate(0.5 + 0.5 * (hind - fore) / k);
                float d = lerp(hind, fore, h) - k * h * (1.0 - h);
                d += (Noise(p * 6.0 + i.info.y * 3.1) - 0.5) * 0.03;
                float cover = saturate(-d / max(fwidth(d), 1e-4));
                // Eaten away with its own noise when it dies, edges soft.
                if (i.info.z > 0.0) cover *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                if (cover <= 0.0) discard;

                float2 hinge = float2(0.0, 0.48);
                float out01 = saturate(length(p - hinge) / 0.95);
                // The membrane: dark at the root, violet further out, see-through all over.
                fixed3 c = lerp(_WingDark.rgb, _Wing.rgb, smoothstep(0.28, 0.42, out01));
                float a = lerp(0.72, 0.50, smoothstep(0.2, 0.8, out01));
                float ang = atan2(p.y - hinge.y, p.x + 0.02);
                float vein = abs(frac(ang * 2.6 + 0.5) - 0.5) * length(p - hinge);
                float veins = smoothstep(0.022, 0.012, vein) * step(0.12, out01) * smoothstep(-0.04, -0.07, d);
                c = lerp(c, _WingDark.rgb * 0.7, veins); a = lerp(a, 0.85, veins);
                // The lit edge: a band of lilac light along the rim, brightest at the tip.
                float edge = smoothstep(-0.075, -0.035, d);
                c = lerp(c, _Edge.rgb * (1.0 + 0.3 * out01), edge); a = lerp(a, 0.95, edge);
                // A crescent moon on the forewing, her mark, out toward the tip.
                float moon = length(p - float2(0.78, 0.74)), bite = length(p - float2(0.81, 0.77));
                float spot = smoothstep(0.066, 0.058, moon) * smoothstep(0.042, 0.05, bite);
                c = lerp(c, _Moon.rgb * 1.1, spot); a = lerp(a, 1.0, spot);
                a *= cover;
                // Premultiplied; the edge and the moon add a little more light than they cover.
                return fixed4(c * lit * a * (1.0 + 0.35 * max(edge, spot)), a);
            }
            ENDCG
        }
    }
}
