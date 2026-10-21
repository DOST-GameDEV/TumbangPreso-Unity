using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterNameplate
    {
        private sealed class RingScope
        {
            public Camera Camera;public Mesh Mesh;public Vector3 Scale;public Material Material;
            public MaterialPropertyBlock Block;
        }
        private readonly List<RingScope> _ringScopes=new List<RingScope>();
        private readonly Stack<RingScope> _ringPool=new Stack<RingScope>();
        private static Mesh _catchable;
        private Material _catchMaterial;
        private void OnEnable(){Camera.onPreCull+=BeginCatchable;Camera.onPostRender+=EndCatchable;}
        private void OnDisable()
        {
            Camera.onPreCull-=BeginCatchable;Camera.onPostRender-=EndCatchable;
            for(int i=_ringScopes.Count-1;i>=0;i--)RestoreRing(_ringScopes[i]);_ringScopes.Clear();
        }
        private void OnDestroy(){if(_catchMaterial!=null)Destroy(_catchMaterial);if(_groundMaterial!=null)Destroy(_groundMaterial);if(_discMesh!=null)Destroy(_discMesh);}
        private void BeginCatchable(Camera camera)
        {
            if(_ringRenderer==null)return;
            var state=_ringPool.Count>0?_ringPool.Pop():new RingScope{Block=new MaterialPropertyBlock()};
            state.Camera=camera;state.Mesh=_ringFilter.sharedMesh;state.Scale=_ring.localScale;
            state.Material=_ringRenderer.sharedMaterial;_ringRenderer.GetPropertyBlock(state.Block);
            if(_ringScopes.Count>0)ApplyRing(_ringScopes[0]);
            _ringScopes.Add(state);
            if(!CharacterVisual.CatchableFor(camera,_character) || WorldCueProfile.Current.TayaTarget<=0)return;
            if(_catchable==null)_catchable=BuildCatchable();
            if(_catchMaterial==null)_catchMaterial=new Material(Shader.Find("TumbangPreso/PlayerGroundMarker")){name="Catchable ink brackets"};
            _ringFilter.sharedMesh=_catchable;_ringRenderer.sharedMaterial=_catchMaterial;
            var capsule=_character.GetComponent<CharacterController>();float radius=(capsule!=null?capsule.radius:.4f)*2.1f;
            _ring.localScale=new Vector3(radius,1,radius);
            _ringRenderer.GetPropertyBlock(_ringBlock);
            _ringBlock.SetFloat("_Shape",2);_ringBlock.SetFloat("_Weight",WorldCueProfile.Current.TayaTarget);
            _ringBlock.SetColor("_Color",UI.UiTheme.Defense);_ringRenderer.SetPropertyBlock(_ringBlock);
        }
        private void EndCatchable(Camera camera)
        {
            for(int i=_ringScopes.Count-1;i>=0;i--)if(_ringScopes[i].Camera==camera)
            {var state=_ringScopes[i];_ringScopes.RemoveAt(i);RestoreRing(state);return;}
        }
        private void ApplyRing(RingScope state)
        {
            if(_ringRenderer==null)return;
            _ringFilter.sharedMesh=state.Mesh;_ring.localScale=state.Scale;
            _ringRenderer.sharedMaterial=state.Material;_ringRenderer.SetPropertyBlock(state.Block);
        }
        private void RestoreRing(RingScope state){ApplyRing(state);state.Camera=null;_ringPool.Push(state);}
        private static Mesh BuildCatchable() => GroundMarkerMesh("Catchable four brackets");
    }
}
