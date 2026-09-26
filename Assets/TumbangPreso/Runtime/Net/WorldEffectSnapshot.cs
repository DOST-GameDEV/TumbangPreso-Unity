using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Net
{
    // A bounded complete set, applied atomically after the reliable batch ends.
    // Casts and this batch share a reliable channel, so newer casts follow it.
    public static class WorldEffectSnapshot
    {
        public const int MaxFields = 256;
        public enum Kind { Sheet = 1, Barricade = 2, Fire = 3, Shock = 4, Crater = 5, Hex = 6, Fissure = 7, Current = 8, Mirrorwake = 9, Breakwater = 10,
            // ⚠️ APPEND ONLY (protocol 53): Amihan's Whirlwind gale, a travelling front.
            Gale = 11,
            // ⚠️ APPEND ONLY (protocol 55, HERO-9): Paete's seedling, his thorn construct and his sentry.
            Plant = 12, Thorns = 13, Sentry = 14 }
        public struct Field
        {
            public Kind Type;
            [System.NonSerialized] public GameObject Source;
            public Vector3 Position, Forward;
            public float Duration, Remaining, Radius, FirstScale, SecondScale;
            public int Owner;
            public bool Split;
            public int EventId;
            public Vector3[] Path;
        }

        public sealed class Batch
        {
            public readonly int Generation;
            private readonly Field[] _fields;
            private readonly bool[] _seen;
            private int _received;
            private bool _finished;
            public Batch(int generation, int count)
            {
                if (count < 0 || count > MaxFields) throw new System.ArgumentOutOfRangeException(nameof(count));
                Generation = generation; _fields = new Field[count]; _seen = new bool[count];
            }
            public bool Add(int generation, int index, Field field)
            {
                if (_finished || generation != Generation || index < 0 || index >= _fields.Length || _seen[index] || !Valid(field)) return false;
                _fields[index] = field; _seen[index] = true; _received++; return true;
            }
            public bool Finish(int generation, out Field[] fields)
            {
                fields = null;
                if (_finished || generation != Generation || _received != _fields.Length) return false;
                _finished = true; fields = _fields; return true;
            }
        }

        public static List<Field> Capture()
        {
            var fields = new List<Field>();
            foreach (var sheet in Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None))
            {
                if (sheet.Remaining <= .02f) continue;
                fields.Add(new Field { Type = Kind.Sheet, Source = sheet.gameObject, Position = sheet.transform.position,
                    Forward = Vector3.forward, Duration = sheet.Duration, Remaining = sheet.Remaining,
                    Radius = sheet.Radius, Owner = sheet.OwnerSlot,
                    FirstScale = sheet.ChillMultiplier, SecondScale = sheet.SlipScale });
            }
            foreach (var wall in Object.FindObjectsByType<HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None))
            {
                if (wall.Remaining <= .02f) continue;
                fields.Add(new Field { Type = Kind.Barricade, Source = wall.gameObject, Position = wall.transform.position,
                    Forward = wall.transform.forward, Duration = wall.Duration, Remaining = wall.Remaining,
                    Owner = -1, FirstScale = wall.SpanScale, SecondScale = wall.ThicknessScale, Split = wall.Split });
            }
            foreach (var trail in Object.FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None))
                if (trail.Remaining > .02f) fields.Add(new Field { Type = Kind.Fire, Source = trail.gameObject,
                    Position = trail.transform.position, Forward = trail.Forward, Radius = trail.Radius,
                    Duration = trail.Duration, Remaining = trail.Remaining, Owner = trail.OwnerSlot });
            foreach (var trail in Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None))
                if (trail.Remaining > .02f) fields.Add(new Field { Type = Kind.Shock, Source = trail.gameObject,
                    Position = trail.transform.position, Forward = trail.Forward, Radius = trail.Radius,
                    Duration = trail.Duration, Remaining = trail.Remaining, Owner = trail.OwnerSlot, FirstScale = trail.EffectScale });
            foreach (var crater in Object.FindObjectsByType<HeroHazards.SupernovaCraterComponent>(FindObjectsSortMode.None))
                if (crater.Remaining > .02f) fields.Add(new Field { Type = Kind.Crater, Source = crater.gameObject,
                    Position = crater.transform.position, Radius = crater.Radius,
                    Duration = crater.Duration, Remaining = crater.Remaining, Owner = crater.OwnerSlot });
            foreach (var hex in Object.FindObjectsByType<HeroHazards.HexSigilComponent>(FindObjectsSortMode.None))
                if (hex.Remaining > .02f) fields.Add(new Field { Type = Kind.Hex, Source = hex.gameObject,
                    Position = hex.transform.position, Radius = hex.Radius,
                    Duration = hex.Duration, Remaining = hex.Remaining, Owner = hex.OwnerSlot, FirstScale = hex.EffectScale });
            foreach (var pillar in Object.FindObjectsByType<DanteFissurePillar>(FindObjectsSortMode.None))
                if (pillar.isActiveAndEnabled && pillar.Remaining > .02f) fields.Add(new Field { Type = Kind.Fissure, Source = pillar.gameObject,
                    Position = pillar.transform.position, Forward = pillar.transform.forward,
                    Duration = pillar.LifeSeconds, Remaining = pillar.Remaining, Owner = -1, FirstScale = pillar.Side });
            foreach (var gale in Object.FindObjectsByType<AmihanGale>())
                if (gale.isActiveAndEnabled && gale.Remaining > .02f) fields.Add(gale.Capture());
            foreach (var plant in PaetePlant.Live)
                if (plant != null && plant.isActiveAndEnabled && plant.Landed && !plant.IsPulled) fields.Add(plant.Capture());
            foreach (var thorns in Object.FindObjectsByType<PaeteThorns>())
                if (thorns.isActiveAndEnabled && thorns.Age < Core.PaeteRules.ThornConstructSeconds - .05f) fields.Add(thorns.Capture());
            foreach (var sentry in Object.FindObjectsByType<PaeteSentry>())
                if (sentry.isActiveAndEnabled && sentry.Age >= 0f && sentry.Age < Core.PaeteRules.SentryLifeSeconds + .5f) fields.Add(sentry.Capture());
            foreach (var water in RafiWaterField.Active)
                if (water != null && water.isActiveAndEnabled && water.Remaining > .02f) fields.Add(water.Capture());
            return fields;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        public static bool Valid(Field field)
        {
            if (!Finite(field.Position) || !Finite(field.Forward) || !Finite(field.Duration) || !Finite(field.Remaining)
                || !Finite(field.Radius) || !Finite(field.FirstScale) || !Finite(field.SecondScale)
                || field.Duration <= 0 || field.Duration > 60 || field.Remaining < 0 || field.Remaining > field.Duration + .05f
                || field.Owner < -1 || field.Owner >= Core.Balance.PlayerCount) return false;
            if (RafiWaterField.IsWater(field.Type)) return RafiWaterField.Valid(field);
            if (field.Type == Kind.Sheet)
                return field.Radius > 0 && field.Radius <= 10 && field.FirstScale > 0 && field.FirstScale <= 1
                    && field.SecondScale > 0 && field.SecondScale <= 3;
            if (field.Type == Kind.Barricade)
                return field.Forward.sqrMagnitude > .5f && field.Forward.sqrMagnitude < 1.5f
                    && field.FirstScale > 0 && field.FirstScale <= 3 && field.SecondScale > 0 && field.SecondScale <= 3;
            if (field.Type == Kind.Fissure)
                return field.Forward.sqrMagnitude > .5f && field.Forward.sqrMagnitude < 1.5f
                    && (field.FirstScale == -1 || field.FirstScale == 1);
            if (field.Radius <= 0 || field.Radius > 10) return false;
            if (field.Type == Kind.Fire || field.Type == Kind.Shock)
            {
                if (field.Forward.sqrMagnitude < .5f || field.Forward.sqrMagnitude > 1.5f) return false;
                return field.Type == Kind.Fire || (field.FirstScale > 0 && field.FirstScale <= 3);
            }
            if (field.Type == Kind.Crater) return true;
            if (field.Type == Kind.Hex) return field.FirstScale > 0 && field.FirstScale <= 3;
            if (field.Type == Kind.Gale)
                return field.Forward.sqrMagnitude > .5f && field.Forward.sqrMagnitude < 1.5f
                    && field.Duration <= Core.AmihanRules.WhirlwindSeconds + .05f;
            if (field.Type == Kind.Plant)
                return field.Owner >= 0 && field.Duration <= Core.PaeteRules.PlantLifeSeconds + .05f
                    && field.FirstScale >= 0 && field.FirstScale <= Core.PaeteRules.PlantReloadSeconds + .05f;
            if (field.Type == Kind.Thorns)
                return field.Owner >= 0 && field.Duration <= Core.PaeteRules.ThornConstructSeconds + .05f;
            if (field.Type == Kind.Sentry)
                return field.Owner >= 0 && field.Duration <= Core.PaeteRules.SentryLifeSeconds + .65f;
            return false;
        }

        public static bool Apply(IReadOnlyList<Field> fields, float elapsed)
        {
            if (fields == null || fields.Count > MaxFields || !Finite(elapsed) || elapsed < 0) return false;
            foreach (var field in fields) if (!Valid(field)) return false;
            ClearPersistentFields();
            foreach (var field in fields)
            {
                float remaining = Mathf.Clamp(field.Remaining - elapsed, 0, field.Duration);
                if (remaining <= .02f) continue;
                if (RafiWaterField.IsWater(field.Type)) RafiWaterField.Restore(field, elapsed);
                else if (field.Type == Kind.Sheet)
                {
                    var go = HeroHazards.SpawnIceSheet(field.Position, field.Radius, field.Duration,
                        field.Owner, field.SecondScale, silent: true);
                    var sheet = go.GetComponent<HeroHazards.IceSheetComponent>();
                    sheet.ChillMultiplier = field.FirstScale; sheet.RestoreRemaining(remaining);
                    go.GetComponent<FrostSurfacePresentation>().StepTo(field.Duration - remaining);
                }
                else if (field.Type == Kind.Barricade)
                {
                    var go = HeroHazards.SpawnIceBarricade(field.Position, field.Forward, field.Duration,
                        field.FirstScale, field.SecondScale, field.Split, silent: true);
                    go.GetComponent<HeroHazards.IceBarricadeComponent>().RestoreRemaining(remaining);
                }
                else if (field.Type == Kind.Fire)
                {
                    var go = HeroHazards.SpawnFireTrail(field.Position, field.Radius, field.Duration, field.Owner, field.Forward);
                    go.GetComponent<HeroHazards.FireTrailComponent>().RestoreRemaining(remaining);
                    go.GetComponentInChildren<SeanHeatGround>().StepTo(field.Duration - remaining);
                }
                else if (field.Type == Kind.Shock)
                {
                    var go = HeroHazards.SpawnShockTrail(field.Position, field.Radius, field.Duration, field.Owner, field.FirstScale, field.Forward);
                    go.GetComponent<HeroHazards.ShockTrailComponent>().RestoreRemaining(remaining);
                    go.GetComponentInChildren<ZackSkateWake>().StepTo(field.Duration - remaining);
                }
                else if (field.Type == Kind.Crater)
                {
                    var go = HeroHazards.SpawnSupernovaCrater(field.Position, field.Radius, field.Duration, field.Owner);
                    go.GetComponent<HeroHazards.SupernovaCraterComponent>().RestoreRemaining(remaining);
                    go.GetComponentInChildren<SeanHeatGround>().StepTo(field.Duration - remaining);
                }
                else if (field.Type == Kind.Hex)
                {
                    var go = HeroHazards.SpawnHexSigil(field.Position, field.Radius, field.Duration, field.Owner, field.FirstScale, silent: true);
                    go.GetComponent<HeroHazards.HexSigilComponent>().RestoreRemaining(remaining);
                    go.GetComponent<HeroHazards.WardInscribe>().StepTo(field.Duration - remaining);
                }
                else if (field.Type == Kind.Plant)
                    PaetePlant.Restore(field.Position, field.Owner, field.Duration - remaining, Mathf.Max(0f, field.FirstScale - elapsed));
                else if (field.Type == Kind.Thorns)
                    PaeteThorns.Restore(field.Position, field.Owner, field.Duration - remaining);
                else if (field.Type == Kind.Sentry)
                    // A rejoiner's sentry is the same tree at the same age; the host alone catches.
                    PaeteSentry.Spawn(field.Position, field.Position, field.Owner, field.Duration - remaining);
                else if (field.Type == Kind.Gale)
                {
                    // A rejoiner's gale is the same front at the same age; the host alone hits.
                    AmihanGale.Spawn(field.Position, field.Forward, field.Owner, field.Duration - remaining);
                }
                else if (field.Type == Kind.Fissure)
                {
                    var pillar = DanteFissurePillar.Create(field.Position, field.Forward, (int)field.FirstScale, field.Duration);
                    pillar.GetComponent<HeroHazards.EarthPillarComponent>().RestoreRemaining(remaining);
                    pillar.StepTo(field.Duration - remaining);
                }
            }
            // Replaced objects must also replace the emitter's ownership queue.
            // Otherwise its live-field cap and cancellation still point at dead objects.
            if(GameServices.Round!=null) foreach(var player in GameServices.Round.Players)
            {
                if(player==null) continue;
                if(player.AbilitySystem?.Kit is ZackHeroKit zack) zack.AdoptMovementFields(player.PlayerSlot);
                else if(player.AbilitySystem?.Kit is SeanHeroKit sean) sean.AdoptMovementFields(player.PlayerSlot);
            }
            return true;
        }

        // Own the same finite set for snapshot replacement and round retirement.
        // Map hazards and render-only replay copies are deliberately outside it.
        public static void ClearPersistentFields()
        {
            Retire<RafiWaterField>();
            Retire<HeroHazards.IceSheetComponent>();
            Retire<HeroHazards.IceBarricadeComponent>();
            Retire<HeroHazards.FireTrailComponent>();
            Retire<HeroHazards.ShockTrailComponent>();
            Retire<HeroHazards.SupernovaCraterComponent>();
            Retire<HeroHazards.HexSigilComponent>();
            Retire<AmihanGale>();
            Retire<AmihanStorm>();
            Retire<PaetePlant>();
            Retire<PaeteThorns>();
            Retire<PaeteSentry>();
            // Render-only fissures have no gameplay lifetime component.
            Retire<HeroHazards.EarthPillarComponent>();
            Physics.SyncTransforms();
        }

        private static void Retire<T>() where T : Component
        {
            foreach (var field in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            { field.gameObject.SetActive(false); Object.Destroy(field.gameObject); }
        }
    }
}
