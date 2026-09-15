using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPickerView
    {
        private Text _ownerOrigin,_ownerStoryLine;
        private Button _ownerMeet;
        private OwnerCharacterStoryView _ownerStoryView;
        private void BuildPreviousPaintedPicker(Transform owner)
        {
            _canvas=OwnerUiLayout.Canvas(owner,"OwnerLoadoutCanvas",700);OwnerUiBackdrop.Build(_canvas.transform);
            _root=OwnerUiLayout.DesignArea(_canvas.transform,"LoadoutComposition");
            OwnerTextAction.Create(_root,"TumpBack","BACK",Back,55,25,170,70,30);
            var heading=OwnerUiLayout.Text(_root,"Heading","YOUR LOADOUT",62,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(heading.rectTransform,83,121,1040,98);
            var logo=OwnerUiLayout.Art(_root,"OwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,1610,26,220,220*273f/407);
            for(int i=0;i<3;i++)
            {
                int category=i;string[] words={"PEOPLE","CANS","SLIPPERS"};string[] art={"bayan","pasip","tsinelas"};
                var button=OwnerTextAction.Create(_root,"TumpCategory"+i,words[i],()=>SelectCategory(category),83+i*285,244,266,86,29);
                var portrait=OwnerPortraitArt.Create(button.transform,"CategoryPicture","UI/portraits/"+art[i]);
                OwnerUiLayout.Place(portrait.rectTransform,3,0,84,80);
                var text=button.GetComponentInChildren<Text>();OwnerUiLayout.Place(text.rectTransform,89,7,177,66);
                var rule=OwnerUiLayout.Art(button.transform,"SelectedCategory",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(rule.rectTransform,14,79,242,6);_categories.Add(button);
            }
            _grid=OwnerUiLayout.Rect(_root,"OwnerRosterGrid");OwnerUiLayout.Place(_grid,83,368,844,674);
            var layout=_grid.gameObject.AddComponent<GridLayoutGroup>();layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount=4;layout.cellSize=new Vector2(194,206);layout.spacing=new Vector2(20,20);
            _name=OwnerUiLayout.Text(_root,"SelectedName","",53,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform,1025,216,786,86);_name.alignment=TextAnchor.MiddleCenter;
            _stage=OwnerUiLayout.Rect(_root,"ModelStage");OwnerUiLayout.Place(_stage,1041,305,755,405);
            var mat=OwnerUiLayout.Rect(_stage,"PortraitMat").gameObject.AddComponent<OwnerPreviewMat>();
            OwnerUiLayout.Place(mat.rectTransform,126,6,504,390);mat.raycastTarget=false;
            var shadow=OwnerUiLayout.Rect(_stage,"GroundShadow").gameObject.AddComponent<OwnerPreviewMat>();
            shadow.Shadow=true;shadow.raycastTarget=false;
            _preview=_stage.gameObject.AddComponent<ModelPreview>();_preview.Attach(_stage);_preview.CentreSubject();
            _stage.gameObject.AddComponent<TumpPreviewPlinth>().Bind(_preview,shadow.rectTransform);
            var instructions=OwnerUiLayout.Text(_root,"RotateHint","Drag to turn",24,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(instructions.rectTransform,1575,669,208,45);instructions.alignment=TextAnchor.MiddleRight;
            var sheet=OwnerUiLayout.Rect(_root,"LoadoutNotes").gameObject.AddComponent<OwnerUiPaper>();sheet.Style=OwnerUiPaper.Treatment.Note;
            OwnerUiLayout.Place(sheet.rectTransform,1015,728,807,156);sheet.raycastTarget=false;
            _description=OwnerUiLayout.Text(_root,"Description","",29);_description.color=OwnerUiTheme.Current.EnteredInk;
            _description.alignment=TextAnchor.UpperLeft;OwnerUiLayout.Place(_description.rectTransform,1046,751,749,105);
            _stats=OwnerUiLayout.Text(_root,"Traits","",27,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_stats.rectTransform,1027,883,786,62);_stats.alignment=TextAnchor.MiddleCenter;
            _state=OwnerUiLayout.Text(_root,"SelectionState","",24,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_state.rectTransform,86,315,825,51);_state.color=OwnerUiTheme.Current.EnteredInk;
            _use=OwnerPaintedAction.Create(_root,"TumpUseLoadout","USE LOADOUT",()=>_confirm?.Invoke((int[])_picks.Clone()),false,42);
            OwnerUiLayout.Place((RectTransform)_use.transform,1393,955,413,91);
            _skills=OwnerTextAction.Create(_root,"TumpSkills","SKILLS",()=>_openSkills?.Invoke(Entries[_picks[0]].Id),1027,965,290,72,36);
            _ownerOrigin=OwnerUiLayout.Text(_root,"CharacterOrigin","",26);_ownerOrigin.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerOrigin.rectTransform,1025,273,786,32);_ownerOrigin.alignment=TextAnchor.MiddleCenter;
            _ownerStoryLine=OwnerUiLayout.Text(_root,"CharacterPersonality","",30);_ownerStoryLine.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerStoryLine.rectTransform,94,849,827,92);
            _ownerMeet=OwnerTextAction.Create(_root,"MeetCharacter","MEET YOUR HERO",OpenOwnerStory,94,958,730,76,32);
        }
        private void OpenOwnerStory()
        {
            if(_category!=0 || _mode!=GameMode.HeroStrike)return;
            var hero=Roster.HeroPeople[_picks[0]];if(OwnerCharacterStories.For(hero.Id)==null)return;
            if(_ownerStoryView==null)_ownerStoryView=gameObject.AddComponent<OwnerCharacterStoryView>();
            Suspend();_ownerStoryView.Open(hero.Id,hero.Name,Resume);
        }
        private void Refresh()
        {
            var entries=Entries;
            _picks[_category]=Mathf.Clamp(_picks[_category],0,Mathf.Max(0,entries.Count-1));
            if(_builtCategory!=_category || _builtMode!=_mode)
            {
                foreach(var choice in _choices){choice.gameObject.SetActive(false);Destroy(choice.gameObject);}
                _choices.Clear();_builtCategory=_category;_builtMode=_mode;
                for(int i=0;i<entries.Count;i++)
                {
                    var item=entries[i];_choices.Add(BuildCollectionChoice(i,item.Id,item.Name));
                }
            }
            for(int i=0;i<_choices.Count;i++)
            {
                bool selected=i==_picks[_category];((CollectionChoice)_choices[i]).SetPicked(selected);
            }
            for(int i=0;i<_categories.Count;i++)
            {
                _categories[i].transform.Find("SelectedCategory").gameObject.SetActive(i==_category);
                _categories[i].GetComponentInChildren<Text>().color=i==_category?OwnerUiTheme.Current.Lime:OwnerUiTheme.Current.Pale;
            }

            var picked=entries[_picks[_category]];_name.text=picked.Name;_description.text=_describe?.Invoke(picked.Id)??"";
            bool hero=_category==0 && _mode==GameMode.HeroStrike;_skills.gameObject.SetActive(hero);_stats.gameObject.SetActive(!hero);
            var story=hero?OwnerCharacterStories.For(picked.Id):null;
            _ownerOrigin.gameObject.SetActive(story!=null);_ownerMeet.gameObject.SetActive(story!=null);
            if(story!=null)
            {
                _ownerOrigin.text=story.origin;_description.text=story.shortLine;
                _ownerMeet.GetComponentInChildren<Text>().text="STORY";
            }
            string[][] traits={Array.Empty<string>(),new[]{"Reset","Rebound","Stance"},new[]{"Flight","Impact","Recovery"}};
            RefreshCollectionDetails(hero,traits[_category],_category>0?new[]{picked.Bilis,picked.Lakas,picked.Tatag}:null);
            var saved=Settings.SettingsStore.Current;
            _state.text=_picks[0]==saved.CharacterPick && _picks[1]==saved.CanPick && _picks[2]==saved.SlipperPick?"Your equipped loadout":"Previewing changes";
            var book=RosterBook.Load();var art=_category==0?book.PersonArt(_picks[0],_mode):_category==1?book.CanArt(_picks[1]):book.SlipperArt(_picks[2]);
            _preview.ShowingSlipper=_category==2;_preview.Show(art.Model,art.Clips,art.Palette,art.PetModel);
            _preview.SetTileFraming(.86f);
            PlaceCollection();
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
