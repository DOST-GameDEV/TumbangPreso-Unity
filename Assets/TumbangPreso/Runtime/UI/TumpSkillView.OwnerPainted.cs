using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSkillView
    {
        private Text _ownerHeroName,_ownerAbilityName,_ownerBody,_ownerGain,_ownerCost,_ownerUnlock,_ownerBinding;
        private Image _ownerHeroPortrait,_ownerSignature;
        private RectTransform _ownerOptions;
        private Button _ownerEquip;
        private TumpAbilitySymbol _ownerAbilityIcon;
        private readonly List<Button> _ownerTabs=new List<Button>();
        private readonly List<Button> _ownerVariants=new List<Button>();
        private string _ownerOptionsHero;
        private int _ownerOptionsSlot=-1;
        private void Build()
        {
            if(_content==null)BuildOwnerSkillSurface();
            RefreshOwnerSkill();
        }
        private void BuildOwnerSkillSurface()
        {
            OwnerUiBackdrop.Build(_canvas.transform);
            _content=OwnerUiLayout.DesignArea(_canvas.transform,"SkillsComposition");
            OwnerTextAction.Create(_content,"TumpSkillBack","BACK",Back,55,25,170,70,30);
            _ownerHeroPortrait=OwnerPortraitArt.Create(_content,"HeroPortrait","UI/portraits/"+_hero);
            OwnerUiLayout.Place(_ownerHeroPortrait.rectTransform,85,115,162,167);
            _ownerHeroName=OwnerUiLayout.Text(_content,"Heading","",60,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_ownerHeroName.rectTransform,289,146,1460,105);
            for(int i=0;i<3;i++)
            {
                int slot=i==2?0:i+1;
                var tab=OwnerTextAction.Create(_content,"TumpSkillSlot"+slot,i==2?"ULTIMATE":"SKILL "+(i+1),
                    ()=>{_slot=slot;_selected=null;Build();},315+i*455,282,419,112,34);
                var label=tab.GetComponentInChildren<Text>();OwnerUiLayout.Place(label.rectTransform,113,9,300,84);
                var icon=OwnerUiLayout.Rect(tab.transform,"Symbol").gameObject.AddComponent<TumpAbilitySymbol>();
                OwnerUiLayout.Place(icon.rectTransform,8,7,92,86);icon.color=OwnerUiTheme.Current.ActionInk;icon.raycastTarget=false;
                var stroke=OwnerUiLayout.Art(tab.transform,"SelectedSlot",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(stroke.rectTransform,114,98,242,6);_ownerTabs.Add(tab);
            }
            _ownerOptions=OwnerUiLayout.Rect(_content,"VariantChoices");OwnerUiLayout.Place(_ownerOptions,90,444,785,578);
            _ownerSignature=OwnerPortraitArt.Create(_content,"SignatureHero","UI/portraits/"+_hero);
            OwnerUiLayout.Place(_ownerSignature.rectTransform,194,460,590,524);
            var paper=OwnerUiLayout.Rect(_content,"SkillNotes").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(paper.rectTransform,960,447,866,464);paper.raycastTarget=false;
            _ownerAbilityIcon=OwnerUiLayout.Rect(_content,"AbilityPicture").gameObject.AddComponent<TumpAbilitySymbol>();
            OwnerUiLayout.Place(_ownerAbilityIcon.rectTransform,992,481,116,120);_ownerAbilityIcon.color=OwnerUiTheme.Current.ActionInk;
            _ownerAbilityIcon.raycastTarget=false;
            _ownerAbilityName=OwnerUiLayout.Text(_content,"AbilityName","",40,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_ownerAbilityName.rectTransform,1136,465,630,110);
            _ownerBinding=OwnerUiLayout.Text(_content,"Binding","",27,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_ownerBinding.rectTransform,1140,575,616,48);_ownerBinding.color=OwnerUiTheme.Current.Green;
            _ownerBody=OwnerUiLayout.Text(_content,"WhatItDoes","",29);_ownerBody.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerBody.rectTransform,1003,639,775,129);_ownerBody.alignment=TextAnchor.UpperLeft;
            _ownerGain=OwnerUiLayout.Text(_content,"Gain","",27);_ownerGain.color=OwnerUiTheme.Current.Green;
            OwnerUiLayout.Place(_ownerGain.rectTransform,1003,787,775,47);
            _ownerCost=OwnerUiLayout.Text(_content,"Tradeoff","",27);_ownerCost.color=OwnerUiTheme.Current.ActionInk;
            OwnerUiLayout.Place(_ownerCost.rectTransform,1003,839,775,47);
            _ownerUnlock=OwnerUiLayout.Text(_content,"UnlockState","",26);_ownerUnlock.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerUnlock.rectTransform,992,931,348,119);_ownerUnlock.alignment=TextAnchor.UpperLeft;
            _ownerEquip=OwnerPaintedAction.Create(_content,"TumpEquipSkill","EQUIP",()=>EquipOwnerChoice(),false,45);
            OwnerUiLayout.Place((RectTransform)_ownerEquip.transform,1393,958,413,91);
        }
        private AbilityVariant OwnerSelectedVariant()
        {
            if(_slot==0)return null;
            var settings=Settings.SettingsStore.Current;
            var options=HeroLoadoutRules.VariantsFor(_hero,_slot);
            var build=HeroBuildRules.RowFor(settings.HeroBuilds,_hero);
            return options.FirstOrDefault(v=>v.Id==_selected)??HeroBuildRules.Equipped(build,_hero,_slot,settings.AbilityChallenges)??options[0];
        }
        private void EquipOwnerChoice(){var selected=OwnerSelectedVariant();if(selected!=null)Equip(selected);}
        private void RefreshOwnerSkill()
        {
            var hero=Roster.HeroPeople.First(item=>item.Id==_hero);
            _ownerHeroName.text=hero.Name+" · SKILLS";_ownerHeroPortrait.sprite=OwnerPortraitArt.Get("UI/portraits/"+_hero);
            _ownerSignature.sprite=_ownerHeroPortrait.sprite;_ownerSignature.gameObject.SetActive(_slot==0);
            _ownerOptions.gameObject.SetActive(_slot!=0);
            var kit=HeroAbilitySystem.CreateKitFor(_hero);HeroAbility[] powers={kit.Skill1,kit.Skill2,kit.Ultimate};
            for(int i=0;i<_ownerTabs.Count;i++)
            {
                int slot=i==2?0:i+1;bool selected=slot==_slot;
                _ownerTabs[i].GetComponentInChildren<TumpAbilitySymbol>().Glyph=powers[i].Glyph;
                _ownerTabs[i].GetComponentInChildren<TumpAbilitySymbol>().SetVerticesDirty();
                _ownerTabs[i].GetComponentInChildren<Text>().color=selected?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
                _ownerTabs[i].transform.Find("SelectedSlot").gameObject.SetActive(selected);
            }
            var ability=_slot==0?kit.Ultimate:_slot==1?kit.Skill1:kit.Skill2;
            _ownerAbilityIcon.Glyph=ability.Glyph;_ownerAbilityIcon.SetVerticesDirty();
            _ownerBinding.text=Hud.KeyLabelFor(_slot==0?"Ultimate":_slot==1?"Skill1":"Skill2")+"  ·  "+(_slot==0?"ULTIMATE":"SKILL "+_slot);
            var selectedVariant=OwnerSelectedVariant();if(selectedVariant!=null)_selected=selectedVariant.Id;
            var settings=Settings.SettingsStore.Current;
            var equipped=_slot==0?null:HeroBuildRules.Equipped(HeroBuildRules.RowFor(settings.HeroBuilds,_hero),_hero,_slot,settings.AbilityChallenges);
            if(_slot!=0 && (_ownerOptionsHero!=_hero || _ownerOptionsSlot!=_slot))
            {
                foreach(var button in _ownerVariants){button.gameObject.SetActive(false);Destroy(button.gameObject);}
                _ownerVariants.Clear();_ownerOptionsHero=_hero;_ownerOptionsSlot=_slot;
                var options=HeroLoadoutRules.VariantsFor(_hero,_slot);
                for(int i=0;i<options.Count;i++)
                {
                    var option=options[i];
                    var button=OwnerTextAction.Create(_ownerOptions,"TumpVariant_"+option.Id,option.Name,
                        ()=>{_selected=option.Id;Build();},0,i*181,775,155,34);
                    var label=button.GetComponentInChildren<Text>();OwnerUiLayout.Place(label.rectTransform,130,7,620,78);label.alignment=TextAnchor.MiddleLeft;
                    var icon=OwnerUiLayout.Rect(button.transform,"AbilityPicture").gameObject.AddComponent<TumpAbilitySymbol>();
                    icon.Glyph=ability.Glyph;icon.color=OwnerUiTheme.Current.ActionInk;icon.raycastTarget=false;
                    OwnerUiLayout.Place(icon.rectTransform,8,21,102,106);
                    var status=OwnerUiLayout.Text(button.transform,"Status","",26);status.color=OwnerUiTheme.Current.EnteredInk;
                    OwnerUiLayout.Place(status.rectTransform,132,88,609,43);
                    var check=OwnerUiLayout.Art(button.transform,"SelectedVariant",OwnerUiTheme.Piece.LeftRule);
                    OwnerUiLayout.Place(check.rectTransform,132,144,242,6);_ownerVariants.Add(button);
                }
            }
            if(_slot!=0)
            {
                var options=HeroLoadoutRules.VariantsFor(_hero,_slot);
                for(int i=0;i<_ownerVariants.Count;i++)
                {
                    var option=options[i];bool chosen=selectedVariant.Id==option.Id;
                    _ownerVariants[i].GetComponentInChildren<Text>().color=chosen?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
                    _ownerVariants[i].transform.Find("SelectedVariant").gameObject.SetActive(chosen);
                    _ownerVariants[i].transform.Find("Status").GetComponent<Text>().text=equipped?.Id==option.Id?"Equipped":
                        HeroBuildRules.IsUnlocked(settings.AbilityChallenges,option)?"Available":"Locked · view challenge";
                }
            }
            _ownerAbilityName.text=selectedVariant?.Name??ability.Name;_ownerBody.text=selectedVariant?.Description??ability.Summary;
            _ownerGain.text=selectedVariant?.GainLabel??"Your hero's signature move.";_ownerCost.text=selectedVariant?.CostLabel??"";
            _ownerEquip.gameObject.SetActive(selectedVariant!=null);
            if(selectedVariant==null)_ownerUnlock.text="";
            else
            {
                bool unlocked=HeroBuildRules.IsUnlocked(settings.AbilityChallenges,selectedVariant),isEquipped=equipped?.Id==selectedVariant.Id;
                _ownerUnlock.text=unlocked?(isEquipped?"In your loadout":"Available to equip"):
                    selectedVariant.Challenge+"\n"+HeroBuildRules.ChallengeCount(settings.AbilityChallenges,selectedVariant.Id)+" / "+selectedVariant.ChallengeTarget;
                _ownerEquip.GetComponentInChildren<Text>().text=isEquipped?"EQUIPPED":unlocked?"EQUIP":"LOCKED";
                _ownerEquip.interactable=unlocked && !isEquipped;
            }
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
