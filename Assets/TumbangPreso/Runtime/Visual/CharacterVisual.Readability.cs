using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // V2: identity remains the authored body/seat. Defense rim says catchable ONLY
    // to the actual taya camera. It is a render override, never recorded body state.
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
        private MaterialPropertyBlock _readBlock;
        private void OnEnable()
        { Camera.onPreCull+=BeginReadability;Camera.onPostRender+=EndReadability; }
        private void OnDisable()
        {
            Camera.onPreCull-=BeginReadability;Camera.onPostRender-=EndReadability;
            for(int i=_readScopes.Count-1;i>=0;i--)RestoreReadability(_readScopes[i]);
            _readScopes.Clear();
        }
        public static bool CatchableFor(Camera camera,CharacterMotor target)
        {
            var viewer=CourtBoundaryPresentation.Viewer(camera);var round=GameServices.Round;
            return viewer!=null && viewer.IsDefender && target!=null && viewer!=target &&
                round!=null && round.PlayerAt(target.PlayerSlot)==target && target.IsTaggable();
        }
        private void BeginReadability(Camera camera)
        {
            if(_readActor==null)_readActor=GetComponent<CharacterMotor>();
            var round=GameServices.Round;
            if(_readActor==null || round==null || round.PlayerAt(_readActor.PlayerSlot)!=_readActor)return;
            _readBlock??=new MaterialPropertyBlock();
            var scope=_readPool.Count>0?_readPool.Pop():new ReadScope();scope.Camera=camera;
            bool catchable=CatchableFor(camera,_readActor) && WorldCueProfile.Current.TayaTarget>0;
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
                _readBlock.SetFloat("_DepthReadability",WorldCueProfile.Current.DistanceReadability);
                _readBlock.SetFloat("_TayaCue",catchable?1:0);
                if(catchable)
                {
                    _readBlock.SetColor("_RimColor",UI.UiTheme.Defense);
                    _readBlock.SetFloat("_RimStrength",.72f*WorldCueProfile.Current.TayaTarget);
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
