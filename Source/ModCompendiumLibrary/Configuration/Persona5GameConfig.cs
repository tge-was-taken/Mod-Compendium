using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public class Persona5GameConfig : GameConfig
    {
        public override Game Game => Game.Persona5;

        protected override void DeserializeCore( XElement element )
        {
        }

        protected override void SerializeCore( XElement element )
        {
        }
    }
}