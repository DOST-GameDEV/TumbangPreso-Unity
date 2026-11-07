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
        Text[] _names;
        int _candidate=-1;

        public static CourtPreviewPicker Open(Transform parent,int initial,Action<int> confirm,Action cancel)
        {
            var root=HubKit.Modal(parent,"ResultCourtPicker");
            var picker=root.gameObject.AddComponent<CourtPreviewPicker>();
            var background=HubKit.Rect(root,"SelectedCourtBackground").gameObject.AddComponent<RawImage>();
            HubKit.Stretch(background.rectTransform);background.raycastTarget=false;
            var fit=background.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
            picker._media=background.gameObject.AddComponent<MapPreviewVideo>();
            // The boot board `match-result-nextcourt` (`tools/ui_artboards/boot_record.py` `next_court`): the court's
            // own preview dimmed, red BACK and the white title, the court's name large, the lime LOCK VOTE, and every
            // court as a kit card with its picture, the one being browsed lime.
            var dim=HubKit.Rect(root,"CourtDim").gameObject.AddComponent<Image>();dim.color=new Color32(40,8,14,90);dim.raycastTarget=false;HubKit.Stretch(dim.rectTransform);
            var design=OwnerUiLayout.DesignArea(root,"CourtComposition");
            HubKnit.Pressable(design,"CancelCourtPreview","BACK",Knit.Danger,60,40,220,100,44,()=>cancel());
            var title=HubKnit.Display(design,"CourtPickerTitle","NEXT COURT",86,HubKnit.KitWhite,TextAnchor.MiddleLeft,7);
            HubKnit.In(title.rectTransform,310,36,1300,100);
            var instruction=HubKnit.Words(design,"CourtPickerInstruction","Browse a court, then lock your vote.",34,HubKnit.KitWhite,TextAnchor.MiddleLeft,true,3);
            HubKnit.In(instruction.rectTransform,62,158,1300,46);
            picker._name=HubKnit.Display(design,"SelectedCourtName","",170,HubKnit.KitWhite,TextAnchor.MiddleLeft,9);
            HubKnit.In(picker._name.rectTransform,56,342,1300,210);
            int count=SceneFlow.Maps.Length;picker._cards=new HubButton[count];picker._names=new Text[count];
            const float width=290,step=302;float rowWidth=(count-1)*step+width;
            var row=HubKnit.In(HubKit.Rect(design,"CourtChoices"),60,650,rowWidth,250+HubKnit.SlabHeight);
            if(rowWidth>1800)row.localScale=Vector3.one*(1800/rowWidth);
            for(int i=0;i<count;i++)
            {
                int index=i;
                var card=HubKnit.Pressable(row,"PreviewCourt"+i,null,Knit.Card,i*step,0,width,250+HubKnit.SlabHeight,0,()=>picker.Browse(index));
                picker._cards[i]=card;card.Attention+=attended=>{if(attended)picker.Browse(index);};
                // The picture's top left corner cut, as the board's `picture` cuts it.
                HubKnit.Picture(card.Body,"CourtImage",MapPreviewVideo.PosterFor(SceneFlow.Maps[i]),20,18,250,140,.5f,22);
                string words=SceneFlow.MapRegistry[i].Name.ToUpperInvariant();
                var label=picker._names[i]=HubKnit.Display(card.Body,"CourtName",words,words.Length<12?30:26,HubKnit.KitCream,TextAnchor.MiddleCenter);
                HubKnit.In(label.rectTransform,10,174,width-20,48);HubKit.Fit(label,width-20);
            }
            HubKnit.Pressable(design,"ConfirmCourtVote","LOCK VOTE",Knit.Go,1400,470,460,124,54,()=>confirm(picker._candidate));
            int selected=Mathf.Clamp(initial,0,count-1);picker.Browse(selected);
            // ScreenFocus preserves another live screen's selection. Opening a
            // modal explicitly transfers focus before its normal navigation runs.
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(picker._cards[selected].gameObject);
            return picker;
        }
        void Browse(int index)
        {
            if(index<0||index>=_cards.Length||index==_candidate)return;
            _candidate=index;_media.Show(SceneFlow.Maps[index]);_name.text=SceneFlow.MapRegistry[index].Name.ToUpperInvariant();_name.fontSize=170;HubKit.Fit(_name,1300);
            for(int i=0;i<_cards.Length;i++)
            {
                HubKnit.Recolour(_cards[i].transform,i==index?Knit.CardOn:Knit.Card);
                if(_names[i]!=null)_names[i].color=i==index?HubKnit.KitLime:HubKnit.KitCream;
            }
        }
        public void Close(){_media?.Stop();gameObject.SetActive(false);Destroy(gameObject);}
        void OnDisable(){_media?.Stop();}
    }
}
