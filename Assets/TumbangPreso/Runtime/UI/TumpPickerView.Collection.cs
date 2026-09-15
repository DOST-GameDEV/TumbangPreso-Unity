using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPickerView
    {
        private Text _collectionTurnHint;
        private RectTransform _traitPanel,_readingCard,_cardBinding;
        private bool _detailsHero,_detailsEquipment,_detailsDirty;
        private float _detailsScale=-1;
        private readonly Text[] _traitLabels = new Text[3];
        private readonly Image[,] _traitPips = new Image[3,5];

        private static Image CollectionFill(Transform parent, string name, Color colour, float x,float y,float w,float h)
        {
            var image=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(image.rectTransform,x,y,w,h);image.color=colour;image.raycastTarget=false;return image;
        }
        private void Build(Transform owner)
        {
            _canvas=OwnerUiLayout.Canvas(owner,"OwnerLoadoutCanvas",700);
            var background=CollectionFill(_canvas.transform,"CollectionColourField",new Color32(55,37,31,255),0,0,1920,1080);
            OwnerUiLayout.Fill(background.rectTransform);
            _root=OwnerUiLayout.DesignArea(_canvas.transform,"LoadoutComposition");
            var back=OwnerTextAction.Create(_root,"TumpBack","",Back,53,24,166,70,30);
            var backIcon=OwnerUiGlyph.Create(back.transform,"BackIcon",OwnerUiGlyph.Mark.Back,OwnerUiTheme.Current.Pale);
            OwnerUiLayout.Place(backIcon.rectTransform,41,12,58,45);
            back.FeedbackMotion=backIcon.gameObject.AddComponent<OwnerUiMotion>();
            var title=OwnerUiLayout.Text(_root,"Heading","YOUR LOADOUT",66,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,72,103,670,105);title.color=OwnerUiTheme.Current.Pale;
            string[] categoryArt={"bayan","pasip","tsinelas"};
            for(int i=0;i<3;i++)
            {
                int category=i;
                var tab=OwnerTextAction.Create(_root,"TumpCategory"+i,"",()=>SelectCategory(category),
                    1220+i*214,77,180,123,36);
                var backing=OwnerUiLayout.Rect(tab.transform,"CategoryDisc").gameObject.AddComponent<OwnerPreviewMat>();
                OwnerUiLayout.Place(backing.rectTransform,38,0,104,104);backing.raycastTarget=false;
                var icon=OwnerPortraitArt.Create(tab.transform,"CategoryIcon","UI/portraits/"+categoryArt[i]);
                OwnerUiLayout.Place(icon.rectTransform,38,0,104,104);
                tab.FeedbackMotion=icon.gameObject.AddComponent<OwnerUiMotion>();
                CollectionFill(tab.transform,"SelectedCategory",OwnerUiTheme.Current.Lime,24,116,132,4);
                _categories.Add(tab);
            }
            CollectionFill(_root,"HeaderRule",new Color32(132,92,66,255),74,213,1768,2);

            // Separate zones: icon collection, full-height model, one reading card.
            _grid=OwnerUiLayout.Rect(_root,"OwnerRosterGrid");
            var layout=_grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
            var stageArt=OwnerUiLayout.Rect(_root,"CollectionCourtSpot").gameObject.AddComponent<LoadoutCourtSpot>();
            OwnerUiLayout.Place(stageArt.rectTransform,591,245,697,665);stageArt.raycastTarget=false;
            _stage=OwnerUiLayout.Rect(_root,"ModelStage");OwnerUiLayout.Place(_stage,574,232,725,690);
            var shadow=OwnerUiLayout.Rect(_stage,"GroundShadow").gameObject.AddComponent<OwnerPreviewMat>();
            shadow.Shadow=true;shadow.raycastTarget=false;
            _preview=_stage.gameObject.AddComponent<ModelPreview>();_preview.Attach(_stage);_preview.CentreSubject();
            _stage.gameObject.AddComponent<TumpPreviewPlinth>().Bind(_preview,shadow.rectTransform);
            _collectionTurnHint=OwnerUiLayout.Text(_root,"RotateHint","",28,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_collectionTurnHint.rectTransform,677,896,520,50);
            _collectionTurnHint.alignment=TextAnchor.MiddleCenter;_collectionTurnHint.color=OwnerUiTheme.Current.Pale;

            var rotate=OwnerUiGlyph.Create(_root,"RotateModelIcon",OwnerUiGlyph.Mark.Rotate,OwnerUiTheme.Current.Pale);
            OwnerUiLayout.Place(rotate.rectTransform,912,906,50,42);
            var paper=OwnerUiLayout.Rect(_root,"CollectionReadingCard").gameObject.AddComponent<OwnerUiPaper>();
            _readingCard=paper.rectTransform;
            paper.Style=OwnerUiPaper.Treatment.Reading;paper.raycastTarget=false;
            OwnerUiLayout.Place(paper.rectTransform,1315,248,531,683);
            _cardBinding=CollectionFill(_root,"CardBinding",new Color32(157,87,46,255),1355,270,78,7).rectTransform;
            _name=OwnerUiLayout.Text(_root,"SelectedName","",60,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform,1357,294,443,125);
            _ownerOrigin=OwnerUiLayout.Text(_root,"CharacterOrigin","",29);
            _ownerOrigin.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerOrigin.rectTransform,1359,420,439,89);
            _description=OwnerUiLayout.Text(_root,"Description","",31);
            _description.color=OwnerUiTheme.Current.EnteredInk;_description.alignment=TextAnchor.UpperLeft;
            OwnerUiLayout.Place(_description.rectTransform,1359,429,439,210);
            _ownerStoryLine=OwnerUiLayout.Text(_root,"CharacterPersonality","",30);
            _ownerStoryLine.gameObject.SetActive(false);
            _stats=OwnerUiLayout.Text(_root,"Traits","",35,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_stats.rectTransform,1359,645,440,47);
            _traitPanel=OwnerUiLayout.Rect(_root,"TraitBars");OwnerUiLayout.Place(_traitPanel,1359,701,439,220);
            for(int row=0;row<3;row++)
            {
                _traitLabels[row]=OwnerUiLayout.Text(_traitPanel,"TraitLabel"+row,"",29);
                _traitLabels[row].color=OwnerUiTheme.Current.EnteredInk;
                OwnerUiLayout.Place(_traitLabels[row].rectTransform,0,row*62,154,55);
                for(int pip=0;pip<5;pip++)
                    _traitPips[row,pip]=CollectionFill(_traitPanel,"Trait"+row+"Pip"+pip,Color.white,
                        165+pip*53,row*62+15,43,22);
            }
            _skills=OwnerTextAction.Create(_root,"TumpSkills","SKILLS",()=>_openSkills?.Invoke(Entries[_picks[0]].Id),
                1349,694,457,86,32);
            _ownerMeet=OwnerTextAction.Create(_root,"MeetCharacter","STORY",OpenOwnerStory,1349,808,457,80,30);
            _skills.GetComponentInChildren<Text>().font=OwnerUiTheme.Current.Display;
            _ownerMeet.GetComponentInChildren<Text>().font=OwnerUiTheme.Current.Display;
            _state=OwnerUiLayout.Text(_root,"SelectionState","",30);
            OwnerUiLayout.Place(_state.rectTransform,82,980,1200,70);_state.color=OwnerUiTheme.Current.Pale;_state.gameObject.SetActive(false);
            var use=OwnerUiLayout.Rect(_root,"TumpUseLoadout");OwnerUiLayout.Place(use,1375,980,451,82);
            var face=use.gameObject.AddComponent<Image>();face.color=OwnerUiTheme.Current.Lime;
            _use=use.gameObject.AddComponent<Button>();_use.targetGraphic=face;
            var words=OwnerUiLayout.Text(use,"Label","USE LOADOUT",37,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(words.rectTransform);words.alignment=TextAnchor.MiddleCenter;words.color=OwnerUiTheme.Current.Green;
            _use.onClick.AddListener(()=>{MenuSfx.Click();_confirm?.Invoke((int[])_picks.Clone());});
        }

        private Button BuildCollectionChoice(int index,string id,string name)
        {
            var root=OwnerUiLayout.Rect(_grid,"Portrait_"+id);
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var button=root.gameObject.AddComponent<CollectionChoice>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            button.onClick.AddListener(()=>{MenuSfx.Click();_picks[_category]=index;Refresh();});
            var face=OwnerUiLayout.Rect(root,"PortraitCard").gameObject.AddComponent<Image>();OwnerUiLayout.Fill(face.rectTransform);
            face.raycastTarget=false;button.Face=face;
            OwnerPortraitArt.Create(face.transform,"Portrait","UI/portraits/"+id);
            button.Check=OwnerUiGlyph.Create(face.transform,"SelectedCheck",OwnerUiGlyph.Mark.Check,OwnerUiTheme.Current.Green);
            return button;
        }

        private void PlaceCollection()
        {
            bool many=Entries.Count>6;
            var layout=_grid.GetComponent<GridLayoutGroup>();layout.constraintCount=many?3:2;
            layout.cellSize=new Vector2(many?150:228,many?153:215);
            layout.spacing=new Vector2(16,18);
            OwnerUiLayout.Place(_grid,74,258,488,670);
            foreach(var choice in _choices)
            {
                var face=choice.transform.Find("PortraitCard");
                OwnerUiLayout.Place((RectTransform)face.Find("Portrait"),many?12:26,many?14:19,many?126:176,many?126:176);
                OwnerUiLayout.Place((RectTransform)face.Find("SelectedCheck"),many?116:190,8,26,26);
            }
        }
        private void RefreshCollectionDetails(bool hero,string[] labels,int[] values)
        {
            _detailsEquipment=_category>0;
            _traitPanel.gameObject.SetActive(_detailsEquipment);_stats.gameObject.SetActive(_detailsEquipment);
            _stats.text="HANDLING";
            _detailsHero=hero;_detailsDirty=true;
            _ownerStoryLine.gameObject.SetActive(false);
            if(!_detailsEquipment)return;
            for(int row=0;row<3;row++)
            {
                _traitLabels[row].text=labels[row];
                for(int pip=0;pip<5;pip++)_traitPips[row,pip].color=pip<values[row]
                    ?OwnerUiTheme.Current.ActionInk:new Color32(203,182,151,255);
            }
        }
        private void LateUpdate()
        {
            if(_canvas==null || !_canvas.gameObject.activeInHierarchy || _readingCard==null)return;
            if(!_detailsDirty && Mathf.Approximately(_detailsScale,_canvas.scaleFactor))return;
            _detailsDirty=false;_detailsScale=_canvas.scaleFactor;
            // Hug the actual paragraph. Fixed blank description rows made short
            // biographies look disconnected from their own handling information.
            float nameHeight=Mathf.Max(125,_name.preferredHeight+20);
            float descriptionHeight=Mathf.Max(65,_description.preferredHeight+20);
            float originHeight=_detailsHero?Mathf.Max(52,_ownerOrigin.preferredHeight+14):0;
            float body=28+nameHeight+10+descriptionHeight;
            if(_detailsHero)body+=originHeight+12+24+80;
            else if(_detailsEquipment)body+=18+55+10+186;
            float height=body+18+82+24;
            float top=Mathf.Max(238,590-height*.5f);
            OwnerUiLayout.Place(_readingCard,1315,top,531,height);
            OwnerUiLayout.Place(_cardBinding,1355,top+18,78,7);
            float y=top+28;
            OwnerUiLayout.Place(_name.rectTransform,1357,y,443,nameHeight);y+=nameHeight+10;
            if(_detailsHero){OwnerUiLayout.Place(_ownerOrigin.rectTransform,1359,y,439,originHeight);y+=originHeight+12;}
            OwnerUiLayout.Place(_description.rectTransform,1359,y,439,descriptionHeight);y+=descriptionHeight;
            if(_detailsHero)
            {
                y+=24;OwnerUiLayout.Place((RectTransform)_skills.transform,1359,y,212,80);
                OwnerUiLayout.Place((RectTransform)_ownerMeet.transform,1586,y,212,80);y+=80;
            }
            else if(_detailsEquipment)
            {
                y+=18;OwnerUiLayout.Place(_stats.rectTransform,1359,y,439,55);y+=65;
                OwnerUiLayout.Place(_traitPanel,1359,y,439,186);y+=186;
            }
            OwnerUiLayout.Place((RectTransform)_use.transform,1359,y+18,439,82);
        }
    }

}
