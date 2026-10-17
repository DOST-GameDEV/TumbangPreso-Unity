// BASTION's field, b4: HEAT AND EARTH RISING OFF THE SLABS, with no frame round it (`FxStudyDanteBastion`).
//
// ⚠️ WHY IT IS NOT A SHEET ANY MORE. The owner, of b3: "polish it more. the field is really ugly", and of another branch's:
// "make the hologram look more like its from earth and magma", "u js put 2d drawings on the hologram". b3 was a hard
// rectangle with a border of crust plates drawn round it: a picture on a pane. So here:
//   * NOTHING ENDS ON AN EDGE. The quad is wider and taller than the reflect zone (`_Size`, `_Zone`) and every layer has
//     fallen to nothing well before the quad's edge: the sides fall away raggedly past 1.45 m from the middle, and the top
//     is wherever the rising heat has been eaten, around the zone's 2.6 m.
//   * IT RISES. Heat comes off the slab tops and is EATEN UPWARD: the higher, the less of it survives, so its top is ragged
//     and moving. The zone still reads (it is where the heat stands, 3.4 m across, to about 2.6 m) without a line round it.
//   * IT IS SEE-THROUGH. What it covers is small (`_Clear`); most of it is laid on as light, so a player behind is seen.
// Three looks, for a board (`_Mode`), each after a reference named in `FxStudyDanteBastion`:
//   0 LICKS: thin tongues of heat off the slabs, hot low and cooling to a smoky red as they rise and are eaten, with
//     sparks and grit rising through them (the one filmed as b4);
//   1 SHIMMER AND EMBERS: almost clear, faint streaks of rising air and sparks and grit drifting up through it;
//   2 MAGMA VEIL: a thin skin of cooling magma rising off the slabs, its cracks hot and its crust nearly clear, torn away
//     from the top.
// The strikes are as b3's: light only (a white-hot point, a ring, cracks that cool in a second).
// ⚠️ TIME IS `_Age`. No `_Time`, no depth texture, no loops: the rules of `FxStudyRibbon`. Premultiplied.
Shader "TumbangPreso/FxStudyBastionHeat"
{
    Properties
    {
        _ErodeTex ("Cloud map", 2D) = "white" {}
        _StreakTex ("Streaked noise (`noise_simplex_cells`)", 2D) = "white" {}
        _Mode ("Look: 0 licks, 1 shimmer and embers, 2 magma veil", Float) = 0
        _Size ("The quad: its left edge, its foot, wide, high (m)", Vector) = (-2.1, 0.3, 4.2, 2.7)
        _Zone ("The reflect zone: half its width, its top, where the heat starts (m)", Vector) = (1.7, 2.6, 0.55, 0)
        _Age ("Clock (s, set per frame)", Float) = 0
        _Grow ("How far up it has risen (0 to 1)", Range(0,1)) = 1
        _Cool ("How far it has cooled (0 to 1)", Range(0,1)) = 0
        _Gone ("How far it has been eaten (0 to 1)", Range(0,1)) = 0
        _Clear ("The most it covers of what is behind (0 to 1)", Range(0,1)) = 0.10
        _Magma ("Magma at its hottest", Color) = (1.00, 0.70, 0.26, 1)
        _Molten ("Magma", Color) = (0.96, 0.34, 0.07, 1)
        _Cooled ("Magma cooling", Color) = (0.50, 0.12, 0.05, 1)
        _Smoke ("Smoke, where it has cooled", Color) = (0.24, 0.17, 0.14, 1)
        _HitA ("A strike: where (m across from the middle, m up), how long ago (s), on", Vector) = (0, 0, 9, 0)
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
            sampler2D _ErodeTex, _StreakTex;
            float4 _Size, _Zone, _HitA, _HitB, _HitC, _HitD;
            fixed4 _Magma, _Molten, _Cooled, _Smoke;
            float _Mode, _Age, _Grow, _Cool, _Gone, _Clear, _Emit;
            float _FxStudyBloom;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 p:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                // Metres: x across from the zone's middle, y up from the court.
                o.p=_Size.xy+v.uv*_Size.zw;
                return o;
            }
            // The masks in the grey they were painted in (the project is linear).
            float cloud(float2 q) { return pow(tex2D(_ErodeTex,q).r,0.4545); }
            // The streaked noise turned so its streaks stand up (they lie at about 30 degrees in the picture).
            float streak(float2 q) { float2 r=float2(q.x*0.866-q.y*0.5,q.x*0.5+q.y*0.866); return pow(tex2D(_StreakTex,r).r,0.4545); }
            float3 magma(float hot)
            {
                float3 c=lerp(_Smoke.rgb,_Cooled.rgb,saturate(hot*3.0));
                c=lerp(c,_Molten.rgb,saturate(hot*3.0-1.0));
                return lerp(c,_Magma.rgb,saturate(hot*3.0-2.0));
            }
            float hash(float2 c) { return frac(sin(dot(c,float2(127.1,311.7)))*43758.5453); }
            // A strike's light at p (heat 0 to 1). Light only: b3's first pass painted a dark pinwheel.
            float strike(float4 h,float2 p)
            {
                if(h.w<0.5) return 0;
                float2 d=p-h.xy;
                float r=length(d), t=h.z, cool=saturate(1.0-t);
                float core=exp(-r*16.0)*exp(-t*9.0);
                float wave=exp(-abs(r-0.6*saturate(t*1.6))*26.0)*saturate(1.0-t*1.6);
                float n=cloud(float2(atan2(d.y,d.x)*0.40+h.x*0.3,r*0.25+h.y*0.2));
                float lines=smoothstep(0.70,0.715,n)*(1.0-smoothstep(0.715,0.73,n));
                return saturate(core*2.0+wave*0.8+lines*smoothstep(0.45*cool+0.02,0.0,r)*cool);
            }
            // EMBERS AND GRIT: a cell grid that rises; each cell holds at most one spark at its own place, dying as it climbs;
            // one in four is a dark chip of grit, larger. Returns (spark light, grit cover); `hot` is the spark's heat.
            float2 embers(float2 p,float zone,float cut,float h,out float hot,out float grit)
            {
                float2 g=float2(p.x*5.0,p.y*2.6-_Age*1.1);
                float2 c=floor(g), f=frac(g)-0.5;
                float k=hash(c);
                float2 at=float2(hash(c+3.1)-0.5,hash(c+7.7)-0.5)*0.6;
                float d=length((f-at)*float2(1.0,0.55));
                grit=k>0.75?1.0:0.0;
                float size=grit>0.5?0.11:0.06;
                float dot_=(1.0-smoothstep(size*0.5,size,d))*step(0.45,k)*zone*smoothstep(cut-0.3,cut-0.1,0.5)*(1.0-h*0.6);
                hot=grit>0.5?0.0:dot_*(0.7+0.3*hash(c+1.3))*(1.0-0.6*_Cool);
                return float2(grit>0.5?0.0:dot_*0.9,grit>0.5?dot_*0.85:dot_*0.2);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.p;
                float half_=_Zone.x, top=_Zone.y, foot=_Zone.z;
                // How far up it has risen, and how much of the way up from its foot to the zone's top this point is.
                float reach=lerp(foot,top+0.35,_Grow);
                float h=saturate((p.y-foot)/(top-foot));
                // ⚠️ THE SIDES FALL AWAY: no straight edge, the zone's width wavering by a hand's breadth as it rises.
                float side=1.0-smoothstep(half_-0.30,half_+0.20,abs(p.x)+0.22*(cloud(float2(p.y*0.6+(p.x>0.0?0.3:0.7),_Age*0.05))-0.5));
                // Below the slab tops it is behind the stone anyway; it starts just under them, softly.
                float base=smoothstep(foot-0.25,foot+0.10,p.y);
                // ⚠️ ERODED UPWARD: the cut rises with height, so what survives high up is only the strongest of it, and the
                // top is ragged and moving. Past the zone's top nothing survives at all; above `reach` it has not risen yet.
                float cut=0.18+0.62*h+smoothstep(top-0.2,top+0.3,p.y)+smoothstep(reach-0.25,reach,p.y)*2.0+_Gone*1.4*(1.2-h);
                float zone=side*base;

                float light=0.0, cover=0.0, hot=0.0;
                float3 colour=0;
                if(_Mode<0.5)
                {
                    // LICKS: the streaked noise stood up and running upward, broken by the cloud; hot low, smoky high.
                    float a=streak(float2(p.x*0.42,p.y*0.16-_Age*0.22));
                    float b=cloud(float2(p.x*0.9+0.31,p.y*0.38-_Age*0.48));
                    // (Board v1: at full strength the licks were blobs of fire that hid him. Eaten faster as they rise, laid
                    // on as light, and covering at most half of `_Clear`.)
                    float lick=smoothstep(cut+0.10,cut+0.30,a*0.62+b*0.48+0.08*(1.0-h))*zone;
                    hot=lick*(1.0-0.75*h)*(1.0-0.65*_Cool);
                    colour=magma(hot*1.05);
                    light=lick*(0.42-0.30*h);
                    cover=lick*_Clear*0.5*(0.4+0.6*h);
                    // And sparks and grit rising through it.
                    float spark, grit;
                    float2 e=embers(p,zone,cut,h,spark,grit);
                    colour=lerp(colour,grit>0.5?_Smoke.rgb*0.7:magma(0.45+spark*0.6),saturate(e.x+e.y*2.0));
                    light=max(light,e.x); cover=max(cover,e.y); hot=max(hot,spark);
                }
                else if(_Mode<1.5)
                {
                    // SHIMMER: faint streaks of rising air, mostly light.
                    float a=streak(float2(p.x*0.6,p.y*0.10-_Age*0.30));
                    float air=smoothstep(0.55,0.85,a)*zone*(1.0-h*0.8);
                    float spark, grit;
                    float2 e=embers(p,zone,cut,h,spark,grit);
                    hot=spark;
                    colour=lerp(magma(0.45+spark*0.6),_Smoke.rgb*0.7,grit);
                    light=air*0.10+e.x;
                    cover=air*_Clear*0.3+e.y;
                    colour=lerp(colour,magma(0.5),saturate(air*0.6)*(1.0-saturate(e.x+e.y)));
                }
                else
                {
                    // MAGMA VEIL: a thin skin of cooling magma off the slabs. Its cracks (the streaked noise's dark lines, read
                    // twice crossing) are hot; its crust between them is nearly clear and darkens what is behind a little.
                    float a=streak(float2(p.x*0.55,p.y*0.30-_Age*0.12));
                    float b=streak(float2(-p.x*0.48+0.37,p.y*0.27-_Age*0.12+0.5));
                    float crack=max(1.0-smoothstep(0.03,0.10,abs(a-0.5)),1.0-smoothstep(0.03,0.10,abs(b-0.5)));
                    float skin=smoothstep(cut,cut+0.10,cloud(float2(p.x*0.7+0.2,p.y*0.45-_Age*0.12))*0.9+0.2)*zone;
                    hot=crack*skin*(1.0-0.7*h)*(1.0-0.7*_Cool);
                    colour=lerp(_Smoke.rgb,magma(hot),saturate(crack*1.4));
                    light=hot*0.85+skin*0.04;
                    cover=skin*_Clear*0.9+hot*0.2;
                }
                // The strikes, wherever they are.
                float s=max(max(strike(_HitA,p),strike(_HitB,p)),max(strike(_HitC,p),strike(_HitD,p)));
                colour=lerp(colour,magma(max(s,0.6)),saturate(s*1.3));
                light=max(light,s);
                cover=max(cover,s*0.5);
                hot=max(hot,s);
                float boost=1.0+_FxStudyBloom*_Emit*saturate(hot*1.4-0.4);
                // Premultiplied: light is added in full; only `cover` hides what is behind.
                return float4(colour*max(light,cover)*boost,saturate(cover));
            }
            ENDCG
        }
    }
    Fallback Off
}
