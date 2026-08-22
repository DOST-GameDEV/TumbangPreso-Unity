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

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _verbs = GetComponent<CombatVerbs>();
            _context = new AbilityContext(_motor, _carrier, _verbs);
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

            if (!_motor.CanAct()) return;

            var intent = _motor.Intent;
            if (intent == null) return;

            if (intent.JustPressed(Verb.Skill1))
            {
                Kit.TryActivateSkill1(_context);
            }

            if (intent.JustPressed(Verb.Skill2))
            {
                Kit.TryActivateSkill2(_context);
            }

            if (intent.JustPressed(Verb.Ultimate))
            {
                Kit.TryActivateUltimate(_context);
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
