using System.Linq;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso
{
    // A spectator is scenery, never an extra CharacterMotor or network seat.
    public sealed class LagoonResident : MonoBehaviour
    {
        public RosterEntryAsset Art;
        public int ReactionStyle;
        private PlayableGraph _graph;
        private Transform _arm;
        private Quaternion _rest;
        private float _cheerUntil, _nextReaction;
        private void OnEnable() => MatchFlair.Presented += React;
        private void OnDisable() => MatchFlair.Presented -= React;
        private void Start()
        {
            if (Art?.Model == null) return;
            var model = Instantiate(Art.Model, transform, false);
            model.transform.localScale = Vector3.one * 2.38f;
            model.transform.localRotation = Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) { collider.enabled = false; Destroy(collider); }
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, Art.Palette);
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            var idle = Art.Clips?.FirstOrDefault(c => c != null && c.name.ToLowerInvariant().Contains("idle"));
            if (idle != null)
            {
                _graph = PlayableGraph.Create("Lagoon resident idle");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var playable = AnimationClipPlayable.Create(_graph, idle);
                playable.SetTime(ReactionStyle * .37);
                AnimationPlayableOutput.Create(_graph, "Idle", animator).SetSourcePlayable(playable); _graph.Play();
            }
            _arm = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "arm-left");
            if (_arm != null) _rest = _arm.localRotation;
        }
        private void React(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != MatchFlair.Kind.LataDown || Time.time < _nextReaction || Vector3.Distance(at, transform.position) > 34) return;
            _nextReaction = Time.time + 4 + ReactionStyle; _cheerUntil = Time.time + 1.2f;
        }
        private void LateUpdate()
        {
            if (_arm == null || Time.time >= _cheerUntil) return;
            float age = 1.2f - (_cheerUntil - Time.time), envelope = Mathf.Sin(age / 1.2f * Mathf.PI);
            _arm.localRotation = _rest * Quaternion.Euler(-80 * envelope, Mathf.Sin(age * 13) * 12 * envelope, -20 * envelope);
        }
        private void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }
    }
}
