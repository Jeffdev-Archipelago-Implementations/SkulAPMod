using Archipelago.MultiClient.Net.Enums;
using Data;

namespace SkulAPMod
{
    public static class Utils
    {
        public static bool IsTrue(string str)
        {
            return str is "true" or "1";
        }

        public static bool IsConnectedAndEnabled =>
            SkulAPMod.APClient?.IsConnected ?? false;
        
        public static string GetItemColor(ItemFlags flags)
        {
            return flags switch
            {
                _ when (flags & ItemFlags.Trap) != 0         => "fa8080",
                _ when (flags & ItemFlags.Advancement) != 0  => "9676f5",
                _ when (flags & ItemFlags.NeverExclude) != 0 => "318ce0",
                _                                             => "ffffff"
            };
        }

        public static string GetItemDescText(ItemFlags flags, string playerName="")
        {
            string playerAdd = (playerName.Length > 0 ? " for " + playerName : "");
            return flags switch
            {
                _ when (flags & ItemFlags.Trap) != 0         => "Dangerous one" + playerAdd + ", this is...",
                _ when (flags & ItemFlags.Advancement) != 0  => "This trembles with an aura of importance" + playerAdd + "...",
                _ when (flags & ItemFlags.NeverExclude) != 0 => "Appears to be of some use" + playerAdd + "...",
                _                                             => "Nothing special" + playerAdd + ", really..."
            };
        }
    }
}