using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public interface IModBuilder
    {
        /// <summary>
        /// Build a mod file given a virtual directory containing data, and maybe an output path where the mod file should be output to.
        /// </summary>
        /// <param name="root">Virtual directory containing mod file data.</param>
        /// <param name="hostOutputPath">Optional. If specified, the mod builder can be expected to output the built mod file to the location specified.</param>
        /// <param name="gameName">Optional. If compression == true, the mod builder will use the CSV of the game name specified for compression.</param>
        /// <param name="useCompression">Optional. If compression == true, the mod builder will compress the resulting cpk.</param>
        /// <param name="useExtracted">Optional. If extract == true, the mod builder will extract the cpk at the specified cpk path and include its files in the mod.</param>
        /// <returns></returns>
        VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false);
    }
}