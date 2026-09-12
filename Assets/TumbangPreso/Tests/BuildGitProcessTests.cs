using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class BuildGitProcessTests
    {
        [Test,Timeout(15000)]
        public void BuildIdentityDrainsAnErrorPipeLargerThanItsBuffer()
        {
            // Per-call git alias, no repository config or files changed. Git's
            // bundled shell writes 64KB to stderr before the stdout completion.
            var args=new object[]{Path.GetDirectoryName(Application.dataPath),
                "-c \"alias.stderr-probe=!head -c 65536 /dev/zero >&2; echo pipe-drained\" stderr-probe",false};
            var watch=Stopwatch.StartNew();
            string result=(string)typeof(GameBuilder).GetMethod("Git",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
            Assert.IsTrue((bool)args[2],"The bounded git call did not complete successfully.");
            Assert.AreEqual("pipe-drained",result.Trim());
            Assert.Less(watch.Elapsed.TotalSeconds,10,"Pipe handling reached the timeout on a completed command.");
        }
        [Test,Timeout(15000)]
        public void FailedGitDoesNotCertifyTheWorkingTree()
        {
            var args=new object[]{Path.GetDirectoryName(Application.dataPath),"not-a-real-git-subcommand-for-build-test",true};
            string result=(string)typeof(GameBuilder).GetMethod("Git",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
            Assert.IsFalse((bool)args[2]);Assert.IsEmpty(result);
        }
    }
}
