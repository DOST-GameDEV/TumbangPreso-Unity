using UnityEngine;

namespace TumbangPreso
{
    /// <summary>Retain the native scene query between slipper births and destruction.</summary>
    internal static class BotSlipperInventory
    {
        private static Slipper[] _snapshot;
        public static void Invalidate() => _snapshot = null;
        public static View All => default;

        public readonly struct View
        {
            public Enumerator GetEnumerator()
            {
                // Include inactive objects, then filter their current activity below. A
                // disabled Slipper component does not get OnEnable when its object wakes.
                // Ownership and flight state remain live reads, never cached selections.
                if (_snapshot == null)
                    _snapshot = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include);
                return new Enumerator(_snapshot);
            }
        }
        public struct Enumerator
        {
            private readonly Slipper[] _items;
            private int _next;
            public Slipper Current { get; private set; }
            public Enumerator(Slipper[] items) { _items = items; _next = 0; Current = null; }
            public bool MoveNext()
            {
                while (_next < _items.Length)
                {
                    var shoe = _items[_next++];
                    if (shoe == null || !shoe.gameObject.activeInHierarchy) continue;
                    Current = shoe; return true;
                }
                Current = null; return false;
            }
        }
    }
}
