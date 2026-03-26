namespace SkulAPMod
{
    public static class APSessionManager
    {
        public static Scenes.Main PendingMainInstance { get; set; }

        public static void OnConnected()
        {
            if (PendingMainInstance != null)
            {
                Scenes.Main capturedInstance = PendingMainInstance;
                PendingMainInstance = null;

                SkulAPMod.QueueMainThreadAction(() =>
                {
                    Patches.TitleScreenPatch.Suppress = true;
                    capturedInstance.StartGame();
                    Patches.TitleScreenPatch.Suppress = false;
                });
            }
        }
    }
}
