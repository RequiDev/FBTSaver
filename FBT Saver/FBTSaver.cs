using System.Collections.Generic;
using System.Reflection;
using Harmony;
using MelonLoader;
using UnityEngine;

namespace FBT_Saver
{
    public static class BuildInfo
    {
        public const string Name = "FBT Saver";
        public const string Author = "Requi";
        public const string Company = "RequiDev";
        public const string Version = "1.0.1";
        public const string DownloadLink = "https://github.com/RequiDev/FBTSaver";
    }

    internal class FullBodyCalibration
    {
        public Transform Hip;
        public Transform LeftFoot;
        public Transform RightFoot;
    }

    public class FbtSaver : MelonMod
    {
        private static Dictionary<string, FullBodyCalibration> _savedCalibrations;
        public override void OnApplicationStart()
        {
            _savedCalibrations = new Dictionary<string, FullBodyCalibration>();

            var harmonyInstance = HarmonyInstance.Create("FBTSaver");

            MelonModLogger.Log("Patching IsCalibratedForAvatar...");
            harmonyInstance.Patch(
                typeof(VRCTrackingSteam).GetMethod("Method_Public_Virtual_Boolean_String_1"),
                new HarmonyMethod(typeof(FbtSaver).GetMethod("IsCalibratedForAvatar", BindingFlags.Static | BindingFlags.NonPublic)),
                null, null);

            MelonModLogger.Log("Patching PerformCalibration...");
            harmonyInstance.Patch(
                typeof(VRCTrackingSteam).GetMethod("Method_Public_Virtual_Void_Animator_Boolean_Boolean_0"),
                null,
                new HarmonyMethod(typeof(FbtSaver).GetMethod("PerformCalibrationPost", BindingFlags.Static | BindingFlags.NonPublic)),
                null);
            MelonModLogger.Log("Done!");
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
                __instance.hip = savedCalib.Hip;
                __instance.leftFoot = savedCalib.LeftFoot;
                __instance.rightFoot = savedCalib.RightFoot;
                __instance.field_Private_String_0 = avatarId;
                __result = true;
                return false;
            }

            __result = true;
            return false;
        }

        [HarmonyPostfix]
        private static void PerformCalibrationPost(ref VRCTrackingSteam __instance, Animator __0, bool __1, bool __2)
        {
            if (__0.transform.parent == null) return;
            var avatarManager = __0.transform.parent.GetComponent<VRCAvatarManager>();
            if (avatarManager == null) return;
            var avatarId = avatarManager.field_Private_ApiAvatar_0.id;

            _savedCalibrations[avatarId] = new FullBodyCalibration
            {
                Hip = __instance.hip,
                LeftFoot = __instance.leftFoot,
                RightFoot = __instance.rightFoot
            };
        }
    }
}
