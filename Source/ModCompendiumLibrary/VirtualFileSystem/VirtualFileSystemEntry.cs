using System;

namespace ModCompendiumLibrary.VirtualFileSystem
{
    public abstract class VirtualFileSystemEntry
    {
        public VirtualDirectory Parent { get; internal set; }

        public string HostPath { get; }

        public string Name { get; set; }

        public string FullName
        {
            get
            {
                string fullName = Name;
                var parent = Parent;
                while ( parent != null )
                {
                    if ( parent.Name.Length != 0 )
                    {
                        fullName = parent.Name + "\\" + fullName;
                    }

                    parent = parent.Parent;
                }

                return fullName;
            }
        }

        public VirtualFileSystemEntryType EntryType { get; }

        public bool StoredInMemory => HostPath == null;

        protected VirtualFileSystemEntry( VirtualDirectory parent, string hostPath, string name, VirtualFileSystemEntryType entryType )
        {
            if ( name == null )
            {
                throw new ArgumentNullException( nameof( name ) );
            }

            Parent = parent;
            HostPath = hostPath;
            Name = name;
            EntryType = entryType;
        }

        /// <summary>
        ///     Saves the entry to the host filesystem.
        /// </summary>
        /// <param name="destinationHostPath"></param>
        /// <returns>Returns the path to which the entry was saved.</returns>
        public abstract string SaveToHost( string destinationHostPath );

        /// <summary>
        ///     Moves this entry from it's parent directory to the specified directory.
        /// </summary>
        /// <param name="directory"></param>
        public void MoveTo( VirtualDirectory directory, bool replace = false )
        {
            // Remove file from parent directory
            if ( Parent != null )
            {
                Parent.Remove( this );
            }

            // Add file to destination directory
            Parent = directory;

            if ( directory != null )
            {
                directory.InternalAddOrReplace( this, replace );
            }
        }

        /// <summary>
        ///     Makes a copy of this entry and moves the copy to the specified directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>The copy.</returns>
        public VirtualFileSystemEntry CopyTo( VirtualDirectory directory )
        {
            var copy = Copy();

            // Add file to destination directory
            copy.Parent = directory;

            if ( directory != null )
            {
                copy.MoveTo( directory );
            }

            return copy;
        }

        /// <summary>
        ///     Shallow copy.
        /// </summary>
        /// <returns></returns>
        public abstract VirtualFileSystemEntry Copy();

        public override string ToString()
        {
            return $"{FullName} ({HostPath})";
        }
    }
}
