using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UIExpansionKit.API;

namespace FBT_Saver
{
    public static class BuildInfo
    {
        public const string Name = "FBT Saver";
        public const string Author = "Requi";
        public const string Company = "RequiDev";
        public const string Version = "1.1.5";
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

            if (MelonHandler.Mods.Find(m => m.Info.Name == "UI Expansion Kit") != null)
            {
                MelonLogger.Log("Adding UIX Button to clear saved calibrations...");
                ExpansionKitApi.GetExpandedMenu(ExpandedMenu.AvatarMenu).AddSimpleButton("Clear Saved FBT Calibrations",
                    () =>
                    {
                        _savedCalibrations.Clear();
                        File.Delete($"{CalibrationsDirectory}{CalibrationsFile}");
                        MelonLogger.Log("Cleared Saved Calibrations");
                    });
            }

            MelonLogger.Log("Done!");
        }

        private static void PerformCalibration(ref VRCTrackingSteam __instance, Animator __0, bool __1, bool __2)
        {
            var avatarId = VRCPlayer.field_Internal_Static_VRCPlayer_0._player.prop_ApiAvatar_0.id;
            _savedCalibrations[avatarId] = new FbtCalibration
            {
                LeftFoot = new KeyValuePair<Vector3, Quaternion>(__instance.field_Public_Transform_10.localPosition, __instance.field_Public_Transform_10.localRotation),
                RightFoot = new KeyValuePair<Vector3, Quaternion>(__instance.field_Public_Transform_11.localPosition, __instance.field_Public_Transform_11.localRotation),
                Hip = new KeyValuePair<Vector3, Quaternion>(__instance.field_Public_Transform_12.localPosition, __instance.field_Public_Transform_12.localRotation),
            };

            try
            {
                File.WriteAllText($"{CalibrationsDirectory}{CalibrationsFile}", JsonConvert.SerializeObject(_savedCalibrations, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = new DynamicContractResolver("normalized")
                }));
            }
            catch(Exception e)
            {
                File.WriteAllText($"{CalibrationsDirectory}error.log", e.Message);
                MelonLogger.LogError(
                    $"Could not save current calibration to file! Created error.log in /UserData/FTBSaver. Please create an issue on GitHub or message Requi in the VRChat Modding Group Discord with that file.");
            }
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
                __instance.field_Public_Transform_10.localPosition = savedCalib.LeftFoot.Key;
                __instance.field_Public_Transform_10.localRotation = savedCalib.LeftFoot.Value;

                __instance.field_Public_Transform_11.localPosition = savedCalib.RightFoot.Key;
                __instance.field_Public_Transform_11.localRotation = savedCalib.RightFoot.Value;

                __instance.field_Public_Transform_12.localPosition = savedCalib.Hip.Key;
                __instance.field_Public_Transform_12.localRotation = savedCalib.Hip.Value;
            }

            __result = true;
            return false;
        }

        private class DynamicContractResolver : DefaultContractResolver
        {
            private readonly string _propertyNameToExclude;

            public DynamicContractResolver(string propertyNameToExclude)
            {
                _propertyNameToExclude = propertyNameToExclude;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);

                // only serializer properties that are not named after the specified property.
                properties =
                    properties.Where(p => string.Compare(p.PropertyName, _propertyNameToExclude, StringComparison.OrdinalIgnoreCase) != 0).ToList();

                return properties;
            }
        }
    }
}
