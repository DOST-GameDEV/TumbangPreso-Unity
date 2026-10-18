using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // Reads an explicitly requested completed player trace. It changes no assets.
    public static class PlayerCpuTraceExport
    {
        private static string Arg(string name)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);
            if(i<0||i+1>=args.Length)throw new ArgumentException(name+" is required.");
            return Path.GetFullPath(args[i+1]);
        }
        private static string Quote(string text)=>"\""+(text??"").Replace("\"","\"\"")+"\"";
        public static void ExportJitDetails()
        {
            string input=Arg("-tp-profiler-trace"),output=Arg("-tp-profiler-output");
            if(!File.Exists(input))throw new FileNotFoundException("Completed player trace is missing.",input);
            Directory.CreateDirectory(output);
            ProfilerDriver.ClearAllFrames();ProfilerDriver.LoadProfile(input,false);
            int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,worst=-1;
            float longest=-1;
            for(int frame=first;frame<=last;frame++)
            {
                using var data=ProfilerDriver.GetRawFrameDataView(frame,0);
                if(data.valid&&data.sampleCount>0&&data.frameTimeMs>longest)
                {worst=frame;longest=data.frameTimeMs;}
            }
            if(worst<0)throw new InvalidDataException("Trace has no CPU samples.");
            using(var csv=new StreamWriter(Path.Combine(output,"cold-methods.csv")))
            {
                csv.WriteLine("frame,sample,inclusive_ms,name,metadata_count,metadata");
                using var data=ProfilerDriver.GetRawFrameDataView(worst,0);
                for(int sample=0;sample<data.sampleCount;sample++)
                {
                    string name=data.GetSampleName(sample);
                    if(name!="Mono.JIT"&&!name.StartsWith("Loading.",StringComparison.Ordinal))continue;
                    int count=data.GetSampleMetadataCount(sample);
                    var values=new string[count];
                    for(int index=0;index<count;index++)values[index]=data.GetSampleMetadataAsString(sample,index);
                    csv.WriteLine(FormattableString.Invariant($"{worst},{sample},{data.GetSampleTimeMs(sample):R},{Quote(name)},{count},{Quote(string.Join(" | ",values))}"));
                }
            }
            File.WriteAllText(Path.Combine(output,"complete.txt"),FormattableString.Invariant($"frame={worst};cpu_ms={longest:R}"));
            Debug.Log($"[PlayerCpuTrace] cold method details: frame{worst}, {longest:F3} ms. Metadata absence is explicit; no inferred method names.");
        }
        public static void Export()
        {
            string input=Arg("-tp-profiler-input"),output=Arg("-tp-profiler-output");
            Directory.CreateDirectory(output);
            var files=Directory.GetFiles(input,"*.raw").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            if(files.Length==0)throw new InvalidOperationException("No completed binary player traces.");
            foreach(string file in files)
            {
                ProfilerDriver.ClearAllFrames();ProfilerDriver.LoadProfile(file,false);
                int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex;
                if(first<0||last<first)throw new InvalidDataException("Trace has no readable frames: "+file);
                var frames=new List<(int index,float ms)>();
                for(int frame=first;frame<=last;frame++)
                {
                    using var data=ProfilerDriver.GetRawFrameDataView(frame,0);
                    if(data.valid&&data.sampleCount>0)frames.Add((frame,data.frameTimeMs));
                }
                if(frames.Count==0)throw new InvalidDataException("Trace has no CPU samples: "+file);
                string name=Path.GetFileNameWithoutExtension(file);
                using(var csv=new StreamWriter(Path.Combine(output,name+"-frames.csv")))
                {
                    csv.WriteLine("frame,cpu_ms");
                    foreach(var frame in frames)csv.WriteLine(FormattableString.Invariant($"{frame.index},{frame.ms:R}"));
                }
                using(var csv=new StreamWriter(Path.Combine(output,name+"-samples.csv")))
                {
                    csv.WriteLine("frame,thread,thread_name,sample,parent,depth,inclusive_ms,name");
                    foreach(var frame in frames.OrderByDescending(f=>f.ms).Take(20))
                    for(int thread=0;thread<128;thread++)
                    {
                        using var data=ProfilerDriver.GetRawFrameDataView(frame.index,thread);
                        if(!data.valid)break;
                        var parents=new Stack<(int sample,int end)>();
                        for(int sample=0;sample<data.sampleCount;sample++)
                        {
                            while(parents.Count>0&&sample>=parents.Peek().end)parents.Pop();
                            int parent=parents.Count==0?-1:parents.Peek().sample,depth=parents.Count;
                            float ms=data.GetSampleTimeMs(sample);
                            if(ms>=.05f||depth<2)
                                csv.WriteLine(FormattableString.Invariant($"{frame.index},{thread},{Quote(data.threadName)},{sample},{parent},{depth},{ms:R},{Quote(data.GetSampleName(sample))}"));
                            int children=data.GetSampleChildrenCountRecursive(sample);
                            if(children>0)parents.Push((sample,sample+children+1));
                        }
                    }
                }
                Debug.Log($"[PlayerCpuTrace] {name}: {frames.Count} CPU frames, worst {frames.Max(f=>f.ms):F3} ms; inclusive nested samples are not additive.");
            }
            File.WriteAllText(Path.Combine(output,"complete.txt"),files.Length.ToString(CultureInfo.InvariantCulture));
        }
    }
}
