using System;

namespace TumbangPreso.Core
{
    // Only the latest prediction in a channel can have its current action state undone.
    public sealed class PredictionReceiptWindow
    {
        private readonly long[] _requests;
        private readonly bool[] _denied;
        private long _sequence;

        public PredictionReceiptWindow(int channels)
        {
            if (channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));
            _requests = new long[channels]; _denied = new bool[channels];
        }

        public long Begin(int channel)
        {
            if (channel < 0 || channel >= _requests.Length) throw new ArgumentOutOfRangeException(nameof(channel));
            long request = checked(++_sequence);
            _requests[channel] = request; _denied[channel] = false;
            return request;
        }

        public bool TryDeny(int channel, long request)
        {
            if (channel < 0 || channel >= _requests.Length || request <= 0
                || _requests[channel] != request || _denied[channel]) return false;
            _denied[channel] = true;
            return true;
        }

        public void Reset()
        {
            Array.Clear(_requests, 0, _requests.Length);
            Array.Clear(_denied, 0, _denied.Length);
        }
    }
}
