using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ModCompendiumLibrary;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendium.GuiConfig
{
    public class MainWindowConfig : IConfigurable
    {
        public Game SelectedGame { get; set; }

        public Dictionary<Guid, int> ModOrder { get; set; }

        public MainWindowConfig()
        {
            SelectedGame = Game.Persona5;
            ModOrder = new Dictionary< Guid, int >();
            foreach ( var mod in ModDatabase.Mods )
                ModOrder[mod.Id] = 0;
        }

        public void Deserialize( XElement element )
        {
            // Deserialize selected game
            var selectedGameElement = element.Element( nameof( SelectedGame ) );
            if ( selectedGameElement != null && Enum.TryParse<Game>( selectedGameElement.Value, out var game ) )
                SelectedGame = game;

            // Deserialize mod order
            var modOrderElement = element.Element( nameof( ModOrder ) );
            if ( modOrderElement != null )
            {
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
        }

        public void Serialize( XElement element )
        {
            // Serialize selected game
            element.Add( new XElement( nameof( SelectedGame ), SelectedGame ) );

            // Serialize mod order
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
}
