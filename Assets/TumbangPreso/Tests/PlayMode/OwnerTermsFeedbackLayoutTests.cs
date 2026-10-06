using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerTermsFeedbackLayoutTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator TermsStayRaisedUntilFeedbackNeedsSpace()
        {
            var owner=new GameObject("TermsFeedbackLayout");
            try
            {
                var login=owner.AddComponent<SignInScreen>(); login.Install(); login.OpenAtBoot();
                yield return new WaitForSecondsRealtime(.6f);
                var canvas=GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                var row=(RectTransform)canvas.transform.Find("OwnerLoginForm/TermsRow");
                if(row==null) row=(RectTransform)canvas.GetComponentsInChildren<OwnerConsentPulse>().Single().transform;
                var fields=canvas.GetComponentsInChildren<InputField>();
                var user=fields.Single(x=>x.name=="Username"); var pass=fields.Single(x=>x.name=="Password");
                var confirm=fields.Single(x=>x.name=="ConfirmPassword");
                var create=(RectTransform)canvas.GetComponentsInChildren<Button>().Single(x=>x.name=="SubmitAccount").transform;
                var createPosition=create.anchoredPosition;
                Assert.AreEqual(-726,row.anchoredPosition.y,.01f,"Default terms position rises12 units");
                foreach(int width in new[]{1920,1280})
                    yield return TumpUiCapture.Capture("TermsRaised1007-"+width,canvas,width,width==1920?1080:720,false);
                user.text="terms.fixture"; pass.text=confirm.text="Test-only1";
                yield return null; yield return null;
                Assert.AreEqual(-726,row.anchoredPosition.y,.01f,"Ordinary typed text must not lower terms");
                confirm.text="Other-only2"; yield return null; yield return null;
                Assert.AreEqual(-738,row.anchoredPosition.y,.01f,"Visible feedback returns the row to its original position");
                Assert.IsNotEmpty(canvas.GetComponentsInChildren<Text>().Single(x=>x.name=="ConfirmFault").text);
                foreach(int width in new[]{1920,1280})
                    yield return TumpUiCapture.Capture("TermsFeedbackSpace1007-"+width,canvas,width,width==1920?1080:720,false);
                confirm.text=pass.text; yield return null; yield return null;
                Assert.AreEqual(-726,row.anchoredPosition.y,.01f,"Correcting feedback raises terms again");
                Assert.AreEqual(createPosition,create.anchoredPosition,"The create action must stay in place");
            }
            finally { Object.Destroy(owner); }
        }
    }
}
