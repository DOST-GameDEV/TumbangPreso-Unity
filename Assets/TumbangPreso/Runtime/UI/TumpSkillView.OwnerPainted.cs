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
        private void BuildPreviousPaintedSkillSurface()
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
            _ownerAbilityName=OwnerUiLayout.Text(_content,"AbilityName","",40,OwnerUiLayout.TypeRole.Display);
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
            if(!HeroLoadoutRules.SidegradesOpen || _slot!=1 && _slot!=2)return null;
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
            var kit=HeroAbilitySystem.CreateKitFor(_hero);
            var slots=kit.ScreenSlots;
            var shown=slots[GuideIndexFor(_slot,slots.Length)];
            var ability=shown.Ability;
            bool variants=HeroLoadoutRules.SidegradesOpen && shown.LoadoutSlot>0;
            _ownerSignature.sprite=_ownerHeroPortrait.sprite;_ownerSignature.gameObject.SetActive(!variants);
            _ownerOptions.gameObject.SetActive(variants);
            for(int i=0;i<_ownerTabs.Count;i++)
            {
                bool selected=GuideSlotAt(i,slots.Length)==_slot;
                _ownerTabs[i].GetComponentInChildren<TumpAbilitySymbol>().Glyph=slots[i].Ability.Glyph;
                _ownerTabs[i].GetComponentInChildren<Text>().color=selected?OwnerUiTheme.Current.Lime:OwnerUiTheme.Current.Pale;
                _ownerTabs[i].transform.Find("SelectedSlot").gameObject.SetActive(selected);
            }
            _ownerAbilityIcon.Glyph=ability.Glyph;
            _ownerBinding.text=Hud.KeyLabelFor(shown.Action)+"  ·  "+shown.Label;
            if(!variants)
            {
                _ownerAbilityName.text=ability.Name;
                _ownerBody.text=ability.Summary;
                OwnerUiLayout.Place(_ownerBody.rectTransform,820,663,976,250);
                _ownerGain.gameObject.SetActive(false);
                _ownerCost.gameObject.SetActive(false);
                _ownerUnlock.gameObject.SetActive(false);
                _ownerEquip.gameObject.SetActive(false);
                _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
                return;
            }
            OwnerUiLayout.Place(_ownerBody.rectTransform,820,663,976,130);
            _ownerGain.gameObject.SetActive(true);
            _ownerCost.gameObject.SetActive(true);
            _ownerUnlock.gameObject.SetActive(true);
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
                    _ownerVariants.Add(BuildGuideVariation(options[i],i));
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
