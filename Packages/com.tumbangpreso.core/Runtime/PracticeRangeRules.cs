namespace TumbangPreso.Core
{
    public static class PracticeRangeRules
    {
        public static bool Allowed(bool requested, bool networked, bool networkSelected,
            bool authorityRevoked, bool tutorial, bool observing) =>
            requested && !networked && !networkSelected && !authorityRevoked && !tutorial && !observing;
    }
}
