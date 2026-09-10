using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Versioned ordinary views and continuous motion, through the actual runtime graph.
    // Capture timestamps are retained so encoding cannot silently speed up a slow frame.
    // These are automated observations, not a substitute for human feel approval.
    [Category("WallClock")]
    public sealed class ImprovementEvidenceProbe
    {
        private bool _allBots,_spectator;
        private int _soloSeat;
        private CustomRules _rules;
        private bool _pinned;
        [UnitySetUp] public IEnumerator Before()
        {
            _allBots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_soloSeat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();
            _pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_allBots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_soloSeat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private static string Output => Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";

        [UnityTest, Timeout(180000)]
        public IEnumerator RoundClockAndSceneLookupMeasuredInActualPlay()
        {
            Directory.CreateDirectory(Output);
            var report = new StringBuilder();
            report.AppendLine($"Inherited rules: {CustomGameRules.ToWire(SceneFlow.SelectedRules)}");
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.AllBots=true; GameLaunch.Spectator=true;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.5f);
            var round=GameServices.Round; var match=GameServices.Match;
            report.AppendLine($"Measured rules: {CustomGameRules.ToWire(SceneFlow.SelectedRules)}");
            report.AppendLine($"Scene components: {Object.FindObjectsByType<Component>(FindObjectsSortMode.None).Length}");
            const int searches=10000;
            Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            var watch=System.Diagnostics.Stopwatch.StartNew();
            int found=0;
            for(int i=0;i<searches;i++) found+=Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).Length;
            watch.Stop(); allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Slipper query: {0} calls, {1:F4} ms/call, {2:F1} bytes/call, {3} results",searches,watch.Elapsed.TotalMilliseconds/searches,(double)allocated/searches,found));
            if (allocated == 0) report.AppendLine("Allocation counter returned zero; allocation support is unverified, so this is not a zero-allocation claim.");
            int ticks=0;
            Action<int,ScoreEvent> count=(seat,e)=>{if(e==ScoreEvent.DefenseTick)ticks++;};
            match.Scored+=count;
            var trace=new StringBuilder("real_seconds,game_seconds,time_left,defence_events,collected_ticks,time_scale\n");
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            float realStart=Time.realtimeSinceStartup,gameStart=Time.time,next=0;
            var linesField=typeof(MatchStatsCollector).GetField("_lines",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            int collected=0;
            try
            {
                while(round.RoundActive && Time.realtimeSinceStartup-realStart<150)
                {
                    yield return null;
                    float elapsed=Time.realtimeSinceStartup-realStart;
                    if(elapsed<next)continue;
                    next=elapsed+1;
                    collected=((PlayerMatchStats[])linesField.GetValue(GameServices.Stats)).Where(p=>p!=null).Sum(p=>p.DefenceTicks);
                    trace.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F3},{1:F3},{2:F3},{3},{4},{5:F3}",elapsed,Time.time-gameStart,round.TimeLeft,ticks,collected,Time.timeScale));
                    Assert.AreEqual(ticks,collected,"The collector must count each authoritative event once.");
                    Assert.LessOrEqual(ticks,Mathf.FloorToInt((Balance.RoundTime-round.TimeLeft+.03f)/Balance.DefenseTickInterval));
                }
                report.AppendLine($"Round ended={!round.RoundActive}; events={ticks}; collected={collected}; live game seconds={Time.time-gameStart:F3}; real seconds={Time.realtimeSinceStartup-realStart:F3}");
                Assert.IsFalse(round.RoundActive,"The pinned 90-second round must end.");
                Assert.Greater(ticks,0);
            }
            finally
            {
                match.Scored-=count;
                File.WriteAllText(Path.Combine(Output,"clock-query-report.txt"),report.ToString());
                File.WriteAllText(Path.Combine(Output,"round-clock.csv"),trace.ToString());
            }
        }

        [UnityTest]
        public IEnumerator OrdinaryPlayOnEveryMap()
        {
            foreach (string map in new[] { SceneFlow.Eskinita, SceneFlow.BayanPlaza, SceneFlow.IlalimNgTulay })
            {
                SceneFlow.SelectedMode = GameMode.Classic;
                GameLaunch.AllBots = true;
                GameLaunch.Spectator = true;
                yield return SceneManager.LoadSceneAsync(map);
                yield return new WaitForSecondsRealtime(.5f);
                Object.FindFirstObjectByType<SliceRunner>().Begin();
                var witness = MakeWitness();
                witness.transform.position = new Vector3(0, 1.65f, -12);
                witness.transform.LookAt(new Vector3(0, .9f, 0));
                yield return Record(witness, map + "-ordinary", 8f);
                File.WriteAllLines(Path.Combine(Output,map+"-actors.txt"),GameServices.Round.Players.Select(p =>
                    $"seat={p.PlayerSlot} mode={p.Mode} id={Roster.PersonIdAt(p.Mode,p.CharacterIndex)} model={p.GetComponent<CharacterVisual>()?.SourceModel?.name} stun={p.StunElement} left={p.StunLeft:F2}"));
                for (int view = 0; view < 4; view++)
                {
                    Vector3 direction = Quaternion.Euler(0, view * 90, 0) * Vector3.back;
                    witness.transform.position = direction * 10 + Vector3.up * 1.65f;
                    witness.transform.LookAt(new Vector3(0, 1.3f, 0));
                    yield return GameplayShots.Render(witness, map + "-eye-" + view, false, Output);
                }
                Object.Destroy(witness.gameObject);
                yield return PlayModeWorld.Reset();
            }
        }

        [UnityTest]
        public IEnumerator CarryChargeReleaseAndReturn()
        {
            string reviewId = Environment.GetEnvironmentVariable("TUMP_REVIEW_CHARACTER");
            var mode = !string.IsNullOrEmpty(reviewId) && Roster.ClassicPeople.Any(p => p.Id == reviewId)
                ? GameMode.Classic : GameMode.HeroStrike;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.SoloSeat = 1;
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.5f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            yield return new WaitForSecondsRealtime(.3f);
            var who = GameServices.Round.PlayerAt(1);
            Assert.IsNotNull(who);
            who.IsBot = true; // Review input must not grant a real profile unlocks.
            if (!string.IsNullOrEmpty(reviewId))
            {
                var roster = Roster.GetPeople(mode);
                int index = Enumerable.Range(0, roster.Count).Where(i => roster[i].Id == reviewId).DefaultIfEmpty(-1).First();
                Assert.GreaterOrEqual(index, 0, "Unknown review character: " + reviewId);
                who.CharacterIndex = index;
                var entry = RosterBook.Load().People.First(p => p.Id == reviewId);
                who.GetComponent<CharacterVisual>().ApplyModel(entry.Model, entry.Tint, entry.Clips, entry.Palette, entry.PetModel);
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                    arms.SetCharacter(reviewId);
                Directory.CreateDirectory(Output);
                File.WriteAllText(Path.Combine(Output, "review-character.txt"), reviewId + " / " + mode);
            }
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))
                brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
                reader.enabled = false;
            who.Intent.Clear();
            who.Intent.Parked = false;
            var witness = MakeWitness();
            yield return Record(witness, "carry-charge-release", 9f, who, t =>
            {
                who.Intent.Move = t > .7f && t < 5.2f ? new Vector2(.5f, -.35f) : Vector2.zero;
                who.Intent.Set(Verb.Sprint, t > 2f && t < 3.5f);
                who.Intent.Set(Verb.SpecialAbility, t > 3.5f && t < 5f);
                who.Intent.AimPoint = Vector3.zero;
                who.Intent.FaceAimPoint = true;
                if (t > 6f)
                {
                    var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None)
                        .FirstOrDefault(s => s.SeatOfOrigin == 1 && s.State == SlipperState.Loose);
                    if (shoe != null)
                    {
                        Vector3 delta = shoe.transform.position - who.transform.position;
                        who.Intent.Move = new Vector2(delta.x, delta.z).normalized;
                        who.Intent.Set(Verb.Lunge, t > 6.3f && t < 6.6f);
                        who.Intent.Set(Verb.Grab, true);
                    }
                }
            });
            Object.Destroy(witness.gameObject);
        }

        [UnityTest]
        public IEnumerator AirborneGroundSkillsStayOnEveryMapsActualFloor()
        {
            var report = new StringBuilder("map,effect,expected_y,placed_y\n");
            foreach (string map in new[] { SceneFlow.Eskinita, SceneFlow.BayanPlaza, SceneFlow.IlalimNgTulay })
            {
                SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                GameLaunch.AllBots = true; GameLaunch.Spectator = true;
                yield return SceneManager.LoadSceneAsync(map);
                yield return new WaitForSecondsRealtime(.3f);
                var points = new[] { new Vector3(-3, .5f, -2), new Vector3(3, .5f, 2) };
                var effects = new GameObject[2];
                for (int i = 0; i < points.Length; i++)
                {
                    Assert.IsTrue(Physics.Raycast(points[i], Vector3.down, out var hit, 2, ~0,
                        QueryTriggerInteraction.Ignore), map + " review point has no physical floor.");
                    Vector3 airborne = points[i]; airborne.y = 4;
                    effects[i] = i == 0 ? HeroHazards.SpawnIceSheet(airborne, 2.3f, 4)
                        : HeroHazards.SpawnFireTrail(airborne, 1, 4);
                    Assert.AreEqual(hit.point.y, effects[i].transform.position.y, .025f, map + "/" + effects[i].name);
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4}",
                        map, effects[i].name, hit.point.y, effects[i].transform.position.y));
                }
                var camera = MakeWitness();
                camera.transform.position = new Vector3(-7, 1.7f, -7);
                camera.transform.LookAt(new Vector3(-1, .3f, 0));
                yield return new WaitForSeconds(.15f);
                yield return GameplayShots.Render(camera, map + "-ground-skills-forming", false, Output);
                yield return new WaitForSeconds(.4f);
                yield return GameplayShots.Render(camera, map + "-ground-skills", false, Output);
                RecordLookInputs(map, camera);

                Object.Destroy(camera.gameObject);
                yield return PlayModeWorld.Reset();
            }
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "ground-placement.csv"), report.ToString());
        }

        private static void RecordLookInputs(string map, Camera camera)
        {
            var log = new StringBuilder();
            log.AppendLine("grade=" + JsonUtility.ToJson(camera.GetComponent<ColourGrade>()));
            log.AppendLine("ambient=" + RenderSettings.ambientMode + " / " + RenderSettings.ambientLight
                + " sky=" + RenderSettings.ambientSkyColor + " equator=" + RenderSettings.ambientEquatorColor);
            foreach (var player in GameServices.Round.Players)
            {
                log.AppendLine("actor=" + Roster.PersonIdAt(player.Mode, player.CharacterIndex));
                foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer.GetComponentInParent<VfxRenderTag>() != null) continue;
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                    foreach (var material in renderer.sharedMaterials)
                    {
                        var palette = material.GetVectorArray("_Palette");
                        log.AppendLine(renderer.name + " shader=" + material.shader.name
                            + " color=" + (material.HasProperty("_Color") ? material.GetColor("_Color").ToString() : "none")
                            + " palette13=" + (palette.Length > 13 ? palette[13].ToString("F4") : "missing")
                            + " blockColor=" + block.GetColor("_Color") + " flash=" + block.GetFloat("_FlashAmount")
                            + " frost=" + block.GetFloat("_FrostAmount"));
                    }
                }
            }
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.enabled) log.AppendLine("light=" + light.name + " type=" + light.type + " color=" + light.color
                    + " intensity=" + light.intensity + " range=" + light.range + " at=" + light.transform.position);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, map + "-look-inputs.txt"),log.ToString());
        }

        [UnityTest]
        public IEnumerator ZackSprintSustainsSpeedAfterTheInitialImpulseAndResets()
        {
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.5f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            yield return new WaitForSecondsRealtime(.3f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in GameServices.Round.Players) player.Intent.Clear();
            var who = GameServices.Round.PlayerAt(1);
            who.IsBot = true;
            who.CharacterIndex = Roster.HeroPeople.Select((p,i) => (p,i)).First(p => p.p.Id == "zack").i;
            who.AbilitySystem.BindHero("zack");
            who.Teleport(new Vector3(-6, who.transform.position.y, -5));
            who.Intent.Parked = false;
            who.Intent.Move = Vector2.right;
            yield return new WaitForSeconds(.5f);
            float before = new Vector2(who.Velocity.x, who.Velocity.z).magnitude;
            Assert.Greater(before, 1, "Fixture never reached a normal walk.");
            who.Intent.Set(Verb.Skill1, true);
            yield return new WaitForSeconds(.1f);
            who.Intent.Set(Verb.Skill1, false);
            Assert.IsTrue(who.AbilitySystem.Kit.Skill1.IsActive, "Real sprint press was not accepted.");
            yield return new WaitForSeconds(.65f);
            float boosted = new Vector2(who.Velocity.x, who.Velocity.z).magnitude;
            Assert.AreEqual(before * Balance.ZackSprintSpeedScale, boosted, .1f,
                "The skill lost its speed benefit after the initial dash impulse decayed.");
            who.AbilitySystem.ResetKit();
            yield return new WaitForSeconds(.3f);
            float restored = new Vector2(who.Velocity.x, who.Velocity.z).magnitude;
            Assert.AreEqual(before, restored, .1f, "Reset left a speed boost on the actor.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "zack-sprint-speed.csv"),
                string.Format(CultureInfo.InvariantCulture, "phase,speed\nbefore,{0:F4}\nboosted,{1:F4}\nreset,{2:F4}\n", before, boosted, restored));
            who.Intent.Clear();
        }

        [UnityTest]
        public IEnumerator EveryHeroActionThroughTheRealPressAndRelease()
        {
            string[] heroes = { "sean", "zack", "dante", "cheska", "nemu", "phaister" };
            string selectedHero = Environment.GetEnvironmentVariable("TUMP_REVIEW_HERO");
            if (!string.IsNullOrEmpty(selectedHero))
            {
                CollectionAssert.Contains(heroes, selectedHero);
                heroes = new[] { selectedHero };
            }
            var coverage = new StringBuilder("hero,slot,body,first_person,accepted\n");
            foreach (string hero in heroes)
            {
                SceneFlow.SelectedMode = GameMode.HeroStrike;
                GameLaunch.SoloSeat = 1; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                yield return new WaitForSecondsRealtime(.5f);
                Object.FindFirstObjectByType<SliceRunner>().Begin();
                yield return new WaitForSecondsRealtime(.3f);
                foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
                foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
                var who = GameServices.Round.PlayerAt(1);
                who.IsBot = true; // A review cast must never write a player's unlock history.
                who.CharacterIndex = Roster.GetPeople(GameMode.HeroStrike).Select((p,i) => (p,i)).First(p => p.p.Id == hero).i;
                var entry = RosterBook.Load().People.First(p => p.Id == hero);
                who.GetComponent<CharacterVisual>().ApplyModel(entry.Model, entry.Tint, entry.Clips, entry.Palette, entry.PetModel);
                var abilities = who.AbilitySystem;
                abilities.BindHero(hero);
                var witness = MakeWitness();
                for (int slot = 0; slot < 3; slot++)
                {
                    abilities.ResetKit();
                    abilities.Kit.AddUltimateCharge(100);
                    who.ClearStun(); who.ClearTrip();
                    who.Teleport(new Vector3(0, who.transform.position.y, -8));
                    who.transform.rotation = Quaternion.identity;
                    who.Intent.Clear(); who.Intent.Parked = false;
                    who.Intent.AimPoint = new Vector3(0, .1f, -3);
                    if (hero == "zack" && slot == 1)
                    {
                        // Magnet recalls a loose owned shoe; holding one is a correct
                        // refusal, not a missing animation. Exercise its real precondition.
                        var shoe = who.GetComponent<Carrier>().Held;
                        if (shoe != null)
                        {
                            shoe.HostDisarm();
                            shoe.transform.position = new Vector3(1, .05f, -4);
                        }
                    }
                    var ability = slot == 0 ? abilities.Kit.Skill1 : slot == 1 ? abilities.Kit.Skill2 : abilities.Kit.Ultimate;
                    Verb verb = slot == 0 ? Verb.Skill1 : slot == 1 ? Verb.Skill2 : Verb.Ultimate;
                    var answerSlot = (HeroAbilitySystem.Slot)slot;
                    bool accepted = false;
                    yield return Record(witness, hero + "-" + (slot+1), 3.2f, who, t =>
                    {
                        who.Intent.Set(verb, t >= .25f && t < .65f);
                        if (t > .25f && abilities.LastAnswer(answerSlot) == HeroKit.CastOutcome.Cast
                            && abilities.SecondsSinceAnswer(answerSlot) < 2.8f) accepted = true;
                    });
                    who.Intent.Set(verb, false);
                    coverage.AppendLine($"{hero},{slot+1},{ability.CastAction},{ability.ViewmodelAction},{accepted}");
                    File.WriteAllText(Path.Combine(Output,"hero-coverage.csv"), coverage.ToString());
                    Assert.IsTrue(accepted, hero + "/" + ability.Name + " never accepted the real press.");
                    abilities.ResetKit();
                    yield return new WaitForSecondsRealtime(.2f);
                }
                Object.Destroy(witness.gameObject);
                yield return PlayModeWorld.Reset();
            }
        }

        private static Camera MakeWitness()
        {
            var camera = new GameObject("ImprovementWitness").AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 52;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 400;
            camera.allowHDR = true;
            camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            return camera;
        }

        private static IEnumerator Record(Camera camera, string name, float seconds,
            CharacterMotor subject = null, Action<float> drive = null)
        {
            string folder = Path.Combine(Output, name);
            Directory.CreateDirectory(folder);
            string ownerFolder = Path.Combine(folder,"owner");
            if (subject != null) Directory.CreateDirectory(ownerFolder);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var log = new StringBuilder("frame,real_seconds,game_seconds,speed,held,leg_angle,action\n");
            var field = typeof(CharacterAnimator).GetField("_current", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            float start = Time.realtimeSinceStartup, gameStart = Time.time, next = 0;
            int frame = 0;
            var previous = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                while (Time.realtimeSinceStartup - start < seconds)
                {
                    float t = Time.realtimeSinceStartup - start;
                    drive?.Invoke(t);
                    yield return null;
                    if (t < next) continue;
                    next = t + .05f;
                    if (subject != null)
                    {
                        camera.transform.position = subject.transform.position + new Vector3(3, 1.5f, 4);
                        camera.transform.LookAt(subject.transform.position + Vector3.up * .9f);
                    }
                    var owner = subject != null ? Camera.main : null;
                    if (owner != null && owner != camera)
                    {
                        var target = owner.targetTexture;
                        owner.targetTexture = rt;
                        owner.Render();
                        owner.targetTexture = target;
                        SaveFrame(rt,ldr,pixels,Path.Combine(ownerFolder,$"{frame:D5}.jpg"));
                    }
                    var renderers = subject != null ? subject.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
                    var held = subject != null ? subject.GetComponent<Carrier>()?.Held : null;
                    if (held != null) renderers = renderers.Concat(held.GetComponentsInChildren<Renderer>()).Distinct().ToArray();
                    var shadow = renderers.Select(r => r.shadowCastingMode).ToArray();
                    var privateArms = Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)
                        .SelectMany(a=>a.GetComponentsInChildren<Renderer>()).ToArray();
                    var privateEnabled = privateArms.Select(r=>r.enabled).ToArray();
                    foreach(var renderer in privateArms)renderer.enabled=false;
                    for (int i = 0; i < renderers.Length; i++)
                        if (shadow[i] == ShadowCastingMode.ShadowsOnly) renderers[i].shadowCastingMode = ShadowCastingMode.On;
                    try
                    {
                        camera.targetTexture = rt;
                        camera.Render();
                    }
                    finally
                    {
                        for (int i = 0; i < renderers.Length; i++) if(renderers[i]!=null) renderers[i].shadowCastingMode = shadow[i];
                        for (int i = 0; i < privateArms.Length; i++) if(privateArms[i]!=null) privateArms[i].enabled = privateEnabled[i];
                    }
                    SaveFrame(rt,ldr,pixels,Path.Combine(folder,$"{frame:D5}.jpg"));
                    var leg = subject != null ? subject.GetComponentsInChildren<Transform>().FirstOrDefault(b => b.name == "leg-right") : null;
                    string action = subject != null ? (string)field.GetValue(subject.GetComponent<CharacterAnimator>()) : "ordinary";
                    log.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2:F4},{3:F4},{4},{5:F3},{6}",
                        frame++, Time.realtimeSinceStartup-start, Time.time-gameStart,
                        subject != null ? new Vector2(subject.Velocity.x, subject.Velocity.z).magnitude : 0,
                        subject != null && subject.HoldingSlipper, leg != null ? leg.localEulerAngles.x : 0, action));
                }
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(rt); Object.DestroyImmediate(ldr); Object.DestroyImmediate(pixels);
                File.WriteAllText(Path.Combine(folder, "frames.csv"), log.ToString());
                if (subject != null) File.WriteAllText(Path.Combine(ownerFolder,"frames.csv"),log.ToString());
            }
            Assert.Greater(frame, seconds * 4, "The capture must contain enough actual frames to review.");
        }

        private static void SaveFrame(RenderTexture hdr, RenderTexture ldr, Texture2D pixels, string path)
        {
            Graphics.Blit(hdr, ldr);
            var active = RenderTexture.active;
            RenderTexture.active = ldr;
            pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            pixels.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(path,pixels.EncodeToJPG(90));
        }
    }
}
