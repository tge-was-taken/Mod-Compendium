using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    /// <summary>
    /// Used by Config to dynamically serialize/deserialize configurable objects.
    /// Type inheriting IConfigurable must:
    ///     Have a public constructor with no arguments.
    ///     Not rely on deserialized data in constructor or type initializer.
    /// </summary>
    public interface IConfigurable
    {
        void Deserialize( XElement element );

        void Serialize( XElement element );
    }
}
