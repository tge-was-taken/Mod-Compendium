using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class Persona34GameConfig : GameConfig
    {
        protected Persona34GameConfig()
        {
            DvdRootOrIsoPath = string.Empty;
            HostFS = "False";
        }

        /// <summary>
        /// Path to either a directory or an ISO file containing the game's files.
        /// </summary>
        public string DvdRootOrIsoPath { get; set; }
        public string HostFS { get; set; }

        protected override void DeserializeCore( XElement element )
        {
            DvdRootOrIsoPath = element.GetElementValueOrEmpty( nameof( DvdRootOrIsoPath ) );
            HostFS = element.GetElementValueOrEmpty(nameof( HostFS ));
        }

        protected override void SerializeCore( XElement element )
        {
            element.AddNameValuePair( nameof( DvdRootOrIsoPath ), DvdRootOrIsoPath );
            element.AddNameValuePair(nameof(HostFS), HostFS);
        }
    }
}