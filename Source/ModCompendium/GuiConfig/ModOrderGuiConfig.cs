using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendium.GuiConfig
{
    public class ModOrderGuiConfig : IConfigurable
    {
        public Dictionary<Guid, int> ModOrder { get; set; }

        public ModOrderGuiConfig()
        {
            ModOrder = new Dictionary< Guid, int >();

            foreach ( var mod in ModDatabase.Mods )
            {
                ModOrder[mod.Id] = 0;
            }
        }

        public void Deserialize( XElement element )
        {
            var modOrderElement = element.Element( nameof( ModOrder ) );
            if ( modOrderElement == null )
                return;

            foreach ( var subElement in modOrderElement.Elements() )
            {
                var idAttribute = subElement.Attribute( nameof( Mod.Id ) );
                if ( idAttribute == null || !Guid.TryParse( idAttribute.Value, out var id ) || id == Guid.Empty || !ModDatabase.Exists( id ) )
                    return;

                var orderAttribute = subElement.Attribute( "Order" );
                if ( orderAttribute == null || !int.TryParse( orderAttribute.Value, out var order ) )
                    return;

                ModOrder[id] = order;
            }
        }

        public void Serialize( XElement element )
        {
            var modOrderElement = new XElement( nameof( ModOrder ) );
            foreach ( var kvp in ModOrder )
            {
                var subElement = new XElement( $"{nameof( ModOrder )}Element",
                                               new XAttribute( "Id", kvp.Key ),
                                               new XAttribute( "Order", kvp.Value ) );

                modOrderElement.Add( subElement );
            }

            element.Add( modOrderElement );
        }
    }
}
