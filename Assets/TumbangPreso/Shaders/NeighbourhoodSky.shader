Shader "TumbangPreso/NeighbourhoodSky"
{
    Properties
    {
        _Zenith("Upper sky", Color)=(.45,.56,.65,1)
        _Horizon("Horizon haze", Color)=(.78,.77,.70,1)
        _Ground("Below horizon", Color)=(.38,.36,.30,1)
        _SunColor("Sunlight", Color)=(1,.84,.64,1)
        _SunDirection("Sun direction", Vector)=(0,1,0,0)
        _Tint("Weather tint", Color)=(.5,.5,.5,1)
        _Exposure("Weather exposure", Range(0,2))=1
        _CloudMap("Cloud form panorama", 2D)="black"{}
        _CloudLight("Cloud sunlit face", Color)=(.92,.89,.81,1)
        _CloudShade("Cloud body", Color)=(.57,.63,.68,1)
        _CloudYaw("Cloud panorama turn", Float)=0
        _CloudSpeed("Cloud turns per second", Float)=.0001
        _CloudLumaScale("Cloud radiance normalization", Float)=1
        _CloudSunCutoff("Source sun removal", Float)=100
        _CloudLumaLow("Cloud shaded radiance", Float)=0
        _CloudLumaHigh("Cloud lit radiance", Float)=1
        _CloudOpacity("Cloud body opacity", Range(0,1))=.94
        _CloudPaint("Painted clouds (bright look)", Range(0,1))=0
        _SunDisc("Sun disc radius (radians, 0 = none)", Range(0,.2))=0
        _SunHalo("Sun halo strength", Range(0,2))=0
        _SunClear("Clear sky round the sun (radians, 0 = none)", Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _CloudMap;
            float4 _Zenith,_Horizon,_Ground,_SunColor,_SunDirection,_Tint,_CloudLight,_CloudShade;float _SunDisc,_SunHalo,_SunClear;
            float _TumpSkyTime;
            float _CloudSpeed;
            float _Exposure,_CloudYaw,_CloudLumaScale,_CloudSunCutoff,_CloudOpacity,_CloudLumaLow,_CloudLumaHigh,_CloudPaint;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION;float3 direction:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float3 direction=normalize(i.direction);
                float up=saturate(direction.y);
                half3 sky=lerp(_Horizon.rgb,_Zenith.rgb,pow(up,.55));
                sky=lerp(sky,_Ground.rgb,saturate(-direction.y*3));

                // Retain real cloud silhouettes from the credited CC0 panorama,
                // then light their forms with this map's authored colors. The
                // photograph's terrain, exposure and bright sun are not pasted in.
                float2 uv=float2(atan2(direction.x,direction.z)*.159154943+.5+_CloudYaw+frac(_TumpSkyTime*_CloudSpeed),
                                 asin(clamp(direction.y,-1,1))*.318309886+.5);
                float3 source=tex2D(_CloudMap,uv).rgb;
                // ⚠️⚠️ § PAINTED CLOUDS, THE BRIGHT LOOK ONLY (owner 2026-09-25: "do not make the
                // cloud realistic"). The panorama is a photograph, so at full resolution its
                // silhouettes carry every wisp and billow of a real sky. PEAK's clouds are
                // brushed shapes in two flat tones. So the look reads the panorama three mips
                // down (a 2048-wide source becomes 256 texels round the whole sky, about 1.4
                // degrees each), which keeps where the clouds are and loses their photographic
                // detail, then below re-edges the soft result into a shape and splits its light
                // into two tones. tex2Dlod also sidesteps the derivative seam atan2 puts at the
                // back of the sky. The authored material leaves this 0, so Classic's sky is
                // untouched; `WorldLookPresentation` sets it on its own sky instance.
                if(_CloudPaint>0)source=lerp(source,tex2Dlod(_CloudMap,float4(uv,0,3)).rgb,_CloudPaint);
                float maximum=max(max(source.r,source.g),source.b);
                float luma=dot(source,float3(.2126,.7152,.0722));
                float blueness=(source.b-source.r)/max(maximum,.00001);
                float cloud=(1-smoothstep(.12,.46,blueness))*smoothstep(.008,.09,direction.y);
                cloud*=saturate(maximum*_CloudLumaScale*25);
                cloud*=1-smoothstep(_CloudSunCutoff,_CloudSunCutoff*2,luma);
                // Normalize within the cloud body, not against mostly blue sky.
                // This retains shaded billows without letting the source sun
                // flatten an entire panorama into the light endpoint.
                // A painted edge: the blurred coverage steps into a shape with a narrow soft rim.
                cloud=lerp(cloud,smoothstep(.24,.56,cloud),_CloudPaint);
                // ⚠️ A CLEAR PATCH ROUND THE SUN, OPT-IN (owner, 2026-09-27, on the Lagoon Cove sunset:
                // "get rid of that cloud blocking the sun"). Clouds thin out to nothing within
                // _SunClear of the sun, with a soft edge, so the disc and its halo always show. The
                // shipped maps' materials leave it at 0 and keep every cloud where it was.
                if(_SunClear>0)
                {
                    float sunAngle=acos(clamp(dot(direction,normalize(_SunDirection.xyz)),-1,1));
                    cloud*=smoothstep(_SunClear*.55,_SunClear,sunAngle);
                }
                float light=smoothstep(_CloudLumaLow,_CloudLumaHigh,luma);
                // Two tones, lit top and body, with a brushed rather than a hard transition.
                // ⚠️ THE STEP SITS LOW (0.22 to 0.50) SO MOST OF A CLOUD IS ITS LIT CREAM TONE and
                // the teal-grey body is its underside. At 0.40 to 0.62 the first render left the
                // clouds almost all body: one cold cut-out tone.
                light=lerp(light,smoothstep(.22,.50,light),_CloudPaint);
                half3 cloudColor=lerp(_CloudShade.rgb,_CloudLight.rgb,light);
                sky=lerp(sky,cloudColor,cloud*_CloudOpacity);

                float alignment=saturate(dot(direction,normalize(_SunDirection.xyz)));
                float sun=pow(alignment,48)*.065+pow(alignment,1500)*.12;
                sky+=_SunColor.rgb*sun*(1-cloud*.85);
                // ⚠️ A VISIBLE SUN, OPT-IN (owner, 2026-09-27, on the Lagoon Cove sunset: "theres no
                // actual sun visible, i want to add that"). The glow above is a faint pinprick by
                // design for the shipped maps' high afternoon sun, and their materials leave these
                // two at 0, so nothing changes for them. A stylized DISC (a crisp edge a few
                // hundredths of a radian soft, not a photographic bloom) and a wide warm HALO round
                // it; clouds pass in front of both.
                if(_SunDisc>0)
                {
                    // Round 2 (owner: "need a fuzzier more stylized looking sun"): the first disc
                    // had a hard edge and a near-white face, which read as a photographed sun. Now a
                    // warm, FEATHERED disc (the edge fades over half its radius) inside two glows,
                    // a tight bright one and a wide soft one, like a painted sunset.
                    float angle=acos(clamp(alignment,-1,1));
                    float disc=1-smoothstep(_SunDisc*.45,_SunDisc*1.15,angle);
                    float inner=exp(-angle/max(_SunDisc*1.6,1e-3));
                    float outer=exp(-angle/max(_SunDisc*7,1e-3));
                    half3 warm=lerp(_SunColor.rgb,half3(1,.93,.78),.4);
                    float veil=1-cloud*.75;
                    sky=lerp(sky,warm*1.35,disc*.9*veil);
                    sky+=warm*(inner*.45+outer*.28)*_SunHalo*veil;
                }
                return half4(sky*_Tint.rgb*unity_ColorSpaceDouble.rgb*_Exposure,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
