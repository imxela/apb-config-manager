using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace APBConfigManager
{
    public class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException() { }
        public ProfileNotFoundException(string message) : base(message) { }
        public ProfileNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidGamePathException : Exception
    {
        public InvalidGamePathException() { }
        public InvalidGamePathException(string message) : base(message) { }
        public InvalidGamePathException(string message, Exception inner) : base(message, inner) { }
    }

    public class ProfileManager
    {
        private static ProfileManager? _instance;

        public static ProfileManager Instance
        {
            get
            {
                return _instance ??= new ProfileManager();
            }
        }

        private List<Profile> _profiles;

        public List<Profile> Profiles
        {
            get { return _profiles; }
        }

        private ProfileManager()
        {
            _profiles = ReadProfilesFromDisk();
        }

        /// <summary>
        /// Creates a new profile with the specified parameters. If a profile
        /// with the given name already exists no new profile is created and
        /// the preeexisting one is returned.
        /// </summary>
        public Profile CreateProfile(string name, string gameArguments = "", bool readOnly = false)
        {
            if (GetProfilesByName(name).Count > 0)
                return GetProfilesByName(name)[0];

            Guid id = Guid.NewGuid();

            Directory.CreateDirectory(AppConfig.ProfileConfigDirPath + id.ToString());

            Profile profile = new Profile(id, name, gameArguments, readOnly);
            _profiles.Add(profile);

            SyncJsonDatabase();

            return profile;
        }

        /// <summary>
        /// Creates a new configuration profile and imports configuration
        /// files from a pre-existing APB: Reloaded install.
        /// </summary>
        /// <param name="path">
        /// Path to the APB: Reloaded installation directory to copy the
        /// configuration files from.
        /// </param>
        /// <param name="delete">
        /// If true, the specified APB: Reloaded install will be permanently
        /// deleted after  the configuration files have been copied to
        /// the specified profile.
        /// </param>
        /// <returns>The newly imported profile</returns>
        /// <exception cref="InvalidGamePathException">
        /// Throws InvalidGamePathException if the specified path does not
        /// lead to a valid APB: Reloaded installation directory.
        /// </exception>
        public Profile ImportProfile(string path, bool delete = false)
        {
            Profile profile = CreateProfile("Imported Profile");

            CopyGameConfigToProfile(profile.id, path);

            if (delete)
                Directory.Delete(path, true);

            return profile;
        }

        /// <param name="profileId"></param>
        /// <exception cref="DirectoryNotFoundException">
        /// Throws DirectoryNotFoundException if the config directory
        /// for the profile corresponding with the specified GUID
        /// does not exist.
        /// </exception>
        public void DeleteProfile(Guid profileId)
        {
            Profile profile = GetProfileById(profileId);
            _profiles.Remove(profile);

            if (!Directory.Exists(AppConfig.ProfileConfigDirPath + profileId.ToString()))
            {
                throw new DirectoryNotFoundException(
                    "Could not create shortcut because the directory for " +
                    "the specified profile GUID is missing");
            }

            Directory.Delete(AppConfig.ProfileConfigDirPath + profileId, true);

            SyncJsonDatabase();
        }

        /// <exception cref="ProfileNotFoundException">
        /// Throws ProfileNotFoundException if the specified GUID does not
        /// correspond to an existing profile.
        /// </exception>
        public void UpdateProfile(Profile newProfileData)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].id == newProfileData.id)
                {
                    _profiles[i] = newProfileData;
                    SyncJsonDatabase();

                    return;
                }
            }

            throw new ProfileNotFoundException(
                "Could not delete profile because no profile corresponding to" +
                "the specified GUID exists. ");
        }

        /// <summary>
        /// Returns the profile matching the specified ID.
        /// Throws a KeyNotFoundException if no profile with the specified ID exists.
        /// </summary>
        public Profile GetProfileById(Guid id)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].id == id)
                {
                    return _profiles[i];
                }
            }

            throw new ProfileNotFoundException(
                $"Could not retrieve profile because no profile " +
                $"corresponding with the specified GUID exists.");
        }

        /// <summary>
        /// Returns the directory corresponding to the profile with the 
        /// given GUID.
        /// </summary>
        public string GetProfileDirById(Guid profileId)
        {
            if (!DoesProfileWithIdExist(profileId))
                throw new ProfileNotFoundException(
                    "The specified GUID does not correspond with an " +
                    "existing profile.");

            return AppConfig.ProfileConfigDirPath + profileId.ToString();
        }

        /// <summary>
        /// Returns true if a profile with the given ID exists
        /// </summary>
        public bool DoesProfileWithIdExist(Guid id)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].id == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of profiles with the specified name.
        /// </summary>
        public List<Profile> GetProfilesByName(string name)
        {
            List<Profile> matches = new List<Profile>();

            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].name == name)
                {
                    matches.Add(_profiles[i]);
                }
            }

            return matches;
        }

        /// <summary>
        /// Activates the specified profile and runs the APB: Reloaded 
        /// executable with the arguments specified in the profile.
        /// </summary>
        public void RunGameWithProfile(Guid profileId)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = AppConfig.GameExecutableFilepath;
            startInfo.WorkingDirectory = AppConfig.GamePath;
            
            startInfo.Arguments = GetProfileById(profileId)?.gameArgs ?? string.Empty;

            ActivateProfile(profileId);

            Process.Start(startInfo);
        }

        private void SyncJsonDatabase()
        {
            string jsonData = JsonConvert.SerializeObject(_profiles);
            File.WriteAllText(AppConfig.ProfileConfigFilepath, jsonData);
        }

        private List<Profile> ReadProfilesFromDisk()
        {
            Directory.CreateDirectory(AppConfig.ProfileConfigDirPath);
            FileStream stream = File.Open(AppConfig.ProfileConfigFilepath, FileMode.OpenOrCreate);

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            stream.Close();

            if (bytes.Length == 0)
                return new List<Profile>();

            string json = System.Text.Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<List<Profile>>(json) ??
                new List<Profile>();
        }

        /// <exception cref="ProfileNotFoundException">
        /// Throws ProfileNotFoundException if the specified GUID does not
        /// correspond to an existing profile.
        /// </exception>
        /// <exception cref="InvalidGamePathException">
        /// Throws InvalidGamePathException if the specified path does not
        /// lead to a valid APB: Reloaded installation directory.
        /// </exception>
        public void CopyGameConfigToProfile(Guid profileId, string? path = null)
        {
            if (path == null)
                path = AppConfig.GamePath;

            if (!IsValidGamePath(path))
            {
                throw new InvalidGamePathException(
                    "The specified path does not lead to a valid " +
                    "APB: Reloaded installation directory");
            }

            if (!DoesProfileWithIdExist(profileId))
            {
                throw new ProfileNotFoundException(
                    "Can not copy game configuration to profile because" +
                    "no profile with a corresponding GUID exists.");
            }

            string configDirPath = path + AppConfig.RelativeGameConfigDirPath;
            string[] files = Directory.GetFiles(configDirPath, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string relativeFilePath = Path.GetRelativePath(configDirPath, file);
                string profileFilePath = GetProfileDirById(profileId) + "\\" + relativeFilePath;

                new FileInfo(profileFilePath).Directory?.Create();

                File.Copy(file, profileFilePath, true);
            }
        }

        /// <summary>
        /// Activates the specified profile. Works by creating a symlink
        /// from the APB config directory (APBGame/Config) to the config
        /// directory of the specified config profile.
        /// </summary>
        /// <exception cref="ProfileNotFoundException">
        /// Throws ProfileNotFoundException if the specified GUID does not
        /// correspond to an existing profile.
        /// </exception>
        public void ActivateProfile(Guid profileId)
        {
            if (!DoesProfileWithIdExist(profileId))
            {
                throw new ProfileNotFoundException(
                    "Can not activate profile with the specified GUID " +
                    "because no profile with a corrsponding " +
                    "GUID exists.");
            }

            // Delete previous symlink if one exists
            if (IsSymlink(AppConfig.GameConfigDirPath))
                Directory.Delete(AppConfig.GameConfigDirPath);

            Directory.CreateSymbolicLink(AppConfig.GameConfigDirPath, AppConfig.ProfileConfigDirPath + profileId.ToString());

            AppConfig.ActiveProfile = profileId.ToString();
        }

        /// <summary>
        /// Will recreate the symlink if a shortcut to the specified profile
        /// already exists in the given location. Returns false if the 
        /// shortcut could not be created.
        /// </summary>
        /// <exception cref="ProfileNotFoundException">
        /// Throws ProfileNotFoundException if the specified GUID does not
        /// correspond to an existing profile.
        /// </exception>
        public bool CreateDesktopShortcutForProfile(Guid profileId, string shortcutFilepath)
        {
            if (!DoesProfileWithIdExist(profileId))
            {
                throw new ProfileNotFoundException(
                    "Can not create profile shortcut with the specified GUID " +
                    "because no profile with a corrsponding GUID exists.");
            }

            if (File.Exists(shortcutFilepath))
                File.Delete(shortcutFilepath);

            ProcessModule? procMod = Process.GetCurrentProcess().MainModule;
            if (procMod == null || procMod.FileName == null)
                return false;

            string appExePath = procMod.FileName;
            string targetPath = appExePath;

            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut? shortcut =
                shell.CreateShortcut(shortcutFilepath) as IWshRuntimeLibrary.IWshShortcut;

            if (shortcut == null)
                return false;

            shortcut.TargetPath = targetPath;
            shortcut.Arguments = profileId.ToString();
            shortcut.Save();

            return true;
        }


        /// <summary>
        /// Returns true of the specified file is a valid path to an APB
        /// install directory by checking if the game executable exists.
        /// </summary>
        public static bool IsValidGamePath(string path)
        {
            return File.Exists(path + AppConfig.GameRelativeExePath);
        }

        /// <summary>
        /// Returns true if the specified file is a symbolic link.
        /// Will return false if the file does not exist.
        /// </summary>
        private bool IsSymlink(string filepath)
        {
            FileInfo info = new FileInfo(filepath);
            return info.LinkTarget != null;
        }
    }
}