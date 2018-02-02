using System;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.FileParsers;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ ModBuilder( "PS2 ISO Mod Builder" ) ]
    public class Ps2IsoModBuilder : IModBuilder
    {
        private void AddToIsoBuilderRecursively( CDBuilder isoBuilder, VirtualDirectory directory, string executablePath )
        {
            foreach ( var entry in directory )
            {
                // SYSTEM.CNF and the executable have already been added at this point
                if ( entry.Name.Equals( "SYSTEM.CNF", StringComparison.InvariantCultureIgnoreCase ) ||
                     entry.FullName.Equals( executablePath, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    continue;
                }

                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                {
                    isoBuilder.AddFile( entry.FullName, ( ( VirtualFile ) entry ).Open() );
                }
                else
                {
                    AddToIsoBuilderRecursively( isoBuilder, ( VirtualDirectory ) entry, executablePath );
                }
            }
        }

        /// <summary>
        ///     Build a PS2 bootable iso from the files in the root directory.
        ///     If output path is specified, it is expected to be a path to the output ISO.
        ///     Otherwise, if present, the root's name will be used as the base name for the ISO output.
        ///     Requires SYSTEM.CNF and an executable file to be present in the root's file structure.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="hostOutputPath"></param>
        /// <returns>PS2 bootable ISO file.</returns>
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            // Hack: get rid of root's name and restore it later
            // This is so we can use FullName with the builder without having to strip the root's name 
            string rootName = root.Name;
            root.Name = string.Empty;

            // Find system.cnf first, as we need it to get the executable file path
            var systemCnfFile = ( VirtualFile ) root[ "SYSTEM.CNF" ];
            if ( systemCnfFile == null )
            {
                throw new MissingFileException( "SYSTEM.CNF is missing." );
            }

            string executablePath;
            using ( var systemCnfStream = systemCnfFile.Open() )
            {
                executablePath = Ps2SystemConfig.GetExecutablePath( systemCnfStream, hostOutputPath == null, true );
                systemCnfStream.Position = 0;
            }

            if ( executablePath == null )
            {
                throw new MissingFileException( "Executable file path is not specified in SYSTEM.CNF; Unable to locate executable file." );
            }

            var executableFile = ( VirtualFile ) root[ executablePath ];
            if ( executableFile == null )
            {
                throw new MissingFileException( $"Executable file {executablePath} is missing." );
            }

            var isoBuilder = new CDBuilder
            {
                UseJoliet = false,
                UpdateIsolinuxBootTable = false,
                VolumeIdentifier = "AMICITIA"
            };

            // system.cnf first
            isoBuilder.AddFile( systemCnfFile.Name, systemCnfFile.Open() );

            // executable second
            isoBuilder.AddFile( executablePath, executableFile.Open() );

            // And then the rest
            AddToIsoBuilderRecursively( isoBuilder, root, executablePath );

            // HACK: Restore root name
            root.Name = rootName;

            if ( hostOutputPath != null )
            {
                isoBuilder.Build( hostOutputPath );
                return VirtualFile.FromHostFile( hostOutputPath );
            }
            string isoName;
            if ( !string.IsNullOrWhiteSpace( root.Name ) )
            {
                isoName = root.Name + ".iso";
            }
            else
            {
                isoName = "PS2DVD.iso";
            }

            var stream = isoBuilder.Build();
            return new VirtualFile( null, stream, isoName );
        }
    }

    public abstract class Persona34IsoModBuilder : IModBuilder
    {
        protected abstract Persona34FileModBuilder GetFileModBuilder();

        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            var fileModBuilder = GetFileModBuilder();
            var dvdRootDirectory = fileModBuilder.Build( root );
            var ps2IsoModBuilder = new Ps2IsoModBuilder();
            var ps2IsoFile = ps2IsoModBuilder.Build( ( VirtualDirectory ) dvdRootDirectory, hostOutputPath );
            return ps2IsoFile;
        }
    }

    //[ModBuilder("Persona 3 ISO Mod Builder", Game = Game.Persona3)]
    public class Persona3IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder GetFileModBuilder()
        {
            return new Persona3FileModBuilder();
        }
    }

    //[ModBuilder( "Persona 4 ISO Mod Builder", Game = Game.Persona4 )]
    public class Persona4IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder GetFileModBuilder()
        {
            return new Persona4FileModBuilder();
        }
    }
}
