using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(UI.UnlockNotice), "Show")]
    public class UnlockNotice_Show_Patch
    {
        static bool Prefix(UI.UnlockNotice __instance, Sprite icon, string name,
            Image ____icon, TextMeshProUGUI ____name, Animator ____animator)
        {
            ____icon.sprite = icon;
            ____icon.SetNativeSize();
            ____name.text = name;
            ____name.enableAutoSizing = true;
            ____name.fontSizeMin = 8f;
            ____name.fontSizeMax = 36f;

            __instance.gameObject.SetActive(true);
            if (__instance.gameObject.activeInHierarchy)
            {
                __instance.StopAllCoroutines();
                __instance.StartCoroutine(CustomFadeInOut(__instance, ____animator));
            }

            return false;
        }

        private static IEnumerator CustomFadeInOut(UI.UnlockNotice instance, Animator animator)
        {
            const float holdTime = 4f;

            if (animator.runtimeAnimatorController != null)
            {
                if (!animator.enabled) animator.enabled = true;
                animator.Play(0, 0, 0f);
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float halfLength = stateInfo.length * 0.5f;
            animator.enabled = false;

            for (float t = 0f; t < halfLength; t += Time.unscaledDeltaTime)
            {
                animator.Update(Time.unscaledDeltaTime);
                yield return null;
            }

            for (float t = 0f; t < holdTime; t += Time.unscaledDeltaTime)
                yield return null;

            for (float t = 0f; t < halfLength; t += Time.unscaledDeltaTime)
            {
                animator.Update(Time.unscaledDeltaTime);
                yield return null;
            }

            instance.gameObject.SetActive(false);
        }
    }
}
