// COLD FEET's snowflakes (`CheskaColdFeetLook`). One mesh, one quad a flake, everything off `_Age`, nothing written per frame but that.
//   * The big one CRYSTALLIZES: a growth field worked out from the picture itself (`Arrival`) says when frost reaches each point. It runs up
//     an arm, then out along the branch that leaves the arm there, slower, so the branches grow off the arms in order behind the front.
//   * A rim flake snaps on where and when an arm's tip reaches the ring (`CheskaColdFeetLook` works out the moment), with a small flash.
//   * It lies still. A rim flake now and then twinkles: its own slow phase, a few moments in a long while, never all at once.
//   * It melts by the same field run backwards: tips and branch ends first, the edge frayed by noise, the thin lines thinning before they
//     break. There is no alpha fade anywhere: what is still there is still solid. Thin bits left behind glint and go.
//   * Under each lying flake a very faint cold breath, no more than a tenth. It shrinks to nothing as a flake melts, never fades.
// Between the end of the growth and the start of the melt the field is not worked out at all, so a formed flake costs what it always did.
// Alpha blended and never additive: it has to hold on a pale street in daylight with no bloom. The picture is the lotus's own `frost_fern`.
Shader "TumbangPreso/CheskaColdFeetFlakes"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("The lotus's frost", 2D) = "black" {}
        _Edge ("Thin parts", Color) = (0.42, 0.80, 0.96, 1)
        _Heart ("Thick parts", Color) = (0.97, 1.00, 1.00, 1)
        _Breath ("Cold breath under a flake", Color) = (0.55, 0.86, 1.00, 1)
        _BreathStrength ("Its strength", Range(0,0.4)) = 0.17
        _Age ("Owner's clock (s)", Float) = 0
        _DieAt ("Owner's clock at which the melting starts", Float) = 100000
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Edge, _Heart, _Breath;
            float _BreathStrength, _Age, _DieAt;
            // Keep these with `CheskaColdFeetLook`'s, which times the rim from them.
            static const float GrowSeconds=0.44, GrowReach=2.10, GrowPower=1.8, GrowStart=0.02, ArmStagger=0.12;
            // TEXCOORD1: the flake's middle on the ground and half its width. TEXCOORD2: when it appears, seconds it takes to melt, when it starts after `_DieAt`, seed.
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 mid:TEXCOORD1; float4 life:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 info:TEXCOORD1; };

            float Hash(float2 p){ p=frac(p*float2(0.3183,0.3679))*17.0; return frac(p.x*p.y*(p.x+p.y)); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=p-c; f=f*f*(3.0-2.0*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+float2(1,1)),f.x),f.y);
            }
            // When frost reaches this point of the picture: 0 at the heart, 1 at an arm's tip, past 1 out along the last branches. The picture's arms
            // are at 45.1 degrees and every 60 from there (measured), tips at 0.92, and its branches leave an arm at about 55 degrees: a point at `s` up
            // the arm and `d` off it sits on the branch that left at `s-d/tan55`, and is `d/sin55` along it. Each arm runs a little late or early.
            float Arrival(float2 pic)
            {
                float rr=length(pic)*2.0/0.92, th=atan2(pic.y,pic.x)-0.7871, k=round(th/1.0472), phi=th-k*1.0472;
                float s=rr*cos(phi), d=rr*abs(sin(phi));
                float late=frac(fmod(k+6.0,6.0)*0.618);
                return (max(0.0,s-d*0.70)+d*1.22*2.6)*(1.0+ArmStagger*late);
            }
            v2f vert(appdata v)
            {
                v2f o;
                float seed=v.life.w, t=_Age-v.life.x;
                float born=step(0.0,t), small=step(v.mid.w,0.68);
                float melt=saturate((_Age-_DieAt-v.life.z)/v.life.y);
                // The corners the ground gave it, tipped a little to the eye. A rim flake draws in a little as it melts, easing into it.
                // The big one does not: scaled, all 5 m of its picture started sliding at once on the frame its melt began, and that was a snap.
                float3 lying=lerp(v.vertex.xyz,v.mid.xyz,0.16*melt*melt*small);
                float3 camRight=mul((float3x3)unity_CameraToWorld,float3(1,0,0)), camUp=mul((float3x3)unity_CameraToWorld,float3(0,1,0));
                camRight=mul((float3x3)unity_WorldToObject,camRight); camUp=mul((float3x3)unity_WorldToObject,camUp);
                float3 tipped=v.mid.xyz+(camRight*(v.uv.x-0.5)+camUp*(v.uv.y-0.5))*2.0*v.mid.w;
                // The big one stays flat on the ground: tipped by this share it would stand 16 cm up at its far side.
                lying=lerp(lying,tipped,0.13*(1.0-melt*melt)*small);
                // A rim flake snaps out past its size and settles. The big one grows in the picture and does not scale.
                float flash=small*born*exp(-max(t,0.0)/0.075);
                float grow=saturate(t/0.06);
                float pop=lerp(1.0,lerp(0.30,1.0,grow*(2.0-grow))*(1.0+0.30*flash),small);
                float3 p=v.mid.xyz+(lying-v.mid.xyz)*pop;
                p=lerp(v.mid.xyz,p,born);
                o.vertex=UnityObjectToClipPos(float4(p,1.0));
                // The picture is turned, not the quad, so the corners stay on the ground. Fixed: a formed flake holds still. It used to wobble
                // three degrees, which at 2.45 m is 7 cm a second of creep at the tips.
                float a=seed*6.2832;
                float2 c=v.uv-0.5, turn=float2(cos(a),sin(a));
                o.uv=float2(c.x*turn.x-c.y*turn.y,c.x*turn.y+c.y*turn.x);
                // The rim only: on the big one this whitened all 5 m of it at once.
                float twinkle=small*pow(saturate(sin(_Age*(0.9+seed*1.3)+seed*50.0)),14.0);
                o.info=float4(melt,twinkle,flash,1.0-small);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv)*2.0;
                float2 pic=i.uv*1.38;
                float m=tex2D(_MainTex,pic+0.5).r;
                float big=i.info.w, melt=i.info.x, flash=i.info.z;
                // At 5 m across the picture's lines are hairlines. The big one reads it spread by four taps about 1.5 cm out, enough to hold a line at game
                // distance. The owner, of 4 cm: "the center snowflake is a bit too thick". Its cut is higher too, so the picture's soft halo stays out and the heart opens.
                if(big>0.5)
                {
                    float o=0.003*smoothstep(0.08,0.34,length(pic)*2.0);
                    m=max(m,max(tex2D(_MainTex,pic+0.5+float2(o,o)).r,tex2D(_MainTex,pic+0.5+float2(-o,o)).r));
                    m=max(m,max(tex2D(_MainTex,pic+0.5+float2(o,-o)).r,tex2D(_MainTex,pic+0.5+float2(-o,-o)).r));
                }
                m*=1.0-smoothstep(0.92,1.0,length(pic)*2.0);
                m=pow(m,0.75);
                float baseCut=lerp(0.20,0.36,big), cut=baseCut-flash*0.12, soft=0.20, reveal=1.0, hot=0.0, reach=1.0, spark=0.0;
                bool growing=big>0.5&&_Age<GrowSeconds+0.22;
                if(growing||melt>0.0)
                {
                    float2 nuv=pic*lerp(5.0,1.0,big);
                    float n=Noise(nuv*26.0)*0.62+Noise(nuv*67.0)*0.38-0.5;
                    float at=Arrival(pic);
                    if(growing)
                    {
                        // Fast off the mark and easing to the ring. The zone is live from the first frame, so the heart is there on the first frame.
                        float x=saturate((_Age+GrowStart)/GrowSeconds);
                        // Past the ease it keeps going, so the last branch ends are all clear of it before the growth stops being worked out: no jump at the hand-over.
                        float front=GrowReach*(1.0-pow(1.0-x,GrowPower))+0.6*saturate((_Age-GrowSeconds)/0.22);
                        // The melt's own edge, run the other way: the thick of a line shows first and it thickens to its full width
                        // behind the front, so an arm arrives as a thin bright skeleton. A plain wipe cut the dense picture into a solid star.
                        float dead=smoothstep(front-0.42,front,at+n*0.42);
                        cut=lerp(baseCut,1.14,dead);
                        soft=lerp(0.20,0.10,dead);
                        // White at the front, letting go to the formed colour behind it.
                        hot=max(dead,0.30*saturate(1.0-_Age/(GrowSeconds+0.16)));
                        reach=smoothstep(0.0,0.30,front*0.45-r)*step(0.0001,front);
                    }
                    if(melt>0.0)
                    {
                        // The growth run backwards, the front passing every part: `dead` 0 is untouched, 1 is gone.
                        float e=melt*melt*(3.0-2.0*melt)*0.35+melt*0.65;
                        float front=lerp(2.0,-0.27,e);
                        // `begin` lets it in over the first of the melt: the far twigs are already past this front, and they would all change on one frame.
                        float begin=smoothstep(0.0,0.14,melt);
                        float dead=smoothstep(front-0.42,front,at+n*0.42)*begin;
                        cut=lerp(baseCut,1.14,dead);
                        soft=lerp(0.20,0.10,dead);
                        hot=max(hot,dead*0.65);
                        // What is left of a thin line, glinting as it goes: a small star at a few places the ice was, each once.
                        float cells=lerp(9.0,16.0,big);
                        float2 cell=floor(pic*cells), q=pic*cells-cell-0.5;
                        float2 cc=(cell+0.5)/cells;
                        float was=tex2Dlod(_MainTex,float4(cc+0.5,0,2)).r;
                        float glint=saturate((Arrival(cc)-front)/0.26);
                        if(Hash(cell+7.0)>0.50&&was>0.10&&glint>0.0&&glint<1.0)
                        {
                            float star=max(saturate(1.0-abs(q.x)*11.0)*saturate(1.0-abs(q.y)*2.3),saturate(1.0-abs(q.y)*11.0)*saturate(1.0-abs(q.x)*2.3));
                            star=max(star,saturate(1.0-length(q)*6.0));
                            spark=smoothstep(0.12,0.40,star*sin(glint*3.1416))*begin;
                        }
                    }
                }
                float a=smoothstep(cut,cut+soft,m)*reveal;
                float3 col=lerp(_Edge.rgb,_Heart.rgb,saturate(smoothstep(lerp(0.35,0.46,big),lerp(0.80,0.92,big),m)+i.info.y*0.7+hot+flash));
                // The breath: a flash lifts it for a moment. It reaches only as far as the frost has, and draws in as the flake melts.
                float keep=1.0-saturate(melt*1.5);
                keep*=keep;
                float br=saturate(1.0-r/max(keep,0.0001));
                float breath=br*br*_BreathStrength*reach*(1.0+flash*3.0);
                float alpha=max(a,breath);
                col=lerp(_Breath.rgb,col,saturate(a*2.0));
                col=lerp(col,_Heart.rgb,spark);
                alpha=max(alpha,spark);
                return fixed4(col,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
