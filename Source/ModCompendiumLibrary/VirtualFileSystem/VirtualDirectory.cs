using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public class VirtualDirectory : VirtualFileSystemEntry, IEnumerable<VirtualFileSystemEntry>
    {
        private readonly LinkedList<VirtualFileSystemEntry> mEntries;

        public VirtualDirectory( VirtualDirectory parent, string hostPath, string name )
            : base(parent, hostPath, name, VirtualFileSystemEntryType.Directory)
        {
            mEntries = new LinkedList< VirtualFileSystemEntry >();
        }

        public VirtualDirectory( VirtualDirectory parent, string name )
            : base( parent, null, name, VirtualFileSystemEntryType.Directory )
        {
            mEntries = new LinkedList<VirtualFileSystemEntry>();
        }

        public VirtualDirectory() : base(null, null, string.Empty, VirtualFileSystemEntryType.Directory)
        {
            mEntries = new LinkedList<VirtualFileSystemEntry>();
        }

        public VirtualFileSystemEntry this[string name]
        {
            get => Find( name );
        }

        public override string SaveToHost( string destinationHostPath )
        {
            var path = Path.Combine( destinationHostPath, FullName );
            Directory.CreateDirectory( path );

            foreach ( var entry in this )
            {
                entry.SaveToHost( destinationHostPath );
            }

            return path;
        }

        internal override VirtualFileSystemEntry Copy()
        {
            var copy = new VirtualDirectory( null, HostPath, Name );
            foreach ( var entry in this )
            {
                copy.Add( entry.Copy() );
            }

            return copy;
        }

        public VirtualFileSystemEntry Find( string name )
        {
            return mEntries.SingleOrDefault( x => x.Name.Equals( name, System.StringComparison.InvariantCultureIgnoreCase ) );
        }

        public bool Remove( VirtualFileSystemEntry entry )
        {
            bool removed = mEntries.Remove( entry );

            if ( removed )
            {
                entry.Parent = null;
            }

            return removed;
        }

        public void Add( VirtualFileSystemEntry entry, bool replace = false )
        {
            VirtualFileSystemEntry foundEntry;

            if ( (foundEntry = Find(entry.Name)) != null )
            {
                if ( replace )
                {
                    // Replace the found entry with the entry to add
                    Replace( foundEntry, entry );
                }
                else if ( foundEntry.EntryType == VirtualFileSystemEntryType.Directory )
                {
                    // We're not replacing anything, but we do have clashing directories, so we just merge them
                    ( ( VirtualDirectory ) foundEntry ).Merge( ( VirtualDirectory ) entry, false );
                }
                else
                {
                    //throw new VirtualFileSystemFileAlreadyExistsException( $"File '{entry.Name}' already exists" );
                }
            }
            else
            {
                if ( entry.Parent != null )
                    entry.Parent.Remove( entry );

                entry.Parent = this;
                mEntries.AddLast( entry );
            }
        }

        public void MoveEntriesTo( VirtualDirectory other )
        {
            foreach ( var entry in this )
            {
                entry.MoveTo( other );
            }
        }

        public bool Contains( VirtualFileSystemEntry entry )
        {
            return mEntries.Contains( entry );
        }

        public bool Contains( string name )
        {
            return mEntries.Any( x => x.Name.Equals( name, System.StringComparison.InvariantCultureIgnoreCase ) );
        }

        private void Replace( VirtualFileSystemEntry original, VirtualFileSystemEntry replacement )
        {
            if ( original.EntryType != replacement.EntryType )
                throw new IOException( "File system entry types must match" );

            var isDirectory = original.EntryType == VirtualFileSystemEntryType.Directory;

            // Reparent replacement
            if ( replacement.Parent != null )
                replacement.Parent.Remove( replacement );

            replacement.Parent = this;

            // Do replacement
            var originalNode = mEntries.Find( original );

            // ... | original | replacement | ...
            mEntries.AddAfter( originalNode, replacement );

            // ... | replacement | ...
            mEntries.Remove( originalNode );

            if ( isDirectory )
            {
                // Merge
                ( ( VirtualDirectory )replacement ).Merge( ( VirtualDirectory )original, false );
            }
        }

        public void Merge( VirtualDirectory other, bool replace )
        {
            foreach ( var entry in other )
            {
                Add( entry, replace );
            }
        }

        public static VirtualDirectory FromHostDirectory( string hostDirectoryPath, VirtualDirectory parent = null )
        {
            var directory = new VirtualDirectory( null, hostDirectoryPath, Path.GetFileName( hostDirectoryPath ) );
            foreach ( var entry in Directory.EnumerateFileSystemEntries(hostDirectoryPath) )
            {
                if ( File.Exists( entry ) )
                {
                    directory.mEntries.AddLast( VirtualFile.FromHostFile(entry, directory) );
                }
                else
                {
                    directory.mEntries.AddLast( FromHostDirectory( entry, directory ) );
                }
            }

            return directory;
        }

        // IEnumerator implementation
        public IEnumerator<VirtualFileSystemEntry> GetEnumerator()
        {
            var entry = mEntries.First;
            while ( entry != null )
            {
                var value = entry.Value;
                entry = entry.Next;
                yield return value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}