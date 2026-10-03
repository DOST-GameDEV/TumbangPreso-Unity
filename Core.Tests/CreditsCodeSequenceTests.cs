using TumbangPreso.Core;
using Xunit;
namespace TumbangPreso.Core.Tests
{
    public sealed class CreditsCodeSequenceTests
    {
        private static readonly int[] Code = {0,0,1,1,2,3,2,3};
        private static void Complete(CreditsCodeSequence sequence)
        { for(int i=0;i<Code.Length;i++) Assert.Equal(i==7,sequence.Push(Code[i])); }
        [Fact] public void ExactEightDirectionsCompleteWithoutSuffix() { Complete(new CreditsCodeSequence()); Assert.Equal(5000,CreditsCodeSequence.Reward); }
        [Fact] public void RepeatedCompletedEntriesEachComplete() { var s=new CreditsCodeSequence(); Complete(s); Complete(s); }
        [Fact] public void ExtraLeadingUpPreservesOverlappingPrefix() { var s=new CreditsCodeSequence(); Assert.False(s.Push(0)); Complete(s); }
        [Fact] public void WrongDirectionCannotComplete() { var s=new CreditsCodeSequence(); foreach(int d in new[]{0,0,1,1,2,2,2,3}) Assert.False(s.Push(d)); Complete(s); }
        [Fact] public void ClosingResetsPartialEntry() { var s=new CreditsCodeSequence(); s.Push(0);s.Push(0);s.Push(1);s.Reset(); foreach(int d in new[]{1,2,3,2,3}) Assert.False(s.Push(d)); Complete(s); }
        [Fact] public void InvalidDirectionResetsPartialEntry() { var s=new CreditsCodeSequence();s.Push(0);s.Push(0);Assert.False(s.Push(-1));foreach(int d in new[]{1,1,2,3,2,3})Assert.False(s.Push(d));Complete(s); }
    }
}
