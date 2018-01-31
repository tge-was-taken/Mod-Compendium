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
            var entryHistory = new HashSet<string>();

            foreach ( var mod in mods )
            {
                foreach ( var entry in mod.DataDirectory )
                {
                    if ( !entryHistory.Contains( entry.FullName ) )
                    {
                        entry.CopyTo( fileDirectory );
                        entryHistory.Add( entry.FullName );
                    }
                }
            }

            return fileDirectory;
        }
    }
}