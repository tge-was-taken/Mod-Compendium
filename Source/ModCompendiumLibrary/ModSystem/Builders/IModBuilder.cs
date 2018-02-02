using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public interface IModBuilder
    {
        /// <summary>
        ///     Build a mod file given a virtual directory containing data, and maybe an output path where the mod file should be
        ///     output to.
        /// </summary>
        /// <param name="root">Virtual directory containing mod file data.</param>
        /// <param name="hostOutputPath">
        ///     Optional. If specified, the mod builder can be expected to output the built mod file to
        ///     the location specified.
        /// </param>
        /// <returns></returns>
        VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null );
    }
}
