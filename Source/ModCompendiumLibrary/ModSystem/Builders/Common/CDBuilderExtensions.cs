using DiscUtils.Iso9660;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class CDBuilderExtensions
    {
        public static void AddFile( this CDBuilder isoBuilder, string name, VirtualFile file )
        {
            if ( file.StoredInMemory )
                isoBuilder.AddFile( name + ";1", file.Open() );
            else
                isoBuilder.AddFile( name + ";1", file.HostPath );
        }
    }
}