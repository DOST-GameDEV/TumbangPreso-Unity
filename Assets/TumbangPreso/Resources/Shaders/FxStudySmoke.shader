// The effects study's smoke (`FxSmoke` in `Runtime/Visual/FxStudy/FxStudySmoke.cs`): a pile of rough balls.
//
// ⚠️ WHY SMOKE IS BALLS. It was first flat ink splats, then single lathed lumps that wobbled. The owner, twice: "the
// dark smoke changes mesh btw, yours is just the same mesh shape all throughout", and "the smoke again is not just one
// smooth shape. its more like multiple rough spheres". Stylised smoke is a CLUSTER of many balls of different sizes,
// born small at the source, swelling, drifting and eaten, so the outline of the mass is never the same twice.
//
// ⚠️ AND WHY THE CLUSTER, NOT THE BALL, DECIDES THE LIGHT. Lit ball by ball, with a dark underside on every one, the
// same cluster read as a heap of painted bowling balls: on the scorch, twice, and as "a scatter of solid blue balls"
// on the ice flower. What makes a pile of balls one body of smoke is all here:
//   * ONE MASS, ONE LIGHT, BUT STILL BILLOWS: each ball brings the way it lies from the middle of the whole cluster
//     (`TEXCOORD1.xyz`), and a third of the light is taken from that and the rest from the ball's own surface
//     (`_Mass`). So the top and the lit side of the WHOLE cluster are lighter and its underside and far side
//     darker, and every ball still has its own lit cap and its own shadow.
//     ⚠️ A THIRD, NOT TWO THIRDS. At 0.62 each plume went one flat tone from top to bottom and read as a single
//     smooth boulder: the balls' own shading is what makes them billows, and the owner's picture of the look has
//     a clear dark crescent on every one;
//   * STEPPED: lit, mid and dark, flat, with a short soft step between;
//   * THE CREASE ONLY WHERE THERE IS A GAP: the dark line under a ball is drawn at the strength the ball brings
//     (`TEXCOORD1.w`), which `FxSmoke` sets low for a ball sunk deep into the one below it. Two balls that are really
//     one lump are not cut apart by a line; two that only touch are;
//   * A RAGGED RIM: toward its outline a ball is eaten by the erosion map, so it does not end in a perfect circle and
//     where two cross there is no hard arc (`_Rim`);
//   * EATEN, NOT FADED: each ball carries how far gone it is in its vertex alpha, and the map is cut at that, so an
//     old ball tears into flakes while a young one beside it is whole.
// Solid and depth-written, so the balls cut into each other correctly; drawn before the see-through pieces.
// A mesh that brings no `TEXCOORD1` (the scorch's rubble wears this shader too) is lit by its own surface alone.
// A cluster built without `oneMass` (the wisp's) brings none of the way it lies either, with the line at full
// strength, no bite at its rim and `_Crease` where it used to be: that is this shader as it was before all of the above.
// ⚠️ No `_Time`, no depth texture, no loops: the rules of `FxStudyRibbon`.
Shader "TumbangPreso/FxStudySmoke"
{
    Properties
    {
        _Lit ("Lit tone", Color) = (0.62, 0.60, 0.72, 1)
        _Mid ("Mid tone", Color) = (0.34, 0.32, 0.46, 1)
        _Dark ("Dark tone (undersides and creases)", Color) = (0.12, 0.11, 0.20, 1)
        _Key ("Where the light comes from (world)", Vector) = (0.3, 0.85, -0.4, 0)
        _Mass ("How much the whole cluster decides the light, and not the ball", Range(0,1)) = 0.32
        _Rim ("Ragged rim: where it starts (0 facing us, 1 the outline), how deep it bites", Vector) = (0.72, 0.28, 0, 0)
        _Crease ("The dark underside of a ball: the height on it (0 foot, 1 crown) where it ends, from and to", Vector) = (0.16, 0.42, 0, 0)
        _ErodeTex ("Erosion map", 2D) = "white" {}
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("(unused: smoke is eaten, never faded)", Range(0,1)) = 1
        _Erode ("(unused: each ball carries its own)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Lit, _Mid, _Dark;
            float4 _Key, _Rim, _Crease;
            sampler2D _ErodeTex;
            float _Age, _Opacity, _Erode, _Mass;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 mass:TEXCOORD1; fixed4 colour:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float4 mass:TEXCOORD2; float3 view:TEXCOORD3; fixed4 colour:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.mass=float4(mul((float3x3)unity_ObjectToWorld,v.mass.xyz),v.mass.w);
                o.view=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;
                o.colour=v.colour;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                // The map was painted in plain grey and the project is linear: read it back as painted.
                float map=pow(tex2D(_ErodeTex,i.uv*float2(2.0,1.0)+i.colour.a*0.3).r,0.4545);
                float gone=i.colour.a*1.06-0.03;
                // Toward its outline the ball is bitten by the same map, more the nearer the outline.
                float rim=1.0-saturate(dot(n,normalize(i.view)));
                clip(map-gone-smoothstep(_Rim.x,1.0,rim)*_Rim.y);
                // The light: mostly where this ball lies in the whole cluster, partly its own surface.
                float has=step(0.0001,dot(i.mass.xyz,i.mass.xyz));
                float3 whole=normalize(i.mass.xyz+float3(0,1e-5,0));
                float3 facing=normalize(lerp(n,whole,_Mass*has));
                float light=dot(facing,normalize(_Key.xyz));
                fixed3 colour=lerp(_Dark.rgb,_Mid.rgb,smoothstep(0.02,0.10,light));
                colour=lerp(colour,_Lit.rgb,smoothstep(0.58,0.66,light));
                // The crease under a ball, as strong as the ball says: full where it only touches the one below, faint
                // where it is sunk into it.
                colour=lerp(colour,_Dark.rgb,(1.0-smoothstep(_Crease.x,_Crease.y,i.uv.y))*i.mass.w);
                // A thin lit line where it is being eaten.
                colour+=_Lit.rgb*0.35*(1.0-smoothstep(0.0,0.05,map-gone))*step(0.02,i.colour.a);
                return fixed4(colour*i.colour.rgb,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
