using System;
using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class PKGGameConfig : GameConfig
    {
        protected PKGGameConfig()
        {
            PKGPath = string.Empty;
            Compression = "True";
            Region = "UP0177-CUSA17416";
        }

        public string PKGPath { get; set; }
        public string Compression { get; set; }
        public string Region { get; set; }

        protected override void DeserializeCore(XElement element)
        {
            PKGPath = element.GetElementValueOrEmpty(nameof(PKGPath));
            Compression = element.GetElementValueOrEmpty(nameof(Compression));
            Region = element.GetElementValueOrEmpty(nameof(Region));
        }

        protected override void SerializeCore(XElement element)
        {
            element.AddNameValuePair(nameof(PKGPath), PKGPath);
            element.AddNameValuePair(nameof(Compression), Compression);
            element.AddNameValuePair(nameof(Region), Region);
        }

        public class Persona5RoyalGameConfig : PKGGameConfig
        {
            public override Game Game => Game.Persona5Royal;
        }
    }
}