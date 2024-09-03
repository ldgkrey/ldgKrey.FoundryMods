using C3.ModKit;
using QFSW.QC;
using UnityEngine;

namespace LDGKrey.QCEnabler.Configuration
{
    public static class ConsoleConfig
    {
        [ModSettingGroup]
        public static class Positioning
        {
            [ModSettingTitle("Position X")]
            [ModSettingDescription("Current saved position of the console on your screen")]
            public static ModSetting<int> ConsolePositionX = 0;
            [ModSettingTitle("Position Y")]
            [ModSettingDescription("Current saved position of the console on your screen")]
            public static ModSetting<int> ConsolePositionY = 0;

            [ModSettingTitle("Remember?")]
            [ModSettingDescription("Set on or off the remember position feature. If off it will ignore the position saved above")]
            public static ModSetting<bool> RememberPosition = false;
        }

        [ModSettingGroup]
        public static class HotKeys
        {
            [ModSettingTitle("Toggle Console Key")]
            [ModSettingDescription("Current saved position of the console on your screen")]
            public static ModSetting<KeyCode> ToggleKey = KeyCode.F11;
        }

        [ModSettingGroup]
        public static class Zoom
        {
            [ModSettingTitle("Zoom")]
            [ModSettingDescription("Current saved zoom level of the console in percent.")]
            public static ModSetting<int> ZoomLevel = 100;

            [ModSettingTitle("Remember?")]
            [ModSettingDescription("Set on or off the remember zoom level feature. If off it will ignore the zoom level saved aboth")]
            public static ModSetting<bool> RememberZoomLevel = false;
        }

        [ModSettingGroup]
        public static class LogLevel
        {
            public static ModSetting<LoggingLevel> LoggingLevel = QFSW.QC.LoggingLevel.Full;
        }
    }
}
