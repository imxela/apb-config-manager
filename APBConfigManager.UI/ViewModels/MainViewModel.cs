using APBConfigManager.UI.Model;
using Avalonia.Controls;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace APBConfigManager.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Window _window;

        private string _statusText = string.Empty;

        public string WindowTitle
        {
            get
            {
                return "APB Config Manager (Active Profile: " + ProfileManager.Instance.GetProfileById(Guid.Parse(AppConfig.ActiveProfile)).name + ")";
            }
        }

        public string StatusText
        {
            get
            {
                if (!IsBusy)
                {
                    return "Status: Idle";
                }

                return _statusText;
            }

            set
            {
                _statusText = "Status: " + value;
                OnPropertyChanged();
            }
        }

        private bool _isBusy = false;

        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }

            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private bool _isGamePathValid = false;

        public bool IsGamePathValid
        {
            get { return _isGamePathValid; }
        }

        public bool CanEditSelectedProfile
        {
            get
            {
                return IsGamePathValid && SelectedProfile != null &&
                    !SelectedProfile.ReadOnly;
            }
        }

        public string GamePath
        {
            get
            {
                return AppConfig.GamePath;
            }

            set
            {
                if (value == null)
                {
                    AppConfig.GamePath = "Game path not set!";
                    _isGamePathValid = false;
                }
                else if (!ProfileManager.IsValidGamePath(value))
                {
                    AppConfig.GamePath = "The selected directory is not a valid APB install!";
                    _isGamePathValid = false;
                }
                else
                {
                    AppConfig.GamePath = value;
                    _isGamePathValid = true;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGamePathValid));
                OnPropertyChanged(nameof(CanEditSelectedProfile));
            }
        }

        private ObservableCollection<ProfileModel> _profiles =
            new ObservableCollection<ProfileModel>();

        public ObservableCollection<ProfileModel> Profiles
        {
            get
            {
                return _profiles;
            }

            set
            {
                _profiles = value;
                OnPropertyChanged();
            }
        }


        private ProfileModel? _selectedProfile;

        public ProfileModel? SelectedProfile
        {
            get
            {
                return _selectedProfile;
            }

            set
            {
                _selectedProfile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEditSelectedProfile));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public MainViewModel(Window window)
        {
            StatusText = "Initializing...";
            IsBusy = true;

            foreach (Profile profile in ProfileManager.Instance.Profiles)
            {
                _profiles.Add(new ProfileModel(profile));
            }

            SelectedProfile = Profiles[0];

            GamePath = AppConfig.GamePath;

            _window = window;

            IsBusy = false;
        }

        public async void OnLocateCommand()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();

            string? result = await dialog.ShowAsync(_window);

            // null is handled by GamePath
            // this warning is kind of annoying though.
            // Todo: Fix?
            GamePath = result;
        }

        public void OnCreateProfileCommand()
        {
            IsBusy = true;
            StatusText = "Creating Profile...";

            string newProfileName = "New Profile";
            int newProfileCount = 1;
            while (ProfileManager.Instance.GetProfilesByName(newProfileName).Count > 0)
            {
                newProfileName = $"New Profile ({newProfileCount})";
                newProfileCount++;
            }

            Profile profile = ProfileManager.Instance.CreateProfile(newProfileName);
            ProfileModel profileModel = new ProfileModel(profile);
            Profiles.Add(profileModel);

            IsBusy = false;
        }

        public async void OnImportProfileCommand()
        {
            IsBusy = true;
            StatusText = "Importing profile...";

            OpenFolderDialog dialog = new OpenFolderDialog();
            string? path = await dialog.ShowAsync(_window);

            if (path == null)
                return;

            if (path == GamePath)
            {
                await new MessageBoxFactory()
                    .Title("ERROR")
                    .Message("Importing the main APB: Reloaded config is not allowed - create a new profile instead.")
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK")
                    .Show(_window);

                IsBusy = false;
                return;
            }

            bool shouldDelete = await new MessageBoxFactory()
                .Title("Question")
                .Message("Would you like to delete the install after the profile has been imported?")
                .Icon(MessageBoxIconType.Error)
                .Button("Yes")
                .Button("No", true, true)
                .Show(_window) == "Yes";

            try
            {
                Profile profile = ProfileManager.Instance.ImportProfile(path, shouldDelete);
                ProfileModel profileModel = new ProfileModel(profile);
                Profiles.Add(profileModel);
            }
            catch (InvalidGamePathException) 
            {
                await new MessageBoxFactory()
                    .Title("ERROR")
                    .Message("The chosen path is not a valid APB: Reloaded installation.")
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK")
                    .Show(_window);

                IsBusy = false;
                return;
            }

            IsBusy = false;
        }

        public async void OnDeleteProfileCommand()
        {
            if (SelectedProfile == null)
                return;

            if (SelectedProfile.Id == Guid.Parse(AppConfig.ActiveProfile))
            {
                await new MessageBoxFactory()
                    .Title("ERROR")
                    .Message("Deleting the active profile is not allowed!\nActivate a different profile and try again.")
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK")
                    .Show(_window);

                return;
            }

            IsBusy = true;
            StatusText = "Deleting profile...";

            ProfileManager.Instance.DeleteProfile(SelectedProfile.Id);

            int deletedIndex = Profiles.IndexOf(SelectedProfile);
            Profiles.Remove(SelectedProfile);

            if (deletedIndex < Profiles.Count)
                SelectedProfile = Profiles[deletedIndex];
            else
                SelectedProfile = Profiles[Profiles.Count - 1];

            IsBusy = false;
        }

        public void OnSaveProfileCommand()
        {
            if (SelectedProfile == null)
                return;

            IsBusy = true;
            StatusText = "Saving profile...";

            ProfileManager.Instance.UpdateProfile(SelectedProfile.Profile);
            SelectedProfile.IsDirty = false;

            IsBusy = false;
        }

        public void OnActivateProfileCommand()
        {
            if (SelectedProfile == null)
                return;

            IsBusy = true;
            StatusText = "Activating profile...";

            ProfileManager.Instance.ActivateProfile(SelectedProfile.Id);

            OnPropertyChanged(nameof(WindowTitle));

            IsBusy = false;
        }

        public async void OnCreateProfileShortcutCommand()
        {
            if (SelectedProfile == null)
                return;

            IsBusy = true;
            StatusText = "Creating shortcut...";

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialFileName = "APB Reloaded - " + SelectedProfile.Name + ".lnk";
            dialog.DefaultExtension = ".lnk";
            dialog.Directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.Title = "Save Shortcut: " + SelectedProfile.Name;

            string? result = await dialog.ShowAsync(_window);

            if (result == null)
            {
                IsBusy = false;
                return;
            }

            ProfileManager.Instance.CreateDesktopShortcutForProfile(SelectedProfile.Id, result);

            IsBusy = false;
        }

        public void OnOpenGameDirInExplorerCommand()
        {
            OpenInExplorer(GamePath);
        }

        public void OnOpenProfileDirInExplorerCommand()
        {
            if (SelectedProfile == null)
                throw new ProfileNotFoundException(
                    "An attempt was made to open the directory of the " +
                    "currently selected profile but no profile is selected");

            string path = ProfileManager.Instance
                .GetProfileDirById(SelectedProfile.Id);

            OpenInExplorer(path);
        }

        public async void OnRunAdvLauncherCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = AppConfig.AdvLauncherExecutablePath;
            startInfo.WorkingDirectory = GamePath;

            if (IsExecutableRunning(AppConfig.AdvLauncherExecutablePath))
            {
                await new MessageBoxFactory()
                    .Title("ERROR")
                    .Message("An instance of APB Advanced Launcher is already running.")
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK")
                    .Show(_window);

                return;
            }

            Process.Start(startInfo);
        }

        public async void OnRunAPBCommand()
        {
            if (SelectedProfile == null)
                return;

            // If an instance of APB is already running, abort and inform
            if (IsExecutableRunning(AppConfig.GameExecutableFilepath))
            {
                var messageBox = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandardWindow("ERROR", "An instance APB: Reloaded is already running.");

                await messageBox.ShowDialog(_window);

                await new MessageBoxFactory()
                    .Title("ERROR")
                    .Message("An instance of APB: Reloaded is already running.")
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK")
                    .Show(_window);

                return;
            }

            ProfileManager.Instance.RunGameWithProfile(SelectedProfile.Id);
        }

        private void OpenInExplorer(string path)
        {
            Process.Start("explorer.exe", path);
        }

        private bool IsExecutableRunning(string path)
        {
            string processName = Path.GetFileNameWithoutExtension(path);
            return Process.GetProcessesByName(processName).Length > 0;
        }
    }
}
