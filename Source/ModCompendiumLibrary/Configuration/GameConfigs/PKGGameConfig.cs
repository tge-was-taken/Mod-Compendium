using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class PKGGameConfig : GameConfig
    {
        protected PKGGameConfig()
        {
            PKGPath = string.Empty;
            Compression = "True";
        }

        public string PKGPath { get; set; }
        public string Compression { get; set; }

        protected override void DeserializeCore(XElement element)
        {
            PKGPath = element.GetElementValueOrEmpty(nameof(PKGPath));
            Compression = element.GetElementValueOrEmpty(nameof(Compression));
        }

        protected override void SerializeCore(XElement element)
        {
            element.AddNameValuePair(nameof(PKGPath), PKGPath);
            element.AddNameValuePair(nameof(Compression), Compression);
        }

        public class Persona5RoyalGameConfig : PKGGameConfig
        {
            public override Game Game => Game.Persona5Royal;
        }
    }
}