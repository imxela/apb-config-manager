using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace APBConfigManager
{
    public class Profile : INotifyPropertyChanged
    {
        private Guid _id;
        private string _name;
        private string _gameArgs;
        private bool _readOnly;

        public Guid Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string GameArgs
        {
            get
            {
                return _gameArgs;
            }

            set
            {
                _gameArgs = value;
                OnPropertyChanged();
            }
        }

        public bool ReadOnly
        {
            get
            {
                return _readOnly;
            }

            set
            {
                _readOnly = value;
                OnPropertyChanged();
            }
        }

        [JsonConstructor]
        public Profile(Guid id, string name, string gameArgs, bool readOnly)
        {
            _name = name;
            _gameArgs = gameArgs;

            Id = id;
            Name = name;
            GameArgs = gameArgs;
            ReadOnly = readOnly;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
