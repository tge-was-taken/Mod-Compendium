using System;
using System.Runtime.Serialization;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [Serializable]
    public class MissingFileException : Exception
    {
        public MissingFileException()
        {
        }

        public MissingFileException( string message ) : base( message )
        {
        }

        public MissingFileException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected MissingFileException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}