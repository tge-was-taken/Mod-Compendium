using System;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ ModBuilder( "Passthrough Mod Builder" ) ]
    public class PassthroughModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            Log.Builder.Info( $"Copying over mod files to {hostOutputPath}" );

            if ( hostOutputPath != null )
            {
                root.SaveToHost( hostOutputPath );
            }

            return root;
        }
    }
}
