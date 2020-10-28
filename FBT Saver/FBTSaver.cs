using System.Linq;
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
        public const string Version = "1.0.8";
        public const string DownloadLink = "https://github.com/RequiDev/FBTSaver";
    }

    public class FbtSaver : MelonMod
    {
        public override void OnApplicationStart()
        {
            var instance = HarmonyInstance.Create("FBTSaver");

            MelonLogger.Log("Patching IsCalibratedForAvatar...");

            // Yoinked from emm. Thanks <3
            var methods = typeof(VRCTrackingSteam).GetMethods();
            foreach (var methodInfo in methods)
            {
                if (methodInfo.GetParameters().Length == 1 && methodInfo.GetParameters().First().ParameterType == typeof(string) && methodInfo.ReturnType == typeof(bool) && methodInfo.GetRuntimeBaseDefinition() == methodInfo)
                {
                    instance.Patch(methodInfo, new HarmonyMethod(typeof(FbtSaver).GetMethod(nameof(IsCalibratedForAvatar), BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
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
