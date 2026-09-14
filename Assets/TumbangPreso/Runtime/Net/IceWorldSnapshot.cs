using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Net
{
    // A bounded complete set, applied atomically after the reliable batch ends.
    // Casts and this batch share a reliable channel, so newer casts follow it.
    public static class IceWorldSnapshot
    {
        public const int MaxFields = 256;
        public enum Kind { Sheet = 1, Barricade = 2 }
        public struct Field
        {
            public Kind Type;
            public Vector3 Position, Forward;
            public float Duration, Remaining, Radius, FirstScale, SecondScale;
            public int Owner;
            public bool Split;
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
                fields.Add(new Field { Type = Kind.Sheet, Position = sheet.transform.position,
                    Forward = Vector3.forward, Duration = sheet.Duration, Remaining = sheet.Remaining,
                    Radius = sheet.Radius, Owner = sheet.OwnerSlot,
                    FirstScale = sheet.ChillMultiplier, SecondScale = sheet.SlipScale });
            }
            foreach (var wall in Object.FindObjectsByType<HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None))
            {
                if (wall.Remaining <= .02f) continue;
                fields.Add(new Field { Type = Kind.Barricade, Position = wall.transform.position,
                    Forward = wall.transform.forward, Duration = wall.Duration, Remaining = wall.Remaining,
                    Owner = -1, FirstScale = wall.SpanScale, SecondScale = wall.ThicknessScale, Split = wall.Split });
            }
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
            if (field.Type == Kind.Sheet)
                return field.Radius > 0 && field.Radius <= 10 && field.FirstScale > 0 && field.FirstScale <= 1
                    && field.SecondScale > 0 && field.SecondScale <= 3;
            if (field.Type == Kind.Barricade)
                return field.Forward.sqrMagnitude > .5f && field.Forward.sqrMagnitude < 1.5f
                    && field.FirstScale > 0 && field.FirstScale <= 3 && field.SecondScale > 0 && field.SecondScale <= 3;
            return false;
        }

        public static bool Apply(IReadOnlyList<Field> fields, float elapsed)
        {
            if (fields == null || fields.Count > MaxFields || !Finite(elapsed) || elapsed < 0) return false;
            foreach (var field in fields) if (!Valid(field)) return false;
            // Clear only these two types. Never reset players, casts or resources.
            foreach (var sheet in Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None))
            { sheet.gameObject.SetActive(false); Object.Destroy(sheet.gameObject); }
            foreach (var wall in Object.FindObjectsByType<HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None))
            { wall.gameObject.SetActive(false); Object.Destroy(wall.gameObject); }
            Physics.SyncTransforms();
            foreach (var field in fields)
            {
                float remaining = Mathf.Clamp(field.Remaining - elapsed, 0, field.Duration);
                if (remaining <= .02f) continue;
                if (field.Type == Kind.Sheet)
                {
                    var go = HeroHazards.SpawnIceSheet(field.Position, field.Radius, field.Duration,
                        field.Owner, field.SecondScale, silent: true);
                    var sheet = go.GetComponent<HeroHazards.IceSheetComponent>();
                    sheet.ChillMultiplier = field.FirstScale; sheet.RestoreRemaining(remaining);
                    go.GetComponent<FrostSurfacePresentation>().StepTo(field.Duration - remaining);
                }
                else
                {
                    var go = HeroHazards.SpawnIceBarricade(field.Position, field.Forward, field.Duration,
                        field.FirstScale, field.SecondScale, field.Split, silent: true);
                    go.GetComponent<HeroHazards.IceBarricadeComponent>().RestoreRemaining(remaining);
                }
            }
            return true;
        }
    }
}
