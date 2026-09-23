using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class ColourGrade
    {
        // ⚠️⚠️ § THE BRIGHT LOOK'S GRADE. See `ColourGrade.shader`'s note of the same name for
        // what the three terms are for. This half decides WHICH cameras get them: only a camera
        // the map's world look owns, at the look's own weight, so WorldLighting 0 grades exactly
        // as before and the character portrait and map preview never pick up a map's lift.
        //
        // ⚠️ THE BLOOM CHAIN IS SKIPPED ON THE LOW GRAPHICS TIER, read off the static
        // `GraphicsProfiles.Current` for `EffectiveChromatic`'s reason: this runs inside
        // `OnRenderImage` on every camera every frame. Low keeps the lift and vibrance, which cost
        // nothing past the blit this pass already pays.
        private const int BloomLevels=5;
        private const int PassPrefilter=1,PassDown=2,PassUp=3;
        private readonly RenderTexture[] _bloomChain=new RenderTexture[BloomLevels];
        private static readonly int LiftId=Shader.PropertyToID("_Lift");
        private static readonly int VibranceId=Shader.PropertyToID("_Vibrance");
        private static readonly int BloomTexId=Shader.PropertyToID("_BloomTex");
        private static readonly int BloomIntensityId=Shader.PropertyToID("_BloomIntensity");
        private static readonly int BloomThresholdId=Shader.PropertyToID("_BloomThreshold");

        private float BrightLookWeight=>WorldLookPresentation.HandlesCamera(_camera)?WorldLookPresentation.Current.Weight:0;

        /// <summary>Whether the last graded frame carried the bright look's bloom.</summary>
        public bool BloomLive {get;private set;}

        private RenderTexture PrepareBrightLook(RenderTexture source)
        {
            float weight=BrightLookWeight;var profile=WorldLookProfile.Current;
            Color lift=weight>0?WorldLookPresentation.Current.Look.Lift*weight:Color.clear;
            _material.SetColor(LiftId,new Color(lift.r,lift.g,lift.b,weight>0?1:0));
            _material.SetFloat(VibranceId,profile.Vibrance*weight);
            _material.SetFloat(BloomIntensityId,0);_material.SetTexture(BloomTexId,Texture2D.blackTexture);
            BloomLive=false;
            if(weight<=0 || profile.Bloom<=0 || Settings.GraphicsProfiles.Current<=0)return null;

            // The chain must hold values above 1 or the threshold has nothing to find.
            var format=source.format;
            bool hdr=format==RenderTextureFormat.ARGBHalf || format==RenderTextureFormat.ARGBFloat ||
                     format==RenderTextureFormat.RGB111110Float || format==RenderTextureFormat.DefaultHDR;
            if(!hdr && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR))format=RenderTextureFormat.DefaultHDR;
            _material.SetVector(BloomThresholdId,new Vector4(profile.BloomThreshold,.6f,0,0));
            int width=Mathf.Max(1,source.width/2),height=Mathf.Max(1,source.height/2);
            var current=RenderTexture.GetTemporary(width,height,0,format,RenderTextureReadWrite.Linear);
            Graphics.Blit(source,current,_material,PassPrefilter);
            _bloomChain[0]=current;int levels=1;
            for(;levels<BloomLevels;levels++)
            {
                width/=2;height/=2;if(width<2 || height<2)break;
                var next=RenderTexture.GetTemporary(width,height,0,format,RenderTextureReadWrite.Linear);
                Graphics.Blit(current,next,_material,PassDown);_bloomChain[levels]=current=next;
            }
            // Walk back up, adding each smaller level into the one above it.
            for(int i=levels-2;i>=0;i--)
            {
                Graphics.Blit(_bloomChain[i+1],_bloomChain[i],_material,PassUp);
                RenderTexture.ReleaseTemporary(_bloomChain[i+1]);_bloomChain[i+1]=null;
            }
            var result=_bloomChain[0];_bloomChain[0]=null;
            _material.SetTexture(BloomTexId,result);_material.SetFloat(BloomIntensityId,profile.Bloom*weight);
            BloomLive=true;return result;
        }
    }
}
