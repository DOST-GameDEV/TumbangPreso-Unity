Shader "TumbangPreso/BlockyCloud"
{
    // ⚠️⚠️ § THE BLOCKY CLOUDS (LIGHT-3, owner 2026-09-25: "maybe try a more blocky style of
    // clouds where they are real 3d assets"). Voxel cumulus on a ring past the skyline
    // (`BlockyClouds` builds them), drawn in the look's colour rule and nothing else: a warm
    // cream crown toward the sun, a lavender belly and crevices, and a lean into the sky's own
    // colour for the air. No texture, no specular: they are the cast's voxels at sky scale.
    //
    // ⚠️ THE DRIFT IS IN THE VERTEX STAGE, OFF `_TumpSkyTime`. That is the one sky clock
    // (`NeighbourhoodSkyMotion`) the panorama already turns on, and replays and the pause menu
    // sample it through `NeighbourhoodSkyMotion.At`, so the clouds go where the sky goes with no
    // per-frame C# and no replay bytes.
    //
    // ⚠️⚠️ AND THAT IS WHY `RenderType` IS NOT "Opaque". The camera's depth-normals texture is
    // drawn by Unity's replacement shader, which reads the mesh as stored and knows nothing about
    // this vertex rotation, so an Opaque tag would put each cloud's depth where it was at time 0
    // and `WorldOutline` would draw edges round empty sky as the ring turned. An unknown type is
    // left out of that texture, which is correct: a cloud needs no edge pass.
    //
    // ⚠️ NO UNITY FOG. The look's haze ends 180 to 300 m out and the clouds sit 120 to 200 m
    // away, so fog would wash every cloud to the haze colour. `_Air` is the same idea, held to
    // the share that keeps the shape.
    Properties
    {
        _LitColor("Sunlit top", Color)=(1,.96,.9,1)
        _ShadeColor("Body and underside", Color)=(.72,.7,.86,1)
        _AirColor("Horizon air", Color)=(.8,.88,.94,1)
        _ZenithColor("Upper sky", Color)=(.44,.7,.88,1)
        _SunDir("Direction to the sun", Vector)=(0,1,0,0)
        _Air("Air share", Range(0,1))=.32
        _Drift("Drift, degrees per second", Float)=.06
    }
    SubShader
    {
        Tags { "Queue"="Geometry+50" "RenderType"="BlockyCloud" "IgnoreProjector"="True" "DisableBatching"="True" }
        Pass
        {
            Cull Back ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            half4 _LitColor,_ShadeColor,_AirColor,_ZenithColor;
            float4 _SunDir;
            half _Air;
            float _Drift,_TumpSkyTime;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 shape:TEXCOORD1; float3 world:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                float a=radians(_Drift)*_TumpSkyTime,s=sin(a),c=cos(a);
                float3 p=v.vertex.xyz;p.xz=float2(p.x*c-p.z*s,p.x*s+p.z*c);
                float3 n=v.normal;n.xz=float2(n.x*c-n.z*s,n.x*s+n.z*c);
                o.pos=UnityObjectToClipPos(float4(p,1));o.normal=UnityObjectToWorldNormal(n);
                o.world=mul(unity_ObjectToWorld,float4(p,1)).xyz;o.shape=v.uv;
                return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                float3 toSun=normalize(_SunDir.xyz);
                float3 toEye=normalize(_WorldSpaceCameraPos-i.world);
                // ⚠️ THE VOLUME COMES FROM THREE SOFT TERMS, NOT FROM FACE TONES (owner, "less
                // volume-y", "too sharp"): the height through the whole cloud (x, 0 belly to 1
                // crown), a half-Lambert on the ROUNDED normals the builder wrote, and the
                // crevice occlusion (y, 1 open to 0 deep). Face-to-face contrast is small, so a
                // cloud reads as one lit body made of blocks.
                half crown=saturate(i.shape.x);
                half facing=saturate(dot(n,toSun)*.5h+.5h);
                half tone=saturate(crown*.55h+facing*.5h+n.y*.12h-.08h);
                tone=smoothstep(.08h,.92h,tone);
                half3 colour=lerp(_ShadeColor.rgb,_LitColor.rgb,tone);
                // Crevices sink toward the body colour, a deeper lavender.
                colour=lerp(_ShadeColor.rgb*.88h,colour,lerp(.45h,1.0h,saturate(i.shape.y)));
                // ⚠️ SOFT SILHOUETTES. A face seen edge-on melts into the air, so the outline of
                // a blocky cloud is soft against the sky rather than a cut edge.
                // The air is the sky's own colour at this elevation, the same horizon-to-zenith
                // blend `NeighbourhoodSky` draws, so a high cloud melts into blue and a low one
                // into the horizon, with no pale halo either way.
                half up=saturate(-toEye.y);
                half3 air=lerp(_AirColor.rgb,_ZenithColor.rgb,pow(up,.55h));
                half rim=pow(1.0h-saturate(abs(dot(n,toEye))),2.0h);
                colour=lerp(colour,air,saturate(_Air+rim*.4h));
                return half4(colour,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
