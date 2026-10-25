using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    // Render descriptions only: never instantiate gameplay components from these.
    public struct SpectatorPhantom
    {
        public const int WireBytes = 48;
        public const byte HexShoe = 1, DisorientedPlayer = 2, DisorientedShoe = 3;
        public byte Kind, SourceSeat;
        public ushort Skin;
        public uint Id;
        public Vector3 Position, Scale;
        public Quaternion Rotation;
        public bool IsValid => Id != 0 && Kind >= HexShoe && Kind <= DisorientedShoe
            && (Kind == DisorientedPlayer ? SourceSeat < Core.Balance.PlayerCount && Skin == 0
                : (SourceSeat < Core.Balance.PlayerCount || SourceSeat == byte.MaxValue) && Skin < Core.Roster.Slippers.Count)
            && SpectatorPrivateView.WorldPoint(Position) && SpectatorPrivateView.UnitRotation(Rotation)
            && SpectatorPrivateView.WorldPoint(Scale) && Scale.x >= .001f && Scale.y >= .001f && Scale.z >= .001f
            && Scale.x <= 20 && Scale.y <= 20 && Scale.z <= 20;
    }
    public struct SpectatorAimMarker
    {
        public bool Present, Beacon;
        public Vector3 Position;
        public float Radius;
        public byte Style;
        public Color32 Colour;
        public bool IsValid => !Present ? !Beacon : (SpectatorPrivateView.WorldPoint(Position)
            && float.IsFinite(Radius) && Radius >= .2f && Radius <= 100 && Style <= 6);
    }
    public struct SpectatorPrivateView
    {
        public const int MaxPhantoms = 9, MinWireBytes = 2, MaxWireBytes = 455;
        public FixedList512Bytes<SpectatorPhantom> Phantoms;
        public SpectatorAimMarker Marker;
        public bool IsValid
        {
            get
            {
                if (Phantoms.Length > MaxPhantoms || !Marker.IsValid) return false;
                for (int i = 0; i < Phantoms.Length; i++)
                {
                    var item = Phantoms[i]; if (!item.IsValid) return false;
                    for (int j = 0; j < i; j++)
                        if (item.Kind == Phantoms[j].Kind && item.Id == Phantoms[j].Id) return false;
                }
                return true;
            }
        }
        public void Write(FastBufferWriter writer)
        {
            writer.WriteValueSafe((byte)Phantoms.Length);
            foreach (var item in Phantoms)
            {
                writer.WriteValueSafe(item.Kind); writer.WriteValueSafe(item.SourceSeat); writer.WriteValueSafe(item.Skin);
                writer.WriteValueSafe(item.Id); writer.WriteValueSafe(item.Position); writer.WriteValueSafe(item.Rotation); writer.WriteValueSafe(item.Scale);
            }
            writer.WriteValueSafe((byte)(Marker.Present ? (Marker.Beacon ? 3 : 1) : 0));
            if (!Marker.Present) return;
            writer.WriteValueSafe(Marker.Position); writer.WriteValueSafe(Marker.Radius); writer.WriteValueSafe(Marker.Style);
            writer.WriteValueSafe(Marker.Colour.r); writer.WriteValueSafe(Marker.Colour.g);
            writer.WriteValueSafe(Marker.Colour.b); writer.WriteValueSafe(Marker.Colour.a);
        }
        public static bool TryRead(ref FastBufferReader reader, out SpectatorPrivateView view)
        {
            view = default; if (!reader.TryBeginRead(1)) return false;
            reader.ReadValueSafe(out byte count);
            if (count > MaxPhantoms || !reader.TryBeginRead(count * SpectatorPhantom.WireBytes + 1)) return false;
            for (int i = 0; i < count; i++)
            {
                var item = new SpectatorPhantom();
                reader.ReadValueSafe(out item.Kind); reader.ReadValueSafe(out item.SourceSeat); reader.ReadValueSafe(out item.Skin);
                reader.ReadValueSafe(out item.Id); reader.ReadValueSafe(out item.Position); reader.ReadValueSafe(out item.Rotation); reader.ReadValueSafe(out item.Scale);
                view.Phantoms.Add(item);
            }
            reader.ReadValueSafe(out byte marker);
            if ((marker & ~3) != 0 || marker == 2 || ((marker & 1) != 0 && !reader.TryBeginRead(21))) return false;
            if ((marker & 1) != 0)
            {
                view.Marker.Present = true;
                view.Marker.Beacon = (marker & 2) != 0;
                reader.ReadValueSafe(out view.Marker.Position); reader.ReadValueSafe(out view.Marker.Radius); reader.ReadValueSafe(out view.Marker.Style);
                reader.ReadValueSafe(out view.Marker.Colour.r); reader.ReadValueSafe(out view.Marker.Colour.g);
                reader.ReadValueSafe(out view.Marker.Colour.b); reader.ReadValueSafe(out view.Marker.Colour.a);
            }
            return view.IsValid;
        }
        internal static bool WorldPoint(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z)
            && Mathf.Abs(value.x) < 100000 && Mathf.Abs(value.y) < 100000 && Mathf.Abs(value.z) < 100000;
        internal static bool UnitRotation(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y)
            && float.IsFinite(value.z) && float.IsFinite(value.w) && Quaternion.Dot(value, value) > .8f && Quaternion.Dot(value, value) < 1.2f;
    }
}
