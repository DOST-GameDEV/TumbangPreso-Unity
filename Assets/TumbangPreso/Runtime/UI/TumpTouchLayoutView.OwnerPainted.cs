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
            float height=_toolbarExpanded?296:181;
            OwnerUiLayout.Place(_toolbarRect,97,_toolbarBelow?1080-height-28:28,1726,height);
            if(_toolbarPositionLabel!=null)_toolbarPositionLabel.text=_toolbarBelow?"TOOLS ABOVE":"TOOLS BELOW";
        }
        private void BuildToolbar()
        {
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"TouchEditorComposition");
            var bar=OwnerUiLayout.Rect(design,"LayoutToolbar").gameObject.AddComponent<Image>();bar.color=SettingsPalette.Surface;
            _toolbarRect=bar.rectTransform;_toolbarBelow=false;_toolbarExpanded=false;
            PositionToolbar();bar.raycastTarget=true;
            var layer=bar.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=850;layer.vertexColorAlwaysGammaSpace=true;
            bar.gameObject.AddComponent<GraphicRaycaster>();
            var title=OwnerUiLayout.Text(bar.transform,"Heading","TOUCH LAYOUT",51,OwnerUiLayout.TypeRole.Display);
            title.color=SettingsPalette.Ink;
            OwnerUiLayout.Place(title.rectTransform,36,20,750,84);
            var hint=OwnerUiLayout.Text(bar.transform,"Hint","Drag controls. Save to keep your layout.",29);
            hint.color=SettingsPalette.Muted;OwnerUiLayout.Place(hint.rectTransform,36,113,790,58);
            var position=OwnerTextAction.Create(bar.transform,"TouchToolbarPosition","TOOLS BELOW",()=>
            {
                _toolbarBelow=!_toolbarBelow;PositionToolbar();bar.GetComponent<ScreenFocus>().Rebuild();
            },866,113,366,58,30);
            _toolbarPositionLabel=position.GetComponentInChildren<Text>();_toolbarPositionLabel.color=SettingsPalette.Muted;
            var save=OwnerTextAction.Create(bar.transform,"SaveTouchLayout","SAVE LAYOUT",()=>Close(true),1290,20,413,91,39);
            save.GetComponentInChildren<Text>().color=SettingsPalette.Accent;
            OwnerUiLayout.Place((RectTransform)save.transform,1290,20,413,91);
            OwnerTextAction.Create(bar.transform,"CancelTouchLayout","CANCEL",()=>Close(false),1290,113,196,58,30).GetComponentInChildren<Text>().color=SettingsPalette.Ink;
            OwnerTextAction.Create(bar.transform,"ResetTouchLayout","RESET",Reset,1500,113,196,58,30).GetComponentInChildren<Text>().color=SettingsPalette.Ink;
            var adjustments=OwnerUiLayout.Rect(bar.transform,"SizeAndOpacity");OwnerUiLayout.Fill(adjustments);
            OwnerTextAction.Create(bar.transform,"TouchAdjustments","SIZE & OPACITY",()=>
            {
                bool show=!adjustments.gameObject.activeSelf;adjustments.gameObject.SetActive(show);
                _toolbarExpanded=show;PositionToolbar();
                bar.GetComponent<ScreenFocus>().Rebuild();
            },815,32,416,68,30).GetComponentInChildren<Text>().color=SettingsPalette.Muted;
            var opacity=OwnerUiLayout.Text(adjustments,"Opacity","OPACITY",30,OwnerUiLayout.TypeRole.Accent);opacity.color=SettingsPalette.Ink;
            OwnerUiLayout.Place(opacity.rectTransform,36,192,190,58);
            var opacitySlot=OwnerUiLayout.Rect(adjustments,"OpacityControl");OwnerUiLayout.Place(opacitySlot,227,181,533,78);
            _opacitySlider=SettingsWorkspaceRows.Slider(opacitySlot,"TouchOpacity",TouchLayoutStore.Opacity,TouchLayoutStore.MinOpacity,TouchLayoutStore.MaxOpacity,
                v=>TouchLayoutStore.Opacity=v,v=>Mathf.RoundToInt(v*100)+"%");
            var size=OwnerUiLayout.Text(adjustments,"Size","SIZE",30,OwnerUiLayout.TypeRole.Accent);size.color=SettingsPalette.Ink;
            OwnerUiLayout.Place(size.rectTransform,849,192,140,58);
            var sizeSlot=OwnerUiLayout.Rect(adjustments,"SizeControl");OwnerUiLayout.Place(sizeSlot,1020,181,533,78);
            _sizeSlider=SettingsWorkspaceRows.Slider(sizeSlot,"TouchSize",TouchLayoutStore.Scale,TouchLayoutStore.MinScale,TouchLayoutStore.MaxScale,
                v=>TouchLayoutStore.Scale=v,v=>Mathf.RoundToInt(v*100)+"%");
            adjustments.gameObject.SetActive(false);ScreenFocus.Install(bar.gameObject).Rebuild();
        }
    }
}
