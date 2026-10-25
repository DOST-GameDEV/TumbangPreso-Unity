using System;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    // A bounded presentation measurement. No remote timestamp, gameplay state,
    // reliable queue or authority decision participates in a round trip.
    public sealed class LinkRoundTrips
    {
        private const int Capacity = 8;
        private readonly double[] _sent = new double[Capacity];
        private readonly uint[] _serials = new uint[Capacity];
        private readonly ulong _nonce;
        private uint _issued, _accepted;
        private double _replyAt;
        public bool HasReply => _accepted > 0;
        public long RttMs { get; private set; }
        public ulong Nonce => _nonce;
        public LinkRoundTrips(ulong nonce) { _nonce = nonce; }
        public uint Begin(double now)
        {
            if (_nonce == 0 || !double.IsFinite(now) || now < 0 || _issued == uint.MaxValue) return 0;
            uint serial = ++_issued; int index = (int)(serial % Capacity);
            _serials[index] = serial; _sent[index] = now;
            return serial;
        }
        public bool Accept(ulong nonce, uint serial, double now)
        {
            if (nonce != _nonce || nonce == 0 || serial == 0 || serial <= _accepted || serial > _issued || !double.IsFinite(now)) return false;
            int index = (int)(serial % Capacity); double elapsed = now - _sent[index];
            if (_serials[index] != serial || elapsed < 0 || elapsed > Capacity) return false;
            _accepted = serial; _replyAt = now;
            RttMs = Math.Max(1, (long)Math.Round(elapsed * 1000));
            return true;
        }
        public double ReplyAge(double now) => HasReply && double.IsFinite(now) ? Math.Max(0, now - _replyAt) : -1;
    }

    public static class LinkQualityPacket
    {
        public const int Bytes = 17;
        private const uint Magic = 0x514C5054; // TPLQ, an optional cosmetic channel.
        public static void Write(ref FastBufferWriter writer, bool reply, ulong nonce, uint serial)
        {
            writer.WriteValueSafe(Magic); writer.WriteValueSafe((byte)(reply ? 0x11 : 0x10));
            writer.WriteValueSafe(nonce); writer.WriteValueSafe(serial);
        }
        public static bool TryRead(ref FastBufferReader reader, out bool reply, out ulong nonce, out uint serial)
        {
            reply = false; nonce = 0; serial = 0;
            if (reader.Length - reader.Position != Bytes || !reader.TryBeginRead(Bytes)) return false;
            reader.ReadValueSafe(out uint magic); reader.ReadValueSafe(out byte kind);
            reader.ReadValueSafe(out nonce); reader.ReadValueSafe(out serial);
            if (magic != Magic || (kind != 0x10 && kind != 0x11) || nonce == 0 || serial == 0) return false;
            reply = kind == 0x11; return true;
        }
    }
}
