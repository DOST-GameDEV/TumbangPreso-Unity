using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteWardHudFeedbackTests
    {
        private GameObject _body, _can;
        private CharacterMotor _actor;
        private HeroAbilitySystem _hero;
        private TumpMatchReadout _view;
        private AbilityContext _context;
        private readonly List<StatusRow> _rows = new List<StatusRow>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameServices.Ensure(); GameServices.Round.Clear();
            _can = new GameObject("Ward feedback can");
            GameServices.Round.Lata = _can.AddComponent<Lata>();
            _body = new GameObject("Ward feedback actor", typeof(CharacterController));
            _actor = _body.AddComponent<CharacterMotor>(); _actor.enabled = false;
            _actor.PlayerSlot = 1; _actor.Mode = GameMode.HeroStrike;
            _body.AddComponent<Carrier>(); _body.AddComponent<CombatVerbs>();
            _hero = _body.AddComponent<HeroAbilitySystem>(); _hero.BindHero("dante"); _hero.enabled = false;
            GameServices.Round.Register(_actor); GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            _context = new AbilityContext(_actor, _body.GetComponent<Carrier>(), _body.GetComponent<CombatVerbs>());
            _hero.Kit.PracticeMode = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_view != null && _view.Canvas != null) Object.Destroy(_view.Canvas.gameObject);
            if (_body != null) Object.Destroy(_body);
            if (_can != null) Object.Destroy(_can);
            yield return PlayModeWorld.Reset();
        }

        private void Collect() => StatusStack.Collect(_actor, _context.Carrier, _context.Verbs, _rows);

        [UnityTest]
        public IEnumerator ActiveWardHasNamedFeedbackEvenWithTheFirstPersonReticle()
        {
            Assert.IsTrue(_hero.Kit.TryActivateSkill1(_context));
            _view = _body.AddComponent<TumpMatchReadout>(); _view.Build(_body.transform);
            _view.Tick(_actor, false, false, false, false);
            yield return null;
            var visible = _view.Canvas.GetComponentsInChildren<Text>()
                .Where(t => t.enabled && t.text.Contains("UNSTOPPABLE")).ToArray();
            Assert.IsNotEmpty(visible, "The active ward has no named first-person HUD confirmation.");
            Assert.That(visible[0].text, Does.Contain("15.0s"));
            yield return TumpUiCapture.Capture("Basilio-Unstoppable-active", _view.Canvas, 1920, 1080, false);
        }

        [Test]
        public void InactiveWardDoesNotShowAnActiveLabel()
        {
            Collect();
            Assert.IsFalse(_rows.Any(r => r.Label == "UNSTOPPABLE"));
        }

        [Test]
        public void ActiveLabelUsesTheAbilityClockAndDisappearsOnEarlyEnd()
        {
            Assert.IsTrue(_hero.Kit.TryActivateSkill1(_context));
            _hero.Kit.Tick(_context, 2);
            Collect(); var row = _rows.Single(r => r.Label == "UNSTOPPABLE");
            Assert.AreEqual(_hero.Kit.Skill1.DurationRemaining, row.Remaining, .001f);
            Assert.AreEqual(_hero.Kit.Skill1.Duration, row.Total, .001f);
            Assert.IsTrue(row.Timed);
            _hero.Kit.Skill1.EndEarly(_context); Collect();
            Assert.IsFalse(_rows.Any(r => r.Label == "UNSTOPPABLE"));
        }
    }
}
