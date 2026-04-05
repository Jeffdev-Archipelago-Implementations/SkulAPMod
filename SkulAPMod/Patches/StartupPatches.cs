using HarmonyLib;
using UnityEngine;
using System.IO;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Application), "persistentDataPath", MethodType.Getter)]
    public static class Application_persistentDataPath_Patch
    {
        static void Postfix(ref string __result)
        {
            __result = Path.Combine(__result, "archipelago");
            if (!Directory.Exists(__result))
            {
                Directory.CreateDirectory(__result);
            }
        }
    }
}
