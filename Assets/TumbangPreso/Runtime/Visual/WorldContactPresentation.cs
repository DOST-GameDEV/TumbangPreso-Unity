using UnityEngine;
using TumbangPreso.Core;

namespace TumbangPreso.Visual
{
    // Soft contact says grounded. The chalk mark belongs to the reset spot.
    [DefaultExecutionOrder(1700)]
    public sealed class WorldContactPresentation : MonoBehaviour
    {
        public static WorldContactPresentation Current {get;private set;}
        private readonly GroundContactVisual[] _players=new GroundContactVisual[Balance.PlayerCount];
        private readonly GroundContactVisual[] _shoes=new GroundContactVisual[Balance.PlayerCount];
        private Slipper[] _slippers;private Lata _can;
        private GroundContactVisual _canShadow,_homeMark;
        private int _markStyle;
        public bool HomeMarkVisible => _homeMark != null && _homeMark.Renderer.enabled;
        private void OnEnable(){AudioDirector.WorldCuePlayed+=ContactHeard;}
        private void OnDisable(){AudioDirector.WorldCuePlayed-=ContactHeard;}
        private void ContactHeard(string cue,Vector3 at,float pitch,float gain)
        {
            if(GameServices.Round==null || !GameServices.Round.RoundActive || GameServices.Audio?.IsInReplayMix==true)return;
            CourtContactDust.Play(cue,at);
        }
        public static WorldContactPresentation Install(Transform parent,Lata can)
        {
            var go=new GameObject("World object contacts");go.transform.SetParent(parent,false);
            var owner=go.AddComponent<WorldContactPresentation>();owner._can=can;owner.Build();return owner;
        }
        private void Build()
        {
            Current=this;
            for(int i=0;i<Balance.PlayerCount;i++)
            {_players[i]=new GroundContactVisual(transform,"Player contact "+i);_shoes[i]=new GroundContactVisual(transform,"Slipper contact "+i);}
            _canShadow=new GroundContactVisual(transform,"Can contact");_homeMark=new GroundContactVisual(transform,"Can reset mark");
            _markStyle = GroundContactVisual.MarkStyleFor(_can != null ? _can.gameObject.scene.name : gameObject.scene.name);
            _slippers=FindObjectsByType<Slipper>(FindObjectsInactive.Include);
        }
        private void LateUpdate()
        {
            var round=GameServices.Round;if(round==null || _canShadow==null)return;
            float weight=WorldCueProfile.PartOn(WorldCueProfile.PartContact)?WorldCueProfile.LightingWeight:0;
            for(int seat=0;seat<Balance.PlayerCount;seat++)
            {
                var actor=round.PlayerAt(seat);
                if(actor==null || !actor.gameObject.activeInHierarchy || actor.IsSwimming){_players[seat].Hide();continue;}
                var capsule=actor.GetComponent<CharacterController>();float radius=capsule!=null?capsule.radius:.4f;
                float feet=actor.transform.position.y+(capsule!=null?capsule.center.y-capsule.height*.5f:0);
                _players[seat].Place(new Vector3(actor.transform.position.x,feet,actor.transform.position.z),feet,Vector2.one*radius*1.14f,weight*.20f);
            }
            foreach(var contact in _shoes)contact.Hide();
            foreach(var shoe in _slippers)
            {
                if(shoe==null || !shoe.gameObject.activeInHierarchy || shoe.State!=SlipperState.Loose || shoe.SeatOfOrigin<0 || shoe.SeatOfOrigin>=_shoes.Length)continue;
                _shoes[shoe.SeatOfOrigin].Place(shoe.transform.position,shoe.transform.position.y,new Vector2(.24f,.15f),weight*.16f);
            }
            if(_can==null)return;
            float bottom=_can.transform.position.y-_can.PresentationSupportOffset;
            _canShadow.Place(_can.transform.position,bottom,new Vector2(.25f,.22f),weight*.21f);
            if (Map.ArenaIntro.StageUnbuilt) _homeMark.Hide();
            else _homeMark.PlaceCanMark(_can.Mark, _markStyle);
        }
        public static float ModelBottom(Renderer[] surfaces,float fallback)
        {
            float low=float.PositiveInfinity;
            foreach(var surface in surfaces)if(surface!=null && surface.enabled)low=Mathf.Min(low,surface.bounds.min.y);
            return float.IsPositiveInfinity(low)?fallback:low;
        }
        private void OnDestroy()
        {
            if(Current==this)Current=null;
            foreach(var contact in _players)contact?.Dispose();foreach(var contact in _shoes)contact?.Dispose();
            _canShadow?.Dispose();_homeMark?.Dispose();
        }
    }
}
