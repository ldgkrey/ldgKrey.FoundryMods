using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LDGKrey.QCEnabler
{
    public static class CommandExtensions
    {
        public static bool AddStaticCommand(MethodInfo method, string alias, string description = "")
            => AddStaticCommandLogic(method, alias, description);

        public static bool AddStaticCommand(MethodInfo method, IEnumerable<string> aliases, string description = "")
        {
            var result = true;
            foreach (var alias in aliases)
            {
                if (!AddStaticCommandLogic(method, alias, description))
                    result = false;
            }

            return result;
        }

        static bool AddStaticCommandLogic(MethodInfo method, string alias, string description = "")
        {
            try
            {
                CommandAttribute commandAttribute = new CommandAttribute(alias, description);
                CommandData commandData = new CommandData(method, commandAttribute);
                return QuantumConsoleProcessor.TryAddCommand(commandData);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        static MethodInfo ExtractCommandMethods_Pointer = typeof(QuantumConsoleProcessor).GetMethod("ExtractCommandMethods", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo LoadCommandsFromMember_Pointer = typeof(QuantumConsoleProcessor).GetMethod("LoadCommandsFromMember", BindingFlags.Static | BindingFlags.NonPublic);
        public static void AddCommandsFromType(Type type)
        {
            if (type.GetCustomAttribute<QcIgnoreAttribute>() != null)
            {
                return;
            }

            foreach (var (method, memberInfo) in ExtractCommandMethods_Pointer.Invoke(null, new object[] { type }) as IEnumerable<(MethodInfo, MemberInfo)>)
            {
                if (memberInfo.DeclaringType == type)
                {
                    LoadCommandsFromMember_Pointer.Invoke(null, new object[] { memberInfo, method });
                }
            }
        }

        public static void AddCommandsFromType<T>()
        {
            var type = typeof(T);
            AddCommandsFromType(type);
        }
    }
}
