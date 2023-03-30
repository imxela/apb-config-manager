﻿using APBConfigManager.UI.Model;
using Avalonia.Controls;
using JetBrains.Annotations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
                else if (!File.Exists(value + AppConfig.GameRelativeExePath))
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

        public void OnDeleteProfileCommand()
        {
            if (SelectedProfile == null)
                return;

            if (SelectedProfile.Id == Guid.Parse(AppConfig.ActiveProfile))
            {
                // Todo: Display error dialog box:
                //       Unable to delete the profile as it is currently active!
                //       Please activate a different profile before deleting this one.
                throw new NotImplementedException();
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

            ProfileManager.Instance.DeleteDesktopShortcutForProfile(SelectedProfile.Profile);

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

            ProfileManager.Instance.CreateDesktopShortcutForProfile(SelectedProfile.Profile, result);

            IsBusy = false;
        }

        // Todo: Move this somewhere else?
        public void OnRunAdvLauncherCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = AppConfig.AdvLauncherExecutablePath;
            startInfo.WorkingDirectory = GamePath;

            Process.Start(startInfo);
        }

        public void OnRunAPBCommand()
        {
            if (SelectedProfile == null)
                return;

            ProfileManager.Instance.RunGameWithProfile(SelectedProfile.Id);
        }
    }
}