using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public class VirtualDirectory : VirtualFileSystemEntry, IEnumerable< VirtualFileSystemEntry >
    {
        private readonly LinkedList< VirtualFileSystemEntry > mEntries;

        public VirtualFileSystemEntry this[ string name ] => Find( name );

        public VirtualDirectory( VirtualDirectory parent, string hostPath, string name )
            : base( parent, hostPath, name, VirtualFileSystemEntryType.Directory )
        {
            mEntries = new LinkedList< VirtualFileSystemEntry >();
        }

        public VirtualDirectory( VirtualDirectory parent, string name )
            : base( parent, null, name, VirtualFileSystemEntryType.Directory )
        {
            mEntries = new LinkedList< VirtualFileSystemEntry >();
        }

        public VirtualDirectory() : base( null, null, string.Empty, VirtualFileSystemEntryType.Directory )
        {
            mEntries = new LinkedList< VirtualFileSystemEntry >();
        }

        /// <inheritdoc />
        public override string SaveToHost( string destinationHostPath )
        {
            string path = Path.Combine( destinationHostPath, FullName );
            Directory.CreateDirectory( path );

            foreach ( var entry in this )
                entry.SaveToHost( destinationHostPath );

            return path;
        }

        /// <inheritdoc />
        public override VirtualFileSystemEntry Copy()
        {
            var copy = new VirtualDirectory( null, HostPath, Name );
            foreach ( var entry in this )
            {
                var entryCopy = entry.Copy();
                entryCopy.MoveTo( copy );
            }

            return copy;
        }

        // Todo: recurse?
        /// <summary>
        ///     Finds an entry in the directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VirtualFileSystemEntry Find( string name )
        {
            return mEntries.SingleOrDefault( x => x.Name.Equals( name, StringComparison.InvariantCultureIgnoreCase ) );
        }

        /// <summary>
        ///     Returns whether or not the directory contains the specified entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool Contains( VirtualFileSystemEntry entry )
        {
            return mEntries.Contains( entry );
        }

        /// <summary>
        ///     Returns whether or not the directory an entry with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains( string name )
        {
            return mEntries.Any( x => x.Name.Equals( name, StringComparison.InvariantCultureIgnoreCase ) );
        }

        /// <summary>
        ///     Removes the entry from the directory.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Whether the entry was removed or not.</returns>
        public bool Remove( VirtualFileSystemEntry entry )
        {
            bool removed = mEntries.Remove( entry );

            if ( removed )
            {
                entry.Parent = null;
            }

            return removed;
        }

        /// <summary>
        ///     Removes the entry from the directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Whether the entry was removed or not.</returns>
        public bool Remove( string name )
        {
            var entry = Find( name );
            if ( entry != null )
            {
                return Remove( entry );
            }
            return false;
        }

        /// <summary>
        ///     Move the contents of this directory to another directory.
        /// </summary>
        /// <param name="other"></param>
        public void MoveContentsTo( VirtualDirectory other )
        {
            foreach ( var entry in this )
                entry.MoveTo( other );
        }

        public void Merge( VirtualDirectory other, bool replaceExisting )
        {
            foreach ( var entry in other )
                InternalAddOrReplace( entry, replaceExisting );
        }

        /// <summary>
        ///     Creates a new virtual directory from a directory on the host filesystem.
        /// </summary>
        /// <param name="hostDirectoryPath"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static VirtualDirectory FromHostDirectory( string hostDirectoryPath, VirtualDirectory parent = null )
        {
            var directory = new VirtualDirectory( parent, hostDirectoryPath, Path.GetFileName( hostDirectoryPath ) );
            foreach ( string entry in Directory.EnumerateFileSystemEntries( hostDirectoryPath ) )
            {
                if ( File.Exists( entry ) )
                {
                    directory.mEntries.AddLast( VirtualFile.FromHostFile( entry, directory ) );
                }
                else
                {
                    directory.mEntries.AddLast( FromHostDirectory( entry, directory ) );
                }
            }

            return directory;
        }

        internal void InternalAddOrReplace( VirtualFileSystemEntry entry, bool replace = false )
        {
            VirtualFileSystemEntry foundEntry;

            if ( ( foundEntry = Find( entry.Name ) ) != null )
            {
                if ( foundEntry.EntryType == VirtualFileSystemEntryType.Directory && entry.EntryType == VirtualFileSystemEntryType.Directory )
                {
                    // Merge directories
                    ( ( VirtualDirectory ) foundEntry ).Merge( ( VirtualDirectory ) entry, replace );
                }
                else if ( replace )
                {
                    // Replace the found entry with the entry to add
                    Replace( foundEntry, entry );
                }
            }
            else
            {
                if ( entry.Parent != null )
                {
                    entry.Parent.Remove( entry );
                }

                entry.Parent = this;
                mEntries.AddLast( entry );
            }
        }

        private void Replace( VirtualFileSystemEntry original, VirtualFileSystemEntry replacement )
        {
            if ( original.EntryType != replacement.EntryType )
            {
                throw new IOException( "File system entry types must match" );
            }

            bool isDirectory = original.EntryType == VirtualFileSystemEntryType.Directory;

            // Reparent replacement
            if ( replacement.Parent != null )
            {
                replacement.Parent.Remove( replacement );
            }

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
                ( ( VirtualDirectory ) replacement ).Merge( ( VirtualDirectory ) original, false );
            }
        }

        // IEnumerator implementation
        public IEnumerator< VirtualFileSystemEntry > GetEnumerator()
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
