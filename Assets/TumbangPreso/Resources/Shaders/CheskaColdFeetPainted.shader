Shader "TumbangPreso/CheskaColdFeetPainted"
{
    // Painted chill field. Same properties as FrostSurface, so FrostSurfacePresentation drives it
    // unchanged. Three flat tones (deep rim, mid ice, clean light top), frost patches from one
    // pattern panned at two speeds and multiplied by itself, and a few bold four point sparkles.
    // Coverage stays inside the uv radius the mesh already draws, so the footprint is honest.
    // Thaw erodes patches in place; nothing rotates or slides.
    Properties
    {
        _Color ("Frost", Color) = (0.48, 0.80, 0.88, 0.18)
        _Growth ("Formation Radius", Float) = 1.12
        _Opacity ("Life Opacity", Range(0,1)) = 1
        _Thaw ("Spatial Thaw", Range(0,1)) = 0
        _Glass ("Unused here", Range(0,1)) = 0
        _Trail ("Wave Trail Width", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            float4 _Color;
            float _Growth, _Opacity, _Glass, _Trail, _Thaw;

            float Hash(float2 c) { return frac(sin(dot(c,float2(12.9898,78.233)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+float2(1,1)),f.x),f.y);
            }
            float Blob(float2 p) { return Noise(p)*.65+Noise(p*2.1+7.3)*.35; }

            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex); return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 q=(i.uv-.5)*2;
                float radius=length(q);
                float aa=max(fwidth(radius),.002);
                float reveal=(1-smoothstep(_Growth-aa*2,_Growth,radius))*smoothstep(_Growth-_Trail,_Growth-_Trail+aa*2,radius);
                float t=_Time.y;
                // Two speeds of one pattern, multiplied: the patches breathe instead of sliding.
                float a=Blob(q*2.4+float2(t*.05,-t*.03));
                float b=Blob(q*3.3+float2(-t*.11,t*.08)+3.1);
                float living=saturate(a*b*2.2);
                // Chunky painted patches with one clean edge.
                float patch=step(.42,living);
                float top=step(.70,living);
                // Deep rim band so the hazard boundary reads from above.
                float rim=step(_Growth-.10,radius);
                float3 mid=_Color.rgb;
                float3 deep=_Color.rgb*float3(.62,.78,.86);
                float3 light=lerp(_Color.rgb,float3(.93,.98,1),.75);
                float3 col=lerp(mid,light,top);
                col=lerp(col,deep,rim);
                float alpha=lerp(_Color.a*.6,saturate(_Color.a+.30),patch);
                alpha=lerp(alpha,saturate(_Color.a+.36),rim);
                // A few bold sparkles, each with its own twinkle phase.
                float2 cell=floor(q*4.5), f=frac(q*4.5)-.5;
                float h=Hash(cell);
                float2 o=(float2(Hash(cell+5.1),Hash(cell+9.7))-.5)*.5;
                float2 d=abs(f-o);
                float star=saturate(step(min(d.x,d.y),.035)*step(max(d.x,d.y),.20)+step(length(f-o),.07));
                float twinkle=step(.86,h)*step(.55,frac(t*.6+h*7));
                float spark=star*twinkle*(1-rim);
                col=lerp(col,float3(1,1,1),spark);
                alpha=max(alpha,spark*.9);
                // Thaw erodes patch by patch in place.
                float erode=1;
                if(_Thaw>0)
                {
                    float last=1-saturate(radius*.55+Blob(q*3.1)*.45);
                    erode=step(_Thaw,last);
                }
                fixed4 c=fixed4(col,alpha*reveal*_Opacity*erode);
                UNITY_APPLY_FOG(i.fogCoord,c);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
