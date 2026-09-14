using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpTouchLayoutView
    {
        private void BuildToolbar()
        {
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"TouchEditorComposition");
            var bar=OwnerUiLayout.Rect(design,"LayoutToolbar").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(bar.rectTransform,97,28,1726,181);bar.raycastTarget=true;
            var layer=bar.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=850;layer.vertexColorAlwaysGammaSpace=true;
            bar.gameObject.AddComponent<GraphicRaycaster>();
            var title=OwnerUiLayout.Text(bar.transform,"Heading","TOUCH LAYOUT",51,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,36,20,750,84);
            var hint=OwnerUiLayout.Text(bar.transform,"Hint","Drag controls to move them. Save to keep your layout.",29);
            hint.color=OwnerUiTheme.Current.EnteredInk;OwnerUiLayout.Place(hint.rectTransform,36,113,1240,58);
            var save=OwnerPaintedAction.Create(bar.transform,"SaveTouchLayout","SAVE LAYOUT",()=>Close(true),false,39);
            OwnerUiLayout.Place((RectTransform)save.transform,1290,20,413,91);
            OwnerTextAction.Create(bar.transform,"CancelTouchLayout","CANCEL",()=>Close(false),1290,113,196,58,29);
            OwnerTextAction.Create(bar.transform,"ResetTouchLayout","RESET",Reset,1500,113,196,58,29);
            var adjustments=OwnerUiLayout.Rect(bar.transform,"SizeAndOpacity");OwnerUiLayout.Fill(adjustments);
            OwnerTextAction.Create(bar.transform,"TouchAdjustments","SIZE & OPACITY",()=>
            {
                bool show=!adjustments.gameObject.activeSelf;adjustments.gameObject.SetActive(show);
                bar.rectTransform.sizeDelta=new Vector2(1726,show?296:181);
                bar.GetComponent<ScreenFocus>().Rebuild();
            },815,32,416,68,27);
            var opacity=OwnerUiLayout.Text(adjustments,"Opacity","OPACITY",28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(opacity.rectTransform,36,192,190,58);
            var opacitySlot=OwnerUiLayout.Rect(adjustments,"OpacityControl");OwnerUiLayout.Place(opacitySlot,227,181,533,78);
            _opacitySlider=OwnerSettingsRows.Slider(opacitySlot,"TouchOpacity",TouchLayoutStore.Opacity,TouchLayoutStore.MinOpacity,TouchLayoutStore.MaxOpacity,
                v=>TouchLayoutStore.Opacity=v,v=>Mathf.RoundToInt(v*100)+"%");
            var size=OwnerUiLayout.Text(adjustments,"Size","SIZE",28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(size.rectTransform,849,192,140,58);
            var sizeSlot=OwnerUiLayout.Rect(adjustments,"SizeControl");OwnerUiLayout.Place(sizeSlot,1020,181,533,78);
            _sizeSlider=OwnerSettingsRows.Slider(sizeSlot,"TouchSize",TouchLayoutStore.Scale,TouchLayoutStore.MinScale,TouchLayoutStore.MaxScale,
                v=>TouchLayoutStore.Scale=v,v=>Mathf.RoundToInt(v*100)+"%");
            adjustments.gameObject.SetActive(false);ScreenFocus.Install(bar.gameObject).Rebuild();
        }
    }
}
