using HarmonyLib;
using TootTallyCore.Utils.TootTallyGlobals;
using TrombLoader.Data;
using UnityEngine;

namespace TootTallyAutoToot
{
    public static class AutoTootManager
    {
        private static AutoTootController _controller;
        private static TromboneEventManager[] _eventManagers;
        private static BackgroundPuppetController _bgPuppetController;
        private static Vector2 _screenDim;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void OnGameControllerStartSetEasingFunction(GameController __instance)
        {
            _controller = __instance.pointer.AddComponent<AutoTootController>();
            _controller.Init(__instance);
            if (__instance.bgcontroller != null)
            {
                _eventManagers = __instance.bgcontroller.fullbgobject.GetComponentsInChildren<TromboneEventManager>();
                _bgPuppetController = __instance.bgcontroller.fullbgobject.GetComponent<BackgroundPuppetController>();
            }
            else
            {
                _eventManagers = null;
                _bgPuppetController = null;
            }
            _screenDim = new Vector2(Screen.width, Screen.height);
            TootTallyGlobalVariables.usedAutotoot = false;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.getScoreAverage))]
        [HarmonyPrefix]
        public static void OnGameControllerGetScoreAverageSetPerfectPlay(GameController __instance)
        {
            if (Plugin.Instance.PerfectPlay.Value && _controller.isEnabled)
            {
                __instance.notescoreaverage = 100f;
                __instance.notescoretotal = 100f;
                __instance.released_button_between_notes = true;
                __instance.released_during_timing_window = true;
                __instance.force_no_gap_gameobject_to_appear = false;
                __instance.notescoresamples = 1f;
            }
        }

        //Shouldn't need this anymore lol
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

        [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPostfix]
        public static void OnGameControllerUpdateSetPointerPosition(GameController __instance)
        {
            if (_controller.isEnabled && _bgPuppetController != null)
            {
                _bgPuppetController.DoPuppetControl(-_controller.pointerPosition.y / 225, __instance.vibratoamt);
            }
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.isNoteButtonPressed))]
        [HarmonyPostfix]
        public static void OnIsNoteButtonPressedOverwriteValue(GameController __instance, ref bool __result)
        {
            if (_controller.isEnabled && !__instance.freeplay && !__instance.paused && !__instance.quitting)
                __result = _controller.isTooting;
        }


        private static bool _lastIsTooting;
        [HarmonyPatch(typeof(TromboneEventInvoker), nameof(TromboneEventInvoker.LateUpdate))]
        [HarmonyPostfix]
        public static void TromboneEventInvokerPostfix(TromboneEventInvoker __instance)
        {
            if (_controller.isEnabled && _eventManagers != null)
                if (_controller.isTooting)
                    if (_lastIsTooting == false)
                    {
                        _lastIsTooting = true;
                        foreach (var manager in _eventManagers) manager.PlayerTootInputStart?.Invoke();
                    }
                    else
                    if (_lastIsTooting == true)
                    {
                        _lastIsTooting = false;
                        foreach (var manager in _eventManagers) manager.PlayerTootInputEnd?.Invoke();
                    }
        }

        [HarmonyPatch(typeof(TromboneEventManager), nameof(TromboneEventManager.Update))]
        [HarmonyPostfix]
        public static void TromboneEventManagerPostfix(TromboneEventManager __instance)
        {
            if (_controller.isEnabled && _eventManagers != null)
            {
                var pos = ConvertPointerPosToMousePos(_controller.pointerPosition);
                Traverse.Create(__instance).Field("mousePosition").SetValue(pos);
                __instance.MousePositionUpdated.Invoke(new Vector2(pos.x / _screenDim.x, pos.y / _screenDim.y));
            }
        }

        private static Vector2 ConvertPointerPosToMousePos(Vector2 pointerPos)
        {
            pointerPos.y = ((pointerPos.y + 225) / 450) * _screenDim.y;
            pointerPos.x = pointerPos.x / _screenDim.x;
            return pointerPos;
        }
    }
}
