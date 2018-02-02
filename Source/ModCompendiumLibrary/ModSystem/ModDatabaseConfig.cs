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
            var modsDirectoryPathElement = element.Element( nameof( ModsDirectoryPath ) );
            if ( modsDirectoryPathElement != null )
                ModsDirectoryPath = modsDirectoryPathElement.Value;
        }

        void IConfigurable.Serialize( XElement element )
        {
            element.Add( new XElement( nameof( ModsDirectoryPath ), ModsDirectoryPath ) );
        }
    }
}