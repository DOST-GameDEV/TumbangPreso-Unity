// The lava's light on Dante's rocks round Rago's volcano (`FxStudySeanVolcano`): a second, additive draw of a rock that
// stands near the lava, warming only the faces that LOOK AT the lava.
//
// ⚠️ WHY A SECOND DRAW AND NOT HIS SHADER. The rocks are Dante's own meshes in his own approved material (`FxStudyRock`),
// and that file is his and is not edited. Its only warm inputs are `_Flash`, which tints the WHOLE piece toward one colour,
// and `_Heat`, which lights his molten gaps: v3 used `_Flash` and the owner's review said it plainly, a rock beside the
// lava took "a uniform warm tint per rock", not a glow on the side facing the lava. So the rock is drawn once more, over
// itself, by this: up to four lava points handed in per rock (`_LavaA`..`_LavaD`, world xyz, w a weight), and each face
// gains the glow colour by how squarely it faces the nearest of them and how close it is. The face turned away gets
// nothing, so the warmth reads as light thrown from the lava, not paint.
// ⚠️ IN PLAY THE LIGHT IS BAKED (`_Baked` 1, v6). The rocks and the lava never move against each other, so `SeanVolcano`
// works out each vertex's warmth ONCE, from the same four points and the same falloff, into its colour's red, and draws
// every warmed rock in ONE merged mesh: one draw instead of one per rock (13 to 15 of them).
// ⚠️ No `_Time`, no depth texture, no loops but the fixed four.
Shader "TumbangPreso/SeanVolcanoRockHeat"
{
    Properties
    {
        _HeatColor ("The lava's light", Color) = (1.0, 0.42, 0.12, 1)
        _Amount ("How strong at a face looking straight at it, touching", Range(0,2)) = 0.34
        _Reach ("How far the light carries (m)", Float) = 1.0
        _LavaA ("Lava point (world xyz, weight)", Vector) = (0, -100, 0, 0)
        _LavaB ("Lava point", Vector) = (0, -100, 0, 0)
        _LavaC ("Lava point", Vector) = (0, -100, 0, 0)
        _LavaD ("Lava point", Vector) = (0, -100, 0, 0)
        _Baked ("Read the warmth from the vertex colour's red (1) instead of the four points (0)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry+50" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _HeatColor;
            float _Amount, _Reach, _Baked;
            float4 _LavaA, _LavaB, _LavaC, _LavaD;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 colour:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float3 n:TEXCOORD0; float3 world:TEXCOORD1; float baked:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.n=UnityObjectToWorldNormal(v.normal);
                o.baked=v.colour.r;
                return o;
            }
            float lit(float4 l, float3 p, float3 n)
            {
                float3 d=l.xyz-p;
                float r=length(d);
                float fall=saturate(1.0-r/_Reach);
                return saturate(dot(n,d/max(r,1e-3))*1.15-0.05)*fall*fall*l.w;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.n);
                float k=_Baked>0.5?i.baked:max(max(lit(_LavaA,i.world,n),lit(_LavaB,i.world,n)),max(lit(_LavaC,i.world,n),lit(_LavaD,i.world,n)));
                return fixed4(_HeatColor.rgb*_Amount*k,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
