using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    public sealed partial class TouchHud
    {
        private void BuildNative()
        {
            _canvas=OwnerUiLayout.Canvas(transform,"OwnerTouchCanvas",300);var root=(RectTransform)_canvas.transform;
            _lookArea=OwnerUiLayout.Rect(root,"LookArea");_lookArea.anchorMin=new Vector2(.45f,0);_lookArea.anchorMax=Vector2.one;
            _lookArea.offsetMin=_lookArea.offsetMax=Vector2.zero;
            _lookArea.gameObject.AddComponent<Image>().color=Color.clear;_lookArea.gameObject.AddComponent<TouchLookArea>();
            var disc=OwnerUiLayout.Rect(root,"MoveStick").gameObject.AddComponent<OwnerTouchDisc>();disc.color=OwnerUiTheme.Current.DeepInk;
            var stickRect=disc.rectTransform;stickRect.anchorMin=stickRect.anchorMax=Vector2.zero;stickRect.pivot=new Vector2(.5f,.5f);
            stickRect.anchoredPosition=new Vector2(StickCentreX,StickCentreY);stickRect.sizeDelta=new Vector2(StickRadius*2,StickRadius*2);
            var group=stickRect.gameObject.AddComponent<CanvasGroup>();
            var knob=OwnerUiLayout.Rect(stickRect,"Knob").gameObject.AddComponent<OwnerTouchDisc>();knob.color=OwnerUiTheme.Current.Lime;knob.raycastTarget=false;
            knob.rectTransform.anchorMin=knob.rectTransform.anchorMax=knob.rectTransform.pivot=new Vector2(.5f,.5f);
            knob.rectTransform.sizeDelta=new Vector2(StickRadius,StickRadius);
            _stick=stickRect.gameObject.AddComponent<TouchStick>();_stick.Bind(stickRect,knob.rectTransform,StickRadius,group);
            foreach(var entry in InputCatalogue.All)
            {
                float size=TouchMetrics.UnitsFor(entry.Size);
                var face=OwnerUiLayout.Rect(root,"Touch_"+entry.Verb).gameObject.AddComponent<OwnerTouchSurface>();face.Primary=Primary(entry);
                var rect=face.rectTransform;rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(size,size);Place(rect,entry);
                var opacity=face.gameObject.AddComponent<CanvasGroup>();
                var verb=OwnerUiLayout.Rect(rect,"VerbIcon").gameObject.AddComponent<TumpVerbSymbol>();verb.Verb=entry.Verb;
                verb.color=OwnerUiTheme.Current.ActionInk;verb.raycastTarget=false;Inset(verb.rectTransform,size*.2f);
                var ability=OwnerUiLayout.Rect(rect,"AbilityIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                ability.color=OwnerUiTheme.Current.ActionInk;ability.raycastTarget=false;Inset(ability.rectTransform,size*.2f);ability.gameObject.SetActive(false);
                var button=face.gameObject.AddComponent<TouchButton>();button.BindOwnerPresentation(entry,opacity,face,verb,ability);
                if(entry.Zone==TouchZone.SkillRail)
                {
                    var label=OwnerUiLayout.Text(rect,"SkillSlot",entry.Verb==Verb.Ultimate?"ULT":(entry.Slot+1).ToString(),30,OwnerUiLayout.TypeRole.Display);
                    label.alignment=TextAnchor.MiddleCenter;label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,0);
                    label.rectTransform.anchoredPosition=new Vector2(0,9);label.rectTransform.sizeDelta=new Vector2(90,48);
                }
                _buttons.Add(button);
            }
            var sandbox=OwnerTextAction.Create(root,"SandboxToggle",SandboxOffText,()=>{PracticeSandbox.Toggle();RefreshSandbox();},SandboxMargin,SandboxTopInset,SandboxWidth,SandboxHeight,30);
            var paper=OwnerUiLayout.Rect(sandbox.transform,"SandboxPaper").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Fill(paper.rectTransform);paper.raycastTarget=false;paper.transform.SetAsFirstSibling();
            _sandboxRoot=sandbox.gameObject;_sandboxLabel=sandbox.GetComponentInChildren<Text>();
            ApplyModeVisibility();ApplyLayout();RefreshSandbox();
        }
        private static void Inset(RectTransform rect,float pixels)
        {
            OwnerUiLayout.Fill(rect);rect.offsetMin=new Vector2(pixels,pixels);rect.offsetMax=new Vector2(-pixels,-pixels);
        }
    }
}
