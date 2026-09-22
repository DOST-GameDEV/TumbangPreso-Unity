using System.Collections.Generic;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class RafiHeroKit : HeroKit
    {
        private readonly List<Vector3> _recent = new List<Vector3>(8);
        private float _sampleLeft;
        public override float UltimateCost => 16;

        public RafiHeroKit() : base("rafi", "RAFI")
        {
            Skill1 = new Crosscurrent();
            Skill2 = new Mirrorwake(this);
            Ultimate = new Breakwater();
        }

        public override void Tick(AbilityContext ctx, float dt)
        {
            base.Tick(ctx, dt);
            if (!NetAuthority.ShouldResolve() || ctx?.Motor == null) return;
            if (ctx.Round == null || !ctx.Round.RoundActive) { _recent.Clear(); _sampleLeft = 0; return; }
            _sampleLeft -= dt;
            if (_sampleLeft > 0) return;
            _sampleLeft = .10f;
            // Teleport/recovery is a discontinuity, not a route the echo can cross.
            if (_recent.Count > 0 && Vector3.Distance(_recent[_recent.Count - 1], ctx.Position) > 2)
                _recent.Clear();
            if (_recent.Count == 8) _recent.RemoveAt(0);
            _recent.Add(ctx.Position);
        }

        private sealed class Crosscurrent : HeroAbility
        {
            public override bool DefersPredictedEffect => true;
            public Crosscurrent() : base("rafi_skill1", "CROSSCURRENT",
                "Aim a narrow current. The first flying slipper bends toward its direction, keeping its original throw credit. A second throw passes through.",
                0, glyph: AbilityGlyph.RafiCrosscurrent,
                summary: "Bend one flying slipper toward your aim. Credit stays with its thrower.",
                telegraphRadius: .65f, telegraphRange: 6,
                castAction: "hero-rafi-cut", viewmodelAction: "current-cut", castCue: "sfx_cast_rafi_current", charges: 2) { }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                bool tight = ctx.HasVariant("rafi.1.tightcut");
                float speed = 8 * ctx.GainScale("rafi.1.tightcut");
                RafiWaterField.Cast(ctx, WorldEffectSnapshot.Kind.Current, .65f * ctx.CostScale("rafi.1.tightcut"),
                    speed, .18f + 6f / speed, tight);
            }
        }

        private sealed class Mirrorwake : HeroAbility
        {
            private readonly RafiHeroKit _kit;
            public override bool DefersPredictedEffect => true;
            public Mirrorwake(RafiHeroKit kit) : base("rafi_skill2", "MIRRORWAKE",
                "Leave a watery reflection of your recent route. It makes one harmless throw feint before peeling into ribbons. You remain visible and vulnerable.",
                0, glyph: AbilityGlyph.RafiMirrorwake,
                summary: "A watery echo retraces your steps. No hit, shield or teleport.",
                castAction: "hero-rafi-feint", viewmodelAction: "mirror-feint", castCue: "sfx_cast_rafi_mirror", charges: 2)
            { _kit = kit; }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                bool longWake = ctx.HasVariant("rafi.2.longwake");
                var path = _kit._recent.Count >= 2 ? _kit._recent.ToArray() : new[] { ctx.Position, ctx.Position };
                RafiWaterField.Cast(ctx, WorldEffectSnapshot.Kind.Mirrorwake, 1, 0,
                    1.25f * ctx.GainScale("rafi.2.longwake"), longWake, path);
            }
        }

        private sealed class Breakwater : HeroAbility
        {
            public Breakwater() : base("rafi_ultimate", "BREAKWATER",
                "Gather, then release a low travelling wave. It nudges each rival once and carries loose slippers. Jump above it or leave its edge; cover cuts the wave.",
                0, glyph: AbilityGlyph.RafiBreakwater,
                summary: "A low travelling crest clears a route. Jump, sidestep or use cover.",
                telegraphRadius: 3, telegraphRange: 8,
                castAction: "hero-rafi-breakwater", viewmodelAction: "breakwater-release", castCue: "sfx_cast_rafi_breakwater") { }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                RafiWaterField.Cast(ctx, WorldEffectSnapshot.Kind.Breakwater, 3, 5, 2.15f, false);
            }
        }
    }
}
