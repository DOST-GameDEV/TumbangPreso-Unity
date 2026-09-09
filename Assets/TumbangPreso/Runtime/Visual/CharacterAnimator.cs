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
    [DefaultExecutionOrder(-50)]
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
        // The gait author reads these same angles; cadence follows actual leg reach.
        public const float WalkLegSwingDegrees = 38f;
        public const float RunLegSwingDegrees = 44f;

        /// <summary>The fatigued pose. ⚠️ THE RIG SHIPS NO PANTING CLIP — enumerated in full it
        /// carries attack-melee ×4, crouch, die, drive, emote-no, emote-yes, fall, holding-* ×6,
        /// idle, interact-* ×2, jump, pick-up, sit, sprint, static, walk and wheelchair-* ×6 —
        /// and `crouch` is the only one of them that reads as out of breath rather than as an
        /// action. Authoring a real one is the art lane's call, on a CC0 rig this project ships
        /// unmodified.</summary>
        private const string Crouch = "crouch";

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

            // ⚠️ § THE DANCE REPLACED PLAY DEAD on 2026-08-06, on a direct request. The old
            // `dead` entry played the knockdown clip as a taunt; it is gone from the wheel, so
            // an id arriving from an older peer resolves to nothing and falls through to the
            // locomotion pose rather than animating something the sender did not choose.
            //
            // `DanceClip.ClipName` is BUILT at bind time rather than shipped, because this rig
            // has no dance clip and nothing retargetable exists for a seven-bone skeleton. See
            // that file. The second entry is the fallback for a model with no rig to build on.
            { "dance", new[] { DanceClip.ClipName, Idle } },

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

            // ⚠️ THE DANCE LOOPS, and it is built to. Its first and last keys are the same pose
            // (see DanceClip's note on the seam), so a replay is continuous rather than a snap
            // back to the downbeat. A groove that played once and froze would read as the
            // animation having broken.
            { "dance", true },
            { "tpose", false },
            { "bow", true },
        };

        /// <summary>
        /// The action table, for the tests that assert what a player will actually see. See the
        /// § THE HERO CASTS note inside it for why the SECOND entry is the one that ships.
        /// </summary>
        public static IReadOnlyDictionary<string, string[]> ActionChains => ActionClips;

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

            // ⚠️ THE SHOVE LEADS WITH THE OFF ARM AND THE PUNCH WITH THE STRONG ONE, so the two
            // are told apart from behind. They both played `attack-melee-right` and were one
            // gesture: a push that moves somebody and a jab that tags them are different
            // commitments and an opponent has to be able to read which is coming.
            { "shove", new[] { "attack-melee-left", "attack-melee-right", Interact } },
            { "ready", new[] { "emote-yes", Interact } },
            { "grab", new[] { PickUp, Interact, "interact-left" } },
            { "lunge", new[] { "attack-kick-right", "attack-melee-right", Interact } },
            { "punch", new[] { "attack-melee-right", "attack-kick-left", Interact } },

            // ⚠️⚠️ THE RETRIEVAL SLIDE HAS ITS OWN ACTION NAME AND DELIBERATELY NO CLIP OF ITS
            // OWN YET, WHICH CHANGES NOTHING TODAY AND IS THE POINT. `docs/TODO.md` § 146.6:
            // *"the retrieval slide plays the LUNGE clip, because both are a body-led dash and
            // the rig has one. A slide of its own, which is art work rather than code work."*
            // The verb's two call sites in `CombatVerbs` (`ReleaseSlide` and `HostResolveSlide`)
            // asked for `"lunge"` by name, so the day a real slide clip landed somebody would
            // have had to find both of them and know which two of the four `PlayAction("lunge")`
            // calls were the slide's.
            //
            // ⚠️ THE FALLBACK IS BYTE-IDENTICAL TO WHAT `"lunge"` RESOLVES TO, on purpose: no
            // `slide` clip exists on the CC0 rig, so `Play` walks past it to `attack-kick-right`
            // exactly as before. **This is a rename with a hook in it, not a behaviour change.**
            // It is the same argument the `hero-*` block below makes for its own first slots:
            // when the team's own clip lands it drops in by name with no code change, which is
            // the entire reason these are chains. `ASTRA.md` task 3 is the clip.
            { "slide", new[] { "slide", "attack-kick-right", "attack-melee-right", Interact } },

            // -------------------------------------------------------------------
            // § THE HERO CASTS
            //
            // ⚠️⚠️ EVERY `hero-*` CLIP IN THE FIRST SLOT IS ASPIRATIONAL AND NONE OF THEM EXISTS
            // TODAY, WHICH MAKES THE SECOND SLOT THE ONE THAT SHIPS. The CC0 rig carries exactly
            // 43 named clips and not one is a hero cast; `Play` falls to the next name in the
            // chain, so what a player actually sees is entry two, every time, on every hero.
            //
            // ⚠️⚠️ AND ENTRY TWO USED TO BE `emote-yes` FOR ALL SIX ULTIMATES. 🧑 2026-08-29:
            // *"make sure theres an animation for all interactions like pushing tayaing or skill
            // casting, make the animations appropriate for skills and what theyre doing btw dont
            // js spam the same animation"*. Supernova, Thunderstrike, Titan Fissure, Glacial
            // Nova, Devouring Seance and Grand Coven were one nod of the head between them, and
            // `attack-melee-right` covered four more skills on top. The first slot being right
            // is what hid it: the table READS as eighteen distinct casts.
            //
            // ⚠️ SO THE SECOND SLOT IS CHOSEN FOR THE MOTION, not to fill the row. The rule
            // applied: no hero repeats a clip inside its own kit, no two ULTIMATES share one at
            // all, and where two heroes do share, the thing they are doing is the same thing (two
            // dashes, two casts thrown from the hand). Thirteen usable clips against eighteen
            // casts means some sharing is arithmetic; sharing it between a dash and a dash rather
            // than between six ultimates is the whole difference.
            //
            // ⚠️ THE FIRST SLOT STAYS. When the team's own cast animations land they drop in by
            // name with no code change, which is the entire reason these are chains.
            // -------------------------------------------------------------------

            { "hero-sean-dash", new[] { "hero-sean-dash", "attack-kick-right", Sprint } },
            { "hero-sean-ignite", new[] { "hero-sean-ignite", "attack-melee-right", Interact } },
            { "hero-sean-supernova", new[] { "hero-sean-supernova", Jump, "attack-melee-right" } },

            { "hero-zack-sprint", new[] { "hero-zack-sprint", Sprint, "attack-kick-right" } },
            { "hero-zack-charge", new[] { "hero-zack-charge", "emote-no", "attack-melee-right" } },
            { "hero-zack-summon", new[] { "hero-zack-summon", "holding-both-shoot", Jump } },

            { "hero-dante-stomp", new[] { "hero-dante-stomp", PickUp, "attack-kick-right" } },
            { "hero-dante-roar", new[] { "hero-dante-roar", "attack-melee-left", "emote-yes" } },
            { "hero-dante-fissure", new[] { "hero-dante-fissure", "attack-kick-left", PickUp } },

            { "hero-cheska-frostwave", new[] { "hero-cheska-frostwave", "interact-right", "attack-melee-right" } },
            { "hero-cheska-raise", new[] { "hero-cheska-raise", PickUp, Interact } },
            { "hero-cheska-nova", new[] { "hero-cheska-nova", "holding-left-shoot", Jump } },

            { "hero-nemu-ghoststep", new[] { "hero-nemu-ghoststep", Sprint, Walk } },
            { "hero-nemu-project", new[] { "hero-nemu-project", "interact-left", "attack-melee-left" } },
            { "hero-nemu-seance", new[] { "hero-nemu-seance", "emote-yes", Interact } },

            { "hero-phaister-hex", new[] { "hero-phaister-hex", "interact-right", "attack-melee-right" } },
            { "hero-phaister-blink", new[] { "hero-phaister-blink", "attack-kick-right", Sprint } },
            { "hero-phaister-eclipse", new[] { "hero-phaister-eclipse", Crouch, "holding-both" } },
        };

        [SerializeField] private float _blend = 0.12f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private Social.EmotePlayer _emote;
        private Animator _animator;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationLayerMixerPlayable _layers;
        private AnimationMixerPlayable _gait;
        private AnimationClipPlayable _walkGait, _runGait;
        private AvatarMask _legsMask;
        private float _gaitWeight, _runWeight, _gaitPhase;
        private bool _running;
        private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
#if UNITY_EDITOR
        private readonly List<AnimationClip> _generated = new List<AnimationClip>();
#endif

        private string _current;
        private float _weight;
        private float _oneShotLeft;
        private float _transitionSeconds;

        /// ⚠️⚠️ EVERY CLIP ON THESE RIGS IS 0.333 s AND EVERY ONE IS MARKED `isLooping = true`,
        /// MEASURED OFF ALL 29 GLBs ON 2026-08-26. That single fact is behind both halves of the
        /// fall reading wrong, and neither is guessable from reading the code:
        ///
        ///  * `SetDuration(clip.length)` does NOT stop an `AnimationClipPlayable` whose clip is
        ///    marked looping. So `die`, played with `loop: false` precisely so it would hold its
        ///    last frame, wrapped every third of a second instead: over the 1.6 s of a fall the
        ///    body dropped and sprang upright about five times. The comment above the `loop`
        ///    line already describes that exact bug as fixed. It was not; the flag on the asset
        ///    outranks the call.
        ///  * `pick-up` is also 0.333 s and was played at 1x into a 0.90 s floor, so the get-up
        ///    finished 0.57 s early and the body then held a bent-over pose for the remainder.
        ///    🧑, 2026-08-26: *"it js plays an animation and ur already up"*.
        ///
        /// `_holdAtEnd` is the general answer: a clip played non-looping is frozen on its last
        /// frame by this file rather than by the importer, so it holds whatever the asset says
        /// about itself. `_tripPhase` is the specific one: it drives the two clips of a fall
        /// explicitly and fits the get-up to `Balance.MinTripDown`.
        private bool _holdAtEnd;
        private int _tripPhase;

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
        public void Bind(GameObject model) => Bind(model, null);

        /// <summary>
        /// ⚠️ THE CLIPS ARE HANDED IN, NOT SEARCHED FOR. They come off the roster asset, which
        /// is the only reference that survives into a build. See the note on CacheClips.
        /// </summary>
        public void Bind(GameObject model, AnimationClip[] clips)
        {
            // ⚠️ A wardrobe/seat swap can rebind a live component. Retire the old output
            // before replacing its Animator, including when the replacement has no clips.
            ReleaseGraph();
            if (model == null) return;

            _animator = model.GetComponentInChildren<Animator>();
            if (_animator == null) _animator = model.AddComponent<Animator>();

            // ⚠️⚠️ AN AVATAR-LESS ANIMATOR PLAYS NOTHING AND REPORTS NOTHING. glTFast emits an
            // Animator with a null controller, which is right for Playables, and no Avatar,
            // which is not: an animation output bound to one drives no transforms at all and
            // the whole cast stands in its bind pose. See ModelPreview.EnsureAvatar.
            UI.ModelPreview.EnsureAvatar(_animator);

            CacheClips(model, clips);

            if (_clips.Count == 0)
            {
                Debug.LogWarning($"[Anim] {name} has no clips. Check the model's animationType " +
                                 "is Generic and importAnimation is on.");
                return;
            }

            _graph = PlayableGraph.Create($"Anim_{name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            _layers = AnimationLayerMixerPlayable.Create(_graph, 2);
            _layers.ConnectInput(0, _mixer, 0, 1f);
            BuildGaitLayer();

            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            output.SetSourcePlayable(_layers);

            _graph.Play();
            Play(Idle, loop: true, force: true);
        }

        /// <summary>
        /// ⚠️ CLIPS COME FROM THE SOURCE ASSET, NOT FROM THE INSTANCE. An instantiated model
        /// carries no AnimationClip references of its own, so they are loaded from the imported
        /// asset the prefab came from.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ THE CLIPS COME FROM THE ROSTER ASSET, AND EVERY OTHER ROUTE TO THEM IS BROKEN IN
        /// A BUILD. This used to ask the AssetDatabase for the model's source path and fall back
        /// to `Resources.FindObjectsOfTypeAll`. Both fail, for different reasons:
        ///
        ///   - the model is `Instantiate`d at runtime, so it has no prefab link to trace back to
        ///     the `.glb`, and the editor branch found nothing even in the editor, and
        ///   - a clip that nothing references is stripped from the player, so the runtime branch
        ///     searched a set that was empty by construction.
        ///
        /// The whole cast stood still, in every build, with one warning per seat in a log nobody
        /// reads mid-match. `RosterEntryAsset.Clips` is a real serialised reference, which is
        /// what both makes them ship and makes them findable.
        /// </summary>
        private void CacheClips(GameObject model, AnimationClip[] supplied)
        {
            _clips.Clear();

            if (supplied != null)
            {
                foreach (var c in supplied)
                {
                    if (c == null || c.name.StartsWith("__preview")) continue;
                    _clips[c.name] = c;
                }
            }

            if (_clips.Count == 0)
            {
#if UNITY_EDITOR
                // A last resort for a model dropped straight into a scene by hand, which is how
                // a probe or a test fixture usually builds one.
                string path = UnityEditor.AssetDatabase.GetAssetPath(
                    UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(model) ?? (Object)model);

                if (!string.IsNullOrEmpty(path))
                {
                    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                        if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                            _clips[c.name] = c;
                }
#endif
            }

            // ⚠️ LAST, AFTER BOTH IMPORT ROUTES HAVE HAD THEIR SAY. The count test above asks
            // "did this rig ship any animation at all", and a clip we generated ourselves would
            // answer yes for a model that imported nothing, silently skipping the fallback
            // exactly where it is needed.
            BuildGeneratedClips();
        }

        /// <summary>
        /// Clips this project authors from mathematical curves rather than imports. The dance
        /// is baked; the older hero-cast path below is tracked separately in TODO § 80.8.
        ///
        /// ⚠️⚠️ THE DANCE IS BAKED AS AN ASSET, NOT BUILT IN THE PLAYER. `AnimationClip.SetCurve`
        /// populates a non-legacy clip in the editor and is editor-only for that clip type at
        /// runtime. The old code called it here in every build, received a valid clip with zero
        /// curves, and then preferred that empty `generated-dance` over `idle`. A valid empty
        /// clip is the bind pose, which is exactly the reported dance T-pose.
        ///
        /// `GeneratedAnimationAuthor` writes one set under Resources per rig hierarchy, which
        /// preserves the curves in a player. The outer node is model-specific, so sharing one
        /// set would bind `character-male-f/root` onto `character-female-f` and move nothing.
        /// `GeneratedAnimationTests` asserts the selected set against every roster model.
        ///
        /// ⚠️ THE EDITOR FALLBACK EXISTS FOR HAND-BUILT TEST FIXTURES ONLY. It is compiled out
        /// of a player so this fault cannot return as an apparently successful empty clip.
        /// </summary>
        private void BuildGeneratedClips()
        {
            if (_animator == null) return;

            string rig = DanceClip.ResourceName(_animator.transform);
            var baked = string.IsNullOrEmpty(rig)
                ? null
                : Resources.Load<GeneratedAnimationSet>($"{DanceClip.ResourceFolder}/{rig}");

            if (baked != null && baked.Clips != null)
            {
                foreach (var clip in baked.Clips)
                    if (clip != null) _clips[clip.name] = clip;
            }

#if UNITY_EDITOR
            if (!_clips.ContainsKey(DanceClip.ClipName))
            {
                var dance = DanceClip.Build(_animator.transform);
                if (dance != null) { _clips[DanceClip.ClipName] = dance; _generated.Add(dance); }
            }
#endif

#if UNITY_EDITOR
            // ⚠️ SetCurve cannot build these non-legacy clips in a player. Authored
            // roster clips own the shipping route; missing clips use the explicit chains.
            var heroClips = HeroAbilityClips.BuildAll(_animator.transform);
            if (heroClips != null)
            {
                foreach (var kvp in heroClips)
                {
                    if (kvp.Value == null) continue;
                    if (!_clips.ContainsKey(kvp.Key)) { _clips[kvp.Key] = kvp.Value; _generated.Add(kvp.Value); }
                    else DestroyImmediate(kvp.Value);
                }
            }
#endif
        }

        private void OnDestroy() => ReleaseGraph();

        private void ReleaseGraph()
        {
            ClearChargePose();
            if (_graph.IsValid()) _graph.Destroy();
#if UNITY_EDITOR
            foreach (var clip in _generated) if (clip != null) DestroyImmediate(clip);
            _generated.Clear();
#endif
            if (_legsMask != null)
            {
                if (Application.isPlaying) Destroy(_legsMask);
                else DestroyImmediate(_legsMask);
            }
            _legsMask = null;
            _current = null;
            _oneShotLeft = _gaitWeight = _runWeight = _gaitPhase = 0;
            _tripPhase = 0;
            _running = false;
        }

        private void Update()
        {
            if (!_graph.IsValid()) return;

            // Charge state is resolved first, with the arm applied after graph evaluation.
            // The lower-body layer can keep moving throughout the held preparation.
            StepChargePose();
            StepGait();

            // ⚠️ BEFORE THE ONE-SHOT, because a fall interrupts whatever the body was doing.
            // A player tripped mid-throw must be on the tarmac, not finishing the throw.
            if (StepTripPose()) return;

            if (_oneShotLeft > 0.0f)
            {
                _oneShotLeft -= Time.deltaTime;
                Blend();
                HoldLastFrame();
                return;
            }

            // ⚠️ NOT EVERYTHING LOOPS. Looping `die` is exactly the reported bug: the body
            // drops, the clip ends, it restarts from standing and drops again — 🧑 *"im perma
            // jumping up and down the floor and lying down"*. Locomotion loops; an emote loops
            // only if EmoteLoops says so, and otherwise holds its last frame.
            bool emoting = _emote != null && _emote.IsEmoting;
            bool tripped = _motor != null && _motor.IsTripped;
            bool loop = (!emoting || !EmoteHoldsLastFrame(_emote.Current)) && !tripped;

            Play(Choose(), loop);
            Blend();
            HoldLastFrame();

            StepEmoteFinished(emoting);
        }

        private void BuildGaitLayer()
        {
            if (!_clips.TryGetValue(Walk, out var walk) || !_clips.TryGetValue(Sprint, out var run)) return;
            CalibrateGait(walk,run);
            _gait = AnimationMixerPlayable.Create(_graph, 2);
            _walkGait = AnimationClipPlayable.Create(_graph, walk);
            _runGait = AnimationClipPlayable.Create(_graph, run);
            _walkGait.SetApplyFootIK(false); _runGait.SetApplyFootIK(false);
            _walkGait.SetSpeed(0); _runGait.SetSpeed(0);
            _gait.ConnectInput(0, _walkGait, 0, 1f);
            _gait.ConnectInput(1, _runGait, 0, 0f);
            _legsMask = new AvatarMask();
            var transforms = _animator.GetComponentsInChildren<Transform>();
            _legsMask.transformCount = transforms.Length;
            for (int i = 0; i < _legsMask.transformCount; i++)
            {
                // Mask paths, like clip bindings, are relative to this Animator.
                // AddTransformPath includes the seat hierarchy on a spawned instance.
                var transform = transforms[i];
                string path = "";
                for (var step = transform; step != _animator.transform; step = step.parent)
                    path = string.IsNullOrEmpty(path) ? step.name : step.name + "/" + path;
                _legsMask.SetTransformPath(i, path);
                string bone = transform.name;
                // Root lift belongs to the planted gait; torso and arms belong to the
                // carry/fatigue pose. This preserves the original anti-swimming fix.
                _legsMask.SetTransformActive(i, bone == "root" || bone == "leg-left" || bone == "leg-right");
            }
            _layers.ConnectInput(1, _gait, 0, 0f);
            _layers.SetLayerMaskFromAvatarMask(1, _legsMask);
        }

        private float FlatSpeed => new Vector2(_motor.Velocity.x, _motor.Velocity.z).magnitude;
        private float _walkReference = 2.8f;
        private float _runReference = 5.4f;
        private float _walkCycleMetres = .92f;
        private float _runCycleMetres = 1.12f;
        public float FootfallCycleMetres => Mathf.Lerp(_walkCycleMetres,_runCycleMetres,_runWeight);

        private void CalibrateGait(AnimationClip walk, AnimationClip run)
        {
            float reach=0;
            foreach(var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=skin.sharedMesh;
                if(mesh==null)continue;
                // Bind matrices and mesh bounds remain pose-independent, including
                // a rebind during a cast. No vertex readback or per-frame scan.
                var binds=mesh.bindposes;
                var bones=skin.bones;
                for(int i=0;i<bones.Length && i<binds.Length;i++)
                {
                    if(bones[i]==null || (bones[i].name!="leg-left" && bones[i].name!="leg-right"))continue;
                    float localReach=binds[i].inverse.MultiplyPoint3x4(Vector3.zero).y-mesh.bounds.min.y;
                    reach=Mathf.Max(reach,localReach*skin.transform.TransformVector(Vector3.up).magnitude);
                }
            }
            _walkReference=2.8f; _runReference=5.4f;
            if(reach>.05f && reach<1.5f)
            {
                // One stance crosses 2*reach*sin(swing); two stances form a cycle.
                _walkReference=4f*reach*Mathf.Sin(WalkLegSwingDegrees*Mathf.Deg2Rad)/Mathf.Max(.05f,walk.length);
                _runReference=4f*reach*Mathf.Sin(RunLegSwingDegrees*Mathf.Deg2Rad)/Mathf.Max(.05f,run.length);
            }
            _walkCycleMetres=_walkReference*walk.length;
            _runCycleMetres=_runReference*run.length;
        }

        private float OrdinaryWalkSpeed => Core.Balance.Speed * Core.Stamina.RoleSpeedScale(_motor.IsDefender)
            * Core.Roster.PersonSpeedScale(_motor.CharacterIndex, _motor.Mode)
            * Mathf.Max(.1f, _motor.Stamina.SpeedZones.Value);

        private void StepGait()
        {
            float speed = FlatSpeed;
            _running = speed > OrdinaryWalkSpeed * (_running ? 1.10f : 1.22f);
            if (!_gait.IsValid()) return;
            bool layered = _motor.IsGrounded && !_motor.IsTripped && _oneShotLeft <= 0
                && (_emote == null || !_emote.IsEmoting)
                && (_carrier == null || _carrier.ChannelRatio <= 0)
                && (_motor.HoldingSlipper || _motor.Stamina.IsFatigued || _chargePosing);
            float target = layered && speed > WalkSpeedThreshold ? 1f : 0f;
            _gaitWeight = Mathf.MoveTowards(_gaitWeight, target, Time.deltaTime / .08f);
            // Accepted actions immediately own every bone. A leg layer lingering over a
            // slide or stomp would erase its support pose at the moment of commitment.
            if (_oneShotLeft > 0 || _motor.IsTripped || !_motor.IsGrounded) _gaitWeight = 0;
            _runWeight = Mathf.MoveTowards(_runWeight, _running ? 1f : 0f, Time.deltaTime / .10f);
            float reference = Mathf.Lerp(_walkReference, _runReference, _runWeight);
            float length = Mathf.Lerp(_walkGait.GetAnimationClip().length, _runGait.GetAnimationClip().length, _runWeight);
            _gaitPhase = (_gaitPhase + Time.deltaTime * Mathf.Clamp(speed / reference, .05f, 2.6f) / length) % 1f;
            _walkGait.SetTime(_gaitPhase * _walkGait.GetAnimationClip().length);
            _runGait.SetTime(_gaitPhase * _runGait.GetAnimationClip().length);
            _gait.SetInputWeight(0, 1f - _runWeight); _gait.SetInputWeight(1, _runWeight);
            _layers.SetInputWeight(1, _gaitWeight);
            var front = Front();
            if (front.IsValid() && (_current == Walk || _current == Sprint))
                front.SetSpeed(Mathf.Clamp(speed / (_current == Sprint ? _runReference : _walkReference), .05f, 2.6f));
        }

        /// <summary>
        /// ⚠️⚠️ DORMANT BY DESIGN. `character_visual.gd::_on_animation_finished` does nothing
        /// when a non-looping clip reaches its end — no restart, no signal, no stop — so
        /// "sit"/"crouch"/"tpose" hold their pose exactly as long as the looping gestures do,
        /// until something else calls `stop_emote()`. §3aa of this repo's own rules records the
        /// same thing from the other direction: *"the emotes only end when a user does smth to
        /// interrupt it... it doesnt end on its own"*.
        ///
        /// This exists so a genuinely one-shot gesture has somewhere to plug in later without
        /// inventing a second restore path (see `EmotePlayer.Update`, which routes it through
        /// the same `Stop()` every other interruption uses). `EmoteEndsOnClipFinish` is empty on
        /// purpose: none of the seven current emotes opt in, and adding one is a one-line change
        /// here rather than a new code path.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> EmoteEndsOnClipFinish =
            new System.Collections.Generic.HashSet<string>();

        private string _emoteClipPlaying;
        private float _emoteClipStartTime;

        /// <summary>True for exactly one frame's worth of polling once an opted-in emote's clip
        /// has played past its own length. <see cref="EmotePlayer"/> reads this and calls
        /// `Stop()`, which clears `Current` and this goes back to false with it.</summary>
        public bool EmoteClipFinished { get; private set; }

        private void StepEmoteFinished(bool emoting)
        {
            EmoteClipFinished = false;

            if (!emoting || _emote.Current == null || !EmoteEndsOnClipFinish.Contains(_emote.Current))
            {
                _emoteClipPlaying = null;
                return;
            }

            string clipName = ResolveChain(EmoteClips, _emote.Current);
            if (clipName == null || !_clips.TryGetValue(clipName, out var clip)) return;

            if (_emoteClipPlaying != clipName)
            {
                _emoteClipPlaying = clipName;
                _emoteClipStartTime = Time.time;
                return;
            }

            EmoteClipFinished = Time.time - _emoteClipStartTime >= clip.length;
        }

        /// <summary>Can this rig actually play emote <paramref name="id"/>? False for an unknown
        /// id or a model with no clip any of its candidates resolve to — see the note on
        /// <see cref="EmotePlayer.Play"/> for why this gates the camera swing.</summary>
        public bool HasEmoteClip(string id) => ResolveChain(EmoteClips, id) != null;

        /// <summary>
        /// ⚠️ THE ORDER OF THESE BRANCHES IS THE PRIORITY, and it is not arbitrary.
        ///
        /// ⚠️⚠️ A STUN DOES **NOT** PLAY `die`, AND THIS FILE USED TO SAY IT DID. 🧑 on this
        /// build: *"idk why but the bots randomly go to the ground and start flipping around"*.
        /// The line was `if (_motor.IsStunned) return Die;` with a header arguing that stunned
        /// should beat everything — but `die` is the knockdown clip, so every ordinary shove
        /// stun and every 5 s tag penalty dropped the body flat on the road. It happens to bots
        /// most because bots get tagged most, which is why it read as a bot bug.
        ///
        /// The *"flipping around"* was the other half, and the two compounded: nothing stopped a
        /// stunned unit steering, so `CharacterMotor.Steer` kept snapping the body's yaw to each
        /// new AI heading while it lay on the floor. Both are fixed; see that file's own note.
        ///
        /// `character_visual.gd:1806` is the reference and it is precise about the distinction:
        ///
        ///     if _character.state == CharacterBase.State.DOWNED:
        ///         wanted = "die"
        ///
        /// DOWNED, not STAGGERED. They are separate states there, `go_downed()` has no callers
        /// anywhere in the Godot tree, and an ordinary stun is STAGGERED — which falls straight
        /// through this chain to its normal locomotion pose. What actually reads a stun in the
        /// original is not a clip at all: it is § THE STUN FROST, a material effect over the
        /// whole body (`character_visual.gd:1879`).
        ///
        /// ⚠️ AND BOTH HALVES OF THAT ARE ALREADY BUILT HERE, which is what makes removing the
        /// clip safe rather than a net loss of feedback: `CharacterVisual.ProcessFrost` drives
        /// `_FrostAmount` on the body for everyone watching, and `Hud.UpdateFrost` runs the
        /// screen vignette for the victim. So a stun is read by the same two channels the
        /// original reads it by, and the knockdown pose is not one of them.
        ///
        /// `Die` is kept below: the `dead` EMOTE resolves to it, which is the one place the
        /// knockdown pose is played on purpose.
        /// </summary>
        private string Choose()
        {
            // ⚠️ THE TRIP IS NOT CHOSEN HERE ANY MORE. It used to be one line,
            // `_motor.TripLeft > 0.7f ? Die : PickUp`, which picked the right two clips and
            // could say nothing about their SPEED or about holding a frame, and both of those
            // turned out to be the whole defect. See `StepTripPose`, which runs before this and
            // owns a fall end to end. The 0.70 also disagreed with `Balance.MinTripDown` by
            // 0.20 s for no reason anybody recorded.

            if (_emote != null && _emote.IsEmoting)
            {
                string emoteClip = ResolveChain(EmoteClips, _emote.Current);
                if (emoteClip != null) return emoteClip;
            }

            if (_carrier != null && _carrier.ChannelRatio > 0.0f) return Interact;

            // ⚠️⚠️ THE **OBSERVED** WIND-UP, NOT THIS PEER'S OWN CHARGE CLOCK, AND THE
            // DIFFERENCE IS THE ENTIRE COUNTERPLAY. `carrier.gd`'s header spells it out: the
            // charge timer only ticks on the peer that controls the unit, so a third-person
            // pose driven from it is invisible to the person being AIMED AT. The 2.5 s charge
            // exists precisely so *"the taya now has a window in which they can see an attacker
            // winding up and act on it"*, and a wind-up nobody else can see deletes that window
            // while leaving the cost.
            if (_chargePosing) return _motor.HoldingSlipper ? HoldingRight : Idle;

            if (!_motor.IsGrounded) return _motor.Velocity.y > 0.5f ? Jump : Fall;

            // ⚠️⚠️ FATIGUE HAS A POSE, AND THE PORT HAD NONE. 🧑 asked the original for *"a
            // heavy panting animation"* on the empty bar, and `character_visual.gd` answers it
            // with `crouch` — doubled over with the weight forward, the universal hands-on-knees
            // silhouette and the only clip on this rig that reads as exhaustion rather than as
            // an action. Without it, running the bar dry changed nothing a player could see,
            // which is *"shift drains stamina but it isnt noticeable like in godot"*.
            //
            // ⚠️ IT OUTRANKS WALK AND SPRINT, NOT STUN OR AIRBORNE. A fatigued player is still
            // moving, at three-quarter speed, and the whole point is that the state is legible
            // to the three people deciding whether to chase them.
            if (_motor.Stamina.IsFatigued) return Crouch;

            // ⚠️ The historical fix below still owns the UPPER BODY. BuildGaitLayer now
            // replaces only root lift and legs, so its original grip guarantee survives.
            // ⚠️⚠️ CARRYING BEATS WALK AND SPRINT OUTRIGHT, NOT ONLY WHEN STANDING STILL, AND
            // GETTING THAT WRONG IS *"the hands are just floating"*. The rig has no
            // holding-right-walk, so a carrying unit that switches to `walk` swings the arm bone
            // through a full walk cycle — and the carried tsinelas is snapped to that bone every
            // frame while the first-person viewmodel chases the same pose. Both then swim with
            // every step. `character_visual.gd` records the same conclusion and the report that
            // forced it: *"my arms float during windup and when i run while holding"*. Legs that
            // stop swinging while carrying are a far smaller cost than a hand that does.
            if (_motor.HoldingSlipper) return HoldingRight;

            Vector3 flat = _motor.Velocity;
            flat.y = 0.0f;
            float speed = flat.magnitude;

            // ⚠️ Observed speed still owns gait selection. The former fixed 7.5 m/s
            // threshold never admitted an ordinary 3.795 m/s attacker sprint. StepGait
            // compares against this role's walking range with hysteresis on every peer.
            if (_running) return Sprint;
            if (speed > WalkSpeedThreshold) return Walk;

            return Idle;
        }

        /// <summary>The established stationary dead zone. Running uses the role's range.</summary>
        public const float WalkSpeedThreshold = 0.4f;

        // ---- THE THIRD-PERSON WIND-UP ---------------------------------------

        /// <summary>
        /// How far the arm cocks back at full charge, in radians. ⚠️ READ FROM THE VIEWMODEL
        /// RATHER THAN RESTATED: this is the same wind-up the thrower sees on their own arm, and
        /// the two views disagreeing about how far back it went would be its own bug.
        /// </summary>
        public const float ChargePoseRad = CameraSystem.ViewmodelArms.WindupRad;

        /// <summary>
        /// ⚠️ THE SIGN AND THE AXIS ARE MEASURED. `character_visual.gd` records that the first
        /// build of this used -X and the hand DROPPED instead of cocking. A bone's local basis
        /// is not readable by eye; flip the constant, never the caller.
        /// </summary>
        private static readonly Vector3 ChargePoseAxis = new Vector3(1.0f, 0.0f, 0.0f);

        private static readonly string[] ChargePoseBones = { "arm-right", "arm-left" };

        private Transform _chargeBone;
        private Quaternion _chargeBoneRest;
        private bool _chargePosing;

        /// <summary>
        /// The wind-up, written onto the arm bone so OPPONENTS can read the commitment.
        ///
        /// ⚠️⚠️ THE PORT HAD ONLY THE FIRST-PERSON HALF, WHICH IS THE HALF NOBODY ELSE SEES.
        /// `Design.md` §4 says the whole wind-up "is visible on every peer ... so the attacker
        /// can dash, jump or throw through the commitment", and §11's counterplay table answers
        /// the charged melee with exactly *"1.35 s of visible wind-up"*. With nothing on the
        /// body, that counterplay was being PAID for by the charging player and shown to nobody:
        /// a charge you cannot see is a charge you cannot dodge.
        ///
        /// ⚠️ THREE CHARGES WIND THIS ARM UP, NOT ONE. The throw is the obvious one and was the
        /// only one the .gd had at first — which left the taya, who holds nothing, posing
        /// nothing for the entire defending role. 🧑 on that build: *"is that on purpose theres
        /// no taya animation? can u make sure theres an animation or atleast a hand movement for
        /// all movements"*. The lunge and the shove are the other two, and all three report a
        /// 0..1 ratio with -1 at rest, so they compose without any of them knowing about the
        /// others.
        ///
        /// ⚠️⚠️ AND THE PLAYBACK HAS TO STOP, WHICH IS THE WHOLE DIFFERENCE BETWEEN A POSE AND A
        /// FLICKER. Every clip on this rig keys `arm-right`, and the graph writes the bone after
        /// this component's Update — so a bone write that merely races it lands only on frames
        /// where the clip happens to have ended. Measured in the original: 0.025 m of hand
        /// travel while racing, against 0.62 rad of rotation that should move it eight times
        /// that. With the graph stopped nothing else writes the bone and the pose is exactly
        /// what this function says it is.
        /// </summary>
        /// <returns>True while the upper-body charge override is active.</returns>
        private bool StepChargePose()
        {
            float power = ObservedCharge();
            bool winding = power >= 0.0f && !_motor.IsStunned;

            if (!winding)
            {
                if (_chargePosing) ClearChargePose();
                return false;
            }

            if (!_chargePosing)
            {
                if (!ResolveChargeBone()) return false;

                _chargePosing = true;
            }

            return true;
        }

        private void LateUpdate()
        {
            // ⚠️ Evaluate after the graph, before Carrier's LateUpdate reads the hand.
            // Stopping the entire graph froze moving legs throughout every charge.
            if (!_chargePosing || _chargeBone == null) return;
            _chargeBoneRest = _chargeBone.localRotation;
            _chargeBone.localRotation = _chargeBoneRest * Quaternion.AngleAxis(
                ChargePoseRad * Mathf.Clamp01(ObservedCharge()) * Mathf.Rad2Deg, ChargePoseAxis);
        }

        /// <summary>The deepest live charge on this unit, or -1 when none is running. The three
        /// are mutually exclusive in practice — an attacker cannot lunge and a defender cannot
        /// throw — so the first one answering wins.</summary>
        private float ObservedCharge()
        {
            if (_carrier != null && _carrier.Held != null && _carrier.ObservedChargePower >= 0.0f)
                return Mathf.Clamp01(_carrier.ObservedChargePower);

            var verbs = _motor != null ? _motor.GetComponent<CombatVerbs>() : null;
            if (verbs == null) return -1.0f;

            if (verbs.ObservedLungeCharge >= 0.0f) return Mathf.Clamp01(verbs.ObservedLungeCharge);

            // ⚠️ NO SHOVE BRANCH, AND ITS ABSENCE IS CORRECT RATHER THAN AN OMISSION. The .gd
            // has one because the shove used to be a 1.25 s hold; it became a single tap and
            // was taken off the defender entirely, so that clock jumps 0 to 1 in one frame and
            // has nothing to tell anybody. Do not add a tell for a verb with no wind-up.
            return -1.0f;
        }

        private bool ResolveChargeBone()
        {
            var skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned == null || skinned.bones == null) return false;

            foreach (string wanted in ChargePoseBones)
            {
                foreach (var bone in skinned.bones)
                {
                    if (bone == null || bone.name != wanted) continue;

                    _chargeBone = bone;

                    // The pose the clip left the bone in, restored on release so two wind-ups
                    // cannot accumulate.
                    _chargeBoneRest = bone.localRotation;
                    return true;
                }
            }

            return false;
        }

        private void ClearChargePose()
        {
            _chargePosing = false;

            if (_chargeBone != null) _chargeBone.localRotation = _chargeBoneRest;
            _chargeBone = null;

            // ⚠️ THE GRAPH RESTARTS BEFORE ANYTHING ELSE PLAYS. The end of a charge is usually
            // a THROW, and the release fires that one-shot through `PlayAction` on the very next
            // frame; a stopped graph would swallow the one part of the verb everybody watches.
            if (_graph.IsValid()) _graph.Play();
        }

        /// <summary>True while a one-shot still owns the body. Read by callers that want to
        /// REPEAT a read over a hold that outlasts it, without cutting the previous one off
        /// part-played. See `Carrier.StepDefender`, where the reset channel runs far longer than
        /// the gesture that announces it.</summary>
        public bool IsPlayingAction => _oneShotLeft > 0.0f;

        public void CancelHeroAction(string expected = null, string viewmodel = null)
        {
            if (_current == null || !_current.StartsWith("hero-")) return;
            if (expected != null && ResolveChain(ActionClips,expected) != _current) return;
            _oneShotLeft = 0;
            ClearChargePose();
            CameraSystem.CameraRig.CancelViewmodelAction(_motor,viewmodel);
            Play(Idle,true,true);
        }

        /// <summary>A non-looping action that owns the body until it finishes.</summary>
        public void PlayOneShot(string clipName)
        {
            if (!_graph.IsValid() || !_clips.TryGetValue(clipName, out var clip)) return;

            ClearChargePose();
            Play(clipName, loop: false, force: true);
            _oneShotLeft = clip.length;
            if (_layers.IsValid()) _layers.SetInputWeight(1, 0f);
            _gaitWeight = 0f;
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
        public void PlayAction(string action) => PlayAction(action, action);

        /// <summary>
        /// Some hero verbs use a full-body motion and a different first-person hand motion.
        /// They still enter through this one bridge so the two views can never be triggered
        /// independently.
        /// </summary>
        public void PlayAction(string action, string viewmodelAction)
        {
            // ⚠️⚠️ THE FIRST-PERSON ARM IS DRIVEN FROM HERE, AND FROM NOWHERE ELSE.
            // `character_visual.gd::play_action` opens with exactly this call and says why:
            // *"the third-person model has always animated here; in FIRST person the player sees
            // the viewmodel instead, and it was static. Driven from this one call site rather
            // than from the input code so the two views can never disagree about whether a throw
            // happened"*. 🧑 re-reported it against this port in the same words as the original
            // playtest: *"make sure my arm moves or does an animation when i interact with
            // objects like in the real game — raise can, tag someone"*.
            //
            // ⚠️ BEFORE THE CLIP LOOKUP, NOT AFTER, AND THE .gd IS EMPHATIC ABOUT THE ORDER: a
            // Prop has no AnimationPlayer, so resolving the body clip first and returning on a
            // miss would leave the rig untold. Every verb already calls this one method, so
            // throw, grab, shove, punch and lunge all reach the viewmodel for free — and a verb
            // added later cannot forget to.
            CameraSystem.CameraRig.PlayViewmodelAction(_motor, viewmodelAction);

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

        /// <summary>
        /// The clip playable currently on the front input of the mixer, or an invalid handle.
        /// </summary>
        private AnimationClipPlayable Front()
        {
            var input = _mixer.IsValid() ? _mixer.GetInput(1) : default;
            return input.IsValid() && input.IsPlayableOfType<AnimationClipPlayable>()
                ? (AnimationClipPlayable)input
                : default;
        }

        private float ClipLength(string clipName)
            => _clips.TryGetValue(clipName, out var clip) ? clip.length : 0.0f;

        /// <summary>
        /// Freeze a non-looping clip on its last frame.
        ///
        /// ⚠️⚠️ THIS IS DONE HERE BECAUSE THE ASSETS SAY OTHERWISE. Every clip on these rigs
        /// imports with `isLooping = true`, and a looping clip wraps regardless of what
        /// `SetDuration` was told, so "played non-looping" was a description of intent rather
        /// than of behaviour. Zeroing the speed one frame short of the end holds the pose for
        /// as long as the state that asked for it lasts.
        /// </summary>
        /// <summary>
        /// Runs the end of whatever is on the front input: a non-looping clip freezes on its last
        /// pose, a looping one wraps back to its start.
        ///
        /// ⚠️⚠️ THE WRAP IS DONE HERE BECAUSE `SetDuration` DOES NOT DO IT, AND THAT IS 🧑
        /// 2026-08-29: *"sa dance emote dalawang swings lang sya and nag stostop na sya
        /// sumayaw"*. `Play` asks for a loop with `playable.SetDuration(double.MaxValue)`, and
        /// that is a statement about when the PLAYABLE is finished, not about what the CLIP does
        /// when it runs off its own end. An `AnimationClipPlayable` past `clip.length` holds the
        /// final pose unless the clip asset itself is marked Loop Time in the importer, so an
        /// infinite duration on an unlooped clip buys a pose that never ends rather than a groove
        /// that never ends. `EmoteLoops` said `{ "dance", true }` and the dance still played
        /// once and froze, which is why reading that table was not enough to find this: both
        /// halves looked correct and the disagreement was between them.
        ///
        /// ⚠️ WRAPPED HERE RATHER THAN FIXED IN THE IMPORTER, DELIBERATELY. `CLAUDE.md` § 4a:
        /// every clip in the project is placeholder and the team's own animations are coming.
        /// An import setting is a property of THIS asset that the replacement arrives without,
        /// so the loop would break again on the commit that swaps the art, silently, and the
        /// report would come back a second time. This holds whatever the clips are, which is the
        /// same reason `CharacterAnimator` reads clip names off the asset instead of assuming
        /// them.
        ///
        /// ⚠️ IT IS A NO-OP ON A CLIP THAT ALREADY LOOPS. `GetTime` accumulates past the clip's
        /// length whether or not the clip wraps visually, so on a properly imported locomotion
        /// clip this rewrites the time to the phase it was already showing.
        /// </summary>
        private void HoldLastFrame()
        {
            var front = Front();
            if (!front.IsValid()) return;

            var clip = front.GetAnimationClip();
            double length = clip != null ? clip.length : 0.0;
            if (length <= 0.0) return;

            if (!_holdAtEnd)
            {
                // ⚠️⚠️ ONLY A CLIP THAT DOES NOT LOOP ITSELF, AND THE FIRST VERSION OF THIS DID
                // IT TO EVERYTHING. `CarryTests` caught it: a held tsinelas drifted 0.063 m from
                // the hand against a 0.050 bound while its carrier walked. Rewriting the time on
                // a clip that ALREADY wraps sends it back exactly one cycle, and a walk cycle
                // whose first and last frames are not identical pops the hand by that seam on the
                // frame it happens. The locomotion clips are imported with Loop Time and need no
                // help; the emotes are the ones that do not.
                //
                // ⚠️ `clip.isLooping` IS THE IMPORT SETTING, which is exactly the question being
                // asked: "does this asset wrap on its own, or do I have to". It is the one place
                // in this class that reads an import flag, and it reads it to decide whether to
                // COMPENSATE for it rather than to depend on it, so the note in `HoldLastFrame`'s
                // header about the art being replaced still holds. A replacement clip that
                // arrives unlooped gets the wrap; one that arrives looped does not need it.
                if (clip.isLooping) return;

                double time = front.GetTime();

                // ⚠️ THE REMAINDER, NOT ZERO. Snapping to 0 on the frame the clip runs out
                // throws away however far past the seam this frame's delta carried it, which at
                // 30 fps is a third of the frames landing on the downbeat twice.
                if (time >= length) front.SetTime(time % length);

                return;
            }

            if (front.GetTime() >= length - 0.001)
            {
                front.SetTime(length - 0.001);
                front.SetSpeed(0.0);
            }
        }

        /// <summary>
        /// A fall, end to end: the knockdown held on the tarmac, then the get-up time-scaled so
        /// it lands exactly as control returns.
        ///
        /// ⚠️⚠️ IT OWNS BOTH CLIPS AND THEIR SPEED, WHICH `Choose` COULD NOT. `Choose` returns a
        /// clip NAME, and the two things wrong with the fall were a clip that would not stay on
        /// the floor and a clip that finished 0.57 s before the player could move. Neither is
        /// expressible as a name.
        ///
        /// ⚠️ THE SWITCH IS `Balance.MinTripDown`, THE SAME NUMBER THE MASH FLOOR AND THE HUD
        /// USE. Below it nothing can be bought, the prompt has already changed to GETTING UP,
        /// and the body should be getting up. A separate 0.70 lived here and left 0.20 s where
        /// all three disagreed.
        /// </summary>
        private bool StepTripPose()
        {
            if (_motor == null || !_motor.IsTripped)
            {
                _tripPhase = 0;
                return false;
            }

            if (_motor.TripLeft <= Core.Balance.MinTripDown)
            {
                _oneShotLeft = 0;
                if (_tripPhase != 2)
                {
                    _tripPhase = 2;
                    Play(PickUp, loop: false, force: true);

                    // ⚠️ SOLVED FROM THE CLIP, NOT TYPED. `pick-up` measures 0.333 s on every
                    // rig today, which is 0.37x here, but a re-export that changes the clip must
                    // not silently reintroduce a get-up that ends early.
                    float length = ClipLength(PickUp);
                    if (length > 0.0f)
                    {
                        var front = Front();
                        if (front.IsValid())
                            front.SetSpeed(length / Core.Balance.MinTripDown);
                    }
                }
            }
            else if (_tripPhase != 1)
            {
                // A fall interrupts the action instead of resuming a stale spell
                // after the complete get-up. This does not cancel ability gameplay.
                _oneShotLeft = 0;
                CameraSystem.CameraRig.CancelViewmodelAction(_motor);
                _tripPhase = 1;
                Play(Die, loop: false, force: true);
            }

            Blend();
            HoldLastFrame();
            return true;
        }

        private void Play(string clipName, bool loop, bool force = false)
        {
            if (!force && clipName == _current) return;
            if (!_clips.TryGetValue(clipName, out var clip) || !_mixer.IsValid()) return;

            // Retire whatever was on input 0 and slide the current clip down to it, so the
            // mixer always crossfades from what you were doing to what you are doing now.
            //
            // ⚠️⚠️ THE OUTGOING PLAYABLE IS CAPTURED BEFORE THE DISCONNECT, AND READING IT AFTER
            // WAS THE BUG. `PlayableGraph.Disconnect` sets that input to `Playable.Null`, so the
            // old line (disconnect, then `ConnectInput(0, _mixer.GetInput(1), 0)`) connected
            // NOTHING to input 0 on every single clip change. Nothing threw and nothing logged,
            // because connecting an invalid source is a silent no-op.
            //
            // What it cost is the whole crossfade. An `AnimationMixerPlayable` whose weights sum
            // below one fills the remainder from the stream's DEFAULT values, which is the rig's
            // bind pose, so for the first `_blend` seconds of every transition the body was being
            // blended out of a T-pose rather than out of the clip it was actually in. On
            // locomotion that is a twitch you could mistake for footwork. On a ONE-SHOT it is the
            // reported bug: a jab or a kick is about a third of a second, so the blend-in is most
            // of the clip and the verb reads as a snap and a shrug rather than as a swing.
            // 🧑 2026-08-28: *"no animation when tagging"*.
            //
            // ⚠️ AND THE RETIRED PLAYABLE IS DESTROYED, WHICH IT ALSO WAS NOT. The old code
            // destroyed input 0 and then never connected anything there, so the clip LEAVING
            // input 1 was disconnected and abandoned: one orphaned `AnimationClipPlayable` per
            // clip change, for the life of the graph. A match makes hundreds per character.
            Playable outgoing;
            if (_weight < 1f && _mixer.GetInput(0).IsValid())
            {
                // ⚠️ A second action inside a crossfade starts from the mixed pose
                // the player saw, not the new clip's still-small contribution. Freeze
                // that branch and retire it as soon as the new transition finishes.
                outgoing = _mixer;
                outgoing.SetSpeed(0);
                _graph.Disconnect(_layers, 0);
                _mixer = AnimationMixerPlayable.Create(_graph, 2);
                _layers.ConnectInput(0, _mixer, 0, 1f);
                _mixer.ConnectInput(0, outgoing, 0);
            }
            else
            {
                outgoing = _mixer.GetInput(1);
                RetireOutgoing();
                if (outgoing.IsValid())
                {
                    _graph.Disconnect(_mixer, 1);
                    _mixer.ConnectInput(0, outgoing, 0);
                }
            }

            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetDuration(loop ? double.MaxValue : clip.length);

            _mixer.ConnectInput(1, playable, 0);
            _weight = outgoing.IsValid() ? 0.0f : 1.0f;
            _mixer.SetInputWeight(0, 1f - _weight);
            _mixer.SetInputWeight(1, _weight);
            _current = clipName;
            _holdAtEnd = !loop;
            _transitionSeconds = loop ? _blend : Mathf.Min(_blend,.07f);
        }

        private void Blend()
        {
            _weight = _transitionSeconds <= 0.0f ? 1.0f : Mathf.Min(1.0f, _weight + Time.deltaTime / _transitionSeconds);

            _mixer.SetInputWeight(0, 1.0f - _weight);
            _mixer.SetInputWeight(1, _weight);
            if (_weight >= 1f) RetireOutgoing();
        }

        private void RetireOutgoing()
        {
            var old = _mixer.GetInput(0);
            if (!old.IsValid()) return;
            _graph.Disconnect(_mixer, 0);
            _graph.DestroySubgraph(old);
        }
    }
}
