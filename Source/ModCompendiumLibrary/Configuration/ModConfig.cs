using System;
using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    /// <summary>
    /// Mod configuration info. Stores user configurable data for a game mod.
    /// </summary>
    public class ModConfig
    {
        public Guid ModId { get; private set; }

        public int Priority { get; internal set; }

        public bool Enabled { get; internal set; }

        private ModConfig()
        {
        }

        public ModConfig( Guid modId, int priority, bool enabled )
        {
            ModId       = modId;
            Priority = priority;
            Enabled  = enabled;
        }

        public static ModConfig Deserialize( XElement element )
        {
            var instance = new ModConfig();

            foreach ( var xProperty in element.Elements() )
            {
                switch ( xProperty.Name.LocalName )
                {
                    case nameof( ModId ):
                        Guid.TryParse( xProperty.Value, out var modId );
                        instance.ModId = modId;
                        break;

                    case nameof( Priority ):
                        int.TryParse( xProperty.Value, out var priority );
                        instance.Priority = priority;
                        break;

                    case nameof( Enabled ):
                        bool.TryParse( xProperty.Value, out var enabled );
                        instance.Enabled = enabled;
                        break;
                }
            }

            return instance;
        }

        public XElement Serialize()
        {
            var element = new XElement( nameof( ModConfig ) );
            element.AddNameValuePair( nameof( ModId ),    ModId );
            element.AddNameValuePair( nameof( Priority ), Priority );
            element.AddNameValuePair( nameof( Enabled ),  Enabled );
            return element;
        }
    }
}