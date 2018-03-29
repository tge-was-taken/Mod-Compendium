using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    internal static class XElementExtensions
    {
        public static string GetElementValueOrEmpty( this XElement parent, string childName )
        {
            var element = parent.Element( childName );
            if ( element == null )
            {
                return string.Empty;
            }
            else
            {
                return element.Value;
            }
        }

        public static string GetElementValueOrFallback( this XElement parent, string childName, string fallback )
        {
            var element = parent.Element( childName );
            if ( element == null )
            {
                return fallback;
            }
            else
            {
                return element.Value;
            }
        }

        public static void AddNameValuePair( this XElement element, string name, object value )
        {
            element.Add( new XElement( name, value ) );
        }

    }
}
