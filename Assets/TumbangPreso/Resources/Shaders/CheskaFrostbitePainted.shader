Shader "TumbangPreso/CheskaFrostbitePainted"
{
    // Painted frost crust on the loaded slipper. Same property block as FrostbiteLoad, so
    // CheskaFrostbiteCoating drives it unchanged. Chunky patches in three flat tones creep in
    // from toe and heel, breathe at two speeds, and a few bold sparkles blink on the crust.
    Properties { _Strength ("Loaded strength", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _Centre, _Along, _Across;
            float _Lift, _Strength;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float2 plane:TEXCOORD0; float up:TEXCOORD1; };
            float Hash(float2 c){ return frac(sin(dot(c,float2(12.9898,78.233)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+float2(1,1)),f.x),f.y);
            }
            v2f vert(appdata v)
            {
                v2f o;
                float3 p=v.vertex.xyz-_Centre;
                o.plane=float2(dot(p,_Along),dot(p,_Across));
                o.up=saturate(v.normal.y*.5+.5);
                v.vertex.xyz+=v.normal*_Lift*2;
                o.vertex=UnityObjectToClipPos(v.vertex); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.plane*6;
                float t=_Time.y;
                float a=Noise(p+float2(t*.20,0));
                float b=Noise(p*1.6+float2(0,-t*.33)+4.2);
                float living=saturate(a*b*2.3);
                // Crust creeps in from both ends as strength rises, then erodes back the same way.
                float ends=abs(i.plane.x)*2;
                float cover=step(1-_Strength*.95,ends*.6+living*.6);
                float top=step(.62,living)*step(.5,i.up);
                float3 deep=float3(.36,.66,.80), mid=float3(.62,.86,.95), light=float3(.93,.98,1);
                float3 col=lerp(lerp(deep,mid,step(.32,living)),light,top);
                float2 cell=floor(p*1.5), f=frac(p*1.5)-.5;
                float h=Hash(cell);
                float2 d=abs(f);
                float star=saturate(step(min(d.x,d.y),.05)*step(max(d.x,d.y),.30));
                float spark=star*step(.82,h)*step(.6,frac(t*.8+h*5));
                col=lerp(col,float3(1,1,1),spark);
                return fixed4(col,cover*.85*saturate(_Strength*1.5));
            }
            ENDCG
        }
    }
    Fallback Off
}
