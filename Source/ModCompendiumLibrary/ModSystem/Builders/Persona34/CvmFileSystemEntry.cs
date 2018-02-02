using System.Diagnostics;
using System.IO;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class CvmFileSystemEntry
    {
        public CvmDirectoryInfo Parent { get; set; }

        public int Size { get; set; }

        public int LBA { get; set; }

        public CvmFileSystemEntryFlags Flags { get; set; }

        public byte Field0F { get; set; }

        public string Name { get; set; }

        public CvmDirectoryInfo DirectoryInfo { get; set; }

        public CvmFileSystemEntry( CvmDirectoryInfo parent )
        {
            Parent = parent;
        }

        public void Read( BinaryReader reader )
        {
#if DEBUG
            long start = reader.BaseStream.Position;
#endif

            // 0x00 - 0x02 - pad
            reader.BaseStream.Position += 2;

            // 0x02 - 0x06
            Size = reader.ReadInt32();

            // 0x06 - 0x0A - unused
            reader.BaseStream.Position += 4;

            // 0x0A - 0x0E
            LBA = reader.ReadInt32();

            // 0x0E - 0x0F
            Flags = ( CvmFileSystemEntryFlags ) reader.ReadByte();

            // 0x0F - 0x10
            Field0F = reader.ReadByte();

            // 0x10 - 0x30
            Name = string.Empty;
            for ( int i = 0; i < 32; ++i )
            {
                var b = reader.ReadByte();
                if ( b != 0 )
                    Name += ( char ) b;
            }

#if DEBUG
            Debug.Assert( ( reader.BaseStream.Position - start ) == 48 );
#endif
        }

        public void Write( BinaryWriter writer)
        {
#if DEBUG
            long start = writer.BaseStream.Position;
#endif

            // 0x00 - 0x02 - pad
            writer.Write( ( short ) 0 );

            // 0x02 - 0x06
            writer.Write( Size );

            // 0x06 - 0x0A - unused
            writer.Write( 0u );

            // 0x0A - 0x0E
            writer.Write( LBA );

            // 0x0E - 0x0F
            writer.Write( ( byte ) Flags );

            // 0x0F - 0x10
            writer.Write( Field0F );

            // 0x10 - 0x30
            for ( int i = 0; i < 32; i++ )
            {
                writer.Write( i < Name.Length ? ( byte ) Name[ i ] : ( byte ) 0 );
            }

#if DEBUG
            Debug.Assert( (writer.BaseStream.Position - start) == 48 );
#endif
        }
    }
}