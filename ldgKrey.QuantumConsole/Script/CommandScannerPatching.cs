using HarmonyLib;
using QFSW.QC;
using System;
using System.Reflection;
using UnityEngine;

namespace LDGKrey.QCEnabler
{
    [HarmonyPatch]
    static class CommandScannerPatch
    {
        //rewrite the complete Logic
        [HarmonyPatch(typeof(QuantumConsoleProcessor), "AssemblyRequiresScan")]
        [HarmonyPrefix]
        static bool AssemblyRequiresScan_Patch(ref bool __result, Assembly assembly)
        {
            //error producer --System.Reflection.CustomAttributeExtensions.GetCustomAttribute[T] (System.Reflection.Assembly element)
            //var hasIgnoreAttribute = assembly.GetCustomAttribute<QcIgnoreAttribute>() != null;
            //if (hasIgnoreAttribute)
            //    return false;

            string[] array = new string[14] { "System", "Unity", "Microsoft", "Mono.", "mscorlib", "NSubstitute", "JetBrains", "nunit.", "QFSW.QC", "Newtonsoft", "Sirenix", "Rewired", "Harmony", "Dreamteck" };
            string[] array2 = new string[2] { "mcs", "AssetStoreTools" };
            string fullName = assembly.FullName;
            string[] array3 = array;
            foreach (string value in array3)
            {
                if (fullName.StartsWith(value))
                {
                    return false;
                }
            }

            string name = assembly.GetName().Name;
            array3 = array2;
            foreach (string text in array3)
            {
                if (name == text)
                {
                    return false;
                }
            }

            //skip original but set result to true
            __result = true;
            return false;
        }

        //rewrite the complete Logic
        [HarmonyPatch(typeof(QuantumConsoleProcessor), "LoadCommandsFromType")]
        [HarmonyPrefix]
        static bool LoadCommandsFromType_Patch(Type type)
        {
            CommandExtensions.AddCommandsFromType(type);

            //skip original
            return false;
        }

        [HarmonyPatch(typeof(QuantumConsole), "Initialize")]
        [HarmonyPrefix]
        static void Initialize_Patch()
        {
            var field = typeof(QuantumConsoleProcessor).GetField("_loadedAssemblies", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, AppDomain.CurrentDomain.GetAssemblies());
        }

        //[HarmonyPatch(typeof(QuantumConsoleProcessor), "GenerateCommandTable")]
        //[HarmonyPrefix]
        //public static void LoadCommandsFromType_Patch(bool deployThread, bool forceReload)
        //{
        //    Debug.LogError($"Called {deployThread}, {forceReload}");
        //}
    }
}
