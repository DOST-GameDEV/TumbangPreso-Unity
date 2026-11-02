using TumbangPreso.Core;
using TumbangPreso.Abilities;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _warningRoot, _warningTag;
        private Text _warningText, _warningReason;
        private LiveNotice _warning;
        private CharacterMotor _warningOwner;
        private string _recentWarning, _warningWords;
        private float _warningUntil, _warningShookAt = -10, _warningLeaveAt = -10;
        /// <summary>The exact refusal sentence the rules produced, whatever the two lines draw.</summary>
        public string WarningText => _warningRoot != null && _warningRoot.gameObject.activeSelf && _warningLeaveAt < 0 ? _warningWords ?? "" : "";
        private const string CastWarningPrefix = "CANNOT CAST - ";
        private const float WarningLeaveSeconds = .22f;

        // ⚠️ LIVE HUD 2026-11-01 (`boot_match.warning`): PAINTED LETTERING, NOT A RED TILE. The short title is
        // flat-filled red lettering with its outline and slab in one darker red, one tilted mark each side,
        // a soft coral band behind at 55 per cent and the reason on a cream tag below. The words are the
        // rule's own sentence split at its dash and set in uppercase; nothing is reworded. The band pulses
        // slowly while the warning stands. A warning keeps the top spot and moments sit below it.
        private void BuildWarnings()
        {
            _warning = LiveNotice.Create(_root, "WarningMessage", 74, LiveHud.WarnFill, LiveHud.WarnInk, LiveHud.WarnInk, 9, 5, 6, LiveHud.Coral, null);
            _warningRoot = _warning.Root; _warningText = _warning.Title;
            _warningTag = LiveHud.Tag(_warningRoot, "WarningReasonPlate", "", Hub.Knit.ThinPlain, LiveHud.Maroon, 22);
            _warningTag.anchorMin = _warningTag.anchorMax = _warningTag.pivot = new Vector2(.5f, .5f);
            _warningTag.anchoredPosition = new Vector2(0, -86);
            _warningReason = _warningTag.GetComponentInChildren<Text>();
            _warningReason.name = "WarningReason";
            _warningRoot.gameObject.SetActive(false);
        }

        /// <summary>A rule refused this player just now: shown as any other refusal, and kept up while it repeats.</summary>
        public void Refuse(string sentence)
        {
            if(string.IsNullOrEmpty(sentence))return;
            _recentWarning=sentence;_warningUntil=Time.unscaledTime+1.6f;
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
            else if(local.IsDefender&&round.IsCanDownWarningActive)
                text=round.IsCanDownPenaltyActive&&local.CanAct()
                    ?"STAND THE CAN UP - LOSING POINTS":"STAND THE CAN UP";
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
            float now=Time.unscaledTime;
            if(!show)
            {
                // It leaves with a quick fade and rise, then goes.
                if(!_warningRoot.gameObject.activeSelf)return;
                if(_warningLeaveAt<0)_warningLeaveAt=now;
                float out_=LiveHud.Still?1:(now-_warningLeaveAt)/WarningLeaveSeconds;
                if(out_>=1){_warningRoot.gameObject.SetActive(false);_warningLeaveAt=-10;return;}
                _warning.Pose(now-_warningShookAt,Mathf.Clamp01(out_),true);
                return;
            }
            bool appeared=!_warningRoot.gameObject.activeSelf||_warningWords!=text||_warningLeaveAt>=0;
            _warningLeaveAt=-10;
            _warningRoot.gameObject.SetActive(true);
            if(appeared)
            {
                _warningShookAt=now;
                var audio=GameServices.Audio;
                if(!abilitySoundPlayed&&audio!=null&&audio.TryGetClip("ui_error",out _,out _))
                    audio.PlayUi("ui_error",.55f);
            }
            if(_warningWords!=text)
            {
                _warningWords=text;
                int split=text.IndexOf(" - ",System.StringComparison.Ordinal);
                _warning.SetTitle((split>=0?text.Substring(0,split):text).ToUpperInvariant());
                _warningReason.text=split>=0?text.Substring(split+3).ToUpperInvariant():"";
                _warningTag.gameObject.SetActive(split>=0);
                LiveHud.SizeTag(_warningTag,_warningReason);
            }
            _warning.Pose(now-_warningShookAt,0,true);
        }
    }
}
