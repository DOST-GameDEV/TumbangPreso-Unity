using System;
using System.Linq;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSettingsView
    {
        private OwnerOptionMenu _ownerFrameCap;
        private void Build(Transform owner)
        {
            _canvas=OwnerUiLayout.Canvas(owner,"OwnerSettingsCanvas",800);OwnerUiBackdrop.Build(_canvas.transform);
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"SettingsComposition");
            OwnerTextAction.Create(design,"TumpSettingsBack","BACK",Back,54,25,170,70,30);
            var title=OwnerUiLayout.Text(design,"SettingsTitle","SETTINGS",63,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,89,110,1380,98);
            var logo=OwnerUiLayout.Art(design,"OwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,1642,25,190,190*273f/407);
            TumpSymbol.Icon[] icons={TumpSymbol.Icon.Controller,TumpSymbol.Icon.Audio,TumpSymbol.Icon.Eye,TumpSymbol.Icon.Person,TumpSymbol.Icon.Settings};
            for(int i=0;i<Sections.Length;i++)
            {
                int index=i;
                var button=OwnerTextAction.Create(design,"SettingsSection"+i,Sections[i],()=>ShowSection(index),93+i*343,231,327,85,29);
                var label=button.GetComponentInChildren<Text>();OwnerUiLayout.Place(label.rectTransform,67,4,260,65);
                var icon=OwnerUiLayout.Rect(button.transform,"SectionIcon").gameObject.AddComponent<TumpSymbol>();
                icon.Kind=icons[i];icon.color=OwnerUiTheme.Current.ActionInk;icon.raycastTarget=false;
                OwnerUiLayout.Place(icon.rectTransform,8,12,50,50);
                var line=OwnerUiLayout.Art(button.transform,"SelectedSection",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(line.rectTransform,67,76,242,6);_tabs.Add(button);
            }
            var sheet=OwnerUiLayout.Rect(design,"SettingsSheet").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(sheet.rectTransform,90,343,1740,571);sheet.raycastTarget=false;
            _heading=OwnerUiLayout.Text(design,"Heading","",32,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_heading.rectTransform,123,354,1670,57);
            _list=OwnerScrollColumn.Build(design,"SettingsList",new Rect(123,425,1659,455),out var scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom=20;
            _status=OwnerUiLayout.Text(design,"SettingsStatus","",27);_status.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_status.rectTransform,101,960,1190,75);
            _save=OwnerPaintedAction.Create(design,"TumpSaveSettings","SAVE CHANGES",()=>_session.Save(),false,40);
            OwnerUiLayout.Place((RectTransform)_save.transform,1387,952,413,91);
        }
        public void ShowSection(int index)
        {
            _tab=Mathf.Clamp(index,0,Sections.Length-1);if(_canvas==null)return;
            OwnerOptionMenu.OpenOption?.Close();
            if(_session.Listening)_session.CancelRebind();
            foreach(Transform child in _list){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            _bindingRows.Clear();_ownerFrameCap=null;_frameReason=null;
            for(int i=0;i<_tabs.Count;i++)
            {
                _tabs[i].transform.Find("SelectedSection").gameObject.SetActive(i==_tab);
                _tabs[i].GetComponentInChildren<Text>().color=i==_tab?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
            }
            _heading.text=Sections[_tab];
            switch(_tab){case 0:Controls();break;case 1:Audio();break;case 2:Graphics();break;case 3:Player();break;case 4:Accessibility();break;}
            _list.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
            Changed("");_canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private void Note(string words)
        {
            var text=OwnerUiLayout.Text(_list,"Note",words,27);text.color=OwnerUiTheme.Current.EnteredInk;
            text.alignment=TextAnchor.UpperLeft;text.gameObject.AddComponent<LayoutElement>().preferredHeight=67;
        }
        private RectTransform Row(string name,string label)=>OwnerSettingsRows.Row(_list,name,label);
        private void Toggle(string name,string label,bool value,Action<bool> set,Action apply=null)
            =>OwnerSettingsRows.Toggle(Row(name,label),name+"Value",value,v=>{set(v);_session.Preview(apply);});
        private void Choice(string name,string label,string[] values,int value,Action<int> set)
            =>OwnerOptionMenu.Create(Row(name,label),name+"Value",values,value,v=>{set(v);_session.Preview();});
        private void Audio()
        {
            Note("Listen as you adjust. Save to keep your changes.");var s=SettingsStore.Current;
            AudioSlider("MasterVolume","Master volume",s.MasterVolume,v=>s.MasterVolume=v);
            AudioSlider("SoundVolume","Sound effects",s.SfxVolume,v=>s.SfxVolume=v);
            AudioSlider("MusicVolume","Music",s.MusicVolume,v=>s.MusicVolume=v);
        }
        private void AudioSlider(string name,string label,float value,Action<float> set)
            =>OwnerSettingsRows.Slider(Row(name,label),name+"Value",value,0,1,v=>{set(v);_session.Preview();},v=>Mathf.RoundToInt(v*100)+"%");
        private void Graphics()
        {
            Note("Preview a change, then save it or return to your previous settings.");var s=SettingsStore.Current;
            Choice("GraphicsQuality","Graphics quality",GraphicsProfiles.All.Select(p=>p.Label).ToArray(),s.GraphicsQuality,
                v=>{s.GraphicsQuality=v;GraphicsProfiles.Apply(v);});
            Choice("RenderStyle","Visual style",RenderStyles.All.Select(p=>p.Label).ToArray(),s.RenderStyle,
                v=>{s.RenderStyle=v;RenderStyles.Apply(v);});
            Choice("AntiAliasing","Smooth edges",AntiAliasModes.All.Select(p=>p.Label).ToArray(),s.AntiAliasMode,
                v=>{s.AntiAliasMode=v;AntiAliasModes.Apply(v);});
            Choice("VSync","Vertical sync",VSyncModes.All.Select(p=>p.Label).ToArray(),s.VSyncMode,
                v=>{s.VSyncMode=v;VSyncModes.Apply(v);FrameRateOptions.Apply(s.FrameRateLimit);UpdateFrameCapState();});
            _ownerFrameCap=OwnerOptionMenu.Create(Row("FrameRate","Frame rate limit"),"FrameRateValue",
                FrameRateOptions.All.Select(FrameRateOptions.Label).ToArray(),Array.IndexOf(FrameRateOptions.All,s.FrameRateLimit),
                v=>{s.FrameRateLimit=FrameRateOptions.All[v];FrameRateOptions.Apply(s.FrameRateLimit);_session.Preview();});
            _frameReason=OwnerUiLayout.Text(_list,"FrameRateReason","",27);_frameReason.color=OwnerUiTheme.Current.EnteredInk;
            _frameReason.gameObject.AddComponent<LayoutElement>().preferredHeight=67;UpdateFrameCapState();
            Toggle("Fullscreen","Fullscreen",s.Fullscreen,v=>s.Fullscreen=v,s.ApplyDisplay);
        }
        private void UpdateFrameCapState()
        {
            if(_ownerFrameCap==null || _frameReason==null)return;
            bool sync=VSyncModes.Of(SettingsStore.Current.VSyncMode).Count>0;
            int launch=FrameRateOptions.ReadOperatorLimit(Environment.GetCommandLineArgs());
            _ownerFrameCap.Button.interactable=!sync && launch<=0;
            _frameReason.text=launch>0?"Launch settings control the frame rate: "+launch+" FPS.":
                sync?"Turn vertical sync off to use your saved frame rate limit.":"Your saved limit applies while vertical sync is off.";
        }
        private void Player()
        {
            var s=SettingsStore.Current;
            var name=OwnerUiEntry.Create(Row("PlayerName","Player name"),"PlayerNameField","PLAYER NAME",OwnerUiTheme.Piece.SecondField);
            name.SetTextWithoutNotify(s.PlayerName);name.characterLimit=Core.Balance.PlayerNameMax;
            name.onValueChanged.AddListener(v=>{s.PlayerName=v;_session.Preview();});
            Toggle("Telemetry","Share play statistics",s.TelemetryEnabled,v=>s.TelemetryEnabled=v);
            Note("Counts only: matches, modes, maps, picks and frame rate. No names, chat or anything you type.");
        }
        private void Accessibility()
        {
            var s=SettingsStore.Current;
            Toggle("ReducedUiMotion","Reduce interface motion",s.ReducedUiMotion,v=>s.ReducedUiMotion=v);
            Choice("SlipperHighlight","Slipper highlight",SlipperHighlights.All.Select(p=>p.Label).ToArray(),s.SlipperHighlight,v=>s.SlipperHighlight=v);
            Toggle("Rumble","Controller vibration",s.Rumble,v=>s.Rumble=v,()=>Rumble.Enabled=s.Rumble);
            Note("Interface motion can be reduced while gameplay movement stays visible.");
        }
        private void Controls()
        {
            Choice("InputDevice","Input device",new[]{"Keyboard & mouse","Controller","Touch"},(int)_device,
                v=>{_device=(InputDeviceKind)v;ShowSection(0);});
            if(_device==InputDeviceKind.Touch)
            {
                ActionRow("TouchLayout","Touch controls","ARRANGE",()=>{Suspend();_touch?.Invoke();});return;
            }
            if(_device==InputDeviceKind.Gamepad)
                ActionRow("ControllerMap","Controller map","SEE CONTROLLER",()=>{Suspend();_controller?.Invoke();});
            Choice("BindingGroup","Control group",Rebinding.Groups.Select(g=>g.Title).ToArray(),_group,v=>{_group=v;ShowSection(0);});
            foreach(string action in Rebinding.Groups[_group].Actions)
            {
                string id=action;var button=ActionRow("Binding_"+action,Rebinding.LabelFor(action),BindingLabel(action),()=>_session.BeginRebind(id,_device));
                button.interactable=Rebinding.HasBindingFor(_session.Actions,action,_device);_bindingRows[action]=button;
            }
            if(_device==InputDeviceKind.KeyboardMouse && _group==0)
            {
                var s=SettingsStore.Current;
                OwnerSettingsRows.Slider(Row("Sensitivity","Mouse sensitivity"),"SensitivityValue",s.MouseSensitivity,.1f,5,
                    v=>{s.MouseSensitivity=v;_session.Preview();},v=>v.ToString("0.0")+"×");
                Toggle("InvertY","Invert vertical look",s.InvertY,v=>s.InvertY=v);
            }
            ActionRow("ResetControls","Reset bindings","RESET CONTROLS",_session.ResetControls);
        }
        private Button ActionRow(string name,string title,string label,Action action)
        {
            var button=OwnerTextAction.Create(Row(name,title),name+"Action",label,action,0,0,533,78,30);
            return button;
        }
        private void Decision()
        {
            if(_decision!=null){_decision.SetActive(true);return;}
            var root=OwnerUiLayout.Rect(_canvas.transform,"UnsavedDecision");OwnerUiLayout.Fill(root);_decision=root.gameObject;
            var dim=root.gameObject.AddComponent<Image>();var ink=OwnerUiTheme.Current.DeepInk;dim.color=new Color(ink.r,ink.g,ink.b,.8f);
            var paper=OwnerUiLayout.Rect(root,"DecisionPaper").gameObject.AddComponent<OwnerUiPaper>();paper.Style=OwnerUiPaper.Treatment.Dialog;
            paper.rectTransform.anchorMin=paper.rectTransform.anchorMax=paper.rectTransform.pivot=new Vector2(.5f,.5f);
            paper.rectTransform.sizeDelta=new Vector2(950,604);paper.raycastTarget=true;
            var title=OwnerUiLayout.Text(paper.transform,"DecisionHeading","KEEP YOUR CHANGES?",50,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,65,51,828,108);title.alignment=TextAnchor.MiddleCenter;
            var save=OwnerPaintedAction.Create(paper.transform,"SaveAndBack","SAVE & BACK",()=>{_session.Save();_decision.SetActive(false);Back();},false,41);
            OwnerUiLayout.Place((RectTransform)save.transform,267,224,413,91);
            OwnerTextAction.Create(paper.transform,"DiscardAndBack","DISCARD CHANGES",()=>{_session.Discard();_decision.SetActive(false);Back();},139,358,670,78,33);
            OwnerTextAction.Create(paper.transform,"KeepEditing","KEEP EDITING",()=>_decision.SetActive(false),139,474,670,78,33);
            ScreenFocus.Install(root.gameObject).Rebuild();
        }
    }
}
