// Two-sided, alpha-cut leaf and flower cards for the Lagoon Cove sample map.
//
// KantoFoliage's recipe (Standard lighting, Cull Off, an alpha test) with the Lagoon kit's
// colouring: the leaf drawings are GREYSCALE and the colour is the material's tint, times a
// `_Gain` (the export already folds the kit's gain and its clamp into the tint and sends 1).
// One drawing then serves several plants.
//
// ⚠️ THE NORMALS ARE NOT FLIPPED ON BACK FACES, ON PURPOSE (as KantoFoliage): the kit ships
// custom normals pointing out of each clump, so a clump shades as one soft ball. Flipping a
// card's normal when its back is seen would speckle the clump light and dark.
//
// ⚠️ THE PALE KEY (flowers only, `_PaleOn`): above an albedo value of 0.7484..0.9331 (linear,
// a clamped map range) the tinted colour gives way to a cream, the flower's stamen. Without it
// every gumamela is one flat red.
//
// ⚠️ THE PER-PLANT NUDGE, as the Blender material's Object Info Random: value x 0.9..1.08 and a
// hue shift of -0.015..+0.015 of the wheel, from a hash of the object's world position, so a
// row of palms sharing one material does not read as stamped copies. The hash needs the real
// transform, so LagoonCoveSceneBuilder keeps these renderers out of static batching.
Shader "TumbangPreso/LagoonFoliage"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Leaf (greyscale or colour, alpha cut)", 2D) = "white" {}
        _Gain ("Gain", Float) = 1
        _Cutoff ("Alpha cut", Range(0, 1)) = 0.5
        _Glossiness ("Smoothness", Range(0, 1)) = 0.3
        [Toggle] _VertexTint ("Multiply by vertex colour", Float) = 0
        [Toggle] _PaleOn ("Pale key", Float) = 0
        _PaleColour ("Pale colour", Color) = (0.98, 0.93, 0.7, 1)
        _PaleRange ("Pale range (value lo, value hi)", Vector) = (0.7484, 0.9331, 0, 0)
        _Nudge ("Nudge (value lo, value hi, hue lo, hue hi)", Vector) = (0.9, 1.08, -0.015, 0.015)
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0
        #include "LagoonNoise.cginc"

        sampler2D _MainTex;
        fixed4 _Color, _PaleColour;
        half _Glossiness, _Gain, _VertexTint, _PaleOn;
        float4 _PaleRange, _Nudge;

        struct Input { float2 uv_MainTex; float4 color : COLOR; };

        float3 HueShift(float3 c, float shift)
        {
            // RGB to HSV, rotate the hue, back (Blender's Hue/Saturation/Value node, hue only).
            float4 K = float4(0, -1.0 / 3.0, 2.0 / 3.0, -1);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            float d = q.x - min(q.w, q.y);
            float3 hsv = float3(abs(q.z + (q.w - q.y) / (6 * d + 1e-6)), d / (q.x + 1e-6), q.x);
            hsv.x = frac(hsv.x + shift);
            float3 rgb = saturate(abs(frac(hsv.x + float3(1, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
            return hsv.z * lerp(float3(1, 1, 1), rgb, hsv.y);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            float3 albedo = c.rgb * _Color.rgb * _Gain;
            albedo *= lerp(float3(1, 1, 1), IN.color.rgb, _VertexTint);
            if (_PaleOn > 0)
                albedo = lerp(albedo, _PaleColour.rgb, MapRangeLinear(max(c.r, max(c.g, c.b)), _PaleRange.x, _PaleRange.y, 0, 1));
            float3 origin = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
            float3 h = abs(LagoonHash33(floor(origin * 7.31) + 0.5));
            albedo = HueShift(albedo, lerp(_Nudge.z, _Nudge.w, h.y)) * lerp(_Nudge.x, _Nudge.y, h.x);
            o.Albedo = saturate(albedo);
            o.Alpha = c.a;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
