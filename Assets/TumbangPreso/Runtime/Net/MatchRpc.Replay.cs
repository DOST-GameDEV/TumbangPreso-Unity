using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TumbangPreso.CameraSystem;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        public const int ReplayChunkBytes=768;
        private sealed class ClipSend
        {public MatchReplayArchive.Retained Retained;public byte[] Hash;public int Offset;public bool Began;}
        private sealed class ClipReceive
        {public long Id;public byte[] Bytes,Hash;public int Offset;public float Touched;}
        private readonly Dictionary<ulong,Queue<ClipSend>> _clipSends=new Dictionary<ulong,Queue<ClipSend>>();
        private readonly Dictionary<ulong,HashSet<long>> _clipReady=new Dictionary<ulong,HashSet<long>>();
        private readonly List<RecordedMatchClip> _receivedClips=new List<RecordedMatchClip>(3);
        private ClipReceive _clipReceive;
        private long _clipEpoch;
        private float _nextClipSend;
        public int ReplayReadyCount(long clip)=>_clipReady.Values.Count(ids=>ids.Contains(clip));
        public RecordedMatchClip ReceivedReplay(long id)=>_receivedClips.FirstOrDefault(c=>c.Id==id&&c.MatchId==PresentationMatchId);
        private void ClearReplayTransfer()
        {_clipSends.Clear();_clipReady.Clear();_receivedClips.Clear();_clipReceive=null;_clipEpoch=PresentationMatchId;}
        public void StageReplay(MatchReplayArchive.Retained retained)
        {
            if(!NetAuthority.IsHost||_nm?.CustomMessagingManager==null||retained.Clip.MatchId!=PresentationMatchId)return;
            if(_clipEpoch!=PresentationMatchId)ClearReplayTransfer();
            foreach(ulong peer in _nm.ConnectedClientsIds)if(peer!=NetworkManager.ServerClientId)QueueReplay(peer,retained);
        }
        private void QueueReplay(ulong peer,MatchReplayArchive.Retained retained)
        {
            if(_clipReady.TryGetValue(peer,out var ready)&&ready.Contains(retained.Clip.Id))return;
            if(!_clipSends.TryGetValue(peer,out var queue))_clipSends[peer]=queue=new Queue<ClipSend>();
            if(queue.Any(c=>c.Retained.Clip.Id==retained.Clip.Id)||queue.Count>=3)return;
            using var sha=SHA256.Create();queue.Enqueue(new ClipSend{Retained=retained,Hash=sha.ComputeHash(retained.Bytes)});
        }
        private void SendReplayShortlist(ulong peer)
        {
            if(_clipEpoch!=PresentationMatchId)ClearReplayTransfer();
            var archive=FindAnyObjectByType<MatchReplayArchive>();if(archive==null)return;
            foreach(var retained in archive.Clips)QueueReplay(peer,retained);
        }
        private void TickReplayTransfer()
        {
            if(_clipEpoch!=PresentationMatchId)ClearReplayTransfer();
            if(_clipReceive!=null&&Time.unscaledTime-_clipReceive.Touched>20)_clipReceive=null;
            if(!NetAuthority.IsHost||_nm?.CustomMessagingManager==null||Time.unscaledTime<_nextClipSend)return;
            _nextClipSend=Time.unscaledTime+.05f;
            // At most 30 KiB/s per peer, two sub-MTU payloads per tick. No catch-up burst.
            foreach(var pair in _clipSends.ToArray())
            {
                if(!_nm.ConnectedClientsIds.Contains(pair.Key)){_clipSends.Remove(pair.Key);_clipReady.Remove(pair.Key);continue;}
                var queue=pair.Value;if(queue.Count==0)continue;var send=queue.Peek();var clip=send.Retained;
                if(!send.Began)
                {
                    using var begin=new FastBufferWriter(64,Allocator.Temp);begin.WriteValueSafe(PresentationMatchId);begin.WriteValueSafe(clip.Clip.Id);begin.WriteValueSafe(clip.Bytes.Length);
                    foreach(byte b in send.Hash)begin.WriteValueSafe(b);
                    _nm.CustomMessagingManager.SendNamedMessage("ReplayBegin",pair.Key,begin,RecordDelivery);send.Began=true;
                }
                for(int n=0;n<2&&send.Offset<clip.Bytes.Length;n++)
                {
                    int count=Math.Min(ReplayChunkBytes,clip.Bytes.Length-send.Offset);
                    using var chunk=new FastBufferWriter(ReplayChunkBytes+32,Allocator.Temp);chunk.WriteValueSafe(PresentationMatchId);chunk.WriteValueSafe(clip.Clip.Id);chunk.WriteValueSafe(send.Offset);chunk.WriteValueSafe(count);
                    for(int i=0;i<count;i++)chunk.WriteValueSafe(clip.Bytes[send.Offset+i]);
                    _nm.CustomMessagingManager.SendNamedMessage("ReplayChunk",pair.Key,chunk,RecordDelivery);send.Offset+=count;
                }
                if(send.Offset==clip.Bytes.Length)
                {
                    using var end=new FastBufferWriter(16,Allocator.Temp);end.WriteValueSafe(PresentationMatchId);end.WriteValueSafe(clip.Clip.Id);
                    _nm.CustomMessagingManager.SendNamedMessage("ReplayEnd",pair.Key,end,RecordDelivery);queue.Dequeue();
                }
            }
        }
        private void OnReplayBegin(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!reader.TryBeginRead(52))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out long id);reader.ReadValueSafe(out int count);
            if(match!=PresentationMatchId||id<=0||count<8||count>RecordedMatchClip.ByteLimit)return;
            if(_clipEpoch!=PresentationMatchId)ClearReplayTransfer();
            var hash=new byte[32];for(int i=0;i<32;i++)reader.ReadValueSafe(out hash[i]);
            _clipReceive=new ClipReceive{Id=id,Bytes=new byte[count],Hash=hash,Touched=Time.unscaledTime};
        }
        private void OnReplayChunk(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!reader.TryBeginRead(24))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out long id);reader.ReadValueSafe(out int offset);reader.ReadValueSafe(out int count);
            var incoming=_clipReceive;
            if(match!=PresentationMatchId||incoming==null||incoming.Id!=id||offset!=incoming.Offset||count<1||count>ReplayChunkBytes||offset>incoming.Bytes.Length-count||!reader.TryBeginRead(count))return;
            for(int i=0;i<count;i++)reader.ReadValueSafe(out incoming.Bytes[offset+i]);incoming.Offset+=count;incoming.Touched=Time.unscaledTime;
        }
        private void OnReplayEnd(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!reader.TryBeginRead(16))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out long id);var incoming=_clipReceive;
            if(match!=PresentationMatchId||incoming==null||incoming.Id!=id)return;
            _clipReceive=null;if(incoming.Offset!=incoming.Bytes.Length)return;
            using var sha=SHA256.Create();if(!sha.ComputeHash(incoming.Bytes).SequenceEqual(incoming.Hash))return;
            if(!RecordedMatchClip.TryDecode(incoming.Bytes,out var clip,out _)||clip.MatchId!=match||clip.Id!=id)return;
            _receivedClips.RemoveAll(c=>c.Id==id);_receivedClips.Add(clip);
            _receivedClips.Sort((a,b)=>a.Reason==b.Reason?b.Id.CompareTo(a.Id):a.Reason=="CATCH"?-1:1);
            if(_receivedClips.Count>3)_receivedClips.RemoveAt(3);
            using var ready=new FastBufferWriter(16,Allocator.Temp);ready.WriteValueSafe(match);ready.WriteValueSafe(id);
            _nm.CustomMessagingManager.SendNamedMessage("ReplayReady",NetworkManager.ServerClientId,ready);
        }
        private void OnReplayReady(ulong sender,FastBufferReader reader)
        {
            if(!NetAuthority.IsHost||!_nm.ConnectedClientsIds.Contains(sender)||!reader.TryBeginRead(16))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out long id);
            var archive=FindAnyObjectByType<MatchReplayArchive>();
            if(match!=PresentationMatchId||archive==null||!archive.Clips.Any(c=>c.Clip.Id==id))return;
            if(!_clipReady.TryGetValue(sender,out var ready))_clipReady[sender]=ready=new HashSet<long>();
            ready.RemoveWhere(known=>!archive.Clips.Any(c=>c.Clip.Id==known));ready.Add(id);
        }
        public void BroadcastBreak()
        {
            var phase=HalftimePresentation.Instance;
            if(!NetAuthority.IsHost||_nm?.CustomMessagingManager==null||phase?.Active!=true)return;
            using var writer=new FastBufferWriter(48,Allocator.Temp);writer.WriteValueSafe(phase.MatchId);writer.WriteValueSafe(phase.CompletedRound);
            writer.WriteValueSafe(phase.NextTaya);writer.WriteValueSafe(phase.Began);writer.WriteValueSafe(phase.ClipId);writer.WriteValueSafe(phase.IsHalftime);writer.WriteValueSafe(PresentationClock.RequestedScale);
            _nm.CustomMessagingManager.SendNamedMessageToAll("MatchBreak",writer);
        }
        private void OnMatchBreak(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!reader.TryBeginRead(37))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out int completed);reader.ReadValueSafe(out int taya);reader.ReadValueSafe(out double began);
            reader.ReadValueSafe(out long clip);reader.ReadValueSafe(out bool halftime);reader.ReadValueSafe(out float scale);
            HalftimePresentation.Ensure()?.Receive(match,completed,taya,began,clip,halftime,scale);
        }
    }
}
