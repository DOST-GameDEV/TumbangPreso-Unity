using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TumbangPreso.CameraSystem
{
    public sealed class RecordedSceneryView:IDisposable
    {
        private sealed class Item
        {
            public MatchPoseHistory.Copy Copy;public readonly Dictionary<string,Renderer> Renderers=new Dictionary<string,Renderer>();
            public string Art;
            public readonly Dictionary<string,LineRenderer> Lines=new Dictionary<string,LineRenderer>();
            public readonly Dictionary<(string,int),MaterialPropertyBlock> Defaults=new Dictionary<(string,int),MaterialPropertyBlock>();
        }
        private readonly Transform _owner;
        private readonly Dictionary<string,Item> _items=new Dictionary<string,Item>();
        private readonly HashSet<string> _visible=new HashSet<string>();
        private Material _lineMaterial;
        public RecordedSceneryView(Transform owner){_owner=owner;}
        private static string Key(LocalReplaySceneryRoot root)=>root.Kind+":"+root.OwnerPath+":"+root.Id+":"+root.Generation;
        public void Draw(LocalReplaySceneryFrame frame)
        {
            _visible.Clear();foreach(var item in _items.Values)item.Copy.ShowOnlyForCapture(false);
            if(frame==null)return;
            foreach(var root in frame.Roots)
            {
                string key=Key(root);if(!_items.TryGetValue(key,out var item)){item=Create(root);_items.Add(key,item);}
                if(item.Art!=root.Art)throw new InvalidOperationException("Recorded scenery generation changed art content.");
                _visible.Add(key);
                foreach(var baseline in item.Defaults)
                {
                    var renderer=item.Renderers[baseline.Key.Item1];int slot=baseline.Key.Item2;
                    var block=baseline.Value.isEmpty?null:baseline.Value;
                    if(slot<0)renderer.SetPropertyBlock(block);else renderer.SetPropertyBlock(block,slot);
                }
                for(int n=0;n<root.Paths.Length;n++)
                {
                    var bone=root.Paths[n].Length==0?item.Copy.Root.transform:item.Copy.Root.transform.Find(root.Paths[n]);
                    if(bone==null)throw new InvalidOperationException("Recorded scenery bone changed: "+root.Paths[n]);
                    bone.localPosition=root.Positions[n];bone.localRotation=root.Rotations[n];bone.localScale=root.Scales[n];bone.gameObject.SetActive(root.Active[n]);
                }
                foreach(var surface in root.Surfaces)
                {
                    if(!item.Renderers.TryGetValue(surface.Path,out var renderer))throw new InvalidOperationException("Recorded scenery renderer changed.");
                    renderer.enabled=surface.Enabled;
                    var block=new MaterialPropertyBlock();var defaults=item.Defaults[(surface.Path,surface.Slot)];
                    // Read the owned baseline block to retain original appearance
                    // when a recorded override disappears in a later frame.
                    var baseline=defaults.isEmpty?null:defaults;
                    if(surface.Slot<0)renderer.SetPropertyBlock(baseline);else renderer.SetPropertyBlock(baseline,surface.Slot);
                    if(surface.Slot<0)renderer.GetPropertyBlock(block);else renderer.GetPropertyBlock(block,surface.Slot);
                    if(surface.HasColour)block.SetColor("_Color",surface.Colour);if(surface.HasUv)block.SetVector("_MainTex_ST",surface.Uv);
                    if(surface.Slot<0)renderer.SetPropertyBlock(block);else renderer.SetPropertyBlock(block,surface.Slot);
                }
                foreach(var line in item.Lines.Values)line.enabled=false;
                foreach(var recorded in root.Lines)
                {
                    if(!item.Lines.TryGetValue(recorded.Path,out var line))
                    {
                        var node=item.Copy.Root.transform.Find(recorded.Path);
                        if(node==null){node=new GameObject(recorded.Path).transform;node.SetParent(item.Copy.Root.transform,false);}
                        line=node.gameObject.AddComponent<LineRenderer>();item.Lines.Add(recorded.Path,line);
                        if(_lineMaterial==null)_lineMaterial=new Material(Shader.Find("Sprites/Default")){name="Recorded animal line",hideFlags=HideFlags.DontSave};
                        line.sharedMaterial=_lineMaterial;
                    }
                    line.enabled=recorded.Enabled;line.useWorldSpace=recorded.WorldSpace;line.positionCount=recorded.Points.Length;line.SetPositions(recorded.Points);
                    line.startWidth=recorded.StartWidth;line.endWidth=recorded.EndWidth;line.startColor=recorded.StartColour;line.endColor=recorded.EndColour;line.forceRenderingOff=true;
                }
            }
            foreach(string key in _items.Keys.Where(k=>!_visible.Contains(k)).ToArray())
            {Object.Destroy(_items[key].Copy.Root);_items.Remove(key);}
        }
        private Item Create(LocalReplaySceneryRoot root)
        {
            GameObject source;
            if(root.Kind==LocalReplaySceneryKind.Animal)
            {
                var owner=LocalReplaySceneState.Find(root.OwnerPath,SceneManager.GetActiveScene());
                if(owner==null||owner.name!=root.OwnerName)throw new InvalidOperationException("Recorded animal owner changed.");
                var life=owner.GetComponent<AmbientLife>();var animal=life?.Animals.FirstOrDefault(a=>a.Id==root.Id);
                if(animal?.Model==null||animal.Model.name!=root.Model)throw new InvalidOperationException("Recorded animal art changed.");
                source=owner.Find("Ambient "+root.Id)?.gameObject;
            }
            else
            {
                // Template setup's detached mark is reproduced as rendering
                // data. No ArenaDrone Awake/Attend/Hold/Step runs on this copy.
                source=Object.FindAnyObjectByType<ArenaFallRecovery>()?.DroneTemplate;
                if(root.Kind==LocalReplaySceneryKind.DroneMark)source=source?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="drone_spot")?.gameObject;
            }
            if(source==null)throw new InvalidOperationException("Recorded scenery art is unavailable.");
            var track=new MatchPoseHistory.Track(null,source);track.Record(0);track.Record(.05f);
            // The shared clone helper explicitly requires an inactive parent.
            // Bind the whole skin before activating any renderer; never pause
            // or deactivate the live scene or this view's other copies.
            var building=new GameObject("Recorded scenery construction");building.SetActive(false);building.transform.SetParent(_owner,false);
            MatchPoseHistory.Copy copy;
            try{copy=track.Clone(building.transform);if(copy!=null)copy.Root.transform.SetParent(_owner,false);}
            finally{Object.Destroy(building);}
            if(copy==null)throw new InvalidOperationException("Recorded scenery render copy failed.");
            if(root.Kind==LocalReplaySceneryKind.Drone)
            {
                var spot=copy.Root.transform.Find("drone_spot");if(spot!=null){spot.SetParent(null);Object.Destroy(spot.gameObject);}
                copy=new MatchPoseHistory.Copy(copy.Root);
            }
            // Validate actual geometry, indices, materials and textures once
            // per owned model, never bake or hash animation frames repeatedly.
            if(LocalReplaySceneryState.ArtKey(copy.Root)!=root.Art){Object.Destroy(copy.Root);throw new InvalidOperationException("Recorded scenery art content changed.");}
            var item=new Item{Copy=copy,Art=root.Art};
            foreach(var renderer in copy.Renderers)
            {
                string path=LocalReplaySceneryState.Relative(copy.Root.transform,renderer.transform);item.Renderers[path]=renderer;
                var original=path.Length==0?source.transform:source.transform.Find(path);var live=original?.GetComponent<Renderer>();
                if(live!=null){renderer.shadowCastingMode=live.shadowCastingMode;renderer.receiveShadows=live.receiveShadows;}
                for(int slot=-1;slot<renderer.sharedMaterials.Length;slot++)
                {var block=new MaterialPropertyBlock();if(slot<0)renderer.GetPropertyBlock(block);else renderer.GetPropertyBlock(block,slot);item.Defaults[(path,slot)]=block;}
            }
            return item;
        }
        public void Visible(bool on)
        {
            foreach(var pair in _items)
            {bool visible=on&&_visible.Contains(pair.Key);pair.Value.Copy.ShowOnlyForCapture(visible);foreach(var line in pair.Value.Lines.Values)line.forceRenderingOff=!visible;}
        }
        public void Dispose()
        {
            foreach(var item in _items.Values)Object.Destroy(item.Copy.Root);_items.Clear();_visible.Clear();
            if(_lineMaterial!=null)Object.Destroy(_lineMaterial);_lineMaterial=null;
        }
    }
}
