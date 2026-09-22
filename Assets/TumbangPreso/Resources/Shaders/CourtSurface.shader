Shader "TumbangPreso/CourtSurface"
{
    Properties
    {
        _Medium("Chalk or charcoal",Color)=(.96,.92,.81,1)
        _Mode("Overlay mode",Float)=0
        _WearKind("Map medium",Float)=0
        _Weight("Weight",Range(0,1))=1
        _Radius("Court radius",Float)=7
        [HideInInspector] _SrcBlend("Source blend",Float)=5
        [HideInInspector] _DstBlend("Destination blend",Float)=10
    }
    SubShader
    {
        Tags {"Queue"="Transparent-30" "RenderType"="Transparent"}
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_FOG_COORDS(1)};
            fixed4 _Medium;float _Mode,_WearKind,_Weight,_Radius;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;UNITY_TRANSFER_FOG(o,o.pos);return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.world.xz;fixed4 colour;
                if(_Mode<.5)
                {
                    float grain=noise(p*39);float footprint=max(length(ddx(p)),length(ddy(p)))*39;
                    grain=lerp(grain,.65,saturate(footprint-.5));
                    colour=fixed4(_Medium.rgb*lerp(1,lerp(.78,1,grain),_Weight),lerp(1,smoothstep(.06,.21,grain),_Weight));
                }
                else
                {
                    float radius=length(p);float outer=smoothstep(2.1,5.8,radius);
                    float edge=1-smoothstep(_Radius-.7,_Radius,max(abs(p.x),abs(p.y)));
                    float patches=noise(p*.34+_WearKind*17.3),detail=noise(p*8);
                    float amount=0;fixed3 tint=fixed3(.22,.18,.13);
                    if(_WearKind<.5) // Plaza: soft paver dust, centre sun-bleach.
                    {amount=smoothstep(.57,.83,patches)*.075*outer;tint=fixed3(.48,.39,.28);}
                    else if(_WearKind<1.5) // Alley: irregular oil, broad aggregate patches.
                    {amount=smoothstep(.48,.76,patches)*.10*outer;tint=fixed3(.065,.06,.055);}
                    else if(_WearKind<2.5) // Underpass: larger cool damp patches, no oil repeats.
                    {amount=smoothstep(.44,.80,noise(p*.18+8))* .11*outer;tint=fixed3(.16,.19,.18);}
                    else if(_WearKind<3.5) // Rooftop: pale tile dust gathered near edges.
                    {amount=smoothstep(.52,.82,patches)*.10*outer;tint=fixed3(.73,.63,.46);}
                    else // Deck: thin salt scuffs and uneven board wear, not concrete noise.
                    {amount=smoothstep(.63,.84,noise(float2(p.x*1.4,p.y*.20)+21))*.075*outer;tint=fixed3(.72,.70,.55);}
                    float home=exp(-pow((radius-1.08)/.23,2))*lerp(.25,1,detail)*.085;
                    amount=max(amount,home);float lift=(1-smoothstep(2,5.5,radius))*.026;
                    // Both wear and bleach retain the floor's real lighting.
                    // No abrupt switch between a dark stain and white paint.
                    fixed3 factor=1-(1-tint)*amount*edge*_Weight+fixed3(.94,.87,.72)*lift*edge*_Weight;
                    colour=fixed4(factor,1);
                }
                if(_Mode<.5){UNITY_APPLY_FOG(i.fogCoord,colour);}
                else {UNITY_APPLY_FOG_COLOR(i.fogCoord,colour,fixed4(1,1,1,1));}
                return colour;
            }
            ENDCG
        }
    }
}
