Shader "TumbangPreso/PoolCeramic"
{
    Properties
    {
        _Color ("Glazed ceramic",Color)=(.48,.61,.56,1)
        _Grout ("Quiet grout",Color)=(.37,.48,.43,1)
        _TileSize ("Tile metres",Float)=.25
        _Glossiness ("Smoothness",Range(0,1))=.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "NearFade"="Preserve" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        fixed4 _Color,_Grout;
        half _TileSize,_Glossiness;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 n=abs(IN.worldNormal);
            float2 plane=n.y>.5?IN.worldPos.xz:n.x>.5?IN.worldPos.zy:IN.worldPos.xy;
            float2 grid=plane/max(.05,_TileSize);
            float2 edge=min(frac(grid),1-frac(grid));
            float seam=min(edge.x,edge.y);
            float aa=max(.004,fwidth(seam));
            float grout=1-smoothstep(.020-aa,.020+aa,seam);
            float variation=frac(sin(dot(floor(grid),float2(12.9898,78.233)))*43758.5453);
            o.Albedo=lerp(_Color.rgb*(.965+variation*.07),_Grout.rgb,grout);
            o.Smoothness=_Glossiness*(1-grout*.75);
            o.Metallic=0;o.Occlusion=1-grout*.12;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
