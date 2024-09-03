using Assets.ldgKrey.QuantumConsole.Script;
using C3;
using LDGKrey.QCEnabler.Configuration;
using QFSW.QC;
using QFSW.QC.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unfoundry;
using UnityEngine;

namespace LDGKrey.QCEnabler
{
    [AddSystemToMainMenu]
    public class QCEnablerBoot : SystemManager.System
    {

        public override void OnAddedToWorld()
        {
            new QuantumConsoleMod();

            base.OnAddedToWorld();
        }
    }

    public class QuantumConsoleMod : IEscapeCloseable
    {
        public static QuantumConsoleMod Instance { get; private set; }
        public static LogSource log = new("QuantumConsoleMod");

        QuantumConsole console;
        public QuantumKeyConfig keyConfig;
        DynamicCanvasScaler zoomController;
        RectTransform consoleRect;

        public QuantumConsoleMod()
        {
            if (Instance != null)
                return;

            Instance = this;
            InstantiateConsole();
        }

        void InstantiateConsole()
        {
            log.Log("Try instantiate console");

            var prefab = AssetManager.Database.LoadAssetAtPath<GameObject>(ConstAssetsPaths.QuantumConsole);

            if (prefab == null)
            {
                log.LogError("instantiating console failed.");
                return;
            }

            var instance = GameObject.Instantiate(prefab);

            if (!instance.TryGetComponent<QuantumConsole>(out console))
            {
                log.LogError("Quantum Console (component) not found on instance.");
                return;
            }

            consoleRect = typeof(QuantumConsole).GetField("_containerRect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(console) as RectTransform;

            if (!instance.TryGetComponent<Canvas>(out var canvas))
            {
                log.LogError("Canvas (component) on console instance not found");
                return;
            }

            //console over all UI
            canvas.sortingOrder = 9999;

            //get the logs before the console was initiated
            var logs = FilteredLog();

            //insert the logs into te console window
            foreach (var log in logs)
            {
                console.LogToConsole(log);
            }

            //Key configs
            ConsoleConfig.HotKeys.ToggleKey.onValueChanged += OnKeyConfigChanged;
            keyConfig = typeof(QuantumConsole).GetField("_keyConfig", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(console) as QuantumKeyConfig;
            if (keyConfig == null)
                log.LogWarning("cant find keyConfig per reflection");
            if (keyConfig != null && keyConfig.ToggleConsoleVisibilityKey.Key != ConsoleConfig.HotKeys.ToggleKey.value)
                OnKeyConfigChanged(ConsoleConfig.HotKeys.ToggleKey.value);

            //Commands
            BasicCommands.AddCommands();

            //UI / Zoom
            if (!console.TryGetComponent<DynamicCanvasScaler>(out zoomController))
            {
                log.LogWarning("cant find zoomUIController");
            }
            else
            {
                ConsoleConfig.Zoom.ZoomLevel.onValueChanged += ZoomConfigChanged;
            }

            //UI / Positioning
            ConsoleConfig.Positioning.ConsolePositionX.onValueChanged += OnConsolePositionChange;
            ConsoleConfig.Positioning.ConsolePositionY.onValueChanged += OnConsolePositionChange;

            //LogLevel
            if (ConsoleConfig.LogLevel.LoggingLevel.value != LoggingLevel.Full)
                ChangeLogLevel(ConsoleConfig.LogLevel.LoggingLevel.value, false);

            ConsoleConfig.LogLevel.LoggingLevel.onValueChanged += OnLogLevelSettingChanged;

            //Cursor lock/unlock
            console.OnActivate += OnActivate;
            console.OnDeactivate += OnDeactivate;
        }

        bool settingsChanged = false;

        #region LogLevel

        public void ChangeLogLevel(LoggingLevel newLogLevel, bool setSettings = true)
        {
            if (QuantumConsoleProcessor.loggingLevel == newLogLevel)
                return;

            log.Log($"change log level to {newLogLevel}");

            if (setSettings)
            {
                skipLogLevelchange = true;
                ConsoleConfig.LogLevel.LoggingLevel.value = newLogLevel;
                ConsoleConfig.LogLevel.LoggingLevel.save();
            }

            QuantumConsoleProcessor.loggingLevel = newLogLevel;
        }

        bool skipLogLevelchange = false;
        void OnLogLevelSettingChanged(LoggingLevel newValue)
        {
            if (skipLogLevelchange)
            {
                skipLogLevelchange = false;
                return;
            }

            ChangeLogLevel(newValue);
        }

        #endregion

        #region Zoom
        const int zoomLevelMin = 10;
        const int zoomLevelMax = 200;
        bool zoomLevelNotificationSkip = false;
        void ZoomConfigChanged(int newValue)
        {
            if (zoomLevelNotificationSkip)
            {
                zoomLevelNotificationSkip = false;
                return;
            }

            if (!ConsoleConfig.Zoom.RememberZoomLevel.value)
                return;

            if (newValue < zoomLevelMin)
            {
                ConsoleConfig.Zoom.ZoomLevel.value = zoomLevelMin;
                return;
            }

            if (newValue > zoomLevelMax)
            {
                ConsoleConfig.Zoom.ZoomLevel.value = zoomLevelMax;
                return;
            }

            zoomController.ZoomMagnification = (float)newValue / 100;
        }

        public void OnZoomButtonPressed()
        {
            zoomLevelNotificationSkip = true;

            settingsChanged = true;
            ConsoleConfig.Zoom.ZoomLevel.value = Mathf.CeilToInt(zoomController.ZoomMagnification * 100);
        }

        #endregion

        #region Position
        bool skipOnConsolePositionChange = false;
        MethodInfo dynamicScalerUpdate = typeof(DynamicCanvasScaler).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        void OnConsolePositionChange(int newValue)
        {
            if (skipOnConsolePositionChange)
            {
                skipOnConsolePositionChange = false;
                return;
            }

            if (!ConsoleConfig.Positioning.RememberPosition.value)
                return;

            var position = new Vector2(
                ConsoleConfig.Positioning.ConsolePositionX.value,
                ConsoleConfig.Positioning.ConsolePositionY.value);

            dynamicScalerUpdate.Invoke(zoomController, null);
            consoleRect.anchoredPosition = position / zoomController.ZoomMagnification;
        }

        void PreSavePosition()
        {
            if (!ConsoleConfig.Positioning.RememberPosition.value)
                return;

            var currentPosition = consoleRect.anchoredPosition * zoomController.ZoomMagnification;
            if (currentPosition == Vector2.zero)
                return;

            skipOnConsolePositionChange = true;
            ConsoleConfig.Positioning.ConsolePositionX = (int)currentPosition.x;
            ConsoleConfig.Positioning.ConsolePositionY = (int)currentPosition.y;
            settingsChanged = true;
        }

        #endregion

        #region StateChange
        private void OnDeactivate()
        {
            if (GlobalStateManager.getCurrentGameState() == GlobalStateManager.GameState.Game)
            {
                GlobalStateManager.removeCursorRequirement();
            }

            PreSavePosition();

            if (settingsChanged)
            {
                log.Log("Save changed settings");

                if (ConsoleConfig.Positioning.RememberPosition.value)
                {
                    ConsoleConfig.Positioning.ConsolePositionX.save();
                    ConsoleConfig.Positioning.ConsolePositionY.save();
                }
                if (ConsoleConfig.Zoom.RememberZoomLevel.value)
                {
                    ConsoleConfig.Zoom.ZoomLevel.save();
                }

                settingsChanged = false;
            }

            iec_registerFrameClosing();
        }

        private void OnActivate()
        {
            if (GlobalStateManager.getCurrentGameState() == GlobalStateManager.GameState.Game)
            {
                GlobalStateManager.addCursorRequirement();
            }

            if (ConsoleConfig.Zoom.ZoomLevel.value != 100)
                ZoomConfigChanged(ConsoleConfig.Zoom.ZoomLevel.value);

            if (ConsoleConfig.Positioning.ConsolePositionX.value +
                ConsoleConfig.Positioning.ConsolePositionY
                != 0)
                //zero because it is redundant
                OnConsolePositionChange(0);

            iec_registerFrameOpening();
        }

        public void iec_registerFrameOpening()
        {
            GlobalStateManager.registerEscapeCloseable(this);
        }

        public void iec_registerFrameClosing()
        {
            GlobalStateManager.deRegisterEscapeCloseable(this);
        }

        public void iec_triggerFrameClose()
        {
            console.Deactivate();
        }
        #endregion

        #region Keys
        void OnKeyConfigChanged(KeyCode newKey)
        {
            if (keyConfig == null)
                return;

            log.Log($"toggle key changed to {newKey}");

            keyConfig.ToggleConsoleVisibilityKey = new ModifierKeyCombo { Key = newKey };
        }
        #endregion

        #region Helper
        public static string[] Filter = new string[]
        {
                "Can't find custom attr",
                "Fallback handler",
        };
        public static List<string> FilteredLog()
        {
            var path = Path.Combine(Application.persistentDataPath, "Player.log");
            var logMessages = new List<string>();
            bool enableLogging = false;

            //unity writes while this code reads. The file is not locked.
            //https://stackoverflow.com/questions/9759697/reading-a-file-used-by-another-process its basically the same case.
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("Loading Mods from:"))
                    {
                        enableLogging = true;
                    }

                    if (!enableLogging)
                        continue;

                    if (Filter.Any(x => line.StartsWith(x)))
                        continue;

                    logMessages.Add(line);
                }
            }

            return logMessages;
        }
        #endregion
    }
}
