using System;
using System.Xml.Linq;
using ModCompendiumLibrary;
using ModCompendiumLibrary.Configuration;

namespace ModCompendium.Configs
{
    public class MainWindowConfig : IConfigurable
    {
        public Game SelectedGame { get; set; }

        public MainWindowConfig()
        {
            SelectedGame = Game.Persona5;
        }

        public void Deserialize( XElement element )
        {
            // Deserialize selected game
            var selectedGameElement = element.Element( nameof( SelectedGame ) );
            if ( selectedGameElement != null && Enum.TryParse<Game>( selectedGameElement.Value, out var game ) )
                SelectedGame = game;
        }

        public void Serialize( XElement element )
        {
            // Serialize selected game
            element.Add( new XElement( nameof( SelectedGame ), SelectedGame ) );
        }
    }
}
