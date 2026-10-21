using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
namespace TumbangPreso.Tests
{
    public sealed class RecordedClipRejectionTests
    {
        [TestCase(0x42414421,12,"Unsupported clip schema")]
        [TestCase(0x54554d50,99,"Unsupported clip schema")]
        public void MalformedHeaderReturnsAReasonWithoutThrowing(int magic,int version,string expected)
        {
            using var raw=new MemoryStream();
            using(var writer=new BinaryWriter(raw,Encoding.UTF8,true))
            {writer.Write(magic);writer.Write(version);writer.Write(new byte[32]);}
            raw.Position=0;using var packed=new MemoryStream();
            using(var zip=new DeflateStream(packed,CompressionLevel.Fastest,true))raw.CopyTo(zip);
            byte[] bytes=packed.ToArray();Assert.GreaterOrEqual(bytes.Length,8,"Exercise the header parser, not the short-input gate.");
            Assert.IsFalse(RecordedMatchClip.TryDecode(bytes,out var clip,out var reason));
            Assert.IsNull(clip);StringAssert.Contains(expected,reason);
        }
    }
}
