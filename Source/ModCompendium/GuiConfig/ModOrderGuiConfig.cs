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
        }

        public void Deserialize( XElement element )
        {
            ModOrder = new Dictionary<Guid, int>();

            foreach ( var mod in ModDatabase.Mods )
            {
                ModOrder[mod.Id] = 0;
            }

            var modOrderElement = element.Element( nameof( ModOrder ) );
            if ( modOrderElement != null )
            {
                foreach ( var subElement in modOrderElement.Elements() )
                {
                    if ( Guid.TryParse( subElement.Attribute( "Id" ).Value, out var id ) && id != Guid.Empty )
                    {
                        var index = int.Parse( subElement.Attribute( "Index" ).Value );
                        if ( ModDatabase.Exists( id ) )
                        {
                            ModOrder[id] = index;
                        }
                    }
                }
            }
        }

        public void Serialize( XElement element )
        {
            var modOrderElement = new XElement( nameof( ModOrder ) );
            foreach ( var kvp in ModOrder )
            {
                var subElement = new XElement( $"{nameof( ModOrder )}Element",
                                               new XAttribute( "Id", kvp.Key ),
                                               new XAttribute( "Index", kvp.Value ) );

                modOrderElement.Add( subElement );
            }

            element.Add( modOrderElement );
        }
    }
}
