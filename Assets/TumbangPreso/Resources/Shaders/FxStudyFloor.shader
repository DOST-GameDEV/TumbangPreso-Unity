// The floor of the effects bench (`Editor/MapKit/PaeteAbilityFilm.FxBench.cs`): a dark neutral grid, and nothing else.
//
// ⚠️ WHY A FLOOR OF ITS OWN. The first effects were judged inside busy hero scenes on a pale grey ground, where light
// has nothing dark to be bright against. Every reference the owner pointed at is shown on a dark neutral floor with a
// faint grid, so the bench has that floor: two near greys in a chequer with a thin dark line between the squares
// (`_Chequer` 1), or one charcoal with only the line (`_Chequer` 0), dimming away from the middle so the eye stays on
// the effect. Unlit on purpose: an effect's own glow sprites are the only light in the picture.
//
// The squares are laid out in WORLD metres, so one square is `_Cell` metres whatever size the floor mesh is.
Shader "TumbangPreso/FxStudyFloor"
{
    Properties
    {
        _A ("First grey", Color) = (0.33, 0.33, 0.34, 1)
        _B ("Second grey", Color) = (0.25, 0.25, 0.26, 1)
        _LineColor ("Line", Color) = (0.12, 0.12, 0.13, 1)
        _Cell ("Square size (m)", Float) = 1
        _Offset ("Grid slid by (x, z, m)", Vector) = (0, 0, 0, 0)
        _LineWidth ("Line width (share of a square)", Range(0,0.2)) = 0.02
        _Chequer ("Chequer (1) or one grey (0)", Range(0,1)) = 1
        _Dim ("Dims to this share by `_DimAt` metres from the middle", Range(0,1)) = 0.55
        _DimAt ("Distance at which it is dimmest (m)", Float) = 14
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _A, _B, _LineColor;
            float _Cell, _LineWidth, _Chequer, _Dim, _DimAt;
            float4 _Offset;
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.world.xz-_Offset.xy)/_Cell;
                float2 cell=floor(p), f=frac(p);
                float odd=fmod(abs(cell.x+cell.y),2.0);
                fixed3 colour=lerp(_A.rgb,lerp(_A.rgb,_B.rgb,odd),_Chequer);
                // A line softened by how far away it is, so the grid does not shimmer toward the horizon.
                float far=length(i.world-_WorldSpaceCameraPos);
                float width=_LineWidth*(1.0+far*.05);
                float2 edge=min(f,1.0-f);
                float line_=1.0-smoothstep(width*.5,width,min(edge.x,edge.y));
                colour=lerp(colour,_LineColor.rgb,line_/(1.0+far*.04));
                colour*=lerp(1.0,_Dim,saturate(length(i.world.xz)/_DimAt));
                return fixed4(colour,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
