using System;
using System.Linq;
using TumbangPreso.Audio;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class ZackAudioAuthor
    {
        public static void Import()
        {
            var cues = AudioCues.Live.Where(c => (c.StartsWith("sfx_") && c.Contains("zack")) || c == "sfx_status_zapped").ToArray();
            foreach (var cue in cues)
            {
                string path = "Assets/TumbangPreso/Resources/Sfx/" + cue + ".wav";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new InvalidOperationException("Missing Zack cue: " + cue);
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            Debug.Log("Zack audio imported: " + cues.Length + " original electrical cues.");
        }
    }
}
