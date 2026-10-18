// LIVE ULTIMATE LOOK, ONE SHADER FOR FOUR ULTIMATES (`UltimateLiveLook` in `Runtime/Visual/UltimateLiveLook.cs`).
//
// Painted bands over a scrolled mask: the mask is sampled twice at two speeds and two sizes and multiplied by itself,
// so the cells change shape as they move instead of sliding as one rigid sheet. The product is cut into three crisp
// value steps (a thin near-white core, a body colour, a faint veil) and everything under the veil is not drawn, which
// keeps the layers see-through.
//
// ⚠️ COVER, 2026-10-18. The first film (q1) showed Baha as a few pale flecks on the sunlit road and Overclock's column
// as nothing at all: the mask product alone rarely clears the veil cut, so most of a footprint was never drawn, and
// what was drawn was pale and part transparent, which a bright court washes out (there is no bloom on the court).
// `_Cover.x` adds a floor under the mask across the footprint, so a piece can be a painted SOLID body with the moving
// cells only deciding which band each pixel takes. `_Cover.y` is the mask gain (2.2 was the old constant) and
// `_Cover.z` flattens the vertex alpha's fall-off toward 1 (a shell faded out above its lower third, which is why the
// bolt column and the rime wall were scribbles at the foot). The defaults (0, 2.2, 0) are the old look exactly.
// The erode lift grows with the floor, so a covered piece still breaks into islands and is gone before it is destroyed.
//
// `_Clip` (min x, max x, min z, max z, world metres) cuts a piece to a rectangle. Airburst's rule reaches 40 m, far past
// the plaza; the court's playable bounds are what a player can stand in, so the drawn fan stops there. Default: no clip.
//
// Footprint: the piece is revealed outward from its foot as far as the rule's own travel (`_Travel`: delay s, speed m/s,
// limit m) and never past it, so what is drawn is what the rule reaches. Vertices marked green ride the front along the
// object's forward axis (a crest). After `_Fade.x` seconds the piece erodes in place over `_Fade.y` seconds: the cut
// rises through the mask, so it breaks into islands and is gone. Nothing rotates or swings.
//
// Per instance: `_Born` (Time.timeSinceLevelLoad at spawn, via a property block), `_Extent` (metres the u axis spans).
// Shared per ultimate: colours, mask, tiling, pan, cover and clip.
Shader "TumbangPreso/UltLiveBands"
{
    Properties
    {
        _Mask ("Mask (r)", 2D) = "white" {}
        _Core ("Core", Color) = (1,1,0.95,0.95)
        _Body ("Body", Color) = (0.6,0.9,0.8,0.6)
        _Veil ("Veil", Color) = (0.4,0.7,0.7,0.22)
        _Tile ("Tile a (xy) b (zw)", Vector) = (2,1,3.1,1.7)
        _Pan ("Pan a (xy) b (zw) per s", Vector) = (-0.6,0.05,-0.25,-0.08)
        _Cuts ("Veil, body, core cut", Vector) = (0.18,0.42,0.72,0)
        _Cover ("Floor, mask gain, flatten vertex alpha", Vector) = (0,2.2,0,0)
        _Clip ("Clip rect min x, max x, min z, max z", Vector) = (-100000,100000,-100000,100000)
        _Travel ("Delay s, speed m/s, limit m", Vector) = (0,30,40,0)
        _Fade ("Erode start s, erode length s", Vector) = (1.2,0.8,0,0)
        _Born ("Born (s)", Float) = 0
        _Extent ("Metres along u", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _Mask;
            float4 _Core, _Body, _Veil, _Tile, _Pan, _Cuts, _Cover, _Clip, _Travel, _Fade;
            float _Born, _Extent;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float age : TEXCOORD1; float reach : TEXCOORD2; float2 wxz : TEXCOORD3; };

            v2f vert (appdata v)
            {
                v2f o;
                float age = _Time.y - _Born;
                float reach = min(_Travel.z, max(0, age - _Travel.x) * _Travel.y);
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 fwd = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,0,1)));
                w += fwd * reach * v.color.g;
                o.pos = UnityWorldToClipPos(w);
                o.uv = v.uv; o.color = v.color; o.age = age; o.reach = reach; o.wxz = w.xz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = i.age;
                // Revealed only as far as the rule has travelled (crest pieces carry g = 1 and are always "reached").
                float along = i.uv.x * _Extent;
                float shown = max(i.color.g, step(along, i.reach));
                shown *= step(_Clip.x, i.wxz.x) * step(i.wxz.x, _Clip.y) * step(_Clip.z, i.wxz.y) * step(i.wxz.y, _Clip.w);
                // Brighter just behind the travelling edge: the front reads as the leading lip.
                float lip = (1 - i.color.g) * saturate(1 - abs(i.reach - along) / 1.5) * step(i.reach, _Travel.z - 0.01);
                float a = tex2D(_Mask, frac(i.uv * _Tile.xy + _Pan.xy * t)).r;
                float b = tex2D(_Mask, frac(i.uv * _Tile.zw + _Pan.zw * t + 0.37)).r;
                float va = lerp(i.color.a, 1, _Cover.z * step(0.02, i.color.a));
                float m = (saturate(a * b * _Cover.y) + _Cover.x) * va + lip * 0.35;
                float erode = saturate((t - _Fade.x) / max(0.01, _Fade.y));
                m -= erode * erode * (0.9 + _Cover.x * 2.5);
                m *= shown * step(0, t);
                fixed4 c = m > _Cuts.z ? _Core : (m > _Cuts.y ? _Body : _Veil);
                c.a *= step(_Cuts.x, m);
                clip(c.a - 0.004);
                return c;
            }
            ENDCG
        }
    }
}
