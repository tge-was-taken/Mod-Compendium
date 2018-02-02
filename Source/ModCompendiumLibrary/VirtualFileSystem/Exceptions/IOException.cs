using System;
using System.Runtime.Serialization;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    [Serializable]
    public class IOException : Exception
    {
        public IOException()
        {
        }

        public IOException( string message ) : base( message )
        {
        }

        public IOException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected IOException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}