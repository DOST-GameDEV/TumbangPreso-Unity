using TumbangPreso.Core;
using TumbangPreso.Abilities;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _warningRoot;
        private Text _warningText;
        private HudCard _warningPlate;
        private CharacterMotor _warningOwner;
        private string _recentWarning;
        private float _warningUntil;
        public string WarningText => _warningRoot != null && _warningRoot.gameObject.activeSelf ? _warningText.text : "";

        private void BuildWarnings()
        {
            _warningRoot=OwnerUiLayout.Rect(_root,"WarningMessage");
            Pin(_warningRoot,new Vector2(.5f,.66f),Vector2.zero,new Vector2(620,90));
            _warningPlate=_warningRoot.gameObject.AddComponent<HudCard>();
            _warningPlate.color=CourtPresentationPalette.DeepRed;_warningPlate.Radius=18;_warningPlate.raycastTarget=false;
            _warningText=Ink(_warningRoot,"WarningText","",28,true);
            _warningText.horizontalOverflow=HorizontalWrapMode.Wrap;
            OwnerUiLayout.Place(_warningText.rectTransform,24,8,572,74);
            _warningRoot.gameObject.SetActive(false);
        }

        private void Warnings(CharacterMotor local,bool spectating)
        {
            if(_warningRoot==null)return;
            if(_warningOwner!=local){_warningOwner=local;_recentWarning=null;_warningUntil=0;}
            var round=GameServices.Round;
            if(local==null||spectating||round==null||!round.RoundActive||ReadyWindow||ScreenTakeover.AnyOpen)
            {_warningRoot.gameObject.SetActive(false);_warningUntil=0;return;}
            string text=null;
            if(local.IsDefender&&round.IsTayaCampWarningActive)
                text=round.IsTayaCampPenaltyActive&&local.CanAct()
                    ?"DO NOT CAMP - MOVE AWAY FROM CAN IMMEDIATELY":"DO NOT CAMP - MOVE AWAY FROM CAN";
            else if(!local.IsDefender&&!local.HoldingSlipper&&TournamentRules.IsSlipperWarning(round.AttackerIdleSeconds(local.PlayerSlot)))
                text=TournamentRules.IsSlipperPenalty(round.AttackerIdleSeconds(local.PlayerSlot))&&local.CanAct()
                    ?"DO NOT IDLE - RETRIEVE YOUR SLIPPER IMMEDIATELY":"DO NOT IDLE - RETRIEVE YOUR SLIPPER";
            if(text==null&&local.CanAct())
            {
                string refusal=null;var can=round.Lata;
                if(local.Intent.Pressed(Verb.SpecialAbility))
                {
                    if(local.IsDefender&&can!=null&&!can.IsUpright)
                        refusal="CANNOT TAG - CAN MUST BE UPRIGHT FIRST";
                    else if(!local.IsDefender&&local.HoldingSlipper)
                    {
                        if(Confinement.IsInsideBox(local.transform.position.x,local.transform.position.z,Balance.ConfinementRadius))
                            refusal="CANNOT THROW - MUST BE OUTSIDE DANGER ZONE";
                        else if(can!=null&&!can.IsUpright)refusal="CANNOT THROW - CAN MUST BE UPRIGHT FIRST";
                        else if(can!=null&&can.IsProtected)refusal="CANNOT THROW - WAIT FOR CAN BARRIER";
                    }
                }
                if(refusal==null&&local.IsDefender&&local.Intent.Pressed(Verb.Lunge)&&can!=null&&!can.IsUpright)
                    refusal="CANNOT TAG - CAN MUST BE UPRIGHT FIRST";
                if(refusal==null&&local.Intent.Pressed(Verb.Sprint)&&!local.Stamina.IsSprinting
                    &&(local.Stamina.IsFatigued||local.Stamina.Current<Balance.StaminaSprintFloor))
                    refusal="CANNOT RUN - STAMINA MUST BE REPLENISHED FIRST";
                var powers=local.AbilitySystem;
                if(refusal==null&&powers!=null)
                {
                    float newest=float.PositiveInfinity;var answer=HeroKit.CastOutcome.Missing;
                    for(int i=0;i<3;i++)
                    {
                        var slot=(HeroAbilitySystem.Slot)i;float age=powers.SecondsSinceAnswer(slot);
                        if(age<newest){newest=age;answer=powers.LastAnswer(slot);}
                    }
                    if(newest<1.25f&&answer==HeroKit.CastOutcome.Cast&&_recentWarning=="CANNOT CAST - ABILITY MUST MEET REQUIREMENTS")
                    {_recentWarning=null;_warningUntil=0;}
                    else if(newest<1.25f&&answer!=HeroKit.CastOutcome.Cast&&answer!=HeroKit.CastOutcome.Missing)
                        refusal="CANNOT CAST - ABILITY MUST MEET REQUIREMENTS";
                }
                if(refusal!=null){_recentWarning=refusal;_warningUntil=Time.unscaledTime+1.25f;}
                if(Time.unscaledTime<_warningUntil)text=_recentWarning;
            }
            bool show=!string.IsNullOrEmpty(text);
            _warningRoot.gameObject.SetActive(show);
            if(!show)return;
            _warningText.text=text;
            float width=Mathf.Clamp(_warningText.preferredWidth+48,400,620);
            _warningRoot.sizeDelta=new Vector2(width,90);
            OwnerUiLayout.Place(_warningText.rectTransform,24,8,width-48,74);
        }
    }
}
