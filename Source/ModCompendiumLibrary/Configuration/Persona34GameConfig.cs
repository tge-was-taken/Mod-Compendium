using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class Persona34GameConfig : GameConfig
    {
        /// <summary>
        ///     Path to either a directory or an ISO file containing the game's files.
        /// </summary>
        public string DvdRootPath { get; set; }

        protected Persona34GameConfig()
        {
            DvdRootPath = string.Empty;
        }

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
