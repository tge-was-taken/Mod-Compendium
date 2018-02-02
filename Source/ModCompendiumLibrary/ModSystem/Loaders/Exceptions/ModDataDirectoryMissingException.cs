using System;
using System.Runtime.Serialization;

namespace ModCompendiumLibrary.ModSystem.Loaders
{
    [Serializable]
    public class ModDataDirectoryMissingException : Exception
    {
        public ModDataDirectoryMissingException()
        {
        }

        public ModDataDirectoryMissingException( string message ) : base( message )
        {
        }

        public ModDataDirectoryMissingException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected ModDataDirectoryMissingException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}