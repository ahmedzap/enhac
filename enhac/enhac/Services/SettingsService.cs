using System;
using System.IO;
using System.Xml.Serialization;
using EnHac.Models;

namespace EnHac.Services
{
    public class SettingsService
    {
        private static readonly object _lock = new object();
        private static SettingsService _instance;
        private static readonly string SettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        public static SettingsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SettingsService();
                    }
                }
                return _instance;
            }
        }

        public AppSettings AppSettings { get; private set; }
        public EncryptionSettings EncryptionSettings { get; private set; }

        private SettingsService()
        {
            AppSettings = new AppSettings();
            EncryptionSettings = new EncryptionSettings();
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    using var reader = new StreamReader(SettingsPath);
                    var serializer = new XmlSerializer(typeof(SettingsData));
                    if (serializer.Deserialize(reader) is SettingsData data)
                    {
                        AppSettings = data.AppSettings ?? new AppSettings();
                        EncryptionSettings = data.EncryptionSettings ?? new EncryptionSettings();
                    }
                }
                else
                {
                    Save();
                }
            }
            catch
            {
                AppSettings = new AppSettings();
                EncryptionSettings = new EncryptionSettings();
            }
        }

        public void Save()
        {
            try
            {
                var data = new SettingsData
                {
                    AppSettings = AppSettings,
                    EncryptionSettings = EncryptionSettings
                };

                using var writer = new StreamWriter(SettingsPath);
                var serializer = new XmlSerializer(typeof(SettingsData));
                serializer.Serialize(writer, data);
            }
            catch
            {
                // فشل الحفظ
            }
        }

        public void Reset()
        {
            AppSettings = new AppSettings();
            EncryptionSettings = new EncryptionSettings();
            Save();
        }
    }

    [Serializable]
    public class SettingsData
    {
        public AppSettings AppSettings { get; set; }
        public EncryptionSettings EncryptionSettings { get; set; }
    }
}