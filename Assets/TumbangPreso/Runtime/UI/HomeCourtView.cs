using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public static class HomeCourtView
    {
        public static Canvas Build(Transform owner,Action settings,Action credits)
        {
            var canvas=OwnerUiLayout.Canvas(owner,"OwnerHomeCanvas",100);
            var background=OwnerUiLayout.Rect(canvas.transform,"OwnerMainMenuBackground");
            OwnerUiLayout.Fill(background);
            var image=background.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            var scene=background.gameObject.AddComponent<HomeCourtScene>();
            scene.Illustration=OwnerMenuArt.Texture("main-background");scene.Drift=0;
            image.texture=scene.Illustration;
            var dust=OwnerUiLayout.Rect(background,"BackgroundRoadDust").gameObject.AddComponent<OwnerRoadDust>();
            OwnerUiLayout.Fill(dust.rectTransform);dust.Background=image;dust.raycastTarget=false;
            var design=OwnerUiLayout.DesignArea(canvas.transform,"OwnerMainMenuComposition");
            var logo=OwnerMenuArt.Image(design,"OriginalOwnerLogo","main-logo");
            OwnerUiLayout.Place(logo.rectTransform,134,38,658,435);
            Action(design,"StartButton","PLAY","main-play",()=>SceneFlow.Go(SceneFlow.ModeSelect),
                new Rect(164,454,413,156),new Rect(274,497,188,57),84,new Color32(15,86,19,255));
            Action(design,"TutorialButton","TUTORIAL","main-tutorial",SceneFlow.StartTraining,
                new Rect(166,613,446,122),new Rect(243,643,286,41),63,new Color32(25,51,72,255));
            Action(design,"SettingsButton","SETTINGS","main-settings",settings,
                new Rect(165,738,497,122),new Rect(244,767,259,43),63,new Color32(150,87,22,255));
            Action(design,"QuitButton","QUIT","main-quit",SceneFlow.Quit,
                new Rect(175,862,316,123),new Rect(258,894,140,43),62,new Color32(118,21,26,255));
            canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            return canvas;
        }

        private static void Action(Transform parent,string name,string words,string art,Action click,
            Rect frame,Rect ink,int size,Color colour)
        {
            var button=OwnerPaintedAction.Create(parent,name,words,click,false,size,OwnerMenuArt.Piece(art));
            OwnerUiLayout.Place((RectTransform)button.transform,frame.x,frame.y,frame.width,frame.height);
            var label=button.GetComponentInChildren<UnityEngine.UI.Text>();label.color=colour;
            OwnerUiLayout.Place(label.rectTransform,ink.x-frame.x-24,ink.y-frame.y-30,ink.width+48,ink.height+60);
            label.alignment=TextAnchor.MiddleCenter;label.alignByGeometry=true;
        }
    }
}
