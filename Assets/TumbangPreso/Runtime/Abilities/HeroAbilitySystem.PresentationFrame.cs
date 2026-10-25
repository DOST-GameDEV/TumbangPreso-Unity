using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private struct PresentedFrame
        {
            public HeroAbility Ability;
            public Bounds Bounds;
            public float At;
            public int Round;
            public long Match;
        }
        private readonly PresentedFrame[] _presentedFrames = new PresentedFrame[3];

        // Capture at the existing presentation confirmation, before its event is raised.
        // Future mouse movement, a changed hold or a replica's empty Intent cannot move it.
        private void RecordPresentationFrame(Slot slot, HeroAbility ability, AbilityContext context)
        {
            int index = (int)slot;
            if (index < 0 || index >= _presentedFrames.Length || ability == null || context == null) return;
            Vector3 centre = ability.HoldToAim ? AimDestination(ability, context) : ability.TelegraphCentre(context);
            if (!ability.HasTelegraph && !ability.HoldToAim) centre = context.Position;
            float radius = Mathf.Max(.4f, ability.TelegraphRadius);
            float height = 2;
            if (slot == Slot.Ultimate && Kit is SeanHeroKit)
            {
                centre = SeanVolcano.SpotFor(context.Position, context.Forward, AimDestination(ability, context), context.Round?.Lata);
                height = SeanRules.VolcanoHeight + 1;
            }
            else if (slot == Slot.Ultimate && Kit is PaeteHeroKit)
                centre = PaeteVine.SentryTarget(context.Position, context.Forward, AimDestination(ability, context), context.Round?.Lata);
            Bounds bounds = new Bounds(centre + Vector3.up * (height * .5f), new Vector3(radius * 2, height, radius * 2));
            if (slot == Slot.Ultimate && (Kit is CheskaHeroKit || Kit is NemuHeroKit || Kit is AmihanHeroKit || Kit is DanteHeroKit))
            {
                float minX = AIController.PlayableMinX, maxX = AIController.PlayableMaxX;
                float minZ = AIController.PlayableMinZ, maxZ = AIController.PlayableMaxZ;
                bounds = new Bounds(new Vector3((minX + maxX) * .5f, context.Position.y + 1.5f, (minZ + maxZ) * .5f),
                    new Vector3(Mathf.Max(1, maxX - minX), 3, Mathf.Max(1, maxZ - minZ)));
            }
            _presentedFrames[index] = new PresentedFrame { Ability = ability, Bounds = bounds, At = Time.time,
                Round = GameServices.Match?.RoundNumber ?? 0, Match = GameServices.Match?.PresentationMatchId ?? 0 };
        }
        public bool TryPresentedFrame(HeroAbility ability, out Bounds bounds)
        {
            if (ability == null || Kit == null || (ability != Kit.Skill1 && ability != Kit.Skill2 && ability != Kit.Ultimate))
            { bounds = default; return false; }
            foreach (var frame in _presentedFrames)
            {
                if (frame.Ability != ability || frame.Round != (GameServices.Match?.RoundNumber ?? 0)
                    || frame.Match != (GameServices.Match?.PresentationMatchId ?? 0)) continue;
                float age = Time.time - frame.At;
                if (age < 0 || age > Mathf.Max(4, ability.Duration + ability.Windup + 1.5f)) continue;
                bounds = frame.Bounds; return true;
            }
            bounds = default; return false;
        }
    }
}
