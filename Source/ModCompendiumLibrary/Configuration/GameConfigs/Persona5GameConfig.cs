using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public class Persona5GameConfig : GameConfig
    {
        public string Compression { get; set; }

        public override Game Game => Game.Persona5;

        protected override void DeserializeCore( XElement element )
        {
            Compression = element.GetElementValueOrEmpty(nameof(Compression));
        }

        protected override void SerializeCore( XElement element )
        {
            element.AddNameValuePair(nameof(Compression), Compression);
        }
    }
}