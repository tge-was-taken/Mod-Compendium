using System;
using System.IO;
using ModCompendiumLibrary.IO;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public class VirtualFile : VirtualFileSystemEntry, IDisposable
    {
        private Stream mStream;

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
                using ( var inStream = Open())
                using ( var outStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite) )
                {
                    inStream.CopyTo( outStream );
                }
            }

            return path;
        }

        public override void CopyToMemory( bool deleteHostEntry )
        {
            if ( HostPath == null )
                return;

            mStream = new MemoryStream();
            using ( var fileStream = File.OpenRead( HostPath ) )
                fileStream.CopyTo( mStream );

            if ( deleteHostEntry )
                File.Delete( HostPath );

            HostPath = null;
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

        public override void Delete()
        {
            if ( StoredInMemory )
                mStream.Dispose();
            else if ( File.Exists(HostPath ))
                File.Delete( HostPath );
        }

        /// <summary>
        /// Creates a new virtual file from a file on the host filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static VirtualFile FromHostFile( string path, bool copyToMemory = false, VirtualDirectory parent = null )
        {
            if ( copyToMemory )
            {
                MemoryStream stream = new MemoryStream();
                using ( var fileStream = File.OpenRead( path ) )
                    fileStream.CopyTo( fileStream );

                return new VirtualFile( parent, stream, Path.GetFileName( path ) );
            }
            else
            {
                return new VirtualFile( parent, path, Path.GetFileName( path ) );
            }
        }

        public void Dispose()
        {
            if ( StoredInMemory )
                mStream.Dispose();
        }
    }
}