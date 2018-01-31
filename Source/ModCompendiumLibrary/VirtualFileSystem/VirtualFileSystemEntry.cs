using System;
using System.ComponentModel;

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
                var fullName = Name;
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

            if ( !Enum.IsDefined( typeof( VirtualFileSystemEntryType ), entryType ) )
            {
                throw new InvalidEnumArgumentException( nameof( entryType ), ( int ) entryType, typeof( VirtualFileSystemEntryType ) );
            }

            Parent = parent;
            HostPath = hostPath;
            Name = name;
            EntryType = entryType;
        }

        public abstract string SaveToHost( string destinationHostPath );

        public void MoveTo( VirtualDirectory directory )
        {
            // Remove file from parent directory
            if ( Parent != null )
            {
                Parent.Remove( this );
            }

            // Add file to destination directory
            Parent = directory;

            if ( directory != null )
                directory.Add( this );
        }

        public VirtualFileSystemEntry CopyTo( VirtualDirectory directory )
        {
            var copy = Copy();

            // Add file to destination directory
            copy.Parent = directory;

            if ( directory != null )
                directory.Add( copy );

            return copy;
        }

        internal abstract VirtualFileSystemEntry Copy();

        public override string ToString()
        {
            return $"{FullName} ({HostPath})";
        }
    }
}
