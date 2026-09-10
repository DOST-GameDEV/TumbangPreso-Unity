using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // Live Editor verification must retain the same native NUnit evidence as a
    // batch run. Register again after domain reload so PlayMode completion is kept.
    [InitializeOnLoad]
    public static class LiveTestResultRecorder
    {
        private static TestRunnerApi _api;
        private static Recorder _recorder;
        static LiveTestResultRecorder()
        {
            EditorApplication.delayCall+=Install;
            AssemblyReloadEvents.beforeAssemblyReload+=Unregister;
        }
        private static void Install()
        {
            if(_api!=null)return;
            _api=ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.hideFlags=HideFlags.HideAndDontSave;
            _recorder=new Recorder();_api.RegisterCallbacks(_recorder);
        }
        private static void Unregister()
        {
            if(_api==null)return;
            _api.UnregisterCallbacks(_recorder);
            UnityEngine.Object.DestroyImmediate(_api);_api=null;
        }
        private sealed class Recorder:ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                string path=Path.GetFullPath(Path.Combine("Logs","live-tests-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff")+".xml"));
                TestRunnerApi.SaveResultToFile(result,path);
                File.WriteAllText("Logs/live-test-latest.txt",path);
                Debug.Log("[LiveTests] Native NUnit result saved: "+path);
            }
        }
    }
}
