using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerLoginInputAcceptanceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator CaretAndSelectionStayInsideThePaintedEditableArea()
        {
            var owner = new GameObject("LoginInputAcceptance");
            try
            {
                var login = owner.AddComponent<SignInScreen>(); login.Install(); login.OpenAtBoot();
                yield return new WaitForSecondsRealtime(.5f);
                var canvas = GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>().Any(x => x.name == "SignInBack"));
                var user = canvas.GetComponentsInChildren<InputField>().Single(x => x.name == "Username");
                var pass = canvas.GetComponentsInChildren<InputField>().Single(x => x.name == "Password");
                foreach (int width in new[] {1920,1280})
                {
                    foreach (string state in new[] {"empty","typed","password","long","select-all"})
                    {
                        var field = state == "password" ? pass : user;
                        field.text = state == "empty" ? "" : state == "long" || state == "select-all"
                            ? new string('W',100) : "Test-only1";
                        field.ActivateInputField(); yield return null; yield return null;
                        Assert.IsTrue(field.isFocused);
                        field.caretPosition = field.text.Length;
                        if (state == "select-all") { field.selectionAnchorPosition=0; field.selectionFocusPosition=field.text.Length; }
                        string shot = "LoginInput1006-"+state+"-"+width;
                        yield return TumpUiCapture.Capture(shot,canvas,width,width==1920?1080:720,false,
                            inspectViewport: () => InspectGeometry(field,state=="select-all",shot));
                        field.DeactivateInputField();
                    }
                }
            }
            finally { Object.Destroy(owner); }
        }

        private static void InspectGeometry(InputField field,bool selection,string shot)
        {
            Assert.IsInstanceOf<OwnerBoundedInputField>(field);
            typeof(InputField).GetField("m_CaretVisible",Private).SetValue(field,true);
            typeof(InputField).GetMethod("UpdateGeometry",Private,null,System.Type.EmptyTypes,null).Invoke(field,null);
            field.Rebuild(CanvasUpdate.LatePreRender);
            var renderer=(CanvasRenderer)typeof(InputField).GetField("m_CachedInputRenderer",Private).GetValue(field);
            Assert.IsNotNull(renderer);
            var mesh=renderer.GetMesh(); Assert.Greater(mesh.vertexCount,0,shot);
            var area=field.textComponent.rectTransform;
            var bounds=area.rect;
            foreach(var vertex in mesh.vertices)
            {
                Vector3 point=area.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                Assert.That(point.x,Is.InRange(bounds.xMin-.01f,bounds.xMax+.01f),shot+" horizontal caret/selection");
                Assert.That(point.y,Is.InRange(bounds.yMin+2.99f,bounds.yMax-2.99f),shot+" vertical caret/selection");
            }
            Assert.Greater(mesh.bounds.size.y,1,shot+" must remain visible");
            if(selection) Assert.Greater(mesh.bounds.size.x,field.caretWidth+1,shot+" must measure selection");
            else Assert.That(mesh.bounds.size.x,Is.InRange(.5f,field.caretWidth+.1f),shot+" must measure thin caret");
            if(field.contentType==InputField.ContentType.Password)
                Assert.IsFalse(field.textComponent.text.Contains("Test-only1"),"Password must remain masked");
            Debug.Log("[LoginInput1006] "+shot+" editable="+bounds+" rendered="+mesh.bounds+" focused="+field.isFocused);
        }

        [UnityTest]
        public IEnumerator AllInvalidRequirementsHighlightTogetherAndClearAfterCorrection()
        {
            var owner=new GameObject("LoginConsentAcceptance");
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                var login=owner.AddComponent<SignInScreen>(); login.Install(); login.OpenForUpgrade();
                yield return new WaitForSecondsRealtime(.5f);
                var canvas=GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                var fields=canvas.GetComponentsInChildren<InputField>().ToArray();
                var user=fields.Single(x=>x.name=="Username"); var pass=fields.Single(x=>x.name=="Password");
                var confirm=fields.Single(x=>x.name=="ConfirmPassword");
                var terms=canvas.GetComponentsInChildren<Toggle>().Single(x=>x.name=="TermsAcceptance");
                var pulse=terms.GetComponentInParent<OwnerConsentPulse>();
                var submit=canvas.GetComponentsInChildren<Button>().Single(x=>x.name=="SubmitAccount");
                var rect=(RectTransform)pulse.transform;
                var position=rect.anchoredPosition; var size=rect.sizeDelta; var scale=rect.localScale;
                foreach(bool reducedMode in new[]{false,true})
                {
                    Settings.SettingsStore.Current.ReducedUiMotion=reducedMode;
                    user.text=pass.text=confirm.text=""; terms.isOn=false; yield return null;
                    submit.onClick.Invoke(); // Local invalid scenarios only: no service dispatch.
                    Assert.IsTrue(pulse.IsPulsing,"Missing terms pulse");
                    foreach(var f in new[]{user,pass,confirm}) Assert.IsTrue(f.GetComponent<OwnerFieldPulse>().IsPulsing,f.name);
                    Assert.AreEqual("Enter a username.",Text(canvas,"UsernameFault"));
                    Assert.AreEqual("Enter a password.",Text(canvas,"PasswordFault"));
                    Assert.AreEqual("Confirm your password.",Text(canvas,"ConfirmFault"));
                    StringAssert.Contains("terms checkbox",Text(canvas,"AccountStatus"));
                    Assert.AreEqual(position,rect.anchoredPosition); Assert.AreEqual(size,rect.sizeDelta); Assert.AreEqual(scale,rect.localScale);
                    if(reducedMode)
                    {
                        var ink=pulse.GetComponentsInChildren<Graphic>().Select(x=>x.color).ToArray();
                        yield return new WaitForSecondsRealtime(.15f);
                        CollectionAssert.AreEqual(ink,pulse.GetComponentsInChildren<Graphic>().Select(x=>x.color).ToArray(),"Reduced motion tint must remain steady");
                    }
                    terms.isOn=true; yield return null;
                    Assert.IsFalse(pulse.IsPulsing);
                    Assert.IsFalse(Text(canvas,"AccountStatus").Contains("terms"),"Accepted terms must clear its instruction");
                    user.text="feedback.fixture"; pass.text=confirm.text="Test-only1"; yield return null; yield return null;
                    foreach(string fault in new[]{"UsernameFault","PasswordFault","ConfirmFault","AccountStatus"}) Assert.IsEmpty(Text(canvas,fault),fault+" must clear after correction");
                    foreach(var f in new[]{user,pass,confirm}) Assert.IsFalse(f.GetComponent<OwnerFieldPulse>().IsPulsing);
                    terms.isOn=false; submit.onClick.Invoke();
                    Assert.IsTrue(pulse.IsPulsing);
                    Assert.AreEqual("Read and accept the terms to create an account.",Text(canvas,"AccountStatus"));
                    terms.isOn=true; yield return null;
                    Assert.IsEmpty(Text(canvas,"AccountStatus"),"Terms-only refusal must clear after acceptance");
                }
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion=reduced; Object.Destroy(owner); }
        }
        private static string Text(Canvas canvas,string name)=>canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(x=>x.name==name).text;
    }
}
