namespace TumbangPreso.Core
{
    /// <summary>Eight deliberate directions; no B/A suffix. A completion starts a fresh sequence.</summary>
    public sealed class CreditsCodeSequence
    {
        public const int Reward = 5000;
        private static readonly int[] Pattern = { 0, 0, 1, 1, 2, 3, 2, 3 };
        private readonly int[] _recent = new int[8];
        private int _count;
        public void Reset() => _count = 0;
        public bool Push(int direction)
        {
            if (direction < 0 || direction > 3) { Reset(); return false; }
            if (_count == _recent.Length)
                for (int i = 1; i < _recent.Length; i++) _recent[i - 1] = _recent[i];
            else _count++;
            _recent[_count - 1] = direction;
            if (_count < Pattern.Length) return false;
            for (int i = 0; i < Pattern.Length; i++) if (_recent[i] != Pattern[i]) return false;
            Reset(); return true;
        }
    }
}
