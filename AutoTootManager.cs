using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.TootTallyGlobals;

namespace TootTallyAutoToot
{
    public static class AutoTootManager
    {
        private static AutoTootController _controller;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void OnGameControllerStartSetEasingFunction(GameController __instance)
        {
            _controller = __instance.pointer.AddComponent<AutoTootController>();
            _controller.Init(__instance);
            TootTallyGlobalVariables.usedAutotoot = false;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.getScoreAverage))]
        [HarmonyPrefix]
        public static void OnGameControllerGetScoreAverageSetPerfectPlay(GameController __instance)
        {
            if (Plugin.Instance.PerfectPlay.Value && _controller.isEnabled)
            {
                __instance.notescoreaverage = 100f;
                __instance.released_button_between_notes = true;
            }
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.doScoreText))]
        [HarmonyPrefix]
        public static void OnGameControllerDoScoreTextSetPerfectPlay(ref int whichtext, ref float notescore)
        {
            if (Plugin.Instance.PerfectPlay.Value && _controller.isEnabled)
            {
                whichtext = 4;
                notescore = 100f;
            }
        }

        /*[HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPostfix]
        public static void OnGameControllerUpdateSetPointerPosition(GameController __instance)
        {
            //I think I gotta do it that way to prevent race conditions
            if (_controller.isEnabled)
                __instance.pointerrect.anchoredPosition = _controller.pointerPosition;
        }*/

        [HarmonyPatch(typeof(GameController), nameof(GameController.isNoteButtonPressed))]
        [HarmonyPostfix]
        public static void OnIsNoteButtonPressedOverwriteValue(GameController __instance, ref bool __result)
        {
            if (_controller.isEnabled && !__instance.freeplay && !__instance.paused && !__instance.quitting)
                __result = _controller.isTooting;
        }
    }
}
