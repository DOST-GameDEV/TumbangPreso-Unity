using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class SettingsOptionMenu : MonoBehaviour
    {
        public static SettingsOptionMenu OpenOption { get; private set; }
        private string[] _options;
        private int _selected;
        private Action<int> _choose;
        private Text _value;
        private Canvas _popup;
        public Button Button { get; private set; }
        public static SettingsOptionMenu Create(Transform parent,string name,string[] values,int selected,Action<int> choose)
        {
            var frame=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(frame.rectTransform,0,0,533,78); frame.color=SettingsPalette.Control;
            var menu=frame.gameObject.AddComponent<SettingsOptionMenu>();menu._options=values;
            menu._selected=Mathf.Clamp(selected,0,Mathf.Max(0,values.Length-1));menu._choose=choose;
            menu.Button=frame.gameObject.AddComponent<Button>();menu.Button.targetGraphic=frame;
            frame.gameObject.AddComponent<SettingsControlFocus>();
            menu.Button.transition=Selectable.Transition.ColorTint;menu.Button.onClick.AddListener(menu.Open);
            menu._value=OwnerUiLayout.Text(frame.transform,"SelectedValue",values.Length>0?values[menu._selected]:"",29);
            menu._value.color=SettingsPalette.Ink;OwnerUiLayout.Place(menu._value.rectTransform,25,8,428,60);
            var mark=OwnerUiGlyph.Create(frame.transform,"Expand",OwnerUiGlyph.Mark.Back,SettingsPalette.Ink);
            OwnerUiLayout.Place(mark.rectTransform,477,27,25,25);
            mark.rectTransform.pivot=new Vector2(.5f,.5f);mark.rectTransform.anchoredPosition=new Vector2(489.5f,-39.5f);
            mark.rectTransform.localRotation=Quaternion.Euler(0,0,90);
            return menu;
        }
        private void Open()
        {
            if(!Button.IsInteractable() || _options.Length==0)return;
            if(OpenOption==this){Close();return;}
            OpenOption?.Close();OpenOption=this;
            int order=GetComponentInParent<Canvas>().sortingOrder+20;
            _popup=OwnerUiLayout.Canvas(transform,"OwnerOptionCanvas",order);
            var blocker=OwnerUiLayout.Rect(_popup.transform,"OutsideChoice").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(blocker.rectTransform);blocker.color=Color.clear;
            var outside=blocker.gameObject.AddComponent<Button>();outside.targetGraphic=blocker;outside.onClick.AddListener(Close);
            var box=OwnerUiLayout.Rect(_popup.transform,"Choices");box.anchorMin=box.anchorMax=box.pivot=new Vector2(.5f,.5f);
            float height=Mathf.Min(566,Mathf.Max(108,_options.Length*72+40));box.sizeDelta=new Vector2(566,height);
            var origin=(RectTransform)transform;var corners=new Vector3[4];origin.GetWorldCorners(corners);
            var ownerCanvas=GetComponentInParent<Canvas>();var camera=ownerCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ownerCanvas.worldCamera;
            var point=RectTransformUtility.WorldToScreenPoint(camera,(corners[0]+corners[3])*.5f);
            var popupRect=(RectTransform)_popup.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRect,point,null,out var local);
            var bounds=popupRect.rect;
            box.anchoredPosition=new Vector2(Mathf.Clamp(local.x,bounds.xMin+299,bounds.xMax-299),
                Mathf.Clamp(local.y-height*.5f-8,bounds.yMin+height*.5f+18,bounds.yMax-height*.5f-18));
            var paper=box.gameObject.AddComponent<Image>();paper.color=SettingsPalette.Surface;paper.raycastTarget=true;
            var column=OwnerScrollColumn.Build(box,"ChoiceList",new Rect(22,20,522,height-40),out var scroll);
            Button selectedButton=null;
            for(int i=0;i<_options.Length;i++)
            {
                int index=i;
                var button=SettingsWorkspaceRows.Action(column,"Option"+i,_options[i],()=>Select(index),492);
                OwnerUiLayout.Place(button.GetComponentInChildren<Text>().rectTransform,18,0,456,64);
                var layout=button.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=64;
                var colours=button.colors; colours.normalColor=i==_selected?SettingsPalette.Accent:SettingsPalette.Ink; button.colors=colours;
                if(i==_selected)selectedButton=button;
            }
            _popup.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            selectedButton?.Select();
            ScreenTakeover.Register(this,()=>_popup!=null && _popup.gameObject.activeInHierarchy);
        }
        private void Select(int index)
        {
            _selected=index;_value.text=_options[index];Close();_choose?.Invoke(index);
        }
        public void Close()
        {
            if(OpenOption==this)OpenOption=null;
            if(_popup!=null){_popup.gameObject.SetActive(false);Destroy(_popup.gameObject);_popup=null;}
            ScreenTakeover.Unregister(this);
            if(Button!=null && Button.isActiveAndEnabled)Button.Select();
        }
        private void Update()
        {
            if(OpenOption!=this || !InputLayer.MenuNav.CancelPressed)return;
            ScreenTakeover.ConsumeEscape();Close();
        }
        private void OnDisable()=>Close();
    }
}
