using System;
using System.Runtime.Serialization;

namespace ModCompendiumLibrary.ModSystem.Loaders
{
    [Serializable]
    public class ModXmlFileInvalidException : Exception
    {
        public ModXmlFileInvalidException()
        {
        }

        public ModXmlFileInvalidException( string message ) : base( message )
        {
        }

        public ModXmlFileInvalidException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected ModXmlFileInvalidException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}