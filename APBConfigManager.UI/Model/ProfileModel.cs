using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace APBConfigManager.UI.Model
{
    public class ProfileModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Profile _profile;

        public Profile Profile
        {
            get { return _profile; }
        }

        public Guid Id
        {
            get
            {
                return _profile.id;
            }
        }

        public string Name
        {
            get
            {
                if (_profile.readOnly)
                {
                    return "<<" + _profile.name + ">>";
                }
                else
                {
                    return _profile.name;
                }
            }

            set
            {
                if (Regex.Match(value, "^[^0-9\\s<>:\\\"\\/\\\\|\\?\\*\\s][^<>:\\\"\\/\\\\|\\?\\*]*[^\\s\\.]$").Length != value.Length)
                {
                    throw new ArgumentException(nameof(Name), "Profile name contains invalid characters!");
                }
                
                _profile.name = value;
                IsDirty = true;
                OnPropertyChanged();
            }
        }

        public string GameArgs
        {
            get
            {
                return _profile.gameArgs;
            }

            set
            {
                _profile.gameArgs = value;
                IsDirty = true;
                OnPropertyChanged(nameof(GameArgs));
            }
        }

        public bool ReadOnly
        {
            get
            {
                return _profile.readOnly;
            }

            set
            {
                _profile.readOnly = value;
                OnPropertyChanged();
            }
        }

        private bool _isDirty = false;

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }

            set
            {
                _isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
            }
        }

        public ProfileModel(string name, string gameArguments, bool readOnly)
        {
            _profile = ProfileManager.Instance.CreateProfile(name, gameArguments, readOnly);
            OnPropertyChanged();
        }

        public ProfileModel(Profile profile)
        {
            _profile = profile;
        }
    }
}
