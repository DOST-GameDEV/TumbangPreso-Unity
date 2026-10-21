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
        private const float WarningTracking = .65f;
        private const string CastWarningPrefix = "CANNOT CAST - ";

        // ⚠️ UI REVAMP 2026-10-06 (guide p.12): ONE RED TOY TILE. The short title sits on the red
        // face and the reason in a darker band inset into the same tile, so the pair can never be
        // read apart, hidden behind one another or left floating outlined over the sky. The words
        // are the rule's own sentence split at its dash and set in uppercase; nothing is
        // reworded. Widths are measured once per sentence, with slack, because a box exactly as
        // wide as its measurement wraps the last word under the supersampled text renderer.
        private void BuildWarnings()
        {
            _warningRoot=OwnerUiLayout.Rect(_root,"WarningMessage");
            Pin(_warningRoot,new Vector2(.5f,.66f),Vector2.zero,new Vector2(900,WarningTitleHeight+WarningReasonHeight+WarningInset));
            _warningRoot.localScale=Vector3.one*.9f;
            _warningPlate=OwnerUiLayout.Rect(_warningRoot,"WarningTitlePlate").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(_warningPlate.rectTransform);
            // ⚠️ 2026-10-20 OVERHAUL: the same one red tile, now a flat leaning panel like every other
            // glass surface. It stays FULLY OPAQUE (the one surface in the match that does): a
            // refusal must read over anything, and `TumpNativeHudTests` holds it to alpha 1.
            _warningPlate.Glass(HudDraw.Alarm,HudDraw.Lean,3,.4f).raycastTarget=false;_warningPlate.FollowContrast=false;
            _reasonPlate=OwnerUiLayout.Rect(_warningRoot,"WarningReasonPlate").gameObject.AddComponent<HudCard>();
            _reasonPlate.Glass(HudDraw.AlarmDeep,HudDraw.Lean,2,0).raycastTarget=false;_reasonPlate.BorderWidth=0;_reasonPlate.FollowContrast=false;
            _warningText=OwnerUiLayout.Text(_warningRoot,"WarningText","",38,OwnerUiLayout.TypeRole.Reading);
            if(InGameTypography.Bold!=null)_warningText.font=InGameTypography.Bold;
            else _warningText.fontStyle=FontStyle.Bold;
            _warningText.color=Color.white;_warningText.alignment=TextAnchor.MiddleCenter;
            _warningText.horizontalOverflow=HorizontalWrapMode.Wrap;_warningText.verticalOverflow=VerticalWrapMode.Overflow;
            OwnerUiTracking.Apply(_warningText,WarningTracking,perLine:true);
            _warningReason=OwnerUiLayout.Text(_reasonPlate.transform,"WarningReason","",28,OwnerUiLayout.TypeRole.Reading);
            _warningReason.color=new Color32(255,226,220,255);_warningReason.alignment=TextAnchor.MiddleCenter;
            _warningReason.horizontalOverflow=HorizontalWrapMode.Wrap;_warningReason.verticalOverflow=VerticalWrapMode.Overflow;
            OwnerUiTracking.Apply(_warningReason,WarningTracking,perLine:true);
            _warningRoot.gameObject.SetActive(false);
        }

        private void LayoutWarning()
        {
            float limit=Mathf.Max(260,Mathf.Min(_statusPromptMaxWidth,1000));
            bool reason=!string.IsNullOrEmpty(_warningReason.text);
            float width=Mathf.Min(limit,Mathf.Max(_titleWidth,_reasonWidth)+56);
            PlaceWarningText(_warningText,20,0,width-40,WarningTitleHeight);
            float titleHeight=Mathf.Max(WarningTitleHeight,_warningText.preferredHeight+10);
            _warningText.rectTransform.sizeDelta=new Vector2(_warningText.rectTransform.sizeDelta.x,titleHeight);
            if(_reasonPlate.gameObject.activeSelf!=reason)_reasonPlate.gameObject.SetActive(reason);
            float reasonHeight=0;
            if(reason)
            {
                OwnerUiLayout.Place(_reasonPlate.rectTransform,WarningInset,titleHeight,width-WarningInset*2,WarningReasonHeight);
                PlaceWarningText(_warningReason,14,0,width-WarningInset*2-28,WarningReasonHeight);
                reasonHeight=Mathf.Max(WarningReasonHeight,_warningReason.preferredHeight+6);
                _reasonPlate.rectTransform.sizeDelta=new Vector2(width-WarningInset*2,reasonHeight);
                _warningReason.rectTransform.sizeDelta=new Vector2(_warningReason.rectTransform.sizeDelta.x,reasonHeight);
                // Both panels shear around their own centre. Move the lower band onto the
                // outer panel's slope so the visible inset stays equal on both sides.
                _reasonPlate.rectTransform.anchoredPosition=new Vector2(
                    WarningInset+(WarningInset-titleHeight)*.5f*_warningPlate.Slant,-titleHeight);
            }
            _warningRoot.sizeDelta=new Vector2(width,titleHeight+(reason?reasonHeight+WarningInset:4));
        }

        private static float TrackingWidth(Text text)=>Mathf.Max(0,text.text.Length-1)*WarningTracking;

        private static void PlaceWarningText(Text text,float x,float y,float width,float height)
        {
            // Reserve the full line's added spacing before the generator wraps. Each drawn
            // line then expands around its own centre, within the original padded area.
            float extra=TrackingWidth(text);
            OwnerUiLayout.Place(text.rectTransform,x+extra*.5f,y,Mathf.Max(1,width-extra),height);
        }

        /// <summary>One measurement per sentence, without wrapping, plus slack.</summary>
        private static float Measure(Text text)
        {
            var mode=text.horizontalOverflow;text.horizontalOverflow=HorizontalWrapMode.Overflow;
            float width=text.preferredWidth;text.horizontalOverflow=mode;
            return string.IsNullOrEmpty(text.text)?0:Mathf.Ceil(width+TrackingWidth(text))+TextSlack;
        }

        private static string CastWarning(HeroKit.CastOutcome answer,HeroKit kit)
        {
            if(answer==HeroKit.CastOutcome.Cooling)return CastWarningPrefix+"ABILITY IS ON COOLDOWN";
            if(answer==HeroKit.CastOutcome.NoCharge&&kit!=null)
            {
                int missing=Mathf.CeilToInt(Mathf.Max(0,kit.UltimateCost-kit.UltimateCharge));
                return missing>0?CastWarningPrefix+missing+" MORE OBJECTIVE POINTS REQUIRED":null;
            }
            return CastWarningPrefix+"ABILITY MUST MEET REQUIREMENTS";
        }

        private void Warnings(CharacterMotor local,bool spectating)
        {
            if(_warningRoot==null)return;
            if(_warningOwner!=local){_warningOwner=local;_recentWarning=null;_warningUntil=0;}
            var round=GameServices.Round;
            if(local==null||spectating||round==null||!round.RoundActive||ReadyWindow||ScreenTakeover.AnyOpen)
            {_warningRoot.gameObject.SetActive(false);_warningUntil=0;return;}
            string text=null;bool abilitySoundPlayed=false;
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
                    if(newest<1.25f&&answer!=HeroKit.CastOutcome.Missing)
                    {
                        refusal=answer==HeroKit.CastOutcome.Cast?null:CastWarning(answer,powers.Kit);
                        // HeroAbilitySystem already sounds a fresh refusal on the local body.
                        abilitySoundPlayed=refusal!=null&&newest<.1f&&!local.IsBot;
                        if(refusal==null&&_recentWarning!=null&&_recentWarning.StartsWith(CastWarningPrefix,System.StringComparison.Ordinal))
                        {_recentWarning=null;_warningUntil=0;}
                    }
                }
                if(refusal!=null){_recentWarning=refusal;_warningUntil=Time.unscaledTime+1.25f;}
                if(Time.unscaledTime<_warningUntil)text=_recentWarning;
            }
            PresentWarning(text,abilitySoundPlayed);
        }

        private void PresentWarning(string text,bool abilitySoundPlayed=false)
        {
            bool show=!string.IsNullOrEmpty(text);
            bool appeared=show&&(!_warningRoot.gameObject.activeSelf||_warningWords!=text);
            _warningRoot.gameObject.SetActive(show);
            if(!show)return;
            if(appeared)
            {
                _warningShookAt=Time.unscaledTime;
                var audio=GameServices.Audio;
                if(!abilitySoundPlayed&&audio!=null&&audio.TryGetClip("ui_error",out _,out _))
                    audio.PlayUi("ui_error",.55f);
            }
            if(_warningWords!=text)
            {
                _warningWords=text;
                int split=text.IndexOf(" - ",System.StringComparison.Ordinal);
                _warningText.text=(split>=0?text.Substring(0,split):text).ToUpperInvariant();
                _warningReason.text=split>=0?text.Substring(split+3).ToUpperInvariant():"";
                _titleWidth=Measure(_warningText);_reasonWidth=Measure(_warningReason);
            }
            LayoutWarning();
        }
    }
}
