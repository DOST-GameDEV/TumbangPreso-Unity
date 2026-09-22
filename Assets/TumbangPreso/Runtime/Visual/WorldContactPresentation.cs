using UnityEngine;
using TumbangPreso.Core;

namespace TumbangPreso.Visual
{
    // V2: soft contact says grounded; a brief dotted footprint predicts the can's
    // real falling/settling contact. No fabricated ballistic motion or rule zone.
    [DefaultExecutionOrder(1700)]
    public sealed class WorldContactPresentation : MonoBehaviour
    {
        public static WorldContactPresentation Current {get;private set;}
        private readonly GroundContactVisual[] _players=new GroundContactVisual[Balance.PlayerCount];
        private readonly GroundContactVisual[] _shoes=new GroundContactVisual[Balance.PlayerCount];
        private Slipper[] _slippers;private Lata _can;private Renderer[] _canArt;private int _canSkin;
        private GroundContactVisual _canShadow,_landing;private bool _upright;private float _fellAt=-100;
        public bool LandingVisible=>_landing!=null && _landing.Renderer.enabled;
        public static WorldContactPresentation Install(Transform parent,Lata can)
        {
            var go=new GameObject("World object contacts");go.transform.SetParent(parent,false);
            var owner=go.AddComponent<WorldContactPresentation>();owner._can=can;owner._upright=can!=null && can.IsUpright;owner.Build();return owner;
        }
        private void Build()
        {
            Current=this;
            for(int i=0;i<Balance.PlayerCount;i++)
            {_players[i]=new GroundContactVisual(transform,"Player contact "+i);_shoes[i]=new GroundContactVisual(transform,"Slipper contact "+i);}
            _canShadow=new GroundContactVisual(transform,"Can contact");_landing=new GroundContactVisual(transform,"Can settling footprint");
            _slippers=FindObjectsByType<Slipper>(FindObjectsInactive.Include);
        }
        private void LateUpdate()
        {
            var round=GameServices.Round;if(round==null || _canShadow==null)return;
            float weight=WorldCueProfile.Current.WorldLighting;
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
            if(_canArt==null || _canSkin!=_can.SkinIndex)
            {_canArt=CameraSystem.MatchReplayArchive.PropModel(_can.gameObject).GetComponentsInChildren<Renderer>(true);_canSkin=_can.SkinIndex;}
            float bottom=_can.transform.position.y-_can.PresentationSupportOffset;
            _canShadow.Place(_can.transform.position,bottom,new Vector2(.25f,.22f),weight*.21f);
            if(_upright && !_can.IsUpright)_fellAt=Time.time;_upright=_can.IsUpright;
            bool elevated=WorldGround.TryBelow(_can.transform.position,.5f,3.5f,out float floor) && bottom-floor>.12f;
            float age=Time.time-_fellAt;
            float settle=!_can.IsUpright && age>=0 && age<Balance.ToppleTime?1-age/Balance.ToppleTime:0;
            float landing=(elevated?.62f:settle*.62f)*WorldCueProfile.Current.HeroObjects;
            _landing.Place(_can.transform.position,bottom,new Vector2(.29f,.29f),landing,true);
        }
        public static float ModelBottom(Renderer[] surfaces,float fallback)
        {
            float low=float.PositiveInfinity;
            foreach(var surface in surfaces)if(surface!=null && surface.enabled)low=Mathf.Min(low,surface.bounds.min.y);
            return float.IsPositiveInfinity(low)?fallback:low;
        }
        public static float RecordedCanSupport(Renderer[] surfaces,Quaternion rotation)
        {
            float radius=.14f;
            foreach(var surface in surfaces)
            {
                if(surface==null)continue;var filter=surface.GetComponent<MeshFilter>();
                if(filter==null || filter.sharedMesh==null)continue;
                var e=filter.sharedMesh.bounds.extents;var scale=filter.transform.lossyScale;
                radius=Mathf.Max(e.x*Mathf.Abs(scale.x),e.z*Mathf.Abs(scale.z));break;
            }
            float upright=Vector3.Dot(rotation*Vector3.up,Vector3.up);
            return radius*Mathf.Sqrt(Mathf.Max(0,1-upright*upright));
        }
        private void OnDestroy()
        {
            if(Current==this)Current=null;
            foreach(var contact in _players)contact?.Dispose();foreach(var contact in _shoes)contact?.Dispose();
            _canShadow?.Dispose();_landing?.Dispose();
        }
    }
}
