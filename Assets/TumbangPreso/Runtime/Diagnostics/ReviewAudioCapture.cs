using System;
using System.IO;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in engine-output capture for native art review. Never accesses a
    // microphone or desktop audio, and never changes the samples being played.
    public sealed class ReviewAudioCapture : MonoBehaviour
    {
        private readonly object _gate = new object();
        private float[] _samples;
        private int _count, _channels, _rate;
        private bool _armed;
        private string _error;
        private double _started;
        public void Begin(float maximumSeconds = 4)
        {
            lock (_gate)
            {
                _rate = AudioSettings.outputSampleRate;
                _samples = new float[Mathf.CeilToInt(Mathf.Clamp(maximumSeconds, 1, 8) * _rate * 8)];
                _count = _channels = 0; _error = null; _started = AudioSettings.dspTime; _armed = true;
            }
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (_gate)
            {
                if (!_armed) return;
                if (channels < 1 || channels > 8 || (_channels != 0 && channels != _channels))
                { _error = "Output channels changed during capture"; _armed = false; return; }
                _channels = channels;
                int remaining = _samples.Length - _count;
                if (data.Length > remaining) { _error = "Bounded output buffer filled"; _armed = false; return; }
                Array.Copy(data, 0, _samples, _count, data.Length); _count += data.Length;
            }
        }
        public void Save(string folder)
        {
            float[] samples; int channels;
            lock (_gate)
            {
                _armed = false;
                if (_error != null) throw new InvalidOperationException(_error);
                if (_count == 0 || _channels == 0) throw new InvalidOperationException("No actual game output samples arrived.");
                samples = new float[_count]; Array.Copy(_samples, samples, _count); channels = _channels;
            }
            Directory.CreateDirectory(folder);
            using (var writer = new BinaryWriter(File.Create(Path.Combine(folder, "game-audio.wav"))))
            {
                int bytes = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)channels); writer.Write(_rate);
                writer.Write(_rate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1, 1) * short.MaxValue));
            }
            float peak = 0; double squares = 0;
            foreach (float sample in samples) { peak = Mathf.Max(peak, Mathf.Abs(sample)); squares += sample * sample; }
            File.WriteAllText(Path.Combine(folder, "game-audio.txt"), FormattableString.Invariant(
                $"Actual game output, no microphone.\nrate={_rate}\nchannels={channels}\nsamples={samples.Length}\nseconds={samples.Length / (double)(_rate * channels):F6}\ndsp_start={_started:F6}\npeak={peak:F6}\nrms={Math.Sqrt(squares / samples.Length):F6}\n"));
        }
        private void OnDisable() { lock (_gate) _armed = false; }
    }
}
