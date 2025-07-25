using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using Keys = System.Windows.Forms.Keys;
using System.Xml;

namespace WinMicMuteChecker
{
    public static class SettingsManager
    {
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public static string Color { get; set; } = "White";
        public static string Position { get; set; } = "TopLeft";
        public static double Opacity { get; set; } = 1.0;
        public static Keys Hotkey { get; set; } = Keys.A;
        public static uint Modifier { get; set; } = MOD_SHIFT | MOD_WIN;
        public static bool RunAtStartup { get; set; } = false;

        private static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.config");
        private static readonly string startupKey = "WinMicMuteChecker";

        public static void LoadSettings()
        {
            if (!File.Exists(configPath))
            {
                SaveSettings(); // crea il file se non esiste
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(configPath);

                XmlNode root = doc.SelectSingleNode("/settings");

                Color = root["color"]?.InnerText ?? "White";
                Position = root["position"]?.InnerText ?? "TopLeft";

                if (double.TryParse(root["opacity"]?.InnerText, out var op))
                    Opacity = op;

                if (Enum.TryParse<Keys>(root["hotkey"]?.InnerText, out var key))
                    Hotkey = key;

                if (uint.TryParse(root["modifier"]?.InnerText, out var mod))
                    Modifier = mod;

                RunAtStartup = root["runatstartup"]?.InnerText.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
            }
            catch (Exception ex)
            {
                // Fallback in caso di errore XML
                Console.WriteLine("Errore nel caricamento delle impostazioni: " + ex.Message);
                SaveSettings(); // ripristina valori predefiniti
            }

            UpdateStartupRegistration();
        }

        public static void SaveSettings()
        {
            try
            {
                XmlDocument doc = new XmlDocument();

                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(xmlDeclaration);

                XmlElement root = doc.CreateElement("settings");
                doc.AppendChild(root);

                root.AppendChild(CreateElement(doc, "color", Color));
                root.AppendChild(CreateElement(doc, "position", Position));
                root.AppendChild(CreateElement(doc, "opacity", Opacity.ToString()));
                root.AppendChild(CreateElement(doc, "hotkey", Hotkey.ToString()));
                root.AppendChild(CreateElement(doc, "modifier", Modifier.ToString()));
                root.AppendChild(CreateElement(doc, "runatstartup", RunAtStartup.ToString()));

                doc.Save(configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore nel salvataggio delle impostazioni: " + ex.Message);
            }

            UpdateStartupRegistration();
        }

        private static XmlElement CreateElement(XmlDocument doc, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value;
            return element;
        }

        private static void UpdateStartupRegistration()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (rk == null) return;

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (RunAtStartup)
            {
                rk.SetValue(startupKey, $"\"{exePath}\"");
            }
            else
            {
                rk.DeleteValue(startupKey, false);
            }

            rk.Dispose();
        }
    }
}
