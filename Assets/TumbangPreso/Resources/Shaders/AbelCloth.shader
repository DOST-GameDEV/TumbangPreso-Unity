// AMIHAN'S ABEL (2026-10-03, `docs/reports/amihan-presentation-2026-10-02/abel-cloth-direction.md`): her wind is seen by the
// cloth it carries. Solid, toon-lit, two-sided woven cloth in her family's abel: a cream ground, rust selvedges, teal pinstripes,
// and a teal centre band carrying the binakol zigzag in gold. Painted here from uv (x across the cloth, y along it in metres),
// so any length of cloth weaves the same. It ENDS BY FRAYING (`_Fray`): threads drop out across it, the free end first.
// No `_Time`: every moving value is the caller's (`IVfxTimeline`).
Shader "TumbangPreso/AbelCloth"
{
    Properties
    {
        _Cream("Cream", Color) = (0.945, 0.894, 0.784, 1)
        _Teal("Teal", Color) = (0.180, 0.549, 0.525, 1)
        _Rust("Rust", Color) = (0.659, 0.314, 0.180, 1)
        _Gold("Gold", Color) = (0.910, 0.714, 0.290, 1)
        _Ink("Ink", Color) = (0.11, 0.10, 0.08, 1)
        _Weave("Zigzag repeats per metre", Float) = 1.6
        _Fray("Fray", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Cream, _Teal, _Rust, _Gold, _Ink;
            float _Weave, _Fray;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.n = UnityObjectToWorldNormal(v.normal); UNITY_TRANSFER_FOG(o, o.pos); return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            fixed4 frag(v2f i, fixed face : VFACE) : SV_Target
            {
                float x = i.uv.x, y = i.uv.y;
                // FRAY: whole weft threads drop out (bands across the cloth), more toward the free end (uv.y large).
                float weft = hash(float2(floor(y * 40.0), 3.1));
                float warp = hash(float2(floor(x * 18.0), 7.7));
                clip(min(weft, warp) - _Fray * 1.05);
                // THE WEAVE.
                fixed3 c = _Cream.rgb;
                float edge = min(x, 1 - x);
                if (edge < 0.10) c = _Rust.rgb;
                else if (abs(edge - 0.16) < 0.022) c = _Teal.rgb;
                float centre = abs(x - 0.5);
                if (centre < 0.17)
                {
                    c = _Teal.rgb;
                    float zig = abs(frac(y * _Weave) - 0.5) * 2.0;          // 0..1 back and forth along the cloth
                    float zline = abs(centre * (1.0 / 0.17) - zig);
                    if (zline < 0.22) c = _Gold.rgb;
                    if (zline < 0.09) c = _Cream.rgb;
                }
                // A fine woven texture: alternate threads a touch darker.
                c *= 0.94 + 0.06 * step(0.5, frac(y * 22.0 + floor(x * 22.0) * 0.5));
                // TOON LIGHT: two steps, both faces lit as their own side.
                float3 n = normalize(i.n) * (face > 0 ? 1 : -1);
                float lambert = dot(n, normalize(float3(0.35, 0.85, 0.4))) * 0.5 + 0.5;
                c *= lerp(0.68, 1.0, smoothstep(0.42, 0.5, lambert));
                // INK at the selvedge, like the cast's outlines.
                c = lerp(_Ink.rgb, c, smoothstep(0.0, 0.025, edge));
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
