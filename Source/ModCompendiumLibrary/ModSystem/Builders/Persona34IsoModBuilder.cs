using System;
using System.IO;
using System.Linq;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem.Builders.Utilities;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class Persona34IsoModBuilder : IModBuilder
    {
        protected abstract Persona34FileModBuilder CreateFileModBuilder();

        protected abstract Persona34GameConfig GetConfig();

        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            if ( hostOutputPath == null )
            {
                // We need a path, so generate one.
                hostOutputPath = Path.Combine( Path.GetTempPath(), "Persona34IsoModBuilderTemp_" + Path.GetRandomFileName() + ".iso" );
            }
            else if ( Directory.Exists( hostOutputPath ) )
            {
                // Add file name if the path is a directory
                hostOutputPath = Path.Combine( hostOutputPath, "Amicitia.iso" );
            }

            // Build mod files
            var fileModBuilder = CreateFileModBuilder();
            var tempDirectory = Path.Combine( Path.GetTempPath(), "Persona34IsoModBuilderTemp_" + Path.GetRandomFileName() );
            Directory.CreateDirectory( tempDirectory );
            var modFilesDirectory = ( VirtualDirectory )fileModBuilder.Build( root, tempDirectory );

            var config = GetConfig();
            if ( !config.DvdRootOrIsoPath.EndsWith( ".iso" ) )
                throw new NotImplementedException( "This can only be done with an ISO source right now!" );

            // Modify & save new ISO
            Log.Builder.Info( $"Modifying & saving ISO to {hostOutputPath} (this will take a while)" );
            UltraISOUtility.ModifyIso( config.DvdRootOrIsoPath, hostOutputPath, modFilesDirectory.Select( x => x.HostPath ) );

            // Delete temp directory
            Directory.Delete( tempDirectory, true );

            // We're done
            return VirtualFile.FromHostFile( hostOutputPath );
        }
    }

    // Todo
    //[ModBuilder("Persona 3 ISO Mod Builder", Game = Game.Persona3)]
    public class Persona3IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder CreateFileModBuilder() => new Persona3FileModBuilder();

        protected override Persona34GameConfig GetConfig() => ConfigStore.Get<Persona3GameConfig>();
    }

    //[ModBuilder( "Persona 4 ISO Mod Builder", Game = Game.Persona4 )]
    public class Persona4IsoModBuilder : Persona34IsoModBuilder
    {
        protected override Persona34FileModBuilder CreateFileModBuilder() => new Persona4FileModBuilder();

        protected override Persona34GameConfig GetConfig() => ConfigStore.Get<Persona4GameConfig>();
    }
}