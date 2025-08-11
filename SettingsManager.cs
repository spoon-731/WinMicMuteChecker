using System;
using System.IO;
using System.Text.Json;
using Keys = System.Windows.Forms.Keys;

namespace WinMicMuteChecker
{
    public static class SettingsManager
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public static string Color { get; set; } = "White";
        public static string Position { get; set; } = "Top";
        public static double Opacity { get; set; } = 1; // 0..1
        public static uint Modifier { get; set; } = MOD_WIN | MOD_SHIFT;
        public static Keys Hotkey { get; set; } = Keys.A;

        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinMicMuteChecker");
        private static readonly string JsonPath = Path.Combine(AppDir, "settings.json");

        private class SettingsData
        {
            public string? Color { get; set; }
            public string? Position { get; set; }
            public double Opacity { get; set; }
            public uint Modifier { get; set; }
            public int Hotkey { get; set; }
        }

        public static void LoadSettings()
        {
            try
            {
                Directory.CreateDirectory(AppDir);

                if (!File.Exists(JsonPath))
                {
                    SaveSettings();
                    return;
                }

                var json = File.ReadAllText(JsonPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.Color)) Color = data.Color!;
                    if (!string.IsNullOrWhiteSpace(data.Position)) Position = data.Position!;
                    if (data.Opacity > 0 && data.Opacity <= 1) Opacity = data.Opacity;
                    if (data.Modifier != 0) Modifier = data.Modifier;
                    if (data.Hotkey != 0) Hotkey = (Keys)data.Hotkey;
                }
            }
            catch
            {
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(AppDir);
                var data = new SettingsData
                {
                    Color = Color,
                    Position = Position,
                    Opacity = Opacity,
                    Modifier = Modifier,
                    Hotkey = (int)Hotkey
                };

                JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
                JsonSerializerOptions options = jsonSerializerOptions;
                var json = JsonSerializer.Serialize(
                    data,
                    options: options
                );

                File.WriteAllText(JsonPath, json);
            }
            catch
            {
            }
        }

        public static void SaveHotkeyCombination(HotkeyCombination combo)
        {
            Modifier = 0;
            foreach (var k in combo.CombinationKeys)
            {
                switch (k)
                {
                    case Keys.LWin:
                    case Keys.RWin:
                        Modifier |= MOD_WIN; break;

                    case Keys.ControlKey:
                        Modifier |= MOD_CONTROL; break;

                    case Keys.Menu:
                        Modifier |= MOD_ALT; break;

                    case Keys.ShiftKey:
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        Modifier |= MOD_SHIFT; break;

                    default:
                        Hotkey = k; break;
                }
            }

            SaveSettings();
        }

        public static HotkeyCombination LoadHotkeyCombination()
        {
            var list = new System.Collections.Generic.List<Keys>();
            if ((Modifier & MOD_WIN) != 0) list.Add(Keys.LWin);
            if ((Modifier & MOD_CONTROL) != 0) list.Add(Keys.ControlKey);
            if ((Modifier & MOD_ALT) != 0) list.Add(Keys.Menu);
            if ((Modifier & MOD_SHIFT) != 0) list.Add(Keys.ShiftKey);

            list.Add(Hotkey);
            return new HotkeyCombination([.. list]);
        }
    }
}
