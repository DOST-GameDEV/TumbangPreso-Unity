using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerMenuEditsTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest, Timeout(60000)]
        public IEnumerator SplashShaderAndMenuArtWarmupsCompleteInBoundedStages()
        {
            var collection = Resources.Load<ShaderVariantCollection>("ShaderWarmup");
            Assert.IsNotNull(collection);
            Assert.Greater(collection.variantCount, 10, "The collection no longer exercises multiple slices.");
            var go = new GameObject("SplashWarmupOnly");
            var splash = go.AddComponent<SplashScreen>();
            splash.enabled = false;
            bool shaderStageFinished = false;
            void Observe(string message, string stack, LogType type)
            {
                if (message.StartsWith("[SplashShaders] shaders=")) shaderStageFinished = true;
            }
            Application.logMessageReceived += Observe;
            IEnumerator preload = null;
            try
            {
                var method = typeof(SplashScreen).GetMethod("PreloadGameAssets",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(method);
                preload = (IEnumerator)method.Invoke(splash, null);
                int steps = 0;
                int bound = Mathf.CeilToInt(collection.variantCount / 10f) + 3;
                while (!shaderStageFinished && steps++ < bound)
                {
                    Assert.IsTrue(preload.MoveNext(), "Splash preload ended before its shader stage completed.");
                    Assert.IsNull(preload.Current, "The shader stage entered a later preload operation.");
                    yield return null;
                }
                Assert.IsTrue(shaderStageFinished, "The bounded shader stage did not finish.");
                Assert.IsTrue(collection.isWarmedUp, "Splash left shader variants cold.");
                Assert.AreEqual(collection.variantCount, collection.warmedUpVariantCount);
            }
            finally
            {
                Application.logMessageReceived -= Observe;
                (preload as System.IDisposable)?.Dispose();
                Object.Destroy(go);
            }

            int menuSteps = 0;
            var menuWarmup = OwnerMenuArt.Warmup();
            while (menuWarmup.MoveNext()) { menuSteps++; yield return menuWarmup.Current; }
            Assert.AreEqual(27, menuSteps, "All current title and login art should get a staged turn.");
            var background = OwnerMenuArt.Texture("main2-background");
            var logo = OwnerMenuArt.Piece("login3-logo");
            Assert.IsNotNull(background);
            Assert.IsNotNull(logo);
            menuWarmup = OwnerMenuArt.Warmup();
            while (menuWarmup.MoveNext()) yield return menuWarmup.Current;
            Assert.AreSame(background, OwnerMenuArt.Texture("main2-background"));
            Assert.AreSame(logo, OwnerMenuArt.Piece("login3-logo"));

            int avatarSteps = 0;
            var avatarWarmup = Avatars.Warmup();
            while (avatarWarmup.MoveNext()) { avatarSteps++; yield return avatarWarmup.Current; }
            Assert.AreEqual(Avatars.Ids.Length, avatarSteps);
            Assert.AreEqual(22, avatarSteps, "Every currently offered avatar should get a staged turn.");
            var first = Avatars.Get(Avatars.Ids[0]);
            Assert.IsNotNull(first);
            Assert.AreSame(first, Avatars.Get(Avatars.Ids[0]));
            Assert.AreSame(first, Avatars.Get("not-an-offered-avatar"),
                "Unknown saved IDs must still use the first face.");
        }

        /// <summary>
        /// ⚠️⚠️ THIS FIXTURE USED TO DRIVE THE SETTINGS PENNANT AND THERE IS NO PENNANT NOW.
        /// 🧑 2026-09-18: *"MAIN menu is getting revamped it will lose all buttons and will just
        /// have a tap to play or wtv text is"*, and asked where TUTORIAL, SETTINGS and QUIT
        /// should go, *"throw them away gang no need"*. What it was really asserting was that
        /// the owner's art loads, that a control reacts without its hit box moving, and that the
        /// road keeps moving with motion on and stops with it off. All three still have a
        /// subject: the plate, the one full-screen press target, and the dust and the leaves.
        /// </summary>
        [UnityTest,Timeout(120000)]
        public IEnumerator TitleStreetIsOnePressWithHerWeatherMoving()
        {
            bool boot=SceneFlow.BootedThroughSplash,offered=SceneFlow.LoginStepOffered;
            bool reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                SceneFlow.BootedThroughSplash=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                EventSystem.current.SetSelectedGameObject(null);

                // ⚠️ ONE CONTROL, AND IT IS THE WHOLE SCREEN. A second button here would be the
                // start of growing the pennants back one at a time.
                var buttons=canvas.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled).ToArray();
                Assert.AreEqual(1,buttons.Length,"The title screen is one press: "+
                    string.Join(", ",buttons.Select(b=>b.name)));
                Assert.AreEqual("StartButton",buttons[0].name);
                var surface=(RectTransform)buttons[0].transform;
                Assert.AreEqual(Vector2.zero,surface.anchorMin);Assert.AreEqual(Vector2.one,surface.anchorMax);

                var prompt=canvas.GetComponentsInChildren<Text>().Single(t=>t.name=="ContinuePrompt");
                StringAssert.Contains("to continue",prompt.text);

                var air=canvas.GetComponentInChildren<OwnerMenuAir>();
                Assert.IsNotNull(air,"Her clouds and her cast shadow are the menu's only motion in the air");
                var material=air.GetComponent<RawImage>().material;
                foreach(string texture in new[]{"_SkyMask","_Cloud","_Shadow"})
                    Assert.IsNotNull(material.GetTexture(texture),texture+" failed to load");

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("OwnerMenu-v8-"+size.x+"x"+size.y,canvas,size.x,size.y,false);
                Assert.AreEqual("menu",GameServices.Music.Current);

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return new WaitForSecondsRealtime(.3f);
                var dust=canvas.GetComponentInChildren<OwnerRoadDust>();
                var leaves=canvas.GetComponentInChildren<OwnerMenuLeaves>();
                yield return TumpUiCapture.Capture("OwnerMenu-v8-weather",canvas,1920,1080,false);
                Assert.Greater(dust.canvasRenderer.GetMesh().vertexCount,0);
                Assert.Greater(leaves.canvasRenderer.GetMesh().vertexCount,0,"Leaves fall off her tree");

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                Assert.AreEqual(0,dust.canvasRenderer.GetMesh().vertexCount,"Reduced motion must remove drifting dust");
                Assert.AreEqual(0,leaves.canvasRenderer.GetMesh().vertexCount,"Reduced motion must ground the leaves");

                buttons[0].onClick.Invoke();
                yield return new WaitForSecondsRealtime(.5f);
                // ⚠️ UX-1, 2026-09-23: the destination is HOME (the `MatchSetup` scene's hub) by the
                // owner's design; it was LET'S PLAY (`ModeSelect`) until then. Changed on purpose.
                Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name,
                    "A press anywhere on the street is the way in");
            }
            finally{SceneFlow.BootedThroughSplash=boot;SceneFlow.LoginStepOffered=offered;TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;}
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator TitlePlateSharpnessSeparatesSourceImportAndMaterial()
        {
            bool boot=SceneFlow.BootedThroughSplash;
            bool reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            RawImage plate=null;
            OwnerMenuAir air=null;
            OwnerMenuLeaves leaves=null;
            OwnerRoadDust dust=null;
            Text prompt=null;
            Texture originalTexture=null;
            Material originalMaterial=null;
            Texture2D source=null;
            bool airEnabled=false,leavesEnabled=false,dustEnabled=false,promptEnabled=false;
            try
            {
                SceneFlow.BootedThroughSplash=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                plate=canvas.GetComponentInChildren<HomeCourtScene>().GetComponent<RawImage>();
                air=plate.GetComponent<OwnerMenuAir>();
                leaves=canvas.GetComponentInChildren<OwnerMenuLeaves>();
                dust=canvas.GetComponentInChildren<OwnerRoadDust>();
                prompt=canvas.GetComponentsInChildren<Text>().Single(t=>t.name=="ContinuePrompt");
                Assert.IsNotNull(air);Assert.IsNotNull(leaves);Assert.IsNotNull(dust);
                originalTexture=plate.texture;originalMaterial=plate.material;
                airEnabled=air.enabled;leavesEnabled=leaves.enabled;
                dustEnabled=dust.enabled;promptEnabled=prompt.enabled;
                Assert.AreEqual("TumbangPreso/UI/OwnerMenuAir",originalMaterial.shader.name);

                var imported=OwnerMenuArt.Texture("main2-background");
                Assert.AreSame(imported,originalTexture);
                source=new Texture2D(2,2,TextureFormat.RGBA32,false,false);
                var path=System.IO.Path.Combine(Application.dataPath,"TumbangPreso","Resources", "UI",
                    "owner-menu-edits","main2-background.png");
                Assert.IsTrue(source.LoadImage(System.IO.File.ReadAllBytes(path)));
                source.filterMode=imported.filterMode;source.wrapMode=imported.wrapMode;
                source.anisoLevel=imported.anisoLevel;
                Assert.AreEqual(1920,source.width);Assert.AreEqual(1080,source.height);
                var logo=new RectInt(105,120,720,430);
                var can=new RectInt(1560,640,190,310);
                Debug.Log($"[QA01Title] fixed source-pixel top-left ROIs: logo={logo}, can={can}; " +
                    $"source={source.width}x{source.height}/{source.format}/{source.filterMode}, " +
                    $"imported={imported.width}x{imported.height}/{imported.format}/{imported.filterMode}, " +
                    $"mips={imported.mipmapCount}, colorSpace={QualitySettings.activeColorSpace}");

                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(3840,2160)})
                {
                    int factor=size.x/1920;
                    Debug.Log($"[QA01Title] {size.x}x{size.y} top-left edge ROIs: " +
                        $"logo=({logo.x*factor},{logo.y*factor},{logo.width*factor},{logo.height*factor}), " +
                        $"can=({can.x*factor},{can.y*factor},{can.width*factor},{can.height*factor})");
                    TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                    air.enabled=true;yield return null;yield return null;
                    air.enabled=false;leaves.enabled=false;dust.enabled=false;prompt.enabled=false;
                    void Inspect(string arm)
                    {
                        var uv=plate.uvRect;
                        Debug.Log($"[QA01Title] {arm} viewport={size.x}x{size.y} " +
                            $"texture={plate.texture.width}x{plate.texture.height} " +
                            $"material={plate.materialForRendering.shader.name} uv={uv} scale={canvas.scaleFactor}");
                        Assert.That(uv.x,Is.EqualTo(0f).Within(.002f));
                        Assert.That(uv.y,Is.EqualTo(0f).Within(.002f));
                        Assert.That(uv.width,Is.EqualTo(1f).Within(.002f));
                        Assert.That(uv.height,Is.EqualTo(1f).Within(.002f));
                    }

                    plate.texture=source;plate.material=null;
                    yield return TumpUiCapture.Capture($"QA01-source-decode-{size.x}x{size.y}",
                        canvas,size.x,size.y,false,inspectViewport:()=>Inspect("source-decode"));
                    plate.texture=imported;
                    yield return TumpUiCapture.Capture($"QA01-imported-ungraded-{size.x}x{size.y}",
                        canvas,size.x,size.y,false,inspectViewport:()=>Inspect("imported-ungraded"));
                    plate.material=originalMaterial;
                    yield return TumpUiCapture.Capture($"QA01-current-material-{size.x}x{size.y}",
                        canvas,size.x,size.y,false,inspectViewport:()=>Inspect("current-material"));

                    TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                    air.enabled=true;leaves.enabled=true;dust.enabled=true;prompt.enabled=true;
                    yield return null;
                    yield return TumpUiCapture.Capture($"QA01-live-overlay-{size.x}x{size.y}",
                        canvas,size.x,size.y,false,inspectViewport:()=>Inspect("live-overlay"));
                }

                // Capture restores the real canvas mode and viewport. Motion belongs to that
                // settled window, not to either offscreen image resolution.
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                int windowWidth=Screen.width,windowHeight=Screen.height;
                Vector2 canvasSize=((RectTransform)canvas.transform).rect.size;
                float canvasScale=canvas.scaleFactor;
                void CheckWindow()
                {
                    Assert.AreEqual(windowWidth,Screen.width,"The leaf sample changed window width.");
                    Assert.AreEqual(windowHeight,Screen.height,"The leaf sample changed window height.");
                    var size=((RectTransform)canvas.transform).rect.size;
                    Assert.That(size.x,Is.EqualTo(canvasSize.x).Within(.01f));
                    Assert.That(size.y,Is.EqualTo(canvasSize.y).Within(.01f));
                    Assert.That(canvas.scaleFactor,Is.EqualTo(canvasScale).Within(.0001f));
                }
                CheckWindow();
                float deadline=Time.realtimeSinceStartup+10f;
                Mesh firstMesh=leaves.canvasRenderer.GetMesh();
                while((firstMesh==null || firstMesh.vertexCount==0) && Time.realtimeSinceStartup<deadline)
                {yield return null;Canvas.ForceUpdateCanvases();CheckWindow();firstMesh=leaves.canvasRenderer.GetMesh();}
                Assert.IsNotNull(firstMesh,"Live title leaves had no mesh.");
                Assert.Greater(firstMesh.vertexCount,0,"Live title leaves had no visible vertices.");
                var first=firstMesh.vertices;
                float sampledAt=Time.realtimeSinceStartup;
                deadline=sampledAt+10f;
                bool moved=false;
                int secondCount=first.Length;
                while(!moved && Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForSecondsRealtime(.15f);
                    Canvas.ForceUpdateCanvases();CheckWindow();
                    var secondMesh=leaves.canvasRenderer.GetMesh();
                    var second=secondMesh!=null?secondMesh.vertices:System.Array.Empty<Vector3>();
                    secondCount=second.Length;
                    moved=first.Length!=second.Length;
                    for(int i=0;i<Mathf.Min(first.Length,second.Length) && !moved;i++)
                        moved=(first[i]-second[i]).sqrMagnitude>.0025f;
                }
                Assert.IsTrue(moved,"The live title leaves did not move during the bounded actual-window sample.");
                Debug.Log($"[QA01Title] actual-window leaves moved at {windowWidth}x{windowHeight}, " +
                    $"canvas={canvasSize}, scale={canvasScale:0.###}, " +
                    $"vertices={first.Length}->{secondCount}, interval={Time.realtimeSinceStartup-sampledAt:0.###}s");
            }
            finally
            {
                if(plate!=null){plate.texture=originalTexture;plate.material=originalMaterial;}
                if(air!=null)air.enabled=airEnabled;
                if(leaves!=null)leaves.enabled=leavesEnabled;
                if(dust!=null)dust.enabled=dustEnabled;
                if(prompt!=null)prompt.enabled=promptEnabled;
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;
                SceneFlow.BootedThroughSplash=boot;
                if(source!=null)Object.DestroyImmediate(source);
            }
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator StartupLoginStaysSilentUntilGuestRevealsHome()
        {
            bool boot=SceneFlow.BootedThroughSplash,offered=SceneFlow.LoginStepOffered;
            try
            {
                GameServices.Ensure();GameServices.Music.StopNow();
                SceneFlow.BootedThroughSplash=true;SceneFlow.LoginStepOffered=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;yield return null;
                var login=Object.FindFirstObjectByType<SignInScreen>();Assert.True(login.IsOpen);
                Assert.IsNull(GameServices.Music.Current,"Startup login must not start the menu bed");
                var canvas=Find("GuestAccount").GetComponentInParent<Canvas>();
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>().Any(b=>b.name=="BackButton"));
                Assert.IsEmpty(canvas.GetComponentsInChildren<Transform>().Where(t=>t.name=="FieldIcon" || t.name=="PersonIcon"),"Supplied fields already contain icons");
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540),new Vector2Int(1280,960),new Vector2Int(3440,1440)})
                    yield return TumpUiCapture.Capture("OwnerLogin-v7-create-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                var terms = canvas.GetComponentsInChildren<Toggle>().First(t => t.name == "TermsAcceptance");
                Assert.IsFalse(terms.isOn);
                Assert.Less(terms.graphic.canvasRenderer.GetAlpha(), .05f, "Unaccepted consent must look empty.");
                var consentMark = terms.graphic as OwnerUiGlyph;
                Assert.IsNotNull(consentMark, "Accepted consent must use the check mark.");
                Assert.AreEqual(OwnerUiGlyph.Mark.Check, consentMark.Shape);
                bool larger = Settings.SettingsStore.Current.LargerText;
                try
                {
                    foreach (bool large in new[] { false, true })
                    {
                        Settings.SettingsStore.Current.LargerText = large;
                        Find("TermsLink").onClick.Invoke(); yield return null;
                        Assert.IsFalse(terms.isOn, "Opening the terms is not acceptance.");
                        var document = GameObject.Find("OwnerTermsCanvas").GetComponent<Canvas>();
                        var scroll = document.GetComponentInChildren<ScrollRect>();
                        Canvas.ForceUpdateCanvases();
                        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 2, "The long document must actually scroll.");
                        foreach (var size in HubFlowTests.Shapes)
                            yield return TumpUiCapture.Capture("Terms-popup-" + (large ? "large-" : "normal-") + size.x + "x" + size.y,
                                document, size.x, size.y, false, checkActionBounds: true, underlays: new[] { canvas });
                        scroll.verticalNormalizedPosition = 0; yield return null; yield return null;
                        yield return TumpUiCapture.Capture("Terms-popup-end-" + (large ? "large" : "normal"),
                            document, 960, 540, false, checkActionBounds: true, underlays: new[] { canvas });
                        Find("TermsBack").onClick.Invoke(); yield return null;
                        Assert.IsFalse(terms.isOn, "BACK must leave consent unchanged.");
                    }
                }
                finally { Settings.SettingsStore.Current.LargerText = larger; }
                Find("TermsLink").onClick.Invoke(); yield return null;
                Find("AcceptGuidelines").onClick.Invoke(); yield return new WaitForSecondsRealtime(.2f);
                Assert.True(terms.isOn, "I AGREE marks the signup consent square.");
                Assert.Greater(terms.graphic.canvasRenderer.GetAlpha(), .95f, "The accepted check must be visible.");
                yield return TumpUiCapture.Capture("Login-consent-checked", canvas, 960, 540, false, checkActionBounds: true);
                var fields=canvas.GetComponentsInChildren<InputField>();
                Assert.IsFalse(fields.Any(f=>f.name=="Email"));
                var confirmation=fields.First(f=>f.name=="ConfirmPassword");
                fields.First(f=>f.name=="Username").text="local.validation";
                fields.First(f=>f.name=="Password").text="short";
                confirmation.text="short";Find("SubmitAccount").onClick.Invoke();yield return null;
                Assert.AreEqual("Use at least 8 characters.",
                    canvas.GetComponentsInChildren<Text>().First(t=>t.name=="PasswordFault").text);
                Assert.IsEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="AccountStatus").text);
                // ⚠️ A PASSWORD THAT PASSES THE REAL RULES, so this case still reaches the
                // confirmation check. `OwnerFieldFault` enforces UGS's own 8-to-30 with an
                // upper, a lower, a digit and a symbol, and the old fixture's "test-only"
                // would now be refused for its length before the two were ever compared.
                fields.First(f=>f.name=="Password").text="Test-only1";
                confirmation.text="Different-1";Find("SubmitAccount").onClick.Invoke();yield return null;
                // ⚠️⚠️ THE MISMATCH IS UNDER THE FIELD IT IS ABOUT NOW, NOT IN THE SHARED LINE.
                // She redrew the login with a red line under each input; `AccountStatus` keeps
                // only what is about the whole attempt. `SignInScreen.Fail` routes the rest.
                StringAssert.Contains("do not match",canvas.GetComponentsInChildren<Text>().First(t=>t.name=="ConfirmFault").text);
                Assert.IsEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="AccountStatus").text);
                confirmation.text=fields.First(f=>f.name=="Password").text;yield return null;yield return null;
                Assert.IsEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="ConfirmFault").text,
                    "Live validation must clear a fault the player has fixed");
                // ⚠ THE ONE MOMENT IN THE FLOW THAT PHOTOGRAPHS HER VALID MARK. All three
                // fields are good only here, and the mark fades in over `StateSeconds`, so a
                // capture on the same frame gets an empty seat. § 153.9 is judged off this shot.
                yield return new WaitForSecondsRealtime(.6f);
                yield return TumpUiCapture.Capture("OwnerLogin-v8-valid-marks",canvas,1920,1080,false);
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Standard,confirmation.contentType);
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Password,confirmation.contentType);
                var divider=canvas.GetComponentsInChildren<RectTransform>().First(t=>t.name=="AccountDivider");
                var left=(RectTransform)divider.Find("LeftDivider");var right=(RectTransform)divider.Find("RightDivider");
                // ⚠️⚠️ THE TWO STROKES ARE HER OWN PIXELS NOW, AND HERS ARE NOT IDENTICAL.
                // The September 15 pass drew both as generated images so it could assert they
                // matched exactly; she drew them by hand at 241x5 and 242x6. Resizing either one
                // to make this line pass would be squashing her art to satisfy a test, so the
                // assertion is what a player can actually see: a pair, level with each other.
                Assert.That(Mathf.Abs(left.sizeDelta.x-right.sizeDelta.x),Is.LessThanOrEqualTo(2f));
                Assert.That(Mathf.Abs(left.sizeDelta.y-right.sizeDelta.y),Is.LessThanOrEqualTo(2f));
                Assert.That(Mathf.Abs((left.anchoredPosition.y-left.sizeDelta.y*.5f)
                                     -(right.anchoredPosition.y-right.sizeDelta.y*.5f)),Is.LessThanOrEqualTo(1f),
                    "The two strokes must share an optical centre line either side of OR");
                var status = canvas.GetComponentsInChildren<Text>().First(t=>t.name=="AccountStatus");
                var passwordFault = canvas.GetComponentsInChildren<Text>().First(t=>t.name=="PasswordFault");
                status.text = "Creating your account...";
                var fail = typeof(SignInScreen).GetMethod("Fail",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(fail);
                fail.Invoke(login, new object[] { "Password provider: internal check failed." });
                Assert.AreEqual("Password not accepted. Try a different one.", passwordFault.text);
                Assert.IsEmpty(status.text, "A failed password must not leave the account appearing busy");
                yield return new WaitForSecondsRealtime(.25f);
                Canvas.ForceUpdateCanvases();
                Assert.Greater(passwordFault.color.a, .95f,"The password fault did not finish fading in.");
                Assert.Greater(passwordFault.canvasRenderer.GetInheritedAlpha(), .95f);
                Assert.GreaterOrEqual(passwordFault.cachedTextGenerator.characterCountVisible,
                    passwordFault.text.Length,"The password fault lost visible characters.");
                Assert.Greater(passwordFault.canvasRenderer.GetMesh()?.vertexCount ?? 0, 0,
                    "The password fault has no rendered glyphs.");
                Assert.LessOrEqual(passwordFault.preferredWidth,passwordFault.rectTransform.rect.width + 1,
                    "The password fault leaves its available line width.");
                yield return TumpUiCapture.Capture("OwnerLogin-password-fault", canvas, 960, 540, false);
                fields.First(f=>f.name=="Username").text="";fields.First(f=>f.name=="Password").text="";
                confirmation.text="";
                Find("SignInTab").onClick.Invoke();yield return null;
                // ⚠️ HER SIGN-IN SHEET RENAMES THE FIRST FIELD RATHER THAN ADDING ONE.
                Assert.AreEqual("TUMP ID",((Text)fields.First(f=>f.name=="Username").placeholder).text);
                yield return TumpUiCapture.Capture("OwnerLogin-v7-signin",canvas,1920,1080,false,checkActionBounds:true);
                Assert.IsNull(GameServices.Music.Current);
                var password=canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="Password");
                password.text="local-test-only";Find("RevealPassword").onClick.Invoke();
                Assert.AreEqual(InputField.ContentType.Standard,password.contentType);
                Find("RevealPassword").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Password,password.contentType);
                Assert.IsFalse(confirmation.gameObject.activeInHierarchy);
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>().Any(b=>b.name=="GuestAccount"));
                Find("CreateAccountTab").onClick.Invoke();yield return null;
                Find("GuestAccount").onClick.Invoke();yield return null;yield return null;
                Assert.False(login.IsOpen);Assert.AreEqual("menu",GameServices.Music.Current);
                var sources=GameServices.Music.GetComponents<AudioSource>();
                Assert.AreEqual(1,sources.Count(s=>s.isPlaying),"One menu track should play after entering home");
                var playing=sources.First(s=>s.isPlaying);float position=playing.time;
                var home=Object.FindFirstObjectByType<TumpHomeView>();home.Suspend();yield return null;home.Resume();yield return null;
                Assert.AreEqual(1,sources.Count(s=>s.isPlaying));Assert.GreaterOrEqual(playing.time,position);
            }
            finally{SceneFlow.BootedThroughSplash=boot;SceneFlow.LoginStepOffered=offered;}
        }

        private static Button Find(string name)=>Object.FindObjectsByType<Button>().First(b=>b.name==name && b.isActiveAndEnabled);
    }
}
