using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [GameModBuilder( Game.Persona5 )]
    public class Persona5CpkModBuilder : IModBuilder
    {
        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            Log.Builder.Info( "Building Persona 5 Mod" );

            var modFilesDirectory = new VirtualDirectory( null, "mod" );

            Log.Builder.Info( "Gathering files" );
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
                        case "ps4":
                        case "ps4sndjp":
                            {
                                // Move files in 'cpk' directory to 'mod' directory
                                foreach ( var modFileEntry in directory )
                                {
                                    Log.Builder.Trace( $"Adding {modFileEntry.FullName} to mod.cpk" );
                                    modFileEntry.CopyTo( modFilesDirectory );
                                }
                            }
                            break;

                        default:
                            // Move directory to 'mod' directory
                            Log.Builder.Trace( $"Adding {entry.FullName}" );
                            entry.CopyTo( modFilesDirectory );
                            break;
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace( $"Adding {entry.FullName}" );
                    entry.CopyTo( modFilesDirectory );
                }
            }

            // Build mod cpk
            Log.Builder.Info( "Building CPK" );
            var cpkModCompiler = new CpkModBuilder();
            var cpkFilePath = hostOutputPath != null ? Path.Combine( hostOutputPath, "mod.cpk" ) : null;
            var cpkFile = cpkModCompiler.Build( modFilesDirectory, cpkFilePath );

            Log.Builder.Info( $"Done" );

            return cpkFile;
        }
    }
}