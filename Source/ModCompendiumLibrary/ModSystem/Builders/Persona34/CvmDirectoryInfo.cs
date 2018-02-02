using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class CvmDirectoryInfo
    {
        public const string TAG = "#DirLst#";

        public CvmFileSystemEntry Entry { get; set; }

        public List<CvmFileSystemEntry> Entries { get; set; }

        public int DirectoryLBA { get; set; }

        public CvmDirectoryInfo( CvmFileSystemEntry entry )
        {
            Entry = entry;
            Entries = new List< CvmFileSystemEntry >();
        }

        public void Read( BinaryReader reader )
        {
#if DEBUG
            long start = reader.BaseStream.Position;
#endif

            // 0x00 - 0x02
            int entryCount = reader.ReadInt32();

            // 0x04 - 0x08
            int entryCountAux = reader.ReadInt32();

            Debug.Assert( entryCount == entryCountAux, "entryCount != entryCountAux" );

            // 0x08 - 0x0C
            DirectoryLBA = reader.ReadInt32();

            // 0x0C - 0x14
            string tag = string.Empty;
            for ( int i = 0; i < 8; ++i )
            {
                var b = reader.ReadByte();
                if ( b != 0 )
                    tag += ( char )b;
            }

            Debug.Assert( tag.Equals( TAG ) );

            // 0x14 - 0x16
            Debug.Assert( reader.ReadUInt16() == 0 );

#if DEBUG
            Debug.Assert( ( reader.BaseStream.Position - start ) == 22 );
#endif

            // Read children
            Entries.Capacity = entryCount;
            for ( int i = 0; i < entryCount; i++ )
            {
                var entry = new CvmFileSystemEntry(this);
                entry.Read( reader );

                Entries.Add( entry );
            }

            reader.BaseStream.Position = ( reader.BaseStream.Position + 15 ) & ~( 15 );

            for ( int i = 1; i < entryCount; i++ )
            {
                var entry = Entries[ i ];
                if ( entry.Flags.HasFlag( CvmFileSystemEntryFlags.DirectoryRecord ) )
                {
                    var directory = new CvmDirectoryInfo( entry );
                    directory.Read( reader );

                    entry.DirectoryInfo = directory;
                }
            }
        }

        public void Write( BinaryWriter writer )
        {
#if DEBUG
            long start = writer.BaseStream.Position;
#endif

            writer.Write( Entries.Count );
            writer.Write( Entries.Count );
            writer.Write( DirectoryLBA );

            foreach ( var c in TAG )
                writer.Write( ( byte ) c );

            writer.Write( ( short ) 0 );

#if DEBUG
            Debug.Assert( ( writer.BaseStream.Position - start ) == 22 );
#endif
            foreach ( var entry in Entries )
            {
                entry.Write( writer );
            }

            long alignmentByteCount = ( ( writer.BaseStream.Position + 15 ) & ~( 15 ) ) - writer.BaseStream.Position;
            for ( int i = 0; i < alignmentByteCount; ++i )
                writer.Write( ( byte ) 0 );

            for ( int i = 1; i < Entries.Count; ++i )
            {
                var entry = Entries[i];
                if ( entry.Flags.HasFlag( CvmFileSystemEntryFlags.DirectoryRecord ) )
                {
                    entry.DirectoryInfo.Write( writer );
                }
            }
        }
    }
}