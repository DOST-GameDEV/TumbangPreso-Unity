// Rago's volcano's lava (`SeanVolcano`, reviewed on the bench by `FxStudySeanVolcano`): the flows in their channels and
// the pools they feed, ALIVE, posed entirely here from one clock.
//
// The owner, 2026-11-23, of the v2 model: "i want there to be real flowing lava with emissives"; "the lava paths' edges
// also read like straight stickers that dont blend seamlessly"; of the ends: "the red spots dont sell it ... you'd
// typically expect a flowing pool of lava". So:
//   * A FLOW (`_Pool` 0). UV0: u across the strip, v down it (in repeats of the crust field); UV1: (which flow, how far
//     down it, 0 at the source to 1 where it has cooled). The strip is wider than the molten river (the builder's SKIRT,
//     1.75): the middle is molten, the rest is crust lapping over the channel's banks.
//       - THE CRUST DRIFTS: two copies of the crust field (`_CrustTex`) slide down the flow and cross-fade on a two-phase
//         clock, so it flows forever without ever smearing. The middle runs faster than the banks (`_Flow`).
//       - THE CRUST BREAKS: how much of a plate is open molten rock depends on how hot it is there: in the hot middle the
//         plates are small floating rafts on a bright river, toward the banks and down the flow they close up into crust
//         with thin glowing cracks.
//       - IT BLENDS INTO THE ROCK: the molten edge wanders with a noise (never parallel-sided), a warm heat glow falls off
//         across the crust onto the bank, the crust darkens into the scorched rock's own colour (`_Scorch`, the body
//         paint's bank tone), and the outer edge is torn by the noise and clipped.
//       - IT COOLS: down the flow the river narrows and its colour steps down; a flow that stops on the slope ends in a
//         blunt, lobed TOE of crust (UV1.x over 1) with a narrow glowing heart, flaking out round its front: never a
//         point, never a red spot.
//   * A POOL (`_Pool` 1). UV0 planar; UV1.y how far out (the builder's 0.2 to 0.35 in the middle, 1 at the rim). It churns:
//     the same two-phase crust drifts along a slow swirl read off the cloud map, so rafts of crust wander and turn; its rim
//     cools to a dark crust with a torn, lobed edge.
// THE GLOW READS IN DAYLIGHT WITHOUT BLOOM because it is saturated orange and yellow against near-black crust, never
// white: nothing in the game blooms. `_FxStudyBloom` (the bench's blooming camera only) pushes it over.
// ⚠️ IT GROWS DOWN FROM THE CRATER (`_Grow`, metres; v6, for play): TEXCOORD_2.x is how far each point is from the crater
// along its flow (the builder's distance), and nothing past `_Grow` is drawn; the front it grows by is torn by the noise
// and runs hot. 100 (the default) is all of it, so a bench or a cutscene that never sets it sees the whole flow.
// ⚠️ ITS CLOCK IS `_Clock`, set per frame by whoever poses it (the bench hands it the film's age, so a film is the same
// every time); `_UseTime` 1 reads Unity's own time instead, for anything that has no clock to hand it. No loops, no depth
// texture.
Shader "TumbangPreso/SeanVolcanoLava"
{
    Properties
    {
        _CrustTex ("Crust field (R into a plate, G per plate, B soft noise; linear, tiling)", 2D) = "white" {}
        _ErodeTex ("Cloud map (the noises)", 2D) = "white" {}
        _Pool ("A pool (1) or a flow (0)", Range(0,1)) = 0
        _Clock ("Seconds (set per frame)", Float) = 0
        _UseTime ("Read Unity's time instead of _Clock", Range(0,1)) = 0
        _Grow ("Metres grown down from the crater (100: all of it)", Float) = 100
        _Flow ("Flow: repeats a second down the middle, the banks' share of that, the period of the cross-fade (s), across repeats", Vector) = (0.30, 0.25, 2.0, 0.65)
        _Churn ("Pool: how fast the swirl, how big its eddies (per m), how far it spreads out, repeats a metre", Vector) = (0.10, 0.55, 0.035, 2.0)
        _Molten ("Molten share of the skirt; wander of its edge; outer edge; how torn the outer edge is", Vector) = (0.571, 0.30, 0.97, 0.08)
        _Crust ("Crust: dark, lit", Color) = (0.085, 0.060, 0.055, 1)
        _CrustLit ("Crust: its lit plates", Color) = (0.24, 0.17, 0.14, 1)
        _Scorch ("The scorched bank it fades into (the body's bank paint)", Color) = (0.14, 0.11, 0.10, 1)
        _Red ("Molten: its cooled edge", Color) = (0.66, 0.10, 0.03, 1)
        _Orange ("Molten", Color) = (1.00, 0.40, 0.05, 1)
        _Yellow ("Molten: hot", Color) = (1.00, 0.74, 0.20, 1)
        _Glow ("Heat glow on the crust and bank: colour", Color) = (0.95, 0.30, 0.06, 1)
        _Key ("Where the light comes from (world)", Vector) = (0.35, 0.85, -0.40, 0)
        _Emit ("How far over white under a blooming camera (bench only)", Range(0,8)) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Cull Back
            ZWrite On
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _CrustTex, _ErodeTex;
            float4 _Flow, _Churn, _Molten, _Key;
            fixed4 _Crust, _CrustLit, _Scorch, _Red, _Orange, _Yellow, _Glow;
            float _Pool, _Clock, _UseTime, _Emit, _Grow;
            float _FxStudyBloom;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 flow:TEXCOORD1; float2 grow:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 flow:TEXCOORD1; float3 n:TEXCOORD2; float3 world:TEXCOORD3; float2 grow:TEXCOORD4; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                // ⚠️ THE IMPORTER FLIPS EVERY UV SET'S v (glTF's image origin is its top), so the builder's "how far down
                // the flow" arrives as 1 minus itself: read as written, the flows cooled at their source and ran UPHILL
                // (measured: the pools' rim, written as 1, arrived as 0). Both sets are turned back here.
                o.uv=float2(v.uv.x,1.0-v.uv.y); o.flow=float2(v.flow.x,1.0-v.flow.y); o.grow=float2(v.grow.x,1.0-v.grow.y);
                o.n=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float cloud(float2 p) { return pow(tex2D(_ErodeTex,p).r,0.4545); }
            // Molten paint in crisp bands: 0 its cooled edge, 1 its hottest. Never white.
            float3 molten(float v)
            {
                float3 c=lerp(_Red.rgb,_Orange.rgb,smoothstep(0.26,0.34,v));
                return lerp(c,_Yellow.rgb,smoothstep(0.64,0.72,v));
            }
            // The crust field drifted by `drift` (repeats moved over one whole period), on a two-phase clock: each copy
            // slides for one period and is faded out as it would start to smear, while the other, half a period behind,
            // fades in. Returns (into a plate, which plate).
            // ⚠️ `moving` 0 IS ONE STILL COPY (v6). Two copies of the field cross-faded draw, half way through the fade, a
            // plate's crack from one copy faintly inside a plate of the other: the "ring and dot" the v5 review found
            // drifting through the toe. Crust that has stopped (the toe, the last of a flow that stops) is one copy, still.
            float2 crust(float2 uv, float2 drift, float clock, float moving)
            {
                float t=clock/_Flow.z;
                float p0=frac(t), p1=frac(t+0.5);
                float w=lerp(1.0,1.0-abs(1.0-2.0*p0),moving);
                float2 a=tex2D(_CrustTex,uv-drift*p0*moving).rg;
                float2 b=tex2D(_CrustTex,uv-drift*p1+float2(0.37,0.21)).rg;
                return lerp(b,a,w);
            }
            float3 GrowFront(float3 colour,float plateX,float streak,float front,float ahead)
            {
                float open=0.10+0.30*front;
                float seam=(1.0-smoothstep(open-0.05,open+0.02,plateX))*front;
                float lip=_Grow<99.0?1.0-saturate(ahead/0.05):0.0;
                colour=lerp(colour,molten(0.52+0.28*front+0.30*(streak-0.45)),seam);
                return lerp(colour,molten(0.48),lip*0.85);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float clock=_UseTime>0.5?_Time.y:_Clock;
                float3 n=normalize(i.n);
                float lambert=lerp(0.62,1.10,saturate(dot(n,normalize(_Key.xyz))*0.5+0.5));
                float2 wp=i.world.xz+i.world.y*float2(0.31,0.17);
                float wob=(cloud(wp*0.45)-0.5)+(cloud(wp*1.6+0.3)-0.5)*0.45;
                // Growing: nothing past the torn front; the last 0.35 m behind it runs hot (`front`, 0 once grown).
                float ahead=_Grow-i.grow.x+(cloud(wp*2.1+0.2)-0.5)*0.22;
                clip(ahead);
                float front=_Grow<99.0?1.0-saturate(ahead/0.35):0.0;
                float3 colour;
                if(_Pool<0.5)
                {
                    // ⚠️ A TOE (UV1.x over 1, the builder's `toe`): the lobe a flow that stops on the slope ends in. Its
                    // UV1.x is 1 + how far out, read here in place of the strip's across, so the flaking edge runs right
                    // round its blunt front.
                    bool toe=i.flow.x>1.0;
                    float e=toe?saturate(i.flow.x-1.0):abs(i.uv.x-0.5)*2.0;
                    float cool=saturate(i.flow.y);
                    // The molten river: its half-width wanders with the noise and narrows as it cools. In a toe it keeps a
                    // narrow glowing heart under the crust: never a point, never a red spot.
                    float edge=_Molten.x*lerp(1.0,0.30,smoothstep(0.40,1.0,cool))*(1.0+wob*_Molten.y*2.0);
                    edge*=1.0-0.75*smoothstep(0.86,1.0,cool);
                    // The outer edge torn by a finer noise; only the very rim of a toe frays in.
                    float outer=_Molten.z+(cloud(wp*3.3+0.7)-0.5)*_Molten.w*2.0+wob*0.10-smoothstep(0.92,1.0,cool)*0.12;
                    clip(outer-e);
                    float s=saturate(e/max(edge,0.02));
                    // The middle runs faster than the banks; everything slows as it cools.
                    float speed=_Flow.x*lerp(1.0,_Flow.y,s*s)*lerp(1.0,0.35,cool);
                    // It stops as it cools: the last of a flow that stops, and its toe, are still crust (one copy, see `crust`).
                    float moving=toe?0.0:1.0-smoothstep(0.62,0.84,cool);
                    float2 plate=crust(float2(i.uv.x*_Flow.w,i.uv.y),float2(0.0,speed*_Flow.z),clock,moving);
                    float hot=(1.0-s)*(1.0-cool*0.85);
                    // ⚠️ A TOE HAS NO OPEN RIVER: a molten edge drawn in a disc's own radius is a ring, and r13 showed it as a
                    // bullseye in the middle of the lobe. Its heat is all in the cracks between its plates (below).
                    float inRiver=toe?0.0:1.0-smoothstep(0.92,1.04,s);
                    // ⚠️ CRUST EVEN AT THE SOURCE, BRIGHT ONLY IN THE SEAMS (v4's review: near its source the river was "mostly
                    // flat yellow-orange"). Dark rafts cover most of the river all the way down; the hot middle opens the
                    // seams wider and drops a few whole plates (by their own random value) so some open melt shows, but
                    // the bright reads as seams and gaps between dark plates, the value contrast lava needs from far off.
                    float open=lerp(0.08,0.46,pow(saturate(hot),0.9));
                    float gone=step(plate.y,hot*0.20)*inRiver;
                    float melt=max((1.0-smoothstep(open-0.06,open+0.015,plate.x))*inRiver,gone);
                    // ⚠️ THE STRIP'S HEART CARRIES INTO THE TOE (v5: it "stops at the toe's uphill edge"): a narrow seam-lit
                    // stripe down the toe's middle, behind its centre, fading out by half its radius. A stripe, never a ring.
                    float tx=abs(i.uv.x-0.5)*2.0, tf=saturate(i.flow.x-1.0), fr=saturate(i.grow.y);
                    float heartT=toe?(1.0-smoothstep(0.07,0.20,tx))*(1.0-smoothstep(0.25,0.62,tf))*(1.0-fr):0.0;
                    melt=max(melt,(1.0-smoothstep(0.22,0.30,plate.x))*heartT);
                    // Streaks of hotter and cooler melt drifting with the flow.
                    float streak=cloud(float2(i.uv.x*1.3+0.2,i.uv.y*0.7-clock*speed*0.8));
                    // Hottest down the middle of a seam, cooling to orange and red against the plate either side of it.
                    float seam=1.0-saturate(plate.x/max(open,0.05));
                    float heat=saturate(0.12+0.50*hot+0.34*seam*(1.0-gone*0.5)+(streak-0.45)*0.55);
                    float3 lava=molten(heat)*(1.0+_FxStudyBloom*_Emit*saturate(heat*1.4-0.4));
                    // The crust: plates of differing age, lighter where they are thick (their middles), lit by the key.
                    // Fresh plates on the hot river are the darkest: they have only just skinned over.
                    float3 crustC=lerp(_Crust.rgb,_CrustLit.rgb,saturate(plate.y*0.7+plate.x*0.5-0.2)*lerp(1.0,0.40,saturate(hot*1.3)))*lambert;
                    // A thin red-hot lip on each plate where it meets the melt, so a seam steps yellow, orange, red, black.
                    float lip=(1.0-smoothstep(open,open+0.09,plate.x))*inRiver*saturate(hot*1.6+0.15);
                    crustC=lerp(crustC,_Red.rgb*0.75,lip*0.55);
                    // Out over the bank: 0 at the river's edge, 1 at the torn outer edge. A toe stays solid crust out to
                    // about two fifths of its radius, then flakes away over the rest.
                    float bank=saturate((e-edge)/max(outer-edge,0.04));
                    float fb=toe?saturate((e-0.42)/0.55):bank;
                    // Glowing cracks in the crust, dying out across the bank and down the flow; a toe keeps a few.
                    // ⚠️ ONLY ON THE RIVER (v6): v5 let a faint crack run round every skirt plate out over the bank, and near
                    // the source those read as "thin orange outlines" tracing the plates.
                    float crack=(1.0-smoothstep(0.03,0.12+0.10*(1.0-bank),plate.x))*inRiver*(1.0-cool*0.70);
                    float3 crackC=molten(0.30+0.30*(1.0-bank))*0.85;
                    if(toe)
                    {
                        // ⚠️ THE TOE READS FROM ACROSS THE COURT (v5: "a small dark knob" from the player's eye). Its cracks
                        // widen and brighten toward its downhill FRONT (TEXCOORD_2.y), where a real toe splits open over
                        // its hot inside, and they reach out nearly to its rim there; behind, they die out as before.
                        crack=(1.0-smoothstep(0.03,0.18+0.20*fr,plate.x))*lerp(1.0-smoothstep(0.05,0.70,e),1.0-smoothstep(0.50,0.97,e),fr);
                        crackC=molten(0.32+0.40*fr);
                        crustC+=_Glow.rgb*0.14*fr*(1.0-smoothstep(0.6,1.0,e));
                        // Its FRONT splits open: the plates there part over molten rock, a broken glowing band along the
                        // lobes, the brightest thing on the toe and what reads of it from across the court (r17).
                        melt=max(melt,(1.0-smoothstep(0.26,0.36,plate.x))*smoothstep(0.45,0.80,e)*(1.0-smoothstep(0.88,0.98,e))*smoothstep(0.35,0.8,fr));
                    }
                    crustC=lerp(crustC,crackC,crack*0.90);
                    // ⚠️ THE HEAT GLOW HUGS THE LAVA (v4's review: from the player's eye the edge read as "a soft warm
                    // halo"). It falls off within the first sixth of the bank, not across it.
                    float glow=toe?0.0:exp(-bank*10.0)*(1.0-cool*0.75)*(1.0-inRiver);
                    crustC+=_Glow.rgb*glow*0.30;
                    // Out over the bank the crust takes the body's own low-slope tone (`_Scorch`), so the flakes it breaks into sit on rock of nearly their own colour.
                    crustC=lerp(crustC,_Scorch.rgb*lambert,smoothstep(0.30,0.95,fb)*0.85);
                    // ⚠️ NO CLEAN OUTER LINE ("the transition from lava edge to volcano body is too harsh"). Over the bank
                    // the crust breaks into FLAKES: whole crust plates drop out by their own random value, and a fine
                    // noise frays what is left, so the body's own scorched paint shows between them and takes over.
                    clip(plate.y+0.14+(cloud(wp*7.0+0.4)-0.5)*0.30-smoothstep(0.25,1.0,fb)*1.20);
                    colour=lerp(crustC,lava,melt);
                    // ⚠️ THE GROWING FRONT IS OPEN SEAMS, NOT A SHEET (live film 2: a flat pale yellow band 0.35 m deep ran ahead of
                    // the crust, "a yellow sticker"). The plates stay dark rafts; near the front their seams open wide and run hot,
                    // and only a thin orange lip rides the torn edge itself.
                    colour=GrowFront(colour,plate.x,streak,front,ahead);
                }
                else
                {
                    float r=saturate(i.flow.y);
                    // The rim cools: a dark crust whose inner edge wanders, and a torn outer edge.
                    float rim=smoothstep(0.66+wob*0.22,0.97,r);
                    clip(1.0+(cloud(wp*3.3+0.7)-0.5)*0.10+wob*0.04-r);
                    // The churn: a slow swirl read off the cloud map, and a little spreading out from where it is fed.
                    float2 eddy=float2(cloud(wp*_Churn.y)-0.5,cloud(wp*_Churn.y+0.5)-0.5);
                    float2 swirl=float2(-eddy.y,eddy.x)*2.0;
                    float2 away=(i.uv-0.5);
                    float2 drift=(swirl*_Churn.x+away*_Churn.z)*_Flow.z;
                    float2 plate=crust(i.world.xz*_Churn.w*0.5,drift,clock*0.8,1.0);
                    float hot=1.0-rim;
                    // Rafts of dark crust on a bright surface: thin gaps between most plates, and in the hot middle some
                    // plates gone altogether (by their own random value), so the open lava is uneven, never a lattice.
                    float open=lerp(0.03,0.24,hot);
                    float gone=step(plate.y,hot*0.38);
                    float melt=max(1.0-smoothstep(open-0.05,open+0.02,plate.x),gone);
                    // The cooled rim: solid dark crust for the last band before the edge, its inner side wandering.
                    melt*=1.0-smoothstep(0.86+wob*0.08,0.95+wob*0.04,r);
                    float streak=cloud(i.world.xz*1.7+drift*0.25*frac(clock/_Flow.z));
                    float heat=saturate(0.10+0.42*hot+0.40*(streak-0.40)+0.12*(1.0-plate.x));
                    float3 lava=molten(heat)*(1.0+_FxStudyBloom*_Emit*saturate(heat*1.4-0.4));
                    float3 crustC=lerp(_Crust.rgb,_CrustLit.rgb,saturate(plate.y*0.45+plate.x*0.35-0.15))*lambert;
                    // Thin glowing cracks across the cooled plates near the rim.
                    float crack=(1.0-smoothstep(0.012,0.045,plate.x))*rim*(1.0-smoothstep(0.88,0.97,r));
                    crustC=lerp(crustC,molten(0.32),crack*0.9);
                    colour=lerp(crustC,lava,melt);
                    colour=GrowFront(colour,plate.x,streak,front,ahead);
                }
                return fixed4(colour,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
