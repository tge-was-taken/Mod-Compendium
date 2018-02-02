using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class Persona34GameConfig : GameConfig
    {
        protected Persona34GameConfig()
        {
            DvdRootPath = string.Empty;
        }

        /// <summary>
        /// Path to either a directory or an ISO file containing the game's files.
        /// </summary>
        public string DvdRootPath { get; set; }

        protected override void DeserializeCore( XElement element )
        {
            DvdRootPath = SerializationHelper.GetValueOrEmpty( element, nameof( DvdRootPath ) );
        }

        protected override void SerializeCore( XElement element )
        {
            element.Add( new XElement( nameof( DvdRootPath ), DvdRootPath ) );
        }
    }
}