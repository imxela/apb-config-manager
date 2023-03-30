using Newtonsoft.Json;
using System.Diagnostics;

namespace APBConfigManager
{
    public class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException() { }
        public ProfileNotFoundException(string message) : base(message) { }
        public ProfileNotFoundException(string message, Exception inner) : base(message, inner) { }
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

            // If no symlink exists this is likely a first-time run, so we
            // backup the current configuration to a backup profile before
            // creating one.

            if (Directory.Exists(AppConfig.GameConfigDirPath) && 
                !IsSymlink(AppConfig.GameConfigDirPath))
            {
                CopyGameConfigToProfile(GetProfilesByName("Backup")[0].id);

                Profile? baProfile = null;

                List<Profile> backupProfiles = new List<Profile>();
                foreach (var backupProfile in backupProfiles)
                {
                    if (backupProfile.readOnly)
                    {
                        baProfile = backupProfile;
                    }
                }

                // Backup profile not found - create it
                if (baProfile == null)
                {
                    baProfile = CreateProfile("Backup", string.Empty, true);
                }

                CopyGameConfigToProfile(baProfile.id);

                Directory.Delete(AppConfig.GameConfigDirPath, true);
            }
        }

        public Profile CreateProfile(string name, string gameArguments = "", bool readOnly = false)
        {
            Guid id = Guid.NewGuid();

            Directory.CreateDirectory(AppConfig.ProfileConfigDirPath + id.ToString());

            Profile profile = new Profile(id, name, gameArguments, readOnly);
            _profiles.Add(profile);

            SyncJsonDatabase();

            return profile;
        }

        /// <summary>
        /// Returns false if the specified profile does not exist.
        /// </summary>
        public bool DeleteProfile(Guid profileId)
        {
            Profile? match = GetProfileById(profileId);
            if (match == null) return false;

            _profiles.Remove(match);

            if (!Directory.Exists(AppConfig.ProfileConfigDirPath + profileId.ToString()))
                return false;

            Directory.Delete(AppConfig.ProfileConfigDirPath + profileId, true);

            SyncJsonDatabase();

            return true;
        }

        /// <summary>
        /// Returns false if the specified profile does not exist.
        /// </summary>
        public bool UpdateProfile(Profile newProfileData)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].id == newProfileData.id)
                {
                    _profiles[i] = newProfileData;
                    SyncJsonDatabase();
                    return true;
                }
            }

            return false;
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

            throw new ProfileNotFoundException($"The specified GUID ({id}) does not match any existing profiles.");
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
            string json = File.OpenText(AppConfig.ProfileConfigFilepath).ReadToEnd();

            return JsonConvert.DeserializeObject<List<Profile>>(json) ??
                new List<Profile>();
        }

        public void CopyGameConfigToProfile(Guid profileId)
        {
            string[] files = Directory.GetFiles(AppConfig.GameConfigDirPath, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string relativeFilePath = Path.GetRelativePath(AppConfig.GameConfigDirPath, file);
                string profileFilePath = AppConfig.ProfileConfigDirPath + profileId.ToString() + "/" + relativeFilePath;

                new FileInfo(profileFilePath).Directory?.Create();

                File.Copy(file, profileFilePath, true);
            }
        }

        /// <summary>
        /// Activates the specified profile. Works by creating a symlink
        /// from the APB config directory (APBGame/Config) to the config
        /// directory of the specified config profile.
        /// </summary>
        public void ActivateProfile(Guid profileId)
        {
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
        public bool CreateDesktopShortcutForProfile(Profile profile, string shortcutFilepath)
        {
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
            shortcut.Arguments = profile.id.ToString();
            shortcut.Save();

            return true;
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