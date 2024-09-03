using HarmonyLib;
using QFSW.QC;
using QFSW.QC.UI;

namespace LDGKrey.QCEnabler
{
    [HarmonyPatch]
    static class Patch
    {
        //[HarmonyPatch(typeof(QuantumConsole), "Initialize")]
        //[HarmonyPrefix]
        //public static void InitializeMscorelibFix()
        //{
        //    //Skip command table generation because of problems with mscorlib and the way QC detects scans the assemblies
        //    //var property = typeof(QuantumConsoleProcessor).GetProperty("TableGenerated", BindingFlags.Static | BindingFlags.Public);
        //    //property.SetValue(null, true);
        //}

        [HarmonyPatch(typeof(ZoomUIController), "ZoomUp")]
        [HarmonyPostfix]
        public static void OnZoomUpPostFix(ZoomUIController __instance)
        {
            QuantumConsoleMod.Instance.OnZoomButtonPressed();
        }

        [HarmonyPatch(typeof(ZoomUIController), "ZoomDown")]
        [HarmonyPostfix]
        public static void OnZoomDownPostFix(ZoomUIController __instance)
        {
            QuantumConsoleMod.Instance.OnZoomButtonPressed();
        }

        [HarmonyPatch(typeof(ChatFrame), nameof(ChatFrame.showMessageBox))]
        [HarmonyPrefix]
        //return false to skip
        public static bool OnShowMessageBox(ChatFrame __instance)
        {
            if (QuantumConsole.Instance.IsActive)
                //skip chatframe
                return false;

            //continue opening chatframe
            return true;
        }

        [HarmonyPatch(typeof(MainMenuManager), "Start")]
        [HarmonyPrefix]
        public static void TestMainMenu()
        {
            //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //foreach (var item in assemblies)
            //{
            //    Debug.Log(item.FullName);
            //}

            //var methods = typeof(QuantumConsoleProcessor).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            //foreach (var item in methods)
            //{
            //    Debug.Log(item.Name);
            //}

            //AddFoundryCommands.AddThirdPartyCommands();
            //Skip command table generation because of problems with mscorlib and the way QC detects scans the assemblies
            //var property = typeof(QuantumConsoleProcessor).GetProperty("TableGenerated", BindingFlags.Static | BindingFlags.Public);
            //property.SetValue(null, true);
        }
    }
}