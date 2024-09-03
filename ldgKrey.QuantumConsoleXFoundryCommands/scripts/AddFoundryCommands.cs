//#define Dump_Regex

using FoundryCommands;
using HarmonyLib;
using ldgKrey.QuantumConsoleXFoundryCommands.Suggestors;
using Mod.QFSW.QC.Suggestors.Tags;
using QFSW.QC;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unfoundry;
using UnityEngine;

/*
^\/help(?:(?:\s+)(.+))?$
^\/drag\s*?(?:\s+(\d+(?:\.\d*)?))?$
^\/(?:(?:tp)|(?:teleport))(?:\s+(.*?)\s*)?$
^\/(?:(?:tpr)|(?:ret)|(?:return))$
^\/(?:(?:monitor)|(?:mon))\s*?(?:\s+(\d+(?:\.\d*)?))?$
^\/(?:(?:skyPlatform)|(?:sp))$
^\/time$
^\/time\s+([012]?\d)(?:\:(\d\d))?$
^\/(?:(?:c)|(?:calc)|(?:calculate))\s+(.+)$
^\/count$
^\/give(?:\s+(.+?)(?:\s+(\d+))?)?$
*/

namespace LDGKrey.QCEnabler
{
    public static class AddFoundryCommands
    {
        static object[] FoundryCommands_CommandHandlers;
        static LogSource log = new LogSource("QxFC");
        static bool IsEnabled = false;

        [HarmonyPatch]
        public static class Patch
        {
            [HarmonyPatch(typeof(FoundryCommandsSystem), "OnAddedToWorld")]
            [HarmonyPostfix]
            static void OnAddedToWorld_AddHooks_Patch()
            {
                AddFoundryCommandsToQuantumConsole();
            }

            [HarmonyPatch(typeof(FoundryCommandsSystem), "OnRemovedFromWorld")]
            [HarmonyPostfix]
            static void OnRemovedFromWorld_ReleaseHooks_Patch()
            {
                FoundryCommands_CommandHandlers = null;

                HelpCommandDelegate = null;
                DragCommandDelegate = null;
                TeleportCommandDelegate = null;
                TeleportBackCommandDelegate = null;
                MonitorCommandDelegate = null;
                SkyPlatformCommandDelegate = null;
                TimeCommandDelegate = null;
                TimeSetCommandDelegate = null;
                CalculateCommandDelegate = null;
                CountCommandDelegate = null;
                GiveCommandDelegate = null;
            }
        }

        //[Command("testAddThird")]
        static void AddFoundryCommandsToQuantumConsole()
        {
            log.Log($"AddFoundryCommands");

            var commandHandler_Field = typeof(FoundryCommandsSystem).GetField("_commandHandlers", BindingFlags.Instance | BindingFlags.NonPublic);
            if (commandHandler_Field == null)
            {
                log.Log($"Add: Not available");
                return;
            }

            if (GlobalStateManager.getCurrentGameState() != GlobalStateManager.GameState.Game)
                return;

            FoundryCommands_CommandHandlers = (object[])commandHandler_Field.GetValue(FoundryCommandsSystem.Instance);

            //var instance = FCSInstanceGetter.Invoke(null, Array.Empty<object>());

            //Debug.Log(instance != null);
            //var commandHandlers = foundryCommandsSystemType.GetField("_commandHandlers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);

            //FoundryCommands_CommandHandlers = commandHandlers as object[];

            //Debug.Log(FoundryCommands_CommandHandlers != null);
            //if (FoundryCommands_CommandHandlers == null)
            //    return;
            //Debug.Log(FoundryCommands_CommandHandlers.Length);

            #region Regex Dump
#if Dump_Regex
            foreach (var command in FoundryCommands_CommandHandlers)
            {
                var valueType = command.GetType();
                //Debug.Log(valueType != null);
                if (valueType == null)
                    return;
                //Debug.Log(valueType.FullName);

                var valueTypePatternField = valueType.GetField("regex", BindingFlags.Instance | BindingFlags.NonPublic);
                //Debug.Log(valueTypePatternField != null);
                if (valueTypePatternField == null)
                    return;
                //Debug.Log(valueTypePatternField.Name);

                var valueCommandPattern = valueTypePatternField.GetValue(command) as Regex;
                //Debug.Log(valueCommandPattern != null);
                if (valueCommandPattern == null)
                    return;
                Debug.Log(valueCommandPattern.ToString());
            }
#endif
            #endregion

            SetupCommands();
            IsEnabled = true;
        }

        static void SetupCommands()
        {
            int index = 0;

            HelpCommandDelegate = GetCommandInvoke(index++);
            DragCommandDelegate = GetCommandInvoke(index++);
            TeleportCommandDelegate = GetCommandInvoke(index++);
            TeleportBackCommandDelegate = GetCommandInvoke(index++);
            MonitorCommandDelegate = GetCommandInvoke(index++);
            SkyPlatformCommandDelegate = GetCommandInvoke(index++);
            TimeCommandDelegate = GetCommandInvoke(index++);
            TimeSetCommandDelegate = GetCommandInvoke(index++);
            CalculateCommandDelegate = GetCommandInvoke(index++);
            CountCommandDelegate = GetCommandInvoke(index++);
            GiveCommandDelegate = GetCommandInvoke(index++);
        }

        static Type commandType;
        static CommandObjectPair GetCommandInvoke(int index)
        {
            if (FoundryCommands_CommandHandlers.Length <= index)
            {
                //QuantumConsoleMod.log.LogWarning($"Cant get FoundryCommand {index}, (OutOfRange)");
                return default;
            }

            var command = FoundryCommands_CommandHandlers[index];

            if (commandType == null)
            {
                commandType = command.GetType();
            }

            var command_OnProcessCommandField = commandType.GetField("onProcessCommand", BindingFlags.Instance | BindingFlags.NonPublic);
            var command_OnProcessCommandValue = command_OnProcessCommandField.GetValue(command);

            return new CommandObjectPair()
            {
                Method = command_OnProcessCommandValue.GetType().GetMethod("Invoke"),
                Object = command_OnProcessCommandValue
            };
        }

        #region Commands

        static CommandObjectPair HelpCommandDelegate;
        [Command("FC.Help", "Help for FoundryCommands")]
        [Command("FoundryCommands.Help", "Help for FoundryCommands")]
        static void HelpCommand(
            [Suggestions("drag", "teleport", "return", "monitor", "skyplatform", "time", "calculate", "count", "give")] string command = "")
        {
            if (!IsEnabled || HelpCommandDelegate == null)
                return;

            var parameters = command == string.Empty ? new string[0] : new string[] { command };
            HelpCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair DragCommandDelegate;
        [Command("drag", "Change the maximum range for drag building.")]
        static void DragCommand(int dragSize = 0)
        {
            if (!IsEnabled || DragCommandDelegate == null)
                return;

            var parameters = dragSize == default
                ? new string[] { "0" }
                : new string[] { dragSize.ToString() };
            DragCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair TeleportCommandDelegate;
        [Command("tp", "Teleport to the named waypoint.")]
        [Command("teleport", "Teleport to the named waypoint.")]
        static void TeleportCommand([TeleportSuggestion] string waypointName)
        {
            if (!IsEnabled || TeleportCommandDelegate == null)
                return;

            var parameters = new string[] { waypointName.ToString() };
            TeleportCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair TeleportBackCommandDelegate;
        [Command("tpr", "Teleport back.")]
        [Command("ret", "Teleport back.")]
        [Command("return", "Teleport back.")]
        static void TeleportBackCommand()
        {
            if (!IsEnabled || TeleportBackCommandDelegate == null)
                return;

            var parameters = new string[0];
            TeleportBackCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair MonitorCommandDelegate;
        [Command("mon", "Monitors a tank or logistics container's contents once per second or custom interval. Use while not lookong at a valid building to stop.")]
        [Command("monitor", "Monitors a tank or logistics container's contents once per second or custom interval. Use while not lookong at a valid building to stop.")]
        static void MonitorCommand(int custominterval = -1)
        {
            if (!IsEnabled || MonitorCommandDelegate == null)
                return;

            var parameters = custominterval < 0
                ? new string[0]
                : new string[] { custominterval.ToString() };
            MonitorCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair SkyPlatformCommandDelegate;
        [Command("skyplatform", "Opens the sky platform frame.")]
        static void SkyPlatformCommand()
        {
            if (!IsEnabled || SkyPlatformCommandDelegate == null)
                return;

            var parameters = new string[0];
            SkyPlatformCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair TimeCommandDelegate;
        static CommandObjectPair TimeSetCommandDelegate;
        [Command("time", "Displays or sets the time of day. Format for set: HH or HH:MM")]
        static void TimeCommand(int hour = -1, int minutes = -1)
        {
            if (!IsEnabled
                || TimeCommandDelegate == null
                || TimeSetCommandDelegate == null)
                return;

            if (hour < 0)
                TimeCommandDelegate.Invoke(new string[0]);
            else
            {
                var parameters = minutes < 0
                    ? new string[] { hour.ToString() }
                    : new string[] { hour.ToString(), minutes.ToString() };

                TimeSetCommandDelegate.Invoke(parameters);
            }

        }

        static CommandObjectPair CalculateCommandDelegate;
        [Command("calc", "calculate the result of a mathemetical expression. See expressive wiki for available functions.")]
        [Command("calculate", "calculate the result of a mathemetical expression. See expressive wiki for available functions.")]
        [Command("c", "calculate the result of a mathemetical expression. See expressive wiki for available functions.")]
        static void CalculateCommand(string expression)
        {
            if (!IsEnabled || CalculateCommandDelegate == null)
                return;

            var parameters = new string[] { expression };
            CalculateCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair CountCommandDelegate;
        [Command("count", "Dump counts for all buildings within loading distance of the player. Saves to %AppData%\\...\\LocalLow\\Channel3 Entertainment\\Foundry\\FoundryCommands\\count.txt")]
        static void CountCommand()
        {
            if (!IsEnabled || CountCommandDelegate == null)
                return;

            var parameters = new string[0];
            CountCommandDelegate.Invoke(parameters);
        }

        static CommandObjectPair GiveCommandDelegate;
        [Command("give", "Opens the sky platform frame.")]
        static void GiveCommand([ItemSuggestion]string itemname, int amount = -1)
        {
            if (!IsEnabled || GiveCommandDelegate == null)
                return;

            var parameters = amount < 0
                ? new string[] { itemname }
                : new string[] { itemname, amount.ToString() };
            GiveCommandDelegate.Invoke(parameters);
        }

        #endregion

        #region Helper
        class CommandObjectPair
        {
            public MethodInfo Method;
            public object Object;

            public void Invoke(string[] parameters)
            {
                Method.Invoke(Object, new object[] { parameters });
            }
        }

        #region Suggestors


        #endregion
        #endregion
    }
}
