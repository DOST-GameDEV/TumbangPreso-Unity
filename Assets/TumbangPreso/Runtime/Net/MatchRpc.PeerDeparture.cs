using System;
using System.Text;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly PeerLeaveIntents _peerLeaveIntents = new();
        private int _peerDepartureSequence, _lastPeerDepartureSequence;
        public string LastPeerDepartureText { get; private set; } = "";
        public int PeerDepartureNotices { get; private set; }

        private void ClearPeerDepartureState()
        {
            _peerLeaveIntents.Clear();
            _peerDepartureSequence = _lastPeerDepartureSequence = 0;
            LastPeerDepartureText = "";
            PeerDepartureNotices = 0;
        }

        // A hint about THIS sender, never a request to remove another seat. NGO's
        // ordinary Shutdown(false) drains its outgoing queue before teardown.
        public void NotifyLocalPeerLeaving()
        {
            if (_nm == null || !_nm.IsConnectedClient || _nm.IsServer ||
                _nm.CustomMessagingManager == null || PresentationMatchId <= 0) return;
            using var writer = new FastBufferWriter(sizeof(long), Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId);
            _nm.CustomMessagingManager.SendNamedMessage("PeerLeaveIntent", NetworkManager.ServerClientId,
                writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnPeerLeaveIntentMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || _nm == null || senderClientId == _nm.LocalClientId ||
                senderClientId > int.MaxValue || reader.Length != sizeof(long) ||
                !reader.TryBeginRead(sizeof(long))) return;
            reader.ReadValueSafe(out long match);
            var lobby = NetSession.Instance?.Lobby;
            var peer = lobby?.PeerById((int)senderClientId);
            if (lobby?.MatchInProgress != true || peer == null || !ValidSlot(peer.Seat) ||
                match <= 0 || match != PresentationMatchId) return;
            _peerLeaveIntents.Remember(peer.PeerId, match, Time.realtimeSinceStartupAsDouble);
            // Do not change ownership, drive a bot or show a notice here. Only the
            // actual disconnect callback may confirm that this player has gone.
        }

        private void HostAnnouncePeerDeparture(PeerRecord departed, bool intentional)
        {
            if (!NetAuthority.IsHost || departed == null || !ValidSlot(departed.Seat) ||
                NetSession.Instance?.IsStopping == true || _nm?.CustomMessagingManager == null ||
                PresentationMatchId <= 0) return;
            bool bot = Unit(departed.Seat)?.IsBot == true;
            byte reason = (byte)(intentional ? 1 : 0);
            string name = SafeDepartureName(departed.Name, departed.Seat);
            int sequence = ++_peerDepartureSequence;
            PresentPeerDeparture(name, intentional, bot);
            using var writer = new FastBufferWriter(192, Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId);
            writer.WriteValueSafe(sequence);
            writer.WriteValueSafe(departed.Seat);
            writer.WriteValueSafe(reason);
            writer.WriteValueSafe(bot);
            writer.WriteValueSafe(name.Length);
            foreach (char character in name) writer.WriteValueSafe((ushort)character);
            foreach (ulong client in _nm.ConnectedClientsIds)
                if (client != _nm.LocalClientId && client != (ulong)departed.PeerId)
                    _nm.CustomMessagingManager.SendNamedMessage("PeerDeparture", client, writer,
                        NetworkDelivery.ReliableSequenced);
        }

        private void OnPeerDepartureMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length > 192 ||
                !reader.TryBeginRead(sizeof(long) + sizeof(int) * 3 + 2)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int sequence);
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out byte reason);
            reader.ReadValueSafe(out bool bot);
            reader.ReadValueSafe(out int nameLength);
            if (nameLength < 0 || nameLength > 32 || !reader.TryBeginRead(nameLength * sizeof(ushort))) return;
            var characters = new char[nameLength];
            for (int i = 0; i < characters.Length; i++)
            { reader.ReadValueSafe(out ushort character); characters[i] = (char)character; }
            if (match <= 0 || match != PresentationMatchId || sequence <= _lastPeerDepartureSequence ||
                !ValidSlot(seat) || reason > 1) return;
            _lastPeerDepartureSequence = sequence;
            PresentPeerDeparture(SafeDepartureName(new string(characters), seat), reason == 1, bot);
        }

        private static string SafeDepartureName(string raw, int seat)
        {
            var text = new StringBuilder(32);
            foreach (char c in raw ?? "")
            {
                if (char.IsControl(c)) continue;
                if (text.Length == 32) break;
                // HUD text can support rich text; a public display name cannot insert tags.
                text.Append(c == '<' ? '‹' : c == '>' ? '›' : c);
            }
            string clean = text.ToString().Trim();
            return clean.Length > 0 ? clean : "PLAYER " + (seat + 1);
        }

        private void PresentPeerDeparture(string name, bool intentional, bool bot)
        {
            LastPeerDepartureText = name + (intentional ? " LEFT" : " DISCONNECTED")
                + (bot ? " · BOT TAKES OVER" : " · SEAT RESERVED");
            PeerDepartureNotices++;
            UI.Hud.Instance?.ShowToast(LastPeerDepartureText, 3.2f);
        }
    }
}
