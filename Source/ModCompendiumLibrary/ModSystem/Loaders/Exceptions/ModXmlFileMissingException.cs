using System;

namespace ModCompendiumLibrary.ModSystem.Loaders
{
    [Serializable]
    public class ModXmlFileMissingException : Exception
    {
        public ModXmlFileMissingException( string directory ) : base( $"Failed to load mod from directory {directory} because Mod.xml is missing" )
        {
        }
    }
}