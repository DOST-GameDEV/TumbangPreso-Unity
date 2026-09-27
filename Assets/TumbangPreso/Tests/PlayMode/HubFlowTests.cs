using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// UX-1: HOME and every door on it, the owner's flow walked press by press, with captures of
    /// each screen at the brief's five shapes. `docs/TODO.md` UX-1.1 to UX-1.11.
    ///
    /// ⚠️ EVERY DOOR IS PRESSED THROUGH ITS BUTTON, NOT BY PUSHING A SCREEN, so a door whose listener
    /// is missing fails here (`docs/TODO.md` § 108: an EQUIP with no `onClick` that looked perfect).
    /// ⚠️ AND EVERY SCREEN IS LEFT THROUGH `TumpHub.Back`, which is what Escape, pad B and Android
    /// BACK reach through `ConvertedMatchSetup.Cancel`, so a screen that traps a pad fails here.
    /// </summary>
    public sealed class HubFlowTests
    {
        private bool _contrast, _larger;
        [UnitySetUp] public IEnumerator Before()
        {
            _contrast = Settings.SettingsStore.Current.HighContrastHud;
            _larger = Settings.SettingsStore.Current.LargerText;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            // A nested UnityTest enumerator can fail before its parent finally is disposed.
            var settings = Settings.SettingsStore.Current;
            settings.HighContrastHud = _contrast; settings.LargerText = _larger;
            Settings.SettingsStore.Save();
            Net.NetSession.Instance?.Stop(); HubQueueWatch.End(); SceneFlow.Networked = false;
            yield return PlayModeWorld.Reset();
        }

        /// <summary>The brief's shapes: 960x540, 1280x720, 1920x1080, 4:3 and the owner's window.</summary>
        internal static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
            new Vector2Int(1280, 960), new Vector2Int(1600, 680),
        };

        internal static IEnumerator OpenHome()
        {
            // This helper means a fresh HOME. Room-preserving return is tested separately.
            Net.NetSession.Instance?.Stop(); HubQueueWatch.End();
            yield return new WaitForSecondsRealtime(.4f);
            SceneFlow.Networked = false;
            SceneFlow.GoHome();
            float until = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < until && (TumpHub.Current == null || !(TumpHub.Current.Top is HubHome))) yield return null;
            Assert.IsNotNull(TumpHub.Current, "TAP TO START's destination did not build the hub.");
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            // Let the court's first frame land and the stickers settle.
            until = Time.realtimeSinceStartup + 12;
            var preview = TumpHub.Current.Host.Preview;
            while (Time.realtimeSinceStartup < until && preview != null && preview.GetComponent<RawImage>().texture == null) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
        }

        internal static IEnumerator Press(string name)
        {
            var hub = TumpHub.Current;
            Button button = null;
            float until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until)
            {
                button = hub.Canvas.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name && b.IsInteractable());
                if (button != null) break;
                yield return null;
            }
            Assert.IsNotNull(button, "No pressable '" + name + "' on " + hub.Top?.GetType().Name);
            button.onClick.Invoke();
            yield return null;
            yield return new WaitForSecondsRealtime(0.45f);
        }

        internal static IEnumerator Shots(string name)
        {
            var hub = TumpHub.Current;
            var settings = Settings.SettingsStore.Current;
            string prefix = settings.HighContrastHud && settings.LargerText ? "Hub-A11y-" : "Hub-";
            foreach (var size in Shapes)
                yield return TumpUiCapture.Capture(prefix + name + "-" + size.x + "x" + size.y, hub.Canvas, size.x, size.y,
                                                   checkPalette: false, checkActionBounds: true);
            AssertFloor(hub.Canvas, name);
        }

        /// <summary>⚠️ THE BRIEF'S FLOOR: every label on a hub screen is 28 canvas units or more.</summary>
        internal static void AssertFloor(Canvas canvas, string where)
        {
            foreach (var text in canvas.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrWhiteSpace(text.text)) continue;
                if (text.GetComponentInParent<LobbyChat>() != null) continue;   // the chat is its own surface
                Assert.GreaterOrEqual(text.fontSize, HubStyle.Floor, where + "/" + text.name + " is under the 28-unit floor.");
            }
        }

        private static void Back()
        {
            TumpHub.Current.Back();
        }

        private static IEnumerator BackToHome()
        {
            for (int i = 0; i < 6 && !(TumpHub.Current.Top is HubHome); i++) { Back(); yield return new WaitForSecondsRealtime(0.2f); }
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top, "BACK did not lead home.");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PaeteAndSeanKeepTheirRelativeSizeInShopAndCharacterSelect()
        {
            var settings = Settings.SettingsStore.Current;
            int picked = settings.CharacterPick, can = settings.CanPick, slipper = settings.SlipperPick;
            var mode = SceneFlow.SelectedMode;
            string map = SceneFlow.SelectedMap;
            bool networked = SceneFlow.Networked, larger = settings.LargerText;
            var sizes = new[] { TumpUiCapture.OwnerWindow, new Vector2Int(2340, 1080) };
            var shop = new HeroFrame[3, 2];
            var selector = new HeroFrame[3, 2];
            var roleFits = new List<RoleCaptionFit>();
            var ids = new[] { "sean", "cheska", "paete" };
            try
            {
                settings.LargerText = false;
                yield return OpenHome();
                yield return Press("ShopButton");
                yield return Press("HeroShopDoor");
                var hero = TumpHub.Current.Top as HubHero;
                Assert.IsNotNull(hero,"The shop's HEROES door did not open the hero stage.");
                Assert.IsTrue(hero.ShopMode);
                for (int h = 0; h < ids.Length; h++)
                {
                    string target = Roster.HeroPeople.First(p => p.Id == ids[h]).Name;
                    bool found = false;
                    for (int step = 0; step < Roster.HeroPeople.Count; step++)
                    {
                        var heading = hero.GetComponentsInChildren<Text>().Single(t => t.name == "Heading").text;
                        if (heading == target) { found = true; break; }
                        yield return Press("NextHero");
                    }
                    Assert.IsTrue(found,"The shop hero arrows never reached " + target);
                    hero.GetComponentsInChildren<HubButton>().First(b => b.name == "Ability1").Select();
                    yield return new WaitForSecondsRealtime(.3f);
                    for (int s = 0; s < sizes.Length; s++)
                    {
                        int heroIndex = h, sizeIndex = s;
                        var size = sizes[s];
                        yield return TumpUiCapture.Capture("Hub-HeroShop-"+ids[h]+"-"+size.x+"x"+size.y,
                            TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true,
                            inspectViewport:()=>
                            {
                                shop[heroIndex,sizeIndex]=DrawnHeroFrame(hero);
                                roleFits.Add(MeasureRoleCaptions(hero,ids[heroIndex]+"/"+size.x+"x"+size.y+"/attacking"));
                            });
                    }
                }

                hero.GetComponentsInChildren<HubButton>().First(b => b.name == "Ability2").Select();
                yield return new WaitForSecondsRealtime(.3f);
                yield return TumpUiCapture.Capture("Hub-HeroShop-paete-defending-focus-1600x680",
                    TumpHub.Current.Canvas,1600,680,false,checkActionBounds:true,
                    inspectViewport:()=>roleFits.Add(MeasureRoleCaptions(hero,"paete/defending-focus")));
                var tilted = hero.GetComponentsInChildren<HubButton>()
                    .Where(b=>b.name.StartsWith("Ability")).OrderBy(b=>b.name).ToArray();
                var rotations = tilted.Select(b=>b.transform.localRotation).ToArray();
                try
                {
                    for(int i=0;i<tilted.Length;i++)
                        tilted[i].transform.localRotation=Quaternion.Euler(0,0,2-i*2);
                    yield return TumpUiCapture.Capture("Hub-HeroShop-paete-tilted-focus-1600x680",
                        TumpHub.Current.Canvas,1600,680,false,checkActionBounds:true,
                        inspectViewport:()=>roleFits.Add(MeasureRoleCaptions(hero,"paete/tilted-defending-focus")));
                }
                finally
                {
                    for(int i=0;i<tilted.Length;i++)
                        if(tilted[i]!=null)tilted[i].transform.localRotation=rotations[i];
                }
                settings.LargerText = true;
                yield return Press("NextHero");
                yield return Press("PreviousHero");
                hero.GetComponentsInChildren<HubButton>().First(b => b.name == "Ability2").Select();
                yield return new WaitForSecondsRealtime(.3f);
                foreach (var size in sizes)
                    yield return TumpUiCapture.Capture("Hub-HeroShop-paete-larger-"+size.x+"x"+size.y,
                        TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true,
                        inspectViewport:()=>roleFits.Add(MeasureRoleCaptions(hero,"paete/larger/"+size.x+"x"+size.y)));
                settings.LargerText = false;

                yield return BackToHome();
                var hosting = TumpHub.Current.Host.HostRoom("PREVIEW SIZE",SceneFlow.Eskinita,
                    GameMode.HeroStrike,RoomVisibility.Public,false);
                float until = Time.realtimeSinceStartup + 15;
                while (!hosting.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(hosting.IsCompleted && string.IsNullOrEmpty(hosting.Result),
                    "Local LAN preview room did not open.");
                until = Time.realtimeSinceStartup + 5;
                while (!(TumpHub.Current.Top is HubLobby) && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
                yield return Press("CharacterDoor");
                var picker = TumpHub.Current.Top as HubCharacterSelect;
                Assert.IsNotNull(picker);
                var people = Roster.GetPeople(GameMode.HeroStrike);
                for (int h = 0; h < ids.Length; h++)
                {
                    int index = -1;
                    for (int i = 0; i < people.Count; i++) if (people[i].Id == ids[h]) { index = i; break; }
                    Assert.GreaterOrEqual(index,0,"The current hero picker has no " + ids[h]);
                    yield return Press("Portrait"+index);
                    for (int s = 0; s < sizes.Length; s++)
                    {
                        int heroIndex = h, sizeIndex = s;
                        var size = sizes[s];
                        yield return TumpUiCapture.Capture("Hub-CharacterSelect-"+ids[h]+"-"+size.x+"x"+size.y,
                            TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true,
                            inspectViewport:()=>selector[heroIndex,sizeIndex]=DrawnHeroFrame(picker));
                    }
                }

                Assert.AreEqual(10,roleFits.Count,"The shop viewports, focus/tilt states and LargerText captures did not all report role bounds.");
                foreach (var fit in roleFits)
                {
                    Assert.IsTrue(fit.Present,fit.Context+" is missing a role caption.");
                    Assert.GreaterOrEqual(fit.SmallestFont,HubStyle.Floor,fit.Context+" dropped below the type floor.");
                    Assert.LessOrEqual(fit.WidthOverflow,1,fit.Context+" text overflows its own rect.");
                    Assert.LessOrEqual(fit.HeightOverflow,1,fit.Context+" text is vertically clipped.");
                    Assert.LessOrEqual(fit.TileOverflow,1,fit.Context+" tilted/hovered text leaves its tile.");
                    Assert.LessOrEqual(fit.RowOverflow,1,fit.Context+" text leaves the ability row.");
                    Assert.GreaterOrEqual(fit.NeighbourGap,0,fit.Context+" role captions overlap each other.");
                    Assert.LessOrEqual(fit.OuterHorizontalOverflow,1,fit.Context+" a tilted tile leaves the right-column width.");
                    Assert.GreaterOrEqual(fit.TileGap,0,fit.Context+" adjacent tilted tiles overlap.");
                }
                for (int s = 0; s < sizes.Length; s++)
                {
                    for (int h = 0; h < ids.Length; h++)
                    {
                        AssertHeroFrame(shop[h,s],"shop/"+ids[h]+"/"+sizes[s]);
                        AssertHeroFrame(selector[h,s],"selector/"+ids[h]+"/"+sizes[s]);
                    }
                    Debug.Log($"[HeroSize] {sizes[s].x}x{sizes[s].y}: shop Sean={shop[0,s].Share:F3}, Cheska={shop[1,s].Share:F3}, Paete={shop[2,s].Share:F3}; selector Sean={selector[0,s].Share:F3}, Cheska={selector[1,s].Share:F3}, Paete={selector[2,s].Share:F3}");
                    Assert.Greater(shop[2,s].Share,shop[0,s].Share,"Paete should read larger than Sean on the actual shop stage.");
                    Assert.Greater(selector[2,s].Share,selector[0,s].Share,"Paete should read larger than Sean on character select.");
                    Assert.LessOrEqual(Mathf.Abs(shop[2,s].Share-shop[1,s].Share)/shop[1,s].Share,.1f,
                        "Paete shop height differs from Cheska by over 10%.");
                    Assert.LessOrEqual(Mathf.Abs(selector[2,s].Share-selector[1,s].Share)/selector[1,s].Share,.1f,
                        "Paete selector height differs from Cheska by over 10%.");
                }
            }
            finally
            {
                if (TumpHub.Current != null) TumpHub.Current.Home();
                Net.NetSession.Instance?.Stop();
                SceneFlow.Networked=networked;
                SceneFlow.SelectedMode=mode;SceneFlow.SelectedMap=map;
                settings.CharacterPick=picked;settings.CanPick=can;settings.SlipperPick=slipper;
                settings.LargerText=larger;
                Settings.SettingsStore.Save();
            }
        }

        private struct HeroFrame
        {
            public bool HasTarget;
            public int Bottom, Top, Height;
            public float Share;
        }

        private static void AssertHeroFrame(HeroFrame frame, string where)
        {
            Assert.IsTrue(frame.HasTarget,where+" has no rendered hero texture.");
            Assert.Greater(frame.Bottom,2,where+" clips or misses the feet.");
            Assert.Less(frame.Top,frame.Height-3,where+" clips the head.");
        }

        private static HeroFrame DrawnHeroFrame(HubScreen screen)
        {
            var target = screen?.GetComponentInChildren<ModelPreview>()?.Target;
            if (target == null) return default;
            var before = RenderTexture.active;
            var pixels = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try
            {
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                var colors=pixels.GetPixels32();
                int bottom=-1,top=-1;
                for(int y=0;y<target.height;y++)
                    for(int x=0;x<target.width;x++)
                        if(colors[y*target.width+x].a>40){if(bottom<0)bottom=y;top=y;break;}
                return new HeroFrame { HasTarget=true, Bottom=bottom, Top=top, Height=target.height,
                    Share=bottom<0?0:(top-bottom+1)/(float)target.height };
            }
            finally { RenderTexture.active=before;Object.Destroy(pixels); }
        }

        private struct RoleCaptionFit
        {
            public string Context;
            public bool Present;
            public int SmallestFont;
            public float WidthOverflow, HeightOverflow, TileOverflow, RowOverflow, NeighbourGap;
            public float OuterHorizontalOverflow, TileGap;
        }

        private static float Outside(Rect rect, Vector2 point)
            => Mathf.Max(0,rect.xMin-point.x,point.x-rect.xMax,rect.yMin-point.y,point.y-rect.yMax);

        private static RoleCaptionFit MeasureRoleCaptions(HubHero hero, string context)
        {
            var result = new RoleCaptionFit { Context=context, SmallestFont=int.MaxValue };
            var row = hero.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="Abilities");
            var buttons = new HubButton[2];
            var captions = new Text[2];
            for (int i=0;i<2;i++)
            {
                string name="Ability"+(i+1);
                buttons[i]=hero.GetComponentsInChildren<HubButton>().FirstOrDefault(b=>b.name==name);
                captions[i]=buttons[i]?.Body?.Find("Key")?.GetComponent<Text>();
            }
            if(row==null || captions[0]==null || captions[1]==null)return result;
            result.Present=captions[0].text=="ATTACKING" && captions[1].text=="DEFENDING";
            var edges=new float[2,2];
            var corners=new Vector3[4];
            for(int i=0;i<2;i++)
            {
                var label=captions[i];var tile=(RectTransform)buttons[i].transform;
                result.SmallestFont=Mathf.Min(result.SmallestFont,label.fontSize);
                result.WidthOverflow=Mathf.Max(result.WidthOverflow,label.preferredWidth-label.rectTransform.rect.width);
                result.HeightOverflow=Mathf.Max(result.HeightOverflow,label.preferredHeight-label.rectTransform.rect.height);
                label.rectTransform.GetWorldCorners(corners);
                edges[i,0]=float.PositiveInfinity;edges[i,1]=float.NegativeInfinity;
                foreach(var corner in corners)
                {
                    Vector2 inTile=tile.InverseTransformPoint(corner);
                    Vector2 inRow=row.InverseTransformPoint(corner);
                    result.TileOverflow=Mathf.Max(result.TileOverflow,Outside(tile.rect,inTile));
                    result.RowOverflow=Mathf.Max(result.RowOverflow,Outside(row.rect,inRow));
                    edges[i,0]=Mathf.Min(edges[i,0],inRow.x);
                    edges[i,1]=Mathf.Max(edges[i,1],inRow.x);
                }
            }
            result.NeighbourGap=edges[1,0]-edges[0,1];
            result.TileGap=float.PositiveInfinity;
            float previousRight=float.NegativeInfinity;
            for(int i=0;i<4;i++)
            {
                var button=hero.GetComponentsInChildren<HubButton>().FirstOrDefault(b=>b.name=="Ability"+i);
                if(button==null){result.Present=false;return result;}
                ((RectTransform)button.transform).GetWorldCorners(corners);
                float left=float.PositiveInfinity,right=float.NegativeInfinity;
                foreach(var corner in corners)
                {
                    Vector2 local=row.InverseTransformPoint(corner);
                    result.OuterHorizontalOverflow=Mathf.Max(result.OuterHorizontalOverflow,
                        row.rect.xMin-local.x,local.x-row.rect.xMax);
                    left=Mathf.Min(left,local.x);right=Mathf.Max(right,local.x);
                }
                if(i>0)result.TileGap=Mathf.Min(result.TileGap,left-previousRight);
                previousRight=right;
            }
            return result;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PracticePickerBackAndBothChoicesUseTheirOfflineRoutes()
        {
            var settings = Settings.SettingsStore.Current;
            int difficulty = settings.AiDifficulty, format = settings.MatchFormat;
            string rulesWire = settings.CustomRulesWire;
            var rules = CustomGameRules.Parse(CustomGameRules.ToWire(SceneFlow.SelectedRules), SceneFlow.SelectedMode);
            rules.Password = SceneFlow.SelectedRules.Password;
            bool pinned = SceneFlow.RulesPinned, guided = GameLaunch.GuidedTutorial;
            bool spectator = GameLaunch.Spectator, bots = AIController.BotsEnabled;
            string sceneMap = SceneFlow.SelectedMap, launchMap = GameLaunch.SelectedMap;
            try
            {
                yield return OpenHome();
                yield return Press("ModeCard");
                yield return Press("PracticeCard");
                Assert.IsInstanceOf<HubPracticePopup>(TumpHub.Current.Top);
                Assert.AreEqual("TutorialChoice", TumpHub.Current.Top.FirstFocus?.name);
                Assert.IsNotNull(TumpHub.Current.Top.GetComponentsInChildren<Button>()
                    .FirstOrDefault(b => b.name == "TrainingChoice"));
                yield return Shots("PracticePopup");

                Back(); yield return null;
                Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top,
                    "BACK from the picker must leave GAMEMODE SELECT in place.");
                Assert.IsFalse(SceneFlow.InMatch, "BACK from the picker started a match.");

                yield return Press("PracticeCard");
                yield return Press("TutorialChoice");
                float until = Time.realtimeSinceStartup + 30;
                while (Object.FindFirstObjectByType<GuidedTraining>() == null && Time.realtimeSinceStartup < until)
                    yield return null;
                Assert.IsNotNull(Object.FindFirstObjectByType<GuidedTraining>(),
                    "TUTORIAL did not enter the existing guided route.");
                Assert.IsFalse(SceneFlow.Networked);
                Assert.IsFalse(NetAuthority.IsNetworked);

                SceneFlow.LeaveMatchToMainMenu();
                until = Time.realtimeSinceStartup + 30;
                while ((TumpHub.Current == null || !(TumpHub.Current.Top is HubHome)) &&
                       Time.realtimeSinceStartup < until) yield return null;
                Assert.IsNotNull(TumpHub.Current);
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);

                yield return Press("ModeCard");
                yield return Press("PracticeCard");
                yield return Press("TrainingChoice");
                until = Time.realtimeSinceStartup + 30;
                while ((!SceneFlow.InMatch || !PracticeRange.Active) &&
                       Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(SceneFlow.InMatch, "TRAINING did not enter the offline range.");
                Assert.IsFalse(GameLaunch.GuidedTutorial, "TRAINING installed guided lessons.");
                Assert.IsNull(Object.FindFirstObjectByType<GuidedTraining>());
                Assert.IsTrue(PracticeRange.Active, "TRAINING did not install its range controls.");
                Assert.AreEqual(1, GameServices.Round.Players.Count, "Training should start without active target bots.");
                Assert.IsFalse(SceneFlow.Networked);
                Assert.IsFalse(NetAuthority.IsNetworked);

                SceneFlow.LeaveMatchToMainMenu();
                until = Time.realtimeSinceStartup + 30;
                while ((TumpHub.Current == null || !(TumpHub.Current.Top is HubHome)) &&
                       Time.realtimeSinceStartup < until) yield return null;
                Assert.IsNotNull(TumpHub.Current);
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            }
            finally
            {
                if (SceneFlow.InMatch) SceneFlow.LeaveMatchToMainMenu();
                GameLaunch.GuidedTutorial = guided;
                GameLaunch.Spectator = spectator;
                SceneFlow.SelectedMap = sceneMap;
                GameLaunch.SelectedMap = launchMap;
                SceneFlow.AdoptRemoteRules(rules);
                if (pinned) SceneFlow.PinSelectedRules(rules); else SceneFlow.UnpinSelectedRules();
                SceneFlow.SelectedRules.Password = rules.Password;
                settings.CustomRulesWire = rulesWire;
                settings.MatchFormat = format;
                settings.AiDifficulty = difficulty;
                AIController.ApplyDifficulty(difficulty);
                Settings.SettingsStore.Save();
                AIController.BotsEnabled = bots;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator HomeAndEveryDoorOpensItsScreenAndBackReturns()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                yield return Shots("Home");

                yield return Press("AvatarButton");
                Assert.IsInstanceOf<HubAvatar>(TumpHub.Current.Top);
                yield return Shots("Avatar");
                yield return BackToHome();

                // The skill tree is switched off (owner, 2026-09-26, `HeroLoadoutRules.SidegradesOpen`),
                // and its HOME door is built but hidden, so the door is only walked while it is on.
                if (HeroLoadoutRules.SidegradesOpen)
                {
                    yield return Press("SkillTreeButton");
                    Assert.IsInstanceOf<HubSkillTree>(TumpHub.Current.Top);
                    yield return Shots("SkillTree");
                    yield return BackToHome();
                }
                else
                    Assert.IsFalse(TumpHub.Current.Canvas.GetComponentsInChildren<Button>().Any(b => b.name == "SkillTreeButton"),
                                   "The skill tree is off, so HOME must not show its door.");

                yield return Press("HeroButton");
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shots("Hero");
                yield return Press("Ability0");
                Assert.IsInstanceOf<HubAbilityPopup>(TumpHub.Current.Top);
                yield return Shots("AbilityDetail");
                Back(); yield return null;
                yield return Press("NextHero");
                yield return BackToHome();

                yield return Press("LoadoutButton");
                Assert.IsInstanceOf<HubLoadout>(TumpHub.Current.Top);
                yield return Shots("Loadout");
                yield return Press("UnownedTab");
                yield return Shots("Loadout-unowned");
                yield return Press("OwnedTab");
                yield return Press("LataTab");
                var tile = TumpHub.Current.Canvas.GetComponentsInChildren<Button>().First(b => b.name.StartsWith("Item_"));
                tile.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.8f);
                Assert.IsInstanceOf<HubItemPopup>(TumpHub.Current.Top);
                yield return Shots("ItemPopup");
                yield return Press("InspectButton");
                yield return Shots("ItemPopup-inspect");
                Back(); yield return null;   // leaves inspect first, innermost layer
                Assert.IsInstanceOf<HubItemPopup>(TumpHub.Current.Top, "BACK must leave inspect before the popup.");
                yield return BackToHome();

                yield return Press("ShopButton");
                Assert.IsInstanceOf<HubShopPopup>(TumpHub.Current.Top);
                yield return Shots("Shop");
                yield return Press("HeroShopDoor");
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
                yield return BackToHome();

                yield return Press("TaskButton");
                Assert.IsInstanceOf<HubTasks>(TumpHub.Current.Top);
                yield return Shots("Tasks");
                yield return BackToHome();

                yield return Press("EarnButton");
                Assert.IsInstanceOf<HubTasks>(TumpHub.Current.Top, "The + beside the balance opens TASKS, never a store of currency.");
                yield return BackToHome();

                yield return Press("MenuButton");
                Assert.IsInstanceOf<HubMenu>(TumpHub.Current.Top);
                yield return null;
                // BUGS-0926.4: the MENU is a popup over HOME, so HOME's own background stays up under it.
                var video = Object.FindFirstObjectByType<HubSceneVideo>();
                Assert.IsTrue(video == null || video.GetComponent<RawImage>().enabled,
                              "Opening the MENU hid HOME's background and showed the live court instead.");
                yield return Shots("Menu");
                yield return BackToHome();

                // BUGS-0926.2: BACK on HOME opens the MENU and never drops the player on the title.
                Back(); yield return new WaitForSecondsRealtime(0.2f);
                Assert.IsInstanceOf<HubMenu>(TumpHub.Current.Top, "BACK on HOME must open the MENU.");
                Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name, "BACK on HOME left the hub.");
                Back(); yield return new WaitForSecondsRealtime(0.2f);
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top, "A second BACK closes the MENU.");

                yield return Press("NamePlate");
                Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen, "The name plate is the door to profile settings.");
                Assert.IsFalse(TumpHub.Current.Canvas.enabled, "The hub steps aside for the profile screen.");
                yield return Press_Global("ClosePlayerHub");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(TumpHub.Current.Canvas.enabled);
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
            }
        }

        private static IEnumerator Press_Global(string name)
        {
            Button button = null;
            float until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until && button == null)
            {
                button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
                yield return null;
            }
            Assert.IsNotNull(button, "No " + name + " on screen.");
            button.onClick.Invoke();
        }

        /// <summary>
        /// ⚠️ LIGHT-5 (2026-09-27): the map select was washed out because the hub's two preview
        /// surfaces both loaded the selected map and one copy was never claimed, so its sun lit
        /// every map previewed afterwards. Exactly one directional light may reach the preview
        /// layer, and it must be the shown map's own.
        /// </summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator HostGameMapPreviewIsLitByTheShownMapsSunAlone()
        {
            yield return OpenHome();
            TumpHub.Current.Push<HubHost>();
            var preview = TumpHub.Current.Host.Preview;
            Assert.IsNotNull(preview);
            foreach (string map in new[] { SceneFlow.IlalimNgTulay, SceneFlow.Lagoon })
            {
                TumpHub.Current.Host.SelectMap(map);
                float until = Time.realtimeSinceStartup + 40;
                while (preview.Showing != map && Time.realtimeSinceStartup < until) yield return null;
                Assert.AreEqual(map, preview.Showing);
                yield return new WaitForSecondsRealtime(0.5f);
                var suns = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                    .Where(l => l.isActiveAndEnabled && l.type == LightType.Directional
                                && (l.cullingMask & (1 << MapPreviewSurface.PreviewLayer)) != 0).ToArray();
                Assert.AreEqual(1, suns.Length, map + " preview is lit by: " +
                    string.Join(", ", suns.Select(l => l.name + "@" + l.gameObject.scene.name + " layer " + l.gameObject.layer)));
                Assert.AreEqual(map, suns[0].gameObject.scene.name);
                Assert.AreEqual(MapPreviewSurface.PreviewLayer, suns[0].gameObject.layer, "The lighting scene was never confined.");
            }
            TumpHub.Current.Home();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator GamemodeSelectSetsTheModeCardAndOpensTheCustomFlow()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                yield return Press("ModeCard");
                Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top);
                foreach (string name in new[] { "PracticeCard", "CustomCard", "ClassicCard", "RankedCard" })
                {
                    var card = TumpHub.Current.Top.GetComponentsInChildren<HubButton>()
                        .Single(b => b.name == name);
                    var art = card.Body.Find("Art");
                    Assert.IsNotNull(art, name + " has no poster region");
                    var mask = art.GetComponent<Mask>();
                    var stencil = art.GetComponent<HubShape>();
                    Assert.IsNotNull(mask, name + " has no chamfered art mask");
                    Assert.IsNotNull(stencil, name + " has no shaped stencil");
                    Assert.IsFalse(mask.showMaskGraphic);
                    Assert.IsFalse(stencil.raycastTarget);
                    Assert.AreEqual(0, stencil.OutlineWidth);
                    Assert.AreEqual(0, stencil.RimWidth);
                    Assert.AreEqual(0, stencil.RingWidth);
                    Assert.AreEqual(Vector2.zero, stencil.ShadowOffset);
                    Assert.AreEqual(card.Shape.Seed, stencil.Seed);
                    Assert.IsNotNull(art.Find("Poster"));
                    Assert.IsFalse(card.Body.Find("Label").IsChildOf(art),
                        name + " must keep its title above the poster mask");
                    Assert.IsFalse(card.transform.Find("Tape").IsChildOf(art),
                        name + " must leave the tape outside the poster mask");
                }
                yield return Shots("GameModes");

                // Hover and focus reveal a card's description (the owner's darkened CLASSIC card).
                var classic = TumpHub.Current.Canvas.GetComponentsInChildren<HubButton>().First(b => b.name == "ClassicCard");
                classic.Select();
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(classic.transform.GetComponentsInChildren<Text>().Any(t => t.name == "Description" && t.isActiveAndEnabled));
                yield return Shots("GameModes-focus");

                // RANKED is always Hero Strike and returns HOME.
                yield return Press("RankedCard");
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.AreEqual(0, HubHome.Choice);
                Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode);

                // CLASSIC asks which game, then returns HOME with that on the card.
                yield return Press("ModeCard");
                yield return Press("ClassicCard");
                Assert.IsInstanceOf<HubClassicPopup>(TumpHub.Current.Top);
                yield return Shots("ClassicPopup");
                yield return Press("ClassicChoice");
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.AreEqual(1, HubHome.Choice);
                Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
                Assert.IsTrue(TumpHub.Current.Canvas.GetComponentsInChildren<Text>().Any(t => t.name == "ModeSubtitle" && t.text == "CLASSIC"));

                // CUSTOM is a popup with HOST and JOIN, and each leads to its screen.
                yield return Press("ModeCard");
                yield return Press("CustomCard");
                Assert.IsInstanceOf<HubCustomPopup>(TumpHub.Current.Top);
                yield return Shots("CustomPopup");
                yield return Press("HostDoor");
                Assert.IsInstanceOf<HubHost>(TumpHub.Current.Top);
                yield return Shots("Host");
                yield return Press("MapDropdown");
                Assert.IsInstanceOf<HubChoicePopup>(TumpHub.Current.Top);
                yield return Shots("Host-map-list");
                yield return Press("Option1");
                Assert.AreEqual(SceneFlow.MapRegistry[1].Id, SceneFlow.SelectedMap, "The map choice moves the court behind the form.");
                Back(); yield return null;
                Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top, "BACK from HOST returns to GAMEMODE SELECT.");
                yield return Press("CustomCard");
                yield return Press("JoinDoor");
                Assert.IsInstanceOf<HubJoin>(TumpHub.Current.Top);
                yield return Shots("Join-online");
                yield return Press("Source1");
                yield return Shots("Join-lan");
                yield return Press("Source2");
                yield return Shots("Join-code");
                yield return BackToHome();
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                HubHome.Choice = 2;
                yield return Press("PlayButton");

                // Offline the relay host cannot open, and the queue keeps searching: that is the plate.
                float until = Time.realtimeSinceStartup + 3;
                while (Time.realtimeSinceStartup < until && !HubQueueWatch.QueueRoom) yield return null;
                Assert.IsTrue(HubQueueWatch.QueueRoom, "PLAY did not start the queue.");
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.IsTrue(TumpHub.Current.Canvas.GetComponentsInChildren<Button>().Any(b => b.name == "CancelQueue"),
                              "The queue plate with its X is on screen while queued.");
                yield return Shots("Home-queued");
                yield return Press("CancelQueue");
                Assert.IsFalse(HubQueueWatch.QueueRoom, "X did not leave the queue.");
                Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked, "Cancelling must close the queue's room.");

                // BUGS-0926.3: the PLAY button reads IN QUEUE while queued, and pressing it leaves.
                yield return Press("PlayButton");
                until = Time.realtimeSinceStartup + 3;
                while (Time.realtimeSinceStartup < until && !HubQueueWatch.QueueRoom) yield return null;
                Assert.IsTrue(HubQueueWatch.QueueRoom, "PLAY did not start the queue a second time.");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Press("PlayButton");
                Assert.IsFalse(HubQueueWatch.QueueRoom, "Pressing IN QUEUE did not leave the queue.");
                Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked, "Leaving through IN QUEUE must close the queue's room.");

                TumpHub.Current.Push<HubMatchFound>();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shots("MatchFound");
                until = Time.realtimeSinceStartup + 4;
                while (Time.realtimeSinceStartup < until && !(TumpHub.Current.Top is HubCharacterSelect)) yield return null;
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top, "MATCH FOUND advances to CHARACTER SELECT.");
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shots("CharacterSelect");
                TumpHub.Current.Home();

                // A LAN room, hosted for real, and its lobby.
                var task = TumpHub.Current.Host.HostRoom("TEST ROOM", SceneFlow.Eskinita, GameMode.HeroStrike, RoomVisibility.Public, false);
                until = Time.realtimeSinceStartup + 15;
                while (Time.realtimeSinceStartup < until && !task.IsCompleted) yield return null;
                Assert.IsTrue(task.IsCompleted && string.IsNullOrEmpty(task.Result), "Hosting a LAN room failed: " + (task.IsCompleted ? task.Result : "timeout"));
                until = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < until && !(TumpHub.Current.Top is HubLobby)) yield return null;
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top,
                    "A room opened outside the hub's buttons must automatically present LOBBY.");
                var lobby = TumpHub.Current.Top;
                TumpHub.Current.ShowLobby();
                Assert.AreSame(lobby, TumpHub.Current.Top, "Repeated room completion must reuse the lobby.");
                yield return new WaitForSecondsRealtime(1.0f);
                yield return Shots("Lobby");
                Assert.IsTrue(TumpHub.Current.Host.Seats().Any(s => s.Mine && s.Host), "The host's own seat carries the host mark.");
                yield return Press("CharacterDoor");
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top);
                TumpHub.Current.ShowLobby();
                yield return null;
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top,
                    "Repeated room observations must not dismiss a lobby's character subpage.");
                Assert.AreEqual(1, TumpHub.Current.Canvas.GetComponentsInChildren<HubLobby>(true).Length);
                yield return Shots("CharacterSelect-lobby");
                Back(); yield return null;
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
                Back(); yield return null;
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.IsFalse(Net.NetSession.Instance.IsNetworked, "BACK from the lobby leaves the room.");

                // The loading curtain, drawn over the court without loading anything.
                HubLoading.Begin(SceneFlow.Eskinita, externallyLoaded: true);
                yield return new WaitForSecondsRealtime(0.3f);
                var loading = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.name == "TumpLoadingCanvas");
                var settings = Settings.SettingsStore.Current;
                string prefix = settings.HighContrastHud && settings.LargerText ? "Hub-A11y-Loading-" : "Hub-Loading-";
                foreach (var size in Shapes)
                    yield return TumpUiCapture.Capture(prefix + size.x + "x" + size.y, loading, size.x, size.y,
                                                       checkPalette: false, checkActionBounds: true);
                AssertFloor(loading, "Loading");
                Object.Destroy(Object.FindFirstObjectByType<HubLoading>().gameObject);
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
                Net.NetSession.Instance?.Stop();
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator HighContrastAndLargerTextKeepEveryDoorAndLobbyReadable()
        {
            var settings = Settings.SettingsStore.Current;
            bool contrast = settings.HighContrastHud, larger = settings.LargerText;
            try
            {
                settings.HighContrastHud = true;
                settings.LargerText = true;
                // Walk the same public routes, with the same bounds and 28-unit assertions.
                // Distinct capture names retain both settings together without replacing normal evidence.
                yield return HomeAndEveryDoorOpensItsScreenAndBackReturns();
                yield return GamemodeSelectSetsTheModeCardAndOpensTheCustomFlow();
                yield return QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn();
            }
            finally
            {
                Settings.SettingsStore.Current.HighContrastHud = contrast;
                Settings.SettingsStore.Current.LargerText = larger;
                Settings.SettingsStore.Save();
            }
        }
    }
}
