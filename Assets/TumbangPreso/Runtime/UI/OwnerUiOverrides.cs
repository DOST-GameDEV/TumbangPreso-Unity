using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Explicit designer overrides only. Empty books add no traversal work.
    [DefaultExecutionOrder(1800)]
    public sealed class OwnerUiOverrides : MonoBehaviour
    {
        private OwnerUiOverrideBook _book;
        private sealed class TargetState
        {
            public RectTransform Target;
            public Vector2 Position,Size;
            public Image Replacement;
        }
        private readonly Dictionary<OwnerUiOverrideBook.Entry,TargetState> _targets=new Dictionary<OwnerUiOverrideBook.Entry,TargetState>();
        private void Awake()=>_book=Resources.Load<OwnerUiOverrideBook>("UI/owner-painted/OwnerUiOverrides");
        private void LateUpdate()
        {
            if(_book==null || _book.Entries.Count==0)return;
            foreach(var entry in _book.Entries)
            {
                if(entry==null || !entry.Enabled || entry.CanvasName!=name || string.IsNullOrEmpty(entry.ElementPath))continue;
                var target=transform.Find(entry.ElementPath) as RectTransform;if(target==null)continue;
                if(!_targets.TryGetValue(entry,out var state) || state.Target!=target)
                {
                    state=new TargetState{Target=target,Position=target.anchoredPosition,Size=target.sizeDelta};_targets[entry]=state;
                }
                if(entry.Move)target.anchoredPosition=state.Position+entry.Offset;
                if(entry.Resize && entry.Size.x>0 && entry.Size.y>0)target.sizeDelta=entry.Size;
                var text=target.GetComponent<Text>();
                if(text!=null)
                {
                    // Matching a source label preserves changing scores, names and other live data.
                    if(entry.ReplaceText && text.text==entry.ExpectedText)text.text=entry.Replacement??"";
                    if(entry.Font!=null && text.font!=entry.Font)text.font=entry.Font;
                    if(entry.FontSize>0 && text.fontSize!=entry.FontSize)text.fontSize=entry.FontSize;
                }
                var graphic=target.GetComponent<Graphic>();if(entry.ChangeInk && graphic!=null && graphic.color!=entry.Ink)graphic.color=entry.Ink;
                var paper=target.GetComponent<OwnerUiPaper>();
                if(entry.ChangePaperTreatment && paper!=null && paper.Style!=entry.PaperTreatment)
                {paper.Style=entry.PaperTreatment;paper.SetVerticesDirty();}
                var image=target.GetComponent<Image>();
                if(entry.Artwork!=null && image!=null && image.sprite!=entry.Artwork){image.sprite=entry.Artwork;image.preserveAspect=true;}
                else if(entry.Artwork!=null && image==null && graphic!=null && !(graphic is Text))
                {
                    if(state.Replacement==null)
                    {
                        state.Replacement=OwnerUiLayout.Rect(target,"DesignerArtwork").gameObject.AddComponent<Image>();
                        OwnerUiLayout.Fill(state.Replacement.rectTransform);state.Replacement.transform.SetAsFirstSibling();
                        state.Replacement.raycastTarget=graphic.raycastTarget;
                    }
                    graphic.enabled=false;state.Replacement.sprite=entry.Artwork;state.Replacement.preserveAspect=true;
                    state.Replacement.color=entry.ChangeInk?entry.Ink:Color.white;
                }
            }
        }
    }
}
