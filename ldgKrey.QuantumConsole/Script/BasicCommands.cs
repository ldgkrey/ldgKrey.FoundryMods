using Mod.QFSW.QC.Suggestors.Tags;
using QFSW.QC;
using System.Linq;
using System.Reflection;

namespace LDGKrey.QCEnabler
{
    public static class BasicCommands
    {
        public static void AddCommands()
        {
            CommandExtensions.AddCommandsFromType(typeof(QuantumConsoleProcessor));
            AddCommandNameAttribute();

            //AddHelp();
            //AddCommandCount();
            //AddManualHelp();
            //AddHelpCommand();
            //AddCommandList();
            //AddClearCommand();
            //AddChangeLogLevelCommand();


        }

        //To help,man,manual
        static void AddCommandNameAttribute()
        {
            var commands = QuantumConsoleProcessor.GetAllCommands();
            var injectionData = typeof(BasicCommands).GetMethod(nameof(GenerateCommandManual), BindingFlags.Static | BindingFlags.NonPublic).GetParameters();
            var data_Pointer = typeof(CommandData).GetField(nameof(CommandData.MethodParamData), BindingFlags.Instance | BindingFlags.Public);

            foreach (var item in commands.Where(x => x.CommandName == "help" || x.CommandName == "manual" || x.CommandName == "man"))
            {
                //QuantumConsoleMod.log.Log($"item: {item.CommandName}");
                //QuantumConsoleMod.log.Log($"param: {string.Join(',', item.MethodParamData.Select(x => x.ToString()))}");

                if (item.MethodParamData.Length != 1)
                    continue;

                //QuantumConsoleMod.log.Log("inject");

                data_Pointer.SetValue(item, injectionData);
            }

        }

        //static MethodInfo GenerateCommandManual_Method = typeof(QuantumConsoleProcessor).GetMethod("GenerateCommandManual", BindingFlags.Static | BindingFlags.NonPublic);

        //[CommandDescription("Generates a user manual for any given command, including built in ones. To use the man command, simply put the desired command name infront of it. For example, 'man my-command' will generate the manual for 'my-command'")]
        //[Command("help")]
        //[Command("manual")]
        //[Command("man")]
        private static void GenerateCommandManual([CommandName] string commandName) { }

        //static void AddCommandCount()
        //{
        //    var method = typeof(QuantumConsoleProcessor).GetProperty(nameof(QuantumConsoleProcessor.LoadedCommandCount)).GetMethod;

        //    CommandExtensions.AddStaticCommand(method, "command-count", "Gets the number of loaded commands.");
        //}

        //static void AddHelp()
        //{
        //    var method = typeof(QuantumConsoleProcessor).GetMethod("GetHelp", BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, "help", "Shows a basic help guide for Quantum Console.");
        //}

        //static void AddManualHelp()
        //{
        //    var method = typeof(QuantumConsoleProcessor).GetMethod("ManualHelp", BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, new string[] { "man", "manual" });
        //}

        //static void AddHelpCommand()
        //{
        //    var method = typeof(QuantumConsoleProcessor).GetMethod("GenerateCommandManual", BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, new string[] { "help", "man", "manual" });
        //}

        //static void AddCommandList()
        //{
        //    var method = typeof(QuantumConsoleProcessor).GetMethod("GenerateCommandList", BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, "commands", "Shows a basic help guide for Quantum Console.");
        //}

        //static void AddClearCommand()
        //{
        //    var method = typeof(BasicCommands).GetMethod(nameof(ClearDelegate), BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, "clear", "Clears the console window, full log stays in your persistent game folder.");
        //}

        [Command("clear", "Clear all text from console.")]
        static void ClearDelegate()
            => QuantumConsole.Instance.ClearConsole();

        //static void AddChangeLogLevelCommand()
        //{
        //    var method = typeof(BasicCommands).GetMethod(nameof(ChangeLogLevelDelegate), BindingFlags.Static | BindingFlags.NonPublic);

        //    CommandExtensions.AddStaticCommand(method, "SetLogLevel", "Sets the loglevel. (Optional) Can set your loglevel settings permanent.");
        //}

        [Command("setLogLevel", "Sets the loglevel. (Optional) Can set your loglevel settings permanent.", MonoTargetType.Single)]
        static void ChangeLogLevelDelegate(LoggingLevel logLevel, bool saveToSettings = false)
            => QuantumConsoleMod.Instance.ChangeLogLevel(logLevel, saveToSettings);
    }
}
