using System.Xml.Linq;
using ModCompendiumLibrary.Configuration;

namespace ModCompendiumLibrary.ModSystem
{
    public class ModDatabaseConfig : IConfigurable
    {
        public string ModsDirectoryPath { get; private set; }

        public ModDatabaseConfig()
        {
            ModsDirectoryPath = "Mods\\";
        }

        void IConfigurable.Deserialize( XElement element )
        {
            ModsDirectoryPath = element.GetElementValueOrFallback( nameof( ModsDirectoryPath ), "Mods\\" );
        }

        void IConfigurable.Serialize( XElement element )
        {
            element.AddNameValuePair( nameof( ModsDirectoryPath ), ModsDirectoryPath );
        }
    }
}