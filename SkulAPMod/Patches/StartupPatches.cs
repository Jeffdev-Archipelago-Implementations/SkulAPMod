using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Diagnostics;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Application), "persistentDataPath", MethodType.Getter)]
    public static class Application_persistentDataPath_Patch
    {
        private static string _originalPath;
        private static string _slotSubPath;

        private const string SessionFileName = "archipelago_session.txt";

        // Returns true if the slot changed and the game needs to restart so the new
        // path is in place before save data is read on the next launch.
        public static bool SetSlot(string slotName, string seed)
        {
            string newSubPath = $"{slotName}_{seed}";
            bool changed = newSubPath != _slotSubPath;
            _slotSubPath = newSubPath;
            if (_originalPath != null)
                File.WriteAllText(Path.Combine(_originalPath, SessionFileName), _slotSubPath);
            return changed;
        }

        static void Postfix(ref string __result)
        {
            if (_originalPath == null)
            {
                _originalPath = __result;

                string sessionFile = Path.Combine(_originalPath, SessionFileName);
                if (File.Exists(sessionFile))
                    _slotSubPath = File.ReadAllText(sessionFile).Trim();
            }

            string subPath = string.IsNullOrEmpty(_slotSubPath)
                ? "archipelago"
                : Path.Combine("archipelago", _slotSubPath);
            __result = Path.Combine(_originalPath, subPath);
            if (!Directory.Exists(__result))
                Directory.CreateDirectory(__result);
        }
    }
}
