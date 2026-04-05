namespace SkulAPMod.Patches
{
    public static class StageTracker
    {
        public static int Chapter  = -1; // 0=Forest, 1=GrandHall, 2=BlackLab, 3=Fortress
        public static int MapIndex = -1; // 0-based, incremented each time a Normal map loads within the chapter
    }
}
