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
        private Text _warningText, _warningReason;
        private HudCard _warningPlate, _reasonPlate;
        private CharacterMotor _warningOwner;
        private string _recentWarning, _warningWords;
        private float _warningUntil, _titleWidth, _reasonWidth, _warningShookAt = -10;
        /// <summary>The exact refusal sentence the rules produced, whatever the two lines draw.</summary>
        public string WarningText => _warningRoot != null && _warningRoot.gameObject.activeSelf ? _warningWords ?? "" : "";
        private const float WarningTitleHeight = 48, WarningReasonHeight = 38, WarningInset = 7, TextSlack = 12;

        // ⚠️ UI REVAMP 2026-10-06 (guide p.12): ONE RED TOY TILE. The short title sits on the red
        // face and the reason in a darker band inset into the same tile, so the pair can never be
        // read apart, hidden behind one another or left floating outlined over the sky. The words
        // are the rule's own sentence split at its dash and set in sentence case; nothing is
        // reworded. Widths are measured once per sentence, with slack, because a box exactly as
        // wide as its measurement wraps the last word under the supersampled text renderer.
        private void BuildWarnings()
        {
            _warningRoot=OwnerUiLayout.Rect(_root,"WarningMessage");
            Pin(_warningRoot,new Vector2(.5f,.66f),Vector2.zero,new Vector2(900,WarningTitleHeight+WarningReasonHeight+WarningInset));
            _warningPlate=OwnerUiLayout.Rect(_warningRoot,"WarningTitlePlate").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(_warningPlate.rectTransform);
            _warningPlate.Toy(HudDraw.Alarm,HudDraw.AlarmSide,6,12,.4f).raycastTarget=false;_warningPlate.FollowContrast=false;
            _reasonPlate=OwnerUiLayout.Rect(_warningRoot,"WarningReasonPlate").gameObject.AddComponent<HudCard>();
            _reasonPlate.Toy(HudDraw.AlarmDeep,Color.clear,0,8,0).raycastTarget=false;_reasonPlate.Sheen=false;_reasonPlate.FollowContrast=false;
            _warningText=OwnerUiLayout.Text(_warningRoot,"WarningText","",38,OwnerUiLayout.TypeRole.Reading);
            _warningText.color=Color.white;_warningText.alignment=TextAnchor.MiddleCenter;
            _warningText.horizontalOverflow=HorizontalWrapMode.Wrap;_warningText.verticalOverflow=VerticalWrapMode.Overflow;
            _warningReason=OwnerUiLayout.Text(_reasonPlate.transform,"WarningReason","",28,OwnerUiLayout.TypeRole.Reading);
            _warningReason.color=new Color32(255,226,220,255);_warningReason.alignment=TextAnchor.MiddleCenter;
            _warningReason.horizontalOverflow=HorizontalWrapMode.Wrap;_warningReason.verticalOverflow=VerticalWrapMode.Overflow;
            _warningRoot.gameObject.SetActive(false);
        }

        private static string WarningSentence(string words)
            => string.IsNullOrEmpty(words)?"":char.ToUpperInvariant(words[0])+words.Substring(1).ToLowerInvariant();

        private void LayoutWarning()
        {
            float limit=Mathf.Max(260,Mathf.Min(_statusPromptMaxWidth,1000));
            bool reason=!string.IsNullOrEmpty(_warningReason.text);
            float width=Mathf.Min(limit,Mathf.Max(_titleWidth,_reasonWidth)+56);
            OwnerUiLayout.Place(_warningText.rectTransform,20,0,width-40,WarningTitleHeight);
            float titleHeight=Mathf.Max(WarningTitleHeight,_warningText.preferredHeight+10);
            _warningText.rectTransform.sizeDelta=new Vector2(width-40,titleHeight);
            if(_reasonPlate.gameObject.activeSelf!=reason)_reasonPlate.gameObject.SetActive(reason);
            float reasonHeight=0;
            if(reason)
            {
                OwnerUiLayout.Place(_reasonPlate.rectTransform,WarningInset,titleHeight,width-WarningInset*2,WarningReasonHeight);
                OwnerUiLayout.Place(_warningReason.rectTransform,14,0,width-WarningInset*2-28,WarningReasonHeight);
                reasonHeight=Mathf.Max(WarningReasonHeight,_warningReason.preferredHeight+6);
                _reasonPlate.rectTransform.sizeDelta=new Vector2(width-WarningInset*2,reasonHeight);
                _warningReason.rectTransform.sizeDelta=new Vector2(width-WarningInset*2-28,reasonHeight);
            }
            _warningRoot.sizeDelta=new Vector2(width,titleHeight+(reason?reasonHeight+WarningInset:4));
        }

        /// <summary>One measurement per sentence, without wrapping, plus slack.</summary>
        private static float Measure(Text text)
        {
            var mode=text.horizontalOverflow;text.horizontalOverflow=HorizontalWrapMode.Overflow;
            float width=text.preferredWidth;text.horizontalOverflow=mode;
            return string.IsNullOrEmpty(text.text)?0:Mathf.Ceil(width)+TextSlack;
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
                        if(Confinement.IsInsideBox(local.transform.position.x,local.transform.position.z))
                            refusal="CANNOT THROW - MUST BE OUTSIDE DANGER ZONE";
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
            if(_warningWords!=text)
            {
                _warningWords=text;_warningShookAt=Time.unscaledTime;
                int split=text.IndexOf(" - ",System.StringComparison.Ordinal);
                _warningText.text=WarningSentence(split>=0?text.Substring(0,split):text);
                _warningReason.text=split>=0?WarningSentence(text.Substring(split+3)):"";
                _titleWidth=Measure(_warningText);_reasonWidth=Measure(_warningReason);
            }
            LayoutWarning();
        }
    }
}
