using System;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("Persona 5 mod.cpk Builder", Game = Game.Persona5)]
    public class Persona5CpkModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            Log.Builder.Info( "Building Persona 5 Mod" );

            var modFilesDirectory = new VirtualDirectory( null, "mod" );

            Log.Builder.Info( "Processing mod files" );
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

            // Build mod cpk
            Log.Builder.Info( "Building mod.cpk" );
            var cpkModCompiler = new CpkModBuilder();
            var cpkFilePath = hostOutputPath != null ? Path.Combine( hostOutputPath, "mod.cpk" ) : null;
            var cpkFile = cpkModCompiler.Build( modFilesDirectory, cpkFilePath );

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