using System;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Tests
{
    public sealed class NetworkPayloadAndProfileTests
    {
        [Test]
        public void FourUnicodeProfilesFitTheirActualWireAllocation()
        {
            var seats=new LobbySeatInfo[4];
            for(int i=0;i<4;i++)seats[i]=new LobbySeatInfo{Seat=i,PeerId=i,
                Name="Niño María "+i,Occupied=true,CharacterPick=i,CanPick=i,SlipperPick=i,
                Banner=new BannerSelection(),Look=new string('a',160),Custom=new string('b',600),Build=new string('c',100)};
            using var writer=new FastBufferWriter(MatchRpc.LobbyRosterCapacity(seats),Allocator.Temp);
            writer.WriteValueSafe(4);
            foreach(var s in seats)
            {
                writer.WriteValueSafe(s.Seat);writer.WriteValueSafe(s.PeerId);writer.WriteValueSafe(s.Name);
                writer.WriteValueSafe(s.Occupied);writer.WriteValueSafe(s.Spectator);
                writer.WriteValueSafe(s.CharacterPick);writer.WriteValueSafe(s.CanPick);writer.WriteValueSafe(s.SlipperPick);
                writer.WriteValueSafe(s.Ready);writer.WriteValueSafe(BannerCodec.EncodeSelection(s.Banner));
                writer.WriteValueSafe(s.Look);writer.WriteValueSafe(s.Custom);writer.WriteValueSafe(s.Build);
            }
            writer.WriteValueSafe(3);
            Assert.Greater(writer.Length,512,"This fixture must exceed the writer that failed in the real room.");
            Assert.Greater(writer.Length,1400,"This fixture must also exercise fragmented delivery size.");
        }
        [Test]
        public void ExplicitProfilesAreStableDistinctAndCannotEscapeTheRoot()
        {
            string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"profile-test"));
            Assert.AreEqual(root,ProfilePaths.ForProfile(root,null));
            Assert.AreEqual(root,ProfilePaths.ForProfile(root," "));
            string one=ProfilePaths.ForProfile(root,"owner");
            Assert.AreEqual(one,ProfilePaths.ForProfile(root," owner "));
            Assert.AreNotEqual(one,ProfilePaths.ForProfile(root,"observer"));
            string hostile=Path.GetFullPath(ProfilePaths.ForProfile(root,"../../elsewhere"));
            StringAssert.StartsWith(root+Path.DirectorySeparatorChar,hostile);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void LinkedWorktreeBuildIdentityReadsItsCommonRefs(bool packed)
        {
            string temp=Path.Combine(Path.GetTempPath(),"tump-git-"+Guid.NewGuid().ToString("N"));
            try
            {
                string root=Path.Combine(temp,"work"),common=Path.Combine(temp,"meta"),gitdir=Path.Combine(common,"worktrees","fixture");
                Directory.CreateDirectory(root);Directory.CreateDirectory(gitdir);
                File.WriteAllText(Path.Combine(root,".git"),"gitdir: "+gitdir);
                File.WriteAllText(Path.Combine(gitdir,"HEAD"),"ref: refs/heads/ASTRAReworks");
                File.WriteAllText(Path.Combine(gitdir,"commondir"),"../..");
                const string sha="74664f4ea446175e29c38b31ec58acb947d6780d";
                if(packed)File.WriteAllText(Path.Combine(common,"packed-refs"),sha+" refs/heads/ASTRAReworks");
                else
                {
                    string refs=Path.Combine(common,"refs","heads");Directory.CreateDirectory(refs);
                    File.WriteAllText(Path.Combine(refs,"ASTRAReworks"),sha);
                }
                Assert.AreEqual(sha,BuildIdentity.HeadSha(root));
            }
            finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}
        }
    }
}
