using System;
using System.Diagnostics;
using System.IO;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.IO;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.FileParsers;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class Persona34FileModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        public bool OutputUnmodifiedFiles { get; } = false;

        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string compression = null)
        {
            if ( root == null )
            {
                throw new ArgumentNullException( nameof( root ) );
            }

            Log.Builder.Info( $"Building {Game} Mod" );
            if ( hostOutputPath != null )
            {
                Log.Builder.Info( $"Output directory: {hostOutputPath}" );
            }

            // Get game config
            var config = ConfigManager.Get( Game ) as Persona34GameConfig;
            if ( config == null )
            {
                // Unlikely
                throw new InvalidOperationException( "Game config is missing." );
            }

            if ( string.IsNullOrWhiteSpace( config.DvdRootOrIsoPath ) )
            {
                throw new InvalidConfigException( "Dvd root path/ISO path is not specified." );
            }

            // Get files
            VirtualDirectory dvdRootDirectory;
            CDReader isoFileSystem = null;

            Log.Builder.Trace( $"RootOrIsoPath = {config.DvdRootOrIsoPath}" );

            if ( config.DvdRootOrIsoPath.EndsWith(".iso") )
            {
                Log.Builder.Info( $"Mounting ISO: {config.DvdRootOrIsoPath}" );

                if ( !File.Exists( config.DvdRootOrIsoPath ) )
                {
                    throw new InvalidConfigException( $"Dvd root path references an ISO file that does not exist: {config.DvdRootOrIsoPath}." );
                }

                // Iso file found, convert it to our virtual file system
                isoFileSystem = new CDReader( File.OpenRead( config.DvdRootOrIsoPath ), false );
                dvdRootDirectory = ( VirtualDirectory ) ConvertEntryRecursively( isoFileSystem, isoFileSystem.Root.FullName );
            }
            else
            {
                Log.Builder.Info( $"Mounting directory: {config.DvdRootOrIsoPath}" );

                if ( !Directory.Exists( config.DvdRootOrIsoPath ) )
                {
                    throw new InvalidConfigException( $"Dvd root path references a directory that does not exist: {config.DvdRootOrIsoPath}." );
                }

                // No iso file found, assume files are extracted
                dvdRootDirectory = VirtualDirectory.FromHostDirectory( config.DvdRootOrIsoPath );
                dvdRootDirectory.Name = string.Empty;
            }

            // Find system config
            var systemConfigFile = ( VirtualFile ) dvdRootDirectory[ "SYSTEM.CNF" ];
            if ( systemConfigFile == null )
            {
                throw new MissingFileException( "SYSTEM.CNF is missing from the dvd root file source." );
            }

            string executablePath;
            using ( var systemConfigStream = systemConfigFile.Open() )
            {
                executablePath = Ps2SystemConfig.GetExecutablePath( systemConfigStream, hostOutputPath == null, true );
                systemConfigStream.Position = 0;
            }          

            if ( executablePath == null )
            {
                throw new MissingFileException( "Executable file path is not specified in SYSTEM.CNF; Unable to locate executable file." );
            }

            Log.Builder.Info( $"Executable path: {executablePath}" );
            
            var executableFile = ( VirtualFile ) dvdRootDirectory[ executablePath ];
            if ( executableFile == null )
            {
                throw new MissingFileException( "The executable file is missing from the dvd root file source." );
            }

            // Some basic checks have been done, let's start generating the cvms
            var dvdRootDirectoryPath = hostOutputPath == null
                ? Path.Combine( Path.GetTempPath(), "Persona34ModCompilerTemp_" + Path.GetRandomFileName() )
                : hostOutputPath;

            Log.Builder.Trace( $"Creating (temp?) output directory: {dvdRootDirectoryPath}" );
            Directory.CreateDirectory( dvdRootDirectoryPath );

            var bgmCvmFile = ( VirtualFile ) dvdRootDirectory[ "BGM.CVM" ];
            var bgmCvmModified = false;
            var btlCvmFile = ( VirtualFile ) dvdRootDirectory[ "BTL.CVM" ];
            var btlCvmModified = false;
            var dataCvmFile = ( VirtualFile ) dvdRootDirectory[ "DATA.CVM" ];
            var dataCvmModified = false;
            var envCvmFile = ( VirtualFile ) dvdRootDirectory[ "ENV.CVM" ];
            var envCvmModified = false;

            var newDvdRootDirectory = OutputUnmodifiedFiles ? dvdRootDirectory : new VirtualDirectory();
            newDvdRootDirectory.Remove( "ZZZZZ.BIN" ); // large dummy file

            // Process mod files
            Log.Builder.Info( "Process mod files" );
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
                            bgmCvmFile.MoveTo( newDvdRootDirectory, true );
                            bgmCvmModified = true;
                            break;

                        case "btl":
                            Log.Builder.Info( "Replacing files in btl.cvm" );
                            btlCvmFile = UpdateAndRecompileCvm( btlCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "btl.cvm" ) );
                            btlCvmFile.MoveTo( newDvdRootDirectory, true );
                            btlCvmModified = true;
                            break;

                        case "data":
                            Log.Builder.Info( "Replacing files in data.cvm" );
                            dataCvmFile = UpdateAndRecompileCvm( dataCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "data.cvm" ) );
                            dataCvmFile.MoveTo( newDvdRootDirectory, true );
                            dataCvmModified = true;
                            break;

                        case "env":
                            {
                                Log.Builder.Info( "Replacing files in env.cvm" );

                                if ( envCvmFile == null )
                                {
                                    throw new MissingFileException( "Mod replaces files in env.cvm but env.cvm isn't present." );
                                }

                                envCvmFile = UpdateAndRecompileCvm( envCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "env.cvm" ) );
                                envCvmFile.MoveTo( newDvdRootDirectory, true );
                                envCvmModified = true;
                            }
                            break;

                        default:
                            Log.Builder.Info( $"Adding directory {entry.Name} to root directory" );
                            entry.MoveTo( newDvdRootDirectory, true );
                            break;
                    }
                }
                else
                {
                    Log.Builder.Info( $"Adding file {entry.Name} to root directory" );
                    entry.MoveTo( newDvdRootDirectory, true );
                }
            }

            // Patch executable
            var executableFilePath = executableFile.SaveToHost( dvdRootDirectoryPath );

            Log.Builder.Info( $"Patching executable" );
            Log.Builder.Trace( $"Executable file path: {executableFilePath}" );

            if ( bgmCvmModified )
                PatchExecutable( executableFilePath, bgmCvmFile.HostPath );

            if ( btlCvmModified )
                PatchExecutable( executableFilePath, btlCvmFile.HostPath );

            if ( dataCvmModified )
                PatchExecutable( executableFilePath, dataCvmFile.HostPath );

            if ( envCvmModified )
                PatchExecutable( executableFilePath, envCvmFile.HostPath );

            executableFile = VirtualFile.FromHostFile( executableFilePath );
            executableFile.MoveTo( newDvdRootDirectory, true );

            if ( hostOutputPath != null )
            {
                Log.Builder.Info( $"Copying files to output directory (might take a while): {hostOutputPath}" );
                newDvdRootDirectory.SaveToHost( hostOutputPath );
            }
            else
            {
                Directory.Delete( dvdRootDirectoryPath, true );
            }

            if ( hostOutputPath != null && isoFileSystem != null )
            {
                isoFileSystem.Dispose();
            }

            Log.Builder.Info($"Done" );

            return newDvdRootDirectory;
        }

        private VirtualFileSystemEntry ConvertEntryRecursively( CDReader isoFileSystem, string path )
        {
            if ( isoFileSystem.FileExists( path ) )
            {
                var fileName = Path.GetFileName( path );
                if ( fileName.EndsWith( ";1" ) )
                {
                    fileName = fileName.Substring( 0, fileName.Length - 2 );
                }

                return new VirtualFile( null, isoFileSystem.OpenFile( path, FileMode.Open ), fileName );
            }
            else
            {
                var directory = new VirtualDirectory( null, Path.GetFileName( path ) );

                foreach ( var file in isoFileSystem.GetFiles( path ) )
                {
                    var entry = ConvertEntryRecursively( isoFileSystem, file );
                    entry.MoveTo( directory );
                }

                foreach ( var subDirectory in isoFileSystem.GetDirectories( path ) )
                {
                    var entry = ConvertEntryRecursively( isoFileSystem, subDirectory );
                    entry.MoveTo( directory );
                }

                return directory;
            }
        }

        private VirtualDirectory ConvertCvmToVirtualDirectory( VirtualFile cvmFile )
        {
            using ( var stream = cvmFile.Open() )
            {
                var streamView = new StreamView( stream, 0x1800, stream.Length - 0x1800 );
                var cvmIsoFilesystem = new CDReader( streamView, false );

                var directory = ( VirtualDirectory )ConvertEntryRecursively( cvmIsoFilesystem, cvmIsoFilesystem.Root.FullName );
                directory.Name = Path.GetFileNameWithoutExtension( cvmFile.Name );

                return directory;
            }
        }

        private VirtualFile UpdateAndRecompileCvm( VirtualFile cvmFile, VirtualDirectory directory, string hostOutputPath )
        {
            // Deserialize cvm
            Log.Builder.Trace( $"Mounting CVM filesystem: {cvmFile.Name}" );
            var cvmDirectory = ConvertCvmToVirtualDirectory( cvmFile );

            // Merge contents
            LogModFilesInDirectory( directory );
            cvmDirectory.Merge( directory, true );

            // Recompile cvm
            var cvmModCompiler = new CvmModBuilder();

            Log.Builder.Trace( $"Building new CVM: {cvmFile.Name} to {hostOutputPath}" );
            return ( VirtualFile )cvmModCompiler.Build( cvmDirectory, hostOutputPath );
        }

        private void LogModFilesInDirectory(VirtualDirectory directory)
        {
            foreach ( var entry in directory )
            {
                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                    Log.Builder.Trace( $"Adding mod file: {entry.FullName}" );
                else
                    LogModFilesInDirectory( ( VirtualDirectory )entry );
            }
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
    }

    [ModBuilder("Persona 3 File Mod Builder", Game = Game.Persona3)]
    public class Persona3FileModBuilder : Persona34FileModBuilder
    {
        protected override Game Game => Game.Persona3;
    }

    [ModBuilder("Persona 4 File Mod Builder", Game = Game.Persona4)]
    public class Persona4FileModBuilder : Persona34FileModBuilder
    {
        protected override Game Game => Game.Persona4;
    }
}
