using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    // Captures Unity's actual DSP mix, including world attenuation, loops and the introduction source.
    // AudioRenderer takes over output while recording. No offline recreation of gameplay cues.
    internal sealed class ZackFilmAudio : IDisposable
    {
        private readonly MemoryStream _samples = new MemoryStream();
        private readonly BinaryWriter _writer;
        private readonly StringBuilder _cues = new StringBuilder("seconds,cue,x,y,z,pitch,gain\n");
        private readonly float _start;
        private readonly int _channels, _rate;
        private readonly string _folder;
        private float _peak;
        private double _squares;
        private int _count;

        public ZackFilmAudio(string folder)
        {
            _folder = folder;
            _channels = AudioSettings.speakerMode switch
            {
                AudioSpeakerMode.Mono => 1, AudioSpeakerMode.Stereo => 2,
                AudioSpeakerMode.Quad => 4, AudioSpeakerMode.Surround => 5,
                AudioSpeakerMode.Mode5point1 => 6, AudioSpeakerMode.Mode7point1 => 8,
                _ => throw new InvalidOperationException("Unsupported capture speaker mode")
            };
            _rate = AudioSettings.outputSampleRate;
            _start = Time.time;
            _writer = new BinaryWriter(_samples);
            Assert.IsTrue(AudioRenderer.Start(), "Native Unity audio recording must start.");
            AudioDirector.WorldCuePlayed += Cue;
        }

        private void Cue(string cue, Vector3 at, float pitch, float gain)
            => _cues.AppendLine(FormattableString.Invariant($"{Time.time-_start:F3},{cue},{at.x:F3},{at.y:F3},{at.z:F3},{pitch:F3},{gain:F4}"));

        public void Frame()
        {
            int frames = AudioRenderer.GetSampleCountForCaptureFrame();
            using var buffer = new NativeArray<float>(frames * _channels, Allocator.Temp);
            Assert.IsTrue(AudioRenderer.Render(buffer), "The native audio frame must render.");
            for (int i = 0; i < buffer.Length; i++)
            {
                float sample = buffer[i];
                if (!float.IsFinite(sample)) Assert.Fail("The native mix contains a non-finite sample.");
                _peak = Mathf.Max(_peak, Mathf.Abs(sample)); _squares += sample * sample; _count++;
                _writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1, 1) * 32767));
            }
        }

        public void Dispose()
        {
            AudioDirector.WorldCuePlayed -= Cue;
            AudioRenderer.Stop();
            using (var output = new BinaryWriter(File.Create(Path.Combine(_folder, "audio.wav"))))
            {
                int bytes = (int)_samples.Length;
                output.Write(Encoding.ASCII.GetBytes("RIFF")); output.Write(36 + bytes);
                output.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); output.Write(16);
                output.Write((short)1); output.Write((short)_channels); output.Write(_rate);
                output.Write(_rate * _channels * 2); output.Write((short)(_channels * 2)); output.Write((short)16);
                output.Write(Encoding.ASCII.GetBytes("data")); output.Write(bytes); output.Write(_samples.ToArray());
            }
            File.WriteAllText(Path.Combine(_folder, "cues.csv"), _cues.ToString());
            File.WriteAllText(Path.Combine(_folder, "audio.txt"), FormattableString.Invariant(
                $"Native Unity AudioRenderer; rate={_rate}; channels={_channels}; seconds={_count/(double)(_rate*_channels):F4}; peak={_peak:F6}; rms={Math.Sqrt(_squares/Math.Max(1,_count)):F6}"));
            _writer.Dispose(); _samples.Dispose();
            Assert.Greater(_count, 0); Assert.Greater(_peak, .001f, "Captured game audio must be non-silent.");
            Assert.Less(_peak, .98f, "The captured combined game mix must keep headroom.");
        }
    }
}
