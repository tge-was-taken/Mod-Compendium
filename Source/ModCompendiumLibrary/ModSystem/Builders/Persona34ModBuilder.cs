using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.IO;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class Persona34ModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            Log.Builder.Info( "Building Persona 3/4 Mod" );
            if ( hostOutputPath != null )
            {
                Log.Builder.Info( $"Output directory: {hostOutputPath}" );
            }

            if ( hostOutputPath == null )
            {
                throw new NotImplementedException();
            }

            // Get game config
            var config = Config.Get( Game ) as Persona34GameConfig;
            if ( config == null )
            {
                // Unlikely
                throw new InvalidOperationException( "Game config is missing." );
            }

            if ( string.IsNullOrWhiteSpace( config.DvdRootPath ) )
            {
                throw new InvalidConfigException( "Dvd root path/ISO path is not specified." );
            }

            // Get dvd root files
            VirtualDirectory dvdRootDirectory;
            CDReader isoFileSystem = null;

            Log.Builder.Trace( $"DvdRootPath = {config.DvdRootPath}" );

            if ( config.DvdRootPath.EndsWith(".iso") )
            {
                Log.Builder.Info( $"Mounting ISO: {config.DvdRootPath}" );

                if ( !File.Exists( config.DvdRootPath ) )
                {
                    throw new InvalidConfigException( $"Dvd root path references an ISO file that does not exist: {config.DvdRootPath}." );
                }

                // Iso file found, convert it to our virtual file system
                isoFileSystem = new CDReader( File.OpenRead( config.DvdRootPath ), false );
                dvdRootDirectory = ( VirtualDirectory ) ConvertEntryRecursively( isoFileSystem, null, isoFileSystem.Root.FullName );
            }
            else
            {
                Log.Builder.Info( $"Mounting directory: {config.DvdRootPath}" );

                if ( !Directory.Exists( config.DvdRootPath ) )
                {
                    throw new InvalidConfigException( $"Dvd root path references a directory that does not exist: {config.DvdRootPath}." );
                }

                // No iso file found, assume files are extracted
                dvdRootDirectory = VirtualDirectory.FromHostDirectory( config.DvdRootPath );
                dvdRootDirectory.Name = string.Empty;
            }

            // Find executable file for patching later
            var executableFile = ( VirtualFile ) dvdRootDirectory
                .SingleOrDefault( x => x.Name.StartsWith( "SL" ) && char.IsDigit( x.Name.Last() ) );

            if ( executableFile == null )
            {
                throw new MissingFileException( "The executable file is missing from the dvd root file source." );
            }

            // Some basic checks have been done, let's start generating the cvms
            var dvdRootDirectoryPath = hostOutputPath == null
                ? Path.Combine( Path.GetTempPath(), "Persona34ModCompilerTemp_" + Path.GetRandomFileName() )
                : hostOutputPath;

            Directory.CreateDirectory( dvdRootDirectoryPath );

            var bgmCvmFile = ( VirtualFile ) dvdRootDirectory[ "bgm.cvm" ];
            var btlCvmFile = ( VirtualFile ) dvdRootDirectory[ "btl.cvm" ];
            var dataCvmFile = ( VirtualFile ) dvdRootDirectory[ "data.cvm" ];
            var envCvmFile = ( VirtualFile ) dvdRootDirectory[ "env.cvm" ];

            // Process mod files
            Log.Builder.Info( "Gathering files" );
            foreach ( var entry in root )
            {
                if ( entry.EntryType == VirtualFileSystemEntryType.Directory )
                {
                    var name = entry.Name.ToLowerInvariant();
                    var directory = ( VirtualDirectory ) entry;

                    switch ( name )
                    {
                        case "bgm":
                            Log.Builder.Info( "Replacing files in bgm.cvm" );
                            bgmCvmFile = UpdateAndRecompileCvm( bgmCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "bgm.cvm" ) );
                            break;

                        case "btl":
                            Log.Builder.Info( "Replacing files in btl.cvm" );
                            btlCvmFile = UpdateAndRecompileCvm( btlCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "btl.cvm" ) );
                            break;

                        case "data":
                            Log.Builder.Info( "Replacing files in data.cvm" );
                            dataCvmFile = UpdateAndRecompileCvm( dataCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "data.cvm" ) );
                            break;

                        case "env":
                            {
                                Log.Builder.Info( "Replacing files in env.cvm" );

                                if ( envCvmFile == null )
                                {
                                    throw new MissingFileException( "Mod replaces files in env.cvm but env.cvm isn't present." );
                                }

                                envCvmFile = UpdateAndRecompileCvm( envCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "env.cvm" ) );
                            }
                            break;

                        default:
                            Log.Builder.Info( $"Adding directory {entry.Name} to root directory" );

                            if ( hostOutputPath != null )
                            {
                                // Output modded files if we're outputting to disk & not building ISO
                                entry.SaveToHost( dvdRootDirectoryPath );
                            }
                            break;
                    }
                }
                else
                {
                    Log.Builder.Info( $"Adding file {entry.Name} to root directory" );

                    if ( hostOutputPath != null )
                    {
                        // Output modded files if we're outputting to disk & not building ISO
                        entry.SaveToHost( dvdRootDirectoryPath );
                    }
                }
            }

            // Patch executable
            var executableFilePath = executableFile.SaveToHost( dvdRootDirectoryPath );

            Log.Builder.Info( $"Patching executable" );

            if ( !bgmCvmFile.StoredInMemory )
                PatchExecutable( executableFilePath, bgmCvmFile.HostPath );

            if ( !btlCvmFile.StoredInMemory )
                PatchExecutable( executableFilePath, btlCvmFile.HostPath );

            if ( !dataCvmFile.StoredInMemory )
                PatchExecutable( executableFilePath, dataCvmFile.HostPath );

            if ( envCvmFile != null && !envCvmFile.StoredInMemory )
                PatchExecutable( executableFilePath, envCvmFile.HostPath );

            if ( isoFileSystem != null )
            {
                isoFileSystem.Dispose();
            }

            Log.Builder.Info( $"Done" );

            return dvdRootDirectory;
        }

        private VirtualFileSystemEntry ConvertEntryRecursively( CDReader isoFileSystem, VirtualDirectory parent, string path )
        {
            if ( isoFileSystem.FileExists( path ) )
            {
                var fileName = Path.GetFileName( path );
                if ( fileName.EndsWith( ";1" ) )
                {
                    fileName = fileName.Substring( 0, fileName.Length - 2 );
                }

                return new VirtualFile( parent, isoFileSystem.OpenFile( path, FileMode.Open ), fileName );
            }
            else
            {
                var directory = new VirtualDirectory( parent, Path.GetFileName( path ) );

                foreach ( var file in isoFileSystem.GetFiles(path) )
                    directory.Add( ConvertEntryRecursively( isoFileSystem, directory, file ) );

                foreach ( var subDirectory in isoFileSystem.GetDirectories( path ) )
                    directory.Add( ConvertEntryRecursively( isoFileSystem, directory, subDirectory ) );

                return directory;
            }
        }

        private VirtualDirectory ConvertCvmToVirtualDirectory( VirtualFile cvmFile )
        {
            var stream = cvmFile.Open();
            var streamView = new StreamView( stream, 0x1800, stream.Length - 0x1800 );
            var cvmIsoFilesystem = new CDReader( streamView, false );

            var directory = ( VirtualDirectory )ConvertEntryRecursively( cvmIsoFilesystem, null, cvmIsoFilesystem.Root.FullName );
            directory.Name = Path.GetFileNameWithoutExtension(cvmFile.Name);

            return directory;
        }

        private VirtualFile UpdateAndRecompileCvm( VirtualFile cvmFile, VirtualDirectory directory, string hostOutputPath )
        {
            // DeserializeCore cvm
            var cvmDirectory = ConvertCvmToVirtualDirectory( cvmFile );

            // Merge contents
            cvmDirectory.Merge( directory, true );

            // Recompile cvm
            var cvmModCompiler = new CvmModBuilder();

            return ( VirtualFile )cvmModCompiler.Build( cvmDirectory, hostOutputPath );
        }

        private void PatchExecutable( string executableFilePath, string cvmFilePath )
        {
            Log.Builder.Trace( $"Patching executable for CVM: {cvmFilePath}" );

            var processStartInfo = new ProcessStartInfo( "Dependencies\\PersonaPatcher\\PersonaPatcher.exe",
                                                         $"\"{executableFilePath}\" \"{cvmFilePath}\"" );

            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            try
            {
                var process = Process.Start( processStartInfo );
                if ( process != null && !process.WaitForExit( 2000 ) && !process.HasExited )
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception)
            {
                // There's a possible condition where between HasExited returning false and Kill() the process might've exited already
            }
        }

        private void CopyOverUnmodifiedFilesImpl( List<VirtualFileSystemEntry> dvdRootModdedEntries, VirtualFile executableFile, string dvdRootDirectoryPath, VirtualDirectory dvdRootDirectory )
        {
            // Copy over unmodified files
            var modifiedFileDictionary = dvdRootModdedEntries.ToDictionary( x => x.FullName.ToLowerInvariant() );

            void RecursivelySaveFiles( VirtualFileSystemEntry entry )
            {
                if ( modifiedFileDictionary.ContainsKey( entry.FullName.ToLowerInvariant() ) )
                    return;

                if ( entry == executableFile || entry.Name.EndsWith( "cvm" ) )
                    return;

                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                {
                    entry.SaveToHost( dvdRootDirectoryPath );
                }
                else
                {
                    if ( !string.IsNullOrWhiteSpace( entry.FullName ) )
                        Directory.CreateDirectory( Path.Combine( dvdRootDirectoryPath, entry.FullName ) );

                    foreach ( var directoryEntry in ( ( VirtualDirectory )entry ) )
                    {
                        RecursivelySaveFiles( directoryEntry );
                    }
                }
            }

            RecursivelySaveFiles( dvdRootDirectory );
        }
    }

    [GameModBuilder( Game.Persona3 )]
    public class Persona3ModBuilder : Persona34ModBuilder
    {
        protected override Game Game => Game.Persona3;
    }

    [GameModBuilder( Game.Persona4 )]
    public class Persona4ModBuilder : Persona34ModBuilder
    {
        protected override Game Game => Game.Persona4;
    }
}
