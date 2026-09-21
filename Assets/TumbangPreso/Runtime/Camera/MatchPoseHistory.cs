using UnityEngine;
using TumbangPreso.Visual;

namespace TumbangPreso.CameraSystem
{
    // Bounded, non-authoritative body history shared by catch reconstruction and
    // future retained highlights. It records actual transforms, never resimulates.
    [DefaultExecutionOrder(1800)]
    public sealed class MatchPoseHistory : MonoBehaviour
    {
        public const int Samples = 160;
        public const float Interval = .05f;
        public const int TransformLimit = 256;
        private readonly Track[] _tracks = new Track[Core.Balance.PlayerCount];
        private float _next;
        private int _round;
        public event System.Action<float> Sampled;
        public Track ForSeat(int seat) => seat >= 0 && seat < _tracks.Length ? _tracks[seat] : null;

        private void LateUpdate()
        {
            var round = GameServices.Round; var match = GameServices.Match;
            if (round == null || match == null) return;
            if (_round != match.RoundNumber) { System.Array.Clear(_tracks, 0, _tracks.Length); _round = match.RoundNumber; }
            if (!round.RoundActive || Time.time < _next) return;
            _next = Time.time + Interval;
            for (int i = 0; i < _tracks.Length; i++)
            {
                var actor = round.PlayerAt(i); var visual = actor != null ? actor.GetComponent<CharacterVisual>() : null;
                if (visual == null || visual.Model == null) { _tracks[i] = null; continue; }
                if (_tracks[i] == null || _tracks[i].Source != visual.Model)
                    _tracks[i] = new Track(actor, visual.Model);
                _tracks[i].Record(Time.time);
            }
            Sampled?.Invoke(Time.time);
        }

        public static Transform[] StableTransforms(GameObject source)
        {
            var result=new System.Collections.Generic.List<Transform>();
            foreach(var node in source.GetComponentsInChildren<Transform>(true))
            {
                bool transient=false;
                for(var parent=node;parent!=null&&parent!=source.transform;parent=parent.parent)
                    if(parent.GetComponent<DanteCarapaceVisual>()!=null||parent.GetComponent<SeanIgnitionVisual>()!=null||parent.GetComponent<ZackMagnetCharge>()!=null)
                    {transient=true;break;}
                if(!transient)result.Add(node);
            }
            return result.ToArray();
        }

        public sealed class Track
        {
            public readonly CharacterMotor Actor;
            public readonly GameObject Source;
            private readonly Transform[] _bones;
            private readonly Frame[] _frames;
            private int _cursor, _count;
            public bool Ready => _count >= 2;
            public float Newest => _count > 0 ? _frames[(_cursor + Samples - 1) % Samples].Time : 0;
            public float Oldest => _count > 0 ? _frames[(_cursor + Samples - _count) % Samples].Time : 0;
            public Track(CharacterMotor actor, GameObject source)
            {
                Actor = actor; Source = source; _bones = StableTransforms(source);
                if (_bones.Length > TransformLimit) { _frames = new Frame[0]; return; }
                _frames = new Frame[Samples];
                for (int i = 0; i < Samples; i++) _frames[i] = new Frame(_bones.Length);
            }
            public void Record(float time)
            {
                if (Source == null || _frames.Length == 0) return;
                var frame = _frames[_cursor]; frame.Time = time;ReadState(out frame.State,out frame.Holder);
                frame.HasCoat=ReadCoat(out frame.Frost,out frame.Flash,out frame.Element);frame.Epoch=ReadEpoch();
                for (int i = 0; i < _bones.Length; i++)
                {
                    var bone = _bones[i]; if (bone == null) { _count = 0; return; }
                    frame.Position[i] = i == 0 ? bone.position : bone.localPosition;
                    frame.Rotation[i] = i == 0 ? bone.rotation : bone.localRotation;
                    frame.Scale[i] = i == 0 ? bone.lossyScale : bone.localScale;
                    frame.Active[i] = bone.gameObject.activeSelf;
                }
                _cursor = (_cursor + 1) % Samples; _count = Mathf.Min(Samples, _count + 1);
            }
            public RecordedPoseTrack.Sample Capture(float time)
            {
                if(Source==null||_frames.Length==0)return null;
                var sample=new RecordedPoseTrack.Sample{Time=time,Positions=new Vector3[_bones.Length],
                    Rotations=new Quaternion[_bones.Length],Scales=new Vector3[_bones.Length],Active=new bool[_bones.Length]};
                for(int i=0;i<_bones.Length;i++)
                {
                    var bone=_bones[i];if(bone==null)return null;
                    sample.Positions[i]=i==0?bone.position:bone.localPosition;
                    sample.Rotations[i]=i==0?bone.rotation:bone.localRotation;
                    sample.Scales[i]=i==0?bone.lossyScale:bone.localScale;
                    sample.Active[i]=bone.gameObject.activeSelf;
                }
                ReadState(out sample.State,out sample.Holder);
                sample.HasCoat=ReadCoat(out sample.Frost,out sample.Flash,out sample.Element);sample.Epoch=ReadEpoch();
                return sample;
            }
            private int ReadEpoch()
            {var motor=Source.GetComponentInParent<CharacterMotor>();return motor!=null?motor.MovementEpoch:-1;}
            private bool ReadCoat(out float frost,out float flash,out StunElement element)
            {
                frost=flash=0;element=StunElement.None;
                var visual=Source.GetComponentInParent<CharacterVisual>();if(visual==null||visual.Model!=Source)return false;
                visual.CaptureRecordedCoat(out frost,out flash,out element);return true;
            }
            private void ReadState(out int state,out int holder)
            {
                state=0;holder=-1;
                var can=Source.GetComponentInParent<Lata>();
                if(can!=null){state=(can.IsUpright?1:0)|(can.IsProtected?2:0);return;}
                var shoe=Source.GetComponentInParent<Slipper>();
                if(shoe!=null){state=(int)shoe.State|((int)shoe.Affinity<<8);holder=shoe.Holder!=null?shoe.Holder.PlayerSlot:-1;}
            }
            public Transform CopiedBone(Copy copy, Transform source)
            {
                int index = System.Array.IndexOf(_bones, source);
                return index >= 0 && index < copy.Bones.Length ? copy.Bones[index] : null;
            }
            public RecordedPoseTrack Retain(float start,float end)
            {
                if(!Ready||end<=start||start<Oldest||end>Newest||Source==null)return null;
                var paths=new string[_bones.Length];
                for(int i=0;i<_bones.Length;i++)
                {
                    if(_bones[i]==null)return null;
                    var parts=new System.Collections.Generic.List<string>();
                    for(var bone=_bones[i];bone!=Source.transform;bone=bone.parent)
                    {if(bone==null)return null;parts.Add(bone.name);}
                    parts.Reverse();paths[i]=string.Join("/",parts);
                }
                int oldest=(_cursor+Samples-_count)%Samples;
                int from=0,to=_count-1;
                for(int i=0;i<_count;i++)
                {
                    float time=_frames[(oldest+i)%Samples].Time;
                    if(time<=start)from=i;
                    if(time>=end){to=i;break;}
                }
                var result=new RecordedPoseTrack.Sample[to-from+1];
                for(int i=from;i<=to;i++)
                {
                    var frame=_frames[(oldest+i)%Samples];
                    result[i-from]=new RecordedPoseTrack.Sample{Time=frame.Time,State=frame.State,Holder=frame.Holder,Epoch=frame.Epoch,HasCoat=frame.HasCoat,Frost=frame.Frost,Flash=frame.Flash,Element=frame.Element,
                        Positions=(Vector3[])frame.Position.Clone(),Rotations=(Quaternion[])frame.Rotation.Clone(),
                        Scales=(Vector3[])frame.Scale.Clone(),Active=(bool[])frame.Active.Clone()};
                }
                return new RecordedPoseTrack(paths,result);
            }
            public float ContactTime(Vector3 at)
            {
                float best = 1.5f * 1.5f, time = -1;
                for (int n = 0; n < _count; n++)
                {
                    var frame = _frames[(_cursor + Samples - 1 - n) % Samples];
                    if (Newest - frame.Time > Core.Balance.TagStunTime) break;
                    Vector3 delta = frame.Position[0] - at; delta.y = 0;
                    if (delta.sqrMagnitude < best) { best = delta.sqrMagnitude; time = frame.Time; }
                }
                return time;
            }
            public Copy Clone(Transform inactiveParent)
            {
                if (!Ready || Source == null) return null;
                foreach (var bone in _bones) if (bone == null) return null;
                // Copy rendering data, never GameObjects carrying scripts.
                // Disabling a cloned MonoBehaviour would not prevent its Awake
                // when the stage activates. Shared meshes/materials stay owned
                // by the live assets; only these transforms/renderers are new.
                var map = new System.Collections.Generic.Dictionary<Transform, Transform>(_bones.Length);
                foreach (var bone in _bones)
                {
                    var copied = new GameObject(bone.name).transform;
                    copied.SetParent(inactiveParent, false); map[bone] = copied;
                }
                for (int i = 0; i < _bones.Length; i++)
                {
                    var source = _bones[i]; var target = map[source];
                    target.SetParent(i == 0 ? inactiveParent : map[source.parent], false);
                    target.gameObject.layer = source.gameObject.layer;
                    target.gameObject.SetActive(source.gameObject.activeSelf);
                }
                GameObject root = map[_bones[0]].gameObject;
                root.name = "RecordedBody-P" + (Actor != null ? Actor.PlayerSlot + 1 : 0);
                foreach (var bone in _bones)
                {
                    var target = map[bone].gameObject;
                    var skin = bone.GetComponent<SkinnedMeshRenderer>();
                    if (skin != null)
                    {
                        var rendered = target.AddComponent<SkinnedMeshRenderer>();
                        rendered.sharedMesh = skin.sharedMesh; rendered.localBounds = skin.localBounds;
                        var bones = skin.bones; var copiedBones = new Transform[bones.Length];
                        for (int i = 0; i < bones.Length; i++)
                        {
                            if (bones[i] == null || !map.TryGetValue(bones[i], out copiedBones[i]))
                            { Object.Destroy(root); return null; }
                        }
                        rendered.bones = copiedBones;
                        if (skin.rootBone != null && map.TryGetValue(skin.rootBone, out var copiedRoot)) rendered.rootBone = copiedRoot;
                        rendered.updateWhenOffscreen = true;
                        if (skin.sharedMesh != null)
                            for (int n = 0; n < skin.sharedMesh.blendShapeCount; n++) rendered.SetBlendShapeWeight(n, skin.GetBlendShapeWeight(n));
                        CopySurface(skin, rendered);
                    }
                    var mesh = bone.GetComponent<MeshFilter>(); var surface = bone.GetComponent<MeshRenderer>();
                    if (mesh != null && surface != null)
                    {
                        target.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                        CopySurface(surface, target.AddComponent<MeshRenderer>());
                    }
                }
                var result = new Copy(root);
                if (result.Bones.Length != _bones.Length) { Object.Destroy(root); return null; }
                return result;
            }
            private static void CopySurface(Renderer source, Renderer target)
            {
                target.sharedMaterials = source.sharedMaterials; target.enabled = source.enabled;
                target.sortingLayerID = source.sortingLayerID; target.sortingOrder = source.sortingOrder;
                var properties = new MaterialPropertyBlock(); source.GetPropertyBlock(properties); target.SetPropertyBlock(properties);
                target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                target.forceRenderingOff = true;
            }
            public void Apply(Copy copy, float time)
            {
                if (!Ready || copy == null) return;
                int oldest = (_cursor + Samples - _count) % Samples;
                Frame left = _frames[oldest], right = left;
                for (int n = 1; n < _count; n++)
                {
                    right = _frames[(oldest + n) % Samples];
                    if (right.Time >= time) break;
                    left = right;
                }
                float t = right.Time > left.Time ? Mathf.Clamp01((time-left.Time)/(right.Time-left.Time)) : 0;
                for (int i = 0; i < copy.Bones.Length; i++)
                {
                    var bone = copy.Bones[i];
                    bone.localPosition = Vector3.Lerp(left.Position[i], right.Position[i], t);
                    bone.localRotation = Quaternion.Slerp(left.Rotation[i], right.Rotation[i], t);
                    bone.localScale = Vector3.Lerp(left.Scale[i], right.Scale[i], t);
                    bone.gameObject.SetActive(t < .5f ? left.Active[i] : right.Active[i]);
                }
            }
        }
        private sealed class Frame
        {
            public float Time;
            public int State,Holder=-1,Epoch=-1;
            public bool HasCoat;public float Frost,Flash;public StunElement Element;
            public readonly Vector3[] Position, Scale;
            public readonly Quaternion[] Rotation;
            public readonly bool[] Active;
            public Frame(int count)
            { Position = new Vector3[count]; Scale = new Vector3[count]; Rotation = new Quaternion[count]; Active = new bool[count]; }
        }
        public sealed class Copy
        {
            public readonly GameObject Root;
            public readonly Transform[] Bones;
            public readonly Renderer[] Renderers;
            public Copy(GameObject root)
            {
                Root = root; Bones = root.GetComponentsInChildren<Transform>(true);
                Renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in Renderers)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = true; renderer.forceRenderingOff = true;
                }
            }
            public void ShowOnlyForCapture(bool on)
            { foreach (var renderer in Renderers) if (renderer != null) renderer.forceRenderingOff = !on; }
        }
    }
}
