using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.CameraSystem
{
    // Render values only. Weather is sampled, never replayed through SkyEvent.Play.
    public struct RecordedEnvironment
    {
        public AmbientMode Ambient;
        public FogMode FogMode;
        public bool Fog,HasSun,HasExposure,HasTint,FillOn;
        public Color Sky,Equator,Ground,Flat,FogColour,SunColour,SkyTint,FillColour;
        public float SunPower,FogStart,FogEnd,FogDensity,Exposure,Brightness,Saturation,FillRange,FillPower;
        public Vector3 FillPosition;
        public static RecordedEnvironment Capture(ColourGrade grade=null)
        {
            var sun=SkyEvent.RecordedSun;var fill=SkyEvent.RecordedFill;var sky=RenderSettings.skybox;
            if(grade==null&&Camera.main!=null)grade=Camera.main.GetComponent<ColourGrade>();
            return new RecordedEnvironment{Ambient=RenderSettings.ambientMode,FogMode=RenderSettings.fogMode,Fog=RenderSettings.fog,
                Sky=RenderSettings.ambientSkyColor,Equator=RenderSettings.ambientEquatorColor,Ground=RenderSettings.ambientGroundColor,Flat=RenderSettings.ambientLight,
                FogColour=RenderSettings.fogColor,FogStart=RenderSettings.fogStartDistance,FogEnd=RenderSettings.fogEndDistance,FogDensity=RenderSettings.fogDensity,
                HasSun=sun!=null,SunColour=sun!=null?sun.color:Color.white,SunPower=sun!=null?sun.intensity:0,
                HasExposure=sky!=null&&sky.HasProperty("_Exposure"),Exposure=sky!=null&&sky.HasProperty("_Exposure")?sky.GetFloat("_Exposure"):1,
                HasTint=sky!=null&&sky.HasProperty("_Tint"),SkyTint=sky!=null&&sky.HasProperty("_Tint")?sky.GetColor("_Tint"):Color.white,
                Brightness=grade!=null?grade.RecordedBrightness:1,Saturation=grade!=null?grade.RecordedSaturation:1,
                FillOn=fill!=null&&fill.enabled,FillColour=fill!=null?fill.color:Color.white,FillPower=fill!=null?fill.intensity:0,
                FillRange=fill!=null?fill.range:34,FillPosition=fill!=null?fill.transform.position:Vector3.up*11};
        }
        private void Apply(ColourGrade grade,Material sky,Light sun,Light fill)
        {
            RenderSettings.ambientMode=Ambient;RenderSettings.ambientSkyColor=Sky;RenderSettings.ambientEquatorColor=Equator;
            RenderSettings.ambientGroundColor=Ground;RenderSettings.ambientLight=Flat;RenderSettings.fog=Fog;RenderSettings.fogMode=FogMode;
            RenderSettings.fogColor=FogColour;RenderSettings.fogStartDistance=FogStart;RenderSettings.fogEndDistance=FogEnd;RenderSettings.fogDensity=FogDensity;
            if(sun!=null&&HasSun){sun.color=SunColour;sun.intensity=SunPower;}
            if(sky!=null){if(HasExposure&&sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",Exposure);if(HasTint&&sky.HasProperty("_Tint"))sky.SetColor("_Tint",SkyTint);}
            if(grade!=null)grade.SetEventGrade(Brightness,Saturation);
            if(fill!=null){fill.enabled=FillOn;fill.color=FillColour;fill.intensity=FillPower;fill.range=FillRange;fill.transform.position=FillPosition;}
        }
        public IDisposable Use(ColourGrade grade,Material replaySky,Light replayFill)=>new Scope(this,grade,replaySky,replayFill);
        private sealed class Scope:IDisposable
        {
            private readonly RecordedEnvironment _before;
            private readonly ColourGrade _grade;
            private readonly Material _sky;
            private readonly Light _sun,_fill,_replayFill;
            public Scope(RecordedEnvironment recorded,ColourGrade grade,Material replaySky,Light replayFill)
            {
                _before=Capture(grade);_grade=grade;_sky=RenderSettings.skybox;_sun=SkyEvent.RecordedSun;_fill=SkyEvent.RecordedFill;_replayFill=replayFill;
                if(_fill!=null)_fill.enabled=false;if(replaySky!=null)RenderSettings.skybox=replaySky;
                recorded.Apply(grade,replaySky,_sun,replayFill);
            }
            public void Dispose()
            {RenderSettings.skybox=_sky;_before.Apply(_grade,_sky,_sun,_fill);if(_replayFill!=null)_replayFill.enabled=false;}
        }
        public void Write(BinaryWriter w)
        {
            w.Write((int)Ambient);w.Write((int)FogMode);w.Write(Fog);w.Write(HasSun);w.Write(HasExposure);w.Write(HasTint);w.Write(FillOn);
            foreach(var c in new[]{Sky,Equator,Ground,Flat,FogColour,SunColour,SkyTint,FillColour}){w.Write(c.r);w.Write(c.g);w.Write(c.b);w.Write(c.a);}
            foreach(float f in new[]{SunPower,FogStart,FogEnd,FogDensity,Exposure,Brightness,Saturation,FillRange,FillPower,FillPosition.x,FillPosition.y,FillPosition.z})w.Write(f);
        }
        public static RecordedEnvironment Read(BinaryReader r)
        {
            var value=new RecordedEnvironment{Ambient=(AmbientMode)r.ReadInt32(),FogMode=(FogMode)r.ReadInt32(),Fog=r.ReadBoolean(),HasSun=r.ReadBoolean(),HasExposure=r.ReadBoolean(),HasTint=r.ReadBoolean(),FillOn=r.ReadBoolean()};
            var colours=new Color[8];for(int i=0;i<8;i++){colours[i]=new Color(ReadNumber(r,0,16),ReadNumber(r,0,16),ReadNumber(r,0,16),ReadNumber(r,0,1));}
            value.Sky=colours[0];value.Equator=colours[1];value.Ground=colours[2];value.Flat=colours[3];value.FogColour=colours[4];value.SunColour=colours[5];value.SkyTint=colours[6];value.FillColour=colours[7];
            value.SunPower=ReadNumber(r,0,100);value.FogStart=ReadNumber(r,-10000,10000);value.FogEnd=ReadNumber(r,-10000,10000);value.FogDensity=ReadNumber(r,0,10);
            value.Exposure=ReadNumber(r,0,100);value.Brightness=ReadNumber(r,.15f,1);value.Saturation=ReadNumber(r,0,1);
            value.FillRange=ReadNumber(r,0,1000);value.FillPower=ReadNumber(r,0,100);value.FillPosition=new Vector3(ReadNumber(r,-10000,10000),ReadNumber(r,-10000,10000),ReadNumber(r,-10000,10000));
            if(!Enum.IsDefined(typeof(AmbientMode),value.Ambient)||!Enum.IsDefined(typeof(FogMode),value.FogMode))throw new InvalidDataException("Invalid recorded lighting mode");
            return value;
        }
        private static float ReadNumber(BinaryReader r,float min,float max)
        {float n=r.ReadSingle();if(float.IsNaN(n)||float.IsInfinity(n)||n<min||n>max)throw new InvalidDataException("Invalid recorded lighting value");return n;}
    }
}
