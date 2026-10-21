// § THE RECALL BEAM'S ONE PASS. The line of light standing on your own tsinelas while it lies in
// the road, in the colour you picked for the slipper highlight.
//
// Request, 2026-09-30, over a first-person frame of the 0.48 m locator: "overhaul the highlight
// beam when a tsinelas is thrown. it should look similar to the highlight beam of the dropped
// items in apex legends ... it should be noticeable but not too distracting."
//
// ⚠️⚠️ WHAT THE REFERENCE ACTUALLY IS, BECAUSE THE TWO EARLIER BUILDS OF THIS FILE WERE BOTH
// SOMETHING ELSE. A dropped item in that game stands under a THIN vertical line of light: a
// near-white core a few centimetres wide, a soft haze in the rarity colour that hugs the bottom,
// no visible top (it thins into nothing) and no silhouette (it has no walls, so there is nothing
// to read as an object). The item itself carries a rim in the same colour, which this game
// already has as the landed rim. Two things follow from that picture and this shader is built on
// both:
//
//   * **It is a camera-facing ribbon, not a cylinder.** The 2026-09-20 build was a hollow cone
//     with bright walls (`_Rim`), which is a Fortnite column: wide, sculpted, and something you
//     see AS a shape. Its 2.2 m version hid the can, the feet and the chase behind half a metre of
//     glowing tube, and 3694d67c cut it to a 0.48 m locator for exactly that reason, which then
//     stopped being findable from across the street. A line that always faces the camera has the
//     same few centimetres of core from every angle, so it can be tall enough to be a landmark and
//     still cover almost nothing.
//   * **Its width is pinned in PIXELS at range and in METRES up close.** `_Widen` is the floor: at
//     distance the ribbon stops narrowing, so the core never drops under about a pixel and
//     shimmers out. The haze is divided by the same factor, so a far beam is a crisp line rather
//     than a growing smear. This is the half of the reference that makes it "noticeable": it reads
//     at any range. The near fade in `SlipperBeam` is the half that makes it "not distracting":
//     it is gone before you are standing in it.
//
// ⚠️ STILL ONE PASS, NO TEXTURE, NO DEPTH TEXTURE, and still additive (`Blend One One`, with the
// fragment returning premultiplied light), so it only ever brightens the street. The streak
// field is gone: a thin core with slow rising sparkles is the reference's motion, and banded
// streaks round a column were a Fortnite detail that only worked on a wide one.
//
// ⚠️⚠️ IT MUST BE IN `GameBuilder.EnsureRuntimeShaders` AND IT IS. `VfxMaterial.Beam` reaches it
// through `Shader.Find` and nothing in any scene references it. Its miss path is a flat `Ghost`
// cylinder, which the editor would never show and the player would ship.
Shader "TumbangPreso/SlipperBeam"
{
    Properties
    {
        _Color ("Beam colour", Color) = (0.30,0.62,1.00,1)

        // The whole effect's strength, written every frame by `SlipperBeam.Paint`: the near-fade
        // as the grab comes into reach, times a small breath. Zero is invisible rather than absent,
        // so the component still switches the object off at the bottom of the fade.
        _Strength ("Strength", Range(0,2)) = 1

        // ⚠️ 0 IS THE RIBBON AND 1 IS THE POOL ON THE ROAD, in one shader on purpose. They are the
        // same light seen two ways and they must fade together.
        _Mode ("0 ribbon, 1 ground pool", Float) = 0

        // ⚠️ WORLD METRES, NOT OBJECT SCALE. The ribbon rebuilds its own corners from the object's
        // ORIGIN (the road under the shoe) in the vertex shader, so the slipper's scale, which
        // differs per skin, can never stretch it.
        _Height ("Ribbon height, metres", Float) = 1.9
        _Width ("Half width of the haze, metres", Float) = 0.14

        // How wide the ribbon is allowed to be per metre of distance, as a floor. 0.009 keeps the
        // core at roughly a pixel at 720p out to the far end of the longest arena.
        // ⚠️ 0.014, MEASURED AGAINST `beam-far` v1. At 0.009 the line was gone at 18 m: the core
        // held its pixel but a one-pixel line of that brightness does not read over asphalt.
        _Widen ("Minimum half width per metre of distance", Float) = 0.014

        // The core as a fraction of the half width. Small: this is the line the eye finds.
        _Core ("Core width, fraction of half width", Range(0.02,0.5)) = 0.13

        _CoreAlpha ("Core brightness", Range(0,2)) = 1.1
        _Alpha ("Haze brightness", Range(0,1)) = 0.42

        // The core runs nearly the full height; the haze is gone by about half way. That split is
        // what makes it a line rising out of a glow rather than a glowing post. v1's 1.15 had the
        // core visibly finished by about 1.2 m in `beam-witness`; 0.8 carries it to the tip.
        _CoreFalloff ("Core vertical falloff power", Range(0.5,6)) = 0.8
        _HazeFalloff ("Haze vertical falloff power", Range(0.5,8)) = 2.6

        // How white the core goes. Light that is one saturated hue from end to end reads as a
        // coloured object; a hot core is what says it is emitting.
        _Hot ("White-hot core", Range(0,1)) = 0.55

        _Scroll ("Sparkle climb, metres a second", Range(0,4)) = 0.55
        _Sparkle ("How much the sparkles lift the core", Range(0,1)) = 0.35
    }

    SubShader
    {
        // ⚠️ `Queue` IS TRANSPARENT+1 SO THE RIBBON DRAWS OVER THE POOL IT STANDS IN.
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }

        // `Cull Off` because the ribbon is one quad and must read from both sides while the
        // billboard turns. `ZWrite Off` because light that occludes the fight behind it is a wall.
        // `Blend One One` because light adds; the fragment premultiplies its own alpha.
        Cull Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #include "../Resources/Shaders/RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Strength, _Mode, _Height, _Width, _Widen, _Core;
            float _CoreAlpha, _Alpha, _CoreFalloff, _HazeFalloff, _Hot, _Scroll, _Sparkle;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                // Ribbon: x -1..1 across, y 0 road .. 1 tip. Pool: object xz scaled to the unit disc.
                float2 uv     : TEXCOORD0;
                // How much the ribbon was widened past `_Width`, so the haze can be dimmed by it.
                float  widen  : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                if (_Mode > 0.5)
                {
                    // The pool: a flat disc, drawn where the mesh puts it. A Unity cylinder has a
                    // radius of 0.5 in object space, so twice the xz length is 0..1 at the rim.
                    // The xz position is passed and its LENGTH taken per pixel: a length taken per
                    // vertex interpolates as straight lines between the rim vertices and draws a
                    // polygon rather than a circle.
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.vertex.xz * 2.0;
                    o.widen = 1.0;
                    return o;
                }

                // ⚠️⚠️ A CYLINDRICAL BILLBOARD, BUILT FROM THE ORIGIN. The quad's x (-1..1) is
                // swung to face the camera round the vertical only, so the line stays upright when
                // the player looks down at it from first person, which a full billboard would tip
                // toward the camera and turn into a smear across the road.
                float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 toCam = _WorldSpaceCameraPos - origin;
                float3 flat = float3(toCam.x, 0.0, toCam.z);
                float flatLen = length(flat);
                float3 right = flatLen > 1e-4 ? cross(float3(0, 1, 0), flat / flatLen) : float3(1, 0, 0);

                float halfWidth = max(_Width, length(toCam) * _Widen);

                float3 world = origin
                             + right * (v.vertex.x * halfWidth)
                             + float3(0.0, v.vertex.y * _Height, 0.0);

                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = float2(v.vertex.x, saturate(v.vertex.y));
                o.widen = halfWidth / max(_Width, 1e-4);
                return o;
            }

            float hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Mode > 0.5)
                {
                    // A soft round glow on the road, strongest under the shoe and gone at the rim.
                    float r = saturate(length(i.uv));
                    float pool = pow(1.0 - r, 2.2) * _Alpha * _Strength;
                    return fixed4(saturate(_Color.rgb * pool), 1.0);
                }

                float x = i.uv.x;
                float h = i.uv.y;

                // Across the ribbon. The haze reaches exactly zero at the quad's edge, so the
                // rectangle is never visible; the core is a narrow gaussian inside it.
                float haze = pow(saturate(1.0 - x * x), 3.0);
                float core = exp(-(x * x) / (_Core * _Core));

                // Up the ribbon. A few centimetres of fade-in at the road so the light grows out of
                // the ground under the shoe instead of starting on a hard line, then two different
                // falloffs: the core runs up, the haze stays low.
                float root = smoothstep(0.0, 0.025, h);
                float coreUp = pow(1.0 - h, _CoreFalloff) * root;
                float hazeUp = pow(1.0 - h, _HazeFalloff) * root;

                // ⚠️ SLOW RISING SPARKLES ON THE CORE ONLY. Three soft pulses a metre climbing at
                // `_Scroll`, each with its own phase, so the line reads as light moving up rather
                // than a lit rod. Kept shallow: a beacon that flickers pulls the eye off the fight.
                float metres = h * _Height;
                float cell = floor(metres * 3.0 - TumpShaderTime() * _Scroll * 3.0);
                float local = frac(metres * 3.0 - TumpShaderTime() * _Scroll * 3.0);
                float spark = smoothstep(0.0, 0.5, local) * smoothstep(1.0, 0.5, local);
                spark *= step(0.45, hash11(cell));
                float lift = 1.0 + _Sparkle * spark;

                // At range the ribbon was widened to keep the core a pixel wide; the haze spread
                // over that wider quad is dimmed so a far beam stays a line. By the square root
                // rather than the whole factor: dimming it fully left nothing around the core to
                // catch the eye at range (`beam-far` v1).
                float hazeA = _Alpha * haze * hazeUp / sqrt(i.widen);
                float coreA = _CoreAlpha * core * coreUp * lift;

                float3 coreCol = lerp(_Color.rgb, float3(1, 1, 1), _Hot);
                float3 light = (_Color.rgb * hazeA + coreCol * coreA) * _Strength;

                // ⚠️ CLAMPED, AND `AbilityShowcaseProbe` IS THE NUMBER BEHIND IT: an additive blend
                // with no ceiling is exactly how an effect blows a frame to white.
                return fixed4(saturate(light), 1.0);
            }
            ENDCG
        }
    }

    // ⚠️ NO FALLBACK LINE ON PURPOSE. `VfxMaterial.Beam` handles the miss in code, where it can
    // warn, rather than silently drawing an opaque lit cylinder.
}
