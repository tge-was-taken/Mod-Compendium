using System;
using System.IO;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public class VirtualFile : VirtualFileSystemEntry
    {
        private readonly Stream mStream;

        public VirtualFile( VirtualDirectory parent, string hostPath, string name ) 
            : base(parent, hostPath, name, VirtualFileSystemEntryType.File)
        {
        }

        public VirtualFile( VirtualDirectory parent, Stream stream, string name )
            : base( parent, null, name, VirtualFileSystemEntryType.File )
        {
            mStream = stream ?? throw new ArgumentNullException( nameof( stream ) );
        }

        public Stream Open()
        {
            if ( StoredInMemory )
                return mStream;
            else
                return File.OpenRead( HostPath );
        }

        public override string SaveToHost( string destinationHostPath )
        {
            var path = Path.Combine( destinationHostPath, FullName );

            if ( StoredInMemory )
            {
                using ( var fileStream = File.OpenWrite( path ) )
                {
                    mStream.CopyTo( fileStream );
                    mStream.Position = 0;
                }
            }
            else
            {
                File.Copy( HostPath, path, true );
            }

            return path;
        }

        internal override VirtualFileSystemEntry Copy()
        {
            if ( StoredInMemory )
                return new VirtualFile( null, mStream, Name );
            else
                return new VirtualFile( null, HostPath, Name );
        }

        public static VirtualFile FromHostFile( string path, VirtualDirectory parent = null )
        {
            return new VirtualFile( parent, path, Path.GetFileName( path ) );
        }
    }
}