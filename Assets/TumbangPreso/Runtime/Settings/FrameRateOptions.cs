using System;
using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>A display preference, independent of simulation rate and visual quality.</summary>
    public static class FrameRateOptions
    {
        public const int Default=0;
        public static readonly int[] All={0,30,60,90,120,144,165,240};
        public static int Current {get;private set;}=Default;
        private static readonly int OperatorLimit=ReadOperatorLimit(Environment.GetCommandLineArgs());
        public static int Normalize(int value)=>Array.IndexOf(All,value)>=0?value:Default;
        public static string Label(int value)=>Normalize(value)==0?"Unlimited":Normalize(value)+" FPS";
        public static bool ControlledByDisplay=>QualitySettings.vSyncCount>0;
        public static void Apply(int value)
        {
            Current=Normalize(value);
            // Preserve the existing explicit venue/benchmark command-line cap
            // across later settings loads, including values such as50Hz.
            if(OperatorLimit>0){QualitySettings.vSyncCount=0;Application.targetFrameRate=OperatorLimit;return;}
            // Unity desktop waits for vertical blanks when VSync is enabled.
            // Keep the user's cap saved so switching sync off restores it, but
            // don't claim that targetFrameRate controls a synchronized display.
            Application.targetFrameRate=ControlledByDisplay||Current==0?-1:Current;
        }
        public static int ReadOperatorLimit(string[] args)
        {
            if(args==null)return 0;
            for(int i=0;i<args.Length-1;i++)
                if(args[i]=="-tp-framecap"&&int.TryParse(args[i+1],out int cap)&&cap>0)return cap;
            return 0;
        }
    }
}
