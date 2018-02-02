using System.Collections.Generic;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Mergers
{
    public interface IModMerger
    {
        VirtualDirectory Merge( IEnumerable<Mod> mods );
    }
}