using System;
using System.Drawing;
using System.Xml.Serialization;

namespace EnHac.Models
{
    [Serializable]
    public class AppSettings
    {
        public ThemeType Theme { get; set; } = ThemeType.Dark;
        public int PrimaryColorArgb { get; set; } = Color.FromArgb(67, 97, 238).ToArgb();
        public int SecondaryColorArgb { get; set; } = Color.FromArgb(76, 201, 240).ToArgb();
        public string Language { get; set; } = "English";
        public bool ClearClipboardOnExit { get; set; } = true;
        public int PasswordCacheTimeout { get; set; } = 300;
        public bool ShowPasswordStrength { get; set; } = true;
        public string DefaultOutputFolder { get; set; } = "";
        public bool OverwriteWithoutPrompt { get; set; } = false;
        public int MaxFileSizeMB { get; set; } = 2048;
        public bool EnableLogging { get; set; } = true;
        public string LogFilePath { get; set; } = "encryption_log.txt";

        [XmlIgnore]
        public Color PrimaryColor
        {
            get => Color.FromArgb(PrimaryColorArgb);
            set => PrimaryColorArgb = value.ToArgb();
        }

        [XmlIgnore]
        public Color SecondaryColor
        {
            get => Color.FromArgb(SecondaryColorArgb);
            set => SecondaryColorArgb = value.ToArgb();
        }
    }

    public enum ThemeType
    {
        Dark,
        Light,
        Custom
    }
}