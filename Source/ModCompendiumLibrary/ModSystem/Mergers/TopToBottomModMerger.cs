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
            var entryHistory = new Dictionary<string, VirtualFileSystemEntry>();

            foreach ( var mod in mods )
            {
                var dataDirectory = VirtualDirectory.FromHostDirectory( mod.DataDirectory );
                dataDirectory.Name = string.Empty;

                void AddEntryRecursively( VirtualDirectory directory )
                {
                    foreach ( var entry in directory )
                    {
                        bool inCache = entryHistory.TryGetValue( entry.FullName, out var cachedEntry );

                        if (!inCache)
                        {
                            var newEntry = entry.CopyTo( fileDirectory );
                            entryHistory[entry.FullName] = newEntry;
                        }
                        else if ( entry.EntryType == VirtualFileSystemEntryType.Directory && cachedEntry.EntryType == VirtualFileSystemEntryType.Directory )
                        {
                            ( ( VirtualDirectory ) cachedEntry ).CopyMerge( ( VirtualDirectory ) entry, false );
                        }
                    }
                }

                AddEntryRecursively( dataDirectory );
            }

            return fileDirectory;
        }
    }
}