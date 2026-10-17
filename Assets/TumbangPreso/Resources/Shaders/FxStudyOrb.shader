// The effects study's round things (`Runtime/Visual/FxStudy`): a ball of light, the bubble round it, a dish, a dome.
//
// A ribbon is flat and wears a picture (`FxStudyRibbon`). A ball has no picture to wear: what makes it read as a ball
// of LIGHT in the references is that it is white where it faces the eye and coloured where it turns away, with a
// brighter line on its very edge. That is all this does: `_Core` facing the eye, `_Rim` at the edge, mixed by how far
// the surface has turned (`_Power`), each with its own opacity so the same shader is a solid white heart (core opaque)
// or an empty bubble (core clear, rim bright). `_Band` adds a lit band from the mesh's own v (the lip of a dish).
//
// ⚠️ No `_Time`, no depth texture, no loops: the same rules as `FxStudyRibbon`, for the same reasons.
// Premultiplied; `_Add` 1 is light only.
Shader "TumbangPreso/FxStudyOrb"
{
    Properties
    {
        _Core ("Colour facing the eye", Color) = (1, 1, 1, 1)
        _Rim ("Colour at the edge", Color) = (0.4, 0.9, 1, 1)
        _CoreAlpha ("Opacity facing the eye", Range(0,1)) = 1
        _RimAlpha ("Opacity at the edge", Range(0,1)) = 1
        _Power ("How late the edge colour comes in", Range(0.3,8)) = 2
        _Opacity ("Opacity (set per frame)", Range(0,1)) = 1
        _Add ("Additive (1 light only, 0 covers)", Range(0,1)) = 0
        _Gain ("Brightness", Range(0,6)) = 1
        _Band ("Lit band: v where it sits, its width, its strength", Vector) = (1, 0.1, 0, 0)
        _BandColor ("Lit band colour", Color) = (0.7, 0.95, 1, 1)
        _Age ("Clock (s, set per frame)", Float) = 0
        _Billow ("Billow: how far (set per frame), lobes a metre, how fast they travel, seed", Vector) = (0, 2, 1, 0)
        _Emit ("How far over white it goes under a blooming camera", Range(0,8)) = 0
        _ErodeTex ("Erosion map", 2D) = "white" {}
        _Erode ("Erosion (0 whole, 1 gone, set per frame)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Core, _Rim, _BandColor;
            float _CoreAlpha, _RimAlpha, _Power, _Opacity, _Add, _Gain;
            float4 _Band;
            sampler2D _ErodeTex;
            float _Erode, _Age, _Emit, _FxStudyBloom;
            float4 _Billow;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 view:TEXCOORD2; float lobe:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                // ⚠️ SMOKE BILLOWS. The owner, of the lumps when they were fixed meshes: "the dark smoke changes mesh btw, yours
                // is just the same mesh shape all throughout". So the surface is pushed along its normal by a field of slow
                // waves that TRAVELS through it with the clock (outward and a little up): big slow lobes and smaller quick
                // ones. A lobe swells, rolls across and sinks back, and the outline is different every frame. The normal is
                // leant by the field's own slope, so the lit rim follows the new shape.
                float3 p=v.vertex.xyz*_Billow.y+float3(_Billow.w,_Billow.w*1.7,_Billow.w*2.3);
                float3 flow=float3(0.9,0.5,0.6)*_Age*_Billow.z;
                float3 a1=p-flow, a2=p*2.3-flow*1.9;
                float big=sin(a1.x)*cos(a1.z)+0.7*sin(a1.y+a1.x*0.6);
                float small=sin(a2.x+1.3)*cos(a2.y)+sin(a2.z*1.2+0.7);
                float lobe=big*0.62+small*0.24;
                float3 slope=float3(cos(a1.x)*cos(a1.z)+0.42*cos(a1.y+a1.x*0.6),0.7*cos(a1.y+a1.x*0.6),-sin(a1.x)*sin(a1.z))*0.62*_Billow.y;
                v.vertex.xyz+=v.normal*lobe*_Billow.x;
                v.normal=normalize(v.normal-(slope-v.normal*dot(slope,v.normal))*_Billow.x);
                o.lobe=lobe*step(1e-4,_Billow.x);
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.view=WorldSpaceViewDir(v.vertex);
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                // Both faces are drawn, so the back of a bubble turns away exactly as its front does.
                float facing=abs(dot(normalize(i.normal),normalize(i.view)));
                float turn=pow(1.0-facing,_Power);
                fixed3 colour=lerp(_Core.rgb,_Rim.rgb,turn);
                float alpha=lerp(_CoreAlpha,_RimAlpha,turn);
                float band=exp(-pow((i.uv.y-_Band.x)/max(_Band.y,1e-3),2.0))*_Band.z;
                colour=lerp(colour,_BandColor.rgb,saturate(band));
                // Eaten away, not faded: a volume of smoke comes apart in holes (the erosion map read on its own surface).
                // The thin lobes that stand out furthest are the first to go.
                float eaten=tex2D(_ErodeTex,i.uv*float2(2.0,1.0)).r-0.22*i.lobe;
                alpha=saturate(alpha+band)*_Opacity*smoothstep(_Erode*1.2-0.1,_Erode*1.2,eaten);
                return float4(colour*alpha*_Gain*(1.0+_FxStudyBloom*_Emit),alpha*(1.0-_Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
