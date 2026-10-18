// BASTION's field: the sheet of heat that stands on Basilio's slabs (`FxStudyDanteBastion`, the bench; the slabs are
// `FxStudyRock.shader`). It is the reflect area made visible (`DanteHeroKit.Barrier`): `GeoRules.BarrierWidth` wide,
// from the slab tops to 2.6 m, where a slipper thrown at him is turned back.
//
// ⚠️ WHAT IT IS MADE OF, AND WHY. The owner, of another branch's field: "make the hologram look more like its from earth
// and magma" and "u js put 2d drawings on the hologram". So nothing here is a picture laid on a sheet:
//   * THE MIDDLE IS CLEAR. A warm tint at a few percent and heat rising through it, so a player behind it is seen as
//     well as one in front. It never hides anybody: the most the middle covers is `_Clear` (5 percent).
//   * THE RIM IS MOLTEN ROCK. Down both sides and along the top a band of magma with a ragged inner edge, crusted over
//     with basalt that has cooled on it: the same palette as his BOULDER's gaps, and the same cloud map its stone wears.
//     It is the edge of the reflect, and it says so.
//   * A SLIPPER THAT STRIKES IT lights where it struck: a white-hot point, a ring of heat running out, and cracks that
//     cool from yellow through red to the crust's dark in about a second (`_HitA` to `_HitD`).
//   * IT IS READ FROM BOTH SIDES (`Cull Off`): the thrower sees it, and so does Basilio behind it.
//   * IT RISES from the slab tops (`_Grow`), COOLS in its last 1.5 s (`_Cool`) and is EATEN FROM THE TOP DOWN at the
//     end (`_Gone`), never faded.
// ⚠️ TIME IS `_Age`, set by whoever plays it. No `_Time`, no depth texture, no loops: the rules of `FxStudyRibbon`.
// Premultiplied, as `FxStudyRibbon` is.
Shader "TumbangPreso/FxStudyBastionField"
{
    Properties
    {
        _ErodeTex ("Cloud map (the rim's crust, the heat, the bite)", 2D) = "white" {}
        _Size ("The sheet in metres: wide, high", Vector) = (3.4, 2.1, 0, 0)
        _Age ("Clock (s, set per frame)", Float) = 0
        _Grow ("How far up it has risen (0 to 1)", Range(0,1)) = 1
        _Cool ("How far it has cooled (0 to 1)", Range(0,1)) = 0
        _Gone ("How far it has been eaten (0 to 1)", Range(0,1)) = 0
        _Clear ("How much of what is behind the middle covers at most (0 to 1)", Range(0,1)) = 0.05
        _Rim ("The rim: narrowest and widest (m), where its crust starts in the cloud map (higher less crust), how many plates a metre", Vector) = (0.10, 0.24, 0.47, 2.2)
        _Magma ("Magma at its hottest", Color) = (1.00, 0.66, 0.22, 1)
        _Molten ("Magma", Color) = (0.94, 0.30, 0.06, 1)
        _Cooled ("Magma cooling", Color) = (0.46, 0.10, 0.04, 1)
        _Crust ("The crust on it (basalt)", Color) = (0.20, 0.17, 0.15, 1)
        _Heat ("The tint of the middle", Color) = (1.00, 0.58, 0.28, 1)
        _HitA ("A strike: where (m across, m up), how long ago (s), on", Vector) = (0, 0, 9, 0)
        _HitB ("A strike", Vector) = (0, 0, 9, 0)
        _HitC ("A strike", Vector) = (0, 0, 9, 0)
        _HitD ("A strike", Vector) = (0, 0, 9, 0)
        _Emit ("How far over white the magma goes under a blooming camera", Range(0,8)) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _ErodeTex;
            float4 _Size, _Rim, _HitA, _HitB, _HitC, _HitD;
            fixed4 _Magma, _Molten, _Cooled, _Crust, _Heat;
            float _Age, _Grow, _Cool, _Gone, _Clear, _Emit;
            float _FxStudyBloom;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 p:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.p=v.uv*_Size.xy;
                return o;
            }
            // The cloud map in the grey it was painted in (the project is linear).
            float cloud(float2 q) { return pow(tex2D(_ErodeTex,q).r,0.4545); }
            // Magma by how hot it is: crust-dark, cooled red, molten, white-hot.
            float3 magma(float hot)
            {
                float3 c=lerp(_Crust.rgb,_Cooled.rgb,saturate(hot*3.0));
                c=lerp(c,_Molten.rgb,saturate(hot*3.0-1.0));
                return lerp(c,_Magma.rgb,saturate(hot*3.0-2.0));
            }
            // One strike's light and cover at p: (heat 0 to 1, cover 0 to 1). ⚠️ IT IS ONLY LIGHT AND ONLY HOT: a first pass
            // drew its cracks down through the crust's dark as well, and they read as a black and orange pinwheel painted on.
            float2 strike(float4 h,float2 p)
            {
                if(h.w<0.5) return 0;
                float2 d=p-h.xy;
                float r=length(d), t=h.z;
                float cool=saturate(1.0-t);
                // The white-hot point, gone in a quarter second.
                float core=exp(-r*16.0)*exp(-t*9.0);
                // A ring of heat running out to about 0.6 m and gone by then.
                float ring=0.6*saturate(t*1.6);
                float wave=exp(-abs(r-ring)*26.0)*saturate(1.0-t*1.6);
                // Cracks: hairlines out from the point, picked from the cloud map round it, shortening as they cool.
                float a=atan2(d.y,d.x);
                float n=cloud(float2(a*0.40+h.x*0.3,r*0.25+h.y*0.2));
                float lines=smoothstep(0.70,0.715,n)*(1.0-smoothstep(0.715,0.73,n));
                float crack=lines*smoothstep(0.45*cool+0.02,0.0,r)*cool;
                float heat=saturate(core*2.0+wave*0.8+crack);
                return float2(heat,saturate(heat*1.1));
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.p;
                float W=_Size.x, H=_Size.y;
                // Risen this far from the slab tops, with a molten front.
                float top=_Grow*H;
                clip(top-p.y);
                // Eaten from the top down: the cloud map at a cut that runs down the sheet.
                float bite=cloud(p*0.55+0.3)*0.5+0.5*(p.y/H);
                float left=(1.0-_Gone*1.3)-bite;
                clip(left);

                float edge=min(min(p.x,W-p.x),top-p.y);
                float slow=cloud(p*0.9+0.13);
                float width=lerp(_Rim.x,_Rim.y,slow);
                // The band and its ragged inner side.
                float ragged=width*(0.70+0.6*cloud(p*2.6+0.41));
                float band=1.0-smoothstep(ragged*0.60,ragged,edge);
                // ⚠️ THE CRUST IS PLATES WITH RELIEF, NOT A PATTERN. Broad plates of basalt cooled on the magma (the cloud map
                // read large), each lit along its upper edge and shadowed along its lower one from the same map read a
                // little higher, so they stand off the magma; between them the magma shows in cracks. A first pass laid
                // small flakes and read as an orange dashed line drawn round the sheet.
                float2 q=p*_Rim.w+0.77;
                float plate=cloud(q), above=cloud(q+float2(0.0,0.05));
                float threshold=_Rim.z-_Cool*0.30;
                float crust=smoothstep(threshold-0.015,threshold+0.015,plate);
                float relief=saturate((plate-above)*9.0+0.5);
                float crack=(1.0-crust)*smoothstep(threshold-0.10,threshold,plate);
                float hotRim=lerp(0.95,0.32,_Cool)*(0.82+0.18*cloud(float2(p.x*1.7,p.y*1.7-_Age*0.35)));
                float rimHeat=hotRim*(1.0-crust)*(0.55+0.45*crack+0.45*saturate(1.0-edge/max(ragged,1e-3)));
                // The rising front, while it rises: a line of magma along the top.
                float front=(1.0-smoothstep(0.0,0.06,top-p.y))*saturate((1.0-_Grow)*6.0);

                // ⚠️ THE MIDDLE ADDS A LITTLE LIGHT AND COVERS ALMOST NOTHING. Heat rising off the slabs (a haze low down,
                // thin strands higher) is laid on mostly as light; what it covers is `_Clear` at most. A first pass tinted
                // the whole sheet and it read as an orange curtain.
                float rising=cloud(float2(p.x*1.1+0.2,p.y*0.35-_Age*0.32));
                // Strands rise off the slabs and are spent by two thirds of the way up (at the top they hung like drips).
                float strand=smoothstep(0.62,0.90,rising)*(1.0-_Cool*0.7)*saturate(1.3-1.6*p.y/H);
                float haze=saturate(1.0-p.y/0.6);
                float glow=0.10*haze+0.08*strand;
                float body=_Clear*(0.3+0.5*haze+0.4*strand);

                // The strikes.
                float2 s=strike(_HitA,p); float2 t=strike(_HitB,p);
                float2 u=strike(_HitC,p); float2 v=strike(_HitD,p);
                float hitHeat=max(max(s.x,t.x),max(u.x,v.x));
                float hitCover=max(max(s.y,t.y),max(u.y,v.y));

                float heat=max(max(rimHeat*band,front),hitHeat);
                float3 rock=_Crust.rgb*lerp(0.55,1.9,relief);
                float3 rim=lerp(magma(rimHeat),rock,crust);
                float3 colour=_Heat.rgb*glow/max(body,1e-3);
                float cover=body;
                // The rim over the middle, then the front and the strikes over both.
                colour=lerp(colour,rim,band); cover=lerp(cover,0.95,band);
                colour=lerp(colour,magma(max(front,hitHeat)),saturate(front+hitCover*1.2)); cover=max(cover,max(front,hitCover));
                // The bite's edge glows as it is eaten.
                float lip=(1.0-smoothstep(0.0,0.06,left))*step(0.001,_Gone);
                colour=lerp(colour,magma(0.8),lip);
                cover=max(cover,lip*0.8);
                float boost=1.0+_FxStudyBloom*_Emit*saturate(heat*1.4-0.4)*(1.0-crust*band);
                // Premultiplied: the middle's light is added in full while it covers only `body`.
                return float4(colour*max(cover,glow)*boost,cover);
            }
            ENDCG
        }
    }
    Fallback Off
}
