using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI.Hub;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class UiBrowserFeedbackReview
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ActualJoinViewRefreshesQueryStatusAndKeepsCodeCaretInsideField()
        {
            yield return HubFlowTests.OpenHome();
            var query = NetSession.Ensure().Query;
            query.enabled = false; // Controlled reply only; no live service request in this check.
            var join = TumpHub.Current.Push<HubJoin>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var label = (Text)typeof(HubJoin).GetField("_empty", flags).GetValue(join);
            Assert.AreEqual("Finding public rooms...", label.text);
            var request = (Task)typeof(ServerQuery).GetMethod("RefreshOnlineLobbiesWithDispatchAsync", flags)
                .Invoke(query, new object[] {
                    (System.Func<Task<bool>>)(() => Task.FromResult(false)),
                    (System.Func<QueryLobbiesOptions,Task<QueryResponse>>)(_ => throw new System.Exception("Must not query without authentication")) });
            while (!request.IsCompleted) yield return null;
            request.GetAwaiter().GetResult();
            yield return new WaitForSecondsRealtime(1.1f);
            join.Tick();
            Assert.AreEqual(query.OnlineBrowserMessage, label.text);
            StringAssert.Contains("unavailable", label.text);
            yield return TumpUiCapture.Capture("UI-browser-unavailable", TumpHub.Current.Canvas, 1920,1080,false);

            typeof(HubJoin).GetMethod("Source", flags).Invoke(join, new object[] { 2 });
            var field = (InputField)typeof(HubJoin).GetField("_codeField", flags).GetValue(join);
            field.text = "UMKB"; field.caretPosition = 4; field.ActivateInputField();
            yield return null; yield return null;
            Assert.AreEqual(field, field.textComponent.GetComponentInParent<InputField>());
            Assert.IsTrue(field.isFocused);
            typeof(InputField).GetField("m_CaretVisible", flags).SetValue(field, true);
            typeof(InputField).GetMethod("UpdateGeometry", flags, null, System.Type.EmptyTypes, null).Invoke(field, null);
            var renderer = (CanvasRenderer)typeof(InputField).GetField("m_CachedInputRenderer", flags).GetValue(field);
            var caret = renderer.GetMesh();
            Assert.Greater(caret.vertexCount, 0);
            var rect = (RectTransform)field.transform;
            Rect allowed = rect.rect; allowed.xMin -= 2; allowed.xMax += 2; allowed.yMin -= 2; allowed.yMax += 2;
            foreach (var vertex in caret.vertices)
            {
                Vector3 local = rect.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                Assert.IsTrue(allowed.Contains((Vector2)local), "Caret escaped the actual code field: " + local);
            }
            yield return TumpUiCapture.Capture("UI-code-native-caret", TumpHub.Current.Canvas, 1920,1080,false);
        }
    }
}
