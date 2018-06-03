using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class CDReaderExtensions
    {
        public static VirtualDirectory ToVirtualDirectory( this CDReader isoFileSystem )
        {
            return ToVirtualFileSystemEntry( isoFileSystem, isoFileSystem.Root.FullName ) as VirtualDirectory;
        }

        public static VirtualFileSystemEntry ToVirtualFileSystemEntry( this CDReader isoFileSystem, string path )
        {
            if ( isoFileSystem.FileExists( path ) )
            {
                var fileName = Path.GetFileName( path );
                if ( fileName.EndsWith( ";1" ) )
                {
                    fileName = fileName.Substring( 0, fileName.Length - 2 );
                }

                return new VirtualFile( null, isoFileSystem.OpenFile( path, FileMode.Open ), fileName );
            }
            else
            {
                var directory = new VirtualDirectory( null, Path.GetFileName( path ) );

                foreach ( var file in isoFileSystem.GetFiles( path ) )
                {
                    var entry = ToVirtualFileSystemEntry( isoFileSystem, file );
                    entry.MoveTo( directory );
                }

                foreach ( var subDirectory in isoFileSystem.GetDirectories( path ) )
                {
                    var entry = ToVirtualFileSystemEntry( isoFileSystem, subDirectory );
                    entry.MoveTo( directory );
                }

                return directory;
            }
        }
    }
}
