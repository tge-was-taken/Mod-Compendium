using System.Collections.Generic;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Mergers
{
    public class TopToBottomModMerger : IModMerger
    {
        public VirtualDirectory Merge( IEnumerable<Mod> mods )
        {
            Log.Merger.Info( "Merging mods from top to bottom" );

            var fileDirectory = new VirtualDirectory();

            foreach ( var mod in mods )
            {
                var dataDirectory = VirtualDirectory.FromHostDirectory( mod.DataDirectory );
                fileDirectory.Merge( dataDirectory, Operation.AddOnly );
            }

            return fileDirectory;
        }
    }
}