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

        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            if ( root == null )
                throw new ArgumentNullException( nameof( root ) );

            Log.Builder.Info( $"Building {Game} Mod" );
            if ( hostOutputPath != null )
                Log.Builder.Info( $"Output directory: {hostOutputPath}" );

            // Get game config
            var config = ConfigStore.Get( Game ) as Persona34GameConfig ?? throw new InvalidOperationException( "Game config is missing." );

            if ( string.IsNullOrWhiteSpace( config.DvdRootOrIsoPath ) )
                throw new InvalidConfigException( "Dvd root path/ISO path is not specified." );

            // Get files
            Log.Builder.Trace( $"DvdRootOrIsoPath = {config.DvdRootOrIsoPath}" );
            var dvdRootDirectory = Persona34Common.GetRootDirectory( config, out var isoFileSystem );

            // Find system config
            var systemConfigFile = dvdRootDirectory[ "SYSTEM.CNF" ] as VirtualFile ?? throw new MissingFileException( "SYSTEM.CNF is missing from the file source." );

            string executablePath;
            using ( var systemConfigStream = systemConfigFile.Open() )
            {
                bool leaveOpen = isoFileSystem == null;
                executablePath = Ps2SystemConfig.GetExecutablePath( systemConfigStream, leaveOpen, true );
            }          

            if ( executablePath == null )
                throw new MissingFileException( "Executable file path is not specified in SYSTEM.CNF; Unable to locate executable file." );

            Log.Builder.Info( $"Executable path: {executablePath}" );
            
            var executableFile = ( VirtualFile ) dvdRootDirectory[ executablePath ] ?? throw new MissingFileException( "The executable file is missing from the dvd root file source." );

            // Some basic checks have been done, let's start generating the cvms
            var dvdRootDirectoryPath = hostOutputPath ?? Path.Combine( Path.GetTempPath(), "Persona34ModCompilerTemp_" + Path.GetRandomFileName() );

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

            var newDvdRootDirectory = new VirtualDirectory();

            // Process mod files
            Log.Builder.Info( "Processing mod files" );
            foreach ( var entry in root )
            {
                if ( entry.EntryType == VirtualFileSystemEntryType.File )
                {
                    Log.Builder.Info( $"Adding file {entry.Name} to root directory" );
                    entry.MoveTo( newDvdRootDirectory, true );
                    continue;
                }

                var name = entry.Name.ToLowerInvariant();
                var directory = ( VirtualDirectory )entry;

                switch ( name )
                {
                    case "bgm":
                        UpdateAndRecompileCvm( ref bgmCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "bgm.cvm" ), newDvdRootDirectory );
                        bgmCvmModified = true;
                        break;

                    case "btl":
                        UpdateAndRecompileCvm( ref btlCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "btl.cvm" ), newDvdRootDirectory );
                        btlCvmModified = true;
                        break;

                    case "data":
                        UpdateAndRecompileCvm( ref dataCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "data.cvm" ), newDvdRootDirectory );
                        dataCvmModified = true;
                        break;

                    case "env":
                        {
                            Log.Builder.Info( "Replacing files in env.cvm" );

                            if ( envCvmFile == null )
                                throw new MissingFileException( "Mod replaces files in env.cvm but env.cvm isn't present." );

                            UpdateAndRecompileCvm( ref envCvmFile, directory, Path.Combine( dvdRootDirectoryPath, "env.cvm" ), newDvdRootDirectory );
                            envCvmModified = true;
                        }
                        break;

                    default:
                        Log.Builder.Info( $"Adding directory {entry.Name} to root directory" );
                        entry.MoveTo( newDvdRootDirectory, true );
                        break;
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

            if ( hostOutputPath != null && isoFileSystem != null )
                isoFileSystem.Dispose();

            Log.Builder.Info( "Done" );

            return newDvdRootDirectory;
        }

        private VirtualDirectory ConvertCvmToVirtualDirectory( VirtualFile cvmFile )
        {
            using ( var stream = cvmFile.Open() )
            {
                var streamView = new StreamView( stream, 0x1800, stream.Length - 0x1800 );
                var cvmIsoFilesystem = new CDReader( streamView, false );

                var directory = cvmIsoFilesystem.ToVirtualDirectory();
                directory.Name = Path.GetFileNameWithoutExtension( cvmFile.Name );

                return directory;
            }
        }

        private void UpdateAndRecompileCvm( ref VirtualFile cvmFile, VirtualDirectory directory, string hostOutputPath, VirtualDirectory newDvdRootDirectory )
        {
            Log.Builder.Info( $"Replacing files in {cvmFile.Name}" );

            // Deserialize cvm
            Log.Builder.Trace( $"Mounting CVM filesystem: {cvmFile.Name}" );
            var cvmDirectory = ConvertCvmToVirtualDirectory( cvmFile );

            // Merge contents
            LogModFilesInDirectory( directory );
            cvmDirectory.Merge( directory, Operation.ReplaceOnly );

            // Recompile cvm
            Log.Builder.Trace( $"Building new CVM: {cvmFile.Name} to {hostOutputPath}" );
            var cvmModCompiler = new CvmModBuilder();
            cvmFile = ( VirtualFile )cvmModCompiler.Build( cvmDirectory, hostOutputPath );

            cvmFile.MoveTo( newDvdRootDirectory, true );
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
