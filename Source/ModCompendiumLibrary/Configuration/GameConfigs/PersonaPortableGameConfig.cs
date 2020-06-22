using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class PersonaPortableGameConfig : GameConfig
    {
        protected PersonaPortableGameConfig()
        {
            CpkRootOrPath = string.Empty;
            Compression = "True";
            Extract = "True";
            PC = "False";
        }

        /// <summary>
        /// Path to either a directory or a CPK file containing the game's files.
        /// </summary>
        public string CpkRootOrPath { get; set; }
        public string Compression { get; set; }
        public string Extract { get; set; }
        public string PC { get; set; }

        protected override void DeserializeCore(XElement element)
        {
            CpkRootOrPath = element.GetElementValueOrEmpty(nameof(CpkRootOrPath));
            Compression = element.GetElementValueOrEmpty(nameof(Compression));
            Extract = element.GetElementValueOrEmpty(nameof(Extract));
            PC = element.GetElementValueOrEmpty(nameof(PC));
        }

        protected override void SerializeCore( XElement element )
        {
            element.AddNameValuePair(nameof(CpkRootOrPath), CpkRootOrPath);
            element.AddNameValuePair(nameof(Compression), Compression);
            element.AddNameValuePair(nameof(Extract), Extract);
            element.AddNameValuePair(nameof(PC), PC);
        }
    }
}