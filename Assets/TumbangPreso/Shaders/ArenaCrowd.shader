// The Arena's crowd: tens of thousands of spectators, each ONE QUAD of a few static meshes, each
// showing a cell of one atlas of the game's own people (tools/author_arena_crowd.py renders it,
// tools/arena_crowd_atlas.json describes it, `ArenaCrowd` builds the meshes).
//
// EVERYTHING A SPECTATOR DOES IS DECIDED HERE, PER VERTEX, FROM FOUR THINGS: the seat's own
// random numbers (baked into the mesh), the time, and the globals `ArenaCrowd` sets once a frame:
//
//     _ArenaCrowdExcitement   0 calm (seated, the odd clap) to 1 (everybody on their feet)
//     _ArenaCrowdGroan        0 to 1: the share of the seated who put their hands to their heads
//     _ArenaCrowdWave         x: where the wave is, 0 to 1 round the bowl (a bearing / 360)
//                             y: its half width in the same unit   z: its strength, 0 when there is none
//
// So the whole crowd reacts with no per-object cost: no script touches a seat, no material is
// duplicated, and the meshes never change.
//
// ⚠️ THE MESH IS NOT A MODEL. A vertex's POSITION is its seat (all four corners of a quad carry
// the same point), and the corner is in TEXCOORD0.xy. The quad is opened here, about the vertical
// axis, toward the camera. So the mesh's own bounds are set by hand in `ArenaCrowd` (a seat cloud
// has no height), and this shader must not be put on anything else.
//
//     POSITION    the seat: the row's floor under the spectator, object space
//     COLOR       rgb: this seat's tint (a small palette)   a: which glow colour, 0..3 in thirds
//     TEXCOORD0   x, y: the corner, 0 or 1   z: the seat's phase, 0..1   w: its bearing / 360
//     TEXCOORD1   x: the person (atlas column) / 255   y: 0 nothing held, 0.5 a light stick or
//                 phone, 1 a flag   z: how hard this seat is to excite, 0..1   w: its size
//
// ⚠️ THE LOOP TABLE BELOW IS THE ATLAS'S ROWS and `ArenaCrowd.Loops`: idle 2 frames, clap 2,
// cheer 4, jump 4, stick 4, stick_up 4, flag 4, groan 4. `ArenaCrowdBuilder` refuses an atlas
// whose layout file says otherwise.
//
// ⚠️ ALPHA TEST, NOT BLEND, AND NO SHADOWS. A blended crowd would have to be sorted, and it
// cannot be: the quads of one mesh are in seat order. Tested, it writes depth and needs no
// order. There is no shadow caster pass on purpose: the stand is lit by emission and the look
// profile, and 25,000 quads in the shadow map would buy nothing. `Fallback Off` keeps one from
// being borrowed.
//
// ⚠️ THE NIGHT TINT IS APPLIED IN DISPLAY (GAMMA) SPACE, because that is where the kit's mock
// stand (Logs/arena/crowd) chose it: a multiply in linear space by the same numbers is far
// darker. The project is linear, so the sample is taken to gamma, tinted, and taken back.
//
// NOT COMPILED BY THE KIT'S AUTHOR (shaders do not compile outside Unity). Written against the
// built-in pipeline's UnityCG.cginc, in the manner of VfxFlipbook.shader.
Shader "TumbangPreso/ArenaCrowd"
{
    Properties
    {
        _MainTex ("Atlas (rgb the people, a the cutout)", 2D) = "white" {}
        _EmitTex ("What glows (r)", 2D) = "black" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _MipBias ("Mip Bias (negative is sharper)", Range(-2, 1)) = -0.5

        // x, y: one cell in UV (48 / 2048, 72 / 2048). z: how many people (columns).
        _Grid ("Cell U, Cell V, Columns", Vector) = (0.0234375, 0.03515625, 42, 0)
        // x, y: what a cell covers in metres. z: how far the row's floor sits above the cell's
        // bottom edge. w: how high a body on its feet bobs at full excitement.
        _CellMetres ("Cell Width, Height, Ground, Hop", Vector) = (1.8, 2.7, 0.2, 0.3)

        // Rule 8 of docs/ARENA_ART_BRIEF.md: the stand is darker and lower in contrast than the
        // stage. The bodies are dimmed and greyed toward navy; the light comes from what they hold.
        _NightTint ("Night Tint (display space)", Color) = (0.52, 0.56, 0.74, 1)
        _NightDesaturate ("Night Desaturate", Range(0, 1)) = 0.2
        _ExciteLift ("Brightness Added At Full Excitement", Range(0, 1)) = 0.12

        _EmitStrength ("Glow Strength", Range(0, 8)) = 2.4
        _Glow0 ("Glow: white", Color) = (0.94, 0.97, 1.0, 1)
        _Glow1 ("Glow: ice", Color) = (0.62, 0.90, 1.0, 1)
        _Glow2 ("Glow: deep LED blue", Color) = (0.30, 0.40, 1.0, 1)
        _Glow3 ("Glow: magenta, rare", Color) = (1.0, 0.42, 0.80, 1)

        // The share of seats whose phone flashes in any eighth of a second, at full excitement.
        _FlashRate ("Camera Flash Rate", Range(0, 0.3)) = 0.05
        _FlashStrength ("Camera Flash Strength", Range(0, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }

        // A billboard has no back: whichever way the quad winds once it is turned, draw it.
        Cull Off
        Lighting Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                // xy: the corner, for the flash. z: the flash, 0 or 1. w: glow gain.
                float4 cell : TEXCOORD1;
                fixed3 tint : TEXCOORD2;
                fixed3 glow : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            sampler2D _MainTex;
            sampler2D _EmitTex;
            half _Cutoff;
            half _MipBias;
            float4 _Grid;
            float4 _CellMetres;
            fixed4 _NightTint;
            half _NightDesaturate;
            half _ExciteLift;
            half _EmitStrength;
            fixed4 _Glow0, _Glow1, _Glow2, _Glow3;
            half _FlashRate;
            half _FlashStrength;

            // Set by ArenaCrowd, once a frame, for every crowd material at once.
            float _ArenaCrowdExcitement;
            float _ArenaCrowdGroan;
            float4 _ArenaCrowdWave;

            // The loops, in the atlas's row order: idle, clap, cheer, jump, stick, stick_up, flag, groan.
            #define LOOP_IDLE 0
            #define LOOP_CLAP 1
            #define LOOP_CHEER 2
            #define LOOP_JUMP 3
            #define LOOP_STICK 4
            #define LOOP_STICK_UP 5
            #define LOOP_FLAG 6
            #define LOOP_GROAN 7
            static const float LoopStart[8] = { 0, 2, 4, 8, 12, 16, 20, 24 };
            static const float LoopCount[8] = { 2, 2, 4, 4, 4, 4, 4, 4 };
            static const float LoopFpsCalm[8] = { 1.2, 5.0, 7.0, 7.0, 4.0, 7.0, 5.0, 3.0 };
            static const float LoopFpsExcited[8] = { 2.0, 8.0, 10.0, 9.0, 6.0, 10.0, 8.0, 4.0 };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;

                float2 corner = v.uv0.xy;
                float phase = v.uv0.z;
                float bearing = v.uv0.w;
                float person = floor(v.uv1.x * 255.0 + 0.5);
                float held = v.uv1.y;
                float reluctance = v.uv1.z;
                float size = 0.75 + v.uv1.w * 0.75;
                float second = frac(phase * 7.31 + reluctance * 3.77);

                // How excited THIS seat is: the crowd's level, or the wave passing its bearing.
                float away = abs(frac(bearing - _ArenaCrowdWave.x + 0.5) - 0.5);
                float wave = _ArenaCrowdWave.z * (1.0 - smoothstep(0.0, max(_ArenaCrowdWave.y, 1e-4), away));
                float excite = saturate(max(_ArenaCrowdExcitement, wave));
                // A slow swell that travels round the bowl, so a calm crowd claps in pockets.
                float swell = 0.5 + 0.5 * sin(bearing * 75.0 + _Time.y * 0.35 + second * 2.0);

                // Which loop. A flag is always waved; a stick is held up once its holder is
                // roused; the rest groan, or get to their feet, or clap, or sit.
                int loop = LOOP_IDLE;
                if (held > 0.75) loop = LOOP_FLAG;
                else if (held > 0.25) loop = excite > 0.15 + reluctance * 0.6 ? LOOP_STICK_UP : LOOP_STICK;
                else if (_ArenaCrowdGroan > reluctance) loop = LOOP_GROAN;
                else if (excite > 0.20 + reluctance * 0.65) loop = second < 0.45 ? LOOP_JUMP : LOOP_CHEER;
                else if (excite + 0.16 * swell > 0.10 + reluctance * 0.9) loop = LOOP_CLAP;

                float count = LoopCount[loop];
                float fps = lerp(LoopFpsCalm[loop], LoopFpsExcited[loop], excite);
                float frame = floor(frac(_Time.y * fps / count + phase) * count);
                float row = LoopStart[loop] + min(frame, count - 1.0);

                // A body on its feet bobs. The jump's own lift is drawn in its frames; this is
                // the part a still frame cannot carry, and it is what reads from 85 m.
                float standing = (loop == LOOP_CHEER || loop == LOOP_JUMP || loop == LOOP_STICK_UP) ? 1.0 : 0.0;
                float hop = standing * abs(sin(_Time.y * 7.0 + phase * 6.2831853)) * _CellMetres.w * excite;

                // Open the quad about the vertical axis, toward the camera.
                float3 seat = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                float3 toCamera = _WorldSpaceCameraPos - seat;
                float2 flat = toCamera.xz;
                float flatLength = max(length(flat), 1e-4);
                float3 right = float3(-flat.y, 0.0, flat.x) / flatLength;
                float3 world = seat
                    + right * ((corner.x - 0.5) * _CellMetres.x * size)
                    + float3(0.0, (corner.y * _CellMetres.y - _CellMetres.z) * size + hop, 0.0);
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));

                // The cell: a person is a column, a frame is a row, and row 0 is the TOP of the
                // picture (v = 1).
                o.uv = float2((person + corner.x) * _Grid.x, 1.0 - (row + 1.0 - corner.y) * _Grid.y);

                // A phone's flash: this seat, this eighth of a second.
                float tick = floor(_Time.y * 8.0);
                float flash = Hash(float2(phase * 91.7 + reluctance * 13.1, tick * 0.137 + bearing * 57.0)) < _FlashRate * excite ? 1.0 : 0.0;
                o.cell = float4(corner, flash, 1.0 + 0.5 * excite);

                o.tint = v.color.rgb * (1.0 + _ExciteLift * excite);
                float pick = v.color.a * 3.0;
                o.glow = pick < 0.5 ? _Glow0.rgb : pick < 1.5 ? _Glow1.rgb : pick < 2.5 ? _Glow2.rgb : _Glow3.rgb;

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2Dbias(_MainTex, float4(i.uv, 0.0, _MipBias));
                clip(c.a - _Cutoff);
                half emit = tex2Dbias(_EmitTex, float4(i.uv, 0.0, _MipBias)).r;

                // The body, dimmed for night in display space (see the note at the top).
                #ifdef UNITY_COLORSPACE_GAMMA
                    half3 body = c.rgb;
                #else
                    half3 body = LinearToGammaSpace(c.rgb);
                #endif
                half grey = dot(body, half3(0.30h, 0.59h, 0.11h));
                body = lerp(body, grey.xxx, _NightDesaturate) * _NightTint.rgb * i.tint;
                #ifndef UNITY_COLORSPACE_GAMMA
                    body = GammaToLinearSpace(body);
                    half3 glow = GammaToLinearSpace(i.glow);
                #else
                    half3 glow = i.glow;
                #endif

                // What it holds glows in the seat's own colour, and is not dimmed.
                half3 colour = lerp(body, glow * _EmitStrength * i.cell.w, emit);

                // The flash: a small bright block at head height, only where the body is.
                half2 d = abs(i.cell.xy - half2(0.5h, 0.58h));
                half inFlash = i.cell.z * step(d.x, 0.045h) * step(d.y, 0.03h);
                colour = lerp(colour, _FlashStrength.xxx, inFlash);

                half4 result = half4(colour, 1.0);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }

    Fallback Off
}
