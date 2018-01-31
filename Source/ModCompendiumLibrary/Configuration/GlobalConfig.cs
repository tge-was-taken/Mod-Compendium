using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public class GlobalConfig : IConfigurable
    {
        void IConfigurable.Deserialize( XElement element )
        {
        }

        void IConfigurable.Serialize( XElement element )
        {
        }
    }
}
