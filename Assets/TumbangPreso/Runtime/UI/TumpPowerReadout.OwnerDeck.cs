using TumbangPreso.Abilities;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPowerReadout
    {
        private readonly OwnerAbilitySeal[] _ownerDials=new OwnerAbilitySeal[3];
        private readonly Image[] _ownerKeycaps=new Image[3];
        public void Build(Transform root)
        {
            _deck=OwnerUiLayout.Rect(root,"PowerSeals");
            _deck.anchorMin=_deck.anchorMax=_deck.pivot=new Vector2(.5f,0);_deck.anchoredPosition=new Vector2(0,34);_deck.sizeDelta=new Vector2(470,212);
            for(int i=0;i<3;i++)
            {
                float size=i==2?116:98;
                _ownerDials[i]=OwnerUiLayout.Rect(_deck,"Power"+i).gameObject.AddComponent<OwnerAbilitySeal>();_ownerDials[i].raycastTarget=false;
                OwnerUiLayout.Place(_ownerDials[i].rectTransform,30+i*144,i==2?0:18,size,size);
                _symbols[i]=OwnerUiLayout.Rect(_ownerDials[i].transform,"PowerIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                OwnerUiLayout.Fill(_symbols[i].rectTransform);_symbols[i].rectTransform.offsetMin=new Vector2(21,21);_symbols[i].rectTransform.offsetMax=new Vector2(-21,-21);_symbols[i].raycastTarget=false;
                _ownerKeycaps[i]=OwnerUiLayout.Rect(_deck,"KeyboardCap"+i).gameObject.AddComponent<Image>();
                _ownerKeycaps[i].color=OwnerUiTheme.Current.Pale;_ownerKeycaps[i].raycastTarget=false;OwnerUiLayout.Place(_ownerKeycaps[i].rectTransform,52+i*144,126,60,42);
                _keys[i]=OwnerUiLayout.Text(_deck,"LiveBinding"+i,"",29,OwnerUiLayout.TypeRole.Display);_keys[i].alignment=TextAnchor.MiddleCenter;
                OwnerUiLayout.Place(_keys[i].rectTransform,16+i*144,120,132,52);_keys[i].verticalOverflow=VerticalWrapMode.Overflow;
                _keyGlyphs[i]=OwnerUiLayout.Rect(_keys[i].transform,"BindingGlyph").gameObject.AddComponent<Image>();
                _keyGlyphs[i].preserveAspect=true;_keyGlyphs[i].raycastTarget=false;OwnerUiLayout.Fill(_keyGlyphs[i].rectTransform);
                _keyGlyphs[i].rectTransform.offsetMin=new Vector2(5,5);_keyGlyphs[i].rectTransform.offsetMax=new Vector2(-5,-5);
                _states[i]=OwnerUiLayout.Text(_ownerDials[i].transform,"PowerState","",28,OwnerUiLayout.TypeRole.Display);
                _states[i].color=OwnerUiTheme.Current.Pale;_states[i].alignment=TextAnchor.MiddleCenter;OwnerUiLayout.Fill(_states[i].rectTransform);
            }
            _hint=OwnerUiLayout.Text(_deck,"PowerInfoBinding","",28);_hint.color=OwnerUiTheme.Current.Pale;_hint.alignment=TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(_hint.rectTransform,-70,173,590,49);
            var outline=_hint.gameObject.AddComponent<Outline>();outline.effectColor=OwnerUiTheme.Current.DeepInk;outline.effectDistance=new Vector2(1,-1);
            BuildDetails(root);
            var asset=Resources.Load<InputActionAsset>("TumbangPreso");_inspect=asset?.FindActionMap("Player",false)?.FindAction("AbilityInfo",false);_inspect?.Enable();
        }
        public void Tick(HeroAbilitySystem system,bool visible)
        {
            var kit=system!=null?system.Kit:null;visible&=kit!=null;_deck.gameObject.SetActive(visible);
            if(!visible){_detail.gameObject.SetActive(false);return;}
            _skills[0]=kit.Skill1;_skills[1]=kit.Skill2;_skills[2]=kit.Ultimate;var theme=OwnerUiTheme.Current;
            for(int i=0;i<3;i++)
            {
                var skill=_skills[i];if(skill==null)continue;
                if(_symbols[i].Glyph!=skill.Glyph){_symbols[i].Glyph=skill.Glyph;_symbols[i].SetVerticesDirty();}
                bool ready=!kit.PracticeMode && (i==2?kit.IsUltimateReady:skill.IsReady);
                float ratio=i==2?kit.UltimateRatio:skill.IsActive?skill.DurationRatio:1-skill.CooldownRatio;
                _ownerDials[i].State(ratio,ready,skill.IsActive,i==2);_symbols[i].color=ready?theme.Lime:theme.Pale;
                string state=kit.PracticeMode?"Wait":skill.IsActive?skill.CanReactivate?"Again":skill.DurationRemaining.ToString("0.0"):
                    i==2?ready?"":Mathf.FloorToInt(kit.UltimateRatio*100)+"%":
                    skill.UsesCharges?skill.ChargesRemaining.ToString():skill.CooldownRemaining>0?AbilityDeckHud.CooldownLabel(skill.CooldownRemaining):"";
                var slot=i==0?HeroAbilitySystem.Slot.Skill1:i==1?HeroAbilitySystem.Slot.Skill2:HeroAbilitySystem.Slot.Ultimate;
                if(system.SecondsSinceAnswer(slot)<.8f)
                {
                    var answer=system.LastAnswer(slot);
                    if(answer==HeroKit.CastOutcome.CannotAct)state="Wait";
                    else if(answer==HeroKit.CastOutcome.NoCharge)state="Empty";
                    else if(answer==HeroKit.CastOutcome.NotYet)state="Not yet";
                }
                _states[i].text=state;_symbols[i].canvasRenderer.SetAlpha(string.IsNullOrEmpty(state)?1:.25f);
                string binding=Hud.KeyLabelFor(Actions[i]);_keys[i].text=Hud.OnTouch?"":binding;
                bool pad=LastInputDevice.Current==InputDeviceKind.Gamepad;
                _keyGlyphs[i].sprite=pad?InputGlyphs.For(binding.ToUpperInvariant(),true):null;_keyGlyphs[i].enabled=_keyGlyphs[i].sprite!=null;
                _ownerKeycaps[i].gameObject.SetActive(!Hud.OnTouch && !pad && binding.Length<=3);
                _keys[i].color=_keyGlyphs[i].enabled?Color.clear:_ownerKeycaps[i].gameObject.activeSelf?theme.ActionInk:theme.Pale;
            }
            if(kit.IsUltimateReady && !kit.PracticeMode && !_ultimateReady)GameServices.Audio?.PlayUi("sfx_super_ready");
            _ultimateReady=kit.IsUltimateReady && !kit.PracticeMode;
            _hint.text=Hud.OnTouch?"Hold info for skills":"Hold "+Hud.KeyLabelFor("AbilityInfo")+" for skills";
            bool held=_captureReference || (_inspect!=null && _inspect.IsPressed());_detail.gameObject.SetActive(held);if(held)Describe(kit,_skills);
        }
    }
}
