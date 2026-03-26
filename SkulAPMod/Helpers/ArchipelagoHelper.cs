using static System.Int32;

namespace SkulAPMod.Helpers
{
    // General Helper Class
    public static class ArchipelagoHelper
    {
        private static bool IsTrue(string str)
        {
            return str is "true" or "1";
        }

        public static bool IsConnectedAndEnabled =>
            SkulAPMod.APClient?.IsConnected ?? false;
        
    }
}
