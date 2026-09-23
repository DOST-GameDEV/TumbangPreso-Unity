using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Saved planks already own their joints. Only lagoon deck renderers opt into
    // filtered single-plank grain; houses, posts and other maps keep their finish.
    public sealed class LagoonDeckPresentation : MonoBehaviour
    {
        private struct Board {public Renderer Renderer;public float Original;}
        private readonly List<Board> _boards=new List<Board>();
        private MaterialPropertyBlock _block;
        private float _weight=-1;
        public int BoardRenderers=>_boards.Count;
        public static LagoonDeckPresentation Install(Transform parent)
        {
            if(parent.gameObject.scene.name!=UI.SceneFlow.Lagoon)return null;
            var go=new GameObject("Lagoon deck sampling");go.transform.SetParent(parent,false);
            var owner=go.AddComponent<LagoonDeckPresentation>();owner.Build();return owner;
        }
        private void Build()
        {
            _block=new MaterialPropertyBlock();
            foreach(var renderer in FindObjectsByType<MeshRenderer>())
            {
                if(renderer.gameObject.scene!=gameObject.scene || (renderer.name!="Deck board" && !renderer.name.StartsWith("Thin deck surface ")))continue;
                renderer.GetPropertyBlock(_block);
                _boards.Add(new Board{Renderer=renderer,Original=_block.GetFloat("_DeckSurface")});
            }
            Apply();
        }
        private void LateUpdate(){if(_weight!=WorldCueProfile.Current.LagoonDeckDetail)Apply();}
        private void Apply()
        {
            _weight=Mathf.Clamp01(WorldCueProfile.Current.LagoonDeckDetail);
            foreach(var board in _boards)
            {
                if(board.Renderer==null)continue;board.Renderer.GetPropertyBlock(_block);
                _block.SetFloat("_DeckSurface",Mathf.Lerp(board.Original,1,_weight));board.Renderer.SetPropertyBlock(_block);
            }
        }
        private void OnDisable()
        {
            if(_block==null)return;
            foreach(var board in _boards)
            {if(board.Renderer==null)continue;board.Renderer.GetPropertyBlock(_block);_block.SetFloat("_DeckSurface",board.Original);board.Renderer.SetPropertyBlock(_block);}
            _weight=-1;
        }
    }
}
