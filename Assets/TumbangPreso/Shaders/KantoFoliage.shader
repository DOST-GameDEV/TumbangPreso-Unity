// Two-sided, alpha-cut leaf cards for the Kanto sample map's foliage.
//
// ⚠️ WHY A SHADER AND NOT THE STANDARD ONE. Every leaf is a flat card whose drawn silhouette
// is cut out by the texture's alpha, and whose card is seen from both sides. Unity's Standard
// shader culls back faces, so half of every clump would vanish, and its Cutout mode cannot be
// told otherwise. This is Standard's own lighting (`surface surf Standard`) with Cull Off and
// an alpha test, nothing more.
//
// ⚠️ THE NORMALS ARE NOT FLIPPED ON BACK FACES, ON PURPOSE. The model ships custom normals that
// point away from each clump's centre (tools/author_kanto_models.py, Buf.finish), so a clump of
// cards shades as one soft ball. Flipping a card's normal when its back is seen would undo that
// and speckle the clump light and dark.
Shader "TumbangPreso/KantoFoliage"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Leaf (RGB) Alpha (A)", 2D) = "white" {}
        _Cutoff ("Alpha cut", Range(0, 1)) = 0.5
        _Glossiness ("Smoothness", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;

        struct Input { float2 uv_MainTex; };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb * _Color.rgb;
            o.Alpha = c.a;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
