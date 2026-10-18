using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The title screen, as she redrew it on 2026-09-18: her street, her graffiti,
    /// and one line of text. Nothing else.
    ///
    /// ⚠️⚠️ THE FOUR PENNANT BUTTONS AND THE FLOATING LOGO ARE GONE ON THE OWNER'S
    /// INSTRUCTION AND THIS IS THE WHOLE SCREEN NOW. 🧑 2026-09-18: *"MAIN menu is
    /// getting revamped it will lose all buttons and will just have a tap to play
    /// or wtv text is"*. The logo is not missing: it is the graffiti painted on
    /// the wall in the plate, which is why the plate changed at the same time.
    ///
    /// ⚠️⚠️ AND TUTORIAL, SETTINGS, QUIT AND CREDITS DO NOT REAPPEAR SOMEWHERE
    /// ELSE, ALSO ON HIS INSTRUCTION. Asked where their doors should go, 🧑:
    /// *"throw them away gang no need we hhave new plan for main menu which we
    /// will edit next time"*. So this screen deliberately fails § 6.3's "every
    /// destination has a visible door" for those four, as a stated interim state
    /// rather than an oversight. **Nothing was deleted**: `ConvertedSettingsPanel`,
    /// `TumpCreditsView`, `SceneFlow.StartTraining` and `SceneFlow.Quit` are all
    /// still built and still wired into `ConvertedMainMenu`, so restoring a door
    /// is adding a call here and not rebuilding a feature. That is `docs/TODO.md`
    /// § 68.3's keep-the-old-chrome rule applied to a screen instead of a scene.
    ///
    /// ⚠️ ESCAPE STILL LEAVES THE GAME, BECAUSE A TITLE SCREEN WITH NO WAY OUT IS
    /// A TRAP RATHER THAN A CLEAN SCREEN. `ConvertedMainMenu.CancelTarget` is the
    /// one place that decides, and on Android the hardware BACK button arrives
    /// through the same reader (`CLAUDE.md` § 4a).
    ///
    /// ⚠️ THE WHOLE SCREEN IS THE BUTTON AND IT IS STILL CALLED `StartButton`.
    /// Three PlayMode fixtures press that name to get off this screen, and the
    /// control they are pressing genuinely is the same control; renaming it would
    /// have been a rename dressed up as a redesign.
    /// </summary>
    public static class HomeCourtView
    {
        // Measured off her own composition. Her caption's ink spans x607 to x1312
        // and y996 to y1040, so its centre lands on 959.5 against a 960 screen
        // centre and Darumadrop at 59 measures 712 units against her 706.
        //
        // ⚠️ THE BOX IS TALLER THAN THE INK ON PURPOSE. Darumadrop's line metrics
        // run past its painted glyph height, which `OwnerPaintedAction` records as
        // the reason its own captions overflow rather than truncate; an 80 unit
        // box around a 45 unit line is what stops the descender being clipped on a
        // fractional laptop canvas scale.
        private const int CaptionSize = 59;
        private static readonly Rect Caption = new Rect(607, 978, 706, 80);

        public static Canvas Build(Transform owner,Action settings,Action credits)
        {
            var canvas=OwnerUiLayout.Canvas(owner,"OwnerHomeCanvas",100);
            var paper=OwnerUiLayout.Rect(canvas.transform,"TitleArrivalPaper").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(paper.rectTransform);paper.color=new Color32(252,211,159,255);paper.raycastTarget=false;
            var background=OwnerUiLayout.Rect(canvas.transform,"OwnerMainMenuBackground");
            OwnerUiLayout.Fill(background);
            var image=background.gameObject.AddComponent<RawImage>();
            var scene=background.gameObject.AddComponent<HomeCourtScene>();
            scene.Illustration=OwnerMenuArt.Texture("main2-background");scene.Drift=0;
            image.texture=scene.Illustration;

            var design=OwnerUiLayout.DesignArea(canvas.transform,"OwnerMainMenuComposition");

            // The supplied title is the automatic Home-loading surface. Its small
            // progress rail replaces the previous full-screen continuation target.
            var rail=OwnerUiLayout.Rect(design,"LoadingProgressTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rail.rectTransform,775,1018,370,7);
            rail.color=new Color(1,1,1,.24f);rail.raycastTarget=false;
            var progress=OwnerUiLayout.Rect(design,"ActualProgress").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(progress.rectTransform,775,1018,370,7);
            progress.color=Color.white;progress.raycastTarget=false;
            progress.rectTransform.sizeDelta=new Vector2(0,7);

            canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            return canvas;
        }
    }
}
