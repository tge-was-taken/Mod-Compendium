using System.Collections.Generic;
using System.Linq;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Mergers
{
    public class BottomToTopModMerger : IModMerger
    {
        public VirtualDirectory Merge( IEnumerable<Mod> mods )
        {
            Log.Merger.Info( "Merging mods from bottom to top" );

            var fileDirectory = new VirtualDirectory();

            foreach ( var mod in mods.Reverse() )
            {
                var dataDirectory = VirtualDirectory.FromHostDirectory( mod.DataDirectory );
                fileDirectory.Merge( dataDirectory, Operation.AddOnly );
            }

            return fileDirectory;
        }
    }
}