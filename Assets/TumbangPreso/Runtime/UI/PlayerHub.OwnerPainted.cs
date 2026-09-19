using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        private Text _ownerPageTitle;
        private Image _ownerPortrait;
        private readonly Dictionary<string,string> _ownerDraft=new Dictionary<string,string>();
        private string _ownerDraftId,_ownerFriendSearch="";
        private bool _ownerSaving;
        private RectTransform _ownerDetailList;

        private void InstallPreviousPaintedHub()
        {
            if(_canvas!=null)return;
            ScreenTakeover.Register(this,()=>_root!=null && _root.activeInHierarchy);
            _canvas=OwnerUiLayout.Canvas(transform,"OwnerPlayerHubCanvas",500);
            _root=OwnerUiLayout.Rect(_canvas.transform,"HubRoot").gameObject;OwnerUiLayout.Fill((RectTransform)_root.transform);
            OwnerUiBackdrop.Build(_root.transform);var design=OwnerUiLayout.DesignArea(_root.transform,"HubComposition");
            OwnerTextAction.Create(design,"ClosePlayerHub","BACK",Close,52,22,170,72,30);
            _ownerPortrait=OwnerPortraitArt.Create(design,"ProfilePortrait","UI/portraits/bayan");
            OwnerUiLayout.Place(_ownerPortrait.rectTransform,87,122,132,137);
            _handle=OwnerUiLayout.Text(design,"AccountHandle","",52,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_handle.rectTransform,261,111,1141,90);
            _state=OwnerUiLayout.Text(design,"AccountState","",27);_state.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_state.rectTransform,265,211,1110,72);_state.alignment=TextAnchor.UpperLeft;
            _levelChip=OwnerUiLayout.Text(design,"AccountLevel","",32,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_levelChip.rectTransform,1467,109,334,58);_levelChip.alignment=TextAnchor.MiddleRight;
            _xpCount=OwnerUiLayout.Text(design,"AccountXp","",27);_xpCount.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_xpCount.rectTransform,1467,182,334,52);_xpCount.alignment=TextAnchor.MiddleRight;
            var xp=OwnerUiLayout.Rect(design,"AccountXpTrack").gameObject.AddComponent<Image>();xp.color=OwnerUiTheme.Current.Peach;xp.raycastTarget=false;
            OwnerUiLayout.Place(xp.rectTransform,1466,239,334,10);_xpFill=OwnerUiLayout.Rect(xp.transform,"XpFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_xpFill.rectTransform);_xpFill.color=OwnerUiTheme.Current.Green;_xpFill.raycastTarget=false;
            var tabs=new[]{Tab.Profile,Tab.Friends,Tab.Career,Tab.Matches,Tab.Account};
            var words=new[]{"PROFILE","FRIENDS","CAREER","HISTORY","ACCOUNT"};
            var icons=new[]{TumpSymbol.Icon.Person,TumpSymbol.Icon.Friends,TumpSymbol.Icon.Trophy,TumpSymbol.Icon.Book,TumpSymbol.Icon.Settings};
            for(int i=0;i<tabs.Length;i++)
            {
                var tab=tabs[i];var button=OwnerTextAction.Create(design,"HubTab"+tab,words[i],()=>Show(tab),97+i*342,293,322,77,30);
                var label=button.GetComponentInChildren<Text>();OwnerUiLayout.Place(label.rectTransform,65,0,257,64);
                var icon=OwnerUiLayout.Rect(button.transform,"TabIcon").gameObject.AddComponent<TumpSymbol>();icon.Kind=icons[i];icon.color=OwnerUiTheme.Current.ActionInk;icon.raycastTarget=false;
                OwnerUiLayout.Place(icon.rectTransform,9,12,44,44);
                var mark=OwnerUiLayout.Art(button.transform,"SelectedTab",OwnerUiTheme.Piece.LeftRule);OwnerUiLayout.Place(mark.rectTransform,61,68,242,6);_tabs.Add(tab,button);
            }
            var paper=OwnerUiLayout.Rect(design,"HubPagePaper").gameObject.AddComponent<OwnerUiPaper>();OwnerUiLayout.Place(paper.rectTransform,91,406,1740,491);paper.raycastTarget=false;
            _ownerPageTitle=OwnerUiLayout.Text(design,"HubPageTitle","",32,OwnerUiLayout.TypeRole.Display);OwnerUiLayout.Place(_ownerPageTitle.rectTransform,126,418,1640,52);
            _list=OwnerScrollColumn.Build(design,"HubRows",new Rect(126,486,1655,373),out _scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom=24;
            _footerNote=OwnerUiLayout.Text(design,"HubNotice","",27);_footerNote.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_footerNote.rectTransform,105,943,1134,94);
            _footerAction=OwnerPaintedAction.Create(design,"HubPrimary","SAVE",FooterPressed,false,47);
            OwnerUiLayout.Place((RectTransform)_footerAction.transform,1378,952,413,91);_footerLabel=_footerAction.GetComponentInChildren<Text>();
            _signIn=GetComponent<SignInScreen>();if(_signIn==null)_signIn=gameObject.AddComponent<SignInScreen>();_signIn.Install();
            _signIn.Closed+=OnSignInClosed;_signIn.Opened+=visible=>{if(_root!=null)_root.SetActive(!visible);};
            _root.SetActive(false);
            if(GameServices.Account!=null)GameServices.Account.Changed+=OnDataChanged;
            if(GameServices.Career!=null)GameServices.Career.Changed+=OnDataChanged;
            if(GameServices.Social!=null)GameServices.Social.Changed+=OnDataChanged;
        }
        private void Show(Tab tab)
        {
            string identity=GameServices.Account?.PlayerId??"local";
            if(_ownerDraftId!=identity){_ownerDraftId=identity;_ownerDraft.Clear();_ownerFriendSearch="";_shown.Clear();_page=0;}
            bool arriving=_tab!=tab;_tab=tab;_deleteArmed&=tab==Tab.Account;
            if(arriving && tab==Tab.Friends)GameServices.Social?.Refresh();
            OwnerOptionMenu.OpenOption?.Close();
            foreach(var pair in _tabs)
            {
                pair.Value.GetComponentInChildren<Text>().color=pair.Key==tab?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.Pale;
                var index=pair.Value.GetComponent<ProfileIndexTab>();index.Selected=pair.Key==tab;index.SetVerticesDirty();
                pair.Value.transform.Find("SelectedTab").gameObject.SetActive(pair.Key==tab);
            }
            foreach(Transform child in _list){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            ForgetFields();RefreshHeader();_ownerPageTitle.text=tab==Tab.Matches?"MATCH HISTORY":tab.ToString().ToUpperInvariant();
            switch(tab){case Tab.Profile:BuildProfileTab();break;case Tab.Friends:BuildFriendsTab();break;case Tab.Career:BuildCareerTab();break;case Tab.Matches:BuildMatchesTab();break;case Tab.Account:BuildAccountTab();break;}
            _scroll.verticalNormalizedPosition=1;_footerAction.interactable=!_ownerSaving;_canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void RefreshHeader()
        {
            var account=GameServices.Account;var profile=GameServices.Career?.Profile;
            _handle.text=account?.LobbyName??Settings.SettingsStore.Current.PlayerName??"PLAYER";
            _state.text=account==null?"Profile unavailable":account.IsGuest?"Guest · progress stays on this device":
                account.HasPassword?"Signed in as "+account.Username:"Saved on this device · no username yet";
            int xp=profile?.Xp??0;_levelChip.text="LEVEL "+ProgressionRules.LevelForXp(xp);
            int into=ProgressionRules.XpIntoLevel(xp);_xpCount.text=into+" / "+ProgressionRules.XpPerLevel+" XP";
            _xpFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(into/(float)ProgressionRules.XpPerLevel),1);
            var people=Roster.GetPeople(SceneFlow.SelectedMode);int pick=Mathf.Clamp(Settings.SettingsStore.Current.CharacterPick,0,people.Count-1);
            _ownerPortrait.sprite=OwnerPortraitArt.Get("UI/portraits/"+people[pick].Id);
        }
        private bool Group(string title,string subtitle,bool openByDefault=true)
        {
            string key=_tab+"/"+title;if(!_groups.TryGetValue(key,out bool open))open=openByDefault;
            var row=OwnerUiLayout.Rect(_list,"Group_"+title);row.gameObject.AddComponent<LayoutElement>().preferredHeight=102;
            var button=OwnerTextAction.Create(row,"ToggleGroup",(open?"-  ":"+  ")+title,()=>{_groups[key]=!open;Show(_tab);},0,0,1530,48,28);
            button.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;
            var hint=OwnerUiLayout.Text(row,"GroupHint",subtitle,28);hint.color=OwnerUiTheme.Current.EnteredInk;OwnerUiLayout.Place(hint.rectTransform,0,49,1540,48);
            return open;
        }
        private RectTransform HubValue(string label,string value,string detail="",Color? tint=null,string portrait=null)
        {
            var row=OwnerUiLayout.Rect(_list,"Value_"+label);row.gameObject.AddComponent<LayoutElement>().preferredHeight=string.IsNullOrEmpty(detail)?74:114;
            float inset=0;
            if(!string.IsNullOrEmpty(portrait)){var image=OwnerPortraitArt.Create(row,"ValuePortrait",portrait);OwnerUiLayout.Place(image.rectTransform,0,1,76,78);inset=98;}
            var title=OwnerUiLayout.Text(row,"ValueName",label,29,OwnerUiLayout.TypeRole.Accent);OwnerUiLayout.Place(title.rectTransform,inset,0,910-inset,64);
            bool longValue=(value?.Length??0)>22;
            var text=OwnerUiLayout.Text(row,"ValueText",value,longValue?28:31,longValue?OwnerUiLayout.TypeRole.Reading:OwnerUiLayout.TypeRole.Display);text.color=tint??OwnerUiTheme.Current.ActionInk;
            OwnerUiLayout.Place(text.rectTransform,946,0,622,64);text.alignment=TextAnchor.MiddleRight;
            if(!string.IsNullOrEmpty(detail)){var hint=OwnerUiLayout.Text(row,"ValueDetail",detail,28);hint.color=OwnerUiTheme.Current.EnteredInk;OwnerUiLayout.Place(hint.rectTransform,inset,65,1540-inset,43);}
            return row;
        }
        private Button HubAction(string name,string title,string action,Action click,string detail="")
        {
            var row=HubValue(title,"",detail);
            return OwnerTextAction.Create(row,name,action,click,1010,0,549,70,28);
        }
        private void HubNote(string text)
        {
            var label=OwnerUiLayout.Text(_list,"HubNote",text,28);label.color=OwnerUiTheme.Current.EnteredInk;label.gameObject.AddComponent<TumpParagraph>();
        }
        private void HubChoice(string name,string label,string[] options,int selected,Action<int> choose)
            =>RecordChoice.Create(RecordFields.Row(_list,name,label),name+"Value",options,selected,choose);
        private InputField HubField(string key,string label,string value,int limit,string placeholder,OwnerUiTheme.Piece frame)
        {
            var field=RecordFields.Input(RecordFields.Row(_list,key+"Row",label),key,placeholder);
            field.characterLimit=limit;field.SetTextWithoutNotify(_ownerDraft.TryGetValue(key,out var draft)?draft:value??"");
            field.interactable=!_ownerSaving;field.onValueChanged.AddListener(text=>
            {
                _ownerDraft[key]=text;_notice="";
                if(_footerNote!=null && _tab==Tab.Profile)_footerNote.text="Unsaved profile changes.";
            });return field;
        }
        private string OwnerDraftValue(string key,string fallback)=>_ownerDraft.TryGetValue(key,out var value)?value:fallback;
        private void OwnerProfileSaving(bool saving)
        {
            _ownerSaving=saving;
            foreach(var field in new[]{_displayName,_country,_pronouns,_bio})if(field!=null)field.interactable=!saving;
            if(_footerAction!=null)_footerAction.interactable=!saving;
        }
        private void OwnerProfileSaved()
        {
            _ownerDraft.Clear();
            if(_root!=null && _root.activeInHierarchy && _tab==Tab.Profile)Show(Tab.Profile);
        }
        private void BuildProfileTab()
        {
            var account=GameServices.Account;
            // The fixed tag is already in the identity header. Keep the useful edit first.
            _displayName=HubField("PlayerNameEdit","Display name",account?.DisplayName??Settings.SettingsStore.Current.PlayerName,
                AccountRules.DisplayNameMax,"DISPLAY NAME",OwnerUiTheme.Piece.FirstField);
            if(Group("Optional details","Optional and public on your career page.",false))
            {
                _country=HubField("ProfileCountry","Country",account?.Country,AccountRules.CountryCodeLength,"PH",OwnerUiTheme.Piece.SecondField);
                _pronouns=HubField("ProfilePronouns","Pronouns",account?.Pronouns,AccountRules.PronounsMax,"PRONOUNS",OwnerUiTheme.Piece.ThirdField);
                _bio=HubField("ProfileBio","Bio",account?.Bio,AccountRules.BioMax,"ONE LINE ABOUT YOU",OwnerUiTheme.Piece.FirstField);
            }
            BuildBannerGroup();SetFooter("SAVE",string.IsNullOrEmpty(_notice) && _ownerDraft.Count>0?"Unsaved profile changes.":_notice);
        }
        private void BuildBannerGroup()
        {
            var earned=BannerRules.Earned(GameServices.Career?.Profile);
            if(!Group("Banner",earned.Count==0?"Play to earn titles, badges, borders and palettes.":"Choose what appears beside your name.",false))return;
            if(earned.Count==0){HubNote("Nothing earned yet. Account levels and hero mastery unlock banner rewards.");return;}
            var s=Settings.SettingsStore.Current;
            BannerSlot("Title",RewardKind.Title,earned,s.BannerTitleId,id=>s.BannerTitleId=id);
            BannerSlot("Badge",RewardKind.Badge,earned,s.BannerBadgeId,id=>s.BannerBadgeId=id);
            BannerSlot("Border",RewardKind.Border,earned,s.BannerBorderId,id=>s.BannerBorderId=id);
            BannerSlot("Palette",RewardKind.Palette,earned,s.BannerPaletteId,id=>s.BannerPaletteId=id);
        }
        private void BannerSlot(string label,RewardKind kind,List<Reward> earned,string current,Action<string> choose)
        {
            var names=new List<string>{"NONE"};var ids=new List<string>{""};
            foreach(var reward in earned)
            {
                if(reward==null || reward.Kind!=kind || ids.Contains(reward.Id))continue;
                ids.Add(reward.Id);names.Add(reward.Label??reward.Id);
            }
            if(ids.Count<2)return;
            HubChoice("Banner"+label,label,names.ToArray(),Mathf.Max(0,ids.IndexOf(current??"")),index=>
            {choose(ids[index]);Settings.SettingsStore.Save();Show(Tab.Profile);});
        }
        private void BuildAccountTab()
        {
            var account=GameServices.Account;
            if(account==null){HubNote("Accounts are unavailable right now.");SetFooter("",_notice);return;}
            string status=account.Status??"";
            if(status.Contains("UGS sign-in is disabled in batch mode"))status="Online sign-in is unavailable here. Local multiplayer still works.";
            HubValue("Status",account.IsGuest?"GUEST":account.HasPassword?"SIGNED IN":"LOCAL ONLY",status);
            if(!account.HasPassword)
                HubAction("SetupAccount","Keep this progress","SET UP SIGN IN",OpenSignIn,"Your current progress stays with you.");
            else HubAction("SwitchAccount","Signed in as "+account.Username,"SWITCH ACCOUNT",OpenSignIn);
            // ⚠⚠ THESE TWO ROWS ARE THE WHOLE OF PASSWORD RECOVERY IN THIS GAME, AND THEY ONLY
            // WORK BEFORE THE DAY THEY ARE NEEDED. UGS's username-password provider holds no
            // address, so there is no reset to send and no admin endpoint this client can call:
            // a player who forgets their password with no second identity attached has lost the
            // account. CHANGE is for the player who still knows it, CONNECT is the second door.
            // `docs/TODO.md` § 153.15.
            if(account.HasPassword)
                HubAction("ChangePassword","Password","CHANGE",OpenChangePassword,
                    account.GoogleAvailable && !account.HasGoogle
                        ? "There is no password reset. Connect Google below so you have a way back in."
                        : "There is no password reset, so keep it somewhere safe.");
            if(account.GoogleAvailable)
            {
                if(account.HasGoogle)HubValue("Google","CONNECTED","This is how you get back in if you forget your password.");
                else HubAction("ConnectGoogle","Google account","CONNECT",OpenSignInForGoogle,
                    "A second way into this account, and the only one there is.");
            }
            if(Group("Tournament guest","Let somebody else play without changing your account.",account.IsGuest))
                HubAction("TournamentGuest",account.IsGuest?"A guest is playing":"Hand over this device",account.IsGuest?"TAKE IT BACK":"PLAY AS GUEST",ToggleGuest);
            if(Group("Account details","Technical account information.",false))HubValue("Player ID",account.PlayerId);
            if(Group("Delete account","Permanently removes this account and its server history.",_deleteArmed))
                HubAction("DeleteAccount","Delete this account",_deleteArmed?"PRESS AGAIN TO DELETE":"DELETE",DeleteAccount,_deleteArmed?"This is permanent.":"");
            SetFooter("",_notice);
        }
    }
}
