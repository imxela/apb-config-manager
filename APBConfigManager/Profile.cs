using Newtonsoft.Json;

namespace APBConfigManager
{
    public class Profile
    {
        public Guid id;
        public string name;
        public string gameArgs;
        public bool readOnly;

        [JsonConstructor]
        internal Profile(Guid id, string name, string gameArgs, bool readOnly)
        {
            this.id = id;
            this.name = name;
            this.gameArgs = gameArgs;
            this.readOnly = readOnly;
        }
    }
}
