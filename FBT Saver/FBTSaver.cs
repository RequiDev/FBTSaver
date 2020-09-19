using System.Reflection;
using Harmony;
using MelonLoader;

namespace FBT_Saver
{
    public static class BuildInfo
    {
        public const string Name = "FBT Saver";
        public const string Author = "Requi";
        public const string Company = "RequiDev";
        public const string Version = "1.0.4";
        public const string DownloadLink = "https://github.com/RequiDev/FBTSaver";
    }

    public class FbtSaver : MelonMod
    {
        public override void OnApplicationStart()
        {
            var harmonyInstance = HarmonyInstance.Create("FBTSaver");

            MelonLogger.Log("Patching IsCalibratedForAvatar...");
            harmonyInstance.Patch(
                typeof(VRCTrackingSteam).GetMethod(nameof(VRCTrackingSteam.Method_Public_Virtual_Boolean_String_0)),
                new HarmonyMethod(typeof(FbtSaver).GetMethod(nameof(IsCalibratedForAvatar), BindingFlags.Static | BindingFlags.NonPublic)));
            MelonLogger.Log("Done!");
        }

        private static bool IsCalibratedForAvatar(ref VRCTrackingSteam __instance, ref bool __result, string __0)
        {
            if (__instance.field_Private_String_0 == null)
            {
                __result = false;
                return true;
            }

            __result = true;
            return false;
        }
    }
}
