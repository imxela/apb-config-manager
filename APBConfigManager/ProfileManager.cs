using Newtonsoft.Json;
using System.Diagnostics;

namespace APBConfigManager;

public class ProfileManager
{
    public FullyObservableCollection<Profile> Profiles;

    public Guid ActiveProfile
    {
        get
        {
            return AppConfig.ActiveProfile;
        }
    }

    public ProfileManager()
    {
        Profiles = new FullyObservableCollection<Profile>();

        foreach (Profile profile in LoadProfiles())
        {
            Profiles.Add(profile);
        }

        // Save profiles on change
        Profiles.CollectionChanged += (sender, e) => {
            Debug.WriteLine("Profiles.CollectionChanged fired, saving profiles");
            SaveProfiles();
        };

        CreateBackupProfile();

        // This should only ever happen if the application was uninstalled
        // and then reinstalled. Uninstalling replaces the symlink with a
        // normal directory but keeps the app config files. As such,
        // I have to recreate/relink it here.
        if (!FileUtils.IsSymlink(AppConfig.AbsGameConfigDir))
        {
            // Triggers the symlink the be recreated
            MakeProfileActive(ActiveProfile);
        }
    }

    /// <summary>
    /// Loads saved profiles from disk as an array.
    /// </summary>
    private Profile[] LoadProfiles()
    {
        string profileData = FileUtils.ReadJsonFile(AppConfig.ProfileDatabaseFilepath);

        Profile[]? profiles = 
            JsonConvert.DeserializeObject<Profile[]>(profileData);

        if (profiles == null)
            return new Profile[0];

        return profiles;
    }

    /// <summary>
    /// Saves in-memory profiles to disk.
    /// </summary>
    private void SaveProfiles()
    {
        string profileData = 
            JsonConvert.SerializeObject(Profiles, Formatting.Indented);

        FileUtils.WriteJsonFile(profileData, AppConfig.ProfileDatabaseFilepath);
    }

    /// <summary>
    /// Creates a new profile and returns the GUID corresponding to it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the specified name is in use by an existing profile.
    /// </exception>
    public Guid CreateProfile(string name, bool readOnly = false, string? gameArgs = null)
    {
        // Disallow multiple profiles with the same name to avoid confusion
        if (DoesProfileByNameExist(name))
        {
            throw new ArgumentException("The specified profile name " +
                "is already in use");
        }

        Profile profile = new Profile(
            Guid.NewGuid(),
            name,
            gameArgs ?? string.Empty,
            readOnly);

        Profiles.Add(profile);

        SaveProfiles();

        return profile.Id;
    }

    /// <summary>
    /// Imports the specified APB installation as a configuration profile.
    /// Optionally allows for removing the installation once imported.
    /// </summary>
    public Guid ImportProfile(string path, bool delete = false)
    {
        Guid profileId = CreateProfile(path, false, null);

        CopyGameConfigToProfile(profileId, path);

        if (delete)
            Directory.Delete(path, true);

        return profileId;
    }

    /// <summary>
    /// Deletes the profile corresponding to the given GUID.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile corresponding to the given GUID exists.
    /// </exception>
    public void DeleteProfile(Guid id)
    {
        if (!DoesProfileByIdExist(id))
        {
            throw new ArgumentException("Cannot delete profile because no " +
                "profile corresponding with the given GUID exists.");
        }

        // If the profile to be deleted is the currently active one
        // we have to set another profile as active before deletion
        // otherwise the ID of the active profile will become dangling.
        if (ActiveProfile == id)
        {
            int profileIndex = Profiles.IndexOf(GetProfileById(id));

            // Assuming 'id' is not the last item in the list the
            // index of the new active profile will remain the same
            // as the list will be resized and therefore the next item
            // will take the index of the currently active one.
            int newActiveProfileIndex = profileIndex;

            // If 'id' is the last item in the list, simply
            // set the new active profile to be the item in the
            // index located before the old active profile.
            if (profileIndex == Profiles.Count - 1)
                newActiveProfileIndex = profileIndex - 1;

            MakeProfileActive(Profiles[newActiveProfileIndex].Id);
        }

        Profile profile = GetProfileById(id);

        Profiles.Remove(profile);
    }

    /// <summary>
    /// Creates a backup profile if one does not exist. Returns false if one
    /// already exists.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if APB's config directory is symlinked without a backup
    /// profile having been previously created.
    /// </exception>
    public bool CreateBackupProfile()
    {
        if (DoesProfileByNameExist("Backup"))
        {
            return false;
        }

        // It should not be possible for APB's config directory to be
        // symlinked if not backup exists. This is because a symlink
        // indicates that APBConfigManager has already been run, which
        // in turn means that it should already have created a backup profile.
        // Therefore, an exception is thrown here as something has clearly
        // gone wrong somewhere.
        if (FileUtils.IsSymlink(AppConfig.AbsGameConfigDir))
        {
            throw new InvalidOperationException(
                "Expected directory but found symlink " +
                "while creating a backup profile");
        }

        Guid backupProfile = CreateProfile("Backup", true);
        CopyGameConfigToProfile(backupProfile);

        // Note: This could be bad if backup was not created properly!
        Directory.Delete(AppConfig.AbsGameConfigDir, true);

        MakeProfileActive(backupProfile);

        return true;
    }

    /// <summary>
    /// Activates the specified profile by symlinking APB's config file
    /// directory to the config directory associated with the specified
    /// profile.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the config directory isn't symlinked and no backup exists.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile corresponding to the given GUID exists.
    /// </exception>
    public void MakeProfileActive(Guid id)
    {
        if (!DoesProfileByIdExist(id))
        {
            throw new ArgumentException(
                "Cannot activate profile because no profile " +
                "corresponding to the given GUID exists.");
        }

        // Delete previous symlink if one exists
        if (FileUtils.IsSymlink(AppConfig.AbsGameConfigDir))
        {
            Directory.Delete(AppConfig.AbsGameConfigDir, true);
        }
        else if (Directory.Exists(AppConfig.AbsGameConfigDir))
        {
            // Sanity check: Make sure the backup exists before deleting
            // the game's config directory.
            if (!DoesProfileByNameExist("Backup"))
            {
                throw new InvalidOperationException("Attempted to activate a" +
                    " profile when no previous symlink existed, while also " +
                    "not having any backup profile. Aborting to prevent " +
                    "loss of data.");
            }

            // Should be safe to delete if we have a backup
            Directory.Delete(AppConfig.AbsGameConfigDir, true);
        }

        Directory.CreateSymbolicLink(
            AppConfig.AbsGameConfigDir, 
            AppConfig.AbsProfileConfigDir + id.ToString());

        AppConfig.ActiveProfile = id;

        Debug.WriteLine($"Successfully switched to profile '{id}'");
    }

    /// <summary>
    /// Copies the specified APB configuration files to the given profile.
    /// In most cases, this is synonymous with the configuration directory
    /// of the currently active profile since the game configuration
    /// directory is symlinked to it. If <paramref name="apbPath"/> is
    /// null the path will default to the currently selected APB path.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile with the given GUID exists or if the given
    /// game path does not lead to a valid APB game installation directory.
    /// </exception>
    public void CopyGameConfigToProfile(Guid id, string? apbPath = null)
    {
        string gamePath = apbPath != null ? apbPath : AppConfig.GamePath;

        if (!IsValidGamePath(gamePath))
        {
            throw new ArgumentException(
                "The specified path does not lead to a valid " +
                "APB: Reloaded installation directory");
        }

        if (!DoesProfileByIdExist(id))
        {
            throw new ArgumentException(
                "Can not copy game configuration to profile because" +
                "no profile associated with the given GUID exists.");
        }

        string configDirPath = gamePath + AppConfig.RelGameConfigDir;
        string[] files = Directory.GetFiles(configDirPath, "*", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string relativeFilePath = Path.GetRelativePath(configDirPath, file);
            string profileFilePath = GetProfileConfigDirectory(id) + "\\" + relativeFilePath;

            new FileInfo(profileFilePath).Directory?.Create();

            File.Copy(file, profileFilePath, true);
        }
    }

    /// <summary>
    /// Creates a shortcut in the given path for the specified profile.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the given GUID does not correspond to an existing profile.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if calls to the underlaying IWshRuntimeLibrary returns null.
    /// There is however no way to determine when this happens as Microsoft
    /// has not documented any reasons as to why the method(s) might return
    /// null. Good luck!
    /// </exception>
    public void CreateProfileShortcut(Guid id, string filepath)
    {
        if (!DoesProfileByIdExist(id))
        {
            throw new ArgumentException(
                "Cannot create profile shortcut because no profile" +
                "associated with the given GUID exists.");
        }

        // Remove shortcut if it already exists - overwrite it
        if (File.Exists(filepath))
            File.Delete(filepath);

        ProcessModule? procMod = Process.GetCurrentProcess().MainModule;
        if (procMod == null)
        {
            throw new InvalidOperationException(
                "'Process.GetCurrentProcess().MainModule' returned null " +
                "due to reasons not documented by Microsoft.");
        }
        else if (procMod.FileName == null)
        {
            throw new InvalidOperationException(
                "'Process.GetCurrentProcess().MainModule.FileName' " +
                "returned null despite being documented by Microsoft as " +
                "a non-nullable type.");
        }

        string appExePath = procMod.FileName;
        string targetPath = appExePath;

        IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
        IWshRuntimeLibrary.IWshShortcut? shortcut =
            shell.CreateShortcut(filepath) as IWshRuntimeLibrary.IWshShortcut;

        if (shortcut == null)
        {
            throw new InvalidOperationException(
                "Failed to create shortcut using IWshRuntimeLibrary for " +
                "reasons unknown to me because I cannot find any " +
                "documentation.");
        }

        shortcut.TargetPath = targetPath;
        shortcut.Arguments = "--run " + id.ToString();
        shortcut.Save();
    }


    /// <summary>
    /// Sets the path to the APB installation which the profiles will be
    /// applied to. Returns false if the given path is not a valid path
    /// to an APB installation directory.
    /// </summary>
    public bool SetGamePath(string path)
    {
        if (!IsValidGamePath(path))
            return false;

        AppConfig.GamePath = path;

        // The active profile has to be reapplied to the new APB install
        MakeProfileActive(ActiveProfile);

        return true;
    }

    /// <summary>
    /// Returns true if the given path is a valid path to an APB install
    /// root directory. This is determined by checking for the existance
    /// of the APB executable (APB.exe) in the "Binaries" subdirectory.
    /// </summary>
    public static bool IsValidGamePath(string path)
    {
        return File.Exists(path + "\\Binaries\\APB.exe");
    }

    /// <summary>
    /// Returns the profile corresponding to the given GUID.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile corresponding to the given GUID exists.
    /// </exception>
    public Profile GetProfileById(Guid id)
    {
        foreach (Profile profile in Profiles)
        {
            if (profile.Id == id) return profile;
        }

        throw new ArgumentException(
            "No profile corresponding to the given GUID exists.");
    }

    /// <summary>
    /// Returns the profile with the specified name.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile with the specified name exists.
    /// </exception>
    public Profile GetProfileByName(string name)
    {
        foreach (Profile profile in Profiles)
        {
            if (profile.Name == name) return profile;
        }

        throw new ArgumentException("No profile with the given name exists.");
    }


    /// <summary>
    /// Returns true if a profile corresponding to the given GUID exists or
    /// false if not.
    /// </summary>
    public bool DoesProfileByIdExist(Guid id)
    {
        foreach (Profile profile in Profiles)
        {
            if (profile.Id == id) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if a profile with the given name exists or false if not.
    /// </summary>
    public bool DoesProfileByNameExist(string name)
    {
        foreach (Profile profile in Profiles)
        {
            if (profile.Name == name) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a path to the directory where configuration files for the
    /// corresponding to the given GUID is stored.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no profile corresponding to the given GUID exists.
    /// </exception>
    public string GetProfileConfigDirectory(Guid id)
    {
        if (!DoesProfileByIdExist(id))
        {
            throw new ArgumentException(
                "No profile corresponding to the given GUID exists.");
        }

        return AppConfig.AppDataPath + "Profiles\\" + id.ToString();
    }

    /// <summary>
    /// Activates the specified profile and runs the APB: Reloaded 
    /// executable with the arguments specified in the profile.
    /// </summary>
    public void RunGameWithProfile(Guid profileId)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = AppConfig.AbsGameExeFilepath;
        startInfo.WorkingDirectory = AppConfig.GamePath;

        startInfo.Arguments = GetProfileById(profileId).GameArgs;

        MakeProfileActive(profileId);

        Process.Start(startInfo);
    }
}