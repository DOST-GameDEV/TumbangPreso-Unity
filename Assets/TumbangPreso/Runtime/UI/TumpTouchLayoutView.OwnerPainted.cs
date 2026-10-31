using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpTouchLayoutView
    {
        private RectTransform _toolbarRect;
        private bool _toolbarBelow,_toolbarExpanded;
        private Text _toolbarPositionLabel;
        private void PositionToolbar()
        {
            float height=_toolbarExpanded?404:214;
            OwnerUiLayout.Place(_toolbarRect,40,_toolbarBelow?1080-height-28:26,1520,height);
            if(_toolbarPositionLabel!=null)_toolbarPositionLabel.text=_toolbarBelow?"TOOLS ABOVE":"TOOLS BELOW";
        }
        private void BuildToolbar()
        {
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"TouchEditorComposition");
            var bar=OwnerUiLayout.Rect(design,"LayoutToolbar").gameObject.AddComponent<Image>();Hub.HubKnit.KitFace(bar,Hub.Knit.Panel);
            _toolbarRect=bar.rectTransform;_toolbarBelow=false;_toolbarExpanded=false;
            PositionToolbar();bar.raycastTarget=true;
            var layer=bar.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=850;layer.vertexColorAlwaysGammaSpace=true;
            bar.gameObject.AddComponent<GraphicRaycaster>();
            var title=OwnerUiLayout.Text(bar.transform,"Heading","TOUCH LAYOUT",56,OwnerUiLayout.TypeRole.Display);
            title.color=SettingsPalette.PanelAccent;
            OwnerUiLayout.Place(title.rectTransform,48,44,760,84);
            var hint=OwnerUiLayout.Text(bar.transform,"Hint","Drag controls. Save to keep your layout.",32);
            hint.color=SettingsPalette.PanelInk;hint.font=Hub.HubKnit.SentenceFont(false);OwnerUiLayout.Place(hint.rectTransform,48,122,770,58);
            var position=OwnerTextAction.Create(bar.transform,"TouchToolbarPosition","TOOLS BELOW",()=>
            {
                _toolbarBelow=!_toolbarBelow;PositionToolbar();bar.GetComponent<ScreenFocus>().Rebuild();
            },1190,20,300,80,28);
            _toolbarPositionLabel=position.GetComponentInChildren<Text>();
            SettingsWorkspaceRows.KitAction(position,Hub.Knit.Plain,1190,20,300,80,28);
            var save=OwnerTextAction.Create(bar.transform,"SaveTouchLayout","SAVE LAYOUT",()=>Close(true),1270,108,230,84,30);
            SettingsWorkspaceRows.KitAction(save,Hub.Knit.Go,1270,108,230,84,30);
            var cancel=OwnerTextAction.Create(bar.transform,"CancelTouchLayout","CANCEL",()=>Close(false),840,108,200,84,30);
            SettingsWorkspaceRows.KitAction(cancel,Hub.Knit.Danger,840,108,200,84,30);
            var reset=OwnerTextAction.Create(bar.transform,"ResetTouchLayout","RESET",Reset,1055,108,200,84,30);
            SettingsWorkspaceRows.KitAction(reset,Hub.Knit.Yellow,1055,108,200,84,30);
            var adjustments=OwnerUiLayout.Rect(bar.transform,"SizeAndOpacity");OwnerUiLayout.Fill(adjustments);
            var tune=OwnerTextAction.Create(bar.transform,"TouchAdjustments","SIZE & OPACITY",()=>
            {
                bool show=!adjustments.gameObject.activeSelf;adjustments.gameObject.SetActive(show);
                _toolbarExpanded=show;PositionToolbar();
                bar.GetComponent<ScreenFocus>().Rebuild();
            },840,20,335,80,28);
            SettingsWorkspaceRows.KitAction(tune,Hub.Knit.Plain,840,20,335,80,28);
            var opacity=OwnerUiLayout.Text(adjustments,"Opacity","OPACITY",36,OwnerUiLayout.TypeRole.Display);opacity.color=SettingsPalette.PanelAccent;
            OwnerUiLayout.Place(opacity.rectTransform,48,212,260,70);
            var opacitySlot=OwnerUiLayout.Rect(adjustments,"OpacityControl");OwnerUiLayout.Place(opacitySlot,320,208,620,78);
            _opacitySlider=SettingsWorkspaceRows.Slider(opacitySlot,"TouchOpacity",TouchLayoutStore.Opacity,TouchLayoutStore.MinOpacity,TouchLayoutStore.MaxOpacity,
                v=>TouchLayoutStore.Opacity=v,v=>Mathf.RoundToInt(v*100)+"%");
            var size=OwnerUiLayout.Text(adjustments,"Size","SIZE",36,OwnerUiLayout.TypeRole.Display);size.color=SettingsPalette.PanelAccent;
            OwnerUiLayout.Place(size.rectTransform,48,300,260,70);
            var sizeSlot=OwnerUiLayout.Rect(adjustments,"SizeControl");OwnerUiLayout.Place(sizeSlot,320,296,620,78);
            _sizeSlider=SettingsWorkspaceRows.Slider(sizeSlot,"TouchSize",TouchLayoutStore.Scale,TouchLayoutStore.MinScale,TouchLayoutStore.MaxScale,
                v=>TouchLayoutStore.Scale=v,v=>Mathf.RoundToInt(v*100)+"%");
            adjustments.gameObject.SetActive(false);ScreenFocus.Install(bar.gameObject).Rebuild();
        }
    }
}
