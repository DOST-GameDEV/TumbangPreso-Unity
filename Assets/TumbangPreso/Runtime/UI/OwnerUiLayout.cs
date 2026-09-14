using UnityEngine;

namespace TumbangPreso.UI
{
    // Shared layout/input plumbing only. Each view supplies its own composition
    // and deliberate control family instead of a universal generated surface.
    public static class OwnerUiLayout
    {
        public enum TypeRole { Display, Accent, Reading }
        public static RectTransform Rect(Transform parent,string name)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);return rect;
        }
        public static void Fill(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        public static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        public static RectTransform DesignArea(Transform parent,string name)
        {
            var rect=Rect(parent,name);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=OwnerUiTheme.Current.ReferenceResolution;return rect;
        }
        public static Canvas Canvas(Transform owner,string name,int order=100)
        {
            var root=Rect(null,name);var scene=owner.gameObject.scene;
            if(scene.IsValid())UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject,scene);
            root.gameObject.AddComponent<CanvasLifetime>().Bind(owner.gameObject);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            // Keep dark supplied ink colors precise in a linear-color project;
            // gamma-to-linear conversion belongs in the shader, after 8-bit vertex storage.
            canvas.vertexColorAlwaysGammaSpace=true;
            root.gameObject.AddComponent<OwnerUiCanvas>();
            canvas.overrideSorting=true;canvas.sortingOrder=order;canvas.pixelPerfect=true;
            var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=OwnerUiTheme.Current.ReferenceResolution;
            scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            InputLayer.UiInputModule.Ensure();InputLayer.ScreenFocus.Install(root.gameObject);return canvas;
        }
        public static UnityEngine.UI.Image Art(Transform parent,string name,OwnerUiTheme.Piece piece)
        {
            var image=Rect(parent,name).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=OwnerUiTheme.Current.Art(piece);image.preserveAspect=true;
            image.raycastTarget=false;image.color=Color.white;
            image.rectTransform.sizeDelta=OwnerUiTheme.SourceRect(piece).size;return image;
        }
        public static UnityEngine.UI.Text Text(Transform parent,string name,string words,int size,TypeRole role=TypeRole.Reading)
        {
            var theme=OwnerUiTheme.Current;var text=Rect(parent,name).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.text=words;text.font=role==TypeRole.Display?theme.Display:role==TypeRole.Accent?theme.Accent:theme.Reading;
            text.fontSize=size;text.fontStyle=FontStyle.Normal;text.color=theme.Ink;
            text.alignment=TextAnchor.MiddleLeft;text.alignByGeometry=true;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.supportRichText=false;return text;
        }
    }
}
