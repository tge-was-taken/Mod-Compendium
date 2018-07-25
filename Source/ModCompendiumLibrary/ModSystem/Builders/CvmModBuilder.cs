using System;
using System.IO;
using ModCompendiumLibrary.VirtualFileSystem;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.Logging;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("CVM Mod Builder")]
    public class CvmModBuilder : IModBuilder
    {
        private static readonly byte[] sDummyCvmHeader = GenerateDummyCvmHeader();

        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            Log.Builder.Info( $"Building CVM: {root.Name}" );

            // Create temp directory 
            var tempDirectoryPath = Path.Combine( Path.GetTempPath(), "CvmModCompilerTemp_" + Path.GetRandomFileName() );
            Log.Builder.Trace( $"Creating temp directory: {tempDirectoryPath}" );
            Directory.CreateDirectory( tempDirectoryPath );

            // Make ISO
            Log.Builder.Info( "Creating ISO filesystem" );
            var isoBuilder = new CDBuilder()
            {
                UpdateIsolinuxBootTable = false,
                UseJoliet = false,
                VolumeIdentifier = "AMICITIA"
            };

            foreach ( var entry in root )
            {
                AddToBuilderRecursively( isoBuilder, entry, root.FullName );
            }

            // Build ISO
            Log.Builder.Info( "Building ISO" );
            var isoPath = Path.Combine( tempDirectoryPath, root.Name + ".iso" );
            isoBuilder.Build( isoPath );

            var cvmName = root.Name + ".cvm";
            var cvmPath = hostOutputPath == null ? Path.Combine( tempDirectoryPath, cvmName ) : hostOutputPath;

            // Write CVM file
            Log.Builder.Info( "Writing CVM" );
            using ( var cvmStream = File.Create( cvmPath ) )
            using ( var isoStream = File.OpenRead( isoPath ) )
            {
                // Dummy header first
                cvmStream.Write( sDummyCvmHeader, 0, sDummyCvmHeader.Length );
                
                // Aand then the ISO contents
                isoStream.CopyTo( cvmStream );
            }

            // Create virtual cvm file entry
            VirtualFile cvmFile;

            if ( hostOutputPath == null )
            {
                // Copy CVM to memory
                var memoryStream = new MemoryStream();
                using ( var fileStream = File.OpenRead( cvmPath ) )
                {
                    fileStream.CopyTo( memoryStream );
                }

                cvmFile = new VirtualFile( null, memoryStream, cvmName );
            }
            else
            {
                cvmFile = new VirtualFile( null, cvmPath, cvmName );
            }

            // Delete temp directory
            Log.Builder.Trace( $"Deleting temp directory: {tempDirectoryPath}" );
            Directory.Delete( tempDirectoryPath, true );

            return cvmFile;
        }

        private void AddToBuilderRecursively( CDBuilder isoBuilder, VirtualFileSystemEntry entry, string rootPrefix )
        {
            if ( entry.EntryType == VirtualFileSystemEntryType.File )
            {
                AddFile( isoBuilder, entry.FullName.Remove( 0, rootPrefix.Length ), ( ( VirtualFile ) entry ) );
            }
            else
            {
                isoBuilder.AddDirectory( entry.FullName.Remove( 0, rootPrefix.Length ) );
                foreach ( var directoryEntry in ((VirtualDirectory)entry) )
                {
                    AddToBuilderRecursively( isoBuilder, directoryEntry, rootPrefix );
                }
            }
        }

        private static void AddFile( CDBuilder isoBuilder, string name, VirtualFile file )
        {
            if ( file.StoredInMemory )
                isoBuilder.AddFile( name, file.Open() );
            else
                isoBuilder.AddFile( name, file.HostPath );
        }

        private static byte[] GenerateDummyCvmHeader()
        {
            byte[] bytes = new byte[0x1800];
            bytes[ 0x0000 ] = 0x43;
            bytes[ 0x0001 ] = 0x56;
            bytes[ 0x0002 ] = 0x4D;
            bytes[ 0x0003 ] = 0x48;
            bytes[ 0x0083 ] = 0x01;
            bytes[ 0x008B ] = 0x03;
            bytes[ 0x0103 ] = 0x01;
            bytes[ 0x0800 ] = 0x5A;
            bytes[ 0x0801 ] = 0x4F;
            bytes[ 0x0802 ] = 0x4E;
            bytes[ 0x0803 ] = 0x45;
            bytes[ 0x082F ] = 0x03;

            return bytes;
        }
    }
}
