using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Drives the character's animation from what the unit is actually doing.
    ///
    /// ⚠️⚠️ IT USES PLAYABLES, NOT AN AnimatorController ASSET, AND THAT IS DELIBERATE. A
    /// controller is an authored asset: it cannot be generated at runtime, it cannot be diffed,
    /// and it would have to be rebuilt by hand for every one of the twelve rigs. The Playables
    /// API plays an AnimationClip directly, so the clips that ship inside each GLB are enough
    /// and there is nothing to author or keep in sync.
    ///
    /// ⚠️ CLIP NAMES ARE READ OFF THE ASSET, NOT GUESSED. A wrong clip name does not error, it
    /// simply never plays, and the character stands still forever while looking like the
    /// animation layer is unfinished. Every name below was taken from a probe of the real
    /// model, and a missing one falls back rather than throwing.
    ///
    /// ⚠️ AND STATE COMES FROM THE MOTOR, NOT FROM INPUT. A stunned player is holding keys down
    /// and must not walk; a bot presses no keys at all and must. Reading the motor means both
    /// animate correctly from one code path, which is the same reason the input indirection
    /// exists at all.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        /// <summary>Names as they appear in the shipped GLBs. Verified by ModelProbe.</summary>
        private const string Idle = "idle";
        private const string Walk = "walk";
        private const string Sprint = "sprint";
        private const string Jump = "jump";
        private const string Fall = "fall";
        private const string PickUp = "pick-up";
        private const string Punch = "attack-melee-right";
        private const string HoldingRight = "holding-right";
        private const string Throwing = "holding-right-shoot";
        private const string Interact = "interact-right";
        private const string Die = "die";

        /// <summary>
        /// ⚠️ FALLBACK CHAINS, NOT SINGLE CLIPS. Godot stores an ORDERED LIST per key so a rig
        /// missing the first clip still animates on the second. Flattening these to one name
        /// each — which this table used to do — means a swapped model with a slightly
        /// different clip set silently stops animating that verb.
        /// </summary>
        private static readonly Dictionary<string, string[]> EmoteClips = new Dictionary<string, string[]>
        {
            // A literal thumbs-up on this rig. Also what "ready" plays, deliberately: the
            // gesture means the same thing in both places.
            { "yes", new[] { "emote-yes", Interact } },
            { "no", new[] { "emote-no", "interact-left" } },
            { "sit", new[] { "sit", "crouch" } },

            // ⚠️ RELABELLED "VICTORY", STILL THE crouch CLIP. 🧑 asked for a victory pose and
            // this rig does not have one — its clips are locomotion, combat and two gestures,
            // and nothing reads as arms-up celebration. crouch is the closest available and
            // is the honest placeholder rather than a promise the animation does not keep.
            { "crouch", new[] { "crouch", "sit" } },

            // The taunt. `die` is the knockdown clip played on purpose.
            { "dead", new[] { Die, "crouch" } },

            // `static` is the rig's unanimated bind pose — arms out, feet together — which is
            // exactly the T-pose the joke is about.
            { "tpose", new[] { "static", Idle } },

            // ⚠️ `pick-up`, NOT `interact-right`. It bends the torso forward over the legs, so
            // standing still it reads as a bow. One clip having two jobs costs no new asset;
            // this table had the wrong clip and the bow read as a shrug.
            { "bow", new[] { PickUp, Interact } },
        };

        /// <summary>
        /// ⚠️⚠️ WHICH EMOTES LOOP AND WHICH HOLD THEIR LAST FRAME. 🧑 2026-08-04: *"play dead
        /// looks hella weird rn im perma jumping up and down the floor and lying down"*, and
        /// *"for play dead can u NOT loop the animation and instead js let me stay on the
        /// floor till i stop"*.
        ///
        /// That is what looping does to a clip with a beginning and an end: `die` drops the
        /// body, finishes, restarts from standing and drops again. Anything that ENDS
        /// somewhere holds its last frame; only the gestures that read as repeatable loop.
        /// </summary>
        private static readonly Dictionary<string, bool> EmoteLoops = new Dictionary<string, bool>
        {
            { "yes", true },
            { "no", true },
            { "sit", false },      // sitting down ends sitting; standing up every 2s is not sitting
            { "crouch", false },
            { "dead", false },
            { "tpose", false },
            { "bow", true },
        };

        /// <summary>
        /// The gameplay verbs' reads.
        ///
        /// ⚠️⚠️ LUNGE AND PUNCH HAVE THEIR OWN CLIPS AND MUST KEEP THEM. Both used to play the
        /// shove animation, so the taya's one-metre lunge, their close jab and an attacker
        /// shoving a rival were three different commitments with one animation between them.
        /// `attack-kick-right` leads with the body, which is what a dash INTO somebody looks
        /// like; `attack-melee-right` is the arm, which is the punch.
        ///
        /// ⚠️ "shove" WAS RENAMED FROM "bump". The bump meter it was built for is deleted, and
        /// a clip key naming a mechanic that no longer exists is how the next reader concludes
        /// the mechanic still does.
        /// </summary>
        private static readonly Dictionary<string, string[]> ActionClips = new Dictionary<string, string[]>
        {
            { "throw", new[] { Throwing, PickUp, Interact } },
            { "shove", new[] { "attack-melee-right", "attack-kick-right", Interact } },
            { "ready", new[] { "emote-yes", Interact } },
            { "grab", new[] { PickUp, Interact, "interact-left" } },
            { "lunge", new[] { "attack-kick-right", "attack-melee-right", Interact } },
            { "punch", new[] { "attack-melee-right", "attack-kick-right", Interact } },
        };

        [SerializeField] private float _blend = 0.12f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private Social.EmotePlayer _emote;
        private Animator _animator;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();

        private string _current;
        private float _weight;
        private float _oneShotLeft;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _emote = GetComponent<Social.EmotePlayer>();
        }

        /// <summary>
        /// Called by <see cref="CharacterVisual"/> once the model exists. ⚠️ IT CANNOT RUN IN
        /// Awake because the model is instanced later, and an Animator that is bound before its
        /// rig exists silently drives nothing.
        /// </summary>
        public void Bind(GameObject model)
        {
            if (model == null) return;

            _animator = model.GetComponentInChildren<Animator>();
            if (_animator == null) _animator = model.AddComponent<Animator>();

            CacheClips(model);

            if (_clips.Count == 0)
            {
                Debug.LogWarning($"[Anim] {name} has no clips. Check the model's animationType " +
                                 "is Generic and importAnimation is on.");
                return;
            }

            _graph = PlayableGraph.Create($"Anim_{name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            output.SetSourcePlayable(_mixer);

            _graph.Play();
            Play(Idle, loop: true, force: true);
        }

        /// <summary>
        /// ⚠️ CLIPS COME FROM THE SOURCE ASSET, NOT FROM THE INSTANCE. An instantiated model
        /// carries no AnimationClip references of its own, so they are loaded from the imported
        /// asset the prefab came from.
        /// </summary>
        private void CacheClips(GameObject model)
        {
            _clips.Clear();

#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(
                UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(model) ?? (Object)model);

            if (!string.IsNullOrEmpty(path))
            {
                foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                        _clips[c.name] = c;
            }
#endif

            // In a build the clips travel with the prefab through the Animator's controller or
            // through a direct reference, so anything already reachable is registered too.
            foreach (var c in Resources.FindObjectsOfTypeAll<AnimationClip>())
            {
                if (c == null || c.name.StartsWith("__preview")) continue;
                if (!_clips.ContainsKey(c.name)) _clips[c.name] = c;
            }
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void Update()
        {
            if (!_graph.IsValid()) return;

            if (_oneShotLeft > 0.0f)
            {
                _oneShotLeft -= Time.deltaTime;
                Blend();
                return;
            }

            // ⚠️ NOT EVERYTHING LOOPS. Looping `die` is exactly the reported bug: the body
            // drops, the clip ends, it restarts from standing and drops again — 🧑 *"im perma
            // jumping up and down the floor and lying down"*. Locomotion loops; an emote loops
            // only if EmoteLoops says so, and otherwise holds its last frame.
            bool emoting = _emote != null && _emote.IsEmoting;
            bool loop = !emoting || !EmoteHoldsLastFrame(_emote.Current);

            Play(Choose(), loop);
            Blend();
        }

        /// <summary>
        /// ⚠️ THE ORDER OF THESE BRANCHES IS THE PRIORITY, and it is not arbitrary. Stunned
        /// beats everything, because a stunned player must visibly be stunned no matter what
        /// they are holding or pressing: that readout is what the other three players read to
        /// decide whether to commit.
        /// </summary>
        private string Choose()
        {
            if (_emote != null && _emote.IsEmoting)
            {
                string emoteClip = ResolveChain(EmoteClips, _emote.Current);
                if (emoteClip != null) return emoteClip;
            }

            if (_motor.IsStunned) return Die;

            if (_carrier != null && _carrier.ChannelRatio > 0.0f) return Interact;
            if (_carrier != null && _carrier.ChargeRatio > 0.0f) return Throwing;

            if (!_motor.IsGrounded) return _motor.Velocity.y > 0.5f ? Jump : Fall;

            Vector3 flat = _motor.Velocity;
            flat.y = 0.0f;
            float speed = flat.magnitude;

            if (speed > 0.15f)
            {
                // Sprinting reads off the actual speed rather than the key, so fatigue and the
                // attacker speed scale both show up honestly.
                bool fast = _motor.Stamina.IsSprinting;
                return fast ? Sprint : Walk;
            }

            return _motor.HoldingSlipper ? HoldingRight : Idle;
        }

        /// <summary>A non-looping action that owns the body until it finishes.</summary>
        public void PlayOneShot(string clipName)
        {
            if (!_graph.IsValid() || !_clips.TryGetValue(clipName, out var clip)) return;

            Play(clipName, loop: false, force: true);
            _oneShotLeft = clip.length;
        }

        public void PlayPickUp() => PlayOneShot(PickUp);

        /// <summary>
        /// Play a gameplay verb's read by NAME rather than by clip, so callers ask for
        /// "lunge" and this file owns which animation that is.
        ///
        /// ⚠️ THIS IS THE ONLY ENTRY POINT THAT KEEPS LUNGE, PUNCH AND SHOVE DISTINCT. A
        /// caller reaching for PlayOneShot("attack-melee-right") directly re-merges the three
        /// verbs the 2026-08-01 split separated.
        /// </summary>
        public void PlayAction(string action)
        {
            string clip = ResolveChain(ActionClips, action);
            if (clip != null) PlayOneShot(clip);
        }

        /// <summary>
        /// First clip in the chain this rig actually has.
        ///
        /// ⚠️ IT RETURNS null RATHER THAN A GUESS when nothing matches. A missing verb should
        /// leave the body in its locomotion pose, not snap it to an unrelated animation.
        /// </summary>
        private string ResolveChain(Dictionary<string, string[]> table, string key)
        {
            if (key == null || !table.TryGetValue(key, out var chain)) return null;

            foreach (string name in chain)
                if (_clips.ContainsKey(name)) return name;

            return null;
        }

        /// <summary>Does this emote hold its last frame instead of looping?
        /// See <see cref="EmoteLoops"/> — `die` restarting from standing is the bug this
        /// answers.</summary>
        public static bool EmoteHoldsLastFrame(string emoteId)
            => EmoteLoops.TryGetValue(emoteId, out bool loops) && !loops;
        public void PlayPunch() => PlayOneShot(Punch);

        private void Play(string clipName, bool loop, bool force = false)
        {
            if (!force && clipName == _current) return;
            if (!_clips.TryGetValue(clipName, out var clip)) return;

            // Retire whatever was on input 0 and slide the current clip down to it, so the
            // mixer always crossfades from what you were doing to what you are doing now.
            if (_mixer.GetInput(0).IsValid()) _mixer.GetInput(0).Destroy();
            if (_mixer.GetInput(1).IsValid())
            {
                _graph.Disconnect(_mixer, 1);
                _mixer.ConnectInput(0, _mixer.GetInput(1), 0);
            }

            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetDuration(loop ? double.MaxValue : clip.length);

            _mixer.ConnectInput(1, playable, 0);
            _weight = 0.0f;
            _current = clipName;
        }

        private void Blend()
        {
            _weight = _blend <= 0.0f ? 1.0f : Mathf.Min(1.0f, _weight + Time.deltaTime / _blend);

            _mixer.SetInputWeight(0, 1.0f - _weight);
            _mixer.SetInputWeight(1, _weight);
        }
    }
}
