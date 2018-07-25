using System;
using System.Diagnostics;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem.Builders.Utilities;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("CPK Mod Builder")]
    public class CpkModBuilder : IModBuilder
    {
        private const string CSV_PATH = "cpkmaker.out.csv";

        public int Alignment { get; } = 2048;

        public string CodePage { get; } = "SJIS";

        public string Mode { get; } = "FILENAME";

        public bool DeleteCsv { get; } = true;

        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Log.Builder.Info($"Building CPK: {root.Name}");

            // SerializeCore files to temporary directory
            // This is so the builder can put them in the cpk
            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), "CpkModCompilerTemp_" + Path.GetRandomFileName());

            // Copy mod directory to temp directory     
            Log.Builder.Trace($"Copying mod files to temp directory: {tempDirectoryPath}");
            var modDirectoryPath = root.SaveToHost(tempDirectoryPath);

            //If compression is enabled, make CSV and get CSV path
            string csvPath = string.Empty;
            if (useCompression)
            {
                Log.Builder.Info("Compression enabled: Building new CSV file");
                csvPath = CpkCsvMaker.MakeCsv(modDirectoryPath, gameName, Path.ChangeExtension(hostOutputPath, null));
            }

            // Get meta file info for CPK
            var cpkName = root.Name + ".cpk";

            string cpkPath;
            if (hostOutputPath == null)
            {
                cpkPath = modDirectoryPath + ".cpk";
            }
            else
            {
                cpkPath = hostOutputPath;
            }

            // Build cpk
            string arguments;
            if (csvPath == string.Empty)
            {
                arguments = $"\"{Path.GetFullPath(modDirectoryPath)}\" \"{Path.GetFullPath(cpkPath)}\" -align={Alignment} -code={CodePage} -mode={Mode}";
            }
            else
            {
                arguments = $"\"{Path.GetFullPath(csvPath)}\" \"{Path.GetFullPath(cpkPath)}\" -dir=\"{modDirectoryPath}\" -align={Alignment} -mode={Mode}";
                Log.Builder.Info($"Compressing CPK (this can take a long time, please wait...)");
            }

            var processStartInfo = new ProcessStartInfo("Dependencies\\CpkMaker\\cpkmakec.exe",
                                                         arguments);

            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (DeleteCsv && File.Exists(CSV_PATH))
            {
                Log.Builder.Trace($"Deleting CSV: {CSV_PATH}");
                File.Delete(CSV_PATH);
            }

            // Create virtual CPK file entry
            VirtualFile cpkFile;
            if (hostOutputPath == null)
            {
                // Copy cpk to memory
                var memoryStream = new MemoryStream();
                using (var fileStream = File.OpenRead(cpkPath))
                    fileStream.CopyTo(memoryStream);

                memoryStream.Position = 0;

                // Create virtual file for cpk
                cpkFile = new VirtualFile(null, memoryStream, cpkName);
            }
            else
            {
                cpkFile = new VirtualFile(null, cpkPath, cpkName);
            }

            // Delete temp directory
            Log.Builder.Trace($"Deleting temp directory: {tempDirectoryPath}");
            Directory.Delete(tempDirectoryPath, true);

            return cpkFile;
        }
    }
}