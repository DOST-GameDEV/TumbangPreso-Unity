using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        private ModelPreview _finishPreview;
        private FinishCelebration _finishCelebration;
        private Text _finishCaption;
        private RectTransform _finishFigure;
        private TumpSymbol _finishDraw;
        private float _finishEntered;
        private readonly HudCard[] _finishTickets=new HudCard[4];
        private readonly CanvasGroup[] _finishFades=new CanvasGroup[4];
        private readonly Vector2[] _finishRowOrigins=new Vector2[4];

        private void BuildFinishFigure(RectTransform parent)
        {
            var badge=OwnerUiLayout.Rect(parent,"WinnerBanner");OwnerUiLayout.Place(badge,1190,-6,530,70);
            var brush=badge.gameObject.AddComponent<CourtPopupGraphic>();brush.Brush=true;brush.color=CourtPresentationPalette.Red;brush.raycastTarget=false;
            _finishCaption=OwnerUiLayout.Text(badge,"WinnerCaption","WINNER",34,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(_finishCaption.rectTransform);_finishCaption.alignment=TextAnchor.MiddleCenter;_finishCaption.color=CourtPresentationPalette.Paper;
            _finishFigure=OwnerUiLayout.Rect(parent,"WinnerFigure");OwnerUiLayout.Place(_finishFigure,1180,62,550,434);
            _finishPreview=_finishFigure.gameObject.AddComponent<ModelPreview>();_finishPreview.Attach(_finishFigure);
            _finishCelebration=_finishFigure.gameObject.AddComponent<FinishCelebration>();
            _finishDraw=OwnerUiLayout.Rect(parent,"DrawCan").gameObject.AddComponent<TumpSymbol>();
            OwnerUiLayout.Place(_finishDraw.rectTransform,1340,150,240,260);_finishDraw.Kind=TumpSymbol.Icon.Can;
            _finishDraw.color=CourtPresentationPalette.Red;_finishDraw.raycastTarget=false;_finishDraw.gameObject.SetActive(false);
        }

        private void PresentFinishPerformance(int winner)
        {
            if(_finishFigure==null)return;
            _finishEntered=Time.unscaledTime;
            bool hasWinner=winner>=0&&GameServices.Round?.PlayerAt(winner)!=null;
            _finishFigure.gameObject.SetActive(hasWinner);_finishDraw.gameObject.SetActive(!hasWinner);
            _finishCaption.text=hasWinner?"MATCH WINNER":"EVEN AT THE TOP";
            if(hasWinner)
            {
                var actor=GameServices.Round.PlayerAt(winner);var visual=actor.GetComponent<Visual.CharacterVisual>();
                var art=RosterBook.Load().PersonArt(actor.CharacterIndex,actor.Mode);
                _finishPreview.Show(visual!=null&&visual.SourceModel!=null?visual.SourceModel:art.Model,
                    art.Clips,visual!=null?visual.AppliedPalette:art.Palette,art.PetModel);
                _finishPreview.CentreSubject();_finishPreview.SetTileFraming(.94f);_finishCelebration.Bind(_finishPreview);
            }
            // VISUAL-1.16: the bar's chips grown. The winner is a gold border and a crown, not a
            // deep-red ticket (red means "something is wrong"); you are the gold underline.
            var order=GameServices.Match.Ranking();_accoladeOrder=order;
            int mine=LocalResultSeat();
            for(int i=0;i<4;i++)
            {
                bool best=hasWinner&&order[i]==winner;
                _finishTickets[i].Style(CourtPresentationPalette.Paper,best?CourtPresentationPalette.Gold:Color.clear,best?5:0,
                    order[i]==mine?CourtPresentationPalette.Gold:Color.clear,order[i]==mine?5:0);
                if(_finishSwatches[i]!=null)_finishSwatches[i].color=PlayerIdentity.Colour(order[i]);
                if(_finishCrowns[i]!=null)_finishCrowns[i].Show(best?HudBadge.Glyph.Crown:HudBadge.Glyph.None,CourtPresentationPalette.Gold,Color.clear);
                foreach(var cell in _rows[i])cell.color=HudDraw.CardInk;
                _rows[i][1].text=SeatLabel.WithIdentity(order[i]);
            }
            PaintAccolades(GameServices.Stats?.Last);
            TickFinishPerformance();
        }

        private static int LocalResultSeat()
        {
            if(NetAuthority.IsNetworked)return NetAuthority.LocalSlot;
            var round=GameServices.Round;if(round==null)return -1;
            foreach(var player in round.Players)if(player!=null&&!player.IsBot)return player.PlayerSlot;
            return -1;
        }

        private void TickFinishPerformance()
        {
            if(_finishFigure==null||!NativeVisible)return;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            for(int i=0;i<4;i++)
            {
                float t=reduced?1:Mathf.Clamp01((Time.unscaledTime-_finishEntered-i*.065f)/.26f);
                float enter=1-Mathf.Pow(1-t,3);
                _finishFades[i].alpha=reduced?1:Mathf.Lerp(.35f,1,t);
                _finishTickets[i].rectTransform.anchoredPosition=_finishRowOrigins[i]+Vector2.right*(22*(1-enter));
            }
        }
    }
}
