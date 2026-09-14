using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerHomeView
    {
        public static Canvas Build(Transform owner,Action settings,Action credits)
        {
            var canvas=OwnerUiLayout.Canvas(owner,"OwnerHomeCanvas",100);
            OwnerUiBackdrop.Build(canvas.transform);
            var design=OwnerUiLayout.DesignArea(canvas.transform,"OwnerHomeComposition");
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,695,75,530,530*273f/407f);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            var play=OwnerPaintedAction.Create(design,"StartButton","PLAY",()=>SceneFlow.Go(SceneFlow.ModeSelect),false,64);
            OwnerUiLayout.Place((RectTransform)play.transform,755,486,413,91);
            var learn=OwnerPaintedAction.Create(design,"TutorialButton","LEARN TO PLAY",SceneFlow.StartTraining,true,39);
            OwnerUiLayout.Place((RectTransform)learn.transform,755,614,413,91);
            Link(design,"SettingsButton","SETTINGS",settings,755,749,413,70,40);
            Link(design,"CreditsButton","CREDITS",credits,694,864,230,62,31);
            Link(design,"QuitButton","QUIT",SceneFlow.Quit,1008,864,220,62,31);
            var version=OwnerUiLayout.Text(design,"GameVersion","",22,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(version.rectTransform,700,1000,520,40);version.alignment=TextAnchor.MiddleCenter;
            version.color=OwnerUiTheme.Current.ActionInk;GameVersion.ApplyTo(version);
            canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();return canvas;
        }
        private static void Link(Transform parent,string name,string words,Action action,float x,float y,float width,float height,int size)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,x,y,width,height);
            var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
            var button=root.gameObject.AddComponent<OwnerTextAction>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;
            button.onClick.AddListener(()=>{MenuSfx.Click();action();});
            var text=OwnerUiLayout.Text(root,"Label",words,size,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(text.rectTransform);text.alignment=TextAnchor.MiddleCenter;text.color=OwnerUiTheme.Current.ActionInk;
            text.gameObject.AddComponent<OwnerUiMotion>();
        }
    }
}
