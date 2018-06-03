using DiscUtils.Iso9660;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class CDBuilderExtensions
    {
        public static void AddFile( this CDBuilder isoBuilder, string name, VirtualFile file )
        {
            if ( !name.EndsWith( ";1" ) )
                name += ";1";

            if ( file.StoredInMemory )
                isoBuilder.AddFile( name, file.Open() );
            else
                isoBuilder.AddFile( name, file.HostPath );
        }
    }
}