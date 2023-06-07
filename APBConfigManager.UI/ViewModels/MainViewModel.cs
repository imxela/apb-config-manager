using Avalonia.Controls;
using JetBrains.Annotations;
using System;
using System.IO;
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

        private ProfileManager _profileManager;

        private string _statusText = string.Empty;

        public string WindowTitle
        {
            get
            {
                return "APB Config Manager";
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
                return IsGamePathValid &&
                    !IsGameRunning &&
                    !SelectedProfile.ReadOnly;
            }
        }

        public bool IsGameRunning
        {
            get
            {
                return IsGamePathValid && IsExecutableRunning(AppConfig.GamePath);
            }
        }

        public string GamePath
        {
            get
            {
                if (AppConfig.GamePath == string.Empty)
                    return "Please select a valid APB install path";

                return AppConfig.GamePath;
            }

            set
            {
                if (!_profileManager.SetGamePath(value))
                {
                    _ = new MessageBoxFactory()
                        .Icon(MessageBoxIconType.Error)
                        .Title("Error")
                        .Message("The selected path does not lead to a valid installation of APB: Reloaded.")
                        .Button("OK", true, true)
                        .Show(_window);
                }
                else
                {
                    _isGamePathValid = true;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGamePathValid));
                OnPropertyChanged(nameof(CanEditSelectedProfile));
            }
        }


        public Profile SelectedProfile
        {
            get
            {
                return _profileManager.GetProfileById(_profileManager.ActiveProfile);
            }

            set
            {
                if (IsExecutableRunning(AppConfig.GamePath))
                {
                    _ = new MessageBoxFactory()
                        .Icon(MessageBoxIconType.Error)
                        .Title("Error")
                        .Message("Cannot switch profile while APB is running!")
                        .Button("Abort", true, true)
                        .Show(_window);
                }
                else if (value == null)
                {
                    _profileManager.MakeProfileActive(_profileManager.ActiveProfile);
                }
                else
                {
                    _profileManager.MakeProfileActive(value.Id);
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEditSelectedProfile));
            }
        }

        public FullyObservableCollection<Profile> Profiles
        {
            get
            {
                return _profileManager.Profiles;
            }
        }

        public MainViewModel(Window window, ProfileManager profileManager)
        {
            StatusText = "Initializing...";
            IsBusy = true;

            _window = window;
            _profileManager = profileManager;

            _profileManager.Profiles.CollectionChanged += (sender, e) =>
            {
                // why are you not updating you silly dum dum code
                Debug.WriteLine("CollectionChanged on _profileManager.Profiles");
                OnPropertyChanged(nameof(Profiles));
            };

            GamePath = AppConfig.GamePath;

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
            while (_profileManager.DoesProfileByNameExist(newProfileName))
            {
                newProfileName = $"New Profile ({newProfileCount})";
                newProfileCount++;
            }

            Guid profileId = _profileManager.CreateProfile(newProfileName);

            Guid backupProfileId = _profileManager.GetProfileByName("Backup").Id;
            _profileManager.CopyProfileConfig(backupProfileId, profileId);

            IsBusy = false;
        }

        public void OnDuplicateProfileCommand()
        {
            IsBusy = true;
            StatusText = "Duplicating Profile...";

            string duplicateProfileName = SelectedProfile.Name;
            int duplicateProfileCount = 1;
            while (_profileManager.DoesProfileByNameExist(duplicateProfileName))
            {
                duplicateProfileName = $"{duplicateProfileName} ({duplicateProfileCount})";
                duplicateProfileCount++;
            }

            Guid profileId = _profileManager.CreateProfile(duplicateProfileName);

            // Todo: This is probably not a good way to copy the game args over
            //       It would probably be better to add a Copy trait to the 
            //       Profile Model, or alternatively to have a DuplicateProfile
            //       method in ProfileManager.
            _profileManager.GetProfileById(profileId).GameArgs = SelectedProfile.GameArgs;

            _profileManager.CopyProfileConfig(SelectedProfile.Id, profileId);

            IsBusy = false;
        }

        public async void OnImportProfileCommand()
        {
            IsBusy = true;
            StatusText = "Importing profile...";

            OpenFolderDialog dialog = new OpenFolderDialog();
            string? path = await dialog.ShowAsync(_window);

            if (path == null)
            {
                IsBusy = false;
                return;
            }

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
                // Retrieve directory name to use as profile name
                string? newProfileName = path.Substring(path.LastIndexOf('\\') + 1);
                ArgumentNullException.ThrowIfNull(newProfileName);

                Debug.WriteLine("newProfileName: " + newProfileName + " for path: " + path);

                Guid profileId = _profileManager.ImportProfile(path, shouldDelete);

                Profile profile = _profileManager.GetProfileById(profileId);

                profile.Name = newProfileName;
            }
            catch 
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
            IsBusy = true;
            StatusText = "Deleting profile...";

            string result = await new MessageBoxFactory()
                .Title("Delete?")
                .Message($"Are you sure you want to delete the '{SelectedProfile.Name}' profile?")
                .Icon(MessageBoxIconType.Question)
                .Button("No", true, true)
                .Button("Yes")
                .Show(_window);

            if (result != "Yes")
            {
                IsBusy = false;
                return;
            }

            _profileManager.DeleteProfile(SelectedProfile.Id);

            IsBusy = false;
        }

        public async void OnCreateProfileShortcutCommand()
        {
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

            _profileManager.CreateProfileShortcut(SelectedProfile.Id, result);

            IsBusy = false;
        }

        public void OnOpenGameDirInExplorerCommand()
        {
            OpenInExplorer(GamePath);
        }

        public void OnOpenProfileDirInExplorerCommand()
        {
            string path = _profileManager
                .GetProfileConfigDirectory(SelectedProfile.Id);

            OpenInExplorer(path);
        }

        public async void OnRunAdvLauncherCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = AppConfig.AbsAdvLauncherExePath;
            startInfo.WorkingDirectory = GamePath;

            if (IsExecutableRunning(AppConfig.AbsAdvLauncherExePath))
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
            // If an instance of APB is already running, abort and inform
            if (IsExecutableRunning(AppConfig.AbsGameExeFilepath))
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

            _profileManager.RunGameWithProfile(SelectedProfile.Id);
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
