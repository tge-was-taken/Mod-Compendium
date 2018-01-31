using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class PassthroughModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            Log.Builder.Info( $"Copying over mod files to {hostOutputPath}" );

            if ( hostOutputPath != null )
            {
                root.SaveToHost( hostOutputPath );
            }

            return root;
        }
    }
}
