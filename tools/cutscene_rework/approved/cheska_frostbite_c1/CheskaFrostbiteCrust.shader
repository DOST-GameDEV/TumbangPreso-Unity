// FROSTBITE's loaded tsinelas (`CheskaFrostbiteCoating`, switch `CheskaFrostbiteCoating.Crust`), c1 2026-10-29: a crust
// of her approved GLACIAL WALL DepthB ice on the real shoe. Same property block as `FrostbiteLoad`, so the coating
// drives it with no other change.
//   * THE WALL'S PAINT: four crisp value bands (pale top, light face, cyan body, crease) from the same painted light
//     (key light wrapped, tilt to the sky), the same cyan, a crisp frosted rim at the silhouette, thin bright cracks and
//     a few blinking glints. Near opaque, so it reads on a sunlit court with no bloom.
//   * IT GROWS FROM BOTH ENDS: toe and heel crust over first and it closes in the middle, as `_Strength` rises.
//   * IT MELTS, NEVER DARKENS. q1's crust died dark: its half-clear cells let the black sole show through a cyan tint.
//     Here a cell is crust or it is not, and the edge that is eating in is a bright melt line, so a dying crust only
//     ever gets paler and smaller, back toward both ends.
// Cheap: no texture reads, two value noises and one glint cell a pixel.
Shader "TumbangPreso/CheskaFrostbiteCrust"
{
    Properties
    {
        _Strength ("Loaded strength", Range(0,1)) = 0
        _Top ("Band 1: pale top", Color) = (0.88, 1.00, 0.99, 1)
        _Light ("Band 2: light face", Color) = (0.58, 0.93, 0.96, 1)
        _Mid ("Band 3: cyan body", Color) = (0.30, 0.79, 0.89, 1)
        _Crease ("Band 4: crease", Color) = (0.13, 0.52, 0.70, 1)
        _RimColor ("Frosted rim and melt line", Color) = (0.94, 1.00, 1.00, 1)
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
    }
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            float3 _Centre, _Along, _Across;
            float _Lift, _Strength;
            fixed4 _Top, _Light, _Mid, _Crease, _RimColor;
            float4 _KeyDir;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float2 plane:TEXCOORD0; float3 wn:TEXCOORD1; float3 wpos:TEXCOORD2; };
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
                // Lifted clear of the shoe's own surface and outline: a crust has thickness.
                v.vertex.xyz+=v.normal*_Lift*2.6;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.wn=UnityObjectToWorldNormal(v.normal);
                o.wpos=mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Broad cells, one octave and a half: chunky crust, not grain.
                float2 q=i.plane*7.0;
                float n=Noise(q)*0.7+Noise(q*2.3+5.1)*0.3;
                // Toe and heel first. `field` is high at both ends and in the crust's thick cells.
                float ends=saturate(abs(i.plane.x)*2.0);
                float field=ends*0.58+n*0.52;
                float cut=1.04-_Strength*1.14;
                float cover=step(cut,field)*step(0.001,_Strength);
                // The edge that is spreading or melting is a bright line, a cell wide at most.
                float melt=cover*(1.0-step(cut+0.07,field))*step(0.02,_Strength)*step(_Strength,0.985);

                float3 n3=normalize(i.wn);
                float3 nf=normalize(cross(ddy(i.wpos),ddx(i.wpos)));
                nf*=sign(dot(nf,n3)+1e-4);
                float3 nn=normalize(lerp(n3,nf,0.6));
                float3 l=normalize(_KeyDir.xyz), v=normalize(_WorldSpaceCameraPos-i.wpos);
                float paint=(dot(nn,l)*0.5+0.5)*0.78+saturate(nn.y)*0.30+(n-0.5)*0.22-0.10;
                float e=0.015;
                float b1=smoothstep(0.30-e,0.30+e,paint), b2=smoothstep(0.50-e,0.50+e,paint), b3=smoothstep(0.70-e,0.70+e,paint);
                float3 col=lerp(_Crease.rgb,_Mid.rgb,b1);
                col=lerp(col,_Light.rgb,b2);
                col=lerp(col,_Top.rgb,b3);
                // Thin bright cracks: two slanted families, broken into segments.
                float c1=abs(frac(dot(i.plane,float2(9.0,5.5))+Noise(q*0.6)*0.25)-0.5);
                float c2=abs(frac(dot(i.plane,float2(-6.0,8.5))+0.31)-0.5);
                float crack=max((1.0-smoothstep(0.012,0.04,c1))*step(0.55,Noise(q*0.9+11.0)),
                                (1.0-smoothstep(0.012,0.04,c2))*step(0.6,Noise(q*1.1+23.0)));
                col=lerp(col,_RimColor.rgb,crack*0.8);
                // Frosted rim at the silhouette.
                float fres=1.0-saturate(dot(nn,v));
                col=lerp(col,_RimColor.rgb,smoothstep(0.55,0.66,fres)*0.9);
                // A few glints that blink at their own phase.
                float2 g=q*0.9; float2 cell=floor(g), f=frac(g)-0.5;
                float h=Hash(cell);
                float2 d=abs(f-(float2(Hash(cell+5.1),Hash(cell+9.7))-0.5)*0.5);
                float star=saturate(max((1.0-smoothstep(0.02,0.05,min(d.x,d.y)))*(1.0-smoothstep(0.12,0.26,max(d.x,d.y))),1.0-smoothstep(0.03,0.07,length(d))));
                float blink=pow(saturate(sin(_Time.y*4.0+h*6.2832)),3.0)*step(0.7,h);
                col=lerp(col,float3(1,1,1),star*blink*(0.4+0.6*b2));
                col=lerp(col,_RimColor.rgb,melt);
                return fixed4(col,cover*0.96);
            }
            ENDCG
        }
    }
    Fallback Off
}
