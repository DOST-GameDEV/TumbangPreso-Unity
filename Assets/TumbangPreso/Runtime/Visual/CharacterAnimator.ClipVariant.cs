using UnityEngine.Playables;
using UnityEngine.Animations;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A BODY'S OWN SET OF CLIPS FOR A WHILE. Dante in MOUNTAIN FORM (owner, 2026-10-18: *"baguhin mo na ren animation
    /// niya"*) moves as a heavy armoured body, not as the kid under the suit: while `ClipVariant` is "mountain-" every clip
    /// this file plays by name (`Play`, `PlayOneShot`, the action chains, `Choose`) is played as "mountain-" + name when
    /// the rig has that clip, and as itself when it does not, so a missing variant never freezes a body. The gait layer's
    /// walk and sprint are swapped with it (`BuildGaitLayer` builds them once).
    ///
    /// ⚠️ `_current` KEEPS THE PLAIN NAME. Everything that asks what is playing (`_current == Walk`, the hero clocks, the
    /// rejoin seek) still reads the verb, and only `_playing` knows which clip stands behind it.
    ///
    /// ⚠️ A THROW IS POSED BY `ThrowBody` OVER WHATEVER PLAYS, so a variant `throw` is the body UNDER it: the clip is played
    /// as the throw releases (`ThrowVariantClip`), and the arms and chest still come from the gesture.
    /// Presentation only: set on every peer by whatever draws the form (`DanteMountainArmour`).
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        public const string MountainVariant = "mountain-";
        /// <summary>The name a variant throw's legs are played under (`mountain-throw`).</summary>
        private const string ThrowVariantClip = "throw";

        private string _variant;
        private string _playing;

        /// <summary>The prefix of the clip set this body plays now (`MountainVariant`), or null for its own.</summary>
        public string ClipVariant
        {
            get => _variant;
            set
            {
                if (value == "") value = null;
                if (value == _variant) return;
                _variant = value;
                if (!_graph.IsValid()) return;
                SwapGaitClips();
                // The pose playing now is replaced at once by its variant (a one-shot keeps its time).
                if (_current != null && _oneShotLeft <= 0f) Play(_current, loop: !_holdAtEnd, force: true);
            }
        }

        /// <summary>The clip that stands for <paramref name="name"/> on this body now.</summary>
        private string Variant(string name)
        {
            if (_variant == null || name == null) return name;
            string variant = _variant + name;
            return _clips.ContainsKey(variant) ? variant : name;
        }

        /// <summary>Does this body have a clip, or a variant of one, for <paramref name="name"/>?</summary>
        private bool HasClip(string name) => name != null && _clips.ContainsKey(Variant(name));

        /// <summary>
        /// The clip this body would play for <paramref name="name"/> (its variant when it has one), or null. Read-only: for a
        /// render-only copy that samples a pose (Zack's AFTERIMAGE runs his own sprint), never for this body's own graph.
        /// </summary>
        public UnityEngine.AnimationClip ClipNamed(string name)
            => name != null && _clips.TryGetValue(Variant(name), out var clip) ? clip : null;

        /// <summary>Under a throw's release, the variant's own legs (null when this body has none).</summary>
        private string ThrowBase => _variant != null && _clips.ContainsKey(_variant + ThrowVariantClip) ? ThrowVariantClip : null;

        private void SwapGaitClips()
        {
            if (!_gait.IsValid()) return;
            if (!_clips.TryGetValue(Variant(Walk), out var walk) || !_clips.TryGetValue(Variant(Sprint), out var run)) return;
            if (_walkGait.IsValid() && _walkGait.GetAnimationClip() == walk && _runGait.IsValid() && _runGait.GetAnimationClip() == run) return;
            float walkWeight = _gait.GetInputWeight(0), runWeight = _gait.GetInputWeight(1);
            _graph.Disconnect(_gait, 0); _graph.Disconnect(_gait, 1);
            if (_walkGait.IsValid()) _walkGait.Destroy();
            if (_runGait.IsValid()) _runGait.Destroy();
            _walkGait = AnimationClipPlayable.Create(_graph, walk);
            _runGait = AnimationClipPlayable.Create(_graph, run);
            _walkGait.SetApplyFootIK(false); _runGait.SetApplyFootIK(false);
            _walkGait.SetSpeed(0); _runGait.SetSpeed(0);
            _gait.ConnectInput(0, _walkGait, 0, walkWeight);
            _gait.ConnectInput(1, _runGait, 0, runWeight);
            CalibrateGait(walk, run);
        }
    }
}
