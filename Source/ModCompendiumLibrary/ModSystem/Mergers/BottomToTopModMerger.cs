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
            var entryHistory = new HashSet<string>();

            foreach ( var mod in mods.Reverse() )
            {
                var dataDirectory = VirtualDirectory.FromHostDirectory( mod.DataDirectory );
                dataDirectory.Name = string.Empty;

                void AddEntryRecursively( VirtualDirectory directory )
                {
                    foreach ( var entry in directory )
                    {
                        if ( !entryHistory.Contains( entry.FullName ) )
                        {
                            entry.CopyTo( fileDirectory );
                            entryHistory.Add( entry.FullName );
                        }
                        else if (entry.EntryType == VirtualFileSystemEntryType.Directory)
                        {
                            AddEntryRecursively( ( VirtualDirectory ) entry );
                        }
                    }
                }

                AddEntryRecursively( dataDirectory );
            }

            return fileDirectory;
        }
    }
}