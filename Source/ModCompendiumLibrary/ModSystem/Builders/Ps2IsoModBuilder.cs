using System;
using ModCompendiumLibrary.VirtualFileSystem;
using ModCompendiumLibrary.FileParsers;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.Configuration;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("PS2 ISO Mod Builder")]
    public class Ps2IsoModBuilder : IModBuilder
    {
        /// <summary>
        /// Build a PS2 bootable iso from the files in the root directory.
        /// If output path is specified, it is expected to be a path to the output ISO.
        /// Otherwise, if present, the root's name will be used as the base name for the ISO output.
        /// Requires SYSTEM.CNF and an executable file to be present in the root's file structure.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="hostOutputPath"></param>
        /// <returns>PS2 bootable ISO file.</returns>
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false)
        {
            if ( root == null )
                throw new ArgumentNullException( nameof( root ) );

            // Hack: get rid of root's name and restore it later
            // This is so we can use FullName with the builder without having to strip the root's name 
            var rootName = root.Name;
            root.Name = string.Empty;

            // Find system.cnf first, as we need it to get the executable file path
            var systemCnfFile = root[ "SYSTEM.CNF" ] as VirtualFile ?? throw new MissingFileException( "SYSTEM.CNF is missing." );

            var executablePath = Ps2SystemConfig.GetExecutablePath( systemCnfFile.Open(), false, true ) ??
                                 throw new MissingFileException(
                                     "Executable file path is not specified in SYSTEM.CNF; Unable to locate executable file." );

            var executableFile = root[ executablePath ] as VirtualFile ??
                                 throw new MissingFileException( $"Executable file {executablePath} is missing." );

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
            else
            {
                string isoName;
                if ( !string.IsNullOrWhiteSpace( root.Name ) )
                    isoName = root.Name + ".iso";
                else
                    isoName = "PS2DVD.iso";

                var stream = isoBuilder.Build();
                return new VirtualFile( null, stream, isoName );
            }
        }

        private void AddToIsoBuilderRecursively( CDBuilder isoBuilder, VirtualDirectory directory, string executablePath )
        {
            foreach ( var entry in directory )
            {
                // SYSTEM.CNF and the executable have already been added at this point
                if ( entry.Name.Equals( "SYSTEM.CNF", StringComparison.InvariantCultureIgnoreCase ) || 
                    entry.FullName.Equals( executablePath, StringComparison.InvariantCultureIgnoreCase) )
                    continue;

                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                {
                    isoBuilder.AddFile( entry.FullName, ( ( VirtualFile )entry ).Open() );
                }
                else
                {
                    AddToIsoBuilderRecursively( isoBuilder, ( VirtualDirectory ) entry, executablePath );
                }
            }
        }
    }

    public abstract class Persona34IsoModBuilder : IModBuilder
    {
        protected abstract Persona34FileModBuilder CreateFileModBuilder();

        protected abstract Persona34GameConfig GetConfig();

        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false)
        {
            // Build mod files
            var fileModBuilder = CreateFileModBuilder();
            var modFilesDirectory = ( VirtualDirectory )fileModBuilder.Build( root );

            // Merge original files with mod files
            var config = GetConfig();
            var rootDirectory = Persona34Helper.GetRootDirectory( config, out var isoFileSystem );
            rootDirectory.Merge( modFilesDirectory, true );

            // Build ISO
            var ps2IsoModBuilder = new Ps2IsoModBuilder();
            var ps2IsoFile = ps2IsoModBuilder.Build( rootDirectory, hostOutputPath );

            if (hostOutputPath != null)
                isoFileSystem?.Dispose();

            return ps2IsoFile;
        }
    }

    // Todo
    //[ModBuilder("Persona 3 ISO Mod Builder", Game = Game.Persona3)]
    public class Persona3IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder CreateFileModBuilder() => new Persona3FileModBuilder();

        protected override Persona34GameConfig GetConfig() => ConfigManager.Get<Persona3GameConfig>();
    }

    //[ModBuilder( "Persona 4 ISO Mod Builder", Game = Game.Persona4 )]
    public class Persona4IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder CreateFileModBuilder() => new Persona4FileModBuilder();

        protected override Persona34GameConfig GetConfig() => ConfigManager.Get<Persona4GameConfig>();
    }
}
