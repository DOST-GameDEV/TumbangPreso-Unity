using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Component attached to a character in Hero Strike mode that drives active hero abilities,
    /// cooldowns, inputs, and ultimate meter.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class HeroAbilitySystem : MonoBehaviour
    {
        public HeroKit Kit { get; private set; }
        private CharacterMotor _motor;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private AbilityContext _context;

        public bool IsImmuneToTags => Kit is NemuHeroKit nemu && nemu.IsPhantomPhaseActive;
        public bool IsImmuneToStuns => Kit is DanteHeroKit dante && dante.IsDemonicCarapaceActive;

        private GroundReticle _reticle;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _verbs = GetComponent<CombatVerbs>();
            _context = new AbilityContext(_motor, _carrier, _verbs);
            _reticle = GroundReticle.Create(transform);
        }

        public void BindHero(string heroId)
        {
            Kit = CreateKitFor(heroId);
        }

        public static HeroKit CreateKitFor(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return new DanteHeroKit();

            switch (heroId.ToLowerInvariant())
            {
                case "dante":
                case "bayan":
                    return new DanteHeroKit();

                case "cheska":
                case "inday":
                    return new CheskaHeroKit();

                case "sean":
                case "kuya_boy":
                case "iggy":
                    return new SeanHeroKit();

                case "zack":
                    return new ZackHeroKit();

                case "nemu":
                    return new NemuHeroKit();

                default:
                    return new DanteHeroKit();
            }
        }

        private void Update()
        {
            if (Kit == null || _motor == null) return;

            float dt = Time.deltaTime;
            Kit.Tick(_context, dt);

            if (!_motor.CanAct())
            {
                if (_reticle != null) _reticle.Hide();
                return;
            }

            var intent = _motor.Intent;
            if (intent == null)
            {
                if (_reticle != null) _reticle.Hide();
                return;
            }

            UpdateReticle(intent);

            if (intent.JustPressed(Verb.Skill1))
            {
                if (Kit.TryActivateSkill1(_context))
                {
                    GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("dash");
                    CameraSystem.CameraRig.PlayViewmodelAction(_motor, "thrust");
                }
            }

            if (intent.JustPressed(Verb.Skill2))
            {
                if (Kit.TryActivateSkill2(_context))
                {
                    GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("shove");
                    CameraSystem.CameraRig.PlayViewmodelAction(_motor, "cast");
                }
            }

            if (intent.JustPressed(Verb.Ultimate))
            {
                if (Kit.TryActivateUltimate(_context))
                {
                    GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("jump");
                    CameraSystem.CameraRig.PlayViewmodelAction(_motor, "slam");
                }
            }
        }

        private void UpdateReticle(InputIntent intent)
        {
            if (_reticle == null || Kit == null) return;

            Color heroColor = UiTheme.ColorForHero(Kit.HeroId);

            if (intent.IsPressed(Verb.Ultimate) && Kit.IsUltimateReady)
            {
                Vector3 target = _context.Position + _context.Forward * 3.5f;
                _reticle.Show(target, 7.5f, heroColor);
            }
            else if (intent.IsPressed(Verb.Skill1) && Kit.Skill1 != null && Kit.Skill1.IsReady)
            {
                Vector3 target = (Kit is CheskaHeroKit) ? (_context.Position + _context.Forward * 3.5f) : _context.Position;
                _reticle.Show(target, 5.0f, heroColor);
            }
            else if (intent.IsPressed(Verb.Skill2) && Kit.Skill2 != null && Kit.Skill2.IsReady)
            {
                Vector3 target = (Kit is CheskaHeroKit) ? (_context.Position + _context.Forward * 2.4f) : _context.Position;
                _reticle.Show(target, 3.5f, heroColor);
            }
            else
            {
                _reticle.Hide();
            }
        }

        public void OnLataKnocked()
        {
            Kit?.AddUltimateCharge(25.0f);
        }

        public void OnTagScored()
        {
            Kit?.AddUltimateCharge(20.0f);
        }

        public void OnThrowReleased()
        {
            Kit?.AddUltimateCharge(8.0f);
        }

        public void ResetKit()
        {
            Kit?.Reset();
        }
    }
}
