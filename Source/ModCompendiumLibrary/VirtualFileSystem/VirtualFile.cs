using System;
using System.IO;
using ModCompendiumLibrary.IO;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public class VirtualFile : VirtualFileSystemEntry, IDisposable
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

        /// <summary>
        /// Opens the file for reading.
        /// </summary>
        /// <returns></returns>
        public Stream Open()
        {
            if ( StoredInMemory )
                return new UncloseableStream( mStream );
            else
                return File.OpenRead( HostPath );
        }

        /// <inheritdoc />
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
            else if (!HostPath.Equals(path, StringComparison.InvariantCultureIgnoreCase))
            {
                File.Copy( HostPath, path, true );
            }

            return path;
        }

        /// <summary>
        /// Creates a copy of this virtual file. Do not that this is a shallow copy and does not copy the backing store of the file.
        /// </summary>
        /// <returns></returns>
        public override VirtualFileSystemEntry Copy()
        {
            if ( StoredInMemory )
                return new VirtualFile( null, mStream, Name );
            else
                return new VirtualFile( null, HostPath, Name );
        }

        /// <summary>
        /// Creates a new virtual file from a file on the host filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static VirtualFile FromHostFile( string path, VirtualDirectory parent = null )
        {
            return new VirtualFile( parent, path, Path.GetFileName( path ) );
        }

        public void Dispose()
        {
            if ( StoredInMemory )
                mStream.Dispose();
        }
    }
}