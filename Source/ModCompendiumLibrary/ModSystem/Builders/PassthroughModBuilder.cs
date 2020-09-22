using System;
using System.Collections.Generic;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    /// <summary>
    /// This mod builder copies the given files without any processing.
    /// </summary>
    [ModBuilder( "Passthrough Mod Builder" )]
    public class PassthroughModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, List<Mod> enabledMods, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            if ( root == null )
                throw new ArgumentNullException( nameof( root ) );

            Log.Builder.Info( $"Copying over mod files to {hostOutputPath}" );

            if ( hostOutputPath != null )
                root.SaveToHost( hostOutputPath );

            return root;
        }
    }
}
