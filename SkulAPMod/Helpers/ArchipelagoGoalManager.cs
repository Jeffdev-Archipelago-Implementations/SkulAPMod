namespace SkulAPMod.Helpers
{
    // Goal Helper Classs
    public static class ArchipelagoGoalManager
    {
        public static long GetGoalId()
        {
            string goalString = SkulAPMod.APClient.GetSlotDataValue("goal");
            long.TryParse(goalString, out long goalId);

            return goalId;
        }

        public static void CheckAndCompleteGoal()
        {
            if (!Utils.IsConnectedAndEnabled) return;
        }

        private static void CompleteGoal()
        {
            SkulAPMod.APClient.GetSession().SetGoalAchieved();
        }
    }
}
