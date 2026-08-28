// The toon pass for a surface whose texture carries real transparency.
//
// ⚠️⚠️ THIS IS A SEPARATE SHADER ON INSTRUCTION, AND THE FIRST TWO ATTEMPTS AT DOING IT
// INSIDE `TumbangPreso/Toon` ARE WHY. 🧑 2026-08-28, over the fuzzy house slipper:
// *"instead of cleaving my model's shit js create independent shaders for shit with fuzz
// or detailed shit"*, *"i dont want u to cleave my ike and fuzz off wtf"*.
//
// The slipper's `Fuzz` material is `alphaMode: BLEND` and its fur is drawn as alpha
// mapped cards. On the opaque shader every transparent texel rendered solid and the
// inverted hull drew an ink edge round each card's rectangle, which read as black
// tearing across the shoe. An alpha CLIP was tried instead and that was worse in a more
// insidious way: a fur alpha map is mostly soft gradient, because that is how strands are
// drawn, so any useful threshold throws the wisps away and leaves a smooth moulded
// slipper. At 0.5 the fuzz vanished entirely, at 0.15 it was a fringe. The source has
// dense fur and none of it is optional.
//
// ⚠️ SO NOTHING IS DISCARDED HERE. It BLENDS, which is what the material asked for in the
// first place, and a soft alpha stays soft.
//
// ⚠️ AND `TumbangPreso/Toon` IS LEFT EXACTLY AS IT WAS. It has no `clip`, no `_Cutoff` and
// no transparent path, so every opaque prop, character and viewmodel in the game renders
// bit-identically to before this file existed. Two shaders is the point: an opaque one
// that is simple and fast for the ninety-nine props that need nothing, and this for the
// ones with real detail in the alpha channel.
Shader "TumbangPreso/ToonTransparent"
{
    Properties
    {
        _Color ("Albedo", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo Map", 2D) = "white" {}

        _FlashColor ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        // Kept in step with Toon.shader's own values by hand. See the note on ShadowBand
        // there: these two ARE the flat two-band look, and a transparent surface that
        // banded differently would read as a different art style rather than as fur.
        _ShadowBand ("Shadow Band", Range(0, 1)) = 0.55
        _BandEdge ("Band Edge", Range(0.001, 0.5)) = 0.02
    }

    SubShader
    {
        // ⚠️ TRANSPARENT QUEUE, AND `IgnoreProjector` WITH IT. Fur cards sort against each
        // other and against the shoe they sit on; leaving this on Geometry would draw them
        // before the surfaces behind them and punch holes in the slipper.
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // ⚠️⚠️ `ZWrite Off` IS WHAT MAKES OVERLAPPING FUR CARDS LOOK LIKE FUR. With depth
        // writes on, the first card drawn claims every pixel it covers at full opacity in
        // the depth buffer, and every strand behind it is rejected however transparent the
        // one in front was. The slipper then reads as a few flat opaque flakes. Off, the
        // strands accumulate, which is the entire visual.
        //
        // ⚠️ THE COST IS THAT THIS SURFACE CANNOT SELF-OCCLUDE, and that is accepted rather
        // than overlooked. A slipper is a small convex prop seen from outside at arena
        // distance; the sorting artefacts that would matter on a large concave mesh are not
        // reachable here.
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // ⚠️ TWO-SIDED, BECAUSE A FUR CARD IS A PLANE AND HALF OF THEM FACE AWAY. The source
        // material is `doubleSided` and culling the back faces halves the fur.
        Cull Off

        // ⚠️⚠️ AND THERE IS NO OUTLINE PASS IN THIS SHADER AT ALL, WHICH IS DELIBERATE AND IS
        // HALF THE ORIGINAL BUG. `Toon.shader`'s inverted hull expands the mesh along its
        // normals and draws it in ink. On a solid prop that is a silhouette. On a stack of
        // alpha cards it is a black rectangle round every card, including the transparent
        // parts, because the hull has no idea the texture has holes in it. The slipper still
        // gets its ink edge: the `Shoe` material underneath is opaque, stays on
        // `TumbangPreso/Toon`, and carries the outline for the whole prop.
        CGPROGRAM
        #pragma surface surf Toon alpha:fade noshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _FlashColor;
        half _FlashAmount;
        half _ShadowBand;
        half _BandEdge;

        struct Input
        {
            float2 uv_MainTex;
        };

        // Transcribed from Toon.shader. The two must agree or a fur strand shades on a
        // different ramp from the shoe it grows out of.
        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half shade = dot(s.Normal, lightDir) * atten;
            half band = smoothstep(0.0h, _BandEdge, shade);
            half level = lerp(_ShadowBand, 1.0h, band);

            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * level;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 sampled = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = lerp(sampled.rgb, _FlashColor.rgb, _FlashAmount);

            // ⚠️ THE ALPHA IS CARRIED THROUGH UNTOUCHED. No threshold, no power curve, no
            // "tidying". Whatever the artist drew in that channel is what gets blended.
            o.Alpha = sampled.a;
            o.Specular = 0.0;
            o.Gloss = 0.0;
        }
        ENDCG
    }

    Fallback "Transparent/VertexLit"
}
