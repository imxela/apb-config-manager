using Newtonsoft.Json;
using System.Reflection;

namespace APBConfigManager
{
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
        public string? _gamePath;

        [JsonProperty]
        public string? _activeProfile;

        public static string GamePath
        {
            get
            {
                return Instance._gamePath ?? string.Empty;
            }

            set
            {
                Instance._gamePath = value;
                Instance.Save();
            }
        }

        public static string GameRelativeExePath
        {
            get
            {
                return "/Binaries/APB.exe";
            }
        }

        public static string GameExecutableFilepath
        {
            get
            {
                return GamePath + GameRelativeExePath;
            }
        }

        public static string AdvLauncherExecutablePath
        {
            get
            {
                return GamePath + "/Advanced APB Launcher.exe";
            }
        }

        public static string ActiveProfile
        {
            get
            {
                return Instance._activeProfile ??
                    throw new NullReferenceException("Attempted to retrieve active profile before it was set!");
            }

            set
            {
                Instance._activeProfile = value;
                Instance.Save();
            }
        }

        public static string AppRootDir
        {
            get
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(assemblyPath) ?? 
                    throw new DirectoryNotFoundException("Could not locate assembly directory");
            }
        }

        public static string AppConfigFilepath
        {
            get
            {
                return AppRootDir + "/config.json";
            }
        }

        public static string GameConfigDirPath
        {
            get
            {
                return GamePath + "/APBGame/Config/";
            }
        }

        public static string ProfileConfigDirPath
        {
            get
            {
                return AppRootDir + "/Profiles/";
            }
        }

        public static string ProfileConfigFilepath
        {
            get
            {
                return ProfileConfigDirPath + "profiles.json";
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
                _activeProfile = null
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
                // Fails with FileNotFoundException if config file does not exist
                StreamReader jsonFile = File.OpenText(AppConfigFilepath);
                string jsonData = jsonFile.ReadToEnd();
                jsonFile.Close();

                // Should only fail if the above does, which means this
                // exception should never be triggered
                return JsonConvert.DeserializeObject<AppConfig>(jsonData) ??
                    throw new NullReferenceException("Failed to deserialize JSON config");
            }
            catch (FileNotFoundException)
            {
                return Default();
            }
        }

        /// <summary>
        /// Writes the config values to the config file on disk.
        /// </summary>
        public void Save()
        {
            string serial = JsonConvert.SerializeObject(this);
            File.WriteAllText(AppConfigFilepath, serial);
        }
    }
}
