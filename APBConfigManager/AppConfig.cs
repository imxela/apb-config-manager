using JetBrains.Annotations;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace APBConfigManager
{
    /// <summary>
    /// Stores application settings and global variables for APBConfigManager.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class AppConfig
    {
        private static AppConfig? _instance;

        /// <summary>
        /// Retrieves the application configuration.
        /// If no config exists a default config is created.
        /// </summary>
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }

                return _instance;
            }
        }

        [JsonProperty]
        public string _gamePath = string.Empty;

        [JsonProperty]
        public Guid _activeProfile = Guid.Empty;

        public const string APP_VERSION = "v2.0.0";

        public static string AppDataPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\APBConfigManager\\";

                FileUtils.CreateDirectoryIfNotExists(path);

                return path;
            }
        }

        public static string RelProfileConfigDir
        {
            get
            {
                return "\\Profiles\\";
            }
        }

        public static string AbsProfileConfigDir
        {
            get
            {
                return AppDataPath + RelProfileConfigDir;
            }
        }

        public static string AppConfigFilepath
        {
            get
            {
                return AppDataPath + "\\config.json";
            }
        }

        public static string ProfileDatabaseFilepath
        {
            get
            {
                return AbsProfileConfigDir + "\\profiles.json";
            }
        }

        public static string RelGameExePath
        {
            get
            {
                return "\\Binaries\\APB.exe";
            }
        }

        public static string AbsGameExeFilepath
        {
            get
            {
                return GamePath + RelGameExePath;
            }
        }

        public static string GamePath
        {
            get
            {
                return Instance._gamePath;
            }

            set
            {
                Instance._gamePath = value;
                Instance.Save();
            }
        }

        public static string AbsAdvLauncherExePath
        {
            get
            {
                return GamePath + "\\Advanced APB Launcher.exe";
            }
        }

        public static string RelGameConfigDir
        {
            get
            {
                return "\\APBGame\\Config";
            }
        }

        public static string AbsGameConfigDir
        {
            get
            {
                return GamePath + RelGameConfigDir;
            }
        }

        public static Guid ActiveProfile
        {
            get
            {
                return Instance._activeProfile;
            }

            set
            {
                Instance._activeProfile = value;
                Instance.Save();
            }
        }

        /// <summary>
        /// Returns the default application config values.
        /// </summary>
        private static AppConfig Default()
        {
            return new AppConfig
            {
                _gamePath = string.Empty,
                _activeProfile = Guid.Empty
            };
        }

        /// <summary>
        /// Attempts to load the application config file from disk.
        /// If no config file exists, it falls back on a default config.
        /// </summary>
        private static AppConfig Load()
        {
            try
            {
                StreamReader jsonFile = File.OpenText(AppConfigFilepath);
                string jsonData = jsonFile.ReadToEnd();
                jsonFile.Close();

                // Use the default config if the config file is empty
                if (jsonData == string.Empty)
                    return Default();

                return JsonConvert.DeserializeObject<AppConfig>(jsonData) ??
                    throw new NullReferenceException("Failed to deserialize JSON config");
            }
            catch (FileNotFoundException)
            {
                // Use default config if the config file doesn't exist
                return Default();
            }
        }

        /// <summary>
        /// Writes the config values to the config file on disk.
        /// </summary>
        public void Save()
        {
            string serial = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(AppConfigFilepath, serial);
        }
    }
}
