// Yasmin's GLACIAL WALL ice, PAINTED (`CheskaGlacialWallLook`, styles Painted, Chunky and Blocky). The owner, of the
// first samples drawn with the realistic crystal shader: "same for cheska's ice.. doesnt fit the style of the game rn".
// So this is ice the way the rest of TUMP is painted, not ice the way glass refracts:
//   * THREE FLAT VALUES a face, from a fixed key light: a clean light top, a soft mid, a shade. No normal map, no
//     baked cracks, no depth colour, no refraction.
//   * A face turned up gets the pale top, like the light tops of the game's own props.
//   * One soft sheen band drifts up the wall slowly, so the surface is not frozen still while it stands; it shifts a
//     value, it does not glitter.
//   * The rise out of the road and the sink back in are the same per-vertex growth the crystal variant uses
//     (TEXCOORD1 the piece's foot, TEXCOORD2 its birth, height, death and seed), on `_Age` set by the owner.
// SOLID, writes depth, one texture read none: cheap on phones.
Shader "TumbangPreso/CheskaWallIce"
{
    Properties
    {
        _Top ("Light top", Color) = (0.86, 0.97, 1.00, 1)
        _Mid ("Soft mid", Color) = (0.52, 0.81, 0.95, 1)
        _Shade ("Shade", Color) = (0.27, 0.55, 0.82, 1)
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _Rim ("Edge light", Range(0,1)) = 0.18
        _Sheen ("Drifting sheen", Range(0,1)) = 0.10
        _Age ("Owner's clock (s)", Float) = 0
        _Lit ("Lit from inside (1 is normal)", Range(0,3)) = 1
        _GrowTime ("Seconds a piece takes to rise", Float) = 0.24
        _DieAt ("Owner's clock at which the sinking starts", Float) = 100000
        _DieTime ("Seconds a piece takes to sink", Float) = 0.40
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Top, _Mid, _Shade;
            float4 _KeyDir;
            float _Rim, _Sheen, _Age, _Lit, _GrowTime, _DieAt, _DieTime;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 foot:TEXCOORD1; float4 life:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 toLens:TEXCOORD1; float height:TEXCOORD2; float seed:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                float g=saturate((_Age-v.life.x)/_GrowTime);
                float rise=1.0-(1.0-g)*(1.0-g)*(1.0-g);
                float bump=sin(saturate((g-0.45)/0.55)*3.14159);
                float d=saturate((_Age-_DieAt-v.life.z)/_DieTime);
                d=d*d*(3.0-2.0*d);
                float3 rel=v.vertex.xyz-v.foot.xyz;
                o.height=rel.y;
                rel.y*=(1.0+0.12*bump)*(1.0-0.20*d);
                rel.xz*=(lerp(0.62,1.0,rise)-0.06*bump)*(1.0-0.40*d);
                rel.y-=(1.0-rise)*v.life.y*1.08+d*v.life.y*1.05;
                float4 p=float4(v.foot.xyz+rel,1.0);
                o.vertex=UnityObjectToClipPos(p);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.toLens=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,p).xyz;
                o.seed=v.life.w;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal), l=normalize(_KeyDir.xyz), v=normalize(i.toLens);
                float ndl=dot(n,l);
                // Three painted values: shade, mid, light, with soft but short steps between them.
                float mid=smoothstep(-0.05,0.05,ndl), top=smoothstep(0.50,0.58,ndl);
                float3 col=lerp(_Shade.rgb,_Mid.rgb,mid);
                col=lerp(col,_Top.rgb,top);
                // A face turned up is a light top whatever the key light says.
                col=lerp(col,_Top.rgb,smoothstep(0.72,0.80,n.y));
                // A thin edge light at the silhouette, so the wall separates from a pale road.
                float fres=1.0-saturate(dot(n,v));
                col=lerp(col,_Top.rgb,smoothstep(0.62,0.80,fres)*_Rim);
                // One soft band drifting up, a value shift on each piece at its own phase.
                float band=frac(i.height*0.45-_Age*0.22+i.seed*0.137);
                col=lerp(col,_Top.rgb,smoothstep(0.0,0.25,band)*(1.0-smoothstep(0.25,0.5,band))*_Sheen);
                col*=lerp(1.0,1.25,saturate(_Lit-1.0));
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
