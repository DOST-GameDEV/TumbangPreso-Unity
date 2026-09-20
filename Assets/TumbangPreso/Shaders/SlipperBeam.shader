// § THE RECALL BEAM'S ONE PASS. The column of light standing on your own tsinelas while it lies
// in the road, in the colour you picked for the slipper highlight.
//
// 🧑 2026-09-20, with a frame of a Fortnite loot beam: *"create a beam coming off of a slipper
// that is thrown off the ground. this will be similar to the items on ground in fortnite. make
// this a shader not a model."*
//
// ⚠️⚠️ THE MODEL VERSION IS WHY THIS EXISTS, AND ITS OWN NOTES RECORD THE FAULT TWICE. It stacked
// six primitive cylinders at six alphas to fake a taper, after its FIRST build was one cylinder
// and, in that commit's words, *"rendered as a length of plastic pipe"*. The stack was an attempt
// to answer that in geometry; 🧑 asked for a shader instead. ⚠️ **The stacked render was not seen
// from here** and this note does not claim otherwise: the commit was reverted whole before this
// work started. What WAS seen here is `beam-witness` v1 of THIS shader, which had a constant
// width, and it made the same mistake one level up. **Geometry can only ever give a column an
// EDGE**, and every property that makes a beam read as light rather than as plastic is a
// per-pixel one:
//
//   * **The taper is continuous.** `_Falloff` is a curve over the fragment's own height, so there
//     is no step anywhere and no top edge at all: the column stops being there rather than
//     stopping. Six segments could not do that at any count without paying six draws for it.
//   * **The edges are brighter than the middle.** That is the whole reason a shaft of light looks
//     hollow rather than solid, and it is a view-dependent term (`_Rim`): it moves as the player
//     walks round it, which no baked alpha can.
//   * **It never darkens what is behind it.** `Blend SrcAlpha One` adds light. The mesh version
//     was alpha-blended `Standard`, so the column was subtracting the street's own colour before
//     adding its own, which is exactly why it read as tinted plastic over the road.
//   * **It moves without moving anything.** The rising motes in the mesh version were three cube
//     transforms animated from `Update`; here they are `_Scroll` on a procedural streak field,
//     which costs no objects, no per-frame C# and no allocation.
//
// ⚠️ ONE PASS, NO TEXTURE, NO DEPTH TEXTURE. Two of the three arenas are built under things and
// the third is a lit street, so this draws in every scene with no camera setup and no dependency
// on the depth prepass. The noise is a cheap hash rather than a sampled sheet, which also means
// it cannot be stripped out from under the effect the way `VfxFlipbook`'s sheets can.
//
// ⚠️⚠️ IT MUST BE IN `GameBuilder.EnsureRuntimeShaders` AND IT IS. `VfxMaterial.Beam` reaches it
// through `Shader.Find` and nothing in any scene references it, which is exactly the case that
// list exists for. Its miss path falls back to `Ghost`, which is the flat plastic look above: the
// editor would be correct and the .exe would ship the rejected version, with one warning in a log
// nobody reads during a playtest.
Shader "TumbangPreso/SlipperBeam"
{
    Properties
    {
        _Color ("Beam colour", Color) = (0.30,0.62,1.00,1)

        // The whole effect's strength, written every frame by `SlipperBeam.Paint`: the near-fade
        // as the grab comes into reach, times the pulse. Zero is invisible rather than absent, so
        // the component still switches the object off at the bottom of the fade.
        _Strength ("Strength", Range(0,2)) = 1

        // ⚠️ 0 IS THE COLUMN AND 1 IS THE POOL ON THE ROAD, in one shader on purpose. They are the
        // same light seen two ways and they must fade together; two shaders would be two places
        // to forget, which is `HeroAbility.Glyph`'s argument one system down.
        _Mode ("0 column, 1 ground pool", Float) = 0

        _Falloff ("Vertical falloff power", Range(0.5,6)) = 1.35
        _Rim ("Edge brightening power", Range(0.5,8)) = 3.1
        _Core ("Core brightness", Range(0,2)) = 0.16

        // ⚠️⚠️ THE WIDTH TAPER IS IN THE VERTEX SHADER AND THE FIRST SHADER BUILD HAD NO SUCH
        // THING. `beam-witness` v1 is the receipt: a column of constant width, fading on alpha
        // alone, still reads as a CUT PIPE, because the two sides of the barrel stay parallel all
        // the way up and the near and far walls converge into a bright ellipse where it ends. An
        // alpha curve cannot remove a silhouette; only moving the vertices can. This is the one
        // thing the mesh version was right about and it costs nothing to do properly: the column
        // is a cone whose walls close to `_Tip` of their base width, so there is no top edge to
        // see from any angle.
        _Tip ("Width at the top, as a fraction of the base", Range(0.02,1)) = 0.46

        // How much of the light goes white as it leaves the ground. Light that is one saturated
        // hue from end to end is a coloured object; a hot base is what says it is emitting.
        _Hot ("White-hot base", Range(0,1)) = 0.34

        _Streaks ("Streak count round the column", Range(1,24)) = 7
        _StreakDepth ("How much the streaks bite", Range(0,1)) = 0.45
        _Scroll ("Streak climb, metres a second", Range(0,4)) = 0.9

        _Alpha ("Peak alpha", Range(0,1)) = 0.34
    }

    SubShader
    {
        // ⚠️ `Queue` IS TRANSPARENT+1 SO THE COLUMN DRAWS OVER THE POOL IT STANDS IN, whatever
        // order the two renderers were created in. `SlipperBeam` builds the pool first for the
        // same reason and this is what makes that not matter.
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }

        // ⚠️⚠️ THE THREE STATES BELOW ARE THE EFFECT. `Cull Off` because a player walks THROUGH
        // this column in a first-person game and a single-sided shaft disappears from the inside.
        // `ZWrite Off` because a light column that occludes the fight behind it is a wall.
        // `Blend SrcAlpha One` because light adds: see the class note above for what the mesh
        // version's alpha blend did to the road.
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Strength, _Mode, _Falloff, _Rim, _Core, _Tip, _Hot;
            float _Streaks, _StreakDepth, _Scroll, _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 obj    : TEXCOORD0;
                float3 world  : TEXCOORD1;
                float3 wnormal: TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // ⚠️⚠️ THE HEIGHT IS READ FROM OBJECT SPACE, NOT FROM UVs, AND THAT IS NOT A
                // PREFERENCE. A Unity primitive cylinder's side UVs wrap once round the barrel and
                // its two caps carry a disc mapping, so a `uv.y` taper would put a bright ring on
                // the top cap and a seam down the side. Object Y is continuous over every vertex
                // of every mesh this could ever be put on.
                float h = saturate(v.vertex.y * 0.5 + 0.5);

                // ⚠️ THE COLUMN IS NARROWED INTO A CONE HERE. See `_Tip`: this is what removes the
                // top edge, and it is a lerp rather than a multiply by `1-h` so the shaft keeps
                // its width near the ground where it is doing its job. The pool is flat and must
                // keep its radius, hence the `_Mode` guard.
                float squeeze = lerp(lerp(1.0, _Tip, h), 1.0, _Mode);
                float4 shaped = v.vertex;
                shaped.xz *= squeeze;

                o.vertex = UnityObjectToClipPos(shaped);

                // ⚠️ THE FRAGMENT READS THE UNSQUEEZED POSITION, so the height and the angle round
                // the column are the same numbers whatever the taper does to the mesh. Reading the
                // shaped one would make the streaks pinch with the walls.
                o.obj = v.vertex.xyz;
                o.world = mul(unity_ObjectToWorld, shaped).xyz;

                // The normal has to lean with the wall or the edge brightening sits in the wrong
                // place on a cone. The lean is the wall's own slope, which is the base radius
                // minus the tip radius over the height.
                float3 n = v.normal;
                n.y += lerp((1.0 - _Tip) * 0.5, 0.0, _Mode) * length(v.normal.xz);
                o.wnormal = UnityObjectToWorldNormal(normalize(n));
                return o;
            }

            // A cheap deterministic hash, so the streaks differ round the column without a texture
            // and without a random the CPU has to feed in.
            float hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // A Unity cylinder spans -1..1 in object Y, so this is 0 at the road and 1 at the
                // tip. `saturate` covers any other mesh a caller hands it rather than trusting it.
                float h = saturate(i.obj.y * 0.5 + 0.5);

                // The pool is the same falloff read outward instead of upward. Its quad spans
                // -0.5..0.5, so twice the radius is the 0..1 the column's height already is.
                float r = saturate(length(i.obj.xz) * 2.0);
                float t = lerp(h, r, _Mode);

                // The taper. Raised to a power so most of the fade is in the last third: a linear
                // one still ends on a visible edge, which is the mesh version's top step.
                float body = pow(1.0 - t, _Falloff);

                // ⚠️ THE EDGES ARE BRIGHTER THAN THE MIDDLE, WHICH IS WHAT MAKES A SHAFT READ AS
                // HOLLOW. `abs` rather than a clamp because `Cull Off` draws the inside too, and
                // an inward-facing normal would otherwise read as facing away and go black.
                float3 view = normalize(UnityWorldSpaceViewDir(i.world));
                float facing = abs(dot(normalize(i.wnormal), view));
                float rim = pow(1.0 - facing, _Rim);

                // The pool is flat on the ground and its normal points at the sky, so a rim term
                // there is a function of where the player is standing rather than of the shape.
                // It gets the flat core instead.
                float shape = lerp(saturate(rim + _Core * body), 1.0, _Mode);

                // The streaks, climbing. `atan2` round the column, scrolled on height, so the
                // pattern travels up rather than round: a column that spins reads as a machine.
                float angle = atan2(i.obj.z, i.obj.x) / 6.2831853 + 0.5;
                float band = floor(angle * _Streaks);
                float phase = hash11(band);
                float climb = frac(h * 1.35 - _Time.y * _Scroll * 0.25 + phase);

                // One soft pulse per band, so the streaks are separated lights rather than stripes.
                float streak = smoothstep(0.0, 0.35, climb) * smoothstep(1.0, 0.65, climb);

                // ⚠️ THE STREAKS ONLY BITE ON THE COLUMN. On the pool they would read as a
                // spinning fan on the road, which is the "machine" fault one line up.
                float grain = lerp(1.0 - _StreakDepth + _StreakDepth * streak, 1.0, _Mode);

                float a = _Alpha * body * shape * grain * _Strength;

                // ⚠️ CLAMPED, AND `AbilityShowcaseProbe` IS THE NUMBER BEHIND IT. That probe fails
                // a run in which one effect blows more than 12 per cent of the frame to white, and
                // it caught Zack's ultimate at 62.8. This is the one effect a player deliberately
                // walks up to and stands under, so it is the one most able to fill a first-person
                // frame: an additive blend with no ceiling on it would do exactly that.
                a = saturate(a);

                // ⚠️ THE BASE GOES WHITE-HOT AND THE TIP KEEPS THE PLAYER'S COLOUR. A shaft that
                // is one saturated hue from end to end reads as a coloured object; the whitening
                // is what says it is emitting. It is a lerp toward white rather than a brightness
                // multiply because the blend is additive and a multiply would only fatten the
                // frame fraction the clamp above exists to hold down.
                float hot = _Hot * pow(1.0 - t, 5.0);
                float3 rgb = lerp(_Color.rgb, float3(1,1,1), saturate(hot));

                return fixed4(rgb, a);
            }
            ENDCG
        }
    }

    // ⚠️ NO FALLBACK LINE ON PURPOSE. A fallback here would draw the column as an opaque lit
    // cylinder, which is the plastic pipe this shader was written to replace, and it would do so
    // silently. `VfxMaterial.Beam` handles the miss in code, where it can warn.
}
