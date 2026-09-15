using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPickerView
    {
        private Text _collectionTurnHint;
        private RectTransform _traitPanel;
        private readonly Text[] _traitLabels = new Text[3];
        private readonly Text[] _traitValues = new Text[3];
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
            var back=OwnerTextAction.Create(_root,"TumpBack","BACK",Back,53,24,166,70,30);
            back.GetComponentInChildren<Text>().color=OwnerUiTheme.Current.Pale;
            var title=OwnerUiLayout.Text(_root,"Heading","YOUR LOADOUT",66,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,72,103,670,105);title.color=OwnerUiTheme.Current.Pale;
            string[] names={"PEOPLE","CANS","SLIPPERS"};
            for(int i=0;i<3;i++)
            {
                int category=i;
                var tab=OwnerTextAction.Create(_root,"TumpCategory"+i,names[i],()=>SelectCategory(category),
                    908+i*300,115,280,79,36);
                CollectionFill(tab.transform,"SelectedCategory",OwnerUiTheme.Current.Lime,40,73,200,4);
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
            _collectionTurnHint=OwnerUiLayout.Text(_root,"RotateHint","DRAG TO TURN",28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_collectionTurnHint.rectTransform,677,896,520,50);
            _collectionTurnHint.alignment=TextAnchor.MiddleCenter;_collectionTurnHint.color=OwnerUiTheme.Current.Pale;

            var paper=OwnerUiLayout.Rect(_root,"CollectionReadingCard").gameObject.AddComponent<OwnerUiPaper>();
            paper.Style=OwnerUiPaper.Treatment.Reading;paper.raycastTarget=false;
            OwnerUiLayout.Place(paper.rectTransform,1315,248,531,683);
            CollectionFill(_root,"CardBinding",new Color32(157,87,46,255),1355,270,78,7);
            _name=OwnerUiLayout.Text(_root,"SelectedName","",60,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform,1357,294,443,125);
            _ownerOrigin=OwnerUiLayout.Text(_root,"CharacterOrigin","",29);
            _ownerOrigin.color=OwnerUiTheme.Current.EnteredInk;
            _description=OwnerUiLayout.Text(_root,"Description","",31);
            _description.color=OwnerUiTheme.Current.EnteredInk;_description.alignment=TextAnchor.UpperLeft;
            _ownerStoryLine=OwnerUiLayout.Text(_root,"CharacterPersonality","",30);
            _ownerStoryLine.gameObject.SetActive(false);
            _stats=OwnerUiLayout.Text(_root,"Traits","",29,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_stats.rectTransform,1359,645,440,47);
            _traitPanel=OwnerUiLayout.Rect(_root,"TraitBars");OwnerUiLayout.Place(_traitPanel,1359,701,439,220);
            for(int row=0;row<3;row++)
            {
                _traitLabels[row]=OwnerUiLayout.Text(_traitPanel,"TraitLabel"+row,"",29);
                _traitLabels[row].color=OwnerUiTheme.Current.EnteredInk;
                OwnerUiLayout.Place(_traitLabels[row].rectTransform,0,row*74,154,55);
                _traitValues[row]=OwnerUiLayout.Text(_traitPanel,"TraitValue"+row,"",29,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_traitValues[row].rectTransform,365,row*74,74,55);
                _traitValues[row].alignment=TextAnchor.MiddleRight;
                for(int pip=0;pip<5;pip++)
                    _traitPips[row,pip]=CollectionFill(_traitPanel,"Trait"+row+"Pip"+pip,Color.white,
                        165+pip*37,row*74+15,29,22);
            }
            _skills=OwnerTextAction.Create(_root,"TumpSkills","SKILLS & VARIANTS",()=>_openSkills?.Invoke(Entries[_picks[0]].Id),
                1349,694,457,86,32);
            _ownerMeet=OwnerTextAction.Create(_root,"MeetCharacter","MEET YOUR HERO",OpenOwnerStory,1349,808,457,80,30);
            CollectionFill(_root,"FooterRule",new Color32(132,92,66,255),74,960,1768,2);
            _state=OwnerUiLayout.Text(_root,"SelectionState","",30);
            OwnerUiLayout.Place(_state.rectTransform,82,980,1200,70);_state.color=OwnerUiTheme.Current.Pale;
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
            _traitPanel.gameObject.SetActive(!hero);_stats.gameObject.SetActive(!hero);
            _stats.text=_category==0?"PLAY STYLE":"HANDLING";
            OwnerUiLayout.Place(_ownerOrigin.rectTransform,1359,420,439,89);
            OwnerUiLayout.Place(_description.rectTransform,1359,hero?521:429,439,hero?148:210);
            _ownerStoryLine.gameObject.SetActive(false);
            for(int row=0;row<3;row++)
            {
                _traitLabels[row].text=labels[row];_traitValues[row].text=values[row]+"/5";
                for(int pip=0;pip<5;pip++)_traitPips[row,pip].color=pip<values[row]
                    ?OwnerUiTheme.Current.ActionInk:new Color32(203,182,151,255);
            }
        }
    }

}
