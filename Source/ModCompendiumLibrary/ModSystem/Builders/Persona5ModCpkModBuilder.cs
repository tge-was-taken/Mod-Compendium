using System;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.IO;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("Persona 5 mod.cpk Builder", Game = Game.Persona5)]
    public class Persona5ModCpkModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            gameName = Game.Persona5.ToString();

            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            //Get game config
            var config = ConfigStore.Get<Persona5GameConfig>() ?? throw new InvalidOperationException("Game config is missing.");

            Log.Builder.Info( "Building Persona 5 Mod" );
            Log.Builder.Info( "Processing mod files" );

            var modFilesDirectory = new VirtualDirectory( null, "mod" );
            foreach ( var entry in root )
            {
                if ( entry.EntryType == VirtualFileSystemEntryType.Directory )
                {
                    var directory = ( VirtualDirectory )entry;
                    var name = directory.Name.ToLowerInvariant();

                    switch ( name )
                    {
                        case "mod":
                        case "cache":
                        case "data":
                        case "ps3":
                        case "ps3sndjp":
                        case "patch1ps3":
                        case "patch3ps3":
                        case "ps4":
                        case "ps4sndjp":
                            {
                                // Move files in 'cpk' directory to 'mod' directory
                                LogModFilesInDirectory( directory );

                                foreach ( var modFileEntry in directory )
                                {
                                    modFileEntry.CopyTo( modFilesDirectory );
                                }
                            }
                            break;

                        default:
                            // Move directory to 'mod' directory
                            Log.Builder.Trace( $"Adding directory {entry.FullName} to mod.cpk" );
                            entry.CopyTo( modFilesDirectory );
                            break;
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace( $"Adding file {entry.FullName} to mod.cpk" );
                    entry.CopyTo( modFilesDirectory );
                }
            }

            bool.TryParse( config.Compression, out useCompression );

            // Build mod cpk
            Log.Builder.Info( "Building mod.cpk" );
            var cpkModCompiler = new CpkModBuilder();
            var cpkFilePath = hostOutputPath != null ? Path.Combine( hostOutputPath, "mod.cpk" ) : null;
            var cpkFileBuildPath = hostOutputPath != null ? FileHelper.IsFileInUse( cpkFilePath ) ? Path.Combine( Path.GetTempPath(), "mod.cpk" ) : cpkFilePath : null;
            var cpkFile = cpkModCompiler.Build( modFilesDirectory, cpkFileBuildPath, gameName, useCompression );

            if ( cpkFileBuildPath != cpkFilePath )
            {
                File.Copy( cpkFileBuildPath, cpkFilePath, true );
                File.Delete( cpkFileBuildPath );
                cpkFile = VirtualFile.FromHostFile( cpkFilePath );
            }

            Log.Builder.Info( "Done!" );

            return cpkFile;
        }

        private void LogModFilesInDirectory( VirtualDirectory directory )
        {
            foreach ( var entry in directory )
            {
                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                    Log.Builder.Trace( $"Adding mod file: {entry.FullName}" );
                else
                    LogModFilesInDirectory( (VirtualDirectory)entry );
            }
        }
    }
}