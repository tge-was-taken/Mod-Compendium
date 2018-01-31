using System.Diagnostics;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class CpkModBuilder : IModBuilder
    {
        private static readonly string sCsvPath = "cpkmaker.out.csv";

        public int Alignment { get; } = 2048;

        public string CodePage { get; } = "SJIS";

        public string Mode { get; } = "FILENAME";

        public bool DeleteCsv { get; } = true;

        /// <inheritdoc />
        public VirtualFileSystemEntry Build( VirtualDirectory root, string hostOutputPath = null )
        {
            Log.Builder.Info( $"Building CPK: {root.Name}" );

            // SerializeCore files to temporary directory
            // This is so the builder can put them in the cpk
            var tempDirectoryPath = Path.Combine( Path.GetTempPath(), "CpkModCompilerTemp_" + Path.GetRandomFileName() );

            // Copy mod directory to temp directory     
            Log.Builder.Trace( $"Copying mod files to temp directory: {tempDirectoryPath}" );
            var modDirectoryPath = root.SaveToHost( tempDirectoryPath );

            // Get meta file info for CPK
            var cpkName = root.Name + ".cpk";

            string cpkPath;
            if ( hostOutputPath == null )
            {
                cpkPath = modDirectoryPath + ".cpk";
            }
            else
            {
                cpkPath = hostOutputPath;
            }

            // Build cpk
            var arguments = $"\"{modDirectoryPath}\" \"{cpkPath}\" -align={Alignment} -code={CodePage} -mode={Mode}";
            Log.Builder.Trace( $"Running cpkmakec: {arguments}" );
            var processStartInfo = new ProcessStartInfo( "Dependencies\\CpkMaker\\cpkmakec.exe",
                                                         arguments );

            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            var process = Process.Start( processStartInfo );
            process.WaitForExit();

            if ( DeleteCsv && File.Exists( sCsvPath ) )
            {
                Log.Builder.Trace( $"Deleting CSV: {sCsvPath}" );
                File.Delete( sCsvPath );
            }

            // Create virtual CPK file entry
            VirtualFile cpkFile;
            if ( hostOutputPath == null )
            {
                // Copy cpk to memory
                var memoryStream = new MemoryStream();
                using ( var fileStream = File.OpenRead( cpkPath ) )
                    fileStream.CopyTo( memoryStream );

                memoryStream.Position = 0;

                // Create virtual file for cpk
                cpkFile = new VirtualFile( null, memoryStream, cpkName );
            }
            else
            {
                cpkFile = new VirtualFile( null, cpkPath, cpkName );
            }

            // Delete temp directory
            Log.Builder.Trace( $"Deleting temp directory: {tempDirectoryPath}" );
            Directory.Delete( tempDirectoryPath, true );

            return cpkFile;
        }
    }
}
