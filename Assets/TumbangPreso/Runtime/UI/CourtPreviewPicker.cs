using System;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TumbangPreso.UI
{
    /// <summary>Local preview selection. The caller owns ballot submission and cancellation.</summary>
    public sealed class CourtPreviewPicker : MonoBehaviour
    {
        MapPreviewVideo _media;
        Text _name;
        HubButton[] _cards;
        int _candidate=-1;

        public static CourtPreviewPicker Open(Transform parent,int initial,Action<int> confirm,Action cancel)
        {
            var root=HubKit.Modal(parent,"ResultCourtPicker");
            var picker=root.gameObject.AddComponent<CourtPreviewPicker>();
            var background=HubKit.Rect(root,"SelectedCourtBackground").gameObject.AddComponent<RawImage>();
            HubKit.Stretch(background.rectTransform);background.raycastTarget=false;
            var fit=background.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
            picker._media=background.gameObject.AddComponent<MapPreviewVideo>();
            Scrim(root,"TopReadability",new Vector2(0,.72f),Vector2.one,.52f);
            Scrim(root,"BottomReadability",Vector2.zero,new Vector2(1,.38f),.76f);
            var title=HubKit.Text(root,"CourtPickerTitle","NEXT COURT",HubStyle.Display,true,HubStyle.Paper);
            HubKit.Place(title.rectTransform,HubKit.TopLeft,new Vector2(220,-56),new Vector2(1450,120));
            var back=HubKit.Button(root,"CancelCourtPreview","BACK",HubStyle.Honey,cancel,HubStyle.Label,1660);
            HubKit.Place((RectTransform)back.transform,HubKit.TopLeft,new Vector2(56,-56),new Vector2(148,100));
            var instruction=HubKit.Text(root,"CourtPickerInstruction","Browse a court, then lock your vote.",HubStyle.Label,false,HubStyle.Paper);
            HubKit.Place(instruction.rectTransform,HubKit.TopLeft,new Vector2(56,-195),new Vector2(1500,65));
            picker._name=HubKit.Text(root,"SelectedCourtName","",HubStyle.Hero,true,HubStyle.Paper);
            HubKit.Place(picker._name.rectTransform,HubKit.TopLeft,new Vector2(56,-300),new Vector2(1500,170));
            int count=SceneFlow.Maps.Length;picker._cards=new HubButton[count];
            const float width=226,gap=16;float rowWidth=count*(width+gap)-gap;
            var row=HubKit.Place(HubKit.Rect(root,"CourtChoices"),HubKit.Bottom,new Vector2(0,100),new Vector2(rowWidth,176));
            if(rowWidth>1800)row.localScale=Vector3.one*(1800/rowWidth);
            for(int i=0;i<count;i++)
            {
                int index=i;
                var card=HubKit.Button(row,"PreviewCourt"+i,null,HubStyle.Night,()=>picker.Browse(index),0,1670+i);
                picker._cards[i]=card;card.Attention+=attended=>{if(attended)picker.Browse(index);};
                HubKit.Place((RectTransform)card.transform,HubKit.TopLeft,new Vector2(i*(width+gap),0),new Vector2(width,176));
                var image=HubKit.Rect(card.Body,"CourtImage").gameObject.AddComponent<RawImage>();
                image.texture=MapPreviewVideo.PosterFor(SceneFlow.Maps[i]);image.raycastTarget=false;
                HubKit.Place(image.rectTransform,HubKit.TopLeft,new Vector2(12,-12),new Vector2(202,113.625f));
                var label=HubKit.Text(card.Body,"CourtName",SceneFlow.MapRegistry[i].Name.ToUpperInvariant(),HubStyle.Floor,true,HubStyle.Paper);
                HubKit.Place(label.rectTransform,HubKit.TopLeft,new Vector2(12,-128),new Vector2(202,36));HubKit.Fit(label);
            }
            var lockVote=HubKit.Button(root,"ConfirmCourtVote","LOCK VOTE",HubStyle.Golden,()=>confirm(picker._candidate),HubStyle.Title,1690);
            HubKit.Place((RectTransform)lockVote.transform,HubKit.BottomRight,new Vector2(-56,310),new Vector2(430,100));
            int selected=Mathf.Clamp(initial,0,count-1);picker.Browse(selected);
            // ScreenFocus preserves another live screen's selection. Opening a
            // modal explicitly transfers focus before its normal navigation runs.
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(picker._cards[selected].gameObject);
            return picker;
        }
        static void Scrim(Transform parent,string name,Vector2 min,Vector2 max,float alpha)
        {
            var image=HubKit.Rect(parent,name).gameObject.AddComponent<Image>();image.color=new Color(HubStyle.Ink.r,HubStyle.Ink.g,HubStyle.Ink.b,alpha);image.raycastTarget=false;
            HubKit.Span(image.rectTransform,min,max);
        }
        void Browse(int index)
        {
            if(index<0||index>=_cards.Length||index==_candidate)return;
            _candidate=index;_media.Show(SceneFlow.Maps[index]);_name.text=SceneFlow.MapRegistry[index].Name.ToUpperInvariant();HubKit.Fit(_name);
            for(int i=0;i<_cards.Length;i++)HubKit.SetFill(_cards[i],i==index?HubStyle.ArmyDeep:HubStyle.Night);
        }
        public void Close(){_media?.Stop();gameObject.SetActive(false);Destroy(gameObject);}
        void OnDisable(){_media?.Stop();}
    }
}
