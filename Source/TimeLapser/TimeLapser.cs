using System;
using System.Linq;
using Harmony;
using System.Collections;
using UnityEngine;

namespace TimeLapser
{
    [HarmonyPatch(typeof(SaveLoader), "Save", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
    internal class TemplateMod_SaveLoader_Save
    {
        const int savedViewIndex = 1;

        static System.DateTime lastPostfixTime;

        private static void Postfix(SaveLoader __instance, string filename, bool isAutoSave, bool updateSavePointer)
        {
            var currentTime = System.DateTime.UtcNow;
            if (!isAutoSave || lastPostfixTime + TimeSpan.FromSeconds(1) > currentTime)
            {
                return;
            }
            lastPostfixTime = currentTime;
            Debug.Log(" === TimeLapser_SaveLoader_Save Postfix === ");
            var lastState = new GameStateRestoreInfo();

            var presetLocation = GetSavedCameraLocation(savedViewIndex);
            SetupForScreenshots(presetLocation);

            SafeDelayedAction.EnqueueAction(() =>
            {
                try
                {
                    Debug.Log(" === TimeLapser Taking screenshot");
                    Utilities.PressScreenShotKey();
                }
                catch (Exception e)
                {
                    Debug.LogError(" === TimeLapser Error encountered during state restore");
                    Debug.LogException(e);
                }
            }, 800);
            SafeDelayedAction.EnqueueAction(() =>
            {
                try {
                    Debug.Log(" === TimeLapser Restoring State");
                    ResetFromScreenshots(lastState);
                }
                catch (Exception e)
                {
                    Debug.LogError(" === TimeLapser Error encountered during state restore");
                    Debug.LogException(e);
                }
            }, 1000);
        }


        private class GameStateRestoreInfo
        {
            public Vector3 position;
            public float orthoSize;
            public bool isScreenshotMode;
            public float timeScale;
            public GameStateRestoreInfo()
            {
                this.position = CameraController.Instance.transform.localPosition;
                this.orthoSize = new Traverse(CameraController.Instance).Field("targetOrthographicSize").GetValue<float>();
                this.isScreenshotMode = DebugHandler.HideUI;
                this.timeScale = Time.timeScale;
            }
            public GameStateRestoreInfo(Vector3 pos, float orthSize, bool isScreenshotMode, float timeScale)
            {
                this.position = pos;
                this.orthoSize = orthSize;
                this.isScreenshotMode = isScreenshotMode;
                this.timeScale = timeScale;
            }
        }

        private static Tuple<Vector3, float> GetSavedCameraLocation(int index)
        {
            var navigation = Traverse.Create(SaveGame.Instance.GetComponent<UserNavigation>());

            var firstReal = (navigation.Field("hotkeyNavPoints").GetValue() as IEnumerable)
                .Cast<object>()
                .ElementAt(index - 1);

            var pointTraverse = Traverse.Create(firstReal);
            var orthoSize = pointTraverse.Field("orthoSize").GetValue<float>();
            var pos = pointTraverse.Field("pos").GetValue<UnityEngine.Vector3>();

            return new Tuple<Vector3, float>(pos, orthoSize);
        }

        private static void SetupForScreenshots(Tuple<Vector3, float> savedLocation)
        {
            InputBlockerPatch.blockInput = true;
            if (!DebugHandler.HideUI)
            {
                DebugHandler.ToggleScreenshotMode();
            }
            if (Time.timeScale != 0f)
            {
                Time.timeScale = 0f;
            }
            CameraController instance = CameraController.Instance;
            instance.SetTargetPos(savedLocation.first, savedLocation.second, playSound: false);
            instance.SnapTo(savedLocation.first);
        }

        private static void ResetFromScreenshots(GameStateRestoreInfo restoreInfo)
        {
            if (!restoreInfo.isScreenshotMode)
            {
                Debug.Log(" === TimeLapser toggling screenshot mode");
                DebugHandler.ToggleScreenshotMode();
            }
            Debug.Log(String.Format(" === TimeLapser state restore: TimeScale: {0} CamPos: ({1}, {2}) OrthSize: {3}",
                restoreInfo.timeScale, restoreInfo.position.x, restoreInfo.position.y, restoreInfo.orthoSize));
            Time.timeScale = restoreInfo.timeScale;
            CameraController instance = CameraController.Instance;
            instance.SetTargetPos(restoreInfo.position, restoreInfo.orthoSize, playSound: false);
            instance.SnapTo(restoreInfo.position);
            InputBlockerPatch.blockInput = false;
        }
    }
}