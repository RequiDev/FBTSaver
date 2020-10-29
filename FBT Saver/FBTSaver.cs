using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;

namespace FBT_Saver
{
    public static class BuildInfo
    {
        public const string Name = "FBT Saver";
        public const string Author = "Requi";
        public const string Company = "RequiDev";
        public const string Version = "1.1.0";
        public const string DownloadLink = "https://github.com/RequiDev/FBTSaver";
    }

    public class FbtSaver : MelonMod
    {
        private class FbtCalibration
        {
            public KeyValuePair<Vector3, Quaternion> Hip;
            public KeyValuePair<Vector3, Quaternion> LeftFoot;
            public KeyValuePair<Vector3, Quaternion> RightFoot;
        }

        private static Dictionary<string, FbtCalibration> _savedCalibrations;

        private const string CalibrationsDirectory = "UserData/FBTSaver/";
        private const string CalibrationsFile = "calibrations.json";

        public override void OnApplicationStart()
        {
            var instance = HarmonyInstance.Create("FBTSaver");

            Directory.CreateDirectory(CalibrationsDirectory);

            if (File.Exists($"{CalibrationsDirectory}{CalibrationsFile}"))
            {
                _savedCalibrations =
                    JsonConvert.DeserializeObject<Dictionary<string, FbtCalibration>>(
                        File.ReadAllText($"{CalibrationsDirectory}{CalibrationsFile}"));

                MelonLogger.Log($"Loaded {_savedCalibrations.Count} calibrations from disk.");
            }
            else
            {
                MelonLogger.Log($"No saved calibrations found. Creating new.");
                _savedCalibrations = new Dictionary<string, FbtCalibration>(128);
                File.WriteAllText($"{CalibrationsDirectory}{CalibrationsFile}", JsonConvert.SerializeObject(_savedCalibrations, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore // otherwise it gets fucky on Vector3.normalized.normalized...
                }));
            }

            MelonLogger.Log("Patching IsCalibratedForAvatar...");

            // Yoinked from emm. Thanks <3
            var methods = typeof(VRCTrackingSteam).GetMethods();
            foreach (var methodInfo in methods)
            {
                switch (methodInfo.GetParameters().Length)
                {
                    case 1 when methodInfo.GetParameters().First().ParameterType == typeof(string) && methodInfo.ReturnType == typeof(bool) && methodInfo.GetRuntimeBaseDefinition() == methodInfo:
                        instance.Patch(methodInfo, new HarmonyMethod(typeof(FbtSaver).GetMethod(nameof(IsCalibratedForAvatar), BindingFlags.Static | BindingFlags.NonPublic)));
                        break;
                    case 3 when methodInfo.GetParameters().First().ParameterType == typeof(Animator) && methodInfo.ReturnType == typeof(void) && methodInfo.GetRuntimeBaseDefinition() == methodInfo:
                        instance.Patch(methodInfo, null, new HarmonyMethod(typeof(FbtSaver).GetMethod(nameof(PerformCalibration), BindingFlags.Static | BindingFlags.NonPublic)));
                        break;
                }
            }
            MelonLogger.Log("Done!");
        }

        private static void PerformCalibration(ref VRCTrackingSteam __instance, Animator __0, bool __1, bool __2)
        {
            var avatarId = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_VRCAvatarManager_0.field_Private_ApiAvatar_0.id;
            _savedCalibrations[avatarId] = new FbtCalibration
            {
                Hip = new KeyValuePair<Vector3, Quaternion>(__instance.hip.localPosition, __instance.hip.localRotation),
                LeftFoot = new KeyValuePair<Vector3, Quaternion>(__instance.leftFoot.localPosition, __instance.leftFoot.localRotation),
                RightFoot = new KeyValuePair<Vector3, Quaternion>(__instance.rightFoot.localPosition, __instance.rightFoot.localRotation),
            };

            File.WriteAllText($"{CalibrationsDirectory}{CalibrationsFile}", JsonConvert.SerializeObject(_savedCalibrations, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
        }

        private static bool IsCalibratedForAvatar(ref VRCTrackingSteam __instance, ref bool __result, string __0)
        {
            if (__instance.field_Private_String_0 == null)
            {
                __result = false;
                return true;
            }

            var avatarId = __0;
            if (_savedCalibrations.ContainsKey(avatarId))
            {
                var savedCalib = _savedCalibrations[avatarId];
                __instance.hip.localPosition = savedCalib.Hip.Key;
                __instance.hip.localRotation = savedCalib.Hip.Value;

                __instance.leftFoot.localPosition = savedCalib.LeftFoot.Key;
                __instance.leftFoot.localRotation = savedCalib.LeftFoot.Value;

                __instance.rightFoot.localPosition = savedCalib.RightFoot.Key;
                __instance.rightFoot.localRotation = savedCalib.RightFoot.Value;
            }

            __result = true;
            return false;
        }
    }
}
