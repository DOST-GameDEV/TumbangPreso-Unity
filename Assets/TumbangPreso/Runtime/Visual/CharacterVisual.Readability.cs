using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // V2: identity remains the authored body/seat. It is a render override, never recorded body state.
    // The Defense rim says "the taya can tag this body right now" on EVERY gameplay camera (owner, 2026-10-23:
    // "think abt how it should look like too for spectators, and other players"), Pac-Man style: the taggable
    // body itself fills with the colour from the feet up, and drains before it flips (`TagExposure`). It was the taya's camera only, and
    // read `IsTaggable` without the can, so it said "catchable" while the can was down.
    public sealed partial class CharacterVisual
    {
        private sealed class ReadScope
        {
            public Camera Camera;
            public readonly List<Renderer> Surfaces=new List<Renderer>();
            public readonly List<MaterialPropertyBlock> Saved=new List<MaterialPropertyBlock>();
        }
        private readonly List<ReadScope> _readScopes=new List<ReadScope>();
        private readonly Stack<ReadScope> _readPool=new Stack<ReadScope>();
        private CharacterMotor _readActor;
        /// <summary>How far a taggable body's colour goes to the taya's blue, before the throb.</summary>
        public const float TintStrength=.68f;
        /// <summary>How much thicker a taggable body's outline is drawn.</summary>
        public const float OutlineGrowth=2.2f;
        private MaterialPropertyBlock _readBlock;
        private void OnEnable()
        { Camera.onPreCull+=BeginReadability;Camera.onPostRender+=EndReadability; }
        private void OnDisable()
        {
            Camera.onPreCull-=BeginReadability;Camera.onPostRender-=EndReadability;
            for(int i=_readScopes.Count-1;i>=0;i--)RestoreReadability(_readScopes[i]);
            _readScopes.Clear();
        }
        /// <summary>The taya's own camera, looking at a seat a touch would land on now. The nameplate brackets.</summary>
        public static bool CatchableFor(Camera camera,CharacterMotor target)
        {
            var viewer=CourtBoundaryPresentation.Viewer(camera);var round=GameServices.Round;
            return viewer!=null && viewer.IsDefender && target!=null && viewer!=target &&
                round!=null && round.PlayerAt(target.PlayerSlot)==target && round.CanBeTaggedNow(target);
        }
        /// <summary>How far up <paramref name="target"/>'s body the tag fill reaches on <paramref name="camera"/>, 0 to 1.</summary>
        public static float FillFor(Camera camera,CharacterMotor target)
        {
            var round=GameServices.Round;
            // Gameplay views only: previews and reflection probes keep the authored body.
            if(camera==null || camera.cameraType!=CameraType.Game || target==null || round==null ||
               round.PlayerAt(target.PlayerSlot)!=target)return 0;
            return TagExposure.Fill(target);
        }
        private void BeginReadability(Camera camera)
        {
            if(_readActor==null)_readActor=GetComponent<CharacterMotor>();
            var round=GameServices.Round;
            if(_readActor==null || round==null || round.PlayerAt(_readActor.PlayerSlot)!=_readActor)return;
            _readBlock??=new MaterialPropertyBlock();
            var scope=_readPool.Count>0?_readPool.Pop():new ReadScope();scope.Camera=camera;
            float fill=FillFor(camera,_readActor);
            // The taya wears no body outline of their own: their ring and headband say who they are (owner, 2026-10-23:
            // "we can remove the red highlight sa body ng taya"). `_HunterGrow` stays 0 so the toon shader draws them plain.
            // Body cues stay on in the air, over a void and in water (the floor marks are what go); only a cutscene or a
            // replay on this screen fades them (`TagExposure.Presence`, owner: "disappear during ult cutscenes").
            float presence=TagExposure.Presence;
            bool catchable=fill>.001f && presence>.01f && WorldCueProfile.Current.TayaTarget>0;
            foreach(var surface in _renderers)
            {
                if(surface==null || Model==null || !surface.transform.IsChildOf(Model.transform))continue;
                int at=scope.Surfaces.Count;
                if(at==scope.Saved.Count)scope.Saved.Add(new MaterialPropertyBlock());
                surface.GetPropertyBlock(scope.Saved[at]);scope.Surfaces.Add(surface);
                // Nested cameras start from the original block, not the outer
                // camera's personal target override.
                MaterialPropertyBlock original=scope.Saved[at];
                if(_readScopes.Count>0)
                {
                    var first=_readScopes[0];int index=first.Surfaces.IndexOf(surface);
                    if(index>=0)original=first.Saved[index];
                }
                surface.SetPropertyBlock(original);surface.GetPropertyBlock(_readBlock);
                var bodyCapsule=_readActor.GetComponent<CharacterController>();
                _readBlock.SetVector("_WorldBody",new Vector4(_readActor.transform.position.y,bodyCapsule!=null?bodyCapsule.height:1.6f,1,0));
                _readBlock.SetFloat("_DepthReadability",WorldCueProfile.Current.DistanceReadability);
                _readBlock.SetFloat("_TayaCue",catchable?1:0);
                _readBlock.SetFloat("_HunterGrow",0);
                if(catchable)
                {
                    var colour=TagExposure.Colour;
                    _readBlock.SetColor("_RimColor",colour);
                    // It PULSES once full (owner, 2026-10-22): a tag-able attacker throbs in the cue colour rather than
                    // wearing a flat fill, so the open target reads in motion and at a glance. While the fill is still
                    // climbing or draining, the moving line is the motion.
                    float throb=Settings.SettingsStore.Current.ReducedUiMotion || fill<.999f?1:.82f+.24f*Mathf.Sin(Time.unscaledTime*6.5f);
                    float weight=WorldCueProfile.Current.TayaTarget*presence;
                    _readBlock.SetFloat("_RimStrength",.72f*throb*weight);
                    // The fill climbs the body (Pac-Man's blue ghost, rising from the feet: `Toon.shader`), and the ink
                    // outline under the line turns the colour and thickens, so the silhouette reads from across the court.
                    _readBlock.SetFloat("_TayaFill",fill);
                    _readBlock.SetFloat("_TayaTint",TintStrength*throb*weight);
                    _readBlock.SetColor("_TayaInk",Color.Lerp(new Color(.02f,.02f,.03f,1),new Color(colour.r*.8f,colour.g*.8f,colour.b*.8f,1),weight));
                    _readBlock.SetFloat("_TayaOutlineGrow",Mathf.Lerp(1,OutlineGrowth,weight));
                    var capsule=_readActor.GetComponent<CharacterController>();
                    float height=capsule!=null?capsule.height:1.6f;
                    _readBlock.SetVector("_CueHeight",new Vector4(_readActor.transform.position.y,height,0,0));
                }
                surface.SetPropertyBlock(_readBlock);
            }
            _readScopes.Add(scope);
        }
        private void EndReadability(Camera camera)
        {
            for(int i=_readScopes.Count-1;i>=0;i--)if(_readScopes[i].Camera==camera)
            {var scope=_readScopes[i];_readScopes.RemoveAt(i);RestoreReadability(scope);return;}
        }
        private void RestoreReadability(ReadScope scope)
        {
            for(int i=0;i<scope.Surfaces.Count;i++)if(scope.Surfaces[i]!=null)scope.Surfaces[i].SetPropertyBlock(scope.Saved[i]);
            scope.Surfaces.Clear();scope.Camera=null;_readPool.Push(scope);
        }
    }
}
