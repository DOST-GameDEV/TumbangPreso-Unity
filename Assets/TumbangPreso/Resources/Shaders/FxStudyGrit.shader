// THE GRIT, THROWN ON THE GPU: the flecks of `FxStudyRibbon`'s sheet (`FxFlecks`), but posed in the vertex shader from a mesh
// made ONCE, not rebuilt and uploaded every frame.
//
// ⚠️ WHY. `FxFlecks` lays every spark on the CPU each frame (four corners turned to the eye) and uploads the mesh again.
// For his BOULDER's grit that is thirty-four sparks rebuilt sixty times a second for as long as any is in the air. Every
// spark here is a ballistic arc from a start point and a push (`FxStudyDanteBoulder` works them out once, when it is
// placed), so the vertex shader can put it where it is at `_Age` and turn it to the camera itself: one mesh, uploaded
// once, nothing per frame but the clock.
//
// ⚠️ THE PICTURE IS `FxStudyRibbon`'S, UNCHANGED: the fragment below is that shader's own, copied whole, so the grit
// looks exactly as it did. Only the vertex stage is new. (`FxStudyRibbon` itself is another effect's and is not edited.)
//
// What a vertex carries: its spark's start (POSITION, the effect's own space) and push (NORMAL, m/s), its tint and how
// bright (COLOR), its corner of the sheet's dab (TEXCOORD0), when it is thrown and how long it is (TEXCOORD1: seconds on
// the effect's clock, metres), and which corner it is (TEXCOORD2: -1 or 1 across, 0 tail or 1 head, its width).
// A spark not yet thrown, or one on the floor, folds to a point and draws nothing, as `FxFlecks` leaves it out.
Shader "TumbangPreso/FxStudyGrit"
{
    Properties
    {
        _MaskTex ("Mask (u along, v across)", 2D) = "white" {}
        _ErodeTex ("Erosion map", 2D) = "white" {}
        _Deep ("Colour at the mask's dark grey", Color) = (0.20, 0.05, 0.55, 1)
        _Body ("Colour at the mask's mid grey", Color) = (0.62, 0.22, 0.95, 1)
        _Hot ("Colour at the mask's white", Color) = (1, 0.92, 1, 1)
        _HaloColor ("Halo colour (light, added)", Color) = (0.6, 0.2, 1, 1)
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("Opacity (set per frame)", Range(0,1)) = 1
        _Erode ("Erosion (0 fresh, 1 gone, set per frame)", Range(0,1)) = 0
        _Tile ("Mask tiling (along, across) and offset", Vector) = (1, 1, 0, 0)
        _Scroll ("Mask scroll a second (along, across)", Vector) = (0.6, 0, 0, 0)
        _ErodeTile ("Erosion tiling (along, across) and offset", Vector) = (1, 1, 0, 0)
        _ErodeScroll ("Erosion scroll a second", Vector) = (0, 0, 0, 0)
        _Cut ("Mask grey where the piece begins, and where it is solid", Vector) = (0.06, 0.30, 0, 0)
        _Curve ("How late the piece turns solid (1 straight; above 1 keeps mid greys see-through)", Range(0.3,4)) = 1
        _Stops ("Mask grey of the body colour, and of the hot colour (from, to)", Vector) = (0.45, 0.70, 0.95, 0)
        _Ends ("Fade at the root, fade at the tip, fade at each side, (unused)", Vector) = (0.08, 0.25, 0.0, 0)
        _Window ("Drawn from this u to this u, with this softness (set per frame)", Vector) = (-1, 2, 0.05, 0)
        _Sweep ("Erodes root first (+) or tip first (-)", Range(-2,2)) = 0
        _Soft ("Erosion edge softness", Range(0.005,0.5)) = 0.06
        _Burn ("Hot line along a torn edge", Range(0,2)) = 0
        _Lead ("Leading edge: v from which the piece is the lead colour (2 is off)", Range(0,2)) = 2
        _LeadColor ("Leading edge colour", Color) = (0.75, 0.95, 1, 1)
        _Shade ("Shading across the piece: how much darker at v 0", Range(0,1)) = 0
        _Warp ("Warp: how far (m, set per frame), waves along the piece, seed, speed", Vector) = (0, 2, 0, 1)
        _Hue ("Hue drift: degrees by slow noise, degrees by brightness, seed, noise scale", Vector) = (0, 0, 0, 1)
        _Halo ("Halo strength (only where nothing blooms)", Range(0,4)) = 0.6
        _Emit ("How far over white its bright parts go under a blooming camera", Range(0,8)) = 0.7
        _KeepHalo ("Keeps its own halo even where the camera blooms (a ribbon whose halo IS its faint edge)", Range(0,1)) = 0
        _Swap ("Mask laid crosswise: its x across the piece and its y along it", Range(0,1)) = 0
        _Sprite ("A glow sprite: a stand-in for bloom, dimmed where the camera blooms", Range(0,1)) = 0
        _Add ("Additive (1 light only, 0 covers)", Range(0,1)) = 0.6
        _Gravity ("How fast a spark falls (m/s each second)", Float) = 9.8
        _Gain ("Brightness", Range(0,6)) = 1
        _Polar ("Disc: uv is a square, read the erosion round its middle", Range(0,1)) = 0
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
            sampler2D _MaskTex, _ErodeTex;
            fixed4 _Deep, _Body, _Hot, _HaloColor, _LeadColor;
            float _Age, _Opacity, _Erode, _Sweep, _Soft, _Burn, _Lead, _Shade, _Halo, _Add, _Gain, _Polar, _Curve, _Emit, _Sprite, _KeepHalo, _Swap;
            // Set by a camera that blooms (the bench's): 1 there, and 0 everywhere else, the game included.
            float _FxStudyBloom;
            float4 _Tile, _Scroll, _ErodeTile, _ErodeScroll, _Cut, _Stops, _Ends, _Window, _Hue, _Warp;
            float _Gravity;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 colour:COLOR; float2 uv:TEXCOORD0; float2 thrown:TEXCOORD1; float3 corner:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 colour:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                float t=_Age-v.thrown.x;
                // Where its head is now and the way it is going: the arc `FxFlecks`' callers worked out on the CPU.
                float3 head=v.vertex.xyz+v.normal*t+float3(0.0,-0.5*_Gravity*t*t,0.0);
                float3 going=v.normal+float3(0.0,-_Gravity*t,0.0);
                float3 n=going/max(length(going),1e-6);
                // Its width turned to the eye, its length back along its way: `FxFlecks.Add`, corner for corner.
                float3 eye=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1.0)).xyz;
                float3 side=cross(n,eye-head);
                if(dot(side,side)<1e-8) side=cross(n,float3(0.0,1.0,0.0));
                side=normalize(side)*(v.corner.z*0.5);
                float3 p=(v.corner.y>0.5?head:head-n*v.thrown.y)+side*v.corner.x;
                // Not thrown yet, or on the floor: folded to a point.
                if(t<=0.0 || head.y<0.02) p=v.vertex.xyz;
                o.vertex=UnityObjectToClipPos(float4(p,1.0));
                o.uv=v.uv;
                o.colour=v.colour;
                return o;
            }
            // The grey the picture was painted in (see the header).
            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }
            // Turns a colour round the grey axis: a hue shift that leaves its lightness where it was.
            float3 turnHue(float3 c,float degrees)
            {
                float a=radians(degrees), k=0.57735;
                float3 axis=float3(k,k,k);
                return c*cos(a)+cross(axis,c)*sin(a)+axis*dot(axis,c)*(1.0-cos(a));
            }
            float4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float2 at=lerp(uv,uv.yx,step(0.5,_Swap))*_Tile.xy+_Tile.zw-_Scroll.xy*_Age;
                // The vertex colour's alpha thins the mask (a mesh can dim itself toward an edge); its rgb tints the piece.
                float m=grey(tex2D(_MaskTex,at).r)*i.colour.a;
                // The halo: the same picture from two of its small mips, which is the picture blurred.
                float wide=(grey(tex2Dlod(_MaskTex,float4(at,0,3.0)).r)+grey(tex2Dlod(_MaskTex,float4(at,0,5.0)).r))*.5*i.colour.a;

                // Where the piece is at all: its own ends and sides, and the window that is open along it just now.
                float root=_Ends.x>0.0? smoothstep(0.0,_Ends.x,uv.x):1.0;
                float tip=_Ends.y>0.0? smoothstep(0.0,_Ends.y,1.0-uv.x):1.0;
                float side=_Ends.z>0.0? smoothstep(0.0,_Ends.z,uv.y)*smoothstep(0.0,_Ends.z,1.0-uv.y):1.0;
                float open=smoothstep(_Window.x-_Window.z,_Window.x+_Window.z,uv.x)*(1.0-smoothstep(_Window.y-_Window.z,_Window.y+_Window.z,uv.x));
                float shape=root*tip*side*open;

                // Erosion. The threshold runs from below the map's darkest to above its brightest, so 0 leaves the
                // piece whole and 1 leaves nothing; `_Sweep` makes one end go first.
                float2 eat=uv*_ErodeTile.xy+_ErodeTile.zw-_ErodeScroll.xy*_Age;
                float along=uv.x;
                if(_Polar>.5)
                {
                    float2 c=uv-.5;
                    along=saturate(length(c)*2.0);
                    eat=uv;
                }
                float e=grey(tex2D(_ErodeTex,eat).r)+_Sweep*(along-.5);
                float span=1.0+abs(_Sweep)+_Soft*2.0;
                float threshold=_Erode*span-abs(_Sweep)*.5-_Soft;
                float kept=smoothstep(threshold,threshold+_Soft,e);
                float torn=kept*(1.0-smoothstep(threshold+_Soft,threshold+_Soft*2.5,e))*step(0.001,_Erode);

                float v=m*shape;
                float alpha=pow(smoothstep(_Cut.x,_Cut.y,v),_Curve)*kept;
                fixed3 colour=lerp(_Deep.rgb,_Body.rgb,smoothstep(_Cut.x,_Stops.x,v));
                colour=lerp(colour,_Hot.rgb,smoothstep(_Stops.y,_Stops.z,v));
                // A two-tone wisp: one side of it is the pale lead colour, hard.
                float lead=smoothstep(_Lead-.03,_Lead+.03,uv.y);
                colour=lerp(colour,_LeadColor.rgb,lead);
                colour*=lerp(1.0,lerp(1.0-_Shade,1.0,uv.y),1.0-lead);
                colour+=_Hot.rgb*torn*_Burn;
                // ⚠️ HUE DRIFT. The owner, of the references: "theres definitely some hue randomized offsetter", and of ours:
                // "you forget to add transparency and hue shifts". One flat colour is what a first attempt looks like. So the
                // colour is turned a little by a slow noise that drifts along the piece (the erosion map read very coarse)
                // and a little by how bright the piece is there, each piece from its own seed.
                float slow=grey(tex2Dlod(_ErodeTex,float4(uv*float2(0.45,0.30)*_Hue.w+float2(_Hue.z+_Age*0.06,_Hue.z*1.7),0,2.0)).r)-0.5;
                colour=saturate(turnHue(colour,_Hue.x*slow*2.0+_Hue.y*(v-0.5)));
                colour*=i.colour.rgb;

                alpha*=_Opacity;
                // The halo is cut toward the tip twice over, so a ribbon ends in its mask's own shape and not in a haze.
                float3 light=_HaloColor.rgb*wide*shape*tip*tip*kept*_Halo*_Opacity;
                // ⚠️ GLOW IS THE CAMERA'S WHERE THE CAMERA BLOOMS. The owner: "you're putting the wisp glow on the texture and not
                // as an emmissive/actual glwo". So under a blooming camera the halo painted here is dropped, the stand-in
                // glow sprites are dimmed, and the bright part of a piece that is LIGHT (`_Add`) is pushed over white for
                // the bloom to spread; a piece that covers (a dark blade) is never pushed, and so stands dark against the
                // bloom behind it. Where nothing blooms (the game today) all of that is as it was.
                float hot=smoothstep(_Stops.y,_Stops.z,v)*_Add;
                float boost=1.0+_FxStudyBloom*_Emit*hot;
                float dim=lerp(1.0,lerp(1.0,0.22,_Sprite),_FxStudyBloom);
                return float4(colour*alpha*_Gain*boost*dim+light*(1.0-_FxStudyBloom*(1.0-_KeepHalo)),alpha*(1.0-_Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
