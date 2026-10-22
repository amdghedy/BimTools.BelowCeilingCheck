using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Loads and saves user options so the tool remembers its configuration between sessions.</summary>
    public sealed class SettingsService
    {
        private readonly string _directory;
        private readonly string _path;

        public SettingsService()
        {
            _directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BIM TOOLS",
                "BelowCeilingCheck");
            _path = Path.Combine(_directory, "settings.json");
        }

        public CheckSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    using (FileStream stream = File.OpenRead(_path))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CheckSettings));
                        CheckSettings settings = serializer.ReadObject(stream) as CheckSettings;
                        if (settings != null)
                        {
                            settings.Normalize();
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SerializationException || ex is UnauthorizedAccessException)
            {
                return new CheckSettings();
            }

            return new CheckSettings();
        }

        public bool Save(CheckSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(_directory);
                using (FileStream stream = File.Create(_path))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CheckSettings));
                    serializer.WriteObject(stream, settings);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is SerializationException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
