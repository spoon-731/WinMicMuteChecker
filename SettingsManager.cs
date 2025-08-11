using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Keys = System.Windows.Forms.Keys;

namespace WinMicMuteChecker
{
    internal class SettingsManager
    {
        // define special Keys
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // define settings
        public static string Color { get; set; } = "White";
        public static double Opacity { get; set; } = 1;
        public static string Position { get; set; } = "Top";
        public static uint Modifier { get; set; } = MOD_WIN | MOD_SHIFT;
        public static Keys Hotkey { get; set; } = Keys.A;

        // settins file path
        private static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.config");

        // load settings from xml file
        // - if it does not exists it generates it with default values
        public static void LoadSettings()
        {
            if (!File.Exists(configPath))
            {
                SaveSettings(); // create file with default values
                return;
            }

            try
            {
                XmlDocument doc = new();
                doc.Load(configPath);

                XmlNode root = doc.SelectSingleNode("/settings");

                // Color
                if (root["color"].InnerText != null)
                    Color = root["color"].InnerText;

                // Opacity
                if (double.TryParse(root["opacity"].InnerText, out var parsedOpacity))
                    Opacity = parsedOpacity;

                // Position
                if (root["position"].InnerText != null)
                    Position = root["position"].InnerText;

                // Modifier
                if (uint.TryParse(root["modifier"].InnerText, out var parsedModifier))
                    Modifier = parsedModifier;

                // Hotkey
                if (Enum.TryParse<Keys>(root["hotkey"].InnerText, out var parsedKey))
                    Hotkey = parsedKey;
            }
            catch (Exception ex)
            {
                // Fallback in case of error during load
                Console.WriteLine("Errore nel caricamento delle impostazioni: " + ex.Message);
                SaveSettings(); // reset default values
            }
        }

        // saves settings in xml file
        public static void SaveSettings()
        {
            try
            {
                XmlDocument doc = new();

                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(xmlDeclaration);

                XmlElement root = doc.CreateElement("settings");
                doc.AppendChild(root);

                root.AppendChild(CreateElement(doc, "color", Color));
                root.AppendChild(CreateElement(doc, "opacity", Opacity.ToString()));
                root.AppendChild(CreateElement(doc, "position", Position));
                root.AppendChild(CreateElement(doc, "modifier", Modifier.ToString()));
                root.AppendChild(CreateElement(doc, "hotkey", Hotkey.ToString()));

                doc.Save(configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore nel salvataggio delle impostazioni: " + ex.Message);
            }
        }

        // helper xml settings property
        private static XmlElement CreateElement(XmlDocument doc, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value;
            return element;
        }

        // Create HotkeyCombination from settings
        public static HotkeyCombination LoadHotkeyCombination()
        {
            var keys = new List<Keys>();

            if ((Modifier & MOD_WIN) != 0) keys.Add(Keys.LWin);
            if ((Modifier & MOD_ALT) != 0) keys.Add(Keys.Menu);
            if ((Modifier & MOD_CONTROL) != 0) keys.Add(Keys.ControlKey);
            if ((Modifier & MOD_SHIFT) != 0) keys.Add(Keys.ShiftKey);

            keys.Add(Hotkey);

            return new HotkeyCombination([.. keys]);
        }
    }
}
