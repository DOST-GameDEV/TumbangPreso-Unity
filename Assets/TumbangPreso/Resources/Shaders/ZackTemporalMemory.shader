// One hologram with layered contour persistence, not separate historical actors.
Shader "TumbangPreso/ZackTemporalMemory"
{
 Properties
 {
  _MainTex("Albedo",2D)="white"{}
  _Color("Albedo tint",Color)=(1,1,1,1)
  _UsePalette("Palette remap",Float)=0
  _MemoryAge("Past age",Range(0,1))=.1
  _MemoryOpacity("Hologram opacity",Range(0,1))=.6
 }
 SubShader
 {
  Tags {"Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Back
  CGINCLUDE
  #include "UnityCG.cginc"
  sampler2D _MainTex;float4 _MainTex_ST,_Color,_Palette[16],_MemoryTrail[5];
  float _UsePalette,_MemoryAge,_MemoryOpacity;
  struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;float2 echo:TEXCOORD3;};
  v2f MemoryVertex(appdata_base v,int layer)
  {
   v2f o;float4 world=mul(unity_ObjectToWorld,v.vertex);world.xyz+=_MemoryTrail[layer].xyz;
   o.pos=mul(UNITY_MATRIX_VP,world);o.world=world.xyz;o.normal=UnityObjectToWorldNormal(v.normal);
   o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.echo=float2(layer,_MemoryTrail[layer].w);return o;
  }
  v2f vert0(appdata_base v){return MemoryVertex(v,0);}
  v2f vert1(appdata_base v){return MemoryVertex(v,1);}
  v2f vert2(appdata_base v){return MemoryVertex(v,2);}
  v2f vert3(appdata_base v){return MemoryVertex(v,3);}
  v2f vert4(appdata_base v){return MemoryVertex(v,4);}
  fixed4 frag(v2f i):SV_Target
  {
   clip(i.echo.y-.001);
   float3 paint=tex2D(_MainTex,i.uv).rgb;
   if(_UsePalette>.5){float2 cell=floor(clamp(i.uv,0,.9999)*16);int c=(int)cell.x,r=(int)cell.y;if(r<=7)paint=_Palette[(c/2)+(r<=3?8:0)].rgb;}
   paint*=_Color.rgb;
   float3 core=float3(1,.78,.25);
   float edge=pow(1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))),1.6);
   float stripe=lerp(.65,1,smoothstep(.3,.7,frac(i.world.y*8)));
   float fill=i.echo.x<.5?.22:.035;
   float alpha=_MemoryOpacity*i.echo.y*(fill+edge*.75)*stripe;
   alpha*=smoothstep(.65,1.3,distance(i.world,_WorldSpaceCameraPos));
   return fixed4(lerp(paint,core,.58)+core*edge*.75,alpha);
  }
  ENDCG
  Pass { Name "MEMORY_CORE"
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert0
   #pragma fragment frag
   ENDCG
  }
  Pass { Name "MEMORY_ECHO1"
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert1
   #pragma fragment frag
   ENDCG
  }
  Pass { Name "MEMORY_ECHO2"
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert2
   #pragma fragment frag
   ENDCG
  }
  Pass { Name "MEMORY_ECHO3"
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert3
   #pragma fragment frag
   ENDCG
  }
  Pass { Name "MEMORY_ECHO4"
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert4
   #pragma fragment frag
   ENDCG
  }
 }
 Fallback Off
}
