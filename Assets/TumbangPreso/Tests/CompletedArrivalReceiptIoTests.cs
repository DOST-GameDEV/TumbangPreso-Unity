using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class CompletedArrivalReceiptIoTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _body;
        private NetCompletedArrivalProbe _probe;
        private object _receipt;
        private string _directory, _path;

        [SetUp] public void Before()
        {
            _directory = Path.Combine(Path.GetTempPath(), "tp-completed-receipt-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory); _path = Path.Combine(_directory, "client.json");
            _body = new GameObject("Owned completed-arrival receipt");
            _probe = _body.AddComponent<NetCompletedArrivalProbe>();
            typeof(NetCompletedArrivalProbe).GetField("_folder", Private).SetValue(_probe, _directory);
            _receipt = typeof(NetCompletedArrivalProbe).GetField("_receipt", Private).GetValue(_probe);
            _receipt.GetType().GetField("role").SetValue(_receipt, "client");
            Invoke("Save");
        }

        [TearDown] public void After()
        {
            if (_body != null) UnityEngine.Object.DestroyImmediate(_body);
            if (_path != null && File.Exists(_path)) File.Delete(_path);
            if (_directory != null && Directory.Exists(_directory)) Directory.Delete(_directory);
        }

        private object Invoke(string method, params object[] args)
            => typeof(NetCompletedArrivalProbe).GetMethod(method, Private).Invoke(_probe, args);
        private int EndCount => (int)_receipt.GetType().GetField("matchEndedEvents").GetValue(_receipt);

        [Test] public void LockedReceiptCannotThrowOutOfTheMatchEndObserverAndLaterSaveRecovers()
        {
            using (var reader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"\[CompletedArrival\] receipt not written:"));
                Assert.DoesNotThrow(() => Invoke("Ended", 3));
                Assert.DoesNotThrow(() => Invoke("Save"));
                Assert.AreEqual(1, EndCount);
            }
            Assert.DoesNotThrow(() => Invoke("Save"));
            StringAssert.Contains("\"matchEndedEvents\": 1", File.ReadAllText(_path));
        }

        [Test] public void ReceiptReadAllowsAnExistingSharedWriter()
        {
            using (var writer = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var read = Invoke("Read", "client");
                Assert.IsNotNull(read);
                Assert.AreEqual("client", read.GetType().GetField("role").GetValue(read));
            }
        }

        [Test] public void OrdinaryObserverWritesPublishTheirLatestCount()
        {
            Invoke("Ended", 2); Invoke("Ended", 3);
            Assert.AreEqual(2, EndCount);
            StringAssert.Contains("\"matchEndedEvents\": 2", File.ReadAllText(_path));
        }
    }
}
