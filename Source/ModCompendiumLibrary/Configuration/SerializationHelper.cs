using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    internal static class SerializationHelper
    {
        public static string GetValueOrEmpty( XElement parent, string childName )
        {
            var element = parent.Element( childName );
            if ( element == null )
            {
                return string.Empty;
            }
            return element.Value;
        }
    }
}
